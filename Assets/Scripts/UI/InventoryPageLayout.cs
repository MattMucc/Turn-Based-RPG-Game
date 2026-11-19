using System.Collections.Generic;
using UnityEngine;
public enum InventoryFilter { All, Consumables, Weapons, Armor, Accessories, Materials, SpellBooks, Other }
[CreateAssetMenu(menuName="Game/UI/Inventory Layout")]
public sealed class InventoryPageLayout : ScriptableObject
{
    public Color SelectedColor=new Color(.16f,.36f,.44f);
    public InventoryWorkspaceView Workspace;
    public InventorySlotView Slot;
    public InventoryButtonView FilterButton,ContextButton;
    public InventoryDetailsView Details;
    public InventoryContextView Context;
    public List<InventoryFilter> Filters=new(){InventoryFilter.All,InventoryFilter.Consumables,InventoryFilter.Weapons,InventoryFilter.Armor,InventoryFilter.Accessories,InventoryFilter.Materials,InventoryFilter.SpellBooks,InventoryFilter.Other};
}
