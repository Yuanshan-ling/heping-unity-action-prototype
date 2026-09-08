using System;
using UnityEngine;

public enum QuestState
{
    NotStarted,     // 未接受
    InProgress,     // 进行中
    ReadyToTurnIn,  // 已完成，等待提交
    Completed       // 已提交
}

public class QuestManager : MonoBehaviour
{
    [Header("任务设置")]
    [Min(1)]
    [SerializeField] private int requiredKillCount = 3;
    [Min(0)]
    [SerializeField] private int rewardCoins = 10;

    private PlayerWallet wallet;

    private int currentKillCount;
    private QuestState state = QuestState.NotStarted;

    public int RequiredKillCount => requiredKillCount;
    public int CurrentKillCount => currentKillCount;
    public QuestState State => state;

    // 之后右侧任务进度 UI、商人对话按钮都会监听这个事件刷新显示
    public event Action Changed;

    private void Awake()
    {
        wallet = FindFirstObjectByType<PlayerWallet>();
    }

    public bool AcceptQuest()
    {
        if (state != QuestState.NotStarted)
            return false;

        currentKillCount = 0;
        state = QuestState.InProgress;
        Changed?.Invoke();
        return true;
    }

    public void RegisterEnemyKilled()
    {
        if (state != QuestState.InProgress)
            return;

        currentKillCount = Mathf.Min(
            currentKillCount + 1,
            requiredKillCount
        );

        if (currentKillCount >= requiredKillCount)
            state = QuestState.ReadyToTurnIn;

        Changed?.Invoke();
    }

    public bool TrySubmitQuest()
    {
        if (state != QuestState.ReadyToTurnIn)
            return false;

        state = QuestState.Completed;

        if (wallet != null)
            wallet.AddCoins(rewardCoins);

        Changed?.Invoke();
        return true;
    }
}