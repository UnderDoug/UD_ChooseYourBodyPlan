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
using XRL.Collections;

namespace UD_ChooseYourBodyPlan.Mod
{
    public class BodyPlanEntry: ILoadFromDataBucket<BodyPlanEntry>
    {
        public class LimbTextElements
        {
            public static LimbTextElements NaturalEquipment
                => new() { Shader = new Shader { Color = "w" }.Finalize() };

            public static LimbTextElements NoCyber
                => new() { Symbol = BodyPlanFactory.Factory?.GetTextElements(nameof(NoCyber))?.GetSymbol() ?? default };

            public string Type;
            public OptionDelegateContext OptionDelegateContext;
            public Shader Shader;
            public string PostText;
            public Symbol Symbol;
            public bool OverridesNaturalEquipmentColor;
            public bool OverridesNaturalEquipmentStats;

            public LimbTextElements()
            {
                Type = null;
                OptionDelegateContext = null;
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

                string extras = null;
                if (Shader.Apply("A") != "A")
                    extras = $"{extras}[{nameof(Shader)}:{Shader}]";

                if (!PostText.IsNullOrEmpty())
                    extras = $"{extras}[{nameof(PostText)}:{PostText}]";

                if (!Symbol.ToString().IsNullOrEmpty())
                    extras = $"{extras}[{nameof(Symbol)}:{Symbol.DebugString()}]";

                if (OptionDelegateContext != null)
                    extras = $"{extras}[{nameof(Symbol)}:{Symbol.DebugString()}]";

                extras ??= "[empty]";

                return $"{output}{extras}";
            }

            public bool CheckOption(BodyPlanEntry Entry)
                => Entry is not null
                && OptionDelegateContext?.Check(Entry) is not false
                ;

            public bool CheckType(string Type)
                => Type.IsNullOrEmpty()
                || this.Type.IsNullOrEmpty()
                || this.Type.Equals(Type)
                ;

            public bool CheckLimb(BodyPart Limb)
                => !Type.IsNullOrEmpty()
                && CheckType(Limb?.VariantTypeModel()?.Type)
                ;

            public string ProcessColor(string LimbDescription, BodyPlanEntry Entry, string Type = null)
            {
                if (!CheckOption(Entry))
                    return LimbDescription;

                if (!CheckType(Type))
                    return LimbDescription;

                return Shader.Apply(LimbDescription);
            }

            public string ProcessPostText(string LimbDescription, BodyPlanEntry Entry, string Type = null)
            {
                if (!CheckOption(Entry))
                    return LimbDescription;

                if (!CheckType(Type))
                    return LimbDescription;

                if (!PostText.IsNullOrEmpty())
                    LimbDescription = $"{LimbDescription} {PostText}";

                return LimbDescription;
            }

            public string ProcessSymbol(string LimbDescription, BodyPlanEntry Entry, string Type = null)
            {
                if (!CheckOption(Entry))
                    return LimbDescription;

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
                _TextElements ??= new();
                if (WantsTransformation
                    && Transformation != null)
                {
                    TextElementsNames ??= new();
                    TextElementsNames.AddRange(Transformation.GetTextElementsNames());
                    WantsTextElements = TextElementsNames.Count > 0;
                }
                if (WantsTextElements
                    && Factory.TextElementsInitialized
                    && _TextElements.IsNullOrEmpty())
                {
                    WantsTextElements = false;

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

        public Dictionary<string, GamePartBlueprint> AddsParts;

        public List<InventoryObject> NaturalEquipment;

        public List<GamePartBlueprint> ManagedNaturalEquipment;

        private bool? _WantsDerivedFrom;
        public bool WantsDerivedFrom => _WantsDerivedFrom.GetValueOrDefault();

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

            AddsParts = null;

            NaturalEquipment = null;
            ManagedNaturalEquipment = null;
            _WantsDerivedFrom = null;

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
            if (Source == null)
                return;

            AnatomyName = Source.AnatomyName;
            MergeType = Source.MergeType;

            CategoryOverride = Source.CategoryOverride;
            DisplayName = Source.DisplayName;
            Render = Source.Render?.Clone();

            OptionDelegateContexts = new();
            if (!Source.OptionDelegateContexts.IsNullOrEmpty())
                OptionDelegateContexts.AddRange(Source.OptionDelegateContexts);

            WantsTransformation = Source?._Transformation != null;

            TextElementsNames = new();
            if (!Source.TextElementsNames.IsNullOrEmpty())
                TextElementsNames.AddRange(Source.TextElementsNames);
            WantsTextElements = !TextElementsNames.IsNullOrEmpty();

            LimbElementsByType = new();
            if (!Source.LimbElementsByType.IsNullOrEmpty())
                foreach ((var limbType, var limbElements) in Source.LimbElementsByType)
                    LimbElementsByType.Add(limbType, new(limbElements));

            AddsParts = new();
            if (!Source.AddsParts.IsNullOrEmpty())
            {
                foreach ((var partName, var partBlueprint) in Source.AddsParts)
                {
                    if (!partName.IsNullOrEmpty())
                    {
                        var partBlueprintCopy = new GamePartBlueprint(partName);
                        partBlueprintCopy.CopyFrom(partBlueprint);
                        AddsParts.Add(partName, partBlueprintCopy);
                    }
                }
            }

            NaturalEquipment = new();
            if (!Source.NaturalEquipment.IsNullOrEmpty())
                NaturalEquipment.AddRange(Source.NaturalEquipment);

            ManagedNaturalEquipment = new();
            if (!Source.ManagedNaturalEquipment.IsNullOrEmpty())
                ManagedNaturalEquipment.AddRange(Source.ManagedNaturalEquipment);

            _WantsDerivedFrom = Source._WantsDerivedFrom;

            RandomWeight = Source.RandomWeight;

            Tags = new();
            if (!Source.Tags.IsNullOrEmpty())
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

                    OptionDelegateContext optionDelegateContext = null;
                    foreach (var elementName in rawLimbElements.Keys)
                    {
                        if (elementName.StartsWith("Option"))
                        {
                            optionDelegateContext = new KeyValuePair<string, string>(elementName, rawLimbElements[elementName]).ParseOptionTag(out bool remove);
                            if (remove)
                                optionDelegateContext = null;
                        }
                    }

                    Shader shader = default;
                    if (rawLimbElements.GetValue(nameof(Shader.Color)) is string rawShaderColor
                        && !rawShaderColor.IsNullOrEmpty())
                        shader = new Shader { Color = rawShaderColor }.Finalize();
                    else
                    if (rawLimbElements.GetValue(nameof(Shader)).CachedCommaExpansion()?.ToList() is List<string> rawShader
                        && !rawShader.IsNullOrEmpty())
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
                    if (rawLimbElements.GetValue(nameof(Symbol)).CachedCommaExpansion()?.ToList() is List<string> rawSymbol
                        && !rawSymbol.IsNullOrEmpty())
                    {
                        if (rawSymbol.Count > 2)
                            symbol = new(rawSymbol[0], rawSymbol[1][0], rawSymbol[2]);
                        else
                        if (rawSymbol.Count > 1)
                            symbol = new(null, rawSymbol[0][0], rawSymbol[1]);
                        else
                        {
                            if (rawSymbol[0].Contains(".")
                                && rawSymbol[0].Split('.') is string[] rawSymbolTextElementsAddress
                                && Factory.TextElementsByName.TryGetValue(rawSymbolTextElementsAddress[0], out var fullyAddressedTextElements)
                                && fullyAddressedTextElements.SymbolsByName.TryGetValue(rawSymbolTextElementsAddress[1], out var fullyAddressedSymbol))
                                symbol = fullyAddressedSymbol;
                            else
                            if (Factory.TextElementsByName.TryGetValue(rawSymbol[0], out var addressedTextElements)
                                && addressedTextElements.SymbolsByName.TryGetValue(rawSymbol[0], out var addressedSymbol))
                                symbol = addressedSymbol;
                            else
                                symbol = new(null, default, rawSymbol[0]);
                        }
                    }

                    bool overridesNaturalEquipmentColor = rawLimbElements.GetValue(nameof(LimbTextElements.OverridesNaturalEquipmentColor)) is string overridesColor
                        && overridesColor.EqualsNoCase("Yes");

                    bool overridesNaturalEquipmentStats = rawLimbElements.GetValue(nameof(LimbTextElements.OverridesNaturalEquipmentStats)) is string overridesStats
                        && overridesStats.EqualsNoCase("Yes");

                    LimbElementsByType[limbType].Add(
                        item: new LimbTextElements
                        {
                            Type = limbType,
                            OptionDelegateContext = optionDelegateContext,
                            Shader = shader,
                            PostText = rawLimbElements.GetValueOrDefault(nameof(LimbTextElements.PostText)),
                            Symbol = symbol,
                            OverridesNaturalEquipmentColor = overridesNaturalEquipmentColor,
                            OverridesNaturalEquipmentStats = overridesNaturalEquipmentStats,
                        });
                }
            }

            AddsParts ??= new();
            if (DataBucket.GetTag(nameof(AddsParts), null)?.CachedCommaExpansion() is IEnumerable<string> addsPartsTag)
            {
                foreach (var addsPart in addsPartsTag)
                {
                    if (!addsPart.IsNullOrEmpty())
                    {
                        var partBlueprint = new GamePartBlueprint(addsPart);
                        if (DataBucket.HasPart(addsPart))
                            partBlueprint.CopyFrom(DataBucket.GetPart(addsPart));
                        AddsParts.Add(addsPart, partBlueprint);
                    }
                }
            }

            NaturalEquipment ??= new();
            if (!DataBucket.Inventory.IsNullOrEmpty())
                NaturalEquipment.AddRange(DataBucket.Inventory);

            ManagedNaturalEquipment ??= new();
            if (DataBucket.HasPart(nameof(UD_CYBP_BodyPlanNaturalEquipment)))
                ManagedNaturalEquipment.Add(DataBucket.GetPart(nameof(UD_CYBP_BodyPlanNaturalEquipment)));

            if (DataBucket.GetTag(nameof(WantsDerivedFrom), null) is string wantsDerivedFrom)
            {
                if (wantsDerivedFrom.EqualsNoCase(Const.REMOVE_TAG))
                    _WantsDerivedFrom = false;
                else
                    _WantsDerivedFrom = true;
            }

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

            TextElementsNames ??= new();
            if (Anatomy.IsMechanical())
                TextElementsNames.Add("Mechanical");

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

            AddsParts ??= new();
            AddsParts.Clear();
            foreach ((var partName, var partBlueprint) in Other.AddsParts)
            {
                if (!partName.IsNullOrEmpty())
                {
                    var partBlueprintCopy = new GamePartBlueprint(partName);
                    partBlueprintCopy.CopyFrom(partBlueprint);
                    AddsParts.Add(partName, partBlueprintCopy);
                }
            }

            NaturalEquipment ??= new();
            NaturalEquipment.Clear();
            if (!Other.NaturalEquipment.IsNullOrEmpty())
                NaturalEquipment.AddRange(Other.NaturalEquipment);

            ManagedNaturalEquipment ??= new();
            ManagedNaturalEquipment.Clear();
            if (!Other.ManagedNaturalEquipment.IsNullOrEmpty())
                ManagedNaturalEquipment.AddRange(Other.ManagedNaturalEquipment);

            _WantsDerivedFrom = Other._WantsDerivedFrom;

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

            AddsParts ??= new();
            foreach ((var partName, var partBlueprint) in Other.AddsParts)
            {
                if (!partName.IsNullOrEmpty())
                {
                    if (!AddsParts.ContainsKey(partName))
                        AddsParts[partName] = new GamePartBlueprint(partName);

                    AddsParts[partName].CopyFrom(partBlueprint);
                }
            }

            NaturalEquipment ??= new();
            if (!Other.NaturalEquipment.IsNullOrEmpty())
                NaturalEquipment.AddRange(Other.NaturalEquipment);

            ManagedNaturalEquipment ??= new();
            if (!Other.ManagedNaturalEquipment.IsNullOrEmpty())
                ManagedNaturalEquipment.AddRange(Other.ManagedNaturalEquipment);

            if (Other._WantsDerivedFrom != null)
                _WantsDerivedFrom = Other._WantsDerivedFrom;

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

            AddsParts ??= new();
            foreach ((var partName, var partBlueprint) in Other.AddsParts)
            {
                if (!partName.IsNullOrEmpty())
                {
                    if (AddsParts.ContainsKey(partName))
                        AddsParts[partName].CopyFrom(partBlueprint);
                }
            }

            NaturalEquipment ??= new();
            if (!Other.NaturalEquipment.IsNullOrEmpty())
                NaturalEquipment.AddRange(Other.NaturalEquipment);

            ManagedNaturalEquipment ??= new();
            if (!Other.ManagedNaturalEquipment.IsNullOrEmpty())
                ManagedNaturalEquipment.AddRange(Other.ManagedNaturalEquipment);

            _WantsDerivedFrom ??= Other._WantsDerivedFrom;

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
                && !Transformation.Render.IsEmpty()
                && !Transformation.Tile.IsNullOrEmpty()
                && Transformation.DetailColor != default)
                return Transformation.Render;

            if (Render?.IsEmpty() is not false)
                Render = new(GetExampleBlueprint(), false);

            return Render;
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

        public bool AnyNoCyber()
            => Anatomy != null
            && Anatomy.AnyNoCyber()
            ;

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
            var matchingAnatomy = ScopeDisposedList<GameObjectBlueprint>.GetFromPool();
            var animatesWithAnatomy = ScopeDisposedList<GameObjectBlueprint>.GetFromPool();
            var inheritsAnatomy = ScopeDisposedList<GameObjectBlueprint>.GetFromPool();
            foreach (var blueprint in BodyPlanFactory.GenerallyEligbleForDisplayBlueprints)
            {
                if (HasMatchingAnatomy(blueprint))
                {
                    matchingAnatomy.Add(blueprint);
                    continue;
                }

                if (ObjectAnimatesWithAnatomy(blueprint))
                {
                    animatesWithAnatomy.Add(blueprint);
                    continue;
                }

                if (InheritsFromAnatomy(blueprint))
                    inheritsAnatomy.Add(blueprint);
            }

            if (!matchingAnatomy.IsNullOrEmpty())
            {
                foreach (var matchingBlueprint in matchingAnatomy)
                    yield return matchingBlueprint;
                yield break;
            }

            if (!animatesWithAnatomy.IsNullOrEmpty())
            {
                foreach (var animatesWithBlueprint in animatesWithAnatomy)
                    yield return animatesWithBlueprint;
                yield break;
            }

            if (!inheritsAnatomy.IsNullOrEmpty())
            {
                foreach (var inheritsBlueprint in inheritsAnatomy)
                    yield return inheritsBlueprint;
                yield break;
            }

            yield return GameObjectFactory.Factory?.GetBlueprintIfExists("Mimic");
        }

        public GameObjectBlueprint GetExampleBlueprint()
            => GetExampleBlueprints()
                ?.GetRandomElementCosmetic()
            ;

        public bool IsAvailable()
            => Anatomy != null
            && !HasTag("Restricted")
            && (OptionDelegateContexts?.Check(this) is not false)
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

            AddsParts?.Clear();
            AddsParts = null;

            NaturalEquipment?.Clear();
            NaturalEquipment = null;

            ManagedNaturalEquipment?.Clear();
            ManagedNaturalEquipment = null;

            _WantsDerivedFrom = null;

            Tags?.Clear();
            Tags = null;

            ClearCaches();
        }

        public void LogDebug(int Indent, bool IncludeLateLoaded = false)
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

            if (IncludeLateLoaded)
            {
                if (Transformation != null)
                    Transformation.DebugOutput(indent[1]);
                else
                    Debug.Log(nameof(Transformation), "None", Indent: indent[1]);
            }

            Debug.Log(nameof(TextElementsNames), TextElementsNames?.Count ?? 0, Indent: indent[1]);
            Debug.Loggregrate(
                Source: TextElementsNames,
                Proc: n => n,
                Empty: "None",
                PostProc: s => $"::{s}",
                Indent: indent[2]);
            Debug.Log(nameof(WantsTextElements), WantsTextElements, Indent: indent[1]);

            if (IncludeLateLoaded)
            {
                Debug.Log(nameof(TextElements), TextElements?.Count ?? 0, Indent: indent[1]);
                Debug.Loggregrate(
                    Source: TextElements,
                    Proc: n => $"{n.Name}",
                    Empty: "None",
                    PostProc: s => $"::{s}",
                    Indent: indent[3]);
            }

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

            Debug.Log(nameof(AddsParts), AddsParts?.Count ?? 0, Indent: indent[1]);
            Debug.Loggregrate(
                Source: AddsParts.Keys,
                Proc: n => n.ToString(),
                Empty: "None",
                PostProc: s => $"::{s}",
                Indent: indent[2]);

            Debug.Log(nameof(NaturalEquipment), NaturalEquipment?.Count ?? 0, Indent: indent[1]);
            Debug.Loggregrate(
                Source: NaturalEquipment,
                Proc: n => n.ToString(),
                Empty: "None",
                PostProc: s => $"::{s}",
                Indent: indent[2]);

            Debug.Log(nameof(ManagedNaturalEquipment), ManagedNaturalEquipment?.Count ?? 0, Indent: indent[1]);
            Debug.Loggregrate(
                Source: ManagedNaturalEquipment.GetBlueprintsByBodyPartType(AnatomyName),
                Proc: n => n.PairString(),
                Empty: "None",
                PostProc: s => $"::{s}",
                Indent: indent[2]);

            Debug.Log(nameof(WantsDerivedFrom), _WantsDerivedFrom?.ToString() ?? "null", Indent: indent[1]);

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
