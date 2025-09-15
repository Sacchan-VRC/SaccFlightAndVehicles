
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class SaccSeaVehicle : UdonSharpBehaviour
    {
        [Tooltip("Base object reference")]
        public SaccEntity EntityControl;
        [Tooltip("The object containing all non-trigger colliders for the vehicle, their layers are changed when entering and exiting")]
        public Transform VehicleMesh;
        [Tooltip("Change all children of VehicleMesh, or just the objects with colliders?")]
        public bool OnlyChangeColliders = false;
        [Tooltip("Position Thrust force is applied at")]
        public Transform ThrustPoint;
        [Tooltip("Position yawing forces are applied at")]
        public Transform YawMoment;
        [Tooltip("Position traced down from to detect whether the vehicle is currently on the ground.")]
        public Transform GroundDetector;
        [Tooltip("Distance traced down from the ground detector's position to see if the ground is there, in order to determine if the vehicle is grounded")]
        public float GroundDetectorRayDistance = .44f;
        [Tooltip("HP of the vehicle")]
        public LayerMask GroundDetectorLayers = 2049;
        [UdonSynced(UdonSyncMode.None)] public float Health = 23f;
        [Tooltip("Teleport the vehicle to the oposite side of the map when flying too far in one direction?")]
        public bool RepeatingWorld = true;
        [Tooltip("Distance you can travel away from world origin before being teleported to the other side of the map. Not recommended to increase, floating point innacuracy and game freezing issues may occur if larger than default")]
        public float RepeatingWorldDistance = 20000;
        [Tooltip("Use the left hand to control the joystick and the right hand to control the throttle?")]
        public bool SwitchHandsJoyThrottle = false;
        public bool HasAfterburner = true;
        public KeyCode AfterBurnerKey = KeyCode.T;
        [Tooltip("Point in the throttle at which afterburner enables, .8 = 80%")]
        public float ThrottleAfterburnerPoint = 0.8f;
        [Header("Response:")]
        [Tooltip("Vehicle thrust at max throttle without afterburner")]
        public float ThrottleStrength = 20f;
        [Tooltip("Multiply how much the VR throttle moves relative to hand movement")]
        public float ThrottleSensitivity = 6f;
        [Tooltip("How long it takes to raech max throttle while holding the key on desktop")]
        public float KeyboardThrottleSens = .5f;
        [Tooltip("How many degrees to turn the wheel until it reaches max turning, in each direction, animation should match this")]
        public float SteeringWheelDegrees = 360f;
        [Tooltip("How far down you have to push the grip button to grab the joystick and throttle")]
        public float GripSensitivity = .75f;
        [Tooltip("How long keyboard turning must be held down to reach full deflection")]
        public float KeyboardSteeringSpeed = 1.5f;
        [Tooltip("how fast steering wheel returns to neutral position 1 = 1 second, .2 = 5 seconds")]
        public float SteeringReturnSpeed = 1f;
        [Tooltip("how fast steering wheel returns to neutral position in VR 1 = 1 second, .2 = 5 seconds")]
        public float SteeringReturnSpeedVR = .2f;
        [Tooltip("How much more thrust the vehicle has when in full afterburner")]
        public float AfterburnerThrustMulti = 1.5f;
        [Tooltip("How quickly the vehicle throttles up after throttle is increased (Lerp)")]
        public float AccelerationResponse = 4.5f;
        [Tooltip("How quickly the vehicle throttles down relative to how fast it throttles up after throttle is decreased")]
        public float EngineSpoolDownSpeedMulti = .5f;
        [Tooltip("How much the vehicle slows down (Speed lerped towards 0)")]
        public float AirFriction = 0.0004f;
        [Tooltip("Yaw force multiplier, (gets stronger with airspeed)")]
        public float YawStrength = 3f;
        [Tooltip("Yaw rotation force (as multiple of YawStrength) (doesn't get stronger with airspeed, useful for helicopters and ridiculous jets). Setting this to a non - zero value disables inversion of joystick pitch controls when vehicle is travelling backwards")]
        public float YawThrustVecMulti = 0f;
        [Tooltip("Force that stops vehicle from yawing, (gets stronger with airspeed)")]
        public float YawFriction = 15f;
        [Tooltip("Force that stops vehicle from yawing, (doesn't get stronger with airspeed)")]
        public float YawConstantFriction = 0f;
        [Tooltip("How quickly the vehicle responds to changes in joystick's yaw (Lerp)")]
        public float YawResponse = 20f;
        [Tooltip("Adjust the rotation of Unity's inbuilt Inertia Tensor Rotation, which is a function of rigidbodies. If set to 0, the vehicle will be very stable and feel boring to fly.")]
        public float InertiaTensorRotationMulti = 1;
        [Tooltip("Inverts Z axis of the Inertia Tensor Rotation, causing the direction of the yawing experienced after rolling to invert")]
        public bool InvertITRYaw = false;
        [Tooltip("Rotational inputs are multiplied by current speed to make flying at low speeds feel heavier. Above the speed input here, all inputs will be at 100%. Linear. (Meters/second)")]
        public float RotMultiMaxSpeed = 10;
        [Tooltip("How much the the vehicle's nose is pulled toward the direction of movement on the yaw axis")]
        public float VelStraightenStrYaw = 0.045f;
        [Tooltip("Degrees per second the vehicle rotates on the ground. Uses simple object rotation with a lerp, no real physics to it.")]
        public float TaxiRotationSpeed = 35f;
        [Tooltip("How lerped the taxi movement rotation is")]
        public float TaxiRotationResponse = 2.5f;
        [Tooltip("Make taxiing more realistic by not allowing vehicle to rotate on the spot")]
        public bool DisallowTaxiRotationWhileStill = false;
        [Tooltip("When the above is ticked, This is the speed at which the vehicle will reach its full turning speed. Meters/second.")]
        public float TaxiFullTurningSpeed = 20f;
        [Tooltip("Maximum Vel Lift, to stop the nose being pushed up. Technically should probably be 9.81 to counter gravity exactly")]
        public float VelLiftMax = 10f;
        [Tooltip("Vehicle will take damage if experiences more Gs that this (Internally Gs are calculated in all directions, the HUD shows only vertical Gs so it will differ slightly")]
        public float MaxGs = 10f;
        [Tooltip("Damage taken Per G above maxGs, per second.\n(Gs - MaxGs) * GDamage = damage/second")]
        public float GDamage = 10f;
        [Tooltip("Speed at which vehicle will start to take damage from a crash (m/s)")]
        public float CrashDmg_MinSpeed = 15f;
        [Tooltip("Speed at which vehicle will take damage equal to its max health from a crash (m/s)")]
        public float CrashDmg_MaxSpeed = 100f;
        [Header("Other:")]
        [Tooltip("Transform to base the pilot's throttle and joystick controls from. Used to make vertical throttle for helicopters, or if the cockpit of your vehicle can move, on transforming vehicle")]
        public Transform ControlsRoot;
        [Tooltip("Wind speed on each axis")]
        public Vector3 Wind;
        [Tooltip("Strength of noise-based changes in wind strength")]
        public float WindGustStrength = 15;
        [Tooltip("How often wind gust changes strength")]
        public float WindGustiness = 0.03f;
        [Tooltip("Scale of world space gust cells, smaller number = larger cells")]
        public float WindTurbulanceScale = 0.0001f;
        [UdonSynced(UdonSyncMode.None)] public float Fuel = 900;
        [Tooltip("Amount of fuel at which throttle will start reducing")]
        public float LowFuel = 125;
        [Tooltip("Fuel consumed per second at max throttle, scales with throttle")]
        public float FuelConsumption = 1;
        [Tooltip("Multiply FuelConsumption by this number when at full afterburner Scales with afterburner level")]
        public float FuelConsumptionABMulti = 3f;
        [Tooltip("Number of resupply ticks it takes to refuel fully from zero")]
        public float RefuelTime = 25;
        [Tooltip("Number of resupply ticks it takes to repair fully from zero")]
        public float RepairTime = 30;
        [Tooltip("Time until vehicle reappears after exploding")]
        public float RespawnDelay = 10;
        [Tooltip("Time after reappearing the vehicle is invincible for")]
        public float InvincibleAfterSpawn = 2.5f;
        [Tooltip("Damage taken when hit by a bullet")]
        public float BulletDamageTaken = 10f;
        [Tooltip("Locally destroy target if prediction thinks you killed them, should only ever cause problems if you have a system that repairs vehicles during a fight")]
        public bool PredictDamage = true;
        [Tooltip("Impact speed that defines a small crash")]
        public float SmallCrashSpeed = 1f;
        [Tooltip("Impact speed that defines a medium crash")]
        public float MediumCrashSpeed = 8f;
        [Tooltip("Impact speed that defines a big crash")]
        public float BigCrashSpeed = 25f;
        [Tooltip("Set Engine On when entering the vehicle?")]
        public bool EngineOnOnEnter = true;
        [Tooltip("Set Engine Off when entering the vehicle?")]
        public bool EngineOffOnExit = true;
        [Tooltip("Twist throttle like a motorbike?")]
        public bool UseTwistThrottle;
        public bool UseWSForAccelerate;
        Quaternion VehicleRotLastFrameThrottle;
        Quaternion ThrottleZeroPointTwist;
        float ThrottleValue;
        float ThrottleValueLastFrame;
        Vector3 CompareAngleLastFrameThrottle;
        float VRThrottlePos;
        public float TwistThrottleDegrees = 40f;
        private float ThrottleZeroPoint;
        [FieldChangeCallback(nameof(EngineOn))] public bool _EngineOn = false;
        public bool EngineOn
        {
            set
            {
                if (value)
                {
                    if (!_EngineOn)
                    {
                        EntityControl.SendEventToExtensions("SFEXT_G_EngineOn");
                        VehicleAnimator.SetBool("EngineOn", true);
                    }
                }
                else
                {
                    if (_EngineOn)
                    {
                        EntityControl.SendEventToExtensions("SFEXT_G_EngineOff");
                        Taxiinglerper = 0;
                        VehicleAnimator.SetBool("EngineOn", false);
                    }
                }
                _EngineOn = value;
            }
            get => _EngineOn;
        }
        public void SetEngineOn()
        {
            EngineOn = true;
        }
        public void SetEngineOff()
        {
            EngineOn = false;
        }
        [System.NonSerializedAttribute] public float AllGs;


        [System.NonSerializedAttribute][UdonSynced(UdonSyncMode.Linear)] public float EngineOutput = 0f;
        [System.NonSerializedAttribute] public Vector3 CurrentVel = Vector3.zero;
        [System.NonSerializedAttribute][UdonSynced(UdonSyncMode.Linear)] public float VertGs = 1f;
        [System.NonSerializedAttribute][UdonSynced(UdonSyncMode.Linear)] public float AngleOfAttack;//MAX of yaw & pitch aoa //used by effectscontroller and hudcontroller
        [System.NonSerializedAttribute] public bool Occupied = false; //this is true if someone is sitting in pilot seat
        [System.NonSerialized] public int NumPassengers;

        [System.NonSerializedAttribute] public Animator VehicleAnimator;
        [System.NonSerializedAttribute] public Rigidbody VehicleRigidbody;
        [System.NonSerializedAttribute] public Transform VehicleTransform;
        private GameObject VehicleGameObj;
        [System.NonSerializedAttribute] public Transform CenterOfMass;
        private float LerpedYaw;
        [System.NonSerializedAttribute] public bool ThrottleGripLastFrame = false;
        [System.NonSerializedAttribute] public bool JoystickGripLastFrame = false;
        Quaternion JoystickZeroPoint;
        Quaternion VehicleRotLastFrame;
        [System.NonSerializedAttribute] public float PlayerThrottle;
        private float TempThrottle;
        [System.NonSerializedAttribute] public float ThrottleInput = 0f;
        private float yaw = 0f;
        [System.NonSerializedAttribute] public bool Asleep;
        private bool Initialized;
        [System.NonSerializedAttribute] public float FullHealth;
        [System.NonSerializedAttribute] public bool Taxiing = false;
        [System.NonSerializedAttribute] public bool Floating = false;
        [System.NonSerializedAttribute] public Vector3 RotationInputs;
        [System.NonSerializedAttribute][UdonSynced(UdonSyncMode.Linear)] public float YawInput;
        [System.NonSerializedAttribute] public bool Piloting = false;
        [System.NonSerializedAttribute] public bool Passenger = false;
        [System.NonSerializedAttribute] public bool InEditor = true;
        [System.NonSerializedAttribute] public bool InVR = false;
        [System.NonSerializedAttribute] public Vector3 LastFrameVel = Vector3.zero;
        [System.NonSerializedAttribute] public VRCPlayerApi localPlayer;
        private Vector3 Yawing;
        private Vector3 Thrust;
        [System.NonSerializedAttribute] public float Taxiinglerper;
        [System.NonSerializedAttribute] public float ExtraDrag = 1;
        [System.NonSerializedAttribute] public float ExtraLift = 1;
        [System.NonSerializedAttribute] public float Speed;
        [System.NonSerializedAttribute] public float AirSpeed;
        [System.NonSerializedAttribute] public bool IsOwner = false;
        [System.NonSerializedAttribute] public Vector3 FinalWind;//includes Gusts
        [System.NonSerializedAttribute] public Vector3 AirVel;
        private float StillWindMulti;//multiplies the speed of the wind by the speed of the vehicle when taxiing to prevent still vehicles flying away
        private float SoundBarrier;
        [System.NonSerializedAttribute] public float FullFuel;
        private float LowFuelDivider;
        [System.NonSerializedAttribute] public float FullGunAmmo;
        [System.NonSerializedAttribute] public bool DoAAMTargeting;
        [System.NonSerializedAttribute] public Rigidbody GDHitRigidbody;
        [System.NonSerializedAttribute] public bool UsingManualSync;
        [System.NonSerializedAttribute] public bool Grounded = false;
        private bool RepeatingWorldCheckAxis;
        bool FloatingLastFrame = false;
        bool GroundedLastFrame = false;
        private float HandDistanceZLastFrame;
        private float EngineAngle;
        private float PitchThrustVecMultiStart;
        private float YawThrustVecMultiStart;
        private float RollThrustVecMultiStart;
        private float ThrottleNormalizer;
        private float ABNormalizer;
        private float EngineOutputLastFrame;
        bool HasWheelColliders = false;
        [System.NonSerializedAttribute] public bool JoyStickGrippingLastFrame_toggle = false;
        private bool GrabToggle;
        private int JoyStickReleaseCount;
        private float LastGripTimeJoy;
        private float TaxiFullTurningSpeedDivider;
        private bool LowFuelLastFrame;
        private bool NoFuelLastFrame;
        [System.NonSerializedAttribute] public float ThrottleStrengthAB;
        [System.NonSerializedAttribute] public float FuelConsumptionAB;
        [System.NonSerializedAttribute] public bool AfterburnerOn;
        private float GDamageToTake;
        [System.NonSerializedAttribute] public float LastHitTime = -100;
        [System.NonSerializedAttribute] public float PredictedHealth;


        [System.NonSerializedAttribute] public int NumActiveFlares;
        [System.NonSerializedAttribute] public int NumActiveChaff;
        [System.NonSerializedAttribute] public int NumActiveOtherCM;
        //this stuff can be used by DFUNCs
        //if these == 0 then they are not disabled. Being an int allows more than one extension to disable it at a time
        [System.NonSerializedAttribute] public float Limits = 1;
        [System.NonSerializedAttribute] public bool _DisablePhysicsAndInputs;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(DisablePhysicsAndInputs_))] public int DisablePhysicsAndInputs = 0;
        public int DisablePhysicsAndInputs_
        {
            set
            {
                if (value > 0 && DisablePhysicsAndInputs == 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_DisablePhysicsAndInputs_Activated");
                }
                else if (value == 0 && DisablePhysicsAndInputs > 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_DisablePhysicsAndInputs_Deactivated");
                }
                _DisablePhysicsAndInputs = value > 0;
                DisablePhysicsAndInputs = value;
            }
            get => DisablePhysicsAndInputs;
        }
        [System.NonSerialized] public int DisablePhysicsApplication;
        [System.NonSerializedAttribute] public bool _DisableTaxiRotation;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(DisableTaxiRotation_))] public int DisableTaxiRotation = 0;
        public int DisableTaxiRotation_
        {
            set
            {
                if (value > 0 && DisableTaxiRotation == 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_DisableTaxiRotation_Activated");
                }
                else if (value == 0 && DisableTaxiRotation > 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_DisableTaxiRotation_Deactivated");
                }
                _DisableTaxiRotation = value > 0;
                DisableTaxiRotation = value;
            }
            get => DisableTaxiRotation;
        }
        [System.NonSerializedAttribute] public bool _DisableGroundDetection;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(DisableGroundDetection_))] public int DisableGroundDetection = 0;
        public int DisableGroundDetection_
        {
            set
            {
                if (value > 0 && DisableGroundDetection == 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_DisableGroundDetection_Activated");
                }
                else if (value == 0 && DisableGroundDetection > 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_DisableGroundDetection_Deactivated");
                }
                _DisableGroundDetection = value > 0;
                DisableGroundDetection = value;
            }
            get => DisableGroundDetection;
        }
        [System.NonSerializedAttribute] public bool _KeepAwake;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(KeepAwake_))] public int KeepAwake = 0;
        public int KeepAwake_
        {
            set
            {
                if (value > 0 && KeepAwake == 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_KeepAwake_Activated");
                }
                else if (value == 0 && KeepAwake > 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_KeepAwake_Deactivated");
                }
                _KeepAwake = value > 0;
                KeepAwake = value;
            }
            get => KeepAwake;
        }
        [System.NonSerializedAttribute] public bool _DisableJoystickControl;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(DisableJoystickControl_))] public int DisableJoystickControl = 0;
        public int DisableJoystickControl_
        {
            set
            {
                if (value > 0 && DisableJoystickControl == 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_DisableJoystickControl_Activated");
                    if (JoystickGripLastFrame)
                    {
                        EntityControl.SendEventToExtensions("SFEXT_O_JoystickDropped");
                        JoystickGripLastFrame = false;
                    }
                }
                else if (value == 0 && DisableJoystickControl > 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_DisableJoystickControl_Deactivated");
                }
                _DisableJoystickControl = value > 0;
                DisableJoystickControl = value;
            }
            get => DisableJoystickControl;
        }
        [System.NonSerializedAttribute] public bool _DisableThrottleControl;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(DisableThrottleControl_))] public int DisableThrottleControl = 0;
        public int DisableThrottleControl_
        {
            set
            {
                if (value > 0 && DisableThrottleControl == 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_DisableThrottleControl_Activated");
                    if (ThrottleGripLastFrame)
                    {
                        EntityControl.SendEventToExtensions("SFEXT_O_ThrottleDropped");
                        ThrottleGripLastFrame = false;
                    }
                }
                else if (value == 0 && DisableThrottleControl > 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_DisableThrottleControl_Deactivated");
                }
                _DisableThrottleControl = value > 0;
                DisableThrottleControl = value;
            }
            get => DisableThrottleControl;
        }
        [System.NonSerializedAttribute] public bool _ThrottleOverridden;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(ThrottleOverridden_))] public int ThrottleOverridden = 0;
        public int ThrottleOverridden_
        {
            set
            {
                if (value > 0 && ThrottleOverridden == 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_ThrottleOverridden_Activated");
                }
                else if (value == 0 && ThrottleOverridden > 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_ThrottleOverridden_Deactivated");
                }
                _ThrottleOverridden = value > 0;
                ThrottleOverridden = value;
            }
            get => ThrottleOverridden;
        }
        [System.NonSerializedAttribute] public float ThrottleOverride;
        [System.NonSerializedAttribute] public bool _JoystickOverridden;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(JoystickOverridden_))] public int JoystickOverridden = 0;
        public int JoystickOverridden_
        {
            set
            {
                if (value > 0 && JoystickOverridden == 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_JoystickOverridden_Activated");
                }
                else if (value == 0 && JoystickOverridden > 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_JoystickOverridden_Deactivated");
                }
                _JoystickOverridden = value > 0;
                JoystickOverridden = value;
            }
            get => JoystickOverridden;
        }
        [System.NonSerializedAttribute] public bool _PreventEngineToggle;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(PreventEngineToggle_))] public int PreventEngineToggle = 0;
        public int PreventEngineToggle_
        {
            set
            {
                if (value > 0 && PreventEngineToggle == 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_PreventEngineToggle_Activated");
                }
                else if (value == 0 && PreventEngineToggle > 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_PreventEngineToggle_Deactivated");
                }
                _PreventEngineToggle = value > 0;
                PreventEngineToggle = value;
            }
            get => PreventEngineToggle;
        }
        [System.NonSerialized] public float InverThrustMultiplier = -1;
        [System.NonSerializedAttribute] public bool _InvertThrust;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(InvertThrust_))] public int InvertThrust = 0;
        public int InvertThrust_
        {
            set
            {
                if (value > 0 && InvertThrust == 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_InvertThrust_Activated");
                }
                else if (value == 0 && InvertThrust > 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_InvertThrust_Deactivated");
                }
                _InvertThrust = value > 0;
                InvertThrust = value;
            }
            get => InvertThrust;
        }
        [System.NonSerializedAttribute] public Vector3 JoystickOverride;
        private float JoystickGrabValue;
        private float JoystickValueLastFrame;
        private float JoyStickValue;
        Vector3 CompareAngleLastFrame;
        public void SFEXT_L_EntityStart()
        {
            VehicleGameObj = EntityControl.gameObject;
            VehicleTransform = EntityControl.transform;
            VehicleRigidbody = EntityControl.GetComponent<Rigidbody>();

            UsingManualSync = !EntityControl.EntityObjectSync;


            localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer))
            {
                InEditor = true;
                Piloting = true;
                IsOwner = true;
                Occupied = true;
                if (!UsingManualSync)
                {
                    VehicleRigidbody.drag = 0;
                    VehicleRigidbody.angularDrag = 0;
                }
            }
            else
            {
                InEditor = false;
                InVR = EntityControl.InVR;
                if (EntityControl.IsOwner)
                {
                    IsOwner = true;
                    if (!UsingManualSync)
                    {
                        VehicleRigidbody.drag = 0;
                        VehicleRigidbody.angularDrag = 0;
                    }
                }
                else
                {
                    if (!UsingManualSync)
                    {
                        VehicleRigidbody.drag = 9999;
                        VehicleRigidbody.angularDrag = 9999;
                    }
                }
            }

            WheelCollider[] wc = VehicleMesh.GetComponentsInChildren<WheelCollider>(true);
            if (wc.Length != 0) { HasWheelColliders = true; }

            //Adjust wheel values so you don't have to readjust them every time you change rigidbody mass
            float RBMass = VehicleRigidbody.mass;
            foreach (WheelCollider wheel in wc)
            {
                JointSpring SusiSpring = wheel.suspensionSpring;
                SusiSpring.spring *= RBMass;
                SusiSpring.damper *= RBMass;
                wheel.suspensionSpring = SusiSpring;
            }
            VehicleAnimator = EntityControl.GetComponent<Animator>();

            FullHealth = Health;
            FullFuel = Fuel;

            CenterOfMass = EntityControl.CenterOfMass;
            SetCoMMeshOffset();

            if (!HasAfterburner) { ThrottleAfterburnerPoint = 1; }
            ThrottleNormalizer = 1 / ThrottleAfterburnerPoint;
            ABNormalizer = 1 / (1 - ThrottleAfterburnerPoint);

            FuelConsumptionAB = (FuelConsumption * FuelConsumptionABMulti) - FuelConsumption;
            ThrottleStrengthAB = (ThrottleStrength * AfterburnerThrustMulti) - ThrottleStrength;


            LowFuelDivider = 1 / LowFuel;

            if (DisallowTaxiRotationWhileStill)
            {
                TaxiFullTurningSpeedDivider = 1 / TaxiFullTurningSpeed;
            }
            if (!ControlsRoot)
            { ControlsRoot = VehicleTransform; }
            if (!GroundDetector) { DisableGroundDetection_++; }

            SetupGCalcValues();
        }
        public void SetupGCalcValues()
        {
            NumFUinAvgTime = (int)(GsAveragingTime / Time.fixedDeltaTime);
            FrameGs = new Vector3[NumFUinAvgTime];
            Gs_all = Vector3.zero;
        }
        private void LateUpdate()
        {
            float DeltaTime = Time.deltaTime;
            if (IsOwner)//works in editor or ingame
            {
                if (!EntityControl._dead)
                {
                    //G/crash Damage
                    if (GDamageToTake > 0)
                    {
                        Health -= GDamageToTake * DeltaTime * GDamage;//take damage of GDamage per second per G above MaxGs
                        GDamageToTake = 0;
                    }
                    if (Health <= 0f)//vehicle is ded
                    {
                        NetworkExplode();
                    }
                }
                else { GDamageToTake = 0; }

                if (Floating)
                {
                    if (!FloatingLastFrame)
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TouchDownWater));
                    }
                }
                else
                { FloatingLastFrame = false; }
                if (!_DisableGroundDetection)
                {
                    RaycastHit GDHit;
                    if ((Physics.Raycast(GroundDetector.position, -GroundDetector.up, out GDHit, GroundDetectorRayDistance, GroundDetectorLayers, QueryTriggerInteraction.Ignore)))
                    {
                        GDHitRigidbody = GDHit.collider.attachedRigidbody;
                        if (!GroundedLastFrame)
                        {
                            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TouchDown));
                        }
                    }
                    else
                    {
                        GroundedLastFrame = false;
                        GDHitRigidbody = null;
                    }
                }
                if (Taxiing && !GroundedLastFrame && !FloatingLastFrame)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TakeOff));
                    VehicleRigidbody.WakeUp();
                }

                //synced variables because rigidbody values aren't accessable by non-owner players
                CurrentVel = VehicleRigidbody.velocity;
                Speed = CurrentVel.magnitude;
                if (Piloting)
                {
                    WindAndAoA();
                    DoRepeatingWorld();

                    if (!_DisablePhysicsAndInputs)
                    {
                        //collect inputs//inputs as ints
                        int Ai = Input.GetKey(KeyCode.A) ? -1 : 0;
                        int Di = Input.GetKey(KeyCode.D) ? 1 : 0;
                        int lefti = Input.GetKey(KeyCode.LeftArrow) ? -1 : 0;
                        int righti = Input.GetKey(KeyCode.RightArrow) ? 1 : 0;
                        bool Accel;
                        bool Decel;
                        if (UseWSForAccelerate)
                        {
                            Accel = Input.GetKey(KeyCode.W);
                            Decel = Input.GetKey(KeyCode.S);
                        }
                        else
                        {
                            Accel = Input.GetKey(KeyCode.LeftShift);
                            Decel = Input.GetKey(KeyCode.LeftControl);
                        }
                        int AccelKeyi = Accel ? 1 : 0;
                        int DecelKeyi = Decel ? 1 : 0;
                        float LGrip = 0;
                        float RGrip = 0;
                        if (!InEditor)
                        {
                            LGrip = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger");
                            RGrip = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
                        }
                        //MouseX = Input.GetAxisRaw("Mouse X");
                        //MouseY = Input.GetAxisRaw("Mouse Y");
                        float VRJoystickPos = 0;

                        float ThrottleGrip;
                        float JoyStickGrip;
                        if (SwitchHandsJoyThrottle)
                        {
                            JoyStickGrip = LGrip;
                            ThrottleGrip = RGrip;
                        }
                        else
                        {
                            ThrottleGrip = LGrip;
                            JoyStickGrip = RGrip;
                        }
                        //Toggle gripping the steering wheel if double tap grab
                        bool Grabbing = JoyStickGrip > GripSensitivity;
                        if (Grabbing)
                        {
                            if (!JoyStickGrippingLastFrame_toggle)
                            {
                                if (Time.time - LastGripTimeJoy < .25f)
                                {
                                    GrabToggle = true;
                                    JoyStickReleaseCount = 0;
                                }
                                LastGripTimeJoy = Time.time;
                            }
                            JoyStickGrippingLastFrame_toggle = true;
                        }
                        else
                        {
                            if (JoyStickGrippingLastFrame_toggle)
                            {
                                JoyStickReleaseCount++;
                                if (JoyStickReleaseCount > 1)
                                {
                                    GrabToggle = false;
                                }
                            }
                            JoyStickGrippingLastFrame_toggle = false;
                        }
                        //VR Steering wheel
                        if ((Grabbing || GrabToggle) && !_DisableJoystickControl)
                        {
                            Quaternion VehicleRotDif = ControlsRoot.rotation * Quaternion.Inverse(VehicleRotLastFrame);//difference in vehicle's rotation since last frame
                            VehicleRotLastFrame = ControlsRoot.rotation;
                            JoystickZeroPoint = VehicleRotDif * JoystickZeroPoint;//zero point rotates with the vehicle so it appears still to the pilot
                            if (!JoystickGripLastFrame)//first frame you gripped joystick
                            {
                                EntityControl.SendEventToExtensions("SFEXT_O_JoystickGrabbed");
                                VehicleRotDif = Quaternion.identity;
                                if (SwitchHandsJoyThrottle)
                                { JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation; }//rotation of the controller relative to the vehicle when it was pressed
                                else
                                { JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation; }
                                JoyStickValue = YawInput * SteeringWheelDegrees;
                                JoystickValueLastFrame = 0;
                                CompareAngleLastFrame = Vector3.up;
                                JoystickValueLastFrame = 0;
                                if (SwitchHandsJoyThrottle)
                                { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .05f, .222f, 35); }
                                else
                                { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .222f, 35); }
                                JoystickGripLastFrame = true;
                            }
                            //difference between the vehicle and the hand's rotation, and then the difference between that and the JoystickZeroPoint
                            Quaternion JoystickDifference;
                            JoystickDifference = Quaternion.Inverse(ControlsRoot.rotation) *
                                (SwitchHandsJoyThrottle ? localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation
                                                        : localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation)
                            * Quaternion.Inverse(JoystickZeroPoint)
                             * ControlsRoot.rotation;

                            Vector3 JoystickPosYaw = (JoystickDifference * Vector3.up);
                            Vector3 CompareAngle = Vector3.ProjectOnPlane(JoystickPosYaw, Vector3.forward);
                            JoyStickValue += (Vector3.SignedAngle(CompareAngleLastFrame, CompareAngle, Vector3.forward));
                            CompareAngleLastFrame = CompareAngle;
                            JoystickValueLastFrame = JoyStickValue;
                            VRJoystickPos = JoyStickValue / SteeringWheelDegrees;
                        }
                        else
                        {
                            VRJoystickPos = 0;
                            if (JoystickGripLastFrame)//first frame you let go of joystick
                            {
                                EntityControl.SendEventToExtensions("SFEXT_O_JoystickDropped");
                                JoystickGripLastFrame = false;
                                if (SwitchHandsJoyThrottle)
                                { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .05f, .222f, 35); }
                                else
                                { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .222f, 35); }
                            }
                        }
                        //Throttle
                        if (!_DisableJoystickControl)
                        {
                            if (UseTwistThrottle)
                            {
                                if (ThrottleGrip > GripSensitivity)
                                {
                                    Quaternion VehicleRotDif = ControlsRoot.rotation * Quaternion.Inverse(VehicleRotLastFrameThrottle);//difference in vehicle's rotation since last frame
                                    VehicleRotLastFrameThrottle = ControlsRoot.rotation;
                                    ThrottleZeroPointTwist = VehicleRotDif * ThrottleZeroPointTwist;//zero point rotates with the vehicle so it appears still to the pilot
                                    if (!ThrottleGripLastFrame)//first frame you gripped Throttle
                                    {
                                        EntityControl.SendEventToExtensions("SFEXT_O_ThrottleGrabbed");
                                        VehicleRotDif = Quaternion.identity;
                                        if (SwitchHandsJoyThrottle)
                                        { ThrottleZeroPointTwist = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation; }//rotation of the controller relative to the vehicle when it was pressed
                                        else
                                        { ThrottleZeroPointTwist = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation; }
                                        ThrottleValue = -ThrottleInput * TwistThrottleDegrees;
                                        ThrottleValueLastFrame = 0;
                                        CompareAngleLastFrameThrottle = Vector3.up;
                                        ThrottleValueLastFrame = 0;
                                        if (SwitchHandsJoyThrottle)
                                        { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .222f, 35); }
                                        else
                                        { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .05f, .222f, 35); }
                                        ThrottleGripLastFrame = true;
                                    }
                                    //difference between the vehicle and the hand's rotation, and then the difference between that and the ThrottleZeroPoint
                                    Quaternion ThrottleDifference;
                                    ThrottleDifference = Quaternion.Inverse(ControlsRoot.rotation) *
                                        (SwitchHandsJoyThrottle ? localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation
                                                                : localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation)
                                    * Quaternion.Inverse(ThrottleZeroPointTwist)
                                     * ControlsRoot.rotation;

                                    Vector3 ThrottlePosPitch = (ThrottleDifference * Vector3.up);
                                    Vector3 CompareAngle = Vector3.ProjectOnPlane(ThrottlePosPitch, Vector3.right);
                                    ThrottleValue += (Vector3.SignedAngle(CompareAngleLastFrameThrottle, CompareAngle, Vector3.right));
                                    CompareAngleLastFrameThrottle = CompareAngle;
                                    ThrottleValueLastFrame = ThrottleValue;
                                    PlayerThrottle = Mathf.Clamp(-ThrottleValue / TwistThrottleDegrees, 0f, 1f);
                                }
                                else
                                {
                                    PlayerThrottle = Mathf.Max(AccelKeyi - DecelKeyi, 0);
                                    if (ThrottleGripLastFrame)//first frame you let go of Throttle
                                    {
                                        EntityControl.SendEventToExtensions("SFEXT_O_ThrottleDropped");
                                        ThrottleGripLastFrame = false;
                                        if (SwitchHandsJoyThrottle)
                                        { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .222f, 35); }
                                        else
                                        { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .05f, .222f, 35); }
                                    }
                                }
                            }
                            else
                            {
                                if (HasAfterburner)
                                {
                                    if (AfterburnerOn)
                                    { PlayerThrottle = Mathf.Clamp(PlayerThrottle + ((AccelKeyi - DecelKeyi) * KeyboardThrottleSens * DeltaTime), 0, 1f); }
                                    else
                                    { PlayerThrottle = Mathf.Clamp(PlayerThrottle + ((AccelKeyi - DecelKeyi) * KeyboardThrottleSens * DeltaTime), 0, ThrottleAfterburnerPoint); }
                                }
                                else
                                { PlayerThrottle = Mathf.Clamp(PlayerThrottle + ((AccelKeyi - DecelKeyi) * KeyboardThrottleSens * DeltaTime), 0, 1f); }

                                if (ThrottleGrip > GripSensitivity)
                                {
                                    Vector3 handdistance;
                                    if (SwitchHandsJoyThrottle)
                                    { handdistance = ControlsRoot.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position; }
                                    else { handdistance = ControlsRoot.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position; }
                                    handdistance = ControlsRoot.InverseTransformDirection(handdistance);

                                    float HandThrottleAxis = handdistance.z;

                                    if (!ThrottleGripLastFrame)
                                    {
                                        EntityControl.SendEventToExtensions("SFEXT_O_ThrottleGrabbed");
                                        ThrottleZeroPoint = HandThrottleAxis;
                                        TempThrottle = PlayerThrottle;
                                        HandDistanceZLastFrame = 0;
                                        if (SwitchHandsJoyThrottle)
                                        { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .222f, 35); }
                                        else
                                        { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .05f, .222f, 35); }
                                        ThrottleGripLastFrame = true;
                                    }
                                    float ThrottleDifference = ThrottleZeroPoint - HandThrottleAxis;
                                    ThrottleDifference *= ThrottleSensitivity;

                                    //Detent function to prevent you going into afterburner by accident (bit of extra force required to turn on AB (actually hand speed))
                                    if (((HandDistanceZLastFrame - HandThrottleAxis) * ThrottleSensitivity > .05f)/*detent overcome*/ && Fuel > LowFuel || ((PlayerThrottle > ThrottleAfterburnerPoint/*already in afterburner*/) || !HasAfterburner))
                                    {
                                        PlayerThrottle = Mathf.Clamp(TempThrottle + ThrottleDifference, 0, 1);
                                    }
                                    else
                                    {
                                        PlayerThrottle = Mathf.Clamp(TempThrottle + ThrottleDifference, 0, ThrottleAfterburnerPoint);
                                    }
                                    HandDistanceZLastFrame = HandThrottleAxis;
                                }
                                else
                                {
                                    if (ThrottleGripLastFrame)
                                    {
                                        EntityControl.SendEventToExtensions("SFEXT_O_ThrottleDropped");
                                        if (SwitchHandsJoyThrottle)
                                        { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .222f, 35); }
                                        else
                                        { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .05f, .222f, 35); }
                                        ThrottleGripLastFrame = false;
                                    }
                                }
                            }
                        }
                        //keyboard control for afterburner                        //keyboard control for afterburner
                        if (Input.GetKeyDown(AfterBurnerKey))
                        {
                            if (HasAfterburner)
                            {
                                if (PlayerThrottle == 1)
                                { PlayerThrottle = ThrottleAfterburnerPoint; }
                                else
                                { PlayerThrottle = 1; }
                            }
                            else if (PlayerThrottle < 1)
                            { PlayerThrottle = 1; }
                            else
                            { PlayerThrottle = 0; }
                        }
                        if (ThrottleOverridden_ > 0 && !ThrottleGripLastFrame)
                        {
                            ThrottleInput = PlayerThrottle = ThrottleOverride;
                        }
                        else//if cruise control disabled, use inputs
                        {
                            if (!InVR)
                            {
                                float LTrigger = 0;
                                float RTrigger = 0;
                                if (!InEditor)
                                {
                                    LTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
                                    RTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
                                }
                                if (LTrigger > .05f)//axis throttle input for people who wish to use it //.05 deadzone so it doesn't take effect for keyboard users with something plugged in
                                { ThrottleInput = LTrigger; }
                                else { ThrottleInput = PlayerThrottle; }
                            }
                            else { ThrottleInput = PlayerThrottle; }
                        }
                        if (_EngineOn)
                        {
                            if (!_DisableTaxiRotation && Taxiing)
                            {
                                AngleOfAttack = 0;//prevent stall sound and aoavapor when on ground
                                                  //rotate if trying to yaw
                                float TaxiingStillMulti = 1;
                                if (DisallowTaxiRotationWhileStill)
                                { TaxiingStillMulti = Mathf.Min(Speed * TaxiFullTurningSpeedDivider, 1); }
                                Taxiinglerper = Mathf.Lerp(Taxiinglerper, YawInput * TaxiRotationSpeed * Time.smoothDeltaTime * TaxiingStillMulti, TaxiRotationResponse * DeltaTime);
                                VehicleTransform.Rotate(Vector3.up, Taxiinglerper);

                                StillWindMulti = Mathf.Min(Speed * .1f, 1);
                            }
                            else
                            {
                                StillWindMulti = 1;
                                Taxiinglerper = 0;
                            }
                        }
                        FuelEvents();
                        if (!_DisableJoystickControl)
                        {
                            if (_JoystickOverridden && !JoystickGripLastFrame)//joystick override enabled, and player not holding joystick
                            {
                                YawInput = JoystickOverride.z;
                            }
                            else//joystick override disabled, player has control
                            {
                                if (!InVR)
                                {
                                    Vector2 LStickPos = new Vector2(0, 0);
                                    Vector2 RStickPos = new Vector2(0, 0);
                                    if (!InEditor)
                                    {
                                        //LStickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
                                        LStickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
                                        //RStickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
                                        //RStickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
                                    }
                                    VRJoystickPos = LStickPos.y;
                                }

                                float YawAddAmount = VRJoystickPos + (-(float)(Ai + Di + lefti + righti) * DeltaTime * KeyboardSteeringSpeed);
                                if (InVR)
                                {
                                    if (Mathf.Abs(YawAddAmount) > 0)
                                    {
                                        YawInput = Mathf.Clamp(YawAddAmount, -1, 1);
                                    }
                                    else
                                    {
                                        YawInput = Mathf.MoveTowards(YawInput, 0, SteeringReturnSpeedVR * DeltaTime);
                                    }
                                }
                                else
                                {
                                    if (Mathf.Abs(YawAddAmount) > 0)
                                    {
                                        YawInput = Mathf.Clamp(YawInput + YawAddAmount, -1, 1);
                                    }
                                    else
                                    {
                                        YawInput = Mathf.MoveTowards(YawInput, 0, SteeringReturnSpeed * DeltaTime);
                                    }
                                }
                            }
                        }
                        bool MovingForward = Vector3.Dot(AirVel, VehicleTransform.forward) > 0;
                        //invert steering if moving backwards
                        yaw = Mathf.Clamp(YawInput, -1, 1) * (MovingForward ? YawStrength : -YawStrength);

                        //wheel colliders are broken, this workaround stops the vehicle from being 'sticky' when you try to start moving it.
                        if (Speed < .2 && HasWheelColliders && ThrottleInput > 0)
                        {
                            if (ThrottleStrength < 0)
                            { VehicleRigidbody.velocity = VehicleTransform.forward * -.25f; }
                            else
                            { VehicleRigidbody.velocity = VehicleTransform.forward * .25f; }
                        }
                    }
                }
                else
                {
                    if (Speed > .01f)
                    {
                        WindAndAoA();
                    }
                    else if (!Asleep && GroundedLastFrame && !_KeepAwake)
                    {
                        FallAsleep();
                    }
                    if (Taxiing)
                    {
                        StillWindMulti = Mathf.Min(Speed * .1f, 1);
                    }
                    else { StillWindMulti = 1; }
                    if (_EngineOn)
                    {
                        //allow remote piloting using extensions?
                        if (ThrottleOverridden_ > 0)
                        { ThrottleInput = PlayerThrottle = ThrottleOverride; }
                        FuelEvents();
                    }
                    if (_JoystickOverridden)
                    { RotationInputs = JoystickOverride; }
                    DoRepeatingWorld();
                }

                if (!_DisablePhysicsAndInputs)
                {
                    //Lerp the inputs for 'engine response', throttle decrease response is slower than increase (EngineSpoolDownSpeedMulti)
                    if (EngineOutput < ThrottleInput)
                    { EngineOutput = Mathf.Lerp(EngineOutput, ThrottleInput, AccelerationResponse * DeltaTime); }
                    else
                    { EngineOutput = Mathf.Lerp(EngineOutput, ThrottleInput, AccelerationResponse * EngineSpoolDownSpeedMulti * DeltaTime); }

                    if (!Asleep)//optimization
                    {
                        float rotlift = Mathf.Min(AirSpeed / RotMultiMaxSpeed, 1);//using a simple linear curve for increasing control as you move faster

                        yaw *= Mathf.Max(YawThrustVecMulti, rotlift);

                        //Lerp the inputs for 'rotation response'
                        LerpedYaw = Mathf.Lerp(LerpedYaw, yaw, YawResponse * DeltaTime);
                    }
                    if (Taxiing)//on ground or water (boats will never be on ground because you disable grounddetector)
                    {
                        Yawing = (VehicleTransform.right * LerpedYaw);
                        Vector2 Outputs = UnpackThrottles(EngineOutput);
                        if (_InvertThrust)
                        {
                            Outputs *= InverThrustMultiplier;
                        }
                        Thrust = ThrustPoint.forward * (Mathf.Min(Outputs.x)//Throttle
                        * ThrottleStrength
                        + Mathf.Max(Outputs.y, 0)//Afterburner throttle
                        * ThrottleStrengthAB);
                    }
                    else
                    {
                        Yawing = Vector3.zero;
                        Thrust = Vector3.zero;
                    }
                }
            }
            else//non-owners need to know these values
            {
                Speed = AirSpeed = CurrentVel.magnitude;//wind speed is local anyway, so just use ground speed for non-owners
                                                        //AirVel = VehicleRigidbody.velocity - Wind;//wind isn't synced so this will be wrong
                                                        //AirSpeed = AirVel.magnitude;
            }
            RotationInputs.y = YawInput;
        }
        private void FixedUpdate()
        {
            if (IsOwner && !Asleep)
            {
                float DeltaTime = Time.fixedDeltaTime;
                float RBMass = VehicleRigidbody.mass;
                //lerp velocity toward 0 to simulate air friction
                Vector3 VehicleVel = VehicleRigidbody.velocity;
                VehicleRigidbody.velocity = Vector3.Lerp(VehicleVel, FinalWind * StillWindMulti, ((((AirFriction) * ExtraDrag)) * 90) * DeltaTime);
                //apply thrust
                VehicleRigidbody.AddForceAtPosition(Thrust * RBMass, ThrustPoint.position, ForceMode.Force);//deltatime is built into ForceMode.Force
                                                                                                            //apply yawing using yaw moment
                VehicleRigidbody.AddForceAtPosition(Yawing * RBMass, YawMoment.position, ForceMode.Force);
                //calc Gs
                float gravity = 9.81f * DeltaTime;
                LastFrameVel.y -= gravity;
                Vector3 Gs3 = VehicleTransform.InverseTransformDirection(VehicleVel - LastFrameVel);
                Vector3 thisFrameGs = Gs3 / gravity;
                Gs_all -= FrameGs[GsFrameCheck];
                Gs_all += thisFrameGs;
                FrameGs[GsFrameCheck] = thisFrameGs;
                GsFrameCheck++;
                if (GsFrameCheck >= NumFUinAvgTime) { GsFrameCheck = 0; }
                AllGs = Gs_all.magnitude / NumFUinAvgTime;
                GDamageToTake += Mathf.Max((AllGs - MaxGs), 0);
                LastFrameVel = VehicleVel;
            }
        }
        [System.NonSerialized] public float GsAveragingTime = .1f;
        private int NumFUinAvgTime = 1;
        private Vector3 Gs_all;
        private Vector3[] FrameGs;
        private int GsFrameCheck;
        public void NetworkExplode()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Explode));
        }
        public void Explode()
        {
            if (EntityControl._dead) { return; }
            SetEngineOff();
            PlayerThrottle = 0;
            ThrottleInput = 0;
            EngineOutput = 0;
            if (HasAfterburner) { SetAfterburnerOff(); }
            Fuel = FullFuel;
            Yawing = Vector3.zero;

            if (!EntityControl.wrecked) { EntityControl.SetWrecked(); }
            EntityControl.dead = true;
            EntityControl.SendEventToExtensions("SFEXT_G_Explode");

            SendCustomEventDelayedSeconds(nameof(ReAppear), RespawnDelay + Time.fixedDeltaTime * 2);
            SendCustomEventDelayedSeconds(nameof(NotDead), RespawnDelay + InvincibleAfterSpawn);

            if (IsOwner)
            {
                Health = FullHealth;//turns off low health smoke
                Fuel = FullFuel;
                AngleOfAttack = 0;
                SendCustomEventDelayedSeconds(nameof(MoveToSpawn), RespawnDelay - 3);
                EntityControl.SendEventToExtensions("SFEXT_O_Explode");
            }

            //pilot and passengers are dropped out of the vehicle
            if ((Piloting || Passenger) && !InEditor)
            {
                EntityControl.ExitStation();
            }
            if (LowFuelLastFrame)
            { SendNotLowFuel(); }
            if (NoFuelLastFrame)
            { SendNotNoFuel(); }
        }
        public void SFEXT_O_OnPlayerJoined()
        {
            if (GroundedLastFrame)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TouchDown));
            }
            if (FloatingLastFrame)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(TouchDownWater));
            }
            //sync engine status
            if (Piloting)
            {
                if (EngineOnOnEnter && !_EngineOn)
                { SendCustomEventDelayedSeconds(nameof(TurnEngineOffLaterJoiner), 10); }//doesn't work if done straight away because it executes before SFEXT_G_PilotEnter
                else if (!EngineOnOnEnter && _EngineOn)
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetEngineOn)); }
            }
            else if (_EngineOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetEngineOn)); }
        }
        public void TurnEngineOffLaterJoiner()
        {
            if (!EngineOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetEngineOff)); }
        }
        public void ReAppear()
        {
            EntityControl.SendEventToExtensions("SFEXT_G_ReAppear");
            EntityControl.SetWreckedFalse();//compatability
            if (IsOwner)
            {
                if (!UsingManualSync)
                {
                    VehicleRigidbody.drag = 0;
                    VehicleRigidbody.angularDrag = 0;
                }
            }
        }
        public void SetRespawnPos()
        {
            VehicleRigidbody.drag = 0;
            VehicleRigidbody.angularDrag = 0;
            Thrust = Vector3.zero;
            Yawing = Vector3.zero;
            VehicleRigidbody.angularVelocity = Vector3.zero;
            VehicleRigidbody.velocity = Vector3.zero;
            if (InEditor || UsingManualSync)
            {
                VehicleTransform.localPosition = EntityControl.Spawnposition;
                VehicleTransform.localRotation = EntityControl.Spawnrotation;
                VehicleRigidbody.position = VehicleTransform.position;
                VehicleRigidbody.rotation = VehicleTransform.rotation;
            }
            else
            {
                if (EntityControl.EntityObjectSync) { EntityControl.EntityObjectSync.Respawn(); }
            }
            if (EntityControl.RespawnPoint)
            {
                VehicleTransform.position = EntityControl.RespawnPoint.position;
                VehicleTransform.rotation = EntityControl.RespawnPoint.rotation;
                VehicleRigidbody.position = VehicleTransform.position;
                VehicleRigidbody.rotation = VehicleTransform.rotation;
            }
        }
        public void NotDead()
        {
            Health = FullHealth;
            EntityControl.dead = false;
        }
        public void MoveToSpawn()
        {
            PlayerThrottle = 0;//for editor test mode
            EngineOutput = 0;//^
            VehicleRigidbody.angularVelocity = Vector3.zero;
            VehicleRigidbody.velocity = Vector3.zero;
            Health = FullHealth;
            SetRespawnPos();
            EntityControl.SendEventToExtensions("SFEXT_O_MoveToSpawn");
        }
        private void WakeUp()/*  */
        {
            Asleep = false;
            EntityControl.SendEventToExtensions("SFEXT_L_WakeUp");
        }
        private void FallAsleep()
        {
            Asleep = true;
            EntityControl.SendEventToExtensions("SFEXT_L_FallAsleep");
            VehicleRigidbody.Sleep();
            AllGs = 0;
            GDamageToTake = 0;
            VertGs = 0;
            LastFrameVel = Vector3.zero;
        }
        public void SFEXT_L_CoMSet()
        {
            if (Initialized)
            { SetCoMMeshOffset(); }
        }
        private void OnEnable()
        {
            if (Initialized)
            { SetCoMMeshOffset(); }
        }
        public void SetCoMMeshOffset()
        {
            //move objects to so that the vehicle's main pivot is at the CoM so that syncscript's rotation is smoother
            Vector3 CoMOffset = CenterOfMass.position - VehicleTransform.position;
            int c = VehicleTransform.childCount;
            Transform[] MainObjChildren = new Transform[c];
            for (int i = 0; i < c; i++)
            {
                VehicleTransform.GetChild(i).position -= CoMOffset;
            }
            VehicleTransform.position += CoMOffset;
            SendCustomEventDelayedSeconds(nameof(SetCoM_ITR), Time.fixedDeltaTime);//this has to be delayed because ?
            EntityControl.Spawnposition = VehicleTransform.localPosition;
            EntityControl.Spawnrotation = VehicleTransform.localRotation;
        }
        public void SetCoM_ITR()
        {
            VehicleRigidbody.centerOfMass = VehicleTransform.InverseTransformDirection(CenterOfMass.position - VehicleTransform.position);//correct position if scaled
            EntityControl.CoMSet = true;
            VehicleRigidbody.inertiaTensor = VehicleRigidbody.inertiaTensor;
            VehicleRigidbody.inertiaTensorRotation = Quaternion.SlerpUnclamped(Quaternion.identity, VehicleRigidbody.inertiaTensorRotation, InertiaTensorRotationMulti);
            if (InvertITRYaw)
            {
                Vector3 ITR = VehicleRigidbody.inertiaTensorRotation.eulerAngles;
                ITR.x *= -1;
                VehicleRigidbody.inertiaTensorRotation = Quaternion.Euler(ITR);
            }
        }
        public void FuelEvents()
        {
            Vector2 Throttles = UnpackThrottles(ThrottleInput);
            Fuel = Mathf.Max(Fuel -
                                ((Mathf.Max(Throttles.x, 0.25f) * FuelConsumption)
                                    + (Throttles.y * FuelConsumptionAB)) * Time.deltaTime, 0);
            if (Fuel < LowFuel)
            {
                //max throttle scales down with amount of fuel below LowFuel
                ThrottleInput = ThrottleInput * Fuel * LowFuelDivider;
                if (!LowFuelLastFrame)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendLowFuel));
                }
                if (Fuel == 0 && !NoFuelLastFrame)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendNoFuel));
                }
            }
            else
            {
                if (LowFuelLastFrame)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendNotLowFuel));
                }
            }
            if (Fuel > 0)
            {
                if (NoFuelLastFrame)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendNotNoFuel));
                }
            }
            if (HasAfterburner)
            {
                if (ThrottleInput > ThrottleAfterburnerPoint && !AfterburnerOn)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetAfterburnerOn));
                }
                else if (ThrottleInput <= ThrottleAfterburnerPoint && AfterburnerOn)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetAfterburnerOff));
                }
            }
        }
        public void DoRepeatingWorld()
        {
            if (RepeatingWorld)
            {
                if (RepeatingWorldCheckAxis)
                {
                    if (Mathf.Abs(CenterOfMass.position.z) > RepeatingWorldDistance)
                    {
                        if (CenterOfMass.position.z > 0)
                        {
                            Vector3 vehpos = VehicleTransform.position;
                            vehpos.z -= RepeatingWorldDistance * 2;
                            VehicleTransform.position = vehpos;
                            VehicleRigidbody.position = VehicleTransform.position;
                        }
                        else
                        {
                            Vector3 vehpos = VehicleTransform.position;
                            vehpos.z += RepeatingWorldDistance * 2;
                            VehicleTransform.position = vehpos;
                            VehicleRigidbody.position = VehicleTransform.position;
                        }
                    }
                }
                else
                {
                    if (Mathf.Abs(CenterOfMass.position.x) > RepeatingWorldDistance)
                    {
                        if (CenterOfMass.position.x > 0)
                        {
                            Vector3 vehpos = VehicleTransform.position;
                            vehpos.x -= RepeatingWorldDistance * 2;
                            VehicleTransform.position = vehpos;
                            VehicleRigidbody.position = VehicleTransform.position;
                        }
                        else
                        {
                            Vector3 vehpos = VehicleTransform.position;
                            vehpos.x += RepeatingWorldDistance * 2;
                            VehicleTransform.position = vehpos;
                            VehicleRigidbody.position = VehicleTransform.position;
                        }
                    }
                }
                RepeatingWorldCheckAxis = !RepeatingWorldCheckAxis;//Check one axis per frame
            }
        }
        public void TouchDown()
        {
            //Debug.Log("TouchDown");
            if (GroundedLastFrame) { return; }
            Taxiing = true;
            Grounded = false;
            GroundedLastFrame = true;
            EntityControl.SendEventToExtensions("SFEXT_G_TouchDown");
        }
        public void TouchDownWater()
        {
            //Debug.Log("TouchDownWater");
            if (FloatingLastFrame) { return; }
            Taxiing = true;
            Floating = true;
            FloatingLastFrame = true;
            EntityControl.SendEventToExtensions("SFEXT_G_TouchDownWater");
        }
        public void TakeOff()
        {
            //Debug.Log("TakeOff");
            Taxiing = false;
            Floating = false;
            Grounded = false;
            FloatingLastFrame = false;
            GroundedLastFrame = false;
            EntityControl.SendEventToExtensions("SFEXT_G_TakeOff");
        }
        public void SetAfterburnerOn()
        {
            if (!AfterburnerOn)
            {
                AfterburnerOn = true;
                EntityControl.SendEventToExtensions("SFEXT_G_AfterburnerOn");
            }
        }
        public void SetAfterburnerOff()
        {
            if (AfterburnerOn)
            {
                AfterburnerOn = false;
                EntityControl.SendEventToExtensions("SFEXT_G_AfterburnerOff");
            }
        }
        private void ToggleAfterburner()
        {
            if (!AfterburnerOn && ThrottleInput > ThrottleAfterburnerPoint && Fuel > LowFuel)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetAfterburnerOn));
            }
            else if (AfterburnerOn)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetAfterburnerOff));
            }
        }
        public void SendLowFuel()
        {
            LowFuelLastFrame = true;
            EntityControl.SendEventToExtensions("SFEXT_G_LowFuel");
        }
        public void SendNotLowFuel()
        {
            LowFuelLastFrame = false;
            EntityControl.SendEventToExtensions("SFEXT_G_NotLowFuel");
        }
        public void SendNoFuel()
        {
            NoFuelLastFrame = true;
            EntityControl.SendEventToExtensions("SFEXT_G_NoFuel");
            SetEngineOff();
        }
        public void SendNotNoFuel()
        {
            NoFuelLastFrame = false;
            EntityControl.SendEventToExtensions("SFEXT_G_NotNoFuel");
            if (EngineOnOnEnter && Occupied)
            { SetEngineOn(); }
        }
        public void SFEXT_G_ReSupply()
        {
            if ((Fuel < FullFuel - 10) || (Health != FullHealth))
            {
                EntityControl.ReSupplied++;//used to only play the sound if we're actually repairing/getting ammo/fuel
            }

            if (IsOwner)
            {
                Fuel = Mathf.Min(Fuel + (FullFuel / RefuelTime), FullFuel);
                Health = Mathf.Min(Health + (FullHealth / RepairTime), FullHealth);
                if (LowFuelLastFrame && Fuel > LowFuel)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendNotLowFuel));
                }
                if (NoFuelLastFrame && Fuel > 0)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendNotNoFuel));
                }
            }
        }
        public void SFEXT_G_RePair()
        {
            if (Health != FullHealth) { EntityControl.ReSupplied++; }
            if (IsOwner)
            {
                Health = Mathf.Min(Health + (FullHealth / RepairTime), FullHealth);
            }
        }
        public void SFEXT_G_ReFuel()
        {
            if (Fuel < FullFuel - 10) { EntityControl.ReSupplied++; }
            if (IsOwner)
            {
                Fuel = Mathf.Min(Fuel + (FullFuel / RefuelTime), FullFuel);
                if (LowFuelLastFrame && Fuel > LowFuel)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendNotLowFuel));
                }
                if (NoFuelLastFrame && Fuel > 0)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendNotNoFuel));
                }
            }
        }
        public void SFEXT_G_RespawnButton()//called globally when using respawn button
        {
            if (IsOwner)
            {
                //synced variables
                Health = FullHealth;
                Fuel = FullFuel;
                SetRespawnPos();
            }
            if (_EngineOn)
            {
                SetEngineOff();
                PlayerThrottle = ThrottleInput = EngineOutputLastFrame = EngineOutput = 0;
            }
            if (HasAfterburner) { SetAfterburnerOff(); }
            //these two make it invincible and unable to be respawned again for 5s
            EntityControl.dead = true;
            SendCustomEventDelayedSeconds(nameof(NotDead), InvincibleAfterSpawn);
            EntityControl.SendEventToExtensions("");
            if (LowFuelLastFrame)
            { SendNotLowFuel(); }
            if (NoFuelLastFrame)
            { SendNotNoFuel(); }
        }
        public void SFEXT_L_BulletHit()
        {
            if (PredictDamage)
            {
                if (Time.time - LastHitTime > 2)
                {
                    PredictedHealth = Health - (BulletDamageTaken * EntityControl.LastHitBulletDamageMulti);
                    LastHitTime = Time.time;//must be updated before sending explode() for checks in explode event to work
                    if (PredictedHealth <= 0)
                    {
                        EntityControl.SendEventToExtensions("SFEXT_O_GunKill");
                        NetworkExplode();
                    }
                }
                else
                {
                    PredictedHealth -= BulletDamageTaken * EntityControl.LastHitBulletDamageMulti;
                    LastHitTime = Time.time;
                    if (PredictedHealth <= 0)
                    {
                        EntityControl.SendEventToExtensions("SFEXT_O_GunKill");
                        NetworkExplode();
                    }
                }
            }
        }
        public void SFEXT_G_BulletHit()
        {
            if (!EntityControl._dead)
            {
                if (IsOwner)
                {
                    Health -= BulletDamageTaken * EntityControl.LastHitBulletDamageMulti;
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
            if (!EntityControl._dead)
            {
                //Check if we still have the amount of health set to not send explode when killed, and if we do send explode
                if (Health == 0.0911f)
                {
                    NetworkExplode();
                }
            }
        }
        private float LastCollisionTime;
        private float MinCollisionSoundDelay = 0.1f;
        public void SFEXT_L_OnCollisionEnter()
        {
            if (!IsOwner) { return; }
            if (Asleep) { WakeUp(); }
            LastCollisionTime = Time.time;
            Collision col = EntityControl.LastCollisionEnter;
            if (col == null) { return; }
            float colmag = col.impulse.magnitude / VehicleRigidbody.mass;
            float colmag_dmg = colmag;
            if (colmag_dmg > CrashDmg_MinSpeed)
            {
                if (colmag_dmg < CrashDmg_MaxSpeed)
                {
                    float dif = CrashDmg_MaxSpeed - CrashDmg_MinSpeed;
                    float newcolT = (colmag_dmg - CrashDmg_MinSpeed) / dif;
                    colmag_dmg = Mathf.Lerp(0, CrashDmg_MaxSpeed, newcolT);
                }
                float thisGDMG = (colmag_dmg / CrashDmg_MaxSpeed) * FullHealth;
                Health -= thisGDMG;

                if (Health <= 0 /* && thisGDMG > FullHealth * 0.5f (This is for if wrecked is implemented) */)
                {
                    if (Piloting) { EntityControl.SendEventToExtensions("SFEXT_O_Suicide"); }
                    NetworkExplode();
                }
            }
            if (Time.time - LastCollisionTime > MinCollisionSoundDelay)
            {
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
            LastCollisionTime = Time.time;
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
        public void SFEXT_P_PassengerEnter()
        {
            Passenger = true;
            SetCollidersLayer(EntityControl.OnboardVehicleLayer);
        }
        public void SFEXT_P_PassengerExit()
        {
            Passenger = false;
            if (!EntityControl.MySeatIsExternal) { localPlayer.SetVelocity(CurrentVel); }
            SetCollidersLayer(EntityControl.OutsideVehicleLayer);
        }
        public void SFEXT_G_PassengerEnter()
        {
            NumPassengers++;
        }
        public void SFEXT_G_PassengerExit()
        {
            NumPassengers--;
        }
        public void SFEXT_O_TakeOwnership()
        {
            IsOwner = true;
            if (Asleep) { WakeUp(); }
            if (_EngineOn)
            {
                //The !Occupied check is to check if the player just left the instance while not in the vehicle
                //We want the vehicle to keep flying itself if it was left in auto-hover/fly straight mode with no pilot and its owner leaves

                //OnPlayerLeft() runs after OnOwnershipTransferred() // <--- no longer true
                //!EntityControl.pilotLeftFlag is now needed because the order is random
                //if OnPlayerLeft() runs first, '&& !EntityControl.pilotLeftFlag' ensures this still works

                if ((EntityControl.Piloting || !Occupied) && !EntityControl.pilotLeftFlag)
                // pilot wasn't in the vehicle when you took ownership, or you just took ownership by getting in
                {
                    PlayerThrottle = ThrottleInput = EngineOutputLastFrame = EngineOutput;
                }
                else// user was in the vehicle when they left
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetEngineOff));
                    PlayerThrottle = ThrottleInput = EngineOutputLastFrame = EngineOutput = 0;
                }
            }
            if (!UsingManualSync)
            {
                VehicleRigidbody.drag = 0;
                VehicleRigidbody.angularDrag = 0;
            }
            SetupGCalcValues();
        }
        public void SFEXT_O_LoseOwnership()
        {
            IsOwner = false;
            if (!UsingManualSync)
            {
                VehicleRigidbody.drag = 9999;
                VehicleRigidbody.angularDrag = 9999;
            }
        }
        public void SFEXT_O_PilotEnter()
        {
            if (Asleep) { WakeUp(); }
            InVR = EntityControl.InVR;
            GDHitRigidbody = null;
            if (_EngineOn)
            { PlayerThrottle = ThrottleInput = EngineOutputLastFrame = EngineOutput; }
            else
            { PlayerThrottle = ThrottleInput = EngineOutputLastFrame = EngineOutput = 0; }

            Piloting = true;
            if (EntityControl._dead) { Health = FullHealth; }//dead is true for the first 5 seconds after spawn, this might help with spontaneous explosions

            //hopefully prevents explosions when you enter the vehicle
            VehicleRigidbody.velocity = CurrentVel;
            VertGs = 0;
            AllGs = 0;
            LastFrameVel = CurrentVel;
            if (EngineOnOnEnter && Fuel > 0 && !_PreventEngineToggle)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetEngineOn));
            }
            SetupGCalcValues();
            SetCollidersLayer(EntityControl.OnboardVehicleLayer);
        }
        public void SFEXT_G_PilotEnter()
        {
            Occupied = true;
            EntityControl.dead = false;//vehicle stops being invincible if someone gets in, also acts as redundancy incase someone missed the notdead event
        }
        public void SFEXT_G_PilotExit()
        {
            Occupied = false;
            RotationInputs = Vector3.zero;
        }
        public void SFEXT_O_PilotExit()
        {
            //zero control values
            yaw = 0;
            LerpedYaw = 0;
            YawInput = 0;
            ThrottleInput = 0;
            //reset everything
            Piloting = false;
            Taxiinglerper = 0;
            ThrottleGripLastFrame = false;
            JoystickGripLastFrame = false;
            DoAAMTargeting = false;
            Yawing = Vector3.zero;
            if (!EntityControl.MySeatIsExternal) { localPlayer.SetVelocity(CurrentVel); }
            if (EngineOffOnExit && !_PreventEngineToggle)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetEngineOff));
                ThrottleInput = 0; ;
            }

            //set vehicle's collider's layers back
            SetCollidersLayer(EntityControl.OutsideVehicleLayer);
        }
        int numGrapplesAttached;
        public void SFEXT_L_GrappleAttach()
        {
            numGrapplesAttached++;
        }
        public void SFEXT_L_GrappleDetach()
        {
            numGrapplesAttached--;
            if (numGrapplesAttached < 0) { numGrapplesAttached = 0; }
            if (EngineOn) return;
            if (numGrapplesAttached != 0) return;
            if (Piloting)
            {
                if (EngineOnOnEnter)
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetEngineOn));
            }
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
        private void WindAndAoA()
        {
            if (DisablePhysicsAndInputs_ != 0) { return; }
            float TimeGustiness = Time.time * WindGustiness;
            float gustx = TimeGustiness + (VehicleTransform.position.x * WindTurbulanceScale);
            float gustz = TimeGustiness + (VehicleTransform.position.z * WindTurbulanceScale);
            FinalWind = Vector3.Normalize(new Vector3((Mathf.PerlinNoise(gustx + 9000, gustz) - .5f), /* (Mathf.PerlinNoise(gustx - 9000, gustz - 9000) - .5f) */0, (Mathf.PerlinNoise(gustx, gustz + 9999) - .5f))) * WindGustStrength;
            FinalWind = (FinalWind + Wind);
            AirVel = VehicleRigidbody.velocity - (FinalWind * StillWindMulti);
            AirSpeed = AirVel.magnitude;
        }
        public Vector2 UnpackThrottles(float Throttle)
        {
            //x = throttle amount (0-1), y = afterburner amount (0-1)
            return new Vector2(Mathf.Min(Throttle, ThrottleAfterburnerPoint) * ThrottleNormalizer,
            Mathf.Max((Mathf.Max(Throttle, ThrottleAfterburnerPoint) - ThrottleAfterburnerPoint) * ABNormalizer, 0));
        }
    }
}