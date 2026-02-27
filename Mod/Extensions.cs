using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

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
    }
}
