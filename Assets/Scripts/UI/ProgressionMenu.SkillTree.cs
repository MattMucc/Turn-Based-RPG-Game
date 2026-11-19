using System.Collections.Generic;
using System.Linq;
using Game.GridPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class ProgressionMenu
{
    MenuSkillNode selectedSkillNode;
    bool prototypeSkillTree;
    readonly List<(MenuSkillNode node, Button button, SkillTreeNodeGraphic graphic)> skillCards=new();
    List<MenuSkillNode> treeNodes=new();
    bool SkillLearned(MenuSkillNode node)
    {
        if(prototypeSkillTree)return false;
        if(Member.GetComponent<CharacterMenuData>()?.DebtTree==true)return node.Starting||Member.KnowsNode(node.Id);
        var loadout=Loadout();
        return !node.Passive && !string.IsNullOrWhiteSpace(node.ActionName) && loadout
            && loadout.Options.Any(o=>o!=null&&o.Category==BattleActionCategory.Skill&&o.Name==node.ActionName);
    }
    MenuSkillNode Prerequisite(string id) => string.IsNullOrWhiteSpace(id)||treeNodes.Count(n=>n.Id==id)!=1?null:treeNodes.Find(n=>n.Id==id);
    bool SkillRequirementsMet(MenuSkillNode node) => !node.Placeholder && Member.Level>=Mathf.Max(1,node.RequiredLevel)
        && (node.Prerequisites??new List<string>()).All(id=>{var prerequisite=Prerequisite(id);return prerequisite!=null&&SkillLearned(prerequisite);});
    void SkillTree()
    {
        skillCards.Clear();growthPositions.Clear();float width=Mathf.Max(300,body.rect.width);
        var data=Member.GetComponent<CharacterMenuData>();
        treeNodes=data&&data.SkillTree!=null?data.SkillTree.Where(n=>n!=null).ToList():new List<MenuSkillNode>();
        prototypeSkillTree=treeNodes.Count==0;
        if(prototypeSkillTree){treeNodes=GrowthLayout&&GrowthLayout.Arrangement?GrowthLayout.Arrangement.Examples():PrototypeGrowthNodes();if(treeNodes.Count==0)treeNodes=PrototypeGrowthNodes();}
        if(!treeNodes.Contains(selectedSkillNode))selectedSkillNode=treeNodes.FirstOrDefault(n=>n.Id==selectedSkillNode?.Id)??treeNodes.FirstOrDefault(n=>n.Prerequisites==null||n.Prerequisites.Count==0)??treeNodes[0];
        if(!BuildModularGrowth()){
        Label(Rect("Tree title",body,0,0,width,34),DisplayName(Member)+" · SKILL TREE",20);
        Label(Rect("Tree legend",body,0,36,width,46),"Circle: Skill    Diamond: Passive\nGold: learned    Gray: locked / placeholder    Blue: selected",13);
        Vector2 minimum=new Vector2(treeNodes.Min(n=>n.Position.x),treeNodes.Min(n=>n.Position.y));
        float treeWidth=Mathf.Max(width-8,treeNodes.Max(n=>n.Position.x)-minimum.x+160);
        float treeHeight=Mathf.Max(body.rect.height-92,treeNodes.Max(n=>n.Position.y)-minimum.y+150);
        var tree=List(body,"Skill tree",0,92,width,Mathf.Max(180,body.rect.height-92),treeHeight);
        tree.sizeDelta=new Vector2(treeWidth,tree.sizeDelta.y);
        var scroll=tree.parent.GetComponent<ScrollRect>();scroll.horizontal=true;
        float horizontalPadding=(treeWidth-(treeNodes.Max(n=>n.Position.x)-minimum.x))/2;
        // Authored Y is progression height: larger values render farther up the tree.
        Vector2 Center(MenuSkillNode n)=>new Vector2(n.Position.x-minimum.x+horizontalPadding,treeHeight-96-(n.Position.y-minimum.y));
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
            var captured=node;var point=Center(node);bool learned=!prototypeSkillTree&&SkillLearned(node),met=!prototypeSkillTree&&SkillRequirementsMet(node);
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
        }
        // Explicit directional neighbors include off-screen nodes; selecting one scrolls it into view.
        foreach(var card in skillCards){
            var nav=card.button.navigation;nav.mode=Navigation.Mode.Explicit;
            nav.selectOnLeft=SkillNeighbor(card.node,Vector2.left);nav.selectOnRight=SkillNeighbor(card.node,Vector2.right);
            nav.selectOnUp=SkillNeighbor(card.node,Vector2.up);nav.selectOnDown=SkillNeighbor(card.node,Vector2.down);
            if(!nav.selectOnLeft)nav.selectOnLeft=shell.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name=="Growth");
            card.button.navigation=nav;
        }
        ShowSkillNode(selectedSkillNode);
    }
    // Presentation-only examples. Never attach these records to a character or save them.
    static List<MenuSkillNode> PrototypeGrowthNodes() => new()
    {
        new MenuSkillNode{Id="preview-start",Name="Starting node",Position=new Vector2(160,0),Description="The starting point of this character's future Skill Tree."},
        new MenuSkillNode{Id="preview-left",Name="Branch A",Position=new Vector2(80,150),Prerequisites=new(){"preview-start"},Description="Placeholder branch. Its Skills and requirements are undecided."},
        new MenuSkillNode{Id="preview-right",Name="Branch B",Position=new Vector2(240,150),Prerequisites=new(){"preview-start"},Description="Placeholder branch. Its Skills and requirements are undecided."},
        new MenuSkillNode{Id="preview-left-top",Name="Future Skill",Position=new Vector2(0,300),Prerequisites=new(){"preview-left"},Description="Example of a later Skill node; no ability is assigned."},
        new MenuSkillNode{Id="preview-passive",Name="Future passive",Passive=true,Position=new Vector2(160,300),Prerequisites=new(){"preview-left","preview-right"},Description="Example passive node shape; no passive effect is assigned."},
        new MenuSkillNode{Id="preview-right-top",Name="Future Skill",Position=new Vector2(320,300),Prerequisites=new(){"preview-right"},Description="Example of a later Skill node; no ability is assigned."}
    };
    Button SkillNeighbor(MenuSkillNode origin,Vector2 direction)
    {
        Button result=null;float best=float.PositiveInfinity;
        var candidates=skillCards.Where(c=>Vector2.Dot(GrowthPosition(c.node)-GrowthPosition(origin),direction)>.01f).ToList();
        if(Mathf.Abs(direction.y)>.5f){
            var connected=candidates.Where(c=>direction.y>0
                ? (c.node.Prerequisites?.Contains(origin.Id)??false)
                : (origin.Prerequisites?.Contains(c.node.Id)??false)).ToList();
            if(connected.Count>0)candidates=connected;
        }
        foreach(var card in candidates){var delta=GrowthPosition(card.node)-GrowthPosition(origin);float along=Vector2.Dot(delta,direction);if(along<=.01f)continue;
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
        ClearGrowthControls();
        selectedSkillNode=node;
        foreach(var card in skillCards){card.graphic.Selected=card.node==node;card.graphic.SetVerticesDirty();}
        if(prototypeSkillTree){
            Detail(node.Name,"Layout prototype\n\n"+node.Description+"\n\nSkill: Not designed\nRequired level: Not set\nSkill Point cost: Not set\n\nUnlocking is unavailable. These nodes do not grant abilities, spend points or change this character.");
            return;
        }
        if(ShowDebtGrowth(node))return;
        bool learned=!prototypeSkillTree&&SkillLearned(node),met=!prototypeSkillTree&&SkillRequirementsMet(node);
        string state=learned?"Learned":met?"Level and prerequisites met":"Locked";
        string text=$"{(node.Passive?"Passive":"Active Skill")} · {state}\n\n{node.Description}\n\nREQUIREMENTS\nCharacter level: {Member.Level} / {Mathf.Max(1,node.RequiredLevel)}\nSkill Point cost: {Mathf.Max(0,node.SkillPointCost)}\n\nPREREQUISITES";
        var prerequisites=node.Prerequisites??new List<string>();
        if(prerequisites.Count==0)text+="\nNone";
        foreach(var id in prerequisites.Distinct()){
            var parent=Prerequisite(id);text+="\n"+(parent==null?"Unavailable prerequisite":$"{parent.Name} · {(SkillLearned(parent)?"Learned":"Not learned")}");
        }
        text+=learned?"\n\nThis Skill is already in this character's combat Skills.":"\n\nLearning is not available yet. Skill Point balances and learning rules will be added with the character kits.";
        if(node.Passive)text+="\nPassive learning and effects are not implemented yet.";
        Detail(node.Name,text,node.Icon);
    }
}
