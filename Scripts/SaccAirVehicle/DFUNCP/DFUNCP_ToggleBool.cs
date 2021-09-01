
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNCP_ToggleBool : UdonSharpBehaviour
{
    [SerializeField] private Animator BoolAnimator;
    [SerializeField] private string AnimBoolName = "AnimBool";
    [SerializeField] private bool OnDefault = false;
    [Tooltip("Set toggle to its default when exiting?")]
    [SerializeField] private bool PilotExitTurnOff = true;
    [SerializeField] private float ToggleMinDelay;
    [Tooltip("Object enabled when function is enabled (used on MFD)")]
    [SerializeField] private GameObject Dial_Funcon;
    [Tooltip("Send Events to sound script for opening a door?")]
    [SerializeField] private bool OpensDoor = false;
    [Header("Door Only:")]
    [SerializeField] private SAV_SoundController SoundControl;
    [SerializeField] private float DoorCloseTime = 2;
    private bool Dial_FunconNULL = true;
    private bool AnimOn = false;
    private float ToggleTime;
    private bool UseLeftTrigger = false;
    private bool TriggerLastFrame;
    private int AnimBool_STRING;
    private bool sound_DoorOpen;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXTP_L_EntityStart()
    {
        if (OpensDoor && (ToggleMinDelay < DoorCloseTime)) { ToggleMinDelay = DoorCloseTime; }
        AnimBool_STRING = Animator.StringToHash(AnimBoolName);
        Dial_FunconNULL = Dial_Funcon == null;
        if (OnDefault)
        {
            SetBoolOn();
        }
    }
    public void SFEXTP_O_PlayerJoined()
    {
        if (OnDefault && !AnimOn)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOff"); }
        else if (!OnDefault && AnimOn)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOn"); }
    }
    public void DFUNC_Selected()
    {
        gameObject.SetActive(true);
    }
    public void DFUNC_Deselected()
    {
        gameObject.SetActive(false);
    }
    public void SFEXTP_O_UserExit()
    {
        if (PilotExitTurnOff)
        {
            if (!OnDefault && AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOff"); }
            else if (OnDefault && !AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOn"); }
        }
        gameObject.SetActive(false);
    }
    public void SFEXTP_G_Explode()
    {
        if (OnDefault && !AnimOn)
        { SetBoolOn(); }
        else if (!OnDefault && AnimOn)
        { SetBoolOff(); }
    }
    public void KeyboardInput()
    {
        if (Time.time - ToggleTime > ToggleMinDelay)
        {
            if (AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOff"); }
            else
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOn"); }
        }
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
                if (Time.time - ToggleTime > ToggleMinDelay)
                {
                    if (AnimOn)
                    { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOff"); }
                    else
                    { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOn"); }
                }
            }
            TriggerLastFrame = true;
        }
        else { TriggerLastFrame = false; }
    }

    public void SetBoolOn()
    {
        if (AnimOn) { return; }
        ToggleTime = Time.time;
        AnimOn = true;
        BoolAnimator.SetBool(AnimBool_STRING, AnimOn);
        if (!Dial_FunconNULL) { Dial_Funcon.SetActive(true); }
        if (OpensDoor)
        { SoundControl.DoorOpen(); }
    }
    public void SetBoolOff()
    {
        if (!AnimOn) { return; }
        ToggleTime = Time.time;
        AnimOn = false;
        BoolAnimator.SetBool(AnimBool_STRING, AnimOn);
        if (!Dial_FunconNULL) { Dial_Funcon.SetActive(false); }
        if (OpensDoor)
        { SoundControl.SendCustomEventDelayedSeconds("DoorClose", DoorCloseTime); }
    }
    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        if (PilotExitTurnOff && player.isLocal)
        {
            if (!OnDefault && AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOff"); }
            else if (OnDefault && !AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOn"); }
        }
    }
}