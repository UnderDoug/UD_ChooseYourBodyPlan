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
            {
                foreach (var spec in Other)
                {
                    if (spec is BaseOptionDelegate optionDelegateSpec)
                        Merge(optionDelegateSpec);
                    else
                        if (!Contains(spec))
                        Add(spec);
                }
            }
            return this;
        }

        public void Merge(BaseOptionDelegate OptionDelegate)
        {
            foreach (var spec in this)
            {
                if (spec is BaseOptionDelegate optionDelegateSpec)
                {
                    optionDelegateSpec.Merge(OptionDelegate);
                }
            }
        }
    }
}
