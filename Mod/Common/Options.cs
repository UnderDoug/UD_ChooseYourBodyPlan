using XRL;

namespace UD_ChooseYourBodyPlan.Mod
{
    [HasModSensitiveStaticCache]
    [HasOptionFlagUpdate(Prefix = Const.MOD_PREFIX)]
    public static class Options
    {
        // Debug Settings
        [OptionFlag] public static bool? DebugEnableLogging;

        // General Settings
        [OptionFlag] public static bool EnableSortByCategory;

        [OptionFlag] public static bool EnableBodyPlansForTK;

        [OptionFlag] public static bool EnableBodyPlansAvailableViaRecipe;

        [OptionFlag] public static bool EnableBodyPlansThatAreRobotic;
        [OptionFlag] public static bool EnableRoboticBodyPlansMakingYouRobotic;

        public static bool SortByCategory
        {
            get => EnableSortByCategory;
            set => EnableSortByCategory = value;
        }
    }
}
