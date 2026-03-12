using System;
using System.Collections.Generic;
using System.Text;

using UD_BodyPlan_Selection.Mod.XML;

namespace UD_BodyPlan_Selection.Mod.BodyPlans
{
    public class TextElement
    {
        public struct Symbol : IXmlLoaded<Symbol>
        {
            public IXmlFactory<Symbol> Factory => BodyPlanFactory.Factory;

            public XmlDataLoader DataLoader => throw new NotImplementedException();

            public XmlMetaData<Symbol> XmlMetaData => throw new NotImplementedException();

            public string Name;
            public char Color;
            public char Value;

            public Symbol(string Name, char Color, char Value)
            {
                this.Name = Name;
                this.Color = Color;
                this.Value = Value;
            }

            public override readonly string ToString()
                => Color != '\0'
                ? "{{" + $"{Color}|{Value}" + "}}"
                : Value.ToString()
                ;
        }

        public string Name;
        public List<string> DescriptionBefore;
        public List<string> DescriptionAfter;
        public List<string> SummaryBefore;
        public List<string> SummaryAfter;
        public List<Symbol> Symbols;
    }
}
