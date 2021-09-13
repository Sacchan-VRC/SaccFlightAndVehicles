
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class DFUNC_Canopy : UdonSharpBehaviour
{
    [SerializeField] private UdonSharpBehaviour SAVControl;
    [SerializeField] private UdonSharpBehaviour SoundControl;
    [Tooltip("Object enabled when function is active (used on MFD)")]
    [SerializeField] private GameObject Dial_Funcon;
    [SerializeField] private Animator CanopyAnimator;
    [Tooltip("The length of the canopy close animation, or how long to wait before telling the sound controller to change the sounds to inside vehicle sounds when closing")]
    [SerializeField] private float CanopyCloseTime = 1.8f;
    [Tooltip("The canopy can break off? Requires animation setup")]
    [SerializeField] private bool CanopyCanComeOff = true;
    [Header("Meters/s")]
    [Tooltip("Speed at which canopy will break off if it's still open")]
    [SerializeField] private float CanopyBreakSpeed = 50;
    [Tooltip("Speed at which canopy will close itself (useful for noobs/lazy people)")]
    [SerializeField] private float CanopyAutoCloseSpeed = 20;
    [Tooltip("Extra drag applied to vehicle while canopy is open")]
    [SerializeField] private float CanopyDragMulti = 1.2f;
    private SaccEntity EntityControl;
    private bool UseLeftTrigger = false;
    private bool Dial_FunconNULL = true;
    private bool TriggerLastFrame;
    private Transform VehicleTransform;
    private VRCPlayerApi localPlayer;
    private SAV_HUDController HUDControl;
    private bool InVR;
    private int CANOPYOPEN_STRING = Animator.StringToHash("canopyopen");
    private int CANOPYBREAK_STRING = Animator.StringToHash("canopybroken");
    private bool CanopyOpen;
    private bool CanopyBroken;
    private bool Selected;
    private float LastCanopyToggleTime = -999;
    private bool DragApplied;
    private bool CanopyTransitioning = false;
    private bool InEditor = true;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXT_L_EntityStart()
    {
        localPlayer = Networking.LocalPlayer;
        InEditor = localPlayer == null;
        EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
        VehicleTransform = EntityControl.transform;
        Dial_FunconNULL = Dial_Funcon == null;
        //crashes if not sent delayed because the order of events sent by SendCustomEvent are not maintained, (SaccEntity.SendEventToExtensions())
        SendCustomEventDelayedFrames(nameof(CanopyOpening), 1);
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
        if (!Dial_FunconNULL) { Dial_Funcon.SetActive(CanopyOpen); }
        if (!InEditor) { InVR = localPlayer.IsUserInVR(); }
    }
    public void SFEXT_O_PilotExit()
    {
        gameObject.SetActive(false);
        Selected = false;
        TriggerLastFrame = false;
    }
    public void SFEXT_P_PassengerEnter()
    {
        if (!Dial_FunconNULL) { Dial_Funcon.SetActive(CanopyOpen); }
    }
    public void SFEXT_G_Explode()
    {
        RepairCanopy();
        if (!CanopyOpen) { CanopyOpening(); }
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
        if ((float)SAVControl.GetProgramVariable("Health") == (float)SAVControl.GetProgramVariable("FullHealth"))
        {
            if (CanopyBroken)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(RepairCanopy));
            }
        }
    }
    public void RepairCanopy()
    {
        CanopyBroken = false;
        CanopyAnimator.SetBool(CANOPYBREAK_STRING, false);
        if ((bool)SAVControl.GetProgramVariable("IsOwner")) { SendCustomEventDelayedFrames(nameof(SendCanopyRepair), 1); }
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

        if (!CanopyBroken && CanopyOpen && !EntityControl.dead)
        {
            if (CanopyCanComeOff && (float)SAVControl.GetProgramVariable("AirSpeed") > 100)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CanopyBreakOff");
            }
            else if ((float)SAVControl.GetProgramVariable("AirSpeed") > CanopyAutoCloseSpeed && (Time.time - LastCanopyToggleTime) > CanopyCloseTime + .1f)//.1f is extra delay to match the animator because it's using write defaults off
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
        if ((bool)SAVControl.GetProgramVariable("IsOwner"))
        {
            SendCustomEventDelayedFrames(nameof(SendCanopyOpened), 1);
        }

        if (!DragApplied)
        {
            SAVControl.SetProgramVariable("ExtraDrag", (float)SAVControl.GetProgramVariable("ExtraDrag") + CanopyDragMulti);
            DragApplied = true;
        }
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
        if ((bool)SAVControl.GetProgramVariable("IsOwner"))
        {
            SendCustomEventDelayedFrames(nameof(SendCanopyClosed), 1);
        }
        if (DragApplied)
        {
            SAVControl.SetProgramVariable("ExtraDrag", (float)SAVControl.GetProgramVariable("ExtraDrag") - CanopyDragMulti);
            DragApplied = false;
        }
    }
    //these events have to be used with a frame delay because if you call them from an event that was called by the same SendEventToExtensions function, the previous call stops.
    public void SendCanopyClosed()
    {
        EntityControl.SendEventToExtensions("SFEXT_O_CanopyClosed");
    }
    public void SendCanopyOpened()
    {
        EntityControl.SendEventToExtensions("SFEXT_O_CanopyOpen");
    }
    public void SendCanopyBreak()
    {
        EntityControl.SendEventToExtensions("SFEXT_O_CanopyBreak");
    }
    public void SendCanopyRepair()
    {
        EntityControl.SendEventToExtensions("SFEXT_O_CanopyRepair");
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
        if ((bool)SAVControl.GetProgramVariable("IsOwner"))
        {
            SendCustomEventDelayedFrames(nameof(SendCanopyBreak), 1);
        }
        if (!DragApplied)
        {
            SAVControl.SetProgramVariable("ExtraDrag", (float)SAVControl.GetProgramVariable("ExtraDrag") + CanopyDragMulti);
            DragApplied = true;
        }
    }
    public void ToggleCanopy()
    {
        if ((Time.time - LastCanopyToggleTime) > CanopyCloseTime + .1f && !CanopyBroken && !CanopyTransitioning && !(!CanopyCanComeOff && (float)SAVControl.GetProgramVariable("Speed") > CanopyAutoCloseSpeed))
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
