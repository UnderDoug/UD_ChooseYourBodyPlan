using System;
using System.Collections.Generic;
using System.Text;

using XRL;

using UD_BodyPlan_Selection.Mod;

namespace UD_BodyPlan_Selection.Mod.BodyPlans.Factory
{
    public partial class BodyPlanLoader
    {
        public class BodyPlanXMLChildNodeCollection
        {
            public Dictionary<string, BodyPlanEntryXMLNode> Named;

            public List<BodyPlanEntryXMLNode> Unnamed;

            public void Add(BodyPlanEntryXMLNode node, XmlDataHelper reader)
            {
                if (node.Name.IsNullOrEmpty())
                {
                    Unnamed ??= new List<BodyPlanEntryXMLNode>(1);
                    Unnamed.Add(node);
                    return;
                }
                Named ??= new Dictionary<string, BodyPlanEntryXMLNode>(1);
                if (Named.ContainsKey(node.Name) && reader != null)
                {
                    HandleError(reader.modInfo, $"{reader.SanitizedBaseURI()}: Duplicate {node.NodeName} Name='{node.Name}' found at line {reader.LineNumber}");
                    Named[node.Name].Merge(node);
                }
                else
                {
                    Named[node.Name] = node;
                }
            }

            public BodyPlanXMLChildNodeCollection Clone()
            {
                var childNodeCollection = new BodyPlanXMLChildNodeCollection();
                if (Named != null)
                {
                    childNodeCollection.Named = new Dictionary<string, BodyPlanEntryXMLNode>(Named.Count);
                    foreach ((string nodeName, BodyPlanEntryXMLNode childNode) in Named)
                    {
                        childNodeCollection.Named[nodeName] = childNode.Clone();
                    }
                }
                if (Unnamed != null)
                {
                    childNodeCollection.Unnamed = new(Unnamed.Count);
                    foreach (ObjectBlueprintXMLChildNode item2 in Unnamed)
                    {
                        childNodeCollection.Unnamed.Add(item2.Clone());
                    }
                }
                return childNodeCollection;
            }

            public override string ToString()
            {
                string text = "";
                if (Named != null)
                {
                    text = text + "Named: " + Named.Count + "\n";
                    foreach (KeyValuePair<string, ObjectBlueprintXMLChildNode> item in Named)
                    {
                        text = text + "  [" + item.Key + " ";
                        foreach (KeyValuePair<string, string> attribute in item.Value.Attributes)
                        {
                            text = text + attribute.Key + "=\"" + attribute.Value + "\"";
                        }
                        text += "]\n";
                    }
                }
                if (Unnamed != null)
                {
                    text = text + "Unnamed: " + Unnamed.Count + "\n";
                    foreach (ObjectBlueprintXMLChildNode item2 in Unnamed)
                    {
                        text += "  [";
                        foreach (KeyValuePair<string, string> attribute2 in item2.Attributes)
                        {
                            text = text + attribute2.Key + "=\"" + attribute2.Value + "\"";
                        }
                        text += "]\n";
                    }
                }
                return text;
            }

            public void Merge(BodyPlanXMLChildNodeCollection other)
            {
                if (other.Named != null)
                {
                    if (Named == null)
                    {
                        Named = other.Named;
                    }
                    else
                    {
                        foreach (KeyValuePair<string, ObjectBlueprintXMLChildNode> item in other.Named)
                        {
                            if (Named.TryGetValue(item.Key, out var value))
                            {
                                value.Merge(item.Value);
                            }
                            else
                            {
                                Add(item.Value, null);
                            }
                        }
                    }
                }
                if (other.Unnamed != null)
                {
                    if (Unnamed == null)
                    {
                        Unnamed = other.Unnamed;
                    }
                    else
                    {
                        Unnamed.AddRange(other.Unnamed);
                    }
                }
            }
        }
    }
}
