using UnityEngine;
using UnityEngine.Events;

namespace EduUtils.QuestSystem
{
    public class QuestCounter : MonoBehaviour
    {
        [Header("Impostazioni")]
        [Tooltip("Quanti oggetti deve attivare il giocatore?")]
        public int requiredCount = 3;
        
        [Tooltip("La pietra miliare da completare quando si raggiunge il numero")]
        public QuestWaypoint targetWaypoint;

        [Header("UI Feedback")]
        // Evento opzionale per aggiornare una scritta a schermo (es. "1/3")
        public UnityEvent<string> OnProgressUpdated; 

        private int _currentCount = 0;

        



        // Funzione da collegare nell'evento delle Anfore
        public void IncrementProgress()
        {
            _currentCount++;
            
            // Debug e UI
            Debug.Log($"[QuestCounter] Progresso: {_currentCount}/{requiredCount}");
            OnProgressUpdated?.Invoke($"{_currentCount}/{requiredCount}");

            if (_currentCount >= requiredCount)
            {
                if (targetWaypoint != null)
                {
                    targetWaypoint.ForceComplete();
                }
            }
        }

        // Chiama questo per forzare la scritta "0/3" all'inizio
    public void InitializeCounter()
    {
        _currentCount = 0; // Reset di sicurezza
        string status = $"{_currentCount}/{requiredCount}";
        Debug.Log($"[QuestCounter] Inizializzazione UI: {status}");
        OnProgressUpdated?.Invoke(status);
    }

    
    }
}