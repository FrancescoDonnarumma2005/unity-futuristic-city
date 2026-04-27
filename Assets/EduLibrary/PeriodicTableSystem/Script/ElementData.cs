using UnityEngine;

[CreateAssetMenu(fileName = "NewElement", menuName = "Chemistry/Element Data")]
public class ElementData : ScriptableObject
{
    [Header("Info Elemento")]
    public string elementName;
    public string symbol;
    public int atomicNumber;

    [Header("Visuals")]
    public GameObject atomModelPrefab; 
    public Color uiColor = Color.white; 

    [Header("Descrizione")]
    [TextArea(5, 10)] 
    public string descrizioneTestuale; 

    [Header("Dettagli Tabella")]
    public string classe;              
    public string massaAtomica;        
    public string numeriOssidazione;   
    public string configElettronica;   
    public string temperaturaFusione;  
    public string temperaturaEbollizione; 
    public string raggioAtomico;       
    public string energiaIonizzazione; 
    public string elettronegativita;   
    public string affinitaElettronica; 
    public string densita;             
    public string annoScoperta;        
    
    [Header("Immagine Extra")]
    public Sprite fotoReale; 

    [Header("Struttura Nucleare")]
    public int numeroProtoni;  
    public int numeroNeutroni; 
}