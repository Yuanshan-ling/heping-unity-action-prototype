using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSmallForm : MonoBehaviour
{
    [Header("Small Form")]
    [SerializeField] private Sprite smallFormSprite;

    [SerializeField]
    private Vector3 smallFormScale =
        new Vector3(4f, 4f, 1f);

    [SerializeField] private GameObject playerArmor;

    [Header("Camera Zoom")]
    [SerializeField] private Camera mainCamera;

    [SerializeField] private float smallFormCameraSize = 3.5f;

    [Min(0f)]
    [SerializeField] private float cameraZoomDuration = 0.35f;

    private SpriteRenderer spriteRenderer;
    private bool isSmallForm;
    private Coroutine zoomCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.pKey.wasPressedThisFrame &&
            !isSmallForm)
        {
            BecomeSmallForm();
        }
    }

    private void BecomeSmallForm()
    {
        if (spriteRenderer == null ||
            smallFormSprite == null)
        {
            Debug.LogError(
                "PlayerSmallForm ÉÐÎ´Ö¸¶¨·½¿é Sprite¡£"
            );
            return;
        }

        spriteRenderer.sprite = smallFormSprite;
        transform.localScale = smallFormScale;

        if (playerArmor != null)
            playerArmor.SetActive(false);

        if (mainCamera != null)
        {
            if (zoomCoroutine != null)
                StopCoroutine(zoomCoroutine);

            zoomCoroutine = StartCoroutine(ZoomCamera());
        }

        isSmallForm = true;
    }

    private IEnumerator ZoomCamera()
    {
        float startSize = mainCamera.orthographicSize;

        if (cameraZoomDuration <= 0f)
        {
            mainCamera.orthographicSize = smallFormCameraSize;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < cameraZoomDuration)
        {
            elapsed += Time.deltaTime;

            mainCamera.orthographicSize = Mathf.Lerp(
                startSize,
                smallFormCameraSize,
                elapsed / cameraZoomDuration
            );

            yield return null;
        }

        mainCamera.orthographicSize = smallFormCameraSize;
        zoomCoroutine = null;
    }
}