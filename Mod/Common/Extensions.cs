using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using XRL;
using XRL.CharacterBuilds;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;
using XRL.Collections;
using XRL.UI.Framework;

using Event = XRL.World.Event;

using UD_ChooseYourBodyPlan.Mod.Logging;

using static UD_ChooseYourBodyPlan.Mod.Utils;
using static UD_ChooseYourBodyPlan.Mod.Const;

namespace UD_ChooseYourBodyPlan.Mod
{
    public static class Extensions
	{
		#region Debug Registration
		[UD_DebugRegistry]
		public static void doDebugRegistry(DebugMethodRegistry Registry)
			=> Registry.RegisterEach(
				Type: typeof(UD_ChooseYourBodyPlan.Mod.Extensions),
				MethodNameValues: new()
				{
					{ nameof(GetTags), false },
				});
		#endregion

		public static Func<string, T> ToFunc<T>(this Parse<T> Parse)
            => Parse.Invoke;
        public static Parse<T> ToParse<T>(this Func<string, T> Func)
            => s => Func(s);

        public static string SplitCamelCase(this string String)
            => !String.Contains(" ")
            ? Regex.Replace(
                input: Regex.Replace(
                    input: String,
                    pattern: @"(\P{Ll})(\P{Ll}\p{Ll})",
                    replacement: "$1 $2"),
                pattern: @"(\p{Ll})(\P{Ll})",
                replacement: "$1 $2")
            : String
			;

		public static string Join(this string Accumulator, string Next, string Delimiter = ", ")
			=> Accumulator + (!Accumulator.IsNullOrEmpty() ? Delimiter : null) + Next;

		public static string Join(this IEnumerable<string> Strings, string Delimiter = ", ")
			=> Strings?.Aggregate("", (a, n) => a?.Join(n, Delimiter));

		public static string GenericsString(this IEnumerable<Type> Types, bool Short = false)
			=> !Types.IsNullOrEmpty()
			? "<" +
				Types
					.ToList()
					.ConvertAll(t => t.ToStringWithGenerics(Short))
					.Join("," + (!Short ? " " : null)) +
				">"
			: null;

		public static string ToStringWithGenerics(this Type Type, bool Short = false)
		{
			if (Type == null)
				return null;

			if (Type.GetGenericArguments() is not IEnumerable<Type> typeGenerics)
				return !Short
					? Type.Name
					: Type.Name.Acronymize();

			string name = Type.Name.Split('`')[0];

			if (Short)
				name = name.Acronymize();

			return name + typeGenerics.GenericsString(Short);
		}

		public static string TypeStringWithGenerics<T>(this T Object, bool Short = false)
			=> (Object?.GetType() ?? typeof(T))?.ToStringWithGenerics(Short);

		public static string Acronymize(this string String)
		{
			if (String.IsNullOrEmpty()
				|| String.ToLower() == String
				|| String.ToUpper() == String)
				return String;

			return String.Aggregate("", (a, n) => a + (char.IsLetter(n) && char.IsUpper(n) ? n : null));
		}

		public static bool None<T>([NotNullWhen(true)] this IEnumerable<T> Enumberable, Predicate<T> Where)
			=> !Enumberable.Any(Where?.ToFunc());

		public static Func<T, bool> ToFunc<T>(this Predicate<T> Filter, bool ThrowIfNull = false)
		{
			if (Filter == null && ThrowIfNull)
				throw new ArgumentNullException(
					paramName: nameof(Filter),
					message: "cannot be null if " + nameof(ThrowIfNull) + " is set to " + ThrowIfNull.ToString());

			return Input => Filter == null || Filter(Input);
		}

		public static bool InheritsFrom(
			[NotNullWhen(true)] this Type Type,
			[NotNullWhen(true)] Type OtherType,
			bool IncludeSelf = true)
			=> Type != null
			&& OtherType != null
			&& ((IncludeSelf
					&& Type == OtherType)
				|| OtherType.IsSubclassOf(Type)
				|| Type.IsAssignableFrom(OtherType)
				|| (Type.YieldInheritedTypes().ToList() is List<Type> inheritedTypes
					&& inheritedTypes.Contains(OtherType)));

		public static string SafeJoin<T>(this IEnumerable<T> Enumerable, string Delimiter = ", ")
			=> (Enumerable != null
				&& Enumerable.Count() > 0)
			? Enumerable.Aggregate(
				seed: "",
				func: (a, n) => a + (!a.IsNullOrEmpty() ? Delimiter : null) + n.ToString())
			: null;

		public static string ValueUnits(this TimeSpan Duration)
		{
			string durationUnit = "minute";
			double durationValue = Duration.TotalMinutes;
			if (Duration.TotalMinutes < 1)
			{
				durationUnit = "second";
				durationValue = Duration.TotalSeconds;
			}
			if (Duration.TotalSeconds < 1)
			{
				durationUnit = "millisecond";
				durationValue = Duration.TotalMilliseconds;
			}
			if (Duration.TotalMilliseconds < 1)
			{
				durationUnit = "microsecond";
				durationValue = Duration.TotalMilliseconds / 1000;
			}
			return durationValue.Things(durationUnit);
		}

		public static string Signed(this float Float)
			=> (Float < 0
				? null
				: "+") +
			Float
            ;

		public static bool ElementsMatch<Tx, Ty>(this Tx[] X, Ty[] Y)
		{
			if (EitherNullOrEmpty(X, Y, out bool areEqual))
				return areEqual;

			if (X.Length != Y.Length)
				return false;

			for (int i = 0; i < X.Length; i++)
			{
				Tx x = X[i];
				Ty y = Y[i];
				if ((!EitherNull(x, y, out bool iAreEqual)
						&& !x.Equals(y))
					|| iAreEqual)
					return false;
			}

			return true;
		}

		public static bool LogReturning(this bool Return, string Message)
            => LogReturnBool(Return, Message);

        public static bool HasSTag(this GameObjectBlueprint Blueprint, string STag)
            => Blueprint?.Tags?.Keys is Dictionary<string, string>.KeyCollection keys
            && keys.Any(s => s.Equals("Semantic" + STag));

        public static bool InheritsFromAny(this GameObjectBlueprint Blueprint, params string[] Blueprints)
            => !Blueprints.IsNullOrEmpty()
            && Blueprints.Any(bp => Blueprint.InheritsFrom(bp));

        public static string ThisManyTimes(this string @string, int Times = 1)
            => Times.Aggregate("", (a, n) => a + @string)
            ;
        public static string ThisManyTimes(this char @char, int Times = 1)
            => @char.ToString().ThisManyTimes(Times)
            ;

        public static string CallChain(this string String, params string[] Calls)
            => Calls.Aggregate(String, (a, n) => a + "." + n)
            ;

        public static string CallChain(this Type Type, params string[] Calls)
            => Type.Name.CallChain(Calls);

        public static bool Sucks(this Anatomy Anatomy)
            => Anatomy.BodyCategory == BodyPartCategory.LIGHT
            || Anatomy.Category == BodyPartCategory.LIGHT
            || Anatomy.Name == "Echinoid"
            ;

        public static bool HasRecipe(this Anatomy Anatomy)
            => new string[]
            {
                "SlugWithHands",
                "HumanoidOctohedron",
            }
            .Contains(Anatomy.Name);

        public static StringBuilder AppendNoCybernetics(this StringBuilder SB, bool PrependSpace = true)
        {
            if (PrependSpace)
                SB.Append(' ');
            return SB.AppendColored("r", "\x009b");
        }
        public static StringBuilder AppendNaturalWeapon(this StringBuilder SB, bool PrependSpace = true)
        {
            if (PrependSpace)
                SB.Append(' ');
            return SB.AppendColored("w", "\x0006");
        }

        public static string GetTile(this GameObjectBlueprint Blueprint)
            => Utils.GetTile(Blueprint)
            ;
        public static string GetAnatomyName(this GameObjectBlueprint Blueprint)
            => Utils.GetAnatomyName(Blueprint)
            ;
        public static bool HasAnatomy(this GameObjectBlueprint Blueprint)
            => !Utils.GetAnatomyName(Blueprint).IsNullOrEmpty()
            ;
        public static Anatomy GetAnatomy(this GameObjectBlueprint Blueprint)
            => Utils.GetAnatomy(Blueprint)
            ;

        public static T Coalesce<T>(this T Object, T OtherObject)
            => Object ?? OtherObject;

        public static TAccumulate Aggregate<TAccumulate>(
            this int Number,
            TAccumulate seed,
            Func<TAccumulate, int, TAccumulate> func
            )
        {
            for (int i = 0; i < Number; i++)
                seed = func(seed, i);

            return seed;
        }

        public static BallBag<T> AddA<T>(this BallBag<T> Bag, T Ball, int Weight)
        {
            Bag.Add(Ball, Weight);
            return Bag;
        }

        public static ICollection<T> AddA<T>(this ICollection<T> Collection, T Item)
        {
            Collection.Add(Item);
            return Collection;
        }

        public static IEnumerable<TSource> WhereNot<TSource>(
            this IEnumerable<TSource> source,
            Func<TSource, bool> predicate
            )
            => source.Where(t => !predicate(t));

        public static StringBuilder AppendLines(this StringBuilder SB, int Count)
            => Count.Aggregate(SB, (a, n) => a.AppendLine())
            ;

        public static StringBuilder AppendDamage(this StringBuilder SB, string Damage)
            => SB.AppendColored("r", DMG.ToString()).Append(Damage)
            ;
        public static StringBuilder AppendDamage(this StringBuilder SB, string Color, string Damage)
            => SB.AppendColored("r", DMG.ToString()).AppendColored(Color, Damage)
            ;

        public static string PVCapOrInfinity(this int PVCap)
            => PVCap < MeleeWeapon.BONUS_CAP_UNLIMITED
            ? PVCap.ToString()
            : INFT.ToString()
            ;
        public static StringBuilder AppendPVCap(this StringBuilder SB, int PVCap)
            => SB.AppendColored("K", PVCap.PVCapOrInfinity());

        public static StringBuilder AppendPV(this StringBuilder SB, string SymbolColor, int PV, int PVCap)
            => (!SymbolColor.IsNullOrEmpty()
                ? SB.AppendColored(SymbolColor, Const.PV.ToString())
                : SB.Append(Const.PV.ToString()))
            .Append(PV)
            .AppendPVCap(PVCap)
            ;
        public static StringBuilder AppendPV(this StringBuilder SB, string SymbolColor, string Color, int PV, int PVCap)
            => (!SymbolColor.IsNullOrEmpty()
                ? SB.AppendColored(SymbolColor, Const.PV.ToString())
                : SB.Append(Const.PV))
            .AppendColored(Color, PV.ToString())
            .AppendColored("K", "/")
            .AppendPVCap(PVCap)
            ;

        public static StringBuilder AppendAV(this StringBuilder SB, int AV)
            => SB.AppendColored("b", Const.AV.ToString()).Append(AV)
            ;
        public static StringBuilder AppendAV(this StringBuilder SB, string Color, int AV)
            => SB.AppendColored("b", Const.AV.ToString()).AppendColored(Color, AV.ToString())
            ;

        public static StringBuilder AppendDV(this StringBuilder SB, int DV)
            => SB.AppendColored("K", Const.DV.ToString()).Append(DV)
            ;
        public static StringBuilder AppendDV(this StringBuilder SB, string Color, int DV)
            => SB.AppendColored("K", Const.DV.ToString()).AppendColored(Color, DV.ToString())
            ;

        public static StringBuilder AppendArmor(this StringBuilder SB, int AV, int DV)
            => SB.AppendAV(AV).Append(' ').AppendDV(DV)
            ;
        public static StringBuilder AppendArmor(this StringBuilder SB, string Color, int AV, int DV)
            => SB.AppendAV(Color, AV).Append(' ').AppendDV(Color, DV)
            ;

        public static bool EndsWithAny(this string String, params string[] Values)
            => Values.IsNullOrEmpty()
            || Values.Any(s => String.EndsWith(s));

        public static bool ContainsNoCase(this string String, string OtherString)
            => String.ToLower().Contains(OtherString.ToLower());

        /// <summary>
        /// Writes a line to the stream with an optional indent, factored to 2.
        /// </summary>
        /// <param name="Writer">The <see cref="StreamWriter"/> object.</param>
        /// <param name="Value">The Value to write to the stream on its own line.</param>
        /// <param name="Indent">The level of indent (2 spaces) for this line.</param>
        /// <returns>The <see cref="StreamWriter"/> object.</returns>
        public static StreamWriter WriteLine2(this StreamWriter Writer, string Value, int Indent = 0)
        {
            if (Indent > 0)
                Value = " ".ThisManyTimes(Indent * 2) + Value;
            Writer.Write(Value + "\n");
            UnityEngine.Debug.Log(Value);
            return Writer;
        }
        public static StreamWriter WriteLine2If(this StreamWriter Writer, bool Condition, string Value, int Indent = 0)
        {
            if (Condition)
                Writer.WriteLine2(Value, Indent);

            return Writer;
        }

        public static StreamWriter WriteLine4(this StreamWriter Writer, string Value, int Indent = 0)
            => Writer.WriteLine2(Value, Indent * 2)
            ;

        public static StringBuilder AppendAttribute(this StringBuilder SB, KeyValuePair<string, object> Attribute, string After = null)
            => SB.Append(Attribute.Key).Append("=\"").Append(Attribute.Value).Append("\"").Append(After);

        public static StreamWriter WriteNode(
            this StreamWriter Writer,
            string Node,
            Dictionary<string, object> Attributes,
            bool AutoClose = true,
            int Indent = 0
            )
        {
            if (Attributes.IsNullOrEmpty())
                return Writer;

            var sB = Event.NewStringBuilder($"<{Node} ");
            using var attributes = ScopeDisposedList<KeyValuePair<string, object>>.GetFromPoolFilledWith(Attributes);
            int count = attributes.Count();
            for (int i = 0; i < count; i++)
            {
                bool space = !AutoClose && i == count - 1;
                sB.AppendAttribute(attributes[i], space ? " " : null);
            }

            if (AutoClose)
                sB.Append("/>");
            else
                sB.Append($"></{Node}>");

            return Writer.WriteLine2(
                Value: Event.FinalizeString(sB),
                Indent: Indent);
        }

        public static StreamWriter WriteBodyPlanEntryBlueprint(
            this StreamWriter Writer,
            string NamePrefix,
            string AnatomyName,
            string Inherits,
            Dictionary<string, object> Attributes,
            bool IncludeName = true,
            string BasedOnBlueprint = null,
            int Indent = 0
            )
            => Writer.WriteLine2(
                    Value: $"<object Name=\"{NamePrefix} {AnatomyName}\" Inherits=\"{Inherits}\" >",
                    Indent: Indent)
                .WriteLine2(
                    Value: $"<tag Name=\"Anatomy\" Value=\"{AnatomyName}\" />",
                    Indent: Indent + 1)
                .WriteLine2If(
                    Condition: IncludeName,
                    Value: $"<tag Name=\"BasedOnBlueprint\" Value=\"{BasedOnBlueprint}\" />",
                    Indent: Indent + 1)
                .WriteNode(
                    Node: "part",
                    Attributes: Attributes,
                    Indent: Indent + 1)
                .WriteLine2(
                    Value: "</object>",
                    Indent: Indent)
            ;

        public static int GetPartDepth(this BodyPart BodyPart)
            => (BodyPart?.ParentBody?.GetPartDepth(BodyPart)).GetValueOrDefault();

        public static bool IsVariantType(this BodyPart BodyPart)
            => BodyPart?.TypeModel() == BodyPart?.VariantTypeModel()
            || BodyPart?.TypeModel()?.Type == BodyPart?.VariantTypeModel()?.FinalType;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="BodyPart">The body part from which to produce a limb tree.</param>
        /// <param name="Selector">The type parameters of this Func match the other arguments of this method: <paramref name="BodyPart"/>, <paramref name="BodyPartProc"/>, <paramref name="IndentDrawing"/>, <paramref name="DepthLimit"/>, <paramref name="Depth"/>, <paramref name="SiblingOrdinal"/>, <paramref name="SiblingCardinal"/>; returning one element of this method's return value.</param>
        /// <param name="BodyPartProc">Processing to be performed on <paramref name="BodyPart"/> to get a display value for it.</param>
        /// <param name="IndentProc">Processing to be performed on <paramref name="BodyPart"/> to get a display value for it.</param>
        /// <param name="IndentDrawing">Records what should appear at each level of the indent.</param>
        /// <param name="DepthLimit">The maximum depth that indentation should go (truncates anything in excess).</param>
        /// <param name="Depth">The current depth of indentation.</param>
        /// <param name="SiblingOrdinal">The index of the <paramref name="BodyPart"/> in the parent collection of elements in which it might exist.</param>
        /// <param name="SiblingCardinal">The total number of elements in the parent collection in which the <paramref name="BodyPart"/> might exist.</param>
        /// <returns>A collection of strings representing the <paramref name="BodyPart"/> and any subparts it might have in a tree configuration.</returns>
        public static IEnumerable<string> GetLimbTreeLines(
            this BodyPart BodyPart,
            Func<BodyPart, Func<string, string>, Func<BodyPart, string>, Dictionary<int, char>, int, int, int, string> Selector,
            Func<string, string> IndentProc,
            Func<BodyPart, string> BodyPartProc,
            Dictionary<int, char> IndentDrawing,
            int DepthLimit = int.MaxValue,
            int Depth = 0,
            int SiblingOrdinal = 1,
            int SiblingCardinal = 1,
            Predicate<BodyPart> Filter = null
            )
        {
            IndentDrawing ??= new();
            if (BodyPart == null)
                yield break;

            yield return Selector(BodyPart, IndentProc, BodyPartProc, IndentDrawing, Depth, SiblingOrdinal, SiblingCardinal);
            if (Depth >= DepthLimit)
                yield break;

            Filter ??= (bp => true);
            if (BodyPart.LoopSubparts().Where(Filter.Invoke) is IEnumerable<BodyPart> subparts)
            {
                int children = subparts.Count();
                int child = 0;
                if (subparts.SelectMany(
                    selector: bp => bp.GetLimbTreeLines(
                        Selector: Selector,
                        IndentProc: IndentProc,
                        BodyPartProc: BodyPartProc,
                        IndentDrawing: IndentDrawing,
                        DepthLimit: DepthLimit,
                        Depth: Depth + 1,
                        SiblingOrdinal: ++child,
                        SiblingCardinal: children,
                        Filter: Filter)) is IEnumerable<string> subResults)
                    foreach (string subResult in subResults)
                        yield return subResult;
            }
        }
        public static string GetLimbBranch(
            BodyPart BodyPart,
            Func<string, string> IndentProc,
            Func<BodyPart, string> BodyPartProc,
            Dictionary<int, char> IndentDrawing,
            int Depth,
            int Ordinal,
            int Cardinal
            )
        {
            var indent = Event.NewStringBuilder();

            IndentDrawing[Depth] = Ordinal == Cardinal ? NBSP : VERT;

            for (int i = 0; i < Depth; ++i)
            {
                indent
                    .Append(IndentDrawing.GetValueOrDefault(i, NBSP))
                    .Append(NBSP)
                    ;

                if (i == 0)
                    indent.Append(NBSP);
            }

            if (Depth == 0)
                indent
                    .Append(NBSP)
                    .Append(RTRNG)
                    .Append(NBSP)
                    ;
            else
            if (Depth != 0)
                indent
                    .Append(Ordinal == Cardinal ? UANR : VERR)
                    .Append(HRZT)
                    ;

            return $"{IndentProc(Event.FinalizeString(indent))}{BodyPartProc(BodyPart)}";
        }
        public static void GetLimbTree(
            this Body Body,
            StringBuilder SB,
            Func<string, string> IndentProc,
            Func<BodyPart, string> BodyPartProc,
            int DepthLimit = int.MaxValue,
            bool Treat0DepthPartsAsRoot = true
            )
        {
            if (Treat0DepthPartsAsRoot)
                Body
                    ?.LoopParts()
                    ?.Where(IsEqualDepthToRoot)
                    ?.Aggregate(
                        seed: "",
                        func: (a, n) => a + (!a.IsNullOrEmpty() ? '\n' : null) + n.GetLimbTreeLines(
                                Selector: GetLimbBranch,
                                IndentProc: IndentProc ?? (s => "{{y|" + s + "}}"),
                                BodyPartProc: BodyPartProc ?? (bp => bp.Name),
                                IndentDrawing: new Dictionary<int, char>(),
                                DepthLimit: DepthLimit,
                                Filter: IsNotEqualDepthToRoot)
                            ?.Aggregate(
                                seed: SB,
                                func: AggregateNewline));
            else
                Body
                    ?.GetBody()
                    ?.GetLimbTreeLines(
                        Selector: GetLimbBranch,
                        IndentProc: IndentProc ?? (s => "{{y|" + s + "}}"),
                        BodyPartProc: BodyPartProc ?? (bp => bp.Name),
                        IndentDrawing: new Dictionary<int, char>(),
                        DepthLimit: DepthLimit)
                    ?.Aggregate(
                        seed: SB,
                        func: AggregateNewline);
        }



        public static BodyPlan.LimbTreeBranch InitializeLimbTreeBranch(
            this BodyPart BodyPart,
            BodyPlan BodyPlan,
            out bool HasNaturalEquipment
            )
        {
            string defaultBehaviour = BodyPart.VariantTypeModel().DefaultBehavior;
            HasNaturalEquipment = !defaultBehaviour.IsNullOrEmpty();

            string cardinalDescription = BodyPart.GetCardinalDescription();

            string finalType = null;
            if (BodyPart.IsVariantType()
                && !cardinalDescription.ContainsNoCase(BodyPart.Type))
                finalType = BodyPart.TypeModel().FinalType;

            string type = BodyPart.VariantTypeModel().Type;

            string extra = null;
            if (BodyPart.VariantTypeModel().FinalType.EqualsNoCase("body")
                && BodyPart.ParentBody.CalculateMobilitySpeedPenalty() is int moveSpeedPenalty
                && moveSpeedPenalty > 0)
                extra = $" {"{{r|"}{-moveSpeedPenalty} Move Speed Penalty{"}}"}";


            return new()
            {
                CardinalDescription = cardinalDescription,
                Description = BodyPart.VariantTypeModel().Description,
                Type = type,
                FinalType = finalType,
                NaturalEquipmentStats = BodyPlan.GetDefaultBehaviorString(defaultBehaviour),
                Extra = extra,
                LimbElements = BodyPlan?.Entry?.LimbElementsByType?.GetValue(type) ?? new(),
            };
        }

        public static BodyPlan.LimbTreeBranch InitializeLimbTreeBranch(this BodyPart BodyPart, BodyPlan BodyPlan)
            => InitializeLimbTreeBranch(BodyPart, null, out _)
            ;

        public static BodyPlan.LimbTreeBranch InitializeLimbTreeBranch(this BodyPart BodyPart)
            => InitializeLimbTreeBranch(BodyPart, null)
            ;

        public static IEnumerable<BodyPlan.LimbTreeBranch> GetLimbTreeBranches(
            this BodyPart BodyPart,
            Func<
                BodyPart,
                Func<string, string>,
                Func<BodyPart, BodyPlan.LimbTreeBranch>,
                Dictionary<int, char>, int, int, int, BodyPlan.LimbTreeBranch
                > Selector,
            Func<string, string> IndentProc,
            Func<BodyPart, BodyPlan.LimbTreeBranch> BodyPartProc,
            Dictionary<int, char> IndentDrawing,
            int DepthLimit = int.MaxValue,
            int Depth = 0,
            int SiblingOrdinal = 1,
            int SiblingCardinal = 1,
            Predicate<BodyPart> Filter = null
            )
        {
            IndentDrawing ??= new();
            if (BodyPart == null)
                yield break;

            yield return Selector(BodyPart, IndentProc, BodyPartProc, IndentDrawing, Depth, SiblingOrdinal, SiblingCardinal);
            if (Depth >= DepthLimit)
                yield break;

            Filter ??= (bp => true);
            if (BodyPart.LoopSubparts().Where(Filter.Invoke) is IEnumerable<BodyPart> subparts)
            {
                int children = subparts.Count();
                int child = 0;
                if (subparts.SelectMany(
                    selector: bp => bp.GetLimbTreeBranches(
                        Selector: Selector,
                        IndentProc: IndentProc,
                        BodyPartProc: BodyPartProc,
                        IndentDrawing: IndentDrawing,
                        DepthLimit: DepthLimit,
                        Depth: Depth + 1,
                        SiblingOrdinal: ++child,
                        SiblingCardinal: children,
                        Filter: Filter)) is IEnumerable<BodyPlan.LimbTreeBranch> subResults)
                    foreach (BodyPlan.LimbTreeBranch subResult in subResults)
                        yield return subResult;
            }
        }
        public static BodyPlan.LimbTreeBranch GetLimbTreeBranch(
            this BodyPart BodyPart,
            Func<string, string> IndentProc,
            Func<BodyPart, BodyPlan.LimbTreeBranch> BodyPartProc,
            Dictionary<int, char> IndentDrawing,
            int Depth,
            int Ordinal,
            int Cardinal
            )
        {
            var indent = Event.NewStringBuilder();

            IndentDrawing[Depth] = Ordinal == Cardinal ? NBSP : VERT;

            for (int i = 0; i < Depth; ++i)
            {
                indent
                    .Append(IndentDrawing.GetValueOrDefault(i, NBSP))
                    .Append(NBSP)
                    ;

                if (i == 0)
                    indent.Append(NBSP);
            }

            if (Depth == 0)
                indent
                    .Append(NBSP)
                    .Append(RTRNG)
                    .Append(NBSP)
                    ;
            else
            if (Depth != 0)
                indent
                    .Append(Ordinal == Cardinal ? UANR : VERR)
                    .Append(HRZT)
                    ;

            var limbTreeBranch = BodyPartProc(BodyPart);
            limbTreeBranch.Indent = IndentProc(Event.FinalizeString(indent));
            return limbTreeBranch;
        }

        public static IEnumerable<BodyPlan.LimbTreeBranch> GetLimbTree(
            this Body Body,
            Func<string, string> IndentProc,
            Func<BodyPart, BodyPlan.LimbTreeBranch> BodyPartProc,
            int DepthLimit = int.MaxValue,
            bool Treat0DepthPartsAsRoot = true
            )
        {
            if (Treat0DepthPartsAsRoot)
            {
                if (Body
                    ?.LoopParts()
                    ?.Where(IsEqualDepthToRoot) is IEnumerable<BodyPart> bodyParts)
                {
                    foreach (var bodyPart in bodyParts)
                    {
                        if (bodyPart.GetLimbTreeBranches(
                            Selector: GetLimbTreeBranch,
                            IndentProc: IndentProc ?? (s => "{{y|" + s + "}}"),
                            BodyPartProc: BodyPartProc ?? InitializeLimbTreeBranch,
                            IndentDrawing: new Dictionary<int, char>(),
                            DepthLimit: DepthLimit,
                            Filter: IsNotEqualDepthToRoot) is IEnumerable<BodyPlan.LimbTreeBranch> limbTreeBranches)
                            foreach (var limbTreeBranch in limbTreeBranches)
                                yield return limbTreeBranch;
                    }
                }
            }
            else
            {
                if (Body
                    ?.GetBody()
                    .GetLimbTreeBranches(
                        Selector: GetLimbTreeBranch,
                        IndentProc: IndentProc ?? (s => "{{y|" + s + "}}"),
                        BodyPartProc: BodyPartProc ?? InitializeLimbTreeBranch,
                        IndentDrawing: new Dictionary<int, char>(),
                        DepthLimit: DepthLimit)
                    is IEnumerable<BodyPlan.LimbTreeBranch> limbTreeBranches)
                    foreach (var limbTreeBranch in limbTreeBranches)
                        yield return limbTreeBranch;
            }
        }

        public static bool IsShader(this string String)
            => ConsoleLib.Console.MarkupShaders.Shaders.Any(s => s.GetName() == String);

        public static bool IsColor(this string String)
            => ConsoleLib.Console.MarkupShaders.Colors.Any(s => s.GetName() == String)
            || ((String?.Length ?? 0) == 1
                && ConsoleLib.Console.MarkupShaders.GetSolidColor(String[0]) != null);

        public static string ShaderColorOrNull(this string String)
            => String.IsShader()
                || String.IsColor()
            ? String
            : null;

        public static bool TryGetTagValueForData(
            this GameObjectBlueprint DataBucket,
            string TagName,
            out string Value
            )
		{
			using Indent indent = new(1);
            /*
			Debug.LogMethod(indent,
				ArgPairs: new Debug.ArgPair[]
				{
					Debug.Arg(nameof(TagName), TagName ?? "NO_TAG"),
				});
            */
			Value = null;
            if (DataBucket == null
                || !DataBucket.TryGetTag(TagName, out Value)
                || Value.EqualsNoCase(REMOVE_TAG))
			{
				if (!Value.IsNullOrEmpty())
					Debug.YehNah(TagName, Value ?? "NO_VALUE", Value?.EqualsNoCase(REMOVE_TAG) is not false, Indent: indent[1]);
				Value = null;
				return false;
            }
            if (!Value.IsNullOrEmpty())
			    Debug.YehNah(TagName, Value ?? "NO_VALUE", !Value.IsNullOrEmpty(), Indent: indent[1]);
            return !Value.IsNullOrEmpty();
        }

        public static GameObjectBlueprint AssignStringFieldFromTag(
            this GameObjectBlueprint DataBucket,
            string TagName,
            ref string Field
            )
		{
            /*
			using Indent indent = new(1);
			Debug.LogMethod(indent,
				ArgPairs: new Debug.ArgPair[]
				{
					Debug.Arg(nameof(TagName), TagName ?? "NO_TAG"),
					Debug.Arg(nameof(Field), Field ?? "NO_FIELD"),
				});*/
			DataBucket.TryGetTagValueForData(TagName, out Field);
            return DataBucket;
        }

        public static GameObjectBlueprint AssignStringFieldFromXTag(
            this GameObjectBlueprint DataBucket,
            string XTagName,
            string XTagKey,
            ref string Field
            )
		{
            /*
			using Indent indent = new(1);
			Debug.LogMethod(indent,
				ArgPairs: new Debug.ArgPair[]
				{
					Debug.Arg(nameof(XTagName), XTagName ?? "NO_TAG"),
					Debug.Arg(nameof(XTagKey), XTagKey ?? "NO_KEY"),
					Debug.Arg(nameof(Field), Field ?? "NO_FIELD"),
				});
            */
            if (DataBucket != null
                && (Field = DataBucket.GetxTag(XTagName, XTagKey, Field)).EqualsNoCase(REMOVE_TAG))
                Field = null;

            return DataBucket;
        }

        public static void AssignStringFieldFromXTag(
            this Dictionary<string, string> XTag,
            string Key,
            ref string Field
            )
        {
            if (!XTag.IsNullOrEmpty()
                && (Field = XTag.GetValue(Key, Field)).EqualsNoCase(REMOVE_TAG))
                Field = null;

			using Indent indent = new(1);
			Debug.LogMethod(indent,
				ArgPairs: new Debug.ArgPair[]
				{
					Debug.Arg(nameof(XTag), XTag?.Count ?? -1),
					Debug.Arg(nameof(Key), Key ?? "NO_KEY"),
					Debug.Arg(nameof(Field), Field ?? "NO_FIELD"),
				});
		}

        public static bool TryGetXtag(
            this GameObjectBlueprint DataBucket,
            string XTagName,
            out Dictionary<string, string> XTag
            )
            => (XTag = DataBucket?.xTags?.GetValue(XTagName)) != null;

        public static int Count<T>(this IEnumerable<T> Source, Predicate<T> Basis)
        {
            int count = 0;
            if (Basis != null)
            {
                foreach (var element in Source)
                    if (Basis(element))
                        count++;
            }
            return count;
        }

        public static IEnumerable<string> SubstringsOfLength(this string String, int Length)
        {
            if (String.IsNullOrEmpty())
                yield break;

            for (int i = 0; i < String.Length - Length; i++)
                yield return String[i..(i + Length)];
        }

        public static Dictionary<string, string> GetTags(
            this GameObjectBlueprint DataBucket,
            Predicate<string> WithName,
            Predicate<string> WithValue
            )
        {
			Dictionary<string, string> tags = null;

            if (DataBucket?.Tags == null)
                return null;

			using Indent indent = new(1);
			Debug.LogMethod(indent);
			foreach ((var tagName, var tagValue) in DataBucket.Tags)
			{

                if (WithName != null && !WithName(tagName))
				{
                    Debug.CheckNah(tagName, $"{tagValue} | Not {nameof(WithName)}", Indent: indent[1]);
					continue;
				}

                if (WithValue != null && !WithValue(tagValue))
				{
					Debug.CheckNah(tagName, $"{tagValue} | Not {nameof(WithValue)}", Indent: indent[1]);
					continue;
				}

				Debug.CheckYeh(tagName, tagValue, Indent: indent[1]);
				tags ??= new();
				tags.Add(tagName, tagValue);
			}
            return tags;
        }

        public static Dictionary<string, string> GetTagsStartingWith(
            this GameObjectBlueprint DataBucket,
            string String
            )
            => DataBucket.GetTags(s => s.StartsWith(String), null);

        public static Dictionary<string, string> GetSubTagsStartingWith(
            this GameObjectBlueprint DataBucket,
            string String
            )
        {
            string superTag = $"{String}.";
            int superTagLength = superTag.Length;
			if (DataBucket.GetTagsStartingWith(superTag) is Dictionary<string, string> tags)
            {
                var subTags = new Dictionary<string, string>();
                foreach ((var tagName, var tagValue) in tags)
                    subTags.Add(tagName[superTagLength..], tagValue);
                return subTags;
			}
            return null;
        }

        public static bool IsOption(this string String)
            => !String.IsNullOrEmpty()
            && XRL.UI.Options.HasOption(String);

        public static string GetOption(this string OptionID)
            => XRL.UI.Options.GetOption(OptionID);

        public static bool TryGetOption(this string OptionID, out string OptionState)
            => !(OptionState = OptionID.GetOption()).IsNullOrEmpty();

        public static bool IsMechanical(this Anatomy Anatomy)
            => Anatomy?.Category == BodyPartCategory.MECHANICAL;

        public static string PairString<TKey, TValue>(this KeyValuePair<TKey, TValue> KVP)
            => $"{KVP.Key}: {KVP.Value}";

        public static Dictionary<string, string> ClearNoInherits(this Dictionary<string, string> Tags)
        {
            if (!Tags.IsNullOrEmpty())
                Tags.RemoveAll(kvp => kvp.Value?.EqualsNoCase(NO_INHERIT_TAG) is true);

            return Tags;
        }

        public static Dictionary<string, string> ClearRemoves(this Dictionary<string, string> Tags)
        {
            if (!Tags.IsNullOrEmpty())
                Tags.RemoveAll(kvp => kvp.Value?.EqualsNoCase(REMOVE_TAG) is true);

            return Tags;
        }

        public static string ColorIf(this string String, string Color, bool Condition)
        {
            if (!Condition
                || Color.IsNullOrEmpty())
                return String;
            return $"{"{{"}{Color}|{String}{"}}"}";
        }

		public static int AddRange<T>(this HashSet<T> Set, IEnumerable<T> Range)
		{
			int count = 0;
			foreach (var item in Range)
			{
				if (Set.Add(item))
					count++;
			}
			return count;
		}

		public static List<GameObjectBlueprint> SafelyGetBlueprintsInheritingFrom(this GameObjectFactory Factory, string Name, bool ExcludeBase = true)
		{
			List<GameObjectBlueprint> outputList = new();
			foreach (GameObjectBlueprint blueprint in Factory.BlueprintList)
				if (blueprint.InheritsFromSafe(Name)
                    && (!ExcludeBase
                        || !blueprint.IsBaseBlueprint()))
					outputList.Add(blueprint);

			return outputList;
		}
		public static List<string> InheritanceRoots => new()
		{
			nameof(Object),
			"SultanMuralController",
		};
		public static bool InheritsFromSafe(this GameObjectBlueprint GameObjectBlueprint, string what)
		{
			string parentBlueprint = GameObjectBlueprint.Inherits;
			while (!parentBlueprint.IsNullOrEmpty())
			{
				if (parentBlueprint == what)
					return true;

				string inherits = parentBlueprint;
				parentBlueprint = GameObjectFactory.Factory?.GetBlueprintIfExists(parentBlueprint)?.Inherits;
				if (parentBlueprint.IsNullOrEmpty()
					&& !InheritanceRoots.Contains(inherits))
				{
					MetricsManager.LogModWarning(ThisMod,
						$"{nameof(Extensions)}.{nameof(InheritsFromSafe)}(\"{what}\"):" +
						$" bluprint ancestor \"{inherits}\" does not exist in blueprint list." +
						$" The first mention of this blueprint in this log should reveal the mod with this inheritance issue.");
				}
			}
			return false;
		}
	}
}
