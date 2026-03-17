using System;
using System.Collections.Generic;
using System.Text;

using XRL.UI.Framework;

namespace UD_ChooseYourBodyPlan.Mod.CharacterBuilds.UI
{
    public class AnatomyCategoryMenuData : CategoryMenuData
    {
        public string ID { set => Id = value; }

        public string DisplayName { set => Title = value; }

        public List<BodyPlanMenuOption> MenuOptions { set => menuOptions = new(value); }
    }
}
