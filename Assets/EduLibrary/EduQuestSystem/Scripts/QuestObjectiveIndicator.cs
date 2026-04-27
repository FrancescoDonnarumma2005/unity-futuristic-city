using UnityEngine;

namespace EduUtils.QuestSystem
{
    public class QuestObjectiveIndicator : MonoBehaviour
    {
        [Header("Configurazione")]
        [Tooltip("La missione durante la quale questo indicatore deve accendersi.")]
        [SerializeField] private QuestStepSO targetQuest;

        [Tooltip("Il GameObject che contiene l'effetto luminoso/particellare.")]
        [SerializeField] private GameObject visualIndicator;

        private bool _hasBeenInteracted;

        private void Start()
        {
            // L'iscrizione avviene in Start per garantire che QuestManager.Awake
            // abbia già inizializzato l'istanza Singleton.
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestStarted += HandleQuestStarted;
                
                // Forza l'allineamento immediato nel caso la missione sia già in corso
                HandleQuestStarted(QuestManager.Instance.GetCurrentQuest());
            }
            else
            {
                Debug.LogWarning($"[QuestObjectiveIndicator] {gameObject.name} non trova il QuestManager all'avvio.");
                if (visualIndicator != null) visualIndicator.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestStarted -= HandleQuestStarted;
            }
        }

        private void HandleQuestStarted(QuestStepSO startedQuest)
        {
            if (_hasBeenInteracted || visualIndicator == null) 
            {
                return;
            }

            if (startedQuest == null)
            {
                visualIndicator.SetActive(false);
                return;
            }

            bool isTargetQuest = (startedQuest == targetQuest);
            visualIndicator.SetActive(isTargetQuest);

            // Debug opzionale per confermare l'attivazione
            if (isTargetQuest)
            {
                Debug.Log($"[QuestObjectiveIndicator] Effetto attivato su {gameObject.name} per la missione {startedQuest.title}");
            }
        }

        public void DisableIndicatorOnInteract()
        {
            if (_hasBeenInteracted) return;

            _hasBeenInteracted = true;

            if (visualIndicator != null)
            {
                visualIndicator.SetActive(false);
            }
        }
    }
}