
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class DFUNCP_ToggleBool : UdonSharpBehaviour
{
    [SerializeField] private Animator BoolAnimator;
    [SerializeField] private string AnimBoolName = "AnimBool";
    [Tooltip("Object enabled when function is enabled (used on MFD)")]
    [SerializeField] private GameObject[] Dial_Funcon;
    public bool OnDefault = false;
    [Tooltip("Set toggle to its default when exiting?")]
    [SerializeField] private bool PilotExitTurnOff = true;
    [SerializeField] private float ToggleMinDelay;
    [Tooltip("Send Events to sound script for opening a door?")]
    [SerializeField] private bool OpensDoor = false;
    [Header("Door Only:")]
    [SerializeField] private UdonBehaviour SoundControl;
    [SerializeField] private float DoorCloseTime = 2;
    [Tooltip("Put another ToggleBool object in this slot to make this toggle a secondary toggle that toggles the same thing")]
    [SerializeField] private UdonSharpBehaviour MasterToggle;
    private bool AnimOn = false;
    private float ToggleTime;
    private bool UseLeftTrigger = false;
    private bool TriggerLastFrame;
    private int AnimBool_STRING;
    private bool sound_DoorOpen;
    private bool Dial_FunconNULL;
    private bool IsSecondary = false;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXTP_L_EntityStart()
    {
        Dial_FunconNULL = Dial_Funcon.Length > 0;
        if (MasterToggle)//this object is slave
        {
            IsSecondary = true;
            ToggleMinDelay = (float)MasterToggle.GetProgramVariable("ToggleMinDelay");
        }
        else//this object is master
        {
            if (OpensDoor && (ToggleMinDelay < DoorCloseTime)) { ToggleMinDelay = DoorCloseTime; }
            AnimBool_STRING = Animator.StringToHash(AnimBoolName);
            if (OnDefault)
            {
                SetBoolOn();
            }
            foreach (GameObject funcon in Dial_Funcon)
            { funcon.SetActive(OnDefault); }
        }
    }
    public void SFEXTP_O_PlayerJoined()
    {
        if (!IsSecondary)
        {
            if (OnDefault && !AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOff"); }
            else if (!OnDefault && AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOn"); }
        }
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
        if (!IsSecondary)
        {
            if (PilotExitTurnOff)
            {
                if (!OnDefault && AnimOn)
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOff"); }
                else if (OnDefault && !AnimOn)
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOn"); }
            }
        }
        gameObject.SetActive(false);
    }
    public void SFEXTP_G_Explode()
    {
        if (!IsSecondary)
        {
            if (OnDefault && !AnimOn)
            { SetBoolOn(); }
            else if (!OnDefault && AnimOn)
            { SetBoolOff(); }
        }
    }
    public void KeyboardInput()
    {
        if (IsSecondary)
        {
            if (Time.time - (float)MasterToggle.GetProgramVariable("ToggleTime") > ToggleMinDelay)
            {
                if ((bool)MasterToggle.GetProgramVariable("AnimOn"))
                {
                    MasterToggle.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOff");
                }
                else
                {
                    MasterToggle.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOn");
                }
            }
        }
        else
        {
            if (Time.time - ToggleTime > ToggleMinDelay)
            {
                if (AnimOn)
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOff"); }
                else
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOn"); }
            }
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
                if (IsSecondary)
                {
                    if (Time.time - (float)MasterToggle.GetProgramVariable("ToggleTime") > ToggleMinDelay)
                    {
                        if ((bool)MasterToggle.GetProgramVariable("AnimOn"))
                        {
                            MasterToggle.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOff");
                        }
                        else
                        {
                            MasterToggle.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOn");
                        }
                    }
                }
                else
                {
                    if (Time.time - ToggleTime > ToggleMinDelay)
                    {
                        if (AnimOn)
                        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOff"); }
                        else
                        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOn"); }
                    }
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
        foreach (GameObject funcon in Dial_Funcon)
        { funcon.SetActive(true); }
        if (OpensDoor)
        { SoundControl.SendCustomEvent("DoorOpen"); }
    }
    public void SetBoolOff()
    {
        if (!AnimOn) { return; }
        ToggleTime = Time.time;
        AnimOn = false;
        BoolAnimator.SetBool(AnimBool_STRING, AnimOn);
        foreach (GameObject funcon in Dial_Funcon)
        { funcon.SetActive(false); }
        if (OpensDoor)
        { SoundControl.SendCustomEventDelayedSeconds("DoorClose", DoorCloseTime); }
    }
    public void SFEXTP_G_RespawnButton()
    {
        if (!IsSecondary)
        {
            if (!OnDefault && AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOff"); }
            else if (OnDefault && !AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetBoolOn"); }
        }
    }
    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        if (!IsSecondary)
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
}