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
    public abstract class XmlMetaData
    {
        public string DataNodeName;

        public string NameAttribute;

        public List<string> KnownAttributes;
        public List<string> KnownNodes;
        
        public bool IsNamed => !NameAttribute.IsNullOrEmpty();

        public bool IsUnique;

        public bool IsInheritable;

        public bool IsMergable;

        public XmlMetaData()
            : base()
        { }

        public virtual IEnumerable<string> GetKnownAttributes()
        {
            using var alreadyYielded = ScopeDisposedList<string>.GetFromPool();

            if (!KnownAttributes.IsNullOrEmpty())
            {
                foreach (var attribute in KnownAttributes)
                {
                    if (!alreadyYielded.Contains(attribute))
                    {
                        alreadyYielded.Add(attribute);
                        yield return attribute;
                    }
                }
            }
        }

        public virtual IEnumerable<string> GetKnownNodes()
        {
            using var alreadyYielded = ScopeDisposedList<string>.GetFromPool();

            if (!KnownNodes.IsNullOrEmpty())
            {
                foreach (var node in KnownNodes)
                {
                    if (!alreadyYielded.Contains(node))
                    {
                        alreadyYielded.Add(node);
                        yield return node;
                    }
                }
            }
        }
    }
    public class XmlMetaData<T> : XmlMetaData
        where T : IXmlLoaded<T>, new()
    {
        public Type Type => typeof(T);

        public Dictionary<string, Func<XmlDataHelper, XmlDataLoader<T>.XmlData>> XmlLoadedNodes;

        public bool ReflectFieldsInKnownAttributes;
        public bool ReflectFieldsInKnownNodes;

        private IEnumerable<string> _ReflectedFieldNames;
        public IEnumerable<string> ReflectedFieldNames
        {
            get
            {
                if (_ReflectedFieldNames.IsNullOrEmpty())
                    _ReflectedFieldNames = typeof(T).GetFieldNames(f => f.IsPublic && !f.IsStatic);

                return _ReflectedFieldNames;
            }
        }

        public XmlMetaData()
            : base()
        { }

        public XmlMetaData(bool ReflectFieldsInKnownAttributes, bool ReflectFieldsInKnownNodes)
            : this()
        {
            this.ReflectFieldsInKnownAttributes = ReflectFieldsInKnownAttributes;
            this.ReflectFieldsInKnownNodes = ReflectFieldsInKnownNodes;
        }

        public override IEnumerable<string> GetKnownAttributes()
        {
            using var alreadyYielded = ScopeDisposedList<string>.GetFromPool();

            foreach (var baseKnownAttribute in base.GetKnownAttributes())
            {
                if (!alreadyYielded.Contains(baseKnownAttribute))
                {
                    alreadyYielded.Add(baseKnownAttribute);
                    yield return baseKnownAttribute;
                }
            }
            if (ReflectFieldsInKnownAttributes)
            {
                foreach (var field in ReflectedFieldNames)
                {
                    if (!alreadyYielded.Contains(field))
                    {
                        alreadyYielded.Add(field);
                        yield return field;
                    }
                }
            }
        }

        public override IEnumerable<string> GetKnownNodes()
        {
            using var alreadyYielded = ScopeDisposedList<string>.GetFromPool();

            foreach (var baseKnownNodes in base.GetKnownNodes())
            {
                if (!alreadyYielded.Contains(baseKnownNodes))
                {
                    alreadyYielded.Add(baseKnownNodes);
                    yield return baseKnownNodes;
                }
            }
            if (ReflectFieldsInKnownNodes)
            {
                foreach (var field in ReflectedFieldNames)
                {
                    if (!alreadyYielded.Contains(field))
                    {
                        alreadyYielded.Add(field);
                        yield return field;
                    }
                }
            }
            if (!XmlLoadedNodes.IsNullOrEmpty())
            {
                foreach (var key in XmlLoadedNodes.Keys)
                {
                    if (!alreadyYielded.Contains(key))
                    {
                        alreadyYielded.Add(key);
                        yield return key;
                    }
                }
            }
        }
    }
}
