using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform patrolLeft;
    [SerializeField] private Transform patrolRight;
    [SerializeField] private LayerMask groundLayer;

    [Header("Random Patrol")]
    [SerializeField] private float decisionIntervalMin = 1.5f;
    [SerializeField] private float decisionIntervalMax = 4f;
    [Range(0f, 1f)]
    [SerializeField] private float pauseChance = 0.35f;
    [Range(0f, 5f)]
    [SerializeField] private float pauseDurationMin = 0.5f;
    [Range(0f, 5f)]
    [SerializeField] private float pauseDurationMax = 4f;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 4f;
    [SerializeField] private float chaseSpeed = 7f;
    [SerializeField] private float alertRange = 20f;
    [SerializeField] private float jumpSpeed = 18f;
    [SerializeField] private float jumpCooldown = 0.5f;
    [SerializeField] private float obstacleCheckDistance = 1f;
    [Header("Flee")]
    [Min(0f)]
    [SerializeField] private float fleeRange = 16f;
    [SerializeField] private float fleeSpeed = 22f;
    [Min(0f)]
    [SerializeField] private float fleeReleaseRange = 30f;

    private bool isFleeingFromThreat;
    [Header("Attack")]
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackVerticalRange = 8f;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackCooldown = 1f;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private SpriteRenderer spriteRenderer;
    private PlayerHealth playerHealth;

    private bool facingRight = true;
    public bool IsChasing { get; private set; }
    private float nextAttackTime;
    private float nextJumpTime;

    private float nextPatrolDecisionTime;
    private float stopPatrolUntilTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();
        ScheduleNextPatrolDecision();
    }

    public void Configure(
    Transform newPlayer,
    Transform newPatrolLeft,
    Transform newPatrolRight,
    LayerMask newGroundLayer
)
    {
        player = newPlayer;
        patrolLeft = newPatrolLeft;
        patrolRight = newPatrolRight;
        groundLayer = newGroundLayer;

        playerHealth = player != null
            ? player.GetComponent<PlayerHealth>()
            : null;
    }

    private void FixedUpdate()
    {
        if (player == null || rb == null || bodyCollider == null)
            return;

        if (playerHealth == null)
            playerHealth = player.GetComponent<PlayerHealth>();

        if (TryGetNearestThreat(out Transform threat))
        {
            IsChasing = false;

            float fleeDirection = Mathf.Sign(
                transform.position.x -
                threat.position.x
            );

            if (Mathf.Abs(fleeDirection) < 0.01f)
                fleeDirection = facingRight ? 1f : -1f;

            MoveInDirection(fleeDirection, fleeSpeed);
            TryJumpOverObstacle();
            return;
        }

        float playerDistance = Vector2.Distance(
            transform.position,
            player.position
        );

        if (IsPlayerInAttackRange())
        {
            IsChasing = false;
            StopMoving();
            FaceDirection(player.position.x - transform.position.x);
            TryAttack();
            return;
        }

        if (playerDistance <= alertRange)
        {
            IsChasing = true;

            MoveInDirection(
                Mathf.Sign(
                    player.position.x -
                    transform.position.x
                ),
                chaseSpeed
            );
        }
        else
        {
            IsChasing = false;
            Patrol();
        }
        TryJumpOverObstacle();
    }

    private bool TryGetNearestThreat(
        out Transform nearestThreat
    )
    {
        nearestThreat = null;

        float checkRange = isFleeingFromThreat
            ? Mathf.Max(fleeRange, fleeReleaseRange)
            : fleeRange;

        float nearestDistance = checkRange;

        if (player != null)
        {
            float playerDistance = Vector2.Distance(
                transform.position,
                player.position
            );

            if (playerDistance <= nearestDistance)
            {
                nearestThreat = player;
                nearestDistance = playerDistance;
            }
        }

        AllyCombatFollower[] allies =
            FindObjectsByType<AllyCombatFollower>(
                FindObjectsSortMode.None
            );

        foreach (AllyCombatFollower ally in allies)
        {
            if (ally == null)
                continue;

            // 长矛兵负责攻击，不触发敌人逃跑。
            if (ally.GetComponent<AllySpearFighter>() != null)
                continue;

            float allyDistance = Vector2.Distance(
                transform.position,
                ally.transform.position
            );

            if (allyDistance <= nearestDistance)
            {
                nearestThreat = ally.transform;
                nearestDistance = allyDistance;
            }
        }

        isFleeingFromThreat = nearestThreat != null;
        return isFleeingFromThreat;
    }

    private void Patrol()
    {
        if (patrolLeft == null || patrolRight == null)
        {
            StopMoving();
            return;
        }

        if (Time.time < stopPatrolUntilTime)
        {
            StopMoving();
            return;
        }

        if (facingRight &&
            transform.position.x >= patrolRight.position.x)
        {
            facingRight = false;
            ScheduleNextPatrolDecision();
        }
        else if (!facingRight &&
                 transform.position.x <= patrolLeft.position.x)
        {
            facingRight = true;
            ScheduleNextPatrolDecision();
        }

        if (Time.time >= nextPatrolDecisionTime)
        {
            if (Random.value < pauseChance)
            {
                float minPause = Mathf.Min(
                    pauseDurationMin,
                    pauseDurationMax
                );

                float maxPause = Mathf.Max(
                    pauseDurationMin,
                    pauseDurationMax
                );

                stopPatrolUntilTime = Time.time +
                    Random.Range(minPause, maxPause);

                ScheduleNextPatrolDecision();
                StopMoving();
                return;
            }

            facingRight = !facingRight;
            ScheduleNextPatrolDecision();
        }

        MoveInDirection(facingRight ? 1f : -1f, patrolSpeed);
    }

    private void ScheduleNextPatrolDecision()
    {
        float minInterval = Mathf.Min(
            decisionIntervalMin,
            decisionIntervalMax
        );

        float maxInterval = Mathf.Max(
            decisionIntervalMin,
            decisionIntervalMax
        );

        nextPatrolDecisionTime = Time.time +
            Random.Range(minInterval, maxInterval);
    }

    private void MoveInDirection(float direction, float speed)
    {
        if (Mathf.Abs(direction) < 0.01f)
        {
            StopMoving();
            return;
        }

        FaceDirection(direction);

        rb.linearVelocity = new Vector2(
            direction * speed,
            rb.linearVelocity.y
        );
    }

    private void StopMoving()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void FaceDirection(float direction)
    {
        if (Mathf.Abs(direction) < 0.01f)
            return;

        facingRight = direction > 0f;

        if (spriteRenderer != null)
            spriteRenderer.flipX = !facingRight;
    }

    private void TryJumpOverObstacle()
    {
        if (Time.time < nextJumpTime ||
            !IsGrounded() ||
            !IsObstacleAhead())
        {
            return;
        }

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            jumpSpeed
        );

        nextJumpTime = Time.time + jumpCooldown;
    }

    private bool IsGrounded()
    {
        Bounds bounds = bodyCollider.bounds;

        Vector2 origin = new Vector2(
            bounds.center.x,
            bounds.min.y + 0.03f
        );

        Vector2 size = new Vector2(
            bounds.size.x * 0.75f,
            0.1f
        );

        return Physics2D.BoxCast(
            origin,
            size,
            0f,
            Vector2.down,
            0.08f,
            groundLayer
        );
    }

    private bool IsObstacleAhead()
    {
        Bounds bounds = bodyCollider.bounds;

        float direction = facingRight ? 1f : -1f;

        Vector2 boxSize = new Vector2(
            bounds.size.x * 0.9f,
            bounds.size.y * 0.9f
        );

        RaycastHit2D hit = Physics2D.BoxCast(
            bounds.center,
            boxSize,
            0f,
            Vector2.right * direction,
            obstacleCheckDistance,
            groundLayer
        );

        return hit.collider != null;
    }

    private bool IsPlayerInAttackRange()
    {
        return Mathf.Abs(
                   player.position.x - transform.position.x
               ) <= attackRange &&
               Mathf.Abs(
                   player.position.y - transform.position.y
               ) <= attackVerticalRange;
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime || playerHealth == null)
            return;

        playerHealth.TakeDamage(attackDamage);
        nextAttackTime = Time.time + attackCooldown;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, alertRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}