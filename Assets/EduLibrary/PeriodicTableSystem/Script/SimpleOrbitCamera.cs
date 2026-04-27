using UnityEngine;
using UnityEngine.InputSystem; 

public class SimpleOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target; 

    [Header("Settings Rotazione")]
    public float rotationSpeed = 0.2f; 

    [Header("Settings Zoom")]
    public float zoomSpeed = 0.5f;      // Velocità dello zoom
    public float minDistance = 2.0f;    // Distanza minima (per non entrare nell'atomo)
    public float maxDistance = 20.0f;   // Distanza massima

    void Update()
    {
        // --- 1. BLOCCO INPUT UI ---
        // Se il Manager esiste E ci dice che l'input è bloccato (menu aperti),
        // ci fermiamo subito. La camera non si muoverà.
        if (PeriodicTableManager.Instance != null && PeriodicTableManager.Instance.IsInputBlocked())
        {
            return; 
        }
        // ----------------------------------

        // Controllo di sicurezza: se non c'è il mouse o il target, non fare nulla
        if (Mouse.current == null || target == null) return;

        // --- 2. ROTAZIONE ---
        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();

            float mouseX = delta.x;
            float mouseY = delta.y;

            // Ruota attorno all'asse Y globale (orizzontale)
            transform.RotateAround(target.position, Vector3.up, mouseX * rotationSpeed);
            // Ruota attorno all'asse X locale (verticale)
            transform.RotateAround(target.position, transform.right, -mouseY * rotationSpeed);
        }

        // --- 3. ZOOM ---
        // Leggiamo la rotellina (Y è il valore verticale dello scroll)
        float scrollValue = Mouse.current.scroll.ReadValue().y;

        // Se stiamo scrollando (il valore è diverso da 0)
        if (Mathf.Abs(scrollValue) > 0.0f)
        {
            // Calcoliamo la distanza attuale
            float currentDistance = Vector3.Distance(transform.position, target.position);

            // Calcoliamo la nuova distanza desiderata
            // Nota: Il valore dello scroll è spesso 120 o -120, lo moltiplichiamo per 0.01 per renderlo gestibile
            float targetDistance = currentDistance - (scrollValue * 0.01f * zoomSpeed);

            // Limitiamo la distanza tra min e max (Clamp)
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);

            // --- APPLICAZIONE DELLA POSIZIONE ---
            // 1. Troviamo la direzione dal target verso la camera
            Vector3 direction = (transform.position - target.position).normalized;

            // 2. Posizioniamo la camera lungo quella direzione alla distanza esatta calcolata
            transform.position = target.position + (direction * targetDistance);
        }
    }
}