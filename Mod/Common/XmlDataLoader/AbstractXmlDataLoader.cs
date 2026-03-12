using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using XRL;

using static UD_BodyPlan_Selection.Mod.Const;

namespace UD_BodyPlan_Selection.Mod.XML
{
    public abstract class AbstractXmlDataLoader
    {
        protected static Action<object> HandleError;
        protected static Action<object> HandleWarning;

        protected abstract XmlMetaData MetaData { get; }

        public IEnumerable<string> KnownChildNodes => MetaData?.GetKnownNodes();
        public IEnumerable<string> KnownAttributes => MetaData?.GetKnownAttributes();

        private Dictionary<string, Dictionary<string, AbstractXmlDataLoader.XmlData>> RawNodes;

        public AbstractXmlDataLoader()
        {
            HandleError = Utils.ThisMod.Error;
            HandleWarning = Utils.ThisMod.Warn;
            RawNodes = new();
        }

        protected void SetLoggers(ModInfo ModInfo)
        {
            if (ModInfo != Utils.ThisMod)
            {
                HandleError = ModInfo.Error;
                HandleWarning = ModInfo.Warn;
            }
            else
            {
                HandleError = Utils.ThisMod.Error;
                HandleWarning = Utils.ThisMod.Warn;
            }
        }

        public void LoadXMLRootNodes()
        {
            HandleXMLStreamsWithRoot(XML_TEXTELEMENTS, RawNodes);
            HandleXMLStreamsWithRoot(XML_BODYPLANS, RawNodes);
        }

        public void HandleXMLStreamsWithRoot(string Root, Dictionary<string, Dictionary<string, XmlData>> NodesByNodeName)
        {
            foreach (var reader in DataManager.YieldXMLStreamsWithRoot(Root))
            {
                SetLoggers(reader.modInfo);
                try
                {
                    ReadRootXML(reader, Root, NodesByNodeName);
                }
                catch (Exception message)
                {
                    MetricsManager.LogPotentialModError(reader.modInfo, message);
                }
            }
        }

        public void ReadRootXML(
            XmlDataHelper Reader,
            string RootNode,
            Dictionary<string, Dictionary<string, XmlData>> NodesByNodeName)
        {
            bool any = false;
            try
            {
                Reader.WhitespaceHandling = WhitespaceHandling.None;
                while (Reader.Read())
                {
                    if (Reader.Name == RootNode)
                    {
                        any = true;
                        ReadRootNode(Reader, XML_BODYPLANS, NodesByNodeName);
                    }
                }
            }
            catch (Exception innerException)
            {
                throw new Exception($"{Reader.FileLinePos()}", innerException);
            }
            finally
            {
                Reader.Close();
            }
            if (!any)
                HandleError($"No <{RootNode}> tag found in {Reader.SanitizedBaseURI()}");
        }

        public int ReadRootNode(XmlDataHelper Reader, string RootNode, Dictionary<string, Dictionary<string, XmlData>> NodesByNodeName)
        {
            int num = 0;
            while (Reader.Read())
            {
                if (Reader.NodeType == XmlNodeType.Element)
                {
                    string nodeName = Reader.Name;
                    if (!KnownChildNodesByNodeName.ContainsKey(nodeName))
                        HandleWarning($"{Reader.FileLinePos()}, Unknown node '{Reader.Name}', may be skipped during bake");

                    if (!NodesByNodeName.ContainsKey(nodeName)
                        || NodesByNodeName[nodeName].IsNullOrEmpty())
                        NodesByNodeName.Add(nodeName, new());

                    num += ReadNode(Reader, NodesByNodeName[nodeName]);
                    continue;
                }

                if (Reader.NodeType != XmlNodeType.Comment)
                {
                    if (Reader.Name == RootNode
                        && Reader.NodeType == XmlNodeType.EndElement)
                        return num;

                    throw new Exception($"{Reader.FileLinePos()}, Unknown node '{Reader.Name}'");
                }
            }
            return num;
        }
        public int ReadNode(XmlDataHelper Reader, Dictionary<string, XmlData> Nodes)
        {
            string nodeName = Reader.Name;
            var xMLData = AbstractXmlNode.ReadNode<XmlData>(Reader);
            string name = xMLData.Name;

            if (xMLData.Load > AbstractXmlNode.LoadType.Replace)
            {
                if (Nodes.TryGetValue(name, out var existingNode))
                    existingNode.Merge(xMLData);
                else
                if (xMLData.Load == AbstractXmlNode.LoadType.Merge)
                    HandleError($"{Reader.FileLinePos()}, Attempt to merge with {name} which is an unknown {nodeName}, node discarded");
            }
            else
                Nodes[xMLData.Name] = xMLData;

            return 1;
        }
    }
}
