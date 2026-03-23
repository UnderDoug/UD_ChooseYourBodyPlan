using System;
using System.Collections.Generic;

using ConsoleLib.Console;

using UD_ChooseYourBodyPlan.Mod.Logging;

using XRL;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;

namespace UD_ChooseYourBodyPlan.Mod
{
    [HasModSensitiveStaticCache]
    [Serializable]
    public class BodyPlanRender : Renderable, ILoadFromDataBucket<BodyPlanRender>
    {
        public string BaseDataBucketBlueprint => "Object";

        public string CacheKey => ToString();

        public bool? HFlip;

        public BodyPlanRender()
            : base()
        {
        }

        public BodyPlanRender(
            string Tile,
            string RenderString = null,
            string ColorString = null,
            string TileColor = null,
            char DetailColor = default,
            bool? HFlip = null)
            : base(
                  Tile: Tile,
                  RenderString: RenderString,
                  ColorString: ColorString,
                  TileColor: TileColor,
                  DetailColor: DetailColor)
        {
            this.HFlip = HFlip;
        }

        public BodyPlanRender(TransformationData Transformation, bool? HFlip = null)
            : this(
                  Tile: Transformation?.Tile,
                  RenderString: Transformation?.RenderString ?? "@",
                  ColorString: $"{Transformation?.TileColor ?? "&Y"}^{Transformation?.DetailColorS ?? "y"}",
                  TileColor: Transformation?.TileColor ?? "&Y",
                  DetailColor: Transformation?.DetailColor ?? 'y',
                  HFlip: HFlip)
        { }

        public BodyPlanRender(GenotypeEntry GenotypeEntry)
            : this()
        {
            SetFromGenotype(GenotypeEntry);
        }

        public BodyPlanRender(SubtypeEntry SubtypeEntry)
            : this()
        {
            SetFromSubtype(SubtypeEntry);
        }

        public BodyPlanRender(Renderable Renderable, bool? HFlip = null)
            : base(Renderable)
        {
            this.HFlip = HFlip;
        }

        public BodyPlanRender(GameObjectBlueprint Blueprint, bool HFlip = false)
            : base(Blueprint)
        {
            this.HFlip = HFlip;
        }

        public override bool getHFlip()
            => HFlip.GetValueOrDefault();

        public override string ToString()
        {
            string output = $"{GetType().Name}";

            string extras = null;
            if (!RenderString.IsNullOrEmpty())
                extras += $"[{nameof(RenderString)}:{RenderString}]";
            
            if (!ColorString.IsNullOrEmpty())
                extras += $"[{nameof(ColorString)}:{ColorString}]";

            if (!Tile.IsNullOrEmpty())
                extras += $"[{nameof(Tile)}:{Tile}]";

            if (!TileColor.IsNullOrEmpty())
                extras += $"[{nameof(TileColor)}:{TileColor}]";

            if (DetailColor != default)
                extras += $"[{nameof(DetailColor)}:{DetailColor}]";

            if (HFlip != null)
                extras += $"[{nameof(HFlip)}:{HFlip}]";

            if (extras.IsNullOrEmpty())
                extras += "[Empty]";

            return output + extras;
        }

        public bool IsEmpty()
            => RenderString.IsNullOrEmpty()
            && ColorString.IsNullOrEmpty()
            && Tile.IsNullOrEmpty()
            && TileColor.IsNullOrEmpty()
            && DetailColor == default
            && HFlip == null
            ;

        private BodyPlanRender LoadFromDataBucketTags(GameObjectBlueprint DataBucket, bool? HFlip = null)
        {
            if (DataBucket == null)
                return null;

            DataBucket.AssignStringFieldFromTag(nameof(Tile), ref Tile);
            DataBucket.AssignStringFieldFromTag(nameof(RenderString), ref RenderString);
            DataBucket.AssignStringFieldFromTag(nameof(ColorString), ref ColorString);
            DataBucket.AssignStringFieldFromTag(nameof(TileColor), ref TileColor);
            if (DataBucket.TryGetTagValueForData(nameof(DetailColor), out string detailColor)
                && !detailColor.EqualsNoCase(Const.REMOVE_TAG))
                DetailColor = detailColor?[0] ?? default;


            if (HFlip == null)
            {
                if (DataBucket.TryGetTagValueForData(nameof(HFlip), out string hFlip)
                    && !hFlip.EqualsNoCase(Const.REMOVE_TAG))
                    if (bool.TryParse(hFlip, out bool hFlipValue))
                        this.HFlip = hFlipValue;
            }
            else
                this.HFlip = HFlip.GetValueOrDefault();

            return this;
        }

        public BodyPlanRender LoadFromDataBucket(GameObjectBlueprint DataBucket)
        {
            if (DataBucket.HasPart(nameof(Render)))
            {
                Set(DataBucket);
                if (DataBucket.TryGetTagValueForData(nameof(HFlip), out string hFlip)
                    && !hFlip.EqualsNoCase(Const.REMOVE_TAG))
                    if (bool.TryParse(hFlip, out bool hFlipValue))
                        HFlip = hFlipValue;
            }
            else
                LoadFromDataBucketTags(DataBucket);

            return this;
        }

        public BodyPlanRender SetFromGenotype(GenotypeEntry Entry)
        {
            Tile = Entry.Tile;
            RenderString = "@";
            ColorString = $"&Y^{Entry.DetailColor}";
            TileColor = "&Y";
            DetailColor = Entry?.DetailColor?[0] ?? 'y';
            HFlip = true;
            return this;
        }

        public BodyPlanRender SetFromSubtype(SubtypeEntry Entry)
        {
            Tile = Entry.Tile;
            RenderString = "@";
            ColorString = $"&Y^{Entry.DetailColor}";
            TileColor = "&Y";
            DetailColor = Entry?.DetailColor?[0] ?? 'y';
            HFlip = true;
            return this;
        }

        public bool SameAs(BodyPlanRender Other)
            => Other != null
            && Tile == Other.Tile
            && RenderString == Other.RenderString
            && ColorString == Other.ColorString
            && TileColor == Other.TileColor
            && DetailColor == Other.DetailColor
            && HFlip == Other.HFlip
            ;

        public BodyPlanRender Merge(BodyPlanRender Other)
        {
            Copy(Other);
            HFlip = Other.HFlip;
            return this;
        }

        public BodyPlanRender Clone()
            => new(this, HFlip);

        public void Dispose()
        {
        }
    }
}
