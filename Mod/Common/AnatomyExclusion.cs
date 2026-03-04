using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using XRL;
using XRL.World;
using XRL.World.Anatomy;

using static UD_BodyPlan_Selection.Mod.AnatomyExclusion;

namespace UD_BodyPlan_Selection.Mod
{
    public class AnatomyExclusion
    {
        public static  string TransformXTagPrefix => "UD_BPS_Transformation";

        public delegate bool BooleanOptionDelegate();

        public class TransformationData
        {
            public static string RemoveTag => "*remove";

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
                    if (xTag.TryGetValue(nameof(RenderString), out RenderString)
                        && RenderString.EqualsNoCase(RemoveTag))
                        RenderString = null;

                    if (xTag.TryGetValue(nameof(Tile), out Tile)
                        && Tile.EqualsNoCase(RemoveTag))
                        Tile = null;

                    if (xTag.TryGetValue(nameof(TileColor), out TileColor)
                        && TileColor.EqualsNoCase(RemoveTag))
                        TileColor = null;

                    if (xTag.TryGetValue(nameof(DetailColor), out DetailColor)
                        && DetailColor.EqualsNoCase(RemoveTag))
                        DetailColor = null;

                    if (xTag.TryGetValue(nameof(Species), out Species)
                        && Species.EqualsNoCase(RemoveTag))
                        Species = null;

                    if (xTag.TryGetValue(nameof(Property), out Property)
                        && Property.EqualsNoCase(RemoveTag))
                        Property = null;

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
        
        private readonly IReadOnlyList<string> Anatomies;

        public string Anatomy;

        public bool IsDifficult;

        public bool IsMechanical;

        public TransformationData Transformation;

        public bool IsTransformation => Transformation != null;

        public bool IsOptional;

        public BooleanOptionDelegate ExemptThisExclusion;

        public string ExceptionMessage;
        public string ExceptionSummary;

        public AnatomyExclusion()
        {
            Anatomies = null;
            Anatomy = null;
            IsDifficult = false;
            IsMechanical = false;
            Transformation = null;
            IsOptional = false;
            ExemptThisExclusion = null;
            ExceptionMessage = null;
            ExceptionSummary = null;
        }
        public AnatomyExclusion(
            IReadOnlyList<string> Anatomies,
            string Anatomy,
            bool IsDifficult,
            bool IsMechanical,
            TransformationData Transformation,
            bool IsOptional,
            BooleanOptionDelegate ExemptThisExclusion,
            string ExceptionMessage,
            string ExceptionSummary
            )
            : this()
        {
            this.Anatomies = Anatomies;
            this.Anatomy = Anatomy;
            this.IsDifficult = IsDifficult;
            this.IsMechanical = IsMechanical;
            this.Transformation = Transformation;
            this.IsOptional = IsOptional;
            this.ExemptThisExclusion = ExemptThisExclusion;
            this.ExceptionMessage = ExceptionMessage;
            this.ExceptionSummary = ExceptionSummary;

            if (Anatomies.IsNullOrEmpty())
            {
                Utils.Log($"{typeof(AnatomyExclusion).CallChain(".ctor")}()");
                Utils.Log($"{nameof(Anatomy)}: {Anatomy}", Indent: 1);
                Utils.Log($"{nameof(IsDifficult)}: {IsDifficult}", Indent: 1);
                Utils.Log($"{nameof(IsMechanical)}: {IsMechanical}", Indent: 1);
                Utils.Log($"{nameof(IsTransformation)}: {IsTransformation}", Indent: 1);
                if (IsTransformation)
                    Transformation.DebugOutput(2);
                Utils.Log($"{nameof(IsOptional)}: {IsOptional}", Indent: 1);
                Utils.Log($"{nameof(ExceptionMessage)}: {ExceptionMessage}", Indent: 1);
                Utils.Log($"{nameof(ExceptionSummary)}: {ExceptionSummary}", Indent: 1);
            }
        }
        public AnatomyExclusion(GameObjectBlueprint DataBucket)
            : this()
        {
            if (DataBucket.TryGetTag("Anatomies", out string anatomies))
                Anatomies = Utils.GetVersionSafeParser<List<string>>()?.Invoke(anatomies);

            if (DataBucket.TryGetTag("Anatomy", out string anatomy))
            {
                var anatomyList = new List<string>() { anatomy };
                if (!Anatomies.IsNullOrEmpty())
                    anatomyList.AddRange(Anatomies);
                Anatomies = anatomyList;
            }

            if (!DataBucket.xTags.IsNullOrEmpty()
                && DataBucket.xTags.TryGetValue(TransformXTagPrefix, out var transformationData))
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

            Anatomy = Anatomies?[0];

            IsDifficult = DataBucket.HasTag("Difficult")
                || DataBucket.HasTag("Sucks");

            Utils.Log($"{typeof(AnatomyExclusion).CallChain(".ctor")}({nameof(GameObjectBlueprint)}: {DataBucket.Name})");

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

            if (DataBucket.TryGetTag("Optional", out string optionID))
            {
                IsOptional = true;
                Utils.Log($"{nameof(IsOptional)}: {IsOptional}", Indent: 1);

                if (!optionID.IsNullOrEmpty())
                {
                    Utils.Log($"{nameof(optionID)}: {optionID}", Indent: 2);
                    if (optionID.EqualsNoCase("AlwaysEnabled"))
                        ExemptThisExclusion = ()
                            => true;
                    else
                        ExemptThisExclusion = ()
                            => XRL.UI.Options.GetOptionBool(optionID);
                }
                else
                if (IsTransformation)
                {
                    Utils.Log($"{nameof(optionID)}: {nameof(Options.EnableBodyPlansAvailableViaRecipe)}", Indent: 2);
                    ExemptThisExclusion = ()
                        => Options.EnableBodyPlansAvailableViaRecipe;
                }
                else
                {
                    Utils.Log($"{nameof(optionID)}: true (no option)", Indent: 2);
                    ExemptThisExclusion = ()
                        => true;
                }
            }
            else
                Utils.Log($"{nameof(IsOptional)}: {IsOptional}", Indent: 1);

            DataBucket.TryGetTag(nameof(ExceptionMessage), out ExceptionMessage);
            DataBucket.TryGetTag(nameof(ExceptionSummary), out ExceptionSummary);

            Utils.Log($"{nameof(ExceptionMessage)}: {ExceptionMessage}", Indent: 1);
            Utils.Log($"{nameof(ExceptionSummary)}: {ExceptionSummary}", Indent: 1);
        }
        public AnatomyExclusion(string Anatomy, AnatomyExclusion Source)
            : this(
                    Anatomies: null,
                    Anatomy: Anatomy,
                    IsDifficult: Source.IsDifficult,
                    IsMechanical: Source.IsMechanical,
                    Transformation: Source.Transformation,
                    IsOptional: Source.IsOptional,
                    ExemptThisExclusion: Source.ExemptThisExclusion,
                    ExceptionMessage: Source.ExceptionMessage,
                    ExceptionSummary: Source.ExceptionSummary
                  )
        {
        }
        public AnatomyExclusion(Anatomy Anatomy)
        {
            Anatomies = new List<string>() { Anatomy.Name };
            IsMechanical = Anatomy.Category == BodyPartCategory.MECHANICAL;
            IsOptional = IsMechanical || IsDifficult;
            if (IsOptional)
            {
                if (IsMechanical)
                    ExemptThisExclusion = ()
                        => Options.EnableBodyPlansThatAreRobotic;
                else
                    ExemptThisExclusion = ()
                        => true;
            }
        }

        public IEnumerable<AnatomyExclusion> FromAnatomiesList()
            => Anatomies.Select(a => new AnatomyExclusion(a, this));

        public bool IsExcluded()
            => !IsOptional
            || !(ExemptThisExclusion?.Invoke() ?? true);
    }

    public static class AnatomyExclusionExtensions
    {
        public static bool IsExcluded(this IEnumerable<AnatomyExclusion> AnatomyExclusions)
            => AnatomyExclusions.Any(e => e.IsExcluded())
            ;
        public static bool Include(this IEnumerable<AnatomyExclusion> AnatomyExclusions)
            => AnatomyExclusions == null
            || !AnatomyExclusions.IsExcluded()
            ;
        public static bool IsMechanical(this IEnumerable<AnatomyExclusion> AnatomyExclusions)
            => AnatomyExclusions.Any(e => e.IsMechanical)
            ;
        public static bool IsTransformation(this IEnumerable<AnatomyExclusion> AnatomyExclusions)
            => AnatomyExclusions.Any(e => e.IsTransformation)
            ;
        public static bool IsDifficult(this IEnumerable<AnatomyExclusion> AnatomyExclusions)
            => AnatomyExclusions.Any(e => e.IsDifficult)
            ;
        public static bool IsOptional(this IEnumerable<AnatomyExclusion> AnatomyExclusions)
            => AnatomyExclusions.Any(e => e.IsOptional)
            ;

        public static TransformationData FirstTransformationOrDefault(this IEnumerable<AnatomyExclusion> AnatomyExclusions)
            => AnatomyExclusions?.FirstOrDefault(e => e.IsTransformation)?.Transformation
            ;

        public static bool HasExceptionMessage(this IEnumerable<AnatomyExclusion> AnatomyExclusions)
            => AnatomyExclusions.Any(e => !e.ExceptionMessage.IsNullOrEmpty())
            ;
        public static IEnumerable<string> ExceptionMessages(this IEnumerable<AnatomyExclusion> AnatomyExclusions)
            => AnatomyExclusions
                .Where(e => !e.ExceptionMessage.IsNullOrEmpty())
                .Select(e => e.ExceptionMessage)
            ;

        public static bool HasExceptionSummary(this IEnumerable<AnatomyExclusion> AnatomyExclusions)
            => AnatomyExclusions.Any(e => !e.ExceptionSummary.IsNullOrEmpty())
            ;
        public static IEnumerable<string> ExceptionSummaries(this IEnumerable<AnatomyExclusion> AnatomyExclusions)
            => AnatomyExclusions
                .Where(e => !e.ExceptionSummary.IsNullOrEmpty())
                .Select(e => e.ExceptionSummary)
            ;
    }
}
