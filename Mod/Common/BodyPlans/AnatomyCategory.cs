using System;
using System.Collections.Generic;
using System.Text;

using UD_ChooseYourBodyPlan.Mod.CharacterBuilds.UI;

namespace UD_ChooseYourBodyPlan.Mod
{
    public class AnatomyCategory : IDisposable
    {
        public class CategoryComparer : IComparer<AnatomyCategory>, IDisposable
        {
            public bool DefaultFirst;

            protected CategoryComparer()
            {
                DefaultFirst = false;
            }
            public CategoryComparer(bool DefaultFirst)
                : this()
            {
                this.DefaultFirst = DefaultFirst;
            }

            public int Compare(AnatomyCategory x, AnatomyCategory y)
            {
                if (y == null)
                {
                    if (x != null)
                        return -1;
                    else
                        return 0;
                }
                else
                if (x == null)
                    return 1;

                if (x.Entry.ID == 0)
                    return -1;
                if (y.Entry.ID == 0)
                    return 1;

                return string.Compare(x.DisplayNameStripped, y.DisplayNameStripped);
            }

            public void Dispose()
            {
            }
        }

        public static CategoryComparer Comparer = new(DefaultFirst: false);
        public static CategoryComparer DefaultFirstComparer = new(DefaultFirst: true);
        protected BodyPlanFactory Factory => BodyPlanFactory.Factory;

        public string Name;

        public AnatomyCategoryEntry Entry => Factory?.GetAnatomyCategoryEntry(Name);

        public string DisplayName => GetDisplayName();

        public string DisplayNameStripped => DisplayName?.Strip();

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
                        if (bodyPlanEntry.TryGetBodyPlan(out BodyPlan bodyPlan))
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

        public AnatomyCategory(string Name)
            : this()
        {
            this.Name = Name;
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

        public IEnumerable<BodyPlan> GetBodyPlans(string Default = null)
        {
            foreach (var bodyPlan in BodyPlans)
            {
                if (Default != null
                    && bodyPlan.SetDefault(Default))
                    Default = null;

                if (IsDefaultMatching(bodyPlan))
                    yield return bodyPlan;
            }
        }

        public IEnumerable<BodyPlan> GetBodyPlans(BodyPlan Default = null)
            => GetBodyPlans(Default?.Anatomy)
            ;

        public List<BodyPlanMenuOption> GetBodyPlanMenuOptions(BodyPlan Selected = null)
        {
            var output = new List<BodyPlanMenuOption>();
            foreach (var bodyPlan in BodyPlans)
                if (IsDefaultMatching(bodyPlan))
                    output.Add(bodyPlan.GetMenuOption(Selected));
            return output;
        }

        public AnatomyCategoryMenuData GetMenuData(BodyPlan Selected = null)
            => new()
            {
                ID = Entry.CategoryName,
                DisplayName = DisplayName,
                MenuOptions = GetBodyPlanMenuOptions(Selected),
            };

        public void Dispose()
        {
        }
    }
}
