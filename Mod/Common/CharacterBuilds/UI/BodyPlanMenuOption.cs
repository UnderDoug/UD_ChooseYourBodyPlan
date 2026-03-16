using System;
using System.Collections.Generic;
using System.Text;

using XRL.UI.Framework;

namespace UD_ChooseYourBodyPlan.Mod.CharacterBuilds.UI
{
    public class BodyPlanMenuOption : PrefixMenuOption
    {
        public string ID { set => Id = value; }

        public bool IsSelected
        {
            get => Prefix == Const.CHECKED;
            set => Prefix = value ? Const.CHECKED : Const.UNCHECKED;
        }

        public string Name { set => Description = value; }

        public string Details { set => LongDescription = value; }

        public BodyPlanRender Render { set => Renderable = value; }
    }
}
