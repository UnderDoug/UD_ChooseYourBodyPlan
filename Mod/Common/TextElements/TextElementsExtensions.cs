using System;
using System.Collections.Generic;
using System.Text;

using UD_ChooseYourBodyPlan.Mod.Logging;
using UD_ChooseYourBodyPlan.Mod.TextHelpers;

using XRL;
using XRL.Collections;
using XRL.World;

namespace UD_ChooseYourBodyPlan.Mod
{
    public static class TextElementsExtensions
    {
        public static string LimbElements => nameof(LimbElements);
        public static string NoCyber => nameof(NoCyber);

        public static IEnumerable<string> GetTextElementsTags(this GameObjectBlueprint DataBucket)
        {
            if (DataBucket.GetSubTagsStartingWith(nameof(TextElements)) is Dictionary<string, string> tags)
            {
                using Indent indent = new(1);
                Debug.LogMethod(indent,
                    ArgPairs: new Debug.ArgPair[]
                    {
                        Debug.Arg(DataBucket?.Name ?? "NO_DATA_BUCKET"),
                    });
                foreach ((var tagName, var _) in tags)
                {
                    Debug.Log(nameof(tagName), tagName, Indent: indent[1]);
                    yield return tagName;
                }
            }
        }
        public static Dictionary<string, Dictionary<string, string>> GetLimbElementsTags(this GameObjectBlueprint DataBucket)
        {

            if (DataBucket.GetSubTagsStartingWith(LimbElements) is not Dictionary<string, string> tags)
                return null;

            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(DataBucket?.Name ?? "NO_DATA_BUCKET"),
                });

            Dictionary<string, Dictionary<string, string>> output = new();
            foreach ((var tagName, var tagValue) in tags)
            {
                Debug.Log(tagName, tagValue, Indent: indent[1]);
                if (tagValue.IsNullOrEmpty())
                {
                    Utils.Warn($"{DataBucket.Name} has {LimbElements} tag {tagName} with empty Value attribute.");
                    continue;
                }
                output[tagName] = new(tagValue.CachedDictionaryExpansion());
            }
            return output;
        }

        public static IEnumerable<TextElements> GetTextElements(
            this IEnumerable<TextElements> TextElements,
            Predicate<TextElements> Where = null
            )
        {
            if (!TextElements.IsNullOrEmpty())
                foreach (var textElements in TextElements)
                    if (Where?.Invoke(textElements) is not false)
                        yield return textElements;
        }

        public static IEnumerable<string> GetDescriptionBefores(
            this IEnumerable<TextElements> TextElements,
            Predicate<TextElements> Where = null
            )
        {
            if (!TextElements.IsNullOrEmpty())
                foreach (var textElements in TextElements.GetTextElements(Where))
                    if (!textElements.DescriptionBefore.IsNullOrEmpty())
                        yield return textElements.DescriptionBefore;
        }

        public static IEnumerable<string> GetDescriptionAfters(
            this IEnumerable<TextElements> TextElements,
            Predicate<TextElements> Where = null
            )
        {
            if (!TextElements.IsNullOrEmpty())
                foreach (var textElements in TextElements.GetTextElements(Where))
                    if (!textElements.DescriptionAfter.IsNullOrEmpty())
                        yield return textElements.DescriptionAfter;
        }

        public static IEnumerable<string> GetSummaryBefores(
            this IEnumerable<TextElements> TextElements,
            Predicate<TextElements> Where = null
            )
        {
            if (!TextElements.IsNullOrEmpty())
                foreach (var textElements in TextElements.GetTextElements(Where))
                    if (!textElements.SummaryBefore.IsNullOrEmpty())
                        yield return textElements.SummaryBefore;
        }

        public static IEnumerable<string> GetSummaryAfters(
            this IEnumerable<TextElements> TextElements,
            Predicate<TextElements> Where = null
            )
        {
            if (!TextElements.IsNullOrEmpty())
                foreach (var textElements in TextElements.GetTextElements(Where))
                    if (!textElements.SummaryAfter.IsNullOrEmpty())
                        yield return textElements.SummaryAfter;
        }

        public static IEnumerable<string> GetSymbols(
            this IEnumerable<TextElements> TextElements,
            BodyPlan BodyPlan,
            Predicate<TextElements> Where = null,
            Predicate<Symbol> Filter = null
            )
        {
            if (!TextElements.IsNullOrEmpty())
                foreach (var textElements in TextElements.GetTextElements(Where))
                    if (!textElements.Symbols.IsNullOrEmpty())
                        foreach (var symbol in textElements.Symbols)
                            if (Filter?.Invoke(symbol) is not false)
                                yield return symbol.ToString();

            if (Utils.IsTruekinEmbarking
                && BodyPlan?.AnyNoCyber is true
                && BodyPlanFactory.Factory?.GetTextElements(NoCyber) is TextElements noCyber
                && Where?.Invoke(noCyber) is not false)
                foreach (var symbol in noCyber.GetSymbols())
                    if (Filter?.Invoke(symbol) is not false)
                    yield return symbol.ToString();
        }

        public static IEnumerable<string> GetLegends(
            this IEnumerable<TextElements> TextElements,
            BodyPlan BodyPlan,
            Predicate<TextElements> Where = null
            )
        {
            if (!TextElements.IsNullOrEmpty())
                foreach (var textElements in TextElements.GetTextElements(Where))
                    if (!textElements.LegendsByName.IsNullOrEmpty())
                        foreach (var legend in textElements.GetLegendsStrings())
                            yield return legend;

            if (Utils.IsTruekinEmbarking
                && BodyPlan?.AnyNoCyber is true
                && BodyPlanFactory.Factory?.GetTextElements(NoCyber) is TextElements noCyber
                && Where?.Invoke(noCyber) is not false)
                foreach (var legend in noCyber.GetLegendsStrings())
                    yield return legend;
        }
    }
}
