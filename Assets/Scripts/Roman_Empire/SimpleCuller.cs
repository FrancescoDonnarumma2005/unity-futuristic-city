using UnityEngine;

public class SimpleCuller : MonoBehaviour
{
    void Start()
    {
        Camera cam = GetComponent<Camera>();
        float[] distances = new float[32];

        // Imposta la distanza di default (0 significa che usa il Far Clip Plane della camera)
        // Ma per il layer specifico (es. layer 6 se "SmallProps" è il sesto nella lista) impostiamo un limite.
        
        // ATTENZIONE: Sostituisci '6' con l'indice numerico del tuo layer "SmallProps"
        // Puoi vederlo nel menu Layers (es. se è scritto "6: SmallProps")
        distances[3] = 90f; // I piccoli oggetti spariscono a 50 metri!

        cam.layerCullDistances = distances;
        cam.layerCullSpherical = true; // Aiuta a nascondere il popping
    }
}