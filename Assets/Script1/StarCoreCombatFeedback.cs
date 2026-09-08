using System.Collections;
using UnityEngine;

public class StarCoreCombatFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Sprite squareSprite;

    [Header("Impact")]
    [SerializeField] private int impactSortingOrder = 80;
    [SerializeField] private float impactLifetime = 0.16f;

    private Coroutine hitStopCoroutine;
    private Coroutine shakeCoroutine;
    private bool hitStopActive;
    private bool shakeActive;
    private float timeScaleBeforeHitStop;
    private float fixedDeltaBeforeHitStop;
    private Vector3 currentShakeOffset;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    public void PlayImpact(
        Vector2 position,
        Color color,
        float size,
        float hitStopDuration,
        float shakeStrength
    )
    {
        if (squareSprite != null)
            StartCoroutine(ImpactRoutine(position, color, size));

        if (hitStopDuration > 0f && !hitStopActive)
        {
            hitStopCoroutine =
                StartCoroutine(HitStopRoutine(hitStopDuration));
        }

        if (shakeStrength > 0f &&
            targetCamera != null &&
            !shakeActive)
        {
            shakeCoroutine = StartCoroutine(
                ShakeRoutine(0.12f, shakeStrength)
            );
        }
    }

    private void OnDisable()
    {
        if (hitStopActive)
        {
            Time.timeScale = timeScaleBeforeHitStop;
            Time.fixedDeltaTime = fixedDeltaBeforeHitStop;
        }

        if (targetCamera != null && currentShakeOffset != Vector3.zero)
        {
            targetCamera.transform.position -= currentShakeOffset;
            currentShakeOffset = Vector3.zero;
        }

        hitStopActive = false;
        shakeActive = false;
    }

    private IEnumerator ImpactRoutine(
        Vector2 position,
        Color color,
        float size
    )
    {
        GameObject root = new GameObject("StarCoreImpact");
        root.transform.position = position;

        for (int index = 0; index < 4; index++)
        {
            GameObject ray = new GameObject("ImpactRay");
            ray.transform.SetParent(root.transform, false);
            ray.transform.localRotation = Quaternion.Euler(
                0f,
                0f,
                index * 45f
            );

            SpriteRenderer renderer =
                ray.AddComponent<SpriteRenderer>();

            renderer.sprite = squareSprite;
            renderer.color = color;
            renderer.sortingOrder = impactSortingOrder;

            ray.transform.localScale = new Vector3(
                size,
                Mathf.Max(0.05f, size * 0.12f),
                1f
            );
        }

        float elapsed = 0f;

        while (elapsed < impactLifetime)
        {
            float progress = Mathf.Clamp01(elapsed / impactLifetime);
            root.transform.localScale =
                Vector3.one * Mathf.Lerp(0.35f, 1.4f, progress);

            SpriteRenderer[] renderers =
                root.GetComponentsInChildren<SpriteRenderer>();

            foreach (SpriteRenderer renderer in renderers)
            {
                Color current = renderer.color;
                current.a = 1f - progress;
                renderer.color = current;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Destroy(root);
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        hitStopActive = true;
        timeScaleBeforeHitStop = Time.timeScale;
        fixedDeltaBeforeHitStop = Time.fixedDeltaTime;

        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = timeScaleBeforeHitStop;
        Time.fixedDeltaTime = fixedDeltaBeforeHitStop;
        hitStopActive = false;
        hitStopCoroutine = null;
    }

    private IEnumerator ShakeRoutine(
        float duration,
        float strength
    )
    {
        shakeActive = true;
        Transform cameraTransform = targetCamera.transform;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            cameraTransform.position -= currentShakeOffset;

            currentShakeOffset = new Vector3(
                Random.Range(-strength, strength),
                Random.Range(-strength, strength),
                0f
            );

            cameraTransform.position += currentShakeOffset;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        cameraTransform.position -= currentShakeOffset;
        currentShakeOffset = Vector3.zero;
        shakeActive = false;
        shakeCoroutine = null;
    }
}
