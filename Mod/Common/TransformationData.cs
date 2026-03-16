using System;
using System.Collections.Generic;
using System.Linq;

using XRL;
using XRL.World;
using XRL.World.Anatomy;

using static UD_ChooseYourBodyPlan.Mod.AnatomyConfiguration;

namespace UD_ChooseYourBodyPlan.Mod
{
    [Serializable]
    public class TransformationData : ILoadFromDataBucket<TransformationData>
    {
        public string BaseDataBucketBlueprint => Const.XFORM_DATA_BLUEPRINT;

        public static string RemoveTag => Const.REMOVE_TAG;

        public string CacheKey => Anatomy;

        public string Anatomy;

        public BodyPlanRender Render;

        public string RenderString => Render?.RenderString;
        public string Tile => Render?.Tile;
        public string TileColor => Render?.TileColor;
        public char DetailColor => Render?.DetailColor ?? '\0';
        public string DetailColorS => (Render?.DetailColor ?? '\0').ToString();

        public string Species;
        public string Property;

        public List<string> Mutations;

        public OptionDelegates OptionDelegates;

        public TransformationData()
        {
            Anatomy = null;
            Render = null;
            Species = null;
            Property = null;
            Mutations = null;
        }
        public TransformationData(GameObjectBlueprint DataBucket)
            : this()
        {
            LoadFromDataBucket(DataBucket);
        }
        public TransformationData(TransformationData Source)
            : this()
        {
            Anatomy = Source.Anatomy;
            Render = new(Source);
            Species = Source.Species;
            Property = Source.Property;
            Mutations = !Source.Mutations.IsNullOrEmpty() ? new(Source.Mutations) : new();
        }

        public void DebugOutput(int Indent = 0)
        {
            Utils.Log($"{nameof(RenderString)}: {RenderString ?? "NO_RENDER_STRING"}", Indent: Indent);
            Utils.Log($"{nameof(Tile)}: {Tile ?? "NO_TILE"}", Indent: Indent);
            Utils.Log($"{nameof(TileColor)}: {TileColor ?? "NO_TILE_COLOR"}", Indent: Indent);
            Utils.Log($"{nameof(DetailColor)}: {DetailColorS ?? "NO_DETAIL_COLOR"}", Indent: Indent);
            Utils.Log($"{nameof(Species)}: {Species ?? "NO_SPECIES"}", Indent: Indent);
            Utils.Log($"{nameof(Property)}: {Property ?? "NO_PROPERTY"}", Indent: Indent);
            Utils.Log($"{nameof(Mutations)}:", Indent: Indent);
            if (Mutations.IsNullOrEmpty())
                Utils.Log("::None", Indent: Indent + 1);
            else
                foreach (string mutation in Mutations)
                    Utils.Log($"::{mutation}", Indent: Indent + 1);
        }

        public TransformationData LoadFromDataBucket(GameObjectBlueprint DataBucket)
        {
            if (DataBucket.InheritsFrom(Const.XFORM_DATA_BLUEPRINT))
            {
                DataBucket.TryGetTagValueForData(nameof(Anatomy), out Anatomy);

                DataBucket.TryGetTagValueForData(nameof(Species), out Species);
                DataBucket.TryGetTagValueForData(nameof(Property), out Property);

                Render = new(DataBucket);
                if (!DataBucket.Mutations.IsNullOrEmpty())
                    Mutations = new(DataBucket.Mutations.Keys);

                OptionDelegates.ParseDataBucket(DataBucket);

                if (DataBucket.TryGetTag(nameof(Mutations), out string mutations)
                    && mutations.CachedCommaExpansion().ToList() is List<string> mutationsList
                    && !mutationsList.IsNullOrEmpty())
                {
                    foreach (var mutation in mutationsList)
                    {
                        if (MutationFactory.GetMutationEntryByName(mutation) is not MutationEntry mutationEntry)
                            continue;

                        if (!Mutations.Select(m => MutationFactory.GetMutationEntryByName(m)).Contains(mutationEntry))
                            Mutations.Add(mutation);
                    }
                }
            }
            else
            {
                Utils.ThisMod.Error($"Aborted attempt to construct {GetType().Name} " +
                    $"from DataBucket inheriting from \"{DataBucket.GetBase()}\" " +
                    $"instead of \"{Const.XFORM_DATA_BLUEPRINT}\"");
            }

            return this;
        }

        public bool SameAs(TransformationData Other)
            => Anatomy == Other?.Anatomy
            ;

        public TransformationData Merge(TransformationData Other)
        {
            Anatomy ??= Other.Anatomy;
            Utils.MergeReplaceField(ref Render, new(Other));
            Utils.MergeReplaceField(ref Species, Other.Species);
            Utils.MergeReplaceField(ref Property, Other.Property);
            Utils.MergeReplaceField(ref Mutations, new(Other.Mutations));

            return this;
        }

        public TransformationData Clone()
            => new TransformationData()
                .Merge(this);

        public void Dispose()
        {
            Render = null;

            OptionDelegates.Clear();
            OptionDelegates = null;

            Mutations.Clear();
            Mutations = null;
        }
    }
}
