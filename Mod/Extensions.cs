using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using XRL;
using XRL.Collections;
using XRL.World;
using XRL.World.Anatomy;

namespace UD_BodyPlan_Selection.Mod
{
    public static class Extensions
    {
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
        {
            if (Times < 1)
                return null;

            string output = "";

            for (int i = 0; i < Times; i++)
                output += @string;

            return output;
        }
        public static string ThisManyTimes(this char @char, int Times = 1)
            => @char.ToString().ThisManyTimes(Times);

        public static string CallChain(this string String, params string[] Calls)
            => Calls.Aggregate(String, (a, n) => a + "." + n);

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
    }
}
