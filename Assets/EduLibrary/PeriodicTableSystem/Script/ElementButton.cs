using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ElementButton : MonoBehaviour
{
    [Header("Configurazione")]
    public int atomicNumber; 

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI symbolText;
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image backgroundImage;

    private void Start()
    {
        Invoke(nameof(AutoSetup), 0.05f); 
        GetComponent<Button>().onClick.AddListener(OnElementClicked);
    }

    void AutoSetup()
    {
        if (ElementDatabase.Instance != null)
        {
            ElementData myData = ElementDatabase.Instance.GetElement(atomicNumber);
            
            if (myData != null)
            {
                if (symbolText) symbolText.text = myData.symbol;
                if (numberText) numberText.text = myData.atomicNumber.ToString();
                if (nameText) nameText.text = myData.elementName;
                if (backgroundImage) backgroundImage.color = myData.uiColor;
            }
        }
    }

    private void OnElementClicked()
    {
        if (PeriodicTableManager.Instance != null)
        {
            PeriodicTableManager.Instance.SelectElement(atomicNumber);
        }
    }
    
    public void ApplicaStileVisivo(Color coloreSfondo)
    {
        if (backgroundImage != null) 
            backgroundImage.color = coloreSfondo;

        Color coloreTesto = new Color(coloreSfondo.r * 0.25f, coloreSfondo.g * 0.25f, coloreSfondo.b * 0.25f, 1f);

        if (symbolText) symbolText.color = coloreTesto;
        if (numberText) numberText.color = coloreTesto;
        if (nameText) nameText.color = coloreTesto;
    }

    public void AttivaModalitaMini()
    {
        if (nameText) nameText.gameObject.SetActive(false);
        if (numberText) numberText.gameObject.SetActive(false);

        if (symbolText)
        {
            symbolText.gameObject.SetActive(true);
            symbolText.enableAutoSizing = true;
            symbolText.fontSizeMin = 5;  
            symbolText.fontSizeMax = 24; 
            symbolText.margin = new Vector4(0, 0, 0, 0); 
        }
    }
}