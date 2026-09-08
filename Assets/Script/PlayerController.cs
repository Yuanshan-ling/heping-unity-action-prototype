using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 16f;
    [SerializeField] private float jumpSpeed = 35f;
    [SerializeField] private float flySpeed = 6f;
    [SerializeField] private float fallGravityMultiplier = 3f;
    [SerializeField] private float apexGravityMultiplier = 2f;
    [SerializeField] private float apexSpeedThreshold = 3f;
    [SerializeField] private float ropeClimbSpeed = 10f;
    [SerializeField] private float ropeExitSpeed = 10f;

    private Rigidbody2D body;
    private Collider2D playerCollider;
    private float normalGravity;
    private float horizontalInput;
    private bool isGrounded;

    private RopeZone currentRope;
    private Transform ropeExitPoint;
    private bool isClimbing;
    private bool isExitingRope;
    private bool climbInput;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        normalGravity = body.gravityScale;
    }

    private void Update()
    {
        horizontalInput = 0f;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (isClimbing || isExitingRope)
        {
            if (keyboard.sKey.wasPressedThisFrame)
            {
                StopRopeClimb();
            }
            else if (isClimbing)
            {
                climbInput = keyboard.eKey.isPressed;
            }

            return;
        }

        if (keyboard.aKey.isPressed)
            horizontalInput = -1f;

        if (keyboard.dKey.isPressed)
            horizontalInput = 1f;

        if (keyboard.spaceKey.wasPressedThisFrame && isGrounded)
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpSpeed);

        if (keyboard.wKey.isPressed)
        {
            body.gravityScale = 0f;
            body.linearVelocity = new Vector2(body.linearVelocity.x, flySpeed);
        }

        if (keyboard.sKey.wasPressedThisFrame)
            body.gravityScale = normalGravity;

        if (currentRope != null &&
            currentRope.ExitPoint != null &&
            keyboard.eKey.wasPressedThisFrame)
        {
            BeginRopeClimb();
        }
    }

    private void FixedUpdate()
    {
        isGrounded = false;

        if (isExitingRope)
        {
            MoveToRopeExit();
            return;
        }

        if (isClimbing)
        {
            MoveOnRope();
            return;
        }

        float verticalSpeed = body.linearVelocity.y;
        float gravityMultiplier = 1f;

        if (verticalSpeed < 0f)
        {
            gravityMultiplier = fallGravityMultiplier;
        }
        else if (verticalSpeed < apexSpeedThreshold)
        {
            gravityMultiplier = apexGravityMultiplier;
        }

        if (gravityMultiplier > 1f && body.gravityScale > 0f)
        {
            verticalSpeed += Physics2D.gravity.y
                * body.gravityScale
                * (gravityMultiplier - 1f)
                * Time.fixedDeltaTime;
        }

        body.linearVelocity = new Vector2(
            horizontalInput * moveSpeed,
            verticalSpeed
        );
    }

    private void BeginRopeClimb()
    {
        ropeExitPoint = currentRope.ExitPoint;
        isClimbing = true;
        climbInput = true;

        body.gravityScale = 0f;
        body.linearVelocity = Vector2.zero;
        body.position = new Vector2(
            currentRope.transform.position.x,
            body.position.y
        );
    }

    private void MoveOnRope()
    {
        if (ropeExitPoint == null)
        {
            StopRopeClimb();
            return;
        }

        body.linearVelocity = Vector2.zero;

        if (!climbInput)
            return;

        Vector2 nextPosition = body.position +
            Vector2.up * ropeClimbSpeed * Time.fixedDeltaTime;

        if (nextPosition.y >= ropeExitPoint.position.y)
        {
            isClimbing = false;
            isExitingRope = true;
            playerCollider.enabled = false;
            return;
        }

        body.MovePosition(nextPosition);
    }

    private void MoveToRopeExit()
    {
        if (ropeExitPoint == null)
        {
            StopRopeClimb();
            return;
        }

        Vector2 targetPosition = new Vector2(
            ropeExitPoint.position.x,
            ropeExitPoint.position.y
        );

        Vector2 nextPosition = Vector2.MoveTowards(
            body.position,
            targetPosition,
            ropeExitSpeed * Time.fixedDeltaTime
        );

        body.MovePosition(nextPosition);

        if (Vector2.Distance(nextPosition, targetPosition) < 0.01f)
        {
            body.position = targetPosition;
            body.gravityScale = normalGravity;
            body.linearVelocity = Vector2.zero;
            playerCollider.enabled = true;

            isExitingRope = false;
            currentRope = null;
            ropeExitPoint = null;
        }
    }

    private void StopRopeClimb()
    {
        isClimbing = false;
        isExitingRope = false;
        climbInput = false;
        playerCollider.enabled = true;

        body.gravityScale = normalGravity;
        body.linearVelocity = Vector2.zero;

        currentRope = null;
        ropeExitPoint = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        RopeZone rope = other.GetComponent<RopeZone>();

        if (rope != null)
            currentRope = rope;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        RopeZone rope = other.GetComponent<RopeZone>();

        if (rope == currentRope && !isClimbing && !isExitingRope)
            currentRope = null;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                return;
            }
        }
    }
}