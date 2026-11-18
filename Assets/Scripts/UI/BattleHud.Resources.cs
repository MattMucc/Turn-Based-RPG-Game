using System.Collections.Generic;
using Game.GridPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class BattleHud
{
    sealed class ResourceCard
    {
        public RectTransform Rect, Health, Mana;
        public TMP_Text Name, HealthText, ManaText, Resources;
        public BattleStatusRow Statuses;
        public int Index;
    }
    readonly List<ResourceCard> resourceCards=new();
    TMP_Text targetResources;
    int lastControlled=-1;
    static string ResourceLine(GridUnit unit)=>$"MP {unit.MagicPoints} / {unit.MaxMagicPoints}    AP {unit.ActionPoints} / {GridUnit.MaximumActionPoints}    Move {unit.Movement} / {GridUnit.MaximumMovement}";

    RectTransform ResourceRect(string name,Transform parent,float y,float height)
    {
        var rect=Rect(name,parent);rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;
        rect.pivot=new Vector2(.5f,1);rect.offsetMin=new Vector2(10,-y-height);rect.offsetMax=new Vector2(-10,-y);return rect;
    }
    RectTransform ResourceBar(string name,Transform parent,float y,Color color,out TMP_Text label)
    {
        var track=ResourceRect(name,parent,y,14);track.gameObject.AddComponent<Image>().color=new Color(.02f,.03f,.05f);
        var fill=Rect("Fill",track);Stretch(fill);var image=fill.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;
        label=Text(track,11);return fill;
    }
    void BuildResourceCards()
    {
        resourceCards.Clear();lastControlled=-1;
        for(int i=0;i<battle.Combatants.Count;i++)if(battle.IsAlly(i))
        {
            var card=new ResourceCard{Index=i,Rect=Rect("Party resources",root)};
            card.Rect.anchorMin=card.Rect.anchorMax=new Vector2(1,0);card.Rect.pivot=new Vector2(1,0);
            card.Rect.gameObject.AddComponent<Image>().color=Dark;
            card.Name=Text(ResourceRect("Name and level",card.Rect,4,20),14);card.Name.alignment=TextAlignmentOptions.MidlineLeft;
            card.Health=ResourceBar("Health",card.Rect,27,new Color(.75f,.12f,.1f),out card.HealthText);
            card.Mana=ResourceBar("Mana",card.Rect,44,new Color(.2f,.4f,.9f),out card.ManaText);
            card.Resources=Text(ResourceRect("AP and movement",card.Rect,60,20),12);
            var status=ResourceRect("Statuses",card.Rect,88,30);
            card.Statuses=status.gameObject.AddComponent<BattleStatusRow>();card.Statuses.Initialize(root,font);
            resourceCards.Add(card);
        }
    }
    void DrawResourceCards()
    {
        if(battle.IsAlly(battle.CurrentActorIndex))lastControlled=battle.CurrentActorIndex;
        if(lastControlled<0&&resourceCards.Count>0)lastControlled=resourceCards[0].Index;
        int emphasized=battle.DefensePresentationActive?battle.DefenseDefenderIndex:lastControlled;
        float bottom=12;
        for(int n=resourceCards.Count-1;n>=0;n--)
        {
            var card=resourceCards[n];var unit=battle.Combatants[card.Index];var info=battle.Info(card.Index);
            bool expanded=card.Index==emphasized;
            card.Rect.anchoredPosition=new Vector2(-12,bottom);card.Rect.sizeDelta=new Vector2(expanded?340:280,expanded?124:114);bottom+=expanded?130:120;
            card.Rect.GetComponent<Image>().color=expanded?new Color(.07f,.19f,.25f,.97f):Dark;
            card.Name.text=$"{battle.DisplayName(card.Index)}  ·  Lv {(info?info.Level:1)}";
            card.Health.anchorMax=new Vector2(unit.MaxHealth>0?(float)unit.Health/unit.MaxHealth:0,1);
            card.Mana.anchorMax=new Vector2(unit.MaxMagicPoints>0?(float)unit.MagicPoints/unit.MaxMagicPoints:0,1);
            card.HealthText.text=$"HP {unit.Health} / {unit.MaxHealth}";
            card.ManaText.text=$"MP {unit.MagicPoints} / {unit.MaxMagicPoints}";
            card.Resources.text=$"AP {unit.ActionPoints} / {GridUnit.MaximumActionPoints}    Move {unit.Movement} / {GridUnit.MaximumMovement}";
            card.Statuses.Show(unit);
        }
    }
}
