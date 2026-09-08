using UnityEngine;

public class EnemyWeaponPose : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyAI enemyAI;
    [SerializeField] private SpecialEnemyPounce enemyPounce;
    [SerializeField] private Rigidbody2D enemyBody;
    [SerializeField] private SpriteRenderer enemyRenderer;

    [Header("Pose Angles")]
    [Min(0f)]
    [SerializeField] private float patrolForwardAngle = 25f;

    [Min(0f)]
    [SerializeField] private float chaseForwardAngle = 120f;

    [Min(0f)]
    [SerializeField] private float pounceForwardAngle = 90f;

    [Header("Transition")]
    [Min(0f)]
    [SerializeField] private float rotationSpeed = 240f;

    [Min(0f)]
    [SerializeField] private float minimumSpeed = 0.05f;

    private float facingDirection = 1f;

    private void Awake()
    {
        if (enemyAI == null)
            enemyAI = GetComponentInParent<EnemyAI>();

        if (enemyPounce == null)
        {
            enemyPounce =
                GetComponentInParent<SpecialEnemyPounce>();
        }

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
        UpdateFacingDirection();

        float angleMagnitude = patrolForwardAngle;

        if (enemyPounce != null &&
            enemyPounce.IsPouncing)
        {
            angleMagnitude = pounceForwardAngle;
        }
        else if (enemyAI != null &&
                 enemyAI.IsChasing)
        {
            angleMagnitude = chaseForwardAngle;
        }

        float signedAngle;

        if (facingDirection > 0f)
        {
            // µÐÈËÏòÓÒ£ºÎäÆ÷Ë³Ê±ÕëÐý×ª
            signedAngle = -angleMagnitude;
        }
        else
        {
            // µÐÈËÏò×ó£ºÎäÆ÷ÄæÊ±ÕëÐý×ª
            signedAngle = angleMagnitude;
        }

        Quaternion targetRotation =
            Quaternion.Euler(0f, 0f, signedAngle);

        transform.localRotation =
            Quaternion.RotateTowards(
                transform.localRotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
    }

    private void UpdateFacingDirection()
    {
        if (enemyBody != null &&
            Mathf.Abs(enemyBody.linearVelocity.x) >=
            minimumSpeed)
        {
            facingDirection =
                Mathf.Sign(enemyBody.linearVelocity.x);

            return;
        }

        if (enemyRenderer != null)
        {
            facingDirection =
                enemyRenderer.flipX ? -1f : 1f;
        }
    }
}