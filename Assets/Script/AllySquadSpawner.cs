using UnityEngine;

public class AllySquadSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform spawnAnchor;
    [SerializeField] private Transform player;
    [SerializeField] private MaterialQuestManager materialQuest;
    [SerializeField] private QuestManager killQuest;

    [Header("Prefabs")]
    [SerializeField] private GameObject ropeAllyPrefab;
    [SerializeField] private GameObject spearAllyPrefab;

    [Header("Count")]
    [SerializeField] private int ropeAllyCount = 6;
    [SerializeField] private int spearAllyCount = 4;

    [Header("Formation")]
    [SerializeField] private float spacing = 7f;
    [SerializeField] private float spawnHeight = 6f;

    private bool hasSpawned;

    private void Awake()
    {
        if (spawnAnchor == null)
            spawnAnchor = transform;

        if (killQuest == null)
            killQuest =
                FindFirstObjectByType<QuestManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasSpawned)
            return;

        PlayerController controller =
            other.GetComponentInParent<PlayerController>();

        if (controller == null)
            return;

        if (killQuest == null ||
            killQuest.State != QuestState.InProgress)
        {
            return;
        }

        player = controller.transform;

        SpawnSquad();
        hasSpawned = true;
    }

    private void SpawnSquad()
    {
        int totalCount =
            ropeAllyCount + spearAllyCount;

        for (int i = 0; i < ropeAllyCount; i++)
        {
            SpawnAlly(
                ropeAllyPrefab,
                i,
                totalCount
            );
        }

        for (int i = 0; i < spearAllyCount; i++)
        {
            SpawnAlly(
                spearAllyPrefab,
                ropeAllyCount + i,
                totalCount
            );
        }
    }

    private void SpawnAlly(
        GameObject allyPrefab,
        int index,
        int totalCount
    )
    {
        if (allyPrefab == null)
            return;

        float centeredIndex =
            index - (totalCount - 1) * 0.5f;

        Vector2 formationOffset = new Vector2(
            centeredIndex * spacing,
            0f
        );

        Vector3 spawnPosition =
            spawnAnchor.position +
            new Vector3(
                formationOffset.x,
                spawnHeight,
                0f
            );

        GameObject ally = Instantiate(
            allyPrefab,
            spawnPosition,
            Quaternion.identity
        );

        AllyCombatFollower follower =
            ally.GetComponent<AllyCombatFollower>();

        if (follower != null)
        {
            follower.Configure(
                player,
                formationOffset
            );
        }
    }
}