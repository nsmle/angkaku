using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform playerTransform;

    [Header("Camera Settings")]
    [SerializeField] private float fixedDistance = 35f;
    [SerializeField] private float height = 25f;
    [SerializeField] private float positionSmoothSpeed = 10f;
    [SerializeField] private float lookTargetHeight = 20f;
    [SerializeField] private float lookAheadDistance = 4f;
    [SerializeField] private float downwardTilt = 0f;
    [SerializeField] private float rotationSmoothSpeed = 12f;
    [SerializeField] private bool followVerticalMotion = false;

    private float lockedPlayerY;
    private bool hasLockedY;

    private void Start()
    {
        if (playerTransform == null) return;

        lockedPlayerY = playerTransform.position.y;
        hasLockedY = true;
    }

    private void LateUpdate()
    {
        if (playerTransform == null) return;

        if (!hasLockedY) {
            lockedPlayerY = playerTransform.position.y;
            hasLockedY = true;
        }

        Vector3 targetPosition = playerTransform.position - playerTransform.forward * fixedDistance;
        float trackedY = followVerticalMotion ? playerTransform.position.y : lockedPlayerY;
        targetPosition.y = trackedY + height;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            positionSmoothSpeed * Time.deltaTime
        );

        Vector3 lookTarget = playerTransform.position
                             + playerTransform.forward * lookAheadDistance;
        lookTarget.y = trackedY + lookTargetHeight;

        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
        targetRotation *= Quaternion.Euler(downwardTilt, 0f, 0f);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSmoothSpeed * Time.deltaTime
        );
    }
}