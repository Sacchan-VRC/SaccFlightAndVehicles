
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
    public AudioSource EngineTurnOffSound;
    public GameObject Dial_Funcon;
    [Space(10)]
    [Tooltip("AnimEngineStartupAnimBool is true when engine is starting, and remains true until engine is turned off")]
    public bool DoEngineStartupAnimBool;
    [Header("Only required if above is ticked")]
    public Animator EngineAnimator;
    public string AnimEngineStartupAnimBool = "EngineStarting";
    private SaccEntity EntityControl;
    private int EngineStartCount;
    private int EngineStartCancelCount;
    private bool UseLeftTrigger = false;
    private bool TriggerLastFrame;
    private float ToggleTime;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXT_L_EntityStart()
    {
        EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
        if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
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
                if (StartUpTime == 0)
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EngineOn)); }
                else
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EngineStartup)); }
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
        EngineStartCount++;
        ToggleTime = Time.time;
        if (EngineStartupSound) { EngineStartupSound.Play(); }
        if (Dial_Funcon) { Dial_Funcon.SetActive(true); }
        SendCustomEventDelayedSeconds(nameof(EngineStartupFinish), StartUpTime);
        if (DoEngineStartupAnimBool)
        { EngineAnimator.SetBool(AnimEngineStartupAnimBool, true); }
        EntityControl.SendEventToExtensions("SFEXT_G_EngineStartup");
    }
    public void EngineStartupFinish()
    {
        if (EngineStartCount > 0) { EngineStartCount--; }
        if (EngineStartCount == 0 && EngineStartCancelCount == 0)
        {
            if (!EntityControl._dead)
            {
                if (EntityControl.IsOwner)
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(JustEngineOn)); }
            }
        }
        if (EngineStartCancelCount > 0)
        { EngineStartCancelCount--; }
    }
    public void EngineStartupCancel()
    {
        EngineStartCancelCount++;
        if (EngineStartupSound) { EngineStartupSound.Stop(); }
        if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
        if (DoEngineStartupAnimBool)
        { EngineAnimator.SetBool(AnimEngineStartupAnimBool, false); }
        EntityControl.SendEventToExtensions("SFEXT_G_EngineStartupCancel");
    }
    public void SFEXT_G_ReAppear()
    {
        EngineOff();
        ResetStartup();
    }
    public void SFEXT_G_RespawnButton()
    {
        ResetStartup();
    }
    public void ResetStartup()
    {
        if (EngineStartCount > 0 && EngineStartCount != EngineStartCancelCount)
        {
            EngineStartCancelCount = EngineStartCount;
        }
        if (EngineStartupSound && EngineStartupSound.isPlaying) { EngineStartupSound.Stop(); }
        if (EngineTurnOffSound && EngineTurnOffSound.isPlaying) { EngineTurnOffSound.Stop(); }
    }
    public void EngineOn()
    {
        ToggleTime = Time.time;
        SAVControl.SetProgramVariable("_EngineOn", true);
        if (DoEngineStartupAnimBool)
        { EngineAnimator.SetBool(AnimEngineStartupAnimBool, true); }
    }
    public void JustEngineOn()
    {
        SAVControl.SetProgramVariable("_EngineOn", true);
    }
    public void EngineOff()
    {
        if ((bool)SAVControl.GetProgramVariable("_EngineOn"))
        {
            ToggleTime = Time.time - StartUpTime;
            SAVControl.SetProgramVariable("_EngineOn", false);
            if (EngineTurnOffSound) { EngineTurnOffSound.Play(); }
        }
    }
    public void SFEXT_G_EngineOff()
    {
        if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
        if (DoEngineStartupAnimBool)
        { EngineAnimator.SetBool(AnimEngineStartupAnimBool, false); }
    }
    public void SFEXT_G_EngineOn()
    {
        if (Dial_Funcon) { Dial_Funcon.SetActive(true); }
        if (DoEngineStartupAnimBool)
        { EngineAnimator.SetBool(AnimEngineStartupAnimBool, true); }
    }
    public void KeyboardInput()
    {
        ToggleEngine();
    }
}
