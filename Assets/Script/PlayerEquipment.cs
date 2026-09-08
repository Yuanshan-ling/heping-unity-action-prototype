using System;
using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;

    [SerializeField] private ItemDefinition head;
    [SerializeField] private ItemDefinition armor;
    [SerializeField] private ItemDefinition leftHand;
    [SerializeField] private ItemDefinition rightHand;

    public event Action Changed;

    private void Awake()
    {
        if (inventory == null)
            inventory = GetComponent<PlayerInventory>();
    }

    public ItemDefinition GetItem(EquipSlot slot)
    {
        return slot switch
        {
            EquipSlot.Head => head,
            EquipSlot.Armor => armor,
            EquipSlot.LeftHand => leftHand,
            EquipSlot.RightHand => rightHand,
            _ => null
        };
    }

    public bool TryEquip(ItemDefinition item, EquipSlot targetSlot)
    {
        if (item == null || inventory == null)
            return false;

        if (item.EquipSlot != targetSlot)
            return false;

        if (!inventory.TryRemoveOne(item))
            return false;

        ItemDefinition previousItem = GetItem(targetSlot);
        SetItem(targetSlot, item);

        if (previousItem != null)
            inventory.AddItem(previousItem);

        Changed?.Invoke();
        return true;
    }

    public bool TryUnequip(EquipSlot sourceSlot)
    {
        ItemDefinition item = GetItem(sourceSlot);

        if (item == null || inventory == null)
            return false;

        if (!inventory.AddItem(item))
            return false;

        SetItem(sourceSlot, null);
        Changed?.Invoke();
        return true;
    }

    public bool TryUnequipToSlot(
        EquipSlot sourceSlot,
        int inventorySlotIndex
    )
    {
        ItemDefinition item = GetItem(sourceSlot);

        if (item == null || inventory == null)
            return false;

        if (!inventory.TryAddOneToSlot(
            item,
            inventorySlotIndex
        ))
        {
            return false;
        }

        SetItem(sourceSlot, null);
        Changed?.Invoke();
        return true;
    }

    public bool DestroyEquippedItem(EquipSlot sourceSlot)
    {
        if (GetItem(sourceSlot) == null)
            return false;

        SetItem(sourceSlot, null);
        Changed?.Invoke();
        return true;
    }

    private void SetItem(EquipSlot slot, ItemDefinition item)
    {
        switch (slot)
        {
            case EquipSlot.Head:
                head = item;
                break;

            case EquipSlot.Armor:
                armor = item;
                break;

            case EquipSlot.LeftHand:
                leftHand = item;
                break;

            case EquipSlot.RightHand:
                rightHand = item;
                break;
        }
    }
}