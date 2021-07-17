
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Limits : UdonSharpBehaviour
{
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private GameObject Dial_Funcon;
    private bool Dial_FunconNULL = true;
    [SerializeField] private bool UseLeftTrigger;
    private bool TriggerLastFrame;
    private EffectsController EffectsControl;
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
                EngineControl.ToggleLimits();
            }
            TriggerLastFrame = true;
        }
        else { TriggerLastFrame = false; }
    }
    public void DFUNC_Selected()
    {
        gameObject.SetActive(true);
    }
    public void DFUNC_Deselected()
    {
        gameObject.SetActive(false);
    }
    private void SFEXT_O_PilotExit()
    {
        gameObject.SetActive(false);
    }
    public void SFEXT_L_ECStart()
    {
        EffectsControl = EngineControl.EffectsControl;
        Dial_FunconNULL = Dial_Funcon == null;
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(EngineControl.FlightLimitsEnabled);
    }
    public void SFEXT_O_PilotEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(EngineControl.FlightLimitsEnabled);
    }
    public void SFEXT_O_PassengerEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(EngineControl.FlightLimitsEnabled);
    }
    public void KeyboardInput()
    {
        EngineControl.ToggleLimits();
    }
    public void SFEXT_O_LimitsOn()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(true);
    }
    public void SFEXT_O_LimitsOff()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(false);
    }
}
