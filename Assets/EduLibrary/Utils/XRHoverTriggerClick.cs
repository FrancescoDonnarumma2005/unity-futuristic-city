using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // Namespace corretto per Unity 6

/// <summary>
/// Permette di attivare un evento premendo il Trigger (Indice) mentre si punta l'oggetto col laser.
/// Funziona con qualsiasi Interactable (Grab o Simple).
/// </summary>
[RequireComponent(typeof(XRBaseInteractable))] // <-- MODIFICA CHIAVE: Ora accetta anche il Grab Interactable!
public class XRHoverTriggerClick : MonoBehaviour
{
    [Header("Input (Dal pacchetto XRI Default Input Actions)")]
    [Tooltip("Inserisci l'azione 'UI Press' o 'Activate' della mano DESTRA")]
    [SerializeField] private InputActionReference rightHandTrigger;
    
    [Tooltip("Inserisci l'azione 'UI Press' o 'Activate' della mano SINISTRA")]
    [SerializeField] private InputActionReference leftHandTrigger;

    [Header("Azione da Eseguire")]
    public UnityEngine.Events.UnityEvent onHoverClicked;

    private XRBaseInteractable _interactable;
    private bool _isHovered;

    private void Awake()
    {
        // Ora pesca dinamicamente il componente corretto (che sia Grab o Simple)
        _interactable = GetComponent<XRBaseInteractable>();
        
        // Registriamo quando il raggio laser entra ed esce dall'oggetto
        _interactable.hoverEntered.AddListener(args => _isHovered = true);
        _interactable.hoverExited.AddListener(args => _isHovered = false);
    }

    private void Update()
    {
        if (!_isHovered) return;

        bool rightClicked = rightHandTrigger != null && rightHandTrigger.action.WasPressedThisFrame();
        bool leftClicked = leftHandTrigger != null && leftHandTrigger.action.WasPressedThisFrame();

        if (rightClicked || leftClicked)
        {
            onHoverClicked?.Invoke();
        }
    }
}