using System;
using Game.GridPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class ProgressionMenu
{
    static readonly Color Ink = new Color(.88f,.94f,1), Panel = new Color(.035f,.075f,.12f,.98f), Accent = new Color(.3f,.8f,1);
    TMP_Text detailName;
    RectTransform detailContent;
    RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0,1);
        rect.anchoredPosition = new Vector2(x,-y); rect.sizeDelta = new Vector2(width,height); return rect;
    }
    TMP_Text Label(RectTransform rect, string value, int size = 17)
    {
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size; text.color = Ink; text.text = value; text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal; return text;
    }
    Button Button(Transform parent, string name, float x, float y, float width, Action click, Action preview = null, float height = 38)
    {
        var rect = Rect(name, parent,x,y,width,height); var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.08f,.17f,.25f);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.highlightedColor = new Color(.35f,.65f,.8f); colors.selectedColor = colors.highlightedColor; colors.pressedColor = Accent; colors.disabledColor = new Color(.3f,.35f,.4f); button.colors = colors;
        button.onClick.AddListener(() => click?.Invoke());
        var text = Label(Rect("Label",rect,12,6,width-24,height-10),name,16); text.alignment = TextAlignmentOptions.MidlineLeft;
        var hover = rect.gameObject.AddComponent<PlayerMenuFocus>(); hover.Preview = preview;
        buttons.Add(button); return button;
    }
    RectTransform List(Transform parent, string name, float x, float y, float width, float height, float contentHeight)
    {
        var viewport = Rect(name,parent,x,y,width,height); viewport.gameObject.AddComponent<Image>().color = new Color(.025f,.05f,.08f,.6f);
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = Rect("Content",viewport,0,0,width-8,Mathf.Max(height,contentHeight));
        var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport=viewport; scroll.content=content;
        scroll.horizontal=false; scroll.vertical=true; scroll.scrollSensitivity=45; scroll.movementType=ScrollRect.MovementType.Clamped;
        return content;
    }
    void Build()
    {
        canvasObject=new GameObject("Player menu",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)); canvasObject.transform.SetParent(transform,false);
        var canvas=canvasObject.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=200;
        var scaler=canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1280,720); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var backdrop=Rect("Backdrop",canvasObject.transform,0,0,0,0); backdrop.anchorMin=Vector2.zero; backdrop.anchorMax=Vector2.one; backdrop.offsetMin=backdrop.offsetMax=Vector2.zero;
        backdrop.gameObject.AddComponent<Image>().color=new Color(.01f,.025f,.045f,1);
        shell=Rect("Menu frame",canvasObject.transform,0,0,1200,660); shell.anchorMin=Vector2.zero; shell.anchorMax=Vector2.one; shell.offsetMin=shell.offsetMax=Vector2.zero;
        shell.gameObject.AddComponent<Image>().color=new Color(Panel.r,Panel.g,Panel.b,1);
        Label(Rect("Brand",shell,24,20,180,40),"PARTY MENU",23).color=Accent;
        title=Label(Rect("Page title",shell,220,20,460,42),"PARTY",26);
        characterLabel=Label(Rect("Selected member",shell,770,24,300,40),"",18);
        Button(shell,"<",720,22,40,()=>Cycle(-1)); Button(shell,">",1100,22,40,()=>Cycle(1));
        int i=0; foreach(Page value in Enum.GetValues(typeof(Page))) { if(value==Page.Checkpoint)continue; var captured=value; Button(shell,value==Page.SkillTree?"Skill Tree":value.ToString(),24,100+48*i++,170,()=>ShowPage(captured)); }
        Button(shell,"Return to world",24,552,170,Close);
        body=Rect("Page content",shell,220,88,510,510); detailRoot=Rect("Details",shell,750,88,426,510);
        footer=Label(Rect("Controls",shell,24,620,1150,30),"",13);
        PlaceAtRight(characterLabel.rectTransform,406);
        PlaceAtRight(shell.Find("<").GetComponent<RectTransform>(),456);
        PlaceAtRight(shell.Find(">").GetComponent<RectTransform>(),76);
        var exit=shell.Find("Return to world").GetComponent<RectTransform>();
        exit.anchorMin=exit.anchorMax=new Vector2(0,0);exit.anchoredPosition=new Vector2(24,104);
        footer.rectTransform.anchorMin=new Vector2(0,0);footer.rectTransform.anchorMax=new Vector2(1,0);
        footer.rectTransform.sizeDelta=new Vector2(-48,30);footer.rectTransform.anchoredPosition=new Vector2(24,44);
        body.anchorMin=new Vector2(0,0);body.anchorMax=new Vector2(1,1);body.offsetMin=new Vector2(220,70);body.offsetMax=new Vector2(-470,-88);
        detailRoot.anchorMin=new Vector2(1,0);detailRoot.anchorMax=Vector2.one;detailRoot.offsetMin=new Vector2(-450,70);detailRoot.offsetMax=new Vector2(-24,-88);
        capture=canvasObject.AddComponent<BattlePortraitCapture>(); canvasObject.SetActive(false);
    }
    void PlaceAtRight(RectTransform rect,float inset)
    { rect.anchorMin=rect.anchorMax=Vector2.one;rect.anchoredPosition=new Vector2(-inset,rect.anchoredPosition.y); }
    void BuildDetails()
    {
        detailName=Label(Rect("Detail name",detailRoot,12,0,400,54),"",22);
        var picture=Rect("Image",detailRoot,12,58,144,144); detailImage=picture.gameObject.AddComponent<RawImage>(); detailImage.raycastTarget=false;
        var bar=Rect("Experience",detailRoot,172,160,238,10); bar.gameObject.AddComponent<Image>().color=new Color(.1f,.15f,.2f);
        var fill=Rect("Fill",bar,0,0,238,10); experienceFill=fill.gameObject.AddComponent<Image>(); experienceFill.color=Accent;
        detailContent=List(detailRoot,"Scrollable details",12,220,400,288,288);
        detailText=Label(Rect("Description",detailContent,8,8,376,272),"",16);
    }
    void Detail(string name,string description,Texture image=null,float progress=-1)
    {
        if (!detailText) return;
        detailName.text=name; detailImage.texture=image; detailImage.color=image?Color.white:Color.clear;
        float top = image ? 220 : progress >= 0 ? 100 : 72;
        var viewport = (RectTransform)detailContent.parent;
        viewport.anchoredPosition = new Vector2(12, -top);
        viewport.sizeDelta = new Vector2(400, Mathf.Max(200, detailRoot.rect.height - top - 2));
        ((RectTransform)experienceFill.transform.parent).anchoredPosition = image ? new Vector2(172,-160) : new Vector2(12,-72);
        detailText.text=description; detailText.rectTransform.sizeDelta=new Vector2(376,Mathf.Max(260,detailText.GetPreferredValues(description,376,0).y));
        detailContent.sizeDelta=new Vector2(detailContent.sizeDelta.x,Mathf.Max(viewport.sizeDelta.y,detailText.rectTransform.sizeDelta.y+20));
        detailContent.anchoredPosition = Vector2.zero;
        experienceFill.transform.parent.gameObject.SetActive(progress>=0);
        experienceFill.rectTransform.sizeDelta=new Vector2(238*Mathf.Clamp01(progress),10);
    }
    Texture Portrait(BattleCharacterInfo member)
    {
        if (member.Portrait) return member.Portrait;
        if (!portraits.TryGetValue(member,out var value)) { value=capture.Capture(member.transform,portraits.Count); portraits[member]=value; }
        return value;
    }
}
