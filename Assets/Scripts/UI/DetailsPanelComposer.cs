using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class DetailsPanelComposer
{
    public sealed class Row
    {
        public DetailSection Binding;
        public RectTransform View;
        public Row(DetailSection binding,RectTransform view){Binding=binding;View=view;}
    }
    // Receives already-bound live data views; rearranges presentation without changing gameplay.
    public static void Compose(RectTransform content,DetailsPanelLayout layout,IEnumerable<Row> source)
    {
        if(!layout)return;
        var rows=source.Where(r=>r.View).ToList();
        foreach(var row in rows)row.View.gameObject.SetActive(false);
        var used=new HashSet<DetailSection>();
        float y=layout.Padding.top,width=content.rect.width-layout.Padding.horizontal;
        foreach(var section in layout.Sections){
            if(section==null)continue;
            if(section.Binding!=DetailSection.Custom&&!used.Add(section.Binding))continue;
            var selected=rows.Where(r=>r.Binding==section.Binding).ToList();
            if(!section.Visible||!section.Prefab||(selected.Count==0&&string.IsNullOrWhiteSpace(section.CustomText)))continue;
            var block=Object.Instantiate(section.Prefab,content,false);
            block.name="Section "+section.Binding;
            var root=(RectTransform)block.transform;
            root.anchorMin=root.anchorMax=root.pivot=new Vector2(0,1);root.anchoredPosition=new Vector2(layout.Padding.left,-y);root.sizeDelta=new Vector2(width,1);
            var host=block.Content?block.Content:root;
            if(host!=root){host.anchorMin=new Vector2(0,0);host.anchorMax=Vector2.one;host.offsetMin=host.offsetMax=Vector2.zero;}
            if(block.TextTemplate)block.TextTemplate.gameObject.SetActive(false);
            float innerWidth=width-block.Padding.horizontal,at=block.Padding.top;
            if(section.Binding==DetailSection.Custom&&block.TextTemplate){
                var label=Object.Instantiate(block.TextTemplate,host,false);label.gameObject.SetActive(true);label.text=section.CustomText;
                Place(label.rectTransform,block.Padding.left,at,innerWidth,Mathf.Max(24,label.GetPreferredValues(label.text,innerWidth,0).y));
                at+=label.rectTransform.rect.height+block.RowSpacing;
            }
            foreach(var row in selected){
                var view=row.View;var text=view.GetComponent<TMP_Text>();
                if(text&&block.TextTemplate){
                    var label=Object.Instantiate(block.TextTemplate,host,false);label.name=view.name;label.gameObject.SetActive(true);label.text=text.text;
                    float h=Mathf.Max(24,label.GetPreferredValues(label.text,innerWidth,0).y+4);
                    Place(label.rectTransform,block.Padding.left,at,innerWidth,h);at+=h+block.RowSpacing;
                    continue;
                }
                view.SetParent(host,false);view.gameObject.SetActive(true);
                float height=Mathf.Max(1,view.rect.height),rowWidth=innerWidth;
                if(view.GetComponent<RawImage>()){height=Mathf.Max(16,block.ArtworkSize);rowWidth=Mathf.Min(innerWidth,height);}
                else if(view.name.StartsWith("Pattern ")&&view.GetComponent<Image>()){
                    // Preserve a square board and its authored tile geometry; fit narrow panels uniformly.
                    float scale=Mathf.Min(1,innerWidth/Mathf.Max(1,view.rect.width));rowWidth=view.rect.width;
                    Place(view,block.Padding.left,at,rowWidth,height);view.localScale=Vector3.one*scale;at+=height*scale+block.RowSpacing;continue;
                }
                Place(view,block.Padding.left,at,rowWidth,height);at+=height+block.RowSpacing;
            }
            float total=Mathf.Max(1,at-block.RowSpacing+block.Padding.bottom);
            root.sizeDelta=new Vector2(width,total);y+=total+layout.Spacing;
        }
        // Remove hidden source rows, including data omitted by the layout.
        foreach(var row in rows)if(row.View&&row.View.parent==content){
            if(Application.isPlaying)Object.Destroy(row.View.gameObject);else Object.DestroyImmediate(row.View.gameObject);
        }
        content.sizeDelta=new Vector2(content.sizeDelta.x,Mathf.Max(((RectTransform)content.parent).rect.height,y+layout.Padding.bottom));
    }
    static void Place(RectTransform rect,float x,float y,float w,float h){
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);
    }
}
