using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SpecialEnemyThrowAttack : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Rigidbody2D playerBody;

    [Header("Enemy")]
    [SerializeField] private EnemyAI enemyAI;
    [SerializeField] private SpecialEnemyPounce enemyPounce;
    [SerializeField] private SpecialEnemyChargeAttack enemyCharge;
    [SerializeField] private SpriteRenderer enemyRenderer;

    [Header("Weapon")]
    [SerializeField] private Transform weaponRig;
    [SerializeField] private Transform weaponMotionRoot;
    [SerializeField] private Transform hookCatchPoint;
    [SerializeField] private Transform spearTipPoint;
    [SerializeField] private EnemyWeaponPose weaponPose;
    [SerializeField] private EnemyWeaponFacing weaponFacing;

    [Header("Throw Camera")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private Vector2 enemyCameraCenterOffset = Vector2.zero;

    [Min(0f)]
    [SerializeField] private float cameraLockMoveSpeed = 40f;

    [Header("Trigger")]
    [Min(0f)]
    [SerializeField] private float maximumGrabDistance = 3.5f;

    [Min(0f)]
    [SerializeField] private float maximumVerticalDistance = 3f;

    [Min(0f)]
    [SerializeField] private float firstAttackDelay = 3f;

    [Min(0f)]
    [SerializeField] private float attackCooldown = 8f;

    [Header("Trigger Mode")]
    [SerializeField] private bool useAutomaticDistanceTrigger;

    [Header("Hook Strike")]
    [SerializeField] private float windupForwardAngle = -35f;
    [SerializeField] private float strikeForwardAngle = 75f;

    [Min(0.01f)]
    [SerializeField] private float windupDuration = 0.3f;

    [Min(0.01f)]
    [SerializeField] private float strikeDuration = 0.25f;

    [Min(0f)]
    [SerializeField] private float hookHoldDuration = 0.8f;

    [Min(0.01f)]
    [SerializeField] private float hookHitRadius = 1.2f;

    [Min(1)]
    [SerializeField] private int hookDamage = 1;

    [Header("Throw Up")]
    [SerializeField] private float liftForwardAngle = 0f;

    [Min(0f)]
    [SerializeField] private float weaponLiftDistance = 1.5f;

    [Min(0.01f)]
    [SerializeField] private float weaponLiftDuration = 0.15f;

    [Min(0f)]
    [SerializeField] private float throwApexHeight = 10f;

    [Min(0.01f)]
    [SerializeField] private float throwRiseDuration = 0.55f;

    [Min(0f)]
    [SerializeField] private float playerControlReturnDelay = 0.6f;

    [Header("Impale And Fling")]
    [Min(0f)]
    [SerializeField] private float impaleTriggerHeight = 6f;

    [Min(0.01f)]
    [SerializeField] private float synchronizedImpaleDuration = 1f;

    [SerializeField] private float impaleWeaponAngle = 0f;

    [Min(0f)]
    [SerializeField] private float impaleStartTipHeight = 1.5f;

    [Min(0.01f)]
    [SerializeField] private float impalePrepareDuration = 0.7f;

    [Min(0f)]
    [SerializeField] private float impaleHoldDuration = 0.5f;

    [Min(1)]
    [SerializeField] private int impaleDamage = 1;

    [SerializeField] private float forwardFlingAngle = 85f;

    [Min(0.01f)]
    [SerializeField] private float forwardFlingDuration = 0.35f;

    [Min(0f)]
    [SerializeField] private float finalHorizontalSpeed = 18f;

    [SerializeField] private float finalUpwardSpeed = 3f;

    [Header("Safe Fling Recovery")]
    [Tooltip("甩飞前用于检查地板的层。留空时自动使用名为 Ground 的层。")]
    [SerializeField] private LayerMask groundLayer;

    [Tooltip("主角与地板重叠时，最多向上寻找多高的安全位置。")]
    [Min(0.1f)]
    [SerializeField] private float safeFlingSearchHeight = 10f;

    [Tooltip("寻找安全位置时，每次向上移动的距离。")]
    [Min(0.01f)]
    [SerializeField] private float safeFlingSearchStep = 0.25f;

    private Rigidbody2D enemyBody;

    private Vector3 originalWeaponMotionPosition;
    private Quaternion originalWeaponMotionRotation;
    private Vector3 originalWeaponMotionScale;
    private Quaternion originalWeaponRigRotation;

    private bool attackEnabled;
    private bool isAttacking;
    private bool hookHitDetected;
    private bool impaleSucceeded;
    private float nextAttackTime;

    private bool playerControllerWasEnabled;
    private bool playerBodyWasSimulated;
    private bool cameraLockedToEnemy;
    private Collider2D playerCollider;

    private readonly Collider2D[] groundOverlapResults =
        new Collider2D[8];

    public bool IsAttacking => isAttacking;

    private void Awake()
    {
        enemyBody = GetComponent<Rigidbody2D>();

        if (enemyAI == null)
            enemyAI = GetComponent<EnemyAI>();

        if (enemyPounce == null)
            enemyPounce = GetComponent<SpecialEnemyPounce>();

        if (enemyCharge == null)
        {
            enemyCharge =
                GetComponent<SpecialEnemyChargeAttack>();
        }

        if (enemyRenderer == null)
            enemyRenderer = GetComponent<SpriteRenderer>();

        if (weaponPose == null)
        {
            weaponPose =
                GetComponentInChildren<EnemyWeaponPose>(true);
        }

        if (weaponRig == null && weaponPose != null)
            weaponRig = weaponPose.transform;

        if (weaponFacing == null && weaponRig != null)
        {
            weaponFacing =
                weaponRig.GetComponent<EnemyWeaponFacing>();
        }

        if (weaponMotionRoot == null && weaponRig != null)
        {
            weaponMotionRoot = FindDescendant(
                weaponRig,
                "WeaponMotionRoot"
            );
        }

        if (weaponMotionRoot != null)
        {
            if (hookCatchPoint == null)
            {
                hookCatchPoint = FindDescendant(
                    weaponMotionRoot,
                    "HookCatchPoint"
                );
            }

            if (spearTipPoint == null)
            {
                spearTipPoint = FindDescendant(
                    weaponMotionRoot,
                    "SpearTipPoint"
                );
            }

            originalWeaponMotionPosition =
                weaponMotionRoot.localPosition;

            originalWeaponMotionRotation =
                weaponMotionRoot.localRotation;

            originalWeaponMotionScale =
                weaponMotionRoot.localScale;
        }

        if (weaponRig != null)
        {
            originalWeaponRigRotation =
                weaponRig.localRotation;
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (cameraFollow == null && mainCamera != null)
        {
            cameraFollow =
                mainCamera.GetComponent<CameraFollow>();
        }

        FindPlayer();
    }

    private void LateUpdate()
    {
        if (!cameraLockedToEnemy || mainCamera == null)
            return;

        Vector3 targetPosition = new Vector3(
            transform.position.x + enemyCameraCenterOffset.x,
            transform.position.y + enemyCameraCenterOffset.y,
            mainCamera.transform.position.z
        );

        if (cameraLockMoveSpeed <= 0f)
        {
            mainCamera.transform.position = targetPosition;
            return;
        }

        mainCamera.transform.position = Vector3.MoveTowards(
            mainCamera.transform.position,
            targetPosition,
            cameraLockMoveSpeed * Time.unscaledDeltaTime
        );
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

        if (enemyCharge != null &&
            enemyCharge.IsAttacking)
        {
            return;
        }

        float horizontalDistance = Mathf.Abs(
            player.position.x - transform.position.x
        );

        float verticalDistance = Mathf.Abs(
            player.position.y - transform.position.y
        );

        if (horizontalDistance <= maximumGrabDistance &&
            verticalDistance <= maximumVerticalDistance)
        {
            StartCoroutine(PerformHookTest());
        }
    }

    public void EnableThrowAttack()
    {
        attackEnabled = true;
        enabled = true;
        nextAttackTime = Time.time + firstAttackDelay;
    }

    public bool TryStartThrowFromDirector()
    {
        return TryStartThrowFromDirector(
            maximumGrabDistance
        );
    }

    public bool TryStartThrowFromDirector(
        float releaseDistance
    )
    {


        enabled = true;
        attackEnabled = true;

        StartCoroutine(PerformHookTest());
        return true;
    }

    public bool CanStartThrowFromDirector()
    {
        return CanStartThrowFromDirector(
            maximumGrabDistance
        );
    }

    public bool CanStartThrowFromDirector(
        float releaseDistance
    )
    {
        if (isAttacking)
            return false;

        FindPlayer();

        if (player == null)
            return false;

        float horizontalDistance = Mathf.Abs(
            player.position.x - transform.position.x
        );

        float verticalDistance = Mathf.Abs(
            player.position.y - transform.position.y
        );

        return horizontalDistance <= releaseDistance &&
            verticalDistance <= maximumVerticalDistance;
    }
    

    public void SetAutomaticDistanceTrigger(bool value)
    {
        useAutomaticDistanceTrigger = value;
    }

    public void DisableThrowAttack()
    {
        attackEnabled = false;
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

        if (player == null)
            return;

        if (playerController == null)
        {
            playerController =
                player.GetComponent<PlayerController>();
        }

        if (playerHealth == null)
        {
            playerHealth =
                player.GetComponent<PlayerHealth>();
        }

        if (playerBody == null)
        {
            playerBody =
                player.GetComponent<Rigidbody2D>();
        }

        if (playerCollider == null)
        {
            playerCollider =
                player.GetComponent<Collider2D>();
        }
    }

    private Transform FindDescendant(
        Transform root,
        string objectName
    )
    {
        if (root == null)
            return null;

        Transform[] descendants =
            root.GetComponentsInChildren<Transform>(true);

        foreach (Transform descendant in descendants)
        {
            if (descendant.name == objectName)
                return descendant;
        }

        return null;
    }

    private IEnumerator PerformHookTest()
    {
        isAttacking = true;
        hookHitDetected = false;
        impaleSucceeded = false;

        float attackDirection = Mathf.Sign(
            player.position.x - transform.position.x
        );

        if (Mathf.Approximately(attackDirection, 0f))
            attackDirection = 1f;

        PauseOtherEnemyActions();

        if (enemyBody != null)
            enemyBody.linearVelocity = Vector2.zero;

        if (enemyRenderer != null)
        {
            enemyRenderer.flipX =
                attackDirection < 0f;
        }

        if (weaponPose != null)
            weaponPose.enabled = false;

        if (weaponFacing != null)
            weaponFacing.SetThrowMirror(true);

        if (weaponMotionRoot != null)
        {
            weaponMotionRoot.localScale =
                originalWeaponMotionScale;
        }

        float windupAngle = GetSignedAngle(
            windupForwardAngle,
            attackDirection
        );

        yield return RotateWeapon(
            windupAngle,
            windupDuration,
            false
        );

        float strikeAngle = GetSignedAngle(
            strikeForwardAngle,
            attackDirection
        );

        yield return RotateWeapon(
            strikeAngle,
            strikeDuration,
            true
        );

        if (hookHitDetected)
        {
            Debug.Log(
                "SpecialEnemyThrowAttack：双齿成功勾住主角。"
            );

            yield return HoldPlayerAtHook();

            float liftAngle = GetSignedAngle(
                liftForwardAngle,
                attackDirection
            );

            yield return LiftWeaponAndPlayer(liftAngle);

            // 主角即将被抛起：镜头暂时固定在特殊敌人。
            LockCameraToEnemy();

            yield return ThrowPlayerIntoAir();

            if (weaponFacing != null)
                weaponFacing.SetThrowMirror(false);

            float impaleAngle = GetSignedAngle(
                impaleWeaponAngle,
                attackDirection
            );

            // 主角仍停留在最高点时，武器先回到敌人中心，
            // 然后主角下落与武器上刺同时开始。
            yield return PrepareWeaponForImpale(
                impaleAngle
            );

            yield return DescendAndStabTogether();

            if (impaleSucceeded)
            {
                if (playerHealth != null)
                    playerHealth.TakeDamage(impaleDamage);

                yield return HoldPlayerAtSpearTip();

                float flingAngle = GetSignedAngle(
                    forwardFlingAngle,
                    attackDirection
                );

                yield return SwingWeaponWithImpaledPlayer(
                    flingAngle
                );

                FlingPlayerSideways(attackDirection);

                // 甩飞发生的同一刻把镜头交还给主角。
                ReleaseCameraToPlayer();

                yield return new WaitForSeconds(
                    playerControlReturnDelay
                );
            }

            RestorePlayerControl();
        }
        else
        {
            Debug.Log(
                "SpecialEnemyThrowAttack：双齿敲击未命中。"
            );
        }

        // 穿刺中途失败时也必须恢复主角镜头，
        // 避免镜头永久停留在特殊敌人身上。
        if (cameraLockedToEnemy)
            ReleaseCameraToPlayer();

        RestoreWeapon();
        ResumeOtherEnemyActions();

        nextAttackTime = Time.time + attackCooldown;
        isAttacking = false;
    }

    private void PauseOtherEnemyActions()
    {
        if (enemyAI != null)
            enemyAI.enabled = false;

        if (enemyPounce != null)
            enemyPounce.enabled = false;

        if (enemyCharge != null)
            enemyCharge.DisableChargeAttack();
    }

    private void ResumeOtherEnemyActions()
    {
        if (enemyPounce != null)
            enemyPounce.EnablePounce();

        if (enemyCharge != null)
            enemyCharge.EnableChargeAttack();

        if (enemyAI != null)
            enemyAI.enabled = true;
    }

    private void LockCameraToEnemy()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        if (cameraFollow == null)
        {
            cameraFollow =
                mainCamera.GetComponent<CameraFollow>();
        }

        if (cameraFollow != null)
            cameraFollow.enabled = false;

        cameraLockedToEnemy = true;
    }

    private void ReleaseCameraToPlayer()
    {
        cameraLockedToEnemy = false;

        if (cameraFollow != null)
        {
            cameraFollow.enabled = true;
            return;
        }

        // 没有 CameraFollow 时的保险处理。
        if (mainCamera != null && player != null)
        {
            Vector3 playerCameraPosition = new Vector3(
                player.position.x,
                player.position.y,
                mainCamera.transform.position.z
            );

            mainCamera.transform.position =
                playerCameraPosition;
        }
    }

    private float GetSignedAngle(
        float forwardAngle,
        float facingDirection
    )
    {
        if (facingDirection > 0f)
            return -forwardAngle;

        return forwardAngle;
    }

    private IEnumerator RotateWeapon(
        float targetAngle,
        float duration,
        bool checkForHit
    )
    {
        if (weaponRig == null)
            yield break;

        Quaternion startRotation =
            weaponRig.localRotation;

        Quaternion targetRotation =
            Quaternion.Euler(0f, 0f, targetAngle);

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

            if (checkForHit && !hookHitDetected)
                TryHookPlayer();

            elapsed += Time.deltaTime;
            yield return null;
        }

        weaponRig.localRotation = targetRotation;

        if (checkForHit && !hookHitDetected)
            TryHookPlayer();
    }

    private void TryHookPlayer()
    {
        if (hookCatchPoint == null ||
            playerHealth == null)
        {
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            hookCatchPoint.position,
            hookHitRadius
        );

        foreach (Collider2D hit in hits)
        {
            PlayerHealth hitHealth =
                hit.GetComponentInParent<PlayerHealth>();

            if (hitHealth == playerHealth)
            {
                hookHitDetected = true;
                playerHealth.TakeDamage(hookDamage);
                CapturePlayer();
                return;
            }
        }
    }

    private void CapturePlayer()
    {
        if (playerController != null)
        {
            playerControllerWasEnabled =
                playerController.enabled;

            playerController.enabled = false;
        }

        if (playerBody != null)
        {
            playerBodyWasSimulated =
                playerBody.simulated;

            playerBody.linearVelocity = Vector2.zero;
            playerBody.simulated = false;
        }

        FollowPoint(hookCatchPoint);
    }

    private IEnumerator HoldPlayerAtHook()
    {
        float elapsed = 0f;

        while (elapsed < hookHoldDuration)
        {
            FollowPoint(hookCatchPoint);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator LiftWeaponAndPlayer(
        float targetAngle
    )
    {
        if (weaponRig == null)
            yield break;

        Quaternion startRotation =
            weaponRig.localRotation;

        Quaternion targetRotation =
            Quaternion.Euler(0f, 0f, targetAngle);

        Vector3 startWeaponPosition =
            weaponMotionRoot != null
                ? weaponMotionRoot.position
                : Vector3.zero;

        Vector3 targetWeaponPosition =
            startWeaponPosition +
            Vector3.up * weaponLiftDistance;

        float elapsed = 0f;

        while (elapsed < weaponLiftDuration)
        {
            float progress = Mathf.Clamp01(
                elapsed / weaponLiftDuration
            );

            progress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            weaponRig.localRotation =
                Quaternion.Lerp(
                    startRotation,
                    targetRotation,
                    progress
                );

            if (weaponMotionRoot != null)
            {
                weaponMotionRoot.position =
                    Vector3.Lerp(
                        startWeaponPosition,
                        targetWeaponPosition,
                        progress
                    );
            }

            FollowPoint(hookCatchPoint);

            elapsed += Time.deltaTime;
            yield return null;
        }

        weaponRig.localRotation = targetRotation;

        if (weaponMotionRoot != null)
        {
            weaponMotionRoot.position =
                targetWeaponPosition;
        }

        FollowPoint(hookCatchPoint);
    }

    private void SetPlayerWorldPosition(
        Vector3 worldPosition
    )
    {
        if (player == null)
            return;

        worldPosition.z = player.position.z;

        if (playerBody != null)
        {
            playerBody.position = new Vector2(
                worldPosition.x,
                worldPosition.y
            );
        }

        player.position = worldPosition;
    }

    private void FollowPoint(Transform point)
    {
        if (point == null)
            return;

        SetPlayerWorldPosition(point.position);
    }

    private IEnumerator ThrowPlayerIntoAir()
    {
        if (player == null || playerBody == null)
        {
            Debug.LogError(
                "上抛失败：Player 或 Player Body 没有引用。"
            );

            yield break;
        }

        playerBody.linearVelocity = Vector2.zero;
        playerBody.simulated = false;

        Vector3 startPosition = player.position;

        // Throw Apex Height 表示相对当前起点实际上升多少距离。
        float apexY =
            startPosition.y + throwApexHeight;

        Vector3 apexPosition = new Vector3(
            transform.position.x,
            apexY,
            startPosition.z
        );

        Debug.Log(
            "SpecialEnemyThrowAttack：Throw Apex Height = " +
            throwApexHeight +
            "，目标最高点 Y = " +
            apexPosition.y
        );

        float elapsed = 0f;

        while (elapsed < throwRiseDuration)
        {
            float t = elapsed /
                Mathf.Max(throwRiseDuration, 0.01f);

            t = Mathf.SmoothStep(0f, 1f, t);

            Vector3 currentPosition = Vector3.Lerp(
                startPosition,
                apexPosition,
                t
            );

            SetPlayerWorldPosition(currentPosition);

            elapsed += Time.deltaTime;
            yield return null;
        }

        SetPlayerWorldPosition(apexPosition);

        // 在恢复物理前再次同步 Rigidbody2D 的位置，
        // 防止物理系统把主角拉回上抛前的位置。
        playerBody.position = new Vector2(
            apexPosition.x,
            apexPosition.y
        );

        playerBody.linearVelocity = Vector2.zero;
        playerBody.simulated = false;

        Debug.Log(
            "SpecialEnemyThrowAttack：主角实际到达 Y = " +
            playerBody.position.y
        );
    }

    private IEnumerator PrepareWeaponForImpale(
        float targetAngle
    )
    {
        if (weaponMotionRoot == null ||
            spearTipPoint == null)
        {
            Debug.LogError(
                "穿刺准备失败：缺少 WeaponMotionRoot " +
                "或 SpearTipPoint 引用。"
            );

            yield break;
        }

        Quaternion startRotation =
            weaponMotionRoot.localRotation;

        Quaternion targetRotation =
            Quaternion.Euler(0f, 0f, targetAngle);

        float halfDuration = Mathf.Max(
            impalePrepareDuration * 0.5f,
            0.01f
        );

        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            float t = elapsed / halfDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            weaponMotionRoot.localRotation =
                Quaternion.Lerp(
                    startRotation,
                    targetRotation,
                    t
                );

            elapsed += Time.deltaTime;
            yield return null;
        }

        weaponMotionRoot.localRotation = targetRotation;

        Vector3 tipOffset =
            spearTipPoint.position -
            weaponMotionRoot.position;

        Vector3 desiredTipPosition = new Vector3(
            transform.position.x,
            transform.position.y + impaleStartTipHeight,
            spearTipPoint.position.z
        );

        Vector3 startRootPosition =
            weaponMotionRoot.position;

        Vector3 targetRootPosition =
            desiredTipPosition - tipOffset;

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            float t = elapsed / halfDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            weaponMotionRoot.position = Vector3.Lerp(
                startRootPosition,
                targetRootPosition,
                t
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        weaponMotionRoot.position = targetRootPosition;

        Debug.Log(
            "SpecialEnemyThrowAttack：" +
            "WeaponMainTip 已回到敌人中心。"
        );
    }

    private IEnumerator DescendAndStabTogether()
    {
        impaleSucceeded = false;

        if (weaponMotionRoot == null ||
            spearTipPoint == null ||
            player == null)
        {
            Debug.LogError(
                "同步穿刺失败：缺少相关引用。"
            );

            yield break;
        }

        Vector3 playerStartPosition =
            player.position;

        Vector3 playerTargetPosition = new Vector3(
            transform.position.x,
            transform.position.y + impaleTriggerHeight,
            player.position.z
        );

        if (playerTargetPosition.y >= playerStartPosition.y)
        {
            Debug.LogError(
                "同步穿刺失败：Impale Trigger Height " +
                "必须低于主角的最高点。"
            );

            yield break;
        }

        Vector3 weaponStartPosition =
            weaponMotionRoot.position;

        float upwardDistance =
            playerTargetPosition.y -
            spearTipPoint.position.y;

        if (upwardDistance < 0f)
        {
            Debug.LogError(
                "同步穿刺失败：准备位置的 WeaponMainTip " +
                "已经高于目标穿刺高度。"
            );

            yield break;
        }

        Vector3 weaponTargetPosition =
            weaponStartPosition +
            Vector3.up * upwardDistance;

        float elapsed = 0f;

        while (elapsed < synchronizedImpaleDuration)
        {
            float t = Mathf.Clamp01(
                elapsed /
                Mathf.Max(synchronizedImpaleDuration, 0.01f)
            );

            // 主角下落逐渐加速。
            float playerProgress = t * t;

            // 武器从一开始就向上刺，临近命中时逐渐减速。
            float weaponProgress =
                Mathf.SmoothStep(0f, 1f, t);

            Vector3 currentPlayerPosition =
                Vector3.Lerp(
                    playerStartPosition,
                    playerTargetPosition,
                    playerProgress
                );

            SetPlayerWorldPosition(
                currentPlayerPosition
            );

            weaponMotionRoot.position = Vector3.Lerp(
                weaponStartPosition,
                weaponTargetPosition,
                weaponProgress
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        weaponMotionRoot.position =
            weaponTargetPosition;

        FollowPoint(spearTipPoint);
        impaleSucceeded = true;

        Debug.Log(
            "SpecialEnemyThrowAttack：" +
            "主角下落与 WeaponMainTip 上刺同步完成。"
        );
    }

    private IEnumerator HoldPlayerAtSpearTip()
    {
        float elapsed = 0f;

        while (elapsed < impaleHoldDuration)
        {
            FollowPoint(spearTipPoint);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator SwingWeaponWithImpaledPlayer(
        float targetAngle
    )
    {
        if (weaponRig == null)
            yield break;

        Quaternion startRotation =
            weaponRig.localRotation;

        Quaternion targetRotation =
            Quaternion.Euler(0f, 0f, targetAngle);

        float elapsed = 0f;

        while (elapsed < forwardFlingDuration)
        {
            float progress = Mathf.Clamp01(
                elapsed / forwardFlingDuration
            );

            progress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            weaponRig.localRotation = Quaternion.Lerp(
                startRotation,
                targetRotation,
                progress
            );

            FollowPoint(spearTipPoint);

            elapsed += Time.deltaTime;
            yield return null;
        }

        weaponRig.localRotation = targetRotation;
        FollowPoint(spearTipPoint);
    }

    private void FlingPlayerSideways(
        float attackDirection
    )
    {
        if (playerBody == null)
            return;

        // 主角此前一直跟随枪尖，枪尖可能正好插进地板碰撞体。
        // 先关闭主角物理、向上找到不与 Ground 重叠的位置，
        // 再交还物理并施加甩飞速度，避免从地板内部开始结算。
        playerBody.simulated = false;
        MovePlayerToSafeFlingPosition();

        playerBody.simulated = playerBodyWasSimulated;

        if (!playerBody.simulated)
            return;

        playerBody.linearVelocity = new Vector2(
            attackDirection * finalHorizontalSpeed,
            finalUpwardSpeed
        );
    }

    private void MovePlayerToSafeFlingPosition()
    {
        if (player == null ||
            playerBody == null ||
            playerCollider == null)
        {
            return;
        }

        int groundMask = groundLayer.value != 0
            ? groundLayer.value
            : LayerMask.GetMask("Ground");

        if (groundMask == 0)
        {
            Debug.LogWarning(
                "SpecialEnemyThrowAttack：未找到 Ground 层，" +
                "无法执行甩飞落点安全检测。"
            );
            return;
        }

        Vector3 startPosition = player.position;
        float step = Mathf.Max(
            safeFlingSearchStep,
            0.01f
        );
        int attempts = Mathf.CeilToInt(
            safeFlingSearchHeight / step
        );

        for (int i = 0; i <= attempts; i++)
        {
            Vector3 candidate = startPosition +
                Vector3.up * (i * step);

            SetPlayerWorldPosition(candidate);
            Physics2D.SyncTransforms();

            if (!IsPlayerOverlappingGround(groundMask))
                return;
        }

        // 罕见情况下整段搜索区域都被地形占据时，
        // 仍把主角移到搜索区域上方，避免继续留在地板内部。
        SetPlayerWorldPosition(
            startPosition +
            Vector3.up * (safeFlingSearchHeight + step)
        );

        Physics2D.SyncTransforms();

        Debug.LogWarning(
            "SpecialEnemyThrowAttack：甩飞落点附近没有找到空位，" +
            "已将主角移到地形上方。"
        );
    }

    private bool IsPlayerOverlappingGround(
        int groundMask
    )
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(groundMask);
        filter.useTriggers = false;

        return playerCollider.Overlap(
            filter,
            groundOverlapResults
        ) > 0;
    }

    private void RestorePlayerControl()
    {
        if (playerBody != null &&
            playerBodyWasSimulated &&
            !playerBody.simulated)
        {
            playerBody.simulated = true;
        }

        if (playerController != null &&
            playerControllerWasEnabled)
        {
            playerController.enabled = true;
        }
    }

    private void RestoreWeapon()
    {
        if (weaponFacing != null)
            weaponFacing.SetThrowMirror(false);

        if (weaponMotionRoot != null)
        {
            weaponMotionRoot.localPosition =
                originalWeaponMotionPosition;

            weaponMotionRoot.localRotation =
                originalWeaponMotionRotation;

            weaponMotionRoot.localScale =
                originalWeaponMotionScale;
        }

        if (weaponRig != null)
        {
            weaponRig.localRotation =
                originalWeaponRigRotation;
        }

        if (weaponPose != null)
            weaponPose.enabled = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (hookCatchPoint == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            hookCatchPoint.position,
            hookHitRadius
        );
    }
}
