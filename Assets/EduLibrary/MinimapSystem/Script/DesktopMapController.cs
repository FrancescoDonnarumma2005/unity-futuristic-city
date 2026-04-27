using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

namespace EduLibrary.MinimapSystem
{
    [Serializable]
    public struct DesktopFastTravelLocation
    {
        [Tooltip("L'ID esatto del bottone nell'UI Builder (es. poi-anfiteatro)")]
        public string uiElementName; 
        public Transform destinationSpawnPoint;
        [Tooltip("Spunta se questo luogo è sbloccato fin dall'inizio")]
        public bool isUnlockedByDefault;
    }

    [RequireComponent(typeof(UIDocument))]
    public class DesktopMapController : MonoBehaviour
    {
        [Header("Riferimenti Giocatore")]
        public Transform playerTransform;
        public Transform headCameraTransform;

        [Header("Calibrazione Mappa 3D")]
        public Transform mapAnchorTopLeft;
        public Transform mapAnchorBottomRight;

        [Header("Sistema di Fast Travel")]
        public List<DesktopFastTravelLocation> fastTravelLocations;

        private UIDocument uiDocument;
        private VisualElement fullmapContainer;
        private VisualElement fullmapPlayerIcon;
        private Label notificationLabel; // La notifica UI Toolkit

        private bool isFullmapVisible = false;

        private void OnEnable()
        {
            uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;

            fullmapContainer = root.Q<VisualElement>("fullmap-container");
            fullmapPlayerIcon = root.Q<VisualElement>("fullmap-player-icon");
            notificationLabel = root.Q<Label>("notification-label");

            // Assicuriamoci che la notifica parta invisibile
            if (notificationLabel != null)
            {
                notificationLabel.style.display = DisplayStyle.None;
                notificationLabel.style.opacity = 0f;
            }

            // Registriamo nel Cervello Globale quelli sbloccati di default
            foreach (var loc in fastTravelLocations)
            {
                if (loc.isUnlockedByDefault)
                {
                    FastTravelManager.UnlockedPOIs.Add(loc.uiElementName);
                }
            }

            // Ci iscriviamo all'evento globale
            FastTravelManager.OnPOIUnlocked += HandleNewUnlock;
            UpdateVisibility();
        }

        private void OnDisable()
        {
            FastTravelManager.OnPOIUnlocked -= HandleNewUnlock;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            {
                ToggleFullMap();
            }
        }

        private void LateUpdate()
        {
            if (isFullmapVisible && fullmapPlayerIcon != null && headCameraTransform != null)
            {
                UpdatePlayerIcon();
            }
        }

        public void ToggleFullMap()
        {
            isFullmapVisible = !isFullmapVisible;
            UpdateVisibility();

            UnityEngine.Cursor.lockState = isFullmapVisible ? CursorLockMode.None : CursorLockMode.Locked;
            UnityEngine.Cursor.visible = isFullmapVisible;

            if (isFullmapVisible) RefreshButtonsVisibility();
        }

        private void UpdateVisibility()
        {
            if (fullmapContainer != null)
            {
                fullmapContainer.style.display = isFullmapVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        // --- SISTEMA DI SBLOCCO E NOTIFICHE (UI TOOLKIT) ---

        private void RefreshButtonsVisibility()
        {
            var root = uiDocument.rootVisualElement;
            foreach (var loc in fastTravelLocations)
            {
                var btn = root.Q<VisualElement>(loc.uiElementName);
                if (btn != null)
                {
                    // Chiediamo al Cervello Globale se questo ID è sbloccato
                    bool isUnlocked = FastTravelManager.IsUnlocked(loc.uiElementName);
                    btn.style.display = isUnlocked ? DisplayStyle.Flex : DisplayStyle.None;

                    // Puliamo i vecchi click e riassegniamo
                    btn.UnregisterCallback<ClickEvent>(OnFastTravelClicked);
                    if (isUnlocked)
                    {
                        btn.userData = loc.destinationSpawnPoint;
                        btn.RegisterCallback<ClickEvent>(OnFastTravelClicked);
                    }
                }
            }
        }

        private void OnFastTravelClicked(ClickEvent ev)
        {
            var element = ev.currentTarget as VisualElement;
            var destination = element.userData as Transform;
            if (destination != null && playerTransform != null)
            {
                var cc = playerTransform.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                playerTransform.position = destination.position;
                playerTransform.rotation = destination.rotation;

                if (cc != null) cc.enabled = true;
                ToggleFullMap();
            }
        }

        private void HandleNewUnlock(string elementID, string displayName)
        {
            // Aggiorna subito i bottoni se la mappa è aperta
            if (isFullmapVisible) RefreshButtonsVisibility();

            // Lancia la notifica a schermo
            if (notificationLabel != null)
            {
                StartCoroutine(ShowNotification(displayName));
            }
        }

        private IEnumerator ShowNotification(string placeName)
        {
            notificationLabel.text = $"Nuovo Viaggio Rapido Sbloccato:\n{placeName}";
            notificationLabel.style.display = DisplayStyle.Flex;
            
            // Fade In manipolando l'Opacity CSS
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                notificationLabel.style.opacity = Mathf.Lerp(0f, 1f, elapsed / 0.5f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            notificationLabel.style.opacity = 1f;

            yield return new WaitForSeconds(3f);

            // Fade Out
            elapsed = 0f;
            while (elapsed < 0.5f)
            {
                notificationLabel.style.opacity = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            notificationLabel.style.opacity = 0f;
            notificationLabel.style.display = DisplayStyle.None;
        }

        // --- ICONA GIOCATORE ---
        private void UpdatePlayerIcon()
        {
            float normalizedX = Mathf.InverseLerp(mapAnchorTopLeft.position.x, mapAnchorBottomRight.position.x, headCameraTransform.position.x);
            float normalizedZ = Mathf.InverseLerp(mapAnchorTopLeft.position.z, mapAnchorBottomRight.position.z, headCameraTransform.position.z);

            fullmapPlayerIcon.style.left = new StyleLength(Length.Percent(normalizedX * 100f));
            fullmapPlayerIcon.style.top = new StyleLength(Length.Percent(normalizedZ * 100f));
            fullmapPlayerIcon.style.rotate = new StyleRotate(new Rotate(Angle.Degrees(headCameraTransform.eulerAngles.y)));
        }
    }
}