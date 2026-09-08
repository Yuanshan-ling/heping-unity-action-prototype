using UnityEngine;
using UnityEngine.InputSystem;

public class CannonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cannonMuzzle;
    [SerializeField] private CannonBall cannonBallPrefab;

    [Header("·¢Éä²ÎÊý")]
    [SerializeField] private float launchSpeed = 38f;

    [Min(0.1f)]
    [SerializeField] private float fireCooldown = 1.5f;

    private bool playerNearby;
    private float nextFireTime;

    private void Update()
    {
        if (playerNearby &&
            Keyboard.current != null &&
            Keyboard.current.fKey.wasPressedThisFrame)
        {
            TryFire();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
            playerNearby = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
            playerNearby = false;
    }

    private void TryFire()
    {
        if (cannonMuzzle == null ||
            cannonBallPrefab == null ||
            Time.time < nextFireTime)
        {
            return;
        }

        nextFireTime = Time.time + fireCooldown;

        CannonBall cannonBall = Instantiate(
            cannonBallPrefab,
            cannonMuzzle.position,
            Quaternion.identity
        );

        cannonBall.Launch(
            (Vector2)cannonMuzzle.right * launchSpeed
        );
    }
}