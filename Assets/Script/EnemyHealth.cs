using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Min(1)]
    [SerializeField] private int maxHealth = 3;

    [SerializeField] private float hitFlashDuration = 0.1f;

    [Header("Kill Coin Reward")]
    [Min(0)]
    [SerializeField] private int killCoinReward = 30;

    [Min(0f)]
    [SerializeField] private float rewardTextStartHeight = 3f;

    [Min(0f)]
    [SerializeField] private float rewardTextRiseDistance = 3f;

    [Min(0.1f)]
    [SerializeField] private float rewardTextDuration = 0.9f;

    [Min(0.01f)]
    [SerializeField] private float rewardTextSize = 0.32f;

    private int currentHealth;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashCoroutine;
    private QuestManager questManager;
    private MaterialQuestManager materialQuestManager;
    private PlayerWallet wallet;

    private void Awake()
    {
        currentHealth = maxHealth;

        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        questManager = FindFirstObjectByType<QuestManager>();
        materialQuestManager =
            FindFirstObjectByType<MaterialQuestManager>();

        wallet = FindFirstObjectByType<PlayerWallet>();
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || currentHealth <= 0)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            if (questManager != null)
                questManager.RegisterEnemyKilled();

            if (materialQuestManager != null)
                materialQuestManager.RegisterMaterialObtained();

            GiveKillCoinReward();

            Destroy(gameObject);
            return;
        }

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRed());
    }

    private void GiveKillCoinReward()
    {
        if (killCoinReward <= 0)
            return;

        if (wallet != null)
            wallet.AddCoins(killCoinReward);

        GameObject popupObject = new GameObject(
            "CoinRewardPopup"
        );

        popupObject.transform.position =
            transform.position +
            Vector3.up * rewardTextStartHeight;

        CoinRewardPopup popup =
            popupObject.AddComponent<CoinRewardPopup>();

        popup.Setup(
            killCoinReward,
            rewardTextRiseDistance,
            rewardTextDuration,
            rewardTextSize
        );
    }

    private IEnumerator FlashRed()
    {
        spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(hitFlashDuration);

        spriteRenderer.color = originalColor;
        flashCoroutine = null;
    }
}