using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TMP_InputField))]
public class VRInputFieldSelect : MonoBehaviour, IPointerClickHandler
{
    private TMP_InputField inputField;

    private void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (VRNumpadManager.Instance != null)
        {
            VRNumpadManager.Instance.OpenNumpad(inputField);
        }
    }
}