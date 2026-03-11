using System;
using System.Collections.Generic;
using System.Text;

using XRL;

namespace UD_BodyPlan_Selection.Mod.BodyPlans.Factory
{
    public static class XmlDataHelperExtensions
    {
        public static string SanitizedBaseURI(this XmlDataHelper Reader)
            => DataManager.SanitizePathForDisplay(Reader.BaseURI);
    }
}
