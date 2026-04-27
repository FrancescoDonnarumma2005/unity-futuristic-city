using UnityEngine;
using TMPro; 
using UnityEngine.UI;

public class CustomAtomUI : MonoBehaviour
{
    [Header("Riferimenti UI")]
    public GameObject pannelloSfondoOscurante; 
    public TMP_InputField inputProtoni;
    public TMP_InputField inputNeutroni;
    public TMP_InputField inputElettroni;
    public TextMeshProUGUI testoErrore; 

    [Header("Manager")]
    public PeriodicTableManager mainManager;

    private void Start()
    {
        // RIMOSSO il SetActive(false) per evitare il cortocircuito al primo avvio
        if (testoErrore) testoErrore.text = "";
    }

    public void ApriPannello()
    {
        if (pannelloSfondoOscurante) pannelloSfondoOscurante.SetActive(true);
        
        inputProtoni.text = "";
        inputNeutroni.text = "";
        inputElettroni.text = "";
        if (testoErrore) testoErrore.text = "";
    }

    public void ChiudiPannello()
    {
        if (pannelloSfondoOscurante) pannelloSfondoOscurante.SetActive(false);
    }

    public void CliccaGenera()
    {
        int p = 0, n = 0, e = 0;
        
        bool pOk = int.TryParse(inputProtoni.text, out p);
        bool nOk = int.TryParse(inputNeutroni.text, out n);
        bool eOk = int.TryParse(inputElettroni.text, out e);

        if (!pOk || !nOk || !eOk)
        {
            MostraErrore("Inserisci numeri validi in tutti i campi!");
            return;
        }

        if (p < 1)
        {
            MostraErrore("Devi avere almeno 1 Protone!");
            return;
        }
        if (p > 118)
        {
            MostraErrore("Massimo 118 Protoni ammessi!");
            return;
        }

        if (n > 176)
        {
            MostraErrore("Massimo 176 Neutroni ammessi!");
            return;
        }

        if (e > 118)
        {
            MostraErrore("Massimo 118 Elettroni ammessi!");
            return;
        }

        if (mainManager != null)
        {
            mainManager.GeneraAtomoCustom(p, n, e);
            ChiudiPannello();
        }
    }

    void MostraErrore(string msg)
    {
        if (testoErrore) testoErrore.text = msg;
        Debug.LogWarning(msg);
    }
}