using UnityEngine;
using TMPro;
using System.Collections;

namespace EduLibrary.MinimapSystem
{
    [RequireComponent(typeof(CanvasGroup))]
    public class DesktopNotificationManager : MonoBehaviour
    {
        public TextMeshProUGUI notificationText;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f; // Parte invisibile
        }

        private void OnEnable()
        {
            // Ascolta lo sblocco dei luoghi
            FastTravelManager.OnPOIUnlocked += ShowNotification;
        }

        private void OnDisable()
        {
            FastTravelManager.OnPOIUnlocked -= ShowNotification;
        }

        private void ShowNotification(string elementID, string placeName)
        {
            StopAllCoroutines(); // Ferma eventuali notifiche precedenti
            StartCoroutine(FadeNotificationRoutine(placeName));
        }

        private IEnumerator FadeNotificationRoutine(string placeName)
        {
            notificationText.text = $"Nuovo Viaggio Rapido Sbloccato:\n<color=#FFD700>{placeName}</color>";

            // Fade In
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / 0.5f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            canvasGroup.alpha = 1f;

            yield return new WaitForSeconds(3.5f);

            // Fade Out
            elapsed = 0f;
            while (elapsed < 0.5f)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }
    }
}