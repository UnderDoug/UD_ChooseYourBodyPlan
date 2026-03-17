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

using static UD_ChooseYourBodyPlan.Mod.ILoadFromDataBucket<UD_ChooseYourBodyPlan.Mod.BodyPlanEntry>;

namespace UD_ChooseYourBodyPlan.Mod
{
    public class BodyPlanEntry: ILoadFromDataBucket<BodyPlanEntry>
    {
        public static string DataBucketFile => "BodyPlans.xml";

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

        public OptionDelegates OptionDelegates;

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
                        Utils.MergeDistinctInCollection(OptionDelegates ??= new(), Transformation.OptionDelegates);
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

        public Dictionary<string, string> Tags;

        public int RandomWeight;

        public BodyPlanEntry()
        {
            _Category = null;
            Render = null;

            _Transformation = null;
            WantsTransformation = true;

            TextElementsNames = null;
            _TextElements = null;
            WantsTextElements = true;

            Tags = null;
        }
        public BodyPlanEntry(Anatomy Anatomy, bool IsDefault, BodyPlanRender Render)
            : this()
        {
            AnatomyName = Anatomy.Name;
            this.Render = Render;

            _ = Category;
        }
        public BodyPlanEntry(Anatomy Anatomy, BodyPlanRender Renderable)
            : this(Anatomy, false, Renderable)
        {
        }
        public BodyPlanEntry(Anatomy Anatomy, bool IsDefault)
            : this(Anatomy, IsDefault, null)
        {
        }
        public BodyPlanEntry(Anatomy Anatomy)
            : this(Anatomy, false, null)
        {
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

            OptionDelegates ??= new();
            OptionDelegates.ParseDataBucket(DataBucket);

            if (DataBucket.GetTextElementsTags() is IEnumerable<string> textElementsTags)
            {
                TextElementsNames = new();
                foreach (var textElementsName in textElementsTags)
                    TextElementsNames.Add(textElementsName);
            }

            if (DataBucket.TryGetTagValueForData(nameof(RandomWeight), out string randomWeight)
                && !int.TryParse(randomWeight, out RandomWeight))
                RandomWeight = 5;

            Tags = new();
            foreach ((string tagName, string tagValue) in DataBucket.Tags)
                Tags[tagName] = tagValue;

            Utils.Log($"{AnatomyName}.{nameof(LoadFromDataBucket)}", Indent: 0);
            LogDebug(1);
            return this;
        }

        public BodyPlanEntry LoadFromAnatomy(Anatomy Anatomy)
        {
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

            Utils.Log($"{AnatomyName}.{nameof(LoadFromAnatomy)}", Indent: 0);
            LogDebug(1);
            return this;
        }

        public bool SameAs(BodyPlanEntry Other)
            => Anatomy?.Name == Other?.Anatomy?.Name
            ;

        public BodyPlanEntry MergeHardReplace(BodyPlanEntry Other)
        {
            AnatomyName = Other.AnatomyName;

            CategoryOverride = Other.CategoryOverride ?? Other.Category?.CategoryName;
            DisplayName = Other.DisplayName;
            Render = new(Other.Render);
            OptionDelegates = new(Other.OptionDelegates);

            TextElementsNames = new(Other.TextElementsNames);
            WantsTextElements = true;

            RandomWeight = Other.RandomWeight;
            Tags = new(Other.Tags);

            Utils.Log($"{AnatomyName}.{nameof(MergeHardReplace)}", Indent: 0);
            LogDebug(1);
            return this;
        }

        public BodyPlanEntry MergeSoftReplace(BodyPlanEntry Other)
        {
            if (Anatomy == null)
                AnatomyName = Other.AnatomyName;

            Utils.MergeReplaceField(ref _CategoryOverride, Other.CategoryOverride ?? Other.Category?.CategoryName);
            Utils.MergeReplaceField(ref DisplayName, Other.DisplayName);
            Render.Merge(Other.Render);

            OptionDelegates.Merge(OptionDelegates);

            Utils.MergeReplaceField(TextElementsNames ??= new(), new HashSet<string>(Other.TextElementsNames));
            WantsTextElements = true;

            Utils.MergeReplaceField(ref RandomWeight, Other.RandomWeight);

            Utils.MergeReplaceDictionary(Tags ??= new(), new Dictionary<string, string>(Other.Tags));

            Utils.Log($"{AnatomyName}.{nameof(MergeSoftReplace)}", Indent: 0);
            LogDebug(1);
            return this;
        }

        public BodyPlanEntry MergeRequire(BodyPlanEntry Other)
        {
            if (Anatomy == null)
                AnatomyName = Other.AnatomyName;

            Utils.MergeRequireField(ref _CategoryOverride, Other.CategoryOverride ?? Other.Category?.CategoryName);
            Utils.MergeReplaceField(ref DisplayName, Other.DisplayName);
            Render = new BodyPlanRender(Other.Render).Merge(Render);

            OptionDelegates = new OptionDelegates(Other.OptionDelegates).Merge(OptionDelegates);

            Utils.MergeRequireField(TextElementsNames ??= new(), new HashSet<string>(Other.TextElementsNames));
            WantsTextElements = true;

            Utils.MergeRequireField(ref RandomWeight, Other.RandomWeight);

            Utils.MergeRequireDictionary(Tags ??= new(), new Dictionary<string, string>(Other.Tags));

            Utils.Log($"{AnatomyName}.{nameof(MergeRequire)}", Indent: 0);
            LogDebug(1);
            return this;
        }

        public BodyPlanEntry Merge(BodyPlanEntry Other)
        {
            ClearCaches();
            if (Anatomy == null)
                AnatomyName = Other.AnatomyName;

            return Other.MergeType switch
            {
                MergeType.Require => MergeRequire(Other),
                MergeType.SoftReplace => MergeSoftReplace(Other),
                _ => MergeHardReplace(Other),
            };
        }

        public BodyPlanEntry Clone()
            => new BodyPlanEntry()
                .Merge(this);

        public override string ToString()
            => $"{DisplayName}{(Render?.Tile is string tile ? " " + tile : null)}";

        public BodyPlanRender GetRender()
        {
            if (Transformation != null
                && !Transformation.Tile.IsNullOrEmpty()
                && Transformation.DetailColor != '\0')
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
            => Tags
                ?.ContainsKey(Name)
            ?? false;

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
            && (OptionDelegates?.Check() ?? true)
            ;

        public BodyPlan GetBodyPlan()
            => new(Anatomy.Name);

        public void Dispose()
        {
            DisplayName = null;

            Render?.Dispose();
            Render = null;

            OptionDelegates?.Clear();
            OptionDelegates = null;

            Tags?.Clear();
            Tags = null;

            ClearCaches();
        }

        public void LogDebug(int Indent = 0)
        {
            Utils.Log($"{nameof(AnatomyName)}: {AnatomyName ?? "MISSING_ANATOMY_NAME"}", Indent: Indent);
            Utils.Log($"{nameof(MergeType)}: {MergeType}", Indent: Indent + 1);
            Utils.Log($"{nameof(CategoryOverride)}: {CategoryOverride ?? "NO_CATEGORY_OVERRIDE"}", Indent: Indent + 1);
            Utils.Log($"{nameof(DisplayName)}: {DisplayName ?? "NO_DISPLAY_NAME"}", Indent: Indent + 1);
            Utils.Log($"{nameof(Render)}: {Render?.ToString() ?? "NO_RENDER"}", Indent: Indent + 1);
            Utils.Log($"{nameof(OptionDelegates)}: {OptionDelegates?.Count ?? 0}", Indent: Indent + 1);
            if (OptionDelegates.IsNullOrEmpty())
                Utils.Log($"::None", Indent: Indent + 2);
            else
                foreach (var optionDelegate in OptionDelegates)
                    Utils.Log($"::{optionDelegate.OptionID} {optionDelegate.Operator} {optionDelegate.TrueState}", Indent: Indent + 2);
            Utils.Log($"{nameof(WantsTransformation)}: {WantsTransformation}", Indent: Indent + 1);
            Utils.Log($"{nameof(TextElementsNames)}: {TextElementsNames?.Count ?? 0}", Indent: Indent + 1);
            if (TextElementsNames.IsNullOrEmpty())
                Utils.Log($"::None", Indent: Indent + 2);
            else
                foreach (var textElements in TextElementsNames)
                    Utils.Log($"::{textElements}", Indent: Indent + 2);
            Utils.Log($"{nameof(WantsTextElements)}: {WantsTextElements}", Indent: Indent + 1);
            Utils.Log($"{nameof(Tags)}: {Tags?.Count ?? 0}", Indent: Indent + 1);
            if (Tags.IsNullOrEmpty())
                Utils.Log($"::None", Indent: Indent + 2);
            else
                foreach ((var tagNme, var tagValue) in Tags)
                    Utils.Log($"::{tagNme}: {tagValue}", Indent: Indent + 2);
            Utils.Log($"{nameof(RandomWeight)}: {RandomWeight}", Indent: Indent + 1);
        }
    }
}
