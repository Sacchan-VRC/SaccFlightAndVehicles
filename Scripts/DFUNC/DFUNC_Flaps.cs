﻿
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
        public GameObject[] Dial_Funcon_Array;
        public string AnimatorBool = "flaps";
        public bool DefaultFlapsOff = false;
        [Tooltip("Multiply Vehicle's drag by this amount while flaps are enabled")]
        public float FlapsDragMulti = 1.4f;
        [Tooltip("Multiply Vehicle's lift by this amount while flaps are enabled")]
        public float FlapsLiftMulti = 1.35f;
        [Tooltip("Multiply Vehicle's vel lift by this amount while flaps are enabled")]
        public float FlapsVelLiftMulti = 1f;
        [Tooltip("Add this much to aircraft's Max Lift by this amount while flaps are enabled")]
        public float FlapsExtraMaxLift = 0;
        [Tooltip("Add this much to aircraft's Straighten values while flaps are enabled")]
        public float FlapsStraightenMulti = 1;
        private float StraightenStartValue_Pitch;
        private float StraightenStartValue_Yaw;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [System.NonSerializedAttribute] public bool Flaps = false;
        private bool TriggerLastFrame;
        private bool DragApplied;
        private bool LiftApplied;
        private bool VelLiftApplied;
        private bool StraightenApplied;
        private bool MaxLiftApplied;
        private bool InVR = false;
        private bool Selected;
        private bool Asleep;
        private bool InEditor = true;
        private VRCPlayerApi localPlayer;
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
            //to match how the old values worked
            FlapsDragMulti -= 1f;
            FlapsLiftMulti -= 1f;
            FlapsVelLiftMulti -= 1f;
            InVR = EntityControl.InVR;

            StraightenStartValue_Pitch = (float)SAVControl.GetProgramVariable("VelStraightenStrPitch");
            StraightenStartValue_Yaw = (float)SAVControl.GetProgramVariable("VelStraightenStrYaw");

            if (Dial_Funcon) Dial_Funcon.SetActive(Flaps);
            for (int i = 0; i < Dial_Funcon_Array.Length; i++) { Dial_Funcon_Array[i].SetActive(Flaps); }
            if (DefaultFlapsOff) { SetFlapsOff(); }
            else { SetFlapsOn(); }
        }
        public void SFEXT_L_OnEnable()
        {
            if (FlapsAnimator) { FlapsAnimator.SetBool(AnimatorBool, Flaps); }
        }
        public void SFEXT_O_PilotEnter()
        {
            InVR = EntityControl.InVR;
            if (Flaps) { gameObject.SetActive(true); }
            if (Dial_Funcon) Dial_Funcon.SetActive(Flaps);
            for (int i = 0; i < Dial_Funcon_Array.Length; i++) { Dial_Funcon_Array[i].SetActive(Flaps); }
        }
        public void SFEXT_O_PilotExit()
        {
            DFUNC_Deselected();
        }
        public void SFEXT_L_PassengerEnter()
        {
            if (Dial_Funcon) Dial_Funcon.SetActive(Flaps);
            for (int i = 0; i < Dial_Funcon_Array.Length; i++) { Dial_Funcon_Array[i].SetActive(Flaps); }
        }
        public void SFEXT_G_Explode()
        {
            if (DefaultFlapsOff)
            { SetFlapsOff(); }
            else
            { SetFlapsOn(); }
        }
        public void SFEXT_G_RespawnButton()
        {
            if (!EntityControl.IsOwner) return;
            if (DefaultFlapsOff)
            { SetFlapsOff(); }
            else
            { SetFlapsOn(); }
        }
        public void SFEXT_L_WakeUp()
        {
            Asleep = false;
        }
        public void SFEXT_L_FallAsleep()
        {
            Asleep = true;
        }
        private void Update()
        {
            if (!Asleep)
            {
                if (Selected)
                {
                    float Trigger;
                    if (LeftDial)
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
        }
        public void KeyboardInput()
        {
            ToggleFlaps();
        }
        public void SetFlapsOff()
        {
            if (!Flaps) { return; }
            if (Dial_Funcon) Dial_Funcon.SetActive(false);
            for (int i = 0; i < Dial_Funcon_Array.Length; i++) { Dial_Funcon_Array[i].SetActive(false); }
            Flaps = false;
            if (FlapsAnimator) { FlapsAnimator.SetBool(AnimatorBool, false); }

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
            if (VelLiftApplied)
            {
                SAVControl.SetProgramVariable("ExtraVelLift", (float)SAVControl.GetProgramVariable("ExtraVelLift") - FlapsVelLiftMulti);
                VelLiftApplied = false;
            }
            if (StraightenApplied)
            {
                SAVControl.SetProgramVariable("VelStraightenStrYaw", StraightenStartValue_Yaw);
                SAVControl.SetProgramVariable("VelStraightenStrPitch", StraightenStartValue_Pitch);
                StraightenApplied = false;
            }
            if (MaxLiftApplied)
            {
                SAVControl.SetProgramVariable("MaxLift", (float)SAVControl.GetProgramVariable("MaxLift") - FlapsExtraMaxLift);
                MaxLiftApplied = false;
            }

            if (EntityControl.IsOwner)
            {
                if (!InVR) { gameObject.SetActive(false); }//for desktop Users
                EntityControl.SendEventToExtensions("SFEXT_O_FlapsOff");
            }
        }
        public void SetFlapsOn()
        {
            if (Flaps) { return; }
            Flaps = true;
            if (FlapsAnimator) { FlapsAnimator.SetBool(AnimatorBool, true); }
            if (Dial_Funcon) Dial_Funcon.SetActive(true);
            for (int i = 0; i < Dial_Funcon_Array.Length; i++) { Dial_Funcon_Array[i].SetActive(true); }

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
            if (!VelLiftApplied)
            {
                SAVControl.SetProgramVariable("ExtraVelLift", (float)SAVControl.GetProgramVariable("ExtraVelLift") + FlapsVelLiftMulti);
                VelLiftApplied = true;
            }
            if (!StraightenApplied)
            {
                SAVControl.SetProgramVariable("VelStraightenStrYaw", StraightenStartValue_Yaw * FlapsStraightenMulti);
                SAVControl.SetProgramVariable("VelStraightenStrPitch", StraightenStartValue_Pitch * FlapsStraightenMulti);
                StraightenApplied = true;
            }
            if (!MaxLiftApplied)
            {
                SAVControl.SetProgramVariable("MaxLift", (float)SAVControl.GetProgramVariable("MaxLift") + FlapsExtraMaxLift);
                MaxLiftApplied = true;
            }

            if (EntityControl.IsOwner)
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