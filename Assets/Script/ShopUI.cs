using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [SerializeField] private GameObject shopRoot;
    [SerializeField] private RectTransform shopPanel;
    [SerializeField] private RectTransform inventoryPanel;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private int minimumCellCount = 12;

    private readonly List<Text> shopTexts = new();
    private readonly List<Button> shopButtons = new();
    private readonly List<Image> shopImages = new();

    private readonly List<Text> inventoryTexts = new();
    private readonly List<Image> inventoryImages = new();

    private MerchantShop currentShop;
    private Font uiFont;
    private GameObject priceTooltip;
    private Text priceTooltipText;
    private Text coinText;

    private void Awake()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (inventory == null)
            inventory = FindFirstObjectByType<PlayerInventory>();

        ConfigureGrid(shopPanel);
        ConfigureGrid(inventoryPanel);

        EnsureShopCells(minimumCellCount);

        if (inventory != null)
        {
            EnsureInventoryCells(inventory.Capacity);
            inventory.Changed += RefreshInventory;
        }
        CreatePriceTooltip();
        CreateCoinText();

        if (wallet != null)
            wallet.Changed += RefreshCoinText;

        shopRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.Changed -= RefreshInventory;
        if (wallet != null)
            wallet.Changed -= RefreshCoinText;
    }

    private void Update()
    {
        if (!shopRoot.activeSelf)
            return;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    public void Open(MerchantShop shop)
    {
        if (shop == null)
            return;

        currentShop = shop;
        shopRoot.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Refresh();
    }

    public void Close()
    {
        shopRoot.SetActive(false);
        currentShop = null;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void ConfigureGrid(RectTransform panel)
    {
        GridLayoutGroup grid = panel.GetComponent<GridLayoutGroup>();

        if (grid == null)
            grid = panel.gameObject.AddComponent<GridLayoutGroup>();

        grid.padding = new RectOffset(12, 12, 12, 12);
        grid.cellSize = new Vector2(96, 96);
        grid.spacing = new Vector2(8, 8);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.UpperCenter;
    }

    private void EnsureShopCells(int count)
    {
        while (shopTexts.Count < count)
        {
            int index = shopTexts.Count;
            CreateCell(shopPanel, true, index, shopTexts, shopButtons, shopImages);
        }
    }

    private void EnsureInventoryCells(int count)
    {
        while (inventoryTexts.Count < count)
        {
            CreateCell(
                inventoryPanel,
                false,
                -1,
                inventoryTexts,
                null,
                inventoryImages
            );
        }
    }

    private void CreateCell(
        RectTransform parent,
        bool clickable,
        int stockIndex,
        List<Text> texts,
        List<Button> buttons,
        List<Image> images)
    {
        GameObject cell = new GameObject(
            "Slot",
            typeof(RectTransform),
            typeof(Image)
        );

        cell.transform.SetParent(parent, false);

        Image image = cell.GetComponent<Image>();
        image.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        images.Add(image);

        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(Text)
        );

        labelObject.transform.SetParent(cell.transform, false);

        RectTransform labelRect =
            labelObject.GetComponent<RectTransform>();

        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.GetComponent<Text>();
        label.font = uiFont;
        label.fontSize = 18;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.black;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;

        texts.Add(label);

        if (!clickable)
            return;

        Button button = cell.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => Buy(stockIndex));

        PriceTooltipTrigger tooltipTrigger =
            cell.AddComponent<PriceTooltipTrigger>();

        tooltipTrigger.Setup(this, -1);

        buttons.Add(button);
    }

    private void Buy(int stockIndex)
    {
        if (currentShop == null)
            return;

        currentShop.TryBuy(stockIndex, inventory, wallet);
        Refresh();
    }

    private void Refresh()
    {
        RefreshShop();
        RefreshInventory();
        RefreshCoinText();
    }

    private void RefreshShop()
    {
        if (currentShop == null)
            return;

        EnsureShopCells(
            Mathf.Max(minimumCellCount, currentShop.Stock.Count)
        );

        for (int i = 0; i < shopTexts.Count; i++)
        {
            bool hasItem = i < currentShop.Stock.Count &&
                currentShop.Stock[i].item != null;

            shopButtons[i].interactable = hasItem &&
                !currentShop.Stock[i].IsSoldOut;

            shopImages[i].color = hasItem
                ? Color.white
                : new Color(0.55f, 0.55f, 0.55f, 1f);

            shopTexts[i].text = hasItem
                ? FormatShopItem(currentShop.Stock[i])
                : "";

            PriceTooltipTrigger tooltipTrigger =
                shopButtons[i].GetComponent<PriceTooltipTrigger>();

            tooltipTrigger.Setup(
                this,
                hasItem ? currentShop.Stock[i].Price : -1
            );
        }
    }

    private void RefreshInventory()
    {
        if (inventory == null)
            return;

        for (int i = 0; i < inventoryTexts.Count; i++)
        {
            bool hasItem = i < inventory.Slots.Count &&
                inventory.Slots[i].item != null;

            inventoryImages[i].color = hasItem
                ? Color.white
                : new Color(0.55f, 0.55f, 0.55f, 1f);

            inventoryTexts[i].text = hasItem
                ? FormatInventoryItem(inventory.Slots[i])
                : "";
        }
    }

    private string FormatShopItem(ShopStockEntry entry)
    {
        string quantity = entry.Quantity < 0
            ? "∞"
            : "x" + entry.Quantity;

        return GetShape(entry.item.Shape) +
            "\n" + entry.item.DisplayName +
            "\n" + quantity;
    }

    private string FormatInventoryItem(InventorySlot slot)
    {
        return GetShape(slot.item.Shape) +
            "\n" + slot.item.DisplayName +
            "\nx" + slot.quantity;
    }

    private string GetShape(ItemShape shape)
    {
        return shape switch
        {
            ItemShape.Triangle => "▲",
            ItemShape.Circle => "●",
            ItemShape.Square => "■",
            _ => "?"
        };
    }
    private void CreatePriceTooltip()
    {
        priceTooltip = new GameObject(
            "PriceTooltip",
            typeof(RectTransform),
            typeof(Image)
        );

        priceTooltip.transform.SetParent(shopRoot.transform, false);

        Image background = priceTooltip.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.85f);
        background.raycastTarget = false;

        RectTransform tooltipRect =
            priceTooltip.GetComponent<RectTransform>();

        tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
        tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipRect.sizeDelta = new Vector2(150f, 36f);

        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(Text)
        );

        labelObject.transform.SetParent(priceTooltip.transform, false);

        RectTransform labelRect =
            labelObject.GetComponent<RectTransform>();

        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        priceTooltipText = labelObject.GetComponent<Text>();
        priceTooltipText.font = uiFont;
        priceTooltipText.fontSize = 18;
        priceTooltipText.alignment = TextAnchor.MiddleCenter;
        priceTooltipText.color = Color.white;
        priceTooltipText.raycastTarget = false;

        priceTooltip.SetActive(false);
    }

    public void ShowPrice(int price, Vector2 screenPosition)
    {
        priceTooltipText.text = "价格：" + price + " 金币";

        RectTransform tooltipRect =
            priceTooltip.GetComponent<RectTransform>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            shopRoot.GetComponent<RectTransform>(),
            screenPosition,
            null,
            out Vector2 localPosition
        );

        tooltipRect.anchoredPosition =
            localPosition + new Vector2(85f, -25f);

        priceTooltip.SetActive(true);
    }

    public void HidePrice()
    {
        priceTooltip.SetActive(false);
    }

    private void CreateCoinText()
    {
        GameObject textObject = new GameObject(
            "CoinText",
            typeof(RectTransform),
            typeof(Text)
        );

        textObject.transform.SetParent(shopRoot.transform, false);

        RectTransform textRect =
            textObject.GetComponent<RectTransform>();

        textRect.anchorMin = new Vector2(1f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(1f, 1f);
        textRect.anchoredPosition = new Vector2(-35f, -30f);
        textRect.sizeDelta = new Vector2(300f, 45f);

        coinText = textObject.GetComponent<Text>();
        coinText.font = uiFont;
        coinText.fontSize = 26;
        coinText.alignment = TextAnchor.MiddleRight;
        coinText.color = new Color(1f, 0.84f, 0f, 1f);
        coinText.raycastTarget = false;

        RefreshCoinText();
    }

    private void RefreshCoinText()
    {
        if (coinText != null && wallet != null)
            coinText.text = "金币：" + wallet.Coins;
    }
}