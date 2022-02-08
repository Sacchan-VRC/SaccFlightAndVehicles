
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class DFUNC_ToggleEngine : UdonSharpBehaviour
{
    public UdonSharpBehaviour SAVControl;
    public float ToggleMinDelay = 0;
    public float StartUpTime = 3f;
    public AudioSource EngineStartupSound;
    private int EngineStartCount;
    private int EngineStartCancelCount;
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
            else if (EngineStartCount > EngineStartCancelCount)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EngineStartupCancel));
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
    public void EngineStartup()
    {
        if (EngineStartupSound) { EngineStartupSound.Play(); }
        EngineStartCount++;
        SendCustomEventDelayedSeconds(nameof(EngineStartupFinish), StartUpTime);
    }
    public void EngineStartupFinish()
    {
        EngineStartCount--;
        if (EngineStartCount == 0 && EngineStartCancelCount == 0)
        { SAVControl.SetProgramVariable("_EngineOn", true); }
        else
        { EngineStartCancelCount--; }
    }
    public void EngineStartupCancel()
    {
        if (EngineStartupSound) { EngineStartupSound.Stop(); }
        EngineStartCancelCount++;
    }
    public void EngineOn()
    {
        if (!(bool)SAVControl.GetProgramVariable("_EngineOn"))//don't bother setting if you're not a late joiner
        {
            ToggleTime = Time.time;
            if (StartUpTime == 0)
            {
                SAVControl.SetProgramVariable("_EngineOn", true);
            }
            else
            {
                EngineStartup();
            }
        }
    }
    public void EngineOff()
    {
        if ((bool)SAVControl.GetProgramVariable("_EngineOn"))
        {
            ToggleTime = Time.time - StartUpTime;
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
