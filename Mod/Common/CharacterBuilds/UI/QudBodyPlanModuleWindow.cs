using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ConsoleLib.Console;

using UnityEngine;

using XRL;
using XRL.CharacterBuilds;
using XRL.Collections;
using XRL.Rules;
using XRL.UI;
using XRL.UI.Framework;
using XRL.World;

using ColorUtility = ConsoleLib.Console.ColorUtility;
using Event = XRL.World.Event;
using UnityGameObject = UnityEngine.GameObject;

using UD_ChooseYourBodyPlan.Mod.Logging;

using Debug = UD_ChooseYourBodyPlan.Mod.Logging.Debug;
using static UD_ChooseYourBodyPlan.Mod.CharacterBuilds.QudBodyPlanModule;

namespace UD_ChooseYourBodyPlan.Mod.CharacterBuilds.UI
{
    [UIView(
        ID: "CharacterCreation:UD_PickBodyPlan",
        NavCategory: "Chargen",
        UICanvas: "Chargen/PickMutations",
        UICanvasHost: 1)]
    public partial class QudBodyPlanModuleWindow : EmbarkBuilderModuleWindowPrefabBase<QudBodyPlanModule, CategoryMenusScroller>
    {
        protected class RandomChoice
        {
            public int CategoryIndex;
            public int ChoiceIndex;
            public PrefixMenuOption Choice;

            public RandomChoice()
            {
                CategoryIndex = 0;
                ChoiceIndex = 0;
                Choice = null;
            }
            public RandomChoice(
                PrefixMenuOption Choice,
                int CategoryIndex,
                int ChoiceIndex
                )
                : this()
            {
                this.CategoryIndex = CategoryIndex;
                this.Choice = Choice;
                this.ChoiceIndex = ChoiceIndex;
            }
        }

        protected const string COMMAND_SORT_ANATOMIES = "Cmd_UDBPS_SortAnatomies";
        protected const string EMPTY_CHECK = Const.UNCHECKED;
        protected const string CHECKED = Const.CHECKED;

        protected BallBag<RandomChoice> RandomChoiceBag;

        // don't remove this. It's what allows the first call to UpdateControls() to actually update the controls.
        public EmbarkBuilderModuleWindowDescriptor windowDescriptor;

        private List<AnatomyCategoryMenuData> BodyPlanMenuOptions = new();

        private List<BodyPlan> BodyPlanChoices => module?.BodyPlanChoices;

        private BodyPlanMenuOption Selected;

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
                && !BodyPlanChoices.Contains(module.SelectedChoice()))
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
        public override UnityGameObject InstantiatePrefab(UnityGameObject prefab)
        {
            prefab.GetComponentInChildren<CategoryMenusScroller>().allowVerticalLayout = false;
            return base.InstantiatePrefab(prefab);
        }

        protected BallBag<RandomChoice> RefillRandomChoices()
        {
            RandomChoiceBag ??= new();
            RandomChoiceBag.Clear();
            if (BodyPlanMenuOptions.IsNullOrEmpty())
            {
                RandomChoiceBag.Add(new(null, 0, 0), 1);
                return RandomChoiceBag;
            }

            for (int i = 0; i < BodyPlanMenuOptions.Count; i++)
            {
                if (BodyPlanMenuOptions[i] is not AnatomyCategoryMenuData categoryMenuOption
                    || !categoryMenuOption.menuOptions.IsNullOrEmpty())
                    continue;

                for (int j = 0; j < categoryMenuOption.menuOptions.Count; j++)
                {
                    if (categoryMenuOption.menuOptions[j] is not BodyPlanMenuOption menuOption
                        || BodyPlanFactory.Factory?.GetBodyPlanEntry(menuOption) is not BodyPlanEntry optionEntry
                        || optionEntry.Anatomy == null)
                        continue;

                    RandomChoiceBag.Add(
                        Item: new RandomChoice
                        {
                            Choice = menuOption,
                            CategoryIndex = i,
                            ChoiceIndex = j,
                        },
                        Weight: optionEntry.RandomWeight);
                }
            }
            return RandomChoiceBag;
        }

        public override void RandomSelection()
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent);

            int categoryIndex = 0;
            int choiceIndex = 0;
            PrefixMenuOption choice = null;
            if (RefillRandomChoices().PluckOne() is RandomChoice randomChoice)
            {
                categoryIndex = randomChoice.CategoryIndex;
                choiceIndex = randomChoice.ChoiceIndex;
                choice = randomChoice.Choice;
            }

            if (choice != null)
            {
                prefabComponent
                    .ContextFor(categoryIndex, choiceIndex)
                    .ActivateAndEnable();

                SelectAnatomy(choice);

                HighlightSelected();
            }
            else
                Utils.Warn($"Failed attemped {nameof(RandomSelection)}: {nameof(choice)} was null.");
        }

        public override void ResetSelection()
        {
            module.setData(module.DefaultData);
            UpdateControls();
            HighlightSelected();
        }

        public override UIBreadcrumb GetBreadcrumb()
        {
            var renderable = module?.SelectedChoice()?.Render;
            return new()
            {
                Id = GetType().FullName,
                Title = module?.SelectedChoice()?.DisplayName ?? "Body Plan",
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
            => SelectAnatomy(dataElement?.Id)
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

        private IEnumerable<BodyPlanMenuOption> GetMenuOptions(AnatomyCategory Category = null)
        {
            using Indent indent = new(1);
            Debug.Log($"{nameof(GetMenuOptions)}({Category?.DisplayName ?? "One Category"})", Indent: indent[0]);

            if (Category?.GetBodyPlans(module?.DefaultBodyPlanChoice) is not IEnumerable<BodyPlan> choices
                || (Category != null
                    && choices.IsNullOrEmpty()))
                choices = BodyPlanChoices;

            if (choices.IsNullOrEmpty())
            {
                yield return new()
                {
                    ID = "BROKEN",
                    IsSelected = true,
                    Name = "NO BODY PLANS",
                    Details = $"Something has gone wrong and there are no body plans to select from.\n\n" +
                        $"It shouldn't be possible to see this description, so if you're seeing it, " +
                        $"please contact the mod author, {Utils.ThisMod.Manifest.Author}, either on the steam workshop page, " +
                        $"or on the Caves of Qud discord.",
                    Render = new(GameObjectFactory.Factory?.GetBlueprintIfExists("Fool of the Gyre")?.GetRenderable(), true),
                };
                yield break;
            }

            string defaultChoice = module?.DefaultBodyPlanChoice?.Anatomy;
            foreach (var choice in choices)
            {
                if (defaultChoice != null
                    && choice.SetDefault(defaultChoice))
                    defaultChoice = null;

                if (choice.GetMenuOption(module?.SelectedChoice()) is BodyPlanMenuOption menuOption)
                {
                    Debug.Log(menuOption?.Id ?? "NO_OPTION_ID", Indent: indent[1]);
                    yield return menuOption;
                }
            }
        }

        protected AnatomyCategoryMenuData MakeCategoryMenuOption(AnatomyCategory Category = null)
            => Category == null
            ? new AnatomyCategoryMenuData
                {
                    DisplayName = "Body Plans",
                    MenuOptions = new(GetMenuOptions()),
                }
            : Category.GetMenuData(module?.SelectedChoice())
            ;

        public virtual IEnumerable<AnatomyCategoryMenuData> GetCategoryMenuOptions(bool ForceNoCategory = false)
        {
            using Indent indent = new(1);
            if (!SortByCategory
                || ForceNoCategory)
                yield return MakeCategoryMenuOption();
            else
            {
                var comparer = AnatomyCategoryEntry.DefaultFirstComparer;

                using var categoryEntries = ScopeDisposedList<AnatomyCategoryEntry>.GetFromPoolFilledWith(AnatomyCategoryEntry.Categories);
                categoryEntries.Sort(comparer);
                foreach (AnatomyCategoryEntry categoryEntry in categoryEntries)
                {
                    Debug.Log(categoryEntry?.GetDisplayName(), Indent: indent[0]);
                    foreach (var choice in categoryEntry?.Entries ?? Enumerable.Empty<BodyPlanEntry>())
                    {
                        Debug.Log(choice?.Anatomy?.Name ?? "NO_ANATOMY_NAME", Indent: indent[1]);
                        _ = indent[0];
                    }

                    if (categoryEntry.IsValidWithAnyAvailable()
                        && categoryEntry.GetAnatomyCategory() is AnatomyCategory category)
                    {
                        yield return MakeCategoryMenuOption(category);
                        _ = indent[0];
                    };
                }
            }
        }

        public BodyPlanMenuOption FindSelected()
        {
            foreach (var categoryMenuOption in BodyPlanMenuOptions ?? Enumerable.Empty<AnatomyCategoryMenuData>())
                foreach (var bodyPlanMenuOption in categoryMenuOption.menuOptions ?? Enumerable.Empty<PrefixMenuOption>())
                    if (bodyPlanMenuOption.Id == module?.SelectedChoice()?.Anatomy)
                        return bodyPlanMenuOption as BodyPlanMenuOption;

            return null;
        }

        public void UpdateControls(bool OverrideHasShown = false)
        {
            BodyPlanMenuOptions ??= new();
            BodyPlanMenuOptions?.Clear();

            BodyPlanMenuOptions.AddRange(GetCategoryMenuOptions());

            if (BodyPlanMenuOptions.IsNullOrEmpty())
            {
                BodyPlanMenuOptions.AddRange(GetCategoryMenuOptions(ForceNoCategory: true));
                MetricsManager.LogModError(Utils.ThisMod, "Failed to change sort order. Alphabetical used by default.");
            }
            Selected = FindSelected();

            // This method exists in two conditionally loaded partials:
            // PreBeta/CharacterBuilds/UI and Beta/CharacterBuilds/UI
            if (!SkippingUIUpdates())
            {
                if (OverrideHasShown)
                    prefabComponent.hasShown = false;

                prefabComponent.BeforeShow(windowDescriptor, BodyPlanMenuOptions);
            }

            if (module?.HasSelection ?? false)
                GetOverlayWindow().nextButton.navigationContext.ActivateAndEnable();
        }
    }
}
