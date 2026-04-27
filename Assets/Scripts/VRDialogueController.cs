using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.InputSystem; // <-- 1. Aggiungiamo la libreria del Nuovo Input System

public class VRDialogueController : MonoBehaviour
{
    [Header("Tipo di Dialogo")]
    public bool isAutomaticZone = false;

    [Header("Contenuto")]
    public string[] lines;
    private int index = 0;

    [Header("Riferimenti UI (World Space)")]
    public GameObject canvasDialogo;
    public TextMeshProUGUI textMesh;
    public TextMeshProUGUI textMeshHint;
    public GameObject hintTasto;

    [Header("Input (Nuovo Sistema)")]
    // 2. Creiamo un'azione configurabile dall'Inspector
    public InputAction interactAction;

    [Header("Eventi")]
    public UnityEvent onDialogueComplete;

    private bool playerInside = false;
    private bool isTalking = false;

    // 3. Nel nuovo sistema, le azioni vanno "accese" e "spente"
    private void OnEnable()
    {
        interactAction.Enable();
    }

    private void OnDisable()
    {
        interactAction.Disable();
    }

    void Start()
    {
        canvasDialogo.SetActive(false);

        if (hintTasto)
        {
            hintTasto.SetActive(false);
            Debug.Log($"[{gameObject.name}] Setup Iniziale: Hint Tasto trovato e disattivato.");
        }
    }

    void Update()
    {
        // 4. Sostituiamo Input.GetKeyDown con la nuova funzione
        if (playerInside && !isAutomaticZone && interactAction.WasPressedThisFrame())
        {
            Debug.Log($"[{gameObject.name}] Tasto di interazione premuto.");
            if (!isTalking)
                StartDialogue();
            else
                NextLine();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            Debug.Log($"[{gameObject.name}] Il Player è ENTRATO nell'area di trigger.");

            if (isAutomaticZone) StartDialogue();
            else if (hintTasto)
            {
                hintTasto.SetActive(true); 
                textMeshHint.text = interactAction.GetBindingDisplayString(0) + " | " + interactAction.GetBindingDisplayString(1);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            Debug.Log($"[{gameObject.name}] Il Player è USCITO.");

            if (hintTasto) hintTasto.SetActive(false);
            EndDialogue();
        }
    }

    void StartDialogue()
    {
        Debug.Log($"[{gameObject.name}] Inizio Dialogo.");
        isTalking = true;
        index = 0;

        canvasDialogo.SetActive(true);
        if (hintTasto) hintTasto.SetActive(false);

        textMesh.text = lines[index];
    }

    public void NextLine()
    {
        index++;
        if (index < lines.Length)
        {
            textMesh.text = lines[index];
        }
        else
        {
            Debug.Log($"[{gameObject.name}] Dialogo terminato. Attivo evento.");
            EndDialogue();
            onDialogueComplete.Invoke();
        }
    }

    void EndDialogue()
    {
        isTalking = false;
        canvasDialogo.SetActive(false);
    }
}