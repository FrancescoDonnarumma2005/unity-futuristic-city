using UnityEngine;

namespace EduUtils.AudioSystem
{
    [RequireComponent(typeof(AudioSource))]
    public class FootstepSystem : MonoBehaviour
    {
        [System.Serializable]
        public struct SurfaceDefinition
        {
            public string surfaceTag;      
            // OTTIMIZZAZIONE: Usiamo gli Array invece delle List per dati statici (minore overhead)
            public AudioClip[] clips;  
        }

        [Header("Configurazione Superfici")]
        [Tooltip("Mappa qui i tag alle clip audio")]
        [SerializeField] private SurfaceDefinition[] _surfaces;
        [SerializeField] private AudioClip[] _defaultClips; 
        
        // FIX CRITICO: Maschera di livello per ignorare il player
        [Tooltip("Inserisci qui il layer del pavimento (es. Default, Terrain) ed ESCLUDI il layer del Player")]
        [SerializeField] private LayerMask _groundLayerMask = Physics.DefaultRaycastLayers;

        [Header("Parametri Movimento")]
        [SerializeField] private float _stepInterval = 0.5f; 
        [SerializeField] private float _minVelocity = 0.1f;  
        [SerializeField] private float _raycastDistance = 2.0f; // Aumentato leggermente per sicurezza

        [Header("Debug")]
        [SerializeField] private bool _showDebugRay = true;

        private AudioSource _audioSource;
        private float _stepTimer;
        private Vector3 _lastPosition;
        private float _sqrMinVelocity;
        private Transform _transformCache;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.spatialBlend = 1f; 
            _transformCache = transform; // Cache del transform per evitare chiamate native
            
            // OTTIMIZZAZIONE: Calcoliamo il quadrato della velocità per evitare Mathf.Sqrt nel loop
            _sqrMinVelocity = _minVelocity * _minVelocity;
        }

        private void Start()
        {
            _lastPosition = _transformCache.position;
        }

        private void Update()
        {
            HandleFootsteps();
        }

        private void HandleFootsteps()
        {
            // OTTIMIZZAZIONE: Sostituito Vector3.Distance (che usa una costosa radice quadrata) con sqrMagnitude
            Vector3 movement = _transformCache.position - _lastPosition;
            float sqrSpeed = movement.sqrMagnitude / (Time.deltaTime * Time.deltaTime);
            
            _lastPosition = _transformCache.position;

            if (sqrSpeed < _sqrMinVelocity) 
            {
                _stepTimer = 0f;
                return;
            }

            _stepTimer += Time.deltaTime;

            if (_stepTimer >= _stepInterval)
            {
                PlayFootstep();
                _stepTimer -= _stepInterval; // Più preciso di _stepTimer = 0f per preservare i resti del delta time
            }
        }

        private void PlayFootstep()
        {
            Vector3 rayStart = _transformCache.position + Vector3.up * 0.5f;

            // FIX: Aggiunta _groundLayerMask e QueryTriggerInteraction.Ignore per non colpire gli ObjectSnap
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, _raycastDistance, _groundLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (_showDebugRay) Debug.DrawLine(rayStart, hit.point, Color.green, 0.5f);

                // OTTIMIZZAZIONE: Sostituito foreach con for loop. 
                // Il foreach sulle collection genera un Enumerator e causa Garbage Collection ad ogni passo. In VR causa micro-stuttering.
                for (int i = 0; i < _surfaces.Length; i++)
                {
                    if (hit.collider.CompareTag(_surfaces[i].surfaceTag))
                    {
                        PlayRandomClip(_surfaces[i].clips);
                        return;
                    }
                }
            }
            else
            {
                if (_showDebugRay) Debug.DrawLine(rayStart, rayStart + Vector3.down * _raycastDistance, Color.red, 0.5f);
            }

            PlayRandomClip(_defaultClips);
        }

        private void PlayRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            _audioSource.pitch = Random.Range(0.9f, 1.1f);
            _audioSource.PlayOneShot(clip);
        }
    }
}