using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class StarCorePlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer playerRenderer;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private StarCoreExecutionerBoss targetBoss;
    [SerializeField] private StarCoreCombatFeedback feedback;
    [SerializeField] private bool useAnimatorParameters;

    [Header("Debug")]
    [SerializeField] private bool showCombatLogs = true;
    [SerializeField] private bool fourHitComboOnlyMode = true;

    [Header("Input")]
    [SerializeField] private bool acceptsInput = true;
    [Min(0.05f)]
    [SerializeField] private float chargeDecisionTime = 0.18f;

    [Header("Light Combo")]
    [SerializeField] private int[] comboDamage = { 1, 1, 2, 3 };
    [SerializeField] private float[] comboWindup =
        { 0.06f, 0.07f, 0.09f, 0.14f };
    [SerializeField] private float[] comboRecovery =
        { 0.12f, 0.13f, 0.16f, 0.28f };
    [SerializeField] private float comboResetTime = 0.75f;
    [SerializeField] private float attackRadius = 2.2f;
    [SerializeField] private Vector2 attackOffset =
        new Vector2(1.35f, 0.2f);
    [SerializeField] private LayerMask bossLayers = ~0;

    [Header("Charged Slash")]
    [Min(0f)]
    [SerializeField] private float fullChargeTime = 0.75f;
    [Min(1)]
    [SerializeField] private int chargedDamage = 6;
    [Min(0f)]
    [SerializeField] private float chargedPostureDamage = 45f;
    [Min(0.01f)]
    [SerializeField] private float chargedWindup = 0.12f;
    [Min(0.01f)]
    [SerializeField] private float chargedRecovery = 0.4f;

    [Header("Directional Dash")]
    [SerializeField] private float dashDistance = 6f;
    [Min(0.01f)]
    [SerializeField] private float dashDuration = 0.22f;
    [Min(0f)]
    [SerializeField] private float dashCooldown = 0.35f;
    [Range(0.01f, 1f)]
    [SerializeField] private float perfectDodgeWindowRatio = 0.45f;

    [Header("Guard And Parry")]
    [Min(0.01f)]
    [SerializeField] private float parryWindow = 0.16f;
    [Min(1f)]
    [SerializeField] private float maximumPosture = 100f;
    [Min(0f)]
    [SerializeField] private float postureRecoveryPerSecond = 22f;
    [Min(0.01f)]
    [SerializeField] private float guardBreakDuration = 0.8f;

    [Header("Star Core Burst")]
    [Min(1)]
    [SerializeField] private int maximumStarEnergy = 3;
    [Min(1)]
    [SerializeField] private int burstDamagePerPass = 3;
    [Min(0f)]
    [SerializeField] private float burstPassDistance = 3.5f;
    [Min(0.01f)]
    [SerializeField] private float burstPassDuration = 0.12f;

    [Header("Air Chase")]
    [SerializeField] private float airChaseRange = 10f;
    [SerializeField] private float airChasePassDistance = 2.5f;
    [SerializeField] private float airChaseDuration = 0.18f;
    [SerializeField] private int airChaseDamage = 3;

    private Rigidbody2D body;
    private float originalGravityScale;
    private bool controllerWasEnabled;
    private bool controllerLocked;

    private bool isBusy;
    private bool isDashing;
    private bool isPerfectDodgeWindow;
    private bool isInvulnerable;
    private bool isGuarding;
    private bool isGuardBroken;
    private bool evaluatingJ;
    private bool chargingJ;
    private bool queuedLightAttack;

    private float jPressedTime;
    private float parryWindowEndTime;
    private float nextDashTime;
    private float lastComboTime;
    private float facingDirection = 1f;
    private float posture;
    private int comboIndex;
    private int starEnergy;

    public bool IsDashing => isDashing;
    public bool IsPerfectDodgeWindow => isPerfectDodgeWindow;
    public bool IsGuarding => isGuarding;
    public float Posture => posture;
    public int StarEnergy => starEnergy;
    public int MaximumStarEnergy => maximumStarEnergy;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        originalGravityScale = body.gravityScale;

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerRenderer == null)
            playerRenderer = GetComponent<SpriteRenderer>();

        if (attackOrigin == null)
            attackOrigin = transform;

        FindBoss();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        RestoreController();

        if (body != null)
            body.gravityScale = originalGravityScale;

        isBusy = false;
        isDashing = false;
        isInvulnerable = false;
        isPerfectDodgeWindow = false;
        isGuarding = false;
        evaluatingJ = false;
        chargingJ = false;
    }

    private void Update()
    {
        UpdateFacingDirection();
        RecoverPosture();

        if (!acceptsInput || Keyboard.current == null)
            return;

        if (fourHitComboOnlyMode)
        {
            HandleGuardInput();
            HandleDashInput();
            HandleBurstInput();
            HandleFourHitComboOnlyInput();
            return;
        }

        HandleGuardInput();
        HandleDashInput();
        HandleBurstInput();
        HandleAttackInput();
    }

    public void SetInputEnabled(bool enabled)
    {
        acceptsInput = enabled;
    }

    private void UpdateFacingDirection()
    {
        if (Keyboard.current == null)
            return;

        float horizontal = 0f;

        if (Keyboard.current.aKey.isPressed)
            horizontal -= 1f;

        if (Keyboard.current.dKey.isPressed)
            horizontal += 1f;

        if (!Mathf.Approximately(horizontal, 0f))
            facingDirection = Mathf.Sign(horizontal);
    }

    private void RecoverPosture()
    {
        if (isGuarding || isGuardBroken || posture <= 0f)
            return;

        posture = Mathf.Max(
            0f,
            posture - postureRecoveryPerSecond * Time.deltaTime
        );
    }

    private void HandleGuardInput()
    {
        if (Keyboard.current.iKey.wasPressedThisFrame &&
            !isBusy &&
            !isDashing &&
            !isGuardBroken)
        {
            isGuarding = true;
            parryWindowEndTime = Time.time + parryWindow;

            LogAction(
                $"防御开始：弹反窗口 {parryWindow:0.00} 秒"
            );

            if (useAnimatorParameters && animator != null)
                animator.SetBool("Guard", true);
        }

        if (Keyboard.current.iKey.wasReleasedThisFrame)
        {
            isGuarding = false;
            LogAction("防御结束");

            if (useAnimatorParameters && animator != null)
                animator.SetBool("Guard", false);
        }
    }

    private void HandleDashInput()
    {
        if (!Keyboard.current.spaceKey.wasPressedThisFrame ||
            Time.time < nextDashTime ||
            isBusy ||
            isGuardBroken)
        {
            return;
        }

        Vector2 direction = ReadDashDirection();
        StartCoroutine(DashRoutine(direction));
    }

    private Vector2 ReadDashDirection()
    {
        Vector2 direction = Vector2.zero;

        if (Keyboard.current.aKey.isPressed)
            direction.x -= 1f;
        if (Keyboard.current.dKey.isPressed)
            direction.x += 1f;
        if (Keyboard.current.wKey.isPressed)
            direction.y += 1f;
        if (Keyboard.current.sKey.isPressed)
            direction.y -= 1f;

        if (direction.sqrMagnitude < 0.01f)
            direction = Vector2.right * facingDirection;

        return direction.normalized;
    }

    private void HandleBurstInput()
    {
        if (!Keyboard.current.kKey.wasPressedThisFrame ||
            isBusy ||
            isDashing ||
            isGuardBroken ||
            starEnergy < maximumStarEnergy)
        {
            return;
        }

        FindBoss();

        if (targetBoss != null)
            StartCoroutine(BurstRoutine());
    }
    private void HandleFourHitComboOnlyInput()
    {
        if (Keyboard.current == null ||
            !Keyboard.current.jKey.wasPressedThisFrame)
        {
            return;
        }

        FindBoss();

        if (targetBoss != null &&
            targetBoss.IsAirborneVulnerable &&
            Vector2.Distance(
                transform.position,
                targetBoss.transform.position
            ) <= airChaseRange)
        {
            if (isBusy)
            {
                queuedLightAttack = true;
                return;
            }

            StartCoroutine(AirChaseRoutine());
            return;
        }

        if (isBusy)
        {
            queuedLightAttack = true;
            return;
        }

        StartCoroutine(LightAttackRoutine());
    }

    private void HandleAttackInput()
    {
        if (Keyboard.current.jKey.wasPressedThisFrame &&
            !isDashing &&
            !isGuardBroken)
        {
            evaluatingJ = true;
            chargingJ = false;
            jPressedTime = Time.time;
        }

        if (!evaluatingJ)
            return;

        if (Keyboard.current.jKey.isPressed &&
            Time.time - jPressedTime >= chargeDecisionTime)
        {
            chargingJ = true;
        }

        if (!Keyboard.current.jKey.wasReleasedThisFrame)
            return;

        float heldTime = Time.time - jPressedTime;
        evaluatingJ = false;

        if (chargingJ)
        {
            if (!isBusy && !isGuarding)
                StartCoroutine(ChargedSlashRoutine(heldTime));
        }
        else
        {
            RequestLightAttack();
        }

        chargingJ = false;
    }

    private void RequestLightAttack()
    {
        FindBoss();

        if (targetBoss != null &&
            targetBoss.IsAirborneVulnerable &&
            Vector2.Distance(transform.position, targetBoss.transform.position)
                <= airChaseRange &&
            !isBusy)
        {
            StartCoroutine(AirChaseRoutine());
            return;
        }

        if (isBusy)
        {
            queuedLightAttack = true;
            return;
        }

        if (isGuarding || isDashing || isGuardBroken)
            return;

        StartCoroutine(LightAttackRoutine());
    }

    private IEnumerator LightAttackRoutine()
    {
        isBusy = true;
        LockController();

        if (Time.time - lastComboTime > comboResetTime)
            comboIndex = 0;

        int safeIndex = Mathf.Clamp(
            comboIndex,
            0,
            comboDamage.Length - 1
        );

        bool launches = safeIndex == comboDamage.Length - 1;
        string attackName = $"四连击第 {safeIndex + 1} 段";

        LogAction(
            $"{attackName}开始：伤害 " +
            $"{GetArrayValue(comboDamage, safeIndex, 1)}" +
            (launches ? "，这一段会击飞 Boss" : string.Empty)
        );

        if (useAnimatorParameters && animator != null)
            animator.SetTrigger("Attack" + (safeIndex + 1));

        yield return new WaitForSeconds(GetArrayValue(comboWindup, safeIndex, 0.08f));

        bool hitBoss = DealDamageToBoss(
            GetArrayValue(comboDamage, safeIndex, 1),
            launches ? 22f : 10f,
            launches,
            new Color(0.75f, 0.9f, 1f)
        );

        LogAction(
            hitBoss
                ? $"{attackName}命中 Boss"
                : $"{attackName}未命中"
        );

        yield return new WaitForSeconds(
            GetArrayValue(comboRecovery, safeIndex, 0.15f)
        );

        comboIndex = (safeIndex + 1) % comboDamage.Length;
        lastComboTime = Time.time;
        isBusy = false;
        RestoreController();

        if (queuedLightAttack)
        {
            queuedLightAttack = false;
            RequestLightAttack();
        }
    }

    private IEnumerator ChargedSlashRoutine(float heldTime)
    {
        isBusy = true;
        LockController();

        float chargeRatio = Mathf.Clamp01(
            heldTime / Mathf.Max(fullChargeTime, 0.01f)
        );

        bool fullCharge = chargeRatio >= 0.95f;
        string attackName = fullCharge ? "完全蓄力斩" : "蓄力斩";

        LogAction(
            $"{attackName}开始：蓄力比例 {chargeRatio:P0}"
        );

        if (useAnimatorParameters && animator != null)
            animator.SetTrigger("ChargedAttack");

        yield return new WaitForSeconds(chargedWindup);

        int damage = Mathf.Max(
            1,
            Mathf.RoundToInt(Mathf.Lerp(2f, chargedDamage, chargeRatio))
        );

        float postureDamage = Mathf.Lerp(
            18f,
            chargedPostureDamage,
            chargeRatio
        );

        bool hitBoss = DealDamageToBoss(
            damage,
            postureDamage,
            fullCharge,
            new Color(0.95f, 0.95f, 1f)
        );

        LogAction(
            hitBoss
                ? $"{attackName}命中：伤害 {damage}，架势伤害 {postureDamage:0}"
                : $"{attackName}未命中"
        );

        yield return new WaitForSeconds(chargedRecovery);

        comboIndex = 0;
        isBusy = false;
        RestoreController();
    }

    private IEnumerator DashRoutine(Vector2 direction)
    {
        isDashing = true;
        isInvulnerable = true;
        isPerfectDodgeWindow = true;
        isGuarding = false;
        nextDashTime = Time.time + dashCooldown;
        LockController();

        LogAction(
            $"方向闪避开始：方向 ({direction.x:0.00}, {direction.y:0.00})，" +
            $"完美闪避窗口约 {dashDuration * perfectDodgeWindowRatio:0.00} 秒"
        );

        if (useAnimatorParameters && animator != null)
            animator.SetTrigger("Dash");

        float savedGravity = body.gravityScale;
        body.gravityScale = 0f;
        body.linearVelocity = Vector2.zero;

        Vector2 start = body.position;
        Vector2 end = start + direction * dashDistance;
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            float progress = Mathf.Clamp01(elapsed / dashDuration);
            body.MovePosition(Vector2.Lerp(start, end, progress));

            if (progress > perfectDodgeWindowRatio)
                isPerfectDodgeWindow = false;

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        body.MovePosition(end);
        body.linearVelocity = Vector2.zero;
        body.gravityScale = savedGravity;

        isPerfectDodgeWindow = false;
        isInvulnerable = false;
        isDashing = false;
        RestoreController();
        LogAction("方向闪避结束");
    }

    private IEnumerator AirChaseRoutine()
    {
        isBusy = true;
        isInvulnerable = true;
        LockController();

        LogAction("空中追击开始：正在冲向被击飞的 Boss");

        Vector2 direction = (
            targetBoss.transform.position - transform.position
        ).normalized;

        Vector2 start = body.position;
        Vector2 end = (Vector2)targetBoss.transform.position +
            direction * airChasePassDistance;

        yield return MoveBody(start, end, airChaseDuration);

        bool accepted = targetBoss.ReceivePlayerHit(
            airChaseDamage,
            25f,
            false,
            targetBoss.transform.position
        );

        LogAction(
            accepted
                ? $"空中追击命中：伤害 {airChaseDamage}"
                : "空中追击被 Boss 拒绝或反制"
        );

        feedback?.PlayImpact(
            targetBoss.transform.position,
            new Color(0.7f, 0.9f, 1f),
            2.2f,
            0.07f,
            0.12f
        );

        yield return new WaitForSeconds(0.12f);

        isInvulnerable = false;
        isBusy = false;
        RestoreController();
        LogAction("空中追击结束");
    }

    private IEnumerator BurstRoutine()
    {
        isBusy = true;
        isInvulnerable = true;
        starEnergy = 0;
        LockController();

        LogAction("星核爆发开始：消耗全部星核能量，连续穿斩 3 次");

        if (useAnimatorParameters && animator != null)
            animator.SetTrigger("StarBurst");

        for (int pass = 0; pass < 3; pass++)
        {
            if (targetBoss == null)
                break;

            float side = pass % 2 == 0 ? 1f : -1f;
            Vector2 start = body.position;
            Vector2 end = (Vector2)targetBoss.transform.position +
                Vector2.right * side * burstPassDistance;

            yield return MoveBody(start, end, burstPassDuration);

            bool accepted = targetBoss.ReceivePlayerHit(
                burstDamagePerPass,
                20f,
                pass == 2,
                targetBoss.transform.position
            );

            LogAction(
                $"星核爆发第 {pass + 1}/3 次穿斩" +
                (accepted
                    ? $"命中：伤害 {burstDamagePerPass}"
                    : "未生效")
            );

            feedback?.PlayImpact(
                targetBoss.transform.position,
                new Color(0.65f, 0.85f, 1f),
                2.4f,
                0.06f,
                0.1f
            );
        }

        isInvulnerable = false;
        isBusy = false;
        RestoreController();
        LogAction("星核爆发结束");
    }

    private IEnumerator MoveBody(
        Vector2 start,
        Vector2 end,
        float duration
    )
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / duration)
            );

            body.MovePosition(Vector2.Lerp(start, end, progress));
            body.linearVelocity = Vector2.zero;
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        body.MovePosition(end);
        body.linearVelocity = Vector2.zero;
    }

    private bool DealDamageToBoss(
        int damage,
        float postureDamage,
        bool launches,
        Color impactColor
    )
    {
        Vector2 center = (Vector2)attackOrigin.position +
            new Vector2(
                attackOffset.x * facingDirection,
                attackOffset.y
            );

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            center,
            attackRadius,
            bossLayers
        );

        HashSet<StarCoreExecutionerBoss> damaged = new();
        bool hitBoss = false;

        foreach (Collider2D hit in hits)
        {
            StarCoreExecutionerBoss boss =
                hit.GetComponentInParent<StarCoreExecutionerBoss>();

            if (boss == null || !damaged.Add(boss))
                continue;

            bool accepted = boss.ReceivePlayerHit(
                damage,
                postureDamage,
                launches,
                hit.ClosestPoint(center)
            );

            if (accepted)
            {
                hitBoss = true;
                feedback?.PlayImpact(
                    hit.ClosestPoint(center),
                    impactColor,
                    launches ? 2.5f : 1.4f,
                    launches ? 0.09f : 0.04f,
                    launches ? 0.14f : 0.05f
                );
            }
        }

        return hitBoss;
    }

    public StarCoreDefenseResult ReceiveBossAttack(
        int damage,
        float postureDamage,
        bool parryable,
        Vector2 attackSource
    )
    {
        if (!gameObject.activeInHierarchy || !isActiveAndEnabled)
            return StarCoreDefenseResult.Invulnerable;

        if (isPerfectDodgeWindow)
        {
            AddStarEnergy(1);
            LogAction("完美闪避成功：Boss 招式未命中，获得 1 点星核能量");
            feedback?.PlayImpact(
                transform.position,
                Color.cyan,
                1.8f,
                0.05f,
                0.04f
            );
            return StarCoreDefenseResult.PerfectDodged;
        }

        if (isInvulnerable)
        {
            LogAction("无敌状态生效：Boss 攻击未造成伤害");
            return StarCoreDefenseResult.Invulnerable;
        }

        if (isGuarding &&
            parryable &&
            Time.time <= parryWindowEndTime)
        {
            AddStarEnergy(1);
            posture = Mathf.Max(0f, posture - 18f);
            LogAction("弹反成功：获得 1 点星核能量，并削减 Boss 架势");
            feedback?.PlayImpact(
                transform.position,
                Color.white,
                2f,
                0.07f,
                0.08f
            );
            return StarCoreDefenseResult.Parried;
        }

        if (isGuarding && !isGuardBroken)
        {
            posture += postureDamage;

            if (posture >= maximumPosture)
            {
                LogAction(
                    $"防御崩解：架势 {posture:0}/{maximumPosture:0}"
                );
                StartCoroutine(GuardBreakRoutine());
                ApplyDamage(Mathf.Max(1, damage / 2));
                return StarCoreDefenseResult.GuardBroken;
            }

            feedback?.PlayImpact(
                transform.position,
                new Color(0.4f, 0.7f, 1f),
                1.2f,
                0.025f,
                0.03f
            );
            if (showCombatLogs)
            {
                Debug.Log(
                    "<color=#00FF00><b>格挡成功</b></color>",
                    this
                );
            }

            return StarCoreDefenseResult.Blocked;
        }

        LogAction($"被 Boss 直接命中：即将受到 {damage} 点伤害");
        ApplyDamage(damage);
        StartCoroutine(KnockbackRoutine(attackSource));
        return StarCoreDefenseResult.Hit;
    }

    private void ApplyDamage(int damage)
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            LogAction(
                $"主角当前生命：{playerHealth.CurrentHealth}/" +
                $"{playerHealth.MaxHealth}"
            );
        }
    }

    private IEnumerator GuardBreakRoutine()
    {
        isGuardBroken = true;
        isGuarding = false;
        posture = maximumPosture;
        LockController();

        LogAction($"进入破防硬直：持续 {guardBreakDuration:0.00} 秒");

        if (useAnimatorParameters && animator != null)
            animator.SetTrigger("GuardBreak");

        yield return new WaitForSeconds(guardBreakDuration);

        posture = maximumPosture * 0.35f;
        isGuardBroken = false;
        RestoreController();
        LogAction("破防硬直结束，可以重新行动");
    }

    private IEnumerator KnockbackRoutine(Vector2 source)
    {
        float direction = Mathf.Sign(transform.position.x - source.x);

        if (Mathf.Approximately(direction, 0f))
            direction = 1f;

        body.linearVelocity = new Vector2(direction * 7f, 4f);
        yield return new WaitForSeconds(0.16f);
    }

    private void AddStarEnergy(int amount)
    {
        int before = starEnergy;
        starEnergy = Mathf.Clamp(
            starEnergy + amount,
            0,
            maximumStarEnergy
        );

        if (starEnergy != before)
        {
            LogAction(
                $"星核能量变化：{before} -> {starEnergy}/" +
                $"{maximumStarEnergy}" +
                (starEnergy >= maximumStarEnergy
                    ? "（已满，可以按 K）"
                    : string.Empty)
            );
        }
    }

    private void LogAction(string message)
    {
        if (showCombatLogs)
            Debug.Log($"[星核战斗][主角] {message}", this);
    }

    private void FindBoss()
    {
        if (targetBoss == null)
        {
            targetBoss =
                FindFirstObjectByType<StarCoreExecutionerBoss>();
        }
    }

    private void LockController()
    {
        if (playerController == null || controllerLocked)
            return;

        controllerWasEnabled = playerController.enabled;
        playerController.enabled = false;
        controllerLocked = true;
    }

    private void RestoreController()
    {
        if (!controllerLocked)
            return;

        if (playerController != null)
            playerController.enabled = controllerWasEnabled;

        controllerLocked = false;
    }

    private static int GetArrayValue(
        int[] values,
        int index,
        int fallback
    )
    {
        if (values == null || values.Length == 0)
            return fallback;

        return values[Mathf.Clamp(index, 0, values.Length - 1)];
    }

    private static float GetArrayValue(
        float[] values,
        int index,
        float fallback
    )
    {
        if (values == null || values.Length == 0)
            return fallback;

        return values[Mathf.Clamp(index, 0, values.Length - 1)];
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = attackOrigin != null
            ? attackOrigin
            : transform;

        Vector2 center = (Vector2)origin.position +
            new Vector2(
                attackOffset.x * facingDirection,
                attackOffset.y
            );

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center, attackRadius);
    }
}
