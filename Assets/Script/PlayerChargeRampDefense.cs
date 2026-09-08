using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerChargeRampDefense : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform targetEnemy;

    [SerializeField]
    private SpecialEnemyChargeAttack targetChargeAttack;

    [SerializeField] private Sprite squareSprite;

    [Header("Defense Trigger")]
    [Min(0f)]
    [SerializeField] private float triggerDistance = 6f;

    [Min(0f)]
    [SerializeField] private float maximumVerticalDistance = 4f;

    [Min(0f)]
    [SerializeField] private float defenseCooldown = 0.5f;

    [Header("Blue Ramp Visual")]
    [SerializeField]
    private Color rampColor =
        new Color(0.1f, 0.55f, 1f, 0.45f);

    [SerializeField]
    private Vector2 rampSize =
        new Vector2(5f, 0.6f);

    [Range(5f, 70f)]
    [SerializeField] private float rampAngle = 30f;

    [Min(0f)]
    [SerializeField] private float rampForwardDistance = 2.5f;

    [SerializeField] private float rampVerticalOffset = 0.8f;
    [SerializeField] private int rampSortingOrder = 25;

    [Header("Enemy Ramp Movement")]
    [Min(0.01f)]
    [SerializeField] private float approachRampDuration = 0.08f;

    [Min(0.01f)]
    [SerializeField] private float moveAlongRampDuration = 0.28f;

    [Min(0f)]
    [SerializeField] private float verticalRiseDistance = 10f;

    [Min(0.01f)]
    [SerializeField] private float verticalRiseDuration = 0.45f;

    [Min(0f)]
    [SerializeField] private float enemyHoverDuration = 0.35f;

    [Header("J Air Counter Window")]
    [Min(0f)]
    [SerializeField] private float airCounterMaximumDistance = 16f;

    [Min(0f)]
    [SerializeField]
    private float heightAbovePlayerToConfirmRise = 0.5f;

    [Min(0f)]
    [SerializeField] private float landingHeightTolerance = 0.15f;

    [Header("Silver Counter Visual")]
    [SerializeField]
    private Color silverCounterColor =
        new Color(0.82f, 0.9f, 1f, 0.92f);

    [Min(0.01f)]
    [SerializeField] private float silverCounterWidth = 0.65f;

    [Header("Silver Counter Scale Animation")]
    [SerializeField]
    private Vector2 silverCounterMaximumScale =
        new Vector2(1.7f, 1.7f);

    [Min(0f)]
    [SerializeField] private float silverCounterExtraLength = 1f;

    [Min(0.01f)]
    [SerializeField] private float silverCounterThrustDuration = 0.14f;

    [Min(0f)]
    [SerializeField] private float silverCounterHoldDuration = 0.06f;

    [Range(0f, 180f)]
    [SerializeField] private float silverCounterSwingAngle = 70f;

    [Min(0.01f)]
    [SerializeField] private float silverCounterSwingDuration = 0.22f;

    [SerializeField] private int silverCounterSortingOrder = 35;

    [Header("Silver Counter Damage")]
    [Min(1)]
    [SerializeField] private int silverCounterDamage = 3;

    private float nextDefenseTime;
    private GameObject activeRampObject;
    private SpecialEnemyChargeAttack subscribedChargeAttack;

    private bool movementLockedByDefense;
    private bool playerControllerWasEnabled;

    private bool airCounterWindowOpen;
    private bool enemyHasRisenAboveDefender;
    private bool airCounterUsedThisWindow;

    private Coroutine silverCounterCoroutine;
    private GameObject activeSilverCounterObject;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        FindEnemyReferences();
    }

    private void OnEnable()
    {
        FindEnemyReferences();
        EnsureChargeSubscription();
    }

    private void OnDisable()
    {
        if (subscribedChargeAttack != null)
        {
            subscribedChargeAttack.ReachedRampTop -=
                HandleEnemyReachedRampTop;
        }

        subscribedChargeAttack = null;

        if (silverCounterCoroutine != null)
        {
            StopCoroutine(silverCounterCoroutine);
            silverCounterCoroutine = null;
        }

        RemoveActiveRamp();
        CloseAirCounterWindow();
        RemoveActiveSilverCounter();
        RestoreDefenderMovement();
    }

    private void Update()
    {
        FindEnemyReferences();
        EnsureChargeSubscription();
        UpdateAirCounterWindow();

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame &&
            Time.time >= nextDefenseTime &&
            CanDefendCurrentCharge())
        {
            TryCreateRampDefense();
        }

        if (Keyboard.current.jKey.wasPressedThisFrame &&
            CanUseAirCounter())
        {
            StartSilverCounter();
        }
    }

    private void FindEnemyReferences()
    {
        if (targetChargeAttack == null)
        {
            targetChargeAttack =
                FindFirstObjectByType<SpecialEnemyChargeAttack>();
        }

        if (targetEnemy == null &&
            targetChargeAttack != null)
        {
            targetEnemy = targetChargeAttack.transform;
        }
    }

    private void EnsureChargeSubscription()
    {
        if (subscribedChargeAttack == targetChargeAttack)
            return;

        if (subscribedChargeAttack != null)
        {
            subscribedChargeAttack.ReachedRampTop -=
                HandleEnemyReachedRampTop;
        }

        subscribedChargeAttack = targetChargeAttack;

        if (subscribedChargeAttack != null)
        {
            subscribedChargeAttack.ReachedRampTop +=
                HandleEnemyReachedRampTop;
        }
    }

    private bool CanDefendCurrentCharge()
    {
        if (targetEnemy == null ||
            targetChargeAttack == null ||
            squareSprite == null)
        {
            return false;
        }

        if (!targetChargeAttack.IsChargingForward)
            return false;

        float horizontalDistance = Mathf.Abs(
            targetEnemy.position.x - transform.position.x
        );

        if (horizontalDistance > triggerDistance)
            return false;

        float verticalDistance = Mathf.Abs(
            targetEnemy.position.y - transform.position.y
        );

        if (verticalDistance > maximumVerticalDistance)
            return false;

        float directionFromEnemyToDefender = Mathf.Sign(
            transform.position.x - targetEnemy.position.x
        );

        if (Mathf.Approximately(
            directionFromEnemyToDefender,
            0f))
        {
            return false;
        }

        return directionFromEnemyToDefender *
            targetChargeAttack.CurrentChargeDirection > 0f;
    }

    private void TryCreateRampDefense()
    {
        float enemySide = Mathf.Sign(
            targetEnemy.position.x - transform.position.x
        );

        if (Mathf.Approximately(enemySide, 0f))
            enemySide = 1f;

        Vector2 rampCenter =
            (Vector2)transform.position +
            Vector2.right * enemySide * rampForwardDistance +
            Vector2.up * rampVerticalOffset;

        float radians = rampAngle * Mathf.Deg2Rad;

        Vector2 alongRamp = new Vector2(
            -enemySide * Mathf.Cos(radians),
            Mathf.Sin(radians)
        );

        Vector2 halfRamp =
            alongRamp.normalized * (rampSize.x * 0.5f);

        Vector2 rampBase = rampCenter - halfRamp;
        Vector2 rampTop = rampCenter + halfRamp;

        GameObject rampObject = CreateRampObject(
            rampCenter,
            enemySide
        );

        bool redirected =
            targetChargeAttack.TryRedirectChargeUpward(
                rampBase,
                rampTop,
                approachRampDuration,
                moveAlongRampDuration,
                verticalRiseDistance,
                verticalRiseDuration,
                enemyHoverDuration
            );

        if (!redirected)
        {
            Destroy(rampObject);
            return;
        }

        RemoveActiveRamp();
        activeRampObject = rampObject;

        LockDefenderMovement();

        nextDefenseTime =
            Time.time + defenseCooldown;

        Debug.Log(
            "PlayerChargeRampDefense：Space 斜坡防御成功。"
        );
    }

    private void HandleEnemyReachedRampTop()
    {
        RemoveActiveRamp();
        RestoreDefenderMovement();
        OpenAirCounterWindow();

        Debug.Log(
            "PlayerChargeRampDefense：" +
            "敌人到达斜坡顶，J 空中反击窗口开启。"
        );
    }

    private void OpenAirCounterWindow()
    {
        airCounterWindowOpen = true;
        enemyHasRisenAboveDefender = false;
        airCounterUsedThisWindow = false;
    }

    private void CloseAirCounterWindow()
    {
        airCounterWindowOpen = false;
        enemyHasRisenAboveDefender = false;
        airCounterUsedThisWindow = false;
    }

    private void UpdateAirCounterWindow()
    {
        if (!airCounterWindowOpen)
            return;

        if (targetEnemy == null)
        {
            CloseAirCounterWindow();
            return;
        }

        float heightAboveDefender =
            targetEnemy.position.y - transform.position.y;

        if (heightAboveDefender >=
            heightAbovePlayerToConfirmRise)
        {
            enemyHasRisenAboveDefender = true;
        }

        if (enemyHasRisenAboveDefender &&
            heightAboveDefender <= landingHeightTolerance)
        {
            CloseAirCounterWindow();

            Debug.Log(
                "PlayerChargeRampDefense：" +
                "敌人已下降到主角高度，" +
                "J 空中反击窗口关闭。"
            );
        }
    }

    private bool CanUseAirCounter()
    {
        if (!airCounterWindowOpen ||
            airCounterUsedThisWindow ||
            targetEnemy == null ||
            squareSprite == null)
        {
            return false;
        }

        float distance = Vector2.Distance(
            transform.position,
            targetEnemy.position
        );

        return distance <= airCounterMaximumDistance;
    }

    private void StartSilverCounter()
    {
        airCounterUsedThisWindow = true;

        if (silverCounterCoroutine != null)
            StopCoroutine(silverCounterCoroutine);

        RemoveActiveSilverCounter();

        silverCounterCoroutine =
            StartCoroutine(PlaySilverCounter());

        Debug.Log(
            "PlayerChargeRampDefense：" +
            "J 淡蓝长方形反击成功。"
        );
    }

    private IEnumerator PlaySilverCounter()
    {
        if (targetEnemy == null ||
            squareSprite == null)
        {
            silverCounterCoroutine = null;
            yield break;
        }

        GameObject root =
            new GameObject("SilverAirCounter");

        GameObject bladeObject =
            new GameObject("SilverAirCounterBlade");

        bladeObject.transform.SetParent(
            root.transform,
            false
        );

        SpriteRenderer bladeRenderer =
            bladeObject.AddComponent<SpriteRenderer>();

        bladeRenderer.sprite = squareSprite;
        bladeRenderer.color = silverCounterColor;
        bladeRenderer.sortingOrder =
            silverCounterSortingOrder;

        activeSilverCounterObject = root;

        float elapsed = 0f;
        float finalLength = 0.01f;
        float finalAngle = 0f;

        Vector3 maximumCounterScale = new Vector3(
            Mathf.Max(0.01f, silverCounterMaximumScale.x),
            Mathf.Max(0.01f, silverCounterMaximumScale.y),
            1f
        );

        root.transform.localScale = Vector3.one;

        while (elapsed < silverCounterThrustDuration)
        {
            if (targetEnemy == null)
                break;

            Vector2 origin = transform.position;
            Vector2 target = targetEnemy.position;
            Vector2 direction = target - origin;

            finalLength = Mathf.Max(
                0.01f,
                direction.magnitude + silverCounterExtraLength
            );

            finalAngle = Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

            float progress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(
                    elapsed / silverCounterThrustDuration
                )
            );

            root.transform.position = new Vector3(
                origin.x,
                origin.y,
                transform.position.z
            );

            root.transform.rotation = Quaternion.Euler(
                0f,
                0f,
                finalAngle
            );

            SetSilverBladeLength(
                bladeObject.transform,
                Mathf.Lerp(
                    0.01f,
                    finalLength,
                    progress
                )
            );

            root.transform.localScale = Vector3.Lerp(
                Vector3.one,
                maximumCounterScale,
                progress
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        SetSilverBladeLength(
            bladeObject.transform,
            finalLength
        );

        root.transform.localScale = maximumCounterScale;

        // 长方形完整划到敌人位置时，只造成一次伤害。
        DealSilverCounterDamage();

        if (silverCounterHoldDuration > 0f)
        {
            yield return new WaitForSeconds(
                silverCounterHoldDuration
            );
        }

        float swingDirection =
            targetEnemy != null &&
            targetEnemy.position.x <
            transform.position.x
                ? -1f
                : 1f;

        float swingEndAngle =
            finalAngle +
            swingDirection * silverCounterSwingAngle;

        elapsed = 0f;

        while (elapsed < silverCounterSwingDuration)
        {
            float progress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(
                    elapsed / silverCounterSwingDuration
                )
            );

            Vector2 origin = transform.position;

            root.transform.position = new Vector3(
                origin.x,
                origin.y,
                transform.position.z
            );

            float currentAngle = Mathf.LerpAngle(
                finalAngle,
                swingEndAngle,
                progress
            );

            root.transform.rotation = Quaternion.Euler(
                0f,
                0f,
                currentAngle
            );

            root.transform.localScale = Vector3.Lerp(
                maximumCounterScale,
                Vector3.one,
                progress
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        RemoveActiveSilverCounter();
        silverCounterCoroutine = null;
    }

    private void DealSilverCounterDamage()
    {
        if (targetEnemy == null)
            return;

        EnemyHealth enemyHealth =
            targetEnemy.GetComponent<EnemyHealth>();

        if (enemyHealth == null)
        {
            Debug.LogWarning(
                "PlayerChargeRampDefense：目标敌人没有 EnemyHealth，" +
                "淡蓝长方形未造成伤害。"
            );
            return;
        }

        enemyHealth.TakeDamage(silverCounterDamage);

        Debug.Log(
            "PlayerChargeRampDefense：淡蓝长方形命中，" +
            "造成 " + silverCounterDamage + " 点伤害。"
        );
    }

    private void SetSilverBladeLength(
        Transform bladeTransform,
        float length
    )
    {
        bladeTransform.localPosition = new Vector3(
            length * 0.5f,
            0f,
            0f
        );

        bladeTransform.localScale = new Vector3(
            length,
            silverCounterWidth,
            1f
        );
    }

    private void RemoveActiveSilverCounter()
    {
        if (activeSilverCounterObject == null)
            return;

        Destroy(activeSilverCounterObject);
        activeSilverCounterObject = null;
    }

    private void LockDefenderMovement()
    {
        if (playerController == null ||
            movementLockedByDefense)
        {
            return;
        }

        playerControllerWasEnabled =
            playerController.enabled;

        playerController.enabled = false;
        movementLockedByDefense = true;
    }

    private void RestoreDefenderMovement()
    {
        if (!movementLockedByDefense)
            return;

        if (playerController != null)
        {
            playerController.enabled =
                playerControllerWasEnabled;
        }

        movementLockedByDefense = false;
    }

    private void RemoveActiveRamp()
    {
        if (activeRampObject == null)
            return;

        Destroy(activeRampObject);
        activeRampObject = null;
    }

    private GameObject CreateRampObject(
        Vector2 rampCenter,
        float enemySide
    )
    {
        GameObject rampObject =
            new GameObject("ChargeDefenseRamp");

        rampObject.transform.position = new Vector3(
            rampCenter.x,
            rampCenter.y,
            transform.position.z
        );

        rampObject.transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                -enemySide * rampAngle
            );

        rampObject.transform.localScale = new Vector3(
            rampSize.x,
            rampSize.y,
            1f
        );

        SpriteRenderer rampRenderer =
            rampObject.AddComponent<SpriteRenderer>();

        rampRenderer.sprite = squareSprite;
        rampRenderer.color = rampColor;
        rampRenderer.sortingOrder = rampSortingOrder;

        return rampObject;
    }
}