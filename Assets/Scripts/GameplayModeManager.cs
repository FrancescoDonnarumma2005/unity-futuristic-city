using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Enables switching between a VR setup and a desktop first-person setup both from the Inspector and from WebGL JavaScript.
/// </summary>
public class GameplayModeManager : MonoBehaviour
{
    [Tooltip("If true the game starts in VR, otherwise it starts in desktop mode.")]
    public bool useVR = true;

    [Header("Rig Roots")]
    [SerializeField] private GameObject vrRigRoot;
    [SerializeField] private GameObject desktopRigRoot;
    
    [Header("UI Roots")]
    [SerializeField] private GameObject desktopUIRoot;
    [SerializeField] private GameObject vrUIRoot; // NUOVO: Riferimento al Canvas World Space per la VR

    [Header("Optional component toggles")]
    [SerializeField] private Behaviour[] enableOnlyInVR;
    [SerializeField] private Behaviour[] enableOnlyInDesktop;

    [Header("Optional GameObject toggles")]
    [SerializeField] private GameObject[] activateOnlyInVR;
    [SerializeField] private GameObject[] activateOnlyInDesktop;

    [Header("UX")]
    [SerializeField] private bool manageCursorState = true;

    [SerializeField] private UnityEvent<bool> onModeChanged;

    private bool isInVR;
    private bool hasAppliedMode;

    /// <summary>
    /// Current mode state. While playing this reflects the active rig; in edit mode it mirrors the useVR flag.
    /// </summary>
    public bool IsInVR => hasAppliedMode ? isInVR : useVR;

    /// <summary>
    /// C# event counterpart to the inspector UnityEvent, useful for scripts that need to react to mode changes.
    /// </summary>
    public event Action<bool> ModeChanged;

    private void Start()
    {
        ApplyMode(useVR);
    }

    /// <summary>
    /// Public API to change the mode from C# (or SendMessage with a boolean/int argument).
    /// </summary>
    public void SetVRMode(bool enableVR)
    {
        useVR = enableVR;
        ApplyMode(enableVR);
    }

    /// <summary>
    /// Convenience overload for WebGL: accepts "true/false", "1/0" or any numeric string.
    /// </summary>
    public void SetVRModeFromJS(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return;
        }

        rawValue = rawValue.Trim();

        if (bool.TryParse(rawValue, out var boolValue))
        {
            SetVRMode(boolValue);
            return;
        }

        if (int.TryParse(rawValue, out var intValue))
        {
            SetVRMode(intValue != 0);
            return;
        }

        if (float.TryParse(rawValue, out var floatValue))
        {
            SetVRMode(Mathf.Abs(floatValue) > Mathf.Epsilon);
        }
    }

    private void ApplyMode(bool enableVR)
    {
        if (isInVR == enableVR && Application.isPlaying)
        {
            return;
        }

        isInVR = enableVR;
        hasAppliedMode = true;

        if (vrRigRoot != null)
        {
            vrRigRoot.SetActive(enableVR);
        }

        if (desktopRigRoot != null)
        {
            desktopRigRoot.SetActive(!enableVR);
        }

        // GESTIONE DUAL-UI
        if (vrUIRoot != null)
        {
            vrUIRoot.SetActive(enableVR);
        }

        if (desktopUIRoot != null)
        {
            desktopUIRoot.SetActive(!enableVR);
        }

        SetBehaviours(enableOnlyInVR, enableVR);
        SetBehaviours(enableOnlyInDesktop, !enableVR);

        SetGameObjects(activateOnlyInVR, enableVR);
        SetGameObjects(activateOnlyInDesktop, !enableVR);

        if (manageCursorState)
        {
            var lockState = enableVR ? CursorLockMode.None : CursorLockMode.Locked;
            if (Cursor.lockState != lockState)
            {
                Cursor.lockState = lockState;
            }

            Cursor.visible = enableVR;
        }

        onModeChanged?.Invoke(enableVR);
        ModeChanged?.Invoke(enableVR);
    }

    private static void SetBehaviours(Behaviour[] components, bool enabled)
    {
        if (components == null)
        {
            return;
        }

        foreach (var component in components)
        {
            if (component != null)
            {
                component.enabled = enabled;

                // Fix per i Canvas: disabilitare solo il componente Canvas interrompe il rendering, ma lascia vivi 
                // script e raycaster, o visibili oggetti 3D figli. Usiamo SetActive per farlo scomparire del tutto.
                if (component is Canvas || component is CanvasGroup)
                {
                    component.gameObject.SetActive(enabled);
                }
            }
        }
    }

    private static void SetGameObjects(GameObject[] gameObjects, bool active)
    {
        if (gameObjects == null)
        {
            return;
        }

        foreach (var go in gameObjects)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        // L'uso di delayCall evita noiosi avvisi di Unity (es. "SendMessage cannot be called during Awake, CheckConsistency, or OnValidate")
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;

            if (vrRigRoot != null)
            {
                vrRigRoot.SetActive(useVR);
            }

            if (desktopRigRoot != null)
            {
                desktopRigRoot.SetActive(!useVR);
            }

            // GESTIONE DUAL-UI NELL'EDITOR
            if (vrUIRoot != null)
            {
                vrUIRoot.SetActive(useVR);
            }

            if (desktopUIRoot != null)
            {
                desktopUIRoot.SetActive(!useVR);
            }

            // Applica correttamente i cambiamenti nell'Editor e nasconde istantaneamente gli oggetti/Canvas!
            SetBehaviours(enableOnlyInVR, useVR);
            SetBehaviours(enableOnlyInDesktop, !useVR);
            
            SetGameObjects(activateOnlyInVR, useVR);
            SetGameObjects(activateOnlyInDesktop, !useVR);
        };
    }
#endif
}