using System;
using System.Collections.Generic;
using System.Text;

using XRL;
using XRL.Collections;
using XRL.World;

namespace UD_ChooseYourBodyPlan.Mod
{
    public static class TextElementsExtensions
    {
        public static IEnumerable<string> GetTextElementsTags(this GameObjectBlueprint DataBucket)
        {
            string startsWith = $"{nameof(TextElements)}.";
            int startsWithIndex = startsWith.Length;
            if (DataBucket.GetTagsStartingWith(startsWith) is Dictionary<string, string> tags)
            {
                Utils.Log($"{nameof(DataBucket)} {DataBucket.Name} {nameof(GetTextElementsTags)}", Indent: 0);
                foreach ((var tagName, var _) in tags)
                {
                    Utils.Log($"{nameof(tagName)}: {tagName[startsWithIndex..]}", Indent: 1);
                    yield return tagName[startsWithIndex..];
                }
            }
        }

        public static IEnumerable<TextElements> GetTextElements(
            this IEnumerable<TextElements> TextElements,
            Predicate<TextElements> Where = null
            )
        {
            foreach (var textElements in TextElements)
                if (Where?.Invoke(textElements) is not false)
                    yield return textElements;
        }

        public static IEnumerable<string> GetDescriptionBefores(
            this IEnumerable<TextElements> TextElements,
            Predicate<TextElements> Where = null
            )
        {
            foreach (var textElements in TextElements.GetTextElements(Where))
                yield return textElements.DescriptionBefore;
        }

        public static IEnumerable<string> GetDescriptionAfters(
            this IEnumerable<TextElements> TextElements,
            Predicate<TextElements> Where = null
            )
        {
            foreach (var textElements in TextElements.GetTextElements(Where))
                yield return textElements.DescriptionAfter;
        }

        public static IEnumerable<string> GetSummaryBefores(
            this IEnumerable<TextElements> TextElements,
            Predicate<TextElements> Where = null
            )
        {
            foreach (var textElements in TextElements.GetTextElements(Where))
                yield return textElements.SummaryBefore;
        }

        public static IEnumerable<string> GetSummaryAfters(
            this IEnumerable<TextElements> TextElements,
            Predicate<TextElements> Where = null
            )
        {
            foreach (var textElements in TextElements.GetTextElements(Where))
                yield return textElements.SummaryAfter;
        }

        public static IEnumerable<string> GetSymbols(
            this IEnumerable<TextElements> TextElements,
            Predicate<TextElements> Where = null,
            Predicate<TextElements.Symbol> Filter = null
            )
        {
            foreach (var textElements in TextElements.GetTextElements(Where))
            {
                if (!textElements.Symbols.IsNullOrEmpty())
                {
                    foreach (var symbol in textElements.Symbols)
                        if (Filter?.Invoke(symbol) is not false)
                            yield return symbol.ToString();
                }
            }
        }

        public static IEnumerable<string> GetLegends(
            this IEnumerable<TextElements> TextElements,
            Predicate<TextElements> Where = null
            )
        {
            foreach (var textElements in TextElements.GetTextElements(Where))
            {
                if (!textElements.LegendByName.IsNullOrEmpty())
                {
                    foreach ((var name, var legend) in textElements.LegendByName)
                    {
                        string output = legend;
                        if (textElements.SymbolsByName.ContainsKey(name))
                            output = $"{textElements.SymbolsByName.GetValue(name)} - {output}";

                        yield return output;
                    }
                }
            }
        }
    }
}
