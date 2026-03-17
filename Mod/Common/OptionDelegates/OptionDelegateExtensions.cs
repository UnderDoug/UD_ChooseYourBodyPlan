using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XRL.Collections;
using XRL.World;

namespace UD_ChooseYourBodyPlan.Mod
{
    public static class OptionDelegateExtensions
    {
        public static IEnumerable<bool> GetChecks(this OptionDelegates OptionDelegates)
        {
            foreach (var option in OptionDelegates)
                yield return option.Check();
        }

        public static bool Contains(
            this OptionDelegates OptionDelegates,
            string OptionID
            )
        {
            if (OptionDelegates.IsNullOrEmpty())
                return false;

            foreach (var optionDelegate in OptionDelegates)
                if (optionDelegate.OptionID == OptionID)
                    return true;

            return false;
        }

        private static bool CheckProceed(
            OptionDelegates OptionDelegates,
            string OptionID
            )
        {
            if (OptionDelegates == null)
                return false;

            if (!OptionID.IsOption())
                return false;

            return true;
        }

        public static IEnumerable<BaseOptionDelegate> GetWhere(
            this OptionDelegates OptionDelegates,
            Predicate<BaseOptionDelegate> Where)
        {
            if (OptionDelegates.IsNullOrEmpty())
                yield break;

            foreach (var optionDelegate in OptionDelegates)
                if (Where.Invoke(optionDelegate))
                    yield return optionDelegate;
        }

        public static bool ParseOptionTag(
            this OptionDelegates OptionDelegates,
            KeyValuePair<string, string> OptionTag
            )
        {
            if (OptionDelegates.IsNullOrEmpty())
                return false;

            Utils.Log($"{OptionTag.Key}: {OptionTag.Value}", Indent: 3);
            string tagName = OptionTag.Key;
            string tagValue = OptionTag.Value;

            string optionID = null;
            string operatorString = null;
            string trueWhen = null;

            var validTags = BaseOptionDelegate.ValidTags;

            if (validTags.Any(s => tagName == s))
            {
                optionID = tagValue;
                operatorString = null;
                trueWhen = null;
            }
            else
            if (tagName.Contains("."))
            {
                bool startsWithAny = validTags.Any(s => tagName.StartsWith($"{s}."));
                if (startsWithAny)
                {
                    if (tagName.Split(".") is string[] nameParams)
                    {
                        if (nameParams[1].IsOption())
                        {
                            optionID = nameParams[1];
                            operatorString = null;
                            trueWhen = tagValue;
                        }
                        else
                        if (tagName.IsOption())
                        {
                            if (nameParams[1].EqualsNoCase("remove"))
                            {
                                optionID = tagName;
                                operatorString = null;
                                trueWhen = Const.REMOVE_TAG;
                            }
                            else
                            if (nameParams[1].EqualsNoCase("require")
                                && !OptionDelegates.Contains(tagName))
                            {
                                optionID = tagName;
                                operatorString = null;
                                trueWhen = null;
                            }
                        }
                        else
                        if (nameParams[1].EqualsNoCase("require")
                            && BaseOptionDelegate.TryParseOptionPredicate(tagName, out optionID, out operatorString, out trueWhen)
                            && OptionDelegates.Contains(optionID))
                        {
                            optionID = null;
                            operatorString = null;
                            trueWhen = null;
                        }
                    }
                }
            }
            else
            if (optionID.IsNullOrEmpty())
                Utils.ThisMod.Error($"{new ArgumentException($"Failed to parse into valid {nameof(BaseOptionDelegate)}", nameof(OptionTag))}");

            Utils.Log($"{nameof(BaseOptionDelegate)}: {optionID} {operatorString} {trueWhen}", Indent: 4);

            return !optionID.IsNullOrEmpty()
                && OptionDelegates.Merge(optionID, operatorString, trueWhen);
        }

        public static IEnumerable<KeyValuePair<string, string>> GetOptionTags(this GameObjectBlueprint DataBucket)
            => DataBucket.GetTagsStartingWith("Option");

        public static bool ParseDataBucket(
            this OptionDelegates OptionDelegates,
            GameObjectBlueprint DataBucket
            )
        {
            if (DataBucket.GetOptionTags() is not IEnumerable<KeyValuePair<string, string>> tags
                || tags.IsNullOrEmpty())
                return true;

            Utils.Log($"{DataBucket.Name}: {nameof(BaseOptionDelegate)}:", Indent: 2);
            bool any = false;
            foreach (var optionTag in tags)
                any = OptionDelegates.ParseOptionTag(optionTag) || any;

            return any;
        }

        public static IEnumerable<BaseOptionDelegate> GetOptionDelegates(this OptionDelegates OptionDelegates)
        {
            if (OptionDelegates.IsNullOrEmpty())
                yield break;

            foreach (var optionDelegate in OptionDelegates)
                yield return optionDelegate;
        }

        public static BaseOptionDelegate GetOptionDelegate(
            this OptionDelegates OptionDelegates,
            Predicate<BaseOptionDelegate> Where
            )
        {
            if (OptionDelegates.IsNullOrEmpty())
                return null;

            return OptionDelegates
                .GetOptionDelegates()
                .FirstOrDefault(o => Where?.Invoke(o) is not false);
        }

        public static BaseOptionDelegate GetOptionDelegate(
            this OptionDelegates OptionDelegates,
            string OptionID
            )
            => CheckProceed(OptionDelegates, OptionID)
            ? OptionDelegates.GetOptionDelegate(o => o.OptionID == OptionID)
            : null
            ;

        public static bool TryGetOption(
            this OptionDelegates OptionDelegates,
            string OptionID,
            out BaseOptionDelegate OptionDelegate
            )
            => (OptionDelegate = OptionDelegates.GetOptionDelegate(OptionID)) != null;

        public static bool Merge(
            this OptionDelegates OptionDelegates,
            string OptionID,
            string Operator,
            string TrueState
            )
        {
            if (CheckProceed(OptionDelegates, OptionID))
                return false;

            if (!OptionDelegates.TryGetOption(OptionID, out var existingOption))
            {
                if (!TrueState.IsNullOrEmpty()
                    && new OptionDelegate(OptionID, Operator, TrueState) is OptionDelegate newOption
                    && newOption.IsValid())
                {
                    OptionDelegates.Add(newOption);
                    return true;
                }
                return false;
            }

            if ((TrueState.IsNullOrEmpty()
                    || TrueState.EqualsNoCase(Const.REMOVE_TAG))
                && OptionDelegates.RemoveOptionID(OptionID))
                return true;

            if (!existingOption.ModifyTruth(Operator, TrueState).IsValid())
            {
                OptionDelegates.Remove(existingOption);
                return false;
            }

            return true;
        }

        public static bool Merge(
            this OptionDelegates OptionDelegates,
            BaseOptionDelegate Source
            )
            => OptionDelegates.Merge(
                OptionID: Source.OptionID,
                Operator: Source.Operator,
                TrueState: Source.TrueState)
            ;

        public static bool RemoveOptionID(
            this OptionDelegates OptionDelegates,
            string OptionID
            )
        {
            if (!CheckProceed(OptionDelegates, OptionID))
                return false;

            bool any = false;
            using var iterator = ScopeDisposedList<BaseOptionDelegate>.GetFromPoolFilledWith(OptionDelegates);
            foreach (var optionDelegate in iterator)
            {
                if (optionDelegate.OptionID == OptionID)
                {
                    OptionDelegates.Remove(optionDelegate);
                    any = true;
                }
            }
            return any;
        }
    }
}
