using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Threading;
using UnityEngine.Events; // Necessario per gli eventi

public class SpaceshipController : MonoBehaviour
{
    public bool isGrounded = true;

    [Header("Impostazioni VR (Giocatore)")]
    [Tooltip("Trascina qui l'XR Origin Hands (XR Rig)")]
    [SerializeField] private Transform xrorigin;

    [Tooltip("Il GameObject vuoto SUL PAVIMENTO della navicella dove andranno i piedi del giocatore")]
    public Transform seatTarget;

    [Tooltip("Trascina qui l'intero GameObject 'Locomotion' (quello con figli Turn, Move, ecc.)")]
    public GameObject locomotionGameObject;

    [Tooltip("Il Character Controller presente sull'XR Origin (seleziona l'XR Origin e trascinalo qui)")]
    public CharacterController characterController;

    [Header("Impostazioni Navicella")]
    public float moveSpeed = 10f;
    [Tooltip("Velocità di rotazione della navicella")]
    public float rotationSpeed = 60f;

    [Header("Input System")]
    [Tooltip("Trascina qui l'Action Reference del Joystick SINISTRO (es. XRI LeftHand/Move)")]
    public InputActionReference moveInput;

    [Tooltip("Trascina qui l'Action Reference del Joystick DESTRO (es. XRI RightHand/Move o Turn)")]
    public InputActionReference rotateInput;

    public InputActionReference upInput; // Tasto N (Sopra)
    public InputActionReference downInput; // Tasto B (Sotto)

    [Header("Impostazioni Uscita (Porte)")]
    [Tooltip("Il Transform all'esterno della porta SINISTRA")]
    public Transform leftExitTarget;
    [Tooltip("Il Transform all'esterno della porta DESTRA")]
    public Transform rightExitTarget;

    [Header("Eventi Missione")]
    public UnityEvent onEnterSpaceship; // Questo evento scatterà quando entri  

    private bool isPiloting = false;
    private bool isTransitioning = false; // <-- Sicurezza per bloccare interazioni in uscita
    private Vector3 parkedUpDirection;

    // --- VARIABILI AGGIUNTE PER LA GESTIONE FISICA ---
    private Rigidbody rb;
    private Vector3 currentMoveInput;
    private Vector2 currentRotateInput;

    private void Start()
    {
        // Memorizziamo quale asse LOCALE del modello corrisponde al "Sopra" globale.
        parkedUpDirection = transform.InverseTransformDirection(Vector3.up);

        // Inizializza e configura il Rigidbody via codice per sicurezza
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = false; // DEVE essere false per collidere coi muri
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.freezeRotation = true; // Impedisce che gli urti facciano ruotare la nave
        }
        else
        {
            Debug.LogError("Manca il componente Rigidbody sulla navicella!");
        }
    }

    private void OnEnable()
    {
        if (moveInput != null) moveInput.action.Enable();
        if (rotateInput != null) rotateInput.action.Enable();
        if (upInput != null) upInput.action.Enable();
        if (downInput != null) downInput.action.Enable();
    }

    private void OnDisable()
    {
        if (moveInput != null) moveInput.action.Disable();
        if (rotateInput != null) rotateInput.action.Disable();
        if (upInput != null) upInput.action.Disable();
        if (downInput != null) downInput.action.Disable();
    }

    public void EnterSpaceship()
    {
        // Se stiamo già pilotando o c'è un'animazione in corso, ignora l'input
        if (isPiloting || isTransitioning) return;

        isPiloting = true;
        Debug.Log("isPiloting: " + isPiloting);

        // 1. Disattiva l'intero sistema di locomozione 
        if (locomotionGameObject != null)
        {
            locomotionGameObject.SetActive(false);
        }

        // 2. Spegne la capsula di collisione del giocatore 
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        // 3. Imparenta e posiziona il Rig intero 
        xrorigin.SetParent(seatTarget.transform, false);
        xrorigin.localScale = Vector3.one;

        // 4. Centra la telecamera
        XROrigin originComponent = xrorigin.GetComponent<XROrigin>();
        if (originComponent != null)
        {
            originComponent.MoveCameraToWorldLocation(seatTarget.position);
            originComponent.MatchOriginUpCameraForward(seatTarget.up, seatTarget.forward);
        }

        Debug.Log("Giocatore entrato nella navicella con successo!");
        onEnterSpaceship?.Invoke();
    }

    private void Update()
    {
        // Impedisce di guidare se non si sta pilotando o se si sta uscendo
        if (!isPiloting || isTransitioning) return;

        // --- LETTURA DEGLI INPUT ---
        Vector2 inputValues = moveInput.action.ReadValue<Vector2>();
        float upValue = upInput.action.ReadValue<float>();
        float downValue = downInput.action.ReadValue<float>();

        float verticalValue = upValue - downValue;

        // CORREZIONE PER NAVICELLA RUOTATA DI 90 GRADI SU X
        currentMoveInput = new Vector3(inputValues.x, -inputValues.y, verticalValue);

        if (rotateInput != null)
        {
            currentRotateInput = rotateInput.action.ReadValue<Vector2>();
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        // Blocca la nave fisicamente se non pilotiamo o se stiamo facendo l'animazione di uscita
        if (!isPiloting || isTransitioning)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        // --- APPLICAZIONE MOVIMENTO FISICO ---
        Vector3 worldMove = transform.TransformDirection(currentMoveInput);
        rb.linearVelocity = worldMove * moveSpeed;

        // --- APPLICAZIONE ROTAZIONE FISICA ---
        if (currentRotateInput.magnitude > 0.1f)
        {
            float yaw = currentRotateInput.x * rotationSpeed * Time.fixedDeltaTime;
            float pitch = -currentRotateInput.y * rotationSpeed * Time.fixedDeltaTime;

            Quaternion deltaRotation = Quaternion.Euler(new Vector3(pitch, yaw, 0));
            rb.MoveRotation(rb.rotation * deltaRotation);
        }
        else
        {
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") && isPiloting)
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") && isPiloting)
        {
            isGrounded = false;
        }
    }

    // ----------------------------------------------------
    // GESTIONE PORTE E USCITA ASINCRONA
    // ----------------------------------------------------

    public void InteractWithLeftDoor()
    {
        if (isTransitioning) return; // Sicurezza
        if (isPiloting) ExitFromLeftDoor();
        else EnterSpaceship();
    }

    public void InteractWithRightDoor()
    {
        if (isTransitioning) return; // Sicurezza
        if (isPiloting) ExitFromRightDoor();
        else EnterSpaceship();
    }

    public void ExitFromLeftDoor()
    {
        _ = PerformExitAsync(leftExitTarget);
    }

    public void ExitFromRightDoor()
    {
        _ = PerformExitAsync(rightExitTarget);
    }

    private async Awaitable PerformExitAsync(Transform exitNode)
    {
        if (!isPiloting || isTransitioning) return;

        if (!isGrounded)
        {
            Debug.LogWarning("Azione negata: Impossibile scendere mentre si è in volo!");
            return;
        }

        // Blocca l'input del giocatore ma lo lascia seduto
        isTransitioning = true;
        isPiloting = false;

        Debug.Log("Inizio sequenza di uscita. Stabilizzazione in corso...");

        // 1. Aspetta che la navicella si raddrizzi dolcemente
        await LevelOutSpaceshipAsync(destroyCancellationToken);

        // 2. ORA sgancia il giocatore e lo sposta all'esterno
        xrorigin.SetParent(null, true);

        XROrigin originComponent = xrorigin.GetComponent<XROrigin>();
        if (originComponent != null)
        {
            originComponent.MoveCameraToWorldLocation(exitNode.position);
            originComponent.MatchOriginUpCameraForward(Vector3.up, exitNode.forward);
        }

        // 3. Riattiva i sistemi di movimento
        if (locomotionGameObject != null) locomotionGameObject.SetActive(true);
        if (characterController != null) characterController.enabled = true;

        // Fine dell'animazione, il giocatore può interagire di nuovo
        isTransitioning = false;

        Debug.Log("Uscita completata in totale sicurezza.");
    }

    private async Awaitable LevelOutSpaceshipAsync(CancellationToken token)
    {
        try
        {
            bool wasKinematic = false;

            if (rb != null)
            {
                // Disabilita la fisica per non farla impazzire mentre la ruotiamo a mano
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

            if (rb != null)
            {
                // FIX CRITICO: Riattiva la fisica altrimenti attraverserai i muri al prossimo volo!
                rb.isKinematic = wasKinematic;
            }
        }
        catch (System.OperationCanceledException)
        {
            Debug.Log("Livellamento navicella interrotto.");
            if (rb != null) rb.isKinematic = false; // Sicurezza in caso di interruzione forzata
        }
    }
}