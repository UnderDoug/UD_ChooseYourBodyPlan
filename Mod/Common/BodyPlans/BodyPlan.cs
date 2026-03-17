using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XRL;
using XRL.Collections;
using XRL.UI.Framework;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;

using UD_ChooseYourBodyPlan.Mod.CharacterBuilds.UI;

using Event = XRL.World.Event;

namespace UD_ChooseYourBodyPlan.Mod
{
    public class BodyPlan : IDisposable
    {
        public readonly struct BodyPlanEqualityComparer : IEqualityComparer<BodyPlan>, IDisposable
        {
            public readonly bool ByRef;
            public BodyPlanEqualityComparer(bool ByRef)
            {
                this.ByRef = ByRef;
            }

            public readonly bool Equals(BodyPlan x, BodyPlan y)
                => ByRef
                ? x == y
                : x?.Anatomy == y?.Anatomy
                ;

            public int GetHashCode(BodyPlan obj)
                => ByRef
                ? obj.GetHashCode()
                : (obj?.Anatomy?.GetHashCode() ?? 0);

            public void Dispose()
            {
            }
        }

        public class LimbTreeBranch
        {
            public static bool IsTrueKin = Utils.IsTruekinEmbarking;

            public string Indent;
            public string CardinalDescription;
            public string Description;
            public string FinalType;
            public string NaturalEquipment;
            public string Extra;
            public bool HasNaturalEquipment => !NaturalEquipment.IsNullOrEmpty();
            public bool NoCyber;

            public override string ToString()
            {
                string output = CardinalDescription;
                if (HasNaturalEquipment)
                    output = output.Replace(Description, "{{w|" + Description + "}}");

                if (!FinalType.IsNullOrEmpty())
                    output = $"{output} ({FinalType})";

                if (HasNaturalEquipment)
                    output = $"{output}{NaturalEquipment}";

                if (!Extra.IsNullOrEmpty())
                    output = $"{output} {Extra}";

                output = $"{Indent}{output}";

                if (IsTrueKin
                    && NoCyber
                    && BodyPlanFactory.Factory
                        ?.GetTextElements("NoCyber")
                        ?.SymbolsByName
                        ?.GetValueOrDefault("NoCyber") is TextElements.Symbol noCyber)
                    output += $" {noCyber}";

                return output;
            }
        }

        protected ref StringBuilder SB => ref BodyPlanFactory.SB;

        protected ref GameObject SampleCreature => ref BodyPlanFactory.SampleCreature;

        public static BodyPlanEqualityComparer AnatomyEqualityComparer = new(ByRef: false);

        public string Anatomy;
        public BodyPlanEntry Entry => BodyPlanFactory.Factory?.RequireBodyPlanEntry(Anatomy);

        public string CategoryName => Entry?.Category?.CategoryName;

        public AnatomyCategory Category;

        public string DisplayName => GetDisplayName();

        public string DisplayNameShowDefault => GetDisplayName(ShowDefault: true);

        public string DisplayNameWithSymbols => GetDisplayName(ShowSymbols: true);

        public string DisplayNameShowDefaultWithSymbols => GetDisplayName(ShowDefault: true, ShowSymbols: true);

        public string DisplayNameStripped => DisplayName?.Strip();

        public BodyPlanRender Render => IsDefault ? Utils.EmbarkingGenoSubtypeRender : Entry?.GetRender();

        private string _Description;
        public string Description
        {
            get
            {
                string description = _Description;
                if (description.IsNullOrEmpty())
                {
                    description = GetDescription();
                    if (CacheDescription)
                        _Description = description;
                }
                return description;
            }
        }
        private bool CacheDescription;

        private string _Summary;
        public string Summary
        {
            get
            {
                string summary = _Summary;
                if (summary.IsNullOrEmpty())
                {
                    summary = GetSummary();
                    if (CacheSummary)
                        _Summary = summary;
                }
                return summary;
            }
        }
        private bool CacheSummary;

        public List<TextElements> TextElements => Entry?.TextElements;

        public bool IsDefault;

        #region CachedCollections

        protected List<LimbTreeBranch> LimbTreeBranches;
        protected List<string> BodyLimbTreeLines;

        protected Dictionary<BodyPartType, int> LimbCounts;
        protected List<string> LimbCountLines;

        protected List<string> DescriptionLines;
        private bool CacheDescriptionLines;

        protected List<string> SummaryLines;

        #endregion

        public BodyPlan()
        {
        }

        public BodyPlan(string Anatomy)
            : this()
        {
            this.Anatomy = Anatomy;
        }

        public void ClearCachedValues()
        {
            _Description = null;
            _Summary = null;

            LimbTreeBranches?.Clear();
            LimbTreeBranches = null;
            BodyLimbTreeLines?.Clear();
            BodyLimbTreeLines = null;

            LimbCounts?.Clear();
            LimbCounts = null;
            LimbCountLines?.Clear();
            LimbCountLines = null;

            DescriptionLines?.Clear();
            DescriptionLines = null;

            SummaryLines?.Clear();
            SummaryLines = null;
        }

        public bool SameAs(BodyPlan Other)
            => AnatomyEqualityComparer.Equals(this, Other);

        public GameObject ConfigureSampleCreature(ref GameObject SampleCreature)
        {
            SampleCreature ??= GameObject.CreateSample("Humanoid");
            if (SampleCreature.Body.Anatomy != Anatomy)
                Entry.Anatomy.ApplyTo(SampleCreature.Body);
            return SampleCreature;
        }

        public IEnumerable<string> GetDescriptionBefores(Predicate<TextElements> Where = null)
            => TextElements?.GetDescriptionBefores(Where)
            ?? Enumerable.Empty<string>()
            ;

        public IEnumerable<string> GetDescriptionAfters(Predicate<TextElements> Where = null)
            => TextElements?.GetDescriptionAfters(Where)
            ?? Enumerable.Empty<string>()
            ;

        public IEnumerable<string> GetSummaryBefores(Predicate<TextElements> Where = null)
            => TextElements?.GetSummaryBefores(Where)
            ?? Enumerable.Empty<string>()
            ;

        public IEnumerable<string> GetSummaryAfters(Predicate<TextElements> Where = null)
            => TextElements?.GetSummaryAfters(Where)
            ?? Enumerable.Empty<string>()
            ;

        public IEnumerable<string> GetSymbols(
            Predicate<TextElements> Where = null,
            Predicate<TextElements.Symbol> Filter = null
            )
            => TextElements?.GetSymbols(Where, Filter)
            ?? Enumerable.Empty<string>()
            ;

        public IEnumerable<string> GetLegends(Predicate<TextElements> Where = null)
            => TextElements?.GetLegends(Where)
            ?? Enumerable.Empty<string>()
            ;

        public string GetDisplayName(bool ShowDefault = false, bool ShowSymbols = false)
        {
            SB.Clear();

            string displayName = Anatomy?.SplitCamelCase();

            if (displayName != null
                && Entry?.DisplayName is string entryDisplayName)
                displayName = entryDisplayName;

            SB.Append(displayName ?? BodyPlanEntry.MISSING_ANATOMY);

            if (SB.ToString() != BodyPlanEntry.MISSING_ANATOMY)
            {
                if (ShowDefault
                    && IsDefault)
                    SB.Append(" (default)");

                if (ShowSymbols
                    && GetSymbols() is IEnumerable<string> symbols
                    && !symbols.IsNullOrEmpty())
                    symbols.Aggregate(SB.Append(' '), (a, n) => a.Append(n));
            }

            return SB.ToString();
        }

        private static string ColorBlack(string String)
            => "{{K|" + String + "}}"
            ;
        private static LimbTreeBranch InitializeLimbTreeBranch(BodyPart BodyPart)
            => BodyPart.InitializeLimbTreeBranch()
            ;

        public IEnumerable<string> GetBodyLimbTreeLines()
        {
            if (BodyLimbTreeLines.IsNullOrEmpty()
                && ConfigureSampleCreature(ref SampleCreature).Body is Body sampleBody)
            {
                using var limbTreeBranches = ScopeDisposedList<LimbTreeBranch>.GetFromPoolFilledWith(
                    items: sampleBody.GetLimbTree(
                        IndentProc: ColorBlack,
                        BodyPartProc: InitializeLimbTreeBranch,
                        Treat0DepthPartsAsRoot: true)
                    );

                BodyLimbTreeLines ??= new();
                foreach (var limbTreeBranch in limbTreeBranches)
                    BodyLimbTreeLines.Add(limbTreeBranch.ToString());
            }
            return BodyLimbTreeLines;
        }

        public Dictionary<BodyPartType, int> GetLimbCounts()
        {
            if (LimbCounts.IsNullOrEmpty())
            {
                LimbCounts ??= new();

                using var limbs = ScopeDisposedList<BodyPartType>.GetFromPool();

                if (ConfigureSampleCreature(ref SampleCreature)?.Body?.GetParts() is not List<BodyPart> bodyParts)
                    return LimbCounts;

                foreach (var part in bodyParts)
                    limbs.Add(part.VariantTypeModel());

                if (limbs.IsNullOrEmpty())
                    return LimbCounts;

                foreach (var limb in limbs)
                {
                    if (LimbCounts.ContainsKey(limb))
                        LimbCounts[limb]++;
                    else
                        LimbCounts[limb] = 1;
                }
            }
            return LimbCounts;
        }

        public IEnumerable<string> GetLimbCountLines(Dictionary<BodyPartType, int> LimbCounts)
        {
            if (!LimbCountLines.IsNullOrEmpty())
            {
                foreach (var limbCountLine in LimbCountLines)
                    yield return limbCountLine;
                yield break;
            }

            LimbCountLines ??= new();
            foreach ((var limb, var count) in LimbCounts)
            {
                string timesColored = "}}x {{Y|";
                string limbName = limb.FinalType ?? limb.Type;
                string limbPluralName = null;

                if (limb.DescriptionPrefix is string prefix)
                    limbName = $"{prefix} {limbName}";

                if (limb.Plural.GetValueOrDefault())
                    limbPluralName = timesColored + limbName;

                if (limbName.EqualsNoCase("feet"))
                    limbPluralName = timesColored + "Worn on Feet";

                if (limbName.EqualsNoCase("foot"))
                    limbPluralName = timesColored + "Feet";

                limbName = timesColored + limbName;

                string output = "{{Y|" + count.Things(limbName, limbPluralName) + "}}";

                if (limb.FinalType.EqualsNoCase("body")
                    && SampleCreature.Body.CalculateMobilitySpeedPenalty() is int moveSpeedPenalty
                    && moveSpeedPenalty > 0)
                {
                    output = $"{output} {"{{r|"}{-moveSpeedPenalty} MS{"}}"}";
                }

                if (GetDefaultBehaviorString(limb.DefaultBehavior) is string defaultBehaviourString)
                    output = $"{output}{defaultBehaviourString}";

                LimbCountLines.Add(output);
                yield return output;
            }
        }

        private static bool HasDefaultEquipment(BodyPart BodyPart)
            => !BodyPart
                ?.VariantTypeModel()
                ?.DefaultBehavior.IsNullOrEmpty()
            ?? false
            ;
        public IEnumerable<string> GetDescriptionLines()
        {
            var descriptionLines = new List<string>();
            if (DescriptionLines != null)
                descriptionLines.AddRange(DescriptionLines);

            if (DescriptionLines.IsNullOrEmpty()
                || !CacheDescriptionLines)
            {
                descriptionLines.Clear();

                string empty = "";
                bool newline = false;
                foreach (string descriptionBefore in GetDescriptionBefores())
                {
                    newline = true;
                    descriptionLines.Add(descriptionBefore);
                }

                if (newline)
                {
                    newline = false;
                    descriptionLines.Add(empty);
                }

                bool didLimbs = false;
                if (GetBodyLimbTreeLines() is IEnumerable<string> bodyLimbTree)
                {
                    descriptionLines.Add("Includes the following body part slots:");

                    if (!bodyLimbTree.IsNullOrEmpty())
                    {
                        foreach (var limbTreeBranch in bodyLimbTree)
                        {
                            didLimbs = true;
                            newline = true;
                            descriptionLines.Add(limbTreeBranch.ToString());
                        }
                        CacheDescriptionLines = didLimbs;
                    }
                    else
                    {
                        CacheDescriptionLines = false;
                        descriptionLines.Add("{{R|" + Const.RTRNG + "}} {{W|Something's gone wrong!}}");
                    }

                    if (newline)
                    {
                        newline = false;
                        descriptionLines.Add(empty);
                    }

                    if (didLimbs
                        && SampleCreature.Body.GetFirstPart(HasDefaultEquipment) != null)
                    {
                        newline = true;
                        descriptionLines.Add("{{w|Has natural equipment}}");
                    }
                }

                if (newline)
                {
                    newline = false;
                    descriptionLines.Add(string.Empty);
                }

                foreach (string descriptionAfter in GetDescriptionAfters())
                {
                    newline = true;
                    descriptionLines.Add(descriptionAfter);
                }

                if (newline)
                {
                    newline = false;
                    descriptionLines.Add(empty);
                }

                foreach (var legend in GetLegends())
                {
                    newline = true;
                    descriptionLines.Add(legend);
                }
                if (CacheDescriptionLines)
                {
                    DescriptionLines?.Clear();
                    DescriptionLines ??= new();
                    DescriptionLines.AddRange(descriptionLines);
                }
            }
            CacheDescription = CacheDescriptionLines;
            return descriptionLines;
        }

        public IEnumerable<string> GetSummaryLines()
        {
            if (SummaryLines.IsNullOrEmpty())
            {
                SummaryLines ??= new();

                foreach (string summaryBefore in GetSummaryBefores())
                    SummaryLines.Add(summaryBefore);

                foreach (var limbCountLine in GetLimbCountLines(GetLimbCounts()))
                    SummaryLines.Add(limbCountLine);

                foreach (string summaryAfter in GetSummaryAfters())
                    SummaryLines.Add(summaryAfter);

                CacheSummary = !SummaryLines.IsNullOrEmpty();
            }
            return SummaryLines;
        }

        public string GetDescription()
            => GetDescriptionLines()
                ?.Aggregate(
                    seed: SB.Clear(),
                    func: Utils.AggregateNewline)
            .ToString();

        public string GetSummary()
            => GetSummaryLines()
                ?.Aggregate(
                    seed: SB.Clear(),
                    func: Utils.AggregateNewline)
            .ToString();

        public static string GetDefaultBehaviorString(GameObjectBlueprint DefaultBehvaiour)
        {
            if (DefaultBehvaiour?.createSample() is not GameObject sampleDefaultBehaviour)
                return null;

            var mw = sampleDefaultBehaviour.GetPart<MeleeWeapon>();
            bool mwNotImprovisedAndNull = !(mw?.IsImprovisedWeapon() ?? true);
            string damage = mwNotImprovisedAndNull ? mw?.BaseDamage : null;

            int pVCap = mwNotImprovisedAndNull ? mw?.MaxStrengthBonus ?? 0 : 0;
            int pV = mwNotImprovisedAndNull ? 4 : 0;
            string pVSymbolColor = GetDisplayNamePenetrationColorEvent.GetFor(sampleDefaultBehaviour);

            var armor = sampleDefaultBehaviour.GetPart<Armor>();
            int aV = armor != null ? armor.AV : 0;
            int dV = armor != null ? armor.DV : 0;

            var sB = Event.NewStringBuilder();

            if (armor != null)
                sB.Append(' ').AppendArmor("y", aV, dV);

            if (pV > 0
                && pVCap > 0)
                sB.Append(' ').AppendPV(pVSymbolColor, "y", pV, pVCap);

            if (!damage.IsNullOrEmpty())
                sB.Append(' ').AppendDamage("y", damage);

            sampleDefaultBehaviour?.Obliterate();
            return Event.FinalizeString(sB);
        }
        public static string GetDefaultBehaviorString(string DefaultBehvaiour)
            => GetDefaultBehaviorString(GameObjectFactory.Factory.GetBlueprintIfExists(DefaultBehvaiour));

        public BodyPlanMenuOption GetMenuOption(bool IsSelected = false)
            => new()
            {
                ID = Entry.Anatomy.Name,
                IsSelected = IsSelected,
                Name = GetDisplayName(),
                Details = GetDescription(),
                Render = Render,
            };

        public BodyPlanMenuOption GetMenuOption(BodyPlan Selected = null)
            => GetMenuOption(SameAs(Selected))
            ;

        public static bool IsValid(BodyPlan BodyPlan)
            => BodyPlan?.Entry != null
            ;

        public bool IsValid()
            => IsValid(this)
            ;

        public static bool IsInvalid(BodyPlan BodyPlan)
            => !IsValid(BodyPlan)
            ;

        public bool IsInvalid()
            => !IsValid()
            ;

        public void Dispose()
        {
            ClearCachedValues();
        }
    }
}
