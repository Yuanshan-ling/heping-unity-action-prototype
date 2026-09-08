using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyFacingByVelocity : MonoBehaviour
{
    [SerializeField] private SpriteRenderer enemyRenderer;

    [Min(0f)]
    [SerializeField] private float minimumHorizontalSpeed = 0.05f;

    private Rigidbody2D body;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();

        if (enemyRenderer == null)
            enemyRenderer = GetComponent<SpriteRenderer>();

        if (enemyRenderer == null)
            enemyRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (body == null || enemyRenderer == null)
            return;

        float horizontalSpeed = body.linearVelocity.x;

        // 速度接近0时保持原朝向，避免原地左右闪烁。
        if (Mathf.Abs(horizontalSpeed) <
            minimumHorizontalSpeed)
        {
            return;
        }

        // 向左移动时勾选Flip X，向右移动时取消。
        enemyRenderer.flipX = horizontalSpeed < 0f;
    }
}