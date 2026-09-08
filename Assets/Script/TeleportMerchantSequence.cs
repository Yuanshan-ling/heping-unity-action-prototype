using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TeleportMerchantSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform merchantApproachPoint;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private PlayerAirCombat playerAirCombat;
    [SerializeField] private StoryDialogueController storyDialogue;

    [Header("Camera")]
    [Range(0.1f, 0.99f)]
    [SerializeField] private float zoomMultiplier = 0.85f;

    [SerializeField] private float zoomDuration = 0.25f;
    [SerializeField] private float returnZoomDuration = 0.2f;

    [Header("Cinematic Border")]
    [Min(0f)]
    [SerializeField] private float borderThickness = 65f;

    [Header("Forced Walk")]
    [SerializeField] private float leftWalkSpeed = 8f;
    [SerializeField] private float arrivalTolerance = 0.2f;

    private Rigidbody2D playerBody;
    private PlayerController playerController;
    private bool airCombatWasEnabled;
    private bool isRunning;
    private bool hasPlayed;
    private GameObject cinematicBorder;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        CreateCinematicBorder();
    }

    private void OnDestroy()
    {
        if (cinematicBorder != null)
            Destroy(cinematicBorder);
    }

    public void BeginSequence(Rigidbody2D newPlayerBody)
    {
        if (hasPlayed || isRunning || newPlayerBody == null ||
                   merchantApproachPoint == null ||
            storyDialogue == null ||
            mainCamera == null)
        {
            return;
        }
        hasPlayed = true;
        playerBody = newPlayerBody;
        playerController =
            playerBody.GetComponent<PlayerController>();

        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        isRunning = true;

        if (playerController != null)
            playerController.enabled = false;

        if (playerAttack != null)
            playerAttack.SetAttackInputEnabled(false);

        if (playerAirCombat != null)
        {
            airCombatWasEnabled = playerAirCombat.enabled;
            playerAirCombat.enabled = false;
        }

        float normalCameraSize = mainCamera.orthographicSize;
        float zoomedCameraSize =
            normalCameraSize * zoomMultiplier;

        yield return StartCoroutine(
            ChangeCameraSize(
                normalCameraSize,
                zoomedCameraSize,
                zoomDuration
            )
        );

        if (cinematicBorder != null)
            cinematicBorder.SetActive(true);

        while (playerBody.position.x >
               merchantApproachPoint.position.x +
               arrivalTolerance)
        {
            float horizontalSpeed = 0f;

            if (Keyboard.current != null &&
                Keyboard.current.aKey.isPressed)
            {
                horizontalSpeed = -leftWalkSpeed;
            }

            playerBody.linearVelocity = new Vector2(
                horizontalSpeed,
                playerBody.linearVelocity.y
            );

            yield return null;
        }

        playerBody.linearVelocity = new Vector2(
            0f,
            playerBody.linearVelocity.y
        );

        if (cinematicBorder != null)
            cinematicBorder.SetActive(false);

        storyDialogue.StartDialogue();

        if (playerController != null)
            playerController.enabled = true;

        if (playerAttack != null)
            playerAttack.SetAttackInputEnabled(true);

        if (playerAirCombat != null)
            playerAirCombat.enabled = airCombatWasEnabled;

        yield return StartCoroutine(
            ChangeCameraSize(
                zoomedCameraSize,
                normalCameraSize,
                returnZoomDuration
            )
        );

        isRunning = false;
    }

    private IEnumerator ChangeCameraSize(
        float startSize,
        float endSize,
        float duration
    )
    {
        if (duration <= 0f)
        {
            mainCamera.orthographicSize = endSize;
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            mainCamera.orthographicSize = Mathf.Lerp(
                startSize,
                endSize,
                time / duration
            );

            yield return null;
        }

        mainCamera.orthographicSize = endSize;
    }

    private void CreateCinematicBorder()
    {
        cinematicBorder = new GameObject(
            "TeleportCinematicBorder",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
        );

        Canvas canvas =
            cinematicBorder.GetComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler =
            cinematicBorder.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution = new Vector2(1920f, 1080f);

        CreateBorderPiece(
            "Top",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, -borderThickness),
            Vector2.zero
        );

        CreateBorderPiece(
            "Bottom",
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            Vector2.zero,
            new Vector2(0f, borderThickness)
        );

        CreateBorderPiece(
            "Left",
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            Vector2.zero,
            new Vector2(borderThickness, 0f)
        );

        CreateBorderPiece(
            "Right",
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(-borderThickness, 0f),
            Vector2.zero
        );

        cinematicBorder.SetActive(false);
    }

    private void CreateBorderPiece(
        string pieceName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax
    )
    {
        GameObject piece = new GameObject(
            pieceName,
            typeof(RectTransform),
            typeof(Image)
        );

        piece.transform.SetParent(
            cinematicBorder.transform,
            false
        );

        RectTransform rect =
            piece.GetComponent<RectTransform>();

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Image image = piece.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;
    }
}