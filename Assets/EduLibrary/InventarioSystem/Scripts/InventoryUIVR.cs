using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using InspectionSystem;
using TMPro;
using UnityEngine.Events;

namespace EduUtils.InventorySystem
{
    public class InventoryUIVR : MonoBehaviour
    {
        [Header("Riferimenti Struttura UI")]
        [Tooltip("Il pannello principale dell'inventario da accendere/spegnere")]
        [SerializeField] private GameObject _visualContainer; 
        [SerializeField] private Transform _gridContent; 
        [SerializeField] private GameObject _slotPrefab;

        [Header("Elementi Informativi (Progresso)")]
        [SerializeField] private TextMeshProUGUI _progressText; 
        [SerializeField] private Slider _progressBar;

        [Header("Impostazioni VR")]
        [Tooltip("L'azione del controller (lasciata per compatibilità con l'Editor)")]
        [SerializeField] private InputActionReference _toggleAction;
        
        [Tooltip("La telecamera del visore (Main Camera) per capire dove sta guardando il giocatore")]
        [SerializeField] private Transform _vrCamera;
        
        [Tooltip("Distanza in metri a cui appare il menu rispetto alla faccia del giocatore")]
        [SerializeField] private float _spawnDistance = 1.0f;
        
        [Tooltip("Oggetto che contiene il Locomotion System da disabilitare quando l'inventario è aperto")]
        [SerializeField] private GameObject _vrLocomotionSystem;

        [Header("Database Completo")]
        [SerializeField] private List<InspectableItemData> _allItemsInGame;

        [Header("Eventi Completamento")]
        [Tooltip("Si attiva automaticamente quando la barra raggiunge il 100%")]
        public UnityEvent OnCollectionComplete;
        
        private bool _isOpen = false;
        private bool _hasTriggeredCompletion = false; 
        
        // Memoria per il fix di WebGL sull'Input System
        private bool _isActionPressedMem = false; 

        private void Start()
        {
            GenerateGrid();
            
            if (_visualContainer != null) 
                _visualContainer.SetActive(false);

            if (CollectionManager.Instance != null)
            {
                CollectionManager.Instance.OnItemUnlocked += RefreshGridState;
            }
            
            UpdateProgressUI();
            CheckForCompletion(); 
        }

        private void OnEnable()
        {
            if (_toggleAction != null && _toggleAction.action != null)
            {
                _toggleAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (_toggleAction != null && _toggleAction.action != null)
            {
                _toggleAction.action.Disable();
            }
        }

        private void OnDestroy()
        {
             if (CollectionManager.Instance != null)
            {
                CollectionManager.Instance.OnItemUnlocked -= RefreshGridState;
            }
        }

        private void Update()
        {
            bool isTriggeredThisFrame = false;

            // 2. INPUT SYSTEM CORAZZATO (Bypass del bug WebGL sui frame persi)
            if (_toggleAction != null && _toggleAction.action != null)
            {
                // WebGL odia "WasPressedThisFrame". Usiamo "IsPressed" per leggere la corrente continua del bottone.
                bool currentlyPressed = _toggleAction.action.IsPressed();
                
                if (currentlyPressed && !_isActionPressedMem)
                {
                    Debug.Log("<color=cyan>INVENTARIO: Tasto Letto via IsPressed (Input System)!</color>");
                    isTriggeredThisFrame = true;
                }
                _isActionPressedMem = currentlyPressed;
            }

            // 3. SPIA GAMEPAD HTML5 (Il segreto di WebXR)
            // WebXR spesso maschera il controller destro come un joypad standard per i browser. 
            // Il Tasto A è il "Button South", il Tasto B è il "Button East".
            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.buttonEast.wasPressedThisFrame)
                {
                    Debug.Log("<color=yellow>INVENTARIO: Tasto Letto da API Gamepad HTML5!</color>");
                    isTriggeredThisFrame = true;
                }
            }

            // 4. ESECUZIONE
            if (isTriggeredThisFrame)
            {
                // Blocco di sicurezza: ignora se stai ispezionando un oggetto 3D
                if (InspectionManager.Instance != null && InspectionManager.Instance.IsCurrentlyInspecting)
                {
                    return; 
                }

                ToggleInventory();
            }
        }

        public void ToggleInventory()
        {
            _isOpen = !_isOpen;
            
            if (_visualContainer != null) 
                _visualContainer.SetActive(_isOpen);

            if (_isOpen)
            {
                // 1. Posiziona la UI davanti al giocatore
                PositionUIInFrontOfPlayer();

                // 2. Disabilita il movimento VR per evitare che il giocatore si allontani dal menu
                if (_vrLocomotionSystem != null) 
                    _vrLocomotionSystem.SetActive(false);

                GenerateGrid();
                UpdateProgressUI();
            }
            else
            {
                // 3. Riabilita il movimento VR quando si chiude il menu
                if (_vrLocomotionSystem != null) 
                    _vrLocomotionSystem.SetActive(true);
            }
        }

        private void PositionUIInFrontOfPlayer()
        {
            if (_vrCamera == null) return;

            // Calcola la direzione "in avanti" ignorando l'inclinazione verticale della testa
            Vector3 forwardFlat = new Vector3(_vrCamera.forward.x, 0, _vrCamera.forward.z).normalized;
            
            transform.position = _vrCamera.position + (forwardFlat * _spawnDistance);
            transform.rotation = Quaternion.LookRotation(forwardFlat);
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
                
                // FIX VR: Forza la scala a 1 e la posizione Z a 0
                newSlot.transform.localScale = Vector3.one;
                Vector3 localPos = newSlot.transform.localPosition;
                newSlot.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);

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

        private void CheckForCompletion()
        {
            if (_hasTriggeredCompletion || _allItemsInGame == null || _allItemsInGame.Count == 0) return;

            int totalItems = _allItemsInGame.Count;
            int unlockedItems = CountUnlockedItems();

            if (unlockedItems == totalItems)
            {
                _hasTriggeredCompletion = true;
                Debug.Log("<color=yellow>COMPLETAMENTO VR: Tutti gli oggetti trovati!</color>");
                OnCollectionComplete?.Invoke();
            }
        }

        private void RefreshGridState(InspectableItemData newItem)
        {
            CheckForCompletion();
            
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
            // Chiude il menu fluttuante
            ToggleInventory(); 

            // Lancia l'ispezione in 3D
            if (InspectionManager.Instance != null)
            {
                InspectionManager.Instance.StartInspection(selectedItem);
            }
        }
    }
}