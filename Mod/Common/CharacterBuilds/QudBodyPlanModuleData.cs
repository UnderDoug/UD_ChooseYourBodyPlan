using UD_BodyPlan_Selection.Mod.BodyPlans;

using XRL.CharacterBuilds;
using XRL.World.Anatomy;

using static UD_BodyPlan_Selection.Mod.AnatomyConfiguration;

namespace UD_BodyPlan_Selection.Mod.CharacterBuilds
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

        public QudBodyPlanModuleData(string Selection, TransformationData Transformation)
            : this()
            => this.Selection = !Selection.IsNullOrEmpty()
                ? new QudBodyPlanModuleDataRow(Selection, Transformation)
                : null
            ;

        public QudBodyPlanModuleData(Anatomy Selection, TransformationData Transformation = null)
            : this(Selection?.Name, Transformation)
        { }

        public QudBodyPlanModuleData(AnatomyChoice Selection)
            : this(Selection?.Anatomy, Selection?.AnatomyConfigurations?.FirstTransformationOrDefault())
        { }
    }
}
