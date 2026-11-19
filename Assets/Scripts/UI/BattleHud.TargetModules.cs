using UnityEngine;
public sealed partial class BattleHud {
 [SerializeField] TargetUILayout targetLayout;
 bool suppressTargetLayout;
 TargetUILayout Targets=>suppressTargetLayout?null:targetLayout?targetLayout:Resources.Load<TargetUILayout>("UI/Targets/Targets");
 TargetDetailsView modularTarget;
 bool BuildModularTarget(){
  if(!Targets||!Targets.Target)return false;
  modularTarget=Instantiate(Targets.Target,root,false);var v=modularTarget;
  targetPanel=(RectTransform)v.transform;targetPanel.name="Hovered or pinned target";
  targetName=v.Name;targetHealth=v.Health;targetFill=v.HealthFill;targetMana=v.Mana;targetManaFill=v.ManaFill;targetManaText=v.ManaText;
  targetShield=v.Shield;targetShieldFill=v.ShieldFill;targetShieldText=v.ShieldText;targetResources=v.Resources;damagePreview=v.DamagePreview;
  targetStatuses=v.Statuses;targetStatuses.Layout=Targets;targetStatuses.Initialize(root,font);targetPanel.gameObject.SetActive(false);return true;
 }
 bool BuildModularInspection(){
  if(!Targets||!Targets.Inspection)return false;
  var v=Instantiate(Targets.Inspection,root,false);inspectionLayer=(RectTransform)v.transform;inspectionLayer.name="Inspection modal";
  contextPanel=v.Context;contextUse=v.Use;examinePanel=v.Panel;examineContent=v.Content;examineText=v.Details;examineStatuses=v.Statuses;
  examineStatuses.Layout=Targets;examineStatuses.Initialize(root,font);
  v.Dismiss.onClick.AddListener(CloseInspection);v.Examine.onClick.AddListener(()=>{contextChoice=0;ControllerInspectSubmit();});v.UseButton.onClick.AddListener(UseInspectedObject);v.Close.onClick.AddListener(CloseInspection);
  CloseInspection();return true;
 }
}
