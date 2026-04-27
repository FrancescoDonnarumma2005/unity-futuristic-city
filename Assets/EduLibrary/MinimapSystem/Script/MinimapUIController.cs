using UnityEngine;
using UnityEngine.UIElements;

namespace EduLibrary.MinimapSystem
{
    /// <summary>
    /// Controller ottimizzato per aggiornare gli elementi UI della minimappa.
    /// Evita allocazioni in LateUpdate usando struct (StyleRotate, Rotate, Angle).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MinimapUIController : MonoBehaviour
    {
        [Header("Riferimenti Scena")]
        [Tooltip("Il transform del giocatore (DesktopFirstPersonController)")]
        [SerializeField] private Transform playerTransform;

        private UIDocument uiDocument;
        private VisualElement playerIcon;

        private void OnEnable()
        {
            uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;

            // Query dell'elemento UI tramite il suo nome
            playerIcon = root.Q<VisualElement>("player-icon");

            if (playerIcon == null)
            {
                Debug.LogWarning("MinimapUIController: 'player-icon' non trovato nel documento UXML.");
            }
        }

        private void LateUpdate()
        {
            if (playerTransform == null || playerIcon == null) return;

            // Leggiamo la rotazione sull'asse Y del giocatore
            float playerYaw = playerTransform.eulerAngles.y;

            // Applichiamo la rotazione all'elemento UI Toolkit.
            // Nota: Utilizziamo i tipi struct nativi di UI Toolkit per evitare Garbage Collection.
            playerIcon.style.rotate = new StyleRotate(new Rotate(Angle.Degrees(playerYaw)));
        }

        /// <summary>
        /// Metodo pubblico per aggiornare il target dinamicamente, utile quando
        /// il GameplayModeManager effettua lo switch tra VR e Desktop.
        /// </summary>
        public void SetPlayerTarget(Transform newPlayerTransform)
        {
            playerTransform = newPlayerTransform;
        }
    }
}