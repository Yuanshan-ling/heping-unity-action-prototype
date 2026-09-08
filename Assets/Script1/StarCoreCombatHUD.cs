using UnityEngine;
using UnityEngine.UI;

public sealed class StarCoreCombatHUD : MonoBehaviour
{
    private sealed class BarView
    {
        public RectTransform Fill;
        public Text Label;
    }

    private static StarCoreCombatHUD instance;

    private StarCorePlayerCombat playerCombat;
    private StarCoreExecutionerBoss boss;
    private Canvas canvas;
    private BarView bossHealthBar;
    private BarView starCoreBar;
    private Font uiFont;
    private float nextReferenceSearchTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateAutomatically()
    {
        if (FindFirstObjectByType<StarCoreCombatHUD>() != null)
            return;

        GameObject hudObject = new GameObject("StarCoreCombatHUD");
        hudObject.AddComponent<StarCoreCombatHUD>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUI();
        FindCombatReferences();
        RefreshUI();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextReferenceSearchTime)
        {
            if (playerCombat == null || boss == null)
                FindCombatReferences();

            nextReferenceSearchTime = Time.unscaledTime + 0.5f;
        }

        RefreshUI();
    }

    private void FindCombatReferences()
    {
        if (playerCombat == null)
            playerCombat = FindFirstObjectByType<StarCorePlayerCombat>();

        if (boss == null)
            boss = FindFirstObjectByType<StarCoreExecutionerBoss>();
    }

    private void RefreshUI()
    {
        bool combatVisible =
            playerCombat != null &&
            boss != null &&
            playerCombat.gameObject.activeInHierarchy &&
            boss.gameObject.activeInHierarchy;

        if (canvas.gameObject.activeSelf != combatVisible)
            canvas.gameObject.SetActive(combatVisible);

        if (!combatVisible)
            return;

        float healthRatio = boss.MaximumHealth > 0
            ? (float)boss.CurrentHealth / boss.MaximumHealth
            : 0f;

        SetBarRatio(bossHealthBar, healthRatio);
        bossHealthBar.Label.text =
            "STAR CORE EXECUTIONER    " +
            Mathf.Max(0, boss.CurrentHealth) +
            " / " +
            boss.MaximumHealth +
            "    " +
            boss.CurrentPhase;

        float starRatio = playerCombat.MaximumStarEnergy > 0
            ? (float)playerCombat.StarEnergy /
              playerCombat.MaximumStarEnergy
            : 0f;

        SetBarRatio(starCoreBar, starRatio);

        bool burstReady =
            playerCombat.StarEnergy >=
            playerCombat.MaximumStarEnergy;

        starCoreBar.Label.text =
            "STAR CORE    " +
            playerCombat.StarEnergy +
            " / " +
            playerCombat.MaximumStarEnergy +
            (burstReady ? "    K READY" : string.Empty);

        starCoreBar.Label.color = burstReady
            ? new Color(1f, 0.92f, 0.35f, 1f)
            : Color.white;
    }

    private static void SetBarRatio(BarView bar, float ratio)
    {
        ratio = Mathf.Clamp01(ratio);
        bar.Fill.anchorMax = new Vector2(ratio, 1f);
    }

    private void BuildUI()
    {
        GameObject canvasObject = new GameObject(
            "StarCoreCombatCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
        );

        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        bossHealthBar = CreateBar(
            "BossHealthBar",
            canvasObject.transform,
            new Vector2(0.5f, 1f),
            new Vector2(0f, -38f),
            new Vector2(860f, 64f),
            new Color(0.82f, 0.08f, 0.12f, 1f),
            25
        );

        starCoreBar = CreateBar(
            "PlayerStarCoreBar",
            canvasObject.transform,
            new Vector2(0f, 0f),
            new Vector2(35f, 35f),
            new Vector2(430f, 54f),
            new Color(0.1f, 0.72f, 1f, 1f),
            23
        );
    }

    private BarView CreateBar(
        string objectName,
        Transform parent,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector2 size,
        Color fillColor,
        int fontSize
    )
    {
        GameObject root = CreateImage(
            objectName,
            parent,
            new Color(0.015f, 0.02f, 0.035f, 0.9f)
        );

        RectTransform rootRect =
            root.GetComponent<RectTransform>();

        rootRect.anchorMin = anchor;
        rootRect.anchorMax = anchor;
        rootRect.pivot = anchor;
        rootRect.anchoredPosition = anchoredPosition;
        rootRect.sizeDelta = size;

        Outline rootOutline = root.AddComponent<Outline>();
        rootOutline.effectColor = new Color(0.65f, 0.75f, 1f, 0.8f);
        rootOutline.effectDistance = new Vector2(2f, -2f);

        GameObject fillObject = CreateImage(
            "Fill",
            root.transform,
            fillColor
        );

        RectTransform fillRect =
            fillObject.GetComponent<RectTransform>();

        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(5f, 5f);
        fillRect.offsetMax = new Vector2(-5f, -5f);

        GameObject highlightObject = CreateImage(
            "Highlight",
            fillObject.transform,
            new Color(1f, 1f, 1f, 0.16f)
        );

        RectTransform highlightRect =
            highlightObject.GetComponent<RectTransform>();

        highlightRect.anchorMin = new Vector2(0f, 0.58f);
        highlightRect.anchorMax = Vector2.one;
        highlightRect.offsetMin = Vector2.zero;
        highlightRect.offsetMax = Vector2.zero;

        GameObject textObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(Text)
        );

        textObject.transform.SetParent(root.transform, false);

        RectTransform textRect =
            textObject.GetComponent<RectTransform>();

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 0f);
        textRect.offsetMax = new Vector2(-10f, 0f);

        Text label = textObject.GetComponent<Text>();
        label.font = uiFont;
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;

        Outline textOutline = textObject.AddComponent<Outline>();
        textOutline.effectColor = Color.black;
        textOutline.effectDistance = new Vector2(1.5f, -1.5f);

        return new BarView
        {
            Fill = fillRect,
            Label = label
        };
    }

    private static GameObject CreateImage(
        string objectName,
        Transform parent,
        Color color
    )
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image)
        );

        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return imageObject;
    }
}
