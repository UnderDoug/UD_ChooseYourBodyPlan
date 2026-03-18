using System;
using System.Collections.Generic;
using System.Text;

using UD_ChooseYourBodyPlan.Mod.Logging;

using XRL;
using XRL.Collections;
using XRL.World;

namespace UD_ChooseYourBodyPlan.Mod
{
    public class TextElements : ILoadFromDataBucket<TextElements>
    {
        public struct Symbol
        {
            public string Name;
            public char Color;
            public string Value;

            public Symbol(string Name, char Color, string Value)
            {
                this.Name = Name;
                this.Color = Color;
                this.Value = Value;
            }
            public Symbol(KeyValuePair<string, string> XTagEntry)
                : this()
            {
                Name = XTagEntry.Key;
                if (!XTagEntry.Value.Contains("::"))
                    Value = XTagEntry.Value;
                else
                {
                    if (XTagEntry.Value.Split("::") is string[] pair)
                    {
                        if (pair.Length > 1)
                        {
                            if (pair[0] is string dualColorString
                                && !dualColorString.IsNullOrEmpty())
                                Color = dualColorString[0];

                            if (pair[1] is string dualValueString
                                && !dualValueString.IsNullOrEmpty())
                                Value = dualValueString;
                        }
                        else
                        if (pair.Length == 1)
                        {
                            if (pair[0] is string singleValueString
                                && !singleValueString.IsNullOrEmpty())
                                Value = singleValueString;
                        }
                    }
                }
                using Indent indent = new(1);
                Debug.LogCaller(indent,
                    ArgPairs: new Debug.ArgPair[]
                    {
                        Debug.Arg(Name ?? "NO_NAME", $"{Color}|{Value}"),
                    });
            }

            public override readonly string ToString()
                => Color != default
                ? "{{" + $"{Color}|{Value}" + "}}"
                : Value.ToString()
                ;
        }
        public static string DataBucketFile => "TextElements.xml";

        public string CacheKey => Name;

        public string BaseDataBucketBlueprint => Const.TEXT_ELEMENTS_BLUEPRINT;

        public string Name;
        public string DescriptionBefore;
        public string DescriptionAfter;
        public string SummaryBefore;
        public string SummaryAfter;

        public StringMap<string> LegendByName;

        public StringMap<Symbol> SymbolsByName;

        private List<Symbol> _Symbols;
        public List<Symbol> Symbols
        {
            get
            {
                if (_Symbols.IsNullOrEmpty())
                {
                    _Symbols = new();
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

            DataBucket.AssignStringFieldFromTag(nameof(DescriptionBefore), ref DescriptionBefore);
            DataBucket.AssignStringFieldFromTag(nameof(DescriptionAfter), ref DescriptionAfter);

            DataBucket.AssignStringFieldFromTag(nameof(SummaryBefore), ref SummaryBefore);
            DataBucket.AssignStringFieldFromTag(nameof(SummaryAfter), ref SummaryAfter);

            using Indent indent = new(1);
            Debug.LogCaller(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                        Debug.Arg(DataBucket?.Name ?? "NO_DATA_BUCKET"),
                });

            if (DataBucket.TryGetXtag($"{Const.MOD_PREFIX_SHORT}{nameof(Symbols)}", out Dictionary<string, string> symbolsXTag))
            {
                SymbolsByName = new();
                foreach (var xTagEntry in symbolsXTag)
                    SymbolsByName[xTagEntry.Key] = new(xTagEntry);
            }
            if (DataBucket.GetSubTagsStartingWith(nameof(Symbols)) is Dictionary<string, string> symbolsTags)
            {
                SymbolsByName = new();
                foreach (var tagEntry in symbolsTags)
                    SymbolsByName[tagEntry.Key] = new(tagEntry);
            }

            if (DataBucket.TryGetXtag($"{Const.MOD_PREFIX_SHORT}Legend", out Dictionary<string, string> legendXTag))
            {
                LegendByName = new();
                foreach (var xTagEntry in legendXTag)
                    LegendByName[xTagEntry.Key] = xTagEntry.Value;
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

            Utils.MergeReplaceDictionary(LegendByName ??= new(), new StringMap<string>(Other.LegendByName));

            return this;
        }

        public TextElements Clone()
            => new TextElements()
                .Merge(this);

        public void Dispose()
        {
            SymbolsByName?.Clear();
            SymbolsByName = null;

            _Symbols?.Clear();
            _Symbols = null;
        }
    }
}
