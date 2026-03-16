using System;
using System.Collections.Generic;
using System.Text;

using XRL;
using XRL.UI;
using XRL.UI.Framework;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;

namespace UD_ChooseYourBodyPlan.Mod
{
    [HasModSensitiveStaticCache]
    public class BodyPlanFactory
    {
        #region Blueprints For Display

        private static List<GameObjectBlueprint> _GenerallyEligbleForDisplayBlueprints = null;
        public static List<GameObjectBlueprint> GenerallyEligbleForDisplayBlueprints
        {
            get
            {
                if (_GenerallyEligbleForDisplayBlueprints.IsNullOrEmpty())
                {
                    _GenerallyEligbleForDisplayBlueprints ??= new();
                    foreach (var blueprint in GameObjectFactory.Factory?.BlueprintList ?? new List<GameObjectBlueprint>())
                        if (IsGenerallyEligbleForDisplay(blueprint))
                            _GenerallyEligbleForDisplayBlueprints.Add(blueprint);
                }
                return _GenerallyEligbleForDisplayBlueprints;
            }
        }

        [ModSensitiveCacheInit]
        public static void ClearCacheOfGenerallyEligbleForDisplayBlueprints()
        {
            Utils.Log(typeof(BodyPlanFactory).CallChain(nameof(ClearCacheOfGenerallyEligbleForDisplayBlueprints)));
            _GenerallyEligbleForDisplayBlueprints = null;
        }

        public static string GetTile(GameObjectBlueprint Blueprint)
            => Blueprint.GetRenderable()?.Tile
            ;
        public static string GetAnatomyName(GameObjectBlueprint Blueprint)
            => Blueprint.GetPartParameter<string>(nameof(Body), nameof(Body.Anatomy))
            ;
        public static Anatomy GetAnatomy(GameObjectBlueprint Blueprint)
            => Anatomies.GetAnatomyOrFail(GetAnatomyName(Blueprint))
            ;
        public static bool IsGenerallyEligbleForDisplay(GameObjectBlueprint Blueprint)
        {
            if (Blueprint == null)
                return false;

            if (!Blueprint.HasAnatomy()
                && !Blueprint.HasTag("BodyType"))
            {
                bool any = false;
                foreach (var anatomyName in Anatomies.AnatomyTable.Keys)
                {
                    if (Blueprint.InheritsFrom(anatomyName))
                    {
                        any = true;
                        break;
                    }
                }
                if (!any)
                    return false;
            }

            if (!Blueprint.InheritsFrom("PhysicalObject"))
                return false;

            if (Blueprint.HasSTag("Chiliad"))
                return false;

            if (Blueprint.HasTag("Golem"))
                return false;

            if (Blueprint.InheritsFromAny(
                Blueprints: new string[]
                {
                    "Templar",
                }))
                return false;

            if (Blueprint.InheritsFrom("Chair")
                && Blueprint.Name.EndsWithAny(" L", " C", " R"))
                return false;

            if (Blueprint.Name.Contains("Cherub"))
                return false;

            if (GetTile(Blueprint) is not string renderTile)
                return false;

            if (renderTile.Contains("sw_farmer")
                && GetAnatomyName(Blueprint) is string anatomy
                && !anatomy.EqualsNoCase("Humanoid"))
                return false;

            if (Blueprint.TryGetPartParameter(nameof(Door), nameof(Door.SyncAdjacent), out bool syncAdjacent)
                && syncAdjacent)
                return false;

            return true;
        }

        #endregion

        public const string NO_ENTRY_DATA_BUCKET = "UD_CYBP_BodyPlanEntry UD_CYBP_NoEntry";
        public static string NoEntryName => "UD_CYBP_NoEntry";

        public static StringBuilder SB = new();

        public static GameObject SampleCreature = null;

        [ModSensitiveStaticCache]
        private static BodyPlanFactory _Factory;
        public static BodyPlanFactory Factory
        {
            get
            {
                if (_Factory == null)
                {
                    _Factory = new();
                    Loading.LoadTask("Loading TransformationData.xml", _Factory.LoadTransformationData);
                    Loading.LoadTask("Loading BodyPlans.xml", _Factory.LoadBodyPlans);
                    Loading.LoadTask("Loading BodyPlanCategories.xml", _Factory.LoadAnatomyCategoryEntries);
                    Loading.LoadTask("Loading TextElements.xml", _Factory.LoadTextElements);
                }
                return _Factory;
            }
        }

        public Dictionary<string, TextElements> TextElementsByName;

        public Dictionary<string, TransformationData> TransformationDataByAnatomyName;

        public Dictionary<string, BodyPlanEntry> BodyPlanEntryByAnatomyName;

        public Dictionary<string, AnatomyCategoryEntry> AnatomyCategoryEntryByCategoryName;

        public bool TextElementsInitialized { get; protected set; }

        public bool TransformationDataInitialized { get; protected set; }

        public bool BodyPlanEntriesInitialized { get; protected set; }

        public bool AnatomyCategoryEntriesInitialized { get; protected set; }

        public BodyPlanFactory()
        {
            TextElementsInitialized = false;
            TransformationDataInitialized = false;
            BodyPlanEntriesInitialized = false;
            AnatomyCategoryEntriesInitialized = false;
        }

        public void LoadTextElements()
        {
            Load(ref TextElementsByName);
            TextElementsInitialized = true;
        }

        public void LoadTransformationData()
        {
            Load(ref TransformationDataByAnatomyName);
            TransformationDataInitialized = true;
        }

        public void LoadBodyPlans()
        {
            LoadBaseBodyPlanEntries();
            Load(ref BodyPlanEntryByAnatomyName);
            BodyPlanEntriesInitialized = true;
        }

        public void LoadAnatomyCategoryEntries()
        {
            Load(ref AnatomyCategoryEntryByCategoryName);
            AssignCategoryEntries();
            AnatomyCategoryEntriesInitialized = true;
        }

        public void Load<T>(ref Dictionary<string, T> CacheByName)
            where T : ILoadFromDataBucket<T>, new()
        {
            CacheByName = new();
            foreach (var dataBucket in GetDataBuckets<T>())
            {
                if (TryLoadFromDataBucket(dataBucket, out T loaded))
                {
                    if (CacheByName.ContainsKey(loaded.CacheKey))
                        CacheByName[loaded.CacheKey].Merge(loaded);
                    else
                        CacheByName[loaded.CacheKey] = loaded;
                }
            }
        }

        public IEnumerable<GameObjectBlueprint> GetDataBuckets<T>()
            where T : ILoadFromDataBucket<T>, new()
            => GameObjectFactory.Factory
                ?.GetBlueprintsInheritingFrom(ILoadFromDataBucket<T>.GetBaseDataBucketBlueprint())
            ;

        public T LoadFromDataBucket<T>(GameObjectBlueprint DataBucket)
            where T : ILoadFromDataBucket<T>, new()
            => new T().LoadFromDataBucket(DataBucket);

        public bool TryLoadFromDataBucket<T>(GameObjectBlueprint DataBucket, out T Result)
            where T : ILoadFromDataBucket<T>, new()
            => (Result = LoadFromDataBucket<T>(DataBucket)) != null;

        public void LoadBaseBodyPlanEntries()
        {
            BodyPlanEntryByAnatomyName ??= new();
            if (GameObjectFactory.Factory.GetBlueprintIfExists(NO_ENTRY_DATA_BUCKET) is not GameObjectBlueprint noEntryDataBucket)
            {
                Utils.Error($"{nameof(BodyPlanFactory)} failed to find {NoEntryName} data bucket: \"{NO_ENTRY_DATA_BUCKET}\". " +
                    $"Unable to load {nameof(BodyPlanEntry)} for anatomies without one explicitly defined.");
            }
            else
            {
                BodyPlanEntryByAnatomyName[NoEntryName] = new BodyPlanEntry().LoadFromDataBucket(noEntryDataBucket);
                foreach (var anatomy in Anatomies.AnatomyList)
                {
                    if (LoadFromAnatomy(anatomy) is BodyPlanEntry anatomyEntry)
                        BodyPlanEntryByAnatomyName[anatomy.Name] = anatomyEntry;
                }
            }
        }

        public BodyPlanEntry LoadFromAnatomy(Anatomy Anatomy)
        {
            if (Anatomy == null)
                return null;

            if (GetEmptyBodyPlanEntry() is not BodyPlanEntry emptyEntry)
                return null;

            return emptyEntry.LoadFromAnatomy(Anatomy);
        }

        public void AssignCategoryEntries()
        {
            foreach (var category in AnatomyCategoryEntryByCategoryName.Values)
            {
                category.Entries ??= new();
                if (category.CategoryName == "Default"
                    || category.ID == 0)
                    category.Entries.AddRange(BodyPlanEntryByAnatomyName.Values);
                else
                {
                    foreach (var bodyPlanEntry in BodyPlanEntryByAnatomyName.Values)
                    {
                        if (bodyPlanEntry.CategoryOverride == category.CategoryName
                            || bodyPlanEntry.BestGuessForCategoryID() == category.ID)
                            category.Entries.Add(bodyPlanEntry);
                    }
                }
            }
        }

        public TransformationData GetTransformationData(string AnatomyName)
            => !AnatomyName.IsNullOrEmpty()
            ? TransformationDataByAnatomyName.GetValueOrDefault(AnatomyName)
            : null
            ;

        public TransformationData GetTransformationData(Anatomy Anatomy)
            => GetTransformationData(Anatomy?.Name)
            ;

        public TransformationData GetTransformationData(BodyPlanEntry BodyPlanEntry)
            => GetTransformationData(BodyPlanEntry?.Anatomy)
            ;

        protected BodyPlanEntry GetEmptyBodyPlanEntry()
            => BodyPlanEntryByAnatomyName?.GetValue(NoEntryName)
                ?.Clone()
            ;

        public BodyPlanEntry RequireBodyPlanEntry(string Anatomy)
        {
            if (Anatomy.IsNullOrEmpty())
                return BodyPlanEntryByAnatomyName?.GetValueOrDefault(NoEntryName).Clone();

            if (!BodyPlanEntryByAnatomyName.ContainsKey(Anatomy))
            {
                if (Anatomies.GetAnatomy(Anatomy) is not Anatomy anatomy)
                    BodyPlanEntryByAnatomyName[Anatomy] = BodyPlanEntryByAnatomyName
                        ?.GetValueOrDefault(NoEntryName)
                        ?.Clone();
                else
                    BodyPlanEntryByAnatomyName[anatomy.Name] = BodyPlanEntryByAnatomyName
                        ?.GetValueOrDefault(NoEntryName)
                        ?.Clone()
                        ?.LoadFromAnatomy(anatomy);
            }
            return BodyPlanEntryByAnatomyName[Anatomy];
        }

        public BodyPlanEntry GetBodyPlanEntry(PrefixMenuOption MenuOption)
            => RequireBodyPlanEntry(MenuOption?.Id)
            ;

        public TextElements GetTextElements(string Name)
            => !Name.IsNullOrEmpty()
            ? TextElementsByName?.GetValueOrDefault(Name)
            : null
            ;

        public AnatomyCategoryEntry GetAnatomyCategoryEntry(string Name)
            => !Name.IsNullOrEmpty()
            ? AnatomyCategoryEntryByCategoryName.GetValueOrDefault(Name)
            : null
            ;

        public AnatomyCategoryEntry GetAnatomyCategoryEntryFor(BodyPlanEntry BodyPlanEntry)
        {
            if (!AnatomyCategoryEntryByCategoryName.IsNullOrEmpty())
            {
                foreach (var category in AnatomyCategoryEntryByCategoryName.Values)
                {
                    if (category.CategoryName == "Default"
                        || category.ID == 0)
                        continue;

                    if (!category.Entries.IsNullOrEmpty())
                    {
                        foreach (var bodyPlanEntry in category.Entries)
                            if (bodyPlanEntry.SameAs(BodyPlanEntry))
                                return category;
                    }
                }
            }
            return null;
        }
    }
}
