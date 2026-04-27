using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple WASD + mouse-look controller used when the experience runs outside of VR.
/// Uses the new Input System so it also works in WebGL builds configured for it.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class DesktopFirstPersonController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float lookSensitivity = 2f;
    
    [Tooltip("If true, the camera rotates only when the right mouse button is held down.")]
    [SerializeField] private bool requireClickToLook = false; 
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.25f;
    [SerializeField] private bool lockCursorWhileActive = true;

    private CharacterController characterController;
    private float pitch;
    private float verticalVelocity;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction clickAction; // Nuova azione per il click

    private bool actionsInitialized;
    private bool inputsSuspended;
    private bool cursorUnlockedForSuspend;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }
    }

    private void OnEnable()
    {
        EnsureInputActions();
        SetActionsEnabled(true);
        UpdateCursorState(); // Gestione centralizzata del cursore
    }

    private void OnDisable()
    {
        SetActionsEnabled(false);

        if (lockCursorWhileActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void OnDestroy()
    {
        DisposeAction(ref moveAction);
        DisposeAction(ref lookAction);
        DisposeAction(ref jumpAction);
        DisposeAction(ref sprintAction);
        DisposeAction(ref clickAction);
    }

    private void Update()
    {
        if (playerCamera == null || inputsSuspended)
        {
            return;
        }

        HandleCursorDynamicBehavior(); // Controlla il cursore frame per frame se necessario
        HandleLook();
        HandleMovement();
    }

    private void HandleCursorDynamicBehavior()
    {
        // Se siamo in modalità "Click to Look", gestiamo il cursore dinamicamente
        if (requireClickToLook && lockCursorWhileActive)
        {
            bool isClicking = clickAction != null && clickAction.IsPressed();
            
            if (isClicking && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (!isClicking && Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    private void UpdateCursorState()
    {
        if (lockCursorWhileActive)
        {
            // Se richiedo il click, all'inizio il cursore deve essere LIBERO
            if (requireClickToLook)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private void HandleLook()
    {
        if (lookAction == null)
        {
            return;
        }

        // Controllo: Se l'opzione è attiva e NON sto premendo il tasto, esco.
        if (requireClickToLook)
        {
            if (clickAction == null || !clickAction.IsPressed())
                return;
        }

        Vector2 lookDelta = lookAction.ReadValue<Vector2>() * lookSensitivity;
        if (lookDelta.sqrMagnitude <= float.Epsilon)
        {
            return;
        }

        pitch = Mathf.Clamp(pitch - lookDelta.y, -85f, 85f);
        playerCamera.transform.localEulerAngles = new Vector3(pitch, 0f, 0f);
        transform.Rotate(Vector3.up, lookDelta.x);
    }

    private void HandleMovement()
    {
        if (moveAction == null)
        {
            return;
        }

        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 move = (transform.forward * input.y + transform.right * input.x);

        float speed = moveSpeed;
        if (sprintAction != null && sprintAction.IsPressed())
        {
            speed *= sprintMultiplier;
        }

        move = move.normalized * speed;

        if (characterController.isGrounded)
        {
            verticalVelocity = -1f;
            if (jumpAction != null && jumpAction.WasPressedThisFrame())
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        move.y = verticalVelocity;
        characterController.Move(move * Time.deltaTime);
    }

    private void EnsureInputActions()
    {
        if (actionsInitialized)
        {
            return;
        }

        actionsInitialized = true;

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");

        lookAction = new InputAction("Look", InputActionType.Value);
        lookAction.AddBinding("<Mouse>/delta");
        lookAction.AddBinding("<Gamepad>/rightStick");

        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");

        sprintAction = new InputAction("Sprint", InputActionType.Button);
        sprintAction.AddBinding("<Keyboard>/leftShift");
        sprintAction.AddBinding("<Gamepad>/leftStickPress");

        // Definizione azione per il click destro
        clickAction = new InputAction("ClickToLook", InputActionType.Button);
        clickAction.AddBinding("<Mouse>/rightButton");
        // Opzionale: aggiungi binding per Gamepad se necessario (es. grilletto sinistro)
        // clickAction.AddBinding("<Gamepad>/leftTrigger");
    }

    private void SetActionsEnabled(bool enabled)
    {
        if (!actionsInitialized)
        {
            return;
        }

        if (enabled)
        {
            moveAction?.Enable();
            lookAction?.Enable();
            jumpAction?.Enable();
            sprintAction?.Enable();
            clickAction?.Enable();
        }
        else
        {
            moveAction?.Disable();
            lookAction?.Disable();
            jumpAction?.Disable();
            sprintAction?.Disable();
            clickAction?.Disable();
        }
    }

    private static void DisposeAction(ref InputAction action)
    {
        if (action != null)
        {
            action.Disable();
            action.Dispose();
            action = null;
        }
    }

    /// <summary>
    /// Temporarily blocks player input. Optionally unlocks the cursor while suspended.
    /// </summary>
    public void SetInputSuspended(bool suspended, bool unlockCursorWhileSuspended)
    {
        if (inputsSuspended == suspended)
        {
            if (suspended && unlockCursorWhileSuspended && lockCursorWhileActive && !cursorUnlockedForSuspend)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                cursorUnlockedForSuspend = true;
            }

            return;
        }

        inputsSuspended = suspended;

        if (!isActiveAndEnabled)
        {
            cursorUnlockedForSuspend = suspended && unlockCursorWhileSuspended;
            return;
        }

        SetActionsEnabled(!suspended);

        if (suspended)
        {
            verticalVelocity = 0f;
            if (lockCursorWhileActive && unlockCursorWhileSuspended)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                cursorUnlockedForSuspend = true;
            }
        }
        else
        {
            // Ripristina lo stato del cursore basandosi sulla modalità selezionata
            UpdateCursorState();
            cursorUnlockedForSuspend = false;
        }
    }
}