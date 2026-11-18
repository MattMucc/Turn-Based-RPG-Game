using UnityEngine;

public sealed partial class ProgressionMenu
{
    enum SystemTab { Settings, Saves }
    SystemTab systemTab=SystemTab.Saves;

    void SelectSystemTab(SystemTab next)
    {
        CancelDisplayChange();
        resetSettingsPending=false;
        settingsSelection=next==SystemTab.Saves?"Save / Load":settingsTab.ToString();
        savePendingSlot=-1;saveView=SaveView.Home;
        systemTab=next;
        Rebuild();
    }

    void SystemPage()
    {
        float width=Mathf.Max(300,body.rect.width),tabWidth=(width-24)/5;
        string[] labels={"Save / Load","Battle","Graphics","UI","Audio"};
        for(int i=0;i<labels.Length;i++){
            int index=i;
            var tab=Button(body,labels[i],i*(tabWidth+6),0,tabWidth,()=>{
                if(index>0)settingsTab=(SettingsTab)(index-1);
                SelectSystemTab(index==0?SystemTab.Saves:SystemTab.Settings);
            });
            var label=tab.GetComponentInChildren<TMPro.TMP_Text>();label.fontSize=13;label.enableAutoSizing=true;label.fontSizeMin=10;label.fontSizeMax=13;
            if(i==0?systemTab==SystemTab.Saves:systemTab==SystemTab.Settings&&(int)settingsTab==i-1)tab.image.color=new Color(.16f,.36f,.44f);
        }

        // Reuse the existing transactions inside a local workspace, below the System tabs.
        var outer=body;
        body=Rect("System workspace",outer,0,54,width,Mathf.Max(230,outer.rect.height-54));
        try { if(systemTab==SystemTab.Settings)Settings();else Saves(); }
        finally { body=outer; }
    }
}
