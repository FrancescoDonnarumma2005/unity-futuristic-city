using UnityEngine;

/// <summary>
/// Loops the scale of a GameObject between its initial value and one scaled by a multiplier.
/// </summary>
public class ScaleLoop : MonoBehaviour
{
    [SerializeField, Min(0f)] private float multiplier = 1.2f;
    [SerializeField, Min(0.01f)] private float secondsToPeak = 0.5f;
    [SerializeField] private bool useUnscaledTime;

    private Vector3 baseScale;

    private void OnEnable()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        if (secondsToPeak <= 0f || multiplier <= 0f)
        {
            return;
        }

        float timeSource = useUnscaledTime ? Time.unscaledTime : Time.time;
        float normalized = Mathf.PingPong(timeSource / secondsToPeak, 1f);
        Vector3 targetScale = baseScale * multiplier;
        transform.localScale = Vector3.Lerp(baseScale, targetScale, normalized);
    }

    /// <summary>
    /// Call this if you change the object's neutral scale at runtime and want to restart the loop from there.
    /// </summary>
    public void Recalibrate()
    {
        baseScale = transform.localScale;
    }
}
