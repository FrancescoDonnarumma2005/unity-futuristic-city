using UnityEngine;
using System.Collections;

namespace EduUtils.UI
{
    public class AutoClosePanel : MonoBehaviour
    {
        [Tooltip("Tempo in secondi prima che il pannello si disattivi da solo")]
        [SerializeField] private float _displayDuration = 4.0f;

        private void OnEnable()
        {
            // Ogni volta che l'oggetto viene attivato (SetActive(true)), facciamo partire il timer
            StartCoroutine(CloseAfterDelayRoutine());
        }

        private IEnumerator CloseAfterDelayRoutine()
        {
            yield return new WaitForSeconds(_displayDuration);
            gameObject.SetActive(false);
        }
    }
}