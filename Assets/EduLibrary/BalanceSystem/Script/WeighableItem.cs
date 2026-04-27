using UnityEngine;

public class WeighableItem : MonoBehaviour
{
    [Header("Impostazioni Peso Base")]
    [Tooltip("Il peso a vuoto dell'oggetto (la tara). Es: 50g per un becher, 200g per un cubo di ferro.")]
    public float baseWeight = 50f;

    [Header("Integrazione Liquidi (Opzionale)")]
    [Tooltip("Se questo oggetto contiene liquido, trascina qui il suo script LiquidContainer. Se è solido, lascialo vuoto.")]
    public LiquidContainer liquidContainer;
    
    [Tooltip("Il peso specifico del liquido. Es: 1 per l'acqua (1 unità di volume = 1 grammo).")]
    public float liquidDensity = 1f;

    /// <summary>
    /// Metodo pubblico che la Bilancia chiamerà per sapere quanto pesa l'oggetto in questo esatto frame.
    /// </summary>
    /// <returns>Il peso totale (Tara + Peso del liquido se presente)</returns>
    public float GetTotalWeight()
    {
        // Partiamo dalla tara (il peso del vetro vuoto o dell'oggetto solido)
        float totalWeight = baseWeight;

        // Se l'oggetto è un contenitore ed è stato assegnato lo script LiquidContainer...
        if (liquidContainer != null)
        {
            // ...aggiungiamo la massa del liquido (Volume * Densità)
            totalWeight += (liquidContainer.currentVolume * liquidDensity);
        }

        // Ritorniamo il valore finale alla bilancia
        return totalWeight;
    }
}