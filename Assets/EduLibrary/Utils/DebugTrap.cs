using UnityEngine;

public class DebugTrap : MonoBehaviour
{
    private void OnDisable()
    {
        // Questo codice scatta nell'esatto millisecondo in cui il pannello viene spento
        Debug.LogWarning($"[TRAPPOLA SCATTATA] L'oggetto '{gameObject.name}' è stato spento!", this);
    }
}