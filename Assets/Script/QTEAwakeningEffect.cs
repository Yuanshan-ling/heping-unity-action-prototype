using System.Collections;
using UnityEngine;

public class QTEAwakeningEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject secondPlayer;
    [SerializeField] private GameObject secondAttackRig;
    [SerializeField] private Transform secondAttackHand;
    [SerializeField] private Sprite effectSprite;
    [SerializeField] private PlayerAirCombat playerAirCombat;

    [Header("Second Player")]
    [SerializeField] private float secondPlayerDistance = 4f;
    [SerializeField] private float secondPlayerHeightOffset = 0f;
    [SerializeField] private bool secondPlayerFacesEnemy = true;

    [SerializeField]
    private Vector3 raisedHandLocalPosition =
        new Vector3(1.2f, 1.5f, 0f);

    [Header("Green Beam")]
    [SerializeField] private float beamWidth = 1.2f;
    [SerializeField] private float beamHeight = 8f;
    [SerializeField] private float beamDuration = 0.6f;
    [SerializeField]
    private Color beamColor =
    new Color(0.1f, 1f, 0.3f, 0.9f);



    [Header("Green Flash")]
    [SerializeField] private float greenFlashDuration = 0.12f;

    [Header("Wings")]
    [SerializeField]
    private Color wingColor =
    new Color(0.3f, 1f, 0.5f, 1f);
    [SerializeField] private float wingSideOffset = 1.2f;
    [SerializeField] private float wingYOffset = 0.3f;
    [SerializeField]
    private Vector2 wingSize =
        new Vector2(0.5f, 1.6f);

    [SerializeField] private float wingFlapDuration = 0.35f;
    [SerializeField] private float wingStartAngle = 35f;
    [SerializeField] private float wingEndAngle = 85f;

    public IEnumerator Play(Transform enemy)
    {
        if (player == null || enemy == null)
            yield break;

        float awayDirection = Mathf.Sign(
            player.position.x - enemy.position.x
        );

        if (awayDirection == 0f)
            awayDirection = 1f;

        Vector3 secondPosition = player.position +
            Vector3.right * awayDirection *
            secondPlayerDistance +
            Vector3.up * secondPlayerHeightOffset;

        if (secondPlayer != null)
        {
            secondPlayer.transform.position = secondPosition;
            secondPlayer.SetActive(true);

            Rigidbody2D secondBody =
                secondPlayer.GetComponent<Rigidbody2D>();

            if (secondBody != null)
                secondBody.linearVelocity = Vector2.zero;
        }

        if (secondAttackRig != null)
            secondAttackRig.SetActive(true);

        yield return null;

        if (secondAttackRig != null &&
    secondPlayer != null)
        {
            float faceDirection = Mathf.Sign(
                enemy.position.x -
                secondPlayer.transform.position.x
            );

            if (!secondPlayerFacesEnemy)
                faceDirection *= -1f;

            Vector3 rigScale =
                secondAttackRig.transform.localScale;

            rigScale.x = Mathf.Abs(rigScale.x) *
                faceDirection;

            secondAttackRig.transform.localScale = rigScale;

            SpriteRenderer secondRenderer =
                secondPlayer.GetComponent<SpriteRenderer>();

            if (secondRenderer != null)
                secondRenderer.flipX = faceDirection < 0f;
        }

        if (secondAttackHand != null)
            secondAttackHand.localPosition =
                raisedHandLocalPosition;

        SpriteRenderer playerRenderer =
            player.GetComponent<SpriteRenderer>();

        Sprite visualSprite = effectSprite != null
            ? effectSprite
            : playerRenderer.sprite;

        float footY = playerRenderer != null
            ? playerRenderer.bounds.min.y
            : player.position.y;

        GameObject beam = CreateVisual(
            "GreenBeam",
            visualSprite,
            new Vector3(
                player.position.x,
                footY + beamHeight * 0.5f,
                player.position.z
            ),
            new Vector3(beamWidth, beamHeight, 1f),
            beamColor
        );
        StartCoroutine(FadeAndDestroy(beam, beamDuration));

        // 等光柱完全结束后，主角才绿闪
        yield return new WaitForSeconds(beamDuration);

        if (playerRenderer != null)
            yield return StartCoroutine(FlashGreen(playerRenderer));

        // 绿闪结束后，再扇动一次翅膀
        yield return StartCoroutine(FlapWings(visualSprite));
        StartCoroutine(UnlockFlightAfterEffects());
    }

    private IEnumerator FlashGreen(SpriteRenderer renderer)
    {
        Color originalColor = renderer.color;
        renderer.color = new Color(0.2f, 1f, 0.3f, 1f);

        yield return new WaitForSeconds(greenFlashDuration);

        renderer.color = originalColor;
    }

    private IEnumerator FlapWings(Sprite sprite)
    {
        Vector3 center = player.position +
            Vector3.up * wingYOffset;

        GameObject leftWing = CreateVisual(
            "LeftWing",
            sprite,
            center + Vector3.left * wingSideOffset,
            new Vector3(wingSize.x, wingSize.y, 1f),
            wingColor
        );

        GameObject rightWing = CreateVisual(
            "RightWing",
            sprite,
            center + Vector3.right * wingSideOffset,
            new Vector3(wingSize.x, wingSize.y, 1f),
            wingColor
        );

        float time = 0f;

        while (time < wingFlapDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(
                time / wingFlapDuration
            );

            leftWing.transform.rotation = Quaternion.Euler(
                0f,
                0f,
                -Mathf.Lerp(wingStartAngle, wingEndAngle, t)
            );

            rightWing.transform.rotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Lerp(wingStartAngle, wingEndAngle, t)
            );

            yield return null;
        }

        Destroy(leftWing);
        Destroy(rightWing);
    }

    private GameObject CreateVisual(
        string objectName,
        Sprite sprite,
        Vector3 position,
        Vector3 scale,
        Color color
    )
    {
        GameObject visual = new GameObject(
            objectName,
            typeof(SpriteRenderer)
        );

        SpriteRenderer renderer =
            visual.GetComponent<SpriteRenderer>();

        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 20;

        visual.transform.position = position;
        visual.transform.localScale = scale;

        return visual;
    }

    private IEnumerator FadeAndDestroy(
        GameObject visual,
        float duration
    )
    {
        SpriteRenderer renderer =
            visual.GetComponent<SpriteRenderer>();

        Color color = renderer.color;
        float startAlpha = color.a;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            color.a = Mathf.Lerp(startAlpha, 0f, time / duration);

            renderer.color = color;
            yield return null;
        }

        Destroy(visual);
    }
    private IEnumerator UnlockFlightAfterEffects()
    {
        // 等 OpeningQTE 完成它自己的收尾，
        // 防止它重新启用旧的 PlayerController。
        yield return null;

        if (playerAirCombat != null)
            playerAirCombat.UnlockFlight();
    }
}