using UnityEngine;

public sealed partial class ProgressionMenu
{
    enum SystemTab { Settings, Saves }
    SystemTab systemTab;

    void SelectSystemTab(SystemTab next)
    {
        CancelDisplayChange();
        resetSettingsPending=false;
        settingsSelection=null;
        savePendingSlot=-1;saveView=SaveView.Home;
        systemTab=next;
        Rebuild();
    }

    void SystemPage()
    {
        float width=Mathf.Max(300,body.rect.width), tabWidth=(width-8)/2;
        var settings=Button(body,"Settings",0,0,tabWidth,()=>SelectSystemTab(SystemTab.Settings));
        var saves=Button(body,"Save / Load",tabWidth+8,0,tabWidth,()=>SelectSystemTab(SystemTab.Saves));
        (systemTab==SystemTab.Settings?settings:saves).image.color=new Color(.16f,.36f,.44f);

        // Reuse the existing transactions inside a local workspace, below the System tabs.
        var outer=body;
        body=Rect("System workspace",outer,0,54,width,Mathf.Max(230,outer.rect.height-54));
        try { if(systemTab==SystemTab.Settings)Settings();else Saves(); }
        finally { body=outer; }
    }
}
