
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Reverse : UdonSharpBehaviour
{
    [SerializeField] private UdonSharpBehaviour SAVControl;
    [SerializeField] private float ReversingThrottleStrength = -1.25f;
    [SerializeField] private GameObject Dial_funcon;
    private bool UseLeftTrigger = false;
    private bool TriggerLastFrame;
    private float StartThrottleStrength;
    private bool Reversing;
    void Start()
    {
        StartThrottleStrength = (float)SAVControl.GetProgramVariable("ThrottleStrength");
    }
    public void SFEXT_O_PilotExit()
    {
        if (Reversing)
        { SetNotReversing(); }
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
        if (Dial_funcon) { Dial_funcon.SetActive(true); }
    }
    private void SetNotReversing()
    {
        Reversing = false;
        SAVControl.SetProgramVariable("ThrottleStrength", StartThrottleStrength);
        if (Dial_funcon) { Dial_funcon.SetActive(false); }
    }
    public void KeyboardInput()
    {
        ToggleReverse();
    }
}
