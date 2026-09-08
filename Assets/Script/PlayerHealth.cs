using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [Min(1)]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private bool invincibleForTest;

    [SerializeField] private float hitFlashDuration = 0.12f;

    [Header("Respawn")]
    [SerializeField] private Transform respawnPoint;
    [Min(0f)]
    [SerializeField] private float respawnDelay = 1f;

    private int currentHealth;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D body;
    private Color originalColor;
    private Coroutine flashCoroutine;
    private bool isRespawning;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public event Action Changed;

    private void Awake()
    {
        currentHealth = maxHealth;

        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        originalColor = spriteRenderer.color;
    }

    public void TakeDamage(int damage)
    {
        if (invincibleForTest ||
            damage <= 0 ||
            currentHealth <= 0 ||
            isRespawning)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        Changed?.Invoke();

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRed());

        if (currentHealth == 0)
            StartCoroutine(Respawn());
    }

    public void RestoreFullHealth()
    {
        currentHealth = maxHealth;
        Changed?.Invoke();
    }

    private IEnumerator Respawn()
    {
        isRespawning = true;

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }

        yield return new WaitForSeconds(respawnDelay);

        if (respawnPoint != null)
            transform.position = respawnPoint.position;
        else
            Debug.LogError("PlayerHealth ÉÐÎ´Ö¸¶¨ Respawn Point¡£");

        if (body != null)
        {
            body.simulated = true;
            body.linearVelocity = Vector2.zero;
        }

        RestoreFullHealth();
        isRespawning = false;
    }

    private IEnumerator FlashRed()
    {
        spriteRenderer.color = Color.red;

        yield return new WaitForSeconds(hitFlashDuration);

        spriteRenderer.color = originalColor;
        flashCoroutine = null;
    }
}