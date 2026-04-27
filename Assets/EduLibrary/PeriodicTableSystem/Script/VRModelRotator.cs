using UnityEngine;
using UnityEngine.InputSystem;

public class VRModelRotator : MonoBehaviour
{
    // Pattern Singleton
    public static VRModelRotator Instance { get; private set; }

    [Header("Input Settings")]
    [Tooltip("Inserire l'azione dell'analogico destro (es. XRI RightHand/Primary2DAxis)")]
    public InputActionReference rightThumbstick;
    
    [Tooltip("Inserire l'azione dell'analogico sinistro (es. XRI LeftHand/Primary2DAxis)")]
    public InputActionReference leftThumbstick;

    [Header("Rotation Settings")]
    [Tooltip("Velocita di rotazione dell'atomo")]
    public float rotationSpeed = 90f;

    private Transform targetToRotate;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // --- IL FIX ARCHITETTURALE: ACCENSIONE FORZATA ---
    private void OnEnable()
    {
        if (rightThumbstick != null && rightThumbstick.action != null) 
            rightThumbstick.action.Enable();
            
        if (leftThumbstick != null && leftThumbstick.action != null) 
            leftThumbstick.action.Enable();
    }

    private void OnDisable()
    {
        if (rightThumbstick != null && rightThumbstick.action != null) 
            rightThumbstick.action.Disable();
            
        if (leftThumbstick != null && leftThumbstick.action != null) 
            leftThumbstick.action.Disable();
    }
    // ------------------------------------------------

    // Chiamato dal tuo ProceduralAtomRenderer
    public void SetTarget(Transform atomTransform)
    {
        targetToRotate = atomTransform;
        Debug.Log($"[VRModelRotator] Bersaglio agganciato con successo: {atomTransform.name}");
    }

    void Update()
    {
        if (targetToRotate == null) return;

        Vector2 input = Vector2.zero;

        // Legge il destro
        if (rightThumbstick != null && rightThumbstick.action != null)
        {
            Vector2 rightInput = rightThumbstick.action.ReadValue<Vector2>();
            if (rightInput.sqrMagnitude > 0.05f) input = rightInput;
        }

        // Legge il sinistro (se il destro non è in uso)
        if (input == Vector2.zero && leftThumbstick != null && leftThumbstick.action != null)
        {
            Vector2 leftInput = leftThumbstick.action.ReadValue<Vector2>();
            if (leftInput.sqrMagnitude > 0.05f) input = leftInput;
        }

        // Ruota il target
        if (input != Vector2.zero)
        {
            targetToRotate.Rotate(Vector3.up, -input.x * rotationSpeed * Time.deltaTime, Space.World);
            targetToRotate.Rotate(Vector3.right, input.y * rotationSpeed * Time.deltaTime, Space.World);
        }
    }
}