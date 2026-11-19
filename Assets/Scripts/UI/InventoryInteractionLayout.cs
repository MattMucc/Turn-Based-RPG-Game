using UnityEngine;
[CreateAssetMenu(menuName="Game/UI/Inventory Interaction Layout")]
public sealed class InventoryInteractionLayout : ScriptableObject {
 public RectTransform Overlay;
 public InventoryInteractionView Examination,Recipients,Recovery,Learning,AccessorySlots,Equipment;
 public InventoryButtonView Recipient;
 public InventoryInteractionView For(int step,bool learning)=>step==1?Examination:step==2||step==4?Recipients:step==3?(learning?Learning:Recovery):step==5?AccessorySlots:Equipment;
}
