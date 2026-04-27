using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class AtomTooltipManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject calloutContainer; 
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private RectTransform lineRect; 
    [SerializeField] private RectTransform dotRect;
    
    [SerializeField] private RectTransform mainCanvasRect; 

    [Header("Settings")]
    [SerializeField] private Vector2 labelOffset = new Vector2(100f, 50f); 
    [SerializeField] private float padding = 10f; 

    private Transform currentTarget; 

    private void Start()
    {
        if (calloutContainer != null) calloutContainer.SetActive(false);
        
        if (lineRect != null)
        {
            lineRect.pivot = new Vector2(0f, 0.5f); 
        }
    }

    private void Update()
    {
        if (PeriodicTableManager.Instance != null && PeriodicTableManager.Instance.IsInputBlocked())
        {
            HideTooltip();
            return;
        }

        CheckMouseHover();

        if (currentTarget != null && calloutContainer.activeSelf)
        {
            UpdateCalloutPosition();
        }
    }

    private void CheckMouseHover()
    {
        if (Mouse.current == null) return;

        Camera activeCamera = Camera.main;
        if (activeCamera == null) return; 

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = activeCamera.ScreenPointToRay(mousePos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.TryGetComponent<AtomParticle>(out AtomParticle particle))
            {
                if (currentTarget != hit.transform)
                {
                    currentTarget = hit.transform;
                    ShowTooltip(particle.particleType);
                }
            }
            else
            {
                HideTooltip();
            }
        }
        else
        {
            HideTooltip();
        }
    }

   private void UpdateCalloutPosition()
    {
        Camera activeCamera = Camera.main;
        if (activeCamera == null) return;

        Vector3 worldPos = currentTarget.position;
        Vector2 screenPoint = activeCamera.WorldToScreenPoint(worldPos);

        Canvas rootCanvas = mainCanvasRect.GetComponentInParent<Canvas>();
        
        Camera camForUI = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : activeCamera;

        Vector2 localPointStart;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mainCanvasRect,      
            screenPoint,         
            camForUI,            
            out localPointStart  
        );

        if (dotRect != null) dotRect.anchoredPosition = localPointStart;

        Vector2 localPointEnd = localPointStart + labelOffset;
        tooltipText.rectTransform.anchoredPosition = localPointEnd;

        DrawLine(localPointStart, localPointEnd);
    }

    private void DrawLine(Vector2 localStart, Vector2 localEnd)
    {
        Vector2 differenceVector = localEnd - localStart;
        float distance = differenceVector.magnitude - padding; 
        
        lineRect.anchoredPosition = localStart;
        lineRect.sizeDelta = new Vector2(distance, lineRect.sizeDelta.y);

        float angle = Mathf.Atan2(differenceVector.y, differenceVector.x) * Mathf.Rad2Deg;
        lineRect.localRotation = Quaternion.Euler(0, 0, angle);
    }

    private void ShowTooltip(string text)
    {
        if (calloutContainer != null)
        {
            tooltipText.text = text;
            calloutContainer.SetActive(true);
        }
    }

    private void HideTooltip()
    {
        currentTarget = null;
        if (calloutContainer != null && calloutContainer.activeSelf)
        {
            calloutContainer.SetActive(false);
        }
    }
}