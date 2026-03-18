using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using ConsoleLib.Console;

using Qud.UI;

using XRL;
using XRL.Collections;
using XRL.UI;
using XRL.Wish;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;

using Debug = UD_ChooseYourBodyPlan.Mod.Logging.Debug;

using static UD_ChooseYourBodyPlan.Mod.CharacterBuilds.QudBodyPlanModule;
using UD_ChooseYourBodyPlan.Mod.Logging;

namespace UD_ChooseYourBodyPlan.Mod
{
    [HasWishCommand]
    [HasModSensitiveStaticCache]
    public static partial class Utils
    {
        public static ModInfo ThisMod => ModManager.GetMod(Const.MOD_ID);

        public static bool IsTruekinEmbarking = false;

        public static BodyPlanRender EmbarkingGenoSubtypeRender = null;

        #region Pseudo-Debug

        public static void Error(object Message)
            => ThisMod.Error(Message);

        public static void Warn(object Message)
            => ThisMod.Warn(Message);


		public static string CallChain(params string[] Strings)
			=> Strings
				?.Aggregate(
					seed: "",
					func: (a, n) => a + (!a.IsNullOrEmpty() && !n.IsNullOrEmpty() ? "." : null) + n);

		public static bool DisableDebug = false;

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
		
        public static ModInfo GetFirstCallingModNot(ModInfo ThisMod)
		{
			try
			{
				Dictionary<Assembly, ModInfo> modAssemblies = ModManager.ActiveMods
					?.Where(mi => mi != ThisMod && mi.Assembly != null)
					?.ToDictionary(mi => mi.Assembly, mi => mi);

				if (modAssemblies.IsNullOrEmpty())
				{
					return null;
				}
				StackTrace stackTrace = new();
				for (int i = 0; i < 12 && stackTrace?.GetFrame(i) is StackFrame stackFrameI; i++)
				{
					if (stackFrameI?.GetMethod() is MethodBase methodBase
						&& methodBase.DeclaringType is Type declaringType
						&& modAssemblies.ContainsKey(declaringType.Assembly))
					{
						return modAssemblies[declaringType.Assembly];
					}
				}
			}
			catch (Exception x)
			{
				MetricsManager.LogException(nameof(GetFirstCallingModNot), x, Const.GAME_MOD_EXCEPTION);
			}
			return null;
		}

		public static bool TryGetFirstCallingModNot([NotNullWhen(true)] ModInfo ThisMod, out ModInfo FirstCallingMod)
			=> (FirstCallingMod = GetFirstCallingModNot(ThisMod)) != null
            ;

		public static string AppendTick(string String, bool AppendSpace = true)
			=> String + "[" + Const.TICK + "]" + (AppendSpace ? " " : "")
            ;

		public static string AppendCross(string String, bool AppendSpace = true)
			=> String + "[" + Const.CROSS + "]" + (AppendSpace ? " " : "")
            ;

		public static string AppendYehNah(string String, bool Yeh, bool AppendSpace = true)
			=> Yeh
			? AppendTick(String, AppendSpace)
			: AppendCross(String, AppendSpace)
            ;

		public static string YehNah(bool? Yeh = null)
			=> "[" + (Yeh == null ? "-" : (Yeh.GetValueOrDefault() ? Const.TICK : Const.CROSS)) + "]"
            ;

		public static string DelimitedAggregator<T>(string Accumulator, T Next, string Delimiter)
			=> Accumulator + (!Accumulator.IsNullOrEmpty() ? Delimiter : null) + Next
            ;

		public static string CommaDelimitedAggregator<T>(string Accumulator, T Next)
			=> DelimitedAggregator(Accumulator, Next, ",")
            ;

		public static string CommaSpaceDelimitedAggregator<T>(string Accumulator, T Next)
			=> DelimitedAggregator(Accumulator, Next, ", ")
            ;

		public static string NewLineDelimitedAggregator<T>(string Accumulator, T Next)
			=> DelimitedAggregator(Accumulator, Next, "\n")
            ;

		public static bool EitherNull<Tx, Ty>(
			[NotNullWhen(false)] Tx X,
			[NotNullWhen(false)] Ty Y,
			out bool AreEqual)
		{
			AreEqual = (X is null) == (Y is null);
			return X is null
				|| Y is null;
		}

		public static bool EitherNull<Tx, Ty>(
			[NotNullWhen(false)] Tx X,
			[NotNullWhen(false)] Ty Y,
			out int Comparison)
		{
			Comparison = 0;

			bool xNull = X is null;
			bool yNull = Y is null;

			if (!xNull
				&& !yNull)
				return false;

			if (!xNull
				&& yNull)
				Comparison = 1;

			if (xNull
				&& !yNull)
				Comparison = -1;

			return true;
		}

		public static bool EitherNullOrEmpty<Tx, Ty>(
			[NotNullWhen(false)] Tx[] X,
			[NotNullWhen(false)] Ty[] Y,
			out bool AreEqual)
		{
			if (EitherNull(X, Y, out AreEqual))
				return AreEqual;

			AreEqual = (X.Length > 0) == (Y.Length > 0);
			return X.Length > 0
				|| Y.Length > 0;
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

        public static int GetCategoryCode(string Anatomy)
            => Anatomies.GetAnatomyOrFail(Anatomy).BodyCategory is int category
            ? category
            : 1
            ;

        public static T MergeReplaceField<T>(ref T Field, T Becomes)
		{
			using Indent indent = new(1);
			Debug.LogMethod(indent,
				ArgPairs: new Debug.ArgPair[]
				{
					Debug.Arg(nameof(Field), !Equals(Field, null) ? Field.ToString() : "null"),
					Debug.Arg("<-"),
					Debug.Arg(nameof(Becomes), Becomes?.ToString() ?? "null"),
				});

			if (!Equals(Becomes, default))
                Field = Becomes;

            return Field;
        }

        public static T MergeRequireField<T>(ref T Field, T Becomes)
		{
			using Indent indent = new(1);
			Debug.LogMethod(indent,
				ArgPairs: new Debug.ArgPair[]
				{
					Debug.Arg(nameof(Field), !Equals(Field, null) ? Field.ToString() : "null"),
					Debug.Arg("<-"),
					Debug.Arg(nameof(Becomes), Becomes?.ToString() ?? "null"),
				});

            if (Equals(Field, null))
				Field = Becomes;

            return Field;
        }

        public static ICollection<T> MergeRequireField<T>(
            ICollection<T> Field,
            ICollection<T> Becomes
            )
		{
			using Indent indent = new(1);
            /*
			Debug.LogMethod(indent,
				ArgPairs: new Debug.ArgPair[]
				{
					Debug.Arg(nameof(Field), Field?.Count ?? -1),
					Debug.Arg("<-"),
					Debug.Arg(nameof(Becomes), Becomes?.Count ?? -1),
				});
            */
			bool required = false;
			if (Field.IsNullOrEmpty())
			{
				Field = Becomes;
				required = true;
			}
			Debug.YehNah(nameof(required), required, required, Indent: indent[1]);
			return Field;
        }

        public static ICollection<T> MergeReplaceField<T>(
            ICollection<T> Field,
            ICollection<T> Becomes
            )
		{
			using Indent indent = new(1);
            /*
			Debug.LogMethod(indent,
				ArgPairs: new Debug.ArgPair[]
				{
					Debug.Arg(nameof(Field), Field?.Count ?? -1),
					Debug.Arg("<-"),
					Debug.Arg(nameof(Becomes), Becomes?.Count ?? -1),
				});
            */
            bool replaced = false;
			if (!Becomes.IsNullOrEmpty())
			{
				Field = Becomes;
                replaced = true;
			}
            Debug.YehNah(nameof(replaced), replaced, replaced, Indent: indent[1]);
            return Field;
        }

		/// <summary>
		/// "Merge distinct" adds any <paramref name="Other"/> elements that the <paramref name="Source"/> collection doesn't already contain.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="Source"></param>
		/// <param name="Other"></param>
		/// <returns></returns>
		public static ICollection<T> MergeRequireDistinctInCollection<T>(
            ICollection<T> Source,
            ICollection<T> Other,
			Predicate<T> Filter = null
            )
		{
			using Indent indent = new(1);
            /*
			Debug.LogMethod(indent,
				ArgPairs: new Debug.ArgPair[]
				{
					Debug.Arg(nameof(Source), Source?.Count ?? -1),
					Debug.Arg("<-"),
					Debug.Arg(nameof(Other), Other?.Count ?? -1),
				});
            */
			if (!Other.IsNullOrEmpty())
            {
				foreach (var element in Other)
				{
					if (!Source.Contains(element))
					{
						if (Filter?.Invoke(element) is false)
							continue;

						Source.Add(element);
						Debug.CheckYeh(element?.ToString() ?? "NO_ELEMENT", "Added", Indent: indent[1]);
					}
				}
			}
            else
			{
				Debug.CheckNah("Nothing to add", Indent: indent[1]);
			}
            return Source;
        }

		/// <summary>
		/// "Merge replace" adds all <paramref name="Other"/> values to the <paramref name="Source"/> dictionary, overwriting the value of keys already present.
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="Source"></param>
		/// <param name="Other"></param>
		/// <returns>The modified <paramref name="Source"/> dictionary.</returns>
		public static IDictionary<TKey, TValue> MergeReplaceDictionary<TKey, TValue>(
            IDictionary<TKey, TValue> Source,
            IDictionary<TKey, TValue> Other)
		{
			using Indent indent = new(1);
            /*
			Debug.LogMethod(indent,
				ArgPairs: new Debug.ArgPair[]
				{
					Debug.Arg(nameof(Source), Source?.Count ?? -1),
					Debug.Arg("<-"),
					Debug.Arg(nameof(Other), Other?.Count ?? -1),
				});
            */
            if (!Other.IsNullOrEmpty())
			{
				foreach ((TKey key, TValue value) in Other)
				{
					if (Source.ContainsKey(key))
                        Debug.Log($"Value at key {key}", "replaced", Indent: indent[1]);
					Source[key] = value;
				}
			}
			else
			{
				Debug.CheckNah("Nothing to add", Indent: indent[1]);
			}
			return Source;
        }

        /// <summary>
        /// "Merge require" only adds <paramref name="Other"/> values to keys missing from the <paramref name="Source"/> dictionary, skipping any keys for which there is already a value.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="Source"></param>
        /// <param name="Other"></param>
        /// <returns>The modified <paramref name="Source"/> dictionary.</returns>
        public static IDictionary<TKey, TValue> MergeRequireDictionary<TKey, TValue>(
            IDictionary<TKey, TValue> Source,
            IDictionary<TKey, TValue> Other
            )
		{
			using Indent indent = new(1);
            /*
			Debug.LogMethod(indent,
				ArgPairs: new Debug.ArgPair[]
				{
					Debug.Arg(nameof(Source), Source?.Count ?? -1),
					Debug.Arg("<-"),
					Debug.Arg(nameof(Other), Other?.Count ?? -1),
				});
            */
			if (!Other.IsNullOrEmpty())
			{
				foreach ((TKey key, TValue value) in Other)
				{
					if (!Source.ContainsKey(key))
					{
						Debug.Log($"{key} value", "required", Indent: indent[1]);
						Source[key] = value;
					}
				}
			}
			else
			{
				Debug.CheckNah("Nothing to add", Indent: indent[1]);
			}
			return Source;
        }

        #region Wishes

        public static string UD_CYBP_Output => DataManager.SavePath("UD_CYBP_BodyPlans.xml");

        public static List<BodyPlanEntry> BodyPlanChoices => BodyPlanFactory.Factory?.BodyPlanEntryByAnatomyName?.Values?.ToList();

        [WishCommand(Command = "UD_CYBP bodyplans from blueprints")]
        public static bool BodyPlanEntryTags_WishHandler(string Parameters)
        {
            if (Popup.ShowYesNo("Include blueprint names in output file?") is DialogResult result
                && result != DialogResult.Cancel)
            {
                bool IncludeName = result == DialogResult.Yes;

                string forAnatomy = null;
                using var options = ScopeDisposedList<string>.GetFromPool();
                options.Add("All");
                using var hotkeys = ScopeDisposedList<char>.GetFromPool();
                hotkeys.Add(default);
                foreach (var anatomy in Anatomies.AnatomyList)
                {
                    options.Add(anatomy.Name);
                    hotkeys.Add(default);
                }

                Popup.PickOption(
                    Title: "For which anatomy?",
                    Options: options,
                    Hotkeys: hotkeys,
                    OnResult: i =>
                    {
                        if (i > 0) forAnatomy = Anatomies.AnatomyList[i - 1]?.Name;
                    },
                    AllowEscape: true);

                var prefixResult = DialogResult.No;
                string modPrefix = "";
                string namePrefix = "";
                do
                {
                    modPrefix = Popup.AskString(
                        Message: "Enter you mod prefix (leave blank for none, escape to cancel)",
                        Default: modPrefix,
                        ReturnNullForEscape: true);

                    if (modPrefix == null)
                        return false;

                    namePrefix = "UD_CYBP_BodyPlanEntry";

                    if (!modPrefix.IsNullOrEmpty())
                        namePrefix = $"{modPrefix}_{namePrefix}";

                    Popup.WaitNewPopupMessage(
                        message: $"Object blueprints will follow this naming convention: \"{namePrefix} AnatomyName\".", 
                        buttons: new List<QudMenuItem>()
                        {
                            new()
                            {
                                text = ControlManager.getCommandInputFormatted("Accept") + " {{y|Proceed}}",
                                command = "Yes",
                                hotkey = "Y,Accept"
                            },
                            new()
                            {
                                text = ControlManager.getCommandInputFormatted("V Negative") + " {{y|Re-enter}}",
                                command = "No",
                                hotkey = "N,V Negative"
                            },
                            new()
                            {
                                text = "{{W|" + ControlManager.getCommandInputDescription("Cancel", XRL.UI.Options.ModernUI) + " Cancel}}",
                                command = "Cancel",
                                hotkey = "Cancel"
                            }
                        },
                        callback: delegate (QudMenuItem i)
                        {
                            if (i.command == "No")
                                prefixResult = DialogResult.No;

                            if (i.command == "Yes")
                                prefixResult = DialogResult.Yes;

                            if (i.command == "Cancel")
                                prefixResult = DialogResult.Cancel;
                        });

                    if (prefixResult == DialogResult.Cancel)
                        return false;
                }
                while (prefixResult != DialogResult.Yes);

                if (modPrefix == "")
                    modPrefix = null;

                if (Popup.ShowYesNo("There are literal thousands of eligible blueprints.\n\n" +
                    "If you haven't selected a specific anatomy, this operation may take some time, " +
                    "and the resultant file will be quite long.\n\n" +
                    "Picking specific objects out into a separate file is recommended.\n\nAre you sure?") != DialogResult.Yes)
                    return false;

                string inherits = Const.BODYPLAN_ENTRY_BLUEPRINT;
                if (new StreamWriter($"{forAnatomy}{UD_CYBP_Output}") is StreamWriter writer)
                {
                    using (var anatomyChoices = ScopeDisposedList<BodyPlanEntry>.GetFromPoolFilledWith(BodyPlanChoices))
                    {
                        Log("-".ThisManyTimes(25));
                        writer.WriteLine2("<?xml version=\"1.0\" encoding=\"utf-8\" ?>")
                            .WriteLine2("<objects>");

                        var attributes = new Dictionary<string, object>();
                        for (int i = 0; i < anatomyChoices.Count; i++)
                        {
                            if (anatomyChoices[i]?.Anatomy is Anatomy anatomy)
                            {
                                if (!forAnatomy.IsNullOrEmpty()
                                    && anatomy.Name != forAnatomy)
                                    continue;

                                if (i > 0)
                                    writer.WriteLine2("");

                                writer.WriteLine2($"<!-- {anatomy.Name} -->", Indent: 1);

                                if (anatomyChoices[i].GetExampleBlueprints().ToList() is List<GameObjectBlueprint> blueprints
                                    && !blueprints.IsNullOrEmpty())
                                {
                                    int count = blueprints.Count();
                                    for (int j = 0; j < count; j++)
                                    { 
                                        if (blueprints[j] is GameObjectBlueprint blueprint
                                            && blueprint.GetRenderable() is Renderable renderable)
                                        {
                                            attributes[nameof(IPart.Name)] = nameof(Render);

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
                                                writer.WriteBodyPlanEntryBlueprint(
                                                    NamePrefix: namePrefix,
                                                    AnatomyName: anatomy.Name,
                                                    Inherits: inherits,
                                                    Attributes: attributes,
                                                    IncludeName: IncludeName,
                                                    BasedOnBlueprint: blueprint.Name,
                                                    Indent: 1);
                                                attributes.Clear();
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    writer.WriteBodyPlanEntryBlueprint(
                                        NamePrefix: namePrefix,
                                        AnatomyName: anatomy.Name,
                                        Inherits: inherits,
                                        Attributes: new Dictionary<string, object>()
                                        {
                                            { nameof(IPart.Name), nameof(Render) },
                                            { nameof(Render.Tile), "Creatures/sw_mimic.bmp" },
                                            { nameof(Render.RenderString), "*" },
                                            { nameof(Render.ColorString), "&amp;w^y" },
                                            { nameof(Render.TileColor), "&amp;w" },
                                            { nameof(Render.DetailColor), "y" },
                                        },
                                        IncludeName: IncludeName,
                                        BasedOnBlueprint: "default",
                                        Indent: 1);
                                }
                            }
                        }
                        writer.WriteLine2("</objects>");
                        Log("-".ThisManyTimes(25));
                    }
                    writer.Flush();
                    writer.Dispose();
                }
                return true;
            }
            return false;
        }

        #endregion
    }
}
