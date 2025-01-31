
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DFUNC_ToggleBool : UdonSharpBehaviour
    {
        [Tooltip("Not required. Put another ToggleBool object in this slot to make this toggle a secondary toggle that toggles the same thing\n If this isn't empty, the only other setting that does anything here is Dial_Funcon")]
        public UdonSharpBehaviour MasterToggle;
        public Animator BoolAnimator;
        [Tooltip("Object enabled when function is active (used on MFD)")]
        public GameObject[] Dial_Funcon;
        [SerializeField] private bool InvertFuncon = false;
        public bool DoAnimBool = true;
        public string AnimBoolName = "AnimBool";
        public bool OnDefault = false;
        [Tooltip("Set toggle to off when exiting?")]
        public bool PilotExitTurnOff = true;
        [Tooltip("Set toggle to on when exiting?")]
        public bool PilotExitTurnOn = false;
        [Tooltip("Set toggle to off when entering?")]
        public bool PilotEnterTurnOff = false;
        [Tooltip("Set toggle to on when entering?")]
        public bool PilotEnterTurnOn = false;
        [Tooltip("How long before toggle can be activated again")]
        public float ToggleMinDelay;
        [Tooltip("Objects to turn on/off with the toggle")]
        public GameObject[] ToggleObjects;
        [Tooltip("Objects to turn off/on with the toggle")]
        public GameObject[] ToggleObjects_Off;
        [Tooltip("Particle systems to turn on/off emission with the toggle")]
        public ParticleSystem[] ToggleEmission;
        public bool AllowToggleFlying = true;
        public bool AllowToggleGrounded = true;
        [Tooltip("Only for SeaPlanes/Vehicles with floatscript")]
        public bool AllowToggleOnWater = true;
        [Tooltip("Prevent/turn off when in afterburner")]
        public bool AllowAfterBurner = true;
        [Tooltip("Prevent/turn engine is off")]
        public bool AllowToggleEngineOff = true;
        [Tooltip("Prevent/turn engine is on")]
        public bool AllowToggleEngineOn = true;
        [Tooltip("Send Events to sound script for opening a door?")]
        [Space(10)]
        public bool OpensDoor = false;
        [Header("Door Only:")]
        [Tooltip("If this toggle opens a door, it will change the sound to the outside sounds using the soundcontroller")]
        public UdonSharpBehaviour SoundControl;
        [Tooltip("How long it takes for the sound to change after toggle to closed")]
        public float DoorCloseTime = 2;
        [System.NonSerializedAttribute] public bool AnimOn = false;
        [System.NonSerializedAttribute] public float ToggleTime;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [System.NonSerializedAttribute] public SAV_PassengerFunctionsController PassengerFunctionsControl;
        private ParticleSystem.EmissionModule[] ToggleEmission_em;
        private int ParticleLength;
        private bool ToggleAllowed = true;
        private bool TriggerLastFrame;
        private bool IsSecondary = false;
        public void SFEXT_L_EntityStart()
        {
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
                { funcon.SetActive(InvertFuncon ? !OnDefault : OnDefault); }
            }
            ParticleLength = ToggleEmission.Length;
            ToggleEmission_em = new ParticleSystem.EmissionModule[ParticleLength];
            for (int i = 0; i < ParticleLength; i++)
            { ToggleEmission_em[i] = ToggleEmission[i].emission; }
        }
        public void SFEXT_O_OnPlayerJoined()
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
            HoldingTrigger_Held = 0;
            gameObject.SetActive(false);
        }
        public void SFEXT_G_PilotEnter()
        {
            if (!IsSecondary)
            {
                if (PilotEnterTurnOff)
                {
                    if (AnimOn)
                    { SetBoolOff(); }
                }
                if (PilotEnterTurnOn)
                {
                    if (!AnimOn)
                    { SetBoolOn(); }
                }
            }
        }
        public void SFEXT_O_PilotExit()
        {
            HoldingTrigger_Held = 0;
        }
        public void SFEXT_G_PilotExit()
        {
            if (!IsSecondary)
            {
                if (PilotExitTurnOn)
                {
                    if (!AnimOn)
                    { SetBoolOn(); }
                }
                if (PilotExitTurnOff)
                {
                    if (AnimOn)
                    { SetBoolOff(); }
                }
            }
            gameObject.SetActive(false);
        }
        public void SFEXT_G_Explode()
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
            if (HandHeldMode)
                Trigger = HoldingTrigger_Held;
            else
            {
                if (LeftDial)
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                else
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
            }
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
        public void Toggle()
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
            if (DoAnimBool && BoolAnimator) { BoolAnimator.SetBool(AnimBoolName, true); }
            foreach (GameObject funcon in Dial_Funcon)
            { funcon.SetActive(InvertFuncon ? !true : true); }
            if (OpensDoor)
            { SoundControl.SendCustomEvent("DoorOpen"); }
            foreach (GameObject obj in ToggleObjects)
            { obj.SetActive(true); }
            foreach (GameObject obj in ToggleObjects_Off)
            { obj.SetActive(false); }
            for (int i = 0; i < ParticleLength; i++)
            { ToggleEmission_em[i].enabled = true; }
        }
        public void SetBoolOff()
        {
            if (!AnimOn) { return; }
            ToggleTime = Time.time;
            AnimOn = false;
            if (DoAnimBool && BoolAnimator) { BoolAnimator.SetBool(AnimBoolName, false); }
            foreach (GameObject funcon in Dial_Funcon)
            { funcon.SetActive(InvertFuncon ? !false : false); }
            if (OpensDoor)
            { SoundControl.SendCustomEventDelayedSeconds("DoorClose", DoorCloseTime); }
            foreach (GameObject obj in ToggleObjects)
            { obj.SetActive(false); }
            foreach (GameObject obj in ToggleObjects_Off)
            { obj.SetActive(true); }
            for (int i = 0; i < ParticleLength; i++)
            { ToggleEmission_em[i].enabled = false; }
        }
        public void SFEXT_G_RespawnButton()
        {
            if (!IsSecondary)
            {
                if (!OnDefault && AnimOn)
                { SetBoolOff(); }
                else if (OnDefault && !AnimOn)
                { SetBoolOn(); }
            }
        }
        private bool InAir;
        private bool OnWater;
        public void SFEXT_G_TakeOff()
        {
            InAir = true;
            OnWater = false;
            CheckToggleAllowed();
        }
        public void SFEXT_G_TouchDown()
        {
            InAir = false;
            OnWater = false;
            CheckToggleAllowed();
        }
        public void SFEXT_G_TouchDownWater()
        {
            InAir = false;
            OnWater = true;
            CheckToggleAllowed();
        }
        private void CheckToggleAllowed()
        {
            ToggleAllowed =
                    (AllowToggleFlying || !InAir)
                && (AllowAfterBurner || !ABOn)
                && (AllowToggleGrounded || InAir)
                && (AllowToggleOnWater || !OnWater)
                && (AllowToggleEngineOff || EngineOn)
                && (AllowToggleEngineOn || !EngineOn)
            ;
            if (!MasterToggle && AnimOn && !ToggleAllowed)
            { ToggleWhenPossible(); }
        }
        private void ToggleWhenPossible()
        {
            if (Time.time - ToggleTime > ToggleMinDelay)
            { Toggle(); }
            else
            { SendCustomEventDelayedSeconds(nameof(Toggle), Time.time - ToggleTime + .05f); }
        }
        private bool ABOn;
        public void SFEXT_G_AfterburnerOff()
        {
            ABOn = false;
            CheckToggleAllowed();
        }
        public void SFEXT_G_AfterburnerOn()
        {
            ABOn = true;
            CheckToggleAllowed();
        }
        private bool EngineOn;
        public void SFEXT_G_EngineOn()
        {
            EngineOn = true;
            CheckToggleAllowed();
        }
        public void SFEXT_G_EngineOff()
        {
            EngineOn = false;
            CheckToggleAllowed();
        }
        private bool HandHeldMode = false;
        public void SFEXT_O_OnPickup() { HandHeldMode = true; }
        public void SFEXT_G_OnPickup() { SFEXT_G_PilotEnter(); }
        public void SFEXT_G_OnDrop() { SFEXT_G_PilotExit(); }
        private int HoldingTrigger_Held = 0;
        public void SFEXT_O_OnPickupUseDown()
        {
            HoldingTrigger_Held = 1;
        }
        public void SFEXT_O_OnPickupUseUp()
        {
            HoldingTrigger_Held = 0;
        }
        public void SFEXT_O_TakeOwnership()
        {//disable if owner leaves while piloting
            if (!IsSecondary)
            {
                if (!(EntityControl.Piloting || EntityControl.Holding))
                {
                    if (PilotExitTurnOff)
                    {
                        if (AnimOn)
                        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOff)); }
                    }
                    if (PilotExitTurnOn)
                    {
                        if (!AnimOn)
                        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOn)); }
                    }
                }
            }
        }
    }
}