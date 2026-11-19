using System.Collections.Generic;
using Game.GridPrototype;
using UnityEngine;
[CreateAssetMenu(menuName="Game/UI/Abilities Page Layout")]
public sealed class AbilitiesPageLayout:ScriptableObject
{
    public AbilitiesWorkspaceView Workspace;
    public InventoryButtonView CategoryButton,FamilyButton;
    public AbilityRowView AbilityRow;
    public AbilityFavoriteView FavoriteSlot;
    public Color SelectedColor=new Color(.16f,.36f,.44f);
    public List<BattleActionCategory> Categories=new(){BattleActionCategory.Attack,BattleActionCategory.Skill,BattleActionCategory.Magic,BattleActionCategory.Item};
}
