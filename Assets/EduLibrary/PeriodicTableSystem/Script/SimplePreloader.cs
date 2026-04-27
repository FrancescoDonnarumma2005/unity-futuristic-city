using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SimplePreloader : MonoBehaviour
{
    [Header("Impostazioni")]
    [Tooltip("Quanto tempo il logo rimane fermo visibile")]
    public float tempoAttesa = 2.0f;
    
    [Tooltip("Quanto tempo ci mette a sfumare fino a sparire")]
    public float durataFade = 1.0f;

    [Header("Riferimenti")]
    public CanvasGroup gruppoInterfaccia; // Serve per controllare la trasparenza di tutto il blocco

    private void Start()
    {
        // Ci assicuriamo che all'avvio sia tutto visibile e opaco
        if (gruppoInterfaccia != null)
        {
            gruppoInterfaccia.alpha = 1f;
            gruppoInterfaccia.blocksRaycasts = true; // Blocca i click sotto mentre carica
            StartCoroutine(SequenzaIntro());
        }
    }

    IEnumerator SequenzaIntro()
    {
        // 1. Aspetta il tempo stabilito (Mostra il logo)
        yield return new WaitForSeconds(tempoAttesa);

        // 2. Esegui il Fade Out (Diventa trasparente gradualmente)
        float timer = 0f;
        while (timer < durataFade)
        {
            timer += Time.deltaTime;
            // Lerp calcola il valore intermedio tra 1 (visibile) e 0 (invisibile)
            float nuovaAlpha = Mathf.Lerp(1f, 0f, timer / durataFade);
            gruppoInterfaccia.alpha = nuovaAlpha;
            yield return null; // Aspetta il prossimo frame
        }

        // 3. Spegni tutto alla fine per liberare risorse
        gruppoInterfaccia.alpha = 0f;
        gruppoInterfaccia.blocksRaycasts = false; // Riabilita i click sul gioco
        gameObject.SetActive(false);
    }
}