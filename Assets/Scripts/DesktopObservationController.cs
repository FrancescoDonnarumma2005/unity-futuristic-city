using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles transitioning the desktop camera onto an observation anchor and back with smooth motion.
/// </summary>
public class DesktopObservationController : MonoBehaviour
{
    [SerializeField] private DesktopFirstPersonController firstPersonController;
    [SerializeField] private DesktopGrabber desktopGrabber;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private DesktopInstructionUI instructionUI;
    [SerializeField] private LayerMask observationLayers = ~0;
    [SerializeField] private float maxObservationDistance = 4f;
    [SerializeField] private bool unlockCursorWhileObserving = true;
    [SerializeField] private float transitionDuration = 0.4f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("UI Text Overrides")]
    [SerializeField] private string observeInputLabelOverride;
    [SerializeField] private string exitInputLabelOverride = "ESC";

    private InputAction observeAction;
    private InputAction exitAction;
    private bool actionsInitialized;

    private DesktopObservationTarget currentTarget;
    private string observeBindingLabel;
    private string exitBindingLabel;
    private bool observationControlsLocked;

    private Coroutine cameraTransitionRoutine;
    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;
    private bool hasCachedCameraState;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (instructionUI == null)
        {
            instructionUI = DesktopInstructionUI.Instance;
        }
    }

    private void OnEnable()
    {
        EnsureInputActions();
        observeAction?.Enable();
        exitAction?.Enable();
    }

    private void OnDisable()
    {
        observeAction?.Disable();
        exitAction?.Disable();
        ExitObservation(true);
    }

    private void Update()
    {
        if (playerCamera == null)
        {
            return;
        }

        if (currentTarget != null)
        {
            if (exitAction != null && exitAction.WasPressedThisFrame())
            {
                ExitObservation();
            }
            else
            {
                ShowExitHint();
            }

            return;
        }

        if (observationControlsLocked)
        {
            return;
        }

        var target = FindObservableTarget();
        if (target != null)
        {
            ShowHoverHint(target);
            if (observeAction != null && observeAction.WasPressedThisFrame())
            {
                EnterObservation(target);
            }
        }
        else if (EnsureInstructionUI())
        {
            instructionUI.ClearHint(this);
        }
    }

    private DesktopObservationTarget FindObservableTarget()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out var hit, maxObservationDistance, observationLayers, QueryTriggerInteraction.Ignore))
        {
            return null;
        }

        return hit.collider.GetComponentInParent<DesktopObservationTarget>();
    }

    private void EnterObservation(DesktopObservationTarget target)
    {
        if (target == null || playerCamera == null || target == currentTarget)
        {
            return;
        }

        currentTarget = target;
        CacheCameraState();
        BeginObservationState();

        Transform anchor = target.ObservationAnchor;
        if (anchor == null)
        {
            ExitObservation(true);
            return;
        }

        StartCameraTransition(anchor.position, anchor.rotation, () =>
        {
            var cameraTransform = playerCamera.transform;
            cameraTransform.SetParent(anchor, false);
            cameraTransform.localPosition = Vector3.zero;
            cameraTransform.localRotation = Quaternion.identity;
        });

        ShowExitHint();
    }

    private void ExitObservation(bool immediate = false)
    {
        if (currentTarget == null && !hasCachedCameraState)
        {
            return;
        }

        currentTarget = null;

        if (EnsureInstructionUI())
        {
            instructionUI.ClearHint(this);
        }

        if (!hasCachedCameraState)
        {
            EndObservationState();
            return;
        }

        if (cameraTransitionRoutine != null)
        {
            StopCoroutine(cameraTransitionRoutine);
            cameraTransitionRoutine = null;
        }

        if (immediate || transitionDuration <= Mathf.Epsilon || !isActiveAndEnabled)
        {
            RestoreCameraImmediate();
            EndObservationState();
            return;
        }

        Vector3 targetPosition = GetOriginalCameraWorldPosition();
        Quaternion targetRotation = GetOriginalCameraWorldRotation();

        cameraTransitionRoutine = StartCoroutine(TransitionCameraRoutine(targetPosition, targetRotation, () =>
        {
            RestoreCameraImmediate();
            EndObservationState();
        }));
    }

    private void ShowHoverHint(DesktopObservationTarget target)
    {
        if (!EnsureInstructionUI() || target == null)
        {
            return;
        }

        string message = $"Clicca {GetObserveLabel()} per osservare {target.DisplayName}.";
        instructionUI.ShowHint(this, message, DesktopHintPriority.High);
    }

    private void ShowExitHint()
    {
        if (!EnsureInstructionUI() || currentTarget == null)
        {
            return;
        }

        string message = $"Premi {GetExitLabel()} per uscire da {currentTarget.DisplayName}.";
        instructionUI.ShowHint(this, message, DesktopHintPriority.High);
    }

    private string GetObserveLabel()
    {
        if (!string.IsNullOrWhiteSpace(observeInputLabelOverride))
        {
            return observeInputLabelOverride;
        }

        if (string.IsNullOrWhiteSpace(observeBindingLabel) && observeAction != null)
        {
            observeBindingLabel = observeAction.GetBindingDisplayString();
        }

        return string.IsNullOrWhiteSpace(observeBindingLabel) ? "il tasto destro del mouse" : observeBindingLabel;
    }

    private string GetExitLabel()
    {
        if (!string.IsNullOrWhiteSpace(exitInputLabelOverride))
        {
            return exitInputLabelOverride;
        }

        if (string.IsNullOrWhiteSpace(exitBindingLabel) && exitAction != null)
        {
            exitBindingLabel = exitAction.GetBindingDisplayString();
        }

        return string.IsNullOrWhiteSpace(exitBindingLabel) ? "ESC" : exitBindingLabel;
    }

    private void EnsureInputActions()
    {
        if (actionsInitialized)
        {
            return;
        }

        actionsInitialized = true;

        observeAction = new InputAction("Observe", InputActionType.Button);
        observeAction.AddBinding("<Mouse>/rightButton");
        observeAction.AddBinding("<Gamepad>/leftTrigger");

        exitAction = new InputAction("ExitObservation", InputActionType.Button);
        exitAction.AddBinding("<Keyboard>/escape");
        exitAction.AddBinding("<Gamepad>/start");
    }

    private bool EnsureInstructionUI()
    {
        if (instructionUI == null)
        {
            instructionUI = DesktopInstructionUI.Instance;
        }

        return instructionUI != null;
    }

    private void CacheCameraState()
    {
        if (playerCamera == null || hasCachedCameraState)
        {
            return;
        }

        var camTransform = playerCamera.transform;
        originalCameraParent = camTransform.parent;
        originalCameraLocalPosition = camTransform.localPosition;
        originalCameraLocalRotation = camTransform.localRotation;
        hasCachedCameraState = true;
    }

    private void RestoreCameraImmediate()
    {
        if (playerCamera == null || !hasCachedCameraState)
        {
            return;
        }

        var camTransform = playerCamera.transform;
        camTransform.SetParent(originalCameraParent, false);
        camTransform.localPosition = originalCameraLocalPosition;
        camTransform.localRotation = originalCameraLocalRotation;
        hasCachedCameraState = false;
    }

    private Vector3 GetOriginalCameraWorldPosition()
    {
        if (!hasCachedCameraState)
        {
            return playerCamera != null ? playerCamera.transform.position : Vector3.zero;
        }

        return originalCameraParent != null
            ? originalCameraParent.TransformPoint(originalCameraLocalPosition)
            : originalCameraLocalPosition;
    }

    private Quaternion GetOriginalCameraWorldRotation()
    {
        if (!hasCachedCameraState)
        {
            return playerCamera != null ? playerCamera.transform.rotation : Quaternion.identity;
        }

        return originalCameraParent != null
            ? originalCameraParent.rotation * originalCameraLocalRotation
            : originalCameraLocalRotation;
    }

    private void BeginObservationState()
    {
        if (observationControlsLocked)
        {
            return;
        }

        observationControlsLocked = true;
        firstPersonController?.SetInputSuspended(true, unlockCursorWhileObserving);
        desktopGrabber?.SetInteractionsLocked(true);
    }

    private void EndObservationState()
    {
        if (!observationControlsLocked)
        {
            return;
        }

        observationControlsLocked = false;
        firstPersonController?.SetInputSuspended(false, unlockCursorWhileObserving);
        desktopGrabber?.SetInteractionsLocked(false);
    }

    private void StartCameraTransition(Vector3 targetPosition, Quaternion targetRotation, Action onComplete)
    {
        if (playerCamera == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (cameraTransitionRoutine != null)
        {
            StopCoroutine(cameraTransitionRoutine);
        }

        cameraTransitionRoutine = StartCoroutine(TransitionCameraRoutine(targetPosition, targetRotation, onComplete));
    }

    private IEnumerator TransitionCameraRoutine(Vector3 targetPosition, Quaternion targetRotation, Action onComplete)
    {
        var cameraTransform = playerCamera.transform;
        Vector3 startPos = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;

        cameraTransform.SetParent(null, true);

        if (transitionDuration <= Mathf.Epsilon)
        {
            cameraTransform.SetPositionAndRotation(targetPosition, targetRotation);
            onComplete?.Invoke();
            cameraTransitionRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float eased = transitionCurve != null ? transitionCurve.Evaluate(t) : t;
            cameraTransform.SetPositionAndRotation(
                Vector3.Lerp(startPos, targetPosition, eased),
                Quaternion.Slerp(startRot, targetRotation, eased));

            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraTransform.SetPositionAndRotation(targetPosition, targetRotation);
        onComplete?.Invoke();
        cameraTransitionRoutine = null;
    }
}
