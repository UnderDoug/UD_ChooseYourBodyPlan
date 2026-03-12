using System;
using System.Collections.Generic;
using System.Text;

using UD_BodyPlan_Selection.Mod.XML;

namespace UD_BodyPlan_Selection.Mod.BodyPlans
{
    public class BodyPlanCategory : IXmlLoaded<BodyPlanCategory>
    {
        public struct TextShader : IXmlLoaded<TextShader>
        {
            public IXmlFactory<TextShader> Factory => BodyPlanFactory.Factory;
            public XmlMetaData<TextShader> XmlMetaData => new(false, false)
            {

            };

            public string Shader;
            public string Type;
            public string Colors;
            public string Color;

            public TextShader(
                string Shader,
                string Type = null,
                string Colors = null,
                string Color = null
                )
            {
                this.Shader = Shader.ShaderColorOrNull();
                this.Type = Type;
                this.Colors = Colors;
                this.Color = Color;

                if (!this.Colors.IsNullOrEmpty())
                {
                    if (!this.Type.IsNullOrEmpty())
                        this.Shader ??= $"{this.Colors} {this.Type}";

                    this.Color ??= this.Colors[0].ToString();
                }
                this.Shader ??= this.Color;
            }
            public override readonly string ToString()
                => Shader;

            public readonly string Apply(string Text)
                => !Shader.IsNullOrEmpty()
                    && !Text.IsNullOrEmpty()
                ? "{{" + $"{this}|{Text}" + "}}"
                : Text;
        }

        public IXmlFactory<BodyPlanCategory> Factory => BodyPlanFactory.Factory;

        public XmlMetaData<BodyPlanCategory> XmlMetaData => new()
        {
            DataNodeName = "category",
            NameAttribute = nameof(Name),
            KnownAttributes = new()
            {
                "Name",
                "DisplayName",
                "Shader",
                "Color",
                "Load",
            },
            KnownNodes = new()
            {
                "displayName",
                "shader",
                "color",
            },
            XmlLoadedNodes = new()
            {
                { "shader", typeof(TextShader) },
            },
            IsInheritable = true,
            IsMergable = true,
        };

        public bool XmlMetaDataFromFieldReflection => false;

        public string Name;
        public string DisplayName;
        public TextShader Shader;

        public BodyPlanCategory()
        {
        }

        public string GetDisplayName()
            => Shader.Apply(DisplayName);
    }
}
