using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UD_ChooseYourBodyPlan.Mod.Logging;

using XRL.Collections;
using XRL.World;

namespace UD_ChooseYourBodyPlan.Mod
{
    public static class OptionDelegateExtensions
    {
        public static IEnumerable<bool> GetChecks(this OptionDelegateContexts OptionDelegates)
        {
            foreach (var option in OptionDelegates)
                yield return option.Check();
        }

        public static bool Contains(
            this OptionDelegateContexts OptionDelegates,
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
            OptionDelegateContexts OptionDelegates,
            string OptionID
            )
        {
            if (OptionDelegates == null)
                return false;

            if (!OptionID.IsOption())
                return false;

            return true;
        }

        public static IEnumerable<OptionDelegateContext> GetWhere(
            this OptionDelegateContexts OptionDelegates,
            Predicate<OptionDelegateContext> Where)
        {
            if (OptionDelegates.IsNullOrEmpty())
                yield break;

            foreach (var optionDelegate in OptionDelegates)
                if (Where.Invoke(optionDelegate))
                    yield return optionDelegate;
        }

        public static bool ParseOptionTag(
            this OptionDelegateContexts OptionDelegates,
            KeyValuePair<string, string> OptionTag
            )
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(OptionDelegates.Count), OptionDelegates?.Count ?? -1),
                    Debug.Arg(OptionTag.Key ?? "NO_KEY", OptionTag.Value ?? "NO_VALUE"),
                });

            if (OptionDelegates == null)
                return false;

            string tagName = OptionTag.Key;
            string tagValue = OptionTag.Value;

            string optionID = null;
            string operatorString = null;
            string trueWhen = null;

            var validTags = OptionDelegateContext.ValidTags;

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
                            && OptionDelegateContext.TryParseOptionPredicate(tagName, out optionID, out operatorString, out trueWhen)
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
                Utils.Error($"{new ArgumentException($"Failed to parse into valid {nameof(OptionDelegateContext)}", nameof(OptionTag))}");

            Debug.YehNah(nameof(OptionDelegateContext), $"{optionID} {operatorString} {trueWhen}", Indent: indent[1]);

            return !optionID.IsNullOrEmpty()
                && OptionDelegates.Merge(optionID, operatorString, trueWhen);
        }

        public static Dictionary<string, string> GetOptionTags(this GameObjectBlueprint DataBucket)
            => DataBucket.GetTagsStartingWith("Option");

        public static bool ParseDataBucket(
            this OptionDelegateContexts OptionDelegates,
            GameObjectBlueprint DataBucket
            )
        {
            using Indent indent = new(1);
            Debug.Log($"{Utils.CallChain(nameof(Mod.OptionDelegateContexts), nameof(ParseDataBucket))}({Debug.Arg(DataBucket?.Name ?? "NO_DATA_BUCKET")})", Indent: indent);

            if (DataBucket.GetOptionTags() is not Dictionary<string, string> tags
                || tags.IsNullOrEmpty())
                return true;

            bool any = false;
            foreach (var optionTag in tags)
                any = OptionDelegates.ParseOptionTag(optionTag) || any;

            return any;
        }

        public static IEnumerable<OptionDelegateContext> GetOptionDelegates(this OptionDelegateContexts OptionDelegates)
        {
            if (OptionDelegates.IsNullOrEmpty())
                yield break;

            foreach (var optionDelegate in OptionDelegates)
                yield return optionDelegate;
        }

        public static OptionDelegateContext GetOptionDelegate(
            this OptionDelegateContexts OptionDelegates,
            Predicate<OptionDelegateContext> Where
            )
        {
            if (OptionDelegates.IsNullOrEmpty())
                return null;

            return OptionDelegates
                .GetOptionDelegates()
                .FirstOrDefault(o => Where?.Invoke(o) is not false);
        }

        public static OptionDelegateContext GetOptionDelegate(
            this OptionDelegateContexts OptionDelegates,
            string OptionID
            )
            => CheckProceed(OptionDelegates, OptionID)
            ? OptionDelegates.GetOptionDelegate(o => o.OptionID == OptionID)
            : null
            ;

        public static bool TryGetOption(
            this OptionDelegateContexts OptionDelegates,
            string OptionID,
            out OptionDelegateContext OptionDelegate
            )
            => (OptionDelegate = OptionDelegates.GetOptionDelegate(OptionID)) != null;

        public static bool Merge(
            this OptionDelegateContexts OptionDelegates,
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
            this OptionDelegateContexts OptionDelegates,
            OptionDelegateContext Source
            )
            => OptionDelegates.Merge(
                OptionID: Source.OptionID,
                Operator: Source.Operator,
                TrueState: Source.TrueState)
            ;

        public static bool RemoveOptionID(
            this OptionDelegateContexts OptionDelegates,
            string OptionID
            )
        {
            if (!CheckProceed(OptionDelegates, OptionID))
                return false;

            bool any = false;
            using var iterator = ScopeDisposedList<OptionDelegateContext>.GetFromPoolFilledWith(OptionDelegates);
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
