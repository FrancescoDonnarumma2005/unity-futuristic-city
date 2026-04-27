using UnityEngine;

namespace EduLibrary.MinimapSystem
{
    public class MilestoneUnlocker : MonoBehaviour
    {
        [Tooltip("L'ID esatto del bottone nell'UI Builder (es. poi-anfiteatro)")]
        public string mapElementID;
        [Tooltip("Il nome da mostrare nella notifica a schermo")]
        public string displayName;

        public void TriggerUnlock()
        {
            FastTravelManager.UnlockPOI(mapElementID, displayName);
        }
    }
}