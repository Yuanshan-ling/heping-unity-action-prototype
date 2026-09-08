using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SpecialEnemyPounce : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Transform player;
    [SerializeField] private EnemyAI enemyAI;
    [SerializeField] private SpriteRenderer enemyRenderer;

    [Header("扑击触发范围")]
    [Min(0f)]
    [SerializeField] private float minimumPounceDistance = 5f;

    [Min(0f)]
    [SerializeField] private float maximumPounceDistance = 12f;

    [Min(0f)]
    [SerializeField] private float pounceCooldown = 2.5f;

    [Header("Trigger Mode")]
    [SerializeField] private bool useAutomaticDistanceTrigger;

    [Header("扑击运动")]
    [Min(0.1f)]
    [SerializeField] private float pounceDuration = 0.8f;

    [Min(0f)]
    [SerializeField] private float jumpHeight = 6f;

    [Min(0f)]
    [SerializeField] private float passPlayerDistance = 4f;

    [Header("命中")]
    [Min(1)]
    [SerializeField] private int damage = 2;

    [Min(0.01f)]
    [SerializeField] private float hitRadius = 1.2f;

    private Rigidbody2D body;
    private PlayerHealth playerHealth;

    private bool pounceEnabled;
    private bool isPouncing;
    public bool IsPouncing => isPouncing;
    private float nextPounceTime;


    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();

        if (enemyAI == null)
            enemyAI = GetComponent<EnemyAI>();

        if (enemyRenderer == null)
    enemyRenderer = GetComponent<SpriteRenderer>();

        FindPlayer();
    }

    private void FixedUpdate()
    {
        if (!pounceEnabled ||
            !useAutomaticDistanceTrigger ||
            isPouncing)
            return;

        FindPlayer();

        if (player == null || Time.time < nextPounceTime)
            return;

        float distance = Mathf.Abs(
            player.position.x - transform.position.x
        );

        if (distance >= minimumPounceDistance &&
            distance <= maximumPounceDistance)
        {
            StartCoroutine(Pounce());
        }
    }

    public void EnablePounce()
    {
        pounceEnabled = true;
        nextPounceTime = Time.time + pounceCooldown;
        enabled = true;

        Debug.Log("SpecialEnemyPounce：已由 QTE 启用。");
    }

    public bool TryStartPounceFromDirector()
    {
        if (isPouncing)
            return false;

        enabled = true;
        pounceEnabled = true;
        FindPlayer();

        if (player == null)
            return false;

        StartCoroutine(Pounce());
        return true;
    }

    public void SetAutomaticDistanceTrigger(bool value)
    {
        useAutomaticDistanceTrigger = value;
    }

    private void FindPlayer()
    {
        if (player == null)
        {
            PlayerController controller =
                FindFirstObjectByType<PlayerController>();

            if (controller != null)
                player = controller.transform;
        }

        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();
    }

    private IEnumerator Pounce()
    {
        isPouncing = true;

        float previousHorizontalSpeed =
            body.linearVelocity.x;

        float lockedDirection;

        if (Mathf.Abs(previousHorizontalSpeed) > 0.05f)
        {
            lockedDirection =
                Mathf.Sign(previousHorizontalSpeed);
        }
        else if (player != null)
        {
            // 敌人静止时，改为朝主角所在方向跳。
            lockedDirection = Mathf.Sign(
                player.position.x - body.position.x
            );

            if (Mathf.Approximately(lockedDirection, 0f))
                lockedDirection = 1f;
        }
        else if (enemyRenderer != null)
        {
            lockedDirection =
                enemyRenderer.flipX ? -1f : 1f;
        }
        else
        {
            lockedDirection = 1f;
        }

        // 关键：将跳击方向同步到 SpriteRenderer。
        // 左为 true，右为 false，与 EnemyWeaponPose 的判断一致。
        if (enemyRenderer != null)
            enemyRenderer.flipX = lockedDirection < 0f;

        if (enemyAI != null)
            enemyAI.enabled = false;

        body.linearVelocity = Vector2.zero;

        Vector2 startPosition = body.position;

        float distanceToPlayer = player != null
            ? Mathf.Abs(player.position.x - startPosition.x)
            : minimumPounceDistance;

        float travelDistance =
            distanceToPlayer + passPlayerDistance;

        Vector2 endPosition = new Vector2(
            startPosition.x +
                lockedDirection * travelDistance,
            startPosition.y
        );

        bool hasHitPlayer = false;
        float elapsed = 0f;

        while (elapsed < pounceDuration)
        {
            float progress = Mathf.Clamp01(
                elapsed / pounceDuration
            );

            float horizontalPosition = Mathf.Lerp(
                startPosition.x,
                endPosition.x,
                progress
            );

            float verticalPosition =
                Mathf.Lerp(
                    startPosition.y,
                    endPosition.y,
                    progress
                ) +
                4f * jumpHeight *
                progress * (1f - progress);

            body.MovePosition(new Vector2(
                horizontalPosition,
                verticalPosition
            ));

            if (!hasHitPlayer && playerHealth != null)
            {
                Collider2D[] hits =
                    Physics2D.OverlapCircleAll(
                        body.position,
                        hitRadius
                    );

                foreach (Collider2D hit in hits)
                {
                    PlayerHealth hitHealth =
                        hit.GetComponentInParent<PlayerHealth>();

                    if (hitHealth == playerHealth)
                    {
                        playerHealth.TakeDamage(damage);
                        hasHitPlayer = true;
                        break;
                    }
                }
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        body.MovePosition(endPosition);
        body.linearVelocity = Vector2.zero;

        // 落地后再次保证武器使用正确的左右朝向。
        if (enemyRenderer != null)
            enemyRenderer.flipX = lockedDirection < 0f;

        if (enemyAI != null)
            enemyAI.enabled = true;

        nextPounceTime =
            Time.time + pounceCooldown;

        isPouncing = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}
