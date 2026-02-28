using XRL.World.Anatomy;

namespace XRL.CharacterBuilds.Qud
{
    public class Qud_UD_BodyPlanModuleData : AbstractEmbarkBuilderModuleData
    {
        public Qud_UD_BodyPlanModuleDataRow Selection;

        public bool HasSelection => Selection.Anatomy != null; 

        public Qud_UD_BodyPlanModuleData()
            => Selection = null;

        public Qud_UD_BodyPlanModuleData(string Selection)
            : this()
            => this.Selection = !Selection.IsNullOrEmpty()
                ? new Qud_UD_BodyPlanModuleDataRow(Selection)
                : null
            ;

        public Qud_UD_BodyPlanModuleData(Anatomy Selection)
            : this(Selection?.Name)
        { }

        public Qud_UD_BodyPlanModuleData(Qud_UD_BodyPlanModule.AnatomyChoice Selection)
            : this(Selection?.Anatomy)
        { }
    }
}
