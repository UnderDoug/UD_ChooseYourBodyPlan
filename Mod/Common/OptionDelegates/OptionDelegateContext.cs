using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UD_ChooseYourBodyPlan.Mod.Logging;

using XRL;
using XRL.CharacterBuilds;
using XRL.Collections;

namespace UD_ChooseYourBodyPlan.Mod
{
    public delegate bool BinaryOperator<T>(T X, T Y);

    [HasOptionDelegate]
    [HasModSensitiveStaticCache]
    public class OptionDelegateContext
        : IEquatable<OptionDelegateContext>
        , IDisposable
    {
        public struct SimpleDelegate
        {
            public string OptionID;
            public string Operator;
            public string TrueState;

            public readonly bool Check()
                => !IsValid()
                || !OptionID.IsOption()
                || GetOperatorDelegate()
                    ?.Invoke(OptionID.GetOption(), TrueState) is not false;

            public readonly BinaryOperator<string> GetOperatorDelegate()
                => !Operator.IsNullOrEmpty()
                ? OperatorDelegates
                    ?.GetValueOrDefault(Operator)
                : null
                ;

            public override readonly string ToString()
                => $"{OptionID}{Operator}{TrueState}";

            public readonly bool IsValid()
            {
                using var indent = new Indent(1);
                if (OptionID.IsNullOrEmpty())
                {
                    Debug.CheckNah(nameof(IsValid), $"{nameof(OptionID)} is null or empty.", Indent: indent);
                    return false;
                }
                if (GetOperatorDelegate() == null)
                {
                    Debug.CheckNah(nameof(IsValid), $"{nameof(GetOperatorDelegate)} returned null.", Indent: indent);
                    return false;
                }
                if (TrueState.IsNullOrEmpty())
                {
                    Debug.CheckNah(nameof(IsValid), $"{nameof(TrueState)} is null or empty.", Indent: indent);
                    return false;
                }
                return true;
            }
        }

        [ModSensitiveStaticCache]
        public static StringMap<SimpleDelegate> SimpleDelegates = new();

        [OptionDelegate]
        public static bool SimpleOptionDelegate(string TagValue, EmbarkBuilder Builder)
        {
            SimpleDelegates ??= new();

            if (!TryGetSimpleDelegate(TagValue, out var simpleDelegate))
            {
                if (!TryParseSimpleOptionPredicate(TagValue, out simpleDelegate))
                    return true;
                else
                    CacheSimpleOptionDelegate(simpleDelegate);
            }

            if (SimpleDelegates.IsNullOrEmpty())
                return true;

            return simpleDelegate.Check();
        }

        public static string SimpleDelegateName => $"{typeof(OptionDelegateContext)}.{nameof(SimpleOptionDelegate)}";

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

        public override string ToString()
            => $"{DelegateName ?? "MISSING_NAME"};{TagValue ?? "MISSING_TAG_VALUE"}";

        public static SimpleDelegate ParseOptionPredicate(string OptionPredicate)
        {
            using var indent = new Indent(1);

            string optionID = null;
            string operatorString = "==";
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
                foreach (var operatorDelegateString in OperatorDelegates.Keys)
                {
                    if (OptionPredicate.Contains(operatorDelegateString)
                        && OptionPredicate.Split(operatorDelegateString) is string[] operands)
                    {
                        optionID = operands[0];
                        operatorString = operatorDelegateString;
                        trueState = operands[1];
                        break;
                    }
                }
            }
            else
            if (OptionPredicate != null)
            {
                optionID = OptionPredicate;
            }
            return new SimpleDelegate
            {
                OptionID = optionID,
                Operator = operatorString,
                TrueState = trueState,
            };
        }

        public static bool TryParseSimpleOptionPredicate(string OptionPredicate, out SimpleDelegate SimpleDelegate)
            => (SimpleDelegate = ParseOptionPredicate(OptionPredicate)).IsValid();

        public static OptionDelegateContext CacheSimpleOptionDelegate(SimpleDelegate SimpleDelegate)
        {
            SimpleDelegates ??= new();
            SimpleDelegates[SimpleDelegate.ToString()] = SimpleDelegate;
            return new OptionDelegateContext
            {
                DelegateName = SimpleDelegateName,
                TagValue = SimpleDelegate.ToString(),
            };
        }

        public static OptionDelegateContext CacheSimpleOptionDelegate(string OptionPredicate)
            => TryParseSimpleOptionPredicate(OptionPredicate, out SimpleDelegate simpleDelegate)
            ? CacheSimpleOptionDelegate(simpleDelegate)
            : null
            ;

        public bool Check()
        {
            if (!IsValid())
                return true;

            if (BodyPlanFactory.Factory?.OptionDelegates?.GetValueOrDefault(DelegateName) is not OptionDelegateEntry delegateEntry)
                return true;

            var builder = GameManager.Instance?.gameObject?.GetComponent<EmbarkBuilder>();
            return builder == null
                || (delegateEntry.OptionDelegate?.Invoke(TagValue, builder) is not false);
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

        public static SimpleDelegate? GetSimpleDelegate(string TagValue)
            => !TagValue.IsNullOrEmpty()
            ? SimpleDelegates?.GetValueOrDefault(TagValue)
            : null
            ;

        public SimpleDelegate? GetSimpleDelegate()
            => DelegateName == SimpleDelegateName
            ? GetSimpleDelegate(TagValue)
            : null
            ;

        public static bool TryGetSimpleDelegate(string TagValue, out SimpleDelegate SimpleDelegate)
            => (SimpleDelegate = GetSimpleDelegate(TagValue).GetValueOrDefault()).IsValid();

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

        public bool Equals(OptionDelegateContext Other)
            => SameAs(Other);

        public void Dispose()
        {
        }
    }
}
