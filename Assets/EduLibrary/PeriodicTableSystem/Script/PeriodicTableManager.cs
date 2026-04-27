using UnityEngine;
using TMPro;

public class PeriodicTableManager : MonoBehaviour
{
    public static PeriodicTableManager Instance;

    [Header("UI References - Pannelli Principali (Inserire sia VR che Desktop)")]
    [SerializeField] private GameObject[] periodicTablePanels;
    [SerializeField] private GameObject[] detailViewPanels;    
    [SerializeField] private GameObject[] contenitoriTabellaSinistra; 
    [SerializeField] private TextMeshProUGUI[] testiFeedbackAtomo;
    [SerializeField] private ElementDetailsUI[] tabelleDettagli;
    [SerializeField] private GameObject[] titoliPrincipali;
    
    [Header("Preloader")]
    public GameObject pannelloPreloader;
    
    [Header("Riferimenti Mini Tavola")]
    public GameObject[] pannelliMiniaturaCompleti; 

    [Header("Integrazione Carta Identità")]
    public IsotopeCardUI[] carteIdentitaUI;

    [Header("Blocco Input UI (Inserire sia VR che Desktop)")]
    public GameObject[] pannelliCreatoreAtomo; 
    public GameObject[] pannelliCalcolatrici;  

    [Header("3D Scene Settings")]
    [SerializeField] private Transform modelSpawnPoint;
    [SerializeField] private GameObject proceduralAtomBasePrefab; 

    private GameObject currentAtomInstance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (pannelloPreloader != null) 
        {
            pannelloPreloader.SetActive(true);
        }
    }

    private void Start()
    {
        ShowTable();
        SetGameObjectsActive(pannelliMiniaturaCompleti, false);
        SetTexts(testiFeedbackAtomo, "");
    }

    public bool IsInputBlocked()
    {
        return IsAnyActive(pannelliCreatoreAtomo) || 
               IsAnyActive(pannelliCalcolatrici) || 
               IsAnyActive(pannelliMiniaturaCompleti);
    }

    public void SelectElement(int numeroAtomico)
    {
        ElementData data = ElementDatabase.Instance.GetElement(numeroAtomico);
        if (data == null) return;

        SetGameObjectsActive(periodicTablePanels, false);
        SetGameObjectsActive(detailViewPanels, true);
        SetGameObjectsActive(titoliPrincipali, false);
        SetGameObjectsActive(contenitoriTabellaSinistra, true);
        SetGameObjectsActive(pannelliMiniaturaCompleti, false);

        foreach (var txt in testiFeedbackAtomo) if (txt != null) txt.gameObject.SetActive(false);
        foreach (var carta in carteIdentitaUI) if (carta != null) carta.Nascondi();
        foreach (var tab in tabelleDettagli) if (tab != null) tab.AggiornaGrafica(data);

        GeneraAtomo3D(data);
    }

    public void GeneraAtomoCustom(int protoni, int neutroni, int elettroni)
    {
        ElementData customData = ScriptableObject.CreateInstance<ElementData>();
        ElementData elementoRealeMatch = ElementDatabase.Instance.GetElement(protoni);
        
        string nomeElemento = "Sconosciuto";
        string descrizioneBase = ""; 
        int neutroniStandard = -1;

        if (elementoRealeMatch != null)
        {
            nomeElemento = elementoRealeMatch.elementName;
            neutroniStandard = elementoRealeMatch.numeroNeutroni;
            customData.symbol = elementoRealeMatch.symbol;
            descrizioneBase = elementoRealeMatch.descrizioneTestuale;
        }
        else
        {
            customData.symbol = "X";
        }

        customData.elementName = nomeElemento;
        customData.atomicNumber = protoni;
        customData.numeroProtoni = protoni;
        customData.numeroNeutroni = neutroni;
        customData.uiColor = new Color(0.85f, 0.85f, 0.85f); 

        SetGameObjectsActive(periodicTablePanels, false);
        SetGameObjectsActive(detailViewPanels, true);
        SetGameObjectsActive(titoliPrincipali, false);
        SetGameObjectsActive(contenitoriTabellaSinistra, false);
        SetGameObjectsActive(pannelliMiniaturaCompleti, false);

        foreach (var txt in testiFeedbackAtomo) 
        {
            if (txt != null) txt.gameObject.SetActive(true);
        }
        
        AggiornaTestoFeedback(protoni, neutroni, elettroni, nomeElemento, neutroniStandard);

        bool isStandard = (neutroni == neutroniStandard);
        foreach (var carta in carteIdentitaUI)
        {
            if (carta != null) carta.AggiornaCarta(protoni, neutroni, elettroni, customData.symbol, customData.elementName, descrizioneBase, isStandard);
        }

        GeneraAtomo3D(customData, elettroni);
    }

    private void AggiornaTestoFeedback(int protoni, int neutroni, int elettroni, string nomeElemento, int neutroniStandard)
    {
        if (nomeElemento == "Sconosciuto") 
        {
            SetTexts(testiFeedbackAtomo, "Non esiste nessun elemento con questo numero di protoni.", Color.white);
            return;
        }

        bool isNeutro = (elettroni == protoni);
        int carica = protoni - elettroni;
        string tipoIone = !isNeutro ? (carica > 0 ? $"Ione Positivo (+{carica})" : $"Ione Negativo ({carica})") : "";

        bool isStableNucleus = false;
        IsotopeData dbData = IsotopeDatabase.Instance.GetIsotope(protoni, neutroni);
        
        if (dbData != null) 
        {
            isStableNucleus = dbData.isStabile;
        }
        else
        {
            float ratio = (float)neutroni / (float)protoni;
            float targetRatio = (protoni < 20) ? 1.0f : 1.5f; 
            if (protoni == 1 && neutroni == 0) targetRatio = 0f; 
            
            isStableNucleus = Mathf.Abs(ratio - targetRatio) < 0.35f;
            if (protoni > 1 && ratio < 0.8f) isStableNucleus = false; 
        }

        if (isNeutro && neutroni == neutroniStandard) 
        {
            SetTexts(testiFeedbackAtomo, $"Eccellente! Hai costruito un atomo perfetto di {nomeElemento}.", Color.green);
        }
        else if (!isStableNucleus) 
        {
            SetTexts(testiFeedbackAtomo, $"Attenzione: Nucleo Instabile! Questo isotopo di {nomeElemento} è radioattivo e si romperà.", Color.red);
        }
        else if (!isNeutro) 
        {
            SetTexts(testiFeedbackAtomo, $"Hai creato uno {tipoIone} di {nomeElemento}. Il nucleo è stabile.", Color.yellow);
        }
        else 
        {
            SetTexts(testiFeedbackAtomo, $"Interessante! Hai creato un isotopo stabile raro di {nomeElemento}.", new Color(0f, 1f, 1f));
        }
    }

    public void ShowTable()
    {
        SafeDestroyAtom(); // Sostituito il Destroy diretto
        
        SetGameObjectsActive(detailViewPanels, false);
        SetGameObjectsActive(periodicTablePanels, true);
        SetGameObjectsActive(titoliPrincipali, true);
        SetGameObjectsActive(pannelliMiniaturaCompleti, false);
        
        foreach (var carta in carteIdentitaUI) if (carta != null) carta.Nascondi();
    }

    private void GeneraAtomo3D(ElementData data, int customElectrons = -1)
    {
        SafeDestroyAtom(); // Sostituito il Destroy diretto

        if (proceduralAtomBasePrefab != null)
        {
            currentAtomInstance = Instantiate(proceduralAtomBasePrefab, modelSpawnPoint.position, modelSpawnPoint.rotation);
            currentAtomInstance.transform.SetParent(modelSpawnPoint);
            
            var renderer = currentAtomInstance.GetComponent<ProceduralAtomRenderer>();
            if (renderer != null) renderer.InitializeAtom(data, customElectrons);
        }
    }

    // --- NUOVA FUNZIONE ARCHITETTURALE: DISTRUZIONE SICURA ---
    private void SafeDestroyAtom()
    {
        if (currentAtomInstance != null)
        {
#if UNITY_EDITOR
            // Se siamo nell'Editor di Unity, deseleziona l'oggetto prima di distruggerlo.
            // Questo previene i fastidiosi "MissingReferenceException" dell'Inspector.
            if (UnityEditor.Selection.activeGameObject == currentAtomInstance)
            {
                UnityEditor.Selection.activeGameObject = null;
            }
#endif
            Destroy(currentAtomInstance);
            currentAtomInstance = null;
        }
    }
    // ---------------------------------------------------------

    public void ToggleMiniTable()
    {
        foreach (var pannello in pannelliMiniaturaCompleti)
        {
            if (pannello != null) pannello.SetActive(!pannello.activeSelf);
        }
    }

    private void SetGameObjectsActive(GameObject[] array, bool state)
    {
        if (array == null) return;
        foreach (var go in array) if (go != null) go.SetActive(state);
    }

    private bool IsAnyActive(GameObject[] array)
    {
        if (array == null) return false;
        foreach (var go in array) if (go != null && go.activeSelf) return true;
        return false;
    }

    private void SetTexts(TextMeshProUGUI[] array, string text)
    {
        if (array == null) return;
        foreach (var t in array) if (t != null) t.text = text;
    }

    private void SetTexts(TextMeshProUGUI[] array, string text, Color color)
    {
        if (array == null) return;
        foreach (var t in array) 
        {
            if (t != null) 
            {
                t.text = text;
                t.color = color;
            }
        }
    }
}