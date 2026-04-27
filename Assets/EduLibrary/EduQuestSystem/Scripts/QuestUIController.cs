using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI; // NUOVO: Necessario per controllare l'Image di sfondo

namespace EduUtils.QuestSystem
{
    public class QuestUIController : MonoBehaviour
    {
        [Header("Riferimenti UI")]
        [SerializeField] private TextMeshProUGUI _questNumberText; 
        [SerializeField] private TextMeshProUGUI _questTitleText;
        [SerializeField] private TextMeshProUGUI _questDescriptionText;
        [SerializeField] private TextMeshProUGUI _questProgressText; 
        [SerializeField] private GameObject _separatorLine; 
        
        [Header("Pannello Animato")]
        [SerializeField] private CanvasGroup _panelCanvasGroup;
        
        // NUOVO: Riferimento all'immagine di sfondo
        [SerializeField] private Image _panelBackgroundImage; 
        
        [Header("Feedback")]
        [SerializeField] private GameObject _questCompletePopup; 
        [SerializeField] private TextMeshProUGUI _completionPopupText; 
        [SerializeField] private float _animationDuration = 0.5f;

        private bool _isShowingCompletion = false;
        private QuestStepSO _pendingQuest = null;

        private void Start() 
        {
            if (_questCompletePopup != null) _questCompletePopup.SetActive(false);
            
            if (QuestManager.Instance != null) 
            {
                QuestManager.Instance.OnQuestStarted += UpdateUI;
                QuestManager.Instance.OnQuestCompleted += ShowCompletionEffect;
                
                QuestStepSO initialQuest = QuestManager.Instance.GetCurrentQuest();
                if (initialQuest != null)
                {
                    StartCoroutine(FadeInContent(initialQuest));
                }
            }
        }

        private void OnDestroy() 
        {
            if (QuestManager.Instance != null) 
            {
                QuestManager.Instance.OnQuestStarted -= UpdateUI;
                QuestManager.Instance.OnQuestCompleted -= ShowCompletionEffect;
            }
        }

        public void UpdateProgressDisplay(string progressText)
        {
            if (_questProgressText != null)
            {
                _questProgressText.text = progressText;
                _questProgressText.gameObject.SetActive(true);
            }
        }

        private void UpdateUI(QuestStepSO newQuest) 
        {
            if (_isShowingCompletion)
            {
                _pendingQuest = newQuest;
            }
            else
            {
                StartCoroutine(FadeInContent(newQuest));
            }
        }

        private void ShowCompletionEffect(QuestStepSO completedQuest) 
        {
            if (!completedQuest.showCompletionPopup) return;

            if (_completionPopupText != null)
            {
                _completionPopupText.text = string.IsNullOrEmpty(completedQuest.customCompletionText) 
                    ? "MISSIONE COMPLETATA" 
                    : completedQuest.customCompletionText.ToUpper();
            }
            
            StartCoroutine(SequenceCompletion());
        }

        private IEnumerator SequenceCompletion() 
        {
            _isShowingCompletion = true;

            yield return StartCoroutine(FadeCanvasGroup(1, 0, _animationDuration));
            
            ToggleStandardTexts(false);

            if (_questCompletePopup != null) _questCompletePopup.SetActive(true);
            
            yield return StartCoroutine(FadeCanvasGroup(0, 1, _animationDuration));

            yield return new WaitForSeconds(2.0f); 
            
            yield return StartCoroutine(FadeCanvasGroup(1, 0, _animationDuration));

            if (_questCompletePopup != null) _questCompletePopup.SetActive(false);

            _isShowingCompletion = false;

            if (_pendingQuest != null)
            {
                StartCoroutine(FadeInContent(_pendingQuest));
                _pendingQuest = null;
            }
        }

        private IEnumerator FadeInContent(QuestStepSO quest)
        {
            _panelCanvasGroup.alpha = 0;

            ToggleStandardTexts(true);

            if (quest.isMainQuest)
            {
                if (_questNumberText != null)
                {
                    int realIndex = QuestManager.Instance.GetMainQuestDisplayNumber();
                    _questNumberText.text = $"MISSIONE {IntToRoman(realIndex)}";
                }
            }
            else
            {
                if (_questNumberText != null) _questNumberText.gameObject.SetActive(false);
            }

            if (_questTitleText) _questTitleText.text = quest.title.ToUpper();
            if (_questDescriptionText) _questDescriptionText.text = quest.description;
            if (_questProgressText) _questProgressText.gameObject.SetActive(false);

            yield return StartCoroutine(FadeCanvasGroup(0, 1, _animationDuration));
        }

        private void ToggleStandardTexts(bool state)
        {
            if (_questNumberText != null) _questNumberText.gameObject.SetActive(state);
            if (_questTitleText != null) _questTitleText.gameObject.SetActive(state);
            if (_questDescriptionText != null) _questDescriptionText.gameObject.SetActive(state);
            if (_questProgressText != null) _questProgressText.gameObject.SetActive(state);
            if (_separatorLine != null) _separatorLine.SetActive(state); 
            
            // NUOVO: Abilita o disabilita il rendering dello sfondo
            if (_panelBackgroundImage != null) _panelBackgroundImage.enabled = state;
        }

        private IEnumerator FadeCanvasGroup(float start, float end, float duration) 
        {
            float elapsed = 0f;
            while (elapsed < duration) 
            {
                elapsed += Time.deltaTime;
                _panelCanvasGroup.alpha = Mathf.Lerp(start, end, elapsed / duration);
                yield return null;
            }
            _panelCanvasGroup.alpha = end;
        }

        private string IntToRoman(int n) 
        {
             string[] romans = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
             return (n > 0 && n < romans.Length) ? romans[n] : n.ToString();
        }
    }
}