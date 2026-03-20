using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XRL.Collections;
using XRL.World;

namespace UD_ChooseYourBodyPlan.Mod
{
    [Serializable]
    public class OptionDelegateContexts : HashSet<OptionDelegateContext>
    {
        public class OptionDelegateContextEqualityComparer : IEqualityComparer<OptionDelegateContext>
        {
            public bool Equals(OptionDelegateContext x, OptionDelegateContext y)
                => x != null
                ? x.SameAs(y)
                : y == null
                ;

            public int GetHashCode(OptionDelegateContext obj)
                => obj?.DelegateName?.GetHashCode() ?? 0
                 ^ obj?.TagValue?.GetHashCode() ?? 0;
        }

        public static OptionDelegateContextEqualityComparer EqualityComparer = new();

        public OptionDelegateContexts()
            : base(EqualityComparer)
        { }

        public OptionDelegateContexts(IReadOnlyList<OptionDelegateContext> Source)
            : base(Source, EqualityComparer)
        { }

        public OptionDelegateContexts(OptionDelegateContexts Source)
            : base(Source, EqualityComparer)
        { }

        public bool Check(BodyPlanEntry BodyPlanEntry)
        {
            foreach (var optionDelegate in this)
                if (!optionDelegate.Check(BodyPlanEntry))
                    return false;

            return true;
        }

        public int AddRange(IEnumerable<OptionDelegateContext> Range)
        {
            int count = 0;
            if (!Range.IsNullOrEmpty())
            {
                foreach (var item in Range)
                {
                    if (Add(item))
                        count++;
                }
            }
            return count;
        }
    }
}
