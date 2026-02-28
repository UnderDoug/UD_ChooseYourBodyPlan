using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ConsoleLib.Console;

using XRL.UI.Framework;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;

using UD_BodyPlan_Selection.Mod;
using XRL.Collections;

namespace XRL.CharacterBuilds.Qud
{
    [HasOptionFlagUpdate]
    public class Qud_UD_BodyPlanModule : QudEmbarkBuilderModule<Qud_UD_BodyPlanModuleData>
    {
        [HasModSensitiveStaticCache]
        public class AnatomyChoice
        {
            [ModSensitiveStaticCache]
            private static IEnumerable<GameObjectBlueprint> _GenerallyEligbleForDisplayBlueprints = null;
            public static IEnumerable<GameObjectBlueprint> GenerallyEligbleForDisplayBlueprints
            {
                get
                {
                    if (_GenerallyEligbleForDisplayBlueprints.IsNullOrEmpty())
                        _GenerallyEligbleForDisplayBlueprints = GameObjectFactory.Factory
                            ?.BlueprintList
                            ?.Where(IsGenerallyEligbleForDisplay);

                    return _GenerallyEligbleForDisplayBlueprints;
                }
            }

            protected static GameObject SampleCreature = null;

            public static string MISSING_ANATOMY => nameof(MISSING_ANATOMY);

            public Anatomy Anatomy;

            public Renderable Renderable;

            public bool IsDefault;

            public AnatomyChoice()
            {
                Anatomy = null;
                Renderable = null;
                IsDefault = false;
            }
            public AnatomyChoice(Anatomy Anatomy, bool IsDefault, Renderable Renderable)
                : this()
            {
                this.Anatomy = Anatomy;
                this.Renderable = Renderable;
                this.IsDefault = IsDefault;
            }
            public AnatomyChoice(Anatomy Anatomy, Renderable Renderable)
                : this(Anatomy, false, Renderable)
            {
            }
            public AnatomyChoice(Anatomy Anatomy, bool IsDefault)
                : this(Anatomy, IsDefault, null)
            {
            }
            public AnatomyChoice(Anatomy Anatomy)
                : this(Anatomy, false, null)
            {
            }

            [ModSensitiveCacheInit]
            public static void ClearCacheOfGenerallyEligbleForDisplayBlueprints()
            {
                Utils.Log(typeof(AnatomyChoice).CallChain(nameof(ClearCacheOfGenerallyEligbleForDisplayBlueprints)));
                _GenerallyEligbleForDisplayBlueprints = null;
            }

            public override string ToString()
                => GetDescription(true) + (Renderable?.Tile is string tile ? " " + tile : null);

            public string GetDescription(bool ShowDefault = false)
            {
                SB.Clear();

                SB.Append(Anatomy?.Name.SplitCamelCase() ?? MISSING_ANATOMY);
                if (IsDefault && ShowDefault)
                    SB.Append(" (default)");

                return SB.ToString();
            }

            public string GetLongDescription(
                bool Summary = false,
                bool IncludeOpening = false,
                bool IsTrueKin = false
                )
            {
                SB.Clear();

                if (Anatomy == null)
                    return SB.ToString();

                if (Anatomy.Sucks())
                {
                    if (!Summary)
                        SB.AppendColored("r", "This body plan will be difficult to play with.")
                            .AppendLine();
                    else
                        SB.AppendColored("r", "Difficult to play.");
                    SB.AppendLine();
                }

                if (Anatomy.HasRecipe())
                {
                    if (!Summary)
                        SB.AppendColored("m", "There is a cooking recipe to get this body plan.")
                            .AppendLine();
                    else
                        SB.AppendColored("m", "Avaialable via cooking.");
                    SB.AppendLine();
                }

                if (Anatomy.Category == BodyPartCategory.MECHANICAL
                    && !Options.EnableBodyPlansThatAreRoboticWithoutMakingYouRobotic)
                {
                    if (!Summary)
                        SB.AppendColored("c", "You will be made mechanical with this body plan.")
                            .AppendLine();
                    else
                        SB.AppendColored("c", "You are mechanical.");
                    SB.AppendLine();
                }

                if (IncludeOpening)
                {
                    if (!Summary)
                        SB.Append("Includes the following body part slots:");
                    else
                        SB.Append("Included parts:");
                }

                if (!Summary)
                {
                    SampleCreature ??= GameObject.CreateSample("Humanoid");
                    Anatomy.ApplyTo(SampleCreature.Body);

                    foreach (BodyPart bodyPart in SampleCreature.Body.GetParts())
                        GetBodyPartString(SB, bodyPart, IsTrueKin);

                    SB.AppendLine();
                    if (IsTrueKin)
                        SB.AppendLine()
                            .AppendNoCybernetics(false).Append(" - Incompatible with {{c|cybernetics}}")
                            .AppendLine();
                }
                else
                {
                    var limbCounts = new Dictionary<BodyPartType, int>();
                    if (GetAllParts(Anatomy, null).Select(a => a.Type) is IEnumerable<BodyPartType> limbs)
                        foreach (BodyPartType limb in limbs)
                        {
                            if (limbCounts.ContainsKey(limb))
                                limbCounts[limb]++;
                            else
                                limbCounts[limb] = 1;
                        }

                    foreach ((BodyPartType limb, int count) in limbCounts)
                    {
                        if (!SB.IsNullOrEmpty())
                            SB.AppendLine();

                        string timesColored = "}}x {{W|";
                        string limbName = timesColored + limb.Name;
                        string limbPluralName = null;
                        if (limb.Plural.GetValueOrDefault())
                            limbPluralName = limbName;

                        SB.Append("{{W|").Append(count.Things(limbName, limbPluralName)).Append("}}");
                        if (!limb.Name.EqualsNoCase(limb.FinalType))
                            SB.Append(" (").Append(limb.FinalType).Append(")");

                        if (IsTrueKin
                            && limb.Category != BodyPartCategory.ANIMAL)
                            SB.AppendNoCybernetics();
                    }
                }

                return SB.ToString();
            }

            public static IEnumerable<AnatomyPart> GetAllParts(Anatomy Anatomy, AnatomyPart AnatomyPart)
            {
                if (Anatomy == null
                    && AnatomyPart == null)
                    yield break;

                if (Anatomy != null)
                    foreach (AnatomyPart anatomyPart in Anatomy.Parts)
                        foreach (AnatomyPart part in GetAllParts(null, anatomyPart))
                            yield return part;

                if (AnatomyPart != null)
                {
                    yield return AnatomyPart;
                    if (AnatomyPart.Subparts != null)
                        foreach (AnatomyPart part in AnatomyPart.Subparts)
                            foreach (AnatomyPart subpart in GetAllParts(null, part))
                                yield return subpart;
                }
            }

            public static StringBuilder GetAnatomyPartString(
                StringBuilder SB,
                AnatomyPart AnatomyPart,
                int Indent = 0,
                bool IsTrueKin = false
                )
            {
                var limb = AnatomyPart.Type;

                if (!SB.IsNullOrEmpty())
                    SB.AppendLine();

                if (Indent > 0)
                    SB.Append(" ".ThisManyTimes(Indent * 2));

                var bodypart = new BodyPart();
                limb.ApplyTo(bodypart);

                SB.AppendColored("W", bodypart.GetCardinalDescription());
                if (!limb.Name.EqualsNoCase(limb.FinalType))
                    SB.Append(" (").Append(limb.FinalType).Append(")");

                if (GameObjectFactory.Factory.GetBlueprintIfExists(limb.DefaultBehavior) is GameObjectBlueprint defaultBehvaiour)
                    SB.Append(" [").AppendColored("w", defaultBehvaiour.DisplayName()).Append("]");

                if (IsTrueKin
                    && limb.Category != BodyPartCategory.ANIMAL)
                    SB.AppendNoCybernetics();

                if (!AnatomyPart.Subparts.IsNullOrEmpty())
                    foreach (AnatomyPart subpart in AnatomyPart.Subparts)
                        GetAnatomyPartString(SB, subpart, Indent + 1, IsTrueKin);

                return SB;
            }

            public static StringBuilder GetBodyPartString(
                StringBuilder SB,
                BodyPart BodyPart,
                bool IsTrueKin = false
                )
            {
                if (!SB.IsNullOrEmpty())
                    SB.AppendLine();

                int indent = (BodyPart?.ParentBody?.GetPartDepth(BodyPart)).GetValueOrDefault();

                if (indent > 0)
                    SB.Append(" ".ThisManyTimes(indent * 2));

                SB.AppendColored("K", "\x002E").Append(' ').Append(BodyPart.GetCardinalDescription());

                if (!BodyPart.Name.EqualsNoCase(BodyPart.Type))
                    SB.Append(" (").Append(BodyPart.Type).Append(")");

                if (GameObjectFactory.Factory.GetBlueprintIfExists(BodyPart.DefaultBehaviorBlueprint) is GameObjectBlueprint defaultBehvaiour)
                    SB.Append(" [").AppendColored("w", defaultBehvaiour.CachedDisplayNameStrippedLC).Append("]");

                if (IsTrueKin
                    && !BodyPart.CanReceiveCyberneticImplant())
                    SB.AppendNoCybernetics();

                return SB;
            }

            public Renderable GetRenderable()
            {
                if (Renderable == null
                    && Anatomy != null)
                    Renderable = GetExampleBlueprint()?.GetRenderable();

                return Renderable;
            }

            private static string GetTile(GameObjectBlueprint Blueprint)
                => Blueprint.GetPartParameter<string>(nameof(Render), nameof(Render.Tile))
                ;
            private static string GetAnatomy(GameObjectBlueprint Blueprint)
                => Blueprint.GetPartParameter<string>(nameof(Body), nameof(Body.Anatomy))
                ;
            public static bool IsGenerallyEligbleForDisplay(GameObjectBlueprint Blueprint)
            {
                if (Blueprint == null)
                    return false;

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

                if (Blueprint.Name.Contains("Cherub"))
                    return false;

                if (GetTile(Blueprint) is not string renderTile)
                    return false;

                if (renderTile.Contains("sw_farmer")
                    && GetAnatomy(Blueprint) is string anatomy
                    && !anatomy.EqualsNoCase("Humanoid"))
                    return false;

                if (Blueprint.TryGetPartParameter(nameof(Door), nameof(Door.SyncAdjacent), out bool syncAdjacent)
                    && syncAdjacent)
                    return false;

                return true;
            }

            public bool HasMatchingAnatomy(GameObjectBlueprint Blueprint)
                => Blueprint != null
                && Anatomy != null
                && GetAnatomy(Blueprint) is string anatomy
                && anatomy == Anatomy.Name
                ;
            public bool ObjectAnimatesWithAnatomy(GameObjectBlueprint Blueprint)
                => Blueprint != null
                && Anatomy != null
                && Blueprint.TryGetTag("BodyType", out string bodyType) 
                && bodyType == Anatomy.Name
                ;
            public bool InheritsFromAnatomy(GameObjectBlueprint Blueprint)
                => Blueprint != null
                && Anatomy != null
                && Blueprint.InheritsFrom(Anatomy.Name)
                ;

            private static bool IsNotExcluded(
                GameObjectBlueprint Blueprint,
                bool AllowExcludedFromDynamicEncounters
                )
                => AllowExcludedFromDynamicEncounters
                || !Blueprint.IsExcludedFromDynamicEncounters()
                ;
            public IEnumerable<GameObjectBlueprint> GetExampleBlueprints(
                bool FallBackToExcluded = true,
                bool AllowExcludedFromDynamicEncounters = false
                )
            {
                var blueprints = GenerallyEligbleForDisplayBlueprints;

                AllowExcludedFromDynamicEncounters = true;

                if (blueprints
                    ?.Where(HasMatchingAnatomy)
                    ?.Where(bp => IsNotExcluded(bp, AllowExcludedFromDynamicEncounters)) is IEnumerable<GameObjectBlueprint> objectsWithAnatomy
                    && !objectsWithAnatomy.IsNullOrEmpty())
                    return objectsWithAnatomy;

                if (blueprints
                    ?.Where(ObjectAnimatesWithAnatomy)
                    ?.Where(bp => IsNotExcluded(bp, AllowExcludedFromDynamicEncounters)) is IEnumerable<GameObjectBlueprint> objectsAnimatingWithAnatomy
                    && !objectsAnimatingWithAnatomy.IsNullOrEmpty())
                    return objectsAnimatingWithAnatomy;

                if (blueprints
                    ?.Where(InheritsFromAnatomy)
                    ?.Where(bp => IsNotExcluded(bp, AllowExcludedFromDynamicEncounters)) is IEnumerable<GameObjectBlueprint> objectsInheritingAnatomy
                    && !objectsInheritingAnatomy.IsNullOrEmpty())
                    return objectsInheritingAnatomy;

                /*
                if (FallBackToExcluded
                    && !AllowExcludedFromDynamicEncounters
                    && GetExampleBlueprints(false, false) is IEnumerable<GameObjectBlueprint> dynamicEncounterExcludedObjects
                    && !dynamicEncounterExcludedObjects.IsNullOrEmpty())
                    return dynamicEncounterExcludedObjects;
                */

                return new GameObjectBlueprint[0];
            }

            public GameObjectBlueprint GetExampleBlueprint()
                => GetExampleBlueprints(FallBackToExcluded: true, AllowExcludedFromDynamicEncounters: false)
                    ?.GetRandomElementCosmetic()
                ?? GameObjectFactory.Factory.GetBlueprintIfExists("Mimic")
                ;
        }

        private static bool WantClearLists = false;

        private static IEnumerable<AnatomyChoice> _BaseAnatomyChoices;
        public static IEnumerable<AnatomyChoice> BaseAnatomyChoices => _BaseAnatomyChoices ??= Anatomies.AnatomyList
            ?.Where(IsEligibleAnatomy)
            ?.Select(AnatomyToChoice)
            ?.OrderBy(a => a.Anatomy.Name)
            ;

        public static Dictionary<int, bool> OptionallyEnabledAnatomyCategories => new()
        {
            { BodyPartCategory.LIGHT, Options.EnableBodyPlansThatSuck },
            { BodyPartCategory.MECHANICAL, Options.EnableBodyPlansThatAreRobotic },
        };

        public static string[] ExcludedAnatomies => new string[]
        {
            "HumanoidWithHandsFace",
        };

        public static Dictionary<string, bool> OptionallyEnabledAnatomies => new()
        {
            { "Echinoid", Options.EnableBodyPlansThatSuck },
            { "SlugWithHands", Options.EnableBodyPlansAvailableViaRecipe },
            { "HumanoidOctohedron", Options.EnableBodyPlansAvailableViaRecipe },
        };

        public bool WindowShown;

        public bool HasSelection => data?.HasSelection ?? false;

        private AnatomyChoice _PlayerAnatomyChoice; 
        public AnatomyChoice PlayerAnatomyChoice
        {
            get
            {
                if (_PlayerAnatomyChoice == null
                    && GetDefaultPlayerBodyPlan() is string playerAnatomyName
                    && !_AnatomyChoices.IsNullOrEmpty())
                    _PlayerAnatomyChoice = AnatomyChoices.FirstOrDefault(a => a?.Anatomy?.Name == playerAnatomyName);

                return _PlayerAnatomyChoice;
            }
            set => _PlayerAnatomyChoice = null;
        }

        public override AbstractEmbarkBuilderModuleData DefaultData => GetDefaultData(true);

        private List<AnatomyChoice> _AnatomyChoices;
        public List<AnatomyChoice> AnatomyChoices
        {
            get
            {
                if (_AnatomyChoices.IsNullOrEmpty()
                    || WantClearLists)
                {
                    WantClearLists = false;
                    _AnatomyChoices = new();
                    _AnatomyChoices.AddRange(BaseAnatomyChoices.Where(AnatomyChoiceIsValid));
                    _AnatomyChoices.RemoveAll(c => c == null || c.Anatomy == null);
                    SetDefautChoice(true);
                }
                return _AnatomyChoices;
            }
        }

        protected static StringBuilder SB = new();

        public QudGenotypeModuleData GenotypeModuleData => builder?.GetModule<QudGenotypeModule>()?.data;
        public QudSubtypeModuleData SubtypeModuleData => builder?.GetModule<QudSubtypeModule>()?.data;

        public Qud_UD_BodyPlanModule()
        {
            WindowShown = false;
            _AnatomyChoices = null;
            _PlayerAnatomyChoice = null;
        }

        public override bool shouldBeEditable()
            => builder.IsEditableGameMode();

        public override bool shouldBeEnabled()
            => GenotypeModuleData?.Entry is GenotypeEntry genotypeEntry
            && (!genotypeEntry.IsTrueKin
                || Options.EnableBodyPlansForTK)
            && SubtypeModuleData?.Subtype != null;

        private static bool IsQudQudMutationsModuleWindowDescriptor(EmbarkBuilderModuleWindowDescriptor Descriptor)
            => Descriptor.module is QudMutationsModule
            ;
        public override void assembleWindowDescriptors(List<EmbarkBuilderModuleWindowDescriptor> windows)
        {
            int index = windows.FindIndex(IsQudQudMutationsModuleWindowDescriptor);
            if (index < 0)
                base.assembleWindowDescriptors(windows);
            else
                windows.InsertRange(index + 1, this.windows.Values);
        }

        public override SummaryBlockData GetSummaryBlock()
        {
            AnatomyChoice anatomyChoice = SelectedChoice();
            anatomyChoice.GetRenderable();
            string shortDesc = "{{C|::}}" + anatomyChoice.GetDescription() + "{{C|::}}";
            string longDesc = anatomyChoice.GetLongDescription(
                Summary: true,
                IncludeOpening: true,
                IsTrueKin: GenotypeModuleData?.Entry?.IsTrueKin ?? false);
            return new SummaryBlockData
            {
                Id = GetType().FullName,
                Title = "Body Plan",
                Description = shortDesc + "\n" + longDesc,
                SortOrder = 50
            };
        }

        public override object handleBootEvent(
            string id,
            XRLGame game,
            EmbarkInfo info,
            object element = null
            )
        {
            if (element is GameObject player
                && player.Body is Body playerBody
                && data.Selection.Anatomy is string anatomy)
            {
                if (data == null
                    || data.Selection == null)
                {
                    MetricsManager.LogWarning("Body Plan module was active but data or selections was null or empty.");
                    return element;
                }

                if (id == QudGameBootModule.BOOTEVENT_BOOTPLAYEROBJECT)
                    playerBody.Rebuild(data.Selection.Anatomy);

                if (id == QudGameBootModule.BOOTEVENT_AFTERBOOTPLAYEROBJECT)
                {
                    string defaultSpecies = SubtypeModuleData?.Entry?.Species
                        .Coalesce(GenotypeModuleData?.Entry.Species);
                    string defaultTile = SubtypeModuleData?.Entry?.Tile
                        .Coalesce(GenotypeModuleData?.Entry.Tile);
                    if (anatomy == "SlugWithHands")
                    {
                        player.RequirePart<Mutations>().AddMutation(new SlogGlands());
                        playerBody.RegenerateDefaultEquipment();

                        if (player.Render.Tile == defaultTile)
                        {
                            player.Render.RenderString = "Q";
                            player.Render.Tile = "Creatures/sw_slog.bmp";
                        }

                        player.SetStringProperty("AteCloacaSurprise", "true");

                        if (player.GetSpecies() == defaultSpecies)
                            player.SetStringProperty("Species", "slug");
                    }
                    if (anatomy == "HumanoidOctohedron")
                    {
                        if (MutationFactory.TryGetMutationEntry("Crystallinity", out var Entry))
                            player.RequirePart<Mutations>().AddMutation(Entry);
                        playerBody.RegenerateDefaultEquipment();

                        if (player.Render.Tile == defaultTile)
                        {
                            player.Render.RenderString = "µ";
                            player.Render.Tile = "Creatures/sw_crystal_body.png";
                        }

                        player.SetStringProperty("AteCrystalDelight", "true");

                        if (player.GetSpecies() == defaultSpecies)
                            player.SetStringProperty("Species", "crystal");
                    }

                    if (Anatomies.GetAnatomy(anatomy).Category == BodyPartCategory.MECHANICAL
                        && !Options.EnableBodyPlansThatAreRoboticWithoutMakingYouRobotic)
                        World.ObjectBuilders.Roboticized.Roboticize(player);
                }
            }
            return base.handleBootEvent(id, game, info, element);
        }

        public override string DataErrors()
        {
            if (AnatomyChoices.IsNullOrEmpty())
                throw new InvalidOperationException("No anatomies to choose from");

            if (data?.Selection?.Anatomy is string selectedAnatomy
                && Anatomies.GetAnatomy(selectedAnatomy) is null)
                return "Selection is not a valid anatomy";

            return base.DataErrors();
        }

        public override string DataWarnings()
        {
            if (data?.Selection == null)
                return "You have not selected a body plan.\n" +
                    $"The default for your selected genotype/subtype is {GetDefaultPlayerBodyPlan().SplitCamelCase()}";
            return base.DataWarnings();
        }

        public override void handleModuleDataChange(
            AbstractEmbarkBuilderModule module,
            AbstractEmbarkBuilderModuleData oldValues,
            AbstractEmbarkBuilderModuleData newValues
            )
        {
            base.handleModuleDataChange(module, oldValues, newValues);

            if (module is not QudGenotypeModule
                && module is not QudSubtypeModule)
                return;

            if (module == this)
                return;

            OrganizeAnatomyChoices();
        }

        [OptionFlagUpdate]
        public static void OnOptionUpdate()
        {
            _BaseAnatomyChoices = null;
            WantClearLists = true;
        }

        public static bool AnatomyChoiceIsValid(AnatomyChoice Choice)
            => Choice.GetDescription() != AnatomyChoice.MISSING_ANATOMY
            && Choice.Anatomy != null
            ;
        public void OrganizeAnatomyChoices(bool SelectDefaultChoice = false)
        {
            if (AnatomyChoices.IsNullOrEmpty())
                MetricsManager.LogCallingModError(nameof(AnatomyChoices) + " empty when it probably shouldn't be.");

            PlayerAnatomyChoice = null;
            if (PlayerAnatomyChoice != null
                && AnatomyChoices[0] != PlayerAnatomyChoice)
            {
                AnatomyChoices.OrderBy(a => a.Anatomy.Name);
                AnatomyChoices.Remove(PlayerAnatomyChoice);
                AnatomyChoices.Insert(0, PlayerAnatomyChoice);
                SetDefautChoice(SelectDefaultChoice);
            }
        }

        public static bool IsEligibleAnatomy(Anatomy Anatomy)
            => Anatomy != null
            && AnatomyCategoryNotExcluded(Anatomy)
            && AnatomyNameNotExcluded(Anatomy)
            // && true.LogReturning(Anatomy.Name)
            ;
        private static bool IsAnatomyEitherCategory(Anatomy Anatomy, int Category)
            => Anatomy.BodyCategory == Category
            || Anatomy.Category == Category
            ;
        public static bool AnatomyCategoryNotExcluded(Anatomy Anatomy)
        {
            if (Anatomy == null)
                return false;

            if (!OptionallyEnabledAnatomyCategories.IsNullOrEmpty())
                foreach ((int category, bool allowed) in OptionallyEnabledAnatomyCategories)
                    if (IsAnatomyEitherCategory(Anatomy, category)
                        && !allowed)
                        return false;

            return true;
        }
        public static bool AnatomyNameNotExcluded(Anatomy Anatomy)
        {
            if (Anatomy == null)
                return false;

            if (!ExcludedAnatomies.IsNullOrEmpty()
                && ExcludedAnatomies.Contains(Anatomy.Name))
                return false;

            if (!OptionallyEnabledAnatomies.IsNullOrEmpty())
                foreach ((string anatomyName, bool allowed) in OptionallyEnabledAnatomies)
                    if (Anatomy.Name == anatomyName
                        && !allowed)
                        return false;

            return true;
        }

        public static AnatomyChoice AnatomyToChoice(Anatomy Anatomy)
            => new(Anatomy)
            ;
        public void PickAnatomy(int n)
        {
            data ??= new Qud_UD_BodyPlanModuleData();

            if (AnatomyChoices.IsNullOrEmpty())
                MetricsManager.LogCallingModError(nameof(AnatomyChoices) + " empty when it probably shouldn't be.");
            else
                data.Selection = new(AnatomyChoices[n]);

            setData(data);
        }

        private string GetPlayerBlueprint()
        {
            var body = GenotypeModuleData?.Entry?.BodyObject
                .Coalesce(SubtypeModuleData?.Entry?.BodyObject)
                .Coalesce("Humanoid");

            return builder.info?.fireBootEvent(QudGameBootModule.BOOTEVENT_BOOTPLAYEROBJECTBLUEPRINT, The.Game, body);
        }

        public string GetDefaultPlayerBodyPlan()
            => GameObjectFactory.Factory
                ?.GetBlueprintIfExists(GetPlayerBlueprint())
                ?.GetPartParameter<string>(nameof(Body), nameof(Body.Anatomy));

        public Qud_UD_BodyPlanModuleData GetDefaultData(bool Prefill = false)
            => Prefill
            ? new(PlayerAnatomyChoice)
            : new();

        public void SetDefautChoice(bool SelectDefaultChoice = false)
        {
            PlayerAnatomyChoice = null;
            if (PlayerAnatomyChoice is AnatomyChoice defaultChoice)
            {
                defaultChoice.IsDefault = true;

                if (GenotypeModuleData?.Entry is GenotypeEntry genotypeEntry
                    && SubtypeModuleData?.Entry is SubtypeEntry subtypeEntry
                    && defaultChoice.GetRenderable() is Renderable defaultRenderable)
                {
                    if (subtypeEntry.Tile.Coalesce(genotypeEntry.Tile) is string typeTile)
                        defaultRenderable.setTile(typeTile);

                    if (subtypeEntry.DetailColor.Coalesce(genotypeEntry.DetailColor) is string typeDetailColor)
                        defaultRenderable.setDetailColor(typeDetailColor[0]);

                    defaultRenderable.setTileColor("&Y");
                }

                if (SelectDefaultChoice
                    && (data.Selection == null
                        || data.Selection.Anatomy.IsNullOrEmpty()))
                    data.Selection = new(defaultChoice);

                setData(data);
            }
        }

        public bool IsSelected(AnatomyChoice Choice)
            => Choice != null
            && data != null
            && ((Choice.Anatomy == null
                    && data.Selection == null)
                || Choice.Anatomy?.Name == data.Selection?.Anatomy);

        public AnatomyChoice SelectedChoice()
            => AnatomyChoices.Find(IsSelected);
    }
}
