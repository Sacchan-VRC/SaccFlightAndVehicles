
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_AltHold : UdonSharpBehaviour
{
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private GameObject HudHold;
    [SerializeField] private GameObject Dial_Funcon;
    private bool UseLeftTrigger = false;
    private bool Dial_FunconNULL = true;
    private bool TriggerLastFrame;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
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
    public void SFEXT_L_ECStart()
    {
        Dial_FunconNULL = Dial_Funcon == null;
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(false);
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
                bool althold = EngineControl.AltHold;
                EngineControl.AltHold = !althold;
                if (!Dial_FunconNULL) { Dial_Funcon.SetActive(althold); }
                HudHold.SetActive(althold);
            }
            TriggerLastFrame = true;
        }
        else { TriggerLastFrame = false; }
    }
    public void KeyboardInput()
    {
        bool althold = EngineControl.AltHold;
        EngineControl.AltHold = !althold;
        if (!Dial_FunconNULL) { Dial_Funcon.SetActive(althold); }
        HudHold.SetActive(althold);
    }
}