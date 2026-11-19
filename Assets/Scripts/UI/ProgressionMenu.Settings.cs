using System;
using Game.GridPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class ProgressionMenu
{
    enum SettingsTab { Battle, Graphics, UI, Audio }
    SettingsTab settingsTab;
    string settingsSelection;
    bool resetSettingsPending;
    FullScreenMode? pendingFullscreen;
    FullScreenMode previousDisplayMode,requestedDisplayMode;
    const string DisplayModeKey="PlayerMenu.DisplayMode";
    readonly DisplayChangePreview displayPreview=new();
    TMP_Text displayCountdown;
    const string AutoCameraKey="PlayerMenu.AutoCamera", OrderTopKey="PlayerMenu.OrderTop", FullscreenKey="PlayerMenu.Fullscreen";
    void Start()
    {
        ApplySavedMenuSettings();
        // Applying saved window state in a build is safe; never alter the Unity editor window at startup.
        if(!Application.isEditor){if(PlayerPrefs.HasKey(DisplayModeKey))Screen.fullScreenMode=(FullScreenMode)PlayerPrefs.GetInt(DisplayModeKey);else if(PlayerPrefs.HasKey(FullscreenKey))Screen.fullScreen=PlayerPrefs.GetInt(FullscreenKey)!=0;}
    }
    void ApplySavedMenuSettings()
    {
        if(!Manager)return;
        if(Manager.Input&&PlayerPrefs.HasKey("PlayerMenu.Icons"))Manager.Input.ControllerIcons=(ControllerIconStyle)Mathf.Clamp(PlayerPrefs.GetInt("PlayerMenu.Icons"),0,3);
        if(PlayerPrefs.HasKey("PlayerMenu.Assisted"))ApplyAssistance(PlayerPrefs.GetInt("PlayerMenu.Assisted")!=0);
    }
    void ApplyAssistance(bool assisted)
    { foreach(var member in Manager.Progression.Members){if(!member)continue;var defense=member.GetComponent<DefensePrototypeSettings>();if(defense)defense.Assisted=assisted;} }
    void RememberBool(string key,bool value){PlayerPrefs.SetInt(key,value?1:0);PlayerPrefs.Save();}
    void SettingsRebuild(string selected){settingsSelection=selected;Rebuild();}
    void Settings()
    {
        float width=Mathf.Max(300,body.rect.width);
        var list=SystemList(body,"Settings options",0,0,width,Mathf.Max(230,body.rect.height),410);
        width-=20;
        if(displayPreview.Active){
            displayCountdown=SystemLabel(Rect("Display countdown",list,12,16,width-24,80),"",19);
            UpdateDisplayCountdown();
            SystemButton(list,"Keep display settings",12,108,width-24,KeepDisplayChange);
            SystemButton(list,"Revert display settings",12,156,width-24,()=>{CancelDisplayChange();SettingsRebuild("Display mode");});
            Detail("Keep this display mode?","Keep the new mode if it looks right. Revert, Back, leaving Settings, or the countdown expiring restores the previous mode.");return;
        }
        if(resetSettingsPending){
            SystemLabel(Rect("Reset confirmation",list,12,16,width-24,100),$"Restore {settingsTab} defaults?\n"+SettingsDefaultsDescription(),18);
            SystemButton(list,"Confirm defaults",12,136,width-24,ResetSettingsCategory);
            SystemButton(list,"Cancel defaults",12,184,width-24,()=>{resetSettingsPending=false;SettingsRebuild("Restore defaults");});
            Detail("Restore defaults",SettingsDefaultsDescription()+"\n\nOnly this settings category will change. Party progress and shortcuts are unaffected.");return;
        }
        if(settingsTab==SettingsTab.Audio){
            var readout=SystemLabel(Rect("Volume readout",list,12,12,width-24,32),$"Master Volume: {Mathf.RoundToInt(AudioListener.volume*100)}%",19);
            var authored=SystemControl("Master Volume");
            var go=authored&&authored.Slider?authored.Slider.gameObject:DefaultControls.CreateSlider(new DefaultControls.Resources());go.name="Master Volume";if(!authored)go.transform.SetParent(list,false);
            if(!authored){var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(12,-56);rect.sizeDelta=new Vector2(width-24,28);}
            var slider=go.GetComponent<Slider>();slider.minValue=0;slider.maxValue=100;slider.wholeNumbers=true;slider.SetValueWithoutNotify(Mathf.Round(AudioListener.volume*100));
            slider.onValueChanged.AddListener(value=>{AudioListener.volume=value/100;readout.text=$"Master Volume: {value:0}%";PlayerPrefs.SetFloat("PlayerMenu.Volume",AudioListener.volume);PlayerPrefs.Save();});
            SystemButton(list,"Restore defaults",12,110,width-24,()=>{resetSettingsPending=true;SettingsRebuild("Cancel defaults");});
            Detail("Audio","Master volume adjusts all game audio.");
        }else if(settingsTab==SettingsTab.Graphics){
            var modes=new[]{FullScreenMode.Windowed,FullScreenMode.FullScreenWindow,FullScreenMode.ExclusiveFullScreen};
            var chosen=pendingFullscreen??Screen.fullScreenMode;
            SystemLabel(Rect("Mode label",list,12,12,width-24,24),"Display mode",17);
            var apply=SystemButton(list,"Apply display change",12,104,width-24,BeginDisplayChange);apply.interactable=chosen!=Screen.fullScreenMode;
            SettingsDropdown(list,"Display mode",48,width-24,new[]{"Windowed","Borderless","Fullscreen"},Mathf.Max(0,Array.IndexOf(modes,chosen)),value=>{pendingFullscreen=modes[value];apply.interactable=pendingFullscreen!=Screen.fullScreenMode;});
            Detail("Graphics","Windowed, borderless window or fullscreen. Unconfirmed display changes revert after 15 seconds.");
        }else if(settingsTab==SettingsTab.UI){
            SystemLabel(Rect("Icon label",list,12,12,width-24,24),"Input icons",17);
            SettingsDropdown(list,"Controller icons",44,width-24,new[]{"Auto","Xbox","PlayStation","Keyboard"},Manager.Input?(int)Manager.Input.ControllerIcons:0,value=>{
                if(Manager.Input)Manager.Input.ControllerIcons=(ControllerIconStyle)value;PlayerPrefs.SetInt("PlayerMenu.Icons",value);PlayerPrefs.Save();
            });
            SystemLabel(Rect("Order label",list,12,96,width-24,24),"Turn order display",17);
            SettingsDropdown(list,"Turn order",128,width-24,new[]{"Top","Right"},PlayerPrefs.GetInt(OrderTopKey,1)!=0?0:1,value=>RememberBool(OrderTopKey,value==0));
            SystemButton(list,"Restore defaults",12,188,width-24,()=>{resetSettingsPending=true;SettingsRebuild("Cancel defaults");});
            Detail("UI","Input icon appearance and turn order placement. Icon overrides do not change controls.");
        }else{
            var defense=Member.GetComponent<DefensePrototypeSettings>();bool assisted=PlayerPrefs.HasKey("PlayerMenu.Assisted")?PlayerPrefs.GetInt("PlayerMenu.Assisted")!=0:defense&&defense.Assisted;
            bool auto=PlayerPrefs.GetInt(AutoCameraKey,1)!=0;
            SystemButton(list,"Defense timing: "+(assisted?"Assisted":"Standard"),12,12,width-24,()=>{ApplyAssistance(!assisted);RememberBool("PlayerMenu.Assisted",!assisted);SettingsRebuild("Defense timing");});
            SystemButton(list,"Automatic battle camera: "+(auto?"On":"Off"),12,64,width-24,()=>{RememberBool(AutoCameraKey,!auto);SettingsRebuild("Automatic battle camera");});
            SystemButton(list,"Restore defaults",12,128,width-24,()=>{resetSettingsPending=true;SettingsRebuild("Cancel defaults");});
            Detail("Battle","Assisted timing widens dodge and parry windows. Automatic camera follows the acting combatant.");
        }
    }
    TMP_Dropdown SettingsDropdown(Transform parent,string name,float y,float width,string[] choices,int current,UnityEngine.Events.UnityAction<int> changed)
    {
        var authored=SystemControl(name);
        if(authored&&authored.Dropdown){
            var d=authored.Dropdown;d.name=name;d.ClearOptions();d.AddOptions(new System.Collections.Generic.List<string>(choices));d.SetValueWithoutNotify(current);d.onValueChanged.AddListener(changed);
            foreach(var markerArrow in d.GetComponentsInChildren<DropdownArrow>(true))markerArrow.Bind(d);return d;
        }
        var go=TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());go.name=name;go.transform.SetParent(parent,false);
        var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(12,-y);rect.sizeDelta=new Vector2(width,36);
        var dropdown=go.GetComponent<TMP_Dropdown>();dropdown.ClearOptions();dropdown.AddOptions(new System.Collections.Generic.List<string>(choices));dropdown.SetValueWithoutNotify(current);dropdown.onValueChanged.AddListener(changed);
        foreach(var text in go.GetComponentsInChildren<TMP_Text>(true)){text.font=TMP_Settings.defaultFontAsset;text.color=Color.white;text.fontSize=15;}
        foreach(var image in go.GetComponentsInChildren<Image>(true))image.color=new Color(.035f,.075f,.12f,1);
        var colors=dropdown.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.65f,.85f,1);colors.selectedColor=colors.highlightedColor;dropdown.colors=colors;
        var arrow=go.transform.Find("Arrow");
        if(arrow){arrow.GetComponent<Image>().enabled=false;var marker=new GameObject("Direction",typeof(RectTransform));marker.transform.SetParent(arrow,false);var markerRect=(RectTransform)marker.transform;markerRect.anchorMin=Vector2.zero;markerRect.anchorMax=Vector2.one;markerRect.offsetMin=markerRect.offsetMax=Vector2.zero;var graphic=marker.AddComponent<DropdownArrow>();graphic.Bind(dropdown);}
        foreach(var toggle in go.GetComponentsInChildren<Toggle>(true)){if(toggle.graphic)toggle.graphic.color=new Color(.3f,.8f,1);}
        return dropdown;
    }
    string SettingsDefaultsDescription()=>settingsTab==SettingsTab.Audio?"Master volume: 100%":settingsTab==SettingsTab.UI?"Controller icons: Auto\nTurn order: Top":"Defense timing: Standard\nAutomatic battle camera: On";
    void ResetSettingsCategory()
    {
        if(!resetSettingsPending)return;resetSettingsPending=false;
        if(settingsTab==SettingsTab.Audio){AudioListener.volume=1;PlayerPrefs.SetFloat("PlayerMenu.Volume",1);}
        else if(settingsTab==SettingsTab.UI){if(Manager.Input)Manager.Input.ControllerIcons=ControllerIconStyle.Auto;PlayerPrefs.SetInt("PlayerMenu.Icons",0);PlayerPrefs.SetInt(OrderTopKey,1);}
        else if(settingsTab==SettingsTab.Battle){ApplyAssistance(false);PlayerPrefs.SetInt("PlayerMenu.Assisted",0);PlayerPrefs.SetInt(AutoCameraKey,1);}
        PlayerPrefs.Save();SettingsRebuild("Restore defaults");
    }
    void RestoreSettingsSelection()
    {
        if(settingsSelection==null)return;
        foreach(var control in body.GetComponentsInChildren<Selectable>())if(control.IsInteractable()&&control.name.StartsWith(settingsSelection)){events.SetSelectedGameObject(control.gameObject);return;}
    }
    void BeginDisplayChange()
    {
        if(!pendingFullscreen.HasValue||pendingFullscreen==Screen.fullScreenMode)return;
        previousDisplayMode=Screen.fullScreenMode;requestedDisplayMode=pendingFullscreen.Value;
        displayPreview.Begin(false,true,Time.realtimeSinceStartup);
        if(!Application.isEditor)Screen.fullScreenMode=requestedDisplayMode;SettingsRebuild("Revert display settings");
    }
    void KeepDisplayChange()
    {
        if(!displayPreview.Active)return;
        if(displayPreview.Expired(Time.realtimeSinceStartup)){CancelDisplayChange();SettingsRebuild("Display mode");return;}
        PlayerPrefs.SetInt(DisplayModeKey,(int)requestedDisplayMode);PlayerPrefs.Save();displayPreview.End();pendingFullscreen=null;SettingsRebuild("Display mode");
    }
    void CancelDisplayChange()
    {if(displayPreview.Active){if(!Application.isEditor)Screen.fullScreenMode=previousDisplayMode;displayPreview.End();}pendingFullscreen=null;}
    void UpdateDisplayCountdown(){if(displayCountdown)displayCountdown.text=$"Keep this display mode?\nReverting in {Mathf.CeilToInt(Mathf.Max(0,displayPreview.Deadline-Time.realtimeSinceStartup))} seconds";}
    void TickDisplayPreview()
    {
        if(!displayPreview.Active)return;
        if(displayPreview.Expired(Time.realtimeSinceStartup)){CancelDisplayChange();if(open)SettingsRebuild("Display mode");}else UpdateDisplayCountdown();
    }
    bool BackFromSettings()
    {
        if(page!=Page.Settings && !(page==Page.System&&systemTab==SystemTab.Settings))return false;
        if(displayPreview.Active||pendingFullscreen.HasValue){CancelDisplayChange();SettingsRebuild("Display mode");return true;}
        if(resetSettingsPending){resetSettingsPending=false;SettingsRebuild("Restore defaults");return true;}
        return false;
    }
}
