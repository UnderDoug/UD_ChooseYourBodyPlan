using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ConsoleLib.Console;

using XRL.UI.Framework;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;

using UD_BodyPlan_Selection.Mod;
using static UD_BodyPlan_Selection.Mod.AnatomyExclusion;
using Event = XRL.World.Event;
using XRL.Collections;

namespace XRL.CharacterBuilds.Qud
{
    [HasOptionFlagUpdate]
    public partial class Qud_UD_BodyPlanModule : QudEmbarkBuilderModule<Qud_UD_BodyPlanModuleData>
    {
        public class AnatomyChoice
        {
            [HasModSensitiveStaticCache]
            public class ChoiceRenderable : Renderable
            {
                public static string RemoveTag => "*remove";
                public static string xTagPrefix => "UD_BDS_";

                [ModSensitiveStaticCache]
                private static Dictionary<string, Dictionary<string, string>> _AnatomyTiles;
                public static Dictionary<string, Dictionary<string, string>> AnatomyTiles => _AnatomyTiles ??= GameObjectFactory.Factory
                    ?.GetBlueprintIfExists("UD_BodyPlan_Slection_AnatomyTiles")
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
                        if (xTag.TryGetValue(nameof(Tile), out Tile)
                            && Tile.EqualsNoCase(RemoveTag))
                            Tile = null;

                        if (xTag.TryGetValue(nameof(RenderString), out RenderString)
                            && RenderString.EqualsNoCase(RemoveTag))
                            RenderString = null;

                        if (xTag.TryGetValue(nameof(ColorString), out ColorString)
                            && ColorString.EqualsNoCase(RemoveTag))
                            ColorString = null;

                        if (xTag.TryGetValue(nameof(TileColor), out TileColor)
                            && TileColor.EqualsNoCase(RemoveTag))
                            TileColor = null;

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

            protected static GameObject SampleCreature = null;

            public static string MISSING_ANATOMY => nameof(MISSING_ANATOMY);

            public Anatomy Anatomy;

            private List<AnatomyExclusion> _AnatomyExclusion;
            public List<AnatomyExclusion> AnatomyExclusions => _AnatomyExclusion ??= new(Utils.GetAnatomyExclusions(this));

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
                _AnatomyExclusion = null;
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
                => GetDescription(true) + (Renderable?.Tile is string tile ? " " + tile : null);

            public void ClearLongDescriptionCaches()
            {
                _LongDescription = null;
                _LongDescriptionSummary = null;
                _LongDescriptionTK = null;
                _LongDescriptionTKSummary = null;
            }

            public string GetDescription(bool ShowDefault = false)
            {
                SB.Clear();

                SB.Append(Anatomy?.Name.SplitCamelCase() ?? MISSING_ANATOMY);
                if (IsDefault && ShowDefault)
                    SB.Append(" (default)");

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
                    /*
                    foreach (BodyPart bodyPart in SampleCreature.Body.GetParts())
                    {
                        GetBodyPartString(SB, bodyPart, out bool hasNatEquip, IsTrueKin);
                        anyHasNatEquip = hasNatEquip || anyHasNatEquip;
                    }
                    */
                    SampleCreature.Body.GetLimbTree(
                        SB: SB,
                        IndentProc: s => "{{K|" + s + "}}",
                        BodyPartProc: bp => GetBodyPartString(bp, IsTrueKin, true),
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
                            && limb.Category != BodyPartCategory.ANIMAL)
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

                if (((AnatomyExclusions?.IsMechanical() ?? false)
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
                    && (AnatomyExclusions?.HasExceptionMessage() ?? false))
                    foreach (string exceptionMessage in AnatomyExclusions.ExceptionMessages())
                    SB.Append(exceptionMessage)
                        .AppendLine()
                        .AppendLine()
                        ;

                if (Summary
                    && (AnatomyExclusions?.HasExceptionSummary() ?? false))
                    foreach (string exceptionSummary in AnatomyExclusions.ExceptionSummaries())
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

                string description = BodyPart.GetCardinalDescription();

                if (!SB.IsNullOrEmpty())
                    SB.AppendLine();

                if (HasNaturalEquipment
                    && ExcludeDefaultBehaviorName)
                    SB.AppendColored("w", BodyPart.GetCardinalDescription());
                else
                    SB.Append(BodyPart.GetCardinalDescription());

                if (BodyPart.IsVariantType()
                    && !description.ContainsNoCase(BodyPart.Type))
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
                    if (AnatomyExclusions?.FirstTransformationOrDefault() is TransformationData xForm
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

        private static bool WantClearChoices = false;

        private static IEnumerable<AnatomyChoice> _BaseAnatomyChoices;
        public static IEnumerable<AnatomyChoice> BaseAnatomyChoices => _BaseAnatomyChoices ??= Anatomies.AnatomyList
            ?.Where(IsEligibleAnatomy)
            ?.Select(AnatomyToChoice)
            ?.OrderBy(a => a.Anatomy.Name)
            ;

        public bool HasSelection => data?.HasSelection ?? false;

        private AnatomyChoice _PlayerAnatomyChoice; 
        public AnatomyChoice PlayerAnatomyChoice
        {
            get
            {
                if (_PlayerAnatomyChoice == null
                    && GetDefaultPlayerBodyPlan() is string playerAnatomyName
                    && !_AnatomyChoices.IsNullOrEmpty())
                    _PlayerAnatomyChoice = AnatomyChoices.FirstOrDefault(a => a?.Anatomy?.Name == playerAnatomyName);

                return _PlayerAnatomyChoice;
            }
            set => _PlayerAnatomyChoice = null;
        }

        public override AbstractEmbarkBuilderModuleData DefaultData => GetDefaultData();

        private List<AnatomyChoice> _AnatomyChoices;
        public List<AnatomyChoice> AnatomyChoices
        {
            get
            {
                if (_AnatomyChoices.IsNullOrEmpty()
                    || WantClearChoices)
                {
                    WantClearChoices = false;
                    _AnatomyChoices = new();
                    _AnatomyChoices.AddRange(BaseAnatomyChoices.Where(AnatomyChoiceIsValid));
                    _AnatomyChoices.RemoveAll(c => c == null || c.Anatomy == null);
                    SetDefaultChoice();
                    SelectDefaultChoice();
                    Utils.AnatomyChoices = new(AnatomyChoices);
                }
                return _AnatomyChoices;
            }
        }

        protected static StringBuilder SB = new();

        public QudGenotypeModuleData GenotypeModuleData => builder?.GetModule<QudGenotypeModule>()?.data;
        public QudSubtypeModuleData SubtypeModuleData => builder?.GetModule<QudSubtypeModule>()?.data;

        public Qud_UD_BodyPlanModule()
        {
            _AnatomyChoices = null;
            _PlayerAnatomyChoice = null;
        }

        public override bool shouldBeEditable()
            => builder.IsEditableGameMode();

        public override bool shouldBeEnabled()
            => GenotypeModuleData?.Entry is GenotypeEntry genotypeEntry
            && (!genotypeEntry.IsTrueKin
                || Options.EnableBodyPlansForTK)
            && SubtypeModuleData?.Subtype != null;

        private static bool IsQudQudMutationsModuleWindowDescriptor(EmbarkBuilderModuleWindowDescriptor Descriptor)
            => Descriptor.module is QudMutationsModule
            ;
        public override void assembleWindowDescriptors(List<EmbarkBuilderModuleWindowDescriptor> windows)
        {
            int index = windows.FindIndex(IsQudQudMutationsModuleWindowDescriptor);
            if (index < 0)
                base.assembleWindowDescriptors(windows);
            else
                windows.InsertRange(index + 1, this.windows.Values);
        }

        public override SummaryBlockData GetSummaryBlock()
        {
            var choice = SelectedChoice();
            var sB = Event.NewStringBuilder()
                .Append("{{Y|")
                .Append(choice.GetDescription());

            if (!choice.AnatomyExclusions.IsNullOrEmpty())
            {
                using var symbols = ScopeDisposedList<string>.GetFromPool();

                if (choice.AnatomyExclusions.IsTransformation())
                    symbols.Add("{{m|\u00f1}}"); // ±

                if (choice.AnatomyExclusions.IsMechanical()
                    && Options.EnableRoboticBodyPlansMakingYouRobotic)
                    symbols.Add("{{c|\u000f}}"); // ☼

                if (choice.AnatomyExclusions.IsDifficult())
                    symbols.Add("{{r|\u0013}}"); // ‼

                if (!symbols.IsNullOrEmpty())
                    sB.Append($" {symbols.Aggregate("", (a, n) => a + n)}");
            }
            sB.Append(":}}")
                .AppendLine()
                .Append(
                    GenotypeModuleData?.Entry?.IsTrueKin ?? false 
                    ? choice.LongDescriptionNoOpenTKSummary 
                    : choice.LongDescriptionNoOpenSummary
                    );
            return new()
            {
                Id = GetType().FullName,
                Title = "Body Plan",
                Description = Event.FinalizeString(sB),
                SortOrder = 50
            };
        }

        public override object handleBootEvent(
            string id,
            XRLGame game,
            EmbarkInfo info,
            object element = null
            )
        {
            if (element is GameObject player
                && player.Body is Body playerBody
                && data.Selection.Anatomy is string anatomy)
            {
                if (data == null
                    || data.Selection == null)
                {
                    MetricsManager.LogWarning("Body Plan module was active but data or selections was null or empty.");
                    return element;
                }

                if (id == QudGameBootModule.BOOTEVENT_BOOTPLAYEROBJECT)
                    playerBody.Rebuild(data.Selection.Anatomy);

                if (id == QudGameBootModule.BOOTEVENT_AFTERBOOTPLAYEROBJECT)
                {
                    string defaultSpecies = SubtypeModuleData?.Entry?.Species
                        .Coalesce(GenotypeModuleData?.Entry.Species);
                    string defaultTile = SubtypeModuleData?.Entry?.Tile
                        .Coalesce(GenotypeModuleData?.Entry.Tile);

                    if (data.Selection.Transformation is TransformationData xForm)
                    {
                        if (!xForm.Property.IsNullOrEmpty())
                            player.SetStringProperty(xForm.Property, "true");

                        if (!xForm.Mutations.IsNullOrEmpty())
                            foreach (string mutation in xForm.Mutations)
                                if (MutationFactory.TryGetMutationEntry(mutation, out var mutationEntry))
                                    player.RequirePart<Mutations>().AddMutation(mutationEntry);

                        if (player?.Render?.Tile == defaultTile
                            && !xForm.Tile.IsNullOrEmpty())
                        {
                            if (!xForm.RenderString.IsNullOrEmpty())
                                player.Render.RenderString = xForm.RenderString;
                            player.Render.Tile = xForm.Tile;
                        }

                        if (!xForm.DetailColor.IsNullOrEmpty())
                            player.Render.DetailColor = xForm.DetailColor;

                        if (player?.GetSpecies() == defaultSpecies
                            && !xForm.Species.IsNullOrEmpty())
                            player.SetStringProperty("Species", xForm.Species);
                    }

                    if (Anatomies.GetAnatomy(anatomy).Category == BodyPartCategory.MECHANICAL
                        && Options.EnableRoboticBodyPlansMakingYouRobotic)
                        World.ObjectBuilders.Roboticized.Roboticize(player);
                }
            }
            return base.handleBootEvent(id, game, info, element);
        }
        public override void setData(AbstractEmbarkBuilderModuleData values)
        {
            OrganizeAnatomyChoices(true);
            base.setData(values);
        }

        public override string DataErrors()
        {
            if (AnatomyChoices.IsNullOrEmpty())
                throw new InvalidOperationException("No anatomies to choose from");

            if (data?.Selection?.Anatomy is string selectedAnatomy
                && Anatomies.GetAnatomy(selectedAnatomy) is null)
                return "Selection is not a valid anatomy";

            return base.DataErrors();
        }

        public override string DataWarnings()
        {
            if (data?.Selection == null)
                return "You have not selected a body plan.\n" +
                    $"The default for your selected genotype/subtype is {GetDefaultPlayerBodyPlan().SplitCamelCase()}";
            return base.DataWarnings();
        }

        public override void handleModuleDataChange(
            AbstractEmbarkBuilderModule module,
            AbstractEmbarkBuilderModuleData oldValues,
            AbstractEmbarkBuilderModuleData newValues
            )
        {
            base.handleModuleDataChange(module, oldValues, newValues);

            if (module is not QudGenotypeModule
                && module is not QudSubtypeModule)
                return;

            if (module == this)
                return;

            OrganizeAnatomyChoices();
        }

        [OptionFlagUpdate]
        public static void OnOptionUpdate()
        {
            WantClearChoices = true;
        }

        public static bool AnatomyChoiceIsValid(AnatomyChoice Choice)
            => Choice.GetDescription() != AnatomyChoice.MISSING_ANATOMY
            && Choice.Anatomy != null
            && (Choice.AnatomyExclusions == null
                || !Choice.AnatomyExclusions.IsExcluded())
            ;
        public void OrganizeAnatomyChoices(bool SelectDefaultChoice = false, bool OverrideSelection = false)
        {
            if (AnatomyChoices.IsNullOrEmpty())
            {
                MetricsManager.LogCallingModError(nameof(AnatomyChoices) + " empty when it probably shouldn't be.");
                return;
            }

            PlayerAnatomyChoice = null;
            if (PlayerAnatomyChoice != null
                && !IsPlayerChoice(AnatomyChoices[0]))
            {
                AnatomyChoices.OrderBy(a => a?.Anatomy?.Name);
                AnatomyChoices.Remove(PlayerAnatomyChoice);
                AnatomyChoices.Insert(0, PlayerAnatomyChoice);
                SetDefaultChoice();
                if (SelectDefaultChoice)
                    this.SelectDefaultChoice(OverrideSelection);
            }
        }

        public static bool IsEligibleAnatomy(Anatomy Anatomy)
            => Anatomy != null
            && (Utils.GetAnatomyExclusions(Anatomy) is not AnatomyExclusion anatomyExclusion
                || anatomyExclusion.IsOptional)
            ;
        public static AnatomyChoice AnatomyToChoice(Anatomy Anatomy)
            => new(Anatomy)
            ;
        public void PickAnatomy(int n)
        {
            if (data == null)
                setData(DefaultData);

            if (AnatomyChoices.IsNullOrEmpty())
                MetricsManager.LogCallingModError(nameof(AnatomyChoices) + " empty when it probably shouldn't be.");
            else
                data.Selection = new(AnatomyChoices[n]);

            setData(data);
        }

        private string GetPlayerBlueprint()
            => builder?.info?.fireBootEvent(
                id: QudGameBootModule.BOOTEVENT_BOOTPLAYEROBJECTBLUEPRINT,
                game: The.Game,
                element: GenotypeModuleData?.Entry?.BodyObject
                    .Coalesce(SubtypeModuleData?.Entry?.BodyObject)
                    .Coalesce("Humanoid"));

        public string GetDefaultPlayerBodyPlan()
            => GameObjectFactory.Factory
                ?.GetBlueprintIfExists(GetPlayerBlueprint())
                ?.GetPartParameter<string>(nameof(Body), nameof(Body.Anatomy));

        public Qud_UD_BodyPlanModuleData GetDefaultData()
            => new(PlayerAnatomyChoice);

        public void SetDefaultChoice()
        {
            if (AnatomyChoices.FirstOrDefault(c => c.IsDefault) is AnatomyChoice defaultChoice
                && defaultChoice != PlayerAnatomyChoice)
            {
                defaultChoice.IsDefault = false;
                PlayerAnatomyChoice = null;
            }
            if (PlayerAnatomyChoice is AnatomyChoice playerChoice
                && !playerChoice.IsDefault)
            {
                playerChoice.IsDefault = true;
                if (GenotypeModuleData?.Entry is GenotypeEntry genotypeEntry
                    && SubtypeModuleData?.Entry is SubtypeEntry subtypeEntry
                    && subtypeEntry.Tile.Coalesce(genotypeEntry.Tile) is string typeTile
                    && subtypeEntry.DetailColor.Coalesce(genotypeEntry.DetailColor) is string typeDetailColor)
                    playerChoice.OverrideRenderable(
                        Renderable: new(
                            Tile: typeTile,
                            RenderString: "@",
                            ColorString: $"&Y^{typeDetailColor}",
                            TileColor: "&Y",
                            DetailColor: typeDetailColor[0],
                            HFlip: true));

                if (data != null)
                    setData(data);
            }
        }
        public bool IsPlayerChoice(AnatomyChoice Choice)
            => Choice == PlayerAnatomyChoice
            || Choice.Anatomy == PlayerAnatomyChoice.Anatomy
            ;
        public void SelectDefaultChoice(bool Override = false)
        {
            if (data != null)
                if (Override
                    || data.Selection == null
                    || data.Selection.Anatomy.IsNullOrEmpty())
                {
                    int playerIndex = AnatomyChoices.FindIndex(IsPlayerChoice);
                    if (playerIndex < 0)
                        playerIndex = 0;

                    PickAnatomy(playerIndex);
                }
        }

        public bool IsSelected(AnatomyChoice Choice)
            => Choice != null
            && data != null
            && ((Choice.Anatomy == null
                    && data.Selection == null)
                || Choice.Anatomy?.Name == data.Selection?.Anatomy);

        public AnatomyChoice SelectedChoice()
            => AnatomyChoices.Find(IsSelected);
    }
}
