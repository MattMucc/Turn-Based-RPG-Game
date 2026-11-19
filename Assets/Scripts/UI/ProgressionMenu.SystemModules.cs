using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public sealed partial class ProgressionMenu {
    [SerializeField] SystemPageLayout systemLayout;
    SystemPageLayout SystemLayout=>systemLayout?systemLayout:Resources.Load<SystemPageLayout>("UI/System/System");
    SystemPanelView modularSystem;
    SystemSaveSlotView lastSystemSlot;
    SystemControlView SystemControl(string key)=>modularSystem?modularSystem.GetComponentsInChildren<SystemControlView>(true).FirstOrDefault(c=>c.Key.Split('|').Any(k=>key==k||key.StartsWith(k+":"))):null;
    SystemPanelView SystemTemplate(){
        var l=SystemLayout;
        if(systemTab==SystemTab.Saves)return savePendingSlot>=0?l.Confirmation:saveView==SaveView.Home?l.SaveLoad:l.SaveSlots;
        if(displayPreview.Active||resetSettingsPending)return l.Confirmation;
        return settingsTab==SettingsTab.Battle?l.Battle:settingsTab==SettingsTab.Graphics?l.Graphics:settingsTab==SettingsTab.UI?l.UI:l.Audio;
    }
    bool ModularStandaloneSaves(){
        var l=SystemLayout;if(!l)return false;
        var template=savePendingSlot>=0?l.Confirmation:checkpointContext||saveView!=SaveView.Home?l.SaveSlots:l.SaveLoad;
        if(!template)return false;
        modularSystem=InventoryModularUI.Fill(template,body);
        var outer=body;body=(RectTransform)modularSystem.transform;
        try{Saves();}finally{body=outer;modularSystem=null;lastSystemSlot=null;}
        return true;
    }
    bool ModularSystemPage(){
        modularSystem=null;var l=SystemLayout;if(!l||!l.Workspace)return false;
        var workspace=InventoryModularUI.Fill(l.Workspace,body);
        string[] labels={"Save / Load","Battle","Graphics","UI","Audio"};
        for(int i=0;i<labels.Length;i++){
            int index=i;var tab=InventoryModularUI.Choice(l.Tab,workspace.Tabs,labels[i]);
            buttons.Add(tab.Select);tab.Select.onClick.AddListener(()=>{if(index>0)settingsTab=(SettingsTab)(index-1);SelectSystemTab(index==0?SystemTab.Saves:SystemTab.Settings);});
            if(i==0?systemTab==SystemTab.Saves:systemTab==SystemTab.Settings&&(int)settingsTab==i-1)tab.Select.image.color=l.SelectedColor;
        }
        modularSystem=InventoryModularUI.Fill(SystemTemplate(),workspace.Content);
        var outer=body;body=(RectTransform)modularSystem.transform;
        try{if(systemTab==SystemTab.Settings)Settings();else Saves();}
        finally{body=outer;modularSystem=null;lastSystemSlot=null;}
        return true;
    }
    Button SystemButton(Transform parent,string name,float x,float y,float width,Action click,Action preview=null,float height=38){
        Button button=null;TMP_Text label=null;
        if(modularSystem&&(name.StartsWith("Save slot ")||name.StartsWith("Load slot "))){
            lastSystemSlot=Instantiate(SystemLayout.SaveSlot,parent,false);button=lastSystemSlot.Select;label=lastSystemSlot.Name;
        }else{var control=SystemControl(name);if(control){button=control.Button;label=control.Text;}}
        if(!button)return Button(parent,name,x,y,width,click,preview,height);
        button.name=name;if(label)label.text=name;buttons.Add(button);button.onClick.AddListener(()=>click?.Invoke());
        var focus=button.GetComponent<PlayerMenuFocus>();if(!focus)focus=button.gameObject.AddComponent<PlayerMenuFocus>();focus.Preview=preview;return button;
    }
    TMP_Text SystemLabel(RectTransform temporary,string value,int size=17){
        var control=SystemControl(temporary.name);var text=temporary.name=="Slot details"&&lastSystemSlot?lastSystemSlot.Details:control?control.Text:null;
        if(!text)return Label(temporary,value,size);
        temporary.gameObject.SetActive(false);if(Application.isPlaying)Destroy(temporary.gameObject);else DestroyImmediate(temporary.gameObject);
        text.text=value;return text;
    }
    RectTransform SystemList(Transform parent,string name,float x,float y,float width,float height,float contentHeight){
        if(modularSystem){if(modularSystem.Scroll){modularSystem.Scroll.name=name;return modularSystem.Scroll.content;}modularSystem.name=name;return (RectTransform)modularSystem.transform;}
        return List(parent,name,x,y,width,height,contentHeight);
    }
    void FitSystemSlots(){if(!modularSystem||!modularSystem.Scroll)return;Canvas.ForceUpdateCanvases();AbilitiesModularUI.SizeList(modularSystem.Scroll);}
}
