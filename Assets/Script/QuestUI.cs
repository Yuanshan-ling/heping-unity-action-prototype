using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class QuestUI : MonoBehaviour
{
    private QuestManager questManager;
    private MaterialQuestManager materialQuestManager;
    private Canvas canvas;
    private Text progressText;
    private Text notificationText;
    private Coroutine messageCoroutine;
    private Font uiFont;

    private void Awake()
    {
        questManager = GetComponent<QuestManager>();

        if (questManager == null)
            questManager = FindFirstObjectByType<QuestManager>();

        materialQuestManager =
    FindFirstObjectByType<MaterialQuestManager>();

        uiFont = Resources.GetBuiltinResource<Font>(
            "LegacyRuntime.ttf"
        );

        CreateUI();

        if (questManager != null)
        {
            questManager.Changed += Refresh;
            Refresh();
        }

        if (materialQuestManager != null)
        {
            materialQuestManager.Changed += Refresh;
            Refresh();
        }
    }

    private void OnDestroy()
    {
        if (questManager != null)
            questManager.Changed -= Refresh;

        if (canvas != null)
            Destroy(canvas.gameObject);

        if (materialQuestManager != null)
            materialQuestManager.Changed -= Refresh;
    }

    public void ShowMessage(string message)
    {
        if (messageCoroutine != null)
            StopCoroutine(messageCoroutine);

        notificationText.text = message;
        notificationText.gameObject.SetActive(true);

        messageCoroutine = StartCoroutine(HideMessageAfterDelay());
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(3f);

        notificationText.gameObject.SetActive(false);
        messageCoroutine = null;
    }

    private void Refresh()
    {
        if (materialQuestManager != null &&
            materialQuestManager.State !=
            MaterialQuestState.NotStarted)
        {
            progressText.gameObject.SetActive(true);

            if (materialQuestManager.State ==
                MaterialQuestState.InProgress)
            {
                progressText.text =
                    "任务：收集材料 " +
                    materialQuestManager.CurrentMaterialCount +
                    "/" +
                    materialQuestManager.RequiredMaterialCount;
            }
            else
            {
                progressText.text =
                    "材料已收集完成";
            }

            return;
        }

        if (questManager == null)
        {
            progressText.gameObject.SetActive(false);
            return;
        }

        if (questManager.State == QuestState.InProgress)
        {
            progressText.gameObject.SetActive(true);
            progressText.text =
                "任务：击杀怪物 " +
                questManager.CurrentKillCount +
                "/" +
                questManager.RequiredKillCount;
        }
        else if (questManager.State == QuestState.ReadyToTurnIn)
        {
            progressText.gameObject.SetActive(true);
            progressText.text =
                "任务已完成，请回到商人处";
        }
        else
        {
            progressText.gameObject.SetActive(false);
        }
    }

    private void CreateUI()
    {
        GameObject canvasObject = new GameObject(
            "QuestCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
        );

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution = new Vector2(1920f, 1080f);

        progressText = CreateText(
            "QuestProgress",
            canvasObject.transform
        );

        RectTransform progressRect =
            progressText.GetComponent<RectTransform>();

        progressRect.anchorMin = new Vector2(1f, 0.5f);
        progressRect.anchorMax = new Vector2(1f, 0.5f);
        progressRect.pivot = new Vector2(1f, 0.5f);
        progressRect.anchoredPosition = new Vector2(-30f, 0f);
        progressRect.sizeDelta = new Vector2(440f, 100f);

        progressText.alignment = TextAnchor.MiddleRight;
        progressText.fontSize = 25;
        progressText.color = Color.white;

        notificationText = CreateText(
            "QuestNotification",
            canvasObject.transform
        );

        RectTransform notificationRect =
            notificationText.GetComponent<RectTransform>();

        notificationRect.anchorMin = new Vector2(0.5f, 1f);
        notificationRect.anchorMax = new Vector2(0.5f, 1f);
        notificationRect.pivot = new Vector2(0.5f, 1f);
        notificationRect.anchoredPosition = new Vector2(0f, -55f);
        notificationRect.sizeDelta = new Vector2(600f, 70f);

        notificationText.alignment = TextAnchor.MiddleCenter;
        notificationText.fontSize = 30;
        notificationText.color = Color.yellow;
        notificationText.gameObject.SetActive(false);
    }

    private Text CreateText(
        string objectName,
        Transform parent
    )
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Text)
        );

        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = uiFont;
        text.raycastTarget = false;

        return text;
    }
}