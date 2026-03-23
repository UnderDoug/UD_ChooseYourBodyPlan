using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;

using UD_ChooseYourBodyPlan.Mod.Logging;

using XRL;
using XRL.CharacterBuilds;
using XRL.Collections;
using XRL.UI;
using XRL.UI.Framework;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;

using UD_ChooseYourBodyPlan.Mod.TextHelpers;

using static UD_ChooseYourBodyPlan.Mod.AnatomyCategoryEntry;
using System.Linq;

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
                    var blueprints = GameObjectFactory.Factory?.Blueprints.Values ?? Enumerable.Empty<GameObjectBlueprint>();
                    foreach (var blueprint in blueprints)
                        if (IsGenerallyEligbleForDisplay(blueprint))
                            _GenerallyEligbleForDisplayBlueprints.Add(blueprint);
                }
                return _GenerallyEligbleForDisplayBlueprints;
            }
        }

        [ModSensitiveCacheInit]
        public static void ClearCacheOfGenerallyEligbleForDisplayBlueprints()
        {
            using Indent indent = new();
            Debug.LogCaller(indent);
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
                    Loading.LoadTask($"Loading {nameof(OptionDelegates)}", _Factory.LoadOptionDelegates);
                    Loading.LoadTask($"Loading {nameof(ModInfoByAnatomy)}", _Factory.LoadModInfoByAnatomy);
                    Loading.LoadTask($"Loading {TextElements.LoadingDataBucket}", _Factory.LoadTextElements);
                    Loading.LoadTask($"Loading {TransformationData.LoadingDataBucket}", _Factory.LoadTransformationData);
                    Loading.LoadTask($"Loading {BodyPlanEntry.LoadingDataBucket}", _Factory.LoadBodyPlans);
                    Loading.LoadTask($"Loading {AnatomyCategoryEntry.LoadingDataBucket}", _Factory.LoadAnatomyCategoryEntries);
                }
                return _Factory;
            }
        }

        public static int LowestCategory => 1;
        public static int HighestCategory => 23;

        public StringMap<OptionDelegateEntry> OptionDelegates;

        public StringMap<ModInfo> ModInfoByAnatomy;

        public Dictionary<string, TextElements> TextElementsByName;

        public Dictionary<string, TransformationData> TransformationDataByAnatomyName;

        public Dictionary<string, BodyPlanEntry> BodyPlanEntryByAnatomyName;

        public Dictionary<string, AnatomyCategoryEntry> AnatomyCategoryEntryByCategoryName;

        public bool ModdedAnatomiesFound { get; protected set; } = false;

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

        private void LoadModInfoByAnatomy()
        {
            using Indent indent = new();
            Debug.LogCaller(indent);

            ModInfoByAnatomy = new();
            foreach (var reader in DataManager.YieldXMLStreamsWithRoot("Bodies"))
            {
                Debug.Log(DataManager.SanitizePathForDisplay(reader.BaseURI), Indent: indent[1]);
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.EndElement
                        && reader.Name == "bodies")
                        break;

                    if (reader.NodeType == XmlNodeType.Element
                        && reader.Name == "anatomies")
                        ReadAnatomies(reader);
                }
            }
        }

        private void ReadAnatomies(XmlDataHelper Reader)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent);
            while (Reader.Read())
            {
                if (Reader.NodeType == XmlNodeType.EndElement
                    && Reader.Name == "anatomies")
                    break;

                if (Reader.NodeType == XmlNodeType.Element
                    && Reader.Name == "anatomy")
                    ReadAnatomy(Reader);
            }
        }

        private void ReadAnatomy(XmlDataHelper Reader)
        {
            using Indent indent = new(1);
            do
            {
                if (Reader.NodeType == XmlNodeType.EndElement
                    && Reader.Name == "anatomy")
                    break;

                if (Reader.NodeType == XmlNodeType.Element
                    && Reader.Name == "anatomy"
                    && Reader.GetAttribute("Name") is string anatomyName)
                {
                    string source = Reader.modInfo?.DisplayTitleStripped ?? Const.BASE_GAME;
                    Debug.LogMethod(indent,
                        ArgPairs: new Debug.ArgPair[]
                        {
                            Debug.Arg(anatomyName, source),
                        });
                    ModInfoByAnatomy[anatomyName] = Reader.modInfo;
                    if (Reader.modInfo != null)
                        ModdedAnatomiesFound = true;
                }
            }
            while (Reader.Read());
        }

        public static Type[] OptionDelegateMethodParameters = new Type[3]
        {
            typeof(string),
            typeof(BodyPlanEntry),
            typeof(EmbarkBuilder),
        };

        public void LoadOptionDelegates()
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent);
            OptionDelegates = new();
            foreach (var delegateMethod in ModManager.GetMethodsWithAttribute(typeof(OptionDelegateAttribute), typeof(HasOptionDelegateAttribute)))
            {
                var attribute = delegateMethod.GetCustomAttribute<OptionDelegateAttribute>();

                var modInfo = ModManager.GetMod(delegateMethod?.DeclaringType?.Assembly)
                    ?? Utils.ThisMod;

                if (delegateMethod.ReturnType != typeof(bool))
                {
                    modInfo.Warn($"Invalid return type for method decorated with {nameof(OptionDelegateAttribute)}: {delegateMethod?.Name}. " +
                        $"Method returns {delegateMethod.ReturnType} instead of {typeof(bool)}.");
                    continue;
                }

                if (!delegateMethod.IsStatic)
                {
                    modInfo.Warn($"Invalid method decorated with {nameof(OptionDelegateAttribute)}: {delegateMethod?.Name}. " +
                        $"Method must be static.");
                    continue;
                }

                if (delegateMethod.GetParameters() is ParameterInfo[] paramInfos)
                {
                    for (int i = 0; i < paramInfos.Length; i++)
                    {
                        if (i >= OptionDelegateMethodParameters.Length
                            || paramInfos[i].ParameterType != OptionDelegateMethodParameters[i])
                        {
                            string parameterString = OptionDelegateMethodParameters.Aggregate(
                                seed: "",
                                func: Utils.CommaSpaceDelimitedAggregator);
                            modInfo.Warn($"{nameof(OptionDelegateAttribute)} decorated method, {delegateMethod?.Name}, " +
                                $"must contain {OptionDelegateMethodParameters.Length} parameters: {parameterString}, in that order.");
                            continue;
                        }
                    }

                    string name = attribute?.Name ?? delegateMethod.Name;
                    string alternateName = $"{delegateMethod.DeclaringType.Name}.{delegateMethod.Name}";
                    string fullName = $"{delegateMethod.DeclaringType}.{delegateMethod.Name}";
                    var entry = new OptionDelegateEntry
                    {
                        OptionDelegate = (OptionDelegate)delegateMethod.CreateDelegate(typeof(OptionDelegate)),
                    };
                    if (!name.IsNullOrEmpty())
                        OptionDelegates[name] = entry;
                    OptionDelegates[alternateName] = entry;
                    OptionDelegates[fullName] = entry;
                }
            }
        }

        public void LoadTextElements()
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent);
            Load(ref TextElementsByName);
            TextElementsInitialized = true;
        }

        public void LoadTransformationData()
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent);
            Load(ref TransformationDataByAnatomyName);
            TransformationDataInitialized = true;
        }

        public void LoadBodyPlans()
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent);
            LoadBaseBodyPlanEntries();
            Load(ref BodyPlanEntryByAnatomyName);
            BodyPlanEntriesInitialized = true;
        }

        public void LoadAnatomyCategoryEntries()
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent);
            LoadFromCategoryIDs();
            Load(ref AnatomyCategoryEntryByCategoryName);
            AssignCategoryEntries();
            AnatomyCategoryEntriesInitialized = true;
        }

        public void Load<T>(ref Dictionary<string, T> CacheByName)
            where T : ILoadFromDataBucket<T>, new()
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(typeof(T).ToStringWithGenerics()),
                });

            CacheByName ??= new();
            try
            {
                foreach (var dataBucket in GetDataBuckets<T>())
                {
                    Debug.Log(nameof(dataBucket), dataBucket.Name, Indent: indent[1]);
                    if (TryLoadFromDataBucket(dataBucket, out T loaded))
                    {
                        if (loaded.CacheKey is not string cacheKey
                            || cacheKey.IsNullOrEmpty())
                        {
                            Utils.Error($"{loaded.GetType()} with empty or null {nameof(loaded.CacheKey)} from DataBucket \"{dataBucket.Name}\"");
                        }
                        else
                        {
                            if (CacheByName.ContainsKey(cacheKey))
                                CacheByName[cacheKey].Merge(loaded);
                            else
                                CacheByName[cacheKey] = loaded;
                        }
                    }
                }
            }
            catch (Exception x)
            {
                Utils.Error(x);
            }
        }

        public IEnumerable<GameObjectBlueprint> GetDataBuckets<T>()
            where T : ILoadFromDataBucket<T>, new()
            => GameObjectFactory.Factory
                ?.SafelyGetBlueprintsInheritingFrom(ILoadFromDataBucket<T>.GetBaseDataBucketBlueprint())
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
                try
                {
                    BodyPlanEntryByAnatomyName[NoEntryName] = new BodyPlanEntry().LoadFromDataBucket(noEntryDataBucket);
                    foreach (var anatomy in Anatomies.AnatomyList)
                    {
                        if (LoadFromAnatomy(anatomy) is BodyPlanEntry anatomyEntry)
                            BodyPlanEntryByAnatomyName[anatomy.Name] = anatomyEntry;
                    }
                }
                catch (Exception x)
                {
                    Utils.Error(x);
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

        public void LoadFromCategoryIDs()
        {
            AnatomyCategoryEntryByCategoryName ??= new();
            for (int i = 0; i <= HighestCategory; i++)
            {
                try
                {
                    var newCategory = new AnatomyCategoryEntry
                    {
                        ID = i,
                        CategoryName = GetBodyPartCategoryName(i),
                        DisplayName = GetBodyPartCategoryName(i),
                        Shader = new Shader(GetBodyPartCategoryColor(i)).Finalize(),
                        Entries = new()
                    };
                    AnatomyCategoryEntryByCategoryName.Add(newCategory.CategoryName, newCategory);
                }
                catch (Exception x)
                {
                    MetricsManager.LogModWarning(Utils.ThisMod, $"Attempted to make {nameof(AnatomyCategoryEntry)} from invalid {nameof(BodyPartCategory)} value: {i}; {x}");
                }
            }
        }

        public void AssignCategoryEntries()
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent);

            using var bodyPlanEntries = ScopeDisposedList<BodyPlanEntry>.GetFromPoolFilledWith(BodyPlanEntryByAnatomyName.Values);
            Debug.Log($"Clearing {BodyPlanEntry.NO_LIST_TAG} from Assignment pool...", Indent: indent[1]);
            bool anyRemoved = false;
            foreach (var entry in BodyPlanEntryByAnatomyName.Values)
            {
                if (entry.HasTag("NoList"))
                {
                    Debug.CheckYeh(entry.AnatomyName, "removed", Indent: indent[2]);
                    anyRemoved = bodyPlanEntries.Remove(entry)
                        || anyRemoved;
                }
            }
            if (!anyRemoved)
                Debug.CheckNah("None", Indent: indent[2]);

            Debug.Log($"Assigning Categories...", Indent: indent[1]);
            foreach (var category in AnatomyCategoryEntryByCategoryName.Values)
            {
                Debug.Log($"ID: {category.ID} | {category.CategoryName}", Indent: indent[2]);
                category.Entries ??= new();
                if (category.CategoryName == "Default"
                    || category.ID == 0)
                {
                    category.Entries.AddRange(bodyPlanEntries);
                    Debug.Loggregrate(
                        Source: category.Entries,
                        Proc: n => n.DisplayName,
                        Empty: "None",
                        PostProc: s => $"::{s}",
                        Indent: indent[3]);
                }
                else
                {
                    bool anyAdded = false;
                    foreach (var bodyPlanEntry in bodyPlanEntries)
                    {
                        if (bodyPlanEntry.CategoryOverride == category.CategoryName
                            || bodyPlanEntry.BestGuessForCategoryID() == category.ID)
                        {
                            category.Entries.Add(bodyPlanEntry);
                            anyAdded = true;
                            Debug.Log($"::{bodyPlanEntry.DisplayName}", Indent: indent[3]);
                        }
                    }
                    if (!anyAdded)
                        Debug.CheckNah("::None", Indent: indent[3]);
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

        public IEnumerable<BodyPlanEntry> GetBodyPlanEntries(Predicate<BodyPlanEntry> Where = null)
        {
            if (BodyPlanEntryByAnatomyName.IsNullOrEmpty()
                && !BodyPlanEntriesInitialized)
            {
                Utils.Error($"{nameof(BodyPlanFactory)} attempted to iterate {nameof(BodyPlanEntriesInitialized)} before initialization has been performed.");
                yield break;
            }
            foreach (var bodyPlanEntry in BodyPlanEntryByAnatomyName.Values)
                if (Where?.Invoke(bodyPlanEntry) is not false)
                    yield return bodyPlanEntry;
        }

        public BodyPlanEntry GetBodyPlanEntry(string Anatomy)
            => RequireBodyPlanEntry(Anatomy)
            ;

        public BodyPlanEntry GetBodyPlanEntry(Anatomy Anatomy)
            => RequireBodyPlanEntry(Anatomy?.Name)
            ;

        public BodyPlanEntry GetBodyPlanEntry(PrefixMenuOption MenuOption)
            => GetBodyPlanEntry(MenuOption?.Id)
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
