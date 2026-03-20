using System;
using System.Collections.Generic;
using System.Text;

using UD_ChooseYourBodyPlan.Mod.CharacterBuilds;

using XRL;
using XRL.CharacterBuilds;
using XRL.CharacterBuilds.Qud;

namespace UD_ChooseYourBodyPlan.Mod
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class HasOptionDelegateAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class OptionDelegateAttribute : Attribute
    {
        public string Name;
        public OptionDelegateAttribute()
        {
            Name = null;
        }
        public OptionDelegateAttribute(string Name)
            : this()
        {
            this.Name = Name;
        }
    }

    public delegate bool OptionDelegate(string TagValue, BodyPlanEntry BodyPlanEntry, EmbarkBuilder Builder);

    public struct OptionDelegateEntry
    {
        public OptionDelegate OptionDelegate;
    }
}
