using UnityEngine;
using TMPro;
using System.Threading;

public class SpaceshipHUDManager : MonoBehaviour
{
    [Header("Riferimenti UI")]
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;

    private CancellationTokenSource _uiDisplayCTS;

    public void ShowDialogue(FuturisticCity.EduLibrary.POI.POIDataSO data)
    {
        // Se c'è già una comunicazione in corso, la cancelliamo istantaneamente
        _uiDisplayCTS?.Cancel();
        _uiDisplayCTS?.Dispose();
        _uiDisplayCTS = new CancellationTokenSource();

        // Avviamo la sequenza asincrona (Unity 6 Awaitable)
        _ = PlayDialogueSequenceAsync(data, _uiDisplayCTS.Token);
    }

    private async Awaitable PlayDialogueSequenceAsync(FuturisticCity.EduLibrary.POI.POIDataSO data, CancellationToken token)
    {
        try
        {
            if (uiPanel == null) return;

            // Attiviamo la UI all'inizio della trasmissione
            uiPanel.SetActive(true);
            if (titleText != null) titleText.text = data.title;

            // Cicliamo attraverso tutte le frasi (proprio come nel tuo script originale)
            for (int i = 0; i < data.dialogueLines.Length; i++)
            {
                // Controllo sicurezza: l'oggetto è stato distrutto o la comunicazione annullata?
                token.ThrowIfCancellationRequested();

                // Aggiorniamo il testo corrente
                if (contentText != null) contentText.text = data.dialogueLines[i];

                // Attendiamo il tempo stabilito per questa riga [cite: 42]
                await Awaitable.WaitForSecondsAsync(data.timePerLine, token);
            }

            // Fine della sequenza: chiudiamo il pannello
            uiPanel.SetActive(false);
            Debug.Log($"Trasmissione '{data.title}' completata.");
        }
        catch (System.OperationCanceledException)
        {
            // La comunicazione è stata interrotta (es. il giocatore è passato su un altro POI)
            Debug.Log("Comunicazione radio interrotta o sostituita.");
        }
    }

    private void OnDestroy()
    {
        _uiDisplayCTS?.Cancel();
        _uiDisplayCTS?.Dispose();
    }
}