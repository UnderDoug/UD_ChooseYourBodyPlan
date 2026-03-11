using System;
using System.Collections.Generic;
using System.Text;

namespace UD_BodyPlan_Selection.Mod.BodyPlans
{
    public class BodyPlanEntry
    {
        public string Name;
        public string DisplayName;
        public BodyPlanRenderable Renderable;
        public TransformationData Transformation;
        public List<TextElement> TextElements;

        public bool IsBase;
    }
}
