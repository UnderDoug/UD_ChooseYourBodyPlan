using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using XRL;
using XRL.Collections;

namespace UD_BodyPlan_Selection.Mod.XML
{
    public partial class XmlDataLoader<T>
        where T : IXmlLoaded<T>, new()
    {
        public class XmlData : XmlNode
        {
            public string Inherits;

            public ModInfo Mod;

            public override void AssignFromAttributesByName(XmlDataHelper Reader)
            {
                base.AssignFromAttributesByName(Reader);
                Inherits = Reader.GetAttribute("Inherits");
                Mod = Reader.modInfo;
            }

            public override bool HandleNodeTypeElement(XmlDataHelper Reader)
            {
                if (base.HandleNodeTypeElement(Reader))
                {
                    AddChild(ReadNode<XmlNode>(Reader));
                    return true;
                }
                return false;
            }

            public override void Merge(AbstractXmlNode Other)
            {
                if (Other is not XmlData other)
                    HandleError($"Attempted to merge {GetType().Name} with incompatible {Other.GetType().Name}");
                else
                {
                    if (!other.Inherits.IsNullOrEmpty())
                        Inherits = other.Inherits;

                    if (other.Mod != null)
                        Mod = other.Mod;

                    foreach (var childNode in Other.Children)
                        if (childNode is XmlNode typedChildNode)
                            AddChild(typedChildNode);

                    Other = other;
                }
                base.Merge(Other);
            }

            public override AbstractXmlNode Clone()
            {
                var clone = base.Clone() as XmlData;

                clone.Mod = Mod;
                clone.Inherits = Inherits;

                return clone;
            }
        }
    }
}
