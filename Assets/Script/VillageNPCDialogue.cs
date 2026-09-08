using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[System.Serializable]
public class SmallTalkLine
{
    [Tooltip("勾选：NPC 说话；取消：主角说话。")]
    public bool npcSpeaks = true;

    [TextArea(2, 4)]
    public string text = "……";

    [Min(0.1f)]
    public float duration = 2f;
}

public class VillageNPCDialogue : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform talkPoint;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private TMP_FontAsset chineseFont;

    [Header("闲谈内容")]
    [SerializeField]
    private SmallTalkLine[] smallTalkLines =
    {
        new SmallTalkLine
        {
            npcSpeaks = true,
            text = "这片矿区可不太安静。",
            duration = 2f
        },
        new SmallTalkLine
        {
            npcSpeaks = false,
            text = "我会小心的。",
            duration = 2f
        },
        new SmallTalkLine
        {
            npcSpeaks = true,
            text = "祝你好运，旅行者。",
            duration = 2f
        }
    };

    private bool playerNearby;
    private Transform player;

    private GameObject canvasRoot;
    private RectTransform canvasRect;
    private RectTransform menuPanel;

    private RectTransform npcBubble;
    private RectTransform playerBubble;
    private TextMeshProUGUI npcBubbleText;
    private TextMeshProUGUI playerBubbleText;

    private bool isSmallTalking;
    private int smallTalkIndex;

    private void Awake()
    {
        if (talkPoint == null)
            talkPoint = transform;

        if (mainCamera == null)
            mainCamera = Camera.main;

        CreateDialogueUI();
    }

    private void Update()
    {
        if (isSmallTalking)
        {
            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame)
            {
                AdvanceSmallTalk();
            }

            return;
        }

        if (Keyboard.current != null &&
            Keyboard.current.tKey.wasPressedThisFrame &&
            playerNearby &&
            !menuPanel.gameObject.activeSelf)
        {
            OpenMenu();
        }
    }

    private void LateUpdate()
    {
        if (menuPanel != null && menuPanel.gameObject.activeSelf)
            PositionWorldUI(menuPanel, talkPoint.position);

        if (npcBubble != null && npcBubble.gameObject.activeSelf)
            PositionWorldUI(npcBubble, talkPoint.position);

        if (playerBubble != null &&
            playerBubble.gameObject.activeSelf &&
            player != null)
        {
            PositionWorldUI(
                playerBubble,
                GetPlayerTalkPosition()
            );
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController controller =
            other.GetComponentInParent<PlayerController>();

        if (controller == null)
            return;

        player = controller.transform;
        playerNearby = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController controller =
            other.GetComponentInParent<PlayerController>();

        if (controller == null)
            return;

        playerNearby = false;
        CloseMenu();
        StopSmallTalk();
    }

    private void CreateDialogueUI()
    {
        canvasRoot = new GameObject(
            "VillageNPCDialogueCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 70;

        canvasRect = canvasRoot.GetComponent<RectTransform>();

        CanvasScaler scaler =
            canvasRoot.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject panelObject = new GameObject(
            "DialogueChoices",
            typeof(RectTransform),
            typeof(Image),
            typeof(VerticalLayoutGroup)
        );

        panelObject.transform.SetParent(
            canvasRoot.transform,
            false
        );

        menuPanel = panelObject.GetComponent<RectTransform>();
        menuPanel.anchorMin = new Vector2(0.5f, 0.5f);
        menuPanel.anchorMax = new Vector2(0.5f, 0.5f);
        menuPanel.pivot = new Vector2(0.5f, 0f);
        menuPanel.sizeDelta = new Vector2(210f, 120f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.82f);

        VerticalLayoutGroup layout =
            panelObject.GetComponent<VerticalLayoutGroup>();

        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        CreateButton(
            panelObject.transform,
            "闲谈",
            StartSmallTalk
        );

        CreateButton(
            panelObject.transform,
            "退出",
            CloseMenu
        );

        npcBubble = CreateBubble(
            "NPCBubble",
            out npcBubbleText
        );

        playerBubble = CreateBubble(
            "PlayerBubble",
            out playerBubbleText
        );

        menuPanel.gameObject.SetActive(false);
    }

    private void CreateButton(
        Transform parent,
        string label,
        UnityAction action
    )
    {
        GameObject buttonObject = new GameObject(
            label + "Button",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement)
        );

        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = Color.white;

        LayoutElement element =
            buttonObject.GetComponent<LayoutElement>();

        element.preferredHeight = 46f;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        GameObject textObject = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        textObject.transform.SetParent(
            buttonObject.transform,
            false
        );

        RectTransform textRect =
            textObject.GetComponent<RectTransform>();

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text =
            textObject.GetComponent<TextMeshProUGUI>();

        text.font = GetFont();
        text.text = label;
        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        text.raycastTarget = false;
    }

    private RectTransform CreateBubble(
        string objectName,
        out TextMeshProUGUI bubbleText
    )
    {
        GameObject bubbleObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image)
        );

        bubbleObject.transform.SetParent(
            canvasRoot.transform,
            false
        );

        RectTransform bubbleRect =
            bubbleObject.GetComponent<RectTransform>();

        bubbleRect.anchorMin = new Vector2(0.5f, 0.5f);
        bubbleRect.anchorMax = new Vector2(0.5f, 0.5f);
        bubbleRect.pivot = new Vector2(0.5f, 0f);
        bubbleRect.sizeDelta = new Vector2(280f, 94f);

        Image image = bubbleObject.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;

        GameObject textObject = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        textObject.transform.SetParent(
            bubbleObject.transform,
            false
        );

        RectTransform textRect =
            textObject.GetComponent<RectTransform>();

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 8f);
        textRect.offsetMax = new Vector2(-12f, -8f);

        bubbleText = textObject.GetComponent<TextMeshProUGUI>();
        bubbleText.font = GetFont();
        bubbleText.fontSize = 22f;
        bubbleText.alignment = TextAlignmentOptions.Center;
        bubbleText.color = Color.black;
        bubbleText.enableWordWrapping = true;
        bubbleText.raycastTarget = false;

        bubbleObject.SetActive(false);

        return bubbleRect;
    }

    private TMP_FontAsset GetFont()
    {
        if (chineseFont != null)
            return chineseFont;

        return TMP_Settings.defaultFontAsset;
    }

    private void OpenMenu()
    {
        menuPanel.gameObject.SetActive(true);
        PositionWorldUI(menuPanel, talkPoint.position);
    }

    private void CloseMenu()
    {
        if (menuPanel != null)
            menuPanel.gameObject.SetActive(false);
    }

    private void StartSmallTalk()
    {
        CloseMenu();

        smallTalkIndex = 0;
        isSmallTalking = true;

        ShowCurrentSmallTalkLine();
    }

    private void AdvanceSmallTalk()
    {
        smallTalkIndex++;
        ShowCurrentSmallTalkLine();
    }

    private void ShowCurrentSmallTalkLine()
    {
        while (smallTalkIndex < smallTalkLines.Length &&
               (smallTalkLines[smallTalkIndex] == null ||
                string.IsNullOrWhiteSpace(
                    smallTalkLines[smallTalkIndex].text)))
        {
            smallTalkIndex++;
        }

        if (smallTalkIndex >= smallTalkLines.Length)
        {
            StopSmallTalk();
            return;
        }

        SmallTalkLine line = smallTalkLines[smallTalkIndex];

        ShowBubble(line.npcSpeaks, line.text);
    }

    private void StopSmallTalk()
    {
        isSmallTalking = false;
        smallTalkIndex = 0;

        HideBubbles();
    }

    private void ShowBubble(bool npcSpeaks, string message)
    {
        HideBubbles();

        if (npcSpeaks)
        {
            npcBubbleText.text = message;
            npcBubble.gameObject.SetActive(true);
        }
        else
        {
            playerBubbleText.text = message;
            playerBubble.gameObject.SetActive(true);
        }
    }

    private void HideBubbles()
    {
        if (npcBubble != null)
            npcBubble.gameObject.SetActive(false);

        if (playerBubble != null)
            playerBubble.gameObject.SetActive(false);
    }


    private Vector3 GetPlayerTalkPosition()
    {
        Collider2D playerCollider =
            player != null
                ? player.GetComponent<Collider2D>()
                : null;

        if (playerCollider != null)
        {
            return new Vector3(
                playerCollider.bounds.center.x,
                playerCollider.bounds.max.y + 0.6f,
                player.position.z
            );
        }

        return player.position + Vector3.up * 5f;
    }

    private void PositionWorldUI(
        RectTransform target,
        Vector3 worldPosition
    )
    {
        if (mainCamera == null || canvasRect == null)
            return;

        Vector3 screenPosition =
            mainCamera.WorldToScreenPoint(worldPosition);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            null,
            out Vector2 localPosition
        );

        target.anchoredPosition =
            localPosition + new Vector2(0f, 24f);
    }
}