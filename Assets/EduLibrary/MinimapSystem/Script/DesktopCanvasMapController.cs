using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace EduLibrary.MinimapSystem
{
    [System.Serializable]
    public struct DesktopFastTravel
    {
        [Tooltip("L'ID esatto del luogo (es. poi-anfiteatro)")]
        public string elementID;
        public Button poiButton;            
        public Transform destination;       
        [Tooltip("Spunta se questo luogo è già sbloccato all'inizio del gioco")]
        public bool isUnlockedByDefault;
    }

    public class DesktopCanvasMapController : MonoBehaviour
    {
        [Header("Riferimenti")]
        [Tooltip("L'oggetto UI (Image o Panel) che contiene la mappa completa")]
        public GameObject fullMapContainer; 
        public Transform playerTransform;
        public Transform headCameraTransform; // Usato per calcolare la rotazione dell'icona

        [Header("Calibrazione Mappa 2D")]
        public Transform mapAnchorTopLeft;
        public Transform mapAnchorBottomRight;
        public RectTransform playerIcon;

        [Header("Sistema di Notifiche Desktop")]
        public CanvasGroup notificationGroup;
        public TextMeshProUGUI notificationText;

        [Header("Viaggio Rapido")]
        public List<DesktopFastTravel> fastTravelPoints;

        private bool isMapOpen = false;

        private void Start()
        {
            if (fullMapContainer != null) fullMapContainer.SetActive(false);
            if (notificationGroup != null) notificationGroup.alpha = 0f;

            foreach (var point in fastTravelPoints)
            {
                // Registriamo quelli di default nel Cervello Globale
                if (point.isUnlockedByDefault)
                {
                    FastTravelManager.UnlockedPOIs.Add(point.elementID);
                }

                // Colleghiamo il click dei bottoni Canvas al teletrasporto
                if (point.poiButton != null && point.destination != null)
                {
                    point.poiButton.onClick.AddListener(() => StartTeleport(point.destination));
                }
            }

            RefreshButtonsVisibility();
        }

        private void OnEnable()
        {
            FastTravelManager.OnPOIUnlocked += HandleNewUnlock;
        }

        private void OnDisable()
        {
            FastTravelManager.OnPOIUnlocked -= HandleNewUnlock;
        }

        private void Update()
        {
            // Tasto M per aprire la mappa su Desktop
            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            {
                ToggleMap();
            }
        }

        private void LateUpdate()
        {
            if (isMapOpen && headCameraTransform != null && playerIcon != null && mapAnchorTopLeft != null && mapAnchorBottomRight != null)
            {
                UpdatePlayerIcon();
            }
        }

        public void ToggleMap()
        {
            isMapOpen = !isMapOpen;
            if (fullMapContainer != null) fullMapContainer.SetActive(isMapOpen);

            // Gestione del cursore del mouse
            UnityEngine.Cursor.lockState = isMapOpen ? CursorLockMode.None : CursorLockMode.Locked;
            UnityEngine.Cursor.visible = isMapOpen;

            if (isMapOpen) RefreshButtonsVisibility();
        }

        // --- SISTEMA DI SBLOCCO E NOTIFICHE ---

        private void RefreshButtonsVisibility()
        {
            foreach (var point in fastTravelPoints)
            {
                if (point.poiButton != null)
                {
                    bool isUnlocked = FastTravelManager.IsUnlocked(point.elementID);
                    point.poiButton.gameObject.SetActive(isUnlocked);
                }
            }
        }

        private void HandleNewUnlock(string elementID, string displayName)
        {
            RefreshButtonsVisibility();

            if (notificationGroup != null && notificationText != null)
            {
                StartCoroutine(ShowNotificationDesktop(displayName));
            }
        }

        private IEnumerator ShowNotificationDesktop(string placeName)
        {
            notificationText.text = $"Nuovo Viaggio Rapido Sbloccato:\n<color=yellow>{placeName}</color>";

            // Fade In
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                notificationGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / 0.5f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            notificationGroup.alpha = 1f;

            yield return new WaitForSeconds(3f);

            // Fade Out
            elapsed = 0f;
            while (elapsed < 0.5f)
            {
                notificationGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            notificationGroup.alpha = 0f;
        }

        // --- POSIZIONAMENTO ICONA E TELETRASPORTO ---

        private void UpdatePlayerIcon()
        {
            float normalizedX = Mathf.InverseLerp(mapAnchorTopLeft.position.x, mapAnchorBottomRight.position.x, headCameraTransform.position.x);
            float normalizedZ = Mathf.InverseLerp(mapAnchorTopLeft.position.z, mapAnchorBottomRight.position.z, headCameraTransform.position.z);
            
            float canvasX = normalizedX;
            float canvasY = 1.0f - normalizedZ;
            
            playerIcon.anchorMin = new Vector2(canvasX, canvasY);
            playerIcon.anchorMax = new Vector2(canvasX, canvasY);
            playerIcon.anchoredPosition = Vector2.zero; 
            
            float playerRotY = headCameraTransform.eulerAngles.y;
            playerIcon.localEulerAngles = new Vector3(0f, 0f, -playerRotY);
        }

        private void StartTeleport(Transform destination)
        {
            if (playerTransform != null)
            {
                var cc = playerTransform.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                
                playerTransform.position = destination.position;
                playerTransform.rotation = destination.rotation;
                
                if (cc != null) cc.enabled = true;
                
                ToggleMap(); // Chiude la mappa dopo il click
            }
        }
    }
}