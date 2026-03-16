using XRL.CharacterBuilds;
using XRL.Rules;
using XRL.UI.Framework;

namespace UD_ChooseYourBodyPlan.Mod.CharacterBuilds.UI
{
    public partial class QudBodyPlanModuleWindow : EmbarkBuilderModuleWindowPrefabBase<QudBodyPlanModule, CategoryMenusScroller>
    {
        public override void RandomSelectionNoUI()
            => SelectAnatomy(Stat.Roll(0, module.BodyPlanChoices.Count - 1));

        public bool SkippingUIUpdates()
            => module.builder.SkippingUIUpdates;
    }
}
