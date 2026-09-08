using UnityEngine;

public class PlayerArmorUnlocker : MonoBehaviour
{
    [SerializeField] private GameObject playerArmor;
    [SerializeField] private MaterialQuestManager materialQuest;

    private void Start()
    {
        if (materialQuest == null)
            materialQuest = FindFirstObjectByType<MaterialQuestManager>();

        if (materialQuest != null)
            materialQuest.Changed += RefreshArmor;

        RefreshArmor();
    }

    private void OnDestroy()
    {
        if (materialQuest != null)
            materialQuest.Changed -= RefreshArmor;
    }

    private void RefreshArmor()
    {
        if (playerArmor == null)
            return;

        bool isQuestCompleted =
            materialQuest != null &&
            materialQuest.State == MaterialQuestState.Completed;

        playerArmor.SetActive(isQuestCompleted);
    }
}