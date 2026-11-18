using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using Game.GridPrototype;
using UnityEngine;

public sealed class SaveGameService : MonoBehaviour
{
    public bool Busy {get;private set;}
    public int ActiveSlot {get;private set;}=-1;
    public GameSaveData ReadCheckpoint(out bool recovered)
    {
        if(ActiveSlot<0)return Store.Read(SaveFileStore.CheckpointSlot,out recovered);
        var data=Store.Read(ActiveSlot,out recovered);
        if(string.IsNullOrEmpty(data.CheckpointPayload))throw new InvalidDataException("This save slot has no checkpoint yet.");
        return SaveFileStore.Decode(data.CheckpointPayload);
    }
    public bool HasCheckpoint {get {try{ReadCheckpoint(out _);return true;}catch(Exception){return false;}}}
    public string Notice {get;private set;}="";
    public event Action Changed;
    float noticeUntil;
    SaveFileStore store;
    public SaveFileStore Store => store??(store=new SaveFileStore(Path.Combine(Application.persistentDataPath,"Saves")));
    public bool CanOperate => !Busy && GameManager.Instance && GameManager.Instance.PossessedCharacter && !GameManager.Instance.IsSwitchingCharacter && !(BattleManager.Instance&&BattleManager.Instance.InBattle);
    public void SaveManual(int slot,bool overwrite){if(slot<0||slot>=SaveFileStore.ManualSlotCount)return;BeginSave(slot,overwrite,null);}
    public void Rest(SaveCheckpoint checkpoint)
    {
        if(!CanOperate||!checkpoint||!checkpoint.isActiveAndEnabled||!checkpoint.InRange(GameManager.Instance.PossessedCharacter.transform)){Report("Cannot rest here right now.");return;}
        StartCoroutine(RestAndSave(checkpoint));
    }
    IEnumerator RestAndSave(SaveCheckpoint checkpoint)
    {
        Busy=true;Notice="Resting...";Changed?.Invoke();yield return null;
        string failure=null;GameSaveData before=null;
        try{before=GameSaveSnapshot.Capture(GameManager.Instance,null);if(CheckpointRest.TryApply(GameManager.Instance,checkpoint,out failure))failure=null;}
        catch(Exception e){failure=e.Message;if(before!=null)try{GameSaveSnapshot.Apply(before,GameManager.Instance);}catch(Exception restoreError){Debug.LogException(restoreError);}}
        if(failure!=null){Busy=false;Report("Rest failed: "+failure);yield break;}
        // Recovery and the chosen enemy compositions are now part of the autosave snapshot.
        yield return Write(SaveFileStore.CheckpointSlot,true,checkpoint.DisplayName);
    }
    void BeginSave(int slot,bool overwrite,string checkpoint){if(!CanOperate){Report("Cannot save during battle or a character transition.");return;}StartCoroutine(Write(slot,overwrite,checkpoint));}
    IEnumerator Write(int slot,bool overwrite,string checkpoint)
    {
        Busy=true;Notice=slot==SaveFileStore.CheckpointSlot?"Auto-saving...":"Saving...";Changed?.Invoke();yield return null;
        string encoded=null;Exception error=null;
        try{
            var snapshot=GameSaveSnapshot.Capture(GameManager.Instance,checkpoint);
            if(slot==SaveFileStore.CheckpointSlot&&ActiveSlot>=0){var manual=Store.Read(ActiveSlot,out _);manual.CheckpointPayload=SaveFileStore.Encode(snapshot);encoded=SaveFileStore.Encode(manual);}
            else {if(slot<SaveFileStore.ManualSlotCount){try{snapshot.CheckpointPayload=SaveFileStore.Encode(ReadCheckpoint(out _));}catch(Exception){snapshot.CheckpointPayload=null;}}encoded=SaveFileStore.Encode(snapshot);}
        }catch(Exception e){error=e;}
        if(error==null){var store=Store;int destination=slot==SaveFileStore.CheckpointSlot&&ActiveSlot>=0?ActiveSlot:slot;var task=Task.Run(()=>store.Write(destination,encoded,overwrite));while(!task.IsCompleted)yield return null;if(task.IsFaulted)error=task.Exception.GetBaseException();}
        yield return new WaitForSecondsRealtime(.4f);
        if(error==null&&slot<SaveFileStore.ManualSlotCount)ActiveSlot=slot;
        Busy=false;Report(error==null?(slot==SaveFileStore.CheckpointSlot?"Checkpoint saved":"Game saved"):"Save failed: "+error.Message);
    }
    public void Load(int slot){if(slot<0||slot>SaveFileStore.CheckpointSlot||!CanOperate){Report("Cannot load during battle or a character transition.");return;}StartCoroutine(Read(slot));}
    IEnumerator Read(int slot)
    {
        Busy=true;Notice="Loading...";Changed?.Invoke();yield return null;
        Exception error=null;bool recovered=false;GameSaveData data=null;
        // Decode and validate fully before altering any gameplay state.
        try{data=slot==SaveFileStore.CheckpointSlot?ReadCheckpoint(out recovered):Store.Read(slot,out recovered);GameSaveSnapshot.Validate(data,GameManager.Instance);}catch(Exception e){error=e;}
        if(error==null){
            var manager=GameManager.Instance;GameSaveData rollback=null;
            try{rollback=GameSaveSnapshot.Capture(manager,null);GameSaveSnapshot.Apply(data,manager);if(slot<SaveFileStore.ManualSlotCount)ActiveSlot=slot;manager.ProgressionMenu.Close();}
            catch(Exception e){error=e;if(rollback!=null)try{GameSaveSnapshot.Apply(rollback,manager);}catch(Exception rollbackError){Debug.LogException(rollbackError);}}
        }
        yield return new WaitForSecondsRealtime(.4f);Busy=false;
        Report(error==null?(recovered?"Loaded backup save":"Game loaded"):"Load failed: "+error.Message);
    }
    public void RecoverDefeat()
    {
        try{var checkpoint=ReadCheckpoint(out _);GameSaveSnapshot.Validate(checkpoint,GameManager.Instance);Load(SaveFileStore.CheckpointSlot);return;}catch(Exception){}
        if(ActiveSlot>=0){Load(ActiveSlot);return;}
        int latest=-1;string time="";
        for(int i=0;i<SaveFileStore.ManualSlotCount;i++)try{var data=Store.Read(i,out _);GameSaveSnapshot.Validate(data,GameManager.Instance);if(string.CompareOrdinal(data.SavedUtc,time)>0){time=data.SavedUtc;latest=i;}}catch(Exception){}
        if(latest>=0)Load(latest);else Report("Party defeated. No usable checkpoint or manual save was found.");
    }
    void Report(string text){Notice=text;noticeUntil=Time.unscaledTime+5;Changed?.Invoke();Debug.Log("[Save] "+text);}
    Canvas noticeCanvas;TMPro.TMP_Text noticeLabel;UnityEngine.UI.Image saveIcon;
    void LateUpdate()
    {
        if(!Application.isPlaying)return;
        if(!noticeCanvas){
            var go=new GameObject("Save indicator",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));go.transform.SetParent(transform,false);
            noticeCanvas=go.GetComponent<Canvas>();noticeCanvas.renderMode=RenderMode.ScreenSpaceOverlay;noticeCanvas.sortingOrder=1000;
            var scaler=go.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);
            var icon=new GameObject("Save icon",typeof(RectTransform),typeof(UnityEngine.UI.Image));icon.transform.SetParent(go.transform,false);
            var rect=icon.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(1,1);rect.pivot=Vector2.one;rect.anchoredPosition=new Vector2(-232,-22);rect.sizeDelta=new Vector2(20,20);saveIcon=icon.GetComponent<UnityEngine.UI.Image>();saveIcon.raycastTarget=false;
            var inset=new GameObject("Disk label",typeof(RectTransform),typeof(UnityEngine.UI.Image));inset.transform.SetParent(icon.transform,false);var small=inset.GetComponent<RectTransform>();small.sizeDelta=new Vector2(10,7);small.anchoredPosition=new Vector2(0,3);var image=inset.GetComponent<UnityEngine.UI.Image>();image.color=new Color(.05f,.1f,.15f);image.raycastTarget=false;
            var text=new GameObject("Save status",typeof(RectTransform));text.transform.SetParent(go.transform,false);var tr=text.GetComponent<RectTransform>();tr.anchorMin=tr.anchorMax=Vector2.one;tr.pivot=Vector2.one;tr.anchoredPosition=new Vector2(-20,-20);tr.sizeDelta=new Vector2(202,80);
            noticeLabel=text.AddComponent<TMPro.TextMeshProUGUI>();noticeLabel.font=TMPro.TMP_Settings.defaultFontAsset;noticeLabel.fontSize=15;noticeLabel.raycastTarget=false;noticeLabel.outlineWidth=.15f;noticeLabel.outlineColor=Color.black;
        }
        noticeCanvas.gameObject.SetActive(Busy||Time.unscaledTime<noticeUntil);noticeLabel.text=Notice;
        saveIcon.color=Busy?new Color(.5f,.85f,1,.65f+.35f*Mathf.Sin(Time.unscaledTime*6)):Color.white;
    }
}
