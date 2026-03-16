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

using ColorUtility = ConsoleLib.Console.ColorUtility;
using Event = XRL.World.Event;

using UD_ChooseYourBodyPlan.Mod;
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
        protected struct RandomChoice
        {
            public int CategoryIndex;
            public int ChoiceIndex;
            public PrefixMenuOption Choice;

            public RandomChoice(
                PrefixMenuOption Choice,
                int CategoryIndex,
                int ChoiceIndex
                )
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

        private List<CategoryMenuData> BodyPlanCategoryMenuOptions = new();

        private List<BodyPlanEntry> AnatomyChoices => module?.BodyPlanChoices;

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

        protected BallBag<RandomChoice> RefillRandomChoices()
        {
            RandomChoiceBag ??= new();
            RandomChoiceBag.Clear();
            if (BodyPlanCategoryMenuOptions.IsNullOrEmpty())
                return RandomChoiceBag;

            for (int i = 0; i < BodyPlanCategoryMenuOptions.Count; i++)
            {
                if (BodyPlanCategoryMenuOptions[i] is not CategoryMenuData categoryMenuOption
                    || !categoryMenuOption.menuOptions.IsNullOrEmpty())
                    continue;

                for (int j = 0; j < categoryMenuOption.menuOptions.Count; j++)
                {
                    if (categoryMenuOption.menuOptions[j] is not PrefixMenuOption menuOption
                        || BodyPlanFactory.Factory?.GetBodyPlanEntry(menuOption) is not BodyPlanEntry optionEntry
                        || optionEntry.Anatomy != null)
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
            int categoryIndex = 0;
            int choiceIndex = 0;
            PrefixMenuOption choice = BodyPlanCategoryMenuOptions?[0]?.menuOptions?[0];
            if (RefillRandomChoices().PluckOne() is RandomChoice randomChoice)
            {
                categoryIndex = randomChoice.CategoryIndex;
                choiceIndex = randomChoice.ChoiceIndex;
                choice = randomChoice.Choice;
            }

            prefabComponent
                .ContextFor(randomChoice.CategoryIndex, randomChoice.ChoiceIndex)
                .ActivateAndEnable();

            SelectAnatomy(randomChoice.Choice);

            HighlightSelected();
        }

        public override void ResetSelection()
        {
            module.setData(module.DefaultData);
            UpdateControls();
            HighlightSelected();
        }

        public override UIBreadcrumb GetBreadcrumb()
        {
            var renderable = module?.SelectedChoice()?.GetRender();
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

        private PrefixMenuOption MakeMenuOption(BodyPlanEntry Entry, StringBuilder SB, bool IsTK, out bool IsSelected)
        {
            IsSelected = false;

            if (Entry == null)
                return null;

            IsSelected = module.IsSelected(Entry);

            if (IsSelected)
                SB.AppendColored("W", Entry.GetDescription(ShowDefault: !SortByCategory, ShowSymbols: true));
            else
                SB.Append(Entry.GetDescription(ShowDefault: !SortByCategory, ShowSymbols: true));

            string longDesc = IsTK ? Entry.LongDescriptionTK : Entry.LongDescription;

            if (Entry.IsDefault)
            {
                if (module?.GenotypeModuleData?.Entry is GenotypeEntry genotypeEntry)
                    Entry.OverrideRender(new(genotypeEntry));
                if (module?.SubtypeModuleData?.Entry is SubtypeEntry subtypeEntry)
                    Entry.OverrideRender(new(subtypeEntry));
            }

            return new PrefixMenuOption
            {
                Id = Entry.Anatomy.Name,
                Prefix = IsSelected ? CHECKED : EMPTY_CHECK,
                Description = SB.ToString(),
                LongDescription = longDesc,
                Renderable = Entry.GetRender()
            };
        }

        private IEnumerable<PrefixMenuOption> GetMenuOptions(AnatomyCategoryEntry Category)
        {
            var sB = Event.NewStringBuilder();
            bool isTK = Utils.IsTruekinEmbarking;

            if (Category?.GetEntries(AnatomyChoiceIsValid) is not IEnumerable<BodyPlanEntry> choices
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

        protected CategoryMenuData MakeCategoryMenuOption(AnatomyCategoryEntry Category = null)
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
                var comparer = AnatomyCategoryEntry.DefaultFirstComparer;

                using var categories = ScopeDisposedList<AnatomyCategoryEntry>.GetFromPoolFilledWith(AnatomyCategoryEntry.Categories);
                categories.Sort(comparer);

                foreach (AnatomyCategoryEntry category in categories)
                {
                    Utils.Log(category?.GetDisplayName());
                    if (!Utils.DisableDebug)
                        foreach (var choice in category?.Entries ?? new List<BodyPlanEntry>())
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
            BodyPlanCategoryMenuOptions?.Clear();
            BodyPlanCategoryMenuOptions = new(GetCategoryMenuOptions());
            if (BodyPlanCategoryMenuOptions.IsNullOrEmpty())
            {
                BodyPlanCategoryMenuOptions.AddRange(GetCategoryMenuOptions(ForceNoCategory: true));
                MetricsManager.LogModError(Utils.ThisMod, "Failed to change sort order. Alphabetical used by default.");
            }

            // This method exists in two conditionally loaded partials:
            // PreBeta/CharacterBuilds/UI and Beta/CharacterBuilds/UI
            if (!SkippingUIUpdates())
            {
                if (OverrideHasShown)
                    prefabComponent.hasShown = false;

                prefabComponent.BeforeShow(windowDescriptor, BodyPlanCategoryMenuOptions);
            }

            if (module?.HasSelection ?? false)
                GetOverlayWindow().nextButton.navigationContext.ActivateAndEnable();
        }
    }
}
