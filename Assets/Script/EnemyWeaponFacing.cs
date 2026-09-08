using UnityEngine;

public class EnemyWeaponFacing : MonoBehaviour
{
    [SerializeField] private Rigidbody2D enemyBody;
    [SerializeField] private SpriteRenderer enemyRenderer;

    [SerializeField] private bool reverseWeaponVisual = true;

    [Min(0f)]
    [SerializeField] private float minimumSpeed = 0.05f;

    private float horizontalDistance;
    private float heightOffset;
    private float facingDirection = 1f;
    private Vector3 originalScale;
    private bool throwMirrorActive;

    private void Awake()
    {
        if (enemyBody == null)
            enemyBody = GetComponentInParent<Rigidbody2D>();

        if (enemyRenderer == null && enemyBody != null)
        {
            enemyRenderer =
                enemyBody.GetComponent<SpriteRenderer>();
        }

        originalScale = transform.localScale;

        if (enemyBody != null)
        {
            horizontalDistance = Mathf.Abs(
                transform.position.x -
                enemyBody.position.x
            );

            heightOffset =
                transform.position.y -
                enemyBody.position.y;
        }

        if (enemyRenderer != null)
        {
            facingDirection =
                enemyRenderer.flipX ? -1f : 1f;
        }
    }

    public void SetThrowMirror(bool active)
    {
        throwMirrorActive = active;
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

        // 使用世界坐标，避免父物体Scale把左右反转。
        transform.position = new Vector3(
            enemyPosition.x +
                facingDirection * horizontalDistance,
            enemyPosition.y + heightOffset,
            transform.position.z
        );

        Vector3 scale = originalScale;

        float visualDirection = facingDirection;

        // 正常镜像与投技镜像进行一次反转。
        // 投技开启时，武器会变成正常状态的左右相反版本。
        bool shouldReverseVisual =
            reverseWeaponVisual ^ throwMirrorActive;

        if (shouldReverseVisual)
            visualDirection *= -1f;

        scale.x =
            Mathf.Abs(originalScale.x) *
            visualDirection;

        transform.localScale = scale;
    }
}
