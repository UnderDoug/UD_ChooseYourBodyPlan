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
    public static partial class Utils
    {
        public delegate T Parse<T>(string Value);

        public static Parse<T> GetVersionSafeParser<T>()
            => Startup.GetParser<T>()
            ?.ToParse()
            ;
    }
}
