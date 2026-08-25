using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private CharacterController controller;
    private Animator animator;

    [Header("Forward Movement")]
    [SerializeField] private float forwardSpeed = 45f;

    [Header("Lane Movement")]
    [SerializeField] private float laneDistance = 6f;
    [SerializeField] private float laneChangeSpeed = 14f;
    [SerializeField] private float laneChangeDuration = 0.5f;
    [SerializeField] private float laneSnapThreshold = 0.05f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 35f;
    [SerializeField] private float gravity = -50f;
    [SerializeField] private float groundedStickForce = -2f;
    [SerializeField] private float riseGravityMultiplier = 1.15f;
    [SerializeField] private float fallGravityMultiplier = 3.1f;
    [SerializeField] private float jumpCutGravityMultiplier = 3.2f;
    [SerializeField] private float extraFallAcceleration = 25f;
    [SerializeField] private float maxFallSpeed = -90f;

    [Header("Slide")]
    [SerializeField] private float slideDuration = 0.1f;
    [SerializeField] private float slideHeightMultiplier = 0.5f;

    private int currentLane = 1;
    private float verticalVelocity;
    private float defaultHeight;
    private Vector3 defaultCenter;
    private Vector3 laneCenterPoint;
    private float slideTimer;
    private bool isSliding;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        defaultHeight = controller.height;
        defaultCenter = controller.center;
        laneCenterPoint = transform.position;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    private void Update()
    {
        ReadKeyboardInput();

        UpdateSlideState();
        ApplyGravity();
        MoveRunner();
    }

    private void ReadKeyboardInput()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.aKey.wasPressedThisFrame) {
            MoveLane(-1);
        } else if (Keyboard.current.dKey.wasPressedThisFrame) {
            MoveLane(1);
        }

        if (Keyboard.current.zKey.wasPressedThisFrame) {
            TryTurn(-1);
        } else if (Keyboard.current.cKey.wasPressedThisFrame) {
            TryTurn(1);
        }

        if (Keyboard.current.wKey.wasPressedThisFrame) {
            forwardSpeed += 15f;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame) {
            TryJump();
        }

        if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame) {
            TrySlide();
        }
    }

    private void MoveLane(int direction)
    {
        currentLane = Mathf.Clamp(currentLane + direction, 0, 2);
    }

    private void TryJump()
    {
        if (!controller.isGrounded || isSliding) return;
        animator.SetBool("isJump", true);
        verticalVelocity = jumpForce;
    }

    private void TrySlide()
    {
        if (!controller.isGrounded || isSliding) return;

        isSliding = true;
        slideTimer = slideDuration;

        controller.height = defaultHeight * slideHeightMultiplier;
        controller.center = new Vector3(defaultCenter.x, controller.height * 0.5f, defaultCenter.z);
    }

    private void UpdateSlideState()
    {
        if (!isSliding) return;

        slideTimer -= Time.deltaTime;
        if (slideTimer > 0f) return;

        isSliding = false;
        controller.height = defaultHeight;
        controller.center = defaultCenter;
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f) {
            verticalVelocity = groundedStickForce;
            animator.SetBool("isJump", false);
            return;
        }

        float gravityMultiplier;
        bool rising = verticalVelocity > 0f;

        if (rising) {
            bool jumpHeld = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
            gravityMultiplier = jumpHeld ? riseGravityMultiplier : jumpCutGravityMultiplier;
        } else {
            gravityMultiplier = fallGravityMultiplier;
        }

        verticalVelocity += gravity * gravityMultiplier * Time.deltaTime;
        if (!rising) {
            verticalVelocity -= extraFallAcceleration * Time.deltaTime;
        }

        verticalVelocity = Mathf.Max(verticalVelocity, maxFallSpeed);
    }

    private void MoveRunner()
    {
        float laneOffset = (currentLane - 1) * laneDistance;
        Vector3 targetLanePosition = laneCenterPoint + transform.right * laneOffset;
        float lateralDelta = Vector3.Dot(targetLanePosition - transform.position, transform.right);
        float effectiveLaneSpeed = Mathf.Max(laneChangeSpeed, laneDistance / Mathf.Max(0.01f, laneChangeDuration));
        float lateralStep = Mathf.MoveTowards(0f, lateralDelta, effectiveLaneSpeed * Time.deltaTime);

        if (Mathf.Abs(lateralDelta) <= laneSnapThreshold) {
            lateralStep = lateralDelta;
        }

        Vector3 movement =
            transform.right * lateralStep +
            Vector3.up * (verticalVelocity * Time.deltaTime) +
            transform.forward * (forwardSpeed * Time.deltaTime);

        CollisionFlags flags = controller.Move(movement);
        if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0f) {
            verticalVelocity = groundedStickForce;
        }
    }

    private void TryTurn(int direction)
    {
        Quaternion turnRotation = Quaternion.Euler(0f, direction * 90f, 0f);
        transform.rotation = turnRotation * transform.rotation;

        float laneOffset = (currentLane - 1) * laneDistance;
        laneCenterPoint = transform.position - transform.right * laneOffset;
    }
}