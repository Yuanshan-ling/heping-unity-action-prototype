using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform attackHand;

    [SerializeField] private int damage = 1;
    [SerializeField] private float hitDelay = 0.16f;
    [SerializeField] private float hitRadius = 1.2f;
    [Header("Effects")]
    [SerializeField] private GameObject hitSparkPrefab;
    [Header("Control")]
    [SerializeField] private bool acceptsAttackInput = true;

    public void SetAttackInputEnabled(bool enabled)
    {
        acceptsAttackInput = enabled;
    }

    private bool isAttacking;
    private float facingDirection = 1f;
    private float lastPlayerX;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (player != null)
            lastPlayerX = player.position.x;
    }

    private void LateUpdate()
    {
        if (player != null)
        {
            float currentPlayerX = player.position.x;

            if (Mathf.Abs(currentPlayerX - lastPlayerX) > 0.001f)
                facingDirection = Mathf.Sign(currentPlayerX - lastPlayerX);

            transform.position = new Vector3(
                player.position.x,
                player.position.y,
                transform.position.z
            );

            transform.localScale = new Vector3(
                facingDirection,
                1f,
                1f
            );

            lastPlayerX = currentPlayerX;
        }

        if (acceptsAttackInput &&
     Keyboard.current != null &&
     Keyboard.current.jKey.wasPressedThisFrame &&
     !isAttacking)
        {
            TriggerAttack();
        }
    }

    public void TriggerAttack()
    {
        if (!isAttacking)
            StartCoroutine(Attack());
    }

    public IEnumerator PlayQTEAttack(EnemyHealth targetEnemy)
    {
        if (isAttacking)
            yield break;

        isAttacking = true;



        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(hitDelay);

        if (targetEnemy != null)
            DamageEnemy(targetEnemy, null);
        else
            DamageEnemiesNearHand();

        yield return new WaitForSeconds(0.18f);


        isAttacking = false;
    }

    private IEnumerator Attack()
    {
        isAttacking = true;
        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(hitDelay);
        DamageEnemiesNearHand();

        yield return new WaitForSeconds(0.18f);
        isAttacking = false;
    }

   private void DamageEnemiesNearHand()
{
    if (attackHand == null)
        return;

    Collider2D[] hits = Physics2D.OverlapCircleAll(
        attackHand.position,
        hitRadius
    );

    HashSet<EnemyHealth> damagedEnemies = new();

    foreach (Collider2D hit in hits)
    {
        EnemyHealth enemy =
            hit.GetComponentInParent<EnemyHealth>();

        if (enemy != null && damagedEnemies.Add(enemy))
            DamageEnemy(enemy, hit);
    }
}

    private void DamageEnemy(
    EnemyHealth enemy,
    Collider2D enemyCollider
)
    {
        if (enemy == null)
            return;

        if (enemyCollider == null)
            enemyCollider = enemy.GetComponentInChildren<Collider2D>();

        Vector2 sourcePosition = attackHand != null
            ? attackHand.position
            : enemy.transform.position;

        Vector2 hitPoint = enemyCollider != null
            ? enemyCollider.ClosestPoint(sourcePosition)
            : (Vector2)enemy.transform.position;

        if (hitSparkPrefab != null)
            Instantiate(hitSparkPrefab, hitPoint, Quaternion.identity);

        enemy.TakeDamage(damage);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackHand == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackHand.position, hitRadius);
    }
}