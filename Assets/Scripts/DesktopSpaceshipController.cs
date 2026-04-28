using UnityEngine;
using UnityEngine.InputSystem;
using System.Threading;
using UnityEngine.Events; // Necessario per gli eventi

public class DesktopSpaceshipController : MonoBehaviour
{
    public bool isGrounded = true;

    [Header("Architettura Giocatore")]
    public GameObject playerRoot;
    public Camera playerCamera;
    public Transform cockpitCameraMount;

    [Header("Correzione Assi (Blender)")]
    [Tooltip("Attiva se il modello 3D usa l'asse Z per l'altezza e Y in avanti (Modelli Blender non corretti)")]
    public bool fixBlenderAxes = true;

    [Header("Fisica e Movimento")]
    public float moveSpeed = 15f;
    public float rotationSpeed = 80f;

    [Header("Controlli (Configurabili da Inspector)")]
    [Tooltip("Azione Movimento: WASD (Tipo: Value, Vector2)")]
    public InputAction moveAction;
    
    [Tooltip("Azione Verticale: Space/F (Tipo: Value, Axis)")]
    public InputAction verticalAction;
    
    [Tooltip("Azione Imbardata: Q/E (Tipo: Value, Axis)")]
    public InputAction yawAction;
    
    [Tooltip("Azione Entra: E (Tipo: Button)")]
    public InputAction enterAction;
    
    [Tooltip("Azione Esci: ESC (Tipo: Button)")]
    public InputAction exitAction;

    [Header("Controllo Visuale Cabina")]
    public float mouseSensitivity = 0.5f;
    public float maxLookUp = -60f;
    public float maxLookDown = 60f;
    public float maxLookSide = 90f;

    [Header("Interazione Porte")]
    public float interactionDistance = 5f;
    public Transform leftExitTarget;

    [Header("Eventi Missione")]
    public UnityEvent onEnterSpaceship; // Questo evento scatterà quando entri

    private bool isPiloting = false;
    private bool isTransitioning = false;
    private Vector3 parkedUpDirection;

    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPos;
    private Rigidbody rb;
    
    private float cockpitPitch = 0f;
    private float cockpitYaw = 0f;

    // --- GESTIONE DEL CICLO VITA DEGLI INPUT ---
    private void OnEnable()
    {
        moveAction.Enable();
        verticalAction.Enable();
        yawAction.Enable();
        enterAction.Enable();
        exitAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        verticalAction.Disable();
        yawAction.Disable();
        enterAction.Disable();
        exitAction.Disable();
    }

    private void Start()
    {
        parkedUpDirection = transform.InverseTransformDirection(Vector3.up);

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.freezeRotation = true; 
        }

        if (playerCamera == null) playerCamera = Camera.main;

        if (playerCamera != null)
        {
            originalCameraParent = playerCamera.transform.parent;
            originalCameraLocalPos = playerCamera.transform.localPosition;
        }
    }

    private void Update()
    {
        if (isTransitioning) return;

        // --- A PIEDI ---
        if (!isPiloting)
        {
            // Se premo il tasto per entrare...
            if (enterAction.WasPressedThisFrame() && playerCamera != null)
            {
                Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
                {
                    if (hit.collider.transform.IsChildOf(this.transform))
                    {
                        EnterSpaceship();
                    }
                }
            }
            return;
        }

        // --- IN CABINA ---
        if (exitAction.WasPressedThisFrame())
        {
            _ = PerformExitAsync(leftExitTarget);
            return;
        }

        // Rotazione visuale del pilota (Mouse)
        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            cockpitYaw += mouseDelta.x * mouseSensitivity;
            cockpitPitch -= mouseDelta.y * mouseSensitivity; 

            cockpitPitch = Mathf.Clamp(cockpitPitch, maxLookUp, maxLookDown);
            cockpitYaw = Mathf.Clamp(cockpitYaw, -maxLookSide, maxLookSide);

            playerCamera.transform.localRotation = Quaternion.Euler(cockpitPitch, cockpitYaw, 0f);
        }
    }

    private void FixedUpdate()
    {
        if (rb == null || !isPiloting || isTransitioning) return;

        // 1. Lettura dinamica degli Input
        Vector2 moveInput = moveAction.ReadValue<Vector2>(); 
        float verticalInput = verticalAction.ReadValue<float>(); 
        float yawInput = yawAction.ReadValue<float>(); 

        Vector3 localMove;

        // 2. Correzione degli Assi (La magia per aggirare il problema di Blender)
        if (fixBlenderAxes)
        {
            // Se la nave ha la Z in alto:
            // L'Avanti di Unity (Y nel joystick) diventa la Y del modello.
            // Il Su/Giù di Unity (verticalInput) diventa la Z del modello.
            localMove = new Vector3(moveInput.x, moveInput.y, verticalInput);
        }
        else
        {
            // Assi Standard di Unity: Y è Su/Giù, Z è Avanti/Indietro.
            localMove = new Vector3(moveInput.x, verticalInput, moveInput.y);
        }

        // 3. Applicazione del Movimento Lineare
        Vector3 worldMove = transform.TransformDirection(localMove);
        rb.linearVelocity = worldMove * moveSpeed;

        // 4. Applicazione della Rotazione (Senza Rollio)
        if (Mathf.Abs(yawInput) > 0.01f)
        {
            float yawAmount = yawInput * rotationSpeed * Time.fixedDeltaTime;
            
            // Se la navicella di Blender è "sdraiata", l'asse su cui girare il muso è la Z (forward di Unity)
            Vector3 rotationAxis = fixBlenderAxes ? Vector3.forward : Vector3.up;
            
            rb.MoveRotation(rb.rotation * Quaternion.Euler(rotationAxis * yawAmount));
        }
        else
        {
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void EnterSpaceship()
    {
        if (isPiloting || isTransitioning) return;
        isPiloting = true;

        playerCamera.transform.SetParent(cockpitCameraMount, false);
        playerCamera.transform.localPosition = Vector3.zero;
        
        cockpitPitch = 0f;
        cockpitYaw = 0f;
        playerCamera.transform.localRotation = Quaternion.identity;

        if (playerRoot != null) playerRoot.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Debug.Log("Salito a bordo. Comandi attivati.");

        onEnterSpaceship?.Invoke();
    }

    private async Awaitable PerformExitAsync(Transform exitNode)
    {
        if (!isPiloting || isTransitioning) return;
        if (!isGrounded)
        {
            Debug.LogWarning("Devi atterrare prima di scendere!");
            return;
        }

        isTransitioning = true;
        isPiloting = false;
        
        await LevelOutSpaceshipAsync(destroyCancellationToken);

        if (playerRoot != null)
        {
            playerRoot.transform.position = exitNode.position;
            playerRoot.transform.rotation = Quaternion.LookRotation(exitNode.forward, Vector3.up);
            playerRoot.SetActive(true);
        }

        playerCamera.transform.SetParent(originalCameraParent, false);
        playerCamera.transform.localPosition = originalCameraLocalPos;
        playerCamera.transform.localRotation = Quaternion.identity;

        Cursor.lockState = CursorLockMode.None;

        isTransitioning = false;
        Debug.Log("Sceso in sicurezza.");
    }

    private async Awaitable LevelOutSpaceshipAsync(CancellationToken token)
    {
        try
        {
            bool wasKinematic = false;
            if (rb != null)
            {
                wasKinematic = rb.isKinematic;
                rb.isKinematic = true;
            }

            Quaternion startRot = transform.rotation;
            Vector3 currentUp = transform.TransformDirection(parkedUpDirection);
            Quaternion levelingRot = Quaternion.FromToRotation(currentUp, Vector3.up);
            Quaternion targetRot = levelingRot * transform.rotation;

            float duration = 1.2f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                token.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                await Awaitable.NextFrameAsync(token);
            }

            transform.rotation = targetRot;
            if (rb != null) rb.isKinematic = wasKinematic;
        }
        catch (System.OperationCanceledException)
        {
            if (rb != null) rb.isKinematic = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") && isPiloting) isGrounded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") && isPiloting) isGrounded = false;
    }
}