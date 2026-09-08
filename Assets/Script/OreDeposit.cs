using UnityEngine;

public class OreDeposit : MonoBehaviour
{
    [SerializeField] private ItemDefinition oreItem;

    [Min(1)]
    [SerializeField] private int amount = 1;

    public bool TryCollect(PlayerInventory inventory)
    {
        if (inventory == null || oreItem == null)
            return false;

        for (int i = 0; i < amount; i++)
        {
            if (!inventory.AddItem(oreItem))
                return false;
        }

        Destroy(gameObject);
        return true;
    }
}