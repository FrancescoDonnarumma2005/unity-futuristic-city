using UnityEngine;
using UnityEngine.Events;
using InspectionSystem; 

public class InspectionTrigger : MonoBehaviour
{
    [Header("Configurazione")]
    public InspectableItemData itemData;

    [Header("Eventi Interazione")]
    public UnityEvent onHoverEnter; 
    public UnityEvent onHoverExit;  
    
    [Space(10)]
    [Header("Quest Events")]
    // Questo evento parte OGNI VOLTA (per suoni, effetti, ecc.)
    public UnityEvent onInteractionOccurred; 
    
    // NUOVO: Questo evento parte SOLO LA PRIMA VOLTA
    [Tooltip("Collega QUI il QuestCounter. Questo evento scatta una volta sola.")]
    public UnityEvent onFirstInteraction; 

    private bool _hasBeenInteracted = false; // Memoria interna

    public void OnInteract()
    {
        // 1. Logica Ispezione (Sempre)
        if (InspectionManager.Instance != null)
        {
            InspectionManager.Instance.StartInspection(itemData);
        }

        // 2. Evento Generico (Sempre)
        onInteractionOccurred?.Invoke();

        // 3. NUOVO: Logica "One Shot" per le Quest
        if (!_hasBeenInteracted)
        {
            _hasBeenInteracted = true;
            Debug.Log($"[InspectionTrigger] Prima interazione con {gameObject.name}. Punti Quest assegnati.");
            onFirstInteraction?.Invoke();
        }
    }

    public void OnHover(bool isHovering)
    {
        if (isHovering) onHoverEnter.Invoke();
        else onHoverExit.Invoke();
    }
}