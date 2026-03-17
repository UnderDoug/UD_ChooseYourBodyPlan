using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XRL.Collections;
using XRL.World;

namespace UD_ChooseYourBodyPlan.Mod
{
    [Serializable]
    public class OptionDelegates : List<BaseOptionDelegate>
    {
        public OptionDelegates()
        {
        }

        public OptionDelegates(IReadOnlyList<BaseOptionDelegate> Source)
            : this()
        {
            if (!Source.IsNullOrEmpty())
                foreach (var optionDelegate in Source)
                    Merge(optionDelegate);
        }

        public OptionDelegates(OptionDelegates Source)
            : base(Source)
        {
        }

        public bool Check()
        {
            foreach (var optionDelegate in this)
                if (!Check())
                    return false;

            return true;
        }

        public OptionDelegates Merge(OptionDelegates Other)
        {
            if (!Other.IsNullOrEmpty())
                foreach (var otherOptionDelegate in Other)
                    Merge(otherOptionDelegate);

            return this;
        }

        public void Merge(BaseOptionDelegate Other)
        {
            bool any = false;
            foreach (var optionDelegate in this)
            {
                if (optionDelegate.SameAs(Other))
                {
                    optionDelegate.Merge(Other);
                    any = true;
                }
            }
            if (!any)
                Add(Other);
        }
    }
}
