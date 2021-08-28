
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Flaps : UdonSharpBehaviour
{
    [SerializeField] SaccAirVehicle SAVControl;
    [SerializeField] private Animator FlapsAnimator;
    [SerializeField] private GameObject Dial_Funcon;
    [SerializeField] private string AnimatorBool = "flaps";
    [SerializeField] private bool DefaultFlapsOff = false;
    [SerializeField] private float FlapsDragMulti = 1.4f;
    [SerializeField] private float FlapsLiftMulti = 1.35f;
    [SerializeField] private float FlapsExtraMaxLift = 0;
    private SaccEntity EntityControl;
    private bool UseLeftTrigger = false;
    private bool Flaps = false;
    private bool Dial_FunconNULL = true;
    private bool TriggerLastFrame;
    private int FLAPS_STRING;
    private bool DragApplied;
    private bool LiftApplied;
    private bool MaxLiftApplied;
    private bool InVR;
    private bool Selected;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void DFUNC_Selected()
    {
        gameObject.SetActive(true);
        Selected = true;
    }
    public void DFUNC_Deselected()
    {
        if (!Flaps) { gameObject.SetActive(false); }
        TriggerLastFrame = false;
        Selected = false;
    }
    public void SFEXT_L_EntityStart()
    {
        EntityControl = SAVControl.EntityControl;
        FLAPS_STRING = Animator.StringToHash(AnimatorBool);
        //to match how the old values worked
        FlapsDragMulti -= 1f;
        FlapsLiftMulti -= 1f;

        Dial_FunconNULL = Dial_Funcon == null;
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(Flaps);
        if (DefaultFlapsOff) { SetFlapsOff(); }
        else { SetFlapsOn(); }
    }
    public void SFEXT_O_PilotEnter()
    {
        if (Flaps)
        { gameObject.SetActive(true); }
        InVR = Networking.LocalPlayer.IsUserInVR();//move to start when they fix the bug
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(Flaps);
    }
    public void SFEXT_O_PilotExit()
    {
        DFUNC_Deselected();
    }
    public void SFEXT_O_PassengerEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(Flaps);
    }
    public void SFEXT_G_Explode()
    {
        if (DefaultFlapsOff)
        { SetFlapsOff(); }
        else
        { SetFlapsOn(); }
    }
    public void SFEXT_O_RespawnButton()
    {
        if (DefaultFlapsOff)
        { SetFlapsOff(); }
        else
        { SetFlapsOn(); }
    }
    private void Update()
    {
        if (Selected)
        {
            float Trigger;
            if (UseLeftTrigger)
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
            else
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
            if (Trigger > 0.75)
            {
                if (!TriggerLastFrame) { ToggleFlaps(); }
                TriggerLastFrame = true;
            }
            else { TriggerLastFrame = false; }
        }
        if (Flaps)
        {
            if (SAVControl.PitchDown)//flaps on, but plane's angle of attack is negative so they have no helpful effect
            {
                if (LiftApplied) { SAVControl.ExtraLift -= FlapsLiftMulti; LiftApplied = false; }
                if (MaxLiftApplied) { SAVControl.MaxLift -= FlapsExtraMaxLift; MaxLiftApplied = false; }
            }
            else//flaps on positive angle of attack, flaps are useful
            {
                if (!LiftApplied) { SAVControl.ExtraLift += FlapsLiftMulti; LiftApplied = true; }
                if (!MaxLiftApplied) { SAVControl.MaxLift += FlapsExtraMaxLift; MaxLiftApplied = true; }
            }
        }
    }
    public void KeyboardInput()
    {
        ToggleFlaps();
    }
    public void SetFlapsOff()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(false);
        Flaps = false;
        FlapsAnimator.SetBool(FLAPS_STRING, false);

        if (DragApplied) { SAVControl.ExtraDrag -= FlapsDragMulti; DragApplied = false; }
        if (LiftApplied) { SAVControl.ExtraLift -= FlapsLiftMulti; LiftApplied = false; }
        if (MaxLiftApplied) { SAVControl.MaxLift -= FlapsExtraMaxLift; MaxLiftApplied = false; }

        if (SAVControl.IsOwner)
        {
            gameObject.SetActive(false);//for desktop Users
            EntityControl.SendEventToExtensions("SFEXT_O_FlapsOff");
        }
    }
    public void SetFlapsOn()
    {
        Flaps = true;
        FlapsAnimator.SetBool(FLAPS_STRING, true);
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(true);

        if (!DragApplied) { SAVControl.ExtraDrag += FlapsDragMulti; DragApplied = true; }
        if (!LiftApplied) { SAVControl.ExtraLift += FlapsLiftMulti; LiftApplied = true; }
        if (!MaxLiftApplied) { SAVControl.MaxLift += FlapsExtraMaxLift; MaxLiftApplied = true; }

        if (SAVControl.IsOwner)
        {
            gameObject.SetActive(true);//for desktop Users
            EntityControl.SendEventToExtensions("SFEXT_O_FlapsOn");
        }
    }
    public void SFEXT_O_LoseOwnership()
    { gameObject.SetActive(false); }
    public void ToggleFlaps()
    {
        if (!Flaps)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetFlapsOn");
        }
        else
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetFlapsOff");
        }
    }
    public void SFEXT_O_PlayerJoined()
    {
        if (!Flaps && !DefaultFlapsOff)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetFlapsOff");
        }

        else if (Flaps && DefaultFlapsOff)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetFlapsOn");
        }
    }
}
