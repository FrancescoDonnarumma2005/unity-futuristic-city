using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using InspectionSystem; 
using EduUtils.Interaction;
using UnityEngine.UI;

public class DesktopGrabber : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float holdDistance = 0.7f;
    [SerializeField] private float maxGrabDistance = 3f;
    [SerializeField] private LayerMask grabbableLayers = ~0;
    [SerializeField] private float scrollSensitivity = 0.25f;
    [SerializeField] private float minHoldDistance = 0.3f;
    [SerializeField] private float maxHoldDistance = 2f;
    [SerializeField] private float followSmoothTime = 0.08f;
    [SerializeField] private DesktopInstructionUI instructionUI;
    [SerializeField] private string grabInputLabelOverride;
    [SerializeField] private float closeRangeProbeRadius = 0.2f;
    [SerializeField] private float closeRangeProbeDistance = 0.2f;
    [SerializeField] private Collider playerCollider;

    [Header("UI Crosshair")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Color normalPointerColor = Color.white;
    [SerializeField] private Color hoverPointerColor = Color.green;

    private Rigidbody grabbedBody;
    private bool cachedGravity;
    private bool cachedKinematic;
    private CollisionDetectionMode cachedCollisionMode;
    private ObjectSnap grabbedSnap;

    private InputAction grabAction;
    private InputAction scrollAction;
    private InputAction inspectAction; 
    
    private bool actionsInitialized;
    private Transform holdAnchor;
    private Vector3 followVelocity;
    private bool interactionsLocked;
    private string grabBindingLabel;
    
    private int objectLayerMask;
    private bool hasObjectLayerMask;
    private int objectLayerIndex; 
    private int interactableLayerIndex; 
    private int uiLayerIndex; 
    
    private readonly List<Collider> ignoredPlayerColliders = new List<Collider>();
    private IInteractable currentInteractable;

    private void Awake()
    {
        objectLayerIndex = LayerMask.NameToLayer("Object");
        interactableLayerIndex = LayerMask.NameToLayer("Interactable"); 
        uiLayerIndex = LayerMask.NameToLayer("UI");

        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (instructionUI == null) instructionUI = DesktopInstructionUI.Instance;

        if (playerCollider == null)
        {
            var characterController = GetComponentInParent<CharacterController>();
            if (characterController != null) playerCollider = characterController;
        }

        CacheObjectLayerMask();

        if (playerCamera != null)
        {
            var existingAnchor = playerCamera.transform.Find("DesktopHoldAnchor");
            if (existingAnchor != null) holdAnchor = existingAnchor;
            else
            {
                holdAnchor = new GameObject("DesktopHoldAnchor").transform;
                holdAnchor.SetParent(playerCamera.transform);
                holdAnchor.localRotation = Quaternion.identity;
                holdAnchor.localScale = Vector3.one;
            }
            SetHoldDistance(holdDistance);
        }
    }

    private void OnEnable()
    {
        EnsureInputActions();
        grabAction.Enable();
        scrollAction.Enable();
        inspectAction.Enable(); 
        
        grabAction.performed += OnGrabPerformed;
        grabAction.canceled += OnGrabCanceled;
    }

    private void OnDisable()
    {
        if (actionsInitialized)
        {
            grabAction.performed -= OnGrabPerformed;
            grabAction.canceled -= OnGrabCanceled;
            
            grabAction.Disable();
            scrollAction.Disable();
            inspectAction.Disable(); 
        }

        ReleaseGrab();
        instructionUI?.ClearHint(this);
    }

    private void OnDestroy()
    {
        DisposeAction(ref grabAction);
        DisposeAction(ref scrollAction);
        DisposeAction(ref inspectAction); 
    }

    private void Update()
    {
        if (playerCamera == null) return;

        UpdateHoldDistance();
        UpdateInteractionHints();
        UpdateCrosshairColor();
    }

    private void OnGrabPerformed(InputAction.CallbackContext ctx) 
    { 
        if (grabbedBody == null && !interactionsLocked) TryGrab(); 
    }
    
    private void OnGrabCanceled(InputAction.CallbackContext ctx) 
    { 
        ReleaseGrab(); 
    }
    
    private void TryGrab() 
    { 
        if (interactionsLocked) return;
        if (!TryFindTarget(out var targetInfo)) return;
        SetHoldDistance(targetInfo.Distance);
        var body = targetInfo.Body;
        if (body == null) return;
        grabbedBody = body;
        cachedGravity = body.useGravity;
        cachedKinematic = body.isKinematic;
        cachedCollisionMode = body.collisionDetectionMode;
        body.useGravity = false; 
        body.isKinematic = true; 
        body.linearVelocity = Vector3.zero; 
        body.angularVelocity = Vector3.zero; 
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        grabbedSnap = targetInfo.Snap ?? body.GetComponent<ObjectSnap>();
        grabbedSnap?.NotifyGrabbed();
        IgnorePlayerCollision(body, true);
        UpdateInteractionHints();
    }
    
    private void ReleaseGrab() 
    { 
        if (grabbedBody == null) { IgnorePlayerCollision(null, false); return; }
        grabbedBody.useGravity = cachedGravity; 
        grabbedBody.isKinematic = cachedKinematic; 
        grabbedBody.collisionDetectionMode = cachedCollisionMode;
        grabbedSnap?.NotifyReleased(); 
        IgnorePlayerCollision(null, false);
        grabbedBody = null; 
        grabbedSnap = null; 
        followVelocity = Vector3.zero;
        UpdateInteractionHints();
    }
    
    private void UpdateHoldDistance() 
    { 
        if (scrollAction == null) return;
        float scroll = scrollAction.ReadValue<float>();
        if (Mathf.Abs(scroll) <= Mathf.Epsilon) return;
        SetHoldDistance(holdDistance + scroll * scrollSensitivity);
    }
    
    private void LateUpdate() 
    { 
        if (grabbedBody == null || playerCamera == null || interactionsLocked) return;
        Vector3 targetPosition = playerCamera.transform.position + playerCamera.transform.forward * holdDistance;
        Vector3 newPosition = Vector3.SmoothDamp(grabbedBody.transform.position, targetPosition, ref followVelocity, followSmoothTime);
        grabbedBody.MovePosition(newPosition);
    }
    
    private void SetHoldDistance(float distance) 
    { 
        holdDistance = Mathf.Clamp(distance, minHoldDistance, maxHoldDistance);
        if (holdAnchor != null) holdAnchor.localPosition = new Vector3(0f, 0f, holdDistance);
    }
    
    private void EnsureInputActions() 
    { 
        if (actionsInitialized) return;
        actionsInitialized = true;
        grabAction = new InputAction("Grab", InputActionType.Button); 
        grabAction.AddBinding("<Mouse>/leftButton"); 
        grabAction.AddBinding("<Gamepad>/rightTrigger");
        
        scrollAction = new InputAction("Scroll", InputActionType.Value); 
        scrollAction.AddBinding("<Mouse>/scroll/y"); 
        scrollAction.AddBinding("<Gamepad>/dpad/up", processors: "scale(factor=1)"); 
        scrollAction.AddBinding("<Gamepad>/dpad/down", processors: "scale(factor=-1)");
        
        inspectAction = new InputAction("Inspect", InputActionType.Button); 
        inspectAction.AddBinding("<Keyboard>/e"); 
        inspectAction.AddBinding("<Gamepad>/buttonNorth");
    }
    
    private static void DisposeAction(ref InputAction action) 
    { 
        if (action != null) { action.Disable(); action.Dispose(); action = null; }
    }
    
    public void SetInteractionsLocked(bool locked) 
    { 
        if (interactionsLocked == locked) return;
        interactionsLocked = locked;
        if (interactionsLocked) { ReleaseGrab(); instructionUI?.ClearHint(this); }
        else { UpdateInteractionHints(); }
    }

    public bool IsHoldingObject => grabbedBody != null;

    private void UpdateInteractionHints()
    {
        if (instructionUI == null)
        {
            instructionUI = DesktopInstructionUI.Instance;
            if (instructionUI == null) return;
        }

        if (interactionsLocked) return;

        if (grabbedBody != null)
        {
            string objectName = grabbedSnap != null ? grabbedSnap.ObjectDisplayName : grabbedBody.name;
            if (grabbedSnap != null && grabbedSnap.IsInsideSnapZone)
            {
                string targetName = grabbedSnap.SnapTargetDisplayName;
                instructionUI.ShowHint(this, $"Lascia {GetGrabBindingLabel()} per inserire {objectName} in {targetName}.", DesktopHintPriority.High);
            }
            else
            {
                instructionUI.ShowHint(this, $"Lascia {GetGrabBindingLabel()} per rilasciare {objectName}.", DesktopHintPriority.Medium);
            }
            return;
        }

        if (playerCamera == null)
        {
            instructionUI.ClearHint(this);
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        
        int combinedMask = GetLayerMask();
        if (interactableLayerIndex != -1) combinedMask |= (1 << interactableLayerIndex);
        if (uiLayerIndex != -1) combinedMask |= (1 << uiLayerIndex);

        IInteractable newInteractable = null;

        if (Physics.Raycast(ray, out var hit, maxGrabDistance, combinedMask, QueryTriggerInteraction.Collide))
        {
            newInteractable = hit.collider.GetComponent<IInteractable>();
            if (newInteractable == null) newInteractable = hit.collider.GetComponentInParent<IInteractable>();

            if (newInteractable != null)
            {
                string itemName = hit.collider.gameObject.name.Replace("_Prefab", "").Replace("_", " ");
                instructionUI.ShowHint(this, $"Premi E per interagire con {itemName}", DesktopHintPriority.Medium);

                if (inspectAction.WasPerformedThisFrame())
                {
                    newInteractable.OnInteract();
                }
                
                if (newInteractable != currentInteractable)
                {
                    currentInteractable?.OnHover(false);
                    currentInteractable = newInteractable;
                    currentInteractable?.OnHover(true);
                }
                return; 
            }

            var inspectionTrigger = hit.collider.GetComponent<InspectionTrigger>();
            if (inspectionTrigger == null) inspectionTrigger = hit.collider.GetComponentInParent<InspectionTrigger>();

            if (inspectionTrigger != null)
            {
                string itemName = inspectionTrigger.itemData != null ? inspectionTrigger.itemData.itemName : "Oggetto";
                instructionUI.ShowHint(this, $"Premi E per ispezionare {itemName}", DesktopHintPriority.Medium);

                if (inspectAction.WasPerformedThisFrame())
                {
                    inspectionTrigger.OnInteract();
                }
                
                if (currentInteractable != null)
                {
                    currentInteractable.OnHover(false);
                    currentInteractable = null;
                }
                return; 
            }

            if (hit.collider.gameObject.layer == uiLayerIndex)
            {
                Button hitButton = hit.collider.GetComponent<Button>();
                if (hitButton == null) hitButton = hit.collider.GetComponentInParent<Button>();

                if (hitButton != null && hitButton.interactable)
                {
                    instructionUI.ShowHint(this, $"Premi E per selezionare", DesktopHintPriority.Medium);

                    if (inspectAction.WasPerformedThisFrame())
                    {
                        hitButton.onClick.Invoke();
                    }

                    if (currentInteractable != null)
                    {
                        currentInteractable.OnHover(false);
                        currentInteractable = null;
                    }
                    return; 
                }
            }
        }

        if (currentInteractable != null)
        {
            currentInteractable.OnHover(false);
            currentInteractable = null;
        }

        if (!TryFindTarget(out var targetInfo))
        {
            instructionUI.ClearHint(this);
            return;
        }

        var targetBody = targetInfo.Body;
        var snap = targetInfo.Snap ?? targetBody.GetComponent<ObjectSnap>();
        string hoverName = snap != null ? snap.ObjectDisplayName : targetBody.name;

        instructionUI.ShowHint(this, $"Tieni premuto {GetGrabBindingLabel()} per afferrare {hoverName}.", DesktopHintPriority.Medium);
    }

    private string GetGrabBindingLabel() 
    { 
        return string.IsNullOrWhiteSpace(grabBindingLabel) ? "il tasto sinistro del mouse" : grabBindingLabel; 
    }
    
    private bool TryFindTarget(out TargetInfo targetInfo) 
    { 
        targetInfo = default; 
        if (playerCamera == null) return false;
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out var hitInfo, maxGrabDistance, GetLayerMask(), QueryTriggerInteraction.Ignore)) 
        {
            if (objectLayerIndex != -1 && hitInfo.collider.gameObject.layer != objectLayerIndex && hitInfo.collider.gameObject.layer != uiLayerIndex) return false;
            var body = hitInfo.rigidbody;
            if (body != null) 
            { 
                targetInfo = new TargetInfo(body, body.GetComponent<ObjectSnap>(), Mathf.Max(minHoldDistance, hitInfo.distance)); 
                return true; 
            }
        }
        return TryFindCloseRangeTarget(out targetInfo);
    }
    
    private bool TryFindCloseRangeTarget(out TargetInfo targetInfo) 
    { 
        targetInfo = default; 
        if (playerCamera == null || closeRangeProbeRadius <= 0f) return false;
        
        Vector3 origin = playerCamera.transform.position; 
        Vector3 forward = playerCamera.transform.forward; 
        Vector3 probeCenter = origin + forward * closeRangeProbeDistance;
        
        Collider[] overlaps = Physics.OverlapSphere(probeCenter, closeRangeProbeRadius, GetLayerMask(), QueryTriggerInteraction.Ignore);
        if (overlaps == null || overlaps.Length == 0) return false;
        
        Rigidbody bestBody = null; 
        ObjectSnap bestSnap = null; 
        float bestDistance = float.MaxValue;
        
        foreach (var collider in overlaps) 
        {
            if (collider == null) continue;
            if (objectLayerIndex != -1 && collider.gameObject.layer != objectLayerIndex && collider.gameObject.layer != uiLayerIndex) continue;
            var body = collider.attachedRigidbody; 
            if (body == null) continue;
            Vector3 toBody = body.worldCenterOfMass - origin; 
            float forwardDistance = Vector3.Dot(toBody, forward);
            if (forwardDistance < 0f || forwardDistance > maxGrabDistance) continue;
            if (forwardDistance < bestDistance) 
            { 
                bestDistance = forwardDistance; 
                bestBody = body; 
                bestSnap = body.GetComponent<ObjectSnap>(); 
            }
        }
        
        if (bestBody == null) return false;
        targetInfo = new TargetInfo(bestBody, bestSnap, Mathf.Max(minHoldDistance, bestDistance)); 
        return true;
    }
    
    private readonly struct TargetInfo 
    { 
        public readonly Rigidbody Body; 
        public readonly ObjectSnap Snap; 
        public readonly float Distance; 
        public TargetInfo(Rigidbody body, ObjectSnap snap, float distance) 
        { 
            Body = body; 
            Snap = snap; 
            Distance = distance; 
        } 
    }
    
    private void CacheObjectLayerMask() 
    { 
        int layerIndex = LayerMask.NameToLayer("Object");
        if (layerIndex < 0) { hasObjectLayerMask = false; return; }
        objectLayerMask = 1 << layerIndex; 
        if (uiLayerIndex != -1) objectLayerMask |= (1 << uiLayerIndex);
        hasObjectLayerMask = true;
    }
    
    private int GetLayerMask() 
    { 
        return !hasObjectLayerMask ? grabbableLayers : objectLayerMask; 
    }
    
    private void IgnorePlayerCollision(Rigidbody body, bool ignore) 
    { 
        if (playerCollider == null) return;
        if (!ignore) 
        { 
            foreach (var col in ignoredPlayerColliders) 
                if (col != null) Physics.IgnoreCollision(playerCollider, col, false); 
            ignoredPlayerColliders.Clear(); 
            return; 
        }
        ignoredPlayerColliders.Clear(); 
        if (body == null) return;
        
        var colliders = body.GetComponentsInChildren<Collider>();
        foreach (var col in colliders) 
        { 
            if (col == null) continue; 
            Physics.IgnoreCollision(playerCollider, col, true); 
            ignoredPlayerColliders.Add(col); 
        }
    }

    private void UpdateCrosshairColor()
    {
        if (crosshairImage == null || playerCamera == null) return;

        int targetLayerMask = (1 << objectLayerIndex); 
        if (interactableLayerIndex != -1) targetLayerMask |= (1 << interactableLayerIndex);
        if (uiLayerIndex != -1) targetLayerMask |= (1 << uiLayerIndex);
        
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxGrabDistance, targetLayerMask, QueryTriggerInteraction.Collide))
        {
            crosshairImage.color = hoverPointerColor;
        }
        else
        {
            crosshairImage.color = normalPointerColor;
        }
    }
}