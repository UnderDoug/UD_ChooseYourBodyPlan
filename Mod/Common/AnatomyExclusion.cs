using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using XRL;
using XRL.World;
using XRL.World.Anatomy;

namespace UD_BodyPlan_Selection.Mod
{
    public class AnatomyExclusion
    {
        public static  string TransformXTagPrefix => "UD_BPS_Transformation";

        public delegate bool BooleanOptionDelegate();

        public class TransformationData
        {
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
                    xTag.TryGetValue(nameof(RenderString), out RenderString);
                    xTag.TryGetValue(nameof(Tile), out Tile);
                    xTag.TryGetValue(nameof(TileColor), out TileColor);
                    xTag.TryGetValue(nameof(DetailColor), out DetailColor);
                    xTag.TryGetValue(nameof(Species), out Species);
                    xTag.TryGetValue(nameof(Property), out Property);
                    if (xTag.TryGetValue(nameof(Mutations), out string mutations))
                        Mutations = Utils.GetVersionSafeParser<List<string>>()?.Invoke(mutations);
                }
            }

            public void DebugOutput(int Indent = 0)
            {
                Utils.Log($"{nameof(RenderString)}: {RenderString}", Indent: Indent);
                Utils.Log($"{nameof(Tile)}: {Tile}", Indent: Indent);
                Utils.Log($"{nameof(TileColor)}: {TileColor}", Indent: Indent);
                Utils.Log($"{nameof(DetailColor)}: {DetailColor}", Indent: Indent);
                Utils.Log($"{nameof(Species)}: {Species}", Indent: Indent);
                Utils.Log($"{nameof(Property)}: {Property}", Indent: Indent);
                Utils.Log($"{nameof(Mutations)}:", Indent: Indent);
                if (Mutations.IsNullOrEmpty())
                    Utils.Log("::None", Indent: Indent + 1);
                else
                    foreach (string mutation in Mutations)
                        Utils.Log($"::{mutation}", Indent: Indent + 1);
            }
        }
        
        public IReadOnlyList<string> Anatomies;

        public string Anatomy => Anatomies?[0];

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
            this.IsDifficult = IsDifficult;
            this.IsMechanical = IsMechanical;
            this.Transformation = Transformation;
            this.IsOptional = IsOptional;
            this.ExemptThisExclusion = ExemptThisExclusion;
            this.ExceptionMessage = ExceptionMessage;
            this.ExceptionSummary = ExceptionSummary;
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
                    Utils.Log($"{nameof(optionID)}: {nameof(Options.EnableBodyPlansThatSuck)}", Indent: 2);
                    ExemptThisExclusion = ()
                        => Options.EnableBodyPlansThatSuck;
                }
            }
            else
                Utils.Log($"{nameof(IsOptional)}: {IsOptional}", Indent: 1);

            DataBucket.TryGetTag(nameof(ExceptionMessage), out ExceptionMessage);
            DataBucket.TryGetTag(nameof(ExceptionSummary), out ExceptionSummary);

            Utils.Log($"{nameof(ExceptionMessage)}: {ExceptionMessage}", Indent: 1);
            Utils.Log($"{nameof(ExceptionSummary)}: {ExceptionSummary}", Indent: 1);
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
                        => Options.EnableBodyPlansThatSuck;
            }
        }

        public bool IsExcluded()
            => !IsOptional
            || !(ExemptThisExclusion?.Invoke() ?? true);
    }
}
