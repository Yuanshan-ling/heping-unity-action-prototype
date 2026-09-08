using UnityEngine;
using UnityEngine.EventSystems;

public class EquipmentSlotDrop :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler,
    IPointerClickHandler
{
    private InventoryUI inventoryUI;
    private EquipSlot equipSlot;

    public void Setup(InventoryUI ui, EquipSlot slot)
    {
        inventoryUI = ui;
        equipSlot = slot;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        inventoryUI.BeginEquipmentDrag(
            equipSlot,
            eventData.position
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        inventoryUI.UpdateDragPosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        inventoryUI.EndDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        inventoryUI.TryDropOnEquipment(equipSlot);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount >= 2)
            inventoryUI.TryAutoUnequip(equipSlot);
    }
}
