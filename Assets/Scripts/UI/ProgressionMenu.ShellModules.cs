using UnityEngine;
using UnityEngine.UI;
public sealed partial class ProgressionMenu {
 [SerializeField] PlayerMenuShellLayout shellLayout;
 PlayerMenuShellLayout ShellLayout=>shellLayout?shellLayout:Resources.Load<PlayerMenuShellLayout>("UI/Shell/PlayerMenu");
 PlayerMenuShellView modularShell;
 bool BuildModularShell(){
  var layout=ShellLayout;if(!layout||!layout.Prefab)return false;
  modularShell=InventoryModularUI.Fill(layout.Prefab,canvasObject.transform);modularShell.name="Menu frame";
  shell=(RectTransform)modularShell.transform;body=modularShell.Body;detailRoot=modularShell.Details;title=modularShell.Title;characterLabel=modularShell.Character.Name;footer=modularShell.Footer;
  canvasObject.GetComponent<CanvasScaler>().referenceResolution=layout.ReferenceResolution;
  foreach(var command in modularShell.GetComponentsInChildren<PlayerMenuCommandView>(true)){
   var page=command.Page;command.Button.Select.onClick.AddListener(()=>ShowPage(page));command.Button.Select.gameObject.AddComponent<PlayerMenuFocus>();
  }
  modularShell.Character.Previous.Select.onClick.AddListener(()=>Cycle(-1));modularShell.Character.Next.Select.onClick.AddListener(()=>Cycle(1));modularShell.Exit.Select.onClick.AddListener(Close);
  return true;
 }
 void UpdateModularShell(bool service,bool showMember){
  if(modularShell.Currency)modularShell.Currency.text=Manager.Equipment.Coins.ToString("N0")+" coins";
  PlayerMenuShellPresentation.Apply(modularShell,ShellLayout.Prefab,service,showMember);
  var templates=ShellLayout.Prefab.GetComponentsInChildren<PlayerMenuCommandView>(true);
  foreach(var command in modularShell.GetComponentsInChildren<PlayerMenuCommandView>(true)){
   var original=System.Array.Find(templates,c=>c.Page==command.Page);
   command.Button.Select.image.color=command.Page==page?ShellLayout.SelectedPageColor:original?original.Button.Select.image.color:Color.white;
  }
 }
}
