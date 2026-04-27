using UnityEngine;
using EduUtils.QuestSystem;

namespace EduUtils.AudioSystem
{
    [RequireComponent(typeof(AudioSource))]
    public class QuestAudioManager : MonoBehaviour
    {
        [Header("Tracce Audio Missioni")]
        [Tooltip("Suono riprodotto all'interazione con la pietra miliare (inizio missione principale)")]
        [SerializeField] private AudioClip _questAssignedClip;
        
        [Tooltip("Suono riprodotto all'apparizione del popup MISSIONE COMPLETATA")]
        [SerializeField] private AudioClip _questCompletedClip;

        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.spatialBlend = 0f; 
            _audioSource.playOnAwake = false;
        }

        private void Start()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestStarted += HandleQuestAssigned;
                QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
            }
        }

        private void OnDestroy()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestStarted -= HandleQuestAssigned;
                QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
            }
        }

        private void HandleQuestAssigned(QuestStepSO newQuest)
        {
            if (newQuest == null) return;

            // Il suono scatta SOLO se la nuova missione è etichettata come Principale (es. l'Arco di Trionfo)
            if (newQuest.isMainQuest)
            {
                if (_questAssignedClip != null)
                {
                    _audioSource.PlayOneShot(_questAssignedClip);
                }
            }
        }

        private void HandleQuestCompleted(QuestStepSO completedQuest)
        {
            if (completedQuest == null) return;

            // Il suono scatta SOLO se la missione appena conclusa prevede l'apparizione del popup a schermo
            if (completedQuest.showCompletionPopup)
            {
                if (_questCompletedClip != null)
                {
                    _audioSource.PlayOneShot(_questCompletedClip);
                }
            }
        }

        public void PlayQuestCompletedSound()
        {
            if (_audioSource != null && _questCompletedClip != null)
            {
                _audioSource.PlayOneShot(_questCompletedClip);
            }
        }
    }
}