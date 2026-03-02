using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using XRL;
using XRL.Collections;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;

using static UD_BodyPlan_Selection.Mod.Utils;

namespace UD_BodyPlan_Selection.Mod
{
    public static class Extensions
    {
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

        public static bool LogReturning(this bool Return, string Message)
            => Utils.LogReturnBool(Return, Message);

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

        public static StringBuilder AppendLines(this StringBuilder SB, int Count)
            => Count.Aggregate(SB, (a, n) => a.AppendLine())
            ;

        public static bool EndsWithAny(this string String, params string[] Values)
            => Values.IsNullOrEmpty()
            || Values.Any(s => String.EndsWith(s));
    }
}
