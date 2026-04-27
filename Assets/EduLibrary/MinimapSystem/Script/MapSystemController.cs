using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

namespace EduLibrary.MinimapSystem
{
    [Serializable]
    public struct FastTravelLocation
    {
        public string uiElementName; 
        public Transform destinationSpawnPoint;
        public bool isUnlockedByDefault;
    }

    [RequireComponent(typeof(UIDocument))]
    public class MapSystemController : MonoBehaviour
    {
        [Header("Riferimenti Giocatore")]
        public Transform playerTransform;
        public Transform headCameraTransform;

        [Header("Calibrazione Mappa 3D")]
        public Transform mapAnchorTopLeft;
        public Transform mapAnchorBottomRight;

        [Header("Sistema di Fast Travel")]
        public List<FastTravelLocation> fastTravelLocations;

        private UIDocument uiDocument;
        private VisualElement fullmapContainer;
        private VisualElement fullmapPlayerIcon;

        private bool isFullmapVisible = false;

        private void OnEnable()
        {
            uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;

            fullmapContainer = root.Q<VisualElement>("fullmap-container");
            fullmapPlayerIcon = root.Q<VisualElement>("fullmap-player-icon");

            // Sblocca i luoghi di default nel Manager Globale
            foreach (var loc in fastTravelLocations)
            {
                if (loc.isUnlockedByDefault) FastTravelManager.UnlockedPOIs.Add(loc.uiElementName);
            }

            // Ascolta quando un nuovo luogo viene sbloccato per aggiornare la mappa
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
                fullmapContainer.style.display = isFullmapVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RefreshButtonsVisibility()
        {
            var root = uiDocument.rootVisualElement;
            foreach (var loc in fastTravelLocations)
            {
                var btn = root.Q<VisualElement>(loc.uiElementName);
                if (btn != null)
                {
                    bool isUnlocked = FastTravelManager.IsUnlocked(loc.uiElementName);
                    btn.style.display = isUnlocked ? DisplayStyle.Flex : DisplayStyle.None;

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
            // Se la mappa è attualmente aperta mentre sblocchi qualcosa, aggiorna i bottoni
            if (isFullmapVisible) RefreshButtonsVisibility();
        }

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