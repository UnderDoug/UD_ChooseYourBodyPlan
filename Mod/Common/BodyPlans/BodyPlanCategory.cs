using System;
using System.Collections.Generic;
using System.Text;

namespace UD_BodyPlan_Selection.Mod.BodyPlans
{
    public class BodyPlanCategory
    {
        public struct TextShader
        {
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

        public string Name;
        public string DisplayName;
        public TextShader Shader;

        public string GetDisplayName()
            => Shader.Apply(DisplayName);
    }
}
