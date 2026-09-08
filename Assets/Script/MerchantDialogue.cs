using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum DialogueOptionAction
{
    OpenShop,
    SmallTalk,
    CloseDialogue,
    Quest
}

[System.Serializable]
public class DialogueOption
{
    [SerializeField] private string label;
    [SerializeField] private DialogueOptionAction action;

    public string Label => label;
    public DialogueOptionAction Action => action;

    public DialogueOption(
        string newLabel,
        DialogueOptionAction newAction
    )
    {
        label = newLabel;
        action = newAction;
    }
}

public class MerchantDialogue : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private MerchantShop merchantShop;
    [SerializeField] private QuestManager questManager;
    [SerializeField] private QuestUI questUI;
    [SerializeField] private StoryDialogueController storyDialogue;


    [Header("Interaction")]
    [SerializeField] private float interactionRange = 8f;
    [SerializeField] private Vector2 screenOffset = new(70f, 55f);

    [Header("Dialogue Options")]
    [SerializeField]
    private List<DialogueOption> options = new()
    {
        new DialogueOption(
            "进入商店",
            DialogueOptionAction.OpenShop
        ),
        new DialogueOption(
            "闲聊",
            DialogueOptionAction.SmallTalk
        ),
        new DialogueOption(
            "退出对话",
            DialogueOptionAction.CloseDialogue
        )
    };

    private Canvas dialogueCanvas;
    private RectTransform dialoguePanel;
    private Font uiFont;
    private readonly List<Text> optionTexts = new();

    private void Awake()
    {
        uiFont = Resources.GetBuiltinResource<Font>(
            "LegacyRuntime.ttf"
        );


        if (questManager == null)
            questManager = FindFirstObjectByType<QuestManager>();

        if (questUI == null)
            questUI = FindFirstObjectByType<QuestUI>();

        if (storyDialogue == null)
        {
            storyDialogue =
                FindFirstObjectByType<StoryDialogueController>();
        }

        CreateDialogueUI();
        dialoguePanel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (player == null)
            return;

        if (dialoguePanel.gameObject.activeSelf)
        {
            UpdateDialoguePosition();

            if (Vector2.Distance(
                    player.position,
                    transform.position
                ) > interactionRange + 2f)
            {
                CloseDialogue();
            }

            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseDialogue();
            }

            return;
        }

        if (Vector2.Distance(
                player.position,
                transform.position
            ) <= interactionRange &&
            Keyboard.current != null &&
            Keyboard.current.tKey.wasPressedThisFrame)
        {
            OpenDialogue();
        }
    }

    private void OpenDialogue()
    {
        RefreshOptionLabels();
        UpdateDialoguePosition();
        dialoguePanel.gameObject.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void CloseDialogue()
    {
        dialoguePanel.gameObject.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void HandleOption(DialogueOptionAction action)
    {
        switch (action)
        {
            case DialogueOptionAction.OpenShop:
                OpenShop();
                break;

            case DialogueOptionAction.SmallTalk:
                SmallTalk();
                break;

            case DialogueOptionAction.CloseDialogue:
                CloseDialogue();
                break;

            case DialogueOptionAction.Quest:
                HandleQuest();
                break;
        }
    }

    private void HandleQuest()
    {
        if (questManager == null)
            return;

        if (questManager.State == QuestState.NotStarted)
        {
            questManager.AcceptQuest();
            CloseDialogue();
            return;
        }

        if (questManager.State == QuestState.InProgress)
        {
            if (questUI != null)
                questUI.ShowMessage("未完成任务");

            CloseDialogue();
            return;
        }

        if (questManager.State == QuestState.ReadyToTurnIn)
        {
            if (questManager.TrySubmitQuest() &&
                questUI != null)
            {
                questUI.ShowMessage("任务已完成");
            }

            CloseDialogue();
            return;
        }

        if (questManager.State == QuestState.Completed)
        {
            if (questUI != null)
                questUI.ShowMessage("任务已经提交");

            CloseDialogue();
        }
    }

    private string GetOptionLabel(DialogueOption option)
    {
        if (option.Action != DialogueOptionAction.Quest)
            return option.Label;

        if (questManager == null ||
            questManager.State == QuestState.NotStarted)
        {
            return "接受任务";
        }

        if (questManager.State == QuestState.Completed)
            return "任务已提交";

        return "提交任务";
    }

    private void OpenShop()
    {
        CloseDialogue();

        if (shopUI != null && merchantShop != null)
            shopUI.Open(merchantShop);
    }

    private void SmallTalk()
    {
        CloseDialogue();

        if (storyDialogue == null)
        {
            Debug.LogWarning(
                "MerchantDialogue：没有找到 StoryDialogueController。"
            );
            return;
        }

        storyDialogue.StartDialogue();
    }
    private void UpdateDialoguePosition()
    {
        Camera camera = Camera.main;

        if (camera == null)
            return;

        Vector3 screenPosition =
            camera.WorldToScreenPoint(transform.position);

        dialoguePanel.position =
            screenPosition + (Vector3)screenOffset;
    }

    private void RefreshOptionLabels()
    {
        for (int i = 0; i < optionTexts.Count && i < options.Count; i++)
        {
            if (optionTexts[i] != null)
                optionTexts[i].text = GetOptionLabel(options[i]);
        }
    }

    private void CreateDialogueUI()
    {
        GameObject canvasObject = new GameObject(
            "MerchantDialogueCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        dialogueCanvas = canvasObject.GetComponent<Canvas>();
        dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dialogueCanvas.sortingOrder = 20;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject panelObject = new GameObject(
            "DialoguePanel",
            typeof(RectTransform),
            typeof(Image),
            typeof(VerticalLayoutGroup)
        );

        panelObject.transform.SetParent(
            canvasObject.transform,
            false
        );

        dialoguePanel =
            panelObject.GetComponent<RectTransform>();

        dialoguePanel.pivot = new Vector2(0f, 0f);

        int optionCount = Mathf.Max(options.Count, 1);

        dialoguePanel.sizeDelta = new Vector2(
            180f,
            optionCount * 56f + 20f
        );

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.8f);

        VerticalLayoutGroup layout =
            panelObject.GetComponent<VerticalLayoutGroup>();

        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        foreach (DialogueOption option in options)
        {
            if (option == null)
                continue;

            DialogueOption currentOption = option;

            Button button = CreateButton(
                panelObject.transform,
                GetOptionLabel(currentOption)
            );

            optionTexts.Add(button.GetComponentInChildren<Text>());

            button.onClick.AddListener(
                () => HandleOption(currentOption.Action)
            );
        }
    }

    private Button CreateButton(
        Transform parent,
        string labelText
    )
    {
        GameObject buttonObject = new GameObject(
            labelText + "Button",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement)
        );

        buttonObject.transform.SetParent(parent, false);

        Image buttonImage =
            buttonObject.GetComponent<Image>();

        buttonImage.color = new Color(
            0.85f,
            0.85f,
            0.85f,
            1f
        );

        LayoutElement layout =
            buttonObject.GetComponent<LayoutElement>();

        layout.preferredHeight = 48f;

        GameObject textObject = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(Text)
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

        Text text = textObject.GetComponent<Text>();
        text.font = uiFont;
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        text.text = labelText;
        text.raycastTarget = false;

        return buttonObject.GetComponent<Button>();
    }

    private void OnDestroy()
    {
        if (dialogueCanvas != null)
            Destroy(dialogueCanvas.gameObject);
    }

}