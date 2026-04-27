using System.Collections.Generic;
using UnityEngine;
using TMPro; // Usiamo TextMeshPro per il testo 3D
using UnityEngine.XR.Interaction.Toolkit; // Per leggere i controller VR
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DigitalScale : MonoBehaviour
{
    [Header("Interfaccia Visiva")]
    [Tooltip("Trascina qui il testo 3D (TextMeshPro) del display della bilancia")]
    public TextMeshPro displayUI;
    public string unit = "g";

    // La memoria della bilancia: una lista dinamica degli oggetti sul piatto
    private List<WeighableItem> itemsOnScale = new List<WeighableItem>();

    private void Update()
    {
        float totalWeight = 0f;

        // Difesa: pulisce la lista se per caso un becher viene distrutto o disattivato
        itemsOnScale.RemoveAll(item => item == null || !item.gameObject.activeInHierarchy);

        foreach (WeighableItem item in itemsOnScale)
        {
            // Controllo "Anti-Mano VR"
            XRGrabInteractable grabComponent = item.GetComponent<XRGrabInteractable>();
            
            // Se l'oggetto è attualmente afferrato dal giocatore, non lo pesiamo
            if (grabComponent != null && grabComponent.isSelected)
            {
                continue; 
            }

            // Somma il suo peso dinamico (Tara + Liquido)
            totalWeight += item.GetTotalWeight();
        }

        // Aggiorna il display
        UpdateDisplay(totalWeight);
    }

    private void OnTriggerEnter(Collider other)
    {
        WeighableItem item = other.GetComponentInParent<WeighableItem>();
        
        // Se è un oggetto pesabile e non è già in lista, lo aggiungiamo
        if (item != null && !itemsOnScale.Contains(item))
        {
            itemsOnScale.Add(item);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        WeighableItem item = other.GetComponentInParent<WeighableItem>();
        
        // Se l'oggetto esce dal piatto, lo togliamo dalla lista
        if (item != null && itemsOnScale.Contains(item))
        {
            itemsOnScale.Remove(item);
        }
    }

    private void UpdateDisplay(float weight)
    {
        if (displayUI != null)
        {
            // Formattiamo il numero con 1 cifra decimale (es. "50.0 g")
            displayUI.text = weight.ToString("F1") + " " + unit;
        }
    }
}