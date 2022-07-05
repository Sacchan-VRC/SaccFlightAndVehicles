
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DFUNC_Gear : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        [Tooltip("Object enabled when function is active (used on MFD)")]
        public GameObject Dial_Funcon;
        public Animator GearAnimator;
        [Tooltip("Multiply drag by this amount while gear is down")]
        public float LandingGearDragMulti = 1.3f;
        public bool AllowToggleGrounded = true;
        [Tooltip("Distance to check down from CenterOfMass to decide if it's clear to open the gear")]
        public float GearCheckDistance = 2f;
        [Tooltip("If ticked, gear can only be toggled every TransitionLength")]
        public bool AllowToggleDuringTransition = true;
        public float TransitionLength = 5f;
        private float TransitionTime;
        private SaccEntity EntityControl;
        private bool UseLeftTrigger = false;
        private bool TriggerLastFrame;
        [System.NonSerializedAttribute] public bool GearUp = false;
        private bool DragApplied = false;
        private bool IsOwner = false;
        private bool DisableGroundDetector = false;
        private Transform CenterOfMass;
        [System.NonSerializedAttribute] public bool _DisableGearToggle;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(DisableGearToggle_))] public int DisableGearToggle = 0;
        public int DisableGearToggle_
        {
            set
            {
                _DisableGearToggle = value > 0;
                DisableGearToggle = value;
            }
            get => DisableGearToggle;
        }
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        public void SFEXT_L_EntityStart()
        {
            EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
            CenterOfMass = EntityControl.CenterOfMass;
            LandingGearDragMulti -= 1;//to match how the old values worked
            SetGearDown();
            if (Dial_Funcon) { Dial_Funcon.SetActive(!GearUp); }
            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            IsOwner = (bool)SAVControl.GetProgramVariable("IsOwner");

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
            if (Dial_Funcon) Dial_Funcon.SetActive(!GearUp);
        }
        public void SFEXT_O_PilotExit()
        {
            DFUNC_Deselected();
        }
        public void SFEXT_L_PassengerEnter()
        {
            if (Dial_Funcon) Dial_Funcon.SetActive(!GearUp);
        }
        public void SFEXT_O_TakeOwnership()
        {
            IsOwner = true;
        }
        public void SFEXT_O_LoseOwnership()
        {
            IsOwner = false;
        }
        public void SFEXT_G_Explode()
        {
            SetGearDown();
        }
        public void SFEXT_G_RespawnButton()
        {
            SetGearDown();
            GearAnimator.SetTrigger("instantgeardown");
        }
        public void KeyboardInput()
        {
            if (!_DisableGearToggle)
            {
                if (AllowToggleDuringTransition || (Time.time - TransitionTime) > TransitionLength)
                {
                    ToggleGear();
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
                if (!TriggerLastFrame && !_DisableGearToggle)
                {
                    if (AllowToggleDuringTransition || (Time.time - TransitionTime) > TransitionLength)
                    {
                        ToggleGear();
                    }
                }
                TriggerLastFrame = true;
            }
            else { TriggerLastFrame = false; }
        }
        public void SetGearUp()
        {
            //Debug.Log("SetGearUp");
            if (!DisableGroundDetector)
            {
                SAVControl.SetProgramVariable("DisableGroundDetection", (int)SAVControl.GetProgramVariable("DisableGroundDetection") + 1);
                SAVControl.SetProgramVariable("GroundedLastFrame", false);
                SAVControl.SetProgramVariable("GDHitRigidbody", null);
                DisableGroundDetector = true;
            }
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
            GearUp = true;
            GearAnimator.SetBool("gearup", true);
            if (DragApplied)
            {
                SAVControl.SetProgramVariable("ExtraDrag", (float)SAVControl.GetProgramVariable("ExtraDrag") - LandingGearDragMulti);
                DragApplied = false;
            }

            EntityControl.SendEventToExtensions("SFEXT_G_GearUp");
            TransitionTime = Time.time;
        }
        public void SetGearDown()
        {
            //Debug.Log("SetGearDown");
            if (DisableGroundDetector)
            {
                SAVControl.SetProgramVariable("DisableGroundDetection", (int)SAVControl.GetProgramVariable("DisableGroundDetection") - 1);
                DisableGroundDetector = false;
            }
            if (Dial_Funcon) { Dial_Funcon.SetActive(true); }
            GearUp = false;
            GearAnimator.SetBool("gearup", false);
            if (!DragApplied)
            {
                SAVControl.SetProgramVariable("ExtraDrag", (float)SAVControl.GetProgramVariable("ExtraDrag") + LandingGearDragMulti);
                DragApplied = true;
            }

            EntityControl.SendEventToExtensions("SFEXT_G_GearDown");
            TransitionTime = Time.time;
        }
        public void ToggleGear()
        {
            if (AllowToggleGrounded || !(bool)SAVControl.GetProgramVariable("Taxiing"))
            {
                if (!GearUp)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetGearUp));
                }
                else
                {
                    if (AllowToggleGrounded || !Physics.Raycast(CenterOfMass.position, -EntityControl.transform.up, GearCheckDistance, 133121/* Default, Environment, and Walkthrough */))
                    { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetGearDown)); }
                }
            }
        }
        public void SFEXT_O_OnPlayerJoined()
        {
            if (GearUp)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetGearUp));
            }
        }

    }
}