
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Reverse : UdonSharpBehaviour
{
    [SerializeField] private UdonSharpBehaviour SAVControl;
    [SerializeField] private float ReversingThrottleMultiplier = -.5f;
    [SerializeField] private GameObject Dial_funcon;
    private float StartThrottleStrength;
    private float StartABStrength;
    private float ReversingThrottleStrength;
    private float ReversingABStrength;
    private bool UseLeftTrigger = false;
    private bool TriggerLastFrame;
    private bool Reversing;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXT_L_EntityStart()
    {
        StartThrottleStrength = (float)SAVControl.GetProgramVariable("ThrottleStrength");
        StartABStrength = (float)SAVControl.GetProgramVariable("ThrottleStrengthAB");
        ReversingThrottleStrength = StartThrottleStrength * ReversingThrottleMultiplier;
        ReversingABStrength = StartABStrength * ReversingThrottleMultiplier;
    }
    public void DFUNC_Selected()
    {
        TriggerLastFrame = true;
        gameObject.SetActive(true);
    }
    public void DFUNC_Deselected()
    {
        gameObject.SetActive(false);
    }
    public void SFEXT_O_PilotEnter()
    {
        if (Dial_funcon) { Dial_funcon.SetActive(Reversing); }
    }
    public void SFEXT_O_PilotExit()
    {
        if (Reversing)
        { SetNotReversing(); }
        gameObject.SetActive(false);
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
            if (!TriggerLastFrame) { ToggleReverse(); }
            TriggerLastFrame = true;
        }
        else { TriggerLastFrame = false; }
    }
    private void ToggleReverse()
    {
        if (!Reversing)
        { SetReversing(); }
        else
        { SetNotReversing(); }
    }
    private void SetReversing()
    {
        Reversing = true;
        SAVControl.SetProgramVariable("ThrottleStrength", ReversingThrottleStrength);
        SAVControl.SetProgramVariable("ThrottleStrengthAB", ReversingABStrength);
        if (Dial_funcon) { Dial_funcon.SetActive(true); }
    }
    private void SetNotReversing()
    {
        Reversing = false;
        SAVControl.SetProgramVariable("ThrottleStrength", StartThrottleStrength);
        SAVControl.SetProgramVariable("ThrottleStrengthAB", StartABStrength);
        if (Dial_funcon) { Dial_funcon.SetActive(false); }
    }
    public void KeyboardInput()
    {
        ToggleReverse();
    }
}
