using System;

namespace UD_ChooseYourBodyPlan.Mod.CharacterBuilds
{
    [Serializable]
    public class QudBodyPlanModuleDataRow
    {
        public string Anatomy;

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

        public BodyPlanEntry GetBodyPlanEntry()
            => BodyPlanFactory.Factory
                ?.BodyPlanEntryByAnatomyName
                ?.GetValue(Anatomy);

        public TransformationData GetTransformation()
            => GetBodyPlanEntry()
                ?.Transformation;
    }
}
