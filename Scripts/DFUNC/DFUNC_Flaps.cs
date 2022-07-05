
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DFUNC_Flaps : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public Animator FlapsAnimator;
        [Tooltip("Object enabled when function is active (used on MFD)")]
        public GameObject Dial_Funcon;
        public string AnimatorBool = "flaps";
        public bool DefaultFlapsOff = false;
        [Tooltip("Multiply Vehicle's drag by this amount while flaps are enabled")]
        public float FlapsDragMulti = 1.4f;
        [Tooltip("Multiply Vehicle's lift by this amount while flaps are enabled")]
        public float FlapsLiftMulti = 1.35f;
        [Tooltip("Add this much to aircraft's Max Lift by this amount while flaps are enabled")]
        public float FlapsExtraMaxLift = 0;
        private SaccEntity EntityControl;
        private bool UseLeftTrigger = false;
        [System.NonSerializedAttribute] public bool Flaps = false;
        private bool TriggerLastFrame;
        private bool DragApplied;
        private bool LiftApplied;
        private bool MaxLiftApplied;
        private bool InVR = false;
        private bool Selected;
        private bool InEditor = true;
        private VRCPlayerApi localPlayer;
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        public void DFUNC_Selected()
        {
            TriggerLastFrame = true;
            gameObject.SetActive(true);
            Selected = true;
        }
        public void DFUNC_Deselected()
        {
            if (!Flaps) { gameObject.SetActive(false); }
            Selected = false;
        }
        public void SFEXT_L_EntityStart()
        {
            localPlayer = Networking.LocalPlayer;
            if (localPlayer != null) { InEditor = false; }
            EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
            //to match how the old values worked
            FlapsDragMulti -= 1f;
            FlapsLiftMulti -= 1f;

            if ((bool)SAVControl.GetProgramVariable("AutoAdjustValuesToMass"))
            { FlapsExtraMaxLift *= ((Rigidbody)SAVControl.GetProgramVariable("VehicleRigidbody")).mass; }

            if (Dial_Funcon) Dial_Funcon.SetActive(Flaps);
            if (DefaultFlapsOff) { SetFlapsOff(); }
            else { SetFlapsOn(); }
        }
        public void SFEXT_O_PilotEnter()
        {
            if (Flaps) { gameObject.SetActive(true); }
            if (!InEditor) { InVR = Networking.LocalPlayer.IsUserInVR(); }//move to start when they fix the bug
            if (Dial_Funcon) Dial_Funcon.SetActive(Flaps);
        }
        public void SFEXT_O_PilotExit()
        {
            DFUNC_Deselected();
        }
        public void SFEXT_L_PassengerEnter()
        {
            if (Dial_Funcon) Dial_Funcon.SetActive(Flaps);
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
                if ((bool)SAVControl.GetProgramVariable("PitchDown"))//flaps on, but plane's angle of attack is negative so they have no helpful effect
                {
                    if (LiftApplied)
                    {
                        SAVControl.SetProgramVariable("ExtraLift", (float)SAVControl.GetProgramVariable("ExtraLift") - FlapsLiftMulti);
                        LiftApplied = false;
                    }
                    if (MaxLiftApplied)
                    {
                        SAVControl.SetProgramVariable("MaxLift", (float)SAVControl.GetProgramVariable("MaxLift") - FlapsExtraMaxLift);
                        MaxLiftApplied = false;
                    }
                }
                else//flaps on positive angle of attack, flaps are useful
                {
                    if (!LiftApplied)
                    {
                        SAVControl.SetProgramVariable("ExtraLift", (float)SAVControl.GetProgramVariable("ExtraLift") + FlapsLiftMulti);
                        LiftApplied = true;
                    }
                    if (!MaxLiftApplied)
                    {
                        SAVControl.SetProgramVariable("MaxLift", (float)SAVControl.GetProgramVariable("MaxLift") + FlapsExtraMaxLift);
                        MaxLiftApplied = true;
                    }
                }
            }
        }
        public void KeyboardInput()
        {
            ToggleFlaps();
        }
        public void SetFlapsOff()
        {
            if (Dial_Funcon) Dial_Funcon.SetActive(false);
            Flaps = false;
            FlapsAnimator.SetBool(AnimatorBool, false);

            if (DragApplied)
            {
                SAVControl.SetProgramVariable("ExtraDrag", (float)SAVControl.GetProgramVariable("ExtraDrag") - FlapsDragMulti);
                DragApplied = false;
            }
            if (LiftApplied)
            {
                SAVControl.SetProgramVariable("ExtraLift", (float)SAVControl.GetProgramVariable("ExtraLift") - FlapsLiftMulti);
                LiftApplied = false;
            }
            if (MaxLiftApplied)
            {
                SAVControl.SetProgramVariable("MaxLift", (float)SAVControl.GetProgramVariable("MaxLift") - FlapsExtraMaxLift);
                MaxLiftApplied = false;
            }

            if ((bool)SAVControl.GetProgramVariable("IsOwner"))
            {
                if (!InVR) { gameObject.SetActive(false); }//for desktop Users
                EntityControl.SendEventToExtensions("SFEXT_O_FlapsOff");
            }
        }
        public void SetFlapsOn()
        {
            Flaps = true;
            FlapsAnimator.SetBool(AnimatorBool, true);
            if (Dial_Funcon) Dial_Funcon.SetActive(true);

            if (!DragApplied)
            {
                SAVControl.SetProgramVariable("ExtraDrag", (float)SAVControl.GetProgramVariable("ExtraDrag") + FlapsDragMulti);
                DragApplied = true;
            }
            if (!LiftApplied)
            {
                SAVControl.SetProgramVariable("ExtraLift", (float)SAVControl.GetProgramVariable("ExtraLift") + FlapsLiftMulti);
                LiftApplied = true;
            }
            if (!MaxLiftApplied)
            {
                SAVControl.SetProgramVariable("MaxLift", (float)SAVControl.GetProgramVariable("MaxLift") + FlapsExtraMaxLift);
                MaxLiftApplied = true;
            }

            if ((bool)SAVControl.GetProgramVariable("IsOwner"))
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
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetFlapsOn));
            }
            else
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetFlapsOff));
            }
        }
        public void SFEXT_O_OnPlayerJoined()
        {
            if (!Flaps && !DefaultFlapsOff)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetFlapsOff));
            }

            else if (Flaps && DefaultFlapsOff)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetFlapsOn));
            }
        }
    }
}