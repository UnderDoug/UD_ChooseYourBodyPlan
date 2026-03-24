using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

using UD_ChooseYourBodyPlan.Mod.Logging;

using XRL.CharacterBuilds;
using XRL.CharacterBuilds.Qud;
using XRL.Collections;
using XRL.World;
using XRL.World.Anatomy;

using static UD_ChooseYourBodyPlan.Mod.OptionDelegateContext;

namespace UD_ChooseYourBodyPlan.Mod
{
    [HasOptionDelegate]
    public static class OptionDelegateContextExtensions
    {
        [OptionDelegate]
        public static bool DisallowTrueKinUnless(string TagValue, BodyPlanEntry BodyPlanEntry, EmbarkBuilder Builder)
        {
            if (TagValue.FailedToGetSimpleDelegate(out var simpleDelegate))
                return true;

            return Builder?.GetModule<QudGenotypeModule>()?.data?.Entry?.IsTrueKin is not true
                || simpleDelegate.Check()
                ;
        }

        [OptionDelegate]
        public static bool DisallowCrossOrganicMechanicalUnless(string TagValue, BodyPlanEntry BodyPlanEntry, EmbarkBuilder Builder)
        {
            if (TagValue.FailedToGetSimpleDelegate(out var simpleDelegate))
                return true;

            if (simpleDelegate.Check())
                return true;

            var bodyModel = Builder.GetPlayerBodyModel();

            if (bodyModel?.GetAnatomy() is Anatomy originAnatomy
                && BodyPlanEntry.Anatomy is Anatomy destinationAnatomy)
                return originAnatomy.IsMechanical() == destinationAnatomy.IsMechanical();

            return true;
        }

        [OptionDelegate]
        public static bool OrganicMechanicalMismatch(string TagValue, BodyPlanEntry BodyPlanEntry, EmbarkBuilder Builder)
        {
            if (!TagValue.Contains(";"))
                return false;

            if (TagValue.Split(";") is not string[] rawTagValueParts)
                return false;

            bool isTextOrganicWarning = rawTagValueParts[0].EqualsNoCase("Organic");
            bool isTextMechanicalWarning = rawTagValueParts[0].EqualsNoCase("Mechanical");

            if (rawTagValueParts[1].FailedToGetSimpleDelegate(out var simpleDelegate)
                || !simpleDelegate.Check())
                return false;

            var bodyModel = Builder.GetPlayerBodyModel();

            if (bodyModel?.GetAnatomy() is Anatomy originAnatomy
                && BodyPlanEntry.Anatomy is Anatomy destinationAnatomy)
            {
                if (originAnatomy.IsMechanical()
                    && !destinationAnatomy.IsMechanical())
                    return isTextOrganicWarning;
                else
                if (!originAnatomy.IsMechanical()
                    && destinationAnatomy.IsMechanical())
                    return isTextMechanicalWarning;
            }
            return false;
        }

        public static bool FailedToGetSimpleDelegate(this string TagValue, [NotNullWhen(false)] out SimpleDelegate SimpleDelegate)
        {
            using var indent = new Indent(1);

            SimpleDelegates ??= new();

            if (!SimpleDelegates.TryGetValue(TagValue, out SimpleDelegate))
            {
                if (!TryParseSimpleOptionPredicate(TagValue, out SimpleDelegate))
                {
                    Debug.LogMethod(indent,
                        ArgPairs: new Debug.ArgPair[]
                        {
                            Debug.Arg(nameof(TagValue), TagValue),
                        });
                    return true;
                }
                else
                    CacheSimpleOptionDelegate(SimpleDelegate);
            }

            if (SimpleDelegates.IsNullOrEmpty())
            {
                Debug.LogMethod(indent,
                    ArgPairs: new Debug.ArgPair[]
                    {
                        Debug.Arg(nameof(TagValue), TagValue),
                    });
                return true;
            }

            return false;
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

            if (OptionTag.ParseOptionTag(out bool remove) is OptionDelegateContext optionDelegateContext)
            {
                if (!remove)
                    OptionDelegateContexts.Add(optionDelegateContext);
                else
                    OptionDelegateContexts.RemoveWhere(o => o.SameAs(optionDelegateContext));
                return true;
            }
            return false;
        }

        public static OptionDelegateContext ParseOptionTag(this KeyValuePair<string, string> OptionTag, out bool Remove)
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(OptionTag.Key ?? "NO_KEY", OptionTag.Value ?? "NO_VALUE"),
                });

            Remove = false;

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
                Debug.CheckYeh(nameof(OptionDelegateContext), optionDelegateContext, Indent: indent[1]);
                return optionDelegateContext;
            }

            if (tagName.Contains("."))
            {
                if (postTagName.EqualsNoCase("remove"))
                {
                    Remove = true;
                    string delegateName = null;
                    string delegateTagValue = null;
                    if (tagValue.Contains(";"))
                    {
                        if (tagName.Split(";") is string[] tagParams)
                        {
                            delegateName = tagParams[0];
                            delegateTagValue = tagParams[1];
                            Debug.CheckYeh($"Remove {nameof(OptionDelegateContext)}", $"{delegateName} -> {delegateTagValue}", Indent: indent[1]);
                        }
                    }
                    else
                    if (tagValue.Contains(","))
                    {
                        if (tagName.Split(",") is string[] tagParams)
                        {
                            delegateName = tagParams[0];
                            delegateTagValue = tagParams[1];
                            Debug.CheckYeh($"Remove {nameof(OptionDelegateContext)}", $"{delegateName} -> {delegateTagValue}", Indent: indent[1]);
                        }
                    }

                    if (!delegateName.IsNullOrEmpty()
                        && !delegateTagValue.IsNullOrEmpty())
                    {
                        return new OptionDelegateContext
                        {
                            DelegateName = delegateName,
                            TagValue = delegateTagValue,
                        };
                    }
                    else
                    {
                        Utils.Error($"Failed to parse {nameof(OptionDelegateContext)} \"remove\" tag: [{tagName}] value: [{tagValue}]; " +
                            $"invalid tag Value attribute format. Use \"Example_OptionDelegateName;Example_OptionID==ExampleValue\".");
                    }
                }
                else
                if (BodyPlanFactory.Factory?.OptionDelegates?.ContainsKey(postTagName) is true)
                {
                    var optionDelegateContext = new OptionDelegateContext
                    {
                        DelegateName = postTagName,
                        TagValue = tagValue,
                    };
                    Debug.CheckYeh(nameof(OptionDelegateContext), optionDelegateContext, Indent: indent[1]);
                    return optionDelegateContext;
                }
                else
                {
                    Utils.Error($"Failed to parse {nameof(OptionDelegateContext)} tag: [{tagName}] value: [{tagValue}]; " +
                        $"option delegate with name {postTagName} was not found.");
                }
            }
            else
            {
                Utils.Error($"Failed to parse {nameof(OptionDelegateContext)} tag: [{tagName}] value: [{tagValue}]; " +
                    $"unable to determine syntax.");
            }

            Debug.CheckNah($"{nameof(OptionDelegateContext)} Invalid", $"{tagName};{tagValue}", Indent: indent[1]);
            return null;
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
                if (OptionDelegates.ParseOptionTag(optionTag))
                    any = true;

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
