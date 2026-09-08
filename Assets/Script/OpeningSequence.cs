using System.Collections;
using UnityEngine;

public class OpeningSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject player;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private Camera mainCamera;

    [Header("Menu")]
    [SerializeField] private GameObject menuRoot;

    [Header("Black Screen")]
    [SerializeField] private RectTransform blackTop;
    [SerializeField] private RectTransform blackBottom;
    [SerializeField] private OpeningQTE openingQTE;

    [Header("Opening Animation")]
    [SerializeField] private float openingDuration = 1.5f;
    [SerializeField] private float closeCameraSize = 2.5f;
    [SerializeField] private float blackMoveDistance = 1200f;

    private float normalCameraSize;
    private bool isStarting;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        normalCameraSize = mainCamera.orthographicSize;

        if (openingQTE == null)
            openingQTE = FindFirstObjectByType<OpeningQTE>();

        if (cameraFollow != null)
            cameraFollow.enabled = false;

        if (playerController != null)
            playerController.enabled = false;

        if (player != null)
            player.SetActive(false);

        menuRoot.SetActive(true);

        blackTop.gameObject.SetActive(false);
        blackBottom.gameObject.SetActive(false);
    }

    public void StartGame()
    {
        if (isStarting)
            return;

        StartCoroutine(PlayOpening());
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator PlayOpening()
    {
        isStarting = true;
        menuRoot.SetActive(false);

        player.SetActive(true);

        Vector3 playerPosition = player.transform.position;
        playerPosition.z = mainCamera.transform.position.z;
        mainCamera.transform.position = playerPosition;
        mainCamera.orthographicSize = closeCameraSize;

        blackTop.anchoredPosition = Vector2.zero;
        blackBottom.anchoredPosition = Vector2.zero;

        blackTop.gameObject.SetActive(true);
        blackBottom.gameObject.SetActive(true);

        float time = 0f;

        while (time < openingDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / openingDuration);
            t = t * t * (3f - 2f * t);

            blackTop.anchoredPosition =
                Vector2.up * blackMoveDistance * t;

            blackBottom.anchoredPosition =
                Vector2.down * blackMoveDistance * t;

            mainCamera.orthographicSize = Mathf.Lerp(
                closeCameraSize,
                normalCameraSize,
                t
            );

            yield return null;
        }

        mainCamera.orthographicSize = normalCameraSize;

        blackTop.gameObject.SetActive(false);
        blackBottom.gameObject.SetActive(false);

        if (cameraFollow != null)
            cameraFollow.enabled = true;

        if (openingQTE != null)
            openingQTE.Play();
        else if (playerController != null)
            playerController.enabled = true;
    }
}