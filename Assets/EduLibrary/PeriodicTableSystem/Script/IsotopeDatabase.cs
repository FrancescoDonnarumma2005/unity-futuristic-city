using UnityEngine;
using System.Collections.Generic;

public class IsotopeDatabase : MonoBehaviour
{
    public static IsotopeDatabase Instance;
    public TextAsset csvFile; 
    

    // Chiave: "Protoni_Neutroni" (es "6_8") -> Valore: Dati
    private Dictionary<string, IsotopeData> database = new Dictionary<string, IsotopeData>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        LoadDatabase();
    }

    void LoadDatabase()
    {
        if (csvFile == null) return;

        string[] rows = csvFile.text.Split('\n');
        for (int i = 1; i < rows.Length; i++) // Salta header
        {
            string line = rows[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] cells = line.Split(';');
            if (cells.Length < 7) continue;

            IsotopeData iso = new IsotopeData();
            int.TryParse(cells[0], out iso.protoni);
            int.TryParse(cells[1], out iso.neutroni);
            iso.nomeCompleto = cells[2];
            iso.isStabile = cells[3].Trim().ToUpper() == "TRUE";
            iso.dimezzamento = cells[4];
            iso.abbondanza = cells[5];
            iso.descrizione = cells[6];

            string key = $"{iso.protoni}_{iso.neutroni}";
            if (!database.ContainsKey(key)) database.Add(key, iso);
        }
    }

    public IsotopeData GetIsotope(int p, int n)
    {
        string key = $"{p}_{n}";
        return database.ContainsKey(key) ? database[key] : null;
    }
}