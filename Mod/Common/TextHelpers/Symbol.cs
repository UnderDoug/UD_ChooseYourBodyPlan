using System;
using System.Collections.Generic;
using System.Text;

using UD_ChooseYourBodyPlan.Mod.Logging;

namespace UD_ChooseYourBodyPlan.Mod.TextHelpers
{
    public struct Symbol
    {
        public string Name;
        public char Color;
        public string Value;

        public Symbol(string Name, char Color, string Value)
        {
            this.Name = Name;
            this.Color = Color;
            this.Value = Value;
        }
        public Symbol(KeyValuePair<string, string> XTagEntry)
            : this()
        {
            Name = XTagEntry.Key;
            if (!XTagEntry.Value.Contains("::"))
                Value = XTagEntry.Value;
            else
            {
                if (XTagEntry.Value.Split("::") is string[] pair)
                {
                    if (pair.Length > 1)
                    {
                        if (pair[0] is string dualColorString
                            && !dualColorString.IsNullOrEmpty())
                            Color = dualColorString[0];

                        if (pair[1] is string dualValueString
                            && !dualValueString.IsNullOrEmpty())
                            Value = dualValueString;
                    }
                    else
                    if (pair.Length == 1)
                    {
                        if (pair[0] is string singleValueString
                            && !singleValueString.IsNullOrEmpty())
                            Value = singleValueString;
                    }
                }
            }
            using Indent indent = new(1);
            Debug.LogCaller(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                        Debug.Arg(Name ?? "NO_NAME", $"{Color}|{Value}"),
                });
        }

        public override readonly string ToString()
            => Color != default
            ? "{{" + $"{Color}|{Value}" + "}}"
            : Value.ToString()
            ;

        public readonly string DebugString()
            => $"{Name}|{this}";
    }
}
