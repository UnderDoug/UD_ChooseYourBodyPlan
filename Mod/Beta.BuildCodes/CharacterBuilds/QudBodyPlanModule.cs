using XRL.CharacterBuilds;

namespace UD_ChooseYourBodyPlan.Mod.CharacterBuilds
{
    public partial class QudBodyPlanModule : QudEmbarkBuilderModule<QudBodyPlanModuleData>
    {
        public override string GetRequiredMod()
            => SelectedChoice() != DefaultBodyPlanChoice
            ? $"{Utils.ThisMod.DisplayTitle} (Anatomy: {SelectedChoice()?.Anatomy ?? BodyPlanEntry.MISSING_ANATOMY})"
            : null;
    }
}
