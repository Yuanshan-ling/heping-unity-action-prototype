using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAirCombat : MonoBehaviour
{
    private enum FlightState
    {
        Disabled,
        WaitingForLaunch,
        Rising,
        Gliding,
        ChasingEnemy,
        Attacking,
        CounterAttacking
    }

    private enum ArcEase
    {
        InOut,
        In,
        Out
    }

    [Header("References")]
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private Transform attackHand;
    [SerializeField] private Transform targetEnemy;
    [SerializeField] private Collider2D targetEnemyCollider;
    [SerializeField] private Rigidbody2D targetEnemyBody;
    [SerializeField] private SpecialEnemyChargeAttack targetChargeAttack;
    [SerializeField] private SpecialEnemyPounce targetPounceAttack;
    [SerializeField] private EnemyAI targetEnemyAI;
    [SerializeField] private SpecialEnemyAttackDirector targetEnemyAttackDirector;

    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask groundLayer;

    [Header("Pounce Dodge Launch")]
    [Min(0f)]
    [SerializeField] private float dodgeTriggerDistance = 6f;

    [Min(0f)]
    [SerializeField] private float maximumDodgeVerticalDistance = 4f;

    [Min(0f)]
    [SerializeField] private float minimumIncomingSpeed = 2f;

    [Min(0f)]
    [SerializeField] private float minimumDodgeRiseHeight = 6f;

    [Range(0.5f, 0.95f)]
    [SerializeField] private float cameraTopViewportY = 0.88f;

    [SerializeField] private float riseAcceleration = 100f;
    [SerializeField] private float riseSpeed = 16f;

    [Header("Air Movement")]
    [SerializeField] private float slowDescentSpeed = 2f;
    [SerializeField] private float chaseSpeed = 10f;
    [SerializeField] private float attackStartDistance = 1.4f;

    [Header("Attack")]
    [SerializeField] private float attackFreezeDuration = 0.4f;

    [Header("Air Counter Arc")]
    [Range(0.02f, 0.45f)]
    [SerializeField] private float leftScreenViewportX = 0.12f;

    [Min(0f)]
    [SerializeField] private float moveToLeftArcHeight = 3f;

    [Min(0.01f)]
    [SerializeField] private float moveToLeftDuration = 0.45f;

    [Min(0f)]
    [SerializeField] private float enemyApproachArcHeight = 4f;

    [Min(0f)]
    [SerializeField] private float enemyExitArcHeight = 2f;

    [Min(0.01f)]
    [SerializeField] private float passEnemyDuration = 0.55f;

    [Min(0f)]
    [SerializeField] private float passEnemyDistance = 6f;

    [SerializeField] private float counterEndHeightOffset = 2f;

    [Header("Pounce Air Counter Damage")]

    [Min(1)]
    [SerializeField] private int pounceAirCounterDamage = 3;

    private FlightState state = FlightState.Disabled;
    private float originalGravityScale;
    private float flightTopY;
    private float attackFreezeTime;
    private float currentRiseSpeed;
    private bool isInAttackRange;

    // 成功躲开一次跳击后，直到反击或落地前都保留此资格。
    private bool pounceCounterReady;
    private bool enemyLockedForAirCounter;
    private bool enemyAIWasEnabled;
    private bool enemyDirectorWasEnabled;
    private bool enemyBodyWasSimulated;

    // 保留旧冲锋系统的兼容性；本次跳击闪避不会暂停冲锋。
    private bool chargeCounterActive;

    private bool hasEnemyPositionSample;
    private float lastEnemyX;
    private float recentlyMeasuredEnemySpeedX;
    private float measuredSpeedValidUntil;

    private void Awake()
    {
        if (body == null)
            body = GetComponent<Rigidbody2D>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        FindTargetEnemyReferences();

        if (body != null)
            originalGravityScale = body.gravityScale;

        enabled = false;
    }

    public void UnlockFlight()
    {
        if (body == null || targetCamera == null)
        {
            Debug.LogError(
                "PlayerAirCombat 缺少 Rigidbody2D 或 Camera。"
            );
            return;
        }

        FindTargetEnemyReferences();

        enabled = true;
        state = FlightState.WaitingForLaunch;
        ResetEnemyMotionSample();

        Debug.Log(
            "PlayerAirCombat：特殊敌人跳击闪避已启用。"
        );
    }

    private void Update()
    {
        if (Keyboard.current == null || body == null)
            return;

        SampleTargetEnemyMovement();

        if (enemyLockedForAirCounter)
            KeepLockedEnemyFacingPlayer();

        switch (state)
        {
            case FlightState.WaitingForLaunch:
                WaitForChargeDodgeInput();
                break;

            case FlightState.Rising:
                RiseToCameraTop();
                break;

            case FlightState.Gliding:
                GlideDown();

                if (Keyboard.current.jKey.wasPressedThisFrame)
                {
                    // 跳击闪避成功后的 J：
                    // 不追向敌人，而是直接进入弧线穿越反击。
                    if (pounceCounterReady)
                    {
                        pounceCounterReady = false;

                        LockEnemyForAirCounter();

                        StartCoroutine(
                            PerformAirCounterArc()
                        );
                    }

                    else if (isInAttackRange)
                    {
                        StartAirAttack();
                    }
                    else if (targetEnemy != null)
                    {
                        state = FlightState.ChasingEnemy;
                    }
                }

                break;

            case FlightState.ChasingEnemy:
                ChaseEnemy();
                break;

            case FlightState.Attacking:
                HoldStillDuringAttack();
                break;

            case FlightState.CounterAttacking:
                body.linearVelocity = Vector2.zero;
                break;
        }
    }

    private void FindTargetEnemyReferences()
    {
        if (targetEnemy == null)
            return;

        if (targetEnemyCollider == null)
        {
            targetEnemyCollider =
                targetEnemy.GetComponentInChildren<Collider2D>();
        }

        if (targetEnemyBody == null)
        {
            targetEnemyBody =
                targetEnemy.GetComponent<Rigidbody2D>();
        }

        if (targetChargeAttack == null)
        {
            targetChargeAttack =
                targetEnemy.GetComponent<SpecialEnemyChargeAttack>();
        }

        if (targetPounceAttack == null)
        {
            targetPounceAttack =
                targetEnemy.GetComponent<SpecialEnemyPounce>();
        }

        if (targetEnemyAI == null)
        {
            targetEnemyAI =
                targetEnemy.GetComponent<EnemyAI>();
        }

        if (targetEnemyAttackDirector == null)
        {
            targetEnemyAttackDirector =
                targetEnemy.GetComponent<SpecialEnemyAttackDirector>();
        }
    }

    private void WaitForChargeDodgeInput()
    {
        if (Keyboard.current == null ||
            !Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            return;
        }

        if (!CanDodgeCurrentCharge())
            return;

        // 此时 CanDodgeCurrentCharge 已确认敌人正处于跳击。
        pounceCounterReady = true;

        StartRise();
    }

    private bool CanDodgeCurrentCharge()
    {
        FindTargetEnemyReferences();

        if (targetEnemy == null ||
            targetPounceAttack == null)
        {
            return false;
        }

        return targetPounceAttack.IsPouncing;
    }

    private void SampleTargetEnemyMovement()
    {
        if (targetEnemy == null)
            return;

        float currentEnemyX = targetEnemy.position.x;

        if (!hasEnemyPositionSample)
        {
            lastEnemyX = currentEnemyX;
            hasEnemyPositionSample = true;
            return;
        }

        float deltaTime = Mathf.Max(
            Time.deltaTime,
            0.0001f
        );

        float measuredSpeedX =
            (currentEnemyX - lastEnemyX) /
            deltaTime;

        lastEnemyX = currentEnemyX;

        if (Mathf.Abs(measuredSpeedX) >= 0.1f)
        {
            recentlyMeasuredEnemySpeedX = measuredSpeedX;
            measuredSpeedValidUntil =
                Time.unscaledTime + 0.12f;
        }
    }

    private void ResetEnemyMotionSample()
    {
        hasEnemyPositionSample = false;
        recentlyMeasuredEnemySpeedX = 0f;
        measuredSpeedValidUntil = 0f;

        if (targetEnemy != null)
        {
            lastEnemyX = targetEnemy.position.x;
            hasEnemyPositionSample = true;
        }
    }

    private void StartRise()
    {
        if (playerController != null)
            playerController.enabled = false;

        if (playerAttack != null)
            playerAttack.SetAttackInputEnabled(false);

        body.linearVelocity = Vector2.zero;
        body.gravityScale = 0f;
        isInAttackRange = false;
        chargeCounterActive = false;

        float cameraDistance = Mathf.Abs(
            targetCamera.transform.position.z -
            transform.position.z
        );

        Vector3 cameraTop = targetCamera.ViewportToWorldPoint(
            new Vector3(
                0.5f,
                cameraTopViewportY,
                cameraDistance
            )
        );

        flightTopY = Mathf.Max(
            cameraTop.y,
            transform.position.y + minimumDodgeRiseHeight
        );

        currentRiseSpeed = riseSpeed;
        body.linearVelocity = Vector2.up * currentRiseSpeed;
        state = FlightState.Rising;
    }

    private void RiseToCameraTop()
    {
        currentRiseSpeed = Mathf.MoveTowards(
            currentRiseSpeed,
            riseSpeed,
            riseAcceleration * Time.deltaTime
        );

        body.linearVelocity = Vector2.up * currentRiseSpeed;

        if (transform.position.y >= flightTopY)
        {
            body.position = new Vector2(
                body.position.x,
                flightTopY
            );

            body.linearVelocity = Vector2.zero;
            state = FlightState.Gliding;
        }
    }

    private void GlideDown()
    {
        body.linearVelocity =
            Vector2.down * slowDescentSpeed;
    }

    private void ChaseEnemy()
    {
        if (targetEnemy == null)
        {
            state = FlightState.Gliding;
            return;
        }

        if (!Keyboard.current.jKey.isPressed)
        {
            state = FlightState.Gliding;
            return;
        }

        Vector2 direction = (
            targetEnemy.position - transform.position
        ).normalized;

        body.linearVelocity = direction * chaseSpeed;

        if (DistanceToEnemy() <= attackStartDistance)
        {
            isInAttackRange = true;

            body.linearVelocity =
                Vector2.down * slowDescentSpeed;

            state = FlightState.Gliding;
        }
    }

    private void StartAirAttack()
    {
        body.linearVelocity = Vector2.zero;
        attackFreezeTime = attackFreezeDuration;
        state = FlightState.Attacking;

        if (playerAttack != null)
            playerAttack.TriggerAttack();
    }

    private void HoldStillDuringAttack()
    {
        body.linearVelocity = Vector2.zero;
        attackFreezeTime -= Time.deltaTime;

        if (attackFreezeTime <= 0f)
            state = FlightState.Gliding;
    }

    private System.Collections.IEnumerator PerformAirCounterArc()
    {
        if (targetEnemy == null || targetCamera == null)
        {
            ResumeEnemyAfterCounter();
            state = FlightState.Gliding;
            yield break;
        }

        state = FlightState.CounterAttacking;
        body.linearVelocity = Vector2.zero;

        float cameraDistance = Mathf.Abs(
            targetCamera.transform.position.z -
            transform.position.z
        );

        Vector3 leftScreenPoint =
            targetCamera.ViewportToWorldPoint(
                new Vector3(
                    leftScreenViewportX,
                    0.5f,
                    cameraDistance
                )
            );

        Vector2 leftTarget = new Vector2(
            leftScreenPoint.x,
            body.position.y
        );

        yield return MoveAlongArc(
            body.position,
            leftTarget,
            moveToLeftArcHeight,
            moveToLeftDuration
        );

        if (targetEnemy == null)
        {
            ResumeEnemyAfterCounter();
            state = FlightState.Gliding;
            yield break;
        }

        Vector2 enemyPoint = targetEnemy.position;

        Vector2 exitTarget = new Vector2(
            enemyPoint.x + passEnemyDistance,
            enemyPoint.y + counterEndHeightOffset
        );

        float distanceToEnemy = Vector2.Distance(
            body.position,
            enemyPoint
        );

        float distancePastEnemy = Vector2.Distance(
            enemyPoint,
            exitTarget
        );

        float totalDistance = Mathf.Max(
            distanceToEnemy + distancePastEnemy,
            0.01f
        );

        // 两段时间按实际距离分配，能让敌人位置的速度更连续。
        float approachDuration = Mathf.Max(
            passEnemyDuration *
            (distanceToEnemy / totalDistance),
            0.01f
        );

        float exitDuration = Mathf.Max(
            passEnemyDuration - approachDuration,
            0.01f
        );

        // 从屏幕侧边接近敌人：持续加速。
        yield return MoveAlongArc(
            body.position,
            enemyPoint,
            enemyApproachArcHeight,
            approachDuration,
            ArcEase.In
        );

        // 主角抵达敌人位置、开始穿过敌人的瞬间造成一次伤害。
        DealPounceAirCounterDamage();

        // 穿过敌人后：持续减速。
        yield return MoveAlongArc(
            body.position,
            exitTarget,
            enemyExitArcHeight,
            exitDuration,
            ArcEase.Out
        );





        ResumeEnemyAfterCounter();
        state = FlightState.Gliding;
    }

    private void DealPounceAirCounterDamage()
    {
        if (targetEnemy == null)
            return;

        EnemyHealth enemyHealth =
            targetEnemy.GetComponent<EnemyHealth>();

        if (enemyHealth == null)
        {
            Debug.LogWarning(
                "PlayerAirCombat：目标没有 EnemyHealth，" +
                "空中反击未造成伤害。"
            );
            return;
        }

        enemyHealth.TakeDamage(
            pounceAirCounterDamage
        );

        Debug.Log(
            "PlayerAirCombat：空中穿越反击命中，造成 " +
            pounceAirCounterDamage +
            " 点伤害。"
        );
    }

    private System.Collections.IEnumerator MoveAlongArc(
        Vector2 start,
        Vector2 end,
        float arcHeight,
        float duration,
        ArcEase ease = ArcEase.InOut
    )
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(
                elapsed / Mathf.Max(duration, 0.01f)
            );

            float progress = EvaluateArcProgress(
                t,
                ease
            );

            Vector2 position = Vector2.Lerp(
                start,
                end,
                progress
            );

            position.y +=
                Mathf.Sin(progress * Mathf.PI) * arcHeight;

            body.MovePosition(position);
            body.linearVelocity = Vector2.zero;

            elapsed += Time.deltaTime;
            yield return null;
        }

        body.MovePosition(end);
        body.linearVelocity = Vector2.zero;
    }

    private float EvaluateArcProgress(
    float t,
    ArcEase ease
)
    {
        switch (ease)
        {
            case ArcEase.In:
                return t * t;

            case ArcEase.Out:
                return 1f - (1f - t) * (1f - t);

            default:
                return t * t * (3f - 2f * t);
        }
    }

    private void LockEnemyForAirCounter()
    {
        if (enemyLockedForAirCounter)
            return;

        FindTargetEnemyReferences();

        enemyLockedForAirCounter = true;

        if (targetEnemyAI != null)
        {
            enemyAIWasEnabled = targetEnemyAI.enabled;
            targetEnemyAI.enabled = false;
        }

        if (targetEnemyAttackDirector != null)
        {
            enemyDirectorWasEnabled =
                targetEnemyAttackDirector.enabled;

            targetEnemyAttackDirector.enabled = false;
        }

        if (targetEnemyBody != null)
        {
            enemyBodyWasSimulated =
                targetEnemyBody.simulated;

            targetEnemyBody.linearVelocity = Vector2.zero;
            targetEnemyBody.angularVelocity = 0f;
            targetEnemyBody.simulated = false;
        }
    }

    private void KeepLockedEnemyFacingPlayer()
    {
        if (targetEnemy == null)
            return;

        float horizontalDifference =
            transform.position.x - targetEnemy.position.x;

        if (Mathf.Abs(horizontalDifference) < 0.01f)
            return;

        Vector3 scale = targetEnemy.localScale;

        float facingDirection =
            Mathf.Sign(horizontalDifference);

        scale.x = Mathf.Abs(scale.x) * facingDirection;

        targetEnemy.localScale = scale;
    }

    private void UnlockEnemyAfterAirCounter()
    {
        if (!enemyLockedForAirCounter)
            return;

        if (targetEnemyBody != null)
        {
            targetEnemyBody.simulated = enemyBodyWasSimulated;
            targetEnemyBody.linearVelocity = Vector2.zero;
            targetEnemyBody.angularVelocity = 0f;
        }

        // 空中反击结束后，强制恢复特殊敌人的普通追击。
        if (targetEnemyAI != null)
            targetEnemyAI.enabled = true;

        // 不能只重新勾选组件；必须调用此方法重启攻击调度循环。
        if (targetEnemyAttackDirector != null &&
            enemyDirectorWasEnabled)
        {
            targetEnemyAttackDirector.EnableDirector();
        }

        enemyLockedForAirCounter = false;
    }

    private void ResumeEnemyAfterCounter()
    {
        if (chargeCounterActive &&
            targetChargeAttack != null)
        {
            targetChargeAttack.ResumeAfterAirCounter();
        }

        chargeCounterActive = false;
    }

    private float DistanceToEnemy()
    {
        if (targetEnemy == null)
            return float.MaxValue;

        Vector2 handPosition = attackHand != null
            ? attackHand.position
            : transform.position;

        Vector2 enemyPoint = targetEnemyCollider != null
            ? targetEnemyCollider.ClosestPoint(handPosition)
            : (Vector2)targetEnemy.position;

        return Vector2.Distance(
            handPosition,
            enemyPoint
        );
    }

    private void OnCollisionEnter2D(
        Collision2D collision
    )
    {
        if (state == FlightState.Disabled ||
            state == FlightState.WaitingForLaunch)
        {
            return;
        }

        int layerMask =
            1 << collision.gameObject.layer;

        if ((groundLayer.value & layerMask) != 0)
            EndFlight();
    }


        private void EndFlight()
    {
        UnlockEnemyAfterAirCounter();

        ResumeEnemyAfterCounter();

        body.linearVelocity = Vector2.zero;
        body.gravityScale = originalGravityScale;

        if (playerController != null)
            playerController.enabled = true;

        if (playerAttack != null)
            playerAttack.SetAttackInputEnabled(true);

        currentRiseSpeed = 0f;
        isInAttackRange = false;
        pounceCounterReady = false;

        state = FlightState.WaitingForLaunch;
        ResetEnemyMotionSample();
    }
}