using XRL.World.Anatomy;

using static UD_BodyPlan_Selection.Mod.AnatomyExclusion;

namespace XRL.CharacterBuilds.Qud
{
    public class Qud_UD_BodyPlanModuleData : AbstractEmbarkBuilderModuleData
    {
        public Qud_UD_BodyPlanModuleDataRow Selection;

        public bool HasSelection => Selection?.Anatomy != null; 

        public Qud_UD_BodyPlanModuleData()
            => Selection = null;

        public Qud_UD_BodyPlanModuleData(string Selection, TransformationData Transformation)
            : this()
            => this.Selection = !Selection.IsNullOrEmpty()
                ? new Qud_UD_BodyPlanModuleDataRow(Selection, Transformation)
                : null
            ;

        public Qud_UD_BodyPlanModuleData(Anatomy Selection, TransformationData Transformation = null)
            : this(Selection?.Name, Transformation)
        { }

        public Qud_UD_BodyPlanModuleData(Qud_UD_BodyPlanModule.AnatomyChoice Selection)
            : this(Selection?.Anatomy, Selection?.AnatomyExclusion?.Transformation)
        { }
    }
}
