using UnityEngine;
using UnityEngine.EventSystems;

public class PriceTooltipTrigger :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    private ShopUI shopUI;
    private int price;
    private bool canShow;

    public void Setup(ShopUI ui, int newPrice)
    {
        shopUI = ui;
        price = newPrice;
        canShow = newPrice >= 0;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (canShow && shopUI != null)
            shopUI.ShowPrice(price, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (shopUI != null)
            shopUI.HidePrice();
    }
}