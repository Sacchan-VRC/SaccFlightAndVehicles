
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DFUNC_AltHold : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public GameObject HudHold;
        public GameObject Dial_Funcon;
        [Tooltip("Limit Gs that can be pulled by the altitude hold auto pilot")]
        public float GLimiter = 12f;
        [Tooltip("Limit AoA that can be pulled by the altitude hold auto pilot")]
        public float AoALimiter = 15f;
        public float AltHoldPitchProportional = 1f;
        public float AltHoldPitchIntegral = 1f;
        private float AltHoldPitchIntegrator;
        public float AltHoldPitchDerivative = .1f;
        private float AltHoldPitchlastframeerror;
        public float AltHoldRollProportional = .01f;
        public float AltHoldRollDerivative = .1f;
        private float AltHoldRolllastframeerror;
        public float AltHoldYawProportional = 15f;
        public float AltHoldYawDerivative = .1f;
        private float AltHoldYawlastframeerror;
        [Header("Cruise = Helicopter Throttle")]
        public bool HelicopterMode;
        public float CruiseProportional = .1f;
        public float CruiseIntegral = .1f;
        private float CruiseIntegrator;
        private float CruiseIntegratorMax = 5;
        private float CruiseIntegratorMin = -5;
        public float CruiseDerivative = .6f;
        private float SetSpeed;
        private bool Cruise;
        private bool CruiseThrottleOverridden;
        private float Cruiselastframeerror;
        public float AutoHoverStrengthPitch = 5f;
        public float AutoHoverMaxPitch = 10f;
        public float AutoHoverStrengthRoll = 5f;
        public float AutoHoverMaxRoll = 10f;
        [Tooltip("Speed at which the auto hover Angle stops increasing, forward/back = pitch")]
        public float AutoHoverMaxAngleSpeedPitch = 20f;
        [Tooltip("Speed at which the auto hover Angle stops increasing, lateral = yaw")]
        public float AutoHoverMaxAngleSpeedRoll = 20f;
        private SaccEntity EntityControl;
        private bool UseLeftTrigger = false;
        private bool TriggerLastFrame;
        [System.NonSerializedAttribute] public bool AltHold;
        private Rigidbody VehicleRigidbody;
        private Transform VehicleTransform;
        private Vector3 RotationInputs;
        private bool EngineOn;
        private bool IsOwner;
        private bool InVR;
        private bool Selected;
        private bool JoyStickOveridden;
        private bool StickHeld;
        private bool Piloting;
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        public void SFEXT_L_EntityStart()
        {
            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            if (localPlayer != null)
            { InVR = localPlayer.IsUserInVR(); }
            EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
            VehicleRigidbody = (Rigidbody)SAVControl.GetProgramVariable("VehicleRigidbody");
            VehicleTransform = EntityControl.transform;
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
            IsOwner = (bool)SAVControl.GetProgramVariable("IsOwner");
        }
        public void DFUNC_Selected()
        {
            TriggerLastFrame = true;
            gameObject.SetActive(true);
            Selected = true;
        }
        public void DFUNC_Deselected()
        {
            if (!AltHold) { gameObject.SetActive(false); }
            Selected = false;
        }
        public void SFEXT_O_PilotEnter()
        {
            Piloting = true;
            if (!AltHold) { gameObject.SetActive(false); }
            if (Dial_Funcon) Dial_Funcon.SetActive(AltHold);
        }
        public void SFEXT_O_PilotExit()
        {
            Piloting = false;
            Selected = false;
            StickHeld = false;
        }
        public void SFEXT_L_PassengerEnter()
        {
            if (Dial_Funcon) Dial_Funcon.SetActive(AltHold);
        }
        public void SFEXT_G_EngineOn()
        {
            EngineOn = true;
        }
        public void SFEXT_G_EngineOff()
        {
            gameObject.SetActive(false);
            Selected = false;
            EngineOn = false;
            if (AltHold)
            { DeactivateAltHold(); }
        }
        public void SFEXT_G_TouchDown()
        {
            if (AltHold)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(DeactivateAltHold)); }
        }
        public void SFEXT_O_EnterVTOL()
        {
            if (AltHold)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(DeactivateAltHold)); }
        }
        public void SFEXT_O_OnPlayerJoined()
        {
            if (AltHold)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ActivateAltHold));
            }
        }
        public void SFEXT_O_JoystickGrabbed()
        {
            AltHoldPitchIntegrator = 0;
            StickHeld = true;
            if (JoyStickOveridden)
            {
                SAVControl.SetProgramVariable("JoystickOverridden", (int)SAVControl.GetProgramVariable("JoystickOverridden") - 1);
                JoyStickOveridden = false;
            }
        }
        public void SFEXT_O_JoystickDropped()
        {
            AltHoldPitchIntegrator = 0;
            StickHeld = false;
            if (!JoyStickOveridden && AltHold)
            {
                SAVControl.SetProgramVariable("JoystickOverridden", (int)SAVControl.GetProgramVariable("JoystickOverridden") + 1);
                JoyStickOveridden = true;
            }
        }
        public void ActivateAltHold()
        {
            if (AltHold) { return; }
            AltHold = true;
            if (!JoyStickOveridden && !StickHeld)
            {
                SAVControl.SetProgramVariable("JoystickOverridden", (int)SAVControl.GetProgramVariable("JoystickOverridden") + 1);
                JoyStickOveridden = true;
            }
            if (Dial_Funcon) { Dial_Funcon.SetActive(AltHold); }
            if (HudHold) { HudHold.SetActive(AltHold); }
            EntityControl.SendEventToExtensions("SFEXT_G_AltHoldOn");
            if (HelicopterMode)
            { SetCruiseOn(); }
        }
        public void DeactivateAltHold()
        {
            if (!AltHold) { return; }
            if (!InVR || !Selected) { gameObject.SetActive(false); }
            AltHold = false;
            if (Dial_Funcon) { Dial_Funcon.SetActive(AltHold); }
            if (HudHold) { HudHold.SetActive(AltHold); }
            if (JoyStickOveridden)
            {
                SAVControl.SetProgramVariable("JoystickOverridden", (int)SAVControl.GetProgramVariable("JoystickOverridden") - 1);
                JoyStickOveridden = false;
            }
            SAVControl.SetProgramVariable("JoystickOverride", Vector3.zero);
            RotationInputs = Vector3.zero;
            AltHoldPitchIntegrator = 0;
            EntityControl.SendEventToExtensions("SFEXT_G_AltHoldOff");
            if (HelicopterMode)
            { SetCruiseOff(); }
        }
        private void FixedUpdate()
        {
            if (Selected)
            {
                if (InVR)
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
                            ToggleAltHold();
                        }
                        TriggerLastFrame = true;
                    }
                    else { TriggerLastFrame = false; }
                }
            }

            if (AltHold && IsOwner)
            {
                float DeltaTime = Time.deltaTime;
                Vector3 localAngularVelocity = VehicleTransform.InverseTransformDirection(VehicleRigidbody.angularVelocity);
                Vector3 localVelocity = VehicleTransform.InverseTransformDirection(VehicleRigidbody.velocity);
                //Altitude hold PID Controller

                int upsidedown = Vector3.Dot(Vector3.up, VehicleTransform.up) > 0 ? 1 : -1;
                float error;
                if (HelicopterMode)
                {
                    float MovementErrorz = Mathf.Clamp(Mathf.Clamp(localVelocity.z * AutoHoverStrengthPitch, -AutoHoverMaxAngleSpeedPitch, AutoHoverMaxAngleSpeedPitch), -AutoHoverMaxPitch, AutoHoverMaxPitch);
                    error = Vector3.SignedAngle(VehicleTransform.forward, Vector3.ProjectOnPlane(VehicleTransform.forward, Vector3.up), VehicleTransform.right) - MovementErrorz;
                }
                else
                {
                    error = ((Vector3)SAVControl.GetProgramVariable("CurrentVel")).normalized.y - (localAngularVelocity.x * upsidedown * 2.5f);
                }

                AltHoldPitchIntegrator += error * DeltaTime;
                //AltHoldPitchIntegrator = Mathf.Clamp(AltHoldPitchIntegrator, AltHoldPitchIntegratorMin, AltHoldPitchIntegratorMax);
                float AltHoldPitchDerivator = (error - AltHoldPitchlastframeerror) / DeltaTime;
                AltHoldPitchlastframeerror = error;
                RotationInputs.x = AltHoldPitchProportional * error;
                RotationInputs.x += AltHoldPitchIntegral * AltHoldPitchIntegrator;
                RotationInputs.x += AltHoldPitchDerivative * AltHoldPitchDerivator; //works but spazzes out real bad
                RotationInputs.x = Mathf.Clamp(RotationInputs.x, -1, 1);

                //Roll
                float errorRoll = VehicleTransform.localEulerAngles.z;
                if (errorRoll > 180) { errorRoll -= 360; }

                //lock upside down if rotated more than 90
                if (errorRoll > 90)
                {
                    errorRoll -= 180;
                    RotationInputs.x *= -1;
                }
                else if (errorRoll < -90)
                {
                    errorRoll += 180;
                    RotationInputs.x *= -1;
                }
                if (HelicopterMode)
                {
                    errorRoll -= Mathf.Clamp(Mathf.Clamp(localVelocity.x, -AutoHoverMaxAngleSpeedRoll, AutoHoverMaxAngleSpeedRoll) * AutoHoverStrengthRoll, -AutoHoverMaxRoll, AutoHoverMaxRoll);
                }
                errorRoll = -errorRoll;
                float AltHoldRollDerivator = (errorRoll - AltHoldRolllastframeerror) / DeltaTime;
                AltHoldRolllastframeerror = errorRoll;
                RotationInputs.z = Mathf.Clamp((AltHoldRollProportional * errorRoll) + (AltHoldRollDerivator * AltHoldRollDerivative), -1, 1);

                //YAW
                float errorYaw = -localAngularVelocity.y;
                float AltHoldYawDerivator = (errorYaw - AltHoldYawlastframeerror) / DeltaTime;
                AltHoldYawlastframeerror = errorYaw;
                RotationInputs.y = Mathf.Clamp((AltHoldYawProportional * errorYaw) + (AltHoldYawDerivator * AltHoldYawDerivative), -1, 1);

                //flight limit internally enabled when alt hold is enabled
                //old flight limit implemenation
                if (!HelicopterMode)
                {
                    float GLimitStrength = Mathf.Clamp(-((float)(SAVControl.GetProgramVariable("VertGs")) / GLimiter) + 1, 0, 1);
                    float AoALimitStrength = Mathf.Clamp(-(Mathf.Abs((float)SAVControl.GetProgramVariable("AngleOfAttack")) / AoALimiter) + 1, 0, 1);
                    float Limits = Mathf.Min(GLimitStrength, AoALimitStrength);
                    RotationInputs.x *= Limits;
                }

                SAVControl.SetProgramVariable("JoystickOverride", RotationInputs);
                if (HelicopterMode)
                {
                    if (EngineOn)
                    {
                        if (!InVR && Piloting)
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
                        SetSpeed = 0;

                        float errorT = (SetSpeed - ((Vector3)SAVControl.GetProgramVariable("CurrentVel")).y);

                        CruiseIntegrator += errorT * DeltaTime;
                        CruiseIntegrator = Mathf.Clamp(CruiseIntegrator, CruiseIntegratorMin, CruiseIntegratorMax);

                        float Derivator =/*  Mathf.Clamp(( */(errorT - Cruiselastframeerror) / DeltaTime/*) , DerivMin, DerivMax) */;
                        Cruiselastframeerror = errorT;
                        SAVControl.SetProgramVariable("ThrottleOverride", Mathf.Clamp((CruiseProportional * errorT) + (CruiseIntegral * CruiseIntegrator) + (CruiseDerivative * Derivator), 0, 1));
                    }
                }
            }
        }
        public void SetCruiseOn()
        {
            if (Cruise) { return; }
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
            if (CruiseThrottleOverridden)
            {
                SAVControl.SetProgramVariable("ThrottleOverridden", (int)SAVControl.GetProgramVariable("ThrottleOverridden") - 1);
                CruiseThrottleOverridden = false;
            }
            SAVControl.SetProgramVariable("PlayerThrottle", (float)SAVControl.GetProgramVariable("ThrottleInput"));
            Cruise = false;
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
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
        public void KeyboardInput()
        {
            ToggleAltHold();
        }
        private void ToggleAltHold()
        {
            if (AltHold)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(DeactivateAltHold));
            }
            else
            {
                if (((bool)SAVControl.GetProgramVariable("InVTOL") && !HelicopterMode) || (bool)SAVControl.GetProgramVariable("Taxiing") || !EngineOn) { return; }
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ActivateAltHold));
                gameObject.SetActive(true);
            }
        }
        public void SFEXT_G_Explode()
        {
            gameObject.SetActive(false);
        }
        public void SFEXT_O_TakeOwnership()
        {
            IsOwner = true;
            if (AltHold)
            { gameObject.SetActive(true); }
        }
        public void SFEXT_O_LoseOwnership()
        {
            IsOwner = false;
            gameObject.SetActive(false);
        }
    }
}