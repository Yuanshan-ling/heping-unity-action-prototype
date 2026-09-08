using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventorySlot
{
    public ItemDefinition item;
    public int quantity;
}

public class PlayerInventory : MonoBehaviour
{
    [Min(1)]
    [SerializeField] private int capacity = 12;

    [SerializeField] private List<InventorySlot> slots = new();

    public int Capacity => capacity;
    public IReadOnlyList<InventorySlot> Slots => slots;

    public event Action Changed;

    private void Awake()
    {
        EnsureSlots();
    }

    public bool AddItem(ItemDefinition item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        EnsureSlots();

        int remaining = amount;

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];

            if (slot.item != item ||
                slot.quantity >= item.MaxStack)
            {
                continue;
            }

            int added = Mathf.Min(
                item.MaxStack - slot.quantity,
                remaining
            );

            slot.quantity += added;
            remaining -= added;

            if (remaining == 0)
            {
                Changed?.Invoke();
                return true;
            }
        }

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];

            if (slot.item != null)
                continue;

            int added = Mathf.Min(item.MaxStack, remaining);

            slot.item = item;
            slot.quantity = added;
            remaining -= added;

            if (remaining == 0)
            {
                Changed?.Invoke();
                return true;
            }
        }

        Changed?.Invoke();
        return false;
    }

    public bool TryRemoveOne(ItemDefinition item)
    {
        if (item == null)
            return false;

        EnsureSlots();

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];

            if (slot.item != item || slot.quantity <= 0)
                continue;

            slot.quantity--;

            if (slot.quantity == 0)
                slot.item = null;

            Changed?.Invoke();
            return true;
        }

        return false;
    }

    public bool TryAddOneToSlot(
        ItemDefinition item,
        int slotIndex
    )
    {
        if (item == null)
            return false;

        EnsureSlots();

        if (slotIndex < 0 || slotIndex >= slots.Count)
            return false;

        InventorySlot slot = slots[slotIndex];

        if (slot.item == null)
        {
            slot.item = item;
            slot.quantity = 1;
            Changed?.Invoke();
            return true;
        }

        if (slot.item != item ||
            slot.quantity >= item.MaxStack)
        {
            return false;
        }

        slot.quantity++;
        Changed?.Invoke();
        return true;
    }

    private void EnsureSlots()
    {
        if (slots == null)
            slots = new List<InventorySlot>();

        while (slots.Count < capacity)
            slots.Add(new InventorySlot());

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
                slots[i] = new InventorySlot();
        }
    }
}