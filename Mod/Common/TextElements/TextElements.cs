using System;
using System.Collections.Generic;
using System.Text;

using XRL;
using XRL.Collections;
using XRL.World;

using UD_ChooseYourBodyPlan.Mod.Logging;
using UD_ChooseYourBodyPlan.Mod.TextHelpers;

namespace UD_ChooseYourBodyPlan.Mod
{
    public class TextElements : ILoadFromDataBucket<TextElements>
    {
        public static string LoadingDataBucket => "TextElements";

        public string CacheKey => Name;

        public string BaseDataBucketBlueprint => Const.TEXT_ELEMENTS_BLUEPRINT;

        public string Name;
        public string DescriptionBefore;
        public string DescriptionAfter;
        public string SummaryBefore;
        public string SummaryAfter;

        public StringMap<string> LegendsByName;

        public StringMap<Symbol> SymbolsByName;

        private List<Symbol> _Symbols;
        public List<Symbol> Symbols
        {
            get
            {
                if (_Symbols.IsNullOrEmpty())
                {
                    _Symbols = new();
                    SymbolsByName ??= new();
                    using var values = SymbolsByName.Values.GetEnumerator();
                    while (values.MoveNext())
                        _Symbols.Add(values.Current);
                }
                return _Symbols;
            }
        }

        public TextElements LoadFromDataBucket(GameObjectBlueprint DataBucket)
        {
            if (!ILoadFromDataBucket<TextElements>.CheckIsValidDataBucket(this, DataBucket))
                return null;

            if (!DataBucket.TryGetTagValueForData(nameof(TextElements), out Name))
                DataBucket.TryGetTagValueForData(nameof(Name), out Name);

            if (Name.IsNullOrEmpty())
                return null;

            using Indent indent = new(1);
            Debug.LogCaller(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(DataBucket?.Name ?? "NO_DATA_BUCKET"),
                });

            DataBucket.AssignStringFieldFromTag(nameof(DescriptionBefore), ref DescriptionBefore);
            DataBucket.AssignStringFieldFromTag(nameof(DescriptionAfter), ref DescriptionAfter);

            DataBucket.AssignStringFieldFromTag(nameof(SummaryBefore), ref SummaryBefore);
            DataBucket.AssignStringFieldFromTag(nameof(SummaryAfter), ref SummaryAfter);

            SymbolsByName ??= new();
            if (DataBucket.TryGetXtag($"{Const.MOD_PREFIX_SHORT}{nameof(Symbols)}", out Dictionary<string, string> symbolsXTag))
            {
                foreach (var xTagEntry in symbolsXTag)
                    SymbolsByName[xTagEntry.Key] = new(xTagEntry);
            }
            if (DataBucket.GetSubTagsStartingWith(nameof(Symbols)) is Dictionary<string, string> symbolsTags)
            {
                foreach (var tagEntry in symbolsTags)
                    SymbolsByName[tagEntry.Key] = new(tagEntry);
            }

            LegendsByName ??= new();
            if (DataBucket.TryGetXtag($"{Const.MOD_PREFIX_SHORT}Legend", out Dictionary<string, string> legendXTag))
            {
                foreach (var xTagEntry in legendXTag)
                    LegendsByName[xTagEntry.Key] = xTagEntry.Value;
            }
            if (DataBucket.GetSubTagsStartingWith("Legend") is Dictionary<string, string> LegendTags)
            {
                foreach (var tagEntry in LegendTags)
                    LegendsByName[tagEntry.Key] = tagEntry.Value;
            }
            return this;
        }

        public bool SameAs(TextElements Other)
            => Name == Other?.Name
            ;

        public TextElements Merge(TextElements Other)
        {
            Name ??= Other.Name;
            Utils.MergeReplaceField(ref DescriptionBefore, Other.DescriptionBefore);
            Utils.MergeReplaceField(ref DescriptionAfter, Other.DescriptionAfter);
            Utils.MergeReplaceField(ref SummaryBefore, Other.SummaryBefore);
            Utils.MergeReplaceField(ref SummaryAfter, Other.SummaryAfter);

            Utils.MergeReplaceDictionary(SymbolsByName ??= new(), new StringMap<Symbol>(Other.SymbolsByName));
            _Symbols = null;

            Utils.MergeReplaceDictionary(LegendsByName ??= new(), new StringMap<string>(Other.LegendsByName));

            return this;
        }

        public TextElements Clone()
            => new TextElements()
                .Merge(this);

        public Symbol GetSymbol(string Name)
            => SymbolsByName?.GetValue(Name)
            ?? default
            ;

        public Symbol GetSymbol()
            => SymbolsByName?.GetValue(Name)
            ?? default
            ;

        public string GetSymbolString(string Name)
            => SymbolsByName?.GetValue(Name).ToString()
            ;

        public IEnumerable<Symbol> GetSymbols(Predicate<Symbol> WhereName = null)
        {
            if (SymbolsByName.IsNullOrEmpty())
                yield break;

            foreach (var symbol in SymbolsByName.Values)
            {
                if (WhereName?.Invoke(symbol) is false)
                    continue;

                yield return symbol;
            }
        }

        public IEnumerable<string> GetSymbolsStrings(Predicate<string> WhereName = null)
        {
            if (SymbolsByName.IsNullOrEmpty())
                yield break;

            foreach ((var name, var _) in SymbolsByName)
            {
                if (WhereName?.Invoke(name) is false)
                    continue;

                yield return GetSymbolString(name);
            }
        }

        public string GetLegendString(string Name)
        {
            if (!LegendsByName.TryGetValue(Name, out var legend))
                return null;

            return SymbolsByName?.GetValue(Name).ToString() is string symbol
                    && !symbol.IsNullOrEmpty()
                ? $"{symbol} - {legend}"
                : legend
                ;
        }

        public IEnumerable<string> GetLegendsStrings(Predicate<string> WhereName = null)
        {
            if (LegendsByName.IsNullOrEmpty())
                yield break;

            foreach ((var name, var _) in LegendsByName)
            {
                if (WhereName?.Invoke(name) is false)
                    continue;

                yield return GetLegendString(name);
            }
        }

        public void Dispose()
        {
            SymbolsByName?.Clear();
            SymbolsByName = null;

            _Symbols?.Clear();
            _Symbols = null;
        }
    }
}
