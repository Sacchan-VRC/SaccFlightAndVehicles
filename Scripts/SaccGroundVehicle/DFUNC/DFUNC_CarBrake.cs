﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [DefaultExecutionOrder(900)]//before gearbox
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_CarBrake : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SGVControl;
        public Animator BrakeAnimator;
        public SaccWheel[] BrakeWheels_Front;
        public SaccWheel[] BrakeWheels_Back;
        [Tooltip("Handbrake DOES NOT USE BrakeStrength")]
        public float BrakeStrength = 1f;
        [Range(0, 1)]
        public float Brake_FrontStrengthMulti = 1f;
        [Range(0, 1)]
        public float Brake_BackStrengthMulti = 1f;
        [Tooltip("Change keyboard brake strength to prevent skidding")]
        public float KeyboardBrakeMulti = 1f;
        [Tooltip("Because you have to hold the break, and the keyboardcontrols script can only send events, this option is here.")]
        public KeyCode KeyboardControl = KeyCode.S;
        // [Tooltip("If input is above this amount, input is clamped to max")]
        // public float UpperDeadZone = .9f;
        // [Tooltip("If input is bleow this amount, input is clamped to min")]
        // public float LowerDeadZone = .1f;
        private int AnimFloatName_STRING;
        [FieldChangeCallback(nameof(AnimFloatName))] public string _AnimFloatName = "brake";
        public string AnimFloatName
        {
            set
            {
                AnimFloatName_STRING = Animator.StringToHash(value);
                _AnimFloatName = value;
            }
            get => _AnimFloatName;
        }
        [Tooltip("FALSE:\n0=no brake, 1=movement speed - brake strength\nTRUE: 0=no brake, 1=wheel stopped")]
        public bool IsHandBrake = false;
        public bool EnableBrakeOnExit = true;
        [Header("For autoclutch")]
        public bool AutoClutch = true;
        public UdonSharpBehaviour GearBox;
        private bool ClutchOverrideLast = false;
        private string Brake_VariableName = "Brake";
        private string BrakeStrength_VariableName = "BrakeStrength";
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        private bool InVR = false;
        private bool DoFrontWheelBrakes = false;
        private bool DoBackWheelBrakes = false;
        private bool Selected = false;
        private bool Piloting = false;
        private bool Occupied = false;
        private bool BrakingLastFrame = false;
        private bool IsOwner = false;
        private float DeadZoneSize;
        private bool AnimatingBrake;
        private float LastUpdateTime;
        private float BrakeLast;
        [UdonSynced, System.NonSerialized, FieldChangeCallback(nameof(BrakeInput))] public float _BrakeInput;
        public float BrakeInput
        {
            set
            {
                _BrakeInput = value;
                if (IsOwner)
                {
                    if (BrakeAnimator) { BrakeAnimator.SetFloat(AnimFloatName_STRING, value); }
                }
                else
                {
                    if (!AnimatingBrake)
                    {
                        AnimatingBrake = true;
                        AnimateBrake();
                    }
                }
            }
            get => _BrakeInput;
        }
        public void SFEXT_L_EntityStart()
        {
            AnimFloatName = _AnimFloatName;
            // DeadZoneSize = (1 - UpperDeadZone) + LowerDeadZone;
            InVR = EntityControl.InVR;
            IsOwner = (bool)SGVControl.GetProgramVariable("IsOwner");
            if (IsHandBrake)
            {
                Brake_VariableName = "HandBrake";
            }
            else
            {
                Brake_VariableName = "Brake";
            }
            if (BrakeWheels_Back.Length > 0) { DoBackWheelBrakes = true; }
            if (BrakeWheels_Front.Length > 0) { DoFrontWheelBrakes = true; }
            if (!DoFrontWheelBrakes && !DoBackWheelBrakes)
            { Debug.LogWarning("WARNING: DFUNC_CarBrake has no brakewheels set."); }
            if (EnableBrakeOnExit)
            {
                SetBrakeOne();
                if (IsOwner)
                {
                    gameObject.SetActive(true);
                    RequestSerialization();
                }
            }
            if (!IsHandBrake)
            {
                for (int i = 0; i < BrakeWheels_Front.Length; i++)
                { BrakeWheels_Front[i].SetProgramVariable(BrakeStrength_VariableName, BrakeStrength); }
                for (int i = 0; i < BrakeWheels_Back.Length; i++)
                { BrakeWheels_Back[i].SetProgramVariable(BrakeStrength_VariableName, BrakeStrength); }
            }
        }
        private void TurnOnOverrides()
        {
            if (AutoClutch && !ClutchOverrideLast)
            {
                GearBox.SetProgramVariable("ClutchOverride", (int)GearBox.GetProgramVariable("ClutchOverride") + 1);
                ClutchOverrideLast = true;
            }
            if (IsHandBrake && !BrakingLastFrame)
            {
                SGVControl.SetProgramVariable("HandBrakeOn", (int)SGVControl.GetProgramVariable("HandBrakeOn") + 1);
                BrakingLastFrame = true;
            }
        }
        private void TurnOffOverrides()
        {
            if (AutoClutch && ClutchOverrideLast)
            {
                GearBox.SetProgramVariable("ClutchOverride", (int)GearBox.GetProgramVariable("ClutchOverride") - 1);
                ClutchOverrideLast = false;
            }
            if (IsHandBrake && BrakingLastFrame)
            {
                SGVControl.SetProgramVariable("HandBrakeOn", (int)SGVControl.GetProgramVariable("HandBrakeOn") - 1);
                BrakingLastFrame = false;
            }
        }
        public void DFUNC_Selected()
        {
            Selected = true;
        }
        public void DFUNC_Deselected()
        {
            Selected = false;
            SetBrakeZero();
            TurnOffOverrides();
            LastUpdateTime = Time.time;
            BrakeLast = BrakeInput;
            RequestSerialization();
        }
        public void SFEXT_O_PilotEnter()
        {
            SetBrakeZero();
            Piloting = true;
            InVR = EntityControl.InVR;
            LastUpdateTime = Time.time;
            BrakeLast = BrakeInput;
            RequestSerialization();
        }
        public void SFEXT_O_PilotExit()
        {
            Selected = false;
            Piloting = false;
            if (EnableBrakeOnExit && !GrappleActive)
            {
                SetBrakeOne();
            }
            TurnOffOverrides();
        }
        public void SFEXT_G_PilotEnter()
        {
            Occupied = true;
            gameObject.SetActive(true);
        }
        public void SFEXT_G_PilotExit()
        {
            Occupied = false;
            AnimatingBrake = false;
            if (!IsOwner)
            {
                gameObject.SetActive(false);
                if (EnableBrakeOnExit)
                {
                    BrakeInput = 1;
                }
                else
                {
                    BrakeInput = 0;
                }
            }
        }
        private float BrakeMover;
        public void AnimateBrake()
        {
            if (AnimatingBrake)
            {
                BrakeMover = Mathf.MoveTowards(BrakeMover, _BrakeInput, 2 * Time.deltaTime);
                if (BrakeAnimator) { BrakeAnimator.SetFloat(AnimFloatName_STRING, BrakeMover); }
                if (BrakeMover == _BrakeInput)
                {
                    AnimatingBrake = false;
                }
                else
                {
                    SendCustomEventDelayedFrames(nameof(AnimateBrake), 1);
                }
            }
        }
        private void LateUpdate()
        {
            if (Piloting)
            {
                if (!InVR || Selected)
                {
                    float Trigger;
                    if (LeftDial)
                    { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                    else
                    { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

                    float VRBrakeInput = Trigger;
                    /*         if (VRBrakeInput > LowerDeadZone)
                            {
                                float normalizedInput = Mathf.Min((VRBrakeInput - LowerDeadZone) * (1 / (VRBrakeInput - DeadZoneSize)), 1);
                                VRBrakeInput = LowerDeadZone + normalizedInput;
                            }
                            else
                            { VRBrakeInput = 0; } */
                    // if (VRBrakeInput > UpperDeadZone)
                    // { VRBrakeInput = 1f; }
                    // if (VRBrakeInput < LowerDeadZone)
                    // { VRBrakeInput = 0f; }

                    float KeyboardBrakeInput = 0;

                    if (Input.GetKey(KeyboardControl))
                    {
                        KeyboardBrakeInput = KeyboardBrakeMulti;
                    }
                    // if (VRBrakeInput < .1f) { VRBrakeInput = 0f; }//deadzone so there isnt constant brake applied
                    BrakeInput = Mathf.Max(VRBrakeInput, KeyboardBrakeInput);
                    if (BrakeInput > 0f)
                    {
                        TurnOnOverrides();
                    }
                    else
                    {
                        TurnOffOverrides();
                    }
#if UNITY_EDITOR
                    if (!IsHandBrake)
                    {
                        for (int i = 0; i < BrakeWheels_Front.Length; i++)
                        { BrakeWheels_Front[i].SetProgramVariable(BrakeStrength_VariableName, BrakeStrength); }
                        for (int i = 0; i < BrakeWheels_Back.Length; i++)
                        { BrakeWheels_Back[i].SetProgramVariable(BrakeStrength_VariableName, BrakeStrength); }
                    }
#endif
                    if (DoFrontWheelBrakes)
                    {
                        for (int i = 0; i < BrakeWheels_Front.Length; i++)
                        {
                            BrakeWheels_Front[i].SetProgramVariable(Brake_VariableName, BrakeInput * Brake_FrontStrengthMulti);
                        }
                    }
                    if (DoBackWheelBrakes)
                    {
                        for (int i = 0; i < BrakeWheels_Back.Length; i++)
                        {
                            BrakeWheels_Back[i].SetProgramVariable(Brake_VariableName, BrakeInput * Brake_BackStrengthMulti);
                        }
                    }
                    if (Time.time - LastUpdateTime > 0.3f)
                    {
                        if (BrakeInput != BrakeLast)
                        {
                            LastUpdateTime = Time.time;
                            BrakeLast = BrakeInput;
                            RequestSerialization();
                        }
                    }
                }
            }
        }
        public void SetBrakeZero()
        {
            BrakeInput = 0;
            if (DoFrontWheelBrakes)
            {
                for (int i = 0; i < BrakeWheels_Front.Length; i++)
                {
                    BrakeWheels_Front[i].SetProgramVariable(Brake_VariableName, 0f);
                }
            }
            if (DoBackWheelBrakes)
            {
                for (int i = 0; i < BrakeWheels_Back.Length; i++)
                {
                    BrakeWheels_Back[i].SetProgramVariable(Brake_VariableName, 0f);
                }
            }
        }
        public void SetBrakeOne()
        {
            BrakeInput = 1;
            if (DoFrontWheelBrakes)
            {
                for (int i = 0; i < BrakeWheels_Front.Length; i++)
                {
                    BrakeWheels_Front[i].SetProgramVariable(Brake_VariableName, 1f * Brake_FrontStrengthMulti);
                }
            }
            if (DoBackWheelBrakes)
            {
                for (int i = 0; i < BrakeWheels_Back.Length; i++)
                {
                    BrakeWheels_Back[i].SetProgramVariable(Brake_VariableName, 1f * Brake_BackStrengthMulti);
                }
            }
        }
        public void SFEXT_O_TakeOwnership()
        {
            IsOwner = true;
            if (EnableBrakeOnExit && !Piloting)
            {
                gameObject.SetActive(true);
                SetBrakeOne();
            }
        }
        public void SFEXT_O_LoseOwnership()
        {
            if (!Occupied)
            {
                gameObject.SetActive(false);
            }
            IsOwner = false;
        }
        //Don't enable the brake if the car is using it's grapple when you exit to make towing easier
        private bool GrappleActive;
        private int NumGrapplesActive;
        public void SFEXT_G_GrappleInactive()
        {
            NumGrapplesActive--;
            if (NumGrapplesActive == 0)
            { GrappleActive = false; }
        }
        public void SFEXT_G_GrappleActive()
        {
            NumGrapplesActive++;
            GrappleActive = true;
        }
    }
}