using UnityEngine;
using TMPro;

public class VRNumpadManager : MonoBehaviour
{
    public static VRNumpadManager Instance { get; private set; }

    [Header("Riferimenti UI")]
    [Tooltip("Il pannello che contiene i bottoni del Numpad")]
    [SerializeField] private GameObject numpadPanel;
    
    private TMP_InputField activeInputField;

    private void Awake()
    {
        // Pattern Singleton per accesso globale rapido e a zero allocazioni
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (numpadPanel != null) numpadPanel.SetActive(false);
    }

    public void OpenNumpad(TMP_InputField targetField)
    {
        activeInputField = targetField;
        if (numpadPanel != null) numpadPanel.SetActive(true);
    }

    public void CloseNumpad()
    {
        activeInputField = null;
        if (numpadPanel != null) numpadPanel.SetActive(false);
    }

    // Funzione da assegnare ai bottoni da 0 a 9
    public void OnNumberClicked(string number)
    {
        if (activeInputField != null)
        {
            activeInputField.text += number;
        }
    }

    // Funzione per il bottone Cancella/Backspace
    public void OnBackspaceClicked()
    {
        if (activeInputField != null && activeInputField.text.Length > 0)
        {
            activeInputField.text = activeInputField.text.Substring(0, activeInputField.text.Length - 1);
        }
    }

    // Funzione per svuotare tutto il campo
    public void OnClearClicked()
    {
        if (activeInputField != null)
        {
            activeInputField.text = "";
        }
    }
}