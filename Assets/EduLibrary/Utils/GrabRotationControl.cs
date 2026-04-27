using UnityEngine;
using UnityEngine.InputSystem;

namespace EduUtils
{
    /// <summary>
    /// Permette di ruotare l'oggetto a cui è attaccato sull'asse Y.
    /// E' pensato per essere usato quando il giocatore sta tenendo in mano l'oggetto (ad es. un Beaker).
    /// </summary>
    public class GrabRotationControl : MonoBehaviour
    {
        [Header("Impostazioni Rotazione")]
        [Tooltip("Velocità di rotazione in gradi al secondo.")]
        public float rotationSpeed = 45f;
        
        [Tooltip("Specifica se questo oggetto sta venendo tenuto in mano. Se gestito da un sistema di presa VR o custom, impostalo esternamente.")]
        public bool isGrabbed = true; // Di default true per facilitare i test da Desktop, ma in produzioni vere verrà attivato dallo script di presa.

        void Update()
        {
            // Eseguiamo la rotazione solo se l'oggetto è attualmente afferrato dal giocatore
            if (!isGrabbed) return;

            // Riferimento alla tastiera corrente
            if (Keyboard.current == null) return;

            // Variabile per accumulare l'input di rotazione
            float rotationInput = 0f;

            // Controlliamo se i tasti Q (sinistra) o E (destra) sono tenuti premuti
            if (Keyboard.current.eKey.isPressed)
            {
                rotationInput = 1f; // Ruota verso sinistra (senso antiorario)
            }
            if (Keyboard.current.qKey.isPressed)
            {
                rotationInput = -1f; // Ruota verso destra (senso orario) se li premi entrambi la somma fa 0.
            }

            // Applichiamo la rotazione attorno all'asse verticale (Y locale dell'oggetto, puoi cambiarlo in Vector3.up per asse globale)
            // Lavoriamo su Z per il becco, o Y se vuoi girarlo su se stesso. Nel caso del gettar liquido spesso è Z o X, calcoliamo su X.
            // NOTA: il Beaker tipicamente si inclina in avanti/indietro rispetto alla camera quando versi.
            // Se "ruotare a destra/sinistra" intendi inclinarlo per versare (Roll o Pitch), modifichiamo l'asse.
            
            // Qui usiamo l'asse Z locale come pivot tipico per inclinare i liquidi a destra/sinistra. 
            // Se invece vuoi ruotarlo su se stesso (Yaw) bisogna usare Vector3.up (asse Y).
            if (rotationInput != 0f)
            {
                // Incliniamo l'oggetto! (Sostituisci Vector3.forward con Vector3.up se vuoi farlo girare invece che inclinare)
                transform.Rotate(Vector3.forward * rotationInput * rotationSpeed * Time.deltaTime, Space.Self);
            }
        }
    }
}
