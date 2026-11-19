using UnityEngine;
[CreateAssetMenu(menuName="Game/UI/Merchant Page Layout")]
public sealed class MerchantPageLayout:ScriptableObject {
 public bool Blacksmith;
 public MerchantWorkspaceView Workspace;
 public InventoryButtonView Filter,Mode,Action,Cancel,AttackDetails;
 public MerchantSlotView Slot;
 public Color SelectedColor=new Color(.16f,.36f,.44f);
}
