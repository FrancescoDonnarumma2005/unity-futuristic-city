using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace EduUtils.QuestSystem
{
    /// <summary>
    /// Modulo riutilizzabile per gestire indicatori visivi e trigger di completamento missioni.
    /// </summary>
    public class QuestWaypoint : MonoBehaviour
    {
        [Header("Configurazione Visiva (Luce/FX)")]
        [Tooltip("Inserisci qui tutte le missioni durante le quali l'anello deve rimanere acceso.")]
        [SerializeField] private List<QuestStepSO> visibleDuringQuests = new List<QuestStepSO>();
        [SerializeField] private GameObject visualEffectsParent;

        [Header("Configurazione Logica (Completamento)")]
        [Tooltip("La specifica missione che questo oggetto chiude (es. cliccando o entrandoci).")]
        [SerializeField] private QuestStepSO questToComplete;
        [SerializeField] private bool autoCompleteOnEnter = false;

        [Header("Eventi Esterni")]
        public UnityEvent onWaypointActivated;
        public UnityEvent onPlayerArrivedInZone;

        private bool canCompleteCurrent = false;

        private void Start()
        {
            if (visualEffectsParent) visualEffectsParent.SetActive(false);

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestStarted += HandleQuestState;
                HandleQuestState(QuestManager.Instance.GetCurrentQuest());
            }
        }

        private void OnDestroy()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestStarted -= HandleQuestState;
            }
        }

        private void HandleQuestState(QuestStepSO currentQuest)
        {
            // NUOVO: Se riceviamo "null", significa che il gioco è finito. Spegniamo tutto!
            if (currentQuest == null)
            {
                if (visualEffectsParent != null) visualEffectsParent.SetActive(false);
                canCompleteCurrent = false;
                return;
            }

            // 1. Gestione della Luce (rimane accesa se la missione attuale è nella lista)
            bool isVisible = visibleDuringQuests.Contains(currentQuest);
            if (visualEffectsParent != null)
            {
                visualEffectsParent.SetActive(isVisible);
            }

            // 2. Gestione dell'Interazione (si sblocca SOLO per la sua missione di completamento)
            canCompleteCurrent = (currentQuest == questToComplete);
            
            if (canCompleteCurrent)
            {
                onWaypointActivated?.Invoke();
            }
        }

        /// <summary>
        /// Metodo da chiamare tramite IInteractable o pulsanti UI.
        /// </summary>
        public void ForceComplete()
        {
            if (canCompleteCurrent)
            {
                QuestManager.Instance.CompleteCurrentQuest();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!canCompleteCurrent) return;

            // Assicurati che il controller del giocatore abbia il tag "Player"
            if (other.CompareTag("Player")) 
            {
                onPlayerArrivedInZone?.Invoke();

                if (autoCompleteOnEnter)
                {
                    ForceComplete();
                }
            }
        }
    }
}