using UnityEngine;
using TMPro;

public class NeutronCalculatorUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pannelloCalcolatrice; 
    
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField inputMassaA;   
    [SerializeField] private TMP_InputField inputNumeroZ;  

    [Header("Display Output")]
    [SerializeField] private TextMeshProUGUI testoFormula;  
    [SerializeField] private TextMeshProUGUI testoRisultato; 

    private void Start()
    {
        // RIMOSSO il SetActive(false) per evitare il cortocircuito al primo avvio
        ResetCalcolatrice();
    }

    public void ToggleCalcolatrice()
    {
        if (pannelloCalcolatrice != null)
        {
            bool staAprendo = !pannelloCalcolatrice.activeSelf;
            pannelloCalcolatrice.SetActive(staAprendo);

            if (staAprendo) ResetCalcolatrice();
        }
    }

    public void EseguiCalcolo()
    {
        if (string.IsNullOrEmpty(inputMassaA.text) || string.IsNullOrEmpty(inputNumeroZ.text))
        {
            testoRisultato.text = "Inserisci entrambi i valori!";
            testoRisultato.color = Color.yellow;
            return;
        }

        bool aValido = int.TryParse(inputMassaA.text, out int A);
        bool zValido = int.TryParse(inputNumeroZ.text, out int Z);

        if (!aValido || !zValido)
        {
            testoRisultato.text = "Inserisci solo numeri interi.";
            testoRisultato.color = Color.red;
            return;
        }

        if (A < Z)
        {
            testoRisultato.text = "Errore: La Massa (A) deve essere maggiore o uguale a Z.";
            testoRisultato.color = Color.red;
            return;
        }

        int N = A - Z;

        testoRisultato.text = $"N = {N} Neutroni";
        testoRisultato.color = Color.green;
    }

    private void ResetCalcolatrice()
    {
        inputMassaA.text = "";
        inputNumeroZ.text = "";
        testoRisultato.text = "";
        testoRisultato.color = Color.white;
    }
}