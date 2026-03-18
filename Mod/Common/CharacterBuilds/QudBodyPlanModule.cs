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

using XRL.CharacterBuilds;
using XRL.CharacterBuilds.Qud;
using UD_ChooseYourBodyPlan.Mod.Logging;

namespace UD_ChooseYourBodyPlan.Mod.CharacterBuilds
{
    [HasOptionFlagUpdate]
    public partial class QudBodyPlanModule : QudEmbarkBuilderModule<QudBodyPlanModuleData>
    {
        public static string GetDefaultSelectionUIEvent => $"{nameof(QudBodyPlanModule)}_{nameof(GetDefaultSelectionUIEvent)}";
        public static string GetAlphabeticalChoicesUIEvent => $"{nameof(QudBodyPlanModule)}_{nameof(GetAlphabeticalChoicesUIEvent)}";
        public static string GetCategorySortedChoicesUIEvent => $"{nameof(QudBodyPlanModule)}_{nameof(GetCategorySortedChoicesUIEvent)}";

        private static bool WantClearChoices = false;

        private static IEnumerable<BodyPlanEntry> _BodyPlanEntires;
        public static IEnumerable<BodyPlanEntry> BodyPlanEntires => _BodyPlanEntires ??= BodyPlanFactory.Factory?.GetBodyPlanEntries();

        public bool HasSelection => data?.HasSelection ?? false;

        private BodyPlan _DefaultBodyPlanChoice; 
        public BodyPlan DefaultBodyPlanChoice
        {
            get
            {
                if (_DefaultBodyPlanChoice == null
                    && !_BodyPlanChoices.IsNullOrEmpty()
                    && GetDefaultBodyPlanAnatomy() is string defaultChoiceAnatomy
                    && !defaultChoiceAnatomy.IsNullOrEmpty())
                {
                    Debug.Log($"{nameof(DefaultBodyPlanChoice)} -> {defaultChoiceAnatomy}");
                    _DefaultBodyPlanChoice = _BodyPlanChoices.FirstOrDefault(a => a?.Anatomy == defaultChoiceAnatomy);
                }
                return _DefaultBodyPlanChoice;
            }
            set => _DefaultBodyPlanChoice = value;
        }

        public override AbstractEmbarkBuilderModuleData DefaultData => GetDefaultData();

        private List<BodyPlan> _BodyPlanChoices;
        public List<BodyPlan> BodyPlanChoices
        {
            get
            {
                if (_BodyPlanChoices == null
                    || WantClearChoices)
                {
                    WantClearChoices = false;
                    _BodyPlanChoices = new();

                    foreach (var bodyPlanEntry in BodyPlanEntires)
                    {
                        if (bodyPlanEntry.IsAvailable()
                            && bodyPlanEntry.TryGetBodyPlan(out BodyPlan bodyPlan)
                            && bodyPlan.IsValid())
                            _BodyPlanChoices.Add(bodyPlan);
                    }

                    _BodyPlanChoices.OrderBy(c => c?.DisplayNameStripped);

                    SetDefaultChoice();
                    SelectDefaultChoice();
                }
                return _BodyPlanChoices;
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
        private static bool LastSortByCategoryValue = Options.SortByCategory;

        public QudBodyPlanModule()
        {
            _BodyPlanChoices = null;
            _DefaultBodyPlanChoice = null;
        }

        public override bool shouldBeEditable()
            => builder.IsEditableGameMode();

        public override bool shouldBeEnabled()
            => GenotypeModuleData?.Entry is GenotypeEntry genotypeEntry
            && (!genotypeEntry.IsTrueKin
                || Options.EnableBodyPlansForTK)
            && SubtypeModuleData?.Subtype != null
            && !BodyPlanChoices.IsNullOrEmpty()
            && BodyPlanChoices.Count > 1;

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
                        .Append("{{Y|").Append(choice.DisplayName).Append(":}}").AppendLine()
                        .Append(choice.Summary)
                    ),
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
                    if (data.Selection.GetTransformation() is TransformationData xForm
                        && !xForm.Mutations.IsNullOrEmpty())
                        foreach (string mutation in xForm.Mutations)
                            if (MutationFactory.TryGetMutationEntry(mutation, out var mutationEntry))
                                player.RequirePart<Mutations>().AddMutation(mutationEntry);
                }

                if (id == QudGameBootModule.BOOTEVENT_AFTERBOOTPLAYEROBJECT)
                {
                    string defaultSpecies = SubtypeModuleData?.Entry?.Species
                        .Coalesce(GenotypeModuleData?.Entry.Species);

                    if (data.Selection.GetTransformation() is TransformationData xForm)
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
            if (data.Selection.GetTransformation() is TransformationData xForm)
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
                    if (xForm.Render.DetailColor != default)
                        return xForm?.DetailColor.ToString()
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
            if (BodyPlanChoices.IsNullOrEmpty())
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
                    $"The default for your selected genotype/subtype is {DefaultBodyPlanChoice.DisplayName}";
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

            Utils.IsTruekinEmbarking = GenotypeModuleData?.Entry?.IsTrueKin ?? false;

            Utils.EmbarkingGenoSubtypeRender ??= new BodyPlanRender();

            if (GenotypeModuleData?.Entry is GenotypeEntry genotypeEntry)
                Utils.EmbarkingGenoSubtypeRender.SetFromGenotype(genotypeEntry);

            if (SubtypeModuleData?.Entry is SubtypeEntry subtypeEntry)
                Utils.EmbarkingGenoSubtypeRender.SetFromSubtype(subtypeEntry);

            OrganizeAnatomyChoices();
        }

        [OptionFlagUpdate]
        public static void OnOptionUpdate()
        {
            if (LastSortByCategoryValue == Options.SortByCategory)
                WantClearChoices = true;
            else
                LastSortByCategoryValue = Options.SortByCategory;
        }

        public void OrganizeAnatomyChoices(bool SelectDefaultChoice = false, bool OverrideSelection = false)
        {
            if (BodyPlanChoices.IsNullOrEmpty())
            {
                MetricsManager.LogCallingModError(nameof(BodyPlanChoices) + " empty when it probably shouldn't be.");
                return;
            }

            DefaultBodyPlanChoice = null;
            if (DefaultBodyPlanChoice != null
                && !IsDefaultChoice(BodyPlanChoices[0]))
            {
                BodyPlanChoices.Remove(DefaultBodyPlanChoice);
                BodyPlanChoices.Insert(0, DefaultBodyPlanChoice);
                SetDefaultChoice();
                if (SelectDefaultChoice)
                    this.SelectDefaultChoice(OverrideSelection);
            }
        }

        public void PickAnatomy(int n)
        {
            if (data == null)
                setData(DefaultData);

            if (BodyPlanChoices.IsNullOrEmpty())
                MetricsManager.LogCallingModError(nameof(BodyPlanChoices) + " empty when it probably shouldn't be.");
            else
                data.Selection = new(BodyPlanChoices[n]);

            setData(data);
        }
        public void PickAnatomy(string Id)
        {
            if (data == null)
                setData(DefaultData);

            if (BodyPlanChoices.IsNullOrEmpty())
                MetricsManager.LogCallingModError(nameof(BodyPlanChoices) + " empty when it probably shouldn't be.");
            else
                data.Selection = new(BodyPlanChoices.Find(c => c?.Anatomy == Id));

            setData(data);
        }

        private string GetPlayerBlueprint()
            => builder?.info?.fireBootEvent(
                id: QudGameBootModule.BOOTEVENT_BOOTPLAYEROBJECTBLUEPRINT,
                game: The.Game,
                element: GenotypeModuleData?.Entry?.BodyObject
                    .Coalesce(SubtypeModuleData?.Entry?.BodyObject)
                    .Coalesce("Humanoid"));

        public string GetDefaultBodyPlanAnatomy()
            => GameObjectFactory.Factory
                ?.GetBlueprintIfExists(GetPlayerBlueprint())
                ?.GetAnatomyName();

        public QudBodyPlanModuleData GetDefaultData()
            => new(DefaultBodyPlanChoice);

        public void SetDefaultChoice()
        {
            if (BodyPlanChoices.IsNullOrEmpty())
                return;

            foreach (var bodyPlanChoice in BodyPlanChoices)
            {  
                if (bodyPlanChoice.IsDefault
                    && !IsDefaultChoice(bodyPlanChoice))
                {
                    bodyPlanChoice.IsDefault = false;
                    DefaultBodyPlanChoice = null;
                }
            }
            if (DefaultBodyPlanChoice is BodyPlan defaultChoice
                && !defaultChoice.IsDefault)
            {
                defaultChoice.IsDefault = true;
                if (data != null)
                    setData(data);
            }
        }

        public bool IsDefaultChoice(BodyPlan Choice)
            => Choice?.SameAs(DefaultBodyPlanChoice) is true
            ;

        public void SelectDefaultChoice(bool Override = false)
        {
            if (data != null)
            {
                if (Override
                    || data.Selection == null
                    || data.Selection.Anatomy.IsNullOrEmpty())
                {
                    int defaultIndex = BodyPlanChoices.FindIndex(IsDefaultChoice);
                    if (defaultIndex < 0)
                        defaultIndex = 0;

                    PickAnatomy(defaultIndex);
                }
            }
        }

        public bool IsSelected(BodyPlan Choice)
            => Choice != null
            && data != null
            && ((Choice.Anatomy == null
                    && data.Selection == null)
                || Choice.Anatomy == data.Selection?.Anatomy);

        public BodyPlan SelectedChoice()
            => BodyPlanChoices.Find(IsSelected);
    }
}
