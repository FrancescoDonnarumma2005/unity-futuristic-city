using UnityEngine;
using System.Collections.Generic;
using System;
using InspectionSystem; // Necessario per leggere il tuo InspectableItemData

namespace EduUtils.InventorySystem
{
    public class CollectionManager : MonoBehaviour
    {
        public static CollectionManager Instance { get; private set; }

        [Header("Stato Collezione")]
        [Tooltip("La lista degli oggetti attualmente sbloccati dal giocatore")]
        [SerializeField] private List<InspectableItemData> _collectedItems = new List<InspectableItemData>();

        // Evento che la UI ascolterà per aggiornarsi (senza controllare ogni frame)
        public event Action<InspectableItemData> OnItemUnlocked;

        private void Awake()
        {
            // Setup del Singleton
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Opzionale: mantiene l'inventario tra i cambi di scena
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Chiamata quando il giocatore trova un oggetto.
        /// </summary>
        public void UnlockItem(InspectableItemData itemData)
        {
            if (itemData == null) return;

            // Se l'abbiamo già preso, non fare nulla (evita duplicati e spam di suoni)
            if (_collectedItems.Contains(itemData))
            {
                // Qui potremmo mettere un debug: "Hai già questo oggetto!"
                return;
            }

            // Aggiungi alla lista
            _collectedItems.Add(itemData);
            
            Debug.Log($"<color=green>NUOVO OGGETTO SBLOCCATO:</color> {itemData.itemName}");

            // Avvisa chiunque sia in ascolto (la UI, il sistema audio, ecc.)
            OnItemUnlocked?.Invoke(itemData);
        }

        // Metodo utile per la UI per sapere se disegnare il lucchetto o l'icona
        public bool HasItem(InspectableItemData item)
        {
            return _collectedItems.Contains(item);
        }
        
        // Metodo per ottenere tutta la lista (per costruire la UI all'inizio)
        public List<InspectableItemData> GetCollectedItems()
        {
            return _collectedItems;
        }
    }
}