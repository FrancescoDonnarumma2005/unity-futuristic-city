using UnityEngine;
using TMPro;
using System.Collections;

namespace EduLibrary.MinimapSystem
{
    [RequireComponent(typeof(CanvasGroup))]
    public class VRNotificationManager : MonoBehaviour
    {
        [Header("Riferimenti")]
        public TextMeshProUGUI notificationText;
        [Tooltip("La Main Camera del visore")]
        public Transform headCamera;

        [Header("Impostazioni")]
        public float spawnDistance = 1.5f;
        public float heightOffset = 0.1f; // Per non averlo esattamente davanti agli occhi, ma un filo più in alto

        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            FastTravelManager.OnPOIUnlocked += ShowNotification;
        }

        private void OnDisable()
        {
            FastTravelManager.OnPOIUnlocked -= ShowNotification;
        }

        private void ShowNotification(string elementID, string placeName)
        {
            StopAllCoroutines();
            StartCoroutine(FadeNotificationRoutine(placeName));
        }

        private IEnumerator FadeNotificationRoutine(string placeName)
        {
            // 1. Calcola la posizione esatta davanti alla faccia del giocatore
            if (headCamera != null)
            {
                Vector3 forwardFlat = new Vector3(headCamera.forward.x, 0, headCamera.forward.z).normalized;
                transform.position = headCamera.position + (forwardFlat * spawnDistance) + (Vector3.up * heightOffset);
                transform.rotation = Quaternion.LookRotation(forwardFlat);
            }

            notificationText.text = $"Nuovo Viaggio Rapido Sbloccato:\n<color=#FFD700>{placeName}</color>";

            // 2. Fade In
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / 0.5f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            canvasGroup.alpha = 1f;

            // 3. Aspetta che il giocatore legga
            yield return new WaitForSeconds(3.5f);

            // 4. Fade Out
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