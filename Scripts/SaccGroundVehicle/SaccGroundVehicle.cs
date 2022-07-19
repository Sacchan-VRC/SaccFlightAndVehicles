
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [DefaultExecutionOrder(1400)]//before wheels
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class SaccGroundVehicle : UdonSharpBehaviour
    {
        public SaccEntity EntityControl;
        [Tooltip("The object containing all non-trigger colliders for the vehicle, their layers are changed when entering and exiting")]
        public Transform VehicleMesh;
        [Tooltip("Largest renderer on the vehicle, for optimization purposes, checking if visible")]
        public Renderer MainObjectRenderer;
        [Tooltip("Change all children of VehicleMesh, or just the objects with colliders?")]
        public bool OnlyChangeColliders = false;
        [Tooltip("Layer to set the VehicleMesh and it's children to when entering vehicle")]
        public int OnboardVehicleLayer = 31;
        [UdonSynced] public float Health = 73f;
        public Animator VehicleAnimator;
        [System.NonSerialized] public Transform VehicleTransform;
        [System.NonSerialized] public Rigidbody VehicleRigidbody;
        [Tooltip("List of wheels to send Engine values to and from")]
        public UdonSharpBehaviour[] DriveWheels;
        [Tooltip("Wheels to get the 'Grounded' value from for autosteering")]
        public UdonSharpBehaviour[] SteerWheels;
        [Tooltip("All of the rest of the wheels")]
        public UdonSharpBehaviour[] OtherWheels;
        //public Transform[] DriveWheelsTrans;
        //public sustest[] SteeringWheels;
        //public Transform[] SteeringWheelsTrans;
        [Tooltip("How many revs are added when accelerating")]
        public float DriveSpeed;
        [Tooltip("How many revs are taken away all the time")]
        public float EngineSlowDown = .75f;
        [Tooltip("Throttle % that is applied when not touching the controls")]
        public float MinThrottle = .08f;
        [Tooltip("Amount of max DriveSpeed that keyboard users have access to, to stop them spinning out")]
        public float DriveSpeedKeyboardMax = 1f;
        //public float SteerAngle;
        //public float CurrentSteerAngle;
        /*  [UdonSynced(UdonSyncMode.None)]  */

        [Tooltip("How far down you have to push the grip button to grab the joystick and throttle")]
        public float GripSensitivity = .75f;
        [Tooltip("How many degrees the wheel can turn away from neutral position (lock to lock / 2), animation should match this")]
        public float SteeringWheelDegrees = 450f;
        // [Tooltip("How much VR users must twist their hands to reach max throttle, animation should match this")]
        // public float ThrottleDegrees = 50f;
        [Tooltip("How long keyboard turning must be held down to reach full deflection")]
        public float SteeringKeyboardSecsToMax = 0.22222222f;

        [Tooltip("Reduce desktop max steering linearly up to this speed M/s")]
        public float SteeringMaxSpeedDT = 40f;
        [Tooltip("Disable the above feature")]
        public bool SteeringMaxSpeedDTDisabled = false;
        [Tooltip("Steering is reduced but to a minimum of this value")]
        public float DesktopMinSteering = .2f;
        [Tooltip("how fast steering wheel returns to neutral position in destop mode 1 = 1 second, .2 = 5 seconds")]
        public float ThrottleReturnTimeDT = .0001f;
        // [Tooltip("how fast steering wheel returns to neutral position in VR 1 = 1 second, .2 = 5 seconds")]
        // public float ThrottleReturnTimeVR = .1f;
        public float Drag = .02f;
        [Tooltip("Transform to base the pilot's throttle and joystick controls from. Used to make vertical throttle for helicopters, or if the cockpit of your vehicle can move, on transforming vehicle")]
        public Transform ControlsRoot;
        [Tooltip("Efficiency of the engine at different revs, 0=0, 1=revlimiter")]
        public AnimationCurve EngineResponseCurve = AnimationCurve.Linear(0, 1, 1, 1);
        [System.NonSerialized] public Vector3 CurrentVel;
        // [System.NonSerializedAttribute] public bool ThrottleGripLastFrame = false;
        [UdonSynced] public float Fuel = 900;
        [Tooltip("Fuel consumption per second at max revs")]
        public float FuelConsumption = 2;
        /*     [Tooltip("Amount of fuel at which throttle will start reducing")]
            [System.NonSerializedAttribute] public float LowFuel = 125; */
        //[Tooltip("Multiply how much the VR throttle moves relative to hand movement")]
        //[System.NonSerializedAttribute] public float ThrottleSensitivity = 6f;
        [Tooltip("Use the left hand trigger to control throttle?")]
        public bool SwitchHandsJoyThrottle = false;
        [Tooltip("Use the left hand grip to grab the steering wheel??")]
        public bool SteeringHand_Left = true;
        [Tooltip("Use the right hand grip to grab the steering wheel??")]
        public bool SteeringHand_Right = true;
        [System.NonSerializedAttribute] public bool _HandBrakeOn;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(HandBrakeOn_))] public int HandBrakeOn = 0;
        public int HandBrakeOn_
        {
            set
            {
                _HandBrakeOn = value > 0;
                HandBrakeOn = value;
            }
            get => HandBrakeOn;
        }
        private Vector3 VehiclePosLastFrame;
        [Header("Engine")]
        public float RevLimiter = 8000;
        [Tooltip("Vehicle will take damage if experiences more Gs that this (Internally Gs are calculated in all directions, the HUD shows only vertical Gs so it will differ slightly")]
        public float MaxGs = 10;
        [Tooltip("Damage taken Per G above maxGs, per second.\n(Gs - MaxGs) * GDamage = damage/second")]
        public float GDamage = 10f;
        [Tooltip("Shape of the auto steer response curve, smaller number makes it sharper, around 0.5 might be realistic")]
        public bool Drift_AutoSteer;
        [Header("AutoSteer Enabled")]
        [Tooltip("Put in the max degrees the wheels can turn to in order to make autosteer work properly")]
        public float SteeringDegrees = 60;
        // public float AutoSteerCurve = 0f;
        public float AutoSteerStrength = 1f;
        [Header("AutoSteer Disabled")]
        [Tooltip("how fast steering wheel returns to neutral position in destop mode 1 = 1 second, .2 = 5 seconds")]
        public float SteeringReturnSpeedDT = .3f;
        [Tooltip("how fast steering wheel returns to neutral position in VR 1 = 1 second, .2 = 5 seconds")]
        public float SteeringReturnSpeedVR = 5f;
        [Header("Bike")]
        [Tooltip("Max roll angle of head for leaning on bike")]
        public float LeanSensitivity_Roll = 25f;
        [Tooltip("How far head has to move to lean forward/back, high number = less movement required")]
        public float LeanSensitivity_Pitch = 2.5f;
        [Header("Other")]
        [Tooltip("Time until vehicle reappears after exploding")]
        public float RespawnDelay = 10;
        [Tooltip("Time after reappearing the vehicle is invincible for")]
        public float InvincibleAfterSpawn = 2.5f;
        [Tooltip("Damage taken when hit by a bullet")]
        public float BulletDamageTaken = 10f;
        [Tooltip("Impact speed that defines a small crash")]
        public float SmallCrashSpeed = 1f;
        [Tooltip("Impact speed that defines a medium crash")]
        public float MediumCrashSpeed = 8f;
        [Tooltip("Impact speed that defines a big crash")]
        public float BigCrashSpeed = 25f;
        public bool PredictDamage = true;
        [Tooltip("Time in seconds it takes to repair fully from 0")]
        public float RepairTime = 30f;
        [Tooltip("Time in seconds it takes to refuel fully from 0")]
        public float RefuelTime = 25f;
        [Tooltip("Range at which vehicle becomes 'distant' for optimization")]
        public float DistantRange = 400f;
        [Header("Debug")]
        [UdonSynced(UdonSyncMode.Linear)] public float Revs;
        public float Clutch;
        [System.NonSerialized] public int OutsideVehicleLayer;
        public bool EnableLeaning = false;
        public float RevLimiterDelay = .04f;
        private float ThrottleNormalizer;
        public int CurrentGear = 0;
        private bool LimitingRev = false;
        public Vector3 VehicleVel;
        public float debugSpeedSteeringMulti = 0f;
        public bool InVR;
        public bool Sleeping = false;
        public bool Grounded_Steering;
        public bool Grounded;
        public float GearRatio = 0f;
        private float HandDistanceZLastFrame;
        private float VRThrottlePos;
        private float TempThrottle;
        private float ThrottleValue;
        private float GDamageToTake;
        private float ThrottleValueLastFrame;
        private Quaternion ThrottleZeroPoint;
        private bool Piloting;
        private bool Passenger;
        private float LastHitTime;
        private float PredictedHealth;
        private int ReSupplied;
        [System.NonSerializedAttribute] public float PlayerThrottle;
        [System.NonSerializedAttribute] public float VehicleSpeed;//set by syncscript if not owner
        [System.NonSerializedAttribute] public bool MovingForward;
        [System.NonSerialized] public float LastResupplyTime;
        //Quaternion VehicleRotLastFrameThrottle;
        Quaternion VehicleRotLastFrameR;
        [System.NonSerializedAttribute] public bool WheelGripLastFrameR = false;
        [System.NonSerializedAttribute] public bool WheelGrippingLastFrame_toggleR = false;
        Quaternion JoystickZeroPointR;
        Vector3 CompareAngleLastFrameR;
        private float JoystickValueLastFrameR;
        private float JoyStickValueR;
        private bool WheelGrabToggleR;
        private int WheelReleaseCountR;
        private float LastGripTimeR;
        Quaternion VehicleRotLastFrameL;
        [System.NonSerializedAttribute] public bool WheelGripLastFrameL = false;
        [System.NonSerializedAttribute] public bool WheelGrippingLastFrame_toggleL = false;
        Quaternion JoystickZeroPointL;
        Vector3 CompareAngleLastFrameL;
        private float JoystickValueLastFrameL;
        private float JoyStickValueL;
        private bool WheelGrabToggleL;
        private int WheelReleaseCountL;
        private float LastGripTimeL;
        //Vector3 CompareAngleLastFrameThrottle;
        float VRJoystickPosR = 0;
        float VRJoystickPosL = 0;
        [System.NonSerializedAttribute] public float AllGs;
        [System.NonSerializedAttribute] public Vector3 LastFrameVel = Vector3.zero;
        private float FinalThrottle;
        private Vector3 Spawnposition;
        private Quaternion Spawnrotation;
        private float AutoSteerLerper;
        [System.NonSerializedAttribute][UdonSynced(UdonSyncMode.Linear)] public float YawInput;
        [System.NonSerializedAttribute][UdonSynced(UdonSyncMode.Linear)] public float ThrottleInput;
        private VRCPlayerApi localPlayer;
        [System.NonSerializedAttribute] public bool InEditor = true;
        [System.NonSerializedAttribute] public bool Initialized = false;
        private Transform CenterOfMass;
        public float NumGroundedSteerWheels = 0;
        public float NumGroundedWheels = 0;
        public float CurrentDistance;
        public bool CurrentlyDistant = true;
        int HandsOnWheel;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(HasFuel_))] public bool HasFuel = true;
        public bool HasFuel_
        {
            set
            {
                if (value)
                {
                    EntityControl.SendEventToExtensions("SFEXT_G_HasFuel");
                }
                else
                {
                    EntityControl.SendEventToExtensions("SFEXT_G_NoFuel");
                }
                HasFuel = value;
            }
            get => HasFuel;
        }
        public void SetHasFuel() { HasFuel_ = true; }
        public void SetNoFuel() { HasFuel_ = false; }
        [System.NonSerializedAttribute] public bool _DisableInput;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(DisableInput_))] public int DisableInput = 0;
        public int DisableInput_
        {
            set
            {
                if (value > 0 && DisableInput == 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_DisableInput_Activated");
                }
                else if (value == 0 && DisableInput > 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_DisableInput_Deactivated");
                }
                _DisableInput = value > 0;
                DisableInput = value;
            }
            get => DisableInput;
        }
        public void SFEXT_L_EntityStart()
        {
            if (!Initialized) { Init(); }

            FullHealth = Health;
            FullFuel = Fuel;

            OutsideVehicleLayer = VehicleMesh.gameObject.layer;//get the layer of the vehicle as set by the world creator

            IsOwner = EntityControl.IsOwner;
            SetWheelIsOwner();
            InVR = EntityControl.InVR;
            localPlayer = Networking.LocalPlayer;
            if (localPlayer == null)
            {
                InEditor = true;
            }
            else { InEditor = false; }
            Spawnposition = VehicleTransform.localPosition;
            Spawnrotation = VehicleTransform.localRotation;
            if (!ControlsRoot)
            { ControlsRoot = VehicleTransform; }
            CenterOfMass = EntityControl.CenterOfMass;

            ThrottleNormalizer = 1 - MinThrottle;
            // SetWheelSGV();
            for (int i = 0; i < DriveWheels.Length; i++)
            {
                DriveWheels[i].SetProgramVariable("IsDriveWheel", true);
            }
            CurrentlyDistant = true;
            SendCustomEventDelayedSeconds(nameof(CheckDistance), Random.Range(5f, 7f));//dont do all vehicles on same frame
        }
        private void Init()
        {
            Initialized = true;
            VehicleRigidbody = EntityControl.gameObject.GetComponent<Rigidbody>();
            VehicleTransform = EntityControl.transform;
            OutsideVehicleLayer = VehicleMesh.gameObject.layer;//get the layer of the vehicle as set by the world creator
        }
        private void Start()// awake function when
        {
            if (!Initialized) { Init(); }
        }
        public void ReEnableRevs()
        {
            if (Revs < RevLimiter)
            {
                LimitingRev = false;
            }
            else
            {
                SendCustomEventDelayedSeconds(nameof(ReEnableRevs), RevLimiterDelay);
            }
        }
        private void LateUpdate()
        {
            float DeltaTime = Time.deltaTime;
            if (IsOwner)
            {
                if (!EntityControl.dead)
                {
                    //G/crash Damage
                    if (GDamageToTake > 0)
                    {
                        Health -= GDamageToTake * DeltaTime * GDamage;//take damage of GDamage per second per G above MaxGs
                        GDamageToTake = 0;
                    }
                    if (Health <= 0f)//vehicle is ded
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Explode));
                        return;
                    }
                }
                else { GDamageToTake = 0; }
                if (!Sleeping)
                {
                    VehicleSpeed = VehicleVel.magnitude;
                    NumGroundedWheels = 0;
                    NumGroundedSteerWheels = 0;
                    for (int i = 0; i < SteerWheels.Length; i++)
                    {
                        if ((bool)SteerWheels[i].GetProgramVariable("Grounded"))
                        {
                            NumGroundedSteerWheels++;
                        }
                    }
                    NumGroundedWheels = NumGroundedSteerWheels;
                    for (int i = 0; i < DriveWheels.Length; i++)
                    {
                        if ((bool)DriveWheels[i].GetProgramVariable("Grounded"))
                        {
                            NumGroundedWheels++;
                        }
                    }
                    for (int i = 0; i < OtherWheels.Length; i++)
                    {
                        if ((bool)OtherWheels[i].GetProgramVariable("Grounded"))
                        {
                            NumGroundedWheels++;
                        }
                    }
                    //send grounded events
                    if (NumGroundedSteerWheels > 0)
                    {
                        if (!Grounded_Steering)
                        {
                            Grounded_Steering = true;
                            EntityControl.SendEventToExtensions("SFEXT_O_SteeringGrounded");
                        }
                    }
                    else
                    {
                        if (Grounded_Steering)
                        {
                            Grounded_Steering = false;
                            EntityControl.SendEventToExtensions("SFEXT_O_SteeringAirborne");
                        }
                    }
                    if (NumGroundedWheels > 0)
                    {
                        if (!Grounded)
                        {
                            Grounded = true;
                            EntityControl.SendEventToExtensions("SFEXT_O_Grounded");
                        }
                    }
                    else
                    {
                        if (Grounded)
                        {
                            Grounded = false;
                            EntityControl.SendEventToExtensions("SFEXT_O_Airborne");
                        }
                    }
                }
                if (Piloting)
                {
                    int Wi = 0;
                    int Ai = 0;
                    int Di = 0;
                    float LGrip = 0;
                    float RGrip = 0;
                    if (!_DisableInput)
                    {
                        //inputs as ints
                        Wi = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0;
                        //int Si = Input.GetKey(KeyCode.S) ? -1 : 0;
                        Ai = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? -1 : 0;
                        Di = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0;
                        if (!InEditor)
                        {
                            LGrip = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger");
                            RGrip = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
                        }
                    }
                    //float ThrottleGrip;
                    // if (SwitchHandsJoyThrottle)
                    // { ThrottleGrip = RGrip; }
                    // else
                    // { ThrottleGrip = LGrip; }
                    if (EnableLeaning)
                    {
                        int Threei = Input.GetKey(KeyCode.Alpha3) ? -1 : 0;
                        int Ri = Input.GetKey(KeyCode.R) ? 1 : 0;
                        float VRLean = 0;
                        float VRLeanPitch = 0;
                        if (InVR)
                        {
                            Vector3 HeadLean = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.up;
                            Vector3 HeadLeanRoll = Vector3.ProjectOnPlane(HeadLean, ControlsRoot.forward);
                            VRLean = Vector3.SignedAngle(HeadLeanRoll, ControlsRoot.up, ControlsRoot.forward);
                            VRLean = Mathf.Clamp(VRLean / LeanSensitivity_Roll, -1, 1);

                            Vector3 HeadOffset = ControlsRoot.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
                            HeadOffset = ControlsRoot.InverseTransformDirection(HeadOffset);
                            /*                Vector3 HeadLeanPitch = Vector3.ProjectOnPlane(HeadLean, ControlsRoot.right);
                                           VRLeanPitch = Vector3.SignedAngle(HeadLeanPitch, ControlsRoot.up, ControlsRoot.right);
                                           VRLeanPitch = Mathf.Clamp(VRLeanPitch / 25f, -1, 1); */
                            VRLeanPitch = Mathf.Clamp(HeadOffset.z * LeanSensitivity_Pitch, -1, 1);
                        }
                        else
                        {
                            Vector3 HeadLean = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward;
                            Vector3 HeadLeanRoll = Vector3.ProjectOnPlane(HeadLean, ControlsRoot.up);
                            VRLean = Vector3.SignedAngle(HeadLeanRoll, ControlsRoot.forward, ControlsRoot.up);
                            VRLean = -Mathf.Clamp(VRLean / 25f, -1, 1);
                        }

                        VehicleAnimator.SetFloat("lean", (VRLean * .5f) + .5f);
                        VehicleAnimator.SetFloat("leanpitch", (VRLeanPitch * .5f) + .5f);
                        VehicleRigidbody.centerOfMass = transform.InverseTransformDirection(CenterOfMass.position - transform.position);//correct position if scaled}
                    }

                    ///VR Throttle
                    /*                 if (ThrottleGrip > GripSensitivity)
                                    {
                                        Quaternion VehicleRotDif = ControlsRoot.rotation * Quaternion.Inverse(VehicleRotLastFrameThrottle);//difference in vehicle's rotation since last frame
                                        VehicleRotLastFrameThrottle = ControlsRoot.rotation;
                                        ThrottleZeroPoint = VehicleRotDif * ThrottleZeroPoint;//zero point rotates with the vehicle so it appears still to the pilot
                                        if (!ThrottleGripLastFrame)//first frame you gripped Throttle
                                        {
                                            EntityControl.SendEventToExtensions("SFEXT_O_ThrottleGrabbed");
                                            VehicleRotDif = Quaternion.identity;
                                            if (SwitchHandsJoyThrottle)
                                            { ThrottleZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation; }//rotation of the controller relative to the vehicle when it was pressed
                                            else
                                            { ThrottleZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation; }
                                            ThrottleValue = -ThrottleInput * ThrottleDegrees;
                                            ThrottleValueLastFrame = 0;
                                            CompareAngleLastFrameThrottle = Vector3.up;
                                            ThrottleValueLastFrame = 0;
                                        }
                                        ThrottleGripLastFrame = true;
                                        //difference between the vehicle and the hand's rotation, and then the difference between that and the ThrottleZeroPoint
                                        Quaternion ThrottleDifference;
                                        ThrottleDifference = Quaternion.Inverse(ControlsRoot.rotation) *
                                            (SwitchHandsJoyThrottle ? localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation
                                                                    : localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation)
                                        * Quaternion.Inverse(ThrottleZeroPoint)
                                         * ControlsRoot.rotation;

                                        Vector3 ThrottlePosPitch = (ThrottleDifference * Vector3.up);
                                        Vector3 CompareAngle = Vector3.ProjectOnPlane(ThrottlePosPitch, Vector3.right);
                                        ThrottleValue += (Vector3.SignedAngle(CompareAngleLastFrameThrottle, CompareAngle, Vector3.right));
                                        CompareAngleLastFrameThrottle = CompareAngle;
                                        ThrottleValueLastFrame = ThrottleValue;
                                        VRThrottlePos = Mathf.Max(-ThrottleValue / ThrottleDegrees, 0f);
                                    }
                                    else
                                    {
                                        VRThrottlePos = 0f;
                                        if (ThrottleGripLastFrame)//first frame you let go of Throttle
                                        { EntityControl.SendEventToExtensions("SFEXT_O_ThrottleDropped"); }
                                        ThrottleGripLastFrame = false;
                                    } */
                    if (SwitchHandsJoyThrottle)
                    {
                        VRThrottlePos = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
                    }
                    else
                    {
                        VRThrottlePos = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
                    }

                    //Toggle gripping the steering wheel if double tap grab
                    HandsOnWheel = 0;
                    if (SteeringHand_Right)
                    { RHandSteeringWheel(RGrip); }
                    if (SteeringHand_Left)
                    { LHandSteeringWheel(LGrip); }

                    float VRSteerInput = 0;
                    if (HandsOnWheel > 0)
                    {
                        VRSteerInput = (VRJoystickPosL + VRJoystickPosR) / (float)HandsOnWheel;
                    }
                    else
                    {
                        AutoSteerLerper = YawInput;
                    }
                    float SteerInput = -VRSteerInput + Ai + Di;
                    //AUTOSTEER DRIFT FIX(BROKEN)
                    //float AutoSteer = Vector3.SignedAngle(Quaternion.AngleAxis(SteeringDegrees * YawInput, VehicleTransform.up) * VehicleTransform.forward, Vector3.ProjectOnPlane(VehicleVel, VehicleTransform.up), VehicleTransform.up);

                    float AutoSteer = Vector3.SignedAngle(VehicleTransform.forward, Vector3.ProjectOnPlane(VehicleVel, VehicleTransform.up), VehicleTransform.up);
                    if (Mathf.Abs(AutoSteer) > 110)
                    { AutoSteer = 0; }

                    /*     float SteerFalloff = Mathf.Abs(AutoSteer) / 90;
                        if (SteerFalloff > 1) { SteerFalloff -= 1; }
                        //Desmos: (1-\left(\left(x-.5\right)\cdot2\right)^{2}\ )^{.5}\ 
                        SteerFalloff = Mathf.Pow(1 - Mathf.Pow(((Mathf.Abs(SteerFalloff) - .5f) * 2), 2), AutoSteerCurve);
                    */
                    //Desmos: \left(\left(\sin\left(\left(x-.25\right)\cdot\pi\cdot2\right)\right)\cdot.5\right)\ +.5
                    //SteerFalloff = ((Mathf.Sin((SteerFalloff - .25f) * Mathf.PI * 2f)) * .5f) + .5f;

                    //Desmos: \sin\ x\ \cdot\pi
                    /*     if (SteerFalloff >= 1) { SteerFalloff -= 1; }
                        SteerFalloff = Mathf.Sin(SteerFalloff * Mathf.PI); */

                    /*                 if (Mathf.Abs(AutoSteer) > 90)
                                    { AutoSteer = Mathf.Clamp(-AutoSteer / SteeringDegrees, -1, 1); }
                                    else
                                    { AutoSteer = Mathf.Clamp(AutoSteer / SteeringDegrees, -1, 1); } */
                    //AUTOSTEER DRIFT FIX(BROKEN)
                    //AutoSteer += YawInput;

                    { AutoSteer = Mathf.Clamp(AutoSteer / SteeringDegrees, -1, 1); }

                    float GroundedwheelsRatio = NumGroundedWheels / SteerWheels.Length;
                    if (InVR)
                    {
                        AutoSteerLerper = Mathf.Lerp(AutoSteerLerper, AutoSteer, VehicleSpeed * AutoSteerStrength * GroundedwheelsRatio * DeltaTime);
                        float YawAddAmount = SteerInput;
                        if (Mathf.Abs(YawAddAmount) > 0f)
                        {
                            if (Drift_AutoSteer)
                            {
                                YawInput = Mathf.Clamp(AutoSteerLerper + YawAddAmount, -1f, 1f);
                            }
                            else
                            {
                                YawInput = YawAddAmount;
                            }
                        }
                        else
                        {
                            if (Drift_AutoSteer)
                            {
                                YawInput = Mathf.Lerp(YawInput, AutoSteer, VehicleSpeed * AutoSteerStrength * GroundedwheelsRatio * DeltaTime);
                            }
                            else
                            {
                                YawInput = Mathf.MoveTowards(YawInput, 0f, (1f / SteeringReturnSpeedVR) * DeltaTime);
                            }
                        }
                    }
                    else
                    {
                        float YawAddAmount = SteerInput * DeltaTime * (1f / SteeringKeyboardSecsToMax);

                        if (YawAddAmount != 0f)
                        {
                            if (SteeringMaxSpeedDTDisabled || _HandBrakeOn)//no steering limit when handbarke on
                            {
                                YawInput = Mathf.Clamp(YawInput + YawAddAmount, -1, 1);
                            }
                            else
                            {
                                float SpeedSteeringLimitUpper = 1 - (VehicleSpeed / SteeringMaxSpeedDT);
                                SpeedSteeringLimitUpper = Mathf.Clamp(SpeedSteeringLimitUpper, DesktopMinSteering, 1);
                                float SpeedSteeringLimitLower = -SpeedSteeringLimitUpper;

                                if (AutoSteer < 0)
                                {
                                    SpeedSteeringLimitLower = Mathf.Min(SpeedSteeringLimitLower, AutoSteer - DesktopMinSteering);
                                }
                                else
                                {
                                    SpeedSteeringLimitUpper = Mathf.Max(SpeedSteeringLimitUpper, AutoSteer + DesktopMinSteering);
                                }
                                YawInput = Mathf.Clamp(YawInput + YawAddAmount, SpeedSteeringLimitLower, SpeedSteeringLimitUpper);
                            }
                        }
                        else
                        {
                            if (Drift_AutoSteer)
                            { YawInput = Mathf.Lerp(YawInput, AutoSteer, VehicleSpeed * AutoSteerStrength * DeltaTime * GroundedwheelsRatio); }
                            else
                            { YawInput = Mathf.MoveTowards(YawInput, 0f, (1f / SteeringReturnSpeedDT) * DeltaTime); }
                        }
                    }
                    YawInput = Mathf.Clamp(YawInput, -1f, 1f);

                    if (InVR)
                    {
                        ThrottleInput = Mathf.Min(VRThrottlePos + Wi, 1f);
                        /*                                        else
                                           {

                                               float ReturnSpeedGrip = 1 - Mathf.Min(ThrottleGrip / GripSensitivity, 1f);
                                               ThrottleInput = Mathf.MoveTowards(ThrottleInput, 0f, ReturnSpeedGrip * (1f / ThrottleReturnTimeVR) * DeltaTime);
                                           } */
                    }
                    else
                    {
                        if (Wi != 0)
                        {
                            ThrottleInput = Mathf.Clamp(VRThrottlePos + (Wi), -DriveSpeedKeyboardMax, DriveSpeedKeyboardMax);
                        }
                        else
                        {
                            ThrottleInput = Mathf.MoveTowards(ThrottleInput, 0f, (1 / ThrottleReturnTimeDT) * DeltaTime);
                        }
                    }
                    for (int i = 0; i < DriveWheels.Length; i++)
                    {
                        DriveWheels[i].SetProgramVariable("Clutch", Clutch);
                        DriveWheels[i].SetProgramVariable("_GearRatio", GearRatio);
                    }
                    if (Fuel > 0)
                    {
                        if (!HasFuel_)
                        {
                            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetHasFuel));
                        }
                        FinalThrottle = (MinThrottle + (ThrottleInput * ThrottleNormalizer));
                    }
                    else
                    {
                        if (HasFuel_)
                        {
                            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetNoFuel));
                        }
                        FinalThrottle = 0;
                    }
                    Fuel -= Mathf.Max(FuelConsumption * Time.deltaTime * (Revs / RevLimiter), 0);
                }
                VehicleRigidbody.velocity = Vector3.Lerp(VehicleRigidbody.velocity, Vector3.zero, Drag * Time.deltaTime);
            }
            else //TODO: Move this to an effects script / Have a timer to not do it while empty for more than 10s
            {
                VehicleVel = (VehicleTransform.position - VehiclePosLastFrame) / DeltaTime;
                VehicleSpeed = VehicleVel.magnitude;
                VehiclePosLastFrame = VehicleTransform.position;
                MovingForward = Vector3.Dot(VehicleTransform.forward, VehicleVel) < 0f;
            }
        }
        private void FixedUpdate()
        {
            if (!IsOwner) { return; }
            float DeltaTime = Time.fixedDeltaTime;
            if (Piloting)
            {
                Revs = Mathf.Max(Mathf.Lerp(Revs, 0f, EngineSlowDown * DeltaTime), 0f);
                if (!LimitingRev)
                {
                    Revs += FinalThrottle * DriveSpeed * DeltaTime * EngineResponseCurve.Evaluate(Revs / RevLimiter);
                    if (Revs > RevLimiter)
                    {
                        Revs = RevLimiter;
                        LimitingRev = true;
                        SendCustomEventDelayedSeconds(nameof(ReEnableRevs), RevLimiterDelay);
                    }
                }
                for (int i = 0; i < DriveWheels.Length; i++)
                {
                    DriveWheels[i].SetProgramVariable("EngineRevs", Revs);
                }
            }
            else
            {
                Revs = Mathf.Max(Mathf.Lerp(Revs, 0f, EngineSlowDown * DeltaTime), 0f);
            }

            VehicleVel = VehicleRigidbody.velocity;
            float gravity = 9.81f * DeltaTime;
            LastFrameVel.y -= gravity; //add gravity
            AllGs = Vector3.Distance(LastFrameVel, VehicleVel) / gravity;
            GDamageToTake += Mathf.Max((AllGs - MaxGs), 0);
            LastFrameVel = VehicleVel;
        }
        public void Explode()
        {
            if (EntityControl.dead) { return; }//can happen with prediction enabled if two people kill something at the same time
            EntityControl.dead = true;
            Health = FullHealth;
            HasFuel_ = true;
            if (IsOwner)
            {
                YawInput = 0;
                ThrottleInput = 0;
                Fuel = FullFuel;
                EntityControl.SendEventToExtensions("SFEXT_O_Explode");
                SendCustomEventDelayedSeconds(nameof(MoveToSpawn), RespawnDelay - 3);
            }
            EntityControl.SendEventToExtensions("SFEXT_G_Explode");

            SendCustomEventDelayedSeconds(nameof(ReAppear), RespawnDelay);
            SendCustomEventDelayedSeconds(nameof(NotDead), RespawnDelay + InvincibleAfterSpawn);
            //pilot and passengers are dropped out of the vehicle
            if ((Piloting || Passenger) && !InEditor)
            {
                EntityControl.ExitStation();
            }
        }
        public void ReAppear()
        {
            EntityControl.SendEventToExtensions("SFEXT_G_ReAppear");
            if (IsOwner)
            {
                if (!UsingManualSync)
                {
                    VehicleRigidbody.drag = 0;
                    VehicleRigidbody.angularDrag = 0;
                }
            }
        }
        public void MoveToSpawn()
        {
            PlayerThrottle = 0;//for editor test mode
            VehicleRigidbody.angularVelocity = Vector3.zero;
            VehicleRigidbody.velocity = Vector3.zero;
            //these could get set after death by lag, probably
            Health = FullHealth;
            VehicleTransform.localPosition = Spawnposition;
            VehicleTransform.localRotation = Spawnrotation;
            EntityControl.SendEventToExtensions("SFEXT_O_MoveToSpawn");
        }
        public void NotDead()
        {
            Health = FullHealth;
            EntityControl.dead = false;
        }
        [System.NonSerializedAttribute] public float FullHealth;
        //unused variables that are just here for compatability with SAV DFuncs.
        [System.NonSerialized] public int OverrideConstantForce;
        [System.NonSerialized] public Vector3 CFRelativeForceOverride;
        [System.NonSerialized] public Vector3 CFRelativeTorqueOverride;
        [System.NonSerialized] public int DisablePhysicsAndInputs = 0;
        [System.NonSerialized] public int DisableTaxiRotation;
        [System.NonSerialized] public int DisableGroundDetection;
        [System.NonSerialized] public int ThrottleOverridden;
        [System.NonSerialized] public int JoystickOverridden;
        [System.NonSerialized] public bool Taxiing = false;
        //end of compatability variables
        [System.NonSerializedAttribute] public float FullFuel;
        [System.NonSerialized] public bool Occupied;
        [System.NonSerialized] public int NumPassengers;
        [System.NonSerializedAttribute] public bool IsOwner;
        [System.NonSerializedAttribute] public bool UsingManualSync = true;
        public void SFEXT_O_RespawnButton()//called when using respawn button
        {
            if (!Occupied && !EntityControl.dead)
            {
                Networking.SetOwner(localPlayer, EntityControl.gameObject);
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ResetStatus));
                EntityControl.TakeOwnerShipOfExtensions();
                IsOwner = true;
                Fuel = FullFuel;
                Health = FullHealth;
                YawInput = 0;
                AutoSteerLerper = 0;
                if (InEditor || UsingManualSync)
                {
                    VehicleTransform.localPosition = Spawnposition;
                    VehicleTransform.localRotation = Spawnrotation;
                    VehicleRigidbody.velocity = Vector3.zero;
                }
                VehicleRigidbody.angularVelocity = Vector3.zero;//editor needs this
            }
        }
        public void ResetStatus()//called globally when using respawn button
        {
            EntityControl.dead = true;
            SendCustomEventDelayedSeconds(nameof(NotDead), InvincibleAfterSpawn);
            EntityControl.SendEventToExtensions("SFEXT_G_RespawnButton");
        }
        public void SFEXT_O_TakeOwnership()
        {
            IsOwner = true;
            AllGs = 0;
            GDamageToTake = 0f;
            VehicleRigidbody.velocity = CurrentVel;
            LastFrameVel = CurrentVel;
            SetWheelIsOwner();
        }
        public void SFEXT_O_LoseOwnership()
        {
            VehiclePosLastFrame = VehicleTransform.position;
            IsOwner = false;
            SetWheelIsOwner();
        }
        public void SetWheelIsOwner()
        {
            for (int i = 0; i < DriveWheels.Length; i++)
            {
                DriveWheels[i].SetProgramVariable("IsOwner", IsOwner);
                if (IsOwner)
                {
                    Networking.SetOwner(Networking.LocalPlayer, DriveWheels[i].gameObject);
                }
            }
            for (int i = 0; i < SteerWheels.Length; i++)
            {
                SteerWheels[i].SetProgramVariable("IsOwner", IsOwner);
                if (IsOwner)
                {
                    Networking.SetOwner(Networking.LocalPlayer, SteerWheels[i].gameObject);
                }
            }
            for (int i = 0; i < OtherWheels.Length; i++)
            {
                OtherWheels[i].SetProgramVariable("IsOwner", IsOwner);
                if (IsOwner)
                {
                    Networking.SetOwner(Networking.LocalPlayer, OtherWheels[i].gameObject);
                }
            }
        }
        /*     public void SetWheelSGV()
            {
                for (int i = 0; i < DriveWheels.Length; i++)
                {
                    DriveWheels[i].SetProgramVariable("SGVControl", this);
                }
                for (int i = 0; i < OtherWheels.Length; i++)
                {
                    OtherWheels[i].SetProgramVariable("SGVControl", this);
                }
            } */
        public void SFEXT_G_PilotEnter()
        {
            LimitingRev = false;
            Occupied = true;
        }
        public void SFEXT_G_PilotExit()
        {
            Occupied = false;
        }
        public void SFEXT_O_PilotEnter()
        {
            Piloting = true;
            GDamageToTake = 0f;
            AllGs = 0f;
            VehicleRigidbody.velocity = CurrentVel;
            LastFrameVel = CurrentVel;
            InVR = EntityControl.InVR;
            SetCollidersLayer(OnboardVehicleLayer);
        }
        public void SFEXT_O_PilotExit()
        {
            Piloting = false;
            WheelGrippingLastFrame_toggleR = false;
            WheelReleaseCountR = 0;
            WheelGrabToggleR = false;
            for (int i = 0; i < DriveWheels.Length; i++)
            {
                DriveWheels[i].SetProgramVariable("EngineRevs", 0f);
            }
            SetCollidersLayer(OutsideVehicleLayer);
            localPlayer.SetVelocity(CurrentVel);
        }
        public void SFEXT_P_PassengerEnter()
        {
            Passenger = true;
            SetCollidersLayer(OnboardVehicleLayer);
        }
        public void SFEXT_P_PassengerExit()
        {
            Passenger = false;
            localPlayer.SetVelocity(CurrentVel);
            SetCollidersLayer(OutsideVehicleLayer);
        }
        public void SFEXT_G_PassengerEnter()
        {
            NumPassengers++;
        }
        public void SFEXT_G_PassengerExit()
        {
            NumPassengers--;
        }
        public void SFEXT_L_FallAsleep()
        {
            VehicleRigidbody.useGravity = false;
            CurrentVel = LastFrameVel = VehicleRigidbody.velocity = Vector3.zero;
            VehicleRigidbody.angularVelocity = Vector3.zero;
            VehicleSpeed = 0;
            Sleeping = true;
        }
        public void SFEXT_L_WakeUp()
        {
            VehicleRigidbody.useGravity = true;
            Sleeping = false;
        }
        public void SendBulletHit()
        {
            EntityControl.SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void SFEXT_L_BulletHit()
        {
            if (PredictDamage)
            {
                if (Time.time - LastHitTime > 2)
                {
                    PredictedHealth = Health - BulletDamageTaken;
                    if (PredictedHealth <= 0)
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Explode));
                    }
                }
                else
                {
                    PredictedHealth -= BulletDamageTaken;
                    if (PredictedHealth <= 0)
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Explode));
                    }
                }
                LastHitTime = Time.time;
            }
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendBulletHit));
        }
        public void SFEXT_G_BulletHit()
        {
            if (!EntityControl.dead)
            {
                LastHitTime = Time.time;
                if (IsOwner)
                {
                    Health -= BulletDamageTaken;
                    if (PredictDamage && Health <= 0)//the attacker calls the explode function in this case
                    {
                        Health = 0.0911f;
                        //if two people attacked us, and neither predicted they killed us but we took enough damage to die, we must still die.
                        SendCustomEventDelayedSeconds(nameof(CheckLaggyKilled), .25f);//give enough time for the explode event to happen if they did predict we died, otherwise do it ourself
                    }
                }
            }
        }
        public void CheckLaggyKilled()
        {
            if (!EntityControl.dead)
            {
                //Check if we still have the amount of health set to not send explode when killed, and if we do send explode
                if (Health == 0.0911f)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Explode));
                }
            }
        }
        private float LastCollisionTime;
        private float MinCollisionSoundDelay = 0.1f;
        public void SFEXT_L_OnCollisionEnter()
        {
            if (!IsOwner) { return; }
            LastCollisionTime = Time.time;
            if (Time.time - LastCollisionTime < MinCollisionSoundDelay)
            {
                LastCollisionTime = Time.time;
                Collision col = EntityControl.LastCollisionEnter;
                if (col == null) { return; }
                float colmag = col.impulse.magnitude / VehicleRigidbody.mass;
                if (colmag > BigCrashSpeed)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendBigCrash));
                }
                else if (colmag > MediumCrashSpeed)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendMediumCrash));
                }
                else if (colmag > SmallCrashSpeed)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendSmallCrash));
                }
            }
        }
        public void SendSmallCrash()
        {
            EntityControl.SendEventToExtensions("SFEXT_G_SmallCrash");
        }
        public void SendMediumCrash()
        {
            EntityControl.SendEventToExtensions("SFEXT_G_MediumCrash");
        }
        public void SendBigCrash()
        {
            EntityControl.SendEventToExtensions("SFEXT_G_BigCrash");
        }
        public void SFEXT_O_ReSupply()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ReSupply));
        }
        public void ReSupply()
        {
            ReSupplied = 0;//used to know if other scripts resupplied
            if ((Fuel < FullFuel - 10 || Health != FullHealth))
            {
                ReSupplied++;//used to only play the sound if we're actually repairing/getting ammo/fuel
            }
            EntityControl.SendEventToExtensions("SFEXT_G_ReSupply");//extensions increase the ReSupplied value too

            LastResupplyTime = Time.time;

            if (IsOwner)
            {
                Fuel = Mathf.Min(Fuel + (FullFuel / RefuelTime), FullFuel);
                Health = Mathf.Min(Health + (FullHealth / RepairTime), FullHealth);
            }
        }
        public void CheckDistance()
        {
            CurrentDistance = Vector3.Distance(localPlayer.GetPosition(), CenterOfMass.position);
            if (CurrentDistance > DistantRange)
            {
                if (!CurrentlyDistant)
                {
                    CurrentlyDistant = true;
                    EntityControl.SendEventToExtensions("SFEXT_L_BecomeDistant");
                }
            }
            else
            {
                if (CurrentlyDistant)
                {
                    CurrentlyDistant = false;
                    EntityControl.SendEventToExtensions("SFEXT_L_NotDistant");
                }
            }
            SendCustomEventDelayedSeconds(nameof(CheckDistance), 2);
        }
        public void SetCollidersLayer(int NewLayer)
        {
            if (VehicleMesh)
            {
                if (OnlyChangeColliders)
                {
                    Collider[] children = VehicleMesh.GetComponentsInChildren<Collider>(true);
                    foreach (Collider child in children)
                    {
                        child.gameObject.layer = NewLayer;
                    }
                }
                else
                {
                    Transform[] children = VehicleMesh.GetComponentsInChildren<Transform>(true);
                    foreach (Transform child in children)
                    {
                        child.gameObject.layer = NewLayer;
                    }
                }
            }
        }
        void RHandSteeringWheel(float RGrip)
        {
            bool GrabbingR = RGrip > GripSensitivity;
            if (GrabbingR)
            {
                if (!WheelGrippingLastFrame_toggleR)
                {
                    if (Time.time - LastGripTimeR < .25f)
                    {
                        WheelGrabToggleR = true;
                        WheelReleaseCountR = 0;
                    }
                    LastGripTimeR = Time.time;
                }
                WheelGrippingLastFrame_toggleR = true;
            }
            else
            {
                if (WheelGrippingLastFrame_toggleR)
                {
                    WheelReleaseCountR++;
                    if (WheelReleaseCountR > 1)
                    {
                        WheelGrabToggleR = false;
                    }
                }
                WheelGrippingLastFrame_toggleR = false;
            }
            //VR SteeringWheel
            if (GrabbingR || WheelGrabToggleR)
            {
                HandsOnWheel++;
                Quaternion VehicleRotDif = ControlsRoot.rotation * Quaternion.Inverse(VehicleRotLastFrameR);//difference in vehicle's rotation since last frame
                VehicleRotLastFrameR = ControlsRoot.rotation;
                JoystickZeroPointR = VehicleRotDif * JoystickZeroPointR;//zero point rotates with the vehicle so it appears still to the pilot
                if (!WheelGripLastFrameR)//first frame you gripped joystick
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_WheelGrabbedR");
                    VehicleRotDif = Quaternion.identity;
                    JoystickZeroPointR = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;
                    if (Drift_AutoSteer) { JoyStickValueR = 0; }
                    else { JoyStickValueR = -YawInput * SteeringWheelDegrees; }
                    JoystickValueLastFrameR = 0f;
                    CompareAngleLastFrameR = Vector3.up;
                }
                WheelGripLastFrameR = true;
                //difference between the vehicle and the hand's rotation, and then the difference between that and the JoystickZeroPoint
                Quaternion JoystickDifference;
                JoystickDifference = Quaternion.Inverse(ControlsRoot.rotation) * localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation
                    * Quaternion.Inverse(JoystickZeroPointR)
                    * ControlsRoot.rotation;

                Vector3 JoystickPosYaw = (JoystickDifference * Vector3.up);
                Vector3 CompareAngle = Vector3.ProjectOnPlane(JoystickPosYaw, Vector3.forward);
                JoyStickValueR += (Vector3.SignedAngle(CompareAngleLastFrameR, CompareAngle, Vector3.forward));
                CompareAngleLastFrameR = CompareAngle;
                JoystickValueLastFrameR = JoyStickValueR;
                VRJoystickPosR = JoyStickValueR / SteeringWheelDegrees;
            }
            else
            {
                VRJoystickPosR = 0f;
                if (WheelGripLastFrameR)//first frame you let go of wheel
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_WheelDroppedR");
                    WheelGripLastFrameL = false;
                    //LHandSteeringWheel hasn't run yet so don't need to do anything else
                }
                WheelGripLastFrameR = false;
            }
        }
        void LHandSteeringWheel(float LGrip)
        {
            //Toggle gripping the steering wheel if double tap grab
            bool GrabbingL = LGrip > GripSensitivity;
            if (GrabbingL)
            {
                if (!WheelGrippingLastFrame_toggleL)
                {
                    if (Time.time - LastGripTimeL < .25f)
                    {
                        WheelGrabToggleL = true;
                        WheelReleaseCountL = 0;
                    }
                    LastGripTimeL = Time.time;
                }
                WheelGrippingLastFrame_toggleL = true;
            }
            else
            {
                if (WheelGrippingLastFrame_toggleL)
                {
                    WheelReleaseCountL++;
                    if (WheelReleaseCountL > 1)
                    {
                        WheelGrabToggleL = false;
                    }
                }
                WheelGrippingLastFrame_toggleL = false;
            }
            //VR SteeringWheel
            if (GrabbingL || WheelGrabToggleL)
            {
                HandsOnWheel++;
                Quaternion VehicleRotDif = ControlsRoot.rotation * Quaternion.Inverse(VehicleRotLastFrameL);//difference in vehicle's rotation since last frame
                VehicleRotLastFrameL = ControlsRoot.rotation;
                JoystickZeroPointL = VehicleRotDif * JoystickZeroPointL;//zero point rotates with the vehicle so it appears still to the pilot
                if (!WheelGripLastFrameL)//first frame you gripped joystick
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_WheelGrabbedL");
                    VehicleRotDif = Quaternion.identity;
                    JoystickZeroPointL = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation;
                    if (Drift_AutoSteer) { JoyStickValueL = 0; }
                    else { JoyStickValueL = -YawInput * SteeringWheelDegrees; }
                    JoystickValueLastFrameL = 0f;
                    CompareAngleLastFrameL = Vector3.up;
                }
                WheelGripLastFrameL = true;
                //difference between the vehicle and the hand's rotation, and then the difference between that and the JoystickZeroPointL
                Quaternion JoystickDifference = Quaternion.Inverse(ControlsRoot.rotation) * localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation
                   * Quaternion.Inverse(JoystickZeroPointL)
                    * ControlsRoot.rotation;

                Vector3 JoystickPosYaw = (JoystickDifference * Vector3.up);
                Vector3 CompareAngle = Vector3.ProjectOnPlane(JoystickPosYaw, Vector3.forward);
                JoyStickValueL += (Vector3.SignedAngle(CompareAngleLastFrameL, CompareAngle, Vector3.forward));
                CompareAngleLastFrameL = CompareAngle;
                JoystickValueLastFrameL = JoyStickValueL;
                VRJoystickPosL = JoyStickValueL / SteeringWheelDegrees;
            }
            else
            {
                VRJoystickPosL = 0f;
                if (WheelGripLastFrameL)//first frame you let go of wheel
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_WheelDroppedL");
                    if (WheelGripLastFrameR)
                    {
                        WheelGripLastFrameR = false;
                        //regrab the right hand to stop the wheel position teleporting
                        RHandSteeringWheel(Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger"));
                        HandsOnWheel--;//remove one because we ran R twice
                    }
                }
                WheelGripLastFrameL = false;
            }
        }
    }
}