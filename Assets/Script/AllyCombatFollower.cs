using UnityEngine;

public class AllyCombatFollower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D unitCollider;
    [SerializeField] private AllyRopeThrower ropeThrower;
    [SerializeField] private AllySpearFighter spearFighter;


    [Header("Follow Player")]
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float enemyChaseSpeed = 18f;
    [SerializeField] private float followTolerance = 0.5f;

    [Header("Find Enemy")]
    [SerializeField] private float enemySearchRange = 35f;
    [SerializeField] private float playerEnemyAlertRange = 22f;
    [SerializeField] private float combatStandOffDistance = 10f;

    [Header("Auto Jump")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float jumpSpeed = 27f;
    [SerializeField] private float groundCheckHeight = 0.25f;
    [SerializeField] private float obstacleCheckHeight = 5f;
    [SerializeField] private float obstacleCheckDistance = 1.5f;

    private Vector2 formationOffset;
    private float originalScaleX;

    private void Awake()
    {
        if (body == null)
            body = GetComponent<Rigidbody2D>();

        if (unitCollider == null)
            unitCollider = GetComponent<Collider2D>();

        if (ropeThrower == null)
            ropeThrower = GetComponent<AllyRopeThrower>();

        if (spearFighter == null)
            spearFighter = GetComponent<AllySpearFighter>();

        if (player == null)
        {
            PlayerController controller =
                FindFirstObjectByType<PlayerController>();

            if (controller != null)
                player = controller.transform;
        }

        originalScaleX = Mathf.Abs(transform.localScale.x);
    }

    public void Configure(
        Transform newPlayer,
        Vector2 newFormationOffset
    )
    {
        player = newPlayer;
        formationOffset = newFormationOffset;
    }

    private void FixedUpdate()
    {
        if (body == null || player == null)
            return;


        if (ropeThrower != null &&
            ropeThrower.IsRopeActive)
        {
            body.linearVelocity = new Vector2(
                0f,
                body.linearVelocity.y
            );
            return;
        }

        EnemyHealth enemy = ropeThrower == null
            ? FindClosestEnemy()
            : null; float moveDirection = 0f;
        float activeMoveSpeed = moveSpeed;

        if (enemy != null)
        {
            activeMoveSpeed = enemyChaseSpeed;

            float horizontalDistance =
                enemy.transform.position.x - transform.position.x;

            if (Mathf.Abs(horizontalDistance) > 0.01f)
            {
                transform.localScale = new Vector3(
                    originalScaleX *
                        Mathf.Sign(horizontalDistance),
                    transform.localScale.y,
                    transform.localScale.z
                );
            }

            if (Mathf.Abs(horizontalDistance) >
                combatStandOffDistance)
            {
                moveDirection = Mathf.Sign(horizontalDistance);
            }
        }

        else
        {
            float followX =
                player.position.x + formationOffset.x;

            float horizontalDistance =
                followX - transform.position.x;

            if (Mathf.Abs(horizontalDistance) >
                followTolerance)
            {
                moveDirection = Mathf.Sign(horizontalDistance);
            }
        }

        body.linearVelocity = new Vector2(
        moveDirection * activeMoveSpeed,
            body.linearVelocity.y
        );

        if (Mathf.Abs(moveDirection) > 0.01f)
        {
            transform.localScale = new Vector3(
                originalScaleX * Mathf.Sign(moveDirection),
                transform.localScale.y,
                transform.localScale.z
            );

            if (IsGrounded() &&
                HasObstacleAhead(moveDirection))
            {
                body.linearVelocity = new Vector2(
                    body.linearVelocity.x,
                    jumpSpeed
                );
            }
        }
    }

    private EnemyHealth FindTetheredEnemy()
    {
        AllyRopeThrower[] ropeThrowers =
            FindObjectsByType<AllyRopeThrower>(
                FindObjectsSortMode.None
            );

        EnemyHealth closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (AllyRopeThrower rope in ropeThrowers)
        {
            Transform tetheredTarget =
                rope.TetheredEnemy;

            if (tetheredTarget == null)
                continue;

            EnemyHealth enemy =
                tetheredTarget.GetComponent<EnemyHealth>();

            if (enemy == null)
                continue;

            float distance = Vector2.Distance(
                transform.position,
                enemy.transform.position
            );

            if (distance <= enemySearchRange &&
                distance < closestDistance)
            {
                closestEnemy = enemy;
                closestDistance = distance;
            }
        }

        return closestEnemy;
    }

    private EnemyHealth FindClosestEnemy()
    {
        if (spearFighter != null)
        {
            EnemyHealth tetheredEnemy =
                FindTetheredEnemy();

            if (tetheredEnemy != null)
                return tetheredEnemy;
        }

        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(
            FindObjectsSortMode.None
        );

        EnemyHealth closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (EnemyHealth enemy in enemies)
        {
            float distanceFromPlayer = Vector2.Distance(
                player.position,
                enemy.transform.position
            );

            if (distanceFromPlayer > playerEnemyAlertRange)
                continue;

            float distanceFromAlly = Vector2.Distance(
                transform.position,
                enemy.transform.position
            );

            if (distanceFromAlly > enemySearchRange)
                continue;

            if (distanceFromAlly < closestDistance)
            {
                closestEnemy = enemy;
                closestDistance = distanceFromAlly;
            }
        }

        return closestEnemy;
    }

    private bool IsGrounded()
    {
        if (unitCollider == null)
            return false;

        Bounds bounds = unitCollider.bounds;

        Vector2 checkCenter = new Vector2(
            bounds.center.x,
            bounds.min.y + 0.05f
        );

        Vector2 checkSize = new Vector2(
            bounds.size.x * 0.7f,
            groundCheckHeight
        );

        return Physics2D.OverlapBox(
            checkCenter,
            checkSize,
            0f,
            groundLayer
        ) != null;
    }

    private bool HasObstacleAhead(float direction)
    {
        if (unitCollider == null)
            return false;

        Bounds bounds = unitCollider.bounds;

        Vector2 castOrigin = new Vector2(
            bounds.center.x +
            Mathf.Sign(direction) * bounds.extents.x,
            bounds.min.y + obstacleCheckHeight * 0.5f
        );

        Vector2 castSize = new Vector2(
            0.25f,
            obstacleCheckHeight
        );

        RaycastHit2D hit = Physics2D.BoxCast(
            castOrigin,
            castSize,
            0f,
            new Vector2(Mathf.Sign(direction), 0f),
            obstacleCheckDistance,
            groundLayer
        );

        return hit.collider != null;
    }
}