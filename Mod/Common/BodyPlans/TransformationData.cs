using System;
using System.Collections.Generic;
using System.Text;

using ConsoleLib.Console;

using XRL;
using XRL.World;

using static UD_BodyPlan_Selection.Mod.Const;

namespace UD_BodyPlan_Selection.Mod.BodyPlans
{
    public class TransformationData
    {
        public BodyPlanEntry ParentBodyPlan;
        public string RenderString;
        public string Tile;
        public string TileColor;
        public string DetailColor;
        public string Species;
        public string Property;
        public List<string> Mutations;

        public TransformationData()
        {
            ParentBodyPlan = null;
            RenderString = null;
            Tile = null;
            TileColor = null;
            DetailColor = null;
            Species = null;
            Property = null;
            Mutations = null;
        }
        public TransformationData(Dictionary<string, string> xTag)
            : this()
        {
            if (xTag != null)
            {
                if (xTag.TryGetValue(nameof(RenderString), out RenderString)
                    && RenderString.EqualsNoCase(REMOVE_TAG))
                    RenderString = null;

                if (xTag.TryGetValue(nameof(Tile), out Tile)
                    && Tile.EqualsNoCase(REMOVE_TAG))
                    Tile = null;

                if (xTag.TryGetValue(nameof(TileColor), out TileColor)
                    && TileColor.EqualsNoCase(REMOVE_TAG))
                    TileColor = null;

                if (xTag.TryGetValue(nameof(DetailColor), out DetailColor)
                    && DetailColor.EqualsNoCase(REMOVE_TAG))
                    DetailColor = null;

                if (xTag.TryGetValue(nameof(Species), out Species)
                    && Species.EqualsNoCase(REMOVE_TAG))
                    Species = null;

                if (xTag.TryGetValue(nameof(Property), out Property)
                    && Property.EqualsNoCase(REMOVE_TAG))
                    Property = null;

                if (xTag.TryGetValue(nameof(Mutations), out string mutations)
                    && !mutations.EqualsNoCase(REMOVE_TAG))
                    Mutations = Utils.GetVersionSafeParser<List<string>>()?.Invoke(mutations);
            }
        }

        public void DebugOutput(int Indent = 0)
        {
            Utils.Log($"{nameof(RenderString)}: {RenderString ?? "NO_RENDER_STRING"}", Indent: Indent);
            Utils.Log($"{nameof(Tile)}: {Tile ?? "NO_TILE"}", Indent: Indent);
            Utils.Log($"{nameof(TileColor)}: {TileColor ?? "NO_TILE_COLOR"}", Indent: Indent);
            Utils.Log($"{nameof(DetailColor)}: {DetailColor ?? "NO_DETAIL_COLOR"}", Indent: Indent);
            Utils.Log($"{nameof(Species)}: {Species ?? "NO_SPECIES"}", Indent: Indent);
            Utils.Log($"{nameof(Property)}: {Property ?? "NO_PROPERTY"}", Indent: Indent);
            Utils.Log($"{nameof(Mutations)}:", Indent: Indent);
            if (Mutations.IsNullOrEmpty())
                Utils.Log("::None", Indent: Indent + 1);
            else
                foreach (string mutation in Mutations)
                    Utils.Log($"::{mutation}", Indent: Indent + 1);
        }
    }
}
