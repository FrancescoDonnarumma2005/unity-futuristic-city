using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VRNewtonCradleStabilizer : MonoBehaviour
{
    [Header("Impostazioni Pendolo")]
    [Tooltip("Fattore per compensare la perdita di energia di PhysX ad ogni urto")]
    [SerializeField] private float energyPreservationFactor = 1.01f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Setup rigoroso per la fisica ad alta velocità e precisione
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        
        // Rimuoviamo l'attrito dell'aria che rallenterebbe il pendolo nel tempo
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
    }

    private void FixedUpdate()
    {
        // Blocca la rotazione: nel pendolo perfetto l'energia cinetica 
        // non deve disperdersi nel far ruotare la sfera su se stessa
        rb.angularVelocity = Vector3.zero;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Piccolo trucco per mantenere viva l'energia cinetica dopo l'impatto.
        // Utilizziamo linearVelocity che è lo standard aggiornato di Unity 6.
        if (collision.gameObject.CompareTag("NewtonSphere"))
        {
            rb.linearVelocity = rb.linearVelocity * energyPreservationFactor;
        }
    }
}