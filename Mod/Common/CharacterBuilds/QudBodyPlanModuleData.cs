using XRL.CharacterBuilds;
using XRL.World.Anatomy;

namespace UD_ChooseYourBodyPlan.Mod.CharacterBuilds
{
    public class QudBodyPlanModuleData : AbstractEmbarkBuilderModuleData
    {
        public QudBodyPlanModuleDataRow Selection;

        public bool HasSelection => Selection?.Anatomy != null;

        public QudBodyPlanModuleData()
        {
            Selection = null;
            Version = Utils.ThisMod.Manifest.Version;
        }

        public QudBodyPlanModuleData(string Selection)
            : this()
            => this.Selection = !Selection.IsNullOrEmpty()
                ? new QudBodyPlanModuleDataRow(Selection)
                : null
            ;

        public QudBodyPlanModuleData(Anatomy Selection)
            : this(Selection?.Name)
        { }

        public QudBodyPlanModuleData(BodyPlan Selection)
            : this(Selection?.Entry?.Anatomy)
        { }
    }
}
