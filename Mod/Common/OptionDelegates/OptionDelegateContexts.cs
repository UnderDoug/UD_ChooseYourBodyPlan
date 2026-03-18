using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XRL.Collections;
using XRL.World;

namespace UD_ChooseYourBodyPlan.Mod
{
    [Serializable]
    public class OptionDelegateContexts : List<OptionDelegateContext>
    {
        public OptionDelegateContexts()
        {
        }

        public OptionDelegateContexts(IReadOnlyList<OptionDelegateContext> Source)
            : this()
        {
            if (!Source.IsNullOrEmpty())
                foreach (var optionDelegate in Source)
                    Merge(optionDelegate);
        }

        public OptionDelegateContexts(OptionDelegateContexts Source)
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

        public OptionDelegateContexts Merge(OptionDelegateContexts Other)
        {
            if (!Other.IsNullOrEmpty())
                foreach (var otherOptionDelegate in Other)
                    Merge(otherOptionDelegate);

            return this;
        }

        public void Merge(OptionDelegateContext Other)
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
