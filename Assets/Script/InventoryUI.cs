using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject inventoryRoot;
    [SerializeField] private RectTransform backpackPanel;
    [SerializeField] private RectTransform headSlot;
    [SerializeField] private RectTransform armorSlot;
    [SerializeField] private RectTransform leftHandSlot;
    [SerializeField] private RectTransform rightHandSlot;

    [Header("Player References")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private PlayerEquipment equipment;

    [Min(1)]
    [SerializeField] private int minimumCellCount = 12;

    private readonly List<Text> backpackTexts = new();
    private readonly List<Image> backpackImages = new();

    private Font uiFont;
    private Text coinText;
    private Text headText;
    private Text armorText;
    private Text leftHandText;
    private Text rightHandText;

    private ItemDefinition draggingItem;
    private EquipSlot draggingEquipmentSlot = EquipSlot.None;
    private bool equipmentDropSucceeded;
    private bool droppedOnAnySlot;
    private GameObject dragVisual;
    private Text dragVisualText;

    private void Awake()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (inventory == null)
            inventory = FindFirstObjectByType<PlayerInventory>();

        if (wallet == null)
            wallet = FindFirstObjectByType<PlayerWallet>();

        if (equipment == null)
            equipment = FindFirstObjectByType<PlayerEquipment>();

        ConfigureGrid();

        EnsureBackpackCells(
            inventory != null ? inventory.Capacity : minimumCellCount
        );

        headText = CreateEquipmentLabel(headSlot, "Í·¿ø");
        armorText = CreateEquipmentLabel(armorSlot, "¿ø¼×");
        leftHandText = CreateEquipmentLabel(leftHandSlot, "×óÊÖ");
        rightHandText = CreateEquipmentLabel(rightHandSlot, "ÓÒÊÖ");

        SetupEquipmentSlot(headSlot, EquipSlot.Head);
        SetupEquipmentSlot(armorSlot, EquipSlot.Armor);
        SetupEquipmentSlot(leftHandSlot, EquipSlot.LeftHand);
        SetupEquipmentSlot(rightHandSlot, EquipSlot.RightHand);

        CreateCoinText();
        CreateDragVisual();

        if (inventory != null)
            inventory.Changed += RefreshBackpack;

        if (wallet != null)
            wallet.Changed += RefreshCoinText;

        if (equipment != null)
            equipment.Changed += RefreshEquipment;

        inventoryRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.Changed -= RefreshBackpack;

        if (wallet != null)
            wallet.Changed -= RefreshCoinText;

        if (equipment != null)
            equipment.Changed -= RefreshEquipment;
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.bKey.wasPressedThisFrame)
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        if (inventoryRoot.activeSelf)
            Close();
        else
            Open();
    }

    public void Open()
    {
        inventoryRoot.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        RefreshBackpack();
        RefreshEquipment();
        RefreshCoinText();
    }

    public void Close()
    {
        inventoryRoot.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    public void BeginDrag(int slotIndex, Vector2 screenPosition)
    {
        draggingEquipmentSlot = EquipSlot.None;
        equipmentDropSucceeded = false;
        droppedOnAnySlot = false;

        draggingItem = GetInventoryItem(slotIndex);

        if (draggingItem == null)
            return;

        ShowDragVisual(screenPosition);
    }

    public void BeginEquipmentDrag(
        EquipSlot sourceSlot,
        Vector2 screenPosition
    )
    {
        if (equipment == null)
            return;

        draggingEquipmentSlot = sourceSlot;
        equipmentDropSucceeded = false;
        droppedOnAnySlot = false;

        draggingItem = equipment.GetItem(sourceSlot);

        if (draggingItem == null)
            return;

        ShowDragVisual(screenPosition);
    }

    private void ShowDragVisual(Vector2 screenPosition)
    {
        dragVisualText.text =
            GetShape(draggingItem.Shape) +
            "\n" + draggingItem.DisplayName;

        dragVisual.SetActive(true);
        UpdateDragPosition(screenPosition);
    }

    public void UpdateDragPosition(Vector2 screenPosition)
    {
        if (dragVisual == null || !dragVisual.activeSelf)
            return;

        RectTransform visualRect =
            dragVisual.GetComponent<RectTransform>();

        visualRect.position = new Vector3(
            screenPosition.x + 20f,
            screenPosition.y - 20f,
            0f
        );
    }

    public void EndDrag()
    {
        StartCoroutine(ClearDragAfterDrop());
    }

    private IEnumerator ClearDragAfterDrop()
    {
        yield return null;

        if (draggingEquipmentSlot != EquipSlot.None &&
            !equipmentDropSucceeded &&
            !droppedOnAnySlot)
        {
            equipment.DestroyEquippedItem(draggingEquipmentSlot);
        }

        draggingItem = null;
        draggingEquipmentSlot = EquipSlot.None;
        equipmentDropSucceeded = false;
        droppedOnAnySlot = false;

        if (dragVisual != null)
            dragVisual.SetActive(false);

        RefreshBackpack();
        RefreshEquipment();
    }

    public void TryDropOnEquipment(EquipSlot targetSlot)
    {
        if (draggingItem == null || equipment == null)
            return;

        if (draggingEquipmentSlot != EquipSlot.None)
        {
            droppedOnAnySlot = true;
            return;
        }

        equipment.TryEquip(draggingItem, targetSlot);

        RefreshBackpack();
        RefreshEquipment();
    }

    public void TryAutoEquip(int slotIndex)
    {
        ItemDefinition item = GetInventoryItem(slotIndex);

        if (item == null || item.EquipSlot == EquipSlot.None ||
            equipment == null)
        {
            return;
        }

        equipment.TryEquip(item, item.EquipSlot);

        RefreshBackpack();
        RefreshEquipment();
    }

    private ItemDefinition GetInventoryItem(int slotIndex)
    {
        if (inventory == null || slotIndex < 0 ||
            slotIndex >= inventory.Slots.Count)
        {
            return null;
        }

        return inventory.Slots[slotIndex].item;
    }

    private void ConfigureGrid()
    {
        GridLayoutGroup grid =
            backpackPanel.GetComponent<GridLayoutGroup>();

        if (grid == null)
            grid = backpackPanel.gameObject.AddComponent<GridLayoutGroup>();

        grid.padding = new RectOffset(12, 12, 12, 12);
        grid.cellSize = new Vector2(96, 96);
        grid.spacing = new Vector2(8, 8);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.UpperCenter;
    }

    private void EnsureBackpackCells(int count)
    {
        while (backpackTexts.Count < count)
            CreateBackpackCell();
    }

    private void CreateBackpackCell()
    {
        int slotIndex = backpackTexts.Count;

        GameObject cell = new GameObject(
            "Slot",
            typeof(RectTransform),
            typeof(Image)
        );

        cell.transform.SetParent(backpackPanel, false);

        Image image = cell.GetComponent<Image>();
        image.color = new Color(0.55f, 0.55f, 0.55f, 1f);
        backpackImages.Add(image);

        InventorySlotInteraction interaction =
            cell.AddComponent<InventorySlotInteraction>();

        interaction.Setup(this, slotIndex);

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
        label.raycastTarget = false;

        backpackTexts.Add(label);
    }

    private Text CreateEquipmentLabel(
        RectTransform slot,
        string slotName
    )
    {
        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(Text)
        );

        labelObject.transform.SetParent(slot, false);

        RectTransform labelRect =
            labelObject.GetComponent<RectTransform>();

        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.GetComponent<Text>();
        label.font = uiFont;
        label.fontSize = 16;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.black;
        label.raycastTarget = false;
        label.text = slotName;

        return label;
    }

    private void SetupEquipmentSlot(
        RectTransform slot,
        EquipSlot slotType
    )
    {
        EquipmentSlotDrop drop =
            slot.GetComponent<EquipmentSlotDrop>();

        if (drop == null)
            drop = slot.gameObject.AddComponent<EquipmentSlotDrop>();

        drop.Setup(this, slotType);
    }

    private void CreateCoinText()
    {
        GameObject textObject = new GameObject(
            "CoinText",
            typeof(RectTransform),
            typeof(Text)
        );

        textObject.transform.SetParent(inventoryRoot.transform, false);

        RectTransform textRect =
            textObject.GetComponent<RectTransform>();

        textRect.anchorMin = new Vector2(0.75f, 0.5f);
        textRect.anchorMax = new Vector2(0.75f, 0.5f);
        textRect.anchoredPosition = new Vector2(0f, 240f);
        textRect.sizeDelta = new Vector2(300f, 36f);

        coinText = textObject.GetComponent<Text>();
        coinText.font = uiFont;
        coinText.fontSize = 22;
        coinText.alignment = TextAnchor.MiddleCenter;
        coinText.color = new Color(1f, 0.84f, 0f, 1f);
        coinText.raycastTarget = false;
    }

    private void RefreshBackpack()
    {
        if (inventory == null)
            return;

        for (int i = 0; i < backpackTexts.Count; i++)
        {
            bool hasItem = i < inventory.Slots.Count &&
                inventory.Slots[i].item != null;

            backpackImages[i].color = hasItem
                ? Color.white
                : new Color(0.55f, 0.55f, 0.55f, 1f);

            backpackTexts[i].text = hasItem
                ? GetShape(inventory.Slots[i].item.Shape) +
                    "\n" + inventory.Slots[i].item.DisplayName +
                    "\nx" + inventory.Slots[i].quantity
                : "";
        }
    }

    private void RefreshEquipment()
    {
        if (equipment == null)
            return;

        RefreshEquipmentLabel(
            headText,
            "Í·¿ø",
            equipment.GetItem(EquipSlot.Head)
        );

        RefreshEquipmentLabel(
            armorText,
            "¿ø¼×",
            equipment.GetItem(EquipSlot.Armor)
        );

        RefreshEquipmentLabel(
            leftHandText,
            "×óÊÖ",
            equipment.GetItem(EquipSlot.LeftHand)
        );

        RefreshEquipmentLabel(
            rightHandText,
            "ÓÒÊÖ",
            equipment.GetItem(EquipSlot.RightHand)
        );
    }

    private void RefreshEquipmentLabel(
        Text label,
        string slotName,
        ItemDefinition item
    )
    {
        if (label == null)
            return;

        label.text = item == null
            ? slotName
            : slotName + "\n" +
                GetShape(item.Shape) + "\n" +
                item.DisplayName;
    }

    private void RefreshCoinText()
    {
        if (coinText != null && wallet != null)
            coinText.text = "½ð±Ò£º" + wallet.Coins;
    }

    private string GetShape(ItemShape shape)
    {
        return shape switch
        {
            ItemShape.Triangle => "¡ø",
            ItemShape.Circle => "¡ñ",
            ItemShape.Square => "¡ö",
            _ => "?"
        };
    }

    private void CreateDragVisual()
    {
        dragVisual = new GameObject(
            "DragVisual",
            typeof(RectTransform),
            typeof(Image)
        );

        dragVisual.transform.SetParent(inventoryRoot.transform, false);

        Image image = dragVisual.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.75f);
        image.raycastTarget = false;

        RectTransform visualRect =
            dragVisual.GetComponent<RectTransform>();

        visualRect.anchorMin = new Vector2(0.5f, 0.5f);
        visualRect.anchorMax = new Vector2(0.5f, 0.5f);
        visualRect.sizeDelta = new Vector2(96f, 96f);

        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(Text)
        );

        labelObject.transform.SetParent(dragVisual.transform, false);

        RectTransform labelRect =
            labelObject.GetComponent<RectTransform>();

        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        dragVisualText = labelObject.GetComponent<Text>();
        dragVisualText.font = uiFont;
        dragVisualText.fontSize = 16;
        dragVisualText.alignment = TextAnchor.MiddleCenter;
        dragVisualText.color = Color.black;
        dragVisualText.raycastTarget = false;

        dragVisual.SetActive(false);
    }
    public void TryDropOnBackpack(int targetIndex)
    {
        if (draggingEquipmentSlot == EquipSlot.None ||
            equipment == null)
        {
            return;
        }

        droppedOnAnySlot = true;

        equipmentDropSucceeded = equipment.TryUnequipToSlot(
            draggingEquipmentSlot,
            targetIndex
        );

        RefreshBackpack();
        RefreshEquipment();
    }

    public void TryAutoUnequip(EquipSlot sourceSlot)
    {
        if (equipment == null)
            return;

        equipment.TryUnequip(sourceSlot);

        RefreshBackpack();
        RefreshEquipment();
    }
}