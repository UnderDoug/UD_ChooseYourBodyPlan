using System;

namespace XRL.CharacterBuilds.Qud
{
    [Serializable]
    public class Qud_UD_BodyPlanModuleDataRow
    {
        public string Anatomy;
        public Qud_UD_BodyPlanModuleDataRow()
            => Anatomy = null
            ;
        public Qud_UD_BodyPlanModuleDataRow(string Anatomy)
            : this()
            => this.Anatomy = Anatomy
            ;
        public Qud_UD_BodyPlanModuleDataRow(Qud_UD_BodyPlanModule.AnatomyChoice Choice)
            : this(Choice?.Anatomy?.Name)
        { }
    }
}
