using UnityEngine;

public class CameraAxisFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private bool keepInitialOffset = true;

    [Header("Assi da seguire")]
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = true;
    [SerializeField] private bool followZ = true;

    [Header("Movimento")]
    [Min(0f)]
    [SerializeField] private float smoothTime = 0.12f;

    private Vector3 offset;
    private Vector3 velocity;

    private void Start()
    {
        CacheOffset();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + (keepInitialOffset ? offset : Vector3.zero);
        Vector3 currentPosition = transform.position;

        Vector3 finalPosition = new Vector3(
            followX ? desiredPosition.x : currentPosition.x,
            followY ? desiredPosition.y : currentPosition.y,
            followZ ? desiredPosition.z : currentPosition.z
        );

        if (smoothTime <= 0f)
        {
            transform.position = finalPosition;
            return;
        }

        transform.position = Vector3.SmoothDamp(currentPosition, finalPosition, ref velocity, smoothTime);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        CacheOffset();
    }

    private void CacheOffset()
    {
        if (target == null)
        {
            return;
        }

        offset = transform.position - target.position;
    }
}
