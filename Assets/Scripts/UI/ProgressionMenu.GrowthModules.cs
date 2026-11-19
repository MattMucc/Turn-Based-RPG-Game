using System.Collections.Generic;
using UnityEngine;
public sealed partial class ProgressionMenu {
    [SerializeField] GrowthPageLayout growthLayout;
    GrowthPageLayout GrowthLayout=>growthLayout?growthLayout:Resources.Load<GrowthPageLayout>("UI/Growth/Growth");
    readonly Dictionary<Game.GridPrototype.MenuSkillNode,Vector2> growthPositions=new();
    bool BuildModularGrowth(){
        var layout=GrowthLayout;if(!layout||!layout.Workspace)return false;
        var view=InventoryModularUI.Fill(layout.Workspace,body);view.Title.text=DisplayName(Member)+" · SKILL TREE";
        Canvas.ForceUpdateCanvases();
        foreach(var card in GrowthModularUI.Build(view,layout,treeNodes,SkillLearned,n=>!prototypeSkillTree&&SkillRequirementsMet(n))){
            var node=card.Node;var button=card.View.Select;buttons.Add(button);growthPositions[node]=card.Position;
            button.onClick.AddListener(()=>{events.SetSelectedGameObject(button.gameObject);ShowSkillNode(node);});
            var focus=button.GetComponent<PlayerMenuFocus>();if(!focus)focus=button.gameObject.AddComponent<PlayerMenuFocus>();focus.Preview=()=>ShowSkillNode(node);
            skillCards.Add((node,button,card.View.Frame));
        }
        return true;
    }
    Vector2 GrowthPosition(Game.GridPrototype.MenuSkillNode node)=>growthPositions.TryGetValue(node,out var point)?point:node.Position;
}
