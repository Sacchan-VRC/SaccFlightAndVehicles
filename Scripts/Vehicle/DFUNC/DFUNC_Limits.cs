
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Limits : UdonSharpBehaviour
{
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private GameObject HudLimit;
    [SerializeField] private bool DefaultLimitsOn = true;
    [SerializeField] private GameObject Dial_Funcon;
    private bool UseLeftTrigger = false;
    private bool Dial_FunconNULL = true;
    private bool HudLimitNULL = true;
    private bool TriggerLastFrame;
    private bool Selected;
    private bool InVR;
    private bool Piloting;
    [System.NonSerializedAttribute] public bool FlightLimitsEnabled = false;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXT_L_ECStart()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (localPlayer != null)
        { InVR = localPlayer.IsUserInVR(); }
        Dial_FunconNULL = Dial_Funcon == null;
        HudLimitNULL = HudLimit == null;
        if (!DefaultLimitsOn) { SetLimitsOff(); }
    }
    public void DFUNC_Selected()
    {
        gameObject.SetActive(true);
        Selected = true;
    }
    public void DFUNC_Deselected()
    {
        if (!FlightLimitsEnabled) { gameObject.SetActive(false); }
        TriggerLastFrame = false;
        Selected = false;
    }
    public void SFEXT_O_PilotEnter()
    {
        gameObject.SetActive(false);
        Piloting = true;
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(FlightLimitsEnabled);
        if (FlightLimitsEnabled)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetLimitsOn)); }
    }
    public void SFEXT_O_PilotExit()
    {
        gameObject.SetActive(false);
        Selected = false;
        Piloting = false;
    }
    public void SFEXT_G_TouchDown()
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
            SendCustomEventDelayedFrames(nameof(SendLimitsOn), 1);
        }
        else
        {
            SetLimitsOff();
            SendCustomEventDelayedFrames(nameof(SendLimitsOff), 1);
        }
    }
    //these events have to be used with a frame delay because if you call them from an event that was called by the same SendEventToExtensions function, the previous call stops.
    public void SendLimitsOn()
    {
        EngineControl.SendEventToExtensions("SFEXT_G_LimitsOn");
    }
    public void SendLimitsOff()
    {
        EngineControl.SendEventToExtensions("SFEXT_G_LimitsOff");
    }
    public void SetLimitsOn()
    {
        if (FlightLimitsEnabled) { return; }
        if (Piloting) { gameObject.SetActive(true); }
        FlightLimitsEnabled = true;
        if (!HudLimitNULL) { HudLimit.SetActive(true); }
        if (!Dial_FunconNULL) { Dial_Funcon.SetActive(true); }
        HudLimit.SetActive(true);
    }
    public void SetLimitsOff()
    {
        if (!FlightLimitsEnabled) { return; }
        if (Piloting) { gameObject.SetActive(false); }
        FlightLimitsEnabled = false;
        if (!HudLimitNULL) { HudLimit.SetActive(false); }
        if (!Dial_FunconNULL) { Dial_Funcon.SetActive(false); }
        HudLimit.SetActive(false);
        EngineControl.Limits = 1;
    }
    public void SFEXT_O_PassengerEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(FlightLimitsEnabled);
    }
    public void SFEXT_G_RespawnButton()
    {
        if (DefaultLimitsOn) { SetLimitsOn(); }
        else { SetLimitsOff(); }
    }
    public void SFEXT_O_PlayerJoined()
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
            float GLimitStrength = Mathf.Clamp(-(EngineControl.VertGs / EngineControl.GLimiter) + 1, 0, 1);
            float AoALimitStrength = Mathf.Clamp(-(Mathf.Abs(EngineControl.AngleOfAttack) / EngineControl.AoALimiter) + 1, 0, 1);
            EngineControl.Limits = Mathf.Min(GLimitStrength, AoALimitStrength);
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
            if (EngineControl.VTOLAngle != EngineControl.VTOLDefaultValue) { return; }
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetLimitsOn");
        }
        else
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetLimitsOff");
        }
    }
}
