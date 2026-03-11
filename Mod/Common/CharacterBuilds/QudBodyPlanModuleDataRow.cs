using System;

using UD_BodyPlan_Selection.Mod.BodyPlans;

using static UD_BodyPlan_Selection.Mod.AnatomyConfiguration;

namespace UD_BodyPlan_Selection.Mod.CharacterBuilds
{
    [Serializable]
    public class QudBodyPlanModuleDataRow
    {
        public string Anatomy;
        public TransformationData Transformation;
        public BodyPlanEntry Entry => null; // change this once it's set up.

        public QudBodyPlanModuleDataRow()
        {
            Anatomy = null;
            Transformation = null;
        }
        public QudBodyPlanModuleDataRow(string Anatomy, TransformationData Transformation)
            : this()
        {
            this.Anatomy = Anatomy;
            this.Transformation = Transformation;
        }
        public QudBodyPlanModuleDataRow(AnatomyChoice Choice)
            : this(Choice?.Anatomy?.Name, Choice?.AnatomyConfigurations?.FirstTransformationOrDefault())
        { }
    }
}
