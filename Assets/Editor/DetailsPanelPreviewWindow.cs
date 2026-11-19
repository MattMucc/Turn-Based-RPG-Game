using System;
using System.Collections.Generic;
using System.Linq;
using Game.GridPrototype;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Isolated preview scene only: never opens, changes or saves a gameplay scene.
public sealed class DetailsPanelPreviewWindow : EditorWindow
{
    [SerializeField] DetailsPanelLayout layout;
    [SerializeField] PartyPageLayout partyLayout;
    [SerializeField] InventoryPageLayout inventoryLayout;
    [SerializeField] AbilitiesPageLayout abilitiesLayout;
    [SerializeField] GrowthPageLayout growthLayout;
    [SerializeField] SystemPageLayout systemLayout;
    [SerializeField] MerchantPageLayout merchantLayout;
    [SerializeField] PlayerMenuShellLayout shellLayout;
    [SerializeField] BattleUILayout battleLayout;
    [SerializeField] WorldUILayout worldLayout;
    [SerializeField] int worldPreview,worldIcons;
    [SerializeField] float worldPulseTime;
    [SerializeField] CheckpointUILayout checkpointLayout;
    [SerializeField] int checkpointPreview;
    [SerializeField] TooltipHintsLayout hintsLayout;
    [SerializeField] int hintsPreview;
    [SerializeField] bool hintsLongText;
    [SerializeField] InventoryInteractionLayout interactionLayout;
    [SerializeField] int interactionPreview;
    [SerializeField] bool interactionUnavailable,interactionLongText;
    [SerializeField] BattleSwapLayout swapLayout;
    [SerializeField] bool swapConfirm,swapKnockedOut;
    [SerializeField] int swapSelected,swapCount=3;
    [SerializeField] Texture swapPortrait;
    [SerializeField] OverheadUILayout overheadLayout;
    [SerializeField] int overheadPreview,overheadLevel=7;
    [SerializeField] bool overheadStatuses=true,overheadBarrier=true;
    [SerializeField] float overheadHealth=.65f;
    [SerializeField] string overheadName="Prototype Balanced";
    [SerializeField] TargetUILayout targetLayout;
    TargetUILayout previewTargetLayout;
    [SerializeField] int targetPreview;
    [SerializeField] bool targetObject,targetNoStatuses;
    [SerializeField] float targetHealthFraction=.65f;
    [SerializeField] ReactionUILayout reactionLayout;
    [SerializeField] int reactionPreview,sequenceLength=6;
    [SerializeField] float reactionProgress=.65f;
    [SerializeField] bool reactionController,previewHeld;
    [SerializeField] QteButton previewInput=QteButton.South;
    [SerializeField] bool battleRight,battleStatuses,battleActionSpent,battleShortcutFocus;
    [SerializeField] int shellPreview;
    [SerializeField] int merchantPreview;
    [SerializeField] int systemPreview;
    [SerializeField] bool growthDebtPreview;
    [SerializeField] int abilitiesPreview;
    [SerializeField] int inventoryPreview;

    [SerializeField] EquipmentDefinition equipment;
    [SerializeField] int attackIndex;
    [SerializeField] int panelWidth=426,panelHeight=510;
    [SerializeField] bool followSelection=true,live=true;
    [SerializeField] float scroll;
    [SerializeField] float horizontalScroll;
    PreviewRenderUtility preview;
    GameObject canvasRoot;
    RectTransform content;
    DetailsPanelLayout temporaryLayout;
    string fingerprint;
    double nextCheck;
    bool dirty=true;
    string error;
    float contentHeight;
    [MenuItem("Tools/Game/UI/Details panel preview")]
    public static void Open()
    {
        var window=GetWindow<DetailsPanelPreviewWindow>("Details preview");
        window.minSize=new Vector2(380,420);
        if(Selection.activeObject is DetailsPanelLayout asset)window.SetLayout(asset);else if(Selection.activeObject is PartyPageLayout party)window.SetParty(party);else if(Selection.activeObject is InventoryPageLayout inventory)window.SetInventory(inventory);else if(Selection.activeObject is AbilitiesPageLayout abilities)window.SetAbilities(abilities);else if(Selection.activeObject is GrowthPageLayout growth)window.SetGrowth(growth);else if(Selection.activeObject is SystemPageLayout system)window.SetSystem(system);else if(Selection.activeObject is MerchantPageLayout merchant)window.SetMerchant(merchant);else if(Selection.activeObject is PlayerMenuShellLayout menuShell)window.SetShell(menuShell);else if(Selection.activeObject is BattleUILayout battle)window.SetBattle(battle);else if(Selection.activeObject is ReactionUILayout reactions)window.SetReactions(reactions);else if(Selection.activeObject is TargetUILayout targets)window.SetTargets(targets);else if(Selection.activeObject is OverheadUILayout overhead)window.SetOverhead(overhead);else if(Selection.activeObject is BattleSwapLayout swap)window.SetSwap(swap);else if(Selection.activeObject is InventoryInteractionLayout interaction)window.SetInteraction(interaction);else if(Selection.activeObject is TooltipHintsLayout hints)window.SetHints(hints);else if(Selection.activeObject is CheckpointUILayout checkpoint)window.SetCheckpoint(checkpoint);else if(Selection.activeObject is WorldUILayout world)window.SetWorld(world);
    }
    public static void Open(DetailsPanelLayout asset){var window=GetWindow<DetailsPanelPreviewWindow>("Details preview");window.SetLayout(asset);}
    public static void Open(PartyPageLayout asset){var window=GetWindow<DetailsPanelPreviewWindow>("UI preview");window.SetParty(asset);}
    public static void Open(InventoryPageLayout asset){var window=GetWindow<DetailsPanelPreviewWindow>("UI preview");window.SetInventory(asset);}
    public static void Open(AbilitiesPageLayout asset){var window=GetWindow<DetailsPanelPreviewWindow>("UI preview");window.SetAbilities(asset);}
    public static void Open(GrowthPageLayout asset){var window=GetWindow<DetailsPanelPreviewWindow>("UI preview");window.SetGrowth(asset);}
    public static void Open(SystemPageLayout asset){var window=GetWindow<DetailsPanelPreviewWindow>("UI preview");window.SetSystem(asset);}
    public static void Open(MerchantPageLayout asset){var window=GetWindow<DetailsPanelPreviewWindow>("UI preview");window.SetMerchant(asset);}
    public static void Open(PlayerMenuShellLayout asset){var window=GetWindow<DetailsPanelPreviewWindow>("UI preview");window.SetShell(asset);}
    public static void Open(BattleUILayout asset){var window=GetWindow<DetailsPanelPreviewWindow>("UI preview");window.SetBattle(asset);}
    public static void Open(ReactionUILayout asset){var window=GetWindow<DetailsPanelPreviewWindow>("UI preview");window.SetReactions(asset);}
    public static void Open(WorldUILayout asset){var window=GetWindow<DetailsPanelPreviewWindow>("UI preview");window.SetWorld(asset);}
    void SetWorld(WorldUILayout asset){SetReactions(null);worldLayout=asset;dirty=true;Repaint();}
    public static void Open(CheckpointUILayout asset){var window=GetWindow<DetailsPanelPreviewWindow>("UI preview");window.SetCheckpoint(asset);}
    void SetCheckpoint(CheckpointUILayout asset){SetReactions(null);checkpointLayout=asset;dirty=true;Repaint();}
    public static void Open(TooltipHintsLayout asset){var window=GetWindow<DetailsPanelPreviewWindow>("UI preview");window.SetHints(asset);}
    void SetHints(TooltipHintsLayout asset){SetReactions(null);hintsLayout=asset;dirty=true;Repaint();}
    public static void Open(InventoryInteractionLayout asset){var window=GetWindow<DetailsPanelPreviewWindow>("UI preview");window.SetInteraction(asset);}
    void SetInteraction(InventoryInteractionLayout asset){SetReactions(null);interactionLayout=asset;dirty=true;Repaint();}
    public static void Open(BattleSwapLayout asset){var window=GetWindow<DetailsPanelPreviewWindow>("UI preview");window.SetSwap(asset);}
    void SetSwap(BattleSwapLayout asset){SetReactions(null);swapLayout=asset;dirty=true;Repaint();}
    public static void Open(OverheadUILayout asset){var window=GetWindow<DetailsPanelPreviewWindow>("UI preview");window.SetOverhead(asset);}
    void SetOverhead(OverheadUILayout asset){SetReactions(null);overheadLayout=asset;dirty=true;Repaint();}
    public static void Open(TargetUILayout asset){var window=GetWindow<DetailsPanelPreviewWindow>("UI preview");window.SetTargets(asset);}
    void SetTargets(TargetUILayout asset){SetReactions(null);targetLayout=asset;dirty=true;Repaint();}
    void SetReactions(ReactionUILayout asset){worldLayout=null;checkpointLayout=null;hintsLayout=null;interactionLayout=null;swapLayout=null;overheadLayout=null;targetLayout=null;reactionLayout=asset;battleLayout=null;shellLayout=null;merchantLayout=null;systemLayout=null;growthLayout=null;abilitiesLayout=null;inventoryLayout=null;partyLayout=null;layout=null;panelWidth=1280;panelHeight=720;scroll=0;dirty=true;Repaint();}
    void SetBattle(BattleUILayout asset){worldLayout=null;checkpointLayout=null;hintsLayout=null;interactionLayout=null;swapLayout=null;overheadLayout=null;targetLayout=null;reactionLayout=null;battleLayout=asset;shellLayout=null;merchantLayout=null;systemLayout=null;growthLayout=null;abilitiesLayout=null;inventoryLayout=null;partyLayout=null;layout=null;panelWidth=1280;panelHeight=720;scroll=0;dirty=true;Repaint();}
    void SetShell(PlayerMenuShellLayout asset){worldLayout=null;checkpointLayout=null;hintsLayout=null;interactionLayout=null;swapLayout=null;overheadLayout=null;targetLayout=null;reactionLayout=null;battleLayout=null;shellLayout=asset;merchantLayout=null;systemLayout=null;growthLayout=null;abilitiesLayout=null;inventoryLayout=null;partyLayout=null;layout=null;panelWidth=1280;panelHeight=720;scroll=0;dirty=true;Repaint();}
    void SetMerchant(MerchantPageLayout asset){worldLayout=null;checkpointLayout=null;hintsLayout=null;interactionLayout=null;swapLayout=null;overheadLayout=null;targetLayout=null;reactionLayout=null;battleLayout=null;shellLayout=null;merchantLayout=asset;systemLayout=null;growthLayout=null;abilitiesLayout=null;inventoryLayout=null;partyLayout=null;layout=null;panelWidth=590;panelHeight=562;scroll=0;dirty=true;Repaint();}
    void SetSystem(SystemPageLayout asset){worldLayout=null;checkpointLayout=null;hintsLayout=null;interactionLayout=null;swapLayout=null;overheadLayout=null;targetLayout=null;reactionLayout=null;battleLayout=null;shellLayout=null;merchantLayout=null;systemLayout=asset;growthLayout=null;abilitiesLayout=null;inventoryLayout=null;partyLayout=null;layout=null;panelWidth=590;panelHeight=562;scroll=0;dirty=true;Repaint();}
    void SetGrowth(GrowthPageLayout asset){worldLayout=null;checkpointLayout=null;hintsLayout=null;interactionLayout=null;swapLayout=null;overheadLayout=null;targetLayout=null;reactionLayout=null;battleLayout=null;shellLayout=null;merchantLayout=null;systemLayout=null;growthLayout=asset;horizontalScroll=0;abilitiesLayout=null;inventoryLayout=null;partyLayout=null;layout=null;panelWidth=590;panelHeight=562;scroll=0;dirty=true;Repaint();}
    void SetAbilities(AbilitiesPageLayout asset){worldLayout=null;checkpointLayout=null;hintsLayout=null;interactionLayout=null;swapLayout=null;overheadLayout=null;targetLayout=null;reactionLayout=null;battleLayout=null;shellLayout=null;merchantLayout=null;systemLayout=null;growthLayout=null;abilitiesLayout=asset;inventoryLayout=null;partyLayout=null;layout=null;panelWidth=590;panelHeight=562;scroll=0;dirty=true;Repaint();}
    void SetInventory(InventoryPageLayout asset){worldLayout=null;checkpointLayout=null;hintsLayout=null;interactionLayout=null;swapLayout=null;overheadLayout=null;targetLayout=null;reactionLayout=null;battleLayout=null;shellLayout=null;merchantLayout=null;systemLayout=null;growthLayout=null;abilitiesLayout=null;inventoryLayout=asset;partyLayout=null;layout=null;panelWidth=590;panelHeight=562;scroll=0;dirty=true;Repaint();}
    void SetParty(PartyPageLayout asset){worldLayout=null;checkpointLayout=null;hintsLayout=null;interactionLayout=null;swapLayout=null;overheadLayout=null;targetLayout=null;reactionLayout=null;battleLayout=null;shellLayout=null;merchantLayout=null;systemLayout=null;growthLayout=null;abilitiesLayout=null;inventoryLayout=null;partyLayout=asset;layout=null;panelWidth=510;panelHeight=456;scroll=0;dirty=true;Repaint();}
    void SetLayout(DetailsPanelLayout asset){worldLayout=null;checkpointLayout=null;hintsLayout=null;interactionLayout=null;swapLayout=null;overheadLayout=null;targetLayout=null;reactionLayout=null;battleLayout=null;shellLayout=null;merchantLayout=null;systemLayout=null;growthLayout=null;abilitiesLayout=null;inventoryLayout=null;partyLayout=null;layout=asset;scroll=0;dirty=true;Repaint();}
    void OnEnable(){Selection.selectionChanged+=SelectionChanged;Undo.undoRedoPerformed+=Changed;EditorApplication.projectChanged+=Changed;}
    void OnDisable(){Selection.selectionChanged-=SelectionChanged;Undo.undoRedoPerformed-=Changed;EditorApplication.projectChanged-=Changed;DisposePreview();}
    void Changed(){dirty=true;Repaint();}
    void SelectionChanged(){if(!followSelection)return;if(Selection.activeObject is DetailsPanelLayout asset)SetLayout(asset);else if(Selection.activeObject is PartyPageLayout party)SetParty(party);else if(Selection.activeObject is InventoryPageLayout inventory)SetInventory(inventory);else if(Selection.activeObject is AbilitiesPageLayout abilities)SetAbilities(abilities);else if(Selection.activeObject is GrowthPageLayout growth)SetGrowth(growth);else if(Selection.activeObject is SystemPageLayout system)SetSystem(system);else if(Selection.activeObject is MerchantPageLayout merchant)SetMerchant(merchant);else if(Selection.activeObject is PlayerMenuShellLayout menuShell)SetShell(menuShell);else if(Selection.activeObject is BattleUILayout battle)SetBattle(battle);else if(Selection.activeObject is ReactionUILayout reactions)SetReactions(reactions);else if(Selection.activeObject is TargetUILayout targets)SetTargets(targets);else if(Selection.activeObject is OverheadUILayout overhead)SetOverhead(overhead);else if(Selection.activeObject is BattleSwapLayout swap)SetSwap(swap);else if(Selection.activeObject is InventoryInteractionLayout interaction)SetInteraction(interaction);else if(Selection.activeObject is TooltipHintsLayout hints)SetHints(hints);else if(Selection.activeObject is CheckpointUILayout checkpoint)SetCheckpoint(checkpoint);else if(Selection.activeObject is WorldUILayout world)SetWorld(world);}
    void Update()
    {
        if(!live||EditorApplication.timeSinceStartup<nextCheck)return;
        nextCheck=EditorApplication.timeSinceStartup+.3;
        string current=Signature();
        if(current!=fingerprint){fingerprint=current;dirty=true;Repaint();}
    }
    string Signature()
    {
        if(worldLayout){
            string stamp=EditorJsonUtility.ToJson(worldLayout);
            foreach(var reference in new Component[]{worldLayout.Merchant,worldLayout.Blacksmith,worldLayout.Checkpoint,worldLayout.SaveIndicator,worldLayout.EncounterNotice})if(reference)stamp+=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(reference));
            var stage=PrefabStageUtility.GetCurrentPrefabStage();if(stage!=null)foreach(var c in stage.prefabContentsRoot.GetComponentsInChildren<Component>(true))if(c)stamp+=EditorJsonUtility.ToJson(c);
            return stamp;
        }
        if(checkpointLayout){
            string stamp=EditorJsonUtility.ToJson(checkpointLayout);
            var saves=Resources.Load<SystemPageLayout>("UI/System/System");if(saves)foreach(var reference in new Component[]{saves.SaveSlots,saves.Confirmation,saves.SaveSlot})if(reference)stamp+=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(reference));
            foreach(var reference in new Component[]{checkpointLayout.Menu,checkpointLayout.Details})if(reference)stamp+=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(reference));
            var stage=PrefabStageUtility.GetCurrentPrefabStage();if(stage!=null)foreach(var c in stage.prefabContentsRoot.GetComponentsInChildren<Component>(true))if(c)stamp+=EditorJsonUtility.ToJson(c);
            return stamp;
        }
        if(hintsLayout){
            string stamp=EditorJsonUtility.ToJson(hintsLayout);
            foreach(var reference in new Component[]{hintsLayout.BattleAction,hintsLayout.InventoryHover,hintsLayout.Cursor,hintsLayout.Guide})if(reference)stamp+=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(reference));
            var stage=PrefabStageUtility.GetCurrentPrefabStage();if(stage!=null)foreach(var c in stage.prefabContentsRoot.GetComponentsInChildren<Component>(true))if(c)stamp+=EditorJsonUtility.ToJson(c);
            return stamp;
        }
        if(interactionLayout){
            string stamp=EditorJsonUtility.ToJson(interactionLayout);
            foreach(var reference in new Component[]{interactionLayout.Overlay,interactionLayout.Examination,interactionLayout.Recipients,interactionLayout.Recovery,interactionLayout.Learning,interactionLayout.AccessorySlots,interactionLayout.Equipment,interactionLayout.Recipient})if(reference)stamp+=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(reference));
            var stage=PrefabStageUtility.GetCurrentPrefabStage();if(stage!=null)foreach(var c in stage.prefabContentsRoot.GetComponentsInChildren<Component>(true))if(c)stamp+=EditorJsonUtility.ToJson(c);
            return stamp;
        }
        if(swapLayout){
            string stamp=EditorJsonUtility.ToJson(swapLayout);
            foreach(var reference in new Component[]{swapLayout.Window,swapLayout.Portrait})if(reference)stamp+=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(reference));
            var stage=PrefabStageUtility.GetCurrentPrefabStage();if(stage!=null)foreach(var c in stage.prefabContentsRoot.GetComponentsInChildren<Component>(true))if(c)stamp+=EditorJsonUtility.ToJson(c);
            return stamp;
        }
        if(overheadLayout){
            string stamp=EditorJsonUtility.ToJson(overheadLayout);
            if(overheadLayout.Character)stamp+=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(overheadLayout.Character));
            var stage=PrefabStageUtility.GetCurrentPrefabStage();if(stage!=null)foreach(var c in stage.prefabContentsRoot.GetComponentsInChildren<Component>(true))if(c)stamp+=EditorJsonUtility.ToJson(c);
            return stamp;
        }
        if(targetLayout){
            string stamp=EditorJsonUtility.ToJson(targetLayout);
            foreach(var reference in new Component[]{targetLayout.Target,targetLayout.Inspection,targetLayout.StatusBadge,targetLayout.StatusTooltip})if(reference)stamp+=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(reference));
            var stage=PrefabStageUtility.GetCurrentPrefabStage();if(stage!=null)foreach(var c in stage.prefabContentsRoot.GetComponentsInChildren<Component>(true))if(c)stamp+=EditorJsonUtility.ToJson(c);
            return stamp;
        }
        if(reactionLayout){
            string stamp=EditorJsonUtility.ToJson(reactionLayout);
            foreach(var reference in new Component[]{reactionLayout.Defense,reactionLayout.Qte,reactionLayout.SequenceStep})if(reference)stamp+=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(reference));
            var stage=PrefabStageUtility.GetCurrentPrefabStage();if(stage!=null)foreach(var c in stage.prefabContentsRoot.GetComponentsInChildren<Component>(true))if(c)stamp+=EditorJsonUtility.ToJson(c);
            return stamp;
        }
        if(battleLayout){
            string stamp=EditorJsonUtility.ToJson(battleLayout);
            foreach(var reference in new Component[]{battleLayout.ShortcutDimmer,battleLayout.Commands,battleLayout.CommandButton,battleLayout.RemainingTurn,battleLayout.TopOrder,battleLayout.RightOrder,battleLayout.Portrait,battleLayout.PartyStack,battleLayout.ResourceCard})if(reference)stamp+=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(reference));
            var stage=PrefabStageUtility.GetCurrentPrefabStage();if(stage!=null)foreach(var c in stage.prefabContentsRoot.GetComponentsInChildren<Component>(true))if(c)stamp+=EditorJsonUtility.ToJson(c);
            return stamp;
        }
        if(shellLayout){
            string stamp=EditorJsonUtility.ToJson(shellLayout);
            if(shellLayout.Prefab)stamp+=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(shellLayout.Prefab));
            var stage=PrefabStageUtility.GetCurrentPrefabStage();if(stage!=null)foreach(var c in stage.prefabContentsRoot.GetComponentsInChildren<Component>(true))if(c)stamp+=EditorJsonUtility.ToJson(c);
            return stamp;
        }
        if(merchantLayout){
            string stamp=EditorJsonUtility.ToJson(merchantLayout);
            foreach(var reference in new Component[]{merchantLayout.Workspace,merchantLayout.Slot,merchantLayout.Filter,merchantLayout.Mode,merchantLayout.Action,merchantLayout.Cancel,merchantLayout.AttackDetails})
                if(reference)stamp+=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(reference));
            var stage=PrefabStageUtility.GetCurrentPrefabStage();if(stage!=null)foreach(var c in stage.prefabContentsRoot.GetComponentsInChildren<Component>(true))if(c)stamp+=EditorJsonUtility.ToJson(c);
            return stamp;
        }
        if(systemLayout){
            string stamp=EditorJsonUtility.ToJson(systemLayout);
            foreach(var reference in new Component[]{systemLayout.Workspace,systemLayout.Tab,systemLayout.SaveLoad,systemLayout.Battle,systemLayout.Graphics,systemLayout.UI,systemLayout.Audio,systemLayout.Confirmation,systemLayout.SaveSlots,systemLayout.SaveSlot})
                if(reference)stamp+=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(reference));
            var stage=PrefabStageUtility.GetCurrentPrefabStage();if(stage!=null)foreach(var c in stage.prefabContentsRoot.GetComponentsInChildren<Component>(true))if(c)stamp+=EditorJsonUtility.ToJson(c);
            return stamp;
        }
        if(growthLayout){
            string stamp=EditorJsonUtility.ToJson(growthLayout);if(growthDebtPreview)stamp+=AssetDatabase.GetAssetDependencyHash("Assets/Prefabs/Player/Prototype Debt.prefab");
            foreach(var reference in new Component[]{growthLayout.Workspace,growthLayout.Node,growthLayout.Connection,growthLayout.Arrangement})
                if(reference)stamp+=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(reference));
            var stage=PrefabStageUtility.GetCurrentPrefabStage();if(stage!=null)foreach(var c in stage.prefabContentsRoot.GetComponentsInChildren<Component>(true))if(c)stamp+=EditorJsonUtility.ToJson(c);
            return stamp;
        }
        if(abilitiesLayout){
            string stamp=EditorJsonUtility.ToJson(abilitiesLayout);
            foreach(var reference in new Component[]{abilitiesLayout.Workspace,abilitiesLayout.CategoryButton,abilitiesLayout.FamilyButton,abilitiesLayout.AbilityRow,abilitiesLayout.FavoriteSlot})
                if(reference)stamp+=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(reference));
            var stage=PrefabStageUtility.GetCurrentPrefabStage();if(stage!=null)foreach(var c in stage.prefabContentsRoot.GetComponentsInChildren<Component>(true))if(c)stamp+=EditorJsonUtility.ToJson(c);
            return stamp;
        }
        if(inventoryLayout){
            string stamp=EditorJsonUtility.ToJson(inventoryLayout);
            foreach(var reference in new Component[]{inventoryLayout.Workspace,inventoryLayout.Slot,inventoryLayout.Details,inventoryLayout.Context,inventoryLayout.FilterButton,inventoryLayout.ContextButton})
                if(reference)stamp+=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(reference));
            var stage=PrefabStageUtility.GetCurrentPrefabStage();if(stage!=null)foreach(var c in stage.prefabContentsRoot.GetComponentsInChildren<Component>(true))if(c)stamp+=EditorJsonUtility.ToJson(c);
            return stamp;
        }
        if(partyLayout){
            string stamp=EditorJsonUtility.ToJson(partyLayout);
            if(partyLayout.Prefab)stamp+=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(partyLayout.Prefab));
            var stage=PrefabStageUtility.GetCurrentPrefabStage();
            if(stage!=null)foreach(var c in stage.prefabContentsRoot.GetComponentsInChildren<Component>(true))if(c)stamp+=EditorJsonUtility.ToJson(c);
            return stamp;
        }
        if(!layout)return "";
        string result=EditorJsonUtility.ToJson(layout)+(equipment?EditorJsonUtility.ToJson(equipment):"");
        foreach(var section in layout.Sections)if(section?.Prefab){
            var prefab=PreviewPrefab(section.Prefab);
            result+=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(section.Prefab)).ToString();
            // Include unsaved Prefab Mode changes, including child text styles and decoration.
            foreach(var component in prefab.GetComponentsInChildren<Component>(true))if(component)result+=EditorJsonUtility.ToJson(component);
        }
        return result;
    }
    static DetailsSectionView PreviewPrefab(DetailsSectionView asset)
    {
        var stage=PrefabStageUtility.GetCurrentPrefabStage();
        return stage!=null&&stage.assetPath==AssetDatabase.GetAssetPath(asset)
            ?stage.prefabContentsRoot.GetComponent<DetailsSectionView>()??asset:asset;
    }
    void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        var chosen=(ScriptableObject)EditorGUILayout.ObjectField("Layout",worldLayout?(ScriptableObject)worldLayout:checkpointLayout?(ScriptableObject)checkpointLayout:hintsLayout?(ScriptableObject)hintsLayout:interactionLayout?(ScriptableObject)interactionLayout:swapLayout?(ScriptableObject)swapLayout:overheadLayout?(ScriptableObject)overheadLayout:targetLayout?(ScriptableObject)targetLayout:reactionLayout?(ScriptableObject)reactionLayout:battleLayout?(ScriptableObject)battleLayout:shellLayout?(ScriptableObject)shellLayout:merchantLayout?(ScriptableObject)merchantLayout:systemLayout?(ScriptableObject)systemLayout:growthLayout?(ScriptableObject)growthLayout:abilitiesLayout?(ScriptableObject)abilitiesLayout:inventoryLayout?(ScriptableObject)inventoryLayout:partyLayout?(ScriptableObject)partyLayout:layout,typeof(ScriptableObject),false);
        if(chosen!=worldLayout&&chosen!=checkpointLayout&&chosen!=hintsLayout&&chosen!=interactionLayout&&chosen!=swapLayout&&chosen!=overheadLayout&&chosen!=targetLayout&&chosen!=reactionLayout&&chosen!=battleLayout&&chosen!=shellLayout&&chosen!=merchantLayout&&chosen!=systemLayout&&chosen!=growthLayout&&chosen!=abilitiesLayout&&chosen!=inventoryLayout&&chosen!=partyLayout&&chosen!=layout){if(chosen is WorldUILayout world)SetWorld(world);else if(chosen is CheckpointUILayout checkpoint)SetCheckpoint(checkpoint);else if(chosen is TooltipHintsLayout hints)SetHints(hints);else if(chosen is InventoryInteractionLayout interaction)SetInteraction(interaction);else if(chosen is BattleSwapLayout swap)SetSwap(swap);else if(chosen is OverheadUILayout overhead)SetOverhead(overhead);else if(chosen is TargetUILayout targets)SetTargets(targets);else if(chosen is ReactionUILayout reactions)SetReactions(reactions);else if(chosen is BattleUILayout battle)SetBattle(battle);else if(chosen is PlayerMenuShellLayout menuShell)SetShell(menuShell);else if(chosen is MerchantPageLayout merchant)SetMerchant(merchant);else if(chosen is SystemPageLayout system)SetSystem(system);else if(chosen is GrowthPageLayout growth)SetGrowth(growth);else if(chosen is AbilitiesPageLayout abilities)SetAbilities(abilities);else if(chosen is InventoryPageLayout inventory)SetInventory(inventory);else if(chosen is PartyPageLayout party)SetParty(party);else if(chosen is DetailsPanelLayout detail)SetLayout(detail);}

        if(!worldLayout&&!checkpointLayout&&!hintsLayout&&!interactionLayout&&!swapLayout&&!overheadLayout&&!targetLayout&&!reactionLayout&&!battleLayout&&!shellLayout&&!merchantLayout&&!systemLayout&&!growthLayout&&!abilitiesLayout&&!partyLayout&&!inventoryLayout)equipment=(EquipmentDefinition)EditorGUILayout.ObjectField("Example equipment",equipment,typeof(EquipmentDefinition),false);
        using(new EditorGUILayout.HorizontalScope()){
            followSelection=GUILayout.Toggle(followSelection,"Follow layout selection");
            live=GUILayout.Toggle(live,"Live updates");
            if(GUILayout.Button("Refresh",GUILayout.Width(65)))dirty=true;
        }
        panelWidth=EditorGUILayout.IntSlider("Panel width",panelWidth,(worldLayout||checkpointLayout||hintsLayout||interactionLayout||swapLayout||overheadLayout||targetLayout||shellLayout||battleLayout||reactionLayout)?960:280,(worldLayout||checkpointLayout||hintsLayout||interactionLayout||swapLayout||overheadLayout||targetLayout||shellLayout||battleLayout||reactionLayout)?1920:800);
        panelHeight=EditorGUILayout.IntSlider("Panel height",panelHeight,(worldLayout||checkpointLayout||hintsLayout||interactionLayout||swapLayout||overheadLayout||targetLayout||shellLayout||battleLayout||reactionLayout)?600:240,(worldLayout||checkpointLayout||hintsLayout||interactionLayout||swapLayout||overheadLayout||targetLayout||shellLayout||battleLayout||reactionLayout)?1080:900);
        if(worldLayout){worldPreview=EditorGUILayout.Popup("World UI",worldPreview,new[]{"Merchant prompt","Blacksmith prompt","Checkpoint prompt","Saving","Autosaving","Save complete","Save error","Victory notice","Encounter reset","Encounter unavailable"});worldIcons=EditorGUILayout.Popup("Prompt icons",worldIcons,new[]{"Keyboard","Xbox","PlayStation"});worldPulseTime=EditorGUILayout.Slider("Pulse time",worldPulseTime,0,2);}
        if(checkpointLayout)checkpointPreview=EditorGUILayout.Popup("Checkpoint",checkpointPreview,new[]{"Rest / Save / Leave","Rest confirmation","Unavailable checkpoint","Save slots","Overwrite confirmation"});
        if(hintsLayout){hintsPreview=EditorGUILayout.Popup("Tooltip / hint",hintsPreview,new[]{"Battle action","Inventory hover","Cursor mode","Free flight","Directional navigation"});hintsLongText=EditorGUILayout.Toggle("Long description",hintsLongText);}
        if(interactionLayout){interactionPreview=EditorGUILayout.Popup("Interaction",interactionPreview,new[]{"Item examination","Potion recipients","Recovery confirmation","Spell learners","Learning confirmation","Equipment recipients","Accessory slots","Equip confirmation","Unequip confirmation"});interactionUnavailable=EditorGUILayout.Toggle("Unavailable / full resource",interactionUnavailable);interactionLongText=EditorGUILayout.Toggle("Long description",interactionLongText);}
        if(swapLayout){swapConfirm=EditorGUILayout.Toggle("Confirmation",swapConfirm);swapKnockedOut=EditorGUILayout.Toggle("Last reserve knocked out",swapKnockedOut);swapCount=EditorGUILayout.IntSlider("Reserve count",swapCount,0,3);swapSelected=EditorGUILayout.IntSlider("Selected reserve",swapSelected,0,2);swapPortrait=(Texture)EditorGUILayout.ObjectField("Sample portrait",swapPortrait,typeof(Texture),false);}
        if(overheadLayout){overheadPreview=EditorGUILayout.Popup("Overhead view",overheadPreview,new[]{"Single character","Party","Defense pair"});overheadName=EditorGUILayout.TextField("Sample name",overheadName);overheadLevel=EditorGUILayout.IntSlider("Level",overheadLevel,1,99);overheadHealth=EditorGUILayout.Slider("Health",overheadHealth,0,1);overheadStatuses=EditorGUILayout.Toggle("Statuses",overheadStatuses);overheadBarrier=EditorGUILayout.Toggle("Barrier",overheadBarrier);}
        if(targetLayout){targetPreview=EditorGUILayout.Popup("Target view",targetPreview,new[]{"Hover / pinned target","Examine","Context menu","Status tooltip"});targetObject=EditorGUILayout.Toggle("Destructible object",targetObject);targetNoStatuses=EditorGUILayout.Toggle("No statuses",targetNoStatuses);targetHealthFraction=EditorGUILayout.Slider("Health",targetHealthFraction,0,1);}
        if(reactionLayout){
            reactionPreview=EditorGUILayout.Popup("Reaction",reactionPreview,new[]{"Dodge / parry","Single press","Fixed sequence","Repeated tap","Hold","Staged prompts","Dual input"});
            reactionProgress=EditorGUILayout.Slider("Progress",reactionProgress,0,1);reactionController=EditorGUILayout.Toggle("Controller icons",reactionController);previewHeld=EditorGUILayout.Toggle("Input held",previewHeld);
            previewInput=(QteButton)EditorGUILayout.EnumPopup("Example input",previewInput);if(reactionPreview==2)sequenceLength=EditorGUILayout.IntSlider("Sequence length",sequenceLength,1,8);
        }
        if(battleLayout){battleShortcutFocus=EditorGUILayout.Toggle("LT shortcut focus",battleShortcutFocus);battleRight=EditorGUILayout.Toggle("Right turn order",battleRight);battleStatuses=EditorGUILayout.Toggle("Party statuses",battleStatuses);battleActionSpent=EditorGUILayout.Toggle("Action spent",battleActionSpent);}
        if(shellLayout)shellPreview=EditorGUILayout.Popup("Shell page",shellPreview,new[]{"Party","Inventory","Abilities","Growth","System","Shop","Blacksmith"});
        if(merchantLayout)merchantPreview=EditorGUILayout.Popup("Merchant view",merchantPreview,new[]{"Inventory workspace","Transaction buttons","Empty category"});
        if(growthLayout)growthDebtPreview=EditorGUILayout.Toggle("Debt tree",growthDebtPreview);
        if(systemLayout)systemPreview=EditorGUILayout.Popup("System view",systemPreview,new[]{"Save / Load","Battle","Graphics","UI","Audio","Confirmation","Save slots"});
        if(abilitiesLayout)abilitiesPreview=EditorGUILayout.Popup("Ability category",abilitiesPreview,new[]{"Attacks","Skills","Spells","Items"});
        if(inventoryLayout)inventoryPreview=EditorGUILayout.Popup("Inventory view",inventoryPreview,new[]{"Grid and filters","Item details","Context menu"});
        var attacks=AvailableAttacks();
        if(!worldLayout&&!checkpointLayout&&!hintsLayout&&!interactionLayout&&!swapLayout&&!overheadLayout&&!targetLayout&&!reactionLayout&&!battleLayout&&!shellLayout&&!merchantLayout&&!systemLayout&&!growthLayout&&!abilitiesLayout&&!inventoryLayout&&!partyLayout&&attacks.Count>1)attackIndex=EditorGUILayout.Popup("Example Attack",Mathf.Clamp(attackIndex,0,attacks.Count-1),attacks.Select(a=>a.Name).ToArray());
        if(EditorGUI.EndChangeCheck()){scroll=0;dirty=true;}
        EditorGUILayout.HelpBox("Edit Mode only. Example values are not your live party. Layouts, prefab styling and Attack patterns use the game's renderers. Edits in Prefab Mode are previewed before saving.",MessageType.Info);
        if(!worldLayout&&!checkpointLayout&&!hintsLayout&&!interactionLayout&&!swapLayout&&!overheadLayout&&!targetLayout&&!reactionLayout&&!battleLayout&&!shellLayout&&!merchantLayout&&!systemLayout&&!growthLayout&&!abilitiesLayout&&!layout&&!partyLayout&&!inventoryLayout){EditorGUILayout.HelpBox("Select a details layout asset to begin.",MessageType.None);return;}
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorGUILayout.HelpBox("Preview pauses during Play Mode.",MessageType.Info);return;}
        if(dirty){
            try{BuildPreview();error=null;}catch(Exception ex){error=ex.Message;DisposePreview();}
            dirty=false;
        }
        if(error!=null){EditorGUILayout.HelpBox(error,MessageType.Error);return;}
        float maxScroll=Mathf.Max(0,contentHeight-panelHeight);
        scroll=EditorGUILayout.Slider("Scroll",scroll,0,maxScroll);
        if(growthLayout&&previewInventoryScroll)horizontalScroll=EditorGUILayout.Slider("Horizontal scroll",horizontalScroll,0,Mathf.Max(0,previewInventoryScroll.content.rect.width-previewInventoryScroll.viewport.rect.width));
        var area=GUILayoutUtility.GetRect(1,10000,80,10000,GUILayout.ExpandWidth(true),GUILayout.ExpandHeight(true));
        float scale=Mathf.Min(area.width/panelWidth,area.height/panelHeight);
        var frame=new Rect(area.x+(area.width-panelWidth*scale)/2,area.y,panelWidth*scale,panelHeight*scale);
        if(Event.current.type==EventType.ScrollWheel&&frame.Contains(Event.current.mousePosition)){scroll=Mathf.Clamp(scroll+Event.current.delta.y*24,0,maxScroll);Event.current.Use();Repaint();}
        if(Event.current.type==EventType.Repaint&&preview!=null){
            PositionPreview();Canvas.ForceUpdateCanvases();
            preview.BeginPreview(new Rect(0,0,panelWidth,panelHeight),GUIStyle.none);
            RefreshPreviewScrollbars();preview.Render(true);
            var texture=preview.EndPreview();GUI.DrawTexture(frame,texture,ScaleMode.StretchToFill,false);
        }
    }
    List<BattleActionOption> AvailableAttacks()=>equipment
        ?(equipment.Attacks??new()).Concat(WeaponMastery.Milestones(equipment).SelectMany(m=>m.Attacks??new())).Where(a=>a!=null).ToList()
        :new List<BattleActionOption>();
    public void BuildPreview()
    {
        DisposePreview();if(!worldLayout&&!checkpointLayout&&!hintsLayout&&!interactionLayout&&!swapLayout&&!overheadLayout&&!targetLayout&&!reactionLayout&&!battleLayout&&!shellLayout&&!merchantLayout&&!systemLayout&&!growthLayout&&!abilitiesLayout&&!layout&&!partyLayout&&!inventoryLayout)return;
        preview=new PreviewRenderUtility();
        // Game camera rendering includes world-space UI; this is still an isolated Edit Mode preview.
        preview.camera.cameraType=CameraType.Game;
        preview.camera.orthographic=true;preview.camera.orthographicSize=panelHeight*.005f;
        preview.camera.transform.position=new Vector3(panelWidth*.005f,-panelHeight*.005f,-10);
        preview.camera.transform.rotation=Quaternion.identity;preview.camera.nearClipPlane=.1f;preview.camera.farClipPlane=30;
        preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.035f,.075f,.12f);
        canvasRoot=new GameObject("Details preview (temporary)",typeof(RectTransform),typeof(Canvas));
        preview.AddSingleGO(canvasRoot);
        var canvas=canvasRoot.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=preview.camera;
        var root=(RectTransform)canvasRoot.transform;root.pivot=new Vector2(0,1);root.sizeDelta=new Vector2(panelWidth,panelHeight);root.localScale=Vector3.one*.01f;
        var viewport=Rect("Preview viewport",root,panelWidth,panelHeight);viewport.gameObject.AddComponent<RectMask2D>();
        content=Rect("Content",viewport,panelWidth,panelHeight);
        if(worldLayout){BuildWorldPreview();VisibleScrollbars.EnsureAll(canvasRoot);Canvas.ForceUpdateCanvases();foreach(var graphic in canvasRoot.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();Canvas.ForceUpdateCanvases();return;}
        if(checkpointLayout){BuildCheckpointPreview();VisibleScrollbars.EnsureAll(canvasRoot);Canvas.ForceUpdateCanvases();foreach(var graphic in canvasRoot.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();Canvas.ForceUpdateCanvases();return;}
        if(hintsLayout){BuildHintsPreview();VisibleScrollbars.EnsureAll(canvasRoot);Canvas.ForceUpdateCanvases();foreach(var graphic in canvasRoot.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();Canvas.ForceUpdateCanvases();return;}
        if(interactionLayout){BuildInteractionPreview();VisibleScrollbars.EnsureAll(canvasRoot);Canvas.ForceUpdateCanvases();foreach(var graphic in canvasRoot.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();Canvas.ForceUpdateCanvases();return;}
        if(swapLayout){BuildSwapPreview();VisibleScrollbars.EnsureAll(canvasRoot);Canvas.ForceUpdateCanvases();foreach(var graphic in canvasRoot.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();Canvas.ForceUpdateCanvases();return;}
        if(overheadLayout){BuildOverheadPreview();VisibleScrollbars.EnsureAll(canvasRoot);Canvas.ForceUpdateCanvases();foreach(var graphic in canvasRoot.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();Canvas.ForceUpdateCanvases();return;}
        if(targetLayout){BuildTargetPreview();VisibleScrollbars.EnsureAll(canvasRoot);Canvas.ForceUpdateCanvases();foreach(var graphic in canvasRoot.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();Canvas.ForceUpdateCanvases();return;}
        if(reactionLayout){BuildReactionPreview();VisibleScrollbars.EnsureAll(canvasRoot);Canvas.ForceUpdateCanvases();foreach(var graphic in canvasRoot.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();Canvas.ForceUpdateCanvases();return;}
        if(battleLayout){BuildBattlePreview();VisibleScrollbars.EnsureAll(canvasRoot);Canvas.ForceUpdateCanvases();foreach(var graphic in canvasRoot.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();Canvas.ForceUpdateCanvases();return;}
        if(shellLayout){BuildShellPreview();VisibleScrollbars.EnsureAll(canvasRoot);Canvas.ForceUpdateCanvases();foreach(var graphic in canvasRoot.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();Canvas.ForceUpdateCanvases();return;}
        if(merchantLayout){BuildMerchantPreview();VisibleScrollbars.EnsureAll(canvasRoot);Canvas.ForceUpdateCanvases();foreach(var graphic in canvasRoot.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();Canvas.ForceUpdateCanvases();return;}
        if(systemLayout){BuildSystemPreview();VisibleScrollbars.EnsureAll(canvasRoot);Canvas.ForceUpdateCanvases();foreach(var graphic in canvasRoot.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();Canvas.ForceUpdateCanvases();return;}
        if(growthLayout){BuildGrowthPreview();VisibleScrollbars.EnsureAll(canvasRoot);Canvas.ForceUpdateCanvases();foreach(var graphic in canvasRoot.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();Canvas.ForceUpdateCanvases();return;}
        if(abilitiesLayout){BuildAbilitiesPreview();VisibleScrollbars.EnsureAll(canvasRoot);Canvas.ForceUpdateCanvases();foreach(var graphic in canvasRoot.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();Canvas.ForceUpdateCanvases();return;}
        if(inventoryLayout){BuildInventoryPreview();VisibleScrollbars.EnsureAll(canvasRoot);Canvas.ForceUpdateCanvases();foreach(var graphic in canvasRoot.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();Canvas.ForceUpdateCanvases();return;}
        if(partyLayout){BuildPartyPreview();VisibleScrollbars.EnsureAll(canvasRoot);Canvas.ForceUpdateCanvases();foreach(var graphic in canvasRoot.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();Canvas.ForceUpdateCanvases();foreach(var text in canvasRoot.GetComponentsInChildren<TMP_Text>())text.ForceMeshUpdate();return;}
        var rows=new List<DetailsPanelComposer.Row>();
        void Text(DetailSection binding,string value){
            if(string.IsNullOrWhiteSpace(value))return;
            var rect=Rect(binding.ToString(),content,panelWidth-24,30);
            var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.font=TMP_Settings.defaultFontAsset;text.fontSize=16;text.text=value;text.raycastTarget=false;
            rect.sizeDelta=new Vector2(panelWidth-24,Mathf.Max(24,text.GetPreferredValues(value,panelWidth-24,0).y));
            rows.Add(new DetailsPanelComposer.Row(binding,rect));
        }
        var attacks=AvailableAttacks();
        var action=attacks.Count>0?attacks[Mathf.Clamp(attackIndex,0,attacks.Count-1)]:new BattleActionOption{Name="Sweeping Blow",Description="Sweep across nearby targets.",Reach=1,Radius=1,Damage=25,Category=BattleActionCategory.Attack};
        Text(DetailSection.Name,equipment?equipment.DisplayName:action.Name);
        var art=equipment?equipment.Image:action.Image;
        if(art){var picture=Rect("Artwork",content,112,112);var image=picture.gameObject.AddComponent<RawImage>();image.texture=art;rows.Add(new DetailsPanelComposer.Row(DetailSection.Artwork,picture));}
        Text(DetailSection.Description,equipment?equipment.Description:"A sample entry with enough text to preview wrapping and spacing.");
        Text(DetailSection.Availability,"Example character · Level 10");
        Text(DetailSection.Price,"50 coins");
        Text(DetailSection.Upgrade,"Upgrade 0 / 3 → 1");
        Text(DetailSection.Materials,"MATERIALS · required / owned\nIron: 2 / 6\nHide: 1 / 3");
        Text(DetailSection.Stats,"Strength: 10 → 12\nMagic: 8 → 8\nPhysical Defense: 6 → 7");
        Text(DetailSection.Mastery,"Mastery level 1 · XP 4 / 10");
        if(equipment)foreach(var milestone in WeaponMastery.Milestones(equipment))Text(DetailSection.Mastery,milestone.Name+" · "+milestone.RequiredExperience+" XP\n"+milestone.Description);
        var source=new GridUnit("Preview",Vector2Int.zero){Strength=10,Magic=10,Soul=10};
        Text(DetailSection.Attacks,action.TooltipFor(source));
        var before=content.Cast<Transform>().ToArray();
        DetailsPatternPreview.Draw(content,action,0,panelWidth-24,24);
        foreach(RectTransform child in content.Cast<Transform>().Except(before).ToArray())rows.Add(new DetailsPanelComposer.Row(DetailSection.Attacks,child));
        Text(DetailSection.Notice,"Example notice");
        temporaryLayout=Instantiate(layout);temporaryLayout.hideFlags=HideFlags.HideAndDontSave;
        foreach(var section in temporaryLayout.Sections)if(section?.Prefab)section.Prefab=PreviewPrefab(section.Prefab);
        Canvas.ForceUpdateCanvases();
        DetailsPanelComposer.Compose(content,temporaryLayout,rows);
        Canvas.ForceUpdateCanvases();foreach(var text in canvasRoot.GetComponentsInChildren<TMP_Text>())text.ForceMeshUpdate();contentHeight=content.rect.height;
    }
    static T InventoryTemplate<T>(T prefab)where T:Component {
        if(!prefab)throw new InvalidOperationException("Assign all page template references.");
        var stage=PrefabStageUtility.GetCurrentPrefabStage();
        return stage!=null&&stage.assetPath==AssetDatabase.GetAssetPath(prefab)?stage.prefabContentsRoot.GetComponent<T>()??prefab:prefab;
    }
    void BuildWorldPreview(){
        contentHeight=panelHeight;var l=worldLayout;
        if(worldPreview<3){
            var v=Instantiate(InventoryTemplate(worldPreview==0?l.Merchant:worldPreview==1?l.Blacksmith:l.Checkpoint),content,false);var r=(RectTransform)v.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=v.ScreenOffset;
            var catalog=ControllerGlyphs.Catalog;
            v.Icon.sprite=worldIcons==0?(catalog&&catalog.OverworldKeys!=null&&catalog.OverworldKeys.Length>1?catalog.OverworldKeys[1]:null):(catalog&&catalog.Buttons!=null&&catalog.Buttons.Length>3?(worldIcons==1?catalog.Buttons[3].Xbox:catalog.Buttons[3].PlayStation):null);
            v.Icon.enabled=v.Icon.sprite;if(worldIcons>0&&v.Icon.sprite)ControllerGlyphs.StyleFace(v.Icon);
            v.Caption.text=(v.Icon.sprite?"":worldIcons==0?"E · ":worldIcons==1?"Y · ":"Triangle · ")+(worldPreview==0?"Merchant":worldPreview==1?"Blacksmith":"Checkpoint");
        }else if(worldPreview<7){
            var v=Instantiate(InventoryTemplate(l.SaveIndicator),content,false);v.Present(worldPreview==3?"Saving…":worldPreview==4?"Autosaving…":worldPreview==5?"Game saved.":"Save failed. Please try again.",worldPreview<5,worldPulseTime);
        }else{var v=Instantiate(InventoryTemplate(l.EncounterNotice),content,false);v.Message.text=worldPreview==7?"Victory: 120 XP for participants; half for unused reserves.":worldPreview==8?"Back in the overworld. Encounter reset; re-entry cooldown active.":"Unable to start this encounter here.";}
    }
    void BuildCheckpointPreview(){
        contentHeight=panelHeight;
        var left=Rect("Menu area",content,590,562);left.anchoredPosition=new Vector2(40,-60);
        var right=Rect("Details area",content,426,562);right.anchoredPosition=new Vector2(680,-60);
        if(checkpointPreview>=3){
            var saves=Resources.Load<SystemPageLayout>("UI/System/System");
            var panel=InventoryModularUI.Fill(InventoryTemplate(checkpointPreview==3?saves.SaveSlots:saves.Confirmation),left);
            foreach(var control in panel.GetComponentsInChildren<SystemControlView>(true)){
                if(!control.Text)continue;
                if(control.Key=="Save title")control.Text.text="MANUAL SAVE";
                if(control.Key.Split('|').Contains("Save confirmation"))control.Text.text="Overwrite this manual slot with your current progress?";
                if(control.Key.Contains("Confirm overwrite"))control.Text.text="Confirm overwrite";
                if(control.Key.Contains("Cancel save/load"))control.Text.text="Cancel";
            }
            if(checkpointPreview==3){
                for(int i=0;i<20;i++){var slot=Instantiate(InventoryTemplate(saves.SaveSlot),panel.Scroll.content,false);slot.Name.text="Save slot "+(i+1);slot.Details.text=i<3?"Example captain · Level 10":"Empty";}
                Canvas.ForceUpdateCanvases();AbilitiesModularUI.SizeList(panel.Scroll);previewInventoryScroll=panel.Scroll;
                contentHeight=panelHeight+Mathf.Max(0,panel.Scroll.content.rect.height-panel.Scroll.viewport.rect.height);
            }
            var info=InventoryModularUI.Fill(InventoryTemplate(checkpointLayout.Details),right);info.Title.text=checkpointPreview==3?"Save Game":"Confirmation";info.Description.text=checkpointPreview==3?"Twenty manual save slots.":"Replace the selected manual save with your current progress.";info.Notice.text="";
            return;
        }
        var view=InventoryModularUI.Fill(InventoryTemplate(checkpointLayout.Menu),left);view.Present("Wayside checkpoint",checkpointPreview==1,checkpointPreview!=2);
        var details=InventoryModularUI.Fill(InventoryTemplate(checkpointLayout.Details),right);details.Title.text="Wayside checkpoint";
        details.Description.text=checkpointPreview==2?"This checkpoint is no longer available.":"Rest restores HP and MP for the whole party, including reserves and knocked-out members.\n\nRoaming groups return. One-off and story encounters stay cleared.";
        details.Notice.text=checkpointPreview==2?"":"Example: checkpoint autosave updated.";
    }
    void BuildHintsPreview(){
        contentHeight=panelHeight;
        if(hintsPreview<2){
            var v=Instantiate(InventoryTemplate(hintsPreview==0?hintsLayout.BattleAction:hintsLayout.InventoryHover),content,false);
            var action=new BattleActionOption{Name="Sweeping Blow",Description="Sweep across nearby targets.",Reach=1,Damage=25,Category=BattleActionCategory.Attack};
            v.Text.text=hintsPreview==0?action.TooltipFor(new GridUnit("Preview",Vector2Int.zero){Strength=12}):"<b>Health Potion</b>\nRestores 30 HP to one party member.";
            if(hintsLongText)v.Text.text+=string.Concat(Enumerable.Repeat("\n\nAdditional example details to inspect wrapping and scrolling.",12));
            v.Fit(new Vector2(panelWidth-230,panelHeight-155));previewInventoryScroll=v.Scroll;Canvas.ForceUpdateCanvases();contentHeight=panelHeight+Mathf.Max(0,v.Scroll.content.rect.height-v.Scroll.viewport.rect.height);
        }else{
            var guide=Instantiate(InventoryTemplate(hintsLayout.Guide),content,false);guide.Present(hintsPreview-2,"Turn order");
            if(hintsPreview==2){var cursor=Instantiate(InventoryTemplate(hintsLayout.Cursor),content,false);cursor.anchorMin=cursor.anchorMax=new Vector2(.5f,.5f);cursor.anchoredPosition=Vector2.zero;}
        }
    }
    void BuildInteractionPreview(){
        contentHeight=panelHeight;var l=interactionLayout;
        var shade=Instantiate(InventoryTemplate(l.Overlay),content,false);
        int mode=interactionPreview;int step=mode==0?1:mode==1||mode==3?2:mode==2||mode==4?3:mode==5?4:mode==6?5:6;
        var v=Instantiate(InventoryTemplate(l.For(step,mode==4)),shade,false);
        v.Title.text=mode<=2?"Health Potion":mode<=4?"Fireball I spell book":mode==6?"Iron Ring":"Iron Sword";
        bool list=step==2||step==4||step==5;
        v.Lead.text=list?(step==5?"Prototype Balanced · Choose an accessory slot":"Choose a party member"):"";v.Lead.gameObject.SetActive(list);
        v.Choices.gameObject.SetActive(list);v.Description.gameObject.SetActive(!list);v.Notice.gameObject.SetActive(false);
        if(list)for(int i=0;i<(step==5?2:6);i++){
            bool disabled=interactionUnavailable&&i==1;
            string label=step==5?"Accessory "+(i+1)+": "+(i==0?"Bronze Ring":"Empty"):"Party member "+(i+1)+(mode==3?(disabled?" · Already learned":" · Learn spell"):mode==5?(disabled?" · incompatible":""):"   45 / 100   (+30)");
            var row=InventoryModularUI.Choice(InventoryTemplate(l.Recipient),v.Choices,label);row.Select.interactable=!disabled;
        }
        v.Description.text=mode==0?"Restores 30 HP to one party member.":mode==2?(interactionUnavailable?"Prototype Balanced\nHP: 100 / 100 → 100 / 100\nRecovers 0 HP":"Prototype Balanced\nHP: 45 / 100 → 75 / 100\nRecovers 30 HP")+"\nConsumes 1 Health Potion (3 carried)":mode==4?"Prototype Balanced will learn Fireball I.\nConsumes 1 Fireball I spell book (1 carried)":"Prototype Balanced · Weapon\n"+(mode==8?"Iron Sword → Empty":"Bronze Sword → Iron Sword")+"\n\nVitality: 10 → 10\nStrength: 12 → <color=#80E3AC>15</color>\nMagic: 8 → 8\nSoul: 10 → 10\nPhysical Defense: 6 → 6\nMagical Defense: 6 → 6\nInitiative: 8 → 8";
        if(interactionLongText)v.Description.text+=string.Concat(Enumerable.Repeat("\n\nAdditional example details to inspect wrapping and scrolling in the authored window.",8));
        v.Confirm.gameObject.SetActive(step==3||step==6);v.Confirm.Label.text=mode==2?"Confirm use":mode==4?"Confirm learning":mode==8?"Confirm unequip":"Confirm equip";v.Confirm.Select.interactable=!interactionUnavailable;v.Back.Label.text="Back";
        v.SizeContent();previewInventoryScroll=v.Scroll;contentHeight=panelHeight+Mathf.Max(0,v.Scroll.content.rect.height-v.Scroll.viewport.rect.height);
    }
    void BuildSwapPreview(){
        contentHeight=panelHeight;var view=Instantiate(InventoryTemplate(swapLayout.Window),content,false);
        var template=InventoryTemplate(swapLayout.Portrait);
        BattleSwapCardView Card(RectTransform host,string name,int hp,bool available,bool selected){
            var v=Instantiate(template,host,false);var r=(RectTransform)v.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;v.Present(name,swapPortrait,hp,100,available,selected,swapLayout);return v;
        }
        var current=Card(view.Current,"Prototype Balanced",65,true,false);current.Button.interactable=false;
        int count=Mathf.Min(swapCount,view.Reserves.Length);
        for(int i=0;i<view.Reserves.Length;i++){view.Reserves[i].gameObject.SetActive(i<count);if(i<count)Card(view.Reserves[i],"Reserve "+(i+1),swapKnockedOut&&i==count-1?0:80,!(swapKnockedOut&&i==count-1),i==swapSelected);}
        bool confirm=swapConfirm&&swapSelected<count&&!(swapKnockedOut&&swapSelected==count-1);view.Confirm.gameObject.SetActive(confirm);
        view.Prompt.text=confirm?"Swap Prototype Balanced for Reserve "+(swapSelected+1)+"?":"Choose a reserve";
    }
    void BuildOverheadPreview(){
        contentHeight=panelHeight;
        var template=InventoryTemplate(overheadLayout.Character);
        var statusStyle=template.Statuses?template.Statuses.Layout:null;if(!statusStyle)statusStyle=Resources.Load<TargetUILayout>("UI/Targets/Targets");
        if(statusStyle){previewTargetLayout=Instantiate(statusStyle);previewTargetLayout.hideFlags=HideFlags.HideAndDontSave;previewTargetLayout.StatusBadge=InventoryTemplate(statusStyle.StatusBadge);}
        int count=overheadPreview==0?1:overheadPreview==1?3:2;
        for(int i=0;i<count;i++){
            var v=Instantiate(template,content,false);var r=(RectTransform)v.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2((i-(count-1)*.5f)*250,0);r.localScale=Vector3.one;v.gameObject.SetActive(true);
            if(v.Statuses)v.Statuses.Layout=previewTargetLayout;v.Initialize(TMP_Settings.defaultFontAsset);
            var unit=new GridUnit("Preview",Vector2Int.zero);unit.Health=Mathf.Max(1,Mathf.RoundToInt(unit.MaxHealth*overheadHealth));
            if(overheadStatuses){unit.ApplyStatus(CombatStatus.Burning,3);unit.ApplyStatus(CombatStatus.Regeneration,2);unit.ApplyStatus(CombatStatus.Weakened,2);}
            if(overheadBarrier)unit.AddBarrier(30);
            v.Present(i==0?overheadName:overheadPreview==2?"Enemy attacker":"Party member "+(i+1),overheadLevel,unit);
        }
    }
    void BuildTargetPreview(){
        contentHeight=panelHeight;previewTargetLayout=Instantiate(targetLayout);previewTargetLayout.hideFlags=HideFlags.HideAndDontSave;
        previewTargetLayout.StatusBadge=InventoryTemplate(targetLayout.StatusBadge);previewTargetLayout.StatusTooltip=InventoryTemplate(targetLayout.StatusTooltip);
        var unit=new GridUnit("Preview",Vector2Int.zero);unit.Health=Mathf.Max(1,Mathf.RoundToInt(unit.MaxHealth*targetHealthFraction));
        if(!targetNoStatuses){unit.ApplyStatus(CombatStatus.Burning,3);if(!targetObject){unit.ApplyStatus(CombatStatus.Regeneration,2);unit.AddBarrier(30);}}
        BattleStatusRow row=null;
        if(targetPreview==0||targetPreview==3){
            var v=Instantiate(InventoryTemplate(targetLayout.Target),content,false);v.gameObject.SetActive(true);
            v.Name.text=targetObject?"Wooden crate":"Prototype Balanced · Lv 10 · Human";v.Health.text=unit.Health+" / "+unit.MaxHealth;v.HealthFill.anchorMax=new Vector2(targetHealthFraction,1);
            v.Mana.gameObject.SetActive(!targetObject);v.ManaText.text="MP 16 / 24";v.ManaFill.anchorMax=new Vector2(2f/3,1);
            v.Shield.gameObject.SetActive(!targetObject&&!targetNoStatuses);v.ShieldText.text="30 / 30";v.ShieldFill.anchorMax=Vector2.one;
            v.Resources.text=targetObject?"":"AP 4 / 9 · Movement 3 / 10";v.DamagePreview.text="";row=v.Statuses;
        }else{
            var v=Instantiate(InventoryTemplate(targetLayout.Inspection),content,false);v.gameObject.SetActive(true);v.Context.gameObject.SetActive(targetPreview==2);v.Panel.gameObject.SetActive(targetPreview==1);
            v.Use.gameObject.SetActive(targetObject);
            v.Details.text=targetObject?"<b>Wooden crate</b>\nHealth 65 / 100\n\nDestructible object":"<b>Prototype Balanced</b>\nLevel 10 · Human · All-rounder\n\nHealth 65 / 100\nMP 16 / 24\nAP 4 / 9\nMovement 3 / 10\n\n<b>Attributes</b>\nVitality 10\nStrength 12\nMagic 8\nSoul 10\nPhysical Defense 6\nMagical Defense 6\nInitiative 8\n\n<b>Resistances</b>\nFire 0%\nLightning 0%\nIce 0%";
            v.Content.sizeDelta=new Vector2(0,v.Details.GetPreferredValues(v.Details.text,Mathf.Max(100,v.Panel.rect.width-50),0).y+16);row=v.Statuses;
            previewInventoryScroll=v.Panel.GetComponentInChildren<ScrollRect>();
            Canvas.ForceUpdateCanvases();if(targetPreview==1)contentHeight=panelHeight+Mathf.Max(0,v.Content.rect.height-previewInventoryScroll.viewport.rect.height);
        }
        row.Layout=previewTargetLayout;row.Initialize(content,TMP_Settings.defaultFontAsset);row.Show(unit,targetObject);Canvas.ForceUpdateCanvases();
        if(targetPreview==3&&!targetNoStatuses){var hover=row.GetComponentInChildren<StatusBadgeHover>();if(hover)hover.Enter?.Invoke();}
    }
    void BuildReactionPreview(){
        contentHeight=panelHeight;
        var l=reactionLayout;
        if(reactionPreview==0){
            var d=Instantiate(InventoryTemplate(l.Defense),content,false);d.gameObject.SetActive(true);
            var hit=new DefenseImpact(1.4f,.3f,.12f);hit.Advance(reactionProgress*hit.Duration/hit.ImpactPosition);
            d.Caption.text="Example attack · Hit 1 / 3";d.Prompt.text=reactionController?"B: Dodge   |   RB: Parry":"Space: Dodge   |   R: Parry";d.Result.text="Example timing";
            d.Dodge.anchorMin=new Vector2(hit.BarPosition(hit.Duration-hit.DodgeWindow),0);d.Dodge.anchorMax=new Vector2(hit.ImpactPosition,1);d.Dodge.GetComponent<Image>().color=l.Dodge;
            d.Parry.anchorMin=new Vector2(hit.BarPosition(hit.Duration-hit.ParryWindow),0);d.Parry.anchorMax=new Vector2(hit.ImpactPosition,1);d.Parry.GetComponent<Image>().color=l.Parry;
            d.Marker.anchorMin=new Vector2(hit.BarPosition(hit.DisplayTime),0);d.Marker.anchorMax=new Vector2(hit.BarPosition(hit.DisplayTime),1);d.Marker.GetComponent<Image>().color=l.Waiting;return;
        }
        var v=InventoryModularUI.Fill(InventoryTemplate(l.Qte),content);v.gameObject.SetActive(true);
        bool sequence=reactionPreview==2,ring=reactionPreview==3||reactionPreview==4,hold=reactionPreview==4,dual=reactionPreview==6;
        var input=dual?QteButton.StickRight:previewInput;
        var lane=sequence?QteLane.Bottom:input>=QteButton.StickUp||(reactionController&&input>=QteButton.DpadUp)?QteLane.Left:!reactionController&&input==QteButton.South?QteLane.Bottom:QteLane.Right;
        var marker=v.Lane(lane);v.Prompt.anchorMin=v.Prompt.anchorMax=marker.anchorMin;v.Prompt.anchoredPosition=marker.anchoredPosition;
        v.Prompt.sizeDelta=reactionController&&input>=QteButton.StickUp&&!sequence?l.StickPromptSize:sequence?new Vector2(Mathf.Min(panelWidth-24,sequenceLength*l.SequenceSpacing),l.PromptSize.y):ring?l.RingPromptSize:l.PromptSize;
        v.Icon.sprite=ControllerGlyphs.Face(input,reactionController,out var tint);v.Icon.color=tint;ControllerGlyphs.StyleQte(v.Icon,input,reactionController);v.Icon.enabled=!sequence&&v.Icon.sprite;v.Icon.rectTransform.localScale=Vector3.one*(previewHeld?l.PressedScale:1);v.Key.text=v.Icon.enabled||sequence?"":input.ToString();
        v.Caption.text=reactionPreview==5?"Prompt 2 / 3":sequence?"2 / "+sequenceLength:hold?"Hold":ring?"Tap repeatedly":dual?"Hold both inputs":"Single press";v.Countdown.text=((1-reactionProgress)*3).ToString("0.00")+"s";
        float half=(1-reactionProgress)*.5f;v.Fill.anchorMin=new Vector2(.5f-half,0);v.Fill.anchorMax=new Vector2(.5f+half,1);v.Fill.GetComponent<Image>().color=l.Countdown;
        v.Ring.gameObject.SetActive(ring);v.Triangle.gameObject.SetActive(ring);
        if(ring){Canvas.ForceUpdateCanvases();float w=v.Icon.rectTransform.rect.width,h=v.Icon.rectTransform.rect.height;float aspect=v.Icon.sprite?v.Icon.sprite.rect.width/v.Icon.sprite.rect.height:1;if(w/h>aspect)w=h*aspect;else h=w/aspect;v.Ring.FitToIcon(Mathf.Max(w,h)*.5f);v.Ring.Progress=reactionProgress;v.Triangle.Present(hold,v.Ring.OuterRadius,reactionProgress*3,true);}
        if(sequence){float spacing=Mathf.Min(l.SequenceSpacing,v.Prompt.sizeDelta.x/sequenceLength);for(int i=0;i<sequenceLength;i++){var step=Instantiate(InventoryTemplate(l.SequenceStep),v.Prompt,false);var r=(RectTransform)step.transform;r.anchoredPosition=new Vector2((i-(sequenceLength-1)*.5f)*spacing,0);r.sizeDelta=new Vector2(Mathf.Min(r.sizeDelta.x,spacing-2),r.sizeDelta.y);var button=(QteButton)(i%8);step.Icon.sprite=ControllerGlyphs.Face(button,reactionController,out tint);step.Icon.color=tint;ControllerGlyphs.StyleQte(step.Icon,button,reactionController);step.Progress.color=i<2?l.Success:i==2?l.Countdown:Color.gray;}}
        v.Secondary.gameObject.SetActive(dual);if(dual){var second=v.Lane(reactionController?QteLane.Right:QteLane.Bottom);v.Secondary.anchorMin=v.Secondary.anchorMax=second.anchorMin;v.Secondary.anchoredPosition=second.anchoredPosition;v.SecondaryIcon.sprite=ControllerGlyphs.Face(QteButton.South,reactionController,out tint);v.SecondaryIcon.color=tint;ControllerGlyphs.StyleQte(v.SecondaryIcon,QteButton.South,reactionController);v.SecondaryIcon.rectTransform.localScale=Vector3.one*(previewHeld?l.PressedScale:1);}
    }
    void BuildBattlePreview(){
        var l=Instantiate(battleLayout);
        try{
            l.Commands=InventoryTemplate(l.Commands);l.CommandButton=InventoryTemplate(l.CommandButton);l.TopOrder=InventoryTemplate(l.TopOrder);l.RightOrder=InventoryTemplate(l.RightOrder);l.Portrait=InventoryTemplate(l.Portrait);l.PartyStack=InventoryTemplate(l.PartyStack);l.ResourceCard=InventoryTemplate(l.ResourceCard);
            Image previewDimmer=battleShortcutFocus&&!battleActionSpent&&l.ShortcutDimmer?Instantiate(InventoryTemplate(l.ShortcutDimmer),content,false):null;
            var commands=Instantiate(l.Commands,content,false);
            foreach(var name in new[]{"Attacks","Skills","Spells","Block","Items","Swap","Run","End Turn"}){var b=Instantiate(l.CommandButton,commands.Scroll.content,false);b.Configure(name,()=>{},false);}
            commands.Counter.text="2 / 3 targets selected";
            LayoutRebuilder.ForceRebuildLayoutImmediate(commands.Scroll.content);
            ((RectTransform)commands.Scroll.transform).sizeDelta=new Vector2(((RectTransform)l.Commands.Scroll.transform).sizeDelta.x,Mathf.Min(((RectTransform)l.Commands.Scroll.transform).sizeDelta.y,LayoutUtility.GetPreferredHeight(commands.Scroll.content)));
            ((RectTransform)commands.Counter.transform.parent).anchoredPosition=new Vector2(0,4+((RectTransform)commands.Scroll.transform).sizeDelta.y);
            if(battleActionSpent){commands.gameObject.SetActive(false);var end=Instantiate(l.CommandButton,content,false);end.Configure("End Turn",()=>{},false);BattleUIArrangement.Copy(InventoryTemplate(l.RemainingTurn),(RectTransform)end.transform);}
            var order=Instantiate(battleRight?l.RightOrder:l.TopOrder,content,false);var rects=new List<RectTransform>();
            for(int i=0;i<6;i++){var c=Instantiate(l.Portrait,order.content,false);rects.Add((RectTransform)c.transform);c.Portrait.color=i<3?new Color(.15f,.4f,.5f):new Color(.45f,.2f,.16f);c.Health.text="80 / 100";c.HealthFill.anchorMax=new Vector2(.8f,1);c.Shield.gameObject.SetActive(i==0);c.ShieldText.text="20 / 30";c.ShieldFill.anchorMax=new Vector2(.67f,1);c.Hits.text="";if(i==0)c.Border.color=new Color(1,.75f,.25f);}
            BattleUIArrangement.Order(l,order,rects,6,!battleRight);
            var stack=Instantiate(l.PartyStack,content,false);float bottom=0;
            for(int i=2;i>=0;i--){var c=Instantiate(l.ResourceCard,stack,false);BattleUIArrangement.Resource(l,c,i==0,battleStatuses,bottom);bottom+=((RectTransform)c.transform).sizeDelta.y+l.PartySpacing;c.Name.text="Example member "+(i+1)+" · Lv 10";c.HealthText.text="HP 80 / 100";c.ManaText.text="MP 18 / 24";c.Health.anchorMax=new Vector2(.8f,1);c.Mana.anchorMax=new Vector2(.75f,1);c.Resources.text="AP 4 / 9    Move 3 / 10";
                if(battleStatuses){var badge=Rect("Example Burning",c.Statuses,c.StatusIconSize,c.StatusIconSize);badge.anchorMin=badge.anchorMax=badge.pivot=new Vector2(.5f,.5f);badge.gameObject.AddComponent<StatusBadgeGraphic>().Symbol=0;}
            }
            if(previewDimmer){previewDimmer.transform.SetAsLastSibling();commands.transform.SetAsLastSibling();}
            contentHeight=panelHeight;
        }finally{DestroyImmediate(l);}
    }
    void BuildShellPreview(){
        var template=InventoryTemplate(shellLayout.Prefab);var view=InventoryModularUI.Fill(template,content);
        var stage=PrefabStageUtility.GetCurrentPrefabStage();
        if(stage!=null){
            var selector=stage.prefabContentsRoot.GetComponent<PlayerMenuCharacterView>();
            if(selector){var old=view.Character;view.Character=ReplaceWidget(old,selector);DestroyImmediate(old.gameObject);}
            var command=stage.prefabContentsRoot.GetComponent<PlayerMenuCommandView>();
            if(command)foreach(var old in view.GetComponentsInChildren<PlayerMenuCommandView>(true)){
                var replacement=ReplaceWidget(old,command);replacement.Page=old.Page;replacement.name=old.name;replacement.Button.Label.text=old.Button.Label.text;DestroyImmediate(old.gameObject);
            }
        }
        string[] names={"PARTY","INVENTORY","ABILITIES","GROWTH","SYSTEM","SHOP","BLACKSMITH"};
        var pages=new[]{ProgressionMenu.Page.Party,ProgressionMenu.Page.Inventory,ProgressionMenu.Page.Abilities,ProgressionMenu.Page.SkillTree,ProgressionMenu.Page.System};
        view.Title.text=names[Mathf.Clamp(shellPreview,0,6)];view.Character.Name.text="Prototype All-Rounder";
        PlayerMenuShellPresentation.Apply(view,template,shellPreview>=5,shellPreview!=4&&shellPreview!=6);
        foreach(var command in view.GetComponentsInChildren<PlayerMenuCommandView>(true))if(shellPreview<5&&command.Page==pages[shellPreview])command.Button.Select.image.color=shellLayout.SelectedPageColor;
        foreach(var region in new[]{view.Body,view.Details}){
            var image=region.gameObject.AddComponent<Image>();image.color=new Color(.08f,.16f,.22f,.8f);image.raycastTarget=false;
            var text=Rect("Preview region label",region,region.rect.width,40).gameObject.AddComponent<TextMeshProUGUI>();text.font=TMP_Settings.defaultFontAsset;text.fontSize=18;text.text=region==view.Body?"Page workspace":"Details panel";text.raycastTarget=false;
        }
        contentHeight=panelHeight;
    }
    void BuildMerchantPreview(){
        contentHeight=panelHeight;
        if(merchantPreview==1){
            if(!merchantLayout.Blacksmith&&merchantLayout.AttackDetails){var attack=Instantiate(InventoryTemplate(merchantLayout.AttackDetails),content,false);attack.Label.text="Attack details";((RectTransform)attack.transform).anchoredPosition=new Vector2(12,-12);}
            var action=Instantiate(InventoryTemplate(merchantLayout.Action),content,false);action.Label.text=merchantLayout.Blacksmith?"Upgrade · 50 coins":"Buy · 50 coins";
            var cancel=Instantiate(InventoryTemplate(merchantLayout.Cancel),content,false);cancel.Label.text=merchantLayout.Blacksmith?"Cancel upgrade":"Cancel trade";return;
        }
        var view=InventoryModularUI.Fill(InventoryTemplate(merchantLayout.Workspace),content);
        if(!merchantLayout.Blacksmith)foreach(var name in new[]{"Buy","Sell"}){var mode=InventoryModularUI.Choice(InventoryTemplate(merchantLayout.Mode),view.Modes,name);if(name=="Buy")mode.Select.image.color=merchantLayout.SelectedColor;}
        foreach(var name in merchantLayout.Blacksmith?new[]{"All","Weapons","Armor","Accessories"}:new[]{"All","Weapons","Armor","Accessories","Items"}){
            var filter=InventoryModularUI.Choice(InventoryTemplate(merchantLayout.Filter),view.Filters,name);if(name=="All")filter.Select.image.color=merchantLayout.SelectedColor;
        }
        int count=merchantPreview==2?0:30;view.Empty.gameObject.SetActive(count==0);
        for(int i=0;i<count;i++){
            var slot=Instantiate(InventoryTemplate(merchantLayout.Slot),view.Scroll.content,false);slot.Caption.text=(20+i*5)+" coins";
            var icon=slot.Artwork.gameObject.AddComponent<InventorySlotIcon>();icon.Kind=new[]{"Weapons","Armor","Accessories","Consumables"}[i%(merchantLayout.Blacksmith?3:4)];icon.raycastTarget=false;
            if(i==0)slot.Select.image.color=merchantLayout.SelectedColor;
        }
        Canvas.ForceUpdateCanvases();InventoryModularUI.SizeGrid(view.Grid,count,view.Scroll.viewport.rect.width);previewInventoryScroll=view.Scroll;
        contentHeight=panelHeight+Mathf.Max(0,view.Scroll.content.rect.height-view.Scroll.viewport.rect.height);
    }
    void BuildSystemPreview(){
        var workspace=InventoryModularUI.Fill(InventoryTemplate(systemLayout.Workspace),content);
        var labels=new[]{"Save / Load","Battle","Graphics","UI","Audio"};
        for(int i=0;i<labels.Length;i++){var tab=InventoryModularUI.Choice(InventoryTemplate(systemLayout.Tab),workspace.Tabs,labels[i]);if(i==(systemPreview<5?systemPreview:0))tab.Select.image.color=systemLayout.SelectedColor;}
        var templates=new[]{systemLayout.SaveLoad,systemLayout.Battle,systemLayout.Graphics,systemLayout.UI,systemLayout.Audio,systemLayout.Confirmation,systemLayout.SaveSlots};
        var panel=InventoryModularUI.Fill(InventoryTemplate(templates[Mathf.Clamp(systemPreview,0,6)]),workspace.Content);
        // Authoring preview never installs gameplay callbacks or changes settings/save data.
        if(systemPreview==6){
            for(int i=0;i<20;i++){var slot=Instantiate(InventoryTemplate(systemLayout.SaveSlot),panel.Scroll.content,false);slot.Name.text="Save slot "+(i+1);slot.Details.text=i<3?"Example captain · Level 10":"Empty";}
            Canvas.ForceUpdateCanvases();AbilitiesModularUI.SizeList(panel.Scroll);previewInventoryScroll=panel.Scroll;
            contentHeight=panelHeight+Mathf.Max(0,panel.Scroll.content.rect.height-panel.Scroll.viewport.rect.height);
        }else contentHeight=panelHeight;
    }
    List<MenuSkillNode> DebtPreviewNodes(){
        var stage=PrefabStageUtility.GetCurrentPrefabStage();
        var data=stage!=null?stage.prefabContentsRoot.GetComponent<CharacterMenuData>():null;
        if(!data||!data.DebtTree){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Prototype Debt.prefab");data=prefab?prefab.GetComponent<CharacterMenuData>():null;}
        return data&&data.DebtTree?data.SkillTree:DebtSkillTree.Create();
    }
    void BuildGrowthPreview(){
        var sample=Instantiate(growthLayout);
        try{
            sample.Workspace=InventoryTemplate(sample.Workspace);sample.Node=InventoryTemplate(sample.Node);sample.Connection=InventoryTemplate(sample.Connection);sample.Arrangement=InventoryTemplate(sample.Arrangement);
            var view=InventoryModularUI.Fill(sample.Workspace,content);view.Title.text="EXAMPLE CHARACTER · SKILL TREE";Canvas.ForceUpdateCanvases();
            var nodes=growthDebtPreview?DebtPreviewNodes():sample.Arrangement.Examples();var cards=GrowthModularUI.Build(view,sample,nodes,n=>growthDebtPreview&&n.Starting,n=>false);
            if(cards.Count>0){cards[0].View.Frame.Selected=true;cards[0].View.Frame.SetVerticesDirty();}
            previewInventoryScroll=view.Tree;contentHeight=panelHeight+Mathf.Max(0,view.Tree.content.rect.height-view.Tree.viewport.rect.height);
        }finally{DestroyImmediate(sample);}
    }
    void BuildAbilitiesPreview(){
        var template=InventoryTemplate(abilitiesLayout.Workspace);
        var view=InventoryModularUI.Fill(template,content);
        var categories=new[]{BattleActionCategory.Attack,BattleActionCategory.Skill,BattleActionCategory.Magic,BattleActionCategory.Item};
        var selected=categories[Mathf.Clamp(abilitiesPreview,0,3)];
        foreach(var category in abilitiesLayout.Categories){
            string label=category==BattleActionCategory.Attack?"Attacks":category==BattleActionCategory.Skill?"Skills":category==BattleActionCategory.Magic?"Spells":"Items";
            var choice=InventoryModularUI.Choice(InventoryTemplate(abilitiesLayout.CategoryButton),view.Categories,label);
            if(category==selected)choice.Select.image.color=abilitiesLayout.SelectedColor;
        }
        ((TMP_Text)view.Search.placeholder).text=selected==BattleActionCategory.Magic?"Search spell families or tiers…":"Search abilities…";
        AbilitiesModularUI.FamilyMode(view,template,selected==BattleActionCategory.Magic);
        if(selected==BattleActionCategory.Magic)foreach(var name in new[]{"Fireball","Healing","Lightning","Ice"}){
            var family=InventoryModularUI.Choice(InventoryTemplate(abilitiesLayout.FamilyButton),view.Families.content,name);if(name=="Fireball")family.Select.image.color=abilitiesLayout.SelectedColor;
        }
        var names=selected==BattleActionCategory.Magic?new[]{"Fireball I","Fireball II · Locked","Fireball III · Locked"}:
            selected==BattleActionCategory.Skill?new[]{"Dash","Example Skill · Locked"}:
            selected==BattleActionCategory.Item?new[]{"Health Potion","Mana Potion"}:
            new[]{"Quick Attack","Slash","Sweeping Blow · Locked","Triple Strike · Locked","Example Attack 5","Example Attack 6","Example Attack 7"};
        for(int i=0;i<names.Length;i++){
            var row=Instantiate(InventoryTemplate(abilitiesLayout.AbilityRow),view.Entries.content,false);
            row.Title.text=names[i];row.Source.text=selected==BattleActionCategory.Magic?"Fireball · Mastery level "+(i+1):selected==BattleActionCategory.Item?"Shared inventory":selected==BattleActionCategory.Skill?"Character Skills":"Equipped weapon · Mastery level "+(i+1);
            if(i==0)row.Select.image.color=abilitiesLayout.SelectedColor;
        }
        for(int i=0;i<4;i++){
            var slot=Instantiate(InventoryTemplate(abilitiesLayout.FavoriteSlot),view.Favorites,false);slot.Name.text=i==0?names[0]:"Empty";
            // Use the same control-scheme glyph binding as the live menu.
            slot.gameObject.AddComponent<PlayerShortcutGlyph>().Bind(i,slot.Key,slot.Trigger,slot.Plus,slot.Fallback);
        }
        Canvas.ForceUpdateCanvases();AbilitiesModularUI.SizeList(view.Families);AbilitiesModularUI.SizeList(view.Entries);AbilitiesModularUI.FitFavorites(view.Favorites);
        previewInventoryScroll=view.Entries;contentHeight=panelHeight+Mathf.Max(0,view.Entries.content.rect.height-view.Entries.viewport.rect.height);
    }
    void BuildInventoryPreview()
    {
        if(inventoryPreview==0){
            var view=InventoryModularUI.Fill(InventoryTemplate(inventoryLayout.Workspace),content);Canvas.ForceUpdateCanvases();InventoryModularUI.FitFilters(view);
            foreach(var filter in inventoryLayout.Filters)InventoryModularUI.Choice(InventoryTemplate(inventoryLayout.FilterButton),view.Filters.transform,filter==InventoryFilter.SpellBooks?"Spell Books":filter.ToString());
            for(int i=0;i<28;i++){
                var slot=Instantiate(InventoryTemplate(inventoryLayout.Slot),view.Scroll.content,false);
                slot.Equipped.gameObject.SetActive(i==2);slot.Quantity.text=(i+1).ToString();
                if(i<18){var icon=slot.Icon.gameObject.AddComponent<InventorySlotIcon>();icon.Kind=new[]{"Consumables","Weapons","Armor","Accessories","Materials","Spell Books"}[i%6];icon.Mana=i%2==1;icon.raycastTarget=false;}
                else {slot.Quantity.gameObject.SetActive(false);slot.Select.enabled=false;}
            }
            view.Count.text="18 example stacks / equipment copies";
            InventoryModularUI.SizeGrid(view.Grid,28,view.Scroll.viewport.rect.width);
            // Preview the inner scrolling grid with the window's scroll control.
            previewInventoryScroll=view.Scroll;
            contentHeight=panelHeight+Mathf.Max(0,view.Scroll.content.rect.height-view.Scroll.viewport.rect.height);
        }else if(inventoryPreview==1){
            var view=InventoryModularUI.Fill(InventoryTemplate(inventoryLayout.Details),content);
            view.Name.text="Health Potion";view.Category.text="Consumables";view.Stock.text="Carried: 3";
            view.Description.text="Restore up to 50 HP to a party member.\n\nShared stock: 3";
            var icon=view.Artwork.gameObject.AddComponent<InventorySlotIcon>();icon.Kind="Consumables";icon.raycastTarget=false;
            InventoryModularUI.SizeDescription(view);previewInventoryScroll=view.Scroll;
            contentHeight=panelHeight+Mathf.Max(0,view.Scroll.content.rect.height-view.Scroll.viewport.rect.height);
        }else{
            var view=Instantiate(InventoryTemplate(inventoryLayout.Context),content,false);view.Name.text="Example equipment";
            foreach(var name in new[]{"Examine","Equip","Upgrade details","Back"})InventoryModularUI.Choice(InventoryTemplate(inventoryLayout.ContextButton),view.Choices,name);
            InventoryModularUI.SizeContext(view);((RectTransform)view.transform).anchoredPosition=new Vector2(12,-12);
            contentHeight=Mathf.Max(panelHeight,((RectTransform)view.transform).rect.height+24);
        }
    }
    ScrollRect previewInventoryScroll;
    void PositionPreview(){
        if((worldLayout||checkpointLayout||hintsLayout||interactionLayout||targetLayout||merchantLayout||systemLayout||growthLayout||abilitiesLayout||inventoryLayout)&&previewInventoryScroll){content.anchoredPosition=Vector2.zero;previewInventoryScroll.content.anchoredPosition=new Vector2(growthLayout?-horizontalScroll:0,scroll);}
        else content.anchoredPosition=new Vector2(0,scroll);
    }
    void BuildPartyPreview()
    {
        if(!partyLayout.Prefab)throw new InvalidOperationException("Assign a Party page prefab.");
        var stage=PrefabStageUtility.GetCurrentPrefabStage();
        var template=stage!=null&&stage.assetPath==AssetDatabase.GetAssetPath(partyLayout.Prefab)
            ?stage.prefabContentsRoot.GetComponent<PartyPageView>():partyLayout.Prefab;
        var page=Instantiate(template,content,false);var rect=(RectTransform)page.transform;
        rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(0,1);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(0,Mathf.Max(456,panelHeight));
        // Preview unsaved shared-widget edits while preserving each page instance's data binding.
        if(stage!=null){
            foreach(var old in page.GetComponentsInChildren<PartyMemberCardView>(true))if(stage.prefabContentsRoot.GetComponent<PartyMemberCardView>()){
                var replacement=ReplaceWidget(old,stage.prefabContentsRoot.GetComponent<PartyMemberCardView>());replacement.RosterIndex=old.RosterIndex;DestroyImmediate(old.gameObject);
            }
            foreach(var old in page.GetComponentsInChildren<PartyEquipmentSlotView>(true))if(stage.prefabContentsRoot.GetComponent<PartyEquipmentSlotView>()){
                var replacement=ReplaceWidget(old,stage.prefabContentsRoot.GetComponent<PartyEquipmentSlotView>());replacement.Slot=old.Slot;DestroyImmediate(old.gameObject);
            }
            foreach(var old in page.GetComponentsInChildren<PartyAttributeRowView>(true))if(stage.prefabContentsRoot.GetComponent<PartyAttributeRowView>()){
                var replacement=ReplaceWidget(old,stage.prefabContentsRoot.GetComponent<PartyAttributeRowView>());replacement.Stat=old.Stat;DestroyImmediate(old.gameObject);
            }
        }
        foreach(var card in page.GetComponentsInChildren<PartyMemberCardView>(true)){
            card.Name.text="Example member "+(card.RosterIndex+1);card.Role.text=card.RosterIndex==0?"CAPTAIN":card.RosterIndex<3?"ACTIVE":"RESERVE";
            card.Level.text="Lv 10";card.Vitals.text="HP 120 / 150\nMP 18 / 24";card.Portrait.color=new Color(.15f,.32f,.4f);
        }
        if(page.Character)page.Character.color=new Color(.15f,.32f,.4f);
        foreach(var slot in page.GetComponentsInChildren<PartyEquipmentSlotView>(true)){slot.Caption.text=slot.Slot==EquipmentSlot.AccessoryOne?"Accessory 1":slot.Slot==EquipmentSlot.AccessoryTwo?"Accessory 2":slot.Slot.ToString();slot.Artwork.gameObject.SetActive(false);slot.Empty.gameObject.SetActive(true);}
        foreach(var row in page.GetComponentsInChildren<PartyAttributeRowView>(true)){row.Name.text=AttributePreview.Name(row.Stat);row.Value.text="10";row.Decrease.interactable=false;}
        if(page.Points)page.Points.text="3 ATTRIBUTE POINTS AVAILABLE";
        contentHeight=Mathf.Max(456,panelHeight);content.sizeDelta=new Vector2(content.sizeDelta.x,contentHeight);
    }
    static T ReplaceWidget<T>(T old,T template) where T:Component
    {
        var replacement=Instantiate(template,old.transform.parent,false);replacement.transform.SetSiblingIndex(old.transform.GetSiblingIndex());
        var a=(RectTransform)old.transform;var b=(RectTransform)replacement.transform;
        b.anchorMin=a.anchorMin;b.anchorMax=a.anchorMax;b.pivot=a.pivot;b.anchoredPosition=a.anchoredPosition;b.sizeDelta=a.sizeDelta;
        var source=old.GetComponent<LayoutElement>();if(source){var target=replacement.GetComponent<LayoutElement>();if(!target)target=replacement.gameObject.AddComponent<LayoutElement>();EditorUtility.CopySerialized(source,target);}
        return replacement;
    }
    void RefreshPreviewScrollbars(){
        if(!canvasRoot)return;
        Canvas.ForceUpdateCanvases();
        foreach(var scroll in canvasRoot.GetComponentsInChildren<ScrollRect>(true)){
            if(!scroll.content||!scroll.viewport)continue;
            var size=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,scroll.content).size;
            var frame=scroll.viewport.rect.size;
            if(scroll.verticalScrollbar){scroll.verticalScrollbar.gameObject.SetActive(scroll.vertical&&size.y>frame.y+.5f);scroll.verticalScrollbar.size=Mathf.Clamp01(frame.y/Mathf.Max(1,size.y));scroll.verticalScrollbar.SetValueWithoutNotify(scroll.verticalNormalizedPosition);}
            if(scroll.horizontalScrollbar){scroll.horizontalScrollbar.gameObject.SetActive(scroll.horizontal&&size.x>frame.x+.5f);scroll.horizontalScrollbar.size=Mathf.Clamp01(frame.x/Mathf.Max(1,size.x));scroll.horizontalScrollbar.SetValueWithoutNotify(scroll.horizontalNormalizedPosition);}
        }
        Canvas.ForceUpdateCanvases();
    }
    public Texture2D CapturePreview()
    {
        if(preview==null)BuildPreview();
        if(preview==null)return null;
        PositionPreview();
        preview.BeginStaticPreview(new Rect(0,0,panelWidth,panelHeight));
        Canvas.ForceUpdateCanvases();RefreshPreviewScrollbars();preview.Render(true);
        return preview.EndStaticPreview();
    }
    static RectTransform Rect(string name,Transform parent,float w,float h)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.sizeDelta=new Vector2(w,h);return rect;
    }
    void DisposePreview()
    {
        if(preview!=null){preview.Cleanup();preview=null;}canvasRoot=null;content=null;previewInventoryScroll=null;
        if(previewTargetLayout)DestroyImmediate(previewTargetLayout);previewTargetLayout=null;
        if(temporaryLayout)DestroyImmediate(temporaryLayout);temporaryLayout=null;
    }
}
