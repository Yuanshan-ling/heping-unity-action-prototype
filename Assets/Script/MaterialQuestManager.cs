using System;
using UnityEngine;

public enum MaterialQuestState
{
    NotStarted,
    InProgress,
    Completed
}

public class MaterialQuestManager : MonoBehaviour
{
    [Header("材料任务设置")]
    [Min(1)]
    [SerializeField] private int requiredMaterialCount = 6;

    private int currentMaterialCount;
    private MaterialQuestState state =
        MaterialQuestState.NotStarted;

    public int RequiredMaterialCount =>
        requiredMaterialCount;

    public int CurrentMaterialCount =>
        currentMaterialCount;

    public MaterialQuestState State => state;

    public event Action Changed;

    public void StartQuest()
    {
        if (state != MaterialQuestState.NotStarted)
            return;

        currentMaterialCount = 0;
        state = MaterialQuestState.InProgress;
        Changed?.Invoke();
    }

    public void RegisterMaterialObtained()
    {
        if (state != MaterialQuestState.InProgress)
            return;

        currentMaterialCount = Mathf.Min(
            currentMaterialCount + 1,
            requiredMaterialCount
        );

        if (currentMaterialCount >=
            requiredMaterialCount)
        {
            state = MaterialQuestState.Completed;
        }

        Changed?.Invoke();
    }
}