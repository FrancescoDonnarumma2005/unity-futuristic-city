using UnityEngine;
using TMPro;
using EduUtils.Interaction; 

[RequireComponent(typeof(AudioSource))] // Best practice: assicura che Unity aggiunga in automatico un AudioSource al GameObject
public class VRGuideToggle : MonoBehaviour, IInteractable
{
    [Header("Riferimenti UI")]
    [Tooltip("Il pannello principale contenente le istruzioni")]
    [SerializeField] private GameObject mainPanel;
    
    [Tooltip("Il testo all'interno del pulsante toggle")]
    [SerializeField] private TextMeshProUGUI toggleButtonText;

    [Header("Impostazioni Testo")]
    [SerializeField] private string textWhenClosed = "GUIDA";
    [SerializeField] private string textWhenOpen = "CHIUDI";

    [Header("Audio Feedback")]
    [Tooltip("Suono riprodotto all'apertura del pannello")]
    [SerializeField] private AudioClip openSound;
    [Tooltip("Suono riprodotto alla chiusura del pannello")]
    [SerializeField] private AudioClip closeSound;

    private AudioSource audioSource;
    private bool isOpen = false;

    private void Awake()
    {
        // Cache della reference per evitare l'uso di GetComponent a runtime (Zero-Allocation)
        audioSource = GetComponent<AudioSource>();
        
        // Setup per la VR: Forza il suono ad essere 3D in modo che provenga fisicamente dal pannello
        audioSource.spatialBlend = 1f; 
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        // Assicurati che parta sempre chiuso e con il testo corretto
        mainPanel.SetActive(false);
        toggleButtonText.text = textWhenClosed;
    }

    public void OnInteract()
    {
        ToggleGuide();
    }

    public void OnHover(bool isHovering)
    {
        // Spazio riservato per feedback visivo (es. cambio colore o ingrandimento del tasto)
    }

    public void ToggleGuide()
    {
        isOpen = !isOpen;
        mainPanel.SetActive(isOpen);
        
        // Cambia il testo del bottone in base allo stato
        toggleButtonText.text = isOpen ? textWhenOpen : textWhenClosed;

        // --- RIPRODUZIONE AUDIO ---
        if (isOpen && openSound != null)
        {
            audioSource.PlayOneShot(openSound);
        }
        else if (!isOpen && closeSound != null)
        {
            audioSource.PlayOneShot(closeSound);
        }
    }
}