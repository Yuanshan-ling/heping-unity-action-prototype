using UnityEngine;

public class TriangleFadeOnPlayer : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float alphaMultiplier = 0.5f;

    private SpriteRenderer[] renderers;
    private Color[] originalColors;
    private int playerColliderCount;

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].color;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        playerColliderCount++;
        SetTransparency(alphaMultiplier);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        playerColliderCount = Mathf.Max(0, playerColliderCount - 1);

        if (playerColliderCount == 0)
            RestoreTransparency();
    }

    private void SetTransparency(float multiplier)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Color color = originalColors[i];
            color.a *= multiplier;
            renderers[i].color = color;
        }
    }

    private void RestoreTransparency()
    {
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].color = originalColors[i];
    }
}