using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGun : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform gunPivot;
    [SerializeField] private Transform gunMuzzle;
    [SerializeField] private GunBullet bulletPrefab;

    [Header("¸úËæ")]
    [SerializeField]
    private Vector2 followOffset =
        new Vector2(0f, 0f);

    [Header("³ÖÇ¹×ËÊÆ")]
    [SerializeField]
    private Vector2 holdingLocalPosition =
        new Vector2(0f, -1f);

    [SerializeField] private float holdingAngle = -25f;

    [Header("Ãé×¼×ËÊÆ")]
    [SerializeField]
    private Vector2 aimingLocalPosition =
        new Vector2(0f, 1f);

    [SerializeField] private float aimingAngle = 18f;

    [Min(0.1f)]
    [SerializeField] private float aimTransitionSpeed = 12f;

    [Header("Éä»÷")]
    [Min(0.01f)]
    [SerializeField] private float fireCooldown = 0.2f;

    private float facingDirection = 1f;
    private float lastPlayerX;
    private float nextFireTime;

    private void Awake()
    {
        if (player != null)
            lastPlayerX = player.position.x;
    }

    private void Update()
    {
        bool isAiming = Mouse.current != null &&
                        Mouse.current.rightButton.isPressed;

        if (isAiming &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryFire();
        }
    }

    private void LateUpdate()
    {
        if (player == null || gunPivot == null)
            return;

        UpdateFacingDirection();

        transform.position = new Vector3(
            player.position.x + followOffset.x,
            player.position.y + followOffset.y,
            transform.position.z
        );

        transform.localScale = new Vector3(
            facingDirection,
            1f,
            1f
        );

        bool isAiming = Mouse.current != null &&
                        Mouse.current.rightButton.isPressed;

        Vector2 targetPosition = isAiming
            ? aimingLocalPosition
            : holdingLocalPosition;

        float targetAngle = isAiming
            ? aimingAngle
            : holdingAngle;

        gunPivot.localPosition = Vector3.Lerp(
            gunPivot.localPosition,
            targetPosition,
            Time.deltaTime * aimTransitionSpeed
        );

        gunPivot.localRotation = Quaternion.Lerp(
            gunPivot.localRotation,
            Quaternion.Euler(0f, 0f, targetAngle),
            Time.deltaTime * aimTransitionSpeed
        );
    }

    private void UpdateFacingDirection()
    {
        float currentPlayerX = player.position.x;

        if (Mathf.Abs(currentPlayerX - lastPlayerX) > 0.001f)
        {
            facingDirection =
                Mathf.Sign(currentPlayerX - lastPlayerX);
        }

        lastPlayerX = currentPlayerX;
    }

    private void TryFire()
    {
        if (bulletPrefab == null ||
            gunMuzzle == null ||
            Time.time < nextFireTime)
        {
            return;
        }

        nextFireTime = Time.time + fireCooldown;

        GunBullet bullet = Instantiate(
            bulletPrefab,
            gunMuzzle.position,
            Quaternion.identity
        );

        bullet.Launch(gunMuzzle.right);
    }
}