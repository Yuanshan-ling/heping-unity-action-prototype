using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllySpearFighter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator spearAnimator;
    [SerializeField] private Transform spearTip;

    [Header("Attack")]
    [SerializeField]
    private string thrustStateName =
        "AllySpearThrust";

    [SerializeField] private float attackRange = 14f;
    [SerializeField] private float hitRadius = 1.2f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float hitDelay = 0.12f;
    [SerializeField] private float attackCooldown = 1f;

    private bool isAttacking;

    private void Update()
    {
        if (!isAttacking && FindEnemyInFront() != null)
            StartCoroutine(Attack());
    }

    private EnemyHealth FindEnemyInFront()
    {
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(
            FindObjectsSortMode.None
        );

        EnemyHealth closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (EnemyHealth enemy in enemies)
        {
            Vector2 offset =
                enemy.transform.position - transform.position;

            // µ±«∞≥§«π≥Ø”“£¨÷ªπ•ª˜”“≤‡µ–»À°£
            float facing = Mathf.Sign(transform.lossyScale.x);

            if (offset.x * facing <= 0f)
                continue;

            float distance = offset.magnitude;

            if (distance <= attackRange &&
                distance < closestDistance)
            {
                closestEnemy = enemy;
                closestDistance = distance;
            }
        }

        return closestEnemy;
    }

    private IEnumerator Attack()
    {
        isAttacking = true;

        spearAnimator.Play(thrustStateName, 0, 0f);

        yield return new WaitForSeconds(hitDelay);

        DamageEnemiesAtSpearTip();

        yield return new WaitForSeconds(
            Mathf.Max(0.05f, attackCooldown - hitDelay)
        );

        isAttacking = false;
    }

    private void DamageEnemiesAtSpearTip()
    {
        if (spearTip == null)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            spearTip.position,
            hitRadius
        );

        HashSet<EnemyHealth> damagedEnemies = new();

        foreach (Collider2D hit in hits)
        {
            EnemyHealth enemy =
                hit.GetComponentInParent<EnemyHealth>();

            if (enemy != null &&
                damagedEnemies.Add(enemy))
            {
                enemy.TakeDamage(damage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (spearTip == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            spearTip.position,
            hitRadius
        );
    }
}