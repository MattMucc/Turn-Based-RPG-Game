using System;
using Game.GridPrototype;
using UnityEngine;
using UnityEngine.UI;
public sealed partial class ProgressionMenu
{
    SaveGameService menuSaveService;
    int savePendingSlot=-1;bool pendingLoad;
    void SaveMenuChanged(){if(open&&page==Page.Saves)Rebuild();}
    void Saves()
    {
        if(!menuSaveService){menuSaveService=Manager.Saves;menuSaveService.Changed+=SaveMenuChanged;}
        float width=Mathf.Max(300,body.rect.width);
        Label(Rect("Save title",body,0,0,width,40),"SAVE / LOAD",22);
        if(savePendingSlot>=0){
            Label(Rect("Save confirmation",body,0,60,width,100),pendingLoad?"Load this save? Unsaved progress will be replaced.":"Overwrite this manual slot with your current progress?",20);
            int slot=savePendingSlot;
            Button(body,pendingLoad?"Confirm load":"Confirm overwrite",0,176,width,()=>{bool load=pendingLoad;savePendingSlot=-1;if(load)menuSaveService.Load(slot);else menuSaveService.SaveManual(slot,true);});
            Button(body,"Cancel save/load",0,226,width,()=>{savePendingSlot=-1;Rebuild();});
            Detail("Confirmation",pendingLoad?"Your party and world state will return to this save. Loading is unavailable during combat.":"The previous file is kept as a recovery backup. This slot will now represent your current progress.");return;
        }
        var list=List(body,"Save slots",0,52,width,Mathf.Max(260,body.rect.height-52),440);
        for(int i=0;i<4;i++){
            int slot=i;string name=i==3?"Checkpoint autosave":"Manual slot "+(i+1);bool exists=menuSaveService.Store.Exists(i);GameSaveData data=null;string description="Empty";
            if(exists)try{data=menuSaveService.Store.Read(i,out bool backup);description=data.CaptainName+" · "+(DateTime.TryParse(data.SavedUtc,out var date)?date.ToLocalTime().ToString("g"):data.SavedUtc)+(backup?" · backup":"");}catch(Exception){description="Unreadable save";}
            Label(Rect(name,list,10,i*110,width-24,30),name,18);
            Label(Rect("Slot details",list,10,i*110+30,width-24,26),description,13);
            if(i<3)Button(list,"Save slot "+(i+1),10,i*110+60,(width-32)/2,()=>{if(menuSaveService.Store.Exists(slot)){savePendingSlot=slot;pendingLoad=false;Rebuild();}else menuSaveService.SaveManual(slot,false);}).interactable=menuSaveService.CanOperate;
            Button(list,i==3?"Load checkpoint":"Load slot "+(i+1),i==3?10:18+(width-32)/2,i*110+60,i==3?width-24:(width-32)/2,()=>{savePendingSlot=slot;pendingLoad=true;Rebuild();}).interactable=data!=null&&menuSaveService.CanOperate;
        }
        Detail("Saving your progress","Three manual slots and a separate checkpoint autosave.\n\nEnter a checkpoint to auto-save. Saving and loading are unavailable during combat or character switching.\n\nThis prototype restores saves in their original scene; load them while that scene is open.\n\n"+menuSaveService.Notice);
    }
}
