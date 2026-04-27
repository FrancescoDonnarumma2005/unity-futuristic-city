using UnityEngine;
using System.Collections.Generic;

public class ElementDatabase : MonoBehaviour
{
    public static ElementDatabase Instance;
    private Dictionary<int, ElementData> database = new Dictionary<int, ElementData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        LoadDatabase();
    }

    void LoadDatabase()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("elementi");

        if (csvFile == null)
        {
            Debug.LogError("ERRORE CRITICO: File 'elementi.csv' non trovato nella cartella Resources!");
            return;
        }

        string[] lines = csvFile.text.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] d = line.Split(','); 

            // Controllo sicurezza: servono almeno 16 colonne (ma ora ne aspettiamo 18)
            if (d.Length < 16) continue;

            ElementData data = ScriptableObject.CreateInstance<ElementData>();

            try
            {
                data.atomicNumber = int.Parse(d[0]);
                data.symbol = d[1];
                data.elementName = d[2];
                data.classe = d[3];
                data.massaAtomica = d[4];
                data.descrizioneTestuale = d[5].Replace("|", ",");
                data.numeriOssidazione = d[6];
                data.configElettronica = d[7];
                data.temperaturaFusione = d[8];
                data.temperaturaEbollizione = d[9];
                data.raggioAtomico = d[10];
                data.energiaIonizzazione = d[11];
                data.elettronegativita = d[12];
                data.affinitaElettronica = d[13];
                data.densita = d[14];
                data.annoScoperta = d[15];

                // --- LETTURA NUOVI DATI (Protoni e Neutroni) ---
                if (d.Length >= 18)
                {
                    int.TryParse(d[16], out data.numeroProtoni);
                    int.TryParse(d[17], out data.numeroNeutroni);
                }
                else
                {
                    // Fallback se il CSV non è aggiornato
                    data.numeroProtoni = data.atomicNumber;
                    // Calcolo approssimativo neutroni
                    float massa = 0;
                    float.TryParse(data.massaAtomica.Replace(" u", ""), out massa);
                    data.numeroNeutroni = Mathf.Max(0, Mathf.RoundToInt(massa) - data.numeroProtoni);
                }
                // -----------------------------------------------

                data.uiColor = GetColorByCategory(data.classe);

                if (!database.ContainsKey(data.atomicNumber))
                {
                    database.Add(data.atomicNumber, data);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Errore parsing riga {i}: {e.Message}");
            }
        }
        
        Debug.Log($"Database caricato con successo: {database.Count} elementi pronti.");
    }

    public ElementData GetElement(int atomicNumber)
    {
        if (database.ContainsKey(atomicNumber))
            return database[atomicNumber];
        
        return null;
    }

    Color GetColorByCategory(string cat)
    {
        string c = cat.Trim().ToLower();
        if (c.Contains("non metalli")) return new Color32(170, 255, 170, 255);
        if (c.Contains("alcalini")) return new Color32(255, 180, 180, 255);
        if (c.Contains("alcalino terrosi")) return new Color32(255, 240, 190, 255);
        if (c.Contains("transizione")) return new Color32(220, 220, 255, 255);
        if (c.Contains("semimetalli")) return new Color32(200, 250, 230, 255);
        if (c.Contains("metalli del blocco p")) return new Color32(230, 230, 230, 255);
        if (c.Contains("alogeni")) return new Color32(255, 255, 200, 255);
        if (c.Contains("nobili")) return new Color32(190, 240, 255, 255);
        if (c.Contains("lantanidi")) return new Color32(255, 200, 255, 255);
        if (c.Contains("attinidi")) return new Color32(240, 200, 240, 255);
        return new Color32(240, 240, 240, 255);
    }
}