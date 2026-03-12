using System;
using System.Collections.Generic;
using System.Text;

using XRL;
using XRL.World;

using static UD_BodyPlan_Selection.Mod.Const;
using static UD_BodyPlan_Selection.Mod.BodyPlans.TextElement;
using XRL.UI;
using UD_BodyPlan_Selection.Mod.XML;
using static UD_BodyPlan_Selection.Mod.BodyPlans.BodyPlanCategory;

namespace UD_BodyPlan_Selection.Mod.BodyPlans
{
    [HasModSensitiveStaticCache]
    public class BodyPlanFactory
        : IXmlFactory<Symbol>
        , IXmlFactory<BodyPlanCategory>
        , IXmlFactory<TextShader>
        , IXmlFactory<BodyPlanEntry>
        , IXmlFactory<BodyPlanRenderable>
        , IXmlFactory<TransformationData>
    {
        protected Dictionary<string, Action<XmlDataHelper>> TextElementsRootNode;
        protected Dictionary<string, Action<XmlDataHelper>> TextElementsNodeChildren;
        protected Dictionary<string, Action<XmlDataHelper>> BodyPlansRootNode;
        protected Dictionary<string, Action<XmlDataHelper>> BodyPlansNodeChildren;

        [ModSensitiveStaticCache]
        private static bool Initialized = false;

        [ModSensitiveStaticCache]
        private static Dictionary<string, Symbol> _SymbolsByName;
        public static Dictionary<string, Symbol> SymbolsByName
        {
            get
            {
                if (_SymbolsByName.IsNullOrEmpty())
                    LogCacheInitError(nameof(SymbolsByName));

                return _SymbolsByName;
            }
        }

        [ModSensitiveStaticCache]
        private static Dictionary<string, BodyPlanEntry> _EntriesByID;
        public static Dictionary<string, BodyPlanEntry> EntriesByID
        {
            get
            {
                if (_EntriesByID.IsNullOrEmpty())
                    LogCacheInitError(nameof(EntriesByID));

                return _EntriesByID;
            }
        }

        private static BodyPlanFactory _Factory;
        public static BodyPlanFactory Factory
        {
            get
            {
                if (_Factory == null)
                {
                    _Factory = new();
                    Loading.LoadTask("Loading TextElements.xml", _Factory.LoadTextElements);
                    Loading.LoadTask("Loading BodyPlans.xml", _Factory.LoadBodyPlans);
                }
                return _Factory;
            }
        }

        public BodyPlanFactory()
        {
            TextElementsRootNode = new()
            {
                { "cybp_textElements", HandleTextElementsNode },
            };
            TextElementsNodeChildren = new()
            {
                { "symbol", HandleTextElementsSymbolNode },
                { "textElement", HandleTextElementNode },
            };
            BodyPlansRootNode = new()
            {
                { "cybp_bodyplans", HandleBodyPlansNode },
            };
            BodyPlansNodeChildren = new()
            {
                { "category", HandleBodyPlansCategoryNode },
                { "bodyplan", HandleBodyPlanNode },
            };
        }

        private static void LogError(object Message)
            => MetricsManager.LogCallingModError(Message);

        private static void LogCacheInitError(string CacheName)
        {
            string reason = Initialized
                ? "initialized incorrectly"
                : "not initialized";
            LogError($"{CacheName} empty or null, {nameof(BodyPlanFactory)} {reason}");
        }

        private static void LogException(string MethodName, Exception x)
            => LogError($"{nameof(BodyPlanFactory)}.{MethodName} {x}");

        public void LoadTextElements()
        {
            try
            {
                _EntriesByID = new();
                foreach (var xml in DataManager.YieldXMLStreamsWithRoot(XML_TEXTELEMENTS))
                {

                }
            }
            catch (Exception x)
            {
                LogException(nameof(LoadTextElements), x);
            }
        }

        public void LoadBodyPlans()
        {
            try
            {
                _EntriesByID = new();
                foreach (var xml in DataManager.YieldXMLStreamsWithRoot(XML_BODYPLANS))
                {

                }
            }
            catch (Exception x)
            {
                LogException(nameof(LoadBodyPlans), x);
            }
        }

        public void HandleTextElementsNode(XmlDataHelper xml)
            => xml.HandleNodes(TextElementsNodeChildren)
            ;
        public void HandleBodyPlansNode(XmlDataHelper xml)
            => xml.HandleNodes(BodyPlansNodeChildren)
            ;

        public void HandleTextElementsSymbolNode(XmlDataHelper xml)
        {
            string text = xml.ParseAttribute<string>("Name", null, required: true);
            if (text.StartsWith('-'))
            {
                text = text.TrimStart('-');
                MetricsManager.LogPotentialModError(xml.modInfo, DataManager.SanitizePathForDisplay(xml.BaseURI) + ":" + xml.LineNumber + ": Entry removal discontinued, set Hidden attribute instead.");
            }
            if (!SkillList.TryGetValue(text, out NewSkill))
            {
                NewSkill = new SkillEntry
                {
                    Name = text,
                    Cost = -999
                };
                SkillList.Add(text, NewSkill);
            }
            NewSkill.HandleXMLNode(xml);
            xml.HandleNodes(skillNodeChildren);
            NewSkill = null;
        }

        public void HandleTextElementNode(XmlDataHelper reader)
        {
            string text = reader.ParseAttribute<string>("Name", null, required: true);
            if (text.StartsWith('-'))
            {
                text = text.TrimStart('-');
                MetricsManager.LogPotentialModError(reader.modInfo, DataManager.SanitizePathForDisplay(reader.BaseURI) + ":" + reader.LineNumber + ": Entry removal discontinued, set Hidden attribute instead.");
            }
            if (!SkillList.TryGetValue(text, out NewSkill))
            {
                NewSkill = new SkillEntry
                {
                    Name = text,
                    Cost = -999
                };
                SkillList.Add(text, NewSkill);
            }
            NewSkill.HandleXMLNode(reader);
            reader.HandleNodes(skillNodeChildren);
            NewSkill = null;
        }

        public void HandleBodyPlansCategoryNode(XmlDataHelper reader)
        {
            string text = reader.ParseAttribute<string>("Name", null, required: true);
            if (text.StartsWith('-'))
            {
                text = text.TrimStart('-');
                MetricsManager.LogPotentialModError(reader.modInfo, DataManager.SanitizePathForDisplay(reader.BaseURI) + ":" + reader.LineNumber + ": Entry removal discontinued, set Hidden attribute instead.");
            }
            if (!SkillList.TryGetValue(text, out NewSkill))
            {
                NewSkill = new SkillEntry
                {
                    Name = text,
                    Cost = -999
                };
                SkillList.Add(text, NewSkill);
            }
            NewSkill.HandleXMLNode(reader);
            reader.HandleNodes(skillNodeChildren);
            NewSkill = null;
        }

        public void HandleBodyPlanNode(XmlDataHelper reader)
        {
            string text = reader.ParseAttribute<string>("Name", null, required: true);
            if (text.StartsWith('-'))
            {
                text = text.TrimStart('-');
                MetricsManager.LogPotentialModError(reader.modInfo, DataManager.SanitizePathForDisplay(reader.BaseURI) + ":" + reader.LineNumber + ": Entry removal discontinued, set Hidden attribute instead.");
            }
            if (!SkillList.TryGetValue(text, out NewSkill))
            {
                NewSkill = new SkillEntry
                {
                    Name = text,
                    Cost = -999
                };
                SkillList.Add(text, NewSkill);
            }
            NewSkill.HandleXMLNode(reader);
            reader.HandleNodes(skillNodeChildren);
            NewSkill = null;
        }

        public Symbol LoadFromData(XmlDataLoader<Symbol>.XmlData XmlData)
        {
            throw new NotImplementedException();
        }

        public BodyPlanCategory LoadFromData(XmlDataLoader<BodyPlanCategory>.XmlData XmlData)
        {
            throw new NotImplementedException();
        }

        public TextShader LoadFromData(XmlDataLoader<TextShader>.XmlData XmlData)
        {
            throw new NotImplementedException();
        }

        public BodyPlanEntry LoadFromData(XmlDataLoader<BodyPlanEntry>.XmlData XmlData)
        {
            throw new NotImplementedException();
        }
        public BodyPlanRenderable LoadFromData(XmlDataLoader<BodyPlanRenderable>.XmlData XmlData)
        {
            throw new NotImplementedException();
        }

        public TransformationData LoadFromData(XmlDataLoader<TransformationData>.XmlData XmlData)
        {
            throw new NotImplementedException();
        }

        public XmlDataLoader<Symbol> GetXmlDataLoader()
        {
            throw new NotImplementedException();
        }

        XmlDataLoader<BodyPlanCategory> IXmlFactory<BodyPlanCategory>.GetXmlDataLoader()
        {
            throw new NotImplementedException();
        }
    }
}
