using UnityEngine;

public class AllyRopeThrower : MonoBehaviour
{
    private enum RopeState
    {
        Idle,
        Throwing,
        Tethered
    }

    [Header("References")]
    [SerializeField] private Transform ropeOrigin;
    [SerializeField] private Transform ropeHead;
    [SerializeField] private LineRenderer ropeLine;

    [Header("Throw")]
    [SerializeField] private float throwRange = 26f;
    [SerializeField] private float throwSpeed = 35f;
    [SerializeField] private float hitDistance = 0.6f;
    [SerializeField] private float throwCooldown = 3f;

    [Header("Tether")]
    [SerializeField] private float maxRopeLength = 26f;
    [SerializeField] private float softRopeSag = 2f;
    [SerializeField] private int ropePointCount = 16;

    [Header("Ground Drag")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRayHeight = 10f;
    [SerializeField] private float groundRayDistance = 30f;
    [SerializeField] private float groundOffset = 0.08f;
    [SerializeField] private float groundDragDistance = 10f;
    [SerializeField] private float sagTransitionTime = 0.35f;

    private RopeState state;
    private Transform targetEnemy;
    private Rigidbody2D targetBody;
    private float nextThrowTime;
    private float currentSag;
    private float sagVelocity;
    public bool IsTethered =>
    state == RopeState.Tethered &&
    targetEnemy != null;

    public Transform TetheredEnemy =>
        IsTethered ? targetEnemy : null;

    public bool IsRopeActive =>
    state == RopeState.Throwing ||
    state == RopeState.Tethered;


    private void Awake()
    {
        if (ropeOrigin == null)
            ropeOrigin = transform;

        if (ropeLine == null)
            ropeLine = GetComponent<LineRenderer>();

        ropeLine.positionCount = ropePointCount;
        ropeLine.enabled = false;

        if (ropeHead != null)
            ropeHead.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (state == RopeState.Idle)
        {
            TryStartThrow();
            return;
        }

        if (targetEnemy == null)
        {
            ReleaseRope();
            return;
        }

        if (state == RopeState.Throwing)
        {
            ropeHead.position = Vector3.MoveTowards(
                ropeHead.position,
                targetEnemy.position,
                throwSpeed * Time.deltaTime
            );

            DrawRope(ropeHead.position);

            if (Vector2.Distance(
                ropeHead.position,
                targetEnemy.position
            ) <= hitDistance)
            {
                state = RopeState.Tethered;
                targetBody = targetEnemy.GetComponent<Rigidbody2D>();
            }
        }
        else if (state == RopeState.Tethered)
        {
            ropeHead.position = targetEnemy.position;
            DrawRope(targetEnemy.position);
        }
    }

    private void FixedUpdate()
    {
        if (state != RopeState.Tethered || targetBody == null)
            return;

        Vector2 anchorPosition = ropeOrigin.position;
        Vector2 direction = targetBody.position - anchorPosition;
        float distance = direction.magnitude;

        // 敌人向友军靠近时，不会进入这里，因此不会受限制。
        if (distance <= maxRopeLength)
            return;

        Vector2 outwardDirection = direction.normalized;

        // 敌人试图远离时，把它限制在绳子最大长度处。
        targetBody.position =
            anchorPosition + outwardDirection * maxRopeLength;

        float outwardSpeed = Vector2.Dot(
            targetBody.linearVelocity,
            outwardDirection
        );

        if (outwardSpeed > 0f)
        {
            targetBody.linearVelocity -=
                outwardDirection * outwardSpeed;
        }
    }

    private void TryStartThrow()
    {
        if (Time.time < nextThrowTime)
            return;

        EnemyHealth closestEnemy = FindClosestEnemy();

        if (closestEnemy == null)
            return;

        targetEnemy = closestEnemy.transform;
        state = RopeState.Throwing;

        ropeHead.position = ropeOrigin.position;
        ropeHead.gameObject.SetActive(true);
        ropeLine.enabled = true;
    }

    private EnemyHealth FindClosestEnemy()
    {
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(
            FindObjectsSortMode.None
        );

        EnemyHealth closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (EnemyHealth enemy in enemies)
        {
            float distance = Vector2.Distance(
                ropeOrigin.position,
                enemy.transform.position
            );

            if (distance <= throwRange &&
                distance < closestDistance)
            {
                closestEnemy = enemy;
                closestDistance = distance;
            }
        }

        return closestEnemy;
    }

    private void DrawRope(Vector3 endPosition)
    {
        Vector3 startPosition = ropeOrigin.position;

        float distance = Vector2.Distance(
            startPosition,
            endPosition
        );

        float tension = Mathf.Clamp01(
            distance / maxRopeLength
        );

        float naturalSag = Mathf.Lerp(
            softRopeSag,
            0.05f,
            tension
        );

        Vector3 midpoint = (startPosition + endPosition) * 0.5f;
        float groundY = GetRopeGroundY(midpoint);

        float middleHeight =
            (startPosition.y + endPosition.y) * 0.5f;

        float groundSag = Mathf.Max(
            naturalSag,
            middleHeight - groundY
        );

        // 在“拖地距离”的 1.5 倍开始逐渐变弯；
        // 距离小于其 0.5 倍时，完全贴到地面。
        float groundDragFactor = Mathf.InverseLerp(
            groundDragDistance * 1.5f,
            groundDragDistance * 0.5f,
            distance
        );

        float targetSag = Mathf.Lerp(
            naturalSag,
            groundSag,
            groundDragFactor
        );

        currentSag = Mathf.SmoothDamp(
            currentSag,
            targetSag,
            ref sagVelocity,
            sagTransitionTime
        );

        ropeLine.positionCount = ropePointCount;

        for (int i = 0; i < ropePointCount; i++)
        {
            float t = i / (float)(ropePointCount - 1);

            Vector3 point = Vector3.Lerp(
                startPosition,
                endPosition,
                t
            );

            point.y -= Mathf.Sin(t * Mathf.PI) * currentSag;

            // 不让绳子穿进地面，形成贴地效果。
            point.y = Mathf.Max(point.y, groundY);

            ropeLine.SetPosition(i, point);
        }
    }

    private float GetRopeGroundY(Vector3 midpoint)
    {
        Vector2 rayStart =
            midpoint + Vector3.up * groundRayHeight;

        RaycastHit2D hit = Physics2D.Raycast(
            rayStart,
            Vector2.down,
            groundRayHeight + groundRayDistance,
            groundLayer
        );

        if (hit.collider != null)
            return hit.point.y + groundOffset;

        return midpoint.y - softRopeSag;
    }

    private void ReleaseRope()
    {
        state = RopeState.Idle;
        targetEnemy = null;
        targetBody = null;
        nextThrowTime = Time.time + throwCooldown;

        currentSag = 0f;
        sagVelocity = 0f;

        ropeLine.enabled = false;

        if (ropeHead != null)
            ropeHead.gameObject.SetActive(false);
    }
}