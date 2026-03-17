using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UD_ChooseYourBodyPlan.Mod
{
    public delegate bool BinaryOperator<T>(T X, T Y);

    public abstract class BaseOptionDelegate
    {
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

        public string OptionID;
        public string Operator;
        public string TrueState;

        public virtual bool EnforceValidOptionID => true;

        public virtual string OptionState => OptionID.GetOption();

        public BaseOptionDelegate()
        {
            OptionID = null;
            Operator = null;
            TrueState = null;
        }

        public BaseOptionDelegate(string OptionID)
            : this()
        {
            this.OptionID = OptionID;
        }

        public BaseOptionDelegate(string OptionID, string TrueState)
            : this(OptionID)
        {
            this.TrueState = TrueState;
        }

        public BaseOptionDelegate(string OptionID, string Operator, string TrueState)
            : this(OptionID, TrueState)
        {
            this.Operator = Operator;
        }

        public BaseOptionDelegate((string OptionID, string Operator, string TrueState) Parsed)
            : this(Parsed.OptionID, Parsed.Operator, Parsed.TrueState)
        { }

        public static (string OptionID, string Operator, string TrueState) ParseOptionPredicate(string OptionPredicate)
        {
            (string OptionID, string Operator, string TrueState) output = (null, null, null);
            int operatorCount = OperatorDelegates.Keys.Count(o => OptionPredicate.Contains(o));
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
                        output = (operands[0], operatorString, operands[1]);
                        break;
                    }
                }
            }
            else
            {
                output = (OptionPredicate, "==", "Yes");
            }
            return output;
        }

        public static bool TryParseOptionPredicate(string OptionPredicate, out string OptionID, out string Operator, out string TrueState)
        {
            var parsed = ParseOptionPredicate(OptionPredicate);
            OptionID = parsed.OptionID;
            Operator = parsed.Operator;
            TrueState = parsed.TrueState;
            return OptionID.IsOption();
        }

        public virtual bool Check()
            => GetOperatorDelegate(Operator)?.Invoke(OptionState, TrueState) is not false;

        public virtual bool IsValid()
        {
            if (OptionID.IsNullOrEmpty())
            {
                Utils.Error(new MissingFieldException(GetType().Name, nameof(OptionID)));
                return false;
            }
            if (!EnforceValidOptionID
                && !OptionID.IsOption())
            {
                Utils.Error(new InvalidOperationException(
                    $"{nameof(OptionID)} must be a valid option ID " +
                    $"because {GetType().Name} is not marked {nameof(EnforceValidOptionID)}."));
                return false;
            }

            if (!TrueState.IsNullOrEmpty()
                && !Operator.IsNullOrEmpty()
                && !OperatorDelegates.ContainsKey(Operator))
            {
                string operatorMustNotBeEmpty = $"{nameof(Operator)} must have a value if {nameof(TrueState)} is assigned";
                string operatorDelegatesMustHaveValue = $"If {nameof(Operator)} is assigned, {nameof(OperatorDelegates)} must have a matching entry";
                string operatorDelegatesKeys = OperatorDelegates.Keys.Aggregate("", (a, n) => $"{a}{(!a.IsNullOrEmpty() ? ", " : null)}\"{n}\"");
                string operatorDelegates = $"{nameof(Operator)}: {Operator}, {nameof(OperatorDelegates)} Keys: {operatorDelegatesKeys}";
                if (Operator.IsNullOrEmpty())
                    Utils.Error(new InvalidOperationException($"{operatorMustNotBeEmpty}. {operatorDelegatesMustHaveValue}. {operatorDelegates}."));
                return false;
            }

            return true;
        }

        public virtual BinaryOperator<string> GetOperatorDelegate(string Operator)
        {
            if (!IsValid())
                return null;

            return OperatorDelegates[Operator ?? "=="];
        }

        public bool SameAs(BaseOptionDelegate Other)
            => Other != null
            && OptionID != null
            && OptionID == Other.OptionID
            ;

        public virtual void Merge(BaseOptionDelegate Other)
        {
            Utils.MergeRequireField(ref OptionID, Other.OptionID);
            Utils.MergeReplaceField(ref Operator, Other.Operator);
            Utils.MergeReplaceField(ref TrueState, Other.TrueState);
        }

        public virtual BaseOptionDelegate ModifyTruth(string Operator, string TrueState)
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

        public virtual BaseOptionDelegate SetOperator(string Operator)
            => ModifyTruth(Operator, TrueState);

        public virtual BaseOptionDelegate SetTrueWhen(string TrueState)
            => ModifyTruth(Operator, TrueState);


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
    }
}
