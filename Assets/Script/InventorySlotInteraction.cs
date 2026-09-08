using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlotInteraction :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler,
    IPointerClickHandler
{
    private InventoryUI inventoryUI;
    private int slotIndex;

    public void Setup(InventoryUI ui, int index)
    {
        inventoryUI = ui;
        slotIndex = index;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        inventoryUI.BeginDrag(slotIndex, eventData.position);
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
        inventoryUI.TryDropOnBackpack(slotIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount >= 2)
            inventoryUI.TryAutoEquip(slotIndex);
    }
}