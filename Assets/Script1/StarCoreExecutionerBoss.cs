using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(Rigidbody2D))]
public class StarCoreExecutionerBoss : MonoBehaviour
{
    private enum AttackKind
    {
        RhythmCombo,
        CrossDash,
        ThrownWeapon,
        AirSlam,
        CounterStance,
        Grab
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private StarCorePlayerCombat playerCombat;
    [SerializeField] private SpriteRenderer bossRenderer;
    [SerializeField] private Transform weaponRoot;
    [SerializeField] private Sprite squareSprite;
    [SerializeField] private StarCoreCombatFeedback feedback;

    [Header("Weapon Visual")]
    [SerializeField] private Vector2 weaponRestOffset =
        new Vector2(0.62f, 0.04f);
    [SerializeField] private Color weaponMetalColor =
        new Color(0.58f, 0.65f, 0.72f, 1f);
    [SerializeField] private Color weaponEdgeColor =
        new Color(0.9f, 0.96f, 1f, 1f);
    [SerializeField] private Color weaponCoreColor =
        new Color(1f, 0.2f, 0.08f, 1f);
    [SerializeField] private int weaponSortingOrder = 35;

    [Header("Attack Effects")]
    [SerializeField] private Color quickSlashColor =
        new Color(0.55f, 0.9f, 1f, 0.9f);
    [SerializeField] private Color heavySlashColor =
        new Color(1f, 0.65f, 0.12f, 0.95f);
    [SerializeField] private Color dangerEffectColor =
        new Color(1f, 0.08f, 0.08f, 0.72f);
    [SerializeField] private float effectLifetime = 0.18f;

    [Header("Debug")]
    [SerializeField] private bool showCombatLogs = true;
    [SerializeField] private bool targetDummyMode;
    [SerializeField] private bool rhythmComboOnlyMode = true;
    [SerializeField] private bool targetDummyRhythmDemo;

    [Header("Skill Showcase")]
    [SerializeField] private bool skillShowcaseMode;
    [SerializeField] private float showcaseAttackInterval = 2f;
    [Min(0f)]
    [SerializeField] private float showcaseRetreatDistance = 8f;

    [Min(0.01f)]
    [SerializeField] private float showcaseRetreatDuration = 0.5f;

    [Min(0.01f)]
    [SerializeField] private float showcaseReturnDuration = 0.5f;

    private int showcaseAttackIndex;

    private readonly AttackKind[] showcaseAttackOrder =
    {
    AttackKind.RhythmCombo,
    AttackKind.CrossDash,
    AttackKind.ThrownWeapon,
    AttackKind.AirSlam,
    AttackKind.CounterStance,
    AttackKind.Grab
};

    [Header("Health And Posture")]
    [Min(1)]
    [SerializeField] private int maximumHealth = 40;
    [Min(1f)]
    [SerializeField] private float maximumPosture = 100f;
    [Min(0f)]
    [SerializeField] private float postureRecoveryPerSecond = 10f;
    [Min(0.1f)]
    [SerializeField] private float staggerDuration = 1.1f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float preferredDistance = 5f;
    [SerializeField] private float maximumPursuitDistance = 22f;

    [Header("Attack Scheduling")]
    [SerializeField] private float firstAttackDelay = 1.2f;
    [SerializeField] private float attackInterval = 0.7f;
    [SerializeField] private float phaseTwoHealthRatio = 0.7f;
    [SerializeField] private float phaseThreeHealthRatio = 0.35f;

    [Header("Rhythm Combo")]
    [SerializeField] private float comboHitRange = 4f;
    [SerializeField] private int comboDamage = 1;
    [SerializeField] private float comboPostureDamage = 22f;
    [SerializeField] private float fastStrikeTelegraph = 0.18f;
    [SerializeField] private float delayedStrikeTelegraph = 0.52f;

    [Header("Cross Dash")]
    [SerializeField] private float crossDashPassDistance = 5f;
    [SerializeField] private float crossDashHeight = 4f;
    [SerializeField] private float crossDashDuration = 0.25f;
    [SerializeField] private int crossDashDamage = 2;

    [Header("Thrown Weapon")]
    [SerializeField] private float throwDistance = 12f;
    [SerializeField] private float throwOutDuration = 0.45f;
    [SerializeField] private float throwReturnDuration = 0.38f;
    [SerializeField] private int thrownWeaponDamage = 2;
    [SerializeField] private float thrownWeaponHitRadius = 1.1f;

    [Header("Air Slam")]
    [SerializeField] private float airSlamHeight = 9f;
    [SerializeField] private float airSlamRiseDuration = 0.45f;
    [SerializeField] private float airSlamFallDuration = 0.28f;
    [SerializeField] private float airSlamRadius = 6f;
    [SerializeField] private int airSlamDamage = 3;

    [Header("Counter And Grab")]
    [SerializeField] private float counterStanceDuration = 0.75f;
    [SerializeField] private int counterDamage = 3;
    [SerializeField] private float grabRange = 3f;
    [SerializeField] private float grabTelegraph = 0.5f;
    [SerializeField] private int grabDamage = 3;

    [Header("Launch")]
    [SerializeField] private float launchHeight = 5f;
    [SerializeField] private float launchRiseDuration = 0.28f;
    [SerializeField] private float launchFallDuration = 0.42f;

    private Rigidbody2D body;
    private int currentHealth;
    private float posture;
    private float nextAttackTime;
    private float facingDirection = -1f;
    private bool isAttacking;
    private bool isStaggered;
    private bool isDead;
    private bool counterActive;
    private bool actionInterrupted;
    private bool isAirborneVulnerable;
    private StarCoreDefenseResult lastDashDefenseResult;
    private AttackKind lastAttack;
    private StarCoreBossPhase currentPhase;
    private Color originalColor;
    private Vector3 weaponOriginalPosition;
    private Quaternion weaponOriginalRotation;
    private Vector3 weaponOriginalScale;

    public int CurrentHealth => currentHealth;
    public int MaximumHealth => maximumHealth;
    public float Posture => posture;
    public bool IsAirborneVulnerable => isAirborneVulnerable;
    public StarCoreBossPhase CurrentPhase => currentPhase;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        currentHealth = maximumHealth;

        if (targetDummyMode)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.linearVelocity = Vector2.zero;
        }

        if (bossRenderer == null)
            bossRenderer = GetComponent<SpriteRenderer>();

        if (bossRenderer != null)
            originalColor = bossRenderer.color;

        EnsureWeaponVisual();

        if (weaponRoot != null)
        {
            weaponOriginalPosition = weaponRoot.localPosition;
            weaponOriginalRotation = weaponRoot.localRotation;
            weaponOriginalScale = weaponRoot.localScale;
        }

        FindPlayer();
        FacePlayer();
        currentPhase = StarCoreBossPhase.WeaponDuel;
        nextAttackTime = Time.time + firstAttackDelay;

        LogAction(
            $"战斗初始化：生命 {currentHealth}/{maximumHealth}，" +
            $"阶段 {GetPhaseName(currentPhase)}"
        );
    }

    private void Update()
    {
        if (isDead)
            return;
        if (targetDummyMode)
        {
            body.linearVelocity = Vector2.zero;

            HandleTargetDummyRhythmDemo();

            return;
        }

        FindPlayer();
        UpdatePhase();

        if (!isAttacking && !isStaggered)
        {
            posture = Mathf.Max(
                0f,
                posture - postureRecoveryPerSecond * Time.deltaTime
            );
        }

        if (player == null ||
            isAttacking ||
            isStaggered ||
            Time.time < nextAttackTime)
        {
            return;
        }

        StartCoroutine(PerformChosenAttack());
    }

    private void HandleTargetDummyRhythmDemo()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            targetDummyRhythmDemo =
                !targetDummyRhythmDemo;

            if (targetDummyRhythmDemo)
            {
                nextAttackTime = Time.time;

                LogAction(
                    "靶子模式：已开启原地循环三连斩"
                );
            }
            else
            {
                LogAction(
                    "靶子模式：已停止原地循环三连斩"
                );
            }
        }

        if (!targetDummyRhythmDemo ||
            isAttacking ||
            isStaggered ||
            Time.time < nextAttackTime)
        {
            return;
        }

        FindPlayer();

        if (player == null)
            return;

        StartCoroutine(TargetDummyRhythmComboRoutine());
    }

    private IEnumerator TargetDummyRhythmComboRoutine()
    {
        isAttacking = true;
        actionInterrupted = false;

        body.linearVelocity = Vector2.zero;
        FacePlayer();

        LogAction(
            "靶子模式：开始原地节奏三连斩"
        );

        yield return RhythmComboRoutine();

        ResetWeaponPose();

        isAttacking = false;

        nextAttackTime = Time.time + attackInterval;

        LogAction(
            "靶子模式：三连斩结束，等待下一轮"
        );
    }

    private void FixedUpdate()
    {
        if (targetDummyMode)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        if (player == null ||
            isDead ||
            isAttacking ||
            isStaggered ||
            isAirborneVulnerable)
        {
            return;
        }

        float distance = Mathf.Abs(player.position.x - transform.position.x);

        if (distance <= preferredDistance ||
            distance > maximumPursuitDistance)
        {
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            return;
        }

        FacePlayer();
        body.linearVelocity = new Vector2(
            facingDirection * moveSpeed,
            body.linearVelocity.y
        );
    }

    private void FindPlayer()
    {
        if (playerCombat == null)
            playerCombat = FindFirstObjectByType<StarCorePlayerCombat>();

        if (player == null && playerCombat != null)
            player = playerCombat.transform;
    }

    private void UpdatePhase()
    {
        float ratio = (float)currentHealth / maximumHealth;

        StarCoreBossPhase nextPhase;

        if (ratio <= phaseThreeHealthRatio)
            nextPhase = StarCoreBossPhase.Desperation;
        else if (ratio <= phaseTwoHealthRatio)
            nextPhase = StarCoreBossPhase.BrokenArmor;
        else
            nextPhase = StarCoreBossPhase.WeaponDuel;

        if (nextPhase != currentPhase)
        {
            LogAction(
                $"阶段切换：{GetPhaseName(currentPhase)} -> " +
                $"{GetPhaseName(nextPhase)}，生命比例 {ratio:P0}"
            );
            currentPhase = nextPhase;
        }
    }

    private IEnumerator PerformChosenAttack()
    {
        isAttacking = true;
        actionInterrupted = false;
        body.linearVelocity = Vector2.zero;
        FacePlayer();

        AttackKind previousAttack = lastAttack;
        AttackKind chosen = ChooseAttack();

        if (skillShowcaseMode &&
            previousAttack == AttackKind.RhythmCombo &&
            chosen == AttackKind.CrossDash)
        {
            yield return RetreatBeforeShowcaseCrossDash();
        }

        lastAttack = chosen;

        LogAction(
            $"招式开始：{GetAttackName(chosen)}，" +
            $"当前阶段 {GetPhaseName(currentPhase)}，" +
            $"与主角距离 {Vector2.Distance(transform.position, player.position):0.0}"
        );

        switch (chosen)
        {
            case AttackKind.RhythmCombo:
                yield return RhythmComboRoutine();
                break;

            case AttackKind.CrossDash:
                yield return CrossDashRoutine();
                break;

            case AttackKind.ThrownWeapon:
                yield return ThrownWeaponRoutine();
                break;

            case AttackKind.AirSlam:
                yield return AirSlamRoutine();
                break;

            case AttackKind.CounterStance:
                yield return CounterStanceRoutine();
                break;

            case AttackKind.Grab:
                yield return GrabRoutine();
                break;
        }

        ResetWeaponPose();
        counterActive = false;
        isAttacking = false;
        nextAttackTime = Time.time +
            (skillShowcaseMode
                ? showcaseAttackInterval
                : GetPhaseAttackInterval());

        LogAction($"招式结束：{GetAttackName(chosen)}");
    }

    private IEnumerator RetreatBeforeShowcaseCrossDash()
    {
        if (player == null)
            yield break;

        FacePlayer();

        float awayDirection = Mathf.Sign(
            transform.position.x - player.position.x
        );

        if (Mathf.Approximately(awayDirection, 0f))
            awayDirection = -facingDirection;

        Vector2 start = body.position;

        Vector2 end = start +
            Vector2.right *
            awayDirection *
            showcaseRetreatDistance;

        LogAction(
            "技能展示：三连斩结束，Boss 后撤准备交叉冲刺"
        );

        yield return MoveBody(
            start,
            end,
            showcaseRetreatDuration
        );

        FacePlayer();
    }

    private IEnumerator ReturnAfterShowcaseAirSlam(
    Vector2 originalPosition
)
    {
        LogAction(
            "技能展示：空中砸击结束，Boss 返回砸击前的位置"
        );

        yield return MoveBody(
            body.position,
            originalPosition,
            showcaseReturnDuration
        );

        FacePlayer();
    }
    private AttackKind ChooseAttack()
    {
        if (skillShowcaseMode)
        {
            AttackKind selected =
                showcaseAttackOrder[showcaseAttackIndex];

            showcaseAttackIndex =
                (showcaseAttackIndex + 1) %
                showcaseAttackOrder.Length;

            return selected;
        }

        if (rhythmComboOnlyMode)
            return AttackKind.RhythmCombo;

        float distance = Mathf.Abs(player.position.x - transform.position.x);
        AttackKind chosen;

        if (currentPhase == StarCoreBossPhase.WeaponDuel)
        {
            if (distance < comboHitRange)
                chosen = Random.value < 0.65f
                    ? AttackKind.RhythmCombo
                    : AttackKind.AirSlam;
            else
                chosen = Random.value < 0.55f
                    ? AttackKind.ThrownWeapon
                    : AttackKind.AirSlam;
        }
        else if (currentPhase == StarCoreBossPhase.BrokenArmor)
        {
            float roll = Random.value;

            if (roll < 0.3f)
                chosen = AttackKind.CrossDash;
            else if (roll < 0.55f)
                chosen = AttackKind.RhythmCombo;
            else if (roll < 0.78f)
                chosen = AttackKind.ThrownWeapon;
            else
                chosen = AttackKind.CounterStance;
        }
        else
        {
            float roll = Random.value;

            if (distance <= grabRange && roll < 0.3f)
                chosen = AttackKind.Grab;
            else if (roll < 0.58f)
                chosen = AttackKind.CrossDash;
            else if (roll < 0.8f)
                chosen = AttackKind.RhythmCombo;
            else
                chosen = AttackKind.AirSlam;
        }

        if (chosen == lastAttack)
            chosen = chosen == AttackKind.RhythmCombo
                ? AttackKind.ThrownWeapon
                : AttackKind.RhythmCombo;

        return chosen;
    }

    private IEnumerator RhythmComboRoutine()
    {
        float[] telegraphs =
            { fastStrikeTelegraph, fastStrikeTelegraph, delayedStrikeTelegraph };

        for (int hit = 0; hit < 3; hit++)
        {
            FacePlayer();
            LogAction(
                $"节奏三连斩第 {hit + 1}/3 击准备" +
                (hit == 2 ? "（延迟重击）" : "（快速攻击）")
            );

            Color strikeColor = hit == 2
                ? heavySlashColor
                : quickSlashColor;

            SpawnWeaponCharge(strikeColor, hit == 2 ? 1.35f : 0.85f);

            yield return AnimateWeaponTo(
                hit == 2 ? 120f * facingDirection : 65f * facingDirection,
                telegraphs[hit]
            );

            StarCoreDefenseResult result = AttackPlayer(
                comboHitRange,
                hit == 2 ? comboDamage + 1 : comboDamage,
                hit == 2 ? comboPostureDamage * 1.5f : comboPostureDamage,
                true
            );

            LogAction(
                $"节奏三连斩第 {hit + 1}/3 击结果：" +
                DescribeDefenseResult(result)
            );

            SpawnSlashEffect(
                hit == 2 ? -35f : -18f,
                strikeColor,
                hit == 2 ? 5.8f : 4.2f,
                hit == 2 ? 0.3f : 0.2f
            );

            if (result != StarCoreDefenseResult.Invulnerable)
            {
                feedback?.PlayImpact(
                    player.position,
                    strikeColor,
                    hit == 2 ? 2.8f : 1.8f,
                    hit == 2 ? 0.06f : 0.025f,
                    hit == 2 ? 0.12f : 0.05f
                );
            }

            yield return AnimateWeaponTo(
                -75f * facingDirection,
                0.1f
            );

            if (ShouldInterrupt(result))
            {
                yield return StaggerRoutine(staggerDuration * 0.75f);
                yield break;
            }

            yield return new WaitForSeconds(hit == 1 ? 0.12f : 0.08f);
        }
    }

    private IEnumerator CrossDashRoutine()
    {
        LogAction("交叉冲刺：第一段横向冲刺正在预警");
        Vector2 crossDashStart = body.position;
        Vector2 lockedPlayer = player.position;
        float direction = Mathf.Sign(lockedPlayer.x - transform.position.x);

        if (Mathf.Approximately(direction, 0f))
            direction = facingDirection;

        yield return AnimateWeaponTo(0f, 0.12f);
        SpawnWeaponCharge(new Color(0.25f, 0.85f, 1f, 0.9f), 1.25f);

        GameObject firstLine = CreateTelegraphLine(
            body.position,
            new Vector2(14f, 0.18f),
            0f
        );

        yield return new WaitForSeconds(0.42f);
        Destroy(firstLine);

        Vector2 firstEnd = new Vector2(
            lockedPlayer.x + direction * crossDashPassDistance,
            body.position.y
        );

        lastDashDefenseResult =
            StarCoreDefenseResult.Invulnerable;

        yield return DashBodyAndAttack(
            body.position,
            firstEnd,
            crossDashDuration,
            crossDashDamage
        );

        SpawnSlashEffect(
            0f,
            new Color(0.25f, 0.85f, 1f, 0.85f),
            6.5f,
            0.24f
        );

        LogAction(
            "交叉冲刺第一段结果：" +
            DescribeDefenseResult(lastDashDefenseResult)
        );

        if (ShouldInterrupt(lastDashDefenseResult))
        {
            yield return StaggerRoutine(staggerDuration);
            yield break;
        }

        yield return new WaitForSeconds(0.16f);

        LogAction("交叉冲刺：第二段斜向返冲正在预警");

        Vector2 secondEnd;

        if (skillShowcaseMode)
        {
            // 展示模式：第二段反冲回交叉冲刺开始的位置。
            secondEnd = crossDashStart;
        }
        else
        {
            // 正常战斗：保留原本的斜向返冲。
            secondEnd = new Vector2(
                lockedPlayer.x - direction * crossDashPassDistance,
                lockedPlayer.y + crossDashHeight
            );
        }

        float angle = Mathf.Atan2(
            secondEnd.y - body.position.y,
            secondEnd.x - body.position.x
        ) * Mathf.Rad2Deg;

        GameObject secondLine = CreateTelegraphLine(
            (body.position + secondEnd) * 0.5f,
            new Vector2(Vector2.Distance(body.position, secondEnd), 0.18f),
            angle
        );

        yield return new WaitForSeconds(0.32f);
        Destroy(secondLine);

        yield return AnimateWeaponTo(
            angle * facingDirection,
            0.1f
        );

        lastDashDefenseResult =
            StarCoreDefenseResult.Invulnerable;

        yield return DashBodyAndAttack(
            body.position,
            secondEnd,
            crossDashDuration,
            crossDashDamage
        );

        SpawnSlashEffect(
            angle,
            new Color(0.55f, 0.35f, 1f, 0.88f),
            6f,
            0.25f
        );

        LogAction(
            "交叉冲刺第二段结果：" +
            DescribeDefenseResult(lastDashDefenseResult)
        );

        if (ShouldInterrupt(lastDashDefenseResult))
            yield return StaggerRoutine(staggerDuration);
    }

    private IEnumerator ThrownWeaponRoutine()
    {
        LogAction("回旋武器投掷：武器蓄势");
        SpawnWeaponCharge(heavySlashColor, 1.45f);
        yield return AnimateWeaponTo(150f * facingDirection, 0.35f);

        Vector2 target = (Vector2)transform.position +
            Vector2.right * facingDirection * throwDistance;

        GameObject weaponObject = new GameObject("StarCoreThrownWeapon");
        weaponObject.transform.position = weaponRoot != null
            ? weaponRoot.position
            : transform.position;

        feedback?.PlayImpact(
            weaponObject.transform.position,
            heavySlashColor,
            2.4f,
            0.02f,
            0.04f
        );

        StarCoreThrownWeapon thrown =
            weaponObject.AddComponent<StarCoreThrownWeapon>();

        thrown.Configure(
            this,
            playerCombat,
            squareSprite,
            target,
            throwOutDuration,
            throwReturnDuration,
            thrownWeaponDamage,
            thrownWeaponHitRadius
        );

        if (weaponRoot != null)
            weaponRoot.gameObject.SetActive(false);

        while (thrown != null)
            yield return null;

        if (weaponRoot != null)
            weaponRoot.gameObject.SetActive(true);

        LogAction("回旋武器已经返回 Boss 手中");
    }

    private IEnumerator AirSlamRoutine()
    {
        LogAction($"空中砸击：Boss 上升 {airSlamHeight:0.0} 单位");
        Vector2 start = body.position;
        Vector2 apex = start + Vector2.up * airSlamHeight;

        yield return AnimateWeaponTo(90f * facingDirection, 0.16f);
        SpawnWeaponCharge(heavySlashColor, 1.4f);
        yield return MoveBody(start, apex, airSlamRiseDuration);

        yield return AnimateWeaponTo(-90f * facingDirection, 0.14f);

        Vector2 lockedTarget = player.position;
        GameObject marker = CreateTelegraphLine(
            new Vector2(lockedTarget.x, lockedTarget.y),
            new Vector2(airSlamRadius * 2f, 0.22f),
            0f
        );

        yield return new WaitForSeconds(0.42f);
        Destroy(marker);

        LogAction("空中砸击：锁定位置完成，开始下坠");

        Vector2 landing = new Vector2(lockedTarget.x, start.y);
        yield return MoveBody(body.position, landing, airSlamFallDuration);

        SpawnRadialBurst(
            landing,
            new Color(1f, 0.28f, 0.08f, 0.9f),
            airSlamRadius,
            8
        );

        StarCoreDefenseResult result = AttackPlayer(
            airSlamRadius,
            airSlamDamage,
            50f,
            false
        );

        LogAction(
            "空中砸击结果：" + DescribeDefenseResult(result)
        );

        feedback?.PlayImpact(
            landing,
            new Color(1f, 0.35f, 0.15f),
            4f,
            0.09f,
            0.18f
        );

        if (result == StarCoreDefenseResult.PerfectDodged)
            yield return StaggerRoutine(staggerDuration * 0.75f);
        else
            yield return new WaitForSeconds(0.55f);

        if (skillShowcaseMode && !isDead)
        {
            yield return ReturnAfterShowcaseAirSlam(start);
        }
    }

    private IEnumerator CounterStanceRoutine()
    {
        counterActive = true;

        LogAction(
            $"反击架势开启：持续 {counterStanceDuration:0.00} 秒，" +
            "此时攻击 Boss 会触发反击"
        );

        if (bossRenderer != null)
            bossRenderer.color = new Color(1f, 0.25f, 0.25f);

        yield return AnimateWeaponTo(88f * facingDirection, 0.12f);
        SpawnGuardCross(dangerEffectColor);

        float elapsed = 0f;

        while (elapsed < counterStanceDuration && counterActive)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        counterActive = false;

        if (bossRenderer != null)
            bossRenderer.color = originalColor;

        LogAction("反击架势自然结束：主角没有触发反击");
    }

    private IEnumerator CounterPunishRoutine()
    {
        isAttacking = true;
        counterActive = false;
        FacePlayer();

        LogAction("反击惩罚开始：主角在反击架势中攻击了 Boss");

        if (bossRenderer != null)
            bossRenderer.color = Color.red;

        SpawnWeaponCharge(Color.red, 1.3f);
        yield return new WaitForSeconds(0.12f);

        StarCoreDefenseResult result =
            AttackPlayer(4f, counterDamage, 45f, false);

        LogAction(
            "反击惩罚结果：" + DescribeDefenseResult(result)
        );

        yield return AnimateWeaponTo(-130f * facingDirection, 0.14f);
        SpawnSlashEffect(-12f, Color.red, 5.5f, 0.22f);
        yield return new WaitForSeconds(0.24f);

        if (bossRenderer != null)
            bossRenderer.color = originalColor;

        ResetWeaponPose();
        isAttacking = false;
        nextAttackTime = Time.time + attackInterval;
    }

    private IEnumerator GrabRoutine()
    {
        FacePlayer();

        LogAction(
            $"处决抓取开始：预警 {grabTelegraph:0.00} 秒"
        );

        if (bossRenderer != null)
            bossRenderer.color = new Color(1f, 0.15f, 0.15f);

        yield return AnimateWeaponTo(155f * facingDirection, 0.16f);

        GameObject grabWarning = CreateTelegraphLine(
            (body.position + (Vector2)player.position) * 0.5f,
            new Vector2(
                Vector2.Distance(body.position, player.position),
                0.28f
            ),
            0f,
            new Color(1f, 0.05f, 0.15f, 0.65f)
        );

        yield return new WaitForSeconds(
            Mathf.Max(0f, grabTelegraph - 0.16f)
        );

        Destroy(grabWarning);

        if (bossRenderer != null)
            bossRenderer.color = originalColor;

        Vector2 end = new Vector2(
            player.position.x - facingDirection * 0.8f,
            body.position.y
        );

        yield return MoveBody(body.position, end, 0.18f);

        yield return AnimateWeaponTo(0f, 0.11f);
        SpawnSlashEffect(0f, new Color(1f, 0.08f, 0.22f, 0.9f), 4f, 0.2f);

        StarCoreDefenseResult result = AttackPlayer(
            grabRange,
            grabDamage,
            100f,
            false
        );

        LogAction(
            "处决抓取结果：" + DescribeDefenseResult(result)
        );

        if (result == StarCoreDefenseResult.PerfectDodged)
            yield return StaggerRoutine(staggerDuration);
        else
            yield return new WaitForSeconds(0.45f);
    }

    private StarCoreDefenseResult AttackPlayer(
        float range,
        int damage,
        float postureDamage,
        bool parryable
    )
    {
        if (playerCombat == null ||
            Vector2.Distance(transform.position, player.position) > range)
        {
            return StarCoreDefenseResult.Invulnerable;
        }

        StarCoreDefenseResult result = playerCombat.ReceiveBossAttack(
            damage,
            postureDamage,
            parryable,
            transform.position
        );

        if (result == StarCoreDefenseResult.Parried)
            posture += 34f;
        else if (result == StarCoreDefenseResult.PerfectDodged)
            posture += 18f;

        return result;
    }

    private bool ShouldInterrupt(StarCoreDefenseResult result)
    {
        return result == StarCoreDefenseResult.Parried ||
               result == StarCoreDefenseResult.PerfectDodged ||
               actionInterrupted ||
               posture >= maximumPosture;
    }

    public bool ReceivePlayerHit(
        int damage,
        float postureDamage,
        bool launches,
        Vector2 hitPoint
    )
    {
        if (isDead)
            return false;

        if (targetDummyMode)
        {
            currentHealth = maximumHealth;

            posture = Mathf.Min(
                maximumPosture,
                posture + postureDamage
            );

            StartCoroutine(
                FlashRoutine(Color.white, 0.1f)
            );

            feedback?.PlayImpact(
                hitPoint,
                Color.white,
                launches ? 2.8f : 1.5f,
                launches ? 0.05f : 0.02f,
                launches ? 0.1f : 0.035f
            );

            LogAction(
                $"靶子模式受击：原始伤害 {damage}，" +
                $"血量锁定 {currentHealth}/{maximumHealth}，" +
                $"架势 {posture:0}/{maximumPosture:0}" +
                (launches
                    ? "，允许击飞并打开空中追击窗口"
                    : string.Empty)
            );

            if (launches)
            {
                StopAllCoroutines();
                StartCoroutine(LaunchRoutine());
            }
            else if (posture >= maximumPosture &&
                !isStaggered)
            {
                StopAllCoroutines();

                StartCoroutine(
                    StaggerRoutine(staggerDuration)
                );
            }

            return true;
        }

        if (counterActive)
        {
            LogAction(
                "反击架势被主角攻击触发：" +
                "取消受伤并开始反击惩罚"
            );

            counterActive = false;
            StopAllCoroutines();
            StartCoroutine(CounterPunishRoutine());
            return false;
        }

        int healthBefore = currentHealth;

        currentHealth = Mathf.Max(
            0,
            currentHealth - Mathf.Max(1, damage)
        );

        posture += postureDamage;

        LogAction(
            $"受到主角攻击：生命 {healthBefore} -> " +
            $"{currentHealth}/{maximumHealth}，" +
            $"架势 +{postureDamage:0}，" +
            $"当前架势 {posture:0}/{maximumPosture:0}" +
            (launches ? "，附带击飞" : string.Empty)
        );

        StartCoroutine(
            FlashRoutine(Color.white, 0.1f)
        );

        feedback?.PlayImpact(
            hitPoint,
            Color.white,
            launches ? 2.8f : 1.5f,
            launches ? 0.05f : 0.02f,
            launches ? 0.1f : 0.035f
        );

        if (currentHealth <= 0)
        {
            StopAllCoroutines();
            StartCoroutine(DeathRoutine());
            return true;
        }

        if (launches)
        {
            StopAllCoroutines();
            StartCoroutine(LaunchRoutine());
        }
        else if (posture >= maximumPosture)
        {
            StopAllCoroutines();

            StartCoroutine(
                StaggerRoutine(staggerDuration)
            );
        }

        return true;
    }

    public void OnThrownWeaponDefense(StarCoreDefenseResult result)
    {
        if (result == StarCoreDefenseResult.Parried)
        {
            LogAction("投掷武器被主角弹反：Boss 进入硬直");
            posture += 45f;
            actionInterrupted = true;
            StopAllCoroutines();
            StartCoroutine(StaggerRoutine(staggerDuration));
        }
        else if (result == StarCoreDefenseResult.PerfectDodged)
        {
            LogAction("主角完美闪避了投掷武器");
            posture += 15f;
        }
        else
        {
            LogAction(
                "投掷武器结果：" + DescribeDefenseResult(result)
            );
        }
    }

    private IEnumerator LaunchRoutine()
    {
        LogAction("被主角四连击终结段/完全蓄力斩击飞：进入空中追击窗口");
        ResetWeaponPose();
        isAttacking = true;
        isStaggered = true;
        isAirborneVulnerable = true;
        body.linearVelocity = Vector2.zero;

        StartCoroutine(
            FlashRoutine(new Color(0.25f, 0.9f, 1f), 0.22f)
        );

        SpawnRadialBurst(
            transform.position,
            new Color(0.25f, 0.9f, 1f, 0.85f),
            3.5f,
            6
        );

        Vector2 start = body.position;
        Vector2 apex = start + Vector2.up * launchHeight;

        yield return MoveBody(start, apex, launchRiseDuration);
        yield return new WaitForSeconds(0.16f);
        yield return MoveBody(apex, start, launchFallDuration);

        isAirborneVulnerable = false;
        isStaggered = false;
        isAttacking = false;
        posture = 25f;
        nextAttackTime = Time.time + attackInterval;

        LogAction("击飞状态结束：Boss 落地并恢复行动");
    }

    private IEnumerator StaggerRoutine(float duration)
    {
        LogAction($"Boss 进入架势崩溃硬直：持续 {duration:0.00} 秒");
        ResetWeaponPose();
        isAttacking = true;
        isStaggered = true;
        counterActive = false;
        body.linearVelocity = Vector2.zero;

        if (bossRenderer != null)
            bossRenderer.color = new Color(1f, 0.75f, 0.25f);

        SpawnRadialBurst(
            transform.position,
            new Color(1f, 0.7f, 0.1f, 0.9f),
            3f,
            6
        );

        yield return new WaitForSeconds(duration);

        if (bossRenderer != null)
            bossRenderer.color = originalColor;

        posture = 20f;
        isStaggered = false;
        isAttacking = false;
        nextAttackTime = Time.time + attackInterval;

        LogAction("Boss 硬直结束：恢复行动");
    }

    private IEnumerator DeathRoutine()
    {
        isDead = true;
        isAttacking = true;
        body.linearVelocity = Vector2.zero;

        LogAction("Boss 生命归零：死亡演出开始");

        if (bossRenderer != null)
            bossRenderer.color = Color.white;

        feedback?.PlayImpact(
            transform.position,
            Color.white,
            5f,
            0.12f,
            0.25f
        );

        yield return new WaitForSeconds(0.6f);

        if (bossRenderer != null)
            bossRenderer.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        LogAction("Boss 战结束");
        enabled = false;
    }

    private IEnumerator DashBodyAndAttack(
        Vector2 start,
        Vector2 end,
        float duration,
        int damage
    )
    {
        float elapsed = 0f;
        bool checkedHit = false;
        float nextAfterimageTime = 0f;

        while (elapsed < duration)
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            body.MovePosition(Vector2.Lerp(start, end, progress));

            if (elapsed >= nextAfterimageTime)
            {
                SpawnBossAfterimage(
                    new Color(0.25f, 0.75f, 1f, 0.35f)
                );
                nextAfterimageTime += 0.045f;
            }

            if (!checkedHit &&
                Vector2.Distance(transform.position, player.position) <= 2f)
            {
                lastDashDefenseResult =
                    AttackPlayer(2.2f, damage, 34f, true);
                checkedHit = true;
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        body.MovePosition(end);
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
                Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration))
            );

            body.MovePosition(Vector2.Lerp(start, end, progress));
            body.linearVelocity = Vector2.zero;
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        body.MovePosition(end);
        body.linearVelocity = Vector2.zero;
    }

    private IEnumerator AnimateWeaponTo(float angle, float duration)
    {
        if (weaponRoot == null)
        {
            yield return new WaitForSeconds(duration);
            yield break;
        }

        Quaternion start = weaponRoot.localRotation;
        Quaternion end = Quaternion.Euler(0f, 0f, angle);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / duration)
            );

            weaponRoot.localRotation = Quaternion.Lerp(start, end, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }

        weaponRoot.localRotation = end;
    }

    private void EnsureWeaponVisual()
    {
        if (weaponRoot == null || squareSprite == null)
            return;

        if (weaponRoot.localPosition.sqrMagnitude < 0.0001f)
        {
            weaponRoot.localPosition = new Vector3(
                weaponRestOffset.x,
                weaponRestOffset.y,
                0f
            );
        }

        if (weaponRoot.GetComponentsInChildren<SpriteRenderer>(true).Length > 0)
            return;

        CreateWeaponPiece(
            "WeaponShaft",
            new Vector2(0.34f, 0f),
            new Vector2(0.92f, 0.075f),
            0f,
            weaponMetalColor,
            weaponSortingOrder
        );

        CreateWeaponPiece(
            "WeaponEdge",
            new Vector2(0.83f, 0f),
            new Vector2(0.42f, 0.15f),
            0f,
            weaponEdgeColor,
            weaponSortingOrder + 2
        );

        CreateWeaponPiece(
            "WeaponTip",
            new Vector2(1.08f, 0f),
            new Vector2(0.34f, 0.13f),
            0f,
            weaponCoreColor,
            weaponSortingOrder + 3
        );

        CreateWeaponPiece(
            "WeaponGuard",
            new Vector2(-0.14f, 0f),
            new Vector2(0.09f, 0.34f),
            0f,
            weaponEdgeColor,
            weaponSortingOrder + 1
        );

        CreateWeaponPiece(
            "WeaponCore",
            new Vector2(0.05f, 0f),
            new Vector2(0.16f, 0.16f),
            45f,
            weaponCoreColor,
            weaponSortingOrder + 4
        );

        CreateWeaponPiece(
            "UpperHook",
            new Vector2(0.62f, 0.13f),
            new Vector2(0.28f, 0.055f),
            35f,
            weaponMetalColor,
            weaponSortingOrder + 1
        );

        CreateWeaponPiece(
            "LowerHook",
            new Vector2(0.62f, -0.13f),
            new Vector2(0.28f, 0.055f),
            -35f,
            weaponMetalColor,
            weaponSortingOrder + 1
        );
    }

    private void CreateWeaponPiece(
        string objectName,
        Vector2 localPosition,
        Vector2 localScale,
        float localAngle,
        Color color,
        int sortingOrder
    )
    {
        GameObject piece = new GameObject(objectName);
        piece.transform.SetParent(weaponRoot, false);
        piece.transform.localPosition = localPosition;
        piece.transform.localRotation = Quaternion.Euler(0f, 0f, localAngle);
        piece.transform.localScale = new Vector3(
            localScale.x,
            localScale.y,
            1f
        );

        SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
        renderer.sprite = squareSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private void ResetWeaponPose()
    {
        if (weaponRoot == null)
            return;

        weaponRoot.localPosition = weaponOriginalPosition;
        weaponRoot.localRotation = weaponOriginalRotation;
        weaponRoot.localScale = weaponOriginalScale;
        weaponRoot.gameObject.SetActive(true);
        UpdateWeaponFacing();
    }

    private GameObject CreateTelegraphLine(
        Vector2 center,
        Vector2 size,
        float angle
    )
    {
        return CreateTelegraphLine(
            center,
            size,
            angle,
            dangerEffectColor
        );
    }

    private GameObject CreateTelegraphLine(
        Vector2 center,
        Vector2 size,
        float angle,
        Color color
    )
    {
        if (squareSprite == null)
            return null;

        GameObject line = new GameObject("StarCoreTelegraph");
        line.transform.position = center;
        line.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        line.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = line.AddComponent<SpriteRenderer>();
        renderer.sprite = squareSprite;
        renderer.color = color;
        renderer.sortingOrder = 60;
        return line;
    }

    private void SpawnWeaponCharge(Color color, float size)
    {
        Vector2 position = weaponRoot != null
            ? weaponRoot.position
            : transform.position;

        feedback?.PlayImpact(position, color, size, 0f, 0f);

        CreateTransientLine(
            position,
            new Vector2(size * 1.5f, 0.12f),
            45f,
            color,
            effectLifetime
        );

        CreateTransientLine(
            position,
            new Vector2(size * 1.5f, 0.12f),
            -45f,
            color,
            effectLifetime
        );
    }

    private void SpawnSlashEffect(
        float forwardAngle,
        Color color,
        float length,
        float lifetime
    )
    {
        float worldAngle = facingDirection > 0f
            ? forwardAngle
            : 180f - forwardAngle;

        Vector2 direction = new Vector2(
            Mathf.Cos(worldAngle * Mathf.Deg2Rad),
            Mathf.Sin(worldAngle * Mathf.Deg2Rad)
        );

        Vector2 center = (Vector2)transform.position +
            direction * length * 0.35f +
            Vector2.up * 0.25f;

        for (int index = -1; index <= 1; index++)
        {
            Color layerColor = color;
            layerColor.a *= index == 0 ? 1f : 0.55f;

            CreateTransientLine(
                center,
                new Vector2(length, index == 0 ? 0.2f : 0.12f),
                worldAngle + index * 11f,
                layerColor,
                lifetime
            );
        }
    }

    private void SpawnRadialBurst(
        Vector2 position,
        Color color,
        float radius,
        int rayCount
    )
    {
        int count = Mathf.Max(3, rayCount);

        for (int index = 0; index < count; index++)
        {
            float angle = index * 360f / count;
            Vector2 direction = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            CreateTransientLine(
                position + direction * radius * 0.28f,
                new Vector2(radius * 0.65f, 0.14f),
                angle,
                color,
                effectLifetime * 1.35f
            );
        }
    }

    private void SpawnGuardCross(Color color)
    {
        Vector2 center = transform.position;

        CreateTransientLine(
            center,
            new Vector2(4.2f, 0.22f),
            45f,
            color,
            counterStanceDuration
        );

        CreateTransientLine(
            center,
            new Vector2(4.2f, 0.22f),
            -45f,
            color,
            counterStanceDuration
        );
    }

    private void SpawnBossAfterimage(Color color)
    {
        if (bossRenderer == null || bossRenderer.sprite == null)
            return;

        GameObject afterimage = new GameObject("StarCoreDashAfterimage");
        afterimage.transform.position = transform.position;
        afterimage.transform.rotation = transform.rotation;
        afterimage.transform.localScale = transform.lossyScale;

        SpriteRenderer renderer = afterimage.AddComponent<SpriteRenderer>();
        renderer.sprite = bossRenderer.sprite;
        renderer.color = color;
        renderer.flipX = bossRenderer.flipX;
        renderer.sortingOrder = bossRenderer.sortingOrder - 1;

        Destroy(afterimage, effectLifetime * 1.4f + 0.08f);
        StartCoroutine(
            FadeAndDestroy(afterimage, effectLifetime * 1.4f, 1.12f)
        );
    }

    private GameObject CreateTransientLine(
        Vector2 center,
        Vector2 size,
        float angle,
        Color color,
        float lifetime
    )
    {
        if (squareSprite == null)
            return null;

        GameObject line = new GameObject("StarCoreAttackEffect");
        line.transform.position = center;
        line.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        line.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = line.AddComponent<SpriteRenderer>();
        renderer.sprite = squareSprite;
        renderer.color = color;
        renderer.sortingOrder = 70;

        Destroy(line, Mathf.Max(0.02f, lifetime) + 0.08f);
        StartCoroutine(
            FadeAndDestroy(line, Mathf.Max(0.02f, lifetime), 1.18f)
        );
        return line;
    }

    private IEnumerator FadeAndDestroy(
        GameObject target,
        float duration,
        float endScale
    )
    {
        if (target == null)
            yield break;

        SpriteRenderer[] renderers =
            target.GetComponentsInChildren<SpriteRenderer>();
        Vector3 startScale = target.transform.localScale;
        float elapsed = 0f;

        while (target != null && elapsed < duration)
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            target.transform.localScale = Vector3.Lerp(
                startScale,
                startScale * endScale,
                progress
            );

            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                Color current = renderer.color;
                current.a *= 1f - progress;
                renderer.color = current;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (target != null)
            Destroy(target);
    }

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        if (bossRenderer == null)
            yield break;

        Color before = bossRenderer.color;
        bossRenderer.color = color;
        yield return new WaitForSeconds(duration);

        if (!isDead)
            bossRenderer.color = before;
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        float direction = Mathf.Sign(player.position.x - transform.position.x);

        if (!Mathf.Approximately(direction, 0f))
            facingDirection = direction;

        if (bossRenderer != null)
            bossRenderer.flipX = facingDirection < 0f;

        UpdateWeaponFacing();
    }

    private void UpdateWeaponFacing()
    {
        if (weaponRoot == null)
            return;

        Vector3 position = weaponRoot.localPosition;
        position.x = Mathf.Abs(weaponOriginalPosition.x) * facingDirection;
        weaponRoot.localPosition = position;

        Vector3 scale = weaponRoot.localScale;
        float baseScaleX = Mathf.Approximately(weaponOriginalScale.x, 0f)
            ? 1f
            : Mathf.Abs(weaponOriginalScale.x);
        scale.x = baseScaleX * facingDirection;
        weaponRoot.localScale = scale;
    }

    private string GetAttackName(AttackKind attack)
    {
        switch (attack)
        {
            case AttackKind.RhythmCombo:
                return "节奏三连斩";
            case AttackKind.CrossDash:
                return "交叉冲刺";
            case AttackKind.ThrownWeapon:
                return "回旋武器投掷";
            case AttackKind.AirSlam:
                return "空中砸击";
            case AttackKind.CounterStance:
                return "反击架势";
            case AttackKind.Grab:
                return "处决抓取";
            default:
                return attack.ToString();
        }
    }

    private string GetPhaseName(StarCoreBossPhase phase)
    {
        switch (phase)
        {
            case StarCoreBossPhase.WeaponDuel:
                return "第一阶段·武器决斗";
            case StarCoreBossPhase.BrokenArmor:
                return "第二阶段·破甲暴走";
            case StarCoreBossPhase.Desperation:
                return "第三阶段·殊死处刑";
            default:
                return phase.ToString();
        }
    }

    private string DescribeDefenseResult(
        StarCoreDefenseResult result
    )
    {
        switch (result)
        {
            case StarCoreDefenseResult.Hit:
                return "命中主角";
            case StarCoreDefenseResult.Blocked:
                return "被主角普通格挡";
            case StarCoreDefenseResult.Parried:
                return "被主角弹反";
            case StarCoreDefenseResult.PerfectDodged:
                return "被主角完美闪避";
            case StarCoreDefenseResult.GuardBroken:
                return "击破主角防御";
            case StarCoreDefenseResult.Invulnerable:
                return "未命中，或主角处于无敌状态";
            default:
                return result.ToString();
        }
    }

    private void LogAction(string message)
    {
        if (showCombatLogs)
            Debug.Log($"[星核战斗][Boss] {message}", this);
    }

    private float GetPhaseAttackInterval()
    {
        if (currentPhase == StarCoreBossPhase.Desperation)
            return attackInterval * 0.65f;

        if (currentPhase == StarCoreBossPhase.BrokenArmor)
            return attackInterval * 0.82f;

        return attackInterval;
    }
}
