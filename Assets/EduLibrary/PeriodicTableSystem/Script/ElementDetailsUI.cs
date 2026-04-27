using UnityEngine;
using UnityEngine.UI; // Necessario se dovessi manipolare layout o immagini UI standard
using TMPro;

public class ElementDetailsUI : MonoBehaviour
{
    [Header("Intestazione")]
    [SerializeField] private TextMeshProUGUI testoSimbolo; 
    [SerializeField] private TextMeshProUGUI testoNome;    
    [SerializeField] private TextMeshProUGUI testoNumero;  
    [SerializeField] private Image immagineElemento;       

    [Header("Descrizione")]
    [SerializeField] private TextMeshProUGUI testoDescrizione;

    [Header("Righe Dati")]
    [SerializeField] private TextMeshProUGUI valClasse;
    [SerializeField] private TextMeshProUGUI valMassa;
    [SerializeField] private TextMeshProUGUI valOssidazione;
    [SerializeField] private TextMeshProUGUI valConfigurazione;
    [SerializeField] private TextMeshProUGUI valFusione;
    [SerializeField] private TextMeshProUGUI valEbollizione;
    [SerializeField] private TextMeshProUGUI valRaggio;
    [SerializeField] private TextMeshProUGUI valIonizzazione;
    [SerializeField] private TextMeshProUGUI valElettronegativita;
    [SerializeField] private TextMeshProUGUI valAffinita;
    [SerializeField] private TextMeshProUGUI valDensita;
    [SerializeField] private TextMeshProUGUI valAnno;

    
    [Header("Settings Minimizzazione")]
    [Tooltip("Trascina qui l'oggetto 'Scroll View' o il contenitore dei dati da nascondere.")]
    [SerializeField] private GameObject contenutoDaNascondere; 
    
    [Tooltip("Trascina qui il testo dentro al bottone per cambiarlo da '-' a '+'.")]
    [SerializeField] private TextMeshProUGUI testoBottoneToggle; 

    private bool isMinimizzato = false;
    // ------------------------------------

    // Questa funzione aggiorna i testi coi dati dell'atomo
    public void AggiornaGrafica(ElementData dati)
    {
        // 1. Popola l'intestazione
        if(testoSimbolo) testoSimbolo.text = dati.symbol;              
        if(testoNome) testoNome.text = dati.elementName;             
        if(testoNumero) testoNumero.text = dati.atomicNumber.ToString(); 

        // Gestione immagine 2D
        if (immagineElemento != null)
        {
            if (dati.fotoReale != null)
            {
                immagineElemento.sprite = dati.fotoReale;
                immagineElemento.gameObject.SetActive(true);
            }
            else
            {
                immagineElemento.gameObject.SetActive(false);
            }
        }

        // 2. Descrizione
        if(testoDescrizione) testoDescrizione.text = dati.descrizioneTestuale;

        // 3. Popola la tabella (coi controlli null per sicurezza)
        if(valClasse) valClasse.text = dati.classe;
        if(valMassa) valMassa.text = dati.massaAtomica;
        if(valOssidazione) valOssidazione.text = dati.numeriOssidazione;
        if(valConfigurazione) valConfigurazione.text = dati.configElettronica;
        if(valFusione) valFusione.text = dati.temperaturaFusione;
        if(valEbollizione) valEbollizione.text = dati.temperaturaEbollizione;
        if(valRaggio) valRaggio.text = dati.raggioAtomico;
        if(valIonizzazione) valIonizzazione.text = dati.energiaIonizzazione;
        if(valElettronegativita) valElettronegativita.text = dati.elettronegativita;
        if(valAffinita) valAffinita.text = dati.affinitaElettronica;
        if(valDensita) valDensita.text = dati.densita;
        if(valAnno) valAnno.text = dati.annoScoperta;
    }

    
    // Collegata all'evento OnClick del tuo bottone "-"
    public void ToggleTabella()
    {
        isMinimizzato = !isMinimizzato; // Inverte lo stato (Vero -> Falso -> Vero)

        if (isMinimizzato)
        {
            // Nascondiamo il contenuto
            if (contenutoDaNascondere != null) 
                contenutoDaNascondere.SetActive(false);
            
            // Cambiamo il testo del bottone in "+"
            if (testoBottoneToggle != null) 
                testoBottoneToggle.text = "+";
        }
        else
        {
            // Mostriamo tutto
            if (contenutoDaNascondere != null) 
                contenutoDaNascondere.SetActive(true);
            
            // Cambiamo il testo del bottone in "-"
            if (testoBottoneToggle != null) 
                testoBottoneToggle.text = "-";
        }
    }
}