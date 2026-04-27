using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Centralizes desktop instructions and ensures that the most relevant hint is shown on screen.
/// </summary>
public class DesktopInstructionUI : MonoBehaviour
{
    public static DesktopInstructionUI Instance { get; private set; }

    [SerializeField] private Text uiText;
    [SerializeField] private TMP_Text tmpText;
    [SerializeField] [TextArea] private string defaultMessage;

    private readonly Dictionary<object, HintData> activeHints = new Dictionary<object, HintData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple DesktopInstructionUI instances found. Only one can be active.", this);
            enabled = false;
            return;
        }

        Instance = this;
        ApplyText(defaultMessage);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Shows a new hint coming from a specific owner. The highest priority hint wins.
    /// </summary>
    public void ShowHint(object owner, string message, DesktopHintPriority priority)
    {
        if (owner == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        activeHints[owner] = new HintData(message, priority, Time.unscaledTime);

        RefreshHint();
    }

    /// <summary>
    /// Removes the hint owned by the provided object.
    /// </summary>
    public void ClearHint(object owner)
    {
        if (owner == null)
        {
            return;
        }

        if (activeHints.Remove(owner))
        {
            RefreshHint();
        }
    }

    /// <summary>
    /// Clears every hint and shows the default message.
    /// </summary>
    public void ClearAllHints()
    {
        activeHints.Clear();
        ApplyText(defaultMessage);
    }

    private void RefreshHint()
    {
        HintData bestHint = null;

        foreach (var hint in activeHints.Values)
        {
            if (bestHint == null ||
                hint.Priority > bestHint.Priority ||
                (hint.Priority == bestHint.Priority && hint.Timestamp > bestHint.Timestamp))
            {
                bestHint = hint;
            }
        }

        if (bestHint != null)
        {
            ApplyText(bestHint.Message);
        }
        else
        {
            ApplyText(defaultMessage);
        }
    }

    private void ApplyText(string message)
    {
        if (tmpText != null)
        {
            tmpText.text = message ?? string.Empty;
        }

        if (uiText != null)
        {
            uiText.text = message ?? string.Empty;
        }
    }

    private sealed class HintData
    {
        public readonly string Message;
        public readonly DesktopHintPriority Priority;
        public readonly float Timestamp;

        public HintData(string message, DesktopHintPriority priority, float timestamp)
        {
            Message = message;
            Priority = priority;
            Timestamp = timestamp;
        }
    }
}

public enum DesktopHintPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
