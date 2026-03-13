using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ConsoleLib.Console;

using XRL;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;

using Event = XRL.World.Event;

using UD_BodyPlan_Selection.Mod;
using static UD_BodyPlan_Selection.Mod.AnatomyConfiguration;

namespace UD_BodyPlan_Selection.Mod
{
    public class AnatomyChoice
    {
        [HasModSensitiveStaticCache]
        public class ChoiceRenderable : Renderable
        {
            public static string RemoveTag => "*remove";
            public static string xTagPrefix => Const.MOD_PREFIX_SHORT;

            [ModSensitiveStaticCache]
            private static Dictionary<string, Dictionary<string, string>> _AnatomyTiles;
            public static Dictionary<string, Dictionary<string, string>> AnatomyTiles => _AnatomyTiles ??= GameObjectFactory.Factory
                ?.GetBlueprintIfExists(Const.TILES_BLUEPRINT)
                ?.xTags;

            public bool HFlip;

            public ChoiceRenderable(
                string Tile,
                string RenderString = null,
                string ColorString = null,
                string TileColor = null,
                char DetailColor = '\0',
                bool HFlip = false)
                : base(
                      Tile: Tile,
                      RenderString: RenderString,
                      ColorString: ColorString,
                      TileColor: TileColor,
                      DetailColor: DetailColor)
            {
                this.HFlip = HFlip;
            }
            public ChoiceRenderable(TransformationData Transformation, bool HFlip = false)
                : this(
                      Tile: Transformation?.Tile,
                      RenderString: Transformation?.RenderString ?? "@",
                      ColorString: $"{Transformation?.TileColor ?? "&Y"}^{Transformation?.DetailColor ?? "y"}",
                      TileColor: Transformation?.TileColor ?? "&Y",
                      DetailColor: Transformation?.DetailColor?[0] ?? 'y',
                      HFlip: HFlip)
            { }
            public ChoiceRenderable(GenotypeEntry GenotypeEntry)
                : this(
                      Tile: GenotypeEntry.Tile,
                      RenderString: "@",
                      ColorString: $"&Y^{GenotypeEntry.DetailColor}",
                      TileColor: "&Y",
                      DetailColor: GenotypeEntry?.DetailColor?[0] ?? 'y',
                      HFlip: true)
            { }
            public ChoiceRenderable(SubtypeEntry SubtypeEntry)
                : this(
                      Tile: SubtypeEntry.Tile,
                      RenderString: "@",
                      ColorString: $"&Y^{SubtypeEntry.DetailColor}",
                      TileColor: "&Y",
                      DetailColor: SubtypeEntry?.DetailColor?[0] ?? 'y',
                      HFlip: true)
            { }
            public ChoiceRenderable(Renderable Renderable, bool HFlip = false)
                : base(Renderable)
            {
                this.HFlip = HFlip;
            }
            public ChoiceRenderable(GameObjectBlueprint Blueprint, bool HFlip = false)
                : base(Blueprint)
            {
                this.HFlip = HFlip;
            }
            public ChoiceRenderable(Dictionary<string, string> xTag, bool HFlip = false)
                : base()
            {
                this.HFlip = HFlip;

                if (!xTag.IsNullOrEmpty())
                {
                    xTag.AssignStringFieldFromXTag(nameof(Tile), ref Tile);

                    xTag.AssignStringFieldFromXTag(nameof(RenderString), ref RenderString);

                    xTag.AssignStringFieldFromXTag(nameof(ColorString), ref ColorString);

                    xTag.AssignStringFieldFromXTag(nameof(TileColor), ref TileColor);

                    if (xTag.TryGetValue(nameof(DetailColor), out string detailColor)
                        && !detailColor.EqualsNoCase(RemoveTag))
                        DetailColor = detailColor?[0] ?? '\0';

                    if (xTag.TryGetValue(nameof(this.HFlip), out string hFlip))
                        bool.TryParse(hFlip, out this.HFlip);
                }
            }
            public ChoiceRenderable(string Anatomy, bool HFlip = false)
                : this(
                      xTag: AnatomyTiles?.ContainsKey(xTagPrefix + Anatomy) ?? false
                        ? AnatomyTiles[xTagPrefix + Anatomy]
                        : null,
                      HFlip: HFlip)
            { }

            public override bool getHFlip()
                => HFlip;
        }

        protected static StringBuilder SB = new();

        protected static GameObject SampleCreature = null;

        public static string MISSING_ANATOMY => nameof(MISSING_ANATOMY);

        public Anatomy Anatomy;

        private AnatomyCategory _Category;
        public AnatomyCategory Category => _Category ??= AnatomyCategory.TryGetFor(this, out var category) ? category : null;

        private List<AnatomyConfiguration> _AnatomyConfigurations;
        public List<AnatomyConfiguration> AnatomyConfigurations => _AnatomyConfigurations ??= new(Utils.GetAnatomyConfigurations(this));

        public string DisplayNameStripped => GetDescription()?.Strip();

        public ChoiceRenderable Renderable;

        public bool IsDefault;

        private string _LongDescription;
        public string LongDescription => _LongDescription ??= GetLongDescription(IncludeOpening: true);

        private string _LongDescriptionNoOpen;
        public string LongDescriptionNoOpen => _LongDescriptionNoOpen ??= GetLongDescription();

        private string _LongDescriptionSummary;
        public string LongDescriptionSummary => _LongDescriptionSummary ??= GetLongDescription(Summary: true, IncludeOpening: true);

        private string _LongDescriptionNoOpenSummary;
        public string LongDescriptionNoOpenSummary => _LongDescriptionNoOpenSummary ??= GetLongDescription(Summary: true);

        private string _LongDescriptionTK;
        public string LongDescriptionTK => _LongDescriptionTK ??= GetLongDescription(IncludeOpening: true, IsTrueKin: true);

        private string _LongDescriptionNoOpenTK;
        public string LongDescriptionNoOpenTK => _LongDescriptionNoOpenTK ??= GetLongDescription(IsTrueKin: true);

        private string _LongDescriptionTKSummary;
        public string LongDescriptionTKSummary => _LongDescriptionTKSummary ??= GetLongDescription(Summary: true, IncludeOpening: true, IsTrueKin: true);

        private string _LongDescriptionNoOpenTKSummary;
        public string LongDescriptionNoOpenTKSummary => _LongDescriptionNoOpenTKSummary ??= GetLongDescription(Summary: true, IsTrueKin: true);

        public AnatomyChoice()
        {
            Anatomy = null;
            _Category = null;
            _AnatomyConfigurations = null;
            Renderable = null;
            IsDefault = false;

            _LongDescription = null;
            _LongDescriptionSummary = null;
            _LongDescriptionTK = null;
            _LongDescriptionTKSummary = null;
        }
        public AnatomyChoice(Anatomy Anatomy, bool IsDefault, ChoiceRenderable Renderable)
            : this()
        {
            this.Anatomy = Anatomy;
            this.Renderable = Renderable;
            this.IsDefault = IsDefault;

            _ = Category;
        }
        public AnatomyChoice(Anatomy Anatomy, ChoiceRenderable Renderable)
            : this(Anatomy, false, Renderable)
        {
        }
        public AnatomyChoice(Anatomy Anatomy, bool IsDefault)
            : this(Anatomy, IsDefault, null)
        {
        }
        public AnatomyChoice(Anatomy Anatomy)
            : this(Anatomy, false, null)
        {
        }

        public override string ToString()
            => $"{GetDescription(ShowDefault: true, ShowSymbols: true)}{(Renderable?.Tile is string tile ? " " + tile : null)}";

        public void ClearLongDescriptionCaches()
        {
            _LongDescription = null;
            _LongDescriptionSummary = null;
            _LongDescriptionTK = null;
            _LongDescriptionTKSummary = null;
        }

        public string GetDescription(bool ShowDefault = false, bool ShowSymbols = false)
        {
            SB.Clear();

            string displayName = Anatomy?.Name?.SplitCamelCase();

            if (displayName != null
                && AnatomyConfigurations.GetDisplayName() is string configDisplayName)
                displayName = configDisplayName;

            SB.Append(displayName ?? MISSING_ANATOMY);

            if (SB.ToString() != MISSING_ANATOMY)
            {
                if (ShowDefault
                    && IsDefault)
                    SB.Append(" (default)");

                if (ShowSymbols

                    && !AnatomyConfigurations.IsNullOrEmpty()
                    && AnatomyConfigurations.HasSymbols())
                    SB.Append($" {AnatomyConfigurations.Symbols().Aggregate("", (a, n) => a + n)}");
            }

            return SB.ToString();
        }

        public string GetLongDescription(
            bool Summary = false,
            bool IncludeOpening = false,
            bool IsTrueKin = false
            )
        {
            SB.Clear();

            if (Anatomy == null)
                return SB.ToString();

            SampleCreature ??= GameObject.CreateSample("Humanoid");
            Anatomy.ApplyTo(SampleCreature.Body);
            if (!Summary)
            {
                GetLongDescriptionExtras(SB, Summary);

                if (IncludeOpening)
                    GetLongDescriptionOpening(SB, Summary);

                bool anyHasNatEquip = false;

                SampleCreature.Body.GetLimbTree(
                    SB: SB,
                    IndentProc: s => "{{K|" + s + "}}",
                    BodyPartProc: bp => GetBodyPartString(BodyPart: bp, IsTrueKin: IsTrueKin, ExcludeDefaultBehaviorName: true),
                    Treat0DepthPartsAsRoot: true);
                anyHasNatEquip = SampleCreature.Body.GetFirstPart(bp => !bp.VariantTypeModel().DefaultBehavior.IsNullOrEmpty()) != null;

                SB.AppendLine();
                if (IsTrueKin)
                    SB
                        .AppendLine()
                        .AppendNoCybernetics(false).Append(" - Incompatible with {{c|cybernetics}}")
                        ;

                if (anyHasNatEquip)
                    SB
                        .AppendLine()
                        //.AppendColored("w", "Indicates natural equipment")
                        .AppendColored("w", "Has natural equipment")
                        ;

                SB.AppendLines(2);
            }
            else
            {
                if (IncludeOpening)
                    GetLongDescriptionOpening(SB, Summary);

                var limbCounts = new Dictionary<BodyPartType, int>();
                if (SampleCreature.Body.GetParts().Select(p => p.VariantTypeModel()) is IEnumerable<BodyPartType> limbs)
                    foreach (BodyPartType limb in limbs)
                    {
                        if (limbCounts.ContainsKey(limb))
                            limbCounts[limb]++;
                        else
                            limbCounts[limb] = 1;
                    }

                foreach ((BodyPartType limb, int count) in limbCounts)
                {
                    if (!SB.IsNullOrEmpty())
                        SB.AppendLine();

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

                    SB.AppendColored("Y", count.Things(limbName, limbPluralName));

                    if (limb.FinalType.EqualsNoCase("body")
                        && SampleCreature.Body.CalculateMobilitySpeedPenalty() is int moveSpeedPenalty
                        && moveSpeedPenalty > 0)
                        SB
                            .Append(' ')
                            .AppendColored("r", $"{-moveSpeedPenalty} MS");

                    if (GameObjectFactory.Factory.GetBlueprintIfExists(limb.DefaultBehavior) is GameObjectBlueprint defaultBehvaiour)
                        GetDefaultBehaviorString(SB, defaultBehvaiour, true);

                    if (IsTrueKin
                        && (limb?.Category ?? BodyPartCategory.ANIMAL) != BodyPartCategory.ANIMAL)
                        SB.AppendNoCybernetics();
                }
                GetLongDescriptionExtras(SB, Summary);
            }

            return SB.ToString();
        }
        public void GetLongDescriptionOpening(StringBuilder SB, bool Summary = false)
        {
            if (!Summary)
                SB.Append("Includes the following body part slots:");
            else
                SB.Append("Included parts:");
        }
        public void GetLongDescriptionExtras(StringBuilder SB, bool Summary = false)
        {
            if (Summary)
                SB.AppendLine();

            if (Anatomy.HasRecipe())
            {
                if (!Summary)
                    SB.AppendColored("m", "There is a cooking recipe to get this body plan.")
                        .AppendLine()
                        ;
                else
                    SB.AppendColored("m", "Avaialable via cooking");
                SB.AppendLine();
            }

            if (((AnatomyConfigurations?.IsMechanical() ?? false)
                    || Anatomy?.Category == BodyPartCategory.MECHANICAL)
                && Options.EnableRoboticBodyPlansMakingYouRobotic)
            {
                if (!Summary)
                    SB.AppendColored("c", "You will be made mechanical with this body plan.")
                        .AppendLine()
                        ;
                else
                    SB.AppendColored("c", "You are mechanical");
                SB.AppendLine();
            }

            if (!Summary
                && (AnatomyConfigurations?.HasDescriptionAddition() ?? false))
                foreach (string exceptionMessage in AnatomyConfigurations.DescriptionAdditions())
                    SB.Append(exceptionMessage)
                        .AppendLine()
                        .AppendLine()
                        ;

            if (Summary
                && (AnatomyConfigurations?.HasSummaryAddition() ?? false))
                foreach (string exceptionSummary in AnatomyConfigurations.SummaryAdditions())
                    SB.Append(exceptionSummary)
                    .AppendLine()
                    ;
        }

        public static StringBuilder GetBodyPartString(
            StringBuilder SB,
            BodyPart BodyPart,
            out bool HasNaturalEquipment,
            bool IsTrueKin = false,
            bool ExcludeDefaultBehaviorName = false
            )
        {
            string defaultBehaviour = BodyPart.VariantTypeModel().DefaultBehavior;
            HasNaturalEquipment = !defaultBehaviour.IsNullOrEmpty();

            string cardinalDescription = BodyPart.GetCardinalDescription();
            string description = BodyPart.VariantTypeModel().Description;

            if (!SB.IsNullOrEmpty())
                SB.AppendLine();

            if (HasNaturalEquipment
                && ExcludeDefaultBehaviorName)
                SB.Append(cardinalDescription.Replace(description, "{{w|" + description + "}}"))
                    //.AppendColored("w", cardinalDescription)
                    ;
            else
                SB.Append(cardinalDescription);

            if (BodyPart.IsVariantType()
                && !cardinalDescription.ContainsNoCase(BodyPart.Type))
                SB
                    .Append(" (")
                    .Append(BodyPart.TypeModel().FinalType)
                    .Append(")")
                    ;

            if (BodyPart.VariantTypeModel().FinalType.EqualsNoCase("body")
                && BodyPart.ParentBody.CalculateMobilitySpeedPenalty() is int moveSpeedPenalty
                && moveSpeedPenalty > 0)
                SB
                    .Append(' ')
                    .AppendColored("r", $"{-moveSpeedPenalty} Move Speed Penalty");

            if (GameObjectFactory.Factory.GetBlueprintIfExists(BodyPart.VariantTypeModel().DefaultBehavior) is GameObjectBlueprint defaultBehvaiour)
            {
                HasNaturalEquipment = true;
                GetDefaultBehaviorString(SB, defaultBehvaiour, ExcludeDefaultBehaviorName);
            }

            if (IsTrueKin
                && !BodyPart.CanReceiveCyberneticImplant())
                SB.AppendNoCybernetics();

            return SB;
        }
        public static string GetBodyPartString(
            BodyPart BodyPart,
            bool IsTrueKin = false,
            bool ExcludeDefaultBehaviorName = false
            )
            => Event.FinalizeString(
                SB: GetBodyPartString(
                    SB: Event.NewStringBuilder(),
                    BodyPart: BodyPart,
                    HasNaturalEquipment: out _,
                    IsTrueKin: IsTrueKin,
                    ExcludeDefaultBehaviorName: ExcludeDefaultBehaviorName));

        public static StringBuilder GetDefaultBehaviorString(
            StringBuilder SB,
            GameObjectBlueprint defaultBehvaiour,
            bool ExcludeName = false
            )
        {
            var sampleDefaultBehaviour = defaultBehvaiour.createSample();

            if (!ExcludeName)
                SB
                    //.Append(" - ")
                    .Append(' ')
                    .AppendColored("w", sampleDefaultBehaviour.ShortDisplayNameStripped);

            var mw = sampleDefaultBehaviour.GetPart<MeleeWeapon>();
            bool mwNotImprovisedAndNull = !(mw?.IsImprovisedWeapon() ?? true);
            string damage = mwNotImprovisedAndNull ? mw?.BaseDamage : null;

            int pVCap = mwNotImprovisedAndNull ? mw?.MaxStrengthBonus ?? 0 : 0;
            int pV = mwNotImprovisedAndNull ? 4 : 0;
            string pVSymbolColor = GetDisplayNamePenetrationColorEvent.GetFor(sampleDefaultBehaviour);

            var armor = sampleDefaultBehaviour.GetPart<Armor>();
            int aV = armor != null ? armor.AV : 0;
            int dV = armor != null ? armor.DV : 0;

            if (armor != null)
                SB.Append(' ').AppendArmor("y", aV, dV);

            if (pV > 0
                && pVCap > 0)
                SB.Append(' ').AppendPV(pVSymbolColor, "y", pV, pVCap);

            if (!damage.IsNullOrEmpty())
                SB.Append(' ').AppendDamage("y", damage);

            sampleDefaultBehaviour?.Obliterate();
            return SB;
        }

        public ChoiceRenderable GetRenderable()
        {
            if (Renderable == null
                && Anatomy != null)
            {
                string safeAnatomyName = Anatomy.Name.Replace("-", "_").Replace(" ", "_");
                string tileKey = ChoiceRenderable.xTagPrefix + safeAnatomyName;
                if (AnatomyConfigurations?.FirstTransformationOrDefault() is TransformationData xForm
                    && !xForm.Tile.IsNullOrEmpty()
                    && !xForm.DetailColor.IsNullOrEmpty())
                    Renderable = new(xForm, true);
                else
                if (ChoiceRenderable.AnatomyTiles?.ContainsKey(tileKey) ?? false)
                    Renderable = new(safeAnatomyName, false);
                else
                    Renderable = new(GetExampleBlueprint()?.GetRenderable(), false);
            }

            return Renderable;
        }
        public void OverrideRenderable(ChoiceRenderable Renderable)
        {
            if (Renderable != null)
                this.Renderable = Renderable;
        }

        public bool HasMatchingAnatomy(GameObjectBlueprint Blueprint)
            => Anatomy != null
            && Blueprint.GetAnatomyName() is string anatomy
            && anatomy == Anatomy.Name
            ;
        public bool ObjectAnimatesWithAnatomy(GameObjectBlueprint Blueprint)
            => Anatomy != null
            && Blueprint.TryGetTag("BodyType", out string bodyType)
            && Anatomy.Name == bodyType
            ;
        public bool InheritsFromAnatomy(GameObjectBlueprint Blueprint)
            => Anatomy != null
            && Blueprint.InheritsFrom(Anatomy.Name)
            ;

        public IEnumerable<GameObjectBlueprint> GetExampleBlueprints()
        {
            var blueprints = Utils.GenerallyEligbleForDisplayBlueprints;

            if (blueprints
                ?.Where(HasMatchingAnatomy) is IEnumerable<GameObjectBlueprint> objectsWithAnatomy
                && !objectsWithAnatomy.IsNullOrEmpty())
                return objectsWithAnatomy;

            if (blueprints
                ?.Where(ObjectAnimatesWithAnatomy) is IEnumerable<GameObjectBlueprint> objectsAnimatingWithAnatomy
                && !objectsAnimatingWithAnatomy.IsNullOrEmpty())
                return objectsAnimatingWithAnatomy;

            if (blueprints
                ?.Where(InheritsFromAnatomy) is IEnumerable<GameObjectBlueprint> objectsInheritingAnatomy
                && !objectsInheritingAnatomy.IsNullOrEmpty())
                return objectsInheritingAnatomy;

            return new GameObjectBlueprint[0];
        }

        public GameObjectBlueprint GetExampleBlueprint()
            => GetExampleBlueprints()?.GetRandomElementCosmetic()
            ?? GameObjectFactory.Factory.GetBlueprintIfExists("Mimic")
            ;
    }
}
