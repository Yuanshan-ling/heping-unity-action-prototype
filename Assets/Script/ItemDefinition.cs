using UnityEngine;

public enum ItemShape
{
    Triangle,
    Circle,
    Square
}

public enum EquipSlot
{
    None,
    Head,
    Armor,
    LeftHand,
    RightHand
}

[CreateAssetMenu(menuName = "Shop/Item Definition", fileName = "NewItem")]
public class ItemDefinition : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private ItemShape shape;
    [SerializeField] private EquipSlot equipSlot;

    [Min(1)]
    [SerializeField] private int maxStack = 99;

    public string DisplayName => displayName;
    public ItemShape Shape => shape;
    public EquipSlot EquipSlot => equipSlot;
    public int MaxStack => maxStack;
}