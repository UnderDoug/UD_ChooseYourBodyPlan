using System;
using System.Collections.Generic;
using System.Linq;

using UD_ChooseYourBodyPlan.Mod.Logging;

using XRL;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;

using static UD_ChooseYourBodyPlan.Mod.ILoadFromDataBucket<UD_ChooseYourBodyPlan.Mod.TransformationData>;

namespace UD_ChooseYourBodyPlan.Mod
{
    [Serializable]
    public class TransformationData : ILoadFromDataBucket<TransformationData>
    {
        [Serializable]
        public struct Mutation
        {
            public string Name;
            public string Class;
            public string Color;

            public Mutation(MutationEntry Entry, string Color = null)
            {
                Name = Entry.Name;
                Class = Entry.Class;
                this.Color = Color;
            }

            public Mutation(Mutation Source)
            {
                Name = Source.Name;
                Class = Source.Class;
                Color = Source.Color;
            }

            public readonly MutationEntry Entry => Utils.GetMutationByClassOrName(Class ?? Name);

            public readonly void AddMutation(GameObject Object)
            {
                if (Entry != null)
                    Object.RequirePart<Mutations>().AddMutation(Entry);
            }

            public override readonly string ToString()
            {
                if ((Entry?.Name ?? Name) is not string name)
                    return null;
                return "{{" + $"{Color ?? "C"}|{name}" + "}}";
            }
        }

        public static string LoadingDataBucket => "TransformationData";
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

        public OptionDelegateContexts OptionDelegateContexts;

        private HashSet<string> TextElementsNames;

        public List<InventoryObject> NaturalEquipment;

        public List<GamePartBlueprint> ManagedNaturalEquipment;

        public Dictionary<string, Mutation> Mutations;

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
            if (Source == null)
                return;

            Anatomy = Source.Anatomy;

            Render = Source?.Render?.Clone();

            Species = Source.Species;
            Property = Source.Property;

            OptionDelegateContexts = new();
            if (!Source.OptionDelegateContexts.IsNullOrEmpty())
                OptionDelegateContexts.AddRange(Source.OptionDelegateContexts);

            TextElementsNames = new();
            if (!Source.TextElementsNames.IsNullOrEmpty())
                foreach (var textElementsName in Source.TextElementsNames)
                    TextElementsNames.Add(textElementsName);

            NaturalEquipment = new();
            if (!Source.NaturalEquipment.IsNullOrEmpty())
                NaturalEquipment.AddRange(Source.NaturalEquipment);

            ManagedNaturalEquipment = new();
            if (!Source.ManagedNaturalEquipment.IsNullOrEmpty())
                ManagedNaturalEquipment.AddRange(Source.ManagedNaturalEquipment);

            Mutations = new();
            if (!Source.Mutations.IsNullOrEmpty())
                foreach ((var mutationName, var mutation) in Source.Mutations)
                    Mutations.Add(mutationName, new(mutation));
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

            DataBucket.TryGetTagValueForData(nameof(Anatomy), out Anatomy);

            DataBucket.TryGetTagValueForData(nameof(Species), out Species);
            if (!DataBucket.TryGetTagValueForData(nameof(Property), out Property))
                DataBucket.TryGetTagValueForData($"Eaten{nameof(Property)}", out Property);

            Render = new BodyPlanRender().LoadFromDataBucket(DataBucket);

            OptionDelegateContexts ??= new();
            OptionDelegateContexts.ParseDataBucket(DataBucket);

            if (DataBucket.GetTextElementsTags() is IEnumerable<string> textElementsTags)
            {
                TextElementsNames = new();
                foreach (var textElementsName in textElementsTags)
                    TextElementsNames.Add(textElementsName);
            }

            NaturalEquipment ??= new();
            if (!DataBucket.Inventory.IsNullOrEmpty())
                NaturalEquipment.AddRange(DataBucket.Inventory);

            Mutations ??= new();
            if (!DataBucket.Mutations.IsNullOrEmpty())
            {
                foreach (var mutation in DataBucket.Mutations.Keys)
                    Mutations[mutation] = new Mutation
                    {
                        Class = mutation,
                    };
            }

            if (DataBucket.TryGetTag(nameof(Mutations), out string mutations))
            {
                if (mutations.Contains(',')
                    && mutations.CachedCommaExpansion().ToList() is List<string> mutationsList
                    && !mutationsList.IsNullOrEmpty())
                {
                    foreach (var mutation in mutationsList)
                    {
                        AddMutation(mutation);
                    }
                }
                else
                if (mutations.Contains("::")
                    && mutations.CachedDictionaryExpansion() is Dictionary<string, string> mutationsDictionary
                    && !mutationsDictionary.IsNullOrEmpty())
                {
                    foreach ((var mutation, var color) in mutationsDictionary)
                    {
                        AddMutation(mutation, color);
                    }
                }
                else
                {
                    AddMutation(mutations);
                }
            }

            DebugOutput(1);
            return this;
        }

        public void AddMutation(MutationEntry MutationEntry, string Color = null)
            => Mutations[MutationEntry.Class] = new Mutation(MutationEntry, Color)
            ;

        public void AddMutation(string Mutation, string Color = null)
        {
            if (Utils.GetMutationByClassOrName(Mutation) is MutationEntry mutationEntry)
                AddMutation(mutationEntry, Color);
        }

        public bool SameAs(TransformationData Other)
            => Anatomy == Other?.Anatomy
            ;

        public bool HasTextElements()
            => !TextElementsNames.IsNullOrEmpty()
            ;

        public IEnumerable<string> GetTextElementsNames()
        {
            if (TextElementsNames.IsNullOrEmpty())
                yield break;

            foreach (var textElementsName in TextElementsNames)
                yield return textElementsName;
        }

        public TransformationData Merge(TransformationData Other)
        {
            if (Other == null)
                return this;

            Anatomy ??= Other.Anatomy;

            if (Other.Render?.Tile != null)
                Render = Other.Render.Clone();

            Species = Other.Species;
            Property = Other.Property;

            OptionDelegateContexts ??= new();
            if (!Other.OptionDelegateContexts.IsNullOrEmpty())
                OptionDelegateContexts.AddRange(Other.OptionDelegateContexts);

            TextElementsNames ??= new();
            if (!Other.TextElementsNames.IsNullOrEmpty())
                TextElementsNames.AddRange(Other.TextElementsNames);

            NaturalEquipment ??= new();
            if (!Other.NaturalEquipment.IsNullOrEmpty())
                NaturalEquipment.AddRange(Other.NaturalEquipment);

            ManagedNaturalEquipment ??= new();
            if (!Other.ManagedNaturalEquipment.IsNullOrEmpty())
                ManagedNaturalEquipment.AddRange(Other.ManagedNaturalEquipment);

            Mutations ??= new();
            if (!Other.Mutations.IsNullOrEmpty())
                foreach ((var mutationName, var mutation) in Other.Mutations)
                    Mutations.Add(mutationName, new(mutation));

            return this;
        }

        public TransformationData Clone()
            => new(this)
            ;

        public void Dispose()
        {
            Render = null;

            OptionDelegateContexts?.Clear();
            OptionDelegateContexts = null;

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

            Debug.Log(nameof(OptionDelegateContexts), OptionDelegateContexts?.Count ?? 0, Indent: indent[1]);
            Debug.Loggregrate(
                Source: OptionDelegateContexts,
                Proc: o => o.ToString(),
                Empty: "None",
                PostProc: s => $"::{s}",
                Indent: indent[2]);

            Debug.Log(nameof(TextElementsNames), TextElementsNames?.Count ?? 0, Indent: indent[1]);
            Debug.Loggregrate(
                Source: TextElementsNames,
                Proc: n => n,
                Empty: "None",
                PostProc: s => $"::{s}",
                Indent: indent[2]);

            Debug.Log(nameof(NaturalEquipment), NaturalEquipment?.Count ?? 0, Indent: indent[1]);
            Debug.Loggregrate(
                Source: NaturalEquipment,
                Proc: n => n.ToString(),
                Empty: "None",
                PostProc: s => $"::{s}",
                Indent: indent[2]);

            Debug.Log($"{nameof(Mutations)}:", Indent: indent[1]);
            Debug.Loggregrate(
                Source: Mutations,
                Proc: m => m.PairString(),
                Empty: "None",
                PostProc: s => $"::{s}",
                Indent: indent[2]);
        }
    }
}
