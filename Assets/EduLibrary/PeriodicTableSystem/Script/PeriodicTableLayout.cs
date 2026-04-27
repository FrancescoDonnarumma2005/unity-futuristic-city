using UnityEngine;
using UnityEngine.UI;

public class PeriodicTableLayout : MonoBehaviour
{
    [Header("Configurazione")]
    public GameObject elementButtonPrefab;
    public float cellWidth = 60f;
    public float cellHeight = 70f;
    public float spacing = 5f;

    [Header("Posizionamento")]
    public Transform container;

    
    [Header("Aggiustamenti Fini")]
    public float startX = 0f;  // Sposta a Destra (+) o Sinistra (-)
    public float startY = 0f;  // Sposta in Alto (+) o Basso (-)
    
    // --- AGGIORNAMENTO FASE 2: MINIATURA ---
    [Header("Modalità Miniatura")]
    public bool isMiniTable = false; // Se VERO, attiva la modalità icona piccola
    // ---------------------------------------

    void Start()
    {
        // Ritardiamo leggermente per essere sicuri che il Database sia pronto
        Invoke(nameof(GenerateTable), 0.1f);
    }

    void GenerateTable()
    {
        // Pulisce i vecchi bottoni (usiamo 'container' per sicurezza)
        foreach (Transform child in container) Destroy(child.gameObject);

        for (int i = 1; i <= 118; i++)
        {
            Vector2 gridPos = GetGridPosition(i);
            GameObject btn = Instantiate(elementButtonPrefab, container);

            // Configura Dati e Colore
            ElementButton btnScript = btn.GetComponent<ElementButton>();
            if (btnScript != null)
            {
                btnScript.atomicNumber = i;
                
                // --- AGGIORNAMENTO FASE 2: ATTIVAZIONE MINIATURA ---
                // Se questa è la mini-tabella, diciamo al bottone di nascondere nome e numero
                if (isMiniTable)
                {
                    btnScript.AttivaModalitaMini();
                }
                // ---------------------------------------------------

                // Forza il colore subito
                if (ElementDatabase.Instance != null)
                {
                    ElementData data = ElementDatabase.Instance.GetElement(i);
                    if (data != null)
                    {
                        Image img = btn.GetComponent<Image>();
                        if (img != null) img.color = data.uiColor;
                    }
                }
            }

            // --- POSIZIONAMENTO CON OFFSET ---
            RectTransform rect = btn.GetComponent<RectTransform>();
            
            // Calcolo: (Posizione Griglia) + (Tuo Spostamento Manuale)
            float posX = startX + (gridPos.x * (cellWidth + spacing));
            float posY = startY - (gridPos.y * (cellHeight + spacing));

            rect.anchoredPosition = new Vector2(posX, posY);
            rect.sizeDelta = new Vector2(cellWidth, cellHeight);
        }
    }

    Vector2 GetGridPosition(int z)
    {
        if (z == 1) return new Vector2(0, 0);
        if (z == 2) return new Vector2(17, 0);
        if (z >= 3 && z <= 4) return new Vector2(z - 3, 1);
        if (z >= 5 && z <= 10) return new Vector2(z + 7, 1);
        if (z >= 11 && z <= 12) return new Vector2(z - 11, 2);
        if (z >= 13 && z <= 18) return new Vector2(z - 1, 2);
        if (z >= 19 && z <= 36) return new Vector2(z - 19, 3);
        if (z >= 37 && z <= 54) return new Vector2(z - 37, 4);
        
        
        if (z >= 55 && z <= 56) return new Vector2(z - 55, 5); // Cs, Ba
        if (z >= 72 && z <= 86) return new Vector2(z - 69, 5); // Hf -> Rn
        
        
        if (z >= 87 && z <= 88) return new Vector2(z - 87, 6); // Fr, Ra
        if (z >= 104 && z <= 118) return new Vector2(z - 101, 6); // Rf -> Og

        
        if (z >= 57 && z <= 71) return new Vector2((z - 57) + 2, 8); // Riga Lantanidi
        if (z >= 89 && z <= 103) return new Vector2((z - 89) + 2, 9); // Riga Attinidi
        
        return Vector2.zero;
    }
}