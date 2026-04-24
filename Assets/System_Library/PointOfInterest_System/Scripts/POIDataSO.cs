using UnityEngine;

namespace FuturisticCity.EduLibrary.POI
{
    [CreateAssetMenu(fileName = "NuovaComunicazioneRadio", menuName = "EduLibrary/POI Radio Data")]
    public class POIDataSO : ScriptableObject
    {
        public string title = "Comunicazione Radio";
        
        [Tooltip("Ogni riga verrà mostrata in sequenza")]
        [TextArea(3, 10)]
        public string[] dialogueLines;
        
        [Tooltip("Secondi di permanenza per ogni singola riga")]
        public float timePerLine = 3.5f;    
    }
}