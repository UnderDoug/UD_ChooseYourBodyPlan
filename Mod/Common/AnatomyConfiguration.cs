using System;
using System.Collections.Generic;
using System.Linq;

using XRL.World;
using XRL.World.Anatomy;

using static UD_BodyPlan_Selection.Mod.AnatomyConfiguration;

namespace UD_BodyPlan_Selection.Mod
{
    public class AnatomyConfiguration
    {

        public delegate bool BooleanOptionDelegate();

        public class TransformationData
        {
            public static string RemoveTag => Const.REMOVE_TAG;

            public string RenderString;
            public string Tile;
            public string TileColor;
            public string DetailColor;
            public string Species;
            public string Property;
            public List<string> Mutations;

            public TransformationData()
            {
                RenderString = null;
                Tile = null;
                Tile = null;
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
                    xTag.AssignStringFieldFromXTag(nameof(RenderString), ref RenderString);
                    xTag.AssignStringFieldFromXTag(nameof(Tile), ref Tile);
                    xTag.AssignStringFieldFromXTag(nameof(TileColor), ref TileColor);
                    xTag.AssignStringFieldFromXTag(nameof(DetailColor), ref DetailColor);
                    xTag.AssignStringFieldFromXTag(nameof(Species), ref Species);
                    xTag.AssignStringFieldFromXTag(nameof(Property), ref Property);

                    if (xTag.TryGetValue(nameof(Mutations), out string mutations)
                        && !mutations.EqualsNoCase(RemoveTag))
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

        public static string RemoveTag => Const.REMOVE_TAG;

        public static string TransformXTag => Const.MOD_PREFIX_SHORT + "Transformation";
        
        private readonly IReadOnlyList<string> Anatomies;

        public string Anatomy;

        public string DisplayName;

        public string Category;

        public bool IsDifficult;

        public bool IsMechanical;

        public TransformationData Transformation;

        public bool IsTransformation => Transformation != null;

        public bool IsRestricted;

        public bool IsOptional;

        public BooleanOptionDelegate EnableRestricted;

        [Obsolete(message: "Moved to DescriptionAddition in v0.0.2, likely to stick around for a couple of versions.")]
        public string ExceptionMessage;

        [Obsolete(message: "Moved to SummaryAddition in v0.0.2, likely to stick around for a couple of versions.")]
        public string ExceptionSummary;

        public string DescriptionAddition;
        public string SummaryAddition;

        public List<KeyValuePair<char, string>> Symbols; // char color, string symbol

        public AnatomyConfiguration()
        {
            Anatomies = null;
            Anatomy = null;
            DisplayName = null;
            Category = null;
            IsDifficult = false;
            IsMechanical = false;
            Transformation = null;
            IsRestricted = false;
            IsOptional = false;
            EnableRestricted = null;
            DescriptionAddition = null;
            SummaryAddition = null;
            Symbols = new();
        }
        public AnatomyConfiguration(
            IReadOnlyList<string> Anatomies,
            string Anatomy,
            string DisplayName,
            string Category,
            bool IsDifficult,
            bool IsMechanical,
            TransformationData Transformation,
            bool IsRestricted,
            bool IsOptional,
            BooleanOptionDelegate EnableRestricted,
            string DescriptionAddition,
            string SummaryAddition,
            List<KeyValuePair<char, string>> Symbols
            )
            : this()
        {
            this.Anatomies = Anatomies;
            this.Anatomy = Anatomy;
            this.DisplayName = DisplayName;
            this.Category = Category;
            this.IsDifficult = IsDifficult;
            this.IsMechanical = IsMechanical;
            this.Transformation = Transformation;
            this.IsRestricted = IsRestricted;
            this.IsOptional = IsOptional;
            this.EnableRestricted = EnableRestricted;
            this.DescriptionAddition = DescriptionAddition;
            this.SummaryAddition = SummaryAddition;
            this.Symbols = Symbols ?? new();

            if (Anatomies.IsNullOrEmpty())
            {
                Utils.Log($"{typeof(AnatomyConfiguration).CallChain(".ctor")}()");
                Utils.Log($"{nameof(Anatomy)}: {Anatomy}", Indent: 1);
                Utils.Log($"{nameof(DisplayName)}: {DisplayName}", Indent: 1);
                Utils.Log($"{nameof(IsDifficult)}: {IsDifficult}", Indent: 1);
                Utils.Log($"{nameof(IsMechanical)}: {IsMechanical}", Indent: 1);
                Utils.Log($"{nameof(IsTransformation)}: {IsTransformation}", Indent: 1);
                if (IsTransformation)
                    Transformation.DebugOutput(2);
                Utils.Log($"{nameof(IsOptional)}: {IsOptional}", Indent: 1);
                Utils.Log($"{nameof(DescriptionAddition)}: {DescriptionAddition}", Indent: 1);
                Utils.Log($"{nameof(SummaryAddition)}: {SummaryAddition}", Indent: 1);
                Utils.Log($"{nameof(Symbols)}: {Symbols?.Count ?? 0}", Indent: 1);
                if (!Symbols.IsNullOrEmpty())
                    foreach ((char color, string symbol) in Symbols)
                        Utils.Log("::{{" + color + "|" + symbol + "}}", Indent: 2);
            }
        }
        public AnatomyConfiguration(GameObjectBlueprint DataBucket)
            : this()
        {
            if (DataBucket.TryGetTagValueForData("Anatomies", out string anatomies))
                Anatomies = Utils.GetVersionSafeParser<List<string>>()?.Invoke(anatomies);

            if (DataBucket.TryGetTagValueForData("Anatomy", out string anatomy))
            {
                var anatomyList = new List<string>() { anatomy };
                if (!Anatomies.IsNullOrEmpty())
                    anatomyList.AddRange(Anatomies);
                Anatomies = anatomyList;
            }

            if (!DataBucket.xTags.IsNullOrEmpty()
                && DataBucket.xTags.TryGetValue(TransformXTag, out var transformationData))
            {
                if (transformationData.ContainsKey("Anatomies"))
                {
                    var anatomyList = Utils.GetVersionSafeParser<List<string>>()?.Invoke(transformationData["Anatomies"]);
                    if (!Anatomies.IsNullOrEmpty())
                        anatomyList.AddRange(Anatomies);
                    Anatomies = anatomyList;
                }
                if (transformationData.ContainsKey("Anatomy"))
                {
                    var anatomyList = new List<string>() { transformationData["Anatomy"] };
                    if (!Anatomies.IsNullOrEmpty())
                        anatomyList.AddRange(Anatomies);
                    Anatomies = anatomyList;
                }
                Transformation = new(transformationData);
            }

            if (Anatomies.IsNullOrEmpty()
                || Anatomies.Count() <= 1)
                DataBucket.AssignStringFieldFromTag(nameof(DisplayName), ref DisplayName);

            DataBucket.AssignStringFieldFromTag(nameof(Category), ref Category);

            IsDifficult = DataBucket.HasTag("Difficult")
                || DataBucket.HasTag("Sucks");

            Utils.Log($"{typeof(AnatomyConfiguration).CallChain(".ctor")}({nameof(GameObjectBlueprint)}: {DataBucket.Name})");

            Utils.Log($"{nameof(Anatomies)}:", Indent: 1);
            foreach (string anatomyEntry in Anatomies ?? new List<string>(0))
                Utils.Log($"::{anatomyEntry}", Indent: 2);

            Utils.Log($"{nameof(IsDifficult)}: {IsDifficult}", Indent: 1);

            if (DataBucket.HasTag("Transformation"))
                Transformation = new()
                {
                    RenderString = DataBucket.GetTag("TransormationRenderString").Coalesce(DataBucket.GetTag("xFormRenderString")),
                    Tile = DataBucket.GetTag("TransormationTile").Coalesce(DataBucket.GetTag("xFormTile")),
                    Property = DataBucket.GetTag("TransormationProperty").Coalesce(DataBucket.GetTag("xFormProperty")),
                    Species = DataBucket.GetTag("TransormationSpecies").Coalesce(DataBucket.GetTag("xFormSpecies")),
                    TileColor = DataBucket.GetTag("TransormationTileColor").Coalesce(DataBucket.GetTag("xFormTile")),
                    DetailColor = DataBucket.GetTag("TransormationDetailColor").Coalesce(DataBucket.GetTag("xFormDetailColor")),
                    Mutations = Utils.GetVersionSafeParser<List<string>>()?.Invoke(DataBucket.GetTag("TransormationMutations").Coalesce(DataBucket.GetTag("xFormMutations")))
                };

            Utils.Log($"{nameof(IsTransformation)}: {IsTransformation}", Indent: 1);
            if (IsTransformation)
                Transformation.DebugOutput(2);

            IsRestricted = DataBucket.HasTag("Restricted");

            if (DataBucket.TryGetTagValueForData("Optional", out string optionID))
            {
                IsOptional = true;
                IsRestricted = true;
                Utils.Log($"{nameof(IsOptional)}: {IsOptional}", Indent: 1);

                if (!optionID.IsNullOrEmpty())
                {
                    Utils.Log($"{nameof(optionID)}: {optionID}", Indent: 2);
                    if (optionID.EqualsNoCase("AlwaysEnabled"))
                        EnableRestricted = ()
                            => true;
                    else
                        EnableRestricted = ()
                            => XRL.UI.Options.GetOptionBool(optionID);
                }
                else
                if (IsTransformation)
                {
                    Utils.Log($"{nameof(optionID)}: {nameof(Options.EnableBodyPlansAvailableViaRecipe)}", Indent: 2);
                    EnableRestricted = ()
                        => Options.EnableBodyPlansAvailableViaRecipe;
                }
                else
                {
                    Utils.Log($"{nameof(optionID)}: true (no option)", Indent: 2);
                    EnableRestricted = ()
                        => true;
                }
            }
            else
                Utils.Log($"{nameof(IsOptional)}: {IsOptional}", Indent: 1);

            DataBucket.AssignStringFieldFromTag(nameof(ExceptionMessage), ref DescriptionAddition);
            DataBucket.AssignStringFieldFromTag(nameof(ExceptionSummary), ref SummaryAddition);

            DataBucket.AssignStringFieldFromTag(nameof(DescriptionAddition), ref DescriptionAddition);
            DataBucket.AssignStringFieldFromTag(nameof(SummaryAddition), ref SummaryAddition);

            Utils.Log($"{nameof(DescriptionAddition)}: {DescriptionAddition}", Indent: 1);
            Utils.Log($"{nameof(SummaryAddition)}: {SummaryAddition}", Indent: 1);

            Symbols = new();
            if (DataBucket.TryGetTagValueForData(nameof(Symbols), out string symbols))
            {
                if (symbols.Split(";;") is string[] entries)
                    foreach (string entry in entries)
                        ProcessSymbolsString(ref Symbols, entry);
                else
                    ProcessSymbolsString(ref Symbols, symbols);
            }
        }
        public AnatomyConfiguration(string Anatomy, AnatomyConfiguration Source)
            : this(
                Anatomies: null,
                Anatomy: Anatomy,
                DisplayName: Source.DisplayName,
                Category: Source.Category,
                IsDifficult: Source.IsDifficult,
                IsMechanical: Source.IsMechanical,
                Transformation: Source.Transformation,
                IsRestricted: Source.IsRestricted,
                IsOptional: Source.IsOptional,
                EnableRestricted: Source.EnableRestricted,
                DescriptionAddition: Source.DescriptionAddition,
                SummaryAddition: Source.SummaryAddition,
                Symbols: Source.Symbols)
        { }
        public AnatomyConfiguration(Anatomy Anatomy)
            : this(
                Anatomies: null,
                Anatomy: Anatomy.Name,
                DisplayName: null,
                Category: null,
                IsDifficult: false,
                IsMechanical: false,
                Transformation: null,
                IsRestricted: false,
                IsOptional: false,
                EnableRestricted: null,
                DescriptionAddition: null,
                SummaryAddition: null,
                Symbols: new())
        { }

        public static void ProcessSymbolsString(
            ref List<KeyValuePair<char, string>> Symbols,
            string Entry
            )
        {
            Symbols ??= new();
            char color = '\0';
            string symbol = null;
            if (Entry.Split("::") is string[] kvp)
            {
                if (kvp.Length > 1)
                {
                    if (!kvp[0].IsNullOrEmpty()
                        && !kvp[1].IsNullOrEmpty())
                    {
                        symbol = kvp[1];
                        if (!kvp[0].EqualsNoCase("null"))
                            color = kvp[0][0];
                    }
                }
                else
                if (kvp.Length > 0)
                    if (!kvp[0].IsNullOrEmpty())
                        symbol = kvp[0];
            }
            if (!symbol.IsNullOrEmpty())
                Symbols.Add(new(color, symbol));
        }

        public IEnumerable<AnatomyConfiguration> FromAnatomiesList()
        {
            if (!Anatomy.IsNullOrEmpty())
                yield return new(Anatomy, this);

            if (!Anatomies.IsNullOrEmpty())
                foreach (var anatomyConfiguration in Anatomies.Select(a => new AnatomyConfiguration(a, this)))
                    yield return anatomyConfiguration;
        }

        public bool AllowSelection()
            => !IsRestricted
            || (IsOptional
                && (EnableRestricted?.Invoke() ?? true));

        public string GetAnatomy()
            => Anatomy
            ?? Anatomies?[0];
    }

    public static class AnatomyConfigurationExtensions
    {
        public static bool AllowSelection(this IEnumerable<AnatomyConfiguration> AnatomyConfigurations)
            => AnatomyConfigurations?.All(e => e.AllowSelection())
            ?? false
            ;
        public static bool IsExcluded(this IEnumerable<AnatomyConfiguration> AnatomyConfigurations)
            => !(AnatomyConfigurations?.AllowSelection()
                ?? true)
            ;

        public static bool IsMechanical(this IEnumerable<AnatomyConfiguration> AnatomyConfigurations)
            => AnatomyConfigurations?.Any(e => e.IsMechanical)
            ?? false
            ;
        public static bool IsTransformation(this IEnumerable<AnatomyConfiguration> AnatomyConfigurations)
            => AnatomyConfigurations?.Any(e => e.IsTransformation)
            ?? false
            ;
        public static bool IsDifficult(this IEnumerable<AnatomyConfiguration> AnatomyConfigurations)
            => AnatomyConfigurations?.Any(e => e.IsDifficult)
            ?? false
            ;
        public static bool IsOptional(this IEnumerable<AnatomyConfiguration> AnatomyConfigurations)
            => AnatomyConfigurations?.Any(e => e.IsOptional)
            ?? false
            ;

        public static string GetDisplayName(this IEnumerable<AnatomyConfiguration> AnatomyConfigurations)
            => AnatomyConfigurations?.LastOrDefault(c => c.DisplayName != null)?.DisplayName
            ;
        public static string GetCategoryName(this IEnumerable<AnatomyConfiguration> AnatomyConfigurations)
            => AnatomyConfigurations?.LastOrDefault(c => c.Category != null)?.Category
            ;

        public static TransformationData FirstTransformationOrDefault(this IEnumerable<AnatomyConfiguration> AnatomyConfigurations)
            => AnatomyConfigurations?.FirstOrDefault(e => e.IsTransformation)?.Transformation
            ;

        public static bool HasDescriptionAddition(this IEnumerable<AnatomyConfiguration> AnatomyConfigurations)
            => (AnatomyConfigurations
                ?.Any(e => !e.DescriptionAddition.IsNullOrEmpty()))
            ?? false
            ;
        public static IEnumerable<string> DescriptionAdditions(this IEnumerable<AnatomyConfiguration> AnatomyConfigurations)
            => AnatomyConfigurations
                .Where(e => !e.DescriptionAddition.IsNullOrEmpty())
                .Select(e => e.DescriptionAddition)
            ;

        public static bool HasSummaryAddition(this IEnumerable<AnatomyConfiguration> AnatomyConfigurations)
            => (AnatomyConfigurations
                ?.Any(e => !e.SummaryAddition.IsNullOrEmpty()))
            ?? false
            ;
        public static IEnumerable<string> SummaryAdditions(this IEnumerable<AnatomyConfiguration> AnatomyConfigurations)
            => AnatomyConfigurations
                .Where(e => !e.SummaryAddition.IsNullOrEmpty())
                .Select(e => e.SummaryAddition)
            ;

        public static bool HasSymbols(this IEnumerable<AnatomyConfiguration> AnatomyConfigurations)
            => (AnatomyConfigurations
                ?.Any(e => e.Symbols.Any(kvp => !kvp.Value.IsNullOrEmpty())))
            ?? false
            ;
        public static IEnumerable<string> Symbols(this IEnumerable<AnatomyConfiguration> AnatomyConfigurations)
            => AnatomyConfigurations
                .Where(e => e.Symbols.Any(kvp => !kvp.Value.IsNullOrEmpty()))
                .SelectMany(e => e.Symbols.Select(kvp => kvp.Key != '\0' ? $"{"{{"}{kvp.Key}|{kvp.Value}{"}}"}" : kvp.Value))
            ;
    }
}
