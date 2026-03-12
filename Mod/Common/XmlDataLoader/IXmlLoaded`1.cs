using System;
using System.Collections.Generic;
using System.Text;

using XRL;

namespace UD_BodyPlan_Selection.Mod.XML
{
    public interface IXmlLoaded<T>
        where T : IXmlLoaded<T>, new()
    {
        IXmlFactory<T> Factory { get; }

        XmlMetaData<T> XmlMetaData { get; }

        XmlDataLoader<T>.XmlData LoadData(XmlDataHelper Reader)
            => XmlDataLoader<T>.XmlNode.ReadNode(Reader, Factory.GetXmlDataLoader()) as XmlDataLoader<T>.XmlData;

        XmlDataLoader<T>.XmlNode LoadChild(XmlDataHelper Reader)
            => XmlDataLoader<T>.XmlNode.ReadNode(Reader, Factory.GetXmlDataLoader());
    }
}
