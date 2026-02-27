using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XRL;
using XRL.CharacterBuilds;
using XRL.CharacterBuilds.Qud;

namespace UD_BodyPlan_Selection.Mod
{
    [HarmonyPatch]
    public static class EmbarkBuilderConfiguration_Patches
    {
        [HarmonyPatch(
            declaringType: typeof(EmbarkBuilderConfiguration),
            methodName: nameof(EmbarkBuilderConfiguration.Init))]
        [HarmonyPostfix]
        public static void Init_OrderActiveModules_Postfix(ref List<AbstractEmbarkBuilderModule> ___activeModules)
        {
            var modules = ___activeModules;
            if (modules.First(m => m is Qud_UD_BodyPlanModule) is Qud_UD_BodyPlanModule bodyPlanModule
                && modules.First(m => m is QudMutationsModule) is QudMutationsModule mutationsModule)
            {
                modules.Remove(bodyPlanModule);
                modules.Insert(modules.IndexOf(mutationsModule) + 1, bodyPlanModule);
            }
        }
    }
}
