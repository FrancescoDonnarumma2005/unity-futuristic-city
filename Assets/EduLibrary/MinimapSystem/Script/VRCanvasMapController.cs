using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

namespace EduLibrary.MinimapSystem
{
    [System.Serializable]
    public struct VRFastTravel
    {
        [Tooltip("L'ID esatto del luogo (deve coincidere con quello scritto nel MilestoneUnlocker)")]
        public string elementID;
        public Button poiButton;            
        public Transform destination;       
        public bool isUnlockedByDefault;
    }

    public class VRCanvasMapController : MonoBehaviour
    {
        [Header("Riferimenti")]
        public GameObject visualMapContainer; 
        public Transform playerRig;
        public Transform headCamera;
        public GameObject vrLocomotionSystem; 
        public GameObject vrFadeScreen;

        [Header("Impostazioni")]
        public float spawnDistance = 1.2f;
        public InputActionReference toggleMapAction; 

        [Header("Calibrazione Mappa 3D")]
        public Transform mapAnchorTopLeft;
        public Transform mapAnchorBottomRight;
        public RectTransform playerIcon;

        [Header("Viaggio Rapido")]
        public List<VRFastTravel> fastTravelPoints;

        private bool isOpen = false;
        private bool toggleMem = false;

        private void Start()
        {
            if (visualMapContainer != null) visualMapContainer.SetActive(false);

            foreach (var point in fastTravelPoints)
            {
                // Registra quelli aperti fin dall'inizio
                if (point.isUnlockedByDefault) FastTravelManager.UnlockedPOIs.Add(point.elementID);

                // Assegna il teletrasporto al click
                if (point.poiButton != null && point.destination != null)
                {
                    point.poiButton.onClick.AddListener(() => StartTeleport(point.destination));
                }
            }
            RefreshButtonsVisibility();
        }

        private void OnEnable()
        {
            if (toggleMapAction != null && toggleMapAction.action != null) toggleMapAction.action.Enable();
            FastTravelManager.OnPOIUnlocked += HandleNewUnlock;
        }

        private void OnDisable()
        {
            FastTravelManager.OnPOIUnlocked -= HandleNewUnlock;
        }

        private void Update()
        {
            if (toggleMapAction != null && toggleMapAction.action != null)
            {
                bool isPressed = toggleMapAction.action.IsPressed();
                if (isPressed && !toggleMem) ToggleMap();
                toggleMem = isPressed;
            }
        }

        private void LateUpdate()
        {
            if (isOpen && headCamera != null && playerIcon != null && mapAnchorTopLeft != null && mapAnchorBottomRight != null)
                UpdatePlayerIcon();
        }

        public void ToggleMap()
        {
            isOpen = !isOpen;
            if (visualMapContainer != null) visualMapContainer.SetActive(isOpen);

            if (isOpen)
            {
                PositionMapInFrontOfPlayer();
                RefreshButtonsVisibility(); // Ricontrolla i bottoni
                if (vrLocomotionSystem != null) vrLocomotionSystem.SetActive(false);
            }
            else
            {
                if (vrLocomotionSystem != null) vrLocomotionSystem.SetActive(true);
            }
        }

        // --- GESTIONE BOTTONI SBLOCCATI ---
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
            if (isOpen) RefreshButtonsVisibility();
        }

        // --- SISTEMI BASE (Teletrasporto e Posizione rimasti identici) ---
        private void PositionMapInFrontOfPlayer()
        {
            if (headCamera == null) return;
            Vector3 forwardFlat = new Vector3(headCamera.forward.x, 0, headCamera.forward.z).normalized;
            transform.position = headCamera.position + (forwardFlat * spawnDistance);
            transform.position = new Vector3(transform.position.x, transform.position.y - 0.15f, transform.position.z);
            transform.rotation = Quaternion.LookRotation(forwardFlat);
        }

        private void UpdatePlayerIcon()
        {
            float normalizedX = Mathf.InverseLerp(mapAnchorTopLeft.position.x, mapAnchorBottomRight.position.x, headCamera.position.x);
            float normalizedZ = Mathf.InverseLerp(mapAnchorTopLeft.position.z, mapAnchorBottomRight.position.z, headCamera.position.z);
            playerIcon.anchorMin = new Vector2(normalizedX, 1.0f - normalizedZ);
            playerIcon.anchorMax = new Vector2(normalizedX, 1.0f - normalizedZ);
            playerIcon.anchoredPosition = Vector2.zero; 
            playerIcon.localEulerAngles = new Vector3(0f, 0f, -headCamera.eulerAngles.y);
        }

        private void StartTeleport(Transform destination)
        {
            if (destination == null || playerRig == null) return;
            StartCoroutine(PerformTeleport(destination));
        }

        private IEnumerator PerformTeleport(Transform destination)
        {
            CanvasGroup fadeGroup = null;
            if (vrFadeScreen != null)
            {
                vrFadeScreen.SetActive(true);
                fadeGroup = vrFadeScreen.GetComponent<CanvasGroup>();
            }

            if (fadeGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < 1.0f) { fadeGroup.alpha = Mathf.SmoothStep(0f, 1f, elapsed / 1.0f); elapsed += Time.deltaTime; yield return null; }
                fadeGroup.alpha = 1f;
            } else yield return new WaitForSeconds(0.15f);

            var charController = playerRig.GetComponent<CharacterController>();
            if (charController != null) charController.enabled = false;
            playerRig.position = destination.position;
            playerRig.rotation = destination.rotation;
            if (charController != null) charController.enabled = true;

            ToggleMap(); 
            yield return new WaitForSeconds(0.25f); 

            if (fadeGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < 1.0f) { fadeGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Sin((elapsed / 1.0f) * Mathf.PI * 0.5f)); elapsed += Time.deltaTime; yield return null; }
                fadeGroup.alpha = 0f;
                vrFadeScreen.SetActive(false); 
            }
        }
    }
}