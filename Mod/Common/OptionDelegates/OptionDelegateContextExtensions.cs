using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UD_ChooseYourBodyPlan.Mod.Logging;

using XRL.CharacterBuilds;
using XRL.CharacterBuilds.Qud;
using XRL.Collections;
using XRL.World;

using static UD_ChooseYourBodyPlan.Mod.OptionDelegateContext;

namespace UD_ChooseYourBodyPlan.Mod
{
    [HasOptionDelegate]
    public static class OptionDelegateContextExtensions
    {
        [OptionDelegate]
        public static bool DisallowTrueKinUnless(string TagValue, EmbarkBuilder Builder)
        {
            SimpleDelegates ??= new();

            if (!SimpleDelegates.TryGetValue(TagValue, out var simpleDelegate))
            {
                if (!TryParseSimpleOptionPredicate(TagValue, out simpleDelegate))
                    return true;
                else
                    CacheSimpleOptionDelegate(simpleDelegate);
            }

            if (SimpleDelegates.IsNullOrEmpty())
                return true;

            return Builder?.GetModule<QudGenotypeModule>()?.data?.Entry?.IsTrueKin is not true
                || simpleDelegate.Check()
                ;
        }

        public static IEnumerable<bool> GetChecks(this OptionDelegateContexts OptionDelegates)
        {
            foreach (var option in OptionDelegates)
                yield return option.Check();
        }

        public static bool ContainsOptionID(
            this OptionDelegateContexts OptionDelegateContexts,
            string OptionID
            )
        {
            if (OptionDelegateContexts.IsNullOrEmpty())
                return false;

            foreach (var optionDelegate in OptionDelegateContexts)
                if (optionDelegate.DelegateName == SimpleDelegateName
                    && optionDelegate.GetSimpleDelegate()?.OptionID == OptionID)
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
            this OptionDelegateContexts OptionDelegateContexts,
            KeyValuePair<string, string> OptionTag
            )
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(OptionDelegateContexts.Count), OptionDelegateContexts?.Count ?? -1),
                    Debug.Arg(OptionTag.Key ?? "NO_KEY", OptionTag.Value ?? "NO_VALUE"),
                });

            if (OptionDelegateContexts == null)
                return false;

            string tagName = OptionTag.Key;
            string tagValue = OptionTag.Value;

            var validTags = ValidTags;

            string postTagName = tagName;
            if (validTags.Any(s => tagName.StartsWith($"{s}.")))
            {
                foreach (var validTag in validTags)
                {
                    if (postTagName.StartsWith($"{validTag}."))
                    {
                        postTagName = postTagName[(validTag.Length + 1)..];
                        break;
                    }
                }
            }

            if ((validTags.Contains(tagName)
                    || postTagName == SimpleDelegateName
                    || postTagName == "OptionID")
                && TryParseSimpleOptionPredicate(tagValue, out var simpleDelegate))
            {
                var optionDelegateContext = CacheSimpleOptionDelegate(simpleDelegate);
                OptionDelegateContexts.Add(optionDelegateContext);
                Debug.CheckYeh(nameof(OptionDelegateContext), optionDelegateContext, Indent: indent[1]);
                return true;
            }

            if (tagName.Contains("."))
            {
                if (postTagName.EqualsNoCase("remove"))
                {
                    if (tagValue.Contains(";"))
                    {
                        if (tagName.Split(";") is string[] tagParams)
                        {
                            string delegateName = tagParams[0];
                            string delegateTagValue = tagParams[1];
                            OptionDelegateContexts.RemoveWhere(o => o.DelegateName == delegateName && o.TagValue == delegateTagValue);

                            Debug.CheckYeh(nameof(OptionDelegateContext), $"{delegateName} -> {delegateTagValue}", Indent: indent[1]);
                            return true;
                        }
                    }
                    Utils.Error($"Failed to parse {nameof(OptionDelegateContext)} \"remove\" tag: [{tagName}] value: [{tagValue}]; " +
                        $"invalid tag Value attribute format. Use \"Example_OptionDelegateName;Example_OptionID==ExampleValue\".");
                }
                else
                if (BodyPlanFactory.Factory?.OptionDelegates?.ContainsKey(postTagName) is true)
                {
                    var optionDelegateContext = new OptionDelegateContext
                    {
                        DelegateName = postTagName,
                        TagValue = tagValue,
                    };
                    OptionDelegateContexts.Add(optionDelegateContext);
                    Debug.CheckYeh(nameof(OptionDelegateContext), optionDelegateContext, Indent: indent[1]);
                    return true;
                }
                else
                {
                    Utils.Error($"Failed to parse {nameof(OptionDelegateContext)} tag: [{tagName}] value: [{tagValue}]; " +
                        $"option delegate with name {postTagName} was not found.");
                }
                return false;
            }
            else
                Utils.Error($"Failed to parse {nameof(OptionDelegateContext)} tag: [{tagName}] value: [{tagValue}]; " +
                    $"unable to determine syntax.");

            Debug.CheckNah($"{nameof(OptionDelegateContext)} Invalid", $"{tagName};{tagValue}", Indent: indent[1]);
            return false;
        }

        public static Dictionary<string, string> GetOptionTags(this GameObjectBlueprint DataBucket)
            => DataBucket.GetTagsStartingWith("Option");

        public static bool ParseDataBucket(
            this OptionDelegateContexts OptionDelegates,
            GameObjectBlueprint DataBucket
            )
        {
            using Indent indent = new(1);
            Debug.Log($"{Utils.CallChain(nameof(OptionDelegateContexts), nameof(ParseDataBucket))}({Debug.Arg(DataBucket?.Name ?? "NO_DATA_BUCKET")})", Indent: indent);

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

        public static OptionDelegateContext GetOptionDelegateContext(
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

        public static OptionDelegateContext GetSimpleOptionDelegateContext(
            this OptionDelegateContexts OptionDelegates,
            string OptionID
            )
            => CheckProceed(OptionDelegates, OptionID)
            ? OptionDelegates.GetOptionDelegateContext(
                Where: delegate (OptionDelegateContext o)
                {
                    return SimpleDelegates.TryGetValue(o.TagValue, out var simpleDelegate)
                        && simpleDelegate.OptionID == OptionID;
                })
            : null
            ;

        public static bool TryGetSimpleOptionDelegateContext(
            this OptionDelegateContexts OptionDelegates,
            string OptionID,
            out OptionDelegateContext OptionDelegate
            )
            => (OptionDelegate = OptionDelegates.GetSimpleOptionDelegateContext(OptionID)) != null;
    }
}
