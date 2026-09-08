using UnityEngine;

public class CannonBall : MonoBehaviour
{
    [Header("炮弹参数")]
    [SerializeField] private int damage = 5;
    [SerializeField] private float lifetime = 5f;

    [Header("爆炸特效")]
    [SerializeField] private GameObject explosionPrefab;

    private Rigidbody2D body;
    private bool hasExploded;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector2 velocity)
    {
        if (body != null)
            body.linearVelocity = velocity;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded)
            return;

        if (other.GetComponentInParent<PlayerController>() != null)
            return;

        EnemyHealth enemy =
            other.GetComponentInParent<EnemyHealth>();

        // 敌人的 Collider 即使是 Trigger，也允许受击。
        if (enemy != null)
        {
            enemy.TakeDamage(damage);

            Vector2 enemyHitPoint = other.ClosestPoint(
                transform.position
            );

            Explode(enemyHitPoint);
            return;
        }

        // 非敌人的 Trigger，例如警戒范围，继续忽略。
        if (other.isTrigger)
            return;

        Vector2 groundHitPoint = other.ClosestPoint(
            transform.position
        );

        Explode(groundHitPoint);
    }

    private void Explode(Vector2 hitPoint)
    {
        if (hasExploded)
            return;

        hasExploded = true;

        if (explosionPrefab != null)
        {
            Instantiate(
                explosionPrefab,
                hitPoint,
                Quaternion.identity
            );
        }

        Destroy(gameObject);
    }
}