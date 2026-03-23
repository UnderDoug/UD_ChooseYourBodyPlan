using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UD_ChooseYourBodyPlan.Mod;

using XRL.Collections;
using XRL.World.Anatomy;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_CYBP_BodyPlanNaturalEquipment : IScribedPart
    {
        public string Anatomy;

        public BodyPlanEntry Entry => BodyPlanFactory.Factory?.GetBodyPlanEntry(Anatomy);

        public string NaturalEquipment;

        private Dictionary<string, string> _BlueprintByBodyPartType;
        public Dictionary<string, string> BlueprintByBodyPartType
        {
            get
            {
                if (!Anatomy.IsNullOrEmpty()
                    && _BlueprintByBodyPartType.IsNullOrEmpty()
                    && !NaturalEquipment.IsNullOrEmpty())
                    ParseNaturalEquipment();

                return _BlueprintByBodyPartType;
            }
        }

        [NonSerialized]
        public Dictionary<int, GameObject> NaturalEquipmentByBodyPartID;

        public bool DoDerivedFrom;

        private bool DoneDerivedFrom;

        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
            base.Write(Basis, Writer);

            Writer.WriteOptimized(NaturalEquipmentByBodyPartID?.Count ?? 0);
            foreach (var entry in NaturalEquipmentByBodyPartID)
            {
                Writer.WriteOptimized(entry.Key);
                Writer.WriteGameObject(entry.Value);
            }
        }

        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            base.Read(Basis, Reader);

            int count = Reader.ReadOptimizedInt32();
            for (int i = 0; i < count; i++)
            {
                NaturalEquipmentByBodyPartID ??= new();
                NaturalEquipmentByBodyPartID[Reader.ReadOptimizedInt32()] = Reader.ReadGameObject();
            }
        }

        public static void ParseNaturalEquipment(
            string NaturalEquipment,
            ref Dictionary<string, string> BlueprintByBodyPartType
            )
        {
            BlueprintByBodyPartType ??= new();
            if (NaturalEquipment.Contains(','))
            {
                foreach (var typeNamedBlueprint in NaturalEquipment.CachedCommaExpansion())
                    if (IsValidBodyPartAndBlueprint(typeNamedBlueprint, typeNamedBlueprint))
                        BlueprintByBodyPartType[typeNamedBlueprint] = typeNamedBlueprint;
            }
            else
            if (NaturalEquipment.Contains("::"))
            {
                foreach ((var bodyPartType, var blueprintName) in NaturalEquipment.CachedDictionaryExpansion())
                    if (IsValidBodyPartAndBlueprint(bodyPartType, blueprintName))
                        BlueprintByBodyPartType[bodyPartType] = blueprintName;
            }
            else
            {
                if (IsValidBodyPartAndBlueprint(NaturalEquipment, NaturalEquipment))
                    BlueprintByBodyPartType[NaturalEquipment] = NaturalEquipment;
            }
        }
        public void ParseNaturalEquipment(ref Dictionary<string, string> BlueprintByBodyPartType)
            => ParseNaturalEquipment(NaturalEquipment, ref BlueprintByBodyPartType);

        public void ParseNaturalEquipment(string NaturalEquipment)
            => ParseNaturalEquipment(NaturalEquipment, ref _BlueprintByBodyPartType);

        public void ParseNaturalEquipment()
            => ParseNaturalEquipment(NaturalEquipment, ref _BlueprintByBodyPartType);

        public bool MergeFrom(UD_CYBP_BodyPlanNaturalEquipment Other)
        {
            if (Anatomy.IsNullOrEmpty())
                Anatomy = Other.Anatomy;

            if (Anatomy.IsNullOrEmpty())
                return false;

            if (Anatomy != Other.Anatomy)
                return false;

            if (BlueprintByBodyPartType.IsNullOrEmpty())
            {
                NaturalEquipment = Other.NaturalEquipment;
                return true;
            }
            
            foreach ((var bodyPartType, var blueprintName) in Other.BlueprintByBodyPartType ?? Enumerable.Empty<KeyValuePair<string, string>>())
                BlueprintByBodyPartType[bodyPartType] = blueprintName;

            if (BlueprintByBodyPartType.IsNullOrEmpty())
                NaturalEquipment = null;
            else
                NaturalEquipment = BlueprintByBodyPartType.ToStringForCachedDictionaryExpansion();

            _BlueprintByBodyPartType = null;

            return true;
        }

        public bool MergeFrom(GamePartBlueprint PartBlueprint)
        {
            if (PartBlueprint.Name != GetType().Name)
            {
                Utils.Error($"Attempted to merge {nameof(GamePartBlueprint)} {PartBlueprint.Name} into {GetType().Name}.");
                return false;
            }    

            if (Anatomy.IsNullOrEmpty())
                 PartBlueprint.TryGetParameter(nameof(Anatomy), out Anatomy);

            if (Anatomy.IsNullOrEmpty())
                return false;

            if (PartBlueprint.TryGetParameter(nameof(Anatomy), out string anatomy)
                && Anatomy != anatomy)
                return false;

            if (BlueprintByBodyPartType.IsNullOrEmpty())
                return PartBlueprint.TryGetParameter(nameof(NaturalEquipment), out NaturalEquipment);

            if (PartBlueprint.TryGetParameter(nameof(NaturalEquipment), out string naturalEquipment))
                ParseNaturalEquipment(naturalEquipment);

            if (BlueprintByBodyPartType.IsNullOrEmpty())
                NaturalEquipment = null;
            else
                NaturalEquipment = BlueprintByBodyPartType.ToStringForCachedDictionaryExpansion();

            _BlueprintByBodyPartType = null;

            return true;
        }

        public override void Attach()
        {
            base.Attach();
            Anatomy = ParentObject?.Body?.Anatomy;
            ManageNaturalEquipment();
        }

        public static bool IsValidBodyPartAndBlueprint(string BodyPartType, string BlueprintName)
            => Anatomies.BodyPartTypeTable.ContainsKey(BodyPartType)
            && GameObjectFactory.Factory.HasBlueprint(BlueprintName)
            ;

        public UD_CYBP_BodyPlanNaturalEquipment ManageNaturalEquipment()
        {
            if (ParentObject == null)
                return this;

            if (Anatomy.IsNullOrEmpty())
                return this;

            if (ParentObject?.Body?.Anatomy != Anatomy)
                return this;

            if (BlueprintByBodyPartType.IsNullOrEmpty())
                return this;

            NaturalEquipmentByBodyPartID ??= new();

            foreach (var bodyPart in ParentObject.Body.LoopParts())
            {
                if (bodyPart.IsDismembered)
                    continue;

                if (!BlueprintByBodyPartType.TryGetValue(bodyPart.VariantTypeModel().Type, out string naturalEquipmentBlueprint))
                    continue;

                if (!NaturalEquipmentByBodyPartID.TryGetValue(bodyPart.ID, out GameObject naturalEquipmentObject))
                    naturalEquipmentObject = GameObject.CreateUnmodified(
                        Blueprint: naturalEquipmentBlueprint,
                        AfterObjectCreated: GO => GO.SetIntProperty(GetType().Name, 1));

                if (naturalEquipmentObject == null)
                    continue;

                if (bodyPart.Equipped is GameObject equippedObject)
                {
                    if (equippedObject == naturalEquipmentObject)
                        continue;

                    if (equippedObject.IsNatural()
                        && equippedObject.Blueprint == naturalEquipmentBlueprint)
                    {
                        NaturalEquipmentByBodyPartID[bodyPart.ID] = equippedObject;
                        naturalEquipmentObject?.Obliterate();
                        continue;
                    }

                    if (!bodyPart.ForceUnequip(Silent: true))
                    {
                        Utils.Warn($"Failed to force unequip {bodyPart} of {equippedObject.DebugName}");
                        naturalEquipmentObject?.Obliterate();
                        continue;
                    }
                }

                if (!GameObject.Validate(ref naturalEquipmentObject))
                    continue;

                if (bodyPart.Equip(Item: naturalEquipmentObject, Silent: true, Forced: true))
                    NaturalEquipmentByBodyPartID[bodyPart.ID] = naturalEquipmentObject;
            }

            return this;
        }

        public UD_CYBP_BodyPlanNaturalEquipment CleanUpNaturalEquipment()
        {
            if (ParentObject.Body.LoopParts() is not IEnumerable<BodyPart> bodyParts)
                return this;

            if (NaturalEquipmentByBodyPartID.IsNullOrEmpty())
                return this;

            var naturalEquipmentByBodyPartID = new Dictionary<int, GameObject>(NaturalEquipmentByBodyPartID);
            foreach ((int iD, var naturalEquipmentObject) in naturalEquipmentByBodyPartID)
            {
                if (GameObject.Validate(naturalEquipmentObject)
                    && bodyParts.FirstOrDefault(bp => bp.ID == iD) is BodyPart bodyPart)
                {
                    if (bodyPart.IsDismembered)
                        continue;

                    if (bodyPart.Equipped is GameObject equippedNaturalEquipment)
                    {
                        if (equippedNaturalEquipment == naturalEquipmentObject)
                            continue;

                        if (equippedNaturalEquipment.Blueprint == naturalEquipmentObject.Blueprint)
                        {
                            naturalEquipmentObject.Obliterate();
                            NaturalEquipmentByBodyPartID[iD] = equippedNaturalEquipment;
                            continue;
                        }
                    }
                    NaturalEquipmentByBodyPartID.Remove(iD);
                    continue;
                }

                if (!bodyParts.Any(bp => bp.ID == iD))
                {
                    NaturalEquipmentByBodyPartID[iD].Clear();
                    NaturalEquipmentByBodyPartID.Remove(iD);
                    continue;
                }

                if (naturalEquipmentObject.EquippedOn() is BodyPart equippedBodyPart
                    && iD != equippedBodyPart.ID)
                {
                    NaturalEquipmentByBodyPartID[equippedBodyPart.ID] = NaturalEquipmentByBodyPartID[iD];
                    NaturalEquipmentByBodyPartID.Remove(iD);
                    continue;
                }
            }
            naturalEquipmentByBodyPartID.Clear();
            naturalEquipmentByBodyPartID = null;

            foreach (var equipment in ParentObject.Inventory.GetObjects())
            {
                if (equipment.GetIntProperty(GetType().Name, 0) != 0
                    && equipment.EquippedOn()?.ParentBody != ParentObject.Body)
                {
                    equipment.Obliterate();
                }
            }

            return this;
        }

        public override bool AllowStaticRegistration()
            => true;

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(
                EventID: RegenerateDefaultEquipmentEvent.ID,
                Order: EventOrder.EXTREMELY_LATE,
                Serialize: true);

            Registrar.Register(
                EventID: DecorateDefaultEquipmentEvent.ID,
                Order: EventOrder.EXTREMELY_LATE,
                Serialize: true);
            base.Register(Object, Registrar);
        }

        public override bool WantEvent(int ID, int Cascade)
            => base.WantEvent(ID, Cascade)
            || ID == AfterDismemberEvent.ID
            ;

        public override bool HandleEvent(AfterDismemberEvent E)
        {
            // Save some data here, so that it can be restored if necessary.
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(RegenerateDefaultEquipmentEvent E)
        {
            ManageNaturalEquipment();
            CleanUpNaturalEquipment();
            if (DoDerivedFrom
                && !DoneDerivedFrom)
            {
                var naturalEquipmentList = Event.NewGameObjectList();
                GameObject derived = null;
                foreach (var bodyPart in ParentObject.Body.LoopParts())
                {
                    derived ??= bodyPart.DefaultBehavior ?? bodyPart.Equipped;

                    // bodyPart.DefaultBehavior?.SetStringProperty("TemporaryDefaultBehavior", GetType().Name);

                    naturalEquipmentList.AddIf(
                        element: bodyPart.DefaultBehavior,
                        conditional:
                            go => bodyPart.DefaultBehavior != null
                            && derived != bodyPart.DefaultBehavior
                            && !naturalEquipmentList.Contains(bodyPart.DefaultBehavior)
                        );
                    naturalEquipmentList.AddIf(
                        element: bodyPart.Equipped,
                        conditional:
                            go => bodyPart.Equipped != null
                            && derived != bodyPart.Equipped
                            && bodyPart.Equipped.IsNatural()
                            && !naturalEquipmentList.Contains(bodyPart.Equipped)
                        );
                }
                try
                {
                    WasDerivedFromEvent.Send(ParentObject, ParentObject, derived, null, naturalEquipmentList, GetType().Name);
                }
                catch (Exception x)
                {
                    Utils.Error(x);
                }
                try
                {
                    DerivationCreatedEvent.Send(derived, ParentObject, ParentObject, GetType().Name);
                }
                catch (Exception x)
                {
                    Utils.Error(x);
                }
                DoneDerivedFrom = true;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(DecorateDefaultEquipmentEvent E)
        {
            DoneDerivedFrom = false;
            return base.HandleEvent(E);
        }
    }
}
