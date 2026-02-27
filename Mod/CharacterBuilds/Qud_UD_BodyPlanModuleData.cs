using System;
using System.Collections.Generic;
using System.Text;

using XRL.World.Anatomy;

namespace XRL.CharacterBuilds.Qud
{
    public class Qud_UD_BodyPlanModuleData : AbstractEmbarkBuilderModuleData
    {
        public List<Qud_UD_BodyPlanModuleDataRow> Selections;

        public Qud_UD_BodyPlanModuleData()
            => Selections = new();

        public Qud_UD_BodyPlanModuleData(string Selection)
            : this()
            => Selections.Add(
                item: new Qud_UD_BodyPlanModuleDataRow()
                {
                    Anatomy = Selection
                });

        public Qud_UD_BodyPlanModuleData(Anatomy Selection)
            : this(Selection?.Name)
        { }

        public Qud_UD_BodyPlanModuleData(Qud_UD_BodyPlanModule.AnatomyChoice Selection)
            : this(Selection.Anatomy)
        { }
    }
}
