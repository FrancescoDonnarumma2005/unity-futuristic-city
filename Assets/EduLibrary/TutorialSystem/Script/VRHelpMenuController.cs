using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace EduLibrary.TutorialSystem
{
    [RequireComponent(typeof(UIDocument), typeof(MeshRenderer))]
    public class VRHelpMenuController : MonoBehaviour
    {
        [Header("UI & Positioning")]
        [Tooltip("Distanza a cui appare il pannello rispetto agli occhi del giocatore.")]
        [SerializeField] private float _spawnDistance = 1.2f;
        
        [Tooltip("Riferimento opzionale. Se nullo, userà Camera.main al runtime.")]
        [SerializeField] private Transform _playerCamera;

        [Header("Input Setup")]
        [Tooltip("L'azione del New Input System per mostrare/nascondere il menu (es. Tasto B).")]
        [SerializeField] private InputActionReference _toggleMenuAction;

        private UIDocument _uiDocument;
        private MeshRenderer _meshRenderer; 
        private bool _isVisible = false;

        private void Awake()
        {
            // Recuperiamo i componenti obbligatori richiesti dall'attributo RequireComponent
            _uiDocument = GetComponent<UIDocument>();
            _meshRenderer = GetComponent<MeshRenderer>(); 

            // Fallback dinamico se la camera non è assegnata da Inspector
            if (_playerCamera == null && Camera.main != null)
            {
                _playerCamera = Camera.main.transform;
            }
            
            // Assicuriamoci che all'istante 0 tutto sia spento per evitare glitch visivi
            if (_uiDocument != null) _uiDocument.enabled = false;
            if (_meshRenderer != null) _meshRenderer.enabled = false;
        }



        private void OnEnable()
        {
            // Registrazione corretta degli eventi per evitare memory leak
            if (_toggleMenuAction != null && _toggleMenuAction.action != null)
            {
                _toggleMenuAction.action.Enable();
                _toggleMenuAction.action.performed += OnToggleMenuPerformed;
            }
        }

        private void OnDisable()
        {
            // De-registrazione degli eventi quando l'oggetto si spegne o viene distrutto
            if (_toggleMenuAction != null && _toggleMenuAction.action != null)
            {
                _toggleMenuAction.action.performed -= OnToggleMenuPerformed;
                _toggleMenuAction.action.Disable();
            }
        }

        private void OnToggleMenuPerformed(InputAction.CallbackContext context)
        {
            // Esegue il toggle logico dell'interfaccia
            if (_isVisible)
            {
                HideMenu();
            }
            else
            {
                ShowMenu();
            }
        }

        private void ShowMenu()
        {
            if (_playerCamera != null)
            {
                // Calcola la posizione esatta a '_spawnDistance' metri davanti alla testa
                Vector3 targetPosition = _playerCamera.position + (_playerCamera.forward * _spawnDistance);
                
                // Evita che il pannello sia inclinato verso l'alto/basso se l'utente sta guardando per terra o in cielo
                targetPosition.y = _playerCamera.position.y; 

                // Applica la posizione e fa ruotare il pannello per guardare verso la telecamera
                transform.position = targetPosition;
                transform.rotation = Quaternion.LookRotation(transform.position - _playerCamera.position);
            }

            // Riattiviamo la logica UI Toolkit e il rendering fisico (URP)
            if (_uiDocument != null) _uiDocument.enabled = true;
            if (_meshRenderer != null) _meshRenderer.enabled = true;
            
            _isVisible = true;
        }

        private void HideMenu()
        {
            // Spegniamo i componenti per azzerare totalmente i draw calls e i calcoli GPU del visore
            if (_uiDocument != null) _uiDocument.enabled = false;
            if (_meshRenderer != null) _meshRenderer.enabled = false;
            
            _isVisible = false;
        }
    }
}