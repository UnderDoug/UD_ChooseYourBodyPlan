namespace UD_ChooseYourBodyPlan.Mod
{
    public static class Const
    {
        public const string MOD_ID = "UD_ChooseYourBodyPlan";
        public const string MOD_PREFIX = MOD_ID + "_";
        public const string MOD_PREFIX_SHORT = "UD_CYBP_";

        public const string GAME_MOD_EXCEPTION = "game_mod_exception";

		public const string TEXT_ELEMENTS_BLUEPRINT = MOD_PREFIX_SHORT + "BaseTextElements";
        public const string CATEGORY_BLUEPRINT = MOD_PREFIX_SHORT + "BaseAnatomyCategoryEntry";
        public const string CONFIG_BLUEPRINT = MOD_PREFIX_SHORT + "BaseConfiguration";
        public const string BODYPLAN_ENTRY_BLUEPRINT = MOD_PREFIX_SHORT + "BaseBodyPlanEntry";
        public const string XFORM_DATA_BLUEPRINT = MOD_PREFIX_SHORT + "BaseTransformationData";

        public const string REMOVE_TAG = "*remove";
        public const string NO_INHERIT_TAG = "*noinherit";

		public const string UNCHECKED = "[ ]";
		public const string CHECKED = "[■]";

		public const string TICK = "\u221A";  // √
		public const string CROSS = "\u0058"; // X

		public const char SQRE = '\u00fe'; // ■
        public const char NBSP = '\u00ff'; // non-breaking space

        public const char VERT = '\u00b3'; // vertical
        public const char HRZT = '\u00c4'; // horizontal
        public const char UANR = '\u00c0'; // up and right
        public const char VERR = '\u00c3'; // vertical and right

        public const char DMND = '\u0004'; // diamond
        public const char RTRNG = '\u0010'; // ▶

        public const char PV = '\u001a'; // penetration
        public const char INFT = '\u00ec'; // infinity

        public const char DMG = '\u0003'; // damage

        public const char AV = '\u0004'; // armor value
        public const char DV = '\t'; // dodge value
    }
}
