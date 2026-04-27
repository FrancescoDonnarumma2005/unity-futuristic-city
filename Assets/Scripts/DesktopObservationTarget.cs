using UnityEngine;

/// <summary>
/// Marks an object as observable from the desktop rig and exposes an anchor Transform that represents where the desktop camera should move.
/// </summary>
public class DesktopObservationTarget : MonoBehaviour
{
    [SerializeField] private Transform observationAnchor;
    [SerializeField] private string displayName;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;

    public Transform ObservationAnchor => observationAnchor != null ? observationAnchor : transform;
}
