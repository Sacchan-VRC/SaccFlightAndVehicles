
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
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
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
    }
    public void SFEXT_L_ECStart()
    {
        Dial_FunconNULL = Dial_Funcon == null;
        HudLimitNULL = HudLimit == null;
        if (!DefaultLimitsOn) { SetLimitsOff(); }
    }
    public void DFUNC_Selected()
    {
        gameObject.SetActive(true);
    }
    public void DFUNC_Deselected()
    {
        gameObject.SetActive(false);
        TriggerLastFrame = false;
    }
    private void SFEXT_O_PilotExit()
    {
        gameObject.SetActive(false);
        TriggerLastFrame = false;
    }
    public void SFEXT_G_Explode()
    {
        gameObject.SetActive(false);
        if (DefaultLimitsOn)
        {
            SetLimitsOn();
            //CANNOT recursively call a function
            //EngineControl.SendEventToExtensions("SFEXT_G_LimitsOn");
        }
        else
        {
            SetLimitsOff();
            //CANNOT recursively call a function
            //EngineControl.SendEventToExtensions("SFEXT_G_LimitsOff");
        }
    }
    public void SetLimitsOn()
    {
        EngineControl.FlightLimitsEnabled = true;
        if (!HudLimitNULL) { HudLimit.SetActive(true); }
        if (!Dial_FunconNULL) { Dial_Funcon.SetActive(true); }
        HudLimit.SetActive(true);
    }
    public void SetLimitsOff()
    {
        EngineControl.FlightLimitsEnabled = false;
        if (!HudLimitNULL) { HudLimit.SetActive(false); }
        if (!Dial_FunconNULL) { Dial_Funcon.SetActive(false); }
        HudLimit.SetActive(false);
    }
    public void SFEXT_O_PilotEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(EngineControl.FlightLimitsEnabled);
    }
    public void SFEXT_O_PassengerEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(EngineControl.FlightLimitsEnabled);
    }
    public void SFEXT_G_RespawnButton()
    {
        if (DefaultLimitsOn) { SetLimitsOn(); }
        else { SetLimitsOff(); }
    }
    public void SFEXT_O_PlayerJoined()
    {
        if (!EngineControl.FlightLimitsEnabled && DefaultLimitsOn)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetLimitsOff"); }
        else if (EngineControl.FlightLimitsEnabled && !DefaultLimitsOn)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetLimitsOn"); }
    }
    public void KeyboardInput()
    {
        ToggleLimits();
    }
    public void ToggleLimits()
    {
        if (!EngineControl.FlightLimitsEnabled)
        {
            if (EngineControl.VTOLAngle != EngineControl.VTOLDefaultValue) return;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetLimitsOn");
        }
        else
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetLimitsOff");
        }
    }
}
