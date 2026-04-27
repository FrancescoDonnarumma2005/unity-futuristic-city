using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Electromagnet : MonoBehaviour
{
    [Header("Stato")]
    public bool isPoweredOn = false; // Se true, il magnete attira
    
    [Header("Fisica")]
    [Tooltip("La forza di attrazione. Aumentala se gli oggetti sono pesanti.")]
    public float magneticForce = 50f;
    [Tooltip("Il punto verso cui gli oggetti voleranno (es. la punta del magnete)")]
    public Transform magnetCenter; 

    [Header("Impostazioni Desktop")]
    public Camera desktopCamera;
    public float interactionDistance = 2.5f;

    [Header("Feedback VR")]
    public AudioSource humAudio; // Il suono del ronzio elettrico (Looping)

    // Memoria: la lista degli oggetti metallici attualmente dentro la sfera invisibile
    private List<Rigidbody> metalObjectsInRange = new List<Rigidbody>();

    private void Update()
    {
        HandleDesktopInput();
    }

    private void HandleDesktopInput()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Camera cam = desktopCamera != null ? desktopCamera : Camera.main;
            if (cam != null)
            {
                Ray ray;
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
                    Electromagnet hitMagnet = hit.collider.GetComponentInParent<Electromagnet>();
                    
                    if (hitMagnet == this)
                    {
                        ToggleMagnet();
                        Debug.Log($"<color=cyan>ELETTROMAGNETE: Azionato con successo! (Tasto E). isPoweredOn è: {isPoweredOn}</color>");
                    }
                    else
                    {
                        // Se c'è un trigger per la zona di attrazione magnetica, ignoriamolo
                        if (!hit.collider.isTrigger) 
                        {
                            Debug.Log($"<color=yellow>ELETTROMAGNETE: Il raggio (E) ha colpito '{hit.collider.name}' invece del magnete.</color>");
                        }
                    }
                }
                else
                {
                    Debug.Log("<color=orange>ELETTROMAGNETE: Nessun oggetto colpito nel raggio d'interazione.</color>");
                }
            }
        }
    }

    // Usiamo FixedUpdate perché stiamo spingendo oggetti fisici (Rigidbody)
    private void FixedUpdate()
    {
        if (isPoweredOn)
        {
            ApplyMagneticForce();
        }
    }

    private void ApplyMagneticForce()
    {
        // Pulizia di sicurezza nel caso un oggetto venga distrutto mentre viene attratto
        metalObjectsInRange.RemoveAll(item => item == null);

        foreach (Rigidbody rb in metalObjectsInRange)
        {
            // 1. Calcoliamo la direzione dal pezzo di metallo verso il magnete
            Vector3 directionToMagnet = magnetCenter.position - rb.position;
            
            // 2. Calcoliamo la distanza
            float distance = directionToMagnet.magnitude;

            // 3. Simuliamo la fisica reale: più l'oggetto è vicino, più la forza è violenta.
            // Usiamo Clamp per evitare che la forza diventi infinita (bug) se la distanza è 0.
            float distanceMultiplier = Mathf.Clamp(1f / distance, 0.5f, 5f);

            // 4. Applichiamo la forza! (Direzione * Forza Base * Moltiplicatore di vicinanza)
            Vector3 appliedForce = directionToMagnet.normalized * magneticForce * distanceMultiplier;
            
            rb.AddForce(appliedForce * Time.fixedDeltaTime, ForceMode.Impulse);
        }
    }

    // Quando un oggetto entra nella sfera...
    private void OnTriggerEnter(Collider other)
    {
        // ...se ha il Tag giusto...
        if (other.CompareTag("Metal"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null && !metalObjectsInRange.Contains(rb))
            {
                metalObjectsInRange.Add(rb); // Lo aggiungiamo alla lista di attrazione
            }
        }
    }

    // Quando l'oggetto esce dalla sfera (o noi allontaniamo il magnete)...
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Metal"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null && metalObjectsInRange.Contains(rb))
            {
                metalObjectsInRange.Remove(rb); // Smette di essere attratto
            }
        }
    }

    // ==========================================
    // METODO PER LA REALTÀ VIRTUALE
    // ==========================================
    public void ToggleMagnet()
    {
        isPoweredOn = !isPoweredOn;

        // Gestione del ronzio elettrico
        if (isPoweredOn && humAudio != null) humAudio.Play();
        else if (!isPoweredOn && humAudio != null) humAudio.Stop();
        
        // Se spegniamo il magnete, svuotiamo la lista per evitare attrazioni "fantasma"
        if (!isPoweredOn)
        {
            metalObjectsInRange.Clear();
        }
    }
}