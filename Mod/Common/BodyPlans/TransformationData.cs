using System;
using System.Collections.Generic;
using System.Linq;

using UD_ChooseYourBodyPlan.Mod.Logging;

using XRL;
using XRL.World;
using XRL.World.Anatomy;

using static UD_ChooseYourBodyPlan.Mod.ILoadFromDataBucket<UD_ChooseYourBodyPlan.Mod.TransformationData>;

namespace UD_ChooseYourBodyPlan.Mod
{
    [Serializable]
    public class TransformationData : ILoadFromDataBucket<TransformationData>
    {
        public static string DataBucketFile => "TransformationData.xml";
        public string BaseDataBucketBlueprint => Const.XFORM_DATA_BLUEPRINT;

        public static string RemoveTag => Const.REMOVE_TAG;

        public string CacheKey => Anatomy;

        public string Anatomy;

        public BodyPlanRender Render;

        public string RenderString => Render?.RenderString;
        public string Tile => Render?.Tile;
        public string TileColor => Render?.TileColor;
        public char DetailColor => Render?.DetailColor ?? default;
        public string DetailColorS => (Render?.DetailColor ?? default).ToString();

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

        public TransformationData LoadFromDataBucket(GameObjectBlueprint DataBucket)
        {
            using Indent indent = new(1);
            Debug.LogCaller(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(DataBucket?.Name ?? "NO_DATA_BUCKET"),
                });

            if (!CheckIsValidDataBucket(this, DataBucket))
            {
                Dispose();
                return null;
            }

            if (DataBucket.InheritsFrom(Const.XFORM_DATA_BLUEPRINT))
            {
                DataBucket.TryGetTagValueForData(nameof(Anatomy), out Anatomy);

                DataBucket.TryGetTagValueForData(nameof(Species), out Species);
                if (!DataBucket.TryGetTagValueForData(nameof(Property), out Property))
                    DataBucket.TryGetTagValueForData($"Eaten{nameof(Property)}", out Property);

                Render = new BodyPlanRender().LoadFromDataBucket(DataBucket);

                if (!DataBucket.Mutations.IsNullOrEmpty())
                    Mutations = new(DataBucket.Mutations.Keys);

                OptionDelegates ??= new();
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

            DebugOutput(1);
            return this;
        }

        public bool SameAs(TransformationData Other)
            => Anatomy == Other?.Anatomy
            ;

        public TransformationData Merge(TransformationData Other)
        {
            Anatomy ??= Other.Anatomy;
            if (Other?.Render?.Tile != null)
                Render = Other.Render.Clone();

            Utils.MergeReplaceField(ref Species, Other.Species);
            Utils.MergeReplaceField(ref Property, Other.Property);
            Utils.MergeReplaceField(ref Mutations, new(Other.Mutations));

            OptionDelegates.Merge(OptionDelegates);

            return this;
        }

        public TransformationData Clone()
            => new TransformationData()
                .Merge(this);

        public void Dispose()
        {
            Render = null;

            OptionDelegates?.Clear();
            OptionDelegates = null;

            Mutations?.Clear();
            Mutations = null;
        }

        public void DebugOutput(int Indent)
        {
            using Indent indent = new(Indent);
            Debug.LogMethod(indent,
                ArgPairs: new Debug.ArgPair[]
                {
                    Debug.Arg(nameof(Anatomy), Anatomy ?? "MISSING_ANATOMY"),
                });

            Debug.Log(nameof(RenderString), RenderString ?? "NO_RENDER_STRING", Indent: indent[1]);
            Debug.Log(nameof(Tile), Tile ?? "NO_TILE", Indent: indent[1]);
            Debug.Log(nameof(TileColor), TileColor ?? "NO_TILE_COLOR", Indent: indent[1]);
            Debug.Log(nameof(DetailColor), DetailColorS ?? "NO_DETAIL_COLOR", Indent: indent[1]);

            Debug.Log(nameof(Species), Species ?? "NO_SPECIES", Indent: indent[1]);
            Debug.Log(nameof(Property), Property ?? "NO_PROPERTY", Indent: indent[1]);

            Debug.Log($"{nameof(Mutations)}:", Indent: indent[1]);
            Debug.Loggregrate(
                Source: Mutations,
                Proc: m => m,
                Empty: "None",
                PostProc: s => $"::{s}",
                Indent: indent[2]);

            Debug.Log(nameof(OptionDelegates), OptionDelegates?.Count ?? 0, Indent: indent[1]);
            Debug.Loggregrate(
                Source: OptionDelegates,
                Proc: o => $"{o.OptionID} {o.Operator} {o.TrueState}",
                Empty: "None",
                PostProc: s => $"::{s}",
                Indent: indent[2]);
        }
    }
}
