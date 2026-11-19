using System.Linq;
using Game.GridPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class ProgressionMenu
{
    Vector2 inventoryContextScreen;
    Camera inventoryContextCamera;

    void OpenItemContextAt(BattleActionOption item, Vector2 screenPosition, Camera eventCamera)
    {
        if (!open || page != Page.Inventory || inventoryDragSource || inventoryModal) return;
        partyEquipmentContext = false;
        SelectInventoryItem(item); inventoryItem = item; inventoryStep = 0;
        inventoryContextScreen = screenPosition; inventoryContextCamera = eventCamera;
        DrawInventoryModal();
    }

    // Screen/canvas scaling is resolved before this calculation. Return a top-left position.
    static Vector2 InventoryPopupPosition(Vector2 canvasSize, Vector2 cursor, Vector2 popupSize)
    {
        const float gap = 12, margin = 8;
        return new Vector2(
            Mathf.Clamp(cursor.x + gap, margin, Mathf.Max(margin, canvasSize.x - popupSize.x - margin)),
            Mathf.Clamp(cursor.y - popupSize.y - gap, margin, Mathf.Max(margin, canvasSize.y - popupSize.y - margin)));
    }

    void DrawInventoryContextPopup()
    {
        // A transparent dismissal layer captures outside clicks without darkening the page.
        inventoryModal.GetComponent<Image>().color = Color.clear;
        float width = 244, y = 44;
        var panel = Rect("Item context", inventoryModal, 0, 0, width, 100);
        panel.gameObject.AddComponent<Image>().color = Panel;
        var name = Label(Rect("Item name", panel, 12, 8, width - 24, 30), inventoryItem.Name, 16);
        name.enableAutoSizing = true; name.fontSizeMin = 11; name.fontSizeMax = 16;
        int start = buttons.Count;
        Button Row(string label, UnityEngine.Events.UnityAction action)
        {
            var b = Button(panel, label, 8, y, width - 16, () => action(), null, 34); y += 38; return b;
        }
        Row("Examine", () => { inventoryStep = 1; DrawInventoryModal(); });
        if (inventoryEquipment.TryGetValue(inventoryItem, out var copy))
        {
            var owner = Manager.EquipmentOwner(copy.InstanceId);
            Row("Equip", () => { inventoryUnequip = false; inventoryEquipNotice = null; inventoryStep = 4; DrawInventoryModal(); }).interactable = !owner;
            if (owner) Row("Unequip", () =>
            {
                inventoryUnequip = true; inventoryEquipMember = owner; inventoryEquipNotice = null;
                inventoryEquipSlot = System.Enum.GetValues(typeof(EquipmentSlot)).Cast<EquipmentSlot>().First(s => owner.GetComponent<CharacterEquipment>().InstanceAt(s) == copy.InstanceId);
                inventoryStep = 6; DrawInventoryModal();
            });
            Row("Upgrade details", () => { upgradeSelection = copy.InstanceId; ShowPage(Page.Upgrades); });
        }
        if (PartyInventory.UsableOutsideCombat(inventoryItem))
            Row("Use", () => { inventoryStep = 2; DrawInventoryModal(); }).interactable = inventoryItem.Quantity > 0;
        Row("Back", InventoryBack);
        panel.sizeDelta = new Vector2(width, y + 4);
        Canvas.ForceUpdateCanvases();
        var canvas = (RectTransform)inventoryModal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, inventoryContextScreen, inventoryContextCamera, out var local);
        var cursor = new Vector2(local.x - canvas.rect.xMin, canvas.rect.yMax - local.y);
        var position = InventoryPopupPosition(canvas.rect.size, cursor, panel.sizeDelta);
        panel.anchoredPosition = new Vector2(position.x, -position.y);
        var choices = buttons.Skip(start).Where(b => b && b.IsInteractable()).ToList();
        for (int i = 0; i < choices.Count; i++)
        {
            var n = choices[i].navigation; n.mode = Navigation.Mode.Explicit;
            n.selectOnUp = choices[(i + choices.Count - 1) % choices.Count]; n.selectOnDown = choices[(i + 1) % choices.Count]; choices[i].navigation = n;
        }
        if (choices.Count > 0) events.SetSelectedGameObject(choices[0].gameObject);
    }

    void InventoryDetails(BattleActionOption item)
    {
        Clear(detailRoot);
        float width = detailRoot.rect.width;
        detailName = Label(Rect("Detail name", detailRoot, 12, 0, width - 24, 58), item.Name, 23);
        detailName.enableAutoSizing = true; detailName.fontSizeMin = 16; detailName.fontSizeMax = 23;
        var art = Rect("Selected item image", detailRoot, 12, 68, 96, 96);
        art.gameObject.AddComponent<Image>().color = new Color(.06f, .12f, .17f);
        DrawInventoryIcon(art, item, 10, 10, 76);
        Label(Rect("Item category", detailRoot, 126, 72, width - 140, 30), InventoryCategoryName(InventoryKind(item)), 14).color = Accent;
        Label(Rect("Item stock", detailRoot, 126, 108, width - 140, 52),
            inventoryEquipment.TryGetValue(item, out var copy) ? $"Upgrade +{copy.UpgradeLevel} / {copy.Definition.Upgrades.Count}" : $"Carried: {item.Quantity}", 16);
        float top = 184, height = Mathf.Max(100, detailRoot.rect.height - top - 12);
        detailContent = List(detailRoot, "Inventory details", 12, top, width - 24, height, height);
        string description = InventoryDescription(item);
        detailText = Label(Rect("Description", detailContent, 8, 8, width - 48, height - 16), description, 16);
        float textHeight = detailText.GetPreferredValues(description, width - 48, 0).y;
        detailText.rectTransform.sizeDelta = new Vector2(width - 48, Mathf.Max(height - 16, textHeight));
        detailContent.sizeDelta = new Vector2(detailContent.sizeDelta.x, Mathf.Max(height, textHeight + 16));
        // Detail controls are rebuilt on every selection; keep the focus registry free of destroyed entries.
        buttons.RemoveAll(b => !b || !b.gameObject.activeInHierarchy);
    }
}
