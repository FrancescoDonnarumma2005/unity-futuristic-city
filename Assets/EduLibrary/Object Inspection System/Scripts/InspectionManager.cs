using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace InspectionSystem
{
    public class InspectionManager : MonoBehaviour
    {
        public static InspectionManager Instance;

        [Header("Rig References")]
        [SerializeField] private GameObject inspectionRigRoot;
        [SerializeField] private Camera stageCamera;
        [SerializeField] private Transform objectPivot;
        
        [Header("VR Settings")]
        [SerializeField] private Transform vrSpawnPoint; 
        [SerializeField] private GameObject vrPlayerRoot; 
        [SerializeField] private GameObject vrLocomotionSystem; 
        
        [Header("Fader Settings")]
        [SerializeField] private CanvasGroup screenFader;
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Dual UI System (Inspection)")]
        [SerializeField] private GameObject canvasDesktop; 
        [SerializeField] private GameObject canvasVR;      
        [SerializeField] private TextMeshProUGUI titleTextVR;
        [SerializeField] private TextMeshProUGUI descriptionTextVR;
        [SerializeField] private TextMeshProUGUI titleTextDesktop;
        [SerializeField] private TextMeshProUGUI descriptionTextDesktop;

        [Header("External UI Control")]
        [Tooltip("Trascina qui l'oggetto Desktop_HUD (o QuestPanel)")]
        [SerializeField] private GameObject questUiDesktopRoot; 
        [Tooltip("Trascina qui l'oggetto VR_WristUI (o simile)")]
        [SerializeField] private GameObject questUiVRRoot;

        [Header("UI General")]
        [SerializeField] private GameObject uiPointer;
        [Tooltip("Inserisci qui eventuali altri elementi della UI Desktop da nascondere (es. Interaction_Hint, Inventory_Hint)")]
        [SerializeField] private List<GameObject> extraDesktopUiToHide;

        [Header("Input Settings")]
        [SerializeField] private float rotationSpeed = 0.5f;
        [SerializeField] private float zoomSensitivity = 0.02f; 
        [SerializeField] private InputActionReference vrRotateAction; 
        [SerializeField] private InputActionReference vrExitAction;

        private GameObject currentModel;
        private bool isInspecting = false;
        private bool isTransitioning = false; 
        
        public bool IsCurrentlyInspecting => isInspecting || isTransitioning;
        
        // Stato salvato per VR
        private Vector3 vrOriginalPosition;
        private Quaternion vrOriginalRotation;
        
        // Variabili Camera VR
        private Camera vrCamera;
        private int originalCullingMask;
        private CameraClearFlags originalClearFlags;
        private Color originalBackgroundColor;

        // Variabili Zoom Desktop
        private float targetDistance;
        private float currentDistance;
        private float zoomVelocity;
        private float zoomSmoothTime = 0.1f;
        private float currentMinZoom;
        private float currentMaxZoom;

        private DesktopFirstPersonController playerController;
        private DesktopGrabber playerGrabber;
        private GameplayModeManager modeManager; 

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            playerController = Object.FindFirstObjectByType<DesktopFirstPersonController>();
            playerGrabber = Object.FindFirstObjectByType<DesktopGrabber>();
            modeManager = Object.FindFirstObjectByType<GameplayModeManager>();
            
            if (vrPlayerRoot == null)
            {
                var originObj = GameObject.Find("XR Origin"); 
                if (originObj) vrPlayerRoot = originObj;
            }

            if (Camera.main != null) vrCamera = Camera.main;

            if (inspectionRigRoot) inspectionRigRoot.SetActive(false);
            
            if (screenFader)
            {
                screenFader.alpha = 0;
                screenFader.blocksRaycasts = false;
            }
        }

        private void Update()
        {
            if (!isInspecting || isTransitioning) return; 

            bool isVR = modeManager != null && modeManager.IsInVR;

            if (isVR) HandleInputVR();
            else
            {
                HandleInputDesktop();
                HandleZoomDesktop();
            }
        }

        private void HandleInputDesktop()
        {
            if (Mouse.current.leftButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                RotateObject(delta);
            }

            if (Mouse.current.scroll.y.ReadValue() != 0)
            {
                targetDistance -= Mouse.current.scroll.y.ReadValue() * zoomSensitivity;
                float minLimit = currentMinZoom > 0.01f ? currentMinZoom : 0.1f;
                float maxLimit = currentMaxZoom > 0.1f ? currentMaxZoom : 5.0f;
                targetDistance = Mathf.Clamp(targetDistance, minLimit, maxLimit);
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame)
            {
                StopInspection();
            }
        }

        private void HandleInputVR()
        {
            if (vrRotateAction != null && vrRotateAction.action != null)
            {
                Vector2 input = vrRotateAction.action.ReadValue<Vector2>();
                RotateObject(input * 5f); 
            }

            if (vrExitAction != null && vrExitAction.action != null && vrExitAction.action.WasPressedThisFrame())
            {
                StopInspection();
            }
        }

        private void RotateObject(Vector2 delta)
        {
             if (objectPivot != null)
            {
                objectPivot.Rotate(Vector3.up, -delta.x * rotationSpeed, Space.World);
                objectPivot.Rotate(Vector3.right, delta.y * rotationSpeed, Space.World);
            }
        }

        private void HandleZoomDesktop()
        {
            if (stageCamera == null) return;
            
            // 1. Aggiorna la posizione in base allo zoom
            currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref zoomVelocity, zoomSmoothTime);
            stageCamera.transform.localPosition = new Vector3(0, 0, -currentDistance);
            
            // 2. Ricalcola costantemente l'angolazione verso l'oggetto
            if (objectPivot != null)
            {
                stageCamera.transform.LookAt(objectPivot);
            }
        }

        public void StartInspection(InspectableItemData data)
        {
            if (isInspecting || isTransitioning) return;
            StartCoroutine(TransitionRoutine(true, data));
        }

        public void StopInspection()
        {
            if (!isInspecting || isTransitioning) return;
            StartCoroutine(TransitionRoutine(false, null));
        }

        private IEnumerator TransitionRoutine(bool entering, InspectableItemData data)
        {
            isTransitioning = true;
            yield return StartCoroutine(Fade(0, 1));

            if (entering)
            {
                SetupInspection(data);
                isInspecting = true;
            }
            else
            {
                TeardownInspection();
                isInspecting = false;
            }

            yield return null; 
            yield return StartCoroutine(Fade(1, 0));

            isTransitioning = false;
        }

        private IEnumerator Fade(float startAlpha, float endAlpha)
        {
            if (screenFader == null) yield break;

            screenFader.blocksRaycasts = true; 
            float timer = 0f;
            
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                screenFader.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / fadeDuration);
                yield return null;
            }
            
            screenFader.alpha = endAlpha;
            screenFader.blocksRaycasts = (endAlpha > 0); 
        }

        private void SetupInspection(InspectableItemData data)
        {
            bool isVR = modeManager != null && modeManager.IsInVR;

            if (questUiDesktopRoot != null) questUiDesktopRoot.SetActive(false);
            if (questUiVRRoot != null) questUiVRRoot.SetActive(false);

            if (isVR)
            {
                if (canvasDesktop) canvasDesktop.SetActive(false);
                if (canvasVR) canvasVR.SetActive(true);
                if (stageCamera) stageCamera.gameObject.SetActive(false); 

                if (titleTextVR) titleTextVR.text = data.itemName;
                if (descriptionTextVR) descriptionTextVR.text = data.itemDescription;
                
                if (vrLocomotionSystem != null) vrLocomotionSystem.SetActive(false);

                if (vrRotateAction != null && vrRotateAction.action != null)
                {
                    vrRotateAction.action.Enable();
                }

                if (vrPlayerRoot && vrSpawnPoint && vrCamera)
                {
                    vrOriginalPosition = vrPlayerRoot.transform.position;
                    vrOriginalRotation = vrPlayerRoot.transform.rotation;
                    
                    float rotationDiff = vrSpawnPoint.eulerAngles.y - vrCamera.transform.eulerAngles.y;
                    vrPlayerRoot.transform.Rotate(0, rotationDiff, 0);

                    Vector3 headToRootOffset = vrPlayerRoot.transform.position - vrCamera.transform.position;
                    vrPlayerRoot.transform.position = vrSpawnPoint.position + headToRootOffset;
                }

                if (vrCamera != null)
                {
                    originalCullingMask = vrCamera.cullingMask;
                    originalClearFlags = vrCamera.clearFlags;
                    originalBackgroundColor = vrCamera.backgroundColor;

                    vrCamera.clearFlags = CameraClearFlags.SolidColor;
                    vrCamera.backgroundColor = Color.black;
                    vrCamera.cullingMask = LayerMask.GetMask("Inspection", "UI", "VRHands");
                }
            }
            else
            {
                // DESKTOP
                if (canvasVR) canvasVR.SetActive(false);
                if (canvasDesktop) canvasDesktop.SetActive(true);
                if (stageCamera) stageCamera.gameObject.SetActive(true);

                if (titleTextDesktop) titleTextDesktop.text = data.itemName;
                if (descriptionTextDesktop) descriptionTextDesktop.text = data.itemDescription;

                if (playerController) playerController.SetInputSuspended(true, true);
                if (playerGrabber) playerGrabber.SetInteractionsLocked(true);
                if (uiPointer) uiPointer.SetActive(false);
                if (DesktopInstructionUI.Instance != null) DesktopInstructionUI.Instance.ClearAllHints();
                
                if (extraDesktopUiToHide != null)
                {
                    foreach (var ui in extraDesktopUiToHide) 
                    { 
                        if (ui) ui.SetActive(false); 
                    }
                }

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                targetDistance = data.defaultCameraDistance;
                currentDistance = targetDistance;
                currentMinZoom = data.minCameraDistance;
                currentMaxZoom = data.maxCameraDistance;

                if (stageCamera)
                {
                    // 1. Posizioniamo la telecamera alla distanza corretta
                    stageCamera.transform.localPosition = new Vector3(0, 0, -currentDistance);
                    
                    // 2. Calcoliamo la rotazione
                    stageCamera.transform.LookAt(objectPivot);
                }
            }

            inspectionRigRoot.SetActive(true);
            if (objectPivot) objectPivot.localRotation = Quaternion.identity;

            if (currentModel != null) Destroy(currentModel);
            currentModel = Instantiate(data.modelPrefab, objectPivot);
            currentModel.transform.localPosition = Vector3.zero;
            objectPivot.localRotation = Quaternion.Euler(data.initialRotation);
            currentModel.transform.localScale = Vector3.one * data.initialZoom;
            SetLayerRecursively(currentModel, LayerMask.NameToLayer("Inspection"));

            Renderer[] renderers = currentModel.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds combinedBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) combinedBounds.Encapsulate(renderers[i].bounds);
                Vector3 centerOffsetWorld = combinedBounds.center - objectPivot.position;
                Vector3 centerOffsetLocal = objectPivot.InverseTransformVector(centerOffsetWorld);
                currentModel.transform.localPosition = -centerOffsetLocal;
            }
        }

        private void TeardownInspection()
        {
            bool isVR = modeManager != null && modeManager.IsInVR;

            if (currentModel != null) Destroy(currentModel);
            inspectionRigRoot.SetActive(false);

            if (isVR)
            {
                if (questUiVRRoot != null) questUiVRRoot.SetActive(true);

                if (vrPlayerRoot)
                {
                    vrPlayerRoot.transform.position = vrOriginalPosition;
                    vrPlayerRoot.transform.rotation = vrOriginalRotation;
                }
                
                if (vrLocomotionSystem != null) vrLocomotionSystem.SetActive(true);

                if (vrCamera != null)
                {
                    vrCamera.cullingMask = originalCullingMask;
                    vrCamera.clearFlags = originalClearFlags;
                    vrCamera.backgroundColor = originalBackgroundColor;
                }
            }
            else
            {
                if (questUiDesktopRoot != null) questUiDesktopRoot.SetActive(true);

                if (uiPointer) uiPointer.SetActive(true);
                if (playerController) playerController.SetInputSuspended(false, false);
                if (playerGrabber) playerGrabber.SetInteractionsLocked(false);

                if (extraDesktopUiToHide != null)
                {
                    foreach (var ui in extraDesktopUiToHide) 
                    { 
                        if (ui) ui.SetActive(true); 
                    }
                }
            }
        }

        private void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (newLayer < 0) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}