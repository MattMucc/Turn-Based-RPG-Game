using System.Linq;
using Game.GridPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(BoxCollider))]
public sealed class Merchant : MonoBehaviour
{
    public string DisplayName="Merchant";
    [System.Serializable] public sealed class Offer {
        public EquipmentDefinition Equipment;
        public BattleActionOption Item;
        [Min(1)] public int BuyPrice=50;
        [Min(0)] public int SellPrice=10;
        public string Name=>Equipment?Equipment.DisplayName:Item?.Name;
    }
    [Tooltip("Unlimited stock. Equipment must also exist in GameManager Equipment Catalog for save/load support. Prices are per item.")]
    public System.Collections.Generic.List<Offer> Offers=new();
    public bool Trade(GameManager manager,Offer offer,OwnedEquipment copy,bool selling,int quotedPrice,out string message) {
        message="Trade unavailable.";
        if(!CanServe(manager)||(BattleManager.Instance&&BattleManager.Instance.InBattle)||!Offers.Contains(offer))return false;
        int price=selling?offer.SellPrice:offer.BuyPrice;
        if(price<0||(!selling&&price==0)||price!=quotedPrice){message="Price changed. Review again.";return false;}
        if(selling){
            if((long)manager.Equipment.Coins+price>int.MaxValue)return false;
            if(offer.Equipment){
                if(copy==null||!manager.Equipment.Owned.Contains(copy)||copy.Definition!=offer.Equipment||manager.EquipmentOwner(copy.InstanceId)){message="Equipped gear cannot be sold.";return false;}
                manager.Equipment.Owned.Remove(copy);
            }else{
                var item=manager.Inventory.Items.Find(i=>i!=null&&i.ItemId==offer.Item?.ItemId);
                if(item==null||item.Quantity<=0)return false;
                item.Quantity--;manager.Inventory.NotifyChanged();
            }
            manager.Equipment.Coins+=price;
        }else{
            if(manager.Equipment.Coins<price){message="Not enough coins.";return false;}
            if(offer.Equipment){
                if(!manager.Equipment.Catalog.Contains(offer.Equipment)){message="Equipment is not registered in the catalog.";return false;}
                manager.Equipment.Owned.Add(new OwnedEquipment{InstanceId=System.Guid.NewGuid().ToString("N"),Definition=offer.Equipment});
            }else{
                if(offer.Item==null||offer.Item.Category!=BattleActionCategory.Item||string.IsNullOrWhiteSpace(offer.Item.ItemId)||manager.Inventory.Count(offer.Item.ItemId)==int.MaxValue)return false;
                manager.Inventory.Add(offer.Item,1);manager.Inventory.NotifyChanged();
            }
            manager.Equipment.Coins-=price;
        }
        message=selling?"Sold "+offer.Name+".":"Purchased "+offer.Name+".";
        return true;
    }
    [Min(1)] public float InteractionRange=3;
    Canvas promptCanvas;RectTransform prompt;Image glyph;TMP_Text caption;
    void Reset(){GetComponent<BoxCollider>().isTrigger=true;}
    public bool InRange(Transform actor)=>actor&&Vector3.Distance(actor.position,transform.position)<=Mathf.Max(1,InteractionRange);
    public static Merchant Nearest(Transform actor)=>!actor?null:FindObjectsByType<Merchant>(FindObjectsSortMode.None).Where(c=>c.isActiveAndEnabled&&c.InRange(actor)).OrderBy(c=>(c.transform.position-actor.position).sqrMagnitude).FirstOrDefault();
    public bool CanServe(GameManager manager)=>isActiveAndEnabled&&manager&&manager.PossessedCharacter&&manager.PossessedCharacter.gameObject.scene==gameObject.scene&&InRange(manager.PossessedCharacter.transform);
    public bool CanInteract=>isActiveAndEnabled&&GameManager.Instance&&GameManager.Instance.PossessedCharacter&&InRange(GameManager.Instance.PossessedCharacter.transform)&&!GameManager.Instance.WorldInputBlocked&&!(BattleManager.Instance&&BattleManager.Instance.InBattle);
    void Update()
    {
        bool visible=CanInteract&&Nearest(GameManager.Instance.PossessedCharacter.transform)==this;
        if(visible&&GameManager.Instance.Input&&GameManager.Instance.Input.InteractPressed)GameManager.Instance.ProgressionMenu.OpenShop(this);
        if(!Application.isPlaying)return;
        if(!visible){if(promptCanvas)promptCanvas.gameObject.SetActive(false);return;}
        if(!promptCanvas)BuildPrompt();promptCanvas.gameObject.SetActive(true);
        var camera=Camera.main;if(!camera){promptCanvas.gameObject.SetActive(false);return;}
        var screen=camera.WorldToScreenPoint(transform.position+Vector3.up*2);if(screen.z<=0){promptCanvas.gameObject.SetActive(false);return;}
        prompt.position=screen;
        bool controller=GameManager.Instance.Input&&ControllerGlyphs.ShowController(GameManager.Instance.Input);var catalog=ControllerGlyphs.Catalog;
        glyph.sprite=controller&&catalog&&catalog.Buttons!=null&&catalog.Buttons.Length>3?ControllerGlyphs.Resolve(catalog.Buttons[3].Xbox,out _):!controller&&catalog&&catalog.OverworldKeys!=null&&catalog.OverworldKeys.Length>1?catalog.OverworldKeys[1]:null;
        glyph.material=null;glyph.color=Color.white;glyph.enabled=glyph.sprite;if(controller&&glyph.sprite)ControllerGlyphs.StyleFace(glyph);
        caption.text=(glyph.sprite?"":controller?(ControllerGlyphs.PlayStation?"Triangle · ":"Y · "):"E · ")+DisplayName;
    }
    void BuildPrompt()
    {
        var go=new GameObject("Merchant interaction prompt",typeof(Canvas));go.transform.SetParent(transform,false);promptCanvas=go.GetComponent<Canvas>();promptCanvas.renderMode=RenderMode.ScreenSpaceOverlay;promptCanvas.sortingOrder=120;
        var panel=new GameObject("Prompt",typeof(RectTransform),typeof(Image));panel.transform.SetParent(go.transform,false);prompt=panel.GetComponent<RectTransform>();prompt.sizeDelta=new Vector2(220,38);var background=panel.GetComponent<Image>();background.color=new Color(.025f,.05f,.08f,.85f);background.raycastTarget=false;
        var icon=new GameObject("Interact icon",typeof(RectTransform),typeof(Image));icon.transform.SetParent(panel.transform,false);var ir=icon.GetComponent<RectTransform>();ir.sizeDelta=new Vector2(28,28);ir.anchoredPosition=new Vector2(-88,0);glyph=icon.GetComponent<Image>();glyph.preserveAspect=true;glyph.raycastTarget=false;
        var text=new GameObject("Merchant name",typeof(RectTransform));text.transform.SetParent(panel.transform,false);var tr=text.GetComponent<RectTransform>();tr.sizeDelta=new Vector2(170,32);tr.anchoredPosition=new Vector2(20,0);caption=text.AddComponent<TextMeshProUGUI>();caption.font=TMP_Settings.defaultFontAsset;caption.fontSize=15;caption.alignment=TextAlignmentOptions.MidlineLeft;caption.raycastTarget=false;
    }
    void OnDisable(){if(promptCanvas)promptCanvas.gameObject.SetActive(false);}
}
