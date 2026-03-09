using XRL;

namespace UD_BodyPlan_Selection.Mod
{
    [HasModSensitiveStaticCache]
    [HasOptionFlagUpdate(Prefix = "Option_UD_BodyPlan_Selection_")]
    public static class Options
    {
        // Debug Settings
        // public static bool DebugEnableOption;

        // General Settings
        [OptionFlag] public static bool EnableBodyPlansForTK;

        [OptionFlag] public static bool EnableBodyPlansAvailableViaRecipe;

        [OptionFlag] public static bool EnableBodyPlansThatAreRobotic;
        [OptionFlag] public static bool EnableRoboticBodyPlansMakingYouRobotic;

        public static bool SortByCategory = true;

        [ModSensitiveCacheInit]
        public static void SetSortByCategory()
        {
            SortByCategory = true;
        }
    }
}
