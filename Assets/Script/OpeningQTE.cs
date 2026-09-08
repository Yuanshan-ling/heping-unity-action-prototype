using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class OpeningQTE : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject player;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private QTEAwakeningEffect qteAwakeningEffect;
    [SerializeField] private GameObject openingEnemy;
    [SerializeField] private Transform patrolLeft;
    [SerializeField] private Transform patrolRight;
    [SerializeField] private LayerMask groundLayer;

    [Header("Combat Camera")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField]
    private Vector2 combatCameraCenterOffset =
    new Vector2(1f, 0f);

    [SerializeField] private float combatCameraSize = 3.5f;
    [SerializeField] private float combatCameraMoveDuration = 0.15f;
    [SerializeField] private float cameraShakeDuration = 0.4f;
    [SerializeField] private float cameraShakeStrength = 0.15f;

    [Header("Jump")]
    [SerializeField] private float startExtraDistance = 3f;
    [SerializeField] private float nearPlayerDistance = 0.1f;
    [SerializeField] private float jumpHeight = 22f;
    [SerializeField] private float jumpDuration = 1.2f;

    [Header("Slow Motion QTE")]
    [Min(0.1f)]
    [SerializeField] private float slowMotionStartDistance = 20f;

    [Min(0.1f)]
    [SerializeField] private float slowMotionTriggerDistance = 12f;

    [Range(0.05f, 1f)]
    [SerializeField] private float slowMotionScale = 0.2f;

    [SerializeField] private float qteTimeLimit = 1.5f;

    [Header("Result")]
    [SerializeField] private float enemyFlyDistance = 18f;
    [SerializeField] private float enemyFailureRetreatDistance = 12f;
    [SerializeField] private float enemyFailureJumpHeight = 6f;
    [SerializeField] private float enemyFailureRetreatDuration = 0.45f;
    [SerializeField] private float playerKnockbackDistance = 6f;

    [Min(0.05f)]
    [SerializeField] private float playerKnockbackDuration = 0.15f;

    [SerializeField] private float resultDuration = 0.35f;

    [Header("Weapon Reset")]
    [SerializeField] private Transform weaponMotionRoot;

    private Vector3 originalWeaponLocalPosition;
    private Quaternion originalWeaponLocalRotation;
    private Vector3 originalWeaponLocalScale;

    private Canvas qteCanvas;
    private Text promptText;
    private Font uiFont;

    private SpriteRenderer enemyRenderer;
    private SpriteRenderer playerRenderer;
    private Color enemyOriginalColor;
    private Color playerOriginalColor;

    private Rigidbody2D enemyBody;
    private EnemyAI enemyAI;
    private EnemyHealth enemyHealth;

    private bool isPlaying;

    private void Awake()
    {
        uiFont = Resources.GetBuiltinResource<Font>(
            "LegacyRuntime.ttf"
        );

        enemyRenderer = openingEnemy.GetComponent<SpriteRenderer>();
        playerRenderer = player.GetComponent<SpriteRenderer>();

        enemyBody = openingEnemy.GetComponent<Rigidbody2D>();
        enemyAI = openingEnemy.GetComponent<EnemyAI>();
        enemyHealth = openingEnemy.GetComponent<EnemyHealth>();

        enemyOriginalColor = enemyRenderer.color;
        playerOriginalColor = playerRenderer.color;

        CreatePromptUI();
        promptText.gameObject.SetActive(false);

        if (weaponMotionRoot != null)
        {
            originalWeaponLocalPosition =
                weaponMotionRoot.localPosition;

            originalWeaponLocalRotation =
                weaponMotionRoot.localRotation;

            originalWeaponLocalScale =
                weaponMotionRoot.localScale;
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;

        if (qteCanvas != null)
            Destroy(qteCanvas.gameObject);
    }


    public void Play()
    {
        if (!isPlaying)
            StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        isPlaying = true;

        if (playerController != null)
            playerController.enabled = false;

        // 特殊敌人从屏幕右侧向左跳，因此起跳前固定朝左。
        // 开场使用Transform播放跳跃，暂时关闭物理位移。
        if (enemyBody != null)
        {
            enemyBody.simulated = false;

            // 供敌人、红点和武器的朝向脚本判断为向左。
            enemyBody.linearVelocity = Vector2.left;
        }

        if (enemyRenderer != null)
            enemyRenderer.flipX = true;

        openingEnemy.SetActive(true);
        float screenHalfWidth =
            Camera.main.orthographicSize *
            Camera.main.aspect;

        Vector3 startPosition = player.transform.position +
            Vector3.right *
            (screenHalfWidth + startExtraDistance);

        Vector3 landingPosition = player.transform.position +
            Vector3.right * nearPlayerDistance;

        openingEnemy.transform.position = startPosition;

        yield return PlayApproachJump(
            startPosition,
            landingPosition
        );
    }

    private IEnumerator PlayApproachJump(
        Vector3 startPosition,
        Vector3 landingPosition
    )
    {
        float jumpTime = 0f;
        float qteRealTime = 0f;

        bool slowMotionStarted = false;
        bool qteStarted = false;
        bool qteTimedOut = false;

        while (jumpTime < jumpDuration)
        {
            jumpTime += Time.deltaTime;

            // 这段开场跳跃使用Transform移动，没有Rigidbody速度，
            // 因此动画期间明确保持朝左。
            // 每帧覆盖可能残留的向右速度。
            if (enemyBody != null)
                enemyBody.linearVelocity = Vector2.left;

            if (enemyRenderer != null)
                enemyRenderer.flipX = true;

            float t = Mathf.Clamp01(jumpTime / jumpDuration);
            float arc = Mathf.Sin(t * Mathf.PI) * jumpHeight;

            openingEnemy.transform.position =
                Vector3.Lerp(
                    startPosition,
                    landingPosition,
                    t
                ) + Vector3.up * arc;

            float distanceToPlayer = Mathf.Abs(
                openingEnemy.transform.position.x -
                player.transform.position.x
            );

            if (!slowMotionStarted &&
     distanceToPlayer <= slowMotionStartDistance)
            {
                slowMotionStarted = true;
                Time.timeScale = slowMotionScale;
            }

            if (!qteStarted &&
                distanceToPlayer <= slowMotionTriggerDistance)
            {
                qteStarted = true;
                promptText.gameObject.SetActive(true);
            }

            if (qteStarted && !qteTimedOut)
            {
                qteRealTime += Time.unscaledDeltaTime;

                float pulse = 1f +
                    Mathf.Sin(qteRealTime * 8f) * 0.18f;

                promptText.rectTransform.localScale =
                    Vector3.one * pulse;

                if (Keyboard.current != null &&
                    Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    promptText.gameObject.SetActive(false);

                    float direction = GetEnemyDirection();

                    StartCoroutine(PlayCombatCamera());

                    if (playerAttack != null)
                    {
                        EnemyHealth qteEnemyHealth =
                            openingEnemy.GetComponent<EnemyHealth>();


                        yield return StartCoroutine(
                            playerAttack.PlayQTEAttack(qteEnemyHealth)
                        );
                    }

                    yield return PlayerSuccess(direction);

                    if (qteAwakeningEffect != null)
                    {
                        yield return StartCoroutine(
                            qteAwakeningEffect.Play(
                                openingEnemy.transform
                            )
                        );
                    }

                    ActivateSpecialEnemy();
                    EndSequence(false);
                    yield break;
                }

                if (qteRealTime >= qteTimeLimit)
                {
                    qteTimedOut = true;
                    promptText.gameObject.SetActive(false);
                }
            }

            yield return null;
        }

        openingEnemy.transform.position = landingPosition;

        if (qteStarted)
        {
            float direction = GetEnemyDirection();

            StartCoroutine(PlayCombatCamera());

            // 敌人命中主角，主角先被击退。
            // 记录敌人起跳位置。
            Vector3 enemyStart = openingEnemy.transform.position;
            Vector3 enemyEnd = enemyStart +
                Vector3.right * direction *
                enemyFailureRetreatDistance;

            // 主角击退与敌人后跳同时开始。
            Coroutine playerKnockback = StartCoroutine(
                PlayerFailed(direction)
            );

            Coroutine enemyRetreat = StartCoroutine(
                EnemyRetreatJump(enemyStart, enemyEnd)
            );

            // 等两段动作都完成，再继续后续流程。
            yield return playerKnockback;
            yield return enemyRetreat;

            FacePlayer();

            ActivateSpecialEnemy();
            EndSequence(false);
            yield break;
        }

        EndSequence(true);
    }

    private float GetEnemyDirection()
    {
        float direction = Mathf.Sign(
            openingEnemy.transform.position.x -
            player.transform.position.x
        );

        return direction == 0f ? 1f : direction;
    }

    private IEnumerator PlayerSuccess(float direction)
    {
        StartCoroutine(FlashEnemy());

        Vector3 start = openingEnemy.transform.position;
        Vector3 end = start +
            Vector3.right * direction *
            enemyFlyDistance;

        yield return MoveOverTime(
            openingEnemy.transform,
            start,
            end,
            resultDuration
        );
    }

    private IEnumerator PlayerFailed(float direction)
    {
        playerRenderer.color = Color.red;

        Vector3 start = player.transform.position;
        Vector3 end = start -
            Vector3.right * direction *
            playerKnockbackDistance;

        yield return MoveOverTime(
            player.transform,
            start,
            end,
            playerKnockbackDuration
        );

        playerRenderer.color = playerOriginalColor;
    }

    private IEnumerator MoveOverTime(
        Transform target,
        Vector3 start,
        Vector3 end,
        float duration
    )
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);
            target.position = Vector3.Lerp(start, end, t);

            yield return null;
        }

        target.position = end;
    }

    private IEnumerator FlashEnemy()
    {
        enemyRenderer.color = Color.red;

        yield return new WaitForSeconds(0.12f);

        enemyRenderer.color = enemyOriginalColor;
    }

    private IEnumerator EnemyRetreatJump(
    Vector3 start,
    Vector3 end
)
    {
        float time = 0f;

        while (time < enemyFailureRetreatDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(
                time / enemyFailureRetreatDuration
            );

            float arc = Mathf.Sin(t * Mathf.PI) *
                enemyFailureJumpHeight;

            openingEnemy.transform.position =
                Vector3.Lerp(start, end, t) +
                Vector3.up * arc;

            FacePlayer();

            yield return null;
        }

        openingEnemy.transform.position = end;
        FacePlayer();
    }

    private void FacePlayer()
    {
        float directionToPlayer = Mathf.Sign(
            player.transform.position.x -
            openingEnemy.transform.position.x
        );

        if (Mathf.Approximately(directionToPlayer, 0f))
            return;

        // 只翻转敌人本体的 Sprite，不翻转整个父物体。
        if (enemyRenderer != null)
            enemyRenderer.flipX = directionToPlayer < 0f;

        // 武器是 OpeningEnemy 的子物体。
        // 必须保证父物体 X 缩放始终为正，避免武器被二次镜像。
        Vector3 scale = openingEnemy.transform.localScale;
        scale.x = Mathf.Abs(scale.x);
        openingEnemy.transform.localScale = scale;
    }   
    private void ActivateSpecialEnemy()
    {
        ResetSpecialEnemyWeapon();
        Debug.LogWarning(
            "OpeningQTE：正在激活特殊敌人的战斗功能。"
        );

        if (enemyBody != null)
        {
            enemyBody.simulated = true;
            enemyBody.linearVelocity = Vector2.zero;
        }

        if (enemyHealth != null)
            enemyHealth.enabled = true;

        if (enemyAI != null)
        {
            enemyAI.Configure(

                player.transform,
                patrolLeft,
                patrolRight,
                groundLayer
            );

            enemyAI.enabled = true;
        }

        SpecialEnemyPounce pounce =
            openingEnemy.GetComponent<SpecialEnemyPounce>();

        if (pounce != null)
        {
            pounce.EnablePounce();

            Debug.LogWarning(
                "OpeningQTE：特殊敌人扑击已启用。"
            );
        }
        else
        {
            Debug.LogError(
                "OpeningEnemy 上没有找到 SpecialEnemyPounce 组件！"
            );
        }

        SpecialEnemyChargeAttack chargeAttack =
            openingEnemy.GetComponent<SpecialEnemyChargeAttack>();

        if (chargeAttack != null)
        {
            chargeAttack.EnableChargeAttack();

            Debug.LogWarning(
                "OpeningQTE：特殊敌人冲锋已启用。"
            );
        }
        else
        {
            Debug.LogError(
                "OpeningEnemy 上没有找到 " +
                "SpecialEnemyChargeAttack 组件！"
            );
        }

        SpecialEnemyThrowAttack throwAttack =
            openingEnemy.GetComponent<SpecialEnemyThrowAttack>();

        if (throwAttack != null)
        {
            throwAttack.EnableThrowAttack();

            Debug.LogWarning(
                "OpeningQTE：特殊敌人投技已启用。"
            );
        }
        else
        {
            Debug.LogError(
                "OpeningEnemy 上没有找到 " +
                "SpecialEnemyThrowAttack 组件！"
            );
        }

        SpecialEnemyAttackDirector attackDirector =
            openingEnemy.GetComponent<SpecialEnemyAttackDirector>();

        if (attackDirector != null)
        {
            attackDirector.EnableDirector();
        }
        else
        {
            Debug.LogError(
                "OpeningEnemy 上没有找到 SpecialEnemyAttackDirector 组件。"
            );
        }

        PlayerAirCombat chargeDodgeAirCombat =
            player != null
                ? player.GetComponent<PlayerAirCombat>()
                : null;

        if (chargeDodgeAirCombat != null)
        {
            chargeDodgeAirCombat.UnlockFlight();

            Debug.LogWarning(
                "OpeningQTE：主角冲锋闪避已启用。"
            );
        }
        else
        {
            Debug.LogError(
                "Player 上没有找到 PlayerAirCombat 组件！"
            );
        }
    }

    private IEnumerator PlayCombatCamera()
    {
        if (mainCamera == null)
            yield break;

        bool wasFollowing = cameraFollow != null &&
            cameraFollow.enabled;

        if (cameraFollow != null)
            cameraFollow.enabled = false;

        float normalSize = mainCamera.orthographicSize;

        Vector3 startPosition =
            mainCamera.transform.position;

        float cameraZ = startPosition.z;
        float moveTime = 0f;

        while (moveTime < combatCameraMoveDuration)
        {
            moveTime += Time.deltaTime;

            float t = Mathf.Clamp01(
                moveTime / combatCameraMoveDuration
            );

            Vector3 focusPosition =
                GetCombatFocusPosition(cameraZ);

            mainCamera.transform.position =
                Vector3.Lerp(
                    startPosition,
                    focusPosition,
                    t
                );

            mainCamera.orthographicSize = Mathf.Lerp(
                normalSize,
                combatCameraSize,
                t
            );

            yield return null;
        }

        float shakeTime = 0f;

        while (shakeTime < cameraShakeDuration)
        {
            shakeTime += Time.deltaTime;

            Vector3 focusPosition =
                GetCombatFocusPosition(cameraZ);

            Vector2 offset =
                Random.insideUnitCircle *
                cameraShakeStrength;

            mainCamera.transform.position =
                focusPosition +
                new Vector3(offset.x, offset.y, 0f);

            yield return null;
        }

        mainCamera.transform.position =
            GetCombatFocusPosition(cameraZ);

        mainCamera.orthographicSize = normalSize;

        if (cameraFollow != null)
            cameraFollow.enabled = wasFollowing;
    }

    private Vector3 GetCombatFocusPosition(float cameraZ)
    {
        Vector3 focusPosition =
            (player.transform.position +
            openingEnemy.transform.position) * 0.5f;

        focusPosition += new Vector3(
            combatCameraCenterOffset.x,
            combatCameraCenterOffset.y,
            0f
        );

        focusPosition.z = cameraZ;

        return focusPosition;
    }

    private void EndSequence(bool hideEnemy)
    {
        Time.timeScale = 1f;
        promptText.gameObject.SetActive(false);

        if (hideEnemy)
            openingEnemy.SetActive(false);

        if (playerController != null)
            playerController.enabled = true;

        isPlaying = false;
    }

    private void CreatePromptUI()
    {
        GameObject canvasObject = new GameObject(
            "OpeningQTECanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
        );

        qteCanvas = canvasObject.GetComponent<Canvas>();
        qteCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        qteCanvas.sortingOrder = 200;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1920f, 1080f);

        GameObject textObject = new GameObject(
            "SpacePrompt",
            typeof(RectTransform),
            typeof(Text)
        );

        textObject.transform.SetParent(
            canvasObject.transform,
            false
        );

        promptText = textObject.GetComponent<Text>();
        promptText.font = uiFont;
        promptText.text = "[ SPACE ]";
        promptText.fontSize = 56;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = Color.white;
        promptText.raycastTarget = false;

        RectTransform rect = promptText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(420f, 120f);

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3f, -3f);
    }

    private void ResetSpecialEnemyWeapon()
    {
        if (weaponMotionRoot == null)
            return;

        weaponMotionRoot.localPosition =
            originalWeaponLocalPosition;

        weaponMotionRoot.localRotation =
            originalWeaponLocalRotation;

        weaponMotionRoot.localScale =
            originalWeaponLocalScale;
    }
}
