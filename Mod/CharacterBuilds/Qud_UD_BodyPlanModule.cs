using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ConsoleLib.Console;

using UD_BodyPlan_Selection.Mod;

using XRL.Collections;
using XRL.UI.Framework;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;

namespace XRL.CharacterBuilds.Qud
{
    public class Qud_UD_BodyPlanModule : QudEmbarkBuilderModule<Qud_UD_BodyPlanModuleData>
    {
        public class AnatomyChoice
        {
            public static string MISSING_ANATOMY => nameof(MISSING_ANATOMY);

            public Anatomy Anatomy;

            private Renderable Renderable;

            public bool IsDefault;

            public string GetDescription()
            {
                sb.Clear();

                sb.Append(Anatomy?.Name.SplitCamelCase() ?? MISSING_ANATOMY);
                if (IsDefault)
                    sb.Append(" (default)");

                return sb.ToString();
            }

            public string GetLongDescription(bool IncludeOpening = false)
            {
                sb.Clear();

                var limbs = new Dictionary<string, int>();
                if (Anatomy?.Parts?.Select(ap => ap?.Type?.Name ?? "MISSING_PART") is IEnumerable<string> partNames)
                    foreach (string limb in partNames)
                    {
                        if (limbs.ContainsKey(limb))
                            limbs[limb]++;
                        else
                            limbs[limb] = 1;
                    }

                return sb
                    .Append(limbs.Aggregate(
                        seed: IncludeOpening ? "Contains the following body parts:" : "",
                        func: (a, n) => a + (!a.IsNullOrEmpty() ? "\n" : null) + "{{W|" + n.Value.Things("}}x {{W|" + n.Key) + "}}"))
                    .ToString();
            }

            public Renderable GetRenderable()
            {
                if (Renderable == null
                    && Anatomy != null)
                    Renderable = GetExampleBlueprint()?.GetRenderable();

                return Renderable;
            }

            public bool IsEligibleForDisplay(GameObjectBlueprint Blueprint)
                => Blueprint != null
                && (!Blueprint.IsBaseBlueprint()
                    || Blueprint.HasPartParameter(nameof(Render), nameof(Render.Tile)))
                && !Blueprint.HasTag("Golem")
                ;
            public bool HasMatchingAnatomy(GameObjectBlueprint Blueprint)
                => Blueprint != null
                && Blueprint.TryGetPartParameter(nameof(Body), nameof(Body.Anatomy), out string anatomy) && anatomy == Anatomy.Name
                ;
            public bool ObjectAnimatesWithAnatomy(GameObjectBlueprint Blueprint)
                => Blueprint != null
                && Blueprint.TryGetTag("BodyType", out string bodyType) 
                && bodyType == Anatomy.Name
                ;
            public bool InheritsFromAnatomy(GameObjectBlueprint Blueprint)
                => Blueprint != null
                && Blueprint.InheritsFrom(Anatomy.Name)
                ;

            public IEnumerable<GameObjectBlueprint> GetExampleBlueprints()
            {
                if (GameObjectFactory.Factory
                    ?.BlueprintList
                    ?.Where(IsEligibleForDisplay) 
                    ?.Where(HasMatchingAnatomy) is IEnumerable<GameObjectBlueprint> objectsWithAnatomy
                    && !objectsWithAnatomy.IsNullOrEmpty())
                    return objectsWithAnatomy;

                if (GameObjectFactory.Factory
                    ?.BlueprintList
                    ?.Where(IsEligibleForDisplay)
                    ?.Where(ObjectAnimatesWithAnatomy) is IEnumerable<GameObjectBlueprint> objectsAnimatingWithAnatomy
                    && !objectsAnimatingWithAnatomy.IsNullOrEmpty())
                    return objectsAnimatingWithAnatomy;

                if (GameObjectFactory.Factory
                    ?.BlueprintList
                    ?.Where(IsEligibleForDisplay)
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

        public static bool IsEligibleAnatomy(Anatomy Anatomy)
            => Anatomy != null
            && Anatomy.BodyCategory != BodyPartCategory.LIGHT
            && Anatomy.Category != BodyPartCategory.MECHANICAL
            && Anatomy.Name != "HumanoidWithHandsFace"
            && true.LogReturning(Anatomy.Name)
            ;
        public static AnatomyChoice AnatomyToChoice(Anatomy Anatomy)
            => new () { Anatomy = Anatomy }
            ;
        public static IEnumerable<AnatomyChoice> BaseAnatomyChoices => Anatomies.AnatomyList
            ?.Where(IsEligibleAnatomy)
            ?.Select(AnatomyToChoice)
            ?.OrderBy(a => a.Anatomy.Name)
            ;

        public override AbstractEmbarkBuilderModuleData DefaultData => GetDefaultData();

        private List<AnatomyChoice> _AnatomyChoices;
        public List<AnatomyChoice> AnatomyChoices
        {
            get
            {
                if (_AnatomyChoices.IsNullOrEmpty())
                {
                    _AnatomyChoices ??= new();
                    _AnatomyChoices.AddRange(BaseAnatomyChoices.Where(a => a.GetDescription() != AnatomyChoice.MISSING_ANATOMY));
                    SetDefautChoice();
                }
                return _AnatomyChoices;
            }
        }

        protected static StringBuilder sb = new();

        public Qud_UD_BodyPlanModule()
        {
            _AnatomyChoices = null;
        }

        public override void InitFromSeed(string seed)
        { }

        public override bool shouldBeEditable()
            => builder.IsEditableGameMode();

        public override bool shouldBeEnabled()
            => builder.GetModule<QudGenotypeModule>()?.data?.Entry is GenotypeEntry genoType
            && !genoType.IsTrueKin
            && builder?.GetModule<QudSubtypeModule>()?.data?.Subtype != null;

        public override SummaryBlockData GetSummaryBlock()
        {
            AnatomyChoice anatomyChoice = SelectedChoice();
            anatomyChoice.GetRenderable();
            return new SummaryBlockData
            {
                Id = GetType().FullName,
                Title = "Body Plan",
                Description = anatomyChoice.GetDescription() + "\n" + anatomyChoice.GetLongDescription(),
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
            if (id == QudGameBootModule.BOOTEVENT_BOOTPLAYEROBJECT)
            {
                if (data == null
                    || data.Selections.IsNullOrEmpty())
                {
                    MetricsManager.LogWarning("Body Plan module was active but data or selections was null or empty.");
                    return element;
                }
                if (element is GameObject player
                    && player.Body is Body playerBody)
                    playerBody.Rebuild(data.Selections[0].Anatomy);
            }
            return base.handleBootEvent(id, game, info, element);
        }

        public override string DataErrors()
        {
            if (data != null
                && !AnatomyChoices.Any(IsSelected))
                return "Invalid choice selected";

            return base.DataErrors();
        }
        public override void handleModuleDataChange(
            AbstractEmbarkBuilderModule module,
            AbstractEmbarkBuilderModuleData oldValues,
            AbstractEmbarkBuilderModuleData newValues
            )
        {
            if (module is not QudGenotypeModule genotypeModule
                && module is not QudSubtypeModule)
                return;

            if (GetDefaultPlayerBodyPlan() is string playerAnatomyName
                && AnatomyChoices.FirstOrDefault(a => a.Anatomy != null && a.Anatomy.Name == playerAnatomyName) is AnatomyChoice playerAnatomyChoice
                && !playerAnatomyChoice.Equals(default))
            {
                AnatomyChoices.OrderBy(a => a.Anatomy.Name);
                AnatomyChoices.Remove(playerAnatomyChoice);
                AnatomyChoices.Insert(0, playerAnatomyChoice);
                SetDefautChoice();
            }
        }

        private string GetPlayerBlueprint()
        {
            var body = builder.GetModule<QudGenotypeModule>()?.data?.Entry?.BodyObject
                .Coalesce(builder.GetModule<QudSubtypeModule>()?.data?.Entry?.BodyObject)
                .Coalesce("Humanoid");

            return builder.info?.fireBootEvent(QudGameBootModule.BOOTEVENT_BOOTPLAYEROBJECTBLUEPRINT, The.Game, body);
        }

        public string GetDefaultPlayerBodyPlan()
            => GameObjectFactory.Factory
                ?.GetBlueprintIfExists(GetPlayerBlueprint())
                ?.GetPartParameter<string>(nameof(Body), nameof(Body.Anatomy));

        public Qud_UD_BodyPlanModuleData GetDefaultData()
            => AnatomyChoices.FirstOrDefault(a => a?.Anatomy?.Name == GetDefaultPlayerBodyPlan()) is AnatomyChoice defaultChoice
            ? new(defaultChoice)
            : new();

        public void SetDefautChoice()
        {
            if (shouldBeEnabled()
                && _AnatomyChoices.FirstOrDefault(a => a?.Anatomy?.Name == GetDefaultPlayerBodyPlan()) is AnatomyChoice defaultChoice)
                defaultChoice.IsDefault = true;
        }

        public bool IsSelected(AnatomyChoice Choice)
        {
            if (data == null)
                return false;

            if (data.Selections.Count == 0)
                return Choice.Anatomy == null;

            return data.Selections[0] is Qud_UD_BodyPlanModuleDataRow bodyPlanDataRow
                && bodyPlanDataRow.Anatomy == Choice.Anatomy?.Name;
        }

        public AnatomyChoice SelectedChoice()
            => AnatomyChoices.Find(IsSelected);
    }
}
