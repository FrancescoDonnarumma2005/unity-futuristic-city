using UnityEngine;

namespace InspectionSystem
{
    [CreateAssetMenu(fileName = "NewInspectable", menuName = "Inspection System/Item Data")]
    public class InspectableItemData : ScriptableObject
    {
        // --- AGGIUNTA PER L'INVENTARIO ---
        [Header("Inventory UI")]
        [Tooltip("L'immagine che apparirà nella griglia dell'inventario")]
        public Sprite icon; 
        // ---------------------------------------

        [Header("Visuals")]
        public GameObject modelPrefab;
        public float initialZoom = 1f; 
        public Vector3 initialRotation = Vector3.zero;

        [Header("Camera Settings")]
        [Tooltip("Distanza iniziale della telecamera dall'oggetto")]
        public float defaultCameraDistance = 0.1f; 
        [Tooltip("Quanto vicino può arrivare la camera (Zoom In massimo)")]
        public float minCameraDistance = 0.001f;
        [Tooltip("Quanto lontano può andare la camera (Zoom Out massimo)")]
        public float maxCameraDistance = 1.0f;

        [Header("Info")]
        public string itemName;
        [TextArea(3, 10)] public string itemDescription;
    }
}