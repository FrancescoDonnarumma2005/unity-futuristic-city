using UnityEngine;
using UnityEngine.UI;
using InspectionSystem; // Necessario per leggere i dati dei tuoi oggetti

namespace EduUtils.InventorySystem
{
    public class InventorySlot : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("L'immagine che mostrerà lo sprite dell'oggetto")]
        [SerializeField] private Image _iconImage;
        
        [Tooltip("L'immagine del lucchetto sovrapposto")]
        [SerializeField] private Image _lockOverlay;
        
        [Tooltip("Il componente bottone")]
        [SerializeField] private Button _button;

        // Dati interni
        private InspectableItemData _myData;
        private System.Action<InspectableItemData> _onClickCallback;

        /// <summary>
        /// Configura lo slot con i dati, lo stato di sblocco e l'azione da eseguire al click.
        /// </summary>
        public void Setup(InspectableItemData data, bool isUnlocked, System.Action<InspectableItemData> onClickCallback)
        {
            _myData = data;
            _onClickCallback = onClickCallback;

            // Imposta l'icona se presente
            if (data.icon != null) 
                _iconImage.sprite = data.icon;

            // Gestione visiva stato Bloccato/Sbloccato
            if (isUnlocked)
            {
                _iconImage.color = Color.white;           // Colore pieno originale
                _lockOverlay.gameObject.SetActive(false); // Nascondi lucchetto
                _button.interactable = true;              // Rendi cliccabile
            }
            else
            {
                _iconImage.color = Color.gray;            // Grigio (visibile su sfondo scuro)
                _lockOverlay.gameObject.SetActive(true);  // Mostra lucchetto
                _button.interactable = false;             // Non cliccabile
            }

            // Reset dei listener per evitare click multipli se lo slot viene riciclato
            _button.onClick.RemoveAllListeners();
            
            // Aggiungi il nuovo listener
            _button.onClick.AddListener(OnSlotClicked);
        }

        private void OnSlotClicked()
        {
            // Invia i dati di questo oggetto a chi sta ascoltando (InventoryUI)
            _onClickCallback?.Invoke(_myData);
        }
    }
}