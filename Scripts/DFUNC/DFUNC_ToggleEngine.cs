
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class DFUNC_ToggleEngine : UdonSharpBehaviour
{
    public UdonSharpBehaviour SAVControl;
    public float ToggleMinDelay = 3f;
    private bool UseLeftTrigger = false;
    private bool TriggerLastFrame;
    private float ToggleTime;
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
                ToggleEngine();
            }
            TriggerLastFrame = true;
        }
        else { TriggerLastFrame = false; }
    }
    public void ToggleEngine()
    {
        if (Time.time - ToggleTime > ToggleMinDelay)
        {
            if ((bool)SAVControl.GetProgramVariable("_EngineOn"))
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EngineOff));
            }
            else
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EngineOn));
            }
        }
    }
    public void DFUNC_Selected()
    { gameObject.SetActive(true); }
    public void DFUNC_Deselected()
    { gameObject.SetActive(false); }
    public void SFEXT_O_PilotExit()
    { gameObject.SetActive(false); }
    public void EngineOn()
    {
        if (!(bool)SAVControl.GetProgramVariable("_EngineOn"))//don't bother setting if you're not a late joiner
        {
            ToggleTime = Time.time;
            SAVControl.SetProgramVariable("_EngineOn", true);
        }
    }
    public void EngineOff()
    {
        if ((bool)SAVControl.GetProgramVariable("_EngineOn"))
        {
            ToggleTime = Time.time;
            SAVControl.SetProgramVariable("_EngineOn", false);
        }
    }
    public void KeyboardInput()
    {
        ToggleEngine();
    }
    public void SFEXT_G_Explode()
    {
        EngineOff();
    }
}
