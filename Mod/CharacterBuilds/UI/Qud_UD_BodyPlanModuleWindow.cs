using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ConsoleLib.Console;

using UnityEngine;

using XRL.Rules;
using XRL.UI;
using XRL.UI.Framework;

using UD_BodyPlan_Selection.Mod;

using ColorUtility = ConsoleLib.Console.ColorUtility;
using XRL.Collections;

namespace XRL.CharacterBuilds.Qud.UI
{
    [UIView(
        ID: "CharacterCreation:UD_PickBodyPlan",
        NavCategory: "Chargen",
        UICanvas: "Chargen/PickCybernetics",
        UICanvasHost: 1)]
    public class Qud_UD_BodyPlanModuleWindow : EmbarkBuilderModuleWindowPrefabBase<Qud_UD_BodyPlanModule, CategoryMenusScroller>
    {
        protected const string EMPTY_CHECK = "[ ]";

        protected const string CHECKED = "[■]";

        private List<CategoryMenuData> AnatomiesMenuState = new();

        private List<Qud_UD_BodyPlanModule.AnatomyChoice> AnatomyChoices => module?.AnatomyChoices;

        public bool TrySetupModuleData()
        {
            if (module.data == null)
            {
                module.setData(module.DefaultData);
                module.OrganizeAnatomyChoices(true);
                return true;
            }
            return false;
        }
        public override void BeforeShow(EmbarkBuilderModuleWindowDescriptor descriptor)
        {
            module.WindowShown = true;

            TrySetupModuleData();

            prefabComponent.onSelected.RemoveAllListeners();
            prefabComponent.onSelected.AddListener(SelectAnatomy);

            UpdateControls();

            base.BeforeShow(descriptor);
        }
        public override void Show()
        {
            base.Show();
            TrySetupModuleData();

            if (!module.HasSelection)
                module.SetDefautChoice(true);

            UpdateControls();
        }

        public override GameObject InstantiatePrefab(GameObject prefab)
        {
            prefab.GetComponentInChildren<CategoryMenusScroller>().allowVerticalLayout = false;
            return base.InstantiatePrefab(prefab);
        }

        public override void RandomSelectionNoUI()
            => SelectAnatomy(Stat.Roll(0, module.AnatomyChoices.Count - 1));

        public override void RandomSelection()
        {
            int num = Stat.Roll(0, module.AnatomyChoices.Count - 1);
            prefabComponent.ContextFor(0, num).ActivateAndEnable();
            module.PickAnatomy(num);
            UpdateControls();
        }

        public override void ResetSelection()
        {
            module.setData(module.DefaultData);
            UpdateControls();
        }

        public override UIBreadcrumb GetBreadcrumb()
        {
            Renderable renderable = (module?.SelectedChoice())?.GetRenderable();
            return new()
            {
                Id = GetType().FullName,
                Title = (module?.SelectedChoice())?.GetDescription() ?? "Body Plan",
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

        public void UpdateControls()
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
                for (int i = 0; i < AnatomyChoices.Count; i++)
                {
                    if (AnatomyChoices[i] is not Qud_UD_BodyPlanModule.AnatomyChoice choice)
                    {
                        choicesToDelete.Add(i);
                        continue;
                    }

                    bool isSelected = module.IsSelected(choice);
                    string description = choice.GetDescription(ShowDefault: true);
                    if (isSelected)
                        description = "{{W|" + description + "}}";

                    categoryMenuData.menuOptions.Add(
                        item: new PrefixMenuOption()
                        {
                            Prefix = isSelected ? CHECKED : EMPTY_CHECK,
                            Description = description,
                            LongDescription = choice.GetLongDescription(
                                IncludeOpening: true,
                                IsTrueKin: module?.GenotypeModuleData?.Entry?.IsTrueKin ?? false),
                            Renderable = choice.GetRenderable()
                        });
                }
                foreach (int index in choicesToDelete)
                    AnatomyChoices.RemoveAt(index);
            }

            if (!module.builder.SkippingUIUpdates)
                prefabComponent.BeforeShow(descriptor, AnatomiesMenuState);
        }
    }
}
