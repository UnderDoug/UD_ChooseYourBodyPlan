using System;
using System.Collections.Generic;
using System.Text;

using ConsoleLib.Console;

using XRL;
using XRL.World;

using static UD_BodyPlan_Selection.Mod.Const;

namespace UD_BodyPlan_Selection.Mod.BodyPlans
{
    [HasModSensitiveStaticCache]
    public class BodyPlanRenderable : Renderable
    {
        public static string xTagPrefix => "UD_BDS_";

        [ModSensitiveStaticCache]
        private static Dictionary<string, Dictionary<string, string>> _AnatomyTiles;
        public static Dictionary<string, Dictionary<string, string>> AnatomyTiles => _AnatomyTiles ??= GameObjectFactory.Factory
            ?.GetBlueprintIfExists("UD_BodyPlan_Slection_AnatomyTiles")
            ?.xTags;

        public bool HFlip;

        public BodyPlanRenderable(
            string Tile,
            string RenderString = null,
            string ColorString = null,
            string TileColor = null,
            char DetailColor = '\0',
            bool HFlip = false)
            : base(
                  Tile: Tile,
                  RenderString: RenderString,
                  ColorString: ColorString,
                  TileColor: TileColor,
                  DetailColor: DetailColor)
        {
            this.HFlip = HFlip;
        }
        public BodyPlanRenderable(TransformationData Transformation, bool HFlip = false)
            : this(
                  Tile: Transformation?.Tile,
                  RenderString: Transformation?.RenderString ?? "@",
                  ColorString: $"{Transformation?.TileColor ?? "&Y"}^{Transformation?.DetailColor ?? "y"}",
                  TileColor: Transformation?.TileColor ?? "&Y",
                  DetailColor: Transformation?.DetailColor?[0] ?? 'y',
                  HFlip: HFlip)
        { }
        public BodyPlanRenderable(GenotypeEntry GenotypeEntry)
            : this(
                  Tile: GenotypeEntry.Tile,
                  RenderString: "@",
                  ColorString: $"&Y^{GenotypeEntry.DetailColor}",
                  TileColor: "&Y",
                  DetailColor: GenotypeEntry?.DetailColor?[0] ?? 'y',
                  HFlip: true)
        { }
        public BodyPlanRenderable(SubtypeEntry SubtypeEntry)
            : this(
                  Tile: SubtypeEntry.Tile,
                  RenderString: "@",
                  ColorString: $"&Y^{SubtypeEntry.DetailColor}",
                  TileColor: "&Y",
                  DetailColor: SubtypeEntry?.DetailColor?[0] ?? 'y',
                  HFlip: true)
        { }
        public BodyPlanRenderable(Renderable Renderable, bool HFlip = false)
            : base(Renderable)
        {
            this.HFlip = HFlip;
        }
        public BodyPlanRenderable(GameObjectBlueprint Blueprint, bool HFlip = false)
            : base(Blueprint)
        {
            this.HFlip = HFlip;
        }
        public BodyPlanRenderable(Dictionary<string, string> xTag, bool HFlip = false)
            : base()
        {
            this.HFlip = HFlip;

            if (!xTag.IsNullOrEmpty())
            {
                if (xTag.TryGetValue(nameof(Tile), out Tile)
                    && Tile.EqualsNoCase(REMOVE_TAG))
                    Tile = null;

                if (xTag.TryGetValue(nameof(RenderString), out RenderString)
                    && RenderString.EqualsNoCase(REMOVE_TAG))
                    RenderString = null;

                if (xTag.TryGetValue(nameof(ColorString), out ColorString)
                    && ColorString.EqualsNoCase(REMOVE_TAG))
                    ColorString = null;

                if (xTag.TryGetValue(nameof(TileColor), out TileColor)
                    && TileColor.EqualsNoCase(REMOVE_TAG))
                    TileColor = null;

                if (xTag.TryGetValue(nameof(DetailColor), out string detailColor)
                    && !detailColor.EqualsNoCase(REMOVE_TAG))
                    DetailColor = detailColor?[0] ?? '\0';

                if (xTag.TryGetValue(nameof(this.HFlip), out string hFlip))
                    bool.TryParse(hFlip, out this.HFlip);
            }
        }
        public BodyPlanRenderable(string Anatomy, bool HFlip = false)
            : this(
                  xTag: AnatomyTiles?.ContainsKey(xTagPrefix + Anatomy) ?? false
                    ? AnatomyTiles[xTagPrefix + Anatomy]
                    : null,
                  HFlip: HFlip)
        { }

        public override bool getHFlip()
            => HFlip;
    }
}
