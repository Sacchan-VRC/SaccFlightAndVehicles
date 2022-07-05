
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DFUNCP_ToggleBool : UdonSharpBehaviour
    {
        [Tooltip("Put another ToggleBool object in this slot to make this toggle a secondary toggle that toggles the same thing")]
        public UdonSharpBehaviour MasterToggle;
        public Animator BoolAnimator;
        public string AnimBoolName = "AnimBool";
        [Tooltip("Object enabled when function is enabled (used on MFD)")]
        public GameObject[] Dial_Funcon;
        public bool OnDefault = false;
        [Tooltip("Set toggle to its default when exiting?")]
        public bool PilotExitTurnOff = true;
        [Tooltip("How long before toggle can be activated again")]
        public float ToggleMinDelay;
        [Tooltip("Objects to turn on/off with the toggle")]
        public GameObject[] ToggleObjects;
        [Tooltip("Particle systems to turn on/off emission with the toggle")]
        public ParticleSystem[] ToggleEmission;
        public bool AllowToggleFlying = true;
        public bool AllowToggleGrounded = true;
        [Tooltip("Only for SeaPlanes/Vehicles with floatscript")]
        public bool AllowToggleOnWater = true;
        [Tooltip("Send Events to sound script for opening a door?")]
        public bool OpensDoor = false;
        [Header("Door Only:")]
        [Tooltip("If this toggle opens a door, it will change the sound to the outside sounds using the soundcontroller")]
        public UdonBehaviour SoundControl;
        [Tooltip("How long it takes for the sound to change after toggle to closed")]
        public float DoorCloseTime = 2;
        [System.NonSerializedAttribute] public bool AnimOn = false;
        [System.NonSerializedAttribute] public float ToggleTime;
        private ParticleSystem.EmissionModule[] ToggleEmission_em;
        private int ParticleLength;
        private bool ToggleAllowed = true;
        private bool UseLeftTrigger = false;
        private bool TriggerLastFrame;
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
                if (OnDefault)
                {
                    SetBoolOn();
                }
                foreach (GameObject funcon in Dial_Funcon)
                { funcon.SetActive(OnDefault); }
            }
            ParticleLength = ToggleEmission.Length;
            ToggleEmission_em = new ParticleSystem.EmissionModule[ParticleLength];
            for (int i = 0; i < ParticleLength; i++)
            { ToggleEmission_em[i] = ToggleEmission[i].emission; }
        }
        public void SFEXTP_O_PlayerJoined()
        {
            if (!IsSecondary)
            {
                if (OnDefault && !AnimOn)
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOff)); }
                else if (!OnDefault && AnimOn)
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOn)); }
            }
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
        public void SFEXTP_O_UserExit()
        {
            if (!IsSecondary)
            {
                if (PilotExitTurnOff)
                {
                    if (!OnDefault && AnimOn)
                    { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOff)); }
                    else if (OnDefault && !AnimOn)
                    { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOn)); }
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
            if (ToggleAllowed)
            { Toggle(); }
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
                    if (ToggleAllowed)
                    { Toggle(); }
                }
                TriggerLastFrame = true;
            }
            else { TriggerLastFrame = false; }
        }
        private void Toggle()
        {
            if (IsSecondary)
            {
                if (Time.time - (float)MasterToggle.GetProgramVariable("ToggleTime") > ToggleMinDelay)
                {
                    if ((bool)MasterToggle.GetProgramVariable("AnimOn"))
                    {
                        MasterToggle.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOff));
                    }
                    else
                    {
                        MasterToggle.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOn));
                    }
                }
            }
            else
            {
                if (Time.time - ToggleTime > ToggleMinDelay)
                {
                    if (AnimOn)
                    { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOff)); }
                    else
                    { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOn)); }
                }
            }
        }
        public void SetBoolOn()
        {
            if (AnimOn) { return; }
            ToggleTime = Time.time;
            AnimOn = true;
            if (BoolAnimator) { BoolAnimator.SetBool(AnimBoolName, true); }
            foreach (GameObject funcon in Dial_Funcon)
            { funcon.SetActive(true); }
            if (OpensDoor)
            { SoundControl.SendCustomEvent("DoorOpen"); }
            foreach (GameObject obj in ToggleObjects)
            { obj.SetActive(true); }
            for (int i = 0; i < ParticleLength; i++)
            { ToggleEmission_em[i].enabled = true; }
        }
        public void SetBoolOff()
        {
            if (!AnimOn) { return; }
            ToggleTime = Time.time;
            AnimOn = false;
            if (BoolAnimator) { BoolAnimator.SetBool(AnimBoolName, false); }
            foreach (GameObject funcon in Dial_Funcon)
            { funcon.SetActive(false); }
            if (OpensDoor)
            { SoundControl.SendCustomEventDelayedSeconds("DoorClose", DoorCloseTime); }
            foreach (GameObject obj in ToggleObjects)
            { obj.SetActive(false); }
            for (int i = 0; i < ParticleLength; i++)
            { ToggleEmission_em[i].enabled = false; }
        }
        public void SFEXTP_G_RespawnButton()
        {
            if (!IsSecondary)
            {
                if (!OnDefault && AnimOn)
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOff)); }
                else if (OnDefault && !AnimOn)
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOn)); }
            }
        }
        public void SFEXTP_G_TakeOff()
        {
            ToggleAllowed = AllowToggleFlying;
            if (AnimOn && !AllowToggleFlying)
            { Toggle(); }
        }
        public void SFEXTP_G_TouchDown()
        {
            ToggleAllowed = AllowToggleGrounded;
            if (AnimOn && !AllowToggleGrounded)
            { Toggle(); }
        }
        public void SFEXTP_G_TouchDownWater()
        {
            ToggleAllowed = AllowToggleOnWater;
            if (AnimOn && !AllowToggleOnWater)
            { Toggle(); }
        }
        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {//disable if owner leaves while piloting
            if (!IsSecondary)
            {
                if (PilotExitTurnOff && player.isLocal)
                {
                    if (!OnDefault && AnimOn)
                    { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOff)); }
                    else if (OnDefault && !AnimOn)
                    { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOn)); }
                }
            }
        }
    }
}