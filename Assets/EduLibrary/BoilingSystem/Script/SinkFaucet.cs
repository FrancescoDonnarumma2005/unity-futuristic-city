using UnityEngine;
using UnityEngine.InputSystem;

public class SinkFaucet : MonoBehaviour
{
    [Header("Stato Rubinetto")]
    public bool isFlowing = false; // Se true, l'acqua sta uscendo

    [Header("Impostazioni Erogazione")]
    public Transform pourSpout; // Il punto da cui esce l'acqua
    public float pourRate = 20f; // Quanta acqua esce al secondo
    public float pourRadius = 0.05f; // Lo spessore del getto d'acqua
    public LayerMask containerLayerMask; // Il layer dei becher
    
    [Tooltip("Il colore dell'acqua pura che esce dal lavandino")]
    public Color waterColor = new Color(0.8f, 0.9f, 1f, 0.5f); // Un azzurrino trasparente

    [Header("Impostazioni Desktop")]
    [Tooltip("La telecamera del giocatore Desktop per calcolare dove sta guardando")]
    public Camera desktopCamera;
    [Tooltip("Distanza massima (in metri) del raggio visivo per interagire col lavandino")]
    public float interactionDistance = 2.5f;

    [Header("Feedback Visivo e Sonoro")]
    public ParticleSystem waterParticles;
    public AudioSource waterAudio;

    private void Update()
    {
        // 1. Ascoltiamo l'input da tastiera (Tasto E) + Raggio Visivo
        HandleDesktopInput();

        // 2. Gestiamo il flusso d'acqua
        if (isFlowing)
        {
            PourWater();
        }
        else
        {
            StopWater();
        }
    }

    private void HandleDesktopInput()
    {
        // Controlliamo se è stato premuto il tasto E in questo frame
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (desktopCamera != null)
            {
                // Semplifichiamo: spariamo un raggio sempre e solo dritto in avanti dal centro della telecamera
                Ray ray = new Ray(desktopCamera.transform.position, desktopCamera.transform.forward);
                
                // Disegna una linea rossa di 2.5 metri nella tab "Scene" per farti vedere dove stai sparando
                Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.red, 2f);

                // Usiamo un raggio singolo, ma gli diciamo di ignorare il layer 2 ("Ignore Raycast") 
                // dove di solito si trova il corpo del giocatore per evitare che si colpisca da solo
                int layerMask = ~(1 << 2); 

                if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, layerMask))
                {
                    SinkFaucet hitFaucet = hit.collider.GetComponentInParent<SinkFaucet>();
                    
                    if (hitFaucet == this)
                    {
                        ToggleFaucet();
                        Debug.Log($"<color=cyan>LAVANDINO: Azionato con successo! (Tasto E). isFlowing è: {isFlowing}</color>");
                    }
                    else
                    {
                        // Questo log ti salva la vita: ti dice ESATTAMENTE cosa sta colpendo il raggio
                        Debug.Log($"<color=yellow>LAVANDINO: Il raggio ha colpito '{hit.collider.name}' invece del rubinetto. Avvicinati o mira meglio!</color>");
                    }
                }
                else
                {
                    Debug.Log("<color=orange>LAVANDINO: Tasto E premuto, ma sei troppo lontano o stai guardando il vuoto.</color>");
                }
            }
            else
            {
                Debug.LogWarning("<color=orange>Attenzione: Manca la Desktop Camera nello script del lavandino!</color>");
            }
        }
    }

    private void PourWater()
    {
        // 1. Attiva Grafica e Audio
        if (waterParticles != null && !waterParticles.isPlaying) waterParticles.Play();
        if (waterAudio != null && !waterAudio.isPlaying) waterAudio.Play();

        // 2. SphereCastAll per riempire i becher sottostanti
        if (pourSpout != null)
        {
            Vector3 pourDirection = pourSpout.forward;
            
            // Debug visivo per l'Editor
            Debug.DrawRay(pourSpout.position, pourDirection * 1.5f, Color.blue);

            RaycastHit[] hits = Physics.SphereCastAll(pourSpout.position, pourRadius, pourDirection, 1.5f, containerLayerMask);

            float amountToPour = pourRate * Time.deltaTime;

            foreach (RaycastHit hit in hits)
            {
                LiquidContainer receiver = hit.collider.GetComponentInParent<LiquidContainer>();
                
                if (receiver != null)
                {
                    receiver.ReceiveLiquid(amountToPour, waterColor);
                    break; 
                }
            }
        }
    }

    private void StopWater()
    {
        if (waterParticles != null && waterParticles.isPlaying) waterParticles.Stop();
        if (waterAudio != null && waterAudio.isPlaying) waterAudio.Stop();
    }

    // ==========================================
    // METODI PUBBLICI PER LA VR
    // ==========================================

    public void OpenFaucet()
    {
        isFlowing = true;
    }

    public void CloseFaucet()
    {
        isFlowing = false;
    }

    public void ToggleFaucet()
    {
        isFlowing = !isFlowing;
    }
}