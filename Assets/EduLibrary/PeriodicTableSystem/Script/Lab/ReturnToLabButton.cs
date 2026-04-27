using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ReturnToLabButton : MonoBehaviour
{
    private void Start()
    {
        // Prende automaticamente il pulsante a cui è attaccato
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            // Aggiunge la funzione di click via codice, senza bisogno dell'Inspector
            btn.onClick.AddListener(ExecuteReturn);
        }
    }

    private void ExecuteReturn()
    {
        // Sfrutta il Singleton per trovare il manager nella scena corrente
        if (LabTransitionManager.Instance != null)
        {
            LabTransitionManager.Instance.ReturnToLaboratory();
        }
        else
        {
            Debug.LogWarning("[ReturnToLabButton] LabTransitionManager non trovato in questa scena!");
        }
    }
}