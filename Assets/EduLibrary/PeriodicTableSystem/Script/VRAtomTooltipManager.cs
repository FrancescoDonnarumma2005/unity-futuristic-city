using UnityEngine;
using TMPro;

public class VRAtomTooltipManager : MonoBehaviour
{
    [Header("Controller Transforms")]
    [SerializeField] private Transform rightControllerTransform;
    [SerializeField] private Transform leftControllerTransform;

    [Header("UI References")]
    [SerializeField] private GameObject calloutContainer; 
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private RectTransform lineRect; 
    [SerializeField] private RectTransform dotRect;

    [Header("UX Visiva (Reticolo 3D)")]
    [Tooltip("Trascina qui il piccolo oggetto 3D (es. sfera bianca senza collider) che funge da puntatore")]
    public Transform reticleTransform;

    [Header("Settings Ologramma & Fisica")]
    [Tooltip("Lunghezza massima del raggio laser in metri")]
    public float rayLength = 50f; 

    [Tooltip("Spessore del raggio invisibile (in metri). 0.02 = tubo di 2cm")]
    public float rayThickness = 0.02f;
    
    [Tooltip("Seleziona qui il Layer assegnato a Protoni, Neutroni ed Elettroni")]
    public LayerMask particelleLayer = ~0;

    [Space(10)]
    [SerializeField] private Vector2 labelOffset = new Vector2(150f, 100f); 
    [SerializeField] private float padding = 10f; 

    private Transform currentTarget;
    private Transform mainCamera;
    
    // Nuova variabile per tracciare il punto esatto di impatto sulla superficie
    private Vector3 currentHitPoint;

    private void Start()
    {
        if (calloutContainer != null) calloutContainer.SetActive(false);
        if (lineRect != null) lineRect.pivot = new Vector2(0f, 0.5f);
        if (reticleTransform != null) reticleTransform.gameObject.SetActive(false);
        
        mainCamera = Camera.main.transform;
    }

    private void Update()
    {
        if (PeriodicTableManager.Instance != null && PeriodicTableManager.Instance.IsInputBlocked())
        {
            HideTooltip();
            return;
        }

        CheckRayHover();

        if (currentTarget != null && calloutContainer.activeSelf)
        {
            UpdateCalloutPosition();
        }
    }

    private void CheckRayHover()
    {
        if (TryGetHitParticle(rightControllerTransform, out RaycastHit rightHit, out AtomParticle rightParticle))
        {
            HandleHover(rightHit, rightParticle.particleType);
        }
        else if (TryGetHitParticle(leftControllerTransform, out RaycastHit leftHit, out AtomParticle leftParticle))
        {
            HandleHover(leftHit, leftParticle.particleType);
        }
        else
        {
            HideTooltip();
        }
    }

    private bool TryGetHitParticle(Transform controller, out RaycastHit hit, out AtomParticle particle)
    {
        hit = new RaycastHit();
        particle = null;
        
        if (controller == null) return false;

        if (Physics.SphereCast(controller.position, rayThickness, controller.forward, out hit, rayLength, particelleLayer))
        {
            if (hit.collider.TryGetComponent<AtomParticle>(out particle))
            {
                return true;
            }
        }
        return false;
    }

    private void HandleHover(RaycastHit hit, string text)
    {
        Transform targetTransform = hit.transform;

        // 1. GESTIONE PUNTATORE 3D E PUNTO DI ANCORAGGIO
        Vector3 surfacePoint = hit.point + (hit.normal * 0.002f);
        
        if (reticleTransform != null)
        {
            if (!reticleTransform.gameObject.activeSelf) 
                reticleTransform.gameObject.SetActive(true);

            reticleTransform.position = surfacePoint;
            reticleTransform.rotation = Quaternion.LookRotation(hit.normal);
        }

        // Memorizziamo il punto sulla superficie da passare al CalloutContainer
        currentHitPoint = surfacePoint;

        // 2. GESTIONE TOOLTIP UI
        if (currentTarget != targetTransform)
        {
            currentTarget = targetTransform;
            tooltipText.text = text;
            calloutContainer.SetActive(true);
        }
    }

    private void HideTooltip()
    {
        currentTarget = null;
        
        if (calloutContainer != null && calloutContainer.activeSelf)
        {
            calloutContainer.SetActive(false);
        }

        if (reticleTransform != null && reticleTransform.gameObject.activeSelf)
        {
            reticleTransform.gameObject.SetActive(false);
        }
    }

    private void UpdateCalloutPosition()
    {
        // Modifica cruciale: Ancoriamo l'UI al punto sulla superficie esterna, non più al centro della mesh
        calloutContainer.transform.position = currentHitPoint;
        calloutContainer.transform.LookAt(calloutContainer.transform.position + mainCamera.forward);

        if (dotRect != null) dotRect.localPosition = Vector3.zero;
        tooltipText.rectTransform.localPosition = new Vector3(labelOffset.x, labelOffset.y, 0f);

        DrawLine3D(Vector3.zero, tooltipText.rectTransform.localPosition);
    }

    private void DrawLine3D(Vector3 localStart, Vector3 localEnd)
    {
        if (lineRect == null) return;

        Vector3 differenceVector = localEnd - localStart;
        float distance = differenceVector.magnitude - padding; 
        
        lineRect.localPosition = localStart;
        lineRect.sizeDelta = new Vector2(distance, lineRect.sizeDelta.y);

        float angle = Mathf.Atan2(differenceVector.y, differenceVector.x) * Mathf.Rad2Deg;
        lineRect.localRotation = Quaternion.Euler(0, 0, angle);
    }
}