using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using InspectionSystem;
using TMPro;
using UnityEngine.Events; 

namespace EduUtils.InventorySystem
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Riferimenti Struttura UI")]
        [SerializeField] private GameObject _visualContainer; 
        [SerializeField] private GameObject _gameHUD; 
        [SerializeField] private Transform _gridContent; 
        [SerializeField] private GameObject _slotPrefab;

        [Header("Elementi Informativi (Progresso)")]
        [SerializeField] private TextMeshProUGUI _progressText; 
        [SerializeField] private Slider _progressBar;

        [Header("Controllo Giocatore")]
        [SerializeField] private MonoBehaviour _playerControllerScript;

        [Header("Impostazioni Input")]
        [SerializeField] private KeyCode _toggleKey = KeyCode.R;

        [Header("Database Completo")]
        [SerializeField] private List<InspectableItemData> _allItemsInGame;

        [Header("Eventi Completamento")]
        [Tooltip("Si attiva automaticamente quando la barra raggiunge il 100%")]
        public UnityEvent OnCollectionComplete;
        
        private bool _isOpen = false;
        private bool _hasTriggeredCompletion = false; 

        private void Start()
        {
            GenerateGrid();
            
            if (_visualContainer != null) _visualContainer.SetActive(false);
            if (_gameHUD != null) _gameHUD.SetActive(true);

            if (CollectionManager.Instance != null)
            {
                CollectionManager.Instance.OnItemUnlocked += RefreshGridState;
            }
            
            UpdateProgressUI();
            CheckForCompletion(); // Controllo iniziale di sicurezza
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                if (InspectionManager.Instance != null && InspectionManager.Instance.IsCurrentlyInspecting)
                {
                    return; 
                }

                ToggleInventory();
            }
        }

        private void OnDestroy()
        {
             if (CollectionManager.Instance != null)
            {
                CollectionManager.Instance.OnItemUnlocked -= RefreshGridState;
            }
        }

        public void ToggleInventory()
        {
            _isOpen = !_isOpen;
            
            if (_visualContainer != null) _visualContainer.SetActive(_isOpen);
            if (_gameHUD != null) _gameHUD.SetActive(!_isOpen);

            if (_isOpen)
            {
                Time.timeScale = 0f; 
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                if (_playerControllerScript != null) _playerControllerScript.enabled = false;

                GenerateGrid();
                UpdateProgressUI();
            }
            else
            {
                Time.timeScale = 1f; 
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                if (_playerControllerScript != null) _playerControllerScript.enabled = true;
            }
        }

        private void GenerateGrid()
        {
            foreach (Transform child in _gridContent)
            {
                Destroy(child.gameObject);
            }

            foreach (var itemData in _allItemsInGame)
            {
                GameObject newSlot = Instantiate(_slotPrefab, _gridContent);
                InventorySlot slotScript = newSlot.GetComponent<InventorySlot>();
                
                if (slotScript != null)
                {
                    bool isUnlocked = CollectionManager.Instance.HasItem(itemData);
                    slotScript.Setup(itemData, isUnlocked, HandleItemSelection);
                }
            }
        }

        private void UpdateProgressUI()
        {
            if (_allItemsInGame == null || _allItemsInGame.Count == 0) return;

            int totalItems = _allItemsInGame.Count;
            int unlockedItems = CountUnlockedItems();

            if (_progressText != null)
            {
                _progressText.text = $"Hai scoperto {unlockedItems} oggetti su {totalItems}";
            }

            if (_progressBar != null)
            {
                _progressBar.maxValue = totalItems;
                _progressBar.value = unlockedItems;
            }
        }

        // --- NUOVA LOGICA: Funzione dedicata al controllo della vittoria ---
        private void CheckForCompletion()
        {
            if (_hasTriggeredCompletion || _allItemsInGame == null || _allItemsInGame.Count == 0) return;

            int totalItems = _allItemsInGame.Count;
            int unlockedItems = CountUnlockedItems();

            if (unlockedItems == totalItems)
            {
                _hasTriggeredCompletion = true;
                Debug.Log("<color=yellow>COMPLETAMENTO: Tutti gli oggetti trovati!</color>");
                OnCollectionComplete?.Invoke();
            }
        }

        // --- AGGIORNAMENTO: Questo scatta in tempo reale appena sblocchi un oggetto ---
        private void RefreshGridState(InspectableItemData newItem)
        {
            // 1. Controlliamo se abbiamo vinto (indipendentemente dal fatto che la UI sia aperta o chiusa)
            CheckForCompletion();

            // 2. Aggiorniamo la grafica solo se stiamo guardando l'inventario
            if (_isOpen) 
            {
                GenerateGrid();
                UpdateProgressUI();
            }
        }

        private int CountUnlockedItems()
        {
            int count = 0;
            foreach (var item in _allItemsInGame)
            {
                if (CollectionManager.Instance.HasItem(item)) count++;
            }
            return count;
        }

        private void HandleItemSelection(InspectableItemData selectedItem)
        {
            ToggleInventory(); 

            if (InspectionManager.Instance != null)
            {
                InspectionManager.Instance.StartInspection(selectedItem);
            }
        }
    }
}