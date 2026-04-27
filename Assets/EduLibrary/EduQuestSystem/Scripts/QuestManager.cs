using System;
using System.Collections.Generic;
using UnityEngine;

namespace EduUtils.QuestSystem
{
    /// <summary>
    /// Core manager for the Quest System. 
    /// Designed as a decoupled, reusable library module.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        // Modern Singleton implementation
        public static QuestManager Instance { get; private set; }

        [Header("Configurazione Libreria")]
        [Tooltip("La lista sequenziale delle missioni (Scriptable Objects).")]
        [SerializeField] private List<QuestStepSO> questSequence = new List<QuestStepSO>();

        [Header("Debug")]
        [SerializeField] private int currentIndex = -1;

        // Modern C# Events for decoupled architecture
        public event Action<QuestStepSO> OnQuestStarted;
        public event Action<QuestStepSO> OnQuestCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[QuestManager] Multiple instances detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Inizializza la prima missione se la sequenza è configurata
            if (questSequence != null && questSequence.Count > 0)
            {
                StartQuest(0);
            }
            else
            {
                Debug.LogError("[QuestManager] Quest sequence is empty! Please configure the Inspector.");
            }
        }

        /// <summary>
        /// Avvia una missione specifica basata sull'indice.
        /// </summary>
       private void StartQuest(int index)
        {
            // Se siamo andati oltre l'ultima missione, il gioco è finito
            if (index < 0 || index >= questSequence.Count)
            {
                Debug.Log("[QuestManager] Sequenza missioni terminata!");
                currentIndex = questSequence.Count; // Imposta l'indice in modo sicuro "fuori limite"
                OnQuestStarted?.Invoke(null); // INVIA UN SEGNALE NULL: Avvisa tutti che non ci sono più missioni
                return;
            }

            currentIndex = index;
            QuestStepSO newQuest = questSequence[currentIndex];
            
            Debug.Log($"[QuestManager] Missione Iniziata [{currentIndex}]: {newQuest.title}");
            
            // Invoke sicuro con null-conditional operator
            OnQuestStarted?.Invoke(newQuest);
        }

        /// <summary>
        /// Completa la missione corrente e avvia automaticamente la successiva.
        /// Chiamato da QuestWaypoint, contatori o interazioni.
        /// </summary>
        public void CompleteCurrentQuest()
        {
            if (currentIndex < 0 || currentIndex >= questSequence.Count)
            {
                return; 
            }

            QuestStepSO completedQuest = questSequence[currentIndex];
            Debug.Log($"[QuestManager] Missione Completata [{currentIndex}]: {completedQuest.title}");
            
            OnQuestCompleted?.Invoke(completedQuest);

            // Passa alla missione successiva
            StartQuest(currentIndex + 1);
        }

        /// <summary>
        /// Restituisce la missione attualmente attiva. 
        /// Fondamentale per la sincronizzazione tardiva delle UI.
        /// </summary>
        public QuestStepSO GetCurrentQuest()
        {
            if (questSequence != null && currentIndex >= 0 && currentIndex < questSequence.Count)
            {
                return questSequence[currentIndex];
            }
            return null;
        }

        /// <summary>
        /// Restituisce l'indice della missione corrente.
        /// </summary>
        public int GetCurrentStepIndex()
        {
            return currentIndex;
        }

        /// <summary>
    /// Calcola il numero effettivo della missione principale in corso, ignorando i passaggi intermedi.
    /// </summary>
    public int GetMainQuestDisplayNumber()
    {
        int mainCount = 0;
        for (int i = 0; i <= currentIndex; i++)
        {
            if (questSequence[i].isMainQuest)
            {
                mainCount++;
            }
        }
        return mainCount; // Restituisce 1 per la prima missione principale, 2 per la seconda, ecc.
    }
    }
}