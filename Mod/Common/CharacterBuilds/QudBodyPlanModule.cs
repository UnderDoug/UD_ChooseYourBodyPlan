using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ConsoleLib.Console;

using XRL;
using XRL.UI.Framework;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;

using Event = XRL.World.Event;

using UD_BodyPlan_Selection.Mod;
using static UD_BodyPlan_Selection.Mod.AnatomyConfiguration;
using XRL.CharacterBuilds;
using XRL.CharacterBuilds.Qud;

namespace UD_BodyPlan_Selection.Mod.CharacterBuilds
{
    [HasOptionFlagUpdate]
    public partial class QudBodyPlanModule : QudEmbarkBuilderModule<QudBodyPlanModuleData>
    {
        public static string GetDefaultSelectionUIEvent => $"{nameof(QudBodyPlanModule)}_{nameof(GetDefaultSelectionUIEvent)}";

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
                    && !_AnatomyChoices.IsNullOrEmpty())
                {
                    var uIEvent = builder?.handleUIEvent(GetDefaultSelectionUIEvent, _AnatomyChoices);
                    string choiceName = GetDefaultPlayerBodyPlan();
                    List<AnatomyChoice> anatomyChoices = _AnatomyChoices;
                    if (uIEvent is List<AnatomyChoice> uIEventAnatomyChoices)
                        anatomyChoices = uIEventAnatomyChoices;
                    else
                    if (uIEvent is string uIEventChoiceName)
                        choiceName = uIEventChoiceName;
                    else
                    if (uIEvent is AnatomyChoice uIEventChoice)
                        _PlayerAnatomyChoice = uIEventChoice;

                    _PlayerAnatomyChoice ??= anatomyChoices.FirstOrDefault(a => a?.Anatomy?.Name == choiceName);
                }
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
                    if (builder?.handleUIEvent(GetDefaultSelectionUIEvent, _AnatomyChoices) is List<AnatomyChoice> uIEventChoices)
                        _AnatomyChoices = uIEventChoices;
                    _AnatomyChoices.RemoveAll(c => c?.Anatomy == null);
                    SetDefaultChoice();
                    SelectDefaultChoice();
                    Utils.AnatomyChoices = new(AnatomyChoices);
                }
                return _AnatomyChoices;
            }
        }

        protected static StringBuilder SB = new();

        public QudGenotypeModuleData GenotypeModuleData => builder?.GetModule<QudGenotypeModule>()?.data;
        public QudSubtypeModuleData SubtypeModuleData => builder?.GetModule<QudSubtypeModule>()?.data;

        public bool SortByCategory
        {
            get => Options.SortByCategory;
            set => Options.SortByCategory = value;
        }

        public QudBodyPlanModule()
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

        private static bool IsQudMutationsModuleWindowDescriptor(EmbarkBuilderModuleWindowDescriptor Descriptor)
            => Descriptor.module is QudMutationsModule
            ;
        public override void assembleWindowDescriptors(List<EmbarkBuilderModuleWindowDescriptor> windows)
        {
            int index = windows.FindIndex(IsQudMutationsModuleWindowDescriptor);
            if (index < 0)
                base.assembleWindowDescriptors(windows);
            else
                windows.InsertRange(index + 1, this.windows.Values);
        }

        public override SummaryBlockData GetSummaryBlock()
        {
            var choice = SelectedChoice();
            return new()
            {
                Id = GetType().FullName,
                Title = "Body Plan",
                Description = Event.FinalizeString(
                    SB: Event.NewStringBuilder()
                        .Append("{{Y|").Append(choice.GetDescription()).Append(":}}").AppendLine()
                        .Append(GenotypeModuleData?.Entry?.IsTrueKin ?? false
                            ? choice.LongDescriptionNoOpenTKSummary
                            : choice.LongDescriptionNoOpenSummary)),
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
            if (shouldBeEnabled()
                && (data == null
                    || data.Selection == null))
            {
                MetricsManager.LogWarning("Body Plan module was active but data or selections was null or empty.");
                return base.handleBootEvent(id, game, info, element);
            }

            if (element is GameObject player
                && player.Body is Body playerBody
                && data.Selection.Anatomy is string anatomy)
            {
                if (id == QudGameBootModule.BOOTEVENT_BOOTPLAYEROBJECT)
                {
                    playerBody.Rebuild(data.Selection.Anatomy);
                    if (data.Selection.Transformation is TransformationData xForm
                        && !xForm.Mutations.IsNullOrEmpty())
                        foreach (string mutation in xForm.Mutations)
                            if (MutationFactory.TryGetMutationEntry(mutation, out var mutationEntry))
                                player.RequirePart<Mutations>().AddMutation(mutationEntry);
                }

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

                        if (player?.GetSpecies() == defaultSpecies
                            && !xForm.Species.IsNullOrEmpty())
                            player.SetStringProperty("Species", xForm.Species);
                    }

                    if (Anatomies.GetAnatomy(anatomy).Category == BodyPartCategory.MECHANICAL
                        && Options.EnableRoboticBodyPlansMakingYouRobotic)
                        XRL.World.ObjectBuilders.Roboticized.Roboticize(player);
                }
            }
            else
            if (data.Selection.Transformation is TransformationData xForm)
            {
                string stringElement = element as string;
                if (id == QudGameBootModule.BOOTEVENT_BOOTPLAYERTILE)
                {
                    string defaultTile = SubtypeModuleData?.Entry?.Tile
                        .Coalesce(GenotypeModuleData?.Entry.Tile);

                    if (stringElement == defaultTile
                        && !xForm.Tile.IsNullOrEmpty())
                        return xForm.Tile
                            ?? stringElement;
                }
                if (id == QudGameBootModule.BOOTEVENT_BOOTPLAYERTILEFOREGROUND)
                    if (!xForm.TileColor.IsNullOrEmpty())
                    {
                        if (stringElement.IsNullOrEmpty())
                            return xForm.TileColor.Replace("&", "")
                                ?? stringElement;
                        else
                            return xForm.TileColor
                                ?? stringElement;
                    }

                if (id == QudGameBootModule.BOOTEVENT_BOOTPLAYERTILEDETAIL)
                    if (!xForm.DetailColor.IsNullOrEmpty())
                        return xForm.DetailColor
                            ?? stringElement;
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
            && (Choice?.AnatomyConfigurations?.AllowSelection() ?? true)
            ;
        public void OrganizeAnatomyChoices(bool SelectDefaultChoice = false, bool OverrideSelection = false)
        {
            if (AnatomyChoices.IsNullOrEmpty())
            {
                MetricsManager.LogCallingModError(nameof(AnatomyChoices) + " empty when it probably shouldn't be.");
                return;
            }

            PlayerAnatomyChoice = null;
            if (PlayerAnatomyChoice != null
                && !IsPlayerChoice(AnatomyChoices[0]))
            {
                AnatomyChoices.OrderBy(c => c?.DisplayNameStripped);
                AnatomyChoices.Remove(PlayerAnatomyChoice);
                AnatomyChoices.Insert(0, PlayerAnatomyChoice);
                SetDefaultChoice();
                if (SelectDefaultChoice)
                    this.SelectDefaultChoice(OverrideSelection);
            }
        }

        public static bool IsEligibleAnatomy(Anatomy Anatomy)
            => Anatomy != null
            && (Utils.GetAnatomyConfigurations(Anatomy) is not AnatomyConfiguration anatomyConfiguration
                || anatomyConfiguration.IsOptional)
            ;
        public static AnatomyChoice AnatomyToChoice(Anatomy Anatomy)
            => new(Anatomy)
            ;
        public void PickAnatomy(int n)
        {
            if (data == null)
                setData(DefaultData);

            if (AnatomyChoices.IsNullOrEmpty())
                MetricsManager.LogCallingModError(nameof(AnatomyChoices) + " empty when it probably shouldn't be.");
            else
                data.Selection = new(AnatomyChoices[n]);

            setData(data);
        }
        public void PickAnatomy(string Id)
        {
            if (data == null)
                setData(DefaultData);

            if (AnatomyChoices.IsNullOrEmpty())
                MetricsManager.LogCallingModError(nameof(AnatomyChoices) + " empty when it probably shouldn't be.");
            else
                data.Selection = new(AnatomyChoices.Find(c => c?.Anatomy?.Name == Id));

            setData(data);
        }

        private string GetPlayerBlueprint()
            => builder?.info?.fireBootEvent(
                id: QudGameBootModule.BOOTEVENT_BOOTPLAYEROBJECTBLUEPRINT,
                game: The.Game,
                element: GenotypeModuleData?.Entry?.BodyObject
                    .Coalesce(SubtypeModuleData?.Entry?.BodyObject)
                    .Coalesce("Humanoid"));

        public string GetDefaultPlayerBodyPlan()
            => GameObjectFactory.Factory
                ?.GetBlueprintIfExists(GetPlayerBlueprint())
                ?.GetPartParameter<string>(nameof(Body), nameof(Body.Anatomy));

        public QudBodyPlanModuleData GetDefaultData()
            => new(PlayerAnatomyChoice);

        public void SetDefaultChoice()
        {
            if (AnatomyChoices.IsNullOrEmpty())
                return;

            if (AnatomyChoices.FirstOrDefault(c => c?.IsDefault ?? false) is AnatomyChoice defaultChoice
                && defaultChoice != PlayerAnatomyChoice)
            {
                defaultChoice.IsDefault = false;
                PlayerAnatomyChoice = null;
            }
            if (PlayerAnatomyChoice is AnatomyChoice playerChoice
                && !playerChoice.IsDefault)
            {
                playerChoice.IsDefault = true;
                if (GenotypeModuleData?.Entry is GenotypeEntry genotypeEntry
                    && SubtypeModuleData?.Entry is SubtypeEntry subtypeEntry
                    && subtypeEntry.Tile.Coalesce(genotypeEntry.Tile) is string typeTile
                    && subtypeEntry.DetailColor.Coalesce(genotypeEntry.DetailColor) is string typeDetailColor)
                    playerChoice.OverrideRenderable(
                        Renderable: new(
                            Tile: typeTile,
                            RenderString: "@",
                            ColorString: $"&Y^{typeDetailColor}",
                            TileColor: "&Y",
                            DetailColor: typeDetailColor[0],
                            HFlip: true));

                if (data != null)
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
