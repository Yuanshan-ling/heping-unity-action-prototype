using System.Collections;
using UnityEngine;

public class StarCoreThrownWeapon : MonoBehaviour
{
    private StarCoreExecutionerBoss owner;
    private StarCorePlayerCombat playerCombat;
    private SpriteRenderer weaponRenderer;
    private Sprite weaponSprite;
    private Vector2 outwardTarget;
    private float outwardDuration;
    private float returnDuration;
    private int damage;
    private float hitRadius;
    private bool configured;
    private bool wasParried;
    private float nextAfterimageTime;

    public void Configure(
        StarCoreExecutionerBoss newOwner,
        StarCorePlayerCombat newPlayerCombat,
        Sprite weaponSprite,
        Vector2 newOutwardTarget,
        float newOutwardDuration,
        float newReturnDuration,
        int newDamage,
        float newHitRadius
    )
    {
        owner = newOwner;
        playerCombat = newPlayerCombat;
        outwardTarget = newOutwardTarget;
        outwardDuration = Mathf.Max(0.01f, newOutwardDuration);
        returnDuration = Mathf.Max(0.01f, newReturnDuration);
        damage = Mathf.Max(1, newDamage);
        hitRadius = Mathf.Max(0.01f, newHitRadius);

        this.weaponSprite = weaponSprite;
        BuildWeaponVisual();

        configured = true;
        StartCoroutine(FlightRoutine());
    }

    private IEnumerator FlightRoutine()
    {
        while (!configured)
            yield return null;

        Vector2 start = transform.position;
        bool hitOutbound = false;

        yield return MoveSegment(
            start,
            outwardTarget,
            outwardDuration,
            true,
            value => hitOutbound = value,
            () => hitOutbound
        );

        if (wasParried)
            yield break;

        if (owner == null)
        {
            Destroy(gameObject);
            yield break;
        }

        bool hitReturning = false;

        yield return MoveSegment(
            transform.position,
            owner.transform.position,
            returnDuration,
            true,
            value => hitReturning = value,
            () => hitReturning
        );

        if (wasParried)
            yield break;

        Destroy(gameObject);
    }

    private IEnumerator MoveSegment(
        Vector2 start,
        Vector2 end,
        float duration,
        bool parryable,
        System.Action<bool> setHit,
        System.Func<bool> getHit
    )
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (owner == null)
                yield break;

            float progress = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector2.Lerp(start, end, progress);
            transform.Rotate(0f, 0f, 900f * Time.deltaTime);

            if (Time.time >= nextAfterimageTime)
            {
                SpawnAfterimage();
                nextAfterimageTime = Time.time + 0.035f;
            }

            if (!getHit() && playerCombat != null &&
                Vector2.Distance(transform.position, playerCombat.transform.position)
                    <= hitRadius)
            {
                StarCoreDefenseResult result =
                    playerCombat.ReceiveBossAttack(
                        damage,
                        30f,
                        parryable,
                        transform.position
                    );

                owner.OnThrownWeaponDefense(result);
                setHit(true);

                if (result == StarCoreDefenseResult.Parried)
                {
                    wasParried = true;
                    Destroy(gameObject);
                    yield break;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = end;
    }

    private void BuildWeaponVisual()
    {
        if (weaponSprite == null)
            return;

        weaponRenderer = CreatePiece(
            "ThrownBlade",
            Vector2.zero,
            new Vector2(2.8f, 0.34f),
            0f,
            new Color(0.88f, 0.95f, 1f, 1f),
            46
        );

        CreatePiece(
            "ThrownCore",
            Vector2.zero,
            new Vector2(0.52f, 0.52f),
            45f,
            new Color(1f, 0.2f, 0.06f, 1f),
            48
        );

        CreatePiece(
            "ThrownUpperHook",
            new Vector2(0.65f, 0.25f),
            new Vector2(1.05f, 0.14f),
            30f,
            new Color(0.55f, 0.7f, 0.82f, 1f),
            47
        );

        CreatePiece(
            "ThrownLowerHook",
            new Vector2(-0.65f, -0.25f),
            new Vector2(1.05f, 0.14f),
            30f,
            new Color(0.55f, 0.7f, 0.82f, 1f),
            47
        );
    }

    private SpriteRenderer CreatePiece(
        string objectName,
        Vector2 localPosition,
        Vector2 localScale,
        float angle,
        Color color,
        int sortingOrder
    )
    {
        GameObject piece = new GameObject(objectName);
        piece.transform.SetParent(transform, false);
        piece.transform.localPosition = localPosition;
        piece.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        piece.transform.localScale = new Vector3(
            localScale.x,
            localScale.y,
            1f
        );

        SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
        renderer.sprite = weaponSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private void SpawnAfterimage()
    {
        if (weaponRenderer == null || weaponSprite == null)
            return;

        GameObject afterimage = new GameObject("ThrownWeaponTrail");
        afterimage.transform.position = transform.position;
        afterimage.transform.rotation = transform.rotation;
        afterimage.transform.localScale = new Vector3(2.5f, 0.24f, 1f);

        SpriteRenderer renderer = afterimage.AddComponent<SpriteRenderer>();
        renderer.sprite = weaponSprite;
        renderer.color = new Color(1f, 0.25f, 0.05f, 0.42f);
        renderer.sortingOrder = 44;

        Destroy(afterimage, 0.22f);
        StartCoroutine(FadeTrail(afterimage, renderer));
    }

    private IEnumerator FadeTrail(
        GameObject trail,
        SpriteRenderer renderer
    )
    {
        float duration = 0.14f;
        float elapsed = 0f;
        Color startColor = renderer.color;

        while (trail != null && elapsed < duration)
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            Color current = startColor;
            current.a = startColor.a * (1f - progress);
            renderer.color = current;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (trail != null)
            Destroy(trail);
    }
}
