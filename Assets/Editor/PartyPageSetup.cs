using System;
using System.IO;
using Game.GridPrototype;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PartyPageSetup
{
    const string Folder="Assets/Resources/UI/Party";
    static Color Ink=new Color(.88f,.94f,1),Accent=new Color(.3f,.8f,1);
    [MenuItem("Tools/Game/UI/Create missing Party templates")]
    public static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
        Directory.CreateDirectory(Folder);
        var card=Card();var slot=Slot();var stat=Stat();
        foreach(PartyPageKind kind in Enum.GetValues(typeof(PartyPageKind))){
            string path=Folder+"/"+kind+".prefab";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(!prefab){
                var root=Rect(kind.ToString(),null,0,0,510,456);var view=root.gameObject.AddComponent<PartyPageView>();
                try{
                    if(kind==PartyPageKind.Formation){
                        for(int group=0;group<2;group++){
                            var heading=Text(group==0?"Active heading":"Reserve heading",root,group==0?"ACTIVE   3 / 3":"RESERVES   3 / 3",14,0,group*188,510,26);heading.color=Accent;if(group==0)view.ActiveHeading=heading;else view.ReserveHeading=heading;
                            var row=Rect(group==0?"Active team":"Reserve team",root,0,group*188+30,510,154);StretchWidth(row);
                            var flow=row.gameObject.AddComponent<HorizontalLayoutGroup>();flow.spacing=8;flow.childControlWidth=true;flow.childControlHeight=true;flow.childForceExpandWidth=true;flow.childForceExpandHeight=true;
                            for(int i=0;i<3;i++){
                                var instance=(GameObject)PrefabUtility.InstantiatePrefab(card,row);instance.name="Member "+(group*3+i);
                                instance.GetComponent<PartyMemberCardView>().RosterIndex=group*3+i;
                                var element=instance.AddComponent<LayoutElement>();element.flexibleWidth=1;element.minWidth=80;
                            }
                        }
                    }else if(kind==PartyPageKind.Equipment){
                        view.Character=Rect("Equipped character",root,104,20,302,260).gameObject.AddComponent<RawImage>();view.Character.raycastTarget=false;
                        var cr=view.Character.rectTransform;cr.anchorMax=new Vector2(1,1);cr.offsetMax=new Vector2(-104,-20);
                        foreach(EquipmentSlot binding in Enum.GetValues(typeof(EquipmentSlot))){
                            var instance=(GameObject)PrefabUtility.InstantiatePrefab(slot,root);instance.name=binding.ToString();
                            var slotView=instance.GetComponent<PartyEquipmentSlotView>();slotView.Slot=binding;slotView.Caption.text=binding.ToString();
                            var r=(RectTransform)instance.transform;bool right=binding==EquipmentSlot.Weapon||binding==EquipmentSlot.Armor;
                            bool lower=binding==EquipmentSlot.Weapon||binding==EquipmentSlot.AccessoryTwo;
                            r.anchorMin=r.anchorMax=new Vector2(right?1:0,1);r.pivot=new Vector2(right?1:0,1);r.anchoredPosition=new Vector2(0,lower?-160:-40);
                        }
                        view.WeaponAccess=Text("Weapon access",root,"Weapon types: Shortsword, Staff",14,0,288,510,48);StretchWidth(view.WeaponAccess.rectTransform);
                    }else{
                        view.Points=Text("Allocation balance",root,"3 ATTRIBUTE POINTS AVAILABLE",18,0,0,510,34);StretchWidth(view.Points.rectTransform);
                        var rows=Rect("Attribute rows",root,0,42,510,322);StretchWidth(rows);
                        var flow=rows.gameObject.AddComponent<VerticalLayoutGroup>();flow.spacing=8;flow.childControlWidth=true;flow.childControlHeight=true;flow.childForceExpandWidth=true;flow.childForceExpandHeight=false;
                        foreach(CharacterStat binding in Enum.GetValues(typeof(CharacterStat))){
                            var instance=(GameObject)PrefabUtility.InstantiatePrefab(stat,rows);instance.name=binding.ToString();instance.GetComponent<PartyAttributeRowView>().Stat=binding;
                            var element=instance.AddComponent<LayoutElement>();element.preferredHeight=38;
                        }
                        view.Apply=Button("Apply allocation",root,"Apply allocation",0,408,360,38);view.Reset=Button("Reset preview",root,"Reset preview",368,408,142,38);
                        var apply=(RectTransform)view.Apply.transform;apply.anchorMin=new Vector2(0,0);apply.anchorMax=new Vector2(1,0);apply.offsetMin=new Vector2(0,10);apply.offsetMax=new Vector2(-150,48);
                        var reset=(RectTransform)view.Reset.transform;reset.anchorMin=reset.anchorMax=new Vector2(1,0);reset.pivot=new Vector2(1,0);reset.anchoredPosition=new Vector2(0,10);
                    }
                    prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,path);
                }finally{UnityEngine.Object.DestroyImmediate(root.gameObject);}
            }
            string assetPath=Folder+"/"+kind+".asset";
            if(!AssetDatabase.LoadAssetAtPath<PartyPageLayout>(assetPath)){var asset=ScriptableObject.CreateInstance<PartyPageLayout>();asset.Page=kind;asset.Prefab=prefab.GetComponent<PartyPageView>();AssetDatabase.CreateAsset(asset,assetPath);}
        }
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
    }
    static GameObject Card()
    {
        string path=Folder+"/MemberCard.prefab";var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(existing)return existing;
        var button=Button("Member card",null,"Member",0,0,164,154);var go=button.gameObject;var view=go.AddComponent<PartyMemberCardView>();
        try{
            view.Select=button;view.Name=button.GetComponentInChildren<TMP_Text>();view.Name.rectTransform.anchoredPosition=new Vector2(8,-68);view.Name.rectTransform.sizeDelta=new Vector2(148,22);StretchWidth(view.Name.rectTransform,8,8);view.Name.enableAutoSizing=true;view.Name.fontSizeMin=10;view.Name.fontSizeMax=16;
            view.Portrait=Rect("Portrait",go.transform,8,8,52,52).gameObject.AddComponent<RawImage>();view.Portrait.raycastTarget=false;
            view.Role=Text("Role",go.transform,"ACTIVE",10,68,10,88,24);StretchWidth(view.Role.rectTransform,68,8);view.Role.color=Accent;
            view.Level=Text("Level",go.transform,"Lv 10",13,68,36,88,22);StretchWidth(view.Level.rectTransform,68,8);
            view.Vitals=Text("Vitals",go.transform,"HP 120 / 150\nMP 18 / 24",12,8,94,148,42);StretchWidth(view.Vitals.rectTransform,8,8);
            var track=Rect("XP track",go.transform,8,143,148,4);StretchWidth(track,8,8);track.gameObject.AddComponent<Image>().color=new Color(.04f,.08f,.11f);
            var fill=Rect("XP fill",track,0,0,74,4);fill.anchorMin=Vector2.zero;fill.anchorMax=new Vector2(.5f,1);fill.offsetMin=fill.offsetMax=Vector2.zero;
            view.ExperienceFill=fill.gameObject.AddComponent<Image>();view.ExperienceFill.color=Accent;
            return PrefabUtility.SaveAsPrefabAsset(go,path);
        }finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    static GameObject Slot()
    {
        string path=Folder+"/EquipmentSlot.prefab";var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(existing)return existing;
        var root=Rect("Equipment slot",null,0,0,88,114);var view=root.gameObject.AddComponent<PartyEquipmentSlotView>();
        try{
            view.Select=Button("Select",root,"",0,0,88,88);var r=(RectTransform)view.Select.transform;r.anchorMax=new Vector2(1,1);r.offsetMax=new Vector2(0,0);
            view.Artwork=Rect("Artwork",view.Select.transform,6,6,76,76).gameObject.AddComponent<RawImage>();view.Artwork.raycastTarget=false;
            view.Empty=Text("Empty slot",view.Select.transform,"Empty",12,4,4,80,80);view.Empty.alignment=TextAlignmentOptions.Center;
            view.Caption=Text("Slot caption",root,"Weapon",12,0,92,88,22);view.Caption.alignment=TextAlignmentOptions.Center;
            return PrefabUtility.SaveAsPrefabAsset(root.gameObject,path);
        }finally{UnityEngine.Object.DestroyImmediate(root.gameObject);}
    }
    static GameObject Stat()
    {
        string path=Folder+"/AttributeRow.prefab";var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(existing)return existing;
        var root=Rect("Attribute row",null,0,0,510,38);var view=root.gameObject.AddComponent<PartyAttributeRowView>();
        try{
            view.Name=Text("Name",root,"Strength",16,8,6,330,32);StretchWidth(view.Name.rectTransform,8,180);
            view.Value=Text("Value",root,"10",16,340,6,60,32);Right(view.Value.rectTransform,170);
            view.Decrease=Button("Decrease",root,"−",402,0,42,38);Right((RectTransform)view.Decrease.transform,108);
            view.Increase=Button("Increase",root,"+",450,0,42,38);Right((RectTransform)view.Increase.transform,60);
            return PrefabUtility.SaveAsPrefabAsset(root.gameObject,path);
        }finally{UnityEngine.Object.DestroyImmediate(root.gameObject);}
    }
    static void Right(RectTransform r,float inset){r.anchorMin=r.anchorMax=Vector2.one;r.anchoredPosition=new Vector2(-inset,r.anchoredPosition.y);}
    static void StretchWidth(RectTransform r,float left=0,float right=0){r.anchorMax=new Vector2(1,1);r.offsetMin=new Vector2(left,r.offsetMin.y);r.offsetMax=new Vector2(-right,r.offsetMax.y);}
    static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();if(parent)r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    static TMP_Text Text(string name,Transform parent,string value,int size,float x,float y,float w,float h){var t=Rect(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.text=value;t.fontSize=size;t.color=Ink;t.raycastTarget=false;return t;}
    static Button Button(string name,Transform parent,string value,float x,float y,float w,float h){var r=Rect(name,parent,x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.08f,.17f,.25f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;Text("Label",r,value,16,12,6,w-24,h-10);return b;}
}
[CustomEditor(typeof(PartyPageLayout))]
public sealed class PartyPageLayoutEditor : Editor
{
    public override void OnInspectorGUI(){
        if(GUILayout.Button("Open live preview"))DetailsPanelPreviewWindow.Open((PartyPageLayout)target);
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Open the page prefab to arrange its bound widgets. Edit MemberCard, EquipmentSlot and AttributeRow prefabs for shared styling. Keep binding components and references; callbacks are connected automatically in the game.",MessageType.Info);
    }
}
