using System.Collections;
using UnityEngine;

// 完整版：包含冲锋、命中伤害、Space 闪避暂停和 J 空中反击恢复接口。

[RequireComponent(typeof(Rigidbody2D))]
public class SpecialEnemyChargeAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private EnemyAI enemyAI;
    [SerializeField] private SpecialEnemyPounce enemyPounce;
    [SerializeField] private SpriteRenderer enemyRenderer;

    [SerializeField] private Transform weaponRig;
    [SerializeField] private Transform weaponMotionRoot;
    [SerializeField] private EnemyWeaponPose weaponPose;

    [Header("Trigger")]
    [Min(0f)]
    [SerializeField] private float minimumAttackDistance = 5f;

    [Min(0f)]
    [SerializeField] private float maximumAttackDistance = 12f;

    [Min(0f)]
    [SerializeField] private float maximumVerticalDistance = 4f;

    [Min(0f)]
    [SerializeField] private float attackCooldown = 5f;

    [Min(0f)]
    [SerializeField] private float firstAttackDelay = 2f;

    [Header("Trigger Mode")]
    [SerializeField] private bool useAutomaticDistanceTrigger;

    [Header("Weapon Preparation")]
    [Header("Charge Wind-up")]
    [Tooltip("冲锋前武器上举的角度。右向使用负角度，左向使用正角度。")]
    [Range(0f, 180f)]
    [SerializeField] private float windupRaisedAngle = 160f;

    [Tooltip("武器举到最高点所需时间。")]
    [Min(0.01f)]
    [SerializeField] private float windupRaiseDuration = 0.22f;

    [Tooltip("武器举起后、开始落下准备冲锋前的停顿时间。")]
    [Min(0f)]
    [SerializeField] private float windupHoldDuration = 0.15f;

    [Min(0f)]
    [SerializeField] private float levelAngle = 90f;

    [Min(0.01f)]
    [SerializeField] private float levelDuration = 0.12f;

    [Min(0f)]
    [SerializeField] private float pullBackDistance = 1.2f;

    [Min(0.01f)]
    [SerializeField] private float pullBackDuration = 0.18f;

    [Min(0f)]
    [SerializeField] private float pauseDuration = 0.15f;

    [Min(0f)]
    [SerializeField] private float stabDistance = 1.8f;

    [Min(0.01f)]
    [SerializeField] private float stabDuration = 0.09f;

    [Header("Charge")]
    [Min(0f)]
    [SerializeField] private float passPlayerDistance = 5f;

    [Min(0.01f)]
    [SerializeField] private float chargeDuration = 0.35f;

    [Header("Damage")]
    [Min(1)]
    [SerializeField] private int damage = 2;

    [Min(0.01f)]
    [SerializeField] private float hitRadius = 1.5f;

    private Rigidbody2D body;
    private PlayerHealth playerHealth;

    private bool attackEnabled;
    private bool isAttacking;
    private bool isChargingForward;
    private bool pausedForAirCounter;
    private bool rampRedirectRequested;
    private float nextAttackTime;

    private float currentChargeDirection;
    private Vector2 redirectRampBase;
    private Vector2 redirectRampTop;
    private float redirectApproachDuration;
    private float redirectRampDuration;
    private float redirectVerticalDistance;
    private float redirectVerticalDuration;
    private float redirectHoverDuration;

    public bool IsAttacking => isAttacking;

    // 只有敌人真正向前穿过主角时才为 true。
    // 武器准备、后拉、暂停和前刺阶段均为 false。
    public bool IsChargingForward => isChargingForward;
    public bool IsPausedForAirCounter => pausedForAirCounter;
    public float CurrentChargeDirection => currentChargeDirection;

    public event System.Action ReachedRampTop;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();

        if (enemyAI == null)
            enemyAI = GetComponent<EnemyAI>();

        if (enemyPounce == null)
            enemyPounce = GetComponent<SpecialEnemyPounce>();

        if (enemyRenderer == null)
            enemyRenderer = GetComponent<SpriteRenderer>();

        if (weaponPose == null)
        {
            weaponPose =
                GetComponentInChildren<EnemyWeaponPose>(true);
        }

        if (weaponRig == null && weaponPose != null)
            weaponRig = weaponPose.transform;

        if (weaponMotionRoot == null &&
            weaponRig != null)
        {
            weaponMotionRoot =
                weaponRig.Find("WeaponMotionRoot");
        }

        FindPlayer();
    }

    private void OnDisable()
    {
        // 如果组件在冲锋途中被关闭，不要留下错误状态。
        isChargingForward = false;
        pausedForAirCounter = false;
        rampRedirectRequested = false;
        currentChargeDirection = 0f;
    }

    private void FixedUpdate()
    {
        if (!attackEnabled ||
            !useAutomaticDistanceTrigger ||
            isAttacking ||
            Time.time < nextAttackTime)
        {
            return;
        }

        FindPlayer();

        if (player == null)
            return;

        if (enemyPounce != null &&
            enemyPounce.IsPouncing)
        {
            return;
        }

        float horizontalDistance = Mathf.Abs(
            player.position.x - transform.position.x
        );

        float verticalDistance = Mathf.Abs(
            player.position.y - transform.position.y
        );

        if (horizontalDistance >= minimumAttackDistance &&
            horizontalDistance <= maximumAttackDistance &&
            verticalDistance <= maximumVerticalDistance)
        {
            StartCoroutine(PerformChargeAttack());
        }
    }

    public void EnableChargeAttack()
    {
        attackEnabled = true;
        enabled = true;
        nextAttackTime =
            Time.time + firstAttackDelay;
    }

    public bool TryStartChargeFromDirector()
    {
        if (isAttacking)
            return false;

        enabled = true;
        attackEnabled = true;
        FindPlayer();

        if (player == null)
            return false;

        StartCoroutine(PerformChargeAttack());
        return true;
    }

    public void SetAutomaticDistanceTrigger(bool value)
    {
        useAutomaticDistanceTrigger = value;
    }

    public void DisableChargeAttack()
    {
        attackEnabled = false;
    }

    public void PauseForAirCounter()
    {
        if (!isAttacking)
            return;

        pausedForAirCounter = true;
        isChargingForward = false;

        if (body != null)
            body.linearVelocity = Vector2.zero;

        if (enemyAI != null)
            enemyAI.enabled = false;

        if (enemyPounce != null)
            enemyPounce.enabled = false;

        Debug.Log(
            "SpecialEnemyChargeAttack：敌人已停住，等待主角空中反击。"
        );
    }

    public void ResumeAfterAirCounter()
    {
        if (!pausedForAirCounter)
            return;

        pausedForAirCounter = false;

        Debug.Log(
            "SpecialEnemyChargeAttack：空中反击结束，敌人恢复行动。"
        );
    }

    public bool TryRedirectChargeUpward(
        Vector2 rampBase,
        Vector2 rampTop,
        float approachDuration,
        float rampDuration,
        float verticalDistance,
        float verticalDuration,
        float hoverDuration
    )
    {
        if (!isAttacking ||
            !isChargingForward ||
            pausedForAirCounter ||
            rampRedirectRequested)
        {
            return false;
        }

        redirectRampBase = rampBase;
        redirectRampTop = rampTop;
        redirectApproachDuration = Mathf.Max(
            approachDuration,
            0.01f
        );
        redirectRampDuration = Mathf.Max(
            rampDuration,
            0.01f
        );
        redirectVerticalDistance = Mathf.Max(
            verticalDistance,
            0f
        );
        redirectVerticalDuration = Mathf.Max(
            verticalDuration,
            0.01f
        );
        redirectHoverDuration = Mathf.Max(
            hoverDuration,
            0f
        );

        rampRedirectRequested = true;
        isChargingForward = false;

        if (body != null)
            body.linearVelocity = Vector2.zero;

        Debug.Log(
            "SpecialEnemyChargeAttack：冲锋被蓝色斜坡引导向上。"
        );

        return true;
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
        {
            playerHealth =
                player.GetComponent<PlayerHealth>();
        }
    }

    private IEnumerator PerformChargeAttack()
    {
        isAttacking = true;
        isChargingForward = false;
        rampRedirectRequested = false;

        float attackDirection = Mathf.Sign(
            player.position.x - transform.position.x
        );

        if (Mathf.Approximately(attackDirection, 0f))
            attackDirection = 1f;

        currentChargeDirection = attackDirection;

        if (enemyAI != null)
            enemyAI.enabled = false;

        if (enemyPounce != null)
            enemyPounce.enabled = false;

        body.linearVelocity = Vector2.zero;

        if (enemyRenderer != null)
        {
            enemyRenderer.flipX =
                attackDirection < 0f;
        }

        if (weaponPose != null)
            weaponPose.enabled = false;

        float signedLevelAngle =
            attackDirection > 0f
                ? -levelAngle
                : levelAngle;

        // 冲锋前摇：先把武器举到最高点，短暂停顿，再落入原有的冲锋预备姿势。
        // 使用和既有武器角度相同的左右对称规则，敌人向左、向右时都会朝对应方向举武器。
        float signedRaisedAngle =
            attackDirection > 0f
                ? windupRaisedAngle
                : -windupRaisedAngle;

        yield return RotateWeaponTo(
            signedRaisedAngle,
            windupRaiseDuration
        );

        if (windupHoldDuration > 0f)
            yield return new WaitForSeconds(
                windupHoldDuration
            );

        yield return RotateWeaponTo(
            signedLevelAngle,
            levelDuration
        );

        yield return MoveWeaponToOffset(
            -attackDirection * pullBackDistance,
            pullBackDuration
        );

        yield return new WaitForSeconds(pauseDuration);

        yield return MoveWeaponToOffset(
            attackDirection * stabDistance,
            stabDuration
        );

        // 从这里开始，Space 冲锋闪避窗口才允许开启。
        isChargingForward = true;

        yield return ChargeThroughPlayer(
            attackDirection
        );

        isChargingForward = false;
        body.linearVelocity = Vector2.zero;

        while (pausedForAirCounter)
        {
            body.linearVelocity = Vector2.zero;
            yield return null;
        }

        float turnedDirection = -attackDirection;

        if (enemyRenderer != null)
        {
            enemyRenderer.flipX =
                turnedDirection < 0f;
        }

        if (weaponMotionRoot != null)
        {
            weaponMotionRoot.localPosition =
                Vector3.zero;

            weaponMotionRoot.localRotation =
                Quaternion.identity;
        }

        if (weaponPose != null)
            weaponPose.enabled = true;

        if (enemyPounce != null)
            enemyPounce.EnablePounce();

        if (enemyAI != null)
            enemyAI.enabled = true;

        nextAttackTime =
            Time.time + attackCooldown;

        currentChargeDirection = 0f;
        isAttacking = false;
    }

    private IEnumerator RotateWeaponTo(
        float targetAngle,
        float duration
    )
    {
        if (weaponRig == null)
            yield break;

        Quaternion startRotation =
            weaponRig.localRotation;

        Quaternion targetRotation =
            Quaternion.Euler(
                0f,
                0f,
                targetAngle
            );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = Mathf.Clamp01(
                elapsed / duration
            );

            weaponRig.localRotation =
                Quaternion.Lerp(
                    startRotation,
                    targetRotation,
                    progress
                );

            elapsed += Time.deltaTime;
            yield return null;
        }

        weaponRig.localRotation =
            targetRotation;
    }

    private IEnumerator MoveWeaponToOffset(
        float horizontalOffset,
        float duration
    )
    {
        if (weaponRig == null ||
            weaponMotionRoot == null)
        {
            yield break;
        }

        Vector3 startPosition =
            weaponMotionRoot.position;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = Mathf.Clamp01(
                elapsed / duration
            );

            Vector3 targetPosition =
                weaponRig.position +
                Vector3.right * horizontalOffset;

            weaponMotionRoot.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    progress
                );

            elapsed += Time.deltaTime;
            yield return null;
        }

        weaponMotionRoot.position =
            weaponRig.position +
            Vector3.right * horizontalOffset;
    }

    private IEnumerator ChargeThroughPlayer(
        float attackDirection
    )
    {
        Vector2 startPosition = body.position;

        float playerX = player != null
            ? player.position.x
            : startPosition.x;

        Vector2 endPosition = new Vector2(
            playerX +
                attackDirection *
                passPlayerDistance,
            startPosition.y
        );

        bool hasHitPlayer = false;
        float elapsed = 0f;

        while (elapsed < chargeDuration)
        {
            if (rampRedirectRequested)
            {
                yield return RedirectChargeUpward();
                yield break;
            }

            if (pausedForAirCounter)
                yield break;

            float progress = Mathf.Clamp01(
                elapsed / chargeDuration
            );

            Vector2 nextPosition =
                Vector2.Lerp(
                    startPosition,
                    endPosition,
                    progress
                );

            body.MovePosition(nextPosition);

            if (!hasHitPlayer)
                hasHitPlayer = TryDamagePlayer();

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (rampRedirectRequested)
        {
            yield return RedirectChargeUpward();
            yield break;
        }

        if (pausedForAirCounter)
            yield break;

        body.MovePosition(endPosition);

        if (!hasHitPlayer)
            TryDamagePlayer();
    }

    private IEnumerator RedirectChargeUpward()
    {
        isChargingForward = false;
        body.linearVelocity = Vector2.zero;

        yield return MoveBodyTo(
            body.position,
            redirectRampBase,
            redirectApproachDuration
        );

        yield return MoveBodyTo(
            body.position,
            redirectRampTop,
            redirectRampDuration
        );

        ReachedRampTop?.Invoke();

        Vector2 verticalEnd =
            redirectRampTop +
            Vector2.up * redirectVerticalDistance;

        yield return MoveBodyTo(
            body.position,
            verticalEnd,
            redirectVerticalDuration
        );

        float hoverTime = 0f;

        while (hoverTime < redirectHoverDuration)
        {
            body.MovePosition(verticalEnd);
            body.linearVelocity = Vector2.zero;

            hoverTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        body.linearVelocity = Vector2.zero;
        rampRedirectRequested = false;
    }

    private IEnumerator MoveBodyTo(
        Vector2 start,
        Vector2 end,
        float duration
    )
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(
                elapsed / Mathf.Max(duration, 0.01f)
            );

            t = Mathf.SmoothStep(0f, 1f, t);

            body.MovePosition(
                Vector2.Lerp(start, end, t)
            );

            body.linearVelocity = Vector2.zero;
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        body.MovePosition(end);
        body.linearVelocity = Vector2.zero;
    }

    private bool TryDamagePlayer()
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

            if (hitHealth != null &&
                hitHealth == playerHealth)
            {
                hitHealth.TakeDamage(damage);
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position,
            hitRadius
        );
    }
}
