using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShopStockEntry
{
    public ItemDefinition item;

    [Tooltip("-1 表示无限库存；0 表示售罄")]
    [SerializeField] private int quantity = -1;

    [Min(0)]
    [SerializeField] private int price;

    public int Quantity => quantity;
    public int Price => price;
    public bool IsSoldOut => quantity == 0;

    public void RemoveOne()
    {
        if (quantity > 0)
            quantity--;
    }
}

public class MerchantShop : MonoBehaviour
{
    [SerializeField] private List<ShopStockEntry> stock = new();

    public IReadOnlyList<ShopStockEntry> Stock => stock;

    public event Action Changed;

    public bool TryBuy(
        int index,
        PlayerInventory inventory,
        PlayerWallet wallet
    )
    {
        if (inventory == null || wallet == null ||
            index < 0 || index >= stock.Count)
        {
            return false;
        }

        ShopStockEntry entry = stock[index];

        if (entry.item == null || entry.IsSoldOut ||
            wallet.Coins < entry.Price)
        {
            return false;
        }

        if (!inventory.AddItem(entry.item))
            return false;

        wallet.TrySpend(entry.Price);
        entry.RemoveOne();
        Changed?.Invoke();
        return true;
    }
}