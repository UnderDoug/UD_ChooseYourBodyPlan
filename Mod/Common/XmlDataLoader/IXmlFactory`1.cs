using System;
using System.Collections.Generic;
using System.Text;

namespace UD_BodyPlan_Selection.Mod.XML
{
    public interface IXmlFactory<T>
        where T : IXmlLoaded<T>, new()
    {
        XmlDataLoader<T> GetXmlDataLoader();

        T LoadFromData(XmlDataLoader<T>.XmlData XmlData);


    }
}
