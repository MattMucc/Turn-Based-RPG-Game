using System.Collections.Generic;
using Game.GridPrototype;
using UnityEngine;
public sealed class GrowthTreeArrangement:MonoBehaviour {
    public List<MenuSkillNode> Examples(){
        var result=new List<MenuSkillNode>();
        foreach(var marker in GetComponentsInChildren<GrowthNodePlacement>(true)){
            var node=JsonUtility.FromJson<MenuSkillNode>(JsonUtility.ToJson(marker.Example));
            node.Position=transform.InverseTransformPoint(marker.transform.position);result.Add(node);
        }
        return result;
    }
}
