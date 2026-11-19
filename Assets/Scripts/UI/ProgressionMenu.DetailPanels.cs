using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class ProgressionMenu
{
    [Header("Editable details panel layouts")]
    [Tooltip("Optional layout override. Leave empty to use the matching asset in Resources/UI/Details.")]
    [SerializeField] DetailsPanelLayout abilitiesDetailsLayout;
    [Tooltip("Optional layout override. Leave empty to use the matching asset in Resources/UI/Details.")]
    [SerializeField] DetailsPanelLayout shopDetailsLayout;
    [Tooltip("Optional layout override. Leave empty to use the matching asset in Resources/UI/Details.")]
    [SerializeField] DetailsPanelLayout blacksmithDetailsLayout;
    DetailsPanelLayout DetailsLayout(DetailPanelKind kind)
    {
        var selected=kind==DetailPanelKind.Abilities?abilitiesDetailsLayout:kind==DetailPanelKind.Shop?shopDetailsLayout:blacksmithDetailsLayout;
        return selected?selected:Resources.Load<DetailsPanelLayout>("UI/Details/"+kind);
    }
    enum DetailPanelKind { Abilities, Shop, Blacksmith }
    void ComposeDetails(RectTransform content,DetailPanelKind kind)
    {
        var rows=new List<DetailsPanelComposer.Row>();
        bool attackRun=false;
        foreach(RectTransform child in content.Cast<Transform>().ToArray()){
            string n=child.name;DetailSection binding=DetailSection.Description;
            if(kind==DetailPanelKind.Shop){
                if(n=="Base attacks"||n=="Mastery reward")attackRun=true;
                if(attackRun)binding=DetailSection.Attacks;
                else if(n=="Item name")binding=DetailSection.Name;
                else if(n=="Item artwork")binding=DetailSection.Artwork;
                else if(n=="Trade price"||n=="Owned count")binding=DetailSection.Price;
                else if(n=="Gear level")binding=DetailSection.Upgrade;
                else if(n=="Stat")binding=DetailSection.Stats;
                else if(n=="Preview character"||n=="Equipped restriction")binding=DetailSection.Availability;
                else if(n=="Weapon mastery")binding=DetailSection.Mastery;
                if(n=="Equipped restriction")binding=DetailSection.Availability;
            }else if(kind==DetailPanelKind.Abilities){
                if(n=="Ability name")binding=DetailSection.Name;
                else if(n=="Ability artwork")binding=DetailSection.Artwork;
                else if(n=="Ability availability")binding=DetailSection.Availability;
                else if(n.ToLowerInvariant().Contains("mastery")||n.Contains("milestone")||n=="Spell mastery XP")binding=DetailSection.Mastery;
                else if(n=="Ability description"||n.StartsWith("Pattern "))binding=DetailSection.Attacks;
            }else{
                if(!System.Enum.TryParse(n,out binding))binding=DetailSection.Description;
            }
            rows.Add(new DetailsPanelComposer.Row(binding,child));
        }
        DetailsPanelComposer.Compose(content,DetailsLayout(kind),rows);
    }
}
