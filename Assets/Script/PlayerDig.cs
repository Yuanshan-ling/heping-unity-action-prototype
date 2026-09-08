using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class PlayerDig : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private PlayerInventory inventory;

    [Header("Dig Area")]
    [Min(1)]
    [SerializeField] private int digWidth = 6;

    [Min(1)]
    [SerializeField] private int digHeight = 6;

    [Min(0f)]
    [SerializeField] private float digDistance = 0.5f;

    private Collider2D playerCollider;

    private void Awake()
    {
        playerCollider = GetComponent<Collider2D>();

        if (inventory == null)
            inventory = GetComponent<PlayerInventory>();

        if (inventory == null)
            inventory = FindFirstObjectByType<PlayerInventory>();
    }

    private void Update()
    {
        if (Keyboard.current == null ||
            !Keyboard.current.cKey.wasPressedThisFrame)
        {
            return;
        }

        Vector2Int direction = ReadMoveDirection();

        if (direction == Vector2Int.zero)
            return;

        Dig(direction);
    }

    private Vector2Int ReadMoveDirection()
    {
        if (Keyboard.current.aKey.isPressed)
            return Vector2Int.left;

        if (Keyboard.current.dKey.isPressed)
            return Vector2Int.right;

        if (Keyboard.current.wKey.isPressed)
            return Vector2Int.up;

        if (Keyboard.current.sKey.isPressed)
            return Vector2Int.down;

        return Vector2Int.zero;
    }

    private void Dig(Vector2Int direction)
    {
        if (groundTilemap == null)
        {
            Debug.LogError("PlayerDig ÉÐÎ´Ö¸¶¨ Ground Tilemap¡£");
            return;
        }

        Vector2 targetWorldPosition =
            GetDigTargetPosition(direction);

        Vector3Int centerCell =
            groundTilemap.WorldToCell(targetWorldPosition);

        int startX = centerCell.x - digWidth / 2;
        int startY = centerCell.y - digHeight / 2;

        CollectOreInArea(startX, startY);

        for (int x = 0; x < digWidth; x++)
        {
            for (int y = 0; y < digHeight; y++)
            {
                Vector3Int cellPosition = new Vector3Int(
                    startX + x,
                    startY + y,
                    0
                );

                if (groundTilemap.HasTile(cellPosition))
                    groundTilemap.SetTile(cellPosition, null);
            }
        }
    }

    private void CollectOreInArea(int startX, int startY)
    {
        if (inventory == null)
            return;

        OreDeposit[] deposits =
            FindObjectsByType<OreDeposit>(
                FindObjectsSortMode.None
            );

        foreach (OreDeposit deposit in deposits)
        {
            Vector3Int oreCell =
                groundTilemap.WorldToCell(
                    deposit.transform.position
                );

            bool isInsideDigArea =
                oreCell.x >= startX &&
                oreCell.x < startX + digWidth &&
                oreCell.y >= startY &&
                oreCell.y < startY + digHeight;

            if (isInsideDigArea)
                deposit.TryCollect(inventory);
        }
    }

    private Vector2 GetDigTargetPosition(Vector2Int direction)
    {
        if (playerCollider == null)
        {
            return (Vector2)transform.position +
                   (Vector2)direction * digDistance;
        }

        Bounds bounds = playerCollider.bounds;
        Vector2 target = bounds.center;

        if (direction.x != 0)
        {
            target.x += direction.x *
                (bounds.extents.x + digDistance);
        }
        else
        {
            target.y += direction.y *
                (bounds.extents.y + digDistance);
        }

        return target;
    }
}