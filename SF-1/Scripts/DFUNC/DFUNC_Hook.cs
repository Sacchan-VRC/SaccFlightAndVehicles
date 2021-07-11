
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Hook : UdonSharpBehaviour
{
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private GameObject Dial_Funcon;
    private bool Dial_FunconNULL = true;
    [SerializeField] private bool UseLeftTrigger;
    private bool RTriggerLastFrame;
    private void Update()
    {
        float Trigger;
        if (UseLeftTrigger)
        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
        else
        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

        if (Trigger > 0.75)
        {
            if (!RTriggerLastFrame)
            {
                EngineControl.ToggleHook();
                EngineControl.Hooked = false;
            }
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
        Dial_FunconNULL = Dial_Funcon == null;
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(EngineControl.EffectsControl.HookDown);
    }
    public void SFEXT_O_PilotEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(EngineControl.EffectsControl.HookDown);
    }
    public void SFEXT_O_PassengerEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(EngineControl.EffectsControl.HookDown);
    }
    public void KeyboardInput()
    {
        EngineControl.ToggleHook();
    }
    public void SFEXT_O_HookDown()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(true);
    }
    public void SFEXT_O_HookUp()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(false);
    }
}
