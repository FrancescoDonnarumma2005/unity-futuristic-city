using UnityEngine;

public class LiquidContainer : MonoBehaviour
{
    [Header("Volume Settings")]
    public float maxVolume = 100f;
    public float currentVolume = 100f;

    [Header("Visuals")]
    [Tooltip("Inserisci qui il figlio che contiene la mesh del liquido (con pivot alla base)")]
    public Transform liquidMeshTransform;
    private Vector3 initialLiquidScale;

    // VARIABILI PER IL COLORE
    [Tooltip("Il colore iniziale di questo liquido")]
    public Color currentColor = Color.cyan; 
    private Material _liquidMaterial; // La copia indipendente del materiale

    [Header("Pouring (Travaso)")]
    public Transform pourSpout; // L'Empty object sull'orlo del becher
    public ParticleSystem pourParticleSystem; // Il VFX della cascata d'acqua
    public float pourThreshold = 45f; // Gradi di inclinazione per far uscire l'acqua
    public float pourRate = 20f; // Velocità di travaso
    public LayerMask containerLayerMask; // Il Layer degli altri becher (es. "Object")
    public float pourRadius = 0.05f;
    public float beakerRadius = 0.05f;

    [Header("Feedback Sensoriale (Audio Travaso)")]
    [Tooltip("Audio Source per il suono dell'acqua versata")]
    public AudioSource pouringAudioSource;

    [Header("Boiling & Steam (Ebollizione)")]
    public ParticleSystem steamParticleSystem;
    [Tooltip("Audio Source per il suono dell'acqua che bolle")]
    public AudioSource boilingAudioSource;

    

    

    private void Start()
    {
        if (liquidMeshTransform != null)
        {
            initialLiquidScale = liquidMeshTransform.localScale;
            
            // Creiamo un'istanza indipendente del materiale
            MeshRenderer renderer = liquidMeshTransform.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                _liquidMaterial = renderer.material; // .material crea automaticamente una copia!
                UpdateColorVisuals(); // Impostiamo il colore iniziale
            }
        }
        UpdateVisuals();
    }

    // Un piccolo metodo dedicato solo all'aggiornamento del colore
   private void UpdateColorVisuals()
    {
        // 1. Aggiorniamo il colore del liquido nel becher
        if (_liquidMaterial != null)
        {
            _liquidMaterial.SetColor("_BaseColor", currentColor); 
            _liquidMaterial.SetColor("_EmissionColor", currentColor); 
        }

        // 2. Aggiorniamo il colore della cascata (Particle System)
        if (pourParticleSystem != null)
        {
            // Accediamo al modulo principale del sistema particellare
            var mainModule = pourParticleSystem.main;
            
            // Cambiamo il colore di partenza delle particelle
            mainModule.startColor = currentColor;
        }
    }

    private void Update()
    {
        // Calcoliamo l'angolo di inclinazione del becher rispetto all'asse Y globale (il "sopra" del mondo)
        float tiltAngle = Vector3.Angle(Vector3.up, transform.up);

        // Se è inclinato oltre la soglia e c'è ancora liquido dentro
        if (tiltAngle > pourThreshold && currentVolume > 0.01f)
        {
            PourLiquid();
        }
        else
        {
            StopPouring();
        }
    }

    // ==========================================
    // SEZIONE TRAVASO E FISICA
    // ==========================================

    /// <summary>
    /// Gestisce l'uscita del liquido, la parabola e l'audio dinamico
    /// </summary>
    private void PourLiquid()
    {
        // 1. CALCOLO DEL BECCUCCIO DINAMICO
        // Troviamo la direzione orizzontale verso cui sta pendendo il becher
        Vector3 tiltDirection = new Vector3(transform.up.x, 0, transform.up.z).normalized;
        
        // Troviamo il punto esatto sull'orlo più basso (Centro + Direzione * Raggio)
        Vector3 lowestRimPoint = pourSpout.position + (tiltDirection * beakerRadius);

        // 2. GRAFICA E PARABOLA
        float tiltAngle = Vector3.Angle(Vector3.up, transform.up);
        float flowIntensity = Mathf.Clamp01((tiltAngle - pourThreshold) / 45f);
        float pushForce = Mathf.Lerp(0.2f, 1.5f, flowIntensity); 

        if (pourParticleSystem != null)
        {
            // Spostiamo fisicamente le particelle sull'orlo più basso
            pourParticleSystem.transform.position = lowestRimPoint;
            
            // Ruotiamo le particelle in modo che "sparino" l'acqua in avanti
            pourParticleSystem.transform.forward = tiltDirection; 

            if (!pourParticleSystem.isPlaying) pourParticleSystem.Play();
        }

        // 3. AUDIO DINAMICO
        if (pouringAudioSource != null)
        {
            if (!pouringAudioSource.isPlaying) pouringAudioSource.Play();
            pouringAudioSource.volume = Mathf.Lerp(0.2f, 1.0f, flowIntensity);
            pouringAudioSource.pitch = Mathf.Lerp(0.8f, 1.2f, flowIntensity);
        }

        // 4. SOTTRAZIONE DEL LIQUIDO
        float amountToPour = pourRate * Time.deltaTime;
        currentVolume = Mathf.Max(0f, currentVolume - amountToPour);
        UpdateVisuals();

       // 5. SPHERECAST PERFORANTE (Risolve i conflitti con il proprio collider)
        if (pourSpout != null)
        {
            Vector3 pourDirection = (Vector3.down + (tiltDirection * pushForce)).normalized;
            
            // Debug visivo
            Debug.DrawRay(lowestRimPoint, pourDirection * 2.0f, Color.red);

            // QUI LA MAGIA: Usiamo SphereCastAll. Restituisce un Array di TUTTI gli oggetti colpiti lungo i 2 metri!
            RaycastHit[] hits = Physics.SphereCastAll(lowestRimPoint, pourRadius, pourDirection, 2.0f, containerLayerMask);

            // Scorriamo tutti gli oggetti che il nostro cilindro d'acqua sta attraversando
            foreach (RaycastHit hit in hits)
            {
                LiquidContainer receiver = hit.collider.GetComponentInParent<LiquidContainer>();
                
                // Se abbiamo trovato un contenitore valido E NON siamo noi stessi...
                if (receiver != null && receiver != this)
                {
                    // ...versiamo il liquido!
                    receiver.ReceiveLiquid(amountToPour, currentColor);
                    
                    // Interrompiamo il ciclo (non vogliamo riempire due becher impilati uno sull'altro)
                    break; 
                }
            }
        }
    }

    private void StopPouring()
    {
        if (pourParticleSystem != null && pourParticleSystem.isPlaying) 
            pourParticleSystem.Stop();
            
        // Spegniamo il suono istantaneamente quando raddrizziamo il becher
        if (pouringAudioSource != null && pouringAudioSource.isPlaying)
            pouringAudioSource.Stop();
    }

    /// <summary>
    /// Metodo chiamato da un altro becher che ci sta versando liquido addosso
    /// </summary>
    /// <summary>
 /// Metodo chiamato da un altro becher che ci sta versando liquido e colore addosso
 /// </summary>
 public void ReceiveLiquid(float incomingAmount, Color incomingColor)
 {
     // Se il becher è vuoto, prende direttamente il colore del nuovo liquido
     if (currentVolume <= 0.01f)
     {
         currentColor = incomingColor;
     }
     else
     {
         // Se c'è già del liquido, facciamo la media ponderata dei colori!
         float totalVolumeAfterPour = currentVolume + incomingAmount;

         // Calcoliamo "quanto" pesa il nuovo liquido sul totale (es. 0.1 significa che è il 10% del totale)
         float incomingPercentage = incomingAmount / totalVolumeAfterPour;

         // Misceliamo
         currentColor = Color.Lerp(currentColor, incomingColor, incomingPercentage);
     }

     // Aggiorniamo il volume matematico
     currentVolume = Mathf.Min(maxVolume, currentVolume + incomingAmount);

     // Aggiorniamo l'estetica (Mesh e Colore)
     UpdateVisuals();
     UpdateColorVisuals();
 }

    // ==========================================
    // SEZIONE EBULLIZIONE
    // ==========================================

    public void Evaporate(float amountToEvaporate)
    {
        if (currentVolume > 0.01f)
        {
            if (steamParticleSystem != null && !steamParticleSystem.isPlaying) steamParticleSystem.Play();
            if (boilingAudioSource != null && !boilingAudioSource.isPlaying) boilingAudioSource.Play();
            
            currentVolume = Mathf.Max(0f, currentVolume - amountToEvaporate);
            UpdateVisuals(); 
        }
        else
        {
            currentVolume = 0f;
            StopEvaporazione();
            UpdateVisuals();
        }
    }

    public void StopEvaporazione()
    {
        if (steamParticleSystem != null && steamParticleSystem.isPlaying) steamParticleSystem.Stop();
        if (boilingAudioSource != null && boilingAudioSource.isPlaying) boilingAudioSource.Stop();
    }

    // ==========================================
    // SEZIONE GRAFICA GLOBALE
    // ==========================================

    public void UpdateVisuals()
    {
        if (liquidMeshTransform != null)
        {
            float fillPercentage = maxVolume > 0.001f ? (currentVolume / maxVolume) : 0f;
            Vector3 newScale = initialLiquidScale;
            newScale.y = initialLiquidScale.y * fillPercentage;
            
            liquidMeshTransform.localScale = newScale;
            
            // Disabilita la mesh se il becher è vuoto per risparmiare risorse
            liquidMeshTransform.gameObject.SetActive(currentVolume > 0.01f);
        }
    }
}