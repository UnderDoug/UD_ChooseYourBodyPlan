using UD_BodyPlan_Selection.Mod;

namespace XRL.CharacterBuilds.Qud
{
    public partial class Qud_UD_BodyPlanModule : QudEmbarkBuilderModule<Qud_UD_BodyPlanModuleData>
    {
        public override string GetRequiredMod()
            => SelectedChoice() != PlayerAnatomyChoice
            ? Utils.ThisMod.DisplayTitle
            : null;
    }
}
