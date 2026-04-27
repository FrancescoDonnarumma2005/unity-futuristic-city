using UnityEngine;

namespace EduUtils.QuestSystem // Namespace per proteggere il codice
{
    [CreateAssetMenu(fileName = "NewQuestStep", menuName = "EduQuest/Quest Step")]
    public class QuestStepSO : ScriptableObject
    {
        [Header("Dati Generici")]
        public string id; // ID univoco (es. "quest_01")
        public string title; // Es: "Benvenuto a Roma"
        [TextArea] public string description; // Es: "Attraversa l'Arco..."
        
        [Header("Feedback UI")]
        public Sprite icon; // Icona generica per la UI

        [Header("Comportamento UI Libreria")]
    [Tooltip("Se disattivato, nasconde la dicitura 'MISSIONE X' ed è considerata un passaggio intermedio.")]
    public bool isMainQuest = true;
    
    [Tooltip("Se disattivato, non mostra il popup 'Missione Completata' al termine di questo step.")]
    public bool showCompletionPopup = true;

    [Tooltip("Testo personalizzato per il completamento. Se vuoto, usa il default 'MISSIONE COMPLETATA'.")]
    public string customCompletionText = "";
    }
}