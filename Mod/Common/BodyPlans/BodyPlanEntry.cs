using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ConsoleLib.Console;

using XRL;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;

using Event = XRL.World.Event;

using UD_ChooseYourBodyPlan.Mod.Logging;
using UD_ChooseYourBodyPlan.Mod.TextHelpers;

using static UD_ChooseYourBodyPlan.Mod.ILoadFromDataBucket<UD_ChooseYourBodyPlan.Mod.BodyPlanEntry>;

namespace UD_ChooseYourBodyPlan.Mod
{
    public class BodyPlanEntry: ILoadFromDataBucket<BodyPlanEntry>
    {
        public class LimbTextElements
        {
            public static LimbTextElements NaturalEquipment => new() { Shader = new Shader { Color = "W" }.Finalize() };
            public static LimbTextElements NoCyber => new() { Symbol = BodyPlanFactory.Factory?.GetTextElements(nameof(NoCyber))?.GetSymbol() ?? default };

            public string Type;
            public Shader Shader;
            public string PostText;
            public Symbol Symbol;
            public bool OverridesNaturalEquipmentColor;
            public bool OverridesNaturalEquipmentStats;

            public LimbTextElements()
            {
                Type = null;
                Shader = default;
                PostText = null;
                Symbol = default;
                OverridesNaturalEquipmentColor = false;
                OverridesNaturalEquipmentStats = false;
            }
            public LimbTextElements(
                string Type,
                Shader Shader,
                string PostText,
                Symbol Symbol
                )
                : this()
            {
                this.Type = Type;
                this.Shader = Shader;
                this.PostText = PostText;
                this.Symbol = Symbol;
            }

            public LimbTextElements(
                BodyPart Limb,
                Shader Shader,
                string PostText,
                Symbol Symbol
                )
                : this(
                      Type: Limb?.VariantTypeModel()?.Type,
                      Shader: Shader,
                      PostText: PostText,
                      Symbol: Symbol)
            { }

            public override string ToString()
            {
                string output = Type ?? "NO_TYPE";

                if (!Equals(Shader, default))
                    output = $"{output}[{nameof(Shader)}:{Shader}]";

                if (!PostText.IsNullOrEmpty())
                    output = $"{output}[{nameof(PostText)}:{PostText}]";

                if (!Equals(Symbol, default))
                    output = $"{output}[{nameof(Symbol)}:{Symbol.DebugString()}]";

                return output;
            }

            public bool CheckType(string Type)
                => this.Type.IsNullOrEmpty()
                || this.Type.Equals(Type)
                ;

            public bool CheckLimb(BodyPart Limb)
                => !Type.IsNullOrEmpty()
                && CheckType(Limb?.VariantTypeModel()?.Type)
                ;

            public string ProcessColor(string LimbDescription, string Type = null)
                => CheckType(Type)
                ? Shader.Apply(LimbDescription)
                : LimbDescription
                ;

            public string ProcessPost(string LimbDescription, string Type = null)
            {
                if (!CheckType(Type))
                    return LimbDescription;

                if (!PostText.IsNullOrEmpty())
                    LimbDescription = $"{LimbDescription} {PostText}";

                return LimbDescription;
            }

            public string ProcessSymbol(string LimbDescription, string Type = null)
            {
                if (!CheckType(Type))
                    return LimbDescription;

                if (Symbol.ToString() is string symbol
                    && !symbol.IsNullOrEmpty())
                    LimbDescription = $"{LimbDescription}{symbol}";

                return LimbDescription;
            }
        }

        public const string NO_LIST_TAG = "NoList";
        public static string LoadingDataBucket => "BodyPlans";

        public static string MISSING_ANATOMY => nameof(MISSING_ANATOMY);

        public static BodyPlanFactory Factory => BodyPlanFactory.Factory;

        public string BaseDataBucketBlueprint => Const.BODYPLAN_ENTRY_BLUEPRINT;

        public string CacheKey => AnatomyName;

        public string AnatomyName;

        public Anatomy Anatomy
            => !AnatomyName.IsNullOrEmpty()
            ? Anatomies.GetAnatomy(AnatomyName)
            : null
            ;

        protected MergeType MergeType = MergeType.HardReplace;

        protected string _CategoryOverride;
        public string CategoryOverride
        {
            get => _CategoryOverride;
            protected set => _CategoryOverride = value;
        }

        private AnatomyCategoryEntry _Category;
        public AnatomyCategoryEntry Category => _Category ??= Factory?.GetAnatomyCategoryEntryFor(this);

        public string DisplayName;

        public BodyPlanRender Render;

        public OptionDelegateContexts OptionDelegateContexts;

        private TransformationData _Transformation;
        public TransformationData Transformation
        {
            get
            {
                if (WantsTransformation
                    && (Factory?.TransformationDataInitialized ?? false)
                    && _Transformation == null)
                {
                    WantsTransformation = false;
                    _Transformation = Factory.GetTransformationData(this);
                    if (_Transformation != null)
                    {
                        OptionDelegateContexts ??= new();
                        OptionDelegateContexts.AddRange(_Transformation.OptionDelegateContexts);

                        TextElementsNames ??= new();
                        TextElementsNames.AddRange(_Transformation.GetTextElementsNames());
                        WantsTextElements = TextElementsNames.Count > 0;

                        NaturalEquipment ??= new();
                        if (!_Transformation.NaturalEquipment.IsNullOrEmpty())
                            NaturalEquipment.AddRange(_Transformation.NaturalEquipment);
                    }
                }
                return _Transformation;
            }
        }
        protected bool WantsTransformation;

        private HashSet<string> TextElementsNames;

        private List<TextElements> _TextElements;
        public List<TextElements> TextElements
        {
            get
            {
                if (WantsTextElements
                    && Factory.TextElementsInitialized
                    && _TextElements.IsNullOrEmpty())
                {
                    WantsTextElements = false;
                    _TextElements = new();

                    if (!TextElementsNames.IsNullOrEmpty())
                        foreach (var textElementsName in TextElementsNames)
                            if (Factory.TextElementsByName.TryGetValue(textElementsName, out var textElements))
                                _TextElements.Add(textElements);
                }
                return _TextElements;
            }
        }
        protected bool WantsTextElements;

        public Dictionary<string, List<LimbTextElements>> LimbElementsByType;

        public List<InventoryObject> NaturalEquipment;

        public int RandomWeight;

        public Dictionary<string, string> Tags;

        public BodyPlanEntry()
        {
            AnatomyName = null;
            _Category = null;
            Render = null;

            _Transformation = null;
            WantsTransformation = true;

            TextElementsNames = null;
            _TextElements = null;
            WantsTextElements = true;

            LimbElementsByType = null;

            NaturalEquipment = null;

            Tags = null;
        }
        public BodyPlanEntry(Anatomy Anatomy, BodyPlanRender Render)
            : this()
        {
            AnatomyName = Anatomy.Name;
            this.Render = Render;

            _ = Category;
        }
        public BodyPlanEntry(Anatomy Anatomy)
            : this(Anatomy, null)
        {
        }
        public BodyPlanEntry(BodyPlanEntry Source)
            : this()
        {
            AnatomyName = Source?.AnatomyName;
            MergeType = Source?.MergeType ?? MergeType;

            CategoryOverride = Source?.CategoryOverride;
            DisplayName = Source?.DisplayName;
            Render = Source?.Render?.Clone();

            OptionDelegateContexts = new();
            if (Source?.OptionDelegateContexts != null
                && !Source.OptionDelegateContexts.IsNullOrEmpty())
                OptionDelegateContexts.AddRange(Source.OptionDelegateContexts);

            WantsTransformation = Source?._Transformation != null;

            TextElementsNames = new();
            if (Source?.TextElementsNames != null
                && !Source.TextElementsNames.IsNullOrEmpty())
                foreach (var textElementsName in Source.TextElementsNames)
                    TextElementsNames.Add(textElementsName);
            WantsTextElements = !TextElementsNames.IsNullOrEmpty();

            LimbElementsByType = new();
            if (Source?.LimbElementsByType != null
                && !Source.LimbElementsByType.IsNullOrEmpty())
                foreach ((var limbType, var limbElements) in Source.LimbElementsByType)
                    LimbElementsByType.Add(limbType, new(limbElements));

            RandomWeight = Source?.RandomWeight ?? 0;

            Tags = new();
            if (Source?.Tags != null
                && !Source.Tags.IsNullOrEmpty())
                foreach ((var tagName, var tagValue) in Source.Tags)
                    Tags.Add(tagName, tagValue);
        }

        public static bool IsAvailable(BodyPlanEntry BodyPlanEntry)
            => BodyPlanEntry !=null
            && BodyPlanEntry.IsAvailable();

        public BodyPlanEntry ClearCaches()
        {
            _Category = null;

            _Transformation = null;
            WantsTransformation = true;

            _TextElements?.Clear();
            _TextElements = null;

            return this;
        }

        public BodyPlanEntry LoadFromDataBucket(GameObjectBlueprint DataBucket)
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(DataBucket?.Name ?? "NO_DATA_BUCKET"),
                });

            if (!CheckIsValidDataBucket(this, DataBucket))
            {
                Dispose();
                return null;
            }

            if (!DataBucket.TryGetTagValueForData(nameof(Anatomy), out AnatomyName))
            {
                Dispose();
                return null;
            }

            if (AnatomyName != "UD_CYBP_NoEntry")
            {
                if (Anatomy == null)
                {
                    Dispose();
                    return null;
                }
            }

            MergeType = DataBucket.GetTag("Merge")?.ToLower() switch
            {
                "hard" or
                "hardreplace" or
                "overwrite" => MergeType.HardReplace,

                "soft" or
                "softreplace" or
                "after" => MergeType.SoftReplace,

                "require" or
                "onlynull" or
                "fill" or 
                "before" => MergeType.Require,

                _ => MergeType.None,
            };

            DisplayName = AnatomyName?.SplitCamelCase();
            
            DataBucket.AssignStringFieldFromTag(nameof(Category), ref _CategoryOverride);
            DataBucket.AssignStringFieldFromTag(nameof(CategoryOverride), ref _CategoryOverride);
            DataBucket.AssignStringFieldFromTag(nameof(DisplayName), ref DisplayName);

            Render = new BodyPlanRender().LoadFromDataBucket(DataBucket);

            OptionDelegateContexts ??= new();
            OptionDelegateContexts.ParseDataBucket(DataBucket);

            TextElementsNames ??= new();
            if (DataBucket.GetTextElementsTags() is IEnumerable<string> textElementsTags)
                foreach (var textElementsName in textElementsTags)
                    TextElementsNames.Add(textElementsName);

            LimbElementsByType ??= new();
            if (DataBucket.GetLimbElementsTags() is Dictionary<string, Dictionary<string, string>> limbElementsTags)
            {
                foreach ((var limbType, var rawLimbElements) in limbElementsTags)
                {
                    if (!LimbElementsByType.ContainsKey(limbType))
                        LimbElementsByType[limbType] = new();

                    Shader shader = default;

                    if (rawLimbElements.GetValue(nameof(Shader.Color)) is string rawShaderColor)
                        shader = new Shader { Color = rawShaderColor }.Finalize();
                    else
                    if (rawLimbElements.GetValue(nameof(Shader)).CachedCommaExpansion()?.ToList() is List<string> rawShader)
                    {
                        if (rawShader.Count > 2)
                            shader = new Shader { Colors = rawShader[0], Type = rawShader[1], Color = rawShader[2] }.Finalize();
                        else
                        if (rawShader.Count > 1)
                            shader = new Shader { Value = rawShader[0], Color = rawShader[1] }.Finalize();
                        else
                            shader = new Shader { Value = rawShader[0] }.Finalize();
                    }
                    Symbol symbol = default;
                    if (rawLimbElements.GetValue(nameof(Symbol)).CachedCommaExpansion()?.ToList() is List<string> rawSymbol)
                    {
                        if (rawSymbol.Count > 2)
                            symbol = new(rawSymbol[0], rawSymbol[1][0], rawSymbol[2]);
                        else
                        if (rawSymbol.Count > 1)
                            symbol = new(null, rawSymbol[0][0], rawSymbol[1]);
                        else
                            symbol = new(null, default, rawSymbol[0]);
                    }

                    bool overridesNaturalEquipmentColor = rawLimbElements.GetValue(nameof(LimbTextElements.OverridesNaturalEquipmentColor)) is string overridesColor
                        && overridesColor.EqualsNoCase("Yes");

                    bool overridesNaturalEquipmentStats = rawLimbElements.GetValue(nameof(LimbTextElements.OverridesNaturalEquipmentStats)) is string overridesStats
                        && overridesStats.EqualsNoCase("Yes");

                    LimbElementsByType[limbType].Add(
                        item: new LimbTextElements
                        {
                            Type = limbType,
                            Shader = shader,
                            PostText = rawLimbElements.GetValueOrDefault(nameof(LimbTextElements.PostText)),
                            Symbol = symbol,
                            OverridesNaturalEquipmentColor = overridesNaturalEquipmentColor,
                            OverridesNaturalEquipmentStats = overridesNaturalEquipmentStats,
                        });
                }
            }

            NaturalEquipment ??= new();
            if (!DataBucket.Inventory.IsNullOrEmpty())
                NaturalEquipment.AddRange(DataBucket.Inventory);

            if (DataBucket.TryGetTagValueForData(nameof(RandomWeight), out string randomWeight)
                && !int.TryParse(randomWeight, out RandomWeight))
                RandomWeight = 5;

            Tags ??= new();
            foreach ((string tagName, string tagValue) in DataBucket.Tags)
                Tags[tagName] = tagValue;

            LogDebug(1);
            return this;
        }

        public BodyPlanEntry LoadFromAnatomy(Anatomy Anatomy)
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(Anatomy?.Name ?? "NO_ANATOMY"),
                });
            if (Anatomy == null)
            {
                Dispose();
                return null;
            }
            AnatomyName = Anatomy?.Name;
            DisplayName = Anatomy?.Name?.SplitCamelCase();
            if (Anatomy.IsMechanical())
            {
                TextElementsNames ??= new();
                TextElementsNames.Add("Mechanical");
            }

            Tags.ClearNoInherits();
            Tags.ClearRemoves();

            LogDebug(1);
            return this;
        }

        public bool SameAs(BodyPlanEntry Other)
            => Anatomy?.Name == Other?.Anatomy?.Name
            ;

        public void MergeTags(IDictionary<string, string> OtherTags)
        {
            Tags ??= new();
            Tags.ClearNoInherits();
            if (!OtherTags.IsNullOrEmpty())
            {
                switch (MergeType)
                {
                    case MergeType.Require:
                        Utils.MergeRequireDictionary(Tags ??= new(), OtherTags);
                        break;

                    case MergeType.SoftReplace:
                        Utils.MergeReplaceDictionary(Tags ??= new(), OtherTags);
                        break;

                    default:
                    case MergeType.HardReplace:
                        Tags = new(OtherTags);
                        break;

                }
            }
            Tags.ClearRemoves();
        }

        public BodyPlanEntry MergeHardReplace(BodyPlanEntry Other)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg($"{AnatomyName ?? "NO_ANATOMY_NAME"} <- {Other?.AnatomyName ?? "NO_OTHER_ANATOMY_NAME"}"),
                });

            AnatomyName = Other.AnatomyName;

            CategoryOverride = Other.CategoryOverride ?? Other.Category?.CategoryName;
            DisplayName = Other.DisplayName;
            Render = new(Other.Render);

            OptionDelegateContexts ??= new();
            OptionDelegateContexts.Clear(); 
            OptionDelegateContexts.AddRange(Other.OptionDelegateContexts);

            TextElementsNames ??= new();
            TextElementsNames.Clear();
            TextElementsNames.AddRange(Other.TextElementsNames);
            WantsTextElements = TextElementsNames.Count > 0;

            LimbElementsByType ??= new();
            LimbElementsByType.Clear();
            foreach ((var limbType, var limbElements) in Other.LimbElementsByType)
                if (!limbElements.IsNullOrEmpty())
                    LimbElementsByType[limbType] = new(limbElements);

            NaturalEquipment ??= new();
            NaturalEquipment.Clear();
            if (!Other.NaturalEquipment.IsNullOrEmpty())
                NaturalEquipment.AddRange(Other.NaturalEquipment);

            RandomWeight = Other.RandomWeight;

            MergeTags(Other.Tags);

            MergeType = Other.MergeType;

            LogDebug(1);
            return this;
        }

        public BodyPlanEntry MergeSoftReplace(BodyPlanEntry Other)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg($"{AnatomyName ?? "NO_ANATOMY_NAME"} <- {Other?.AnatomyName ?? "NO_OTHER_ANATOMY_NAME"}"),
                });

            if (Anatomy == null)
                AnatomyName = Other.AnatomyName;

            Utils.MergeReplaceField(ref _CategoryOverride, Other.CategoryOverride ?? Other.Category?.CategoryName);
            Utils.MergeReplaceField(ref DisplayName, Other.DisplayName);
            Render.Merge(Other.Render);

            OptionDelegateContexts ??= new();
            OptionDelegateContexts.AddRange(Other.OptionDelegateContexts);

            TextElementsNames ??= new();
            TextElementsNames.AddRange(Other.TextElementsNames);
            WantsTextElements = TextElementsNames.Count > 0;

            LimbElementsByType ??= new();
            foreach ((var limbType, var limbElements) in Other.LimbElementsByType)
            {
                if (!limbElements.IsNullOrEmpty())
                {
                    if (!LimbElementsByType.ContainsKey(limbType))
                        LimbElementsByType[limbType] = new();
                    LimbElementsByType[limbType].AddRange(limbElements);
                }
            }

            NaturalEquipment ??= new();
            if (!Other.NaturalEquipment.IsNullOrEmpty())
                NaturalEquipment.AddRange(Other.NaturalEquipment);

            Utils.MergeReplaceField(ref RandomWeight, Other.RandomWeight);

            MergeTags(Other.Tags);

            MergeType = Other.MergeType;

            LogDebug(1);
            return this;
        }

        public BodyPlanEntry MergeRequire(BodyPlanEntry Other)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg($"{AnatomyName ?? "NO_ANATOMY_NAME"} <- {Other?.AnatomyName ?? "NO_OTHER_ANATOMY_NAME"}"),
                });

            if (Anatomy == null)
                AnatomyName = Other.AnatomyName;

            Utils.MergeRequireField(ref _CategoryOverride, Other.CategoryOverride ?? Other.Category?.CategoryName);
            Utils.MergeReplaceField(ref DisplayName, Other.DisplayName);
            Render = new BodyPlanRender(Other.Render).Merge(Render);

            OptionDelegateContexts ??= new();
            OptionDelegateContexts.AddRange(Other.OptionDelegateContexts);

            TextElementsNames ??= new();
            TextElementsNames.AddRange(Other.TextElementsNames);
            WantsTextElements = TextElementsNames.Count > 0;

            LimbElementsByType ??= new();
            foreach ((var limbType, var limbElements) in Other.LimbElementsByType)
                if (!limbElements.IsNullOrEmpty()
                    && !LimbElementsByType.ContainsKey(limbType))
                    LimbElementsByType[limbType] = new(limbElements);

            NaturalEquipment ??= new();
            if (!Other.NaturalEquipment.IsNullOrEmpty())
                NaturalEquipment.AddRange(Other.NaturalEquipment);

            Utils.MergeRequireField(ref RandomWeight, Other.RandomWeight);

            MergeTags(Other.Tags);

            MergeType = Other.MergeType;

            LogDebug(1);
            return this;
        }

        public BodyPlanEntry Merge(BodyPlanEntry Other)
        {
            ClearCaches();

            return Other.MergeType switch
            {
                MergeType.Require => MergeRequire(Other),
                MergeType.SoftReplace => MergeSoftReplace(Other),
                _ => MergeHardReplace(Other),
            };
        }

        public BodyPlanEntry Clone()
            => new(this);

        public override string ToString()
            => $"{DisplayName}{(Render?.Tile is string tile ? " " + tile : null)}";

        public BodyPlanRender GetRender()
        {
            if (Transformation != null
                && !Transformation.Tile.IsNullOrEmpty()
                && Transformation.DetailColor != default)
                return Transformation.Render;

            return Render ??= new(GetExampleBlueprint(), false);
        }

        public void OverrideRender(BodyPlanRender Render)
        {
            if (Render != null)
                this.Render = Render;
        }

        public int BestGuessForCategoryID()
            => Anatomy?.BodyCategory
            ?? Anatomy?.Category
            ?? Anatomy?.Parts?.FirstOrDefault(p => p.Category != null)?.Category
            ?? 1
            ;

        public bool HasTag(string Name)
            => Tags?.ContainsKey(Name) is true;

        public string GetTag(string Name)
            => Tags?.GetValueOrDefault(Name);

        public bool HasMatchingAnatomy(GameObjectBlueprint Blueprint)
            => Anatomy != null
            && Blueprint.GetAnatomyName() is string anatomy
            && anatomy == Anatomy.Name
            ;
        public bool ObjectAnimatesWithAnatomy(GameObjectBlueprint Blueprint)
            => Anatomy != null
            && Blueprint.TryGetTag("BodyType", out string bodyType)
            && Anatomy.Name == bodyType
            ;
        public bool InheritsFromAnatomy(GameObjectBlueprint Blueprint)
            => Anatomy != null
            && Blueprint.InheritsFrom(Anatomy.Name)
            ;

        public IEnumerable<GameObjectBlueprint> GetExampleBlueprints()
        {
            bool any = false;
            foreach (var blueprint in BodyPlanFactory.GenerallyEligbleForDisplayBlueprints)
            {
                if (HasMatchingAnatomy(blueprint))
                {
                    any = true;
                    yield return blueprint;
                }
            }
            if (any)
                yield break;

            foreach (var blueprint in BodyPlanFactory.GenerallyEligbleForDisplayBlueprints)
            {
                if (ObjectAnimatesWithAnatomy(blueprint))
                {
                    any = true;
                    yield return blueprint;
                }
            }
            if (any)
                yield break;

            foreach (var blueprint in BodyPlanFactory.GenerallyEligbleForDisplayBlueprints)
            {
                if (InheritsFromAnatomy(blueprint))
                {
                    any = true;
                    yield return blueprint;
                }
            }
            if (any)
                yield break;

            yield return GameObjectFactory.Factory?.GetBlueprintIfExists("Mimic");
        }

        public GameObjectBlueprint GetExampleBlueprint()
            => GetExampleBlueprints()
                ?.GetRandomElementCosmetic()
            ;

        public bool IsAvailable()
            => Anatomy != null
            && !HasTag("Restricted")
            && (OptionDelegateContexts?.Check() is not false)
            ;

        public BodyPlan GetBodyPlan()
            => Anatomy?.Name != null
            ? new(Anatomy.Name)
            : null
            ;

        public bool TryGetBodyPlan(out BodyPlan BodyPlan)
            => (BodyPlan = GetBodyPlan()) != null
            ;

        public void Dispose()
        {
            DisplayName = null;

            Render?.Dispose();
            Render = null;

            OptionDelegateContexts?.Clear();
            OptionDelegateContexts = null;

            TextElementsNames?.Clear();
            TextElementsNames = null;
            _TextElements?.Clear();
            _TextElements = null;

            foreach (var limbElements in LimbElementsByType?.Values ?? Enumerable.Empty<List<LimbTextElements>>())
                limbElements.Clear();
            LimbElementsByType?.Clear();
            LimbElementsByType = null;

            NaturalEquipment?.Clear();
            NaturalEquipment = null;

            Tags?.Clear();
            Tags = null;

            ClearCaches();
        }

        public void LogDebug(int Indent)
        {
            using Indent indent = new(Indent);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(AnatomyName), AnatomyName ?? "MISSING_ANATOMY_NAME"),
                });
            Debug.Log(nameof(MergeType), MergeType, Indent: indent[1]);
            Debug.Log(nameof(CategoryOverride), CategoryOverride ?? "NO_CATEGORY_OVERRIDE", Indent: indent[1]);
            Debug.Log(nameof(DisplayName), DisplayName ?? "NO_DISPLAY_NAME", Indent: indent[1]);
            Debug.Log(nameof(Render), Render?.ToString() ?? "NO_RENDER", Indent: indent[1]);

            Debug.Log(nameof(OptionDelegateContexts), OptionDelegateContexts?.Count ?? 0, Indent: indent[1]);
            Debug.Loggregrate(
                Source: OptionDelegateContexts,
                Proc: o => o.ToString(),
                Empty: "None",
                PostProc: s => $"::{s}",
                Indent: indent[2]);

            Debug.Log(nameof(WantsTransformation), WantsTransformation, Indent: indent[1]);

            Debug.Log(nameof(TextElementsNames), TextElementsNames?.Count ?? 0, Indent: indent[1]);
            Debug.Loggregrate(
                Source: TextElementsNames,
                Proc: n => n,
                Empty: "None",
                PostProc: s => $"::{s}",
                Indent: indent[2]);
            Debug.Log(nameof(WantsTextElements), WantsTextElements, Indent: indent[1]);

            Debug.Log(nameof(LimbElementsByType), LimbElementsByType?.Count ?? 0, Indent: indent[1]);
            foreach ((var limbType, var limbElements) in LimbElementsByType)
            {
                Debug.Log($"::{limbType}", Indent: indent[2]);
                Debug.Loggregrate(
                    Source: limbElements,
                    Proc: n => n.ToString(),
                    Empty: "None",
                    PostProc: s => $"::{s}",
                    Indent: indent[3]);
            }

            Debug.Log(nameof(NaturalEquipment), NaturalEquipment?.Count ?? 0, Indent: indent[1]);
            Debug.Loggregrate(
                Source: NaturalEquipment,
                Proc: n => n.ToString(),
                Empty: "None",
                PostProc: s => $"::{s}",
                Indent: indent[2]);

            Debug.Log(nameof(RandomWeight), RandomWeight, Indent: indent[1]);

            Debug.Log(nameof(Tags), Tags?.Count ?? 0, Indent: indent[1]);
            Debug.Loggregrate(
                Source: Tags,
                Proc: t => t.PairString(),
                Empty: "None",
                PostProc: s => $"::{s}",
                Indent: indent[2]);
        }
    }
}
