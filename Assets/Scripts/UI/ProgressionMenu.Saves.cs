using System;
using Game.GridPrototype;
using UnityEngine;
using UnityEngine.UI;
public sealed partial class ProgressionMenu
{
    SaveGameService menuSaveService;
    int savePendingSlot=-1;bool pendingLoad;
    enum SaveView { Home, Save, Load }
    SaveView saveView;
    bool BackFromSaves(){if(saveView==SaveView.Home||!(page==Page.Saves||page==Page.System&&systemTab==SystemTab.Saves))return false;saveView=SaveView.Home;Rebuild();return true;}
    void SaveMenuChanged(){if(open&&(page==Page.Saves||page==Page.Checkpoint||(page==Page.System&&systemTab==SystemTab.Saves)))Rebuild();}
    void Saves()
    {
        if(!menuSaveService){menuSaveService=Manager.Saves;menuSaveService.Changed+=SaveMenuChanged;}
        float width=Mathf.Max(300,body.rect.width);
        Label(Rect("Save title",body,0,0,width,40),checkpointContext?"MANUAL SAVE":"SAVE / LOAD",22);
        if(savePendingSlot>=0){
            Label(Rect("Save confirmation",body,0,60,width,100),pendingLoad?"Load this save? Unsaved progress will be replaced.":"Overwrite this manual slot with your current progress?",20);
            int slot=savePendingSlot;
            Button(body,pendingLoad?"Confirm load":"Confirm overwrite",0,176,width,()=>{bool load=pendingLoad;savePendingSlot=-1;if(load)menuSaveService.Load(slot);else menuSaveService.SaveManual(slot,true);});
            Button(body,"Cancel save/load",0,226,width,()=>{savePendingSlot=-1;Rebuild();});
            Detail("Confirmation",pendingLoad?"Your party and world state will return to this save. Loading is unavailable during combat.":"The previous file is kept as a recovery backup. This slot will now represent your current progress.");return;
        }
        if(saveView==SaveView.Home&&!checkpointContext){
            Button(body,"Save Game",0,60,width,()=>{saveView=SaveView.Save;Rebuild();}).interactable=menuSaveService.CanOperate;
            Button(body,"Load Game",0,110,width,()=>{saveView=SaveView.Load;Rebuild();}).interactable=menuSaveService.CanOperate;
            Button(body,"Load Last Checkpoint",0,160,width,()=>{savePendingSlot=3;pendingLoad=true;Rebuild();}).interactable=menuSaveService.CanOperate&&menuSaveService.HasCheckpoint;
            Detail("Save / Load",(menuSaveService.ActiveSlot<0?"No active manual slot":"Current slot: "+(menuSaveService.ActiveSlot+1))+"\n\nThree manual slots. Each slot keeps its own last checkpoint.\n\n"+menuSaveService.Notice);return;
        }
        bool loading=saveView==SaveView.Load&&!checkpointContext;
        var list=List(body,"Save slots",0,52,width,Mathf.Max(230,body.rect.height-52),380);
        for(int i=0;i<3;i++){
            int slot=i;bool exists=menuSaveService.Store.Exists(i);GameSaveData data=null;string description="Empty";
            if(exists)try{data=menuSaveService.Store.Read(i,out bool backup);description=data.CaptainName+" · "+(DateTime.TryParse(data.SavedUtc,out var date)?date.ToLocalTime().ToString("g"):data.SavedUtc)+(backup?" · backup":"");}catch(Exception){description="Unreadable save";}
            Button(list,(loading?"Load slot ":"Save slot ")+(i+1),10,i*100,width-24,()=>{
                if(loading||menuSaveService.Store.Exists(slot)){savePendingSlot=slot;pendingLoad=loading;Rebuild();}
                else menuSaveService.SaveManual(slot,false);
            }).interactable=menuSaveService.CanOperate&&(!loading||data!=null);
            Label(Rect("Slot details",list,10,i*100+42,width-24,50),description,13);
        }
        Button(list,"Back",10,310,width-24,()=>{if(checkpointContext)ShowPage(Page.Checkpoint);else{saveView=SaveView.Home;Rebuild();}});
        Detail(loading?"Load Game":"Save Game",loading?"Choose a manual save. Unsaved progress will be replaced after confirmation.":"Choose a destination. Existing saves require overwrite confirmation.\n\n"+menuSaveService.Notice);
    }
}
