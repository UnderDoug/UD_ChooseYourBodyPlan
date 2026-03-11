using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using XRL;
using XRL.Collections;

namespace UD_BodyPlan_Selection.Mod.BodyPlans.Factory
{
    public partial class BodyPlanLoader
    {
        public class BodyPlanEntryXMLData : BodyPlanEntryXMLNode
        {
            public string Inherits;

            public ModInfo Mod;

            public BodyPlanEntryXMLData(string NodeName)
                : base(NodeName)
            {
            }

            public static BodyPlanEntryXMLData ReadObjectNode(XmlDataHelper Reader)
            {
                var bodyPlanData = new BodyPlanEntryXMLData(Reader.Name);
                int num = 0;

                bodyPlanData.Name = Reader.GetAttribute("Name");
                bodyPlanData.Inherits = Reader.GetAttribute("Inherits");
                bodyPlanData.Load = Reader.GetAttribute("Load");
                bodyPlanData.Mod = Reader.modInfo;

                if (Reader.NodeType == XmlNodeType.EndElement
                    || Reader.IsEmptyElement)
                    return bodyPlanData;

                while (Reader.Read())
                {
                    if (Reader.NodeType == XmlNodeType.Comment
                        || Reader.NodeType == XmlNodeType.Text)
                        continue;

                    if (Reader.NodeType == XmlNodeType.EndElement
                        && Reader.Name == bodyPlanData.NodeName)
                        return bodyPlanData;

                    if (Reader.NodeType == XmlNodeType.Element)
                    {
                        string name = Reader.Name;
                        if (!KnownNodes.ContainsKey(name) && !name.StartsWith("xtag"))
                        {
                            handleError($"{DataManager.SanitizePathForDisplay(Reader.BaseURI)}: Unknown object element {Reader.Name} at line {Reader.LineNumber}");
                        }
                        ObjectBlueprintXMLChildNode objectBlueprintXMLChildNode = ObjectBlueprintXMLChildNode.ReadChildNode(Reader);
                        if (!bodyPlanData.Children.ContainsKey(name))
                        {
                            bodyPlanData.Children[name] = new ObjectBlueprintXMLChildNodeCollection();
                        }
                        if (name.EqualsNoCase("mixin") && objectBlueprintXMLChildNode.Attributes.TryAdd("Priority", num.ToString()))
                        {
                            num++;
                        }
                        bodyPlanData.Children[name].Add(objectBlueprintXMLChildNode, Reader);
                    }
                    else
                    {
                        handleError($"{DataManager.SanitizePathForDisplay(Reader.BaseURI)}: Unknown problem reading object: {Reader.NodeType}");
                    }
                }
                return bodyPlanData;
            }

            public override void Merge(BodyPlanEntryXMLNode Other)
            {
                if (Other is not BodyPlanEntryXMLData other)
                {
                    HandleError(Mod, $"Aborting attempt to merge {GetType().Name} with incompatible {Other.GetType().Name}");
                    return;
                }

                if (!other.Inherits.IsNullOrEmpty())
                    Inherits = other.Inherits;

                if (other.Mod != null)
                    Mod = other.Mod;

                base.Merge(Other);
            }
        }
    }
}
