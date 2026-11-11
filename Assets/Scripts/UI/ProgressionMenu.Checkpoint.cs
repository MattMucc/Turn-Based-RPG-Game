using UnityEngine;
public sealed partial class ProgressionMenu
{
    SaveCheckpoint checkpointContext;bool restConfirmation;
    public void OpenCheckpoint(SaveCheckpoint checkpoint)
    {
        if(!checkpoint||!checkpoint.CanInteract)return;
        Open();if(!open)return;checkpointContext=checkpoint;ShowPage(Page.Checkpoint);
    }
    void CheckpointPage()
    {
        if(!checkpointContext){Detail("Checkpoint","This checkpoint is no longer available.");Button(body,"Leave",0,60,400,Close);return;}
        if(!menuSaveService){menuSaveService=Manager.Saves;menuSaveService.Changed+=SaveMenuChanged;}
        float width=Mathf.Max(300,body.rect.width);
        Label(Rect("Checkpoint title",body,0,0,width,60),checkpointContext.DisplayName,23);
        if(restConfirmation){
            Label(Rect("Rest warning",body,0,76,width,110),"Restore everyone's HP and MP?\nRoaming enemies will return in new groups.\nYour checkpoint autosave will be updated.",18);
            Button(body,"Confirm rest",0,212,width,()=>{restConfirmation=false;Manager.Saves.Rest(checkpointContext);});
            Button(body,"Cancel rest",0,262,width,()=>{restConfirmation=false;Rebuild();});
        }else{
            Button(body,"Rest",0,80,width,()=>{restConfirmation=true;Rebuild();},()=>Detail("Rest","Recover the whole roster, including reserves and knocked-out members. Reset roaming groups, then update the checkpoint autosave. One-off and story victories remain cleared."));
            Button(body,"Save",0,134,width,()=>ShowPage(Page.Saves),()=>Detail("Save","Choose a manual slot. Saving alone does not heal the party, reset enemies, or replace your checkpoint autosave."));
            Button(body,"Leave",0,188,width,Close);
        }
        Detail(checkpointContext.DisplayName,"Rest is free. It restores HP/MP for the whole party and revives knocked-out members.\n\nRoaming groups reset from their authored pools; one-off and story encounters stay cleared.\n\n"+Manager.Saves.Notice);
    }
}
