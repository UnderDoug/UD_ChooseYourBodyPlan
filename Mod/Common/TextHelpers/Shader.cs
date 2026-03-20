using System;
using System.Collections.Generic;
using System.Text;

using UD_ChooseYourBodyPlan.Mod;
using UD_ChooseYourBodyPlan.Mod.Logging;

namespace UD_ChooseYourBodyPlan.Mod.TextHelpers
{
    public struct Shader
    {
        public static string XTagName => Const.MOD_PREFIX_SHORT + "Shader";

        public string Value;
        public string Type;
        public string Colors;
        public string Color;

        public Shader(
            string Value,
            string Type = null,
            string Colors = null,
            string Color = null
            )
        {
            this.Value = Value.ShaderColorOrNull();
            this.Type = Type;
            this.Colors = Colors;
            this.Color = Color.ShaderColorOrNull();
            Finalize();
        }
        public Shader(Dictionary<string, string> xTag)
            : this(
                  Value: null,
                  Type: xTag?.GetValue(nameof(Type)),
                  Colors: xTag?.GetValue(nameof(Colors)),
                  Color: xTag?.GetValue(nameof(Color)))
        { }

        public Shader(Shader Source)
            : this(Source.Value, Source.Type, Source.Colors, Source.Color)
        { }

        public Shader Finalize(string OriginalShader = null)
        {
            Value = Value.ShaderColorOrNull();
            if (Value.IsNullOrEmpty())
            {
                if (!Colors.IsNullOrEmpty())
                {
                    if (!Type.IsNullOrEmpty())
                        Value = $"{Colors} {Type}";

                    if (Color.IsNullOrEmpty())
                        Color = Colors[0].ToString().ShaderColorOrNull();
                }
            }
            if (Value.IsNullOrEmpty())
                Value = Color;

            if (Value.IsNullOrEmpty())
                Value = OriginalShader;

            return this;
        }

        public override readonly string ToString()
            => Value;

        public readonly string Apply(string Text)
            => !Value.IsNullOrEmpty()
                && !Text.IsNullOrEmpty()
            ? $"{"{{"}{this}|{Text}{"}}"}"
            : Text;

        public Shader Merge(
            string Value,
            string Type = null,
            string Colors = null,
            string Color = null
            )
        {
            string originalShader = this.Value;

            this.Value = Value;

            if (!Type.IsNullOrEmpty())
                this.Type = Type;

            if (!Colors.IsNullOrEmpty())
                this.Colors = Colors;

            if (!Color.IsNullOrEmpty())
                this.Color = Color;

            return Finalize(originalShader).DebugOutput(originalShader);
        }

        public Shader Merge(Shader Other)
            => Merge(
                Value: Other.Value,
                Type: Other.Type,
                Colors: Other.Colors,
                Color: Other.Color)
            ;

        public Shader Merge(Dictionary<string, string> xTag)
            => Merge(
                  Value: null,
                  Type: xTag?.GetValue(nameof(Type)),
                  Colors: xTag?.GetValue(nameof(Colors)),
                  Color: xTag?.GetValue(nameof(Color)))
            ;

        public readonly Shader DebugOutput(string OriginalShader = null)
        {
            using Indent indent = new(1);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(OriginalShader), OriginalShader ??  "NO_ORIGINAL"),
                });

            Debug.Log(nameof(Value), Value ?? "NO_SHADER", Indent: indent[1]);
            Debug.Log(nameof(Type), Type ?? "NO_TYPE", Indent: indent[1]);
            Debug.Log(nameof(Colors), Colors ?? "NO_COLORS", Indent: indent[1]);
            Debug.Log(nameof(Color), Color ?? "NO_COLOR", Indent: indent[1]);

            Debug.YehNah($"Final {nameof(Value)}", Value ?? "NO_SHADER", Indent: indent[1]);
            return this;
        }
    }
}
