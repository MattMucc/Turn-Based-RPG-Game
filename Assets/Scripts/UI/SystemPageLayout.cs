using UnityEngine;
[CreateAssetMenu(menuName="Game/UI/System Page Layout")]
public sealed class SystemPageLayout:ScriptableObject {
    public SystemWorkspaceView Workspace;
    public InventoryButtonView Tab;
    public SystemPanelView SaveLoad,Battle,Graphics,UI,Audio,Confirmation,SaveSlots;
    public SystemSaveSlotView SaveSlot;
    public Color SelectedColor=new Color(.16f,.36f,.44f);
}
