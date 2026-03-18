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
using UD_ChooseYourBodyPlan.Mod.Logging;

namespace UD_ChooseYourBodyPlan.Mod
{
    public class BodyPlanEntry: ILoadFromDataBucket<BodyPlanEntry>
    {
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

        public OptionDelegateContexts OptionDelegates;

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
                        Utils.MergeRequireDistinctInCollection(OptionDelegates ??= new(), Transformation.OptionDelegates);
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
            AnatomyName = null;
            _Category = null;
            Render = null;

            _Transformation = null;
            WantsTransformation = true;

            TextElementsNames = null;
            _TextElements = null;
            WantsTextElements = true;

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

            OptionDelegates = new();
            if (Source?.OptionDelegates != null
                && !Source.OptionDelegates.IsNullOrEmpty())
                OptionDelegates.AddRange(Source.OptionDelegates);

            WantsTransformation = Source?._Transformation != null;

            TextElementsNames = new();
            if (Source?.TextElementsNames != null
                && !Source.TextElementsNames.IsNullOrEmpty())
                foreach (var textElementsName in Source.TextElementsNames)
                    TextElementsNames.Add(textElementsName);
            WantsTextElements = !TextElementsNames.IsNullOrEmpty();

            Tags = new();
            if (Source?.Tags != null
                && !Source.Tags.IsNullOrEmpty())
                foreach ((var tagName, var tagValue) in Source.Tags)
                    Tags.Add(tagName, tagValue);

            RandomWeight = Source?.RandomWeight ?? 0;
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
            OptionDelegates = new(Other.OptionDelegates);

            TextElementsNames = new(Other.TextElementsNames);
            WantsTextElements = true;

            MergeTags(Other.Tags);

            RandomWeight = Other.RandomWeight;

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

            OptionDelegates.Merge(OptionDelegates);

            Utils.MergeReplaceField(TextElementsNames ??= new(), new HashSet<string>(Other.TextElementsNames));
            WantsTextElements = true;

            MergeTags(Other.Tags);

            Utils.MergeReplaceField(ref RandomWeight, Other.RandomWeight);

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

            OptionDelegates = new OptionDelegateContexts(Other.OptionDelegates).Merge(OptionDelegates);

            Utils.MergeRequireField(TextElementsNames ??= new(), new HashSet<string>(Other.TextElementsNames));
            WantsTextElements = true;

            MergeTags(Other.Tags);

            Utils.MergeRequireField(ref RandomWeight, Other.RandomWeight);

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
            && (OptionDelegates?.Check() is not false)
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

            OptionDelegates?.Clear();
            OptionDelegates = null;

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

            Debug.Log(nameof(OptionDelegates), OptionDelegates?.Count ?? 0, Indent: indent[1]);
            Debug.Loggregrate(
                Source: OptionDelegates,
                Proc: o => $"{o.OptionID} {o.Operator} {o.TrueState}",
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

            Debug.Log(nameof(Tags), Tags?.Count ?? 0, Indent: indent[1]);
            Debug.Loggregrate(
                Source: Tags,
                Proc: t => t.PairString(),
                Empty: "None",
                PostProc: s => $"::{s}",
                Indent: indent[2]);

            Debug.Log(nameof(RandomWeight), RandomWeight, Indent: indent[1]);
        }
    }
}
