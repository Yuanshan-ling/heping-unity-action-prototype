using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private Canvas canvas;
    private RectTransform fillRect;
    private Image fillImage;
    private Text healthText;
    private Font uiFont;

    private void Awake()
    {
        uiFont = Resources.GetBuiltinResource<Font>(
            "LegacyRuntime.ttf"
        );

    }

    private void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogError("Î´ÕÒµ½ PlayerHealth¡£");
            return;
        }

        CreateUI();

        playerHealth.Changed += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.Changed -= Refresh;

        if (canvas != null)
            Destroy(canvas.gameObject);
    }

    private void Refresh()
    {
        float ratio = (float)playerHealth.CurrentHealth /
                      playerHealth.MaxHealth;

        fillRect.anchorMax = new Vector2(ratio, 1f);

        fillImage.color = Color.Lerp(
            new Color(0.9f, 0.1f, 0.1f),
            new Color(0.1f, 0.8f, 0.2f),
            ratio
        );

        healthText.text =
            "ÉúÃü£º " +
            playerHealth.CurrentHealth +
            " / " +
            playerHealth.MaxHealth;
    }

    private void CreateUI()
    {
        GameObject canvasObject = new GameObject(
            "PlayerHealthCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
        );

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 31;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject backgroundObject = new GameObject(
            "HealthBarBackground",
            typeof(RectTransform),
            typeof(Image)
        );

        backgroundObject.transform.SetParent(
            canvasObject.transform,
            false
        );

        RectTransform backgroundRect =
            backgroundObject.GetComponent<RectTransform>();

        backgroundRect.anchorMin = new Vector2(0f, 1f);
        backgroundRect.anchorMax = new Vector2(0f, 1f);
        backgroundRect.pivot = new Vector2(0f, 1f);
        backgroundRect.anchoredPosition = new Vector2(30f, -30f);
        backgroundRect.sizeDelta = new Vector2(300f, 42f);

        Image backgroundImage =
            backgroundObject.GetComponent<Image>();

        backgroundImage.color = new Color(0f, 0f, 0f, 0.75f);

        GameObject fillObject = new GameObject(
            "HealthBarFill",
            typeof(RectTransform),
            typeof(Image)
        );

        fillObject.transform.SetParent(
            backgroundObject.transform,
            false
        );

        fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);

        fillImage = fillObject.GetComponent<Image>();

        GameObject textObject = new GameObject(
            "HealthText",
            typeof(RectTransform),
            typeof(Text)
        );

        textObject.transform.SetParent(
            backgroundObject.transform,
            false
        );

        RectTransform textRect =
            textObject.GetComponent<RectTransform>();

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        healthText = textObject.GetComponent<Text>();
        healthText.font = uiFont;
        healthText.fontSize = 22;
        healthText.alignment = TextAnchor.MiddleCenter;
        healthText.color = Color.white;
        healthText.raycastTarget = false;

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1f, -1f);
    }
}