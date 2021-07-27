
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Gear : UdonSharpBehaviour
{
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private bool UseLeftTrigger;
    [SerializeField] private GameObject Dial_Funcon;
    private bool Dial_FunconNULL = true;
    private bool RTriggerLastFrame;
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
            if (!RTriggerLastFrame && !EngineControl.GearToggleDisabled) { EngineControl.ToggleGear(); }
            RTriggerLastFrame = true;
        }
        else { RTriggerLastFrame = false; }
    }
    public void DFUNC_Selected()
    {
        gameObject.SetActive(true);
    }
    public void DFUNC_Deselected()
    {
        gameObject.SetActive(false);
    }
    public void SFEXT_L_ECStart()
    {
        EffectsControl = EngineControl.EffectsControl;
        Dial_FunconNULL = Dial_Funcon == null;
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(!EffectsControl.HookDown);
    }
    public void SFEXT_O_PilotEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(!EffectsControl.GearUp);
    }
    public void SFEXT_O_PilotExit()
    {
        gameObject.SetActive(false);
    }
    public void SFEXT_O_PassengerEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(!EffectsControl.GearUp);
    }
    public void KeyboardInput()
    {
        EngineControl.ToggleGear();
    }
    public void SFEXT_O_GearDown()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(true);
    }
    public void SFEXT_O_GearUp()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(false);
    }
}
