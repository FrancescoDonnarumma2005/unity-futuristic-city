using UnityEngine;
using InspectionSystem;

namespace EduUtils.InventorySystem
{
    public class ItemUnlocker : MonoBehaviour
    {
        [Header("Configurazione")]
        [Tooltip("Trascina qui lo ScriptableObject corrispondente a questo oggetto 3D")]
        [SerializeField] private InspectableItemData _itemData;

        [Header("Debug")]
        [SerializeField] private bool _unlockOnStart = false; // Utile per test veloci senza VR

        private void Start()
        {
            if (_unlockOnStart)
            {
                UnlockNow();
            }
        }

        // Questo è il metodo pubblico che collegheremo all'evento dell'XR Interaction Toolkit
        public void UnlockNow()
        {
            if (CollectionManager.Instance != null)
            {
                CollectionManager.Instance.UnlockItem(_itemData);
            }
            else
            {
                Debug.LogError("CollectionManager non trovato nella scena! Hai creato l'oggetto vuoto?");
            }
        }
    }
}