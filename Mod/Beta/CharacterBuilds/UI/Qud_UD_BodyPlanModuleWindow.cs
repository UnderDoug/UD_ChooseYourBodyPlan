using System.Collections.Generic;

using ConsoleLib.Console;

using UnityEngine;

using XRL.Rules;
using XRL.UI;
using XRL.UI.Framework;

using ColorUtility = ConsoleLib.Console.ColorUtility;
using XRL.Collections;

namespace XRL.CharacterBuilds.Qud.UI
{
    public partial class Qud_UD_BodyPlanModuleWindow : EmbarkBuilderModuleWindowPrefabBase<Qud_UD_BodyPlanModule, CategoryMenusScroller>
    {
        public override void RandomSelectionNoUI()
            => SelectAnatomy(Stat.Roll(0, module.AnatomyChoices.Count - 1));

        public bool SkippingUIUpdates()
            => module.builder.SkippingUIUpdates;
    }
}
