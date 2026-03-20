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
using XRL.Rules;
using UD_ChooseYourBodyPlan.Mod.TextHelpers;
using static UD_ChooseYourBodyPlan.Mod.BodyPlanEntry;

namespace UD_ChooseYourBodyPlan.Mod
{
    public class BodyPlan : IDisposable
    {
        public class BodyPlanEqualityComparer : IEqualityComparer<BodyPlan>, IDisposable
        {
            public readonly bool ByRef;
            public BodyPlanEqualityComparer(bool ByRef)
            {
                this.ByRef = ByRef;
            }

            public bool Equals(BodyPlan x, BodyPlan y)
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

        public class BodyPlanComparer : IComparer<BodyPlan>, IDisposable
        {
            public bool DefaultFirst;

            protected BodyPlanComparer()
            {
                DefaultFirst = false;
            }
            public BodyPlanComparer(bool DefaultFirst)
                : this()
            {
                this.DefaultFirst = DefaultFirst;
            }
            public int Compare(BodyPlan x, BodyPlan y)
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

                if (x.IsDefault)
                    return -1;
                if (y.IsDefault)
                    return 1;

                return string.Compare(x.DisplayNameStripped, y.DisplayNameStripped);
            }

            public void Dispose()
            {
            }
        }

        public class LimbTreeBranch
        {
            public static string DescriptionPlaceholder => "@@DESCRIPTION@@";
            public static bool IsTrueKin => Utils.IsTruekinEmbarking;

            public string Indent;
            public string CardinalDescription;
            public string Description;
            public string Type;
            public string FinalType;
            public string NaturalEquipmentStats;
            public string Extra;
            public bool HasNaturalEquipment => !NaturalEquipmentStats.IsNullOrEmpty();
            public bool HasNaturalEquipmentColorOverride => LimbElements?.Any(l => l.OverridesNaturalEquipmentColor) is true;
            public bool HasNaturalEquipmentStatsOverride => LimbElements?.Any(l => l.OverridesNaturalEquipmentStats) is true;
            public bool NoCyber;

            public List<LimbTextElements> LimbElements;

            public override string ToString()
            {
                string cardinalDescription = CardinalDescription.Replace(Description, DescriptionPlaceholder);
                string description = Description;

                if (HasNaturalEquipment
                    && !HasNaturalEquipmentColorOverride)
                    description = LimbTextElements.NaturalEquipment.ProcessColor(Description);

                if (!LimbElements.IsNullOrEmpty())
                    description = LimbElements[^1].ProcessColor(description);

                if (!FinalType.IsNullOrEmpty())
                    description = $"{description} ({FinalType})";

                if (HasNaturalEquipment
                    && !HasNaturalEquipmentStatsOverride)
                    description = $"{description}{NaturalEquipmentStats}";

                if (!LimbElements.IsNullOrEmpty())
                {
                    foreach (var limbElements in LimbElements ?? Enumerable.Empty<LimbTextElements>())
                        description = limbElements.ProcessPost(description);

                    bool any = false;
                    foreach (var limbElements in LimbElements ?? Enumerable.Empty<LimbTextElements>())
                    {
                        if (!any)
                            description += " ";
                        description = limbElements.ProcessSymbol(description);
                        any = true;
                    }
                }

                if (!Extra.IsNullOrEmpty())
                    description = $"{description} {Extra}";

                if (IsTrueKin
                    && NoCyber)
                    LimbTextElements.NoCyber.ProcessSymbol(description);

                cardinalDescription = cardinalDescription.Replace(DescriptionPlaceholder, description);

                string output = cardinalDescription;

                output = $"{Indent}{output}";

                return output;
            }
        }

        protected ref StringBuilder SB => ref BodyPlanFactory.SB;

        protected ref GameObject SampleCreature => ref BodyPlanFactory.SampleCreature;

        public static BodyPlanEqualityComparer ValueEqualityComparer = new(ByRef: false);

        public static BodyPlanComparer DefaultFirstNameComparer = new(DefaultFirst: true);

        public static BodyPlanComparer NameComparer = new(DefaultFirst: false);

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

        public TransformationData Transformation => Entry?.Transformation;

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


        private bool TKLatch;

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
            => ValueEqualityComparer.Equals(this, Other);

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
            Predicate<Symbol> Filter = null
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

        private static LimbTreeBranch InitializeLimbTreeBranch(BodyPart BodyPart, BodyPlan BodyPlan)
            => BodyPart.InitializeLimbTreeBranch(BodyPlan)
            ;

        private LimbTreeBranch InitializeLimbTreeBranch(BodyPart BodyPart)
            => InitializeLimbTreeBranch(BodyPart, this)
            ;

        public IEnumerable<LimbTreeBranch> GetBodyLimbTreeBranches(out bool HasDefaultEquipment)
        {
            if (LimbTreeBranches.IsNullOrEmpty()
                && ConfigureSampleCreature(ref SampleCreature).Body is Body sampleBody)
                LimbTreeBranches = new(sampleBody.GetLimbTree(
                    IndentProc: ColorBlack,
                    BodyPartProc: InitializeLimbTreeBranch,
                    Treat0DepthPartsAsRoot: true));

            HasDefaultEquipment = SampleCreature?.Body?.GetFirstPart(BodyPlan.HasDefaultEquipment) != null;
            return LimbTreeBranches;
        }

        public IEnumerable<string> GetBodyLimbTreeLines(out bool HasDefaultEquipment)
        {
            HasDefaultEquipment = false;

            if (Utils.IsTruekinEmbarking != TKLatch)
            {
                TKLatch = Utils.IsTruekinEmbarking;
                BodyLimbTreeLines?.Clear();
            }
            if (BodyLimbTreeLines.IsNullOrEmpty()
                && GetBodyLimbTreeBranches(out HasDefaultEquipment) is IEnumerable<LimbTreeBranch> limbTreeBranches)
            {
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

                if (GetDescriptionBefores() is IEnumerable<string> descriptionBefores
                    && !descriptionBefores.IsNullOrEmpty())
                {
                    foreach (string descriptionBefore in descriptionBefores)
                    {
                        descriptionLines.Add(descriptionBefore);
                    }
                    descriptionLines.Add(empty
                        //+ ColorBlack(nameof(GetDescriptionBefores))
                        );
                }

                if (Transformation?.Mutations?.IsNullOrEmpty() is false)
                {
                    if (Transformation.Mutations.Count == 1)
                    {
                        if (Transformation.Mutations.Values.First().ToString() is string mutationName)
                        {
                            descriptionLines.Add($"Gives the {mutationName} mutation.");
                        }
                    }
                    else
                    {
                        descriptionLines.Add($"Gives the following mutations:");
                        foreach (var mutation in Transformation.Mutations.Values)
                        {
                            if (mutation.ToString() is string mutationName)
                            {
                                descriptionLines.Add($" \xFA {mutationName}");
                            }
                        }
                    }
                    descriptionLines.Add(empty
                        //+ ColorBlack(nameof(Transformation))
                        );
                }

                if (GetBodyLimbTreeLines(out bool hasDefaultEquipment) is IEnumerable<string> bodyLimbTree)
                {
                    bool didLimbs = false;

                    descriptionLines.Add("Includes the following body part slots:");
                    if (!bodyLimbTree.IsNullOrEmpty())
                    {
                        foreach (var limbTreeBranch in bodyLimbTree)
                        {
                            didLimbs = true;
                            descriptionLines.Add(limbTreeBranch.ToString());
                        }
                        CacheDescriptionLines = didLimbs;
                    }
                    else
                    {
                        CacheDescriptionLines = false;
                        descriptionLines.Add("{{R|" + Const.RTRNG + "}} {{W|Something's gone wrong!}}");
                    }
                    descriptionLines.Add(empty
                        //+ ColorBlack(nameof(GetBodyLimbTreeLines))
                        );

                    if (hasDefaultEquipment)
                    {
                        descriptionLines.Add("{{w|Has natural equipment}}");
                        descriptionLines.Add(empty
                            //+ ColorBlack(nameof(hasDefaultEquipment))
                            );
                    }
                }

                if (GetDescriptionAfters() is IEnumerable<string> descriptionAfters
                    && !descriptionAfters.IsNullOrEmpty())
                {
                    foreach (string descriptionAfter in descriptionAfters)
                    {
                        descriptionLines.Add(descriptionAfter);
                    }
                    descriptionLines.Add(empty
                        //+ ColorBlack(nameof(GetDescriptionAfters))
                        );
                }

                if (GetLegends() is IEnumerable<string> legends
                    && !legends.IsNullOrEmpty())
                {
                    foreach (var legend in legends)
                    {
                        descriptionLines.Add(legend);
                    }
                    descriptionLines.Add(empty
                        //+ ColorBlack(nameof(GetLegends))
                        );
                }

                if (BodyPlanFactory.Factory?.ModInfoByAnatomy is StringMap<ModInfo> modInfoByAnatomy)
                {
                    string byLine = null;
                    string comesFrom = "is from";
                    string anatomySource = "an unknown source";
                    if (modInfoByAnatomy.ContainsKey(Anatomy))
                    {
                        if (modInfoByAnatomy.GetValue(Anatomy) is not ModInfo modInfo)
                        {
                            anatomySource = Const.BASE_GAME;
                            // comesFrom = "comes from";
                        }
                        else
                            anatomySource = modInfo.DisplayTitleStripped;
                    }

                    if (!anatomySource.IsNullOrEmpty())
                    {
                        byLine = ColorBlack($"Body plan {comesFrom} {anatomySource}.");
                    }

                    if (!byLine.IsNullOrEmpty())
                    {
                        descriptionLines.Add(byLine);
                        descriptionLines.Add(empty
                        //+ ColorBlack(nameof(byLine))
                        );
                    }
                }

                descriptionLines.Add(empty
                        //+ ColorBlack("end")
                        );

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

            var sB = Event.NewStringBuilder();

            if (sampleDefaultBehaviour.TryGetPart(out Armor armor))
            {
                int aV = armor.AV;
                int dV = armor.DV;

                sB.Append(' ').AppendArmor("y", aV, dV);
            }

            if (sampleDefaultBehaviour.TryGetPart(out MeleeWeapon mw))
            {
                if (!mw.IsImprovisedWeapon())
                {
                    string damage = mw.BaseDamage;

                    int pVCap = mw.MaxStrengthBonus;
                    int pV = RuleSettings.VISUAL_PENETRATION_BONUS;
                    string pVSymbolColor = GetDisplayNamePenetrationColorEvent.GetFor(sampleDefaultBehaviour);

                    sB.Append(' ').AppendPV(pVSymbolColor, "y", pV, pVCap);
                    sB.Append(' ').AppendDamage("y", damage);
                }
            }

            if (sampleDefaultBehaviour.HasPart<MissileWeapon>())
            {
                GameObject projectile = null;
                string projectileBlueprint = null;
                if (GetMissileWeaponProjectileEvent.GetFor(sampleDefaultBehaviour, ref projectile, ref projectileBlueprint)
                    && GetMissileWeaponPerformanceEvent.GetFor(null, sampleDefaultBehaviour, projectile) is GetMissileWeaponPerformanceEvent mWPE)
                {
                    if (mWPE.Attributes == null
                        || !mWPE.Attributes.Contains("NonPenetrating"))
                    {
                        string pVSymbolColor = "c";
                        if (mWPE.PenetrateCreatures)
                            pVSymbolColor = "W";
                        if (mWPE.PenetrateWalls)
                            pVSymbolColor = "m";

                        pVSymbolColor = GetDisplayNamePenetrationColorEvent.GetFor(sampleDefaultBehaviour, pVSymbolColor);

                        int pV = Math.Max(mWPE.Penetration + RuleSettings.VISUAL_PENETRATION_BONUS, 1);

                        sB.Append(' ');
                        if (!pVSymbolColor.IsNullOrEmpty())
                            sB.AppendColored(pVSymbolColor, Const.PV.ToString());
                        else
                            sB.Append(Const.PV);

                        if (mWPE.Attributes != null
                            && mWPE.Attributes.Contains("Vorpal"))
                            sB.AppendColored("y", "÷");
                        else
                            sB.AppendColored("y", pV.ToString());

                    }
                    if (mWPE.DamageRoll != null
                        || (!mWPE.BaseDamage.IsNullOrEmpty()
                            && mWPE.BaseDamage != "0"))
                    {
                        string damage = mWPE.DamageRoll?.ToString()
                            ?? mWPE.BaseDamage;

                        sB.Append(' ').AppendDamage("y", damage);
                    }
                }
            }

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
                Name = DisplayNameShowDefaultWithSymbols.ColorIf("W", IsSelected),
                Details = Description,
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

        public bool SetDefault(string Default)
            => IsDefault = Anatomy == Default
            ;

        public bool SetDefault(Anatomy Default)
            => SetDefault(Default?.Name)
            ;

        public bool SetDefault(BodyPlanEntry Default)
            => SetDefault(Default?.Anatomy)
            ;

        public bool SetDefault(BodyPlan Default)
            => SetDefault(Default?.Anatomy)
            ;

        public bool SetDefault(PrefixMenuOption Default)
            => SetDefault(Default?.Id)
            ;

        public void Dispose()
        {
            ClearCachedValues();
        }
    }
}
