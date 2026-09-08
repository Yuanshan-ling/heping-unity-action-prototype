using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public enum DialogueSpeaker
{
    Merchant,
    Player
}

[Serializable]
public class StoryDialogueLine
{
    public DialogueSpeaker speaker;

    [TextArea(2, 5)]
    public string content;
}

public class StoryDialogueController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dialogueCanvas;
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject merchantPortrait;
    [SerializeField] private GameObject playerPortrait;

    [Header("Quest Trigger")]
    [SerializeField] private MaterialQuestManager materialQuest;

    [SerializeField]
    private bool startMaterialQuestAtEnd = true;

    [Header("Dialogue Lines")]
    [SerializeField] private StoryDialogueLine[] lines;

    private int currentLineIndex;
    private bool isPlaying;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (dialogueCanvas != null)
            dialogueCanvas.SetActive(false);

        if (materialQuest == null)
        {
            materialQuest =
                FindFirstObjectByType<MaterialQuestManager>();
        }
    }

    private void Update()
    {
        if (isPlaying &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            NextLine();
        }
    }

    public void StartDialogue()
    {
        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("没有设置对话台词。");
            return;
        }

        currentLineIndex = 0;
        isPlaying = true;
        dialogueCanvas.SetActive(true);
        ShowCurrentLine();
    }

    public void NextLine()
    {
        currentLineIndex++;

        if (currentLineIndex >= lines.Length)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    public void EndDialogue()
    {
        isPlaying = false;
        dialogueCanvas.SetActive(false);
        if (startMaterialQuestAtEnd &&
    materialQuest != null)
        {
            materialQuest.StartQuest();
        }
    }

    private void ShowCurrentLine()
    {
        StoryDialogueLine line = lines[currentLineIndex];

        bool merchantSpeaking =
            line.speaker == DialogueSpeaker.Merchant;

        speakerText.text = merchantSpeaking ? "商人" : "旅行者";
        dialogueText.text = line.content;

        merchantPortrait.SetActive(merchantSpeaking);
        playerPortrait.SetActive(!merchantSpeaking);
    }
}