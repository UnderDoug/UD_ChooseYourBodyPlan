using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XRL;
using XRL.CharacterBuilds;
using XRL.Collections;

namespace UD_ChooseYourBodyPlan.Mod
{
    public delegate bool BinaryOperator<T>(T X, T Y);

    [HasOptionDelegate]
    [HasModSensitiveStaticCache]
    public class OptionDelegateContext : IDisposable
    {
        public struct SimpleDelegate
        {
            public string OptionID;
            public BinaryOperator<string> Operator;
            public string TrueState;

            public readonly bool Check()
                => OptionID.IsNullOrEmpty()
                || TrueState.IsNullOrEmpty()
                || Operator == null
                || Operator.Invoke(OptionID, TrueState) is true;
        }

        [ModSensitiveStaticCache]
        public static StringMap<SimpleDelegate> SimpleDelegates = new();

        [OptionDelegate]
        public static bool SimpleOptionDelegate(string TagValue, EmbarkBuilder Builder)
        {
            if (SimpleDelegates.IsNullOrEmpty())
                return true;

            if (!SimpleDelegates.TryGetValue(TagValue, out var simpleDelegate))
                return true;

            return simpleDelegate.Check();
        }

        public static string[] ValidTags => new string[]
        {
            "Option",
            "OptionID",
            "Optional",
        };

        private static Dictionary<string, BinaryOperator<string>> OperatorDelegates => new()
        {
            { "==" , EqualsNoCase },
            { "!=", NotEqualsNoCase },
            { ">", GreaterThan },
            { ">=", GreaterThanOrEqual },
            { "<", LessThan },
            { "<=", LessThanOrEqual },
        };

        public string DelegateName;
        public string TagValue;

        public OptionDelegateContext()
        {
        }

        public OptionDelegateContext(string DelegateName, string TagValue)
            : this()
        {
            this.DelegateName = DelegateName;
            this.TagValue = TagValue;
        }

        public static SimpleDelegate ParseOptionPredicate(string OptionPredicate)
        {
            string optionID = null;
            BinaryOperator<string> operatorDelegate = GetOperatorDelegate("==");
            string trueState = "Yes";

            int operatorCount = OperatorDelegates
                ?.Keys
                ?.Aggregate(
                    seed: 0,
                    func: (a, n) => a + OptionPredicate.SubstringsOfLength(n.Length).Count(s => s.Contains(n)))
                ?? 0;
            if (operatorCount > 1)
            {
                Utils.Error(new ArgumentException($"Must not contain more than one comparison operator.", nameof(OptionPredicate)));
            }
            else
            if (operatorCount == 1)
            {
                foreach ((var operatorString, var func) in OperatorDelegates)
                {
                    if (OptionPredicate.Contains(operatorString)
                        && OptionPredicate.Split(operatorString) is string[] operands)
                    {
                        optionID = operands[0];
                        operatorDelegate = func;
                        trueState = operands[1];
                        break;
                    }
                }
            }
            return new SimpleDelegate
            {
                OptionID = optionID,
                Operator = operatorDelegate,
                TrueState =trueState,
            };
        }

        public static bool TryParseSimpleOptionPredicate(string OptionPredicate, out SimpleDelegate SimpleDelegate)
        {
            SimpleDelegate = ParseOptionPredicate(OptionPredicate);
            return SimpleDelegate.OptionID.IsOption();
        }

        public static OptionDelegateContext CacheSimpleOptionDelegate(string OptionPredicate)
        {
            if (!TryParseSimpleOptionPredicate(OptionPredicate, out SimpleDelegate simpleDelegate))
                return null;

            SimpleDelegates[OptionPredicate] = simpleDelegate;
            return new OptionDelegateContext
            {
                DelegateName = $"{typeof(OptionDelegateContext)}.{nameof(SimpleOptionDelegate)}",
                TagValue = OptionPredicate,
            };
        }

        public bool Check()
        {
            if (!IsValid())
                return true;

            if (BodyPlanFactory.Factory?.OptionDelegates?.GetValueOrDefault(DelegateName) is not OptionDelegateEntry delegateEntry)
                return true;

            var builder = GameManager.Instance?.gameObject?.GetComponent<EmbarkBuilder>();
            return builder == null
                || (delegateEntry.DelegateOption?.Invoke(TagValue, builder) is not false);
        }

        public virtual bool IsValid()
        {
            if (DelegateName.IsNullOrEmpty())
            {
                Utils.Error(new MissingFieldException(GetType().Name, nameof(DelegateName)));
                return false;
            }
            if (TagValue.IsNullOrEmpty())
            {
                Utils.Error(new MissingFieldException(GetType().Name, nameof(TagValue)));
                return false;
            }
            return true;
        }

        public static BinaryOperator<string> GetOperatorDelegate(string Operator)
            => OperatorDelegates?.GetValueOrDefault(Operator ?? "==")
            ;

        public static bool TryGetOperatorDelegate(string Operator, out BinaryOperator<string> OperatorDelegate)
            => (OperatorDelegate = GetOperatorDelegate(Operator)) != null
            ;

        protected bool SameName(OptionDelegateContext Other)
            => !DelegateName.IsNullOrEmpty()
            && DelegateName == Other?.DelegateName;

        protected bool SameTagValue(OptionDelegateContext Other)
            => !TagValue.IsNullOrEmpty()
            && TagValue == Other?.TagValue;

        public bool SameAs(OptionDelegateContext Other)
            => SameName(Other)
            && SameTagValue(Other)
            ;

        public virtual void Merge(OptionDelegateContext Other)
        {
            Utils.MergeRequireField(ref OptionID, Other.OptionID);
            Utils.MergeReplaceField(ref Operator, Other.Operator);
            Utils.MergeReplaceField(ref TrueState, Other.TrueState);
        }

        public virtual OptionDelegateContext ModifyTruth(string Operator, string TrueState)
        {
            string originalOperator = this.Operator;
            string originalTrueState = this.TrueState;
            this.Operator = Operator;
            this.TrueState = TrueState;

            if (!IsValid())
            {
                this.Operator = originalOperator;
                this.TrueState = originalTrueState;
            }
            return this;
        }

        private static bool EqualsNoCase(string X, string Y)
            => X.EqualsNoCase(Y)
            ;

        private static bool NotEqualsNoCase(string X, string Y)
            => !EqualsNoCase(X, Y)
            ;

        private static bool ParseOrError(string X, string Y, out int ResultX, out int ResultY)
        {
            ResultY = default;
            if (!int.TryParse(X.ToString(), out ResultX))
            {
                Utils.Error(new ArgumentException($"Cannot parse {X} to {typeof(int)}.", nameof(X)));
                return false;
            }

            if (!int.TryParse(Y.ToString(), out ResultY))
            {
                Utils.Error(new ArgumentException($"Cannot parse {Y} to {typeof(int)}.", nameof(Y)));
                return false;
            }
            return true;
        }

        private static bool GreaterThan(string X, string Y)
            => !ParseOrError(X, Y, out int resultX, out int resultY)
            || resultX > resultY
            ;

        private static bool GreaterThanOrEqual(string X, string Y)
            => !ParseOrError(X, Y, out int resultX, out int resultY)
            || resultX >= resultY
            ;

        private static bool LessThan(string X, string Y)
            => !ParseOrError(X, Y, out int resultX, out int resultY)
            || resultX < resultY
            ;

        private static bool LessThanOrEqual(string X, string Y)
            => !ParseOrError(X, Y, out int resultX, out int resultY)
            || resultX <= resultY
            ;

        public void Dispose()
        {
        }
    }
}
