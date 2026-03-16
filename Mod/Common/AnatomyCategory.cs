using System;
using System.Collections.Generic;
using System.Text;

namespace UD_ChooseYourBodyPlan.Mod
{
    public class AnatomyCategory : IDisposable
    {
        protected BodyPlanFactory Factory => BodyPlanFactory.Factory;

        public string Name;

        public AnatomyCategoryEntry Entry => Factory?.GetAnatomyCategoryEntry(Name);

        public string DisplayName => GetDisplayName();

        private List<BodyPlan> _BodyPlans;
        public List<BodyPlan> BodyPlans
        {
            get
            {
                if (_BodyPlans.IsNullOrEmpty())
                {
                    _BodyPlans ??= new();
                    foreach (var bodyPlanEntry in Entry.GetEntries(BodyPlanEntry.IsAvailable))
                    {
                        if (bodyPlanEntry.GetBodyPlan() is BodyPlan bodyPlan)
                        {
                            if (!IsDefault)
                                bodyPlan.Category = this;
                            _BodyPlans.Add(bodyPlan);
                        }
                    }
                }
                return _BodyPlans;
            }
        }

        public bool IsDefault
            => Entry != null
            && Entry.ID == 0
            ;

        public AnatomyCategory()
        {
        }

        public void ClearCachedValues()
        {
            _BodyPlans.Clear();
            _BodyPlans = null;
        }

        public string GetDisplayName()
            => Entry?.GetDisplayName()
            ?? "NO_ANTOMY_CATEGORY_ENTRY";


        public bool IsDefaultMatching(BodyPlan BodyPlan)
            => BodyPlan != null
            && IsDefault == BodyPlan.IsDefault;

        public IEnumerable<BodyPlan> GetBodyPlans()
        {
            foreach (var bodyPlan in BodyPlans)
                if (IsDefaultMatching(bodyPlan))
                    yield return bodyPlan;
        }

        public void Dispose()
        {
        }
    }
}
