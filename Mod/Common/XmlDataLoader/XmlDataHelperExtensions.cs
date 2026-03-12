using System;
using System.Collections.Generic;
using System.Text;

using XRL;

namespace UD_BodyPlan_Selection.Mod.XML
{
    public static class XmlDataHelperExtensions
    {
        public static string SanitizedBaseURI(this XmlDataHelper Reader)
            => DataManager.SanitizePathForDisplay(Reader.BaseURI);

        public static string FileLinePos(this XmlDataHelper Reader)
            => $"File: {Reader.SanitizedBaseURI()}, Line: {Reader.LineNumber}:{Reader.LinePosition}";
    }
}
