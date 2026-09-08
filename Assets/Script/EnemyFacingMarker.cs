using UnityEngine;

public class EnemyFacingMarker : MonoBehaviour
{
    [SerializeField] private Rigidbody2D enemyBody;
    [SerializeField] private SpriteRenderer enemyRenderer;

    [Min(0f)]
    [SerializeField] private float markerDistance = 2f;

    [SerializeField] private float heightOffset = 0f;

    [Min(0f)]
    [SerializeField] private float minimumSpeed = 0.05f;

    private float facingDirection = 1f;

    private void Awake()
    {
        if (enemyBody == null)
            enemyBody = GetComponentInParent<Rigidbody2D>();

        if (enemyRenderer == null && enemyBody != null)
        {
            enemyRenderer =
                enemyBody.GetComponent<SpriteRenderer>();
        }

        if (enemyRenderer != null)
        {
            facingDirection =
                enemyRenderer.flipX ? -1f : 1f;
        }
    }

    private void LateUpdate()
    {
        if (enemyBody == null)
            return;

        float horizontalSpeed =
            enemyBody.linearVelocity.x;

        if (Mathf.Abs(horizontalSpeed) >= minimumSpeed)
        {
            facingDirection =
                Mathf.Sign(horizontalSpeed);
        }

        Vector3 enemyPosition =
            enemyBody.transform.position;

        transform.position = new Vector3(
            enemyPosition.x +
                facingDirection * markerDistance,
            enemyPosition.y + heightOffset,
            transform.position.z
        );
    }
}