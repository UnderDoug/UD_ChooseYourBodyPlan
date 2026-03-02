using XRL;

namespace UD_BodyPlan_Selection.Mod
{
    [HasOptionFlagUpdate(Prefix = "Option_UD_BodyPlan_Selection_", FieldFlags = true)]
    public static class Options
    {
        // Debug Settings
        // public static bool DebugEnableOption;

        // General Settings
        public static bool EnableBodyPlansForTK;

        public static bool EnableBodyPlansAvailableViaRecipe;

        public static bool EnableBodyPlansThatAreRobotic;
        public static bool EnableBodyPlansThatAreRoboticWithoutMakingYouRobotic;

        public static bool EnableBodyPlansThatSuck;
    }
}
