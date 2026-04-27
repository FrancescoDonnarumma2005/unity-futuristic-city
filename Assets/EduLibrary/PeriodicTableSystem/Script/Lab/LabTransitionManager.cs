using UnityEngine;
using System.Collections; // Necessario per le Coroutine

public class LabTransitionManager : MonoBehaviour
{
    public static LabTransitionManager Instance;

    [Header("Manager di Sistema")]
    public GameplayModeManager gameplayModeManager;

    [Header("Effetti Visivi (UX)")]
    [Tooltip("Il componente CanvasGroup attaccato al velo nero della telecamera")]
    public CanvasGroup blackFadeScreen;
    [Tooltip("Durata della dissolvenza in secondi")]
    public float fadeDuration = 0.35f;

    [Header("Sistemi Globali")]
    public MonoBehaviour inventoryScript;
    public GameObject tutorialCanvas;

    [Header("Riferimenti VR")]
    public Transform xrOrigin;
    public GameObject vrLocomotionSystem;
    public Transform vrTeleportDestination;
    public GameObject vrPeriodicTableCanvas;

    [Header("Riferimenti Desktop")]
    public GameObject desktopPlayer;
    public Camera mainLabCamera;
    public Camera periodicTableDesktopCamera;
    public GameObject desktopPeriodicTableCanvas;
    public GameObject desktopHUD;

    private Vector3 savedVRPosition;
    private Quaternion savedVRRotation;
    
    // Variabile di sicurezza per bloccare spam di click
    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void GoToPeriodicTable()
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(true));
    }

    public void ReturnToLaboratory()
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(false));
    }

    private IEnumerator TransitionRoutine(bool goingToTable)
    {
        if (gameplayModeManager == null)
        {
            Debug.LogError("[LabTransitionManager] GameplayModeManager non assegnato!");
            yield break;
        }

        isTransitioning = true;
        bool isVRActive = gameplayModeManager.IsInVR;

        // 1. FADE OUT (Schermo diventa nero)
        if (blackFadeScreen != null)
        {
            yield return StartCoroutine(FadeRoutine(1f));
        }

        // 2. CAMBIO DI STATO (Mentre lo schermo è nero)
        if (goingToTable)
        {
            ExecuteGoToTableLogic(isVRActive);
        }
        else
        {
            ExecuteReturnToLabLogic(isVRActive);
        }

        // Piccolo ritardo opzionale per far stabilizzare i frame dopo lo spostamento
        yield return new WaitForSeconds(0.1f);

        // 3. FADE IN (Lo schermo torna trasparente)
        if (blackFadeScreen != null)
        {
            yield return StartCoroutine(FadeRoutine(0f));
        }

        isTransitioning = false;
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float startAlpha = blackFadeScreen.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            blackFadeScreen.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        blackFadeScreen.alpha = targetAlpha;
    }

    // --- LOGICA ISOLATA PER PULIZIA DEL CODICE ---

    private void ExecuteGoToTableLogic(bool isVRActive)
    {
        if (inventoryScript != null) inventoryScript.enabled = false;
        if (tutorialCanvas != null) tutorialCanvas.SetActive(false);

        if (!isVRActive) // DESKTOP
        {
            if (desktopPlayer != null) desktopPlayer.SetActive(false);
            if (mainLabCamera != null) mainLabCamera.gameObject.SetActive(false);
            if (desktopHUD != null) desktopHUD.SetActive(false); 

            if (periodicTableDesktopCamera != null) periodicTableDesktopCamera.gameObject.SetActive(true);
            if (desktopPeriodicTableCanvas != null) desktopPeriodicTableCanvas.SetActive(true);
        }
        else // VR
        {
            if (xrOrigin != null)
            {
                savedVRPosition = xrOrigin.position;
                savedVRRotation = xrOrigin.rotation;

                if (vrTeleportDestination != null)
                {
                    xrOrigin.position = vrTeleportDestination.position;
                    xrOrigin.rotation = vrTeleportDestination.rotation;
                }
            }

            if (vrLocomotionSystem != null) vrLocomotionSystem.SetActive(false);
            if (vrPeriodicTableCanvas != null) vrPeriodicTableCanvas.SetActive(true);
        }
    }

    private void ExecuteReturnToLabLogic(bool isVRActive)
    {
        if (PeriodicTableManager.Instance != null)
        {
            PeriodicTableManager.Instance.ShowTable();
        }

        if (inventoryScript != null) inventoryScript.enabled = true;
        if (tutorialCanvas != null) tutorialCanvas.SetActive(true);

        if (!isVRActive) // DESKTOP
        {
            if (periodicTableDesktopCamera != null) periodicTableDesktopCamera.gameObject.SetActive(false);
            if (desktopPeriodicTableCanvas != null) desktopPeriodicTableCanvas.SetActive(false);

            if (desktopPlayer != null) desktopPlayer.SetActive(true);
            if (mainLabCamera != null) mainLabCamera.gameObject.SetActive(true);
            if (desktopHUD != null) desktopHUD.SetActive(true); 
        }
        else // VR
        {
            if (vrPeriodicTableCanvas != null) vrPeriodicTableCanvas.SetActive(false);

            if (xrOrigin != null)
            {
                xrOrigin.position = savedVRPosition;
                xrOrigin.rotation = savedVRRotation;
            }

            if (vrLocomotionSystem != null) vrLocomotionSystem.SetActive(true);
        }
    }
}