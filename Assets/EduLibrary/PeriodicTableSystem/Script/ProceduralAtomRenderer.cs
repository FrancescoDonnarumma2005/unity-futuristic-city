using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ProceduralAtomRenderer : MonoBehaviour
{
    [Header("Configurazione Nucleo")]
    public GameObject protonPrefab;
    public GameObject neutronPrefab;
    public float nucleonScale = 0.35f;

    [Header("Algoritmo Packing Nucleo")]
    [Tooltip("Numero di iterazioni per sistemare le sfere. Piu' alto = piu' compatto.")]
    public int packingIterations = 100;
    [Tooltip("Quanto le particelle vengono attratte dal centro.")]
    public float centerAttraction = 0.1f;
    [Tooltip("Quanto le particelle si respingono se si toccano.")]
    public float repulsionForce = 0.5f;

    [Header("Configurazione Elettroni")]
    public GameObject electronPrefab;
    public GameObject orbitLinePrefab;

    [Space(10)]
    public float orbitSpacing = 1.5f;
    public float rotationSpeed = 30f;

    [Header("Animazione")]
    public float animationDuration = 2.0f;

    [Header("Correzioni Modello")]
    public float baseOrbitScale = 1.0f;

    private List<Transform> nucleonTransforms = new List<Transform>();
    private Transform nucleusContainer;
    private Transform orbitsContainer;
    private List<Transform> shellList = new List<Transform>();
    private bool isAnimating = false;

    private void Update()
    {
        if (orbitsContainer != null)
        {
            orbitsContainer.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        }

        // Manteniamo solo il click del mouse per comodita' di test su Desktop
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !isAnimating)
        {
            CheckNucleusClick();
        }
    }

    // --- FUNZIONE PUBBLICA PER IL MANAGER DEGLI INPUT ---
    public void TriggerAnimation()
    {
        if (!isAnimating)
        {
            StartCoroutine(AnimateOrbitsVertical());
        }
    }

    void CheckNucleusClick()
    {
        Camera activeCamera = Camera.main;
        if (activeCamera == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = activeCamera.ScreenPointToRay(mousePos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            string n = hit.collider.name;
            if (n.Contains("Protone") || n.Contains("Neutrone"))
            {
                TriggerAnimation();
            }
        }
    }

    IEnumerator AnimateOrbitsVertical()
    {
        isAnimating = true;
        float elapsed = 0f;
        float targetRotation = 720f;

        List<Vector3> randomAxes = new List<Vector3>();

        for (int i = 0; i < shellList.Count; i++)
        {
            float randomAngle = Random.Range(0f, 360f);
            Vector3 randomAxis = Quaternion.Euler(0, randomAngle, 0) * Vector3.right;
            randomAxes.Add(randomAxis);
        }

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float percentage = elapsed / animationDuration;
            float curve = Mathf.SmoothStep(0f, 1f, percentage);
            float currentAngle = Mathf.Lerp(0, targetRotation, curve);

            for (int i = 0; i < shellList.Count; i++)
            {
                if (shellList[i] == null) continue;
                shellList[i].localRotation = Quaternion.AngleAxis(currentAngle, randomAxes[i]);
            }
            yield return null;
        }

        foreach (Transform shell in shellList)
        {
            if (shell != null) shell.localRotation = Quaternion.identity;
        }

        isAnimating = false;
    }

    public void InitializeAtom(ElementData data, int customElectronCount = -1)
    {
        foreach (Transform child in transform) Destroy(child.gameObject);
        nucleonTransforms.Clear();
        shellList.Clear();

        nucleusContainer = new GameObject("Nucleo_Container").transform;
        nucleusContainer.SetParent(transform, false);

        orbitsContainer = new GameObject("Orbite_Container").transform;
        orbitsContainer.SetParent(transform, false);

        GeneratePackedNucleus(data);
        
        int finalElectrons = (customElectronCount != -1) ? customElectronCount : data.atomicNumber;
        GenerateElectrons(finalElectrons);

        if (VRModelRotator.Instance != null)
        {
            VRModelRotator.Instance.SetTarget(this.transform);
        }
    }

    void GeneratePackedNucleus(ElementData data)
    {
        int numProtons = data.numeroProtoni;
        int numNeutrons = data.numeroNeutroni;

        List<string> particlesToPlace = new List<string>();
        for (int i = 0; i < numProtons; i++) particlesToPlace.Add("P");
        for (int i = 0; i < numNeutrons; i++) particlesToPlace.Add("N");

        ShuffleList(particlesToPlace);

        float initialRadius = nucleonScale * Mathf.Pow(particlesToPlace.Count, 1f / 3f) * 0.5f;

        foreach (string type in particlesToPlace)
        {
            GameObject prefabToUse = (type == "P") ? protonPrefab : neutronPrefab;
            string nameID = (type == "P") ? "Protone" : "Neutrone";

            if (prefabToUse != null)
            {
                GameObject nucleon = Instantiate(prefabToUse, nucleusContainer);
                nucleon.name = nameID;
                
                nucleon.transform.localPosition = Random.insideUnitSphere * initialRadius;
                nucleon.transform.localScale = Vector3.one * nucleonScale;
                nucleon.transform.localRotation = Random.rotation;

                if (nucleon.GetComponent<Collider>() == null) nucleon.AddComponent<SphereCollider>();

                nucleonTransforms.Add(nucleon.transform);
            }
        }

        PerformNucleusPacking();
    }

    void PerformNucleusPacking()
    {
        int count = nucleonTransforms.Count;
        if (count == 0) return;

        float minDistance = nucleonScale; 
        float minDistanceSqr = minDistance * minDistance;

        for (int k = 0; k < packingIterations; k++)
        {
            Vector3[] displacements = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    Transform t1 = nucleonTransforms[i];
                    Transform t2 = nucleonTransforms[j];

                    Vector3 direction = t1.localPosition - t2.localPosition;
                    float distSqr = direction.sqrMagnitude;

                    if (distSqr < minDistanceSqr)
                    {
                        float dist = Mathf.Sqrt(distSqr);
                        if (dist < 0.0001f) 
                        {
                            direction = Random.onUnitSphere;
                            dist = 0.0001f;
                        }

                        float penetration = minDistance - dist;
                        Vector3 pushVector = direction.normalized * (penetration * repulsionForce);

                        displacements[i] += pushVector;
                        displacements[j] -= pushVector;
                    }
                }
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 currentPos = nucleonTransforms[i].localPosition;
                currentPos += displacements[i];
                currentPos = Vector3.Lerp(currentPos, Vector3.zero, centerAttraction);
                nucleonTransforms[i].localPosition = currentPos;
            }
        }
    }

    void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1) { n--; int k = Random.Range(0, n + 1); T value = list[k]; list[k] = list[n]; list[n] = value; }
    }

    void GenerateElectrons(int count)
    {
        int[] shellCapacity = { 2, 8, 8, 18, 18, 32, 32 };
        int electronsLeft = count;
        int shellIndex = 0;

        while (electronsLeft > 0 && shellIndex < shellCapacity.Length)
        {
            int electronsInThisShell = Mathf.Min(electronsLeft, shellCapacity[shellIndex]);

            GameObject shellPivot = new GameObject($"Guscio_{shellIndex}");
            shellPivot.transform.SetParent(orbitsContainer, false);
            shellList.Add(shellPivot.transform);

            CreateOrbitRing(shellIndex, shellPivot.transform);

            for (int i = 0; i < electronsInThisShell; i++)
            {
                float angle = i * (360f / electronsInThisShell);
                float baseOffset = 2.0f;
                float currentRadius = baseOffset + ((shellIndex + 1) * orbitSpacing);
                Vector3 position = GetOrbitPosition(angle, currentRadius);

                if (electronPrefab != null)
                {
                    GameObject electron = Instantiate(electronPrefab, shellPivot.transform);
                    electron.name = "Elettrone"; // Cruciale per i tooltip VR
                    electron.transform.localPosition = position;
                }
            }
            electronsLeft -= electronsInThisShell;
            shellIndex++;
        }
    }

    void CreateOrbitRing(int shellIndex, Transform parent)
    {
        if (orbitLinePrefab == null) return;
        GameObject ring = Instantiate(orbitLinePrefab, parent);
        float baseOffset = 2.0f;
        float radius = baseOffset + ((shellIndex + 1) * orbitSpacing);
        float finalScale = radius * baseOrbitScale;
        ring.transform.localScale = new Vector3(finalScale, finalScale, finalScale);
    }

    Vector3 GetOrbitPosition(float angleDegrees, float radius)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad) * radius, 0, Mathf.Sin(rad) * radius);
    }
}