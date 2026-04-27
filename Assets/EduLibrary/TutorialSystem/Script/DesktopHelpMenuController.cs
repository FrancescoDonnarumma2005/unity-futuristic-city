using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace EduLibrary.TutorialSystem
{
    [RequireComponent(typeof(UIDocument))]
    public class DesktopHelpMenuController : MonoBehaviour
    {
        [Header("Gestione Interfaccia Esterna")]
        [Tooltip("Trascina qui i GameObject da nascondere temporaneamente quando si apre questo menu.")]
        [SerializeField] private GameObject[] _elementsToHide;

        private UIDocument _uiDocument;
        
        // Lo stato logico iniziale: chiuso
        private bool _isVisible = false; 

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            // CRITICO: Ogni volta che l'oggetto viene riacceso (es. dall'inventario),
            // UI Toolkit ha generato un nuovo albero visivo. 
            // Dobbiamo ri-applicare lo stato corretto immediatamente!
            UpdateVisualState();
        }

        private void Update()
        {
            // Controllo input per il tasto H
            if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
            {
                ToggleMenu();
            }
        }

        private void ToggleMenu()
        {
            _isVisible = !_isVisible;
            
            // Aggiorna la grafica dell'UI Toolkit
            UpdateVisualState();

            // Accende o spegne gli ALTRI elementi della UI
            for (int i = 0; i < _elementsToHide.Length; i++)
            {
                if (_elementsToHide[i] != null)
                {
                    _elementsToHide[i].SetActive(!_isVisible);
                }
            }
        }

        /// <summary>
        /// Sincronizza lo stato logico (_isVisible) con lo stato grafico (UI Toolkit)
        /// recuperando l'elemento root aggiornato in tempo reale.
        /// </summary>
        private void UpdateVisualState()
        {
            if (_uiDocument != null && _uiDocument.rootVisualElement != null)
            {
                // Usiamo l'operatore ternario per impostare Flex o None
                _uiDocument.rootVisualElement.style.display = _isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}