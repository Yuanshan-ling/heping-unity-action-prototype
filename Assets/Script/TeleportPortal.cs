using UnityEngine;

public class TeleportPortal : MonoBehaviour
{
    [SerializeField] private Transform destination;
    [SerializeField] private Vector2 exitOffset = new Vector2(2f, 0f);
    [SerializeField] private TeleportMerchantSequence merchantSequence;


    private float lockedUntil;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.time < lockedUntil || destination == null)
            return;

        Rigidbody2D playerBody = other.attachedRigidbody;

        if (playerBody == null ||
            playerBody.GetComponent<PlayerController>() == null)
            return;

        playerBody.position = (Vector2)destination.position + exitOffset;
        playerBody.linearVelocity = Vector2.zero;

        TeleportPortal destinationPortal =
            destination.GetComponent<TeleportPortal>();

        if (destinationPortal != null)
            destinationPortal.lockedUntil = Time.time + 0.2f;
        if (merchantSequence != null)
            merchantSequence.BeginSequence(playerBody);
    }
}