
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Canopy : UdonSharpBehaviour
{
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private UdonSharpBehaviour SoundControl;
    [SerializeField] private GameObject Dial_Funcon;
    [SerializeField] private Animator CanopyAnimator;
    [SerializeField] private float CanopyCloseTime = 1.8f;
    [SerializeField] private bool CanopyCanComeOff = true;
    [Header("Meters/s")]
    [SerializeField] private float CanopyBreakSpeed = 50;
    [SerializeField] private float CanopyAutoCloseSpeed = 20;
    [SerializeField] private float CanopyDragMulti = 1.2f;
    private bool UseLeftTrigger = false;
    private bool Dial_FunconNULL = true;
    private bool TriggerLastFrame;
    private Transform VehicleTransform;
    private VRCPlayerApi localPlayer;
    private HUDController HUDControl;
    private bool InVR;
    private int CANOPYOPEN_STRING = Animator.StringToHash("canopyopen");
    private int CANOPYBREAK_STRING = Animator.StringToHash("canopybroken");
    private bool CanopyOpen;
    private bool CanopyBroken;
    private bool Selected;
    private float LastCanopyToggleTime = -999;
    private bool DragApplied;
    private bool CanopyTransitioning = false;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXT_L_ECStart()
    {
        localPlayer = Networking.LocalPlayer;
        VehicleTransform = EngineControl.VehicleMainObj.GetComponent<Transform>();
        Dial_FunconNULL = Dial_Funcon == null;
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(CanopyOpen);

        CanopyOpening();
    }
    public void DFUNC_Selected()
    {
        Selected = true;
    }
    public void DFUNC_Deselected()
    {
        Selected = false;
        TriggerLastFrame = false;
    }
    public void SFEXT_O_PilotEnter()
    {
        gameObject.SetActive(true);
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(CanopyOpen);
        InVR = EngineControl.InVR;
    }
    public void SFEXT_O_PilotExit()
    {
        gameObject.SetActive(false);
        Selected = false;
        TriggerLastFrame = false;
    }
    public void SFEXT_P_PassengerEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(CanopyOpen);
    }
    public void SFEXT_G_Explode()
    {
        RepairCanopy();
        if (!CanopyOpen) CanopyOpening();
    }
    public void SFEXT_O_PlayerJoined()
    {
        if (CanopyBroken)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CanopyBreakOff"); }
        else if (!CanopyOpen)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CanopyClosing"); }
    }
    public void SFEXT_O_RespawnButton()
    {
        CanopyOpening();
    }
    public void SFEXT_G_RespawnButton()
    {
        CanopyBroken = false;
        CanopyAnimator.SetBool(CANOPYBREAK_STRING, false);
        if (!CanopyOpen) CanopyOpening();
    }
    public void SFEXT_G_ReSupply()
    {
        if (EngineControl.Health == EngineControl.FullHealth)
        {
            if (CanopyBroken)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RepairCanopy");
            }
        }
    }
    public void RepairCanopy()
    {
        CanopyBroken = false;
        CanopyAnimator.SetBool(CANOPYBREAK_STRING, false);
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
                if (!TriggerLastFrame)
                {
                    ToggleCanopy();
                }
                TriggerLastFrame = true;
            }
            else
            {
                TriggerLastFrame = false;
            }
        }

        if (!CanopyBroken && CanopyOpen && !EngineControl.dead)
        {
            if (CanopyCanComeOff && EngineControl.AirSpeed > 100)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CanopyBreakOff");
            }
            else if (EngineControl.AirSpeed > CanopyAutoCloseSpeed && (Time.time - LastCanopyToggleTime) > CanopyCloseTime + .1f)//.1f is extra delay to match the animator because it's using write defaults off
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CanopyClosing");
            }
        }
    }
    public void CanopyOpening()
    {
        if (CanopyOpen || CanopyBroken) { return; }
        LastCanopyToggleTime = Time.time;
        if (!Dial_FunconNULL) { Dial_Funcon.SetActive(true); }
        CanopyOpen = true;
        CanopyAnimator.SetBool(CANOPYOPEN_STRING, true);
        SoundControl.SendCustomEvent("DoorOpen");
        if (EngineControl.IsOwner)
        {
            EngineControl.SendEventToExtensions("SFEXT_O_CanopyOpened");
        }

        if (!DragApplied) { EngineControl.ExtraDrag += CanopyDragMulti; DragApplied = true; }
    }
    public void CanopyClosing()
    {
        if (!CanopyOpen || CanopyBroken) { return; }//don't bother when not necessary (OnPlayerJoined() wasn't you)
        LastCanopyToggleTime = Time.time;
        if (!Dial_FunconNULL) { Dial_Funcon.SetActive(false); }
        CanopyOpen = false;
        CanopyAnimator.SetBool(CANOPYOPEN_STRING, false);
        CanopyTransitioning = true;
        SoundControl.SendCustomEventDelayedSeconds("DoorClose", CanopyCloseTime);
        SendCustomEventDelayedSeconds("SetCanopyTransitioningFalse", CanopyCloseTime);
        if (EngineControl.IsOwner)
        {
            EngineControl.SendEventToExtensions("SFEXT_O_CanopyClosed");
        }
        if (DragApplied) { EngineControl.ExtraDrag -= CanopyDragMulti; DragApplied = false; }
    }
    public void SetCanopyTransitioningFalse()
    {
        CanopyTransitioning = false;
    }
    public void CanopyBreakOff()
    {
        if (CanopyBroken) { return; }
        Dial_Funcon.SetActive(true);
        CanopyOpen = true;
        CanopyBroken = true;
        CanopyAnimator.SetBool(CANOPYBREAK_STRING, true);
        if (EngineControl.IsOwner)
        {
            EngineControl.SendEventToExtensions("SFEXT_O_CanopyBreak");
        }
        if (!DragApplied) { EngineControl.ExtraDrag += CanopyDragMulti; DragApplied = true; }
    }
    public void ToggleCanopy()
    {
        if ((Time.time - LastCanopyToggleTime) > CanopyCloseTime + .1f && !CanopyBroken && !CanopyTransitioning && !(!CanopyCanComeOff && EngineControl.Speed > CanopyAutoCloseSpeed))
        {
            if (CanopyOpen)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CanopyClosing");
            }
            else
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CanopyOpening");
            }
        }
    }
    public void KeyboardInput()
    {
        ToggleCanopy();
    }
}
