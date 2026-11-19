using System.Collections.Generic;
using Game.GridPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Modal owns its input so selecting a reserve never also selects a tile or ends a turn.
public sealed class BattleSwapPanel : MonoBehaviour
{
    [SerializeField] BattleSwapLayout layout;
    BattleSwapLayout UI=>layout?layout:Resources.Load<BattleSwapLayout>("UI/Swap/Swap");
    BattleSwapView modular;
    readonly List<BattleSwapCardView> reserveCards=new();
    private InSceneGridBattle battle;
    private GameObject panel;
    private TMP_FontAsset font;
    private TMP_Text prompt;
    private GridCameraController cameraControl;
    private bool cameraEnabled;
    private readonly List<Button> choices = new();
    private int selected, pending = -1, openedFrame;
    public bool IsOpen => panel && panel.activeSelf;
    public void Open(InSceneGridBattle source, TMP_FontAsset textFont)
    {
        Close(); battle = source; font = textFont; selected = 0; pending = -1; openedFrame = Time.frameCount;
        battle.PartySelectionOpen = true; battle.GridInputEnabled = false;
        cameraControl = battle.BattleCamera.GetComponent<GridCameraController>(); cameraEnabled = cameraControl.enabled; cameraControl.enabled = false;
        if(BuildModular())return;
        panel = new GameObject("Swap party member", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(transform, false);
        var rect = (RectTransform)panel.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0, 0, 0, .65f);
        var capture = panel.AddComponent<BattlePortraitCapture>();
        var row = Rect("Reserve choices", rect, new Vector2(.5f, 0), new Vector2(0, 26), new Vector2(500, 210));
        prompt = Text(Rect("Prompt", row, new Vector2(.5f, 1), new Vector2(0, -12), new Vector2(490, 40)), "Choose a reserve. Swapping costs one action.");
        Portrait(row, source.Info(source.CurrentActorIndex), capture, -1, new Vector2(-195, 60), false);
        choices.Clear();
        for (int i = 0; i < source.Reserves.Count; i++)
        {
            int index = i; var reserve = source.Reserves[i];
            var button = Portrait(row, reserve.Info, capture, i, new Vector2(-65 + i * 115, 60), true);
            button.interactable = reserve.Unit.Health > 0;
            button.onClick.AddListener(() => Choose(index)); choices.Add(button);
        }
        var confirm = MakeButton(row, "Confirm swap", new Vector2(-65, 14), () => Confirm());
        confirm.name = "Confirm swap"; confirm.gameObject.SetActive(false);
        MakeButton(row, "Cancel / B", new Vector2(80, 14), Close);
        Highlight();
    }
    bool BuildModular(){
        if(!UI||!UI.Window||!UI.Portrait||UI.Window.Reserves==null||UI.Window.Reserves.Length<battle.Reserves.Count)return false;
        modular=Instantiate(UI.Window,transform,false);panel=modular.gameObject;panel.name="Swap party member";prompt=modular.Prompt;
        prompt.text="Choose a reserve";var capture=panel.AddComponent<BattlePortraitCapture>();
        var current=MakeCard(modular.Current,battle.Info(battle.CurrentActorIndex),battle.Combatants[battle.CurrentActorIndex],capture,-1);
        current.Button.interactable=false;
        for(int i=0;i<modular.Reserves.Length;i++){
            modular.Reserves[i].gameObject.SetActive(i<battle.Reserves.Count);if(i>=battle.Reserves.Count)continue;
            int index=i;var reserve=battle.Reserves[i];var card=MakeCard(modular.Reserves[i],reserve.Info,reserve.Unit,capture,i);
            card.Button.onClick.AddListener(()=>Choose(index));choices.Add(card.Button);reserveCards.Add(card);
        }
        modular.Confirm.onClick.AddListener(Confirm);modular.Cancel.onClick.AddListener(Close);modular.Confirm.gameObject.SetActive(false);Highlight();return true;
    }
    BattleSwapCardView MakeCard(RectTransform host,BattleCharacterInfo info,GridUnit unit,BattlePortraitCapture capture,int index){
        var card=Instantiate(UI.Portrait,host,false);var r=(RectTransform)card.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
        card.Present(string.IsNullOrWhiteSpace(info.DisplayName)?info.name:info.DisplayName,info.Portrait?info.Portrait:capture.Capture(info.transform,index+20),unit.Health,unit.MaxHealth,unit.Health>0,false,UI);return card;
    }
    void ShowConfirmation(bool visible){
        if(modular)modular.Confirm.gameObject.SetActive(visible);else panel.transform.Find("Reserve choices/Confirm swap").gameObject.SetActive(visible);
        if(!visible)prompt.text="Choose a reserve";
    }
    RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
        var rect = (RectTransform)obj.transform; rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(.5f, 0);
        rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
    }
    TMP_Text Text(RectTransform rect, string value)
    {
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.text = value; text.fontSize = 13;
        text.enableAutoSizing = true; text.fontSizeMin = 9; text.fontSizeMax = 13; text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; return text;
    }
    Button Portrait(Transform row, BattleCharacterInfo info, BattlePortraitCapture capture, int index, Vector2 position, bool selectable)
    {
        var rect = Rect(info.DisplayName, row, new Vector2(.5f, 0), position, new Vector2(96, 112));
        var image = rect.gameObject.AddComponent<RawImage>(); image.texture = info.Portrait ? info.Portrait : capture.Capture(info.transform, index + 20);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.interactable = selectable;
        Text(Rect("Name", rect, new Vector2(.5f, 0), new Vector2(0, -22), new Vector2(110, 22)), string.IsNullOrWhiteSpace(info.DisplayName) ? info.name : info.DisplayName);
        if (index >= 0) Text(Rect("Health", rect, new Vector2(.5f, 0), Vector2.zero, new Vector2(96, 20)), $"{battle.Reserves[index].Unit.Health} / {battle.Reserves[index].Unit.MaxHealth}");
        return button;
    }
    Button MakeButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction click)
    {
        var rect = Rect(label, parent, new Vector2(.5f, 0), position, new Vector2(130, 25));
        var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.15f, .18f, .23f);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.navigation = new Navigation { mode = Navigation.Mode.None }; button.onClick.AddListener(click);
        Text(Rect("Label", rect, new Vector2(.5f, 0), Vector2.zero, rect.sizeDelta), label); return button;
    }
    void Choose(int index)
    {
        if (!IsOpen || !choices[index].interactable) return;
        selected = pending = index;
        prompt.text = $"Swap {battle.DisplayName(battle.CurrentActorIndex)} for {battle.Reserves[index].Info.DisplayName}?";
        ShowConfirmation(true); Highlight();
    }
    void Confirm()
    {
        if (pending < 0) return;
        var source = battle; int index = pending; Close(); source.SwapWithReserve(index);
    }
    void Highlight()
    {
        if(modular){for(int i=0;i<reserveCards.Count;i++)reserveCards[i].Highlight.color=!choices[i].interactable?UI.Unavailable:i==selected?UI.Selected:UI.Normal;return;}
        for (int i = 0; i < choices.Count; i++)
            choices[i].targetGraphic.color = !choices[i].interactable ? new Color(.3f, .3f, .3f) : i == selected ? new Color(1, .85f, .5f) : Color.white;
    }
    void Update()
    {
        if (!IsOpen || Time.frameCount == openedFrame || !Application.isFocused) return;
        var k = Keyboard.current; var p = Gamepad.current;
        if ((k != null && (k.escapeKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame)) || (p != null && p.buttonEast.wasPressedThisFrame)) { Close(); return; }
        int direction = ((k != null && k.rightArrowKey.wasPressedThisFrame) || (p != null && p.dpad.right.wasPressedThisFrame)) ? 1 :
            ((k != null && k.leftArrowKey.wasPressedThisFrame) || (p != null && p.dpad.left.wasPressedThisFrame)) ? -1 : 0;
        if (direction != 0 && choices.Count > 0) { selected = (selected + direction + choices.Count) % choices.Count; pending = -1; ShowConfirmation(false); Highlight(); }
        if ((k != null && k.enterKey.wasPressedThisFrame) || (p != null && p.buttonSouth.wasPressedThisFrame))
        { if (pending >= 0) Confirm(); else if (choices.Count > 0) Choose(selected); }
    }
    public void Close()
    {
        if (panel) { panel.SetActive(false); if (Application.isPlaying) Destroy(panel); else DestroyImmediate(panel); panel = null; }
        if (battle) { battle.PartySelectionOpen = false; battle.PartySelectionClosedFrame = Time.frameCount; battle.GridInputEnabled = true; }
        if (cameraControl) cameraControl.enabled = cameraEnabled; cameraControl = null;
        battle = null; choices.Clear(); reserveCards.Clear();modular=null; pending = -1;
    }
    void OnDisable() => Close();
}
