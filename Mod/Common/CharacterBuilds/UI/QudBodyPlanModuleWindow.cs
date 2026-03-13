using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ConsoleLib.Console;

using UnityEngine;

using XRL.Collections;
using XRL.Rules;
using XRL.UI;
using XRL.UI.Framework;

using ColorUtility = ConsoleLib.Console.ColorUtility;
using Event = XRL.World.Event;

using UD_BodyPlan_Selection.Mod;
using XRL.CharacterBuilds;
using XRL;
using static UD_BodyPlan_Selection.Mod.CharacterBuilds.QudBodyPlanModule;
using System.Text;

namespace UD_BodyPlan_Selection.Mod.CharacterBuilds.UI
{
    [UIView(
        ID: "CharacterCreation:UD_PickBodyPlan",
        NavCategory: "Chargen",
        UICanvas: "Chargen/PickMutations",
        UICanvasHost: 1)]
    public partial class QudBodyPlanModuleWindow : EmbarkBuilderModuleWindowPrefabBase<QudBodyPlanModule, CategoryMenusScroller>
    {
        private struct RandomChoice
        {
            public FrameworkDataElement Category;
            public int CategoryIndex;
            public FrameworkDataElement Choice;
            public int ChoiceIndex;
            public string Id;
            public int Weight;

            public RandomChoice(
                FrameworkDataElement Category,
                int CategoryIndex,
                FrameworkDataElement Choice,
                int ChoiceIndex,
                string Id,
                int Weight
                )
            {
                this.Category = Category;
                this.CategoryIndex = CategoryIndex;
                this.Choice = Choice;
                this.ChoiceIndex = ChoiceIndex;
                this.Id = Id;
                this.Weight = Weight;
            }
        }
        private class Counter
        {
            public int Value;
        }

        protected const string COMMAND_SORT_ANATOMIES = "Cmd_UDBPS_SortAnatomies";
        protected const string EMPTY_CHECK = "[ ]";
        protected const string CHECKED = "[■]";

        // don't remove this. It's what allows the first call to UpdateControls() to actually update the controls.
        public EmbarkBuilderModuleWindowDescriptor windowDescriptor;

        private List<CategoryMenuData> AnatomiesMenuState = new();

        private List<AnatomyChoice> AnatomyChoices => module?.AnatomyChoices;

        private PrefixMenuOption Selected;

        private bool SortByCategory
        {
            get => module?.SortByCategory ?? Options.SortByCategory;
            set
            {
                if (module != null)
                    module.SortByCategory = value;
                else
                    Options.SortByCategory = value;
            }
        }

        public bool TrySetupModuleData()
        {
            if (module.data == null)
            {
                module.setData(module.DefaultData);
                return true;
            }
            if ((module?.HasSelection ?? false)
                && !AnatomyChoices.Contains(module.SelectedChoice()))
                ResetSelection();
            return false;
        }
        public override void BeforeShow(EmbarkBuilderModuleWindowDescriptor descriptor)
        {
            bool overrideHasShown = true;
            if (descriptor != null)
                windowDescriptor = descriptor;
            else
                overrideHasShown = false;

            TrySetupModuleData();

            prefabComponent.onSelected.RemoveAllListeners();
            prefabComponent.onSelected.AddListener(SelectAnatomy);

            if (!module.HasSelection)
                SelectDefaultChoice(true, false);

            UpdateControls(overrideHasShown);

            base.BeforeShow(descriptor);
        }
        public override GameObject InstantiatePrefab(GameObject prefab)
        {
            prefab.GetComponentInChildren<CategoryMenusScroller>().allowVerticalLayout = false;
            return base.InstantiatePrefab(prefab);
        }

        private static IEnumerable<RandomChoice> RandomChoiceSelector(FrameworkDataElement Category, Counter CategoryIndex)
        {
            if (Category is not IFrameworkDataList frameworkDataList
                || frameworkDataList.getChildren() is not IEnumerable<FrameworkDataElement> choiceDataElements
                || choiceDataElements.IsNullOrEmpty())
                yield break;

            int choiceIndex = 0;
            foreach (var choiceDataElement in choiceDataElements)
            {
                yield return new()
                {
                    Category = Category,
                    CategoryIndex = CategoryIndex.Value,
                    Choice = choiceDataElement,
                    ChoiceIndex = choiceIndex++,
                    Id = choiceDataElement.Id,
                    Weight = BaseAnatomyChoices
                            ?.FirstOrDefault(c => c.Anatomy.Name == choiceDataElement.Id)
                            ?.AnatomyConfigurations is not List<AnatomyConfiguration> anatomyConfigs
                        || (!anatomyConfigs.IsMechanical()
                            && !anatomyConfigs.IsDifficult())
                        ? 5
                        : 1
                };
            }
            CategoryIndex.Value++;
            yield break;
        }

        private static BallBag<RandomChoice> AggregateBallBag(BallBag<RandomChoice> Bag, RandomChoice Ball)
        {
            Bag.Add(Ball, Ball.Weight);
            return Bag;
        }

        public override void RandomSelection()
        {
            if (AnatomiesMenuState is List<CategoryMenuData> menuCategoryOptions
                && menuCategoryOptions != null)
            {
                using var randomChoices = ScopeDisposedList<RandomChoice>.GetFromPool();

                var categoryIndex = new Counter() { Value = 0 };
                foreach (var category in menuCategoryOptions)
                    if (RandomChoiceSelector(category, categoryIndex) is IEnumerable<RandomChoice> categoryChoices
                        && !categoryChoices.IsNullOrEmpty())
                        randomChoices.AddRange(categoryChoices);

                if (randomChoices.Aggregate(new BallBag<RandomChoice>(), AggregateBallBag).PluckOne() is RandomChoice randomChoice)
                {
                    prefabComponent
                        .ContextFor(randomChoice.CategoryIndex, randomChoice.ChoiceIndex)
                        .ActivateAndEnable();

                    SelectAnatomy(randomChoice.Choice);

                    HighlightSelected();
                }
            }
        }

        public override void ResetSelection()
        {
            module.setData(module.DefaultData);
            UpdateControls();
            HighlightSelected();
        }

        public override UIBreadcrumb GetBreadcrumb()
        {
            var renderable = module?.SelectedChoice()?.GetRenderable();
            return new()
            {
                Id = GetType().FullName,
                Title = module?.SelectedChoice()?.GetDescription() ?? "Body Plan",
                IconPath = renderable?.getTile() ?? "Creatures/natural-weapon-fist.bmp",
                HFlip = renderable?.HFlip ?? false,
                IconDetailColor = ColorUtility.ColorMap[renderable?.getColorChars().detail ?? 'W'],
                IconForegroundColor = ColorUtility.ColorMap[renderable?.getColorChars().foreground ?? 'w']
            };
        }

        private string GetSortMenuDescription()
        {
            var sB = Event.NewStringBuilder("Sort ");

            string alphabetical = "Alphabetically";
            string category = "By Category";

            string yes = "";
            string no = "K";

            if (SortByCategory)
                sB.AppendColored(no, alphabetical).Append("/").AppendColored(yes, category);
            else
                sB.AppendColored(yes, alphabetical).Append("/").AppendColored(no, category);

            return Event.FinalizeString(sB);
        }
        public override IEnumerable<MenuOption> GetKeyMenuBar()
        {
            yield return new MenuOption
            {
                Id = COMMAND_SORT_ANATOMIES,
                InputCommand = COMMAND_SORT_ANATOMIES,
                KeyDescription = ControlManager.getCommandInputDescription(COMMAND_SORT_ANATOMIES),
                Description = GetSortMenuDescription()
            };
            foreach (var menuOption in base.GetKeyMenuBar())
                yield return menuOption;
        }
        public override void HandleMenuOption(MenuOption menuOption)
        {
            if (menuOption.Id == COMMAND_SORT_ANATOMIES)
                SortOptions(!SortByCategory);
            else
                base.HandleMenuOption(menuOption);
        }

        public void SortOptions(bool SortByCategory)
        {
            this.SortByCategory = SortByCategory;
            module?.OrganizeAnatomyChoices();
            UpdateControls(OverrideHasShown: true);
            module.builder.RefreshActiveWindow();
        }

        public void SelectAnatomy(int n)
        {
            module.PickAnatomy(n);
            UpdateControls();
        }
        public void SelectAnatomy(string Id)
        {
            module.PickAnatomy(Id);
            UpdateControls();
        }
        public void SelectAnatomy(FrameworkDataElement dataElement)
            => SelectAnatomy(dataElement.Id)
            ;
        public void SelectDefaultChoice(bool Override = false, bool UpdateControls = true)
        {
            module.SelectDefaultChoice(Override);
            if (UpdateControls)
                this.UpdateControls();
        }

        public void HighlightSelected()
        {
            if (Selected != null)
                prefabComponent.onHighlight.Invoke(Selected);
        }

        private PrefixMenuOption MakeMenuOption(AnatomyChoice Choice, StringBuilder SB, bool IsTK, out bool IsSelected)
        {
            IsSelected = false;

            if (Choice == null)
                return null;

            IsSelected = module.IsSelected(Choice);

            if (IsSelected)
                SB.AppendColored("W", Choice.GetDescription(ShowDefault: !SortByCategory, ShowSymbols: true));
            else
                SB.Append(Choice.GetDescription(ShowDefault: !SortByCategory, ShowSymbols: true));

            string longDesc = IsTK ? Choice.LongDescriptionTK : Choice.LongDescription;

            if (Choice.IsDefault)
            {
                if (module?.GenotypeModuleData?.Entry is GenotypeEntry genotypeEntry)
                    Choice.OverrideRenderable(new(genotypeEntry));
                if (module?.SubtypeModuleData?.Entry is SubtypeEntry subtypeEntry)
                    Choice.OverrideRenderable(new(subtypeEntry));
            }

            return new PrefixMenuOption
            {
                Id = Choice.Anatomy.Name,
                Prefix = IsSelected ? CHECKED : EMPTY_CHECK,
                Description = SB.ToString(),
                LongDescription = longDesc,
                Renderable = Choice.GetRenderable()
            };
        }

        private IEnumerable<PrefixMenuOption> GetMenuOptions(AnatomyCategory Category)
        {
            var sB = Event.NewStringBuilder();
            bool isTK = module?.GenotypeModuleData?.Entry?.IsTrueKin ?? false;

            if (Category?.GetChoices(AnatomyChoiceIsValid) is not IEnumerable<AnatomyChoice> choices
                || (Category != null
                    && choices.IsNullOrEmpty()))
            {
                choices = AnatomyChoices;
            }

            foreach (var choice in choices)
            {
                if (MakeMenuOption(choice, sB, isTK, out bool isSelected) is PrefixMenuOption menuOption)
                {
                    if (isSelected)
                        Selected = menuOption;

                    sB.Clear();

                    yield return menuOption;
                }
            }

            Event.ResetTo(sB);
            yield break;
        }

        protected CategoryMenuData MakeCategoryMenuOption(AnatomyCategory Category = null)
            => new()
            {
                Title = Category != null ? (Category?.GetDisplayName() ?? "MISSING_DISPLAY_NAME"): "Body Plans",
                menuOptions = new(GetMenuOptions(Category))
            };

        public virtual IEnumerable<CategoryMenuData> GetCategoryMenuOptions(bool ForceNoCategory = false)
        {
            if (!SortByCategory || ForceNoCategory)
            {
                yield return MakeCategoryMenuOption();
            }
            else
            {
                var comparer = AnatomyCategory.DefaultFirstComparer;

                using var categories = ScopeDisposedList<AnatomyCategory>.GetFromPoolFilledWith(AnatomyCategory.Categories);
                categories.Sort(comparer);

                foreach (AnatomyCategory category in categories)
                {
                    Utils.Log(category?.GetDisplayName());
                    if (!Utils.DisableDebug)
                        foreach (var choice in category?.Choices ?? new List<AnatomyChoice>())
                            Utils.Log($"    {choice?.Anatomy?.Name}");

                    if (category.IsValid(c => AnatomyChoices.Any(ch => ch?.Anatomy?.Name == c?.Anatomy?.Name)))
                    {
                        yield return MakeCategoryMenuOption(category);
                    }
                }
            }
        }

        public void UpdateControls(bool OverrideHasShown = false)
        {
            AnatomiesMenuState?.Clear();
            AnatomiesMenuState = new(GetCategoryMenuOptions());
            if (AnatomiesMenuState.IsNullOrEmpty())
            {
                AnatomiesMenuState.AddRange(GetCategoryMenuOptions(ForceNoCategory: true));
                /*
                AnatomiesMenuState.Add(
                    item: new CategoryMenuData
                    {
                        Title = "Body Plan",
                        menuOptions = new() { Selected }
                    });
                */
                MetricsManager.LogModError(Utils.ThisMod, "Failed to change sort order. Alphabetical used by default.");
            }

            // This method exists in two conditionally loaded partials:
            // PreBeta/CharacterBuilds/UI and Beta/CharacterBuilds/UI
            if (!SkippingUIUpdates())
            {
                if (OverrideHasShown)
                    prefabComponent.hasShown = false;

                prefabComponent.BeforeShow(windowDescriptor, AnatomiesMenuState);
            }

            if (module?.HasSelection ?? false)
                GetOverlayWindow().nextButton.navigationContext.ActivateAndEnable();
        }
    }
}
