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
using static UD_BodyPlan_Selection.Mod.AnatomyExclusion;

namespace XRL.CharacterBuilds.Qud
{
    [HasOptionFlagUpdate]
    public class Qud_UD_BodyPlanModule : QudEmbarkBuilderModule<Qud_UD_BodyPlanModuleData>
    {
        public class AnatomyChoice
        {
            protected static GameObject SampleCreature = null;

            public static string MISSING_ANATOMY => nameof(MISSING_ANATOMY);

            public Anatomy Anatomy;

            private AnatomyExclusion _AnatomyExclusion;
            public AnatomyExclusion AnatomyExclusion => _AnatomyExclusion ??= Utils.GetAnatomyExclusion(this);

            public Renderable Renderable;

            public bool IsDefault;

            private string _LongDescription;
            public string LongDescription => _LongDescription ??= GetLongDescription(IncludeOpening: true);

            private string _LongDescriptionSummary;
            public string LongDescriptionSummary => _LongDescriptionSummary ??= GetLongDescription(Summary: true, IncludeOpening: true);

            private string _LongDescriptionTK;
            public string LongDescriptionTK => _LongDescriptionTK ??= GetLongDescription(IncludeOpening: true, IsTrueKin: true);

            private string _LongDescriptionTKSummary;
            public string LongDescriptionTKSummary => _LongDescriptionTKSummary ??= GetLongDescription(Summary: true, IncludeOpening: true, IsTrueKin: true);

            public AnatomyChoice()
            {
                Anatomy = null;
                _AnatomyExclusion = null;
                Renderable = null;
                IsDefault = false;

                _LongDescription = null;
                _LongDescriptionSummary = null;
                _LongDescriptionTK = null;
                _LongDescriptionTKSummary = null;
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

            public override string ToString()
                => GetDescription(true) + (Renderable?.Tile is string tile ? " " + tile : null);

            public void ClearLongDescriptionCaches()
            {
                _LongDescription = null;
                _LongDescriptionSummary = null;
                _LongDescriptionTK = null;
                _LongDescriptionTKSummary = null;
            }

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

                /*
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
                */

                if ((AnatomyExclusion?.IsMechanical ?? false)
                    && !Options.EnableBodyPlansThatAreRoboticWithoutMakingYouRobotic)
                {
                    if (!Summary)
                        SB.AppendColored("c", "You will be made mechanical with this body plan.")
                            .AppendLine();
                    else
                        SB.AppendColored("c", "You are mechanical.");
                    SB.AppendLine();
                }

                if (!Summary
                    && AnatomyExclusion?.ExceptionMessage is string exceptionMessage
                    && !exceptionMessage.IsNullOrEmpty())
                    SB.Append(exceptionMessage)
                        .AppendLine()
                        .AppendLine();

                if (Summary
                    && AnatomyExclusion?.ExceptionSummary is string exceptionSummary
                    && !exceptionSummary.IsNullOrEmpty())
                    SB.Append(exceptionSummary)
                        .AppendLine();

                if (IncludeOpening)
                {
                    if (!Summary)
                        SB.Append("Includes the following body part slots:");
                    else
                        SB.Append("Included parts:");
                }

                SampleCreature ??= GameObject.CreateSample("Humanoid");
                Anatomy.ApplyTo(SampleCreature.Body);
                if (!Summary)
                {
                    foreach (BodyPart bodyPart in SampleCreature.Body.GetParts())
                        GetBodyPartString(SB, bodyPart, IsTrueKin);

                    SB.AppendLine();
                    if (IsTrueKin)
                        SB
                            .AppendLine()
                            .AppendNoCybernetics(false).Append(" - Incompatible with {{c|cybernetics}}")
                            ;

                    SB
                        .AppendLine()
                        .AppendColored("w", "Indicates natural equipment/default behaviour")
                        ;
                }
                else
                {
                    var limbCounts = new Dictionary<BodyPartType, int>();
                    if (SampleCreature.Body.GetParts().Select(p => p.TypeModel()) is IEnumerable<BodyPartType> limbs)
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

                        if (limbName.EqualsNoCase("foot"))
                            limbPluralName = timesColored + "feet";

                        SB.Append("{{W|").Append(count.Things(limbName, limbPluralName)).Append("}}");
                        
                        /*
                        if (!limb.Name.EqualsNoCase(limb.FinalType))
                            SB.Append(" (").Append(limb.FinalType).Append(")");
                        */

                        if (IsTrueKin
                            && limb.Category != BodyPartCategory.ANIMAL)
                            SB.AppendNoCybernetics();
                    }
                }

                return SB.ToString();
            }

            public static IEnumerable<AnatomyPart> GetAllParts(Anatomy Anatomy, AnatomyPart AnatomyPart)
            {
                // Doesn't seem to grab all the parts this anatomy has.
                if (Anatomy == null
                    && AnatomyPart == null)
                    yield break;

                if (Anatomy != null)
                    foreach (AnatomyPart anatomyPart in Anatomy.Parts)
                        foreach (AnatomyPart subpart in GetAllParts(null, anatomyPart))
                            yield return subpart;

                if (AnatomyPart != null)
                {
                    yield return AnatomyPart;
                    if (AnatomyPart.Subparts != null)
                        foreach (AnatomyPart anatomyPart in AnatomyPart.Subparts)
                            foreach (AnatomyPart subpart in GetAllParts(null, anatomyPart))
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
                    SB
                        .Append(" ".ThisManyTimes(indent * 2));

                SB
                    .AppendColored("K", "\x0007")
                    .Append(' ')
                    .Append(BodyPart.GetCardinalDescription());

                if (!BodyPart.Name.EqualsNoCase(BodyPart.Type))
                    SB
                        .Append(" (")
                        .Append(BodyPart.Type)
                        .Append(")");

                if (GameObjectFactory.Factory.GetBlueprintIfExists(BodyPart.DefaultBehaviorBlueprint) is GameObjectBlueprint defaultBehvaiour)
                    SB
                        .Append(" - ")
                        //.Append(" [")
                        .AppendColored("w", defaultBehvaiour.CachedDisplayNameStrippedLC)
                        //.Append("]")
                        ;

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
                var blueprints = Utils.GenerallyEligbleForDisplayBlueprints;

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

        private static bool WantClearChoices = false;

        private static IEnumerable<AnatomyChoice> _BaseAnatomyChoices;
        public static IEnumerable<AnatomyChoice> BaseAnatomyChoices => _BaseAnatomyChoices ??= Anatomies.AnatomyList
            ?.Where(IsEligibleAnatomy)
            ?.Select(AnatomyToChoice)
            ?.OrderBy(a => a.Anatomy.Name)
            ;

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

        public override AbstractEmbarkBuilderModuleData DefaultData => GetDefaultData();

        private List<AnatomyChoice> _AnatomyChoices;
        public List<AnatomyChoice> AnatomyChoices
        {
            get
            {
                if (_AnatomyChoices.IsNullOrEmpty()
                    || WantClearChoices)
                {
                    WantClearChoices = false;
                    _AnatomyChoices = new();
                    _AnatomyChoices.AddRange(BaseAnatomyChoices.Where(AnatomyChoiceIsValid));
                    _AnatomyChoices.RemoveAll(c => c == null || c.Anatomy == null);
                    SetDefaultChoice();
                    SelectDefaultChoice();
                }
                return _AnatomyChoices;
            }
        }

        protected static StringBuilder SB = new();

        public QudGenotypeModuleData GenotypeModuleData => builder?.GetModule<QudGenotypeModule>()?.data;
        public QudSubtypeModuleData SubtypeModuleData => builder?.GetModule<QudSubtypeModule>()?.data;

        public Qud_UD_BodyPlanModule()
        {
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
            bool isTK = GenotypeModuleData?.Entry?.IsTrueKin ?? false;
            string shortDesc = "{{C|::}}" + anatomyChoice.GetDescription() + "{{C|::}}";
            string longDesc = isTK ? anatomyChoice.LongDescriptionTKSummary : anatomyChoice.LongDescriptionSummary;
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

                    if (data.Selection.Transformation is TransformationData xForm)
                    {
                        if (!xForm.Property.IsNullOrEmpty())
                            player.SetStringProperty(xForm.Property, "true");

                        if (!xForm.Mutations.IsNullOrEmpty())
                            foreach (string mutation in xForm.Mutations)
                                if (MutationFactory.TryGetMutationEntry(mutation, out var mutationEntry))
                                    player.RequirePart<Mutations>().AddMutation(mutationEntry);

                        if (player?.Render?.Tile == defaultTile
                            && !xForm.Tile.IsNullOrEmpty())
                        {
                            if (!xForm.RenderString.IsNullOrEmpty())
                                player.Render.RenderString = xForm.RenderString;
                            player.Render.Tile = xForm.Tile;
                        }

                        if (!xForm.DetailColor.IsNullOrEmpty())
                            player.Render.DetailColor = xForm.DetailColor;

                        if (player?.GetSpecies() == defaultSpecies
                            && !xForm.Species.IsNullOrEmpty())
                            player.SetStringProperty("Species", xForm.Species);
                    }

                    if (Anatomies.GetAnatomy(anatomy).Category == BodyPartCategory.MECHANICAL
                        && !Options.EnableBodyPlansThatAreRoboticWithoutMakingYouRobotic)
                        World.ObjectBuilders.Roboticized.Roboticize(player);
                }
            }
            return base.handleBootEvent(id, game, info, element);
        }
        public override void setData(AbstractEmbarkBuilderModuleData values)
        {
            OrganizeAnatomyChoices(true);
            base.setData(values);
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
            WantClearChoices = true;
        }

        public static bool AnatomyChoiceIsValid(AnatomyChoice Choice)
            => Choice.GetDescription() != AnatomyChoice.MISSING_ANATOMY
            && Choice.Anatomy != null
            && (Choice.AnatomyExclusion == null
                || !Choice.AnatomyExclusion.IsExcluded())
            ;
        public void OrganizeAnatomyChoices(bool SelectDefaultChoice = false, bool OverrideSelection = false)
        {
            if (AnatomyChoices.IsNullOrEmpty())
                MetricsManager.LogCallingModError(nameof(AnatomyChoices) + " empty when it probably shouldn't be.");

            PlayerAnatomyChoice = null;
            if (PlayerAnatomyChoice != null
                && !IsPlayerChoice(AnatomyChoices[0]))
            {
                AnatomyChoices.OrderBy(a => a.Anatomy.Name);
                AnatomyChoices.Remove(PlayerAnatomyChoice);
                AnatomyChoices.Insert(0, PlayerAnatomyChoice);
                SetDefaultChoice();
                if (SelectDefaultChoice)
                    this.SelectDefaultChoice(OverrideSelection);
            }
        }

        public static bool IsEligibleAnatomy(Anatomy Anatomy)
            => Anatomy != null
            && (Utils.GetAnatomyExclusion(Anatomy) is not AnatomyExclusion anatomyExclusion
                || anatomyExclusion.IsOptional)
            ;
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

        public Qud_UD_BodyPlanModuleData GetDefaultData()
            => new(PlayerAnatomyChoice);

        public void SetDefaultChoice()
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
                setData(data);
            }
        }
        public bool IsPlayerChoice(AnatomyChoice Choice)
            => Choice == PlayerAnatomyChoice
            || Choice.Anatomy == PlayerAnatomyChoice.Anatomy
            ;
        public void SelectDefaultChoice(bool Override = false)
        {
            if (data != null)
                if (Override
                    || data.Selection == null
                    || data.Selection.Anatomy.IsNullOrEmpty())
                {
                    int playerIndex = AnatomyChoices.FindIndex(IsPlayerChoice);
                    if (playerIndex < 0)
                        playerIndex = 0;

                    PickAnatomy(playerIndex);
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
