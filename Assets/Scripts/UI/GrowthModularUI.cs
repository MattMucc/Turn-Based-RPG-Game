using System;
using System.Collections.Generic;
using System.Linq;
using Game.GridPrototype;
using UnityEngine;
using UnityEngine.UI;
public static class GrowthModularUI {
    public sealed class Card {public MenuSkillNode Node;public GrowthNodeView View;public Vector2 Position;}
    public static List<Card> Build(GrowthWorkspaceView workspace,GrowthPageLayout layout,List<MenuSkillNode> nodes,Func<MenuSkillNode,bool> learned,Func<MenuSkillNode,bool> available){
        var cards=new List<Card>();if(nodes.Count==0)return cards;
        var positions=layout.Arrangement?layout.Arrangement.Examples():new List<MenuSkillNode>();
        Vector2 Position(MenuSkillNode n){var matches=positions.Where(p=>p.Id==n.Id).ToList();return matches.Count==1?matches[0].Position:n.Position;}
        float minX=nodes.Min(n=>Position(n).x),minY=nodes.Min(n=>Position(n).y);
        float spanX=nodes.Max(n=>Position(n).x)-minX,spanY=nodes.Max(n=>Position(n).y)-minY;
        var tree=workspace.Tree.content;var viewport=workspace.Tree.viewport;
        float width=Mathf.Max(viewport.rect.width,spanX+layout.HorizontalPadding*2),height=Mathf.Max(viewport.rect.height,spanY+layout.TopPadding+layout.BottomPadding);
        tree.anchorMin=tree.anchorMax=tree.pivot=new Vector2(0,1);tree.sizeDelta=new Vector2(width,height);
        Vector2 Center(MenuSkillNode n)=>new Vector2(Position(n).x-minX+(width-spanX)/2,-height+layout.BottomPadding+Position(n).y-minY);
        foreach(var node in nodes)foreach(var id in (node.Prerequisites??new List<string>()).Distinct()){
            var parents=nodes.Where(n=>n.Id==id).ToList();if(parents.Count!=1||parents[0]==node)continue;
            var parent=parents[0];var from=Center(parent);var direction=Center(node)-from;float length=direction.magnitude;
            if(length<=layout.ConnectionInset*2)continue;
            var line=UnityEngine.Object.Instantiate(layout.Connection,tree,false);line.name="Connection";line.raycastTarget=false;
            var rect=line.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,.5f);
            rect.anchoredPosition=from+direction.normalized*layout.ConnectionInset;rect.sizeDelta=new Vector2(length-layout.ConnectionInset*2,layout.Connection.rectTransform.sizeDelta.y);
            rect.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            if(learned(parent))line.color=layout.LearnedConnection;
        }
        foreach(var node in nodes){
            var view=UnityEngine.Object.Instantiate(layout.Node,tree,false);view.name="Skill node "+node.Id;
            var rect=(RectTransform)view.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(.5f,1);
            rect.anchoredPosition=Center(node)+new Vector2(0,view.Frame.rectTransform.rect.height/2);
            bool known=learned(node);view.Name.text=node.Name;view.Type.text=node.Passive?"P":"S";view.Type.gameObject.SetActive(!node.Icon);view.Icon.texture=node.Icon;view.Icon.gameObject.SetActive(node.Icon);
            view.Frame.Passive=node.Passive;view.Frame.color=known?layout.LearnedFill:layout.LockedFill;view.Frame.Border=known?layout.LearnedBorder:available(node)?layout.AvailableBorder:layout.LockedBorder;
            cards.Add(new Card{Node=node,View=view,Position=Position(node)});
        }
        return cards;
    }
}
