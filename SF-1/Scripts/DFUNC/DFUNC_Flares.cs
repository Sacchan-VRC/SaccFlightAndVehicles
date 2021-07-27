
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Flares : UdonSharpBehaviour
{
    [SerializeField] private bool UseLeftTrigger;
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private Animator FlaresAnimator;
    private bool TriggerLastFrame;
    private int FLARES_STRING = Animator.StringToHash("flares");
    public void DFUNC_Selected()
    {
        gameObject.SetActive(true);
    }
    public void DFUNC_Deselected()
    {
        gameObject.SetActive(false);
    }
    public void SFEXT_O_PilotExit()
    {
        gameObject.SetActive(false);
    }
    private void Update()
    {
        float Trigger;
        if (UseLeftTrigger)
        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
        else
        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

        if (Trigger > 0.75)
        {
            if (!TriggerLastFrame)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchFlares");
            }
            TriggerLastFrame = true;
        }
        else { TriggerLastFrame = false; }
    }
    public void LaunchFlares()
    {
        FlaresAnimator.SetTrigger(FLARES_STRING);
    }
    public void KeyboardInput()
    {
        FlaresAnimator.SetTrigger(FLARES_STRING);
    }
}
