using System;

namespace UD_ChooseYourBodyPlan.Mod.CharacterBuilds
{
    [Serializable]
    public class QudBodyPlanModuleDataRow
    {
        public string Anatomy;

        public BodyPlanEntry Entry => BodyPlanFactory.Factory?.BodyPlanEntryByAnatomyName?.GetValue(Anatomy);

        public AnatomyCategoryEntry Category => Entry?.Category;

        public TransformationData Transformation => Entry?.Transformation;

        public QudBodyPlanModuleDataRow()
        {
            Anatomy = null;
        }
        public QudBodyPlanModuleDataRow(string Anatomy)
            : this()
        {
            this.Anatomy = Anatomy;
        }
        public QudBodyPlanModuleDataRow(BodyPlan Choice)
            : this(Choice?.Entry?.Anatomy?.Name)
        { }
    }
}
