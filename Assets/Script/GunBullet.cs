using UnityEngine;

public class GunBullet : MonoBehaviour
{
    [Header("×Óµ¯²ÎÊý")]
    [SerializeField] private float speed = 35f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private int damage = 2;

    private Rigidbody2D body;
    private bool hasHit;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();

        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.right;

        direction = direction.normalized;

        transform.right = direction;

        if (body != null)
            body.linearVelocity = direction * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit)
            return;

        EnemyHealth enemy =
            other.GetComponentInParent<EnemyHealth>();

        if (enemy == null)
            return;

        hasHit = true;
        enemy.TakeDamage(damage);

        Destroy(gameObject);
    }
}