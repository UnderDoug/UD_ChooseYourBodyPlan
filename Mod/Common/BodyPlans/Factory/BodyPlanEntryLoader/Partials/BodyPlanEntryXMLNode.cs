using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using XRL;
using XRL.Collections;

using static UD_BodyPlan_Selection.Mod.BodyPlans.TextElement;

namespace UD_BodyPlan_Selection.Mod.BodyPlans.Factory
{
    public partial class BodyPlanLoader
    {
        public static Dictionary<string, List<string>> KnownChildNodesByNodeName = new()
        {
            { 
                "symbol", new()
                {
                    "color",
                    "value",
                }
            },
            { 
                "textElement", new() 
                {
                    "descriptionBefore",
                    "descriptionAfter",
                    "summaryBefore",
                    "summaryAfter", 
                    "symbol", 
                    "symbols", 
                } 
            },
            { 
                "category", new() 
                {
                    "displayName",
                    "shader",
                    "color",
                } 
            },
            { 
                "shader", new() 
                {
                    "type",
                    "colors",
                    "color",
                } 
            },
            { 
                "bodyplan", new() 
                {
                    "tag",
                    "base",
                    "textElement",
                    "dynamic",
                    "optionID",
                    "render",
                    "transformation",
                } 
            },
            { "tag", new() },
            { "base", new() },
            { "dynamic", new() },
            { "optionID", new() },
            {
                "render", new()
                {
                    "displayName",
                    "tile",
                    "renderString",
                    "colorString",
                    "tileColor",
                    "detailColor",
                    "hFlip",
                }
            },
            {
                "transformation", new()
                {
                    "property",
                    "mutations",
                    "render",
                    "species",
                }
            }
        };

        public static Dictionary<string, List<string>> KnownAttributesByNodeName = new()
        {
            {
                "symbol", new()
                {
                    "Name",
                    "Color",
                    "Value",
                    "Load",
                }
            },
            {
                "textElement", new()
                {
                    "Name",
                    "Symbol",
                    "Load",
                }
            },
            {
                "category", new()
                {
                    "Name",
                    "DisplayName",
                    "Shader",
                    "Color",
                    "Load",
                }
            },
            {
                "shader", new()
                {
                    "Type",
                    "Colors",
                    "Color",
                    "Load",
                }
            },
            {
                "bodyplan", new()
                {
                    "Name",
                    "DisplayName",
                    "Category",
                    "OptionID",
                    "Load",
                }
            },
            {
                "tag", new()
                {
                    "Name",
                    "Value",
                }
            },
            { "base", new() },
            {
                "dynamic", new()
                {
                    "When",
                    "Is",
                    "Not",
                    "Mixin",
                }
            },
            {
                "optionID", new()
                {
                    "Value",
                }
            },
            {
                "render", new()
                {
                    "DisplayName",
                    "Tile",
                    "RenderString",
                    "ColorString",
                    "TileColor",
                    "DetailColor",
                    "HFlip",
                }
            },
            {
                "transformation", new()
                {
                    "Property",
                    "Mutations",
                    "Tile",
                    "RenderString",
                    "TileColor",
                    "DetailColor",
                    "Species",
                    "UseBodyPlanRender",
                }
            }
        };

        public class BodyPlanEntryXMLNode
        {
            public string NodeName;
            public string Name;
            public string Load;

            public List<string> TextLines;

            public List<string> KnownChildNodes => KnownChildNodesByNodeName?.GetValue(NodeName);
            public Rack<BodyPlanEntryXMLNode> Children;
            public List<string> KnownAttributes => KnownAttributesByNodeName?.GetValue(NodeName);
            public Dictionary<string, string> Attributes;

            public BodyPlanEntryXMLNode(string NodeName)
            {
                this.NodeName = NodeName;
                Load = "Replace";
                Children = new();
                Attributes = new();
            }

            public static BodyPlanEntryXMLNode ReadNode(XmlDataHelper Reader)
            {
                var childNode = new BodyPlanEntryXMLNode(Reader.Name);
                if (Reader.HasAttributes)
                {
                    string baseURI = Reader.SanitizedBaseURI();

                    Reader.MoveToFirstAttribute();
                    do
                    {
                        if (!childNode.KnownAttributes.IsNullOrEmpty()
                            && !childNode.KnownAttributes.Contains(Reader.Name))
                            HandleWarning(Reader.modInfo, $"{Reader.SanitizedBaseURI()}: Unknown attribute {Reader.Name} in node {childNode.NodeName} on line {Reader.LineNumber}.");

                        if (Reader.Name == "Name")
                            childNode.Name = Reader.Value;

                        if (Reader.Name == "Load")
                            childNode.Load = Reader.Value;

                        childNode.Attributes[Reader.Name] = Reader.Value;
                    }
                    while (Reader.MoveToNextAttribute());
                    Reader.MoveToElement();
                }

                if (Reader.NodeType == XmlNodeType.EndElement
                    || Reader.IsEmptyElement)
                    return childNode;

                while (Reader.Read())
                {
                    if (Reader.NodeType == XmlNodeType.EndElement)
                    {
                        if (Reader.Name != ""
                            && Reader.Name != childNode.NodeName)
                            throw new Exception($"Unexpected end node for {Reader.Name}");
                        return childNode;
                    }

                    if (Reader.NodeType == XmlNodeType.Element)
                    {
                        if (!childNode.KnownChildNodes.IsNullOrEmpty()
                            && !childNode.KnownChildNodes.Contains(Reader.Name))
                            HandleWarning(Reader.modInfo, $"{Reader.SanitizedBaseURI()}: Unknown child node {Reader.Name} in node {childNode.NodeName} on line {Reader.LineNumber}.");

                        childNode.AddChild(ReadNode(Reader));
                    }

                    if (Reader.NodeType == XmlNodeType.Comment)
                        continue;

                    if (Reader.NodeType == XmlNodeType.Text)
                    {
                        if (Reader.ReadString().Split('\n', StringSplitOptions.RemoveEmptyEntries) is string[] textLines)
                        {
                            childNode.TextLines ??= new();
                            for (int i = 0; i < textLines.Length; i++)
                            {
                                if (textLines[i].Replace("\r", "") is string textLine)
                                    childNode.TextLines.Add(textLine);
                            }
                        }
                    }
                }
                return childNode;
            }

            public bool SameAs(BodyPlanEntryXMLNode Other)
                => NodeName == Other.NodeName
                && !Name.IsNullOrEmpty()
                && Name == Other.Name
                ;

            protected void AddChild(BodyPlanEntryXMLNode ChildNode)
            {
                if (ChildNode != null)
                {
                    if (Children.FirstOrDefault(ChildNode.SameAs) is BodyPlanEntryXMLNode existingChildNode)
                    {
                        Children.Remove(existingChildNode);
                        if (ChildNode.Load != "Merge")
                        {
                            existingChildNode.Merge(ChildNode);
                            ChildNode = existingChildNode;
                        }
                    }
                    Children.Add(ChildNode);
                }
            }

            public virtual void Merge(BodyPlanEntryXMLNode Other)
            {
                NodeName = Other.NodeName;
                Name = Other.Name;

                TextLines.AddRangeIf(Other.TextLines, s => !TextLines.Contains(s));

                Utils.Merge(This: Other.Attributes, Into: ref Attributes);

                foreach (var childNode in Other.Children)
                {
                    AddChild(childNode);
                }
            }

            public BodyPlanEntryXMLNode Clone()
            {
                var clone = new BodyPlanEntryXMLNode(NodeName)
                {
                    Name = Name,
                    Load = Load,
                    TextLines = new(TextLines),
                    Attributes = new(),
                    Children = new(),
                };
                foreach ((string name, string value) in Attributes)
                    clone.Attributes.Add(name, value);

                foreach (var childNode in Children)
                    clone.Children.Add(childNode.Clone());

                return clone;
            }

            public string GetAttribute(string Name)
                => Attributes.GetValue(Name);

            public bool HasAttribute(string Name)
                => Attributes.ContainsKey(Name);

            public IEnumerable<BodyPlanEntryXMLNode> GetChildNodes(Predicate<BodyPlanEntryXMLNode> Filter)
            {
                foreach (var child in Children)
                    if (Filter == null
                        || Filter(child))
                        yield return child;
            }

            public IEnumerable<BodyPlanEntryXMLNode> GetChildNodes(string NodeName)
                => GetChildNodes(n => n.NodeName == NodeName);

            public IEnumerable<BodyPlanEntryXMLNode> GetNamedChildNodes()
                => GetChildNodes(n => !n.Name.IsNullOrEmpty());

        }
    }
}
