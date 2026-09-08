using System.Collections;
using UnityEngine;

[RequireComponent(typeof(TextMesh))]
public class CoinRewardPopup : MonoBehaviour
{
    private TextMesh textMesh;
    private MeshRenderer meshRenderer;

    public void Setup(
        int reward,
        float riseDistance,
        float duration,
        float characterSize
    )
    {
        textMesh = GetComponent<TextMesh>();
        meshRenderer = GetComponent<MeshRenderer>();

        textMesh.font = Resources.GetBuiltinResource<Font>(
            "LegacyRuntime.ttf"
        );
        textMesh.text = "+" + reward;
        textMesh.fontSize = 64;
        textMesh.characterSize = characterSize;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = new Color(1f, 0.78f, 0f, 1f);

        meshRenderer.sortingLayerName = "Default";
        meshRenderer.sortingOrder = 100;

        StartCoroutine(
            RiseAndFade(riseDistance, duration)
        );
    }

    private IEnumerator RiseAndFade(
        float riseDistance,
        float duration
    )
    {
        Vector3 startPosition = transform.position;
        Color startColor = textMesh.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsed / duration
            );

            transform.position = startPosition +
                Vector3.up * riseDistance * progress;

            Color color = startColor;
            color.a = Mathf.Lerp(1f, 0f, progress);
            textMesh.color = color;

            yield return null;
        }

        Destroy(gameObject);
    }
}