using UnityEngine;

public class BillBoard : MonoBehaviour
{
    private Transform mainCameraTransform;

    void Start()
    {
        // Trova la telecamera del visore all'avvio
        mainCameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        // Fa ruotare il canvas verso la telecamera, ma lo mantiene dritto sull'asse Y
        transform.LookAt(transform.position + mainCameraTransform.rotation * Vector3.forward,
            mainCameraTransform.rotation * Vector3.up);
    }
}