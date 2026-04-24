using UnityEngine;

namespace FuturisticCity.EduLibrary.POI
{
    public class PointOfInterest : MonoBehaviour
    {
        [SerializeField] private POIDataSO poiData;

        private void OnTriggerEnter(Collider other)
        {
            // Cerchiamo il componente HUD sulla navicella o sul player che entra
            var hud = other.GetComponentInChildren<SpaceshipHUDManager>();
            if (hud != null && poiData != null)
            {
                hud.ShowDialogue(poiData);
            }
        }
    }
}