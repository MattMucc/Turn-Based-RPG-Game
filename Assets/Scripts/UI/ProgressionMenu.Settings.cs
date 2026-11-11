using System;
using Game.GridPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class ProgressionMenu
{
    enum SettingsTab { Audio, Display, Controls, Battle }
    SettingsTab settingsTab;
    string settingsSelection;
    bool resetSettingsPending;
    bool? pendingFullscreen;
    readonly DisplayChangePreview displayPreview=new();
    TMP_Text displayCountdown;
    const string AutoCameraKey="PlayerMenu.AutoCamera", OrderTopKey="PlayerMenu.OrderTop", FullscreenKey="PlayerMenu.Fullscreen";
    void Start()
    {
        ApplySavedMenuSettings();
        // Applying saved window state in a build is safe; never alter the Unity editor window at startup.
        if(!Application.isEditor && PlayerPrefs.HasKey(FullscreenKey))Screen.fullScreen=PlayerPrefs.GetInt(FullscreenKey)!=0;
    }
    void ApplySavedMenuSettings()
    {
        if(!Manager)return;
        if(Manager.Input&&PlayerPrefs.HasKey("PlayerMenu.Icons"))Manager.Input.ControllerIcons=(ControllerIconStyle)Mathf.Clamp(PlayerPrefs.GetInt("PlayerMenu.Icons"),0,2);
        if(PlayerPrefs.HasKey("PlayerMenu.Assisted"))ApplyAssistance(PlayerPrefs.GetInt("PlayerMenu.Assisted")!=0);
    }
    void ApplyAssistance(bool assisted)
    { foreach(var member in Manager.Progression.Members){if(!member)continue;var defense=member.GetComponent<DefensePrototypeSettings>();if(defense)defense.Assisted=assisted;} }
    void RememberBool(string key,bool value){PlayerPrefs.SetInt(key,value?1:0);PlayerPrefs.Save();}
    void SettingsRebuild(string selected){settingsSelection=selected;Rebuild();}
    void Settings()
    {
        float width=Mathf.Max(300,body.rect.width),tabWidth=(width-18)/4;
        for(int i=0;i<4;i++){
            var tab=(SettingsTab)i;var b=Button(body,tab.ToString(),i*(tabWidth+6),0,tabWidth,()=>{
                CancelDisplayChange();resetSettingsPending=false;settingsTab=tab;SettingsRebuild(tab.ToString());});
            if(tab==settingsTab)b.image.color=new Color(.16f,.36f,.44f);
        }
        var list=List(body,"Settings options",0,54,width,Mathf.Max(230,body.rect.height-54),410);
        width-=20;
        if(displayPreview.Active){
            displayCountdown=Label(Rect("Display countdown",list,12,16,width-24,80),"",19);
            UpdateDisplayCountdown();
            Button(list,"Keep display settings",12,108,width-24,KeepDisplayChange);
            Button(list,"Revert display settings",12,156,width-24,()=>{CancelDisplayChange();SettingsRebuild("Display mode");});
            Detail("Keep this display mode?","Keep the new mode if it looks right. Revert, Back, leaving Settings, or the countdown expiring restores the previous mode.");return;
        }
        if(resetSettingsPending){
            Label(Rect("Reset confirmation",list,12,16,width-24,100),$"Restore {settingsTab} defaults?\n"+SettingsDefaultsDescription(),18);
            Button(list,"Confirm defaults",12,136,width-24,ResetSettingsCategory);
            Button(list,"Cancel defaults",12,184,width-24,()=>{resetSettingsPending=false;SettingsRebuild("Restore defaults");});
            Detail("Restore defaults",SettingsDefaultsDescription()+"\n\nOnly this settings category will change. Party progress and shortcuts are unaffected.");return;
        }
        if(settingsTab==SettingsTab.Audio){
            Label(Rect("Volume readout",list,12,12,width-24,40),$"Master volume: {Mathf.RoundToInt(AudioListener.volume*100)}%",21);
            var track=Rect("Volume track",list,12,60,width-24,12);track.gameObject.AddComponent<Image>().color=new Color(.08f,.12f,.16f);
            Rect("Volume fill",track,0,0,(width-24)*Mathf.Clamp01(AudioListener.volume),12).gameObject.AddComponent<Image>().color=Accent;
            Button(list,"Volume -",12,92,(width-32)/2,()=>SetVolume(-.05f),()=>Detail("Master volume","Adjusts all game audio in 5% steps. Zero silences the game."));
            Button(list,"Volume +",20+(width-32)/2,92,(width-32)/2,()=>SetVolume(.05f),()=>Detail("Master volume","Adjusts all game audio in 5% steps. Zero silences the game."));
            Button(list,"Restore defaults",12,164,width-24,()=>{resetSettingsPending=true;SettingsRebuild("Cancel defaults");});
            Detail("Audio","Master volume adjusts all game audio. Changes apply immediately and are remembered between sessions.");
        }else if(settingsTab==SettingsTab.Display){
            bool chosen=pendingFullscreen??Screen.fullScreen;
            Button(list,"Display mode: "+(chosen?"Fullscreen":"Windowed"),12,12,width-24,()=>{pendingFullscreen=!chosen;SettingsRebuild("Display mode");},()=>Detail("Display mode","Choose Fullscreen or Windowed, then Apply. You will have 15 seconds to keep the change.\n\nIn Unity, the Game view is managed by the editor; verify actual window changes in a standalone build."));
            Button(list,"Apply display change",12,64,width-24,BeginDisplayChange).interactable=chosen!=Screen.fullScreen;
            Detail("Display","Choose a mode, then Apply. A 15-second confirmation protects against an unwanted display change. Only a kept mode is saved.");
        }else if(settingsTab==SettingsTab.Controls){
            var style=Manager.Input?Manager.Input.ControllerIcons:ControllerIconStyle.Auto;
            Button(list,"Controller icons: "+style,12,12,width-24,()=>{
                if(Manager.Input){Manager.Input.ControllerIcons=(ControllerIconStyle)(((int)style+1)%3);PlayerPrefs.SetInt("PlayerMenu.Icons",(int)Manager.Input.ControllerIcons);PlayerPrefs.Save();}SettingsRebuild("Controller icons");
            },()=>Detail("Controller icons","Auto follows the detected controller. Xbox and PlayStation override the icon appearance. This does not change button bindings.\n\nFavorite action shortcuts are configured on the Shortcuts page.")).interactable=Manager.Input;
            Button(list,"Restore defaults",12,76,width-24,()=>{resetSettingsPending=true;SettingsRebuild("Cancel defaults");});
            Detail("Controls","Choose automatic, Xbox, or PlayStation controller icons.\n\nMenu: arrows / D-pad to navigate, Enter / bottom face button to select, Escape or Backspace / east face button to go back.\n\nUse the Shortcuts page to assign favorite actions. Full control rebinding is not available yet.");
        }else{
            var defense=Member.GetComponent<DefensePrototypeSettings>();bool assisted=PlayerPrefs.HasKey("PlayerMenu.Assisted")?PlayerPrefs.GetInt("PlayerMenu.Assisted")!=0:defense&&defense.Assisted;
            bool auto=PlayerPrefs.GetInt(AutoCameraKey,1)!=0,top=PlayerPrefs.GetInt(OrderTopKey,1)!=0;
            Button(list,"Defense timing: "+(assisted?"Assisted":"Standard"),12,12,width-24,()=>{ApplyAssistance(!assisted);RememberBool("PlayerMenu.Assisted",!assisted);SettingsRebuild("Defense timing");},()=>Detail("Defense timing","Assisted widens dodge and parry windows using each character's configured assistance multiplier. It does not automatically defend or change attack timing. Applies to all party members."));
            Button(list,"Automatic battle camera: "+(auto?"On":"Off"),12,64,width-24,()=>{RememberBool(AutoCameraKey,!auto);SettingsRebuild("Automatic battle camera");},()=>Detail("Automatic battle camera","Automatically focuses and follows the active combatant. Off leaves camera positioning to you. Manual camera controls remain available."));
            Button(list,"Turn order: "+(top?"Top":"Right"),12,116,width-24,()=>{RememberBool(OrderTopKey,!top);SettingsRebuild("Turn order");},()=>Detail("Turn order placement","Choose whether the portrait turn order appears across the top or down the right side of battle."));
            Button(list,"Restore defaults",12,184,width-24,()=>{resetSettingsPending=true;SettingsRebuild("Cancel defaults");});
            Detail("Battle","Configure defense timing and battle presentation. Choices are remembered for future battles and can also be changed using the existing battle controls.");
        }
    }
    string SettingsDefaultsDescription()=>settingsTab==SettingsTab.Audio?"Master volume: 100%":settingsTab==SettingsTab.Controls?"Controller icons: Auto":"Defense timing: Standard\nAutomatic battle camera: On\nTurn order: Top";
    void ResetSettingsCategory()
    {
        if(!resetSettingsPending)return;resetSettingsPending=false;
        if(settingsTab==SettingsTab.Audio){AudioListener.volume=1;PlayerPrefs.SetFloat("PlayerMenu.Volume",1);}
        else if(settingsTab==SettingsTab.Controls){if(Manager.Input)Manager.Input.ControllerIcons=ControllerIconStyle.Auto;PlayerPrefs.SetInt("PlayerMenu.Icons",0);}
        else if(settingsTab==SettingsTab.Battle){ApplyAssistance(false);PlayerPrefs.SetInt("PlayerMenu.Assisted",0);PlayerPrefs.SetInt(AutoCameraKey,1);PlayerPrefs.SetInt(OrderTopKey,1);}
        PlayerPrefs.Save();SettingsRebuild("Restore defaults");
    }
    void SetVolume(float delta){AudioListener.volume=Mathf.Clamp01(Mathf.Round((AudioListener.volume+delta)*100)/100);PlayerPrefs.SetFloat("PlayerMenu.Volume",AudioListener.volume);PlayerPrefs.Save();SettingsRebuild(delta<0?"Volume -":"Volume +");}
    void RestoreSettingsSelection()
    {var button=buttons.Find(b=>b&&b.IsInteractable()&&settingsSelection!=null&&b.name.StartsWith(settingsSelection));if(button)events.SetSelectedGameObject(button.gameObject);}
    void BeginDisplayChange()
    {
        if(!pendingFullscreen.HasValue||pendingFullscreen==Screen.fullScreen)return;
        displayPreview.Begin(Screen.fullScreen,pendingFullscreen.Value,Time.realtimeSinceStartup);
        Screen.fullScreen=displayPreview.Requested;SettingsRebuild("Revert display settings");
    }
    void KeepDisplayChange()
    {
        if(!displayPreview.Active)return;
        if(displayPreview.Expired(Time.realtimeSinceStartup)){CancelDisplayChange();SettingsRebuild("Display mode");return;}
        RememberBool(FullscreenKey,displayPreview.Requested);displayPreview.End();pendingFullscreen=null;SettingsRebuild("Display mode");
    }
    void CancelDisplayChange()
    {if(displayPreview.Active){Screen.fullScreen=displayPreview.Original;displayPreview.End();}pendingFullscreen=null;}
    void UpdateDisplayCountdown(){if(displayCountdown)displayCountdown.text=$"Keep this display mode?\nReverting in {Mathf.CeilToInt(Mathf.Max(0,displayPreview.Deadline-Time.realtimeSinceStartup))} seconds";}
    void TickDisplayPreview()
    {
        if(!displayPreview.Active)return;
        if(displayPreview.Expired(Time.realtimeSinceStartup)){CancelDisplayChange();if(open)SettingsRebuild("Display mode");}else UpdateDisplayCountdown();
    }
    bool BackFromSettings()
    {
        if(page!=Page.Settings)return false;
        if(displayPreview.Active||pendingFullscreen.HasValue){CancelDisplayChange();SettingsRebuild("Display mode");return true;}
        if(resetSettingsPending){resetSettingsPending=false;SettingsRebuild("Restore defaults");return true;}
        return false;
    }
}
