using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ConsoleLib.Console;

using XRL;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;

using static XRL.CharacterBuilds.Qud.Qud_UD_BodyPlanModule;

namespace UD_BodyPlan_Selection.Mod
{
    [HasModSensitiveStaticCache]
    public static partial class Utils
    {
        public static string THIS_MOD_ID => "UD_BodyPlan_Selection";

        public static ModInfo ThisMod => ModManager.GetMod(THIS_MOD_ID);

        #region Blueprints For Display

        [ModSensitiveStaticCache]
        private static IEnumerable<GameObjectBlueprint> _GenerallyEligbleForDisplayBlueprints = null;
        public static IEnumerable<GameObjectBlueprint> GenerallyEligbleForDisplayBlueprints
        {
            get
            {
                if (_GenerallyEligbleForDisplayBlueprints.IsNullOrEmpty())
                    _GenerallyEligbleForDisplayBlueprints = GameObjectFactory.Factory
                        ?.BlueprintList
                        ?.Where(IsGenerallyEligbleForDisplay);

                return _GenerallyEligbleForDisplayBlueprints;
            }
        }

        [ModSensitiveCacheInit]
        public static void ClearCacheOfGenerallyEligbleForDisplayBlueprints()
        {
            Log(typeof(AnatomyChoice).CallChain(nameof(ClearCacheOfGenerallyEligbleForDisplayBlueprints)));
            _GenerallyEligbleForDisplayBlueprints = null;
        }

        public static string GetTile(GameObjectBlueprint Blueprint)
            => Blueprint.GetPartParameter<string>(nameof(Render), nameof(Render.Tile))
            ;
        public static string GetAnatomyName(GameObjectBlueprint Blueprint)
            => Blueprint.GetPartParameter<string>(nameof(Body), nameof(Body.Anatomy))
            ;
        public static Anatomy GetAnatomy(GameObjectBlueprint Blueprint)
            => Anatomies.GetAnatomyOrFail(GetAnatomyName(Blueprint))
            ;
        public static bool IsGenerallyEligbleForDisplay(GameObjectBlueprint Blueprint)
        {
            if (Blueprint == null)
                return false;

            if (!Blueprint.InheritsFrom("PhysicalObject"))
                return false;

            if (Blueprint.HasSTag("Chiliad"))
                return false;

            if (Blueprint.HasTag("Golem"))
                return false;

            if (Blueprint.InheritsFromAny(
                Blueprints: new string[]
                {
                    "Templar",
                }))
                return false;

            if (Blueprint.InheritsFrom("Chair")
                && Blueprint.Name.EndsWithAny(" L", " C", " R"))
                return false;

            if (Blueprint.Name.Contains("Cherub"))
                return false;

            if (GetTile(Blueprint) is not string renderTile)
                return false;

            if (renderTile.Contains("sw_farmer")
                && GetAnatomyName(Blueprint) is string anatomy
                && !anatomy.EqualsNoCase("Humanoid"))
                return false;

            if (Blueprint.TryGetPartParameter(nameof(Door), nameof(Door.SyncAdjacent), out bool syncAdjacent)
                && syncAdjacent)
                return false;

            return true;
        }

        #endregion
        #region AnatomyExclusions

        [ModSensitiveStaticCache]
        private static List<AnatomyExclusion> _AnatomyExclusions = null;
        public static List<AnatomyExclusion> AnatomyExclusions
        {
            get
            {
                if (_AnatomyExclusions.IsNullOrEmpty())
                    _AnatomyExclusions = GameObjectFactory.Factory.GetBlueprintsInheritingFrom("UD_BodyPlan_Selection_BaseExclusion")
                        .Select(bp => new AnatomyExclusion(bp))
                        .ToList();

                return _AnatomyExclusions;
            }
        }

        public static AnatomyExclusion GetAnatomyExclusion(string Anatomy)
            => !Anatomy.IsNullOrEmpty()
            ? AnatomyExclusions?.FirstOrDefault(e => !e.Anatomies.IsNullOrEmpty() && e.Anatomies.Contains(Anatomy))
            : null
            ;
        public static AnatomyExclusion GetAnatomyExclusion(Anatomy Anatomy)
        {
            var exclusion = GetAnatomyExclusion(Anatomy?.Name);

            if (exclusion == null
                && Anatomy.Category == BodyPartCategory.MECHANICAL)
            {
                exclusion = new(Anatomy);
                AnatomyExclusions.Add(exclusion);
            }
            return exclusion;
        }
        public static AnatomyExclusion GetAnatomyExclusion(AnatomyChoice Choice)
            => GetAnatomyExclusion(Choice?.Anatomy)
            ;

        public static bool TryGetAnatomyExclusion(string Anatomy, out AnatomyExclusion AnatomyExclusion)
            => (AnatomyExclusion = GetAnatomyExclusion(Anatomy)) != null
            ;
        public static bool TryGetAnatomyExclusion(Anatomy Anatomy, out AnatomyExclusion AnatomyExclusion)
            => TryGetAnatomyExclusion(Anatomy?.Name, out AnatomyExclusion)
            ;
        public static bool TryGetAnatomyExclusion(AnatomyChoice Choice, out AnatomyExclusion AnatomyExclusion)
            => TryGetAnatomyExclusion(Choice?.Anatomy, out AnatomyExclusion)
            ;

        #endregion
        #region Pseudo-Debug

        public static void Log(string Message, int Indent = 0)
        {
            if (Indent > 0)
                Message = " ".ThisManyTimes(Indent * 4) + Message;
            UnityEngine.Debug.Log(Message);
        }
        public static void Log(object Context, int Indent = 0)
            => Log(Context.ToString(), Indent);

        public static bool LogReturnBool(bool Return, string Message, int Indent = 0)
        {
            Log(Message, Indent);
            return Return;
        }
        public static bool LogTrue(string Message, int Indent = 0)
            => LogReturnBool(true, Message, Indent)
            ;
        public static bool LogFalse(string Message, int Indent = 0)
            => LogReturnBool(false, Message, Indent)
            ;

        #endregion
    }
}
