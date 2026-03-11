using System;
using System.Collections.Generic;
using System.Text;

using XRL;

namespace UD_BodyPlan_Selection.Mod.BodyPlans.Factory
{
    public partial class BodyPlanLoader
    {
        protected static Action<ModInfo, object> HandleError;
        protected static Action<ModInfo, object> HandleWarning;

        public BodyPlanLoader()
        {
            HandleError = MetricsManager.LogModError;
            HandleWarning = MetricsManager.LogModWarning;
        }
    }
}
