using System.Collections.Generic;
using System;
using System.Linq;
using Game.GridPrototype;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class ProgressionMenu
{
    [Header("Party page layouts (empty uses Resources/UI/Party defaults)")]
    [SerializeField] PartyPageLayout formationLayout,equipmentLayout,attributesLayout;
    bool BuildModularParty(PartyPageKind kind)
    {
        var layout=kind==PartyPageKind.Formation?formationLayout:kind==PartyPageKind.Equipment?equipmentLayout:attributesLayout;
        if(!layout)layout=Resources.Load<PartyPageLayout>("UI/Party/"+kind);
        if(!layout||!layout.Prefab)return false;
        var view=Instantiate(layout.Prefab,partyContent,false);view.name="Modular "+kind;
        var rect=(RectTransform)view.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        void Register(Button button,string name,Action action)
        {
            if(!button)return;button.name=name;button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>action());buttons.Add(button);
            if(!button.GetComponent<PlayerMenuFocus>())button.gameObject.AddComponent<PlayerMenuFocus>();
        }
        if(kind==PartyPageKind.Formation){
            var active=Manager.GetBattleParty();var roster=active.Concat(Manager.Progression.Members.Where(m=>m&&!active.Contains(m))).ToList();
            if(view.ActiveHeading)view.ActiveHeading.text="ACTIVE   "+active.Count+" / 3";
            if(view.ReserveHeading)view.ReserveHeading.text="RESERVES   "+(roster.Count-active.Count)+" / 3";
            foreach(var card in view.GetComponentsInChildren<PartyMemberCardView>(true)){
                if(card.RosterIndex>=roster.Count){card.gameObject.SetActive(false);continue;}
                var member=roster[card.RosterIndex];card.Name.text=DisplayName(member);card.Portrait.texture=Portrait(member);
                card.Role.text=card.RosterIndex==0?"CAPTAIN":card.RosterIndex<3?"ACTIVE":"RESERVE";card.Level.text="Lv "+member.Level;
                var unit=new GridUnit(member.name,Vector2Int.zero);member.Configure(unit);
                int hp=member.StoredHealth<0?unit.MaxHealth:Mathf.Clamp(member.StoredHealth,0,unit.MaxHealth);
                card.Vitals.text=$"HP {hp} / {unit.MaxHealth}\nMP {unit.MagicPoints} / {unit.MaxMagicPoints}"+(hp==0?" · KO":"");
                card.ExperienceFill.rectTransform.anchorMax=new Vector2(member.Level>=Manager.Progression.LevelCap?1:Mathf.Clamp01(Manager.Progression.ProgressFor(member)/(float)Manager.Progression.Cost(Mathf.Max(1,member.Level))),1);
                Register(card.Select,DisplayName(member),()=>SelectPartyMember(member));
                if(member==Member)card.Select.image.color=new Color(.18f,.34f,.39f);
                var pointer=card.Select.gameObject.AddComponent<PlayerInventoryPointer>();partyPortraits[pointer]=member;
                pointer.Context=()=>OpenPartyActions(member);pointer.Hover=()=>{if(!partyModal&&!inventoryModal&&!partyDragging)ShowMember(member);};
                pointer.Exit=()=>{if(!partyModal&&!inventoryModal&&!partyDragging)ShowMember(Member);};
                pointer.Begin=data=>BeginPartyDrag(member,data);pointer.Move=MovePartyDrag;pointer.End=_=>CancelPartyDrag();pointer.Cancel=CancelPartyDrag;
                pointer.Drop=source=>{if(partyPortraits.TryGetValue(source,out var from))StagePartySwap(from,member);};
            }
            ShowMember(Member);
        }else if(kind==PartyPageKind.Equipment){
            Manager.BindEquipment();InventoryEntries();var profile=Member.GetComponent<CharacterEquipment>();
            if(view.Character)view.Character.texture=Portrait(Member);
            if(view.WeaponAccess)view.WeaponAccess.text=profile?"Weapon types: "+string.Join(", ",profile.AllowedWeaponTypes):"No equipment slots available.";
            foreach(var slotView in view.GetComponentsInChildren<PartyEquipmentSlotView>(true)){
                var slot=slotView.Slot;var copy=profile?profile.CopyAt(slot):null;
                slotView.Caption.text=SlotName(slot);slotView.Empty.gameObject.SetActive(copy==null);slotView.Artwork.gameObject.SetActive(copy!=null&&copy.Definition.Image);
                if(copy!=null){slotView.Artwork.texture=copy.Definition.Image;if(!copy.Definition.Image)DrawInventoryIcon(slotView.Select.transform,inventoryEquipment.First(p=>p.Value==copy).Key,6,6,((RectTransform)slotView.Select.transform).rect.width-12);}
                Register(slotView.Select,SlotName(slot),()=>OpenPartyEquipmentPicker(slot));slotView.Select.interactable=profile;
                slotView.Select.GetComponent<PlayerMenuFocus>().Preview=()=>{if(profile){equipmentSlot=slot;PreviewEquipment(profile.InstanceAt(slot));}};
                var pointer=slotView.Select.gameObject.AddComponent<PlayerInventoryPointer>();pointer.Context=()=>OpenPartyEquipmentActions(slot);
            }
            equipmentSlot=EquipmentSlot.Weapon;if(profile)PreviewEquipment(profile.InstanceAt(equipmentSlot));else ShowMember(Member);
        }else{
            if(view.Points)view.Points.text=$"{Mathf.Max(0,Member.UnspentPoints-PartyAllocationCost)} ATTRIBUTE POINTS AVAILABLE";
            foreach(var row in view.GetComponentsInChildren<PartyAttributeRowView>(true)){
                var stat=row.Stat;int staged=partyAllocation.GetValueOrDefault(stat);
                row.Name.text=AttributePreview.Name(stat);row.Value.text=((long)Member.Stat(stat)+staged).ToString();
                row.Value.color=staged>0?new Color(.5f,.89f,.67f):row.Value.color;
                Register(row.Increase,"Increase "+stat,()=>StagePartyPoint(stat,1));Register(row.Decrease,"Decrease "+stat,()=>StagePartyPoint(stat,-1));
                row.Increase.interactable=PartyAllocationCost<Member.UnspentPoints&&(long)Member.Stat(stat)+staged<int.MaxValue;row.Decrease.interactable=staged>0;
            }
            Register(view.Apply,"Apply allocation",ConfirmPartyAllocation);if(view.Apply)view.Apply.interactable=PartyAllocationCost>0;
            Register(view.Reset,"Reset preview",()=>{partyAllocation.Clear();partyFocus="Increase Vitality";Rebuild();});
            PreviewPartyAllocation();
        }
        return true;
    }
}

