﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

//this script is a lazy copy of vtolangle
namespace SaccFlightAndVehicles
{
    public class DFUNC_SlideAnimation : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public float SyncUpdateRate = .25f;
        [SerializeField] KeyCode AnimUpKey = KeyCode.PageUp;
        [SerializeField] KeyCode AnimDownKey = KeyCode.PageDown;
        [System.NonSerializedAttribute, UdonSynced] public float AnimValue;
        [SerializeField] Animator VehicleAnimator;
        [SerializeField] string AnimFloatName;
        private float AnimLastValue;
        [SerializeField] float AnimDefaultValue;
        [SerializeField] private float AnimMoveSpeed = 1f;
        [SerializeField] private bool ResetOnEnter = false;
        private Transform ControlsRoot;
        private VRCPlayerApi localPlayer;
        private bool TriggerLastFrame;
        private bool IsOwner;
        private bool InVR;
        private bool Selected;
        private bool UpdatingVar;
        private bool UpdatingVR;
        private bool UpdatingKeyb;
        private float NewAnimValue;
        private float AnimMover;
        private float UpdateTime;
        [SerializeField] bool LoopingAnimation;
        private float AnimValueInput;
        private float AnimTemp;
        private float AnimZeroPoint;
        private float ThrottleSensitivity;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [System.NonSerializedAttribute] public SAV_PassengerFunctionsController PassengerFunctionsControl;
        public void SFEXT_L_EntityStart()
        {
            ControlsRoot = (Transform)SAVControl.GetProgramVariable("ControlsRoot");
            localPlayer = Networking.LocalPlayer;
            ThrottleSensitivity = (float)SAVControl.GetProgramVariable("ThrottleSensitivity");
            InVR = EntityControl.InVR;
            IsOwner = EntityControl.IsOwner;
            AnimMover = AnimLastValue = NewAnimValue = AnimValue;
        }
        public void SFEXT_O_PilotEnter()
        {
            TriggerLastFrame = false;
            InVR = EntityControl.InVR;
            gameObject.SetActive(true);
            AnimMover = AnimLastValue = NewAnimValue = AnimValue;
            if (ResetOnEnter) { ResetToDefault(); }
            RequestSerialization();
        }
        public void SFEXT_G_PilotEnter()
        {
            gameObject.SetActive(true);
        }
        public void SFEXT_G_PilotExit()
        {
            gameObject.SetActive(false);
        }
        public void DFUNC_Selected()
        {
            Selected = true;
        }
        public void DFUNC_Deselected()
        {
            Selected = false;
            RequestSerialization();
        }
        private void LateUpdate()
        {
            if (IsOwner)
            {
                if (!InVR || Selected)
                {
                    float pgup = Input.GetKey(AnimUpKey) ? 1 : 0;
                    float pgdn = Input.GetKey(AnimDownKey) ? 1 : 0;
                    if (pgup + pgdn != 0)
                    {
                        UpdatingKeyb = true;
                        AnimValueInput = AnimValueInput + ((pgdn - pgup) * (AnimMoveSpeed * Time.deltaTime));
                    }
                    float Trigger;
                    if (LeftDial)
                    { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                    else
                    { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
                    if (Trigger > 0.75)
                    {
                        UpdatingVR = true;
                        Vector3 handpos;
                        if (LeftDial)
                        { handpos = ControlsRoot.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position; }
                        else
                        { handpos = ControlsRoot.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position; }
                        handpos = ControlsRoot.InverseTransformDirection(handpos);

                        if (!TriggerLastFrame)
                        {
                            AnimZeroPoint = handpos.z;
                            AnimTemp = AnimValue;
                        }
                        float newAnim = AnimTemp + ((AnimZeroPoint - handpos.z) * -ThrottleSensitivity);
                        AnimValueInput = newAnim;

                        TriggerLastFrame = true;
                    }
                    else { TriggerLastFrame = false; }

                    if (UpdatingVR || UpdatingKeyb)
                    {
                        if (!UpdatingVar) { UpdateTime = Time.time; }//prevent a tiny adjustment being sent that causes the movement to stutter 
                        if (Time.time - UpdateTime > SyncUpdateRate)
                        {
                            RequestSerialization();
                            UpdateTime = Time.time;
                        }
                        UpdatingVar = true;
                    }
                    else
                    {
                        if (UpdatingVar)
                        {
                            RequestSerialization();//make sure others recieve final position after finished adjusting
                            UpdateTime = Time.time;
                            UpdatingVar = false;
                        }
                    }
                }
            }
            else
            {
                if (LoopingAnimation)
                {
                    if (NewAnimValue >= 0)
                    { NewAnimValue = NewAnimValue - Mathf.Floor(NewAnimValue); }
                    else
                    {
                        float AbsIn = Mathf.Abs(NewAnimValue);
                        NewAnimValue = 1 - (AbsIn - Mathf.Floor(AbsIn));
                    }
                    //set value above or below current AnimValue to make it interpolate in the shortest direction
                    if (AnimMover > NewAnimValue)
                    {
                        if (Mathf.Abs(AnimMover - NewAnimValue) > .5f)
                        { NewAnimValue += 1; }
                    }
                    else
                    {
                        if (Mathf.Abs(AnimMover - NewAnimValue) > .5f)
                        { NewAnimValue -= 1; }
                    }
                    AnimMover = Mathf.MoveTowards(AnimMover, NewAnimValue, AnimMoveSpeed * Time.deltaTime);
                    if (AnimMover < 0) { AnimMover++; }
                    else if (AnimMover > 1) { AnimMover--; }
                }
                else
                {
                    AnimMover = Mathf.MoveTowards(AnimMover, NewAnimValue, AnimMoveSpeed * Time.deltaTime);
                }
                AnimValue = AnimMover;
            }
            SetVTOLRotValues();
        }
        private void SetVTOLRotValues()
        {
            if (LoopingAnimation)
            {
                //handle interpolations from 0.99 to 0.01 properly
                //set value to between 0 and 1
                if (AnimValueInput >= 0)
                { AnimValueInput = AnimValueInput - Mathf.Floor(AnimValueInput); }
                else
                {
                    float AbsIn = Mathf.Abs(AnimValueInput);
                    AnimValueInput = 1 - (AbsIn - Mathf.Floor(AbsIn));
                }
                //set value above or below current AnimValue to make it interpolate in the shortest direction
                if (AnimValue > AnimValueInput)
                {
                    if (Mathf.Abs(AnimValue - AnimValueInput) > .5f)
                    { AnimValueInput += 1; }
                }
                else
                {
                    if (Mathf.Abs(AnimValue - AnimValueInput) > .5f)
                    { AnimValueInput -= 1; }
                }
            }
            else
            {
                AnimValueInput = Mathf.Clamp(AnimValueInput, 0, 1);
            }
            AnimValue = Mathf.MoveTowards(AnimValue, AnimValueInput, AnimMoveSpeed * Time.deltaTime);
            if (AnimValue < 0) { AnimValue++; }
            else if (AnimValue > 1) { AnimValue--; }
            VehicleAnimator.SetFloat(AnimFloatName, AnimValue);
        }
        public override void OnDeserialization()
        {
            if (!IsOwner)
            {
                //extrapolate AnimValue and lerp towards it to smooth out movement
                if (AnimValue != AnimLastValue)
                {
                    NewAnimValue = AnimValue;
                    if (LoopingAnimation)
                    {
                        if (Mathf.Abs(AnimValue - AnimLastValue) > .5f)
                        {
                            if (AnimValue > AnimLastValue)
                            {
                                NewAnimValue++;
                            }
                            else
                            {
                                NewAnimValue--;
                            }
                        }
                    }
                    AnimLastValue = AnimValue;
                }
            }
        }
        public void SFEXT_G_Explode()
        {
            ResetToDefault();
        }
        public void SFEXT_G_RespawnButton()
        {
            ResetToDefault();
        }
        void ResetToDefault()
        {
            AnimMover = AnimLastValue = AnimValue = NewAnimValue = AnimDefaultValue;
            AnimValue = AnimDefaultValue;
        }
        public void SFEXT_O_TakeOwnership()
        {
            IsOwner = true;
            AnimMover = AnimLastValue = AnimValue = NewAnimValue;
        }
        public void SFEXT_O_LoseOwnership()
        {
            IsOwner = false;
            NewAnimValue = AnimMover = AnimLastValue = AnimValue;
        }
    }
}