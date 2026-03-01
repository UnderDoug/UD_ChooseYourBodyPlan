using System;

using static UD_BodyPlan_Selection.Mod.AnatomyExclusion;

namespace XRL.CharacterBuilds.Qud
{
    [Serializable]
    public class Qud_UD_BodyPlanModuleDataRow
    {
        public string Anatomy;
        public TransformationData Transformation;
        public Qud_UD_BodyPlanModuleDataRow()
        {
            Anatomy = null;
            Transformation = null;
        }
        public Qud_UD_BodyPlanModuleDataRow(string Anatomy, TransformationData Transformation)
            : this()
        {
            this.Anatomy = Anatomy;
            this.Transformation = Transformation;
        }
        public Qud_UD_BodyPlanModuleDataRow(Qud_UD_BodyPlanModule.AnatomyChoice Choice)
            : this(Choice?.Anatomy?.Name, Choice?.AnatomyExclusion?.Transformation)
        { }
    }
}
