using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerTreeChop : MonoBehaviour
{
    [Header("¿³Ê÷·¶Î§")]
    [Min(0.1f)]
    [SerializeField] private float chopRange = 7f;

    private PlayerInventory inventory;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();

        if (inventory == null)
            inventory = FindFirstObjectByType<PlayerInventory>();
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.oKey.wasPressedThisFrame)
        {
            ChopNearestTree();
        }
    }

    private void ChopNearestTree()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            chopRange
        );

        TreeHealth nearestTree = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            TreeHealth tree =
                hit.GetComponentInParent<TreeHealth>();

            if (tree == null)
                continue;

            float distance = Vector2.Distance(
                transform.position,
                tree.transform.position
            );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTree = tree;
            }
        }

        if (nearestTree != null)
            nearestTree.TryChop(inventory);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(
            transform.position,
            chopRange
        );
    }
}