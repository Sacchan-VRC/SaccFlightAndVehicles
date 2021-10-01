
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class DFUNC_Limits : UdonSharpBehaviour
{
    [SerializeField] private UdonSharpBehaviour SAVControl;
    [SerializeField] private GameObject HudLimit;
    [SerializeField] private bool DefaultLimitsOn = true;
    [Tooltip("Object enabled when function is active (used on MFD)")]
    [SerializeField] private GameObject Dial_Funcon;
    [Tooltip("Try to stop pilot pulling this many Gs")]
    [SerializeField] private float GLimiter = 12f;
    [Tooltip("Try to stop pilot pulling this much AoA")]
    [SerializeField] private float AoALimiter = 15f;
    private SaccEntity EntityControl;
    private bool UseLeftTrigger = false;
    private bool TriggerLastFrame;
    private bool InVR;
    private bool Piloting;
    [System.NonSerializedAttribute] public bool FlightLimitsEnabled = false;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXT_L_EntityStart()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (localPlayer != null)
        { InVR = localPlayer.IsUserInVR(); }
        EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
        if (!DefaultLimitsOn) { SetLimitsOff(); }
    }
    public void DFUNC_Selected()
    {
        TriggerLastFrame = true;//To prevent function enabling if you hold the trigger when selecting it
        gameObject.SetActive(true);
    }
    public void DFUNC_Deselected()
    {
        if (!FlightLimitsEnabled) { gameObject.SetActive(false); }
        TriggerLastFrame = false;
    }
    public void SFEXT_O_PilotEnter()
    {
        if (FlightLimitsEnabled) { gameObject.SetActive(true); }
        Piloting = true;
        if (Dial_Funcon) Dial_Funcon.SetActive(FlightLimitsEnabled);
        if (FlightLimitsEnabled)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetLimitsOn)); }
    }
    public void SFEXT_O_PilotExit()
    {
        gameObject.SetActive(false);
        Piloting = false;
    }
    public void SFEXT_G_TouchDown()
    {
        if (FlightLimitsEnabled)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetLimitsOff)); }
    }
    public void SFEXT_O_EnterVTOL()
    {
        if (FlightLimitsEnabled)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetLimitsOff)); }
    }
    public void SFEXT_G_Explode()
    {
        gameObject.SetActive(false);
        if (DefaultLimitsOn)
        {
            SetLimitsOn();
            EntityControl.SendEventToExtensions("SFEXT_G_LimitsOn");
        }
        else
        {
            SetLimitsOff();
            EntityControl.SendEventToExtensions("SFEXT_G_LimitsOff");
        }
    }
    public void SetLimitsOn()
    {
        if (FlightLimitsEnabled) { return; }
        if (Piloting) { gameObject.SetActive(true); }
        FlightLimitsEnabled = true;
        if (HudLimit) { HudLimit.SetActive(true); }
        if (Dial_Funcon) { Dial_Funcon.SetActive(true); }
        HudLimit.SetActive(true);
    }
    public void SetLimitsOff()
    {
        if (!FlightLimitsEnabled) { return; }
        if (Piloting) { gameObject.SetActive(false); }
        FlightLimitsEnabled = false;
        if (HudLimit) { HudLimit.SetActive(false); }
        if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
        HudLimit.SetActive(false);
        SAVControl.SetProgramVariable("Limits", 1f);
    }
    public void SFEXT_O_PassengerEnter()
    {
        if (Dial_Funcon) Dial_Funcon.SetActive(FlightLimitsEnabled);
    }
    public void SFEXT_G_RespawnButton()
    {
        if (DefaultLimitsOn) { SetLimitsOn(); }
        else { SetLimitsOff(); }
    }
    public void SFEXT_O_OnPlayerJoined()
    {
        if (!FlightLimitsEnabled && DefaultLimitsOn)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetLimitsOff"); }
        else if (FlightLimitsEnabled && !DefaultLimitsOn)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetLimitsOn"); }
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
                ToggleLimits();
            }
            TriggerLastFrame = true;
        }
        else { TriggerLastFrame = false; }

        if (FlightLimitsEnabled && Piloting)
        {
            float GLimitStrength = Mathf.Clamp(-((float)SAVControl.GetProgramVariable("VertGs") / GLimiter) + 1, 0, 1);
            float AoALimitStrength = Mathf.Clamp(-(Mathf.Abs((float)SAVControl.GetProgramVariable("AngleOfAttack")) / AoALimiter) + 1, 0, 1);
            SAVControl.SetProgramVariable("Limits", Mathf.Min(GLimitStrength, AoALimitStrength));
        }
    }
    public void KeyboardInput()
    {
        ToggleLimits();
    }
    public void ToggleLimits()
    {
        if (!FlightLimitsEnabled)
        {
            if ((float)SAVControl.GetProgramVariable("VTOLAngle") != (float)SAVControl.GetProgramVariable("VTOLDefaultValue")) { return; }
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetLimitsOn");
        }
        else
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetLimitsOff");
        }
    }
}
