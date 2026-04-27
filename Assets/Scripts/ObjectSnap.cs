using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Handles visual hints while the object is grabbed and snaps it onto a target when released inside the snap zone.
/// Supports both custom target anchors and returning to the original starting position.
/// </summary>
public class ObjectSnap : MonoBehaviour
{
    [Header("Visual hints")]
    [SerializeField] private GameObject objectIndicator;    // Small sphere attached to the grabbable
    [SerializeField] private GameObject targetIndicator;    // Sphere that shows the target placement
    [SerializeField] private string objectDisplayName;
    [SerializeField] private string snapTargetDisplayName;

    [Header("Snap settings")]
    [SerializeField] private Transform snapAnchor;          // Desired final transform
    [SerializeField] private Collider snapTrigger;          // Trigger collider used to detect when we're inside the snap area

    [Header("Return to Original Position Settings")]
    [SerializeField, Tooltip("Se abilitato, l'oggetto scatterà nella sua posizione iniziale se rilasciato nelle vicinanze o se non ci sono target custom validi.")]
    private bool canReturnToOrigin = true;
    [SerializeField, Range(0.01f, 1f), Tooltip("Raggio di tolleranza per il riposizionamento all'origine.")]
    private float originSnapThreshold = 0.3f;

    [Header("Proximity resistance")]
    [SerializeField] private bool enableProximityResistance = true;
    [SerializeField, Range(0.01f, 0.5f)] private float resistanceRadius = 0.2f;
    [SerializeField, Range(1f, 15f)] private float maxDragMultiplier = 5f;
    [SerializeField, Range(1f, 15f)] private float maxAngularDragMultiplier = 4f;
    [SerializeField, Range(0f, 20f)] private float resistanceDamping = 6f;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable;
    private Rigidbody rb;

    private bool isGrabbed;
    private bool isInsideSnapZone;
    private bool isSnapped;
    private bool isApplyingResistance;
    private float defaultDrag;
    private float defaultAngularDrag;
    private bool dragCached;

    // Zero-allocation cache per il ritorno all'origine
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float sqrOriginSnapThreshold;

    public string ObjectDisplayName => string.IsNullOrWhiteSpace(objectDisplayName) ? gameObject.name : objectDisplayName;

    public string SnapTargetDisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(snapTargetDisplayName))
            {
                return snapTargetDisplayName;
            }

            return snapAnchor != null ? snapAnchor.name : "la sede";
        }
    }

    public bool IsInsideSnapZone => isInsideSnapZone;

    public bool IsSnapped => isSnapped;

    private void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            defaultDrag = rb.linearDamping;
            defaultAngularDrag = rb.angularDamping;
            dragCached = true;
        }

        if (snapTrigger == null && targetIndicator != null)
        {
            snapTrigger = targetIndicator.GetComponent<Collider>();
        }

        // Cache della posa iniziale e calcolo ottimizzato della soglia
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        sqrOriginSnapThreshold = originSnapThreshold * originSnapThreshold;
    }

    private void OnEnable()
    {
        SubscribeToGrabEvents(true);
        InitializeIndicators();
    }

    private void OnDisable()
    {
        SubscribeToGrabEvents(false);
        ResetProximityResistance();
    }

    private void InitializeIndicators()
    {
        if (isSnapped)
        {
            SetIndicator(objectIndicator, false);
            SetIndicator(targetIndicator, false);
            return;
        }

        SetIndicator(objectIndicator, true);
        SetIndicator(targetIndicator, false);
    }

    private void SubscribeToGrabEvents(bool subscribe)
    {
        if (interactable == null)
        {
            return;
        }

        if (subscribe)
        {
            interactable.selectEntered.AddListener(OnSelectEntered);
            interactable.selectExited.AddListener(OnSelectExited);
        }
        else
        {
            interactable.selectEntered.RemoveListener(OnSelectEntered);
            interactable.selectExited.RemoveListener(OnSelectExited);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args) => HandleGrabbed();

    private void OnSelectExited(SelectExitEventArgs args) => HandleReleased();

    /// <summary>
    /// Allows non-XR grabbers (mouse/keyboard) to reuse the same logic used by XR interactors.
    /// </summary>
    public void NotifyGrabbed() => HandleGrabbed();

    /// <summary>
    /// Call this when a non-XR grabber drops the object.
    /// </summary>
    public void NotifyReleased() => HandleReleased();

    private void FixedUpdate()
    {
        UpdateProximityResistance();
    }

    private void HandleGrabbed()
    {
        if (isSnapped)
        {
            // Allow the player to move already snapped objects again.
            isSnapped = false;
        }

        isGrabbed = true;
        SetIndicator(objectIndicator, false);
        if (!isSnapped && snapAnchor != null) // Mostra l'indicatore target solo se esiste un'ancora custom
        {
            SetIndicator(targetIndicator, true);
        }
        UpdateProximityResistance(true);
    }

    private void HandleReleased()
    {
        isGrabbed = false;

        // 1. Priorità: Snap ad un'ancora personalizzata se siamo nell'area trigger
        if (isInsideSnapZone && snapAnchor != null)
        {
            SnapObjectIntoPlace();
            return;
        }

        // 2. Fallback: Ritorno all'origine se permesso e vicino
        if (canReturnToOrigin && IsCloseToOrigin())
        {
            SnapToOrigin();
            return;
        }

        // 3. Caduta Libera
        if (!isSnapped)
        {
            SetIndicator(objectIndicator, true);
            SetIndicator(targetIndicator, false);
        }

        ResetProximityResistance();
    }

    private void SnapObjectIntoPlace()
    {
        transform.SetPositionAndRotation(snapAnchor.position, snapAnchor.rotation);
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        isSnapped = true;
        SetIndicator(objectIndicator, false);
        SetIndicator(targetIndicator, false);
        ResetProximityResistance();
    }

    /// <summary>
    /// Controlla matematicamente se l'oggetto è vicino alla sua posizione iniziale.
    /// </summary>
    private bool IsCloseToOrigin()
    {
        return (transform.position - originalPosition).sqrMagnitude <= sqrOriginSnapThreshold;
    }

    /// <summary>
    /// Riporta l'oggetto alla posa iniziale azzerando le forze fisiche.
    /// </summary>
    private void SnapToOrigin()
    {
        transform.SetPositionAndRotation(originalPosition, originalRotation);
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        isSnapped = true;
        SetIndicator(objectIndicator, false);
        SetIndicator(targetIndicator, false); // Sicurezza extra, nascondiamo tutto
        ResetProximityResistance();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (snapTrigger == null || other != snapTrigger || isSnapped)
        {
            return;
        }

        isInsideSnapZone = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (snapTrigger == null || other != snapTrigger || isSnapped)
        {
            return;
        }

        isInsideSnapZone = false;
    }

    private static void SetIndicator(GameObject indicator, bool enabled)
    {
        if (indicator != null && indicator.activeSelf != enabled)
        {
            indicator.SetActive(enabled);
        }
    }

    private void UpdateProximityResistance(bool forceRecompute = false)
    {
        if (!enableProximityResistance || rb == null || !dragCached)
        {
            return;
        }

        // Determiniamo verso quale punto applicare la resistenza magnetica
        Vector3 resistanceTargetPoint;
        if (snapAnchor != null)
        {
            resistanceTargetPoint = snapAnchor.position;
        }
        else if (canReturnToOrigin)
        {
            resistanceTargetPoint = originalPosition;
        }
        else
        {
            return; // Nessun target valido per la resistenza
        }

        if (!isGrabbed || isSnapped)
        {
            if (isApplyingResistance || forceRecompute)
            {
                ResetProximityResistance();
            }
            return;
        }

        float distance = Vector3.Distance(transform.position, resistanceTargetPoint);
        if (distance > resistanceRadius)
        {
            if (isApplyingResistance || forceRecompute)
            {
                ResetProximityResistance();
            }
            return;
        }

        float falloff = 1f - Mathf.Clamp01(distance / resistanceRadius);
        float dragMultiplier = Mathf.Lerp(1f, maxDragMultiplier, falloff);
        float angularDragMultiplier = Mathf.Lerp(1f, maxAngularDragMultiplier, falloff);

        rb.linearDamping = defaultDrag * dragMultiplier;
        rb.angularDamping = defaultAngularDrag * angularDragMultiplier;

        if (resistanceDamping > 0f)
        {
            Vector3 velocity = rb.linearVelocity;
            Vector3 counterAcceleration = -velocity * resistanceDamping * falloff;
            rb.AddForce(counterAcceleration, ForceMode.Acceleration);
        }

        isApplyingResistance = true;
    }

    private void ResetProximityResistance()
    {
        if (!dragCached || rb == null)
        {
            return;
        }

        rb.linearDamping = defaultDrag;
        rb.angularDamping = defaultAngularDrag;
        isApplyingResistance = false;
    }
}