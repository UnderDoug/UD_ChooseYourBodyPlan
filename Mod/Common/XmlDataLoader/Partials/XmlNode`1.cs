using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using XRL;
using XRL.Collections;

using static UD_BodyPlan_Selection.Mod.BodyPlans.TextElement;

namespace UD_BodyPlan_Selection.Mod.XML
{
    public partial class XmlDataLoader<T>
        where T : IXmlLoaded<T>, new()
    {
        public class XmlNode : AbstractXmlNode
        {
            public enum LoadType
            {
                Replace,
                MergeIfExists,
                Merge,
            }

            protected XmlDataLoader<T> Loader;

            public string Name;
            public LoadType Load;

            private T Instance;

            protected XmlMetaData<T> MetaData => Instance?.XmlMetaData;

            public bool IsUnique => MetaData?.IsUnique ?? false;

            public XmlNode()
            {
                NodeName = null;
                Load = LoadType.Replace;
                Children = new();
                Attributes = new();
            }

            public XmlNode(XmlDataLoader<T> Loader)
                : this()
            { }

            protected XmlNode(XmlDataHelper Reader, XmlDataLoader<T> Loader)
                : this(Loader)
            {
                NodeName = Reader.Name;
                ReadNodeInternal(Reader);
            }
            protected XmlNode(XmlNode Source, XmlDataLoader<T> Loader)
                : this(Loader)
            {
                NodeName = Source.NodeName;
                Name = Source.Name;
                Load = Source.Load;
                Children = new(Source.Children);
                Attributes = new(Source.Attributes);
            }

            public static TNode ReadNode<TNode>(XmlDataHelper Reader, XmlDataLoader<T> Loader)
                where TNode : XmlNode, new()
                => ReadNode(
                    Node: new TNode() 
                    {
                        NodeName = Reader.Name

                    },
                    Reader) as TNode;

            public static XmlNode ReadNode(XmlDataHelper Reader, XmlDataLoader<T> Loader)
                => ReadNode<XmlNode>(Reader, Loader);

            public override void AssignFromAttributesByName(XmlDataHelper Reader)
            {
                Name = Reader.GetAttribute("Name");
                Load = Reader.GetAttribute("Load") switch
                {
                    "MergeIfExists" => LoadType.MergeIfExists,
                    "Merge" => LoadType.Merge,
                    _ => LoadType.Replace,
                };
            }

            public override bool HandleNodeAttribute(XmlDataHelper Reader)
            {
                if (!KnownAttributes.IsNullOrEmpty()
                    && !KnownAttributes.Contains(Reader.Name))
                    HandleWarning($"{Reader.SanitizedBaseURI()}: Unknown attribute {Reader.Name} in node {NodeName} on line {Reader.LineNumber}.");

                Attributes[Reader.Name] = Reader.Value;

                return true;
            }

            public override void HandleNodeTypeEndElement(XmlDataHelper Reader)
            {
                base.HandleNodeTypeEndElement(Reader);
            }

            public override bool HandleNodeTypeElement(XmlDataHelper Reader)
            {
                if (!KnownChildNodes.IsNullOrEmpty()
                    && !KnownChildNodes.Contains(Reader.Name))
                    HandleWarning($"{Reader.SanitizedBaseURI()}: Unknown child node {Reader.Name} in node {NodeName} on line {Reader.LineNumber}.");

                return base.HandleNodeTypeElement(Reader);
            }

            public override bool HandleNodeTypeText(XmlDataHelper Reader)
            {
                return base.HandleNodeTypeText(Reader);
            }

            public override bool HandleNodeTypeComment(XmlDataHelper Reader)
            {
                return base.HandleNodeTypeComment(Reader);
            }

            public override bool SameAs(AbstractXmlNode Other)
            {
                if (Other is not XmlNode<T> typedNode)
                    return false;

                if (Name != typedNode.Name)
                    return false;

                return true;
            }

            protected override void AddChild<TNode>(TNode ChildNode)
            {
                if (Load > LoadType.Replace
                    && ChildNode is XmlNode<T> typedChildNode)
                {
                    if (Children.FirstOrDefault(ChildNode.SameAs) is XmlNode<T> existingTypedChildNode)
                    {
                        Children.Remove(existingTypedChildNode);
                        if (typedChildNode.Load > LoadType.Replace)
                        {
                            existingTypedChildNode.Merge(ChildNode);
                            ChildNode = existingTypedChildNode as TNode;
                        }
                    }
                }
                base.AddChild(ChildNode);
            }

            public override void Merge(AbstractXmlNode Other)
            {
                if (Other is not XmlNode<T> typedOther)
                    HandleError($"Attempted to merge {GetType().Name} with incompatible {Other.GetType().Name}");
                else
                {
                    Name = typedOther.Name;
                    Load = typedOther.Load;

                    TextLines.AddRangeIf(typedOther.TextLines, s => !TextLines.Contains(s));

                    foreach (var childNode in typedOther.Children)
                        AddChild(childNode.Clone());

                    Other = typedOther;
                }
                base.Merge(Other);
            }

            public override AbstractXmlNode Clone()
            {
                var clone = Activator.CreateInstance(GetType()) as XmlNode<T>;

                clone.Name = Name;
                clone.Load = Load;

                return clone;
            }

            public IEnumerable<XmlNode<T>> GetNamedChildNodes()
            {
                foreach (var child in GetChildNodes(n => n is XmlNode<T> typedN && !typedN.Name.IsNullOrEmpty()))
                    if (child is XmlNode<T> typedChild)
                        yield return typedChild;
            }
        }
    }
}
