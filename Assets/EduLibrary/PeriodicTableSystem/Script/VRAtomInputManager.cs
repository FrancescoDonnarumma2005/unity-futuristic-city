using UnityEngine;
using UnityEngine.InputSystem;

public class VRAtomInputManager : MonoBehaviour
{
    [Header("Input VR")]
    [Tooltip("Assegna qui l'azione del Tasto A (es. XRI RightHand/PrimaryButton)")]
    public InputActionReference animateButtonAction;

    private void Update()
    {
        // Se il tasto viene premuto in questo frame...
        if (animateButtonAction != null && animateButtonAction.action.WasPressedThisFrame())
        {
            // ...cerca l'atomo generato nella scena (metodo ottimizzato per Unity 6)
            ProceduralAtomRenderer currentAtom = Object.FindFirstObjectByType<ProceduralAtomRenderer>();
            
            if (currentAtom != null)
            {
                currentAtom.TriggerAnimation();
            }
        }
    }
}