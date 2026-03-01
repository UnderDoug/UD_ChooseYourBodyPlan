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
            public string DetailColor;
            public string Species;
            public string Property;
            public List<string> Mutations;

            public TransformationData()
            {
                Tile = null;
                DetailColor = null;
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
                    xTag.TryGetValue(nameof(DetailColor), out DetailColor);
                    xTag.TryGetValue(nameof(Species), out Species);
                    xTag.TryGetValue(nameof(Property), out Property);
                    if (xTag.TryGetValue(nameof(Mutations), out string mutations))
                        Mutations = XmlDataHelper.TryGetAttributeParser<List<string>>()?.Invoke(mutations);
                }
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
                Anatomies = XmlDataHelper.TryGetAttributeParser<List<string>>()?.Invoke(anatomies);

            if (DataBucket.TryGetTag("Anatomy", out string anatomy))
            {
                var anatomyList = new List<string>() { anatomy };
                if (!Anatomies.IsNullOrEmpty())
                    anatomyList.AddRange(Anatomies);
                Anatomies = anatomyList;
            }

            IsDifficult = DataBucket.HasTag("Difficult")
                || DataBucket.HasTag("Sucks");

            if (DataBucket.xTags.TryGetValue(TransformXTagPrefix, out var transformationData))
                Transformation = new(transformationData);

            if (DataBucket.HasTag("Transformation"))
                Transformation = new()
                {
                    RenderString = DataBucket.GetTag("TransormationRenderString").Coalesce(DataBucket.GetTag("xFormRenderString")),
                    Tile = DataBucket.GetTag("TransormationTile").Coalesce(DataBucket.GetTag("xFormTile")),
                    Property = DataBucket.GetTag("TransormationProperty").Coalesce(DataBucket.GetTag("xFormProperty")),
                    Species = DataBucket.GetTag("TransormationSpecies").Coalesce(DataBucket.GetTag("xFormSpecies")),
                    DetailColor = DataBucket.GetTag("TransormationDetailColor").Coalesce(DataBucket.GetTag("xFormDetailColor")),
                    Mutations = XmlDataHelper.TryGetAttributeParser<List<string>>()?.Invoke(DataBucket.GetTag("TransormationMutations").Coalesce(DataBucket.GetTag("xFormMutations")))
                };

            if (DataBucket.TryGetTag("Optional", out string optionID))
            {
                IsOptional = true;
                if (optionID != null)
                    ExemptThisExclusion = ()
                        => XRL.UI.Options.GetOptionBool(optionID);
                else
                if (IsTransformation)
                    ExemptThisExclusion = ()
                        => Options.EnableBodyPlansAvailableViaRecipe;
                else
                    ExemptThisExclusion = ()
                        => Options.EnableBodyPlansThatSuck;
            }

            DataBucket.TryGetTag(nameof(ExceptionMessage), out ExceptionMessage);
            DataBucket.TryGetTag(nameof(ExceptionSummary), out ExceptionSummary);
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
            => IsOptional
            && (ExemptThisExclusion?.Invoke() ?? false);
    }
}
