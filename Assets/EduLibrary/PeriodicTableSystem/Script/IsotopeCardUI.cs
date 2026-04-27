using UnityEngine;
using TMPro;

public class IsotopeCardUI : MonoBehaviour
{
    [Header("Riferimenti UI")]
    public GameObject pannelloCompleto; 

    public TextMeshProUGUI txtSimbolo;      // Es. "Li"
    public TextMeshProUGUI txtNumeri;       // Es. "Protoni (Z): 3 | Neutroni (N): 4"
    public TextMeshProUGUI txtNome;         // Es. "Litio-7"
    
    [Header("Nuovi Riferimenti Didattici")]
    public TextMeshProUGUI txtStatoNucleare; // L'etichetta grande colorata (es. "NUCLEO INSTABILE")
    public TextMeshProUGUI txtDescrizione;   // Il testo lungo con la spiegazione del professore

    // Colori didattici
    private Color colorSuccesso = Color.green;
    private Color colorPericolo = Color.red;
    private Color colorIone = new Color(1f, 0.84f, 0f); // Oro/Giallo
    private Color colorNeutro = Color.white;

    public void AggiornaCarta(int p, int n, int e, string simboloBase, string nomeElementoBase, string descrizioneBase, bool isStandard)
    {
        // 1. Attivazione Pannello
        if (pannelloCompleto != null) pannelloCompleto.SetActive(true);
        else gameObject.SetActive(true);

        // 2. Recupero Dati Nucleari
        IsotopeData datiVIP = IsotopeDatabase.Instance.GetIsotope(p, n);
        int numeroMassa = p + n;

        // Visualizzazione Dati Base
        txtSimbolo.text = $"{simboloBase}<size=60%>{numeroMassa}</size>"; 
        txtNumeri.text = $"Protoni (Z): {p} | Neutroni (N): {n}";

        // --- A. ANALISI NUCLEO (Identità e Stabilità) ---
        bool stabilitàNucleare = false;
        string nomeFinale = "";
        string testoNucleo = "";

        if (isStandard)
        {
            // CASO 1: Atomo Standard della Tavola (Vince sempre)
            stabilitàNucleare = true;
            nomeFinale = nomeElementoBase; 
            testoNucleo = "Questo è l'isotopo standard che trovi sulla Tavola Periodica. È il riferimento corretto per questo elemento.";
        }
        else if (datiVIP != null)
        {
            // CASO 2: Isotopo presente nel CSV
            stabilitàNucleare = datiVIP.isStabile;
            nomeFinale = datiVIP.nomeCompleto; 
            
            // Messaggio base didattico
            string spiegazioneBase = datiVIP.isStabile 
                ? "Isotopo stabile esistente in natura." 
                : "Isotopo instabile (radioattivo). Il nucleo non riesce a tenere insieme protoni e neutroni.";

            // Se il CSV ha una descrizione extra (es. "Usato per datazione"), la aggiungiamo
            if (!string.IsNullOrEmpty(datiVIP.descrizione))
            {
                testoNucleo = $"{spiegazioneBase}\n\n<i>Note: {datiVIP.descrizione}</i>";
            }
            else
            {
                testoNucleo = spiegazioneBase;
            }
        }
        else
        {
            // CASO 3: Isotopo Teorico (Calcolo Matematico)
            nomeFinale = $"{nomeElementoBase}-{numeroMassa}";
            stabilitàNucleare = CalcolaStabilitaProcedurale(p, n);
            
            testoNucleo = stabilitàNucleare
                ? "Isotopo teoricamente stabile."
                : "Combinazione instabile. Troppi o troppo pochi neutroni per questo numero di protoni.";
        }

        // --- B. ANALISI ELETTRONI (Carica) ---
        int carica = p - e;
        string testoElettroni = "";
        string titoloStatoElettronico = "";
        Color coloreStatoElettronico = colorNeutro;

        if (carica == 0)
        {
            titoloStatoElettronico = "NEUTRO";
            coloreStatoElettronico = colorSuccesso; 
            testoElettroni = "Le cariche sono bilanciate (P = E). L'atomo è elettricamente neutro.";
        }
        else
        {
            coloreStatoElettronico = colorIone; 
            if (carica > 0)
            {
                titoloStatoElettronico = $"CATIONE (+{carica})";
                testoElettroni = $"Hai {p} protoni (+) e solo {e} elettroni (-).\nVince la carica positiva.";
            }
            else
            {
                titoloStatoElettronico = $"ANIONE ({carica})";
                testoElettroni = $"Hai {e} elettroni (-) contro {p} protoni (+).\nVince la carica negativa.";
            }
        }

        // --- C. AGGIORNAMENTO UI ---

        txtNome.text = nomeFinale;

        // 1. Etichetta Stato Nucleare (Grande e Colorata)
        if (stabilitàNucleare)
        {
            txtStatoNucleare.text = "NUCLEO STABILE";
            txtStatoNucleare.color = colorSuccesso;
        }
        else
        {
            txtStatoNucleare.text = "NUCLEO INSTABILE";
            txtStatoNucleare.color = colorPericolo;
        }

        // 2. Costruzione Descrizione Finale (Rich Text)
        string reportFinale = "";

        // Sezione Nucleo
        string coloreNucleoHex = ColorUtility.ToHtmlStringRGB(stabilitàNucleare ? colorSuccesso : colorPericolo);
        reportFinale += $"<b><color=#{coloreNucleoHex}>STATO NUCLEARE:</color></b>\n{testoNucleo}\n\n";

        // Sezione Elettroni
        string coloreEleHex = ColorUtility.ToHtmlStringRGB(coloreStatoElettronico);
        reportFinale += $"<b><color=#{coloreEleHex}>CONFIGURAZIONE ELETTRONICA ({titoloStatoElettronico}):</color></b>\n{testoElettroni}";

        txtDescrizione.text = reportFinale;
    }

    // Calcolo di fallback se l'isotopo non è nel CSV
    bool CalcolaStabilitaProcedurale(int p, int n)
    {
        float ratio = (float)n / (float)p;
        float targetRatio = (p < 20) ? 1.0f : 1.5f;
        if (p == 1 && n == 0) targetRatio = 0f; 
        
        bool stable = Mathf.Abs(ratio - targetRatio) < 0.35f;
        if (p > 1 && ratio < 0.8f) stable = false;
        return stable;
    }

    public void Nascondi()
    {
        if (pannelloCompleto != null) pannelloCompleto.SetActive(false);
        else gameObject.SetActive(false);
    }
}