
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_Cruise : UdonSharpBehaviour
    {
        [SerializeField] UdonSharpBehaviour SAVControl;
        [Tooltip("Object enabled when function is active (used on MFD)")]
        public GameObject Dial_Funcon;
        public bool AllowCruiseGrounded;
        public Text HUDText_knotstarget;
        [Tooltip("Conversion from meters/s")]
        [SerializeField] private float speedMultiplier = 1.9438445f;
        [Header("PID Controller:")]
        public float CruiseProportional = .13f;
        public float CruiseIntegral = .1f;
        public float CruiseIntegrator_Max = 10;
        public float CruiseIntegrator_Min = -10;
        public float Derivative = 0.1f;
        public float DerivMax = 0f;
        public float DerivMin = -1000f;
        [Header("Debug:")]
        public float CruiseIntegrator;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [System.NonSerializedAttribute] public SAV_PassengerFunctionsController PassengerFunctionsControl;
        private bool TriggerLastFrame;
        private Transform VehicleTransform;
        private VRCPlayerApi localPlayer;
        private float CruiseTemp;
        private float SpeedZeroPoint;
        private float TriggerTapTime = 0;
        [System.NonSerializedAttribute] public bool Cruise;
        private float Cruiselastframeerror;
        private bool func_active;
        private bool Selected;
        private bool Piloting;
        private bool EngineOn;
        private bool InVTOL;
        private bool InReverse;
        private bool InVR;
        private bool CruiseThrottleOverridden;
        private Transform ControlsRoot;
        [System.NonSerializedAttribute] public float SetSpeed;
        private Rigidbody VehicleRigidbody;
        public void SFEXT_L_EntityStart()
        {
            localPlayer = Networking.LocalPlayer;
            if (localPlayer != null)
            { InVR = localPlayer.IsUserInVR(); }
            ControlsRoot = (Transform)SAVControl.GetProgramVariable("ControlsRoot");
            VehicleRigidbody = (Rigidbody)SAVControl.GetProgramVariable("VehicleRigidbody");
            if (Dial_Funcon) Dial_Funcon.SetActive(false);
        }
        public void DFUNC_Selected()
        {
            TriggerLastFrame = true;
            gameObject.SetActive(true);
            Selected = true;
        }
        public void DFUNC_Deselected()
        {
            if (!Cruise) { gameObject.SetActive(false); }
            Selected = false;
        }
        public void SFEXT_O_PilotEnter()
        {
            if (Dial_Funcon) Dial_Funcon.SetActive(Cruise);
            Piloting = true;
            if (HUDText_knotstarget) { HUDText_knotstarget.text = string.Empty; }
        }
        public void SFEXT_P_PassengerEnter()
        {
            if (Dial_Funcon) Dial_Funcon.SetActive(Cruise);
            if (HUDText_knotstarget) { HUDText_knotstarget.text = string.Empty; }
        }
        public void SFEXT_O_PilotExit()
        {
            Piloting = false;
            Selected = false;
        }
        public void SFEXT_G_EngineOn()
        {
            EngineOn = true;
        }
        public void SFEXT_G_EngineOff()
        {
            EngineOn = false;
            gameObject.SetActive(false);
            func_active = false;
            if (Cruise)
            {
                SetCruiseOff();
            }
        }
        public void SFEXT_G_Explode()
        {
            if (Cruise)
            { SetCruiseOff(); }
        }
        public void SFEXT_G_TouchDown()
        {
            if (Cruise && !AllowCruiseGrounded)
            { SetCruiseOff(); }
        }
        public void SFEXT_O_EnterVTOL()
        {
            if (Cruise)
            { SetCruiseOff(); }
            InVTOL = true;
        }
        public void SFEXT_O_ExitVTOL()
        { InVTOL = false; }
        public void SFEXT_O_StartReversing()
        {
            if (Cruise)
            { SetCruiseOff(); }
            InReverse = true;
        }
        public void SFEXT_O_StopReversing()
        { InReverse = false; }
        private void LateUpdate()
        {
            if (EngineOn)
            {
                if (InVR)
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
                            //for setting speed in VR
                            Vector3 handpos = ControlsRoot.position -
                            (LeftDial
                            ? localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position
                            : localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position);
                            handpos = ControlsRoot.InverseTransformDirection(handpos);

                            //enable and disable
                            if (!TriggerLastFrame)
                            {
                                if (!Cruise)
                                {
                                    if ((!(bool)SAVControl.GetProgramVariable("Taxiing") || AllowCruiseGrounded) && !InVTOL && !InReverse)
                                    { SetCruiseOn(); }
                                }
                                if (Time.time - TriggerTapTime < .4f)//double tap detected, turn off cruise
                                {
                                    SetCruiseOff();
                                }
                                SpeedZeroPoint = handpos.z;
                                CruiseTemp = SetSpeed;
                                TriggerTapTime = Time.time;
                            }
                            float SpeedDifference = (SpeedZeroPoint - handpos.z) * 250;
                            SetSpeed = Mathf.Max(CruiseTemp + SpeedDifference, 0);

                            TriggerLastFrame = true;
                        }
                        else { TriggerLastFrame = false; }
                    }
                }
                else
                {
                    bool ShiftCtrl = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftControl);
                    if (ShiftCtrl)
                    {
                        if (CruiseThrottleOverridden)
                        {
                            SAVControl.SetProgramVariable("ThrottleOverridden", (int)SAVControl.GetProgramVariable("ThrottleOverridden") - 1);
                            CruiseThrottleOverridden = false;
                        }
                    }
                    else
                    {
                        if (Cruise)
                        {
                            if (!CruiseThrottleOverridden)
                            {
                                SAVControl.SetProgramVariable("ThrottleOverridden", (int)SAVControl.GetProgramVariable("ThrottleOverridden") + 1);
                                CruiseThrottleOverridden = true;
                            }
                        }
                    }
                }
                float DeltaTime = Time.deltaTime;
                float equals = Input.GetKey(KeyCode.Equals) ? DeltaTime * 10 : 0;
                float minus = Input.GetKey(KeyCode.Minus) ? DeltaTime * 10 : 0;
                float shiftF = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? 3f : 1f;
                SetSpeed = Mathf.Max(SetSpeed + ((equals - minus) * shiftF), 0);
            }
            if (Cruise)
            {
                if (HUDText_knotstarget) { HUDText_knotstarget.text = (SetSpeed * speedMultiplier).ToString("F0"); }
            }
        }
        float lastframeerror;
        void FixedUpdate()
        {
            if (func_active)
            {
                float dt = Time.fixedDeltaTime;
                float error = SetSpeed - VehicleRigidbody.velocity.magnitude;

                CruiseIntegrator += error * dt;
                CruiseIntegrator = Mathf.Clamp(CruiseIntegrator, CruiseIntegrator_Min, CruiseIntegrator_Max);

                float Derivator = Mathf.Clamp((error - lastframeerror) / dt, DerivMin, DerivMax);
                lastframeerror = error;

                SAVControl.SetProgramVariable("ThrottleOverride", Mathf.Clamp((CruiseProportional * error) + (CruiseIntegral * CruiseIntegrator) + (Derivative * Derivator), 0, 1));
            }
        }
        public void KeyboardInput()
        {
            if (!Cruise)
            {
                if ((!(bool)SAVControl.GetProgramVariable("Taxiing") || AllowCruiseGrounded) && !InVTOL && !InReverse)
                { SetCruiseOn(); }
            }
            else
            {
                SetCruiseOff();
            }
        }
        public void SetCruiseOn()
        {
            if (Cruise) { return; }
            CruiseIntegrator = 0f;
            if (Piloting)
            {
                func_active = true;
                if (!InVR)
                { gameObject.SetActive(true); }
            }
            if (!CruiseThrottleOverridden)
            {
                SAVControl.SetProgramVariable("ThrottleOverridden", (int)SAVControl.GetProgramVariable("ThrottleOverridden") + 1);
                CruiseThrottleOverridden = true;
            }
            SetSpeed = (float)SAVControl.GetProgramVariable("AirSpeed");
            Cruise = true;
            if (Dial_Funcon) { Dial_Funcon.SetActive(true); }
            EntityControl.SendEventToExtensions("SFEXT_O_CruiseEnabled");
        }
        public void SetCruiseOff()
        {
            if (!Cruise) { return; }
            if (Piloting)
            {
                func_active = false;
                if (!InVR)
                { gameObject.SetActive(false); }
            }
            if (CruiseThrottleOverridden)
            {
                SAVControl.SetProgramVariable("ThrottleOverridden", (int)SAVControl.GetProgramVariable("ThrottleOverridden") - 1);
                CruiseThrottleOverridden = false;
            }
            SAVControl.SetProgramVariable("PlayerThrottle", (float)SAVControl.GetProgramVariable("ThrottleInput"));
            Cruise = false;
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
            if (HUDText_knotstarget) { HUDText_knotstarget.text = string.Empty; }
            EntityControl.SendEventToExtensions("SFEXT_O_CruiseDisabled");
        }
        public void SFEXT_O_ThrottleDropped()
        {
            if (!CruiseThrottleOverridden && Cruise)
            {
                SAVControl.SetProgramVariable("ThrottleOverridden", (int)SAVControl.GetProgramVariable("ThrottleOverridden") + 1);
                CruiseThrottleOverridden = true;
            }
        }
        public void SFEXT_O_ThrottleGrabbed()
        {
            if (CruiseThrottleOverridden)
            {
                SAVControl.SetProgramVariable("ThrottleOverridden", (int)SAVControl.GetProgramVariable("ThrottleOverridden") - 1);
                CruiseThrottleOverridden = false;
            }
        }
        public void SFEXT_O_LoseOwnership()
        {
            gameObject.SetActive(false);
            func_active = false;
            if (Cruise)
            { SetCruiseOff(); }
        }
    }
}