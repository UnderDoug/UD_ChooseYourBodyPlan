using System.Collections.Generic;

using ConsoleLib.Console;

using UnityEngine;

using XRL.Collections;
using XRL.Rules;
using XRL.UI;
using XRL.UI.Framework;

using ColorUtility = ConsoleLib.Console.ColorUtility;

using UD_BodyPlan_Selection.Mod;
using Options = UD_BodyPlan_Selection.Mod.Options;
using System.Linq;

namespace XRL.CharacterBuilds.Qud.UI
{
    [UIView(
        ID: "CharacterCreation:UD_PickBodyPlan",
        NavCategory: "Chargen",
        UICanvas: "Chargen/PickCybernetics",
        UICanvasHost: 1)]
    public partial class Qud_UD_BodyPlanModuleWindow : EmbarkBuilderModuleWindowPrefabBase<Qud_UD_BodyPlanModule, CategoryMenusScroller>
    {
        private struct RandomChoice
        {
            public int Index;
            public string Anatomy;
            public int Weight;
        }

        protected const string EMPTY_CHECK = "[ ]";

        protected const string CHECKED = "[■]";

        // don't remove this. It's what allows the first call to UpdateControls() to actually update the controls.
        public EmbarkBuilderModuleWindowDescriptor windowDescriptor;

        private List<CategoryMenuData> AnatomiesMenuState = new();

        private List<Qud_UD_BodyPlanModule.AnatomyChoice> AnatomyChoices => module?.AnatomyChoices;

        private PrefixMenuOption Selected;

        public bool TrySetupModuleData()
        {
            if (module.data == null)
            {
                module.setData(module.DefaultData);
                return true;
            }
            if ((module?.HasSelection ?? false)
                && !module.AnatomyChoices.Contains(module.SelectedChoice()))
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

        public override void RandomSelection()
        {
            using var randomChoices = ScopeDisposedList<RandomChoice>.GetFromPool();
            int choiceIndex = 0;
            foreach (var choice in AnatomyChoices)
                randomChoices.Add(
                    Item: new()
                    {
                        Index = choiceIndex++,
                        Anatomy = choice.Anatomy.Name,
                        Weight = choice.AnatomyConfigurations.IsNullOrEmpty()
                                || (!choice.AnatomyConfigurations.IsDifficult()
                                    && !choice.AnatomyConfigurations.IsMechanical())
                            ? 5
                            : 1
                    });

            int num = randomChoices.Aggregate(new BallBag<int>(), (a, n) => a.AddA(n.Index, n.Weight)).PluckOne();

            prefabComponent.ContextFor(0, num).ActivateAndEnable();
            SelectAnatomy(num);
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
            Renderable renderable = module?.SelectedChoice()?.GetRenderable();

            return new()
            {
                Id = GetType().FullName,
                Title = module?.SelectedChoice()?.GetDescription() ?? "Body Plan",
                IconPath = renderable?.getTile() ?? "Creatures/natural-weapon-fist.bmp",
                IconDetailColor = ColorUtility.ColorMap[renderable?.getColorChars().detail ?? 'W'],
                IconForegroundColor = ColorUtility.ColorMap[renderable?.getColorChars().foreground ?? 'w']
            };
        }

        public void SelectAnatomy(int n)
        {
            module.PickAnatomy(n);
            UpdateControls();
        }
        public void SelectAnatomy(FrameworkDataElement dataElement)
            => SelectAnatomy(AnatomiesMenuState[0].menuOptions.FindIndex(d => d == dataElement))
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

        public void UpdateControls(bool OverrideHasShown = false)
        {
            AnatomiesMenuState = new();
            var categoryMenuData = new CategoryMenuData
            {
                Title = "Body Plans",
                menuOptions = new()
            };
            AnatomiesMenuState.Add(categoryMenuData);
            if (AnatomyChoices != null)
            {
                using var choicesToDelete = ScopeDisposedList<int>.GetFromPool();
                bool isTK = module?.GenotypeModuleData?.Entry?.IsTrueKin ?? false;
                var sB = World.Event.NewStringBuilder();
                for (int i = 0; i < AnatomyChoices.Count; i++)
                {
                    if (AnatomyChoices[i] is not Qud_UD_BodyPlanModule.AnatomyChoice choice)
                    {
                        choicesToDelete.Add(i);
                        continue;
                    }

                    bool isSelected = module.IsSelected(choice);

                    if (isSelected)
                        sB.AppendColored("W", choice.GetDescription(ShowDefault: true, ShowSymbols: true));
                    else
                        sB.Append(choice.GetDescription(ShowDefault: true, ShowSymbols: true));

                    string longDesc = isTK ? choice.LongDescriptionTK : choice.LongDescription;

                    if (choice.IsDefault)
                    {
                        if (module.GenotypeModuleData.Entry is GenotypeEntry genotypeEntry)
                            choice.OverrideRenderable(new(genotypeEntry));
                        if (module.SubtypeModuleData.Entry is SubtypeEntry subtypeEntry)
                            choice.OverrideRenderable(new(subtypeEntry));
                    }

                    var renderable = choice.GetRenderable();

                    var menuOption = new PrefixMenuOption()
                    {
                        Prefix = isSelected ? CHECKED : EMPTY_CHECK,
                        Description = sB.ToString(),
                        LongDescription = longDesc,
                        Renderable = renderable
                    };
                    categoryMenuData.menuOptions.Add(
                        item: menuOption);

                    if (isSelected)
                        Selected = menuOption;

                    sB.Clear();
                }
                foreach (int index in choicesToDelete)
                    AnatomyChoices.RemoveAt(index);

                World.Event.ResetTo(sB);
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
