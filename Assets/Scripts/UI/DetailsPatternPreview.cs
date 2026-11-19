using System.Linq;
using Game.GridPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class DetailsPatternPreview
{
    static RectTransform Rect(string name,Transform parent,float x,float y,float width,float height){
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);return rect;
    }
    static TMP_Text Label(RectTransform rect,string value,int size){
        var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.font=TMP_Settings.defaultFontAsset;label.text=value;label.fontSize=size;label.color=new Color(.88f,.94f,1);label.raycastTarget=false;return label;
    }
    static float ProgressionText(Transform parent,string name,string value,float y,float width,int size){
        var label=Label(Rect(name,parent,12,y,width,30),value,size);
        float height=Mathf.Max(24,label.GetPreferredValues(value,width,0).y+6);label.rectTransform.sizeDelta=new Vector2(width,height);return y+height;
    }
    public static float Draw(Transform parent,BattleActionOption action,float y,float width,float maximumTileSize=18)
    {
        if(action.Effect==ActionEffect.Dash)
            return ProgressionText(parent,"Pattern legend","Self only · Grants movement points; no attack area.",y,width,14)+8;
        int extent=Mathf.Clamp(action.Reach+action.Radius,2,8), side=extent*2+1;
        float size=Mathf.Min(maximumTileSize,(width-24)/side);
        var board=new GridBoard(side,side);var origin=new Vector2Int(extent,extent);var target=origin+Vector2Int.up*Mathf.Clamp(action.Reach,1,extent);
        if(!GridTargeting.CanTarget(board,origin,target,action)) {
            var candidates=new System.Collections.Generic.List<Vector2Int>();
            for(int x=0;x<side;x++)for(int z=0;z<side;z++)if(GridTargeting.CanTarget(board,origin,new Vector2Int(x,z),action))candidates.Add(new Vector2Int(x,z));
            if(candidates.Count>0)target=candidates.OrderBy(p=>(p-target).sqrMagnitude).First();
        }
        var footprint=GridTargeting.Footprint(board,origin,target,action);
        var frame=Rect("Pattern "+action.Name,parent,12,y,side*size,side*size);frame.gameObject.AddComponent<Image>().color=Color.black;
        for(int x=0;x<side;x++)for(int z=0;z<side;z++){
            var point=new Vector2Int(x,z);var tile=Rect("Tile",frame,x*size+1,(side-1-z)*size+1,size-2,size-2).gameObject.AddComponent<Image>();tile.raycastTarget=false;
            tile.color=footprint.Contains(point)?new Color(.9f,.2f,.2f):GridTargeting.CanTarget(board,origin,point,action)?new Color(.2f,.55f,1):new Color(.18f,.2f,.22f);
            if(point==origin)Label(Rect("User marker",tile.transform,0,0,size-2,size-2),"U",Mathf.Max(8,(int)size-5)).alignment=TMPro.TextAlignmentOptions.Center;
            if(point==target&&footprint.Count>0){var aim=Rect("Aim marker",tile.transform,(size-6)/2,(size-6)/2,4,4).gameObject.AddComponent<Image>();aim.color=Color.white;aim.raycastTarget=false;}
        }
        y+=side*size+6;
        return ProgressionText(parent,"Pattern legend","U: user · Blue: reach · Red: example hit area\nWhite dot: aim. Red on the user means self-hit is possible.\nFlat-ground preview; terrain and sight still apply."+(action.Pattern==BattleTargetPattern.Multiple?$"\nOne example selection shown; up to {action.MaxTargets} hit selections, including repeated targets.":"")+(footprint.Count==0?"\nNo legal aim within the displayed grid.":"")+(action.Reach+action.Radius>8?" Preview cropped to 8 tiles.":""),y,width,12)+8;
    }
}
