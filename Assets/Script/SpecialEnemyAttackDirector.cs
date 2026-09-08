using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialEnemyAttackDirector : MonoBehaviour
{
    private enum AttackKind
    {
        Pounce,
        Charge,
        Throw
    }

    [Header("References")]
    [SerializeField] private SpecialEnemyPounce pounce;
    [SerializeField] private SpecialEnemyChargeAttack charge;
    [SerializeField] private SpecialEnemyThrowAttack throwAttack;

    [Header("Throw Approach References")]
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D enemyBody;
    [SerializeField] private EnemyAI enemyAI;
    [SerializeField] private SpriteRenderer enemyRenderer;

    [Header("Scheduling")]
    [Min(0f)]
    [SerializeField] private float firstAttackDelay = 1f;

    [Min(0f)]
    [SerializeField] private float delayBetweenAttacks = 1.5f;

    [SerializeField] private bool showDebugLogs = true;

    [Header("Attack Selection Chances")]
    [Range(0f, 1f)]
    [SerializeField] private float throwSelectionChance = 0.2f;

    [Header("Throw Planning")]
    [Min(0f)]
    [SerializeField] private float throwSelectionMaximumDistance = 25f;

    [Min(0.01f)]
    [SerializeField] private float throwReleaseDistance = 5f;

    [Min(0.1f)]
    [SerializeField] private float throwApproachTimeout = 3f;

    [Min(0f)]
    [SerializeField] private float throwApproachSpeed = 10f;

    private bool directorEnabled;
    private bool isLoopRunning;
    private bool isApproachingThrow;
    private bool hasLastAttack;
    private AttackKind lastAttack;

    private void Awake()
    {
        FindSkills();
        FindApproachReferences();
        DisableAutomaticDistanceTriggers();
    }

    private void OnDisable()
    {
        directorEnabled = false;
        isLoopRunning = false;
        isApproachingThrow = false;
        StopApproachMovement();
    }

    public void EnableDirector()
    {
        FindSkills();
        FindApproachReferences();
        DisableAutomaticDistanceTriggers();

        if (directorEnabled)
            return;

        directorEnabled = true;
        enabled = true;

        if (!isLoopRunning)
            StartCoroutine(AttackLoop());

        LogAction("Attack director enabled.");
    }

    public void DisableDirector()
    {
        directorEnabled = false;
        isApproachingThrow = false;
        StopApproachMovement();
        StopAllCoroutines();
        isLoopRunning = false;
    }

    private IEnumerator AttackLoop()
    {
        isLoopRunning = true;

        if (firstAttackDelay > 0f)
            yield return new WaitForSeconds(firstAttackDelay);

        while (directorEnabled)
        {
            while (directorEnabled && AnySkillRunning())
                yield return null;

            if (!directorEnabled)
                break;

            if (delayBetweenAttacks > 0f)
                yield return new WaitForSeconds(delayBetweenAttacks);

            if (!directorEnabled)
                break;

            if (!TryStartRandomAttack())
            {
                LogAction("No attack could start; retrying.");
                yield return null;
            }
        }

        isLoopRunning = false;
    }

    private bool TryStartRandomAttack()
    {
        List<AttackKind> candidates =
            new List<AttackKind>
            {
                AttackKind.Pounce,
                AttackKind.Charge
            };

        bool canSelectThrow =
            CanPlanThrow() &&
            Random.value <= throwSelectionChance;

        if (canSelectThrow)
            candidates.Add(AttackKind.Throw);

        if (hasLastAttack && candidates.Count > 1)
            candidates.Remove(lastAttack);

        while (candidates.Count > 0)
        {
            int index = Random.Range(0, candidates.Count);

            AttackKind selected = candidates[index];
            candidates.RemoveAt(index);

            if (!TryStart(selected))
                continue;

            lastAttack = selected;
            hasLastAttack = true;

            LogAction("Selected " + selected + ".");
            return true;
        }

        return false;
    }

    private bool TryStart(AttackKind attack)
    {
        switch (attack)
        {
            case AttackKind.Pounce:
                return pounce != null &&
                    pounce.TryStartPounceFromDirector();

            case AttackKind.Charge:
                return charge != null &&
                    charge.TryStartChargeFromDirector();

            case AttackKind.Throw:
                return BeginThrowPlan();

            default:
                return false;
        }
    }

    private bool BeginThrowPlan()
    {
        if (isApproachingThrow || !CanPlanThrow())
            return false;

        isApproachingThrow = true;
        StartCoroutine(ApproachThenStartThrow());

        return true;
    }

    private IEnumerator ApproachThenStartThrow()
    {
        FindApproachReferences();

        if (player == null)
        {
            isApproachingThrow = false;
            yield break;
        }

        if (enemyAI != null)
            enemyAI.enabled = false;

        float elapsed = 0f;

        while (elapsed < throwApproachTimeout)
        {
            FindApproachReferences();

            if (player == null)
                break;

            float horizontalDelta =
                player.position.x - transform.position.x;

            float horizontalDistance = Mathf.Abs(horizontalDelta);

            if (horizontalDistance <= throwReleaseDistance)
            {
                StopApproachMovement();

                bool started =
                    throwAttack != null &&
                    throwAttack.TryStartThrowFromDirector(
                        throwReleaseDistance
                    );

                isApproachingThrow = false;

                if (!started && enemyAI != null)
                    enemyAI.enabled = true;

                if (!started)
                {
                    LogAction(
                        "Throw was cancelled: release conditions failed."
                    );
                }

                yield break;
            }

            float direction = Mathf.Sign(horizontalDelta);

            if (Mathf.Approximately(direction, 0f))
                direction = 1f;

            if (enemyRenderer != null)
                enemyRenderer.flipX = direction < 0f;

            if (enemyBody != null)
            {
                enemyBody.linearVelocity = new Vector2(
                    direction * throwApproachSpeed,
                    enemyBody.linearVelocity.y
                );
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        StopApproachMovement();

        if (enemyAI != null)
            enemyAI.enabled = true;

        isApproachingThrow = false;

        LogAction("Throw plan expired; selecting another attack.");
    }

    private bool CanPlanThrow()
    {
        FindSkills();
        FindApproachReferences();

        if (throwAttack == null || player == null)
            return false;

        float horizontalDistance = Mathf.Abs(
            player.position.x - transform.position.x
        );

        return horizontalDistance <= throwSelectionMaximumDistance;
    }

    private bool AnySkillRunning()
    {
        return isApproachingThrow ||
            (pounce != null && pounce.IsPouncing) ||
            (charge != null && charge.IsAttacking) ||
            (throwAttack != null && throwAttack.IsAttacking);
    }

    private void FindSkills()
    {
        if (pounce == null)
            pounce = GetComponent<SpecialEnemyPounce>();

        if (charge == null)
            charge = GetComponent<SpecialEnemyChargeAttack>();

        if (throwAttack == null)
            throwAttack = GetComponent<SpecialEnemyThrowAttack>();
    }

    private void FindApproachReferences()
    {
        if (player == null)
        {
            PlayerController controller =
                FindFirstObjectByType<PlayerController>();

            if (controller != null)
                player = controller.transform;
        }

        if (enemyBody == null)
            enemyBody = GetComponent<Rigidbody2D>();

        if (enemyAI == null)
            enemyAI = GetComponent<EnemyAI>();

        if (enemyRenderer == null)
            enemyRenderer = GetComponent<SpriteRenderer>();
    }

    private void DisableAutomaticDistanceTriggers()
    {
        if (pounce != null)
            pounce.SetAutomaticDistanceTrigger(false);

        if (charge != null)
            charge.SetAutomaticDistanceTrigger(false);

        if (throwAttack != null)
            throwAttack.SetAutomaticDistanceTrigger(false);
    }

    private void StopApproachMovement()
    {
        if (enemyBody == null)
            return;

        enemyBody.linearVelocity = new Vector2(
            0f,
            enemyBody.linearVelocity.y
        );
    }

    private void LogAction(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log(
                "SpecialEnemyAttackDirector: " + message,
                this
            );
        }
    }
}