using UnityEngine;
using UnityEngine.Events;
using EduUtils.Interaction; 

public class MilestoneInteractable : MonoBehaviour, IInteractable 
{
    [Header("Eventi Pietra Miliare")]
    public UnityEvent onInteract;
    public UnityEvent onHoverEnter;
    public UnityEvent onHoverExit;

    public void OnInteract()
    {
        Debug.Log($"[Milestone] Interazione ricevuta su {gameObject.name}");
        onInteract?.Invoke();
    }

    public void OnHover(bool isHovering)
    {
        if (isHovering) onHoverEnter?.Invoke();
        else onHoverExit?.Invoke();
    }
}