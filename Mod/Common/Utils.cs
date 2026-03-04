using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using ConsoleLib.Console;

using XRL;
using XRL.CharacterBuilds;
using XRL.CharacterBuilds.Qud;
using XRL.Collections;
using XRL.Wish;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;

using static XRL.CharacterBuilds.Qud.Qud_UD_BodyPlanModule;

namespace UD_BodyPlan_Selection.Mod
{
    [HasWishCommand]
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
                    _AnatomyExclusions = GameObjectFactory.Factory?.GetBlueprintsInheritingFrom("UD_BodyPlan_Selection_BaseExclusion")
                        .SelectMany(bp => new AnatomyExclusion(bp).FromAnatomiesList())
                        .ToList();

                return _AnatomyExclusions;
            }
        }

        public static IEnumerable<AnatomyExclusion> GetAnatomyExclusions(string Anatomy)
            => AnatomyExclusions?.Where(e => !e.Anatomy.IsNullOrEmpty() && e.Anatomy == Anatomy);

        public static IEnumerable<AnatomyExclusion> GetAnatomyExclusions(Anatomy Anatomy)
        {
            if (Anatomy == null)
                yield break;

            bool anyMechanical = false;
            foreach (AnatomyExclusion exclusion in GetAnatomyExclusions(Anatomy.Name))
            {
                anyMechanical = exclusion.IsMechanical
                    || anyMechanical;
                yield return exclusion;
            }

            if (!anyMechanical
                && Anatomy.Category == BodyPartCategory.MECHANICAL)
            {
                var exclusion = new AnatomyExclusion(Anatomy);
                AnatomyExclusions.Add(exclusion);
                yield return exclusion;
            }
        }
        public static IEnumerable<AnatomyExclusion> GetAnatomyExclusions(AnatomyChoice Choice)
            => GetAnatomyExclusions(Choice?.Anatomy)
            ;

        public static bool TryGetAnatomyExclusions(string Anatomy, out IEnumerable<AnatomyExclusion> AnatomyExclusion)
            => (AnatomyExclusion = GetAnatomyExclusions(Anatomy)).IsNullOrEmpty()
            ;
        public static bool TryGetAnatomyExclusions(Anatomy Anatomy, out IEnumerable<AnatomyExclusion> AnatomyExclusion)
            => TryGetAnatomyExclusions(Anatomy?.Name, out AnatomyExclusion)
            ;
        public static bool TryGetAnatomyExclusionsFor(AnatomyChoice Choice, out IEnumerable<AnatomyExclusion> AnatomyExclusion)
            => TryGetAnatomyExclusions(Choice?.Anatomy, out AnatomyExclusion)
            ;

        #endregion
        #region Pseudo-Debug

        public static bool DisableDebug = true;

        public static void Log(string Message, int Indent = 0)
        {
            if (DisableDebug)
                return;

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

        public static string NewlineAggregator<T>(string Accumulator, T Next)
            => Accumulator + (!Accumulator.IsNullOrEmpty() ? '\n' : null) + Next;

        public static StringBuilder AggregateNewline<T>(StringBuilder Accumulator, T Next)
            => Accumulator
                .Append(!Accumulator.IsNullOrEmpty() ? '\n' : null)
                .Append(Next);

        public static bool IsEqualDepthToRoot(BodyPart BodyPart)
            => BodyPart.GetPartDepth() < 1;

        public static bool IsNotEqualDepthToRoot(BodyPart BodyPart)
            => !IsEqualDepthToRoot(BodyPart);

        #region Wishes

        public static string UD_BPS_Output => DataManager.SavePath("AnatomyTiles.xml");

        [ModSensitiveStaticCache]
        public static List<AnatomyChoice> AnatomyChoices = new();

        [WishCommand(Command = "UD_BPS anatomy tile tags")]
        public static bool AnatomyTileTags_WishHandler(string Parameters)
        {
            bool IncludeName = Parameters?.Contains("with names") ?? false;
            string forAnatomy = Regex.Replace(Parameters, @" ?\@(\w.?) ?", @"$1");
            if (!AnatomyChoices.IsNullOrEmpty()
                && new StreamWriter(UD_BPS_Output) is StreamWriter writer)
            {
                using (var anatomyChoices = ScopeDisposedList<AnatomyChoice>.GetFromPoolFilledWith(AnatomyChoices))
                {
                    Log("-".ThisManyTimes(25));
                    writer.WriteLine2("<?xml version=\"1.0\" encoding=\"utf-8\" ?>")
                        .WriteLine2("<objects>")
                            .WriteLine2("<object Name=\"UD_BodyPlan_Slection_AnatomyTiles\" Load=\"Merge\" >", Indent: 1);
                    var sB = Event.NewStringBuilder();
                    var attributes = new Dictionary<string, object>();
                    for (int i = 0; i < anatomyChoices.Count; i++)
                        if (anatomyChoices[i]?.Anatomy is Anatomy anatomy)
                        {
                            if (!forAnatomy.IsNullOrEmpty()
                                && anatomy.Name.Replace("-", "_").Replace(" ", "_") != forAnatomy)
                                continue;

                            if (i > 0)
                                writer.WriteLine2("");
                            
                            if (anatomyChoices[i].GetExampleBlueprints().ToList() is List<GameObjectBlueprint> blueprints
                                && !blueprints.IsNullOrEmpty())
                            {
                                int count = blueprints.Count();
                                for (int j = 0; j < count; j++)
                                    if (blueprints[j] is GameObjectBlueprint blueprint
                                        && blueprint.GetRenderable() is Renderable renderable)
                                    {
                                        if (renderable?.Tile is string tile)
                                            attributes[nameof(renderable.Tile)] = tile;

                                        if (renderable?.RenderString is string renderString)
                                            attributes[nameof(renderable.RenderString)] = renderString;

                                        if (renderable?.ColorString is string colorString)
                                            attributes[nameof(renderable.ColorString)] = colorString;

                                        if (renderable?.TileColor is string tileColor)
                                            attributes[nameof(renderable.TileColor)] = tileColor;

                                        if (renderable?.DetailColor is char detailColor)
                                            attributes[nameof(renderable.DetailColor)] = detailColor;

                                        if (!attributes.IsNullOrEmpty())
                                        {
                                            sB.Append($"<!--xtagUD_BDS_{anatomy.Name.Replace("-", "_").Replace(" ", "_")} ");

                                            if (IncludeName)
                                                sB.Append($"Blueprint=\"{blueprint.Name}\" ");

                                            foreach ((string attribute, object value) in attributes)
                                                sB.Append($"{attribute}=\"{value}\" ");

                                            sB.Append($"/-->");

                                            writer.WriteLine2(sB.ToString(), 2);
                                            sB.Clear();
                                            attributes.Clear();
                                        }
                                    }
                            }
                            else
                                writer.WriteLine2($"<!--xtagUD_BDS_{anatomy.Name} " +
                                    (IncludeName ? $"Blueprint=\"default\" " : null) +
                                    $"Tile=\"Creatures/sw_mimic.bmp\" " +
                                    $"RenderString=\"*\" " +
                                    $"ColorString=\"&amp;w^y\" " +
                                    $"TileColor=\"&amp;w\" " +
                                    $"DetailColor=\"y\" " +
                                    $"/-->", 2);
                        }

                    writer.WriteLine2("</object>", Indent: 1)
                        .WriteLine2("</objects>");
                    Log("-".ThisManyTimes(25));
                }
                writer.Flush();
                writer.Dispose();
            }
            return true;
        }

        #endregion
    }
}
