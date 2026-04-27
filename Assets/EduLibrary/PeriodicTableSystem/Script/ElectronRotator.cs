using UnityEngine;

public class ElectronRotator : MonoBehaviour
{
    [Tooltip("Velocità di rotazione in gradi al secondo")]
    [SerializeField] private float rotationSpeed = 50f;

    [Tooltip("Asse attorno al quale ruotare (Relativo all'oggetto)")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    [Tooltip("Variazione casuale della velocità per rendere il movimento meno meccanico")]
    [SerializeField] private bool randomizeSpeed = true;

    private float _currentSpeed;

    private void Start()
    {
        // Se attivo, aggiunge una variazione tra -20% e +20% alla velocità base
        if (randomizeSpeed)
        {
            _currentSpeed = rotationSpeed * Random.Range(0.8f, 1.2f);
            
            // Opzionale: Ruota l'oggetto in una posizione casuale all'avvio 
            // per non averli tutti allineati
            transform.Rotate(rotationAxis, Random.Range(0f, 360f));
        }
        else
        {
            _currentSpeed = rotationSpeed;
        }
    }

    private void Update()
    {
        // Ruota sull'asse locale (Space.Self è il default)
        transform.Rotate(rotationAxis, _currentSpeed * Time.deltaTime);
    }
}