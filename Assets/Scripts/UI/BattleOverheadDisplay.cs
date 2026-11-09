using System.Collections.Generic;
using Game.GridPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// World-space billboards are separate from actor hierarchies so they never alter body bounds.
public sealed class BattleOverheadDisplay : MonoBehaviour
{
    sealed class Label
    {
        public RectTransform Root, Fill, Shield;
        public TMP_Text Name, Health;
    }
    readonly List<Label> labels=new();
    InSceneGridBattle battle;
    TMP_FontAsset font;
    public void Bind(InSceneGridBattle source,TMP_FontAsset textFont){battle=source;font=textFont;}
    static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
    {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchoredPosition=position;r.sizeDelta=size;return r;}
    TMP_Text Text(RectTransform rect,int size)
    {
        var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSize=size;text.color=Color.white;text.alignment=TextAlignmentOptions.Center;
        text.raycastTarget=false;text.outlineColor=Color.black;text.outlineWidth=.22f;text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Ellipsis;return text;
    }
    Label Create()
    {
        var r=Rect("Combatant overhead",transform,Vector2.zero,new Vector2(200,80));
        var canvas=r.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=5;
        r.localScale=Vector3.one*(GridCameraController.OverheadWidth/200);
        var label=new Label{Root=r};
        label.Name=Text(Rect("Name and level",r,new Vector2(0,20),new Vector2(200,28)),18);
        label.Name.enableAutoSizing=true;label.Name.fontSizeMin=13;label.Name.fontSizeMax=18;
        var hp=Rect("Health",r,new Vector2(0,-8),new Vector2(184,14));hp.gameObject.AddComponent<Image>().color=new Color(.12f,.025f,.025f,.95f);
        label.Fill=Rect("Health fill",hp,Vector2.zero,Vector2.zero);label.Fill.anchorMin=Vector2.zero;label.Fill.anchorMax=Vector2.one;label.Fill.offsetMin=label.Fill.offsetMax=Vector2.zero;
        label.Fill.gameObject.AddComponent<Image>().color=new Color(.8f,.14f,.12f);
        label.Health=Text(Rect("Current / max",hp,Vector2.zero,new Vector2(184,18)),13);
        var shield=Rect("Barrier",r,new Vector2(0,3),new Vector2(184,5));shield.gameObject.AddComponent<Image>().color=new Color(.05f,.13f,.24f);
        label.Shield=Rect("Barrier fill",shield,Vector2.zero,Vector2.zero);label.Shield.anchorMin=Vector2.zero;label.Shield.anchorMax=Vector2.one;label.Shield.offsetMin=label.Shield.offsetMax=Vector2.zero;
        label.Shield.gameObject.AddComponent<Image>().color=new Color(.25f,.65f,1);
        foreach(var graphic in r.GetComponentsInChildren<Graphic>())graphic.raycastTarget=false;
        return label;
    }
    public static bool ShouldShow(InSceneGridBattle source,int index)
        => source && !source.EntryActive && !source.SignatureActive && source.Combatants[index].Health>0 && source.Actor(index).gameObject.activeInHierarchy
            && (!source.DefensePresentationActive || index==source.DefenseAttackerIndex || index==source.DefenseDefenderIndex);
    public void Render()
    {
        if(!battle || !battle.BattleCamera)return;
        for(int i=0;i<battle.Combatants.Count;i++)
        {
            if(i==labels.Count)labels.Add(Create());var label=labels[i];bool visible=ShouldShow(battle,i);
            label.Root.gameObject.SetActive(visible);if(!visible)continue;
            var actor=battle.Actor(i);var unit=battle.Combatants[i];var info=battle.Info(i);
            var bounds=GridCameraController.ActorBounds(actor);
            label.Root.position=new Vector3(bounds.center.x,bounds.max.y+GridCameraController.OverheadClearance+GridCameraController.OverheadHeight*.5f,bounds.center.z);
            label.Root.rotation=battle.BattleCamera.transform.rotation;
            if(battle.BattleCamera.WorldToViewportPoint(label.Root.position).z<=0){label.Root.gameObject.SetActive(false);continue;}
            label.Name.text=battle.DisplayName(i)+"  ·  Lv "+(info && info.Level>0?info.Level.ToString():"?");
            label.Health.text=$"{unit.Health} / {unit.MaxHealth}";
            label.Fill.anchorMax=new Vector2(Mathf.Clamp01(unit.Health/(float)Mathf.Max(1,unit.MaxHealth)),1);
            label.Shield.parent.gameObject.SetActive(unit.Barrier>0);label.Shield.anchorMax=new Vector2(unit.Barrier/(float)Mathf.Max(1,unit.BarrierMaximum),1);
        }
        for(int i=battle.Combatants.Count;i<labels.Count;i++)labels[i].Root.gameObject.SetActive(false);
    }
}
