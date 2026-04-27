using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class BunsenBurner : MonoBehaviour
{
    [Header("UI & Effetti")]
    public TextMeshProUGUI temperatureDisplay;
    public ParticleSystem flameVFX;

    [Header("Parametri Termici")]
    public float maxTemperature = 100f;
    public float roomTemperature = 0f; 
    public float heatingRate = 15f; 
    public float coolingRate = 25f; 
    public float evaporationRate = 25f; // Quantità di liquido persa al secondo

    [Header("Impostazioni Desktop")]
    public Camera desktopCamera;
    public float interactionDistance = 2.5f;

    private bool _isOn = false;
    private float _currentTemperature;
    
    // Il nostro bersaglio da far evaporare
    private LiquidContainer _targetContainer;

    private void Start()
    {
        _currentTemperature = roomTemperature;
        UpdateDisplay();
        
        if (flameVFX != null) flameVFX.Stop();
    }

    /// <summary>
    /// Metodo collegato al bottone UI del Canvas (VR) o chiamato dalla tastiera (Desktop).
    /// </summary>
    public void ToggleBurner()
    {
        _isOn = !_isOn;
        
        if (_isOn && flameVFX != null) flameVFX.Play();
        else if (!_isOn && flameVFX != null) flameVFX.Stop();
    }

    private void Update()
    {
        // Controllo input desktop (Tasto E e Raggio)
        HandleDesktopInput();

        // 1. Logica della Temperatura (Riscaldamento / Raffreddamento)
        if (_isOn)
        {
            _currentTemperature = Mathf.Min(_currentTemperature + (heatingRate * Time.deltaTime), maxTemperature);
        }
        else
        {
            _currentTemperature = Mathf.Max(_currentTemperature - (coolingRate * Time.deltaTime), roomTemperature);
        }

        UpdateDisplay();

        // 2. Chiamiamo il sensore spaziale per trovare la beuta più vicina
        FindNearestContainer();

        // 3. Logica dell'Evaporazione
        if (_currentTemperature >= maxTemperature)
        {
            if (_targetContainer != null)
            {
                // Trovata! Facciamo evaporare il liquido e suonare l'audio
                _targetContainer.Evaporate(evaporationRate * Time.deltaTime);
            }
        }
        else
        {
            // Se spegniamo il fornello e i gradi scendono, fermiamo l'audio/vapore della beuta
            if (_targetContainer != null)
            {
                _targetContainer.StopEvaporazione();
            }
        }
    }

    /// <summary>
    /// Trova la beuta più vicina entro un raggio d'azione (60 cm), ignorando se è ancorata o meno.
    /// Se una beuta viene allontanata dal fuoco, le dice di spegnere l'ebollizione.
    /// </summary>
    private void FindNearestContainer()
    {
        LiquidContainer newTarget = null;
        
        // Troviamo tutti i LiquidContainer presenti nella scena
        LiquidContainer[] allContainers = FindObjectsByType<LiquidContainer>(FindObjectsSortMode.None);
        
        float maxDistance = 0.5f; // Raggio d'azione del calore: 0.5 metri
        float closestDistance = maxDistance;

        foreach (LiquidContainer container in allContainers)
        {
            // Calcoliamo la distanza tra il fuoco e il contenitore
            float distance = Vector3.Distance(transform.position, container.transform.position);
            
            // Se la beuta è dentro l'area di calore ed è la più vicina trovata finora...
            if (distance < closestDistance)
            {
                newTarget = container;
                closestDistance = distance;
            }
        }

        // DIFESA ARCHITETTURALE: Se c'era una beuta che bolliva e l'abbiamo tolta dal fuoco, spegniamo il suo vapore/audio!
        if (_targetContainer != null && _targetContainer != newTarget)
        {
            _targetContainer.StopEvaporazione();
        }

        // Aggiorniamo il nostro bersaglio attuale
        _targetContainer = newTarget;
    }

    private void HandleDesktopInput()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Camera cam = desktopCamera != null ? desktopCamera : Camera.main;
            if (cam != null)
            {
                Ray ray;
                // Mantiene il comportamento per mouse bloccato al centro, o cursore libero
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                }
                else if (Mouse.current != null)
                {
                    ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
                }
                else
                {
                    ray = new Ray(cam.transform.position, cam.transform.forward);
                }
                
                int layerMask = ~(1 << 2); // ignora il layer 2

                if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, layerMask))
                {
                    BunsenBurner hitBurner = hit.collider.GetComponentInParent<BunsenBurner>();
                    
                    if (hitBurner == this)
                    {
                        ToggleBurner();
                        Debug.Log($"<color=cyan>FORNELLO: Azionato con successo! (Tasto E). _isOn è: {_isOn}</color>");
                    }
                    else
                    {
                        Debug.Log($"<color=yellow>FORNELLO: Il raggio (E) ha colpito '{hit.collider.name}' invece del fornello.</color>");
                    }
                }
                else
                {
                    Debug.Log("<color=orange>FORNELLO: Nessun oggetto colpito nel raggio d'interazione.</color>");
                }
            }
        }
    }

    /// <summary>
    /// Aggiorna il numeretto UI e cambia il colore del testo in base al calore.
    /// </summary>
    private void UpdateDisplay()
    {
        if (temperatureDisplay != null)
        {
            temperatureDisplay.text = Mathf.RoundToInt(_currentTemperature).ToString() + " °C";
            temperatureDisplay.color = Color.Lerp(Color.white, Color.red, (_currentTemperature - roomTemperature) / (maxTemperature - roomTemperature));
        }
    }
}