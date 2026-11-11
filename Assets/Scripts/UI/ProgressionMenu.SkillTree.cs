using System.Collections.Generic;
using System.Linq;
using Game.GridPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class ProgressionMenu
{
    MenuSkillNode selectedSkillNode;
    readonly List<(MenuSkillNode node, Button button, SkillTreeNodeGraphic graphic)> skillCards=new();
    List<MenuSkillNode> treeNodes=new();
    bool SkillLearned(MenuSkillNode node)
    {
        var loadout=Loadout();
        return !node.Passive && !string.IsNullOrWhiteSpace(node.ActionName) && loadout
            && loadout.Options.Any(o=>o!=null&&o.Category==BattleActionCategory.Skill&&o.Name==node.ActionName);
    }
    MenuSkillNode Prerequisite(string id) => string.IsNullOrWhiteSpace(id)||treeNodes.Count(n=>n.Id==id)!=1?null:treeNodes.Find(n=>n.Id==id);
    bool SkillRequirementsMet(MenuSkillNode node) => Member.Level>=Mathf.Max(1,node.RequiredLevel)
        && (node.Prerequisites??new List<string>()).All(id=>{var prerequisite=Prerequisite(id);return prerequisite!=null&&SkillLearned(prerequisite);});
    void SkillTree()
    {
        skillCards.Clear();float width=Mathf.Max(300,body.rect.width);
        Label(Rect("Tree title",body,0,0,width,34),DisplayName(Member)+" · SKILLS",20);
        Label(Rect("Tree legend",body,0,36,width,46),"Circle: Skill    Diamond: Passive\nGold: learned    Gray: locked    Blue outline: selected",13);
        var data=Member.GetComponent<CharacterMenuData>();
        treeNodes=data&&data.SkillTree!=null?data.SkillTree.Where(n=>n!=null).ToList():new List<MenuSkillNode>();
        if(treeNodes.Count==0){
            selectedSkillNode=null;
            Label(Rect("Empty skill tree",body,16,112,width-32,120),"This character's Skill Tree is not available yet.",18);
            Detail("Character Skill Tree","Unique Skills will use Training Points to learn. These are separate from the SP spent using Skills in combat.\n\nCharacter kits and their learning rules are still being defined.");return;
        }
        if(!treeNodes.Contains(selectedSkillNode))selectedSkillNode=treeNodes[0];
        Vector2 minimum=new Vector2(treeNodes.Min(n=>n.Position.x),treeNodes.Min(n=>n.Position.y));
        float treeWidth=Mathf.Max(width-8,treeNodes.Max(n=>n.Position.x)-minimum.x+160);
        float treeHeight=Mathf.Max(180,treeNodes.Max(n=>n.Position.y)-minimum.y+150);
        var tree=List(body,"Skill tree",0,92,width,Mathf.Max(180,body.rect.height-92),treeHeight);
        tree.sizeDelta=new Vector2(treeWidth,tree.sizeDelta.y);
        var scroll=tree.parent.GetComponent<ScrollRect>();scroll.horizontal=true;
        Vector2 Center(MenuSkillNode n)=>n.Position-minimum+new Vector2(80,44);
        foreach(var node in treeNodes)foreach(var id in (node.Prerequisites??new List<string>()).Distinct()) {
            var parent=Prerequisite(id);if(parent==null||parent==node)continue;
            var from=Center(parent);var to=Center(node);var direction=to-from;float length=direction.magnitude;
            if(length<=56)continue;
            from+=direction.normalized*28;
            var line=Rect("Connection",tree,from.x,from.y,length-56,2);line.pivot=new Vector2(0,.5f);
            line.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            var image=line.gameObject.AddComponent<Image>();image.color=SkillLearned(parent)?new Color(.65f,.51f,.25f):new Color(.25f,.31f,.37f);image.raycastTarget=false;
        }
        foreach(var node in treeNodes) {
            var captured=node;var point=Center(node);bool learned=SkillLearned(node),met=SkillRequirementsMet(node);
            var button=Button(tree,"Skill node "+node.Id,point.x-58,point.y-28,116,()=>{events.SetSelectedGameObject(skillCards.Find(c=>c.node==captured).button.gameObject);ShowSkillNode(captured);},()=>ShowSkillNode(captured),96);
            button.image.color=Color.clear;
            var shapeRect=Rect("Node frame",button.transform,30,0,56,56);
            var shape=shapeRect.gameObject.AddComponent<SkillTreeNodeGraphic>();shape.Passive=node.Passive;
            shape.color=learned?new Color(.25f,.2f,.08f):new Color(.07f,.12f,.18f);
            shape.Border=learned?new Color(.95f,.74f,.3f):met?new Color(.5f,.65f,.7f):new Color(.3f,.36f,.42f);
            button.targetGraphic=shape;
            if(node.Icon){var image=Rect("Skill icon",shapeRect,14,14,28,28).gameObject.AddComponent<RawImage>();image.texture=node.Icon;image.raycastTarget=false;}
            else Label(Rect("Node type",shapeRect,0,0,56,56),node.Passive?"P":"S",18).alignment=TextAlignmentOptions.Center;
            var text=button.transform.Find("Label").GetComponent<TMP_Text>();text.rectTransform.anchoredPosition=new Vector2(2,-60);text.rectTransform.sizeDelta=new Vector2(112,34);
            text.text=node.Name; text.fontSize=13;text.alignment=TextAlignmentOptions.Top;text.enableAutoSizing=true;text.fontSizeMin=10;text.fontSizeMax=13;
            skillCards.Add((node,button,shape));
        }
        // Explicit directional neighbors include off-screen nodes; selecting one scrolls it into view.
        foreach(var card in skillCards){
            var nav=card.button.navigation;nav.mode=Navigation.Mode.Explicit;
            nav.selectOnLeft=SkillNeighbor(card.node,Vector2.left);nav.selectOnRight=SkillNeighbor(card.node,Vector2.right);
            nav.selectOnUp=SkillNeighbor(card.node,Vector2.down);nav.selectOnDown=SkillNeighbor(card.node,Vector2.up);
            if(!nav.selectOnLeft)nav.selectOnLeft=shell.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name=="Skill Tree");
            card.button.navigation=nav;
        }
        ShowSkillNode(selectedSkillNode);
    }
    Button SkillNeighbor(MenuSkillNode origin,Vector2 direction)
    {
        Button result=null;float best=float.PositiveInfinity;
        foreach(var card in skillCards){var delta=card.node.Position-origin.Position;float along=Vector2.Dot(delta,direction);if(along<=.01f)continue;
            float perpendicular=Mathf.Abs(delta.x*direction.y-delta.y*direction.x);float score=along+perpendicular*3;
            if(score<best){best=score;result=card.button;}}
        return result;
    }
    void RestoreSkillSelection()
    {
        var selected=skillCards.Find(c=>c.node==selectedSkillNode);
        if(selected.button)events.SetSelectedGameObject(selected.button.gameObject);
    }
    void ShowSkillNode(MenuSkillNode node)
    {
        selectedSkillNode=node;
        foreach(var card in skillCards){card.graphic.Selected=card.node==node;card.graphic.SetVerticesDirty();}
        bool learned=SkillLearned(node),met=SkillRequirementsMet(node);
        string state=learned?"Learned":met?"Level and prerequisites met":"Locked";
        string text=$"{(node.Passive?"Passive":"Active Skill")} · {state}\n\n{node.Description}\n\nREQUIREMENTS\nCharacter level: {Member.Level} / {Mathf.Max(1,node.RequiredLevel)}\nTraining Point cost: {Mathf.Max(0,node.TrainingPointCost)}\n\nPREREQUISITES";
        var prerequisites=node.Prerequisites??new List<string>();
        if(prerequisites.Count==0)text+="\nNone";
        foreach(var id in prerequisites.Distinct()){
            var parent=Prerequisite(id);text+="\n"+(parent==null?"Unavailable prerequisite":$"{parent.Name} · {(SkillLearned(parent)?"Learned":"Not learned")}");
        }
        text+=learned?"\n\nThis Skill is already in this character's combat Skills.":"\n\nLearning is not available yet. Training Point balances and learning rules will be added with the character kits.";
        if(node.Passive)text+="\nPassive learning and effects are not implemented yet.";
        Detail(node.Name,text,node.Icon);
    }
}
