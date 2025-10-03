
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.UdonNetworkCalling;
//
namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    [DefaultExecutionOrder(5)]//after dfuncs that can set values used by this
    public class SaccAirVehicle : UdonSharpBehaviour
    {
        [Tooltip("Base object reference")]
        public SaccEntity EntityControl;
        [Tooltip("The object containing all non-trigger colliders for the vehicle, their layers are changed when entering and exiting")]
        public Transform VehicleMesh;
        [Tooltip("Change all children of VehicleMesh, or just the objects with colliders?")]
        public bool OnlyChangeColliders = false;
        [Tooltip("Position used to raycast from in order to calculate ground effect")]
        public Transform GroundEffectEmpty;
        [Tooltip("Position pitching forces are applied at")]
        public Transform PitchMoment;
        [Tooltip("Position yawing forces are applied at")]
        public Transform YawMoment;
        [Tooltip("Position traced down from to detect whether the vehicle is currently on the ground. Trace distance is 44cm. Place between the back wheels around 20cm above the height where the wheels touch the ground")]
        public Transform GroundDetector;
        [Tooltip("Distance traced down from the ground detector's position to see if the ground is there, in order to determine if the vehicle is grounded")]
        public float GroundDetectorRayDistance = .44f;
        [Tooltip("HP of the vehicle")]
        public LayerMask GroundDetectorLayers = 2049;
        [UdonSynced(UdonSyncMode.None)] public float Health = 53f;
        [Tooltip("If health is lower than this, vehicle instantly explodes")]
        public float ExplodeHealth = -200f;
        public bool RepeatingWorld = true;
        [Tooltip("Distance you can travel away from world origin before being teleported to the other side of the map. Not recommended to increase, floating point innacuracy and game freezing issues may occur if larger than default")]
        public float RepeatingWorldDistance = 20000;
        [Tooltip("Use the left hand to control the joystick and the right hand to control the throttle?")]
        public bool SwitchHandsJoyThrottle = false;
        public bool HasAfterburner = true;
        public KeyCode AfterBurnerKey = KeyCode.T;
        [Tooltip("Point in the throttle at which afterburner enables, .8 = 80%")]
        [Range(0.0f, 1f)]
        public float ThrottleAfterburnerPoint = 0.8f;
        [Tooltip("Disable Thrust/VTOL rotation values transition calculations and assume VTOL mode always (for helicopters)")]
        public bool VTOLOnly = false;
        [Tooltip("Wing scripts to enable while doing vehicle physics")]
        public UdonSharpBehaviour[] LiftSurfaces;
        private bool LiftSurfacesEnabled = true;
        [Header("Response:")]
        [Header("Some values require re-entering playmode to take full effect (ThrustVec..)")]
        [Tooltip("Vehicle thrust at max throttle without afterburner")]
        public float ThrottleStrength = 20f;
        [Tooltip("Make VR Throttle motion controls use the Y axis instead of the Z axis for adjustment (Helicopter collective)")]
        public bool VerticalThrottle = false;
        [Tooltip("Multiply how much the VR throttle moves relative to hand movement")]
        public float ThrottleSensitivity = 6f;
        [Tooltip("How quickly to reach max throttle while holding the key on desktop")]
        public float KeyboardThrottleSens = .5f;
        [Tooltip("Joystick sensitivity. Angle at which joystick will reach maximum deflection in VR")]
        public Vector3 MaxJoyAngles = new Vector3(45, 45, 45);
        [Tooltip("Joystick pitch input to be a slider-style yoke.")]
        public bool JoystickPushPullPitch = false;
        [Tooltip("Joystick sensitivity for above option.")]
        public float JoystickPushPullDistance = 0.2f;
        [Tooltip("How far down you have to push the grip button to grab the joystick and throttle")]
        public float GripSensitivity = .75f;
        [Tooltip("How much more thrust the vehicle has when in full afterburner")]
        public float AfterburnerThrustMulti = 1.5f;
        [Tooltip("How quickly the vehicle throttles up after throttle is increased (Lerp)")]
        public float AccelerationResponse = 4.5f;
        [Tooltip("How quickly the vehicle throttles down relative to how fast it throttles up after throttle is decreased")]
        public float EngineSpoolDownSpeedMulti = .5f;
        [Tooltip("How much the vehicle slows down (Speed lerped towards 0)")]
        public float AirFriction = 0.0004f;
        [Tooltip("Pitch force multiplier, (gets stronger with airspeed)")]
        public float PitchStrength = 5f;
        [Tooltip("Pitch rotation force (as multiple of PitchStrength) (doesn't get stronger with airspeed, useful for helicopters and ridiculous jets). Setting this to a nonzero value disables inversion of joystick pitch controls when vehicle is travelling backwards")]
        [Range(0.0f, 1f)]
        public float PitchThrustVecMulti = 0f;
        [Tooltip("Force that stops vehicle from pitching, (gets stronger with airspeed)")]
        public float PitchFriction = 24f;
        [Tooltip("Force that stops vehicle from pitching, (doesn't get stronger with airspeed)")]
        public float PitchConstantFriction = 0f;
        [Tooltip("How quickly the vehicle responds to changes in joystick's pitch (Lerp)")]
        public float PitchResponse = 20f;
        [Tooltip("If the vehicle is moving backwards, Pitch strength is multiplied by this. No effect if PitchThrustVecMulti is above 0")]
        public float ReversingPitchStrengthMulti = 2;
        [Tooltip("Yaw force multiplier, (gets stronger with airspeed)")]
        public float YawStrength = 3f;
        [Tooltip("Yaw rotation force (as multiple of YawStrength) (doesn't get stronger with airspeed, useful for helicopters and ridiculous jets). Setting this to a nonzero value disables inversion of joystick pitch controls when vehicle is travelling backwards")]
        [Range(0.0f, 1f)]
        public float YawThrustVecMulti = 0f;
        [Tooltip("Force that stops vehicle from yawing, (gets stronger with airspeed)")]
        public float YawFriction = 15f;
        [Tooltip("Force that stops vehicle from yawing, (doesn't get stronger with airspeed)")]
        public float YawConstantFriction = 0f;
        [Tooltip("How quickly the vehicle responds to changes in joystick's yaw (Lerp)")]
        public float YawResponse = 20f;
        [Tooltip("If the vehicle is moving backwards, Yaw strength is multiplied by this. No effect if YawThrustVecMulti is above 0")]
        public float ReversingYawStrengthMulti = 2.4f;
        [Tooltip("Roll force multiplier, (gets stronger with airspeed)")]
        public float RollStrength = 450f;
        [Tooltip("Roll rotation force (as multiple of RollStrength) (doesn't get stronger with airspeed, useful for helicopters and ridiculous jets). Setting this to a nonzero value disables inversion of joystick pitch controls when vehicle is travelling backwards")]
        [Range(0.0f, 1f)]
        public float RollThrustVecMulti = 0f;
        [Tooltip("Force that stops vehicle from rolling, (gets stronger with airspeed)")]
        public float RollFriction = 90f;
        [Tooltip("Force that stops vehicle from rolling, (doesn't get stronger with airspeed)")]
        public float RollConstantFriction = 0f;
        [Tooltip("How quickly the vehicle responds to changes in joystick's roll (Lerp)")]
        public float RollResponse = 20f;
        [Tooltip("If the vehicle is moving backwards, Roll strength is multiplied by this. No effect if RollThrustVecMulti is above 0")]
        public float ReversingRollStrengthMulti = 1.6f;//reversing = AoA > 90
        [Tooltip("Make pitching down a different strength than pitching up")]
        public float PitchDownStrMulti = .8f;
        [Tooltip("When angle of attack is negative (air is hitting the top of the plane) multiply lift by this number (useful for making vehicles weak at flying upside down)")]
        public float PitchDownLiftMulti = .8f;
        [Tooltip("Adjust the rotation of Unity's inbuilt Inertia Tensor Rotation, which is a function of rigidbodies. If set to 0, the plane will be very stable and feel boring to fly.")]
        public float InertiaTensorRotationMulti = 1;
        [Tooltip("Inverts Z axis of the Inertia Tensor Rotation, causing the direction of the yawing experienced after rolling to invert")]
        public bool InvertITRYaw = false;
        [Tooltip("Yawing added to the vehicle with changes in throttle")]
        public float AdverseYaw = 0;
        [Tooltip("Rolling added to the vehicle with the throttle up flying below RotMultiMaxSpeed. Lower speed = more adverse roll.")]
        public float AdverseRoll = 0;
        [Tooltip("Rotational inputs are multiplied by current speed to make flying at low speeds feel heavier. Above the speed input here, all inputs will be at 100%. Linear. (Meters/second)")]
        public float RotMultiMaxSpeed = 220f;
        [Tooltip("How much the the vehicle's nose is pulled toward the direction of movement on the pitch axis")]
        public float VelStraightenStrPitch = 0.035f;
        [Tooltip("How much the the vehicle's nose is pulled toward the direction of movement on the yaw axis")]
        public float VelStraightenStrYaw = 0.045f;
        [Tooltip("Angle of attack on the yaw axis above which the plane will lose control")]
        public float MaxAngleOfAttackPitch = 25f;
        [Tooltip("Angle of attack on the pitch axis above which the plane will lose control")]
        public float MaxAngleOfAttackYaw = 40f;
        [Tooltip("Shape of the angle of attack lift curve. 1= linear, high number = curve more vertical at the beginning, See this to understand (the 2 in the input represents this value, ignore everything outside the 0-1 range in the graph): https://www.wolframalpha.com/input/?i=-%28%281-x%29%5E2%29%2B1")]
        public float AoaCurveStrength = 2f;//1 = linear, >1 = convex, <1 = concave
        [Tooltip("The angle of attack curve is augmented by being MAX'd(taking the higher value) with a linear curve that is multiplied by this number. Use this value to decide how much control the plane has when beyond it's 'max' angle of attack. See AoALiftCurve.png. Pitch AoA and Yaw AoA are calculated seperately, control is reduced based on the worse value.")]
        public float HighPitchAoaMinControl = 0.2f;
        [Tooltip("See above")]
        public float HighYawAoaMinControl = 0.2f;
        [Tooltip("Enable YawAoaRollForce and PitchAoaPitchForce forces used to make vehicle rotate when in a stall, Both curves must be initialized or script will crash.")]
        public bool DoStallForces;
        [Tooltip("Curve of strength of pitch force incurred with Pitch AoA. Left side = -180 AoA(Pitch Down, right side = 180 AoA(Pitch Up).")]
        public AnimationCurve PitchAoaPitchForce = AnimationCurve.Linear(-180, 0, 180, 0);
        [Tooltip("Strength of above force")]
        public float PitchAoaPitchForceMulti = 0;
        [Tooltip("Curve of strength of roll force incurred with Yaw AoA. Left side = 0 AoA, right side = 180 AoA (Symmetrical).")]
        public AnimationCurve YawAoaRollForce = AnimationCurve.Linear(0, 0, 180, 0);
        [Tooltip("Strength of above force")]
        public float YawAoaRollForceMulti = 0;
        [Tooltip("When the plane is is at a high angle of attack you can give it a minimum amount of lift/drag, so that it doesn't just lose all air resistance.")]
        public float HighPitchAoaMinLift = 0.2f;
        [Tooltip("See above")]
        public float HighYawAoaMinLift = 0.2f;
        [Tooltip("The angle at which the aircraft's wings produce zero lift. Influenced by the wing's camber angle. A larger camber usually results in a lower ZeroLiftAoA. Helps simulate the plane's natural glide. Usually a negative value.")]
        public float ZeroLiftAoA = 0f;
        [Tooltip("Degrees per second the vehicle rotates on the ground. Uses simple object rotation with a lerp, no real physics to it.")]
        public float TaxiRotationSpeed = 35f;
        [Tooltip("How lerped the taxi movement rotation is")]
        public float TaxiRotationResponse = 2.5f;
        [Tooltip("Make taxiing more realistic by not allowing vehicle to rotate on the spot")]
        public bool DisallowTaxiRotationWhileStill = false;
        [Tooltip("When the above is ticked, This is the speed at which the vehicle will reach its full turning speed. Meters/second.")]
        public float TaxiFullTurningSpeed = 20f;
        [Tooltip("Changes suspension paramater ally to disable suspension down on V1 speed to avoid sticky wheels.")]
        public bool WheelSuspension = false;
        [Tooltip("Adjust how steep the lift curve is. Higher = more lift")]
        public float Lift = 0.00015f;
        [Tooltip("How much angle of attack on yaw turns the vehicle. Yaw steering strength in air")]
        public float SidewaysLift = .17f;
        [Tooltip("Maximum value for lift, as it's exponential it's wise to stop it at some point?")]
        public float MaxLift = 10f;
        [Tooltip("Push the vehicle up based on speed. Used to counter the fact that without it, the plane's nose will droop down due to gravity. Slower planes need a higher value.")]
        public float VelLift = 0.00015f;
        [Tooltip("Maximum Vel Lift, to stop the nose being pushed up. Technically should probably be 9.81 to counter gravity exactly")]
        public float VelLiftMax = 10f;
        [Tooltip("Vehicle will take damage if experiences more Gs that this (Internally Gs are calculated in all directions, the HUD shows only vertical Gs so it will differ slightly")]
        public float MaxGs = 40f;
        [Tooltip("Damage taken Per G above maxGs, per second.\n(Gs - MaxGs) * GDamage = damage/second")]
        public float GDamage = 10f;
        [Tooltip("Speed at which vehicle will start to take damage from a crash (m/s)")]
        public float Crash_Damage_Speed = 10f;
        [Tooltip("Speed at which vehicle will take damage equal to its max health from a crash (m/s)")]
        public float Crash_Death_Speed = 50f;
        [Tooltip("Length of the trace that looks for the ground to calculate ground effect")]
        public float GroundEffectMaxDistance = 7;
        [Tooltip("Multiply the force of the ground effect")]
        public float GroundEffectStrength = 4;
        [Tooltip("Limit the force that can be applied by ground effect")]
        public float GroundEffectLiftMax = 100;
        [Header("Response VTOL:")]
        [Tooltip("Degrees per second which the angle of the thrusters on the vehicle rotate toward desired angle")]
        public float VTOLAngleTurnRate = 90f;
        [Tooltip("Position between VTOL Min Angle and VTOL Max Angle that the plane is at by default. 0 = min, 1 = max.")]
        [Range(0.0f, 1f)]
        public float VTOLDefaultValue = 0;
        [Tooltip("Allow after burner whilst VTOL is engaged, (VTOL angle is not 0), VTOL Min Angle must be 0 for afterburner to work if this is unticked.")]
        public bool VTOLAllowAfterburner = false;
        [Tooltip("Multiply throttle strength by this value whilst vehicle is in VTOL mode, at VTOL angle of 90 degrees, this value is used, between 0 and 90 degrees the value is linearly transitioned towards this value, above 90 degrees it remains at this value")]
        public float VTOLThrottleStrengthMulti = .7f;
        [Tooltip("Minimum angle of thrust direction, 0 = straight backwards, 90 = straight down, 180 = straight forwards")]
        public float VTOLMinAngle = 0;
        [Tooltip("Maximum angle of thrust direction, 0 = straight backwards, 90 = straight down, 180 = straight forwards")]
        [Range(0.0f, 360f)]
        public float VTOLMaxAngle = 90;
        [Tooltip("Amount of Thrust Vectoring the plane has whilst in VTOL mode. (Remember thrust vectoring is as a multiple of the normal rotation values, so best to keep below 1, usually below .4)\nLeave at 1 and adjust Pitch Strength etc for helicopters")]
        public float VTOLPitchThrustVecMulti = .3f;
        [Tooltip("See above")]
        public float VTOLYawThrustVecMulti = .3f;
        [Tooltip("See above")]
        public float VTOLRollThrustVecMulti = .07f;
        [Tooltip("Speed at which the VTOL Thrust Vec Multi values will stop taking affect, scaled linearly up to this speed. Doesn't have any effect if vehicle is VTOLOnly.")]
        public float VTOLLoseControlSpeed = 120;
        [Tooltip("Strength of ground effect that doesn't depend on speed, and points in the direction of the thrust. Uses GroundEffectEmpty and GroundEffectMaxDistance. Only enabled if the vehicle has VTOL")]
        public float VTOLGroundEffectStrength = 4;
        [Tooltip("Real angle offset from (0 == thrusting backwards) that the SFEXT_O_Enter/ExitVTOL is called. The event used for disabling cruise and flight limits")]
        public float EnterVTOLEvent_Angle = 20;
        [Header("Realism:")]
        [Tooltip("Use square instead of cubic lift? (may be useful for making more realistic aircraft)")]
        public bool AlternateLift = false;
        [Tooltip("Point where the drag force is applied. Leave empty for old behaviour. Only recommended for advanced users")]
        public Transform CenterOfDrag;
        [Tooltip("Point where the thrust force is applied. Leave empty for old behaviour. Only recommended for advanced users")]
        public Transform CenterOfThrust;
        [Tooltip("Point where the lift force is applied. Leave empty for old behaviour. Only recommended for advanced users")]
        public Transform CenterOfLift;
        [Tooltip("Always apply thrust in the world space up direction? (Balloon?)")]
        public bool Thrust_AlwaysWorldUp;
        [Header("Other:")]
        public bool ReverseThrustAllowAfterburner = false;
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
        [Tooltip("Extra drag added when airspeed approaches the speed of sound")]
        public float SoundBarrierStrength = 0.0003f;
        [Tooltip("Within how many meters per second of the speed of sound does the vehicle have to be before they experience extra drag. Extra drag is scaled linearly up to the speed of sound, and dowan after it")]
        public float SoundBarrierWidth = 20f;
        [UdonSynced(UdonSyncMode.None)] public float Fuel = 900;
        [Tooltip("Amount of fuel at which throttle will start reducing")]
        public float LowFuel = 125;
        [Tooltip("Fuel consumed per second at max throttle, scales with throttle")]
        public float FuelConsumption = 1;
        [Tooltip("Fuel consumed per second at max throttle, scales with throttle")]
        public float MinFuelConsumption = .25f;
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
        [Tooltip("Impact speed that defines a small crash")]
        public float SmallCrashSpeed = 1f;
        [Tooltip("Impact speed that defines a medium crash")]
        public float MediumCrashSpeed = 8f;
        [Tooltip("Impact speed that defines a big crash")]
        public float BigCrashSpeed = 25f;
        [Tooltip("Multiply how much damage is done by missiles")]
        public float MissileDamageTakenMultiplier = 1f;
        [Tooltip("Strength of force that pushes the vehicle when a missile hits it")]
        public float MissilePushForce = 1f;
        [Tooltip("Zero height of the calculation of atmosphere thickness and HUD altitude display")]
        public float SeaLevel = -10f;
        [Tooltip("Altitude above 'Sea Level' at which the atmosphere starts thinning, In meters. 12192 = 40,000~ feet")]
        public float AtmosphereThinningStart = 12192f; //40,000 feet
        [Tooltip("Altitude above 'Sea Level' at which the atmosphere reaches zero thickness. In meters. 19812 = 65,000~ feet")]
        public float AtmosphereThinningEnd = 19812; //65,000 feet
        [Tooltip("Instantly explode locally instead of waiting for network confirmation if your client predicts target should, possible desync if target is healing when shot")]
        public bool PredictExplosion = true;
        [Tooltip("Send event when someone gets a kill on this vehicle (SFEXT_O_GotAKill)")]
        public bool SendKillEvents;
        [Tooltip("When in desktop mode, make the joystick input square? (for game controllers, disable for actual joysticks")]
        public bool SquareJoyInput = true;
        [Tooltip("Set Engine On when entering the vehicle?")]
        public bool EngineOnOnEnter = true;
        [Tooltip("Set Engine Off when entering the vehicle?")]
        public bool EngineOffOnExit = true;
        public bool DroneMode = false;
        [Tooltip("Maximum force to cause vehicle to spin when in 'wrecked' state 0HP = 0 spin, -ExplodeHealth HP = max spin, value used is random between 0 and theses values one ach axis")]
        [SerializeField] private Vector3 WreckedSpinForce = new Vector3(2, 3, 4);
        [Header("Debug:")]
        [FieldChangeCallback(nameof(EngineOn))] public bool _EngineOn = false;
        public bool EngineOn
        {
            set
            {
                //disable thrust vectoring if engine off
                if (value)
                {
                    if (!_EngineOn)
                    {
                        EntityControl.SendEventToExtensions("SFEXT_G_EngineOn");
                        VehicleAnimator.SetBool("EngineOn", true);
                        WakeUp();

                        PitchThrustVecMulti = PitchThrustVecMultiStart;
                        YawThrustVecMulti = YawThrustVecMultiStart;
                        RollThrustVecMulti = RollThrustVecMultiStart;
                        ReversingPitchStrengthZero = ReversingPitchStrengthZeroStart;
                        ReversingYawStrengthZero = ReversingYawStrengthZeroStart;
                        ReversingRollStrengthZero = ReversingRollStrengthZeroStart;
                        if (HasWheelColliders)
                        {
                            foreach (WheelCollider wheel in VehicleWheelColliders)
                            {
                                wheel.motorTorque = 0.00000000000000000000000000000000001f;
                                wheel.brakeTorque = 0;
                            }
                        }
                    }
                }
                else
                {
                    if (_EngineOn)
                    {
                        EntityControl.SendEventToExtensions("SFEXT_G_EngineOff");
                        Taxiinglerper = 0;
                        VehicleAnimator.SetBool("EngineOn", false);

                        PitchThrustVecMulti = 0;
                        YawThrustVecMulti = 0;
                        RollThrustVecMulti = 0;
                        ReversingPitchStrengthZero = ReversingPitchStrengthZeroStart;
                        ReversingYawStrengthZero = ReversingYawStrengthZeroStart;
                        ReversingRollStrengthZero = ReversingRollStrengthZeroStart;

                        if (HasWheelColliders)
                        {
                            foreach (WheelCollider wheel in VehicleWheelColliders)
                            { wheel.motorTorque = 0; }
                        }
                    }
                }
                _EngineOn = value;
            }
            get => _EngineOn;
        }
        public void SFEXT_L_SetEngineOn()//send this event from other scripts locally to turn engine on for everyone (grapple uses it)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetEngineOn)); }
        public void SFEXT_L_SetEngineOff()
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetEngineOff)); }
        public void SetEngineOn()
        { EngineOn = true; }
        public void SetEngineOff()
        { EngineOn = false; }
        [System.NonSerializedAttribute] public float AllGs;
        [System.NonSerializedAttribute][UdonSynced(UdonSyncMode.Linear)] public float EngineOutput = 0f;
        [System.NonSerializedAttribute] public Vector3 CurrentVel = Vector3.zero;
        [System.NonSerializedAttribute][UdonSynced(UdonSyncMode.Linear)] public float VertGs = 1f;
        [System.NonSerializedAttribute] public float AngleOfAttackPitch;
        [System.NonSerializedAttribute] public float AngleOfAttackYaw;
        [System.NonSerializedAttribute][UdonSynced(UdonSyncMode.Linear)] public float AngleOfAttack;//MAX of yaw & pitch aoa //used by effectscontroller and hudcontroller
        [System.NonSerializedAttribute] public bool Occupied = false; //this is true if someone is sitting in pilot seat
        [System.NonSerialized] public int NumPassengers;
        [System.NonSerializedAttribute] public float VTOLAngle;

        [System.NonSerializedAttribute] public Animator VehicleAnimator;

#if UNITY_EDITOR // for debugging
        public Vector3 ThrustAcc, LiftAcc;
        public Vector3 VehicleTorque;
        public Vector3 Pitching;
        public Vector3 Yawing;
# else
        private Vector3 ThrustAcc, LiftAcc;
        private Vector3 VehicleTorque;
        private Vector3 Pitching;
        private Vector3 Yawing;
#endif
        [System.NonSerializedAttribute] public Rigidbody VehicleRigidbody;
        [System.NonSerializedAttribute] public Transform VehicleTransform;
        [System.NonSerializedAttribute] public float VTOLAngleForward90;//dot converted to angle, 0=0 90=1 max 1, for adjusting values that change with engine angle
        [System.NonSerializedAttribute] public float VTOLAngleForwardDot;
        [System.NonSerializedAttribute] public bool VTOLAngleForward = true;
        private GameObject VehicleGameObj;
        [System.NonSerializedAttribute] public Transform CenterOfMass;
        private float LerpedRoll;
        private float LerpedPitch;
        private float LerpedYaw;
        [System.NonSerializedAttribute] public bool ThrottleGripLastFrame = false;
        [System.NonSerializedAttribute] public bool JoystickGripLastFrame = false;
        Quaternion JoystickZeroPoint;
        Quaternion VehicleRotLastFrame;
        private Vector3 JoystickZeroPosition;
        [System.NonSerializedAttribute] public float PlayerThrottle;
        private float TempThrottle;
        private float ThrottleZeroPoint;
        [System.NonSerializedAttribute] public float ThrottleInput = 0f;
        private float roll = 0f;
        private float pitch = 0f;
        private float yaw = 0f;
        [System.NonSerializedAttribute] public float FullHealth;
        [System.NonSerializedAttribute] public bool Taxiing = false;
        [System.NonSerializedAttribute] public bool Floating = false;
        [System.NonSerializedAttribute][UdonSynced(UdonSyncMode.Linear)] public Vector3 RotationInputs;
        [System.NonSerializedAttribute] public bool Piloting = false;
        [System.NonSerializedAttribute] public bool Passenger = false;
        [System.NonSerializedAttribute] public bool InEditor = true;
        [System.NonSerializedAttribute] public bool InVR = false;
        [System.NonSerializedAttribute] public Vector3 LastFrameVel = Vector3.zero;
        [System.NonSerializedAttribute] public VRCPlayerApi localPlayer;
        [System.NonSerializedAttribute] public float AtmoshpereFadeDistance;
        [System.NonSerializedAttribute] public float AtmosphereHeightThing;
        [System.NonSerializedAttribute] public float Atmosphere = 1;
        private float AoALiftYaw;
        private float AoALiftPitch;
        private float AoALift_Min;
        [System.NonSerializedAttribute] public float Taxiinglerper;
        [System.NonSerializedAttribute] public float ExtraDrag = 1;
        [System.NonSerializedAttribute] public float ExtraLift = 1;
        [System.NonSerializedAttribute] public float ExtraVelLift = 1;
        private float ReversingPitchStrength;
        private float ReversingYawStrength;
        private float ReversingRollStrength;
        private float ReversingPitchStrengthZero;
        private float ReversingYawStrengthZero;
        private float ReversingRollStrengthZero;
        private float ReversingPitchStrengthZeroStart;
        private float ReversingYawStrengthZeroStart;
        private float ReversingRollStrengthZeroStart;
        [System.NonSerializedAttribute] public float Speed;
        [System.NonSerializedAttribute] public float AirSpeed;
        [System.NonSerializedAttribute] public bool IsOwner = false;
        [System.NonSerializedAttribute] public Vector3 FinalWind;//includes Gusts
        [System.NonSerializedAttribute] public Vector3 AirVel;
        private float StillWindMulti;//multiplies the speed of the wind by the speed of the plane when taxiing to prevent still planes flying away
        private int ThrustVecGrounded = 1;
        private float SoundBarrier;
        [System.NonSerializedAttribute] public float FullFuel;
        private float LowFuelDivider;
        [System.NonSerializedAttribute] public bool Asleep;
        private bool Initialized;
        [System.NonSerializedAttribute] public bool IsAirVehicle = true;//could be checked by any script targeting/checking this vehicle to see if it is the kind of vehicle they're looking for
        [System.NonSerializedAttribute] public float FullGunAmmo;
        //use these for whatever, Only MissilesIncomingHeat is used by the prefab
        [System.NonSerializedAttribute] public int MissilesIncomingHeat = 0;
        [System.NonSerializedAttribute] public int MissilesIncomingRadar = 0;
        [System.NonSerializedAttribute] public int MissilesIncomingOther = 0;
        [System.NonSerializedAttribute] public bool DoAAMTargeting;
        [System.NonSerializedAttribute] public Rigidbody GDHitRigidbody;
        [System.NonSerializedAttribute] public bool UsingManualSync;
        private bool RepeatingWorldCheckAxis;
        bool FloatingLastFrame = false;
        bool GroundedLastFrame = false;
        bool VTOL360;
        [System.NonSerializedAttribute] public float VTOLAngleDegrees;
        private float VelLiftStart;
        private float VelLiftMaxStart;
        private bool HasAirBrake;//set to false if air brake strength is 0
        private float HandDistanceZLastFrame;
        private float PitchThrustVecMultiStart;
        private float YawThrustVecMultiStart;
        private float RollThrustVecMultiStart;
        [System.NonSerializedAttribute] public bool InVTOL;
        [System.NonSerializedAttribute] public bool VTOLenabled;
        [System.NonSerializedAttribute] public float VTOLAngleInput;
        private float ThrottleNormalizer;
        private float VTOLAngleDivider;
        private float ABNormalizer;
        private float EngineOutputLastFrame;
        bool HasWheelColliders = false;
        private float TaxiFullTurningSpeedDivider;
        private float vtolangledif;
        [System.NonSerializedAttribute] public bool JoyStickGrippingLastFrame_toggle = false;
        private bool GrabToggle;
        private int JoyStickReleaseCount;
        private float LastGripTime;
        [System.NonSerializedAttribute] public WheelCollider[] VehicleWheelColliders;

        private float[] initSuspensionDistance;
        private float[] initTargetPosition;

        [System.NonSerializedAttribute] public bool LowFuelLastFrame;
        [System.NonSerializedAttribute] public bool NoFuelLastFrame;
        [System.NonSerializedAttribute] public float ThrottleStrengthAB;
        [System.NonSerializedAttribute] public float FuelConsumptionAB;
        [System.NonSerializedAttribute] public bool AfterburnerOn;
        [System.NonSerializedAttribute] public bool PitchDown;//air is hitting plane from the top
        private float GDamageToTake;

        [System.NonSerializedAttribute] public int NumActiveFlares;
        [System.NonSerializedAttribute] public int NumActiveChaff;
        [System.NonSerializedAttribute] public int NumActiveOtherCM;
        [System.NonSerializedAttribute] public bool UseAtmospherePositionOffset = false;
        [System.NonSerializedAttribute] public float AtmospherePositionOffset = 0;//set UseAtmospherePositionOffset true to use this for floating origin system
        [System.NonSerializedAttribute] public float Limits = 1;//specially used by limits function
                                                                //this stuff can be used by DFUNCs
                                                                //if these == 0 then they are not disabled. Being an int allows more than one extension to disable it at a time
                                                                //the bools exists to save externs every frame
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
        [System.NonSerializedAttribute] public Vector3 JoystickOverride;
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
        [System.NonSerializedAttribute] public bool _DisablePhysicsApplication;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(DisablePhysicsApplication_))] public int DisablePhysicsApplication = 0;
        public int DisablePhysicsApplication_
        {
            set
            {
                if (value > 0 && DisablePhysicsApplication == 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_DisablePhysicsApplication_Activated");
                }
                else if (value == 0 && DisablePhysicsApplication > 0)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_DisablePhysicsApplication_Deactivated");
                }
                _DisablePhysicsApplication = value > 0;
                DisablePhysicsApplication = value;
            }
            get => DisablePhysicsApplication;
        }
#if UNITY_EDITOR
        public bool SetVel;
        public Vector3 VelToSet;
#endif
        public void SFEXT_L_EntityStart()
        {
            Initialized = true;
            VehicleGameObj = EntityControl.gameObject;
            VehicleTransform = EntityControl.transform;
            VehicleRigidbody = EntityControl.GetComponent<Rigidbody>();

            UsingManualSync = !EntityControl.EntityObjectSync;

            localPlayer = Networking.LocalPlayer;
            InVR = EntityControl.InVR;
            if (localPlayer == null)
            {
                Piloting = true;
                Occupied = true;
                if (!UsingManualSync)
                {
                    VehicleRigidbody.drag = 0;
                    VehicleRigidbody.angularDrag = 0;
                }
            }
            else
            {
                if (EntityControl.IsOwner)
                {
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
            InEditor = EntityControl.InEditor;
            IsOwner = EntityControl.IsOwner;

            EnableLiftSurfaces(IsOwner);

            VehicleWheelColliders = VehicleMesh.GetComponentsInChildren<WheelCollider>(true);
            if (VehicleWheelColliders.Length != 0)
            {
                HasWheelColliders = true;

                initSuspensionDistance = new float[VehicleWheelColliders.Length];
                initTargetPosition = new float[VehicleWheelColliders.Length];

                for (int wheelIndex = 0; wheelIndex < VehicleWheelColliders.Length; wheelIndex++)
                {
                    WheelCollider wheel = VehicleWheelColliders[wheelIndex];
                    initSuspensionDistance[wheelIndex] = wheel.suspensionDistance;
                    initTargetPosition[wheelIndex] = wheel.suspensionSpring.targetPosition;
                }
            }

            float RBMass = VehicleRigidbody.mass;
            foreach (WheelCollider wheel in VehicleWheelColliders)
            {
                wheel.mass *= RBMass;
                JointSpring SusiSpring = wheel.suspensionSpring;
                SusiSpring.spring *= RBMass;
                SusiSpring.damper *= RBMass;
                wheel.suspensionSpring = SusiSpring;
            }

            VehicleAnimator = EntityControl.GetComponent<Animator>();

            FullHealth = Health;
            FullFuel = Fuel;

            VelLiftMaxStart = VelLiftMax;
            VelLiftStart = VelLift;

            PitchThrustVecMultiStart = PitchThrustVecMulti;
            YawThrustVecMultiStart = YawThrustVecMulti;
            RollThrustVecMultiStart = RollThrustVecMulti;

            CenterOfMass = EntityControl.CenterOfMass;
            SetCoMMeshOffset();

            if (AtmosphereThinningStart > AtmosphereThinningEnd) { AtmosphereThinningEnd = (AtmosphereThinningStart + 1); }
            AtmoshpereFadeDistance = (AtmosphereThinningEnd + SeaLevel) - (AtmosphereThinningStart + SeaLevel); //for finding atmosphere thinning gradient
            AtmosphereHeightThing = (AtmosphereThinningStart + SeaLevel) / (AtmoshpereFadeDistance); //used to add back the height to the atmosphere after finding gradient

            //used to set each rotation axis' reversing behaviour to inverted if 0 thrust vectoring, and not inverted if thrust vectoring is non-zero.
            //the variables are called 'Zero' because they ask if thrustvec is set to 0.
            if (VTOLOnly)//Never do this for heli-like vehicles
            {
                ReversingPitchStrengthZero = 1;
                ReversingYawStrengthZero = 1;
                ReversingRollStrengthZero = 1;
                ReversingPitchStrengthZeroStart = 1;
                ReversingYawStrengthZeroStart = 1;
                ReversingRollStrengthZeroStart = 1;
            }
            else
            {
                ReversingPitchStrengthZeroStart = ReversingPitchStrengthZero = PitchThrustVecMulti == 0 ? -ReversingPitchStrengthMulti : 1;
                ReversingYawStrengthZeroStart = ReversingYawStrengthZero = YawThrustVecMulti == 0 ? -ReversingYawStrengthMulti : 1;
                ReversingRollStrengthZeroStart = ReversingRollStrengthZero = RollThrustVecMulti == 0 ? -ReversingRollStrengthMulti : 1;
            }


            if (VTOLOnly) { VTOLenabled = true; }
            VTOL360 = VTOLMinAngle == 0f && VTOLMaxAngle == 360f;

            if (!HasAfterburner) { ThrottleAfterburnerPoint = 1; }
            ThrottleNormalizer = 1 / ThrottleAfterburnerPoint;
            ABNormalizer = 1 / (1 - ThrottleAfterburnerPoint);

            FuelConsumptionAB = (FuelConsumption * FuelConsumptionABMulti) - FuelConsumption;
            ThrottleStrengthAB = (ThrottleStrength * AfterburnerThrustMulti) - ThrottleStrength;

            vtolangledif = VTOLMaxAngle - VTOLMinAngle;
            VTOLAngleDivider = VTOLAngleTurnRate / vtolangledif;
            VTOLAngle = VTOLAngleInput = VTOLDefaultValue;
            VTOLAngleDegrees = VTOLMinAngle + (vtolangledif * VTOLAngle);

            if (GroundDetectorRayDistance == 0 || !GroundDetector)
            { DisableGroundDetection_++; }

            if (GroundEffectEmpty == null)
            {
                Debug.Log("GroundEffectEmpty not found, using CenterOfMass instead");
                GroundEffectEmpty = CenterOfMass;
            }

            LowFuelDivider = 1 / LowFuel;

            if (DisallowTaxiRotationWhileStill)
            {
                TaxiFullTurningSpeedDivider = 1 / TaxiFullTurningSpeed;
            }
            if (!ControlsRoot)
            { ControlsRoot = VehicleTransform; }

            if (ExplodeHealth > 0) { ExplodeHealth = 0f; }

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
                bool invincible = EntityControl.dead || EntityControl.invincible;
                if (!wrecked && !invincible)
                {
                    float thisGDMG = GDamageToTake;
                    //G/crash Damage
                    if (GDamageToTake > 0)
                    {
                        Health -= GDamageToTake * .01f * GDamage;//take damage of GDamage per second per G above MaxGs
                        GDamageToTake = 0;
                    }
                    if (Health <= 0f)
                    {
                        if (Piloting)
                        { EntityControl.SendEventToExtensions("SFEXT_O_Suicide"); }
                        EntityControl.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetWrecked");
                    }
                }
                else if (!invincible)
                {
                    if (GDamageToTake > 0)
                    {
                        Health -= GDamageToTake * .01f * GDamage;//take damage of GDamage per second per G above MaxGs
                        GDamageToTake = 0;
                    }
                    if (Health <= ExplodeHealth)
                    {
                        NetworkExplode();
                    }
                }
                else
                {
                    GDamageToTake = 0;
                }

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
                CurrentVel = VehicleRigidbody.velocity;//CurrentVel is set by SAV_SyncScript for non owners
                if (Piloting)
                {
                    DoRepeatingWorld();

                    if (!_DisablePhysicsAndInputs)
                    {
                        //collect inputs
                        int Wi = Input.GetKey(KeyCode.W) ? 1 : 0; //inputs as ints
                        int Si = Input.GetKey(KeyCode.S) ? -1 : 0;
                        int Ai = Input.GetKey(KeyCode.A) ? -1 : 0;
                        int Di = Input.GetKey(KeyCode.D) ? 1 : 0;
                        int Qi = Input.GetKey(KeyCode.Q) ? -1 : 0;
                        int Ei = Input.GetKey(KeyCode.E) ? 1 : 0;
                        int upi = Input.GetKey(KeyCode.UpArrow) ? 1 : 0;
                        int downi = Input.GetKey(KeyCode.DownArrow) ? -1 : 0;
                        int lefti = Input.GetKey(KeyCode.LeftArrow) ? -1 : 0;
                        int righti = Input.GetKey(KeyCode.RightArrow) ? 1 : 0;
                        bool Shift = Input.GetKey(KeyCode.LeftShift);
                        bool Ctrl = Input.GetKey(KeyCode.LeftControl);
                        int Shifti = Shift ? 1 : 0;
                        int LeftControli = Ctrl ? 1 : 0;
                        float LGrip = 0;
                        float RGrip = 0;
                        if (!InEditor)
                        {
                            LGrip = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger");
                            RGrip = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
                        }
                        //MouseX = Input.GetAxisRaw("Mouse X");
                        //MouseY = Input.GetAxisRaw("Mouse Y");
                        Vector3 VRJoystickPos = Vector3.zero;

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
                                if (Time.time - LastGripTime < .25f)
                                {
                                    GrabToggle = true;
                                    JoyStickReleaseCount = 0;
                                }
                                LastGripTime = Time.time;
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
                        //VR Joystick
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
                                {
                                    JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation;
                                    localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .05f, .222f, 35);
                                }//rotation of the controller relative to the plane when it was pressed
                                else
                                {
                                    JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;
                                    localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .222f, 35);
                                }
                            }
                            //difference between the vehicle and the hand's rotation, and then the difference between that and the JoystickZeroPoint, finally rotated by the vehicles rotation to turn it back to vehicle space
                            Quaternion JoystickDifference;
                            JoystickDifference = Quaternion.Inverse(ControlsRoot.rotation) *
                                (SwitchHandsJoyThrottle ? localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation
                                                        : localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation)
                            * Quaternion.Inverse(JoystickZeroPoint)
                             * ControlsRoot.rotation;

                            //create normalized vectors facing towards the 'forward' and 'up' directions of the joystick
                            Vector3 JoystickPosYaw = (JoystickDifference * Vector3.forward);
                            Vector3 JoystickPos = (JoystickDifference * Vector3.up);
                            //use acos to convert the relevant elements of the array into radians, re-center around zero, then normalize between -1 and 1 and dovide for desired deflection
                            //the clamp is there because rotating a vector3 can cause it to go a miniscule amount beyond length 1, resulting in NaN (crashes vrc)
                            VRJoystickPos.x = -((Mathf.Acos(Mathf.Clamp(JoystickPos.z, -1, 1)) - 1.5707963268f) * Mathf.Rad2Deg) / MaxJoyAngles.x;
                            VRJoystickPos.y = -((Mathf.Acos(Mathf.Clamp(JoystickPosYaw.x, -1, 1)) - 1.5707963268f) * Mathf.Rad2Deg) / MaxJoyAngles.y;
                            VRJoystickPos.z = -((Mathf.Acos(Mathf.Clamp(JoystickPos.x, -1, 1)) - 1.5707963268f) * Mathf.Rad2Deg) / MaxJoyAngles.z;

                            // Override pitch input for Push-Pull style York
                            if (JoystickPushPullPitch)
                            {
                                Vector3 joystickPosition = ControlsRoot.InverseTransformPoint(localPlayer.GetTrackingData(SwitchHandsJoyThrottle ? VRCPlayerApi.TrackingDataType.LeftHand : VRCPlayerApi.TrackingDataType.RightHand).position);
                                if (!JoystickGripLastFrame)
                                {
                                    JoystickZeroPosition = joystickPosition;
                                    localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .222f, 35);
                                }
                                VRJoystickPos.x = Mathf.Clamp((joystickPosition.z - JoystickZeroPosition.z) / JoystickPushPullDistance, -1.0f, 1.0f);
                            }

                            JoystickGripLastFrame = true;
                        }
                        else
                        {
                            VRJoystickPos = Vector3.zero;
                            if (JoystickGripLastFrame)//first frame you let go of joystick
                            {
                                EntityControl.SendEventToExtensions("SFEXT_O_JoystickDropped");
                                if (SwitchHandsJoyThrottle)
                                { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .05f, .222f, 35); }
                                else
                                { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .222f, 35); }
                            }
                            JoystickGripLastFrame = false;
                        }
                        if (!_DisableThrottleControl)
                        {
                            if (HasAfterburner && !AfterburnerOn)
                            {
                                PlayerThrottle = Mathf.Clamp(PlayerThrottle + ((Shifti - LeftControli) * KeyboardThrottleSens * DeltaTime), 0, ThrottleAfterburnerPoint);
                            }
                            else
                            { PlayerThrottle = Mathf.Clamp(PlayerThrottle + ((Shifti - LeftControli) * KeyboardThrottleSens * DeltaTime), 0, 1f); }
                        }
                        //VR Throttle
                        if (ThrottleGrip > GripSensitivity && !_DisableThrottleControl)
                        {
                            Vector3 handdistance;
                            if (SwitchHandsJoyThrottle)
                            {
                                handdistance = ControlsRoot.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                            }
                            else
                            {
                                handdistance = ControlsRoot.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                            }
                            handdistance = ControlsRoot.InverseTransformDirection(handdistance);

                            float HandThrottleAxis;
                            if (VerticalThrottle)
                            {
                                HandThrottleAxis = handdistance.y;
                            }
                            else
                            {
                                HandThrottleAxis = handdistance.z;
                            }

                            if (!ThrottleGripLastFrame)
                            {
                                if (SwitchHandsJoyThrottle)
                                { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .222f, 35); }
                                else
                                { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .05f, .222f, 35); }
                                EntityControl.SendEventToExtensions("SFEXT_O_ThrottleGrabbed");
                                ThrottleZeroPoint = HandThrottleAxis;
                                TempThrottle = PlayerThrottle;
                                HandDistanceZLastFrame = 0;
                            }
                            float ThrottleDifference = ThrottleZeroPoint - HandThrottleAxis;
                            ThrottleDifference *= ThrottleSensitivity;
                            // check if VTOLING and VTOL+AB is not allowed OR if reversing and reverse+AB is not allowed 
                            bool AB_Disallowed = (!VTOLAllowAfterburner && VTOLAngleDegrees > EnterVTOLEvent_Angle) || (!ReverseThrustAllowAfterburner && _InvertThrust);
                            //Detent function to prevent you going into afterburner by accident (bit of extra force required to turn on AB (actually hand speed))
                            if (((HandDistanceZLastFrame - HandThrottleAxis) * ThrottleSensitivity > .05f)/*detent overcome*/ && !AB_Disallowed && Fuel > LowFuel || ((PlayerThrottle > ThrottleAfterburnerPoint/*already in afterburner*/&& !AB_Disallowed) || !HasAfterburner))
                            {
                                PlayerThrottle = Mathf.Clamp(TempThrottle + ThrottleDifference, 0, 1);
                            }
                            else
                            {
                                PlayerThrottle = Mathf.Clamp(TempThrottle + ThrottleDifference, 0, ThrottleAfterburnerPoint);
                            }
                            HandDistanceZLastFrame = HandThrottleAxis;
                            ThrottleGripLastFrame = true;
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
                            }
                            ThrottleGripLastFrame = false;
                        }

                        if (!_DisableTaxiRotation && Taxiing && _EngineOn)
                        {
                            AngleOfAttackYaw = 0;
                            AngleOfAttackPitch = 0;
                            AngleOfAttack = 0;//prevent stall sound and aoavapor when on ground
                                              //rotate if trying to yaw
                            float TaxiingStillMulti = 1;
                            if (DisallowTaxiRotationWhileStill)
                            { TaxiingStillMulti = Mathf.Min(Speed * TaxiFullTurningSpeedDivider, 1); }
                            Taxiinglerper = Mathf.Lerp(Taxiinglerper, RotationInputs.y * TaxiRotationSpeed * Time.smoothDeltaTime * TaxiingStillMulti, 1 - Mathf.Pow(0.5f, TaxiRotationResponse * DeltaTime));
                            VehicleTransform.Rotate(VehicleTransform.up, Taxiinglerper);
                            VehicleRigidbody.rotation = VehicleTransform.rotation;//Unity 2022.3.6f1 bug workaround

                            StillWindMulti = Mathf.Min(Speed * .1f, 1);
                            ThrustVecGrounded = 0;
                        }
                        else
                        {
                            StillWindMulti = 1;
                            ThrustVecGrounded = 1;
                            Taxiinglerper = 0;
                        }
                        //keyboard control for afterburner
                        if (!_DisableThrottleControl)
                        {
                            if (Input.GetKeyDown(AfterBurnerKey))
                            {
                                if (HasAfterburner)
                                {
                                    if ((VTOLAngleDegrees < EnterVTOLEvent_Angle || VTOLAllowAfterburner) && (ReverseThrustAllowAfterburner || !_InvertThrust))
                                    {
                                        if (PlayerThrottle == 1)
                                        { PlayerThrottle = ThrottleAfterburnerPoint; }
                                        else
                                        { PlayerThrottle = 1; }
                                    }
                                    else
                                    {
                                        if (PlayerThrottle == ThrottleAfterburnerPoint)
                                        { PlayerThrottle = 0; }
                                        else
                                        { PlayerThrottle = ThrottleAfterburnerPoint; }
                                    }
                                }
                                else if (PlayerThrottle < 1)
                                { PlayerThrottle = 1; }
                                else
                                { PlayerThrottle = 0; }
                            }
                            if (_ThrottleOverridden)
                            {
                                //handled in FixedUpdate()
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
                        }
                        FuelEvents();

                        if (!_DisableJoystickControl)
                        {
                            if (_JoystickOverridden)
                            {
                                //handled in FixedUpdate()
                            }
                            else//joystick override disabled, player has control
                            {
                                if (!InVR)
                                {
                                    //allow stick flight in desktop mode
                                    Vector2 LStickPos = Vector2.zero;
                                    Vector2 RStickPos = Vector2.zero;
                                    if (!InEditor)
                                    {
                                        LStickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
                                        LStickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
                                        RStickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
                                        //RStickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
                                    }
                                    VRJoystickPos.x = LStickPos.y;
                                    VRJoystickPos.y = RStickPos.x;
                                    VRJoystickPos.z = LStickPos.x;
                                    //make stick input square
                                    if (SquareJoyInput)
                                    {
                                        Vector2 LeftStick = new Vector2(VRJoystickPos.z, VRJoystickPos.x);
                                        if (Mathf.Abs(LeftStick.x) > Mathf.Abs(LeftStick.y))
                                        {
                                            if (Mathf.Abs(LeftStick.x) > 0)
                                            {
                                                float temp = LeftStick.magnitude / Mathf.Abs(LeftStick.x);
                                                LeftStick *= temp;
                                            }
                                        }
                                        else if (Mathf.Abs(LeftStick.y) > 0)
                                        {
                                            float temp = LeftStick.magnitude / Mathf.Abs(LeftStick.y);
                                            LeftStick *= temp;
                                        }
                                        VRJoystickPos.z = LeftStick.x;
                                        VRJoystickPos.x = LeftStick.y;
                                    }
                                }

                                RotationInputs.x = Mathf.Clamp(VRJoystickPos.x + Wi + Si + downi + upi, -1, 1);
                                if ((RotationInputs.x < 0 && VertGs > 0) || (RotationInputs.x > 0 && VertGs < 0)) { RotationInputs.x *= Limits; }
                                RotationInputs.y = Mathf.Clamp(Qi + Ei + VRJoystickPos.y, -1, 1) * Limits;
                                //roll isn't subject to flight limits
                                RotationInputs.z = Mathf.Clamp(((VRJoystickPos.z + Ai + Di + lefti + righti) * -1), -1, 1);
                                SetRotInputs();
                            }
                        }

                        //ability to adjust input to be more precise at low amounts. 'exponant'
                        /* RotationInputs.x = RotationInputs.x > 0 ? Mathf.Pow(RotationInputs.x, StickInputPower) : -Mathf.Pow(Mathf.Abs(RotationInputs.x), StickInputPower);
                        RotationInputs.y = RotationInputs.y > 0 ? Mathf.Pow(RotationInputs.y, StickInputPower) : -Mathf.Pow(Mathf.Abs(RotationInputs.y), StickInputPower);
                        RotationInputs.z = RotationInputs.z > 0 ? Mathf.Pow(RotationInputs.z, StickInputPower) : -Mathf.Pow(Mathf.Abs(RotationInputs.z), StickInputPower); */

                        if (VTOLenabled)
                        {
                            SetVTOLRotValues();
                            if (!VTOLOnly)
                            {
                                if (VTOLAngleDegrees > EnterVTOLEvent_Angle && VTOLAngleDegrees < 360 - EnterVTOLEvent_Angle)
                                {
                                    if (!InVTOL)
                                    {
                                        EntityControl.SendEventToExtensions("SFEXT_O_EnterVTOL");
                                        InVTOL = true;
                                        if (!VTOLAllowAfterburner)
                                        {
                                            if (AfterburnerOn)
                                            { PlayerThrottle = ThrottleAfterburnerPoint; }
                                        }
                                    }
                                }
                                else
                                {
                                    if (InVTOL)
                                    {
                                        //check angle
                                        EntityControl.SendEventToExtensions("SFEXT_O_ExitVTOL");
                                        InVTOL = false;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (Taxiing)
                    {
                        StillWindMulti = Mathf.Min(Speed * .1f, 1);
                        ThrustVecGrounded = 0;
                    }
                    else { StillWindMulti = 1; ThrustVecGrounded = 1; }
                    if (_EngineOn)
                    {
                        //this should allow remote piloting using extensions
                        if (_ThrottleOverridden)
                        { ThrottleInput = PlayerThrottle = Mathf.Min(ThrottleOverride, Fuel * LowFuelDivider); }
                        FuelEvents();
                    }
                    DoRepeatingWorld();
                }
                SoundBarrier = (1 - Mathf.Clamp(Mathf.Abs(Speed - 343) / SoundBarrierWidth, 0, 1)) * SoundBarrierStrength;
            }
            else//non-owners need to know these values
            {
                Speed = AirSpeed = CurrentVel.magnitude;//wind speed is local anyway, so just use ground speed for non-owners
                                                        //AirVel = VehicleRigidbody.velocity - Wind;//wind isn't synced so this will be wrong
                                                        //AirSpeed = AirVel.magnitude;
            }
        }
        private void FixedUpdate()
        {

#if UNITY_EDITOR
            if (SetVel)
            {
                VehicleRigidbody.velocity = VelToSet;
            }
#endif
            if (IsOwner)
            {
                float DeltaTime = Time.fixedDeltaTime;
                Speed = VehicleRigidbody.velocity.magnitude;
                if (Piloting)
                {
                    WindAndAoA();
                }
                else if (Speed > .01f)
                {
                    WindAndAoA();
                }
                else if (!Asleep && GroundedLastFrame && !_KeepAwake)
                {
                    FallAsleep();
                }
                if (!_DisablePhysicsAndInputs)
                {
                    if (_JoystickOverridden)
                    {
                        RotationInputs = JoystickOverride;
                        SetRotInputs();
                    }
                    //Lerp the inputs for 'engine response', throttle decrease response is slower than increase (EngineSpoolDownSpeedMulti)
                    if (_EngineOn)
                    {
                        if (EngineOutput < ThrottleInput)
                        { EngineOutput = Mathf.Lerp(EngineOutput, ThrottleInput, 1 - Mathf.Pow(0.5f, AccelerationResponse * DeltaTime)); }
                        else
                        { EngineOutput = Mathf.Lerp(EngineOutput, ThrottleInput, 1 - Mathf.Pow(0.5f, AccelerationResponse * EngineSpoolDownSpeedMulti * DeltaTime)); }
                    }
                    else
                    {
                        EngineOutput = Mathf.Lerp(EngineOutput, 0, 1 - Mathf.Pow(0.5f, AccelerationResponse * EngineSpoolDownSpeedMulti * DeltaTime));
                    }
                    float sidespeed = 0;
                    float downspeed = 0;
                    float SpeedLiftFactor = 0;
                    float SpeedLiftFactor_Lift = 0;
                    float SpeedLiftFactor_pd = 0;
                    float rotlift = 0;

                    if (!Asleep)
                    {
                        //used to create air resistance for updown and sideways if your movement direction is in those directions
                        //to add physics to plane's yaw and pitch, accel angvel towards velocity, and add force to the plane
                        //and add wind
                        sidespeed = Vector3.Dot(AirVel, VehicleTransform.right);
                        downspeed = -Vector3.Dot(AirVel, VehicleTransform.up);
                        PitchDown = downspeed < 0;//air is hitting plane from above?
                        SpeedLiftFactor = AirSpeed * AirSpeed;

                        rotlift = Mathf.Min(AirSpeed / RotMultiMaxSpeed, 1);//using a simple linear curve for increasing control as you move faster

                        //thrust vectoring airplanes have a minimum rotation control
                        //AoALiftPitch is used here before being modified in the next block of code on purpose
                        float minlifttemp = rotlift * Mathf.Min(AoALiftPitch, AoALiftYaw);

                        float pitchnew = pitch * Mathf.Max(PitchThrustVecMulti * ThrustVecGrounded, minlifttemp);
                        float yawnew = yaw * Mathf.Max(YawThrustVecMulti * ThrustVecGrounded, minlifttemp);
                        float rollnew = roll * Mathf.Max(RollThrustVecMulti * ThrustVecGrounded, minlifttemp);

                        //Lerp the inputs for 'rotation response'   
                        LerpedRoll = Mathf.Lerp(LerpedRoll, rollnew, 1 - Mathf.Pow(0.5f, RollResponse * DeltaTime));
                        LerpedPitch = Mathf.Lerp(LerpedPitch, pitchnew, 1 - Mathf.Pow(0.5f, PitchResponse * DeltaTime));
                        LerpedYaw = Mathf.Lerp(LerpedYaw, yawnew, 1 - Mathf.Pow(0.5f, YawResponse * DeltaTime));

                        //rotation inputs are done, now we can set the minimum lift/drag when at high aoa, this should be higher than 0 because if it's 0 you will have 0 drag when at 90 degree AoA.
                        AoALiftPitch = Mathf.Clamp(AoALiftPitch, HighPitchAoaMinLift, 1);
                        AoALiftYaw = Mathf.Clamp(AoALiftYaw, HighYawAoaMinLift, 1);
                        AoALift_Min = Mathf.Min(AoALiftYaw, AoALiftPitch);

                        float GroundEffectAndVelLift = 0;

                        Vector2 Outputs = UnpackThrottles(Mathf.Abs(EngineOutput));

                        if (_InvertThrust)
                        {
                            if (DroneMode)
                            {
                                PitchDown = !PitchDown;
                            }
                            Outputs *= InverThrustMultiplier;
                        }

                        float Thrust = (Mathf.Min(Outputs.x)//Throttle
                        * ThrottleStrength
                        + Mathf.Max(Outputs.y, 0)//Afterburner throttle
                        * ThrottleStrengthAB);


                        if (AlternateLift)
                        {
                            SpeedLiftFactor_Lift = Mathf.Abs(sidespeed) * Lift;// Mathf.Min(, MaxLift);
                            SpeedLiftFactor_pd = Mathf.Abs(downspeed) * Lift;// Mathf.Min(, PitchDown ? MaxLift * PitchDownLiftMulti : MaxLift);
                        }
                        else
                        {
                            SpeedLiftFactor_Lift = Mathf.Min(SpeedLiftFactor * Lift, MaxLift);
                            SpeedLiftFactor_pd = Mathf.Min(AirSpeed * AirSpeed * Lift, PitchDown ? MaxLift * PitchDownLiftMulti : MaxLift);
                        }

                        //Create a Vector3 Containing the thrust, and rotate and adjust strength based on VTOL value
                        //engine output is multiplied so that max throttle without afterburner is max strength (unrelated to vtol)
                        LiftAcc = new Vector3(-sidespeed * SidewaysLift * SpeedLiftFactor_Lift * AoALiftYaw,// X Sideways
                                ((PitchDown ? downspeed * PitchDownLiftMulti : downspeed) * ExtraLift * SpeedLiftFactor_pd * AoALiftPitch),// Y Up
                                0);//Z Forward

                        //float thrust = EngineOutput * ThrottleStrength * AfterburnerThrottle * AfterburnerThrustMulti * Atmosphere;
                        if (Thrust_AlwaysWorldUp)
                        {
                            ThrustAcc = transform.InverseTransformDirection(Vector3.up) * Thrust;
                        }
                        else if (VTOLenabled)
                        {
                            if (VTOLOnly)//just use regular thrust strength if vtol only, no transition to plane flight
                            {
                                ThrustAcc = (Quaternion.SlerpUnclamped(Quaternion.identity, Quaternion.AngleAxis(180, Vector3.right), VTOLAngleDegrees * 0.0055555555555f /* / 180 */)
                                    * Vector3.forward);
                                VTOLAngleForwardDot = Vector3.Dot(ThrustAcc, Vector3.forward);
                                VTOLAngleForward = VTOLAngleForwardDot > 0;
                                ThrustAcc *= Thrust;
                                //VTOLInputAcc = Vector3.RotateTowards(Vector3.forward, VTOL180, VTOLAngle2 * Mathf.Deg2Rad, 0) * Thrust;
                            }
                            else//vehicle can transition from plane-like flight to helicopter-like flight, with different thrust values for each, with a smooth transition between them
                            {
                                float downthrust = Thrust * VTOLThrottleStrengthMulti;
                                ThrustAcc = (Quaternion.SlerpUnclamped(Quaternion.identity, Quaternion.AngleAxis(180, Vector3.right), VTOLAngleDegrees * 0.0055555555555f /* / 180 */)
                                    * Vector3.forward);
                                VTOLAngleForwardDot = Vector3.Dot(ThrustAcc, Vector3.forward);
                                VTOLAngleForward = VTOLAngleForwardDot > 0;
                                VTOLAngleForward90 = Mathf.Min((Mathf.Acos(Mathf.Clamp(VTOLAngleForwardDot, -1, 1)) * 0.63661977236758f /* divide by 90 degrees in radians */), 1);
                                ThrustAcc *= Mathf.Lerp(Thrust, Thrust * VTOLThrottleStrengthMulti, VTOLAngleForward90);
                            }

                            //add ground effect to the VTOL thrust
                            GroundEffectAndVelLift = GroundEffect(true, GroundEffectEmpty.position, -VehicleTransform.TransformDirection(ThrustAcc), VTOLGroundEffectStrength, 1);
                            ThrustAcc *= GroundEffectAndVelLift;

                        }
                        else//Simpler version for non-VTOL craft
                        {
                            ThrustAcc = Vector3.forward * Thrust;
                        }
                        if (AlternateLift)
                            GroundEffectAndVelLift = AoALift_Min * GroundEffect(false, GroundEffectEmpty.position, -VehicleTransform.up, GroundEffectStrength / VehicleRigidbody.mass, AirSpeed);
                        else
                            GroundEffectAndVelLift = AoALift_Min * GroundEffect(false, GroundEffectEmpty.position, -VehicleTransform.up, GroundEffectStrength / VehicleRigidbody.mass, SpeedLiftFactor);
                        LiftAcc.y += GroundEffectAndVelLift;

                        ThrustAcc *= Atmosphere;
                        LiftAcc *= Atmosphere;

                        float outputdif = (EngineOutput - EngineOutputLastFrame) / DeltaTime;
                        float ADVYaw = outputdif * AdverseYaw;
                        float ADVRoll = (1 - rotlift) * AdverseRoll * EngineOutput;
                        EngineOutputLastFrame = EngineOutput;
                        //used to add rotation friction
                        Vector3 localAngularVelocity = transform.InverseTransformDirection(VehicleRigidbody.angularVelocity);

                        float pitchaoapitchforce = 0;
                        float yawaoarollforce = 0;
                        if (DoStallForces)
                        {
                            pitchaoapitchforce = PitchAoaPitchForce.Evaluate(AngleOfAttackPitch) * PitchAoaPitchForceMulti * rotlift;
                            yawaoarollforce = ((AngleOfAttackYaw > 0 ? -1 : 1) * YawAoaRollForce.Evaluate(Mathf.Abs(AngleOfAttackYaw)) * YawAoaRollForceMulti) * rotlift;
                        }

                        //roll + rotational frictions
                        Vector3 FinalInputRot = new Vector3((((-localAngularVelocity.x * PitchFriction * rotlift * AoALiftPitch * AoALiftYaw) - (localAngularVelocity.x * PitchConstantFriction)) + pitchaoapitchforce) * Atmosphere,// X Pitch
                            (((-localAngularVelocity.y * YawFriction * rotlift * AoALiftPitch * AoALiftYaw) + ADVYaw) - (localAngularVelocity.y * YawConstantFriction)) * Atmosphere,// Y Yaw
                                ((LerpedRoll + yawaoarollforce + (-localAngularVelocity.z * RollFriction * rotlift * AoALiftPitch * AoALiftYaw) + ADVRoll) - (localAngularVelocity.z * RollConstantFriction)) * Atmosphere);// Z Roll

                        if (PitchMoment)
                        { Pitching = ((((VehicleTransform.up * LerpedPitch) + (VehicleTransform.up * downspeed * VelStraightenStrPitch * AoALiftPitch * rotlift)) * Atmosphere)); }
                        else
                        {
                            Pitching =
                            new Vector3(
                                ((((LerpedPitch) + (downspeed * VelStraightenStrPitch * AoALiftPitch * rotlift)) * Atmosphere))
                                , 0, 0);
                        }
                        if (YawMoment)
                        { Yawing = ((((VehicleTransform.right * LerpedYaw) + (VehicleTransform.right * -sidespeed * VelStraightenStrYaw * AoALiftYaw * rotlift)) * Atmosphere)); }
                        else
                        {
                            Yawing =
                            new Vector3(
                                0,
                                ((((LerpedYaw) + (-sidespeed * VelStraightenStrYaw * AoALiftYaw * rotlift)) * Atmosphere))
                                , 0);
                        }

                        if (DroneMode)
                        {
                            LiftAcc.y *= _EngineOn ? ThrottleInput : 0;
                        }

                        if (HasWheelColliders && WheelSuspension)
                        {
                            float suspensionDownCof = LiftAcc.y / VehicleRigidbody.mass / 9.81f;
                            suspensionDownCof = Mathf.Clamp(suspensionDownCof, 0, 1);
                            for (int wheelIndex = 0; wheelIndex < VehicleWheelColliders.Length; wheelIndex++)
                            {
                                WheelCollider wheel = VehicleWheelColliders[wheelIndex];
                                wheel.suspensionDistance = initSuspensionDistance[wheelIndex] - initSuspensionDistance[wheelIndex] * suspensionDownCof * initTargetPosition[wheelIndex];
                                JointSpring wheelSpring = wheel.suspensionSpring;
                                wheelSpring.targetPosition = initTargetPosition[wheelIndex] * (1 - suspensionDownCof);
                                wheel.suspensionSpring = wheelSpring;
                            }
                        }

                        VehicleTorque = FinalInputRot;
                    }
                    else
                    {
                        VelLift = pitch = yaw = roll = 0;
                    }
                }

                if (Asleep) { return; }

                Vector3 VehicleVel = VehicleRigidbody.velocity;
                if (!_DisablePhysicsApplication)
                {
                    // I tried changing ForceMode to Acceleration but for some reason the results are different,
                    // so until I work out why, just multiply forces by mass
                    float RBMass = VehicleRigidbody.mass;
                    // lerp velocity toward zero/windspeed for 'air friction'
                    if (CenterOfDrag)
                    {
                        Vector3 drag = -AirVel * ((AirFriction + SoundBarrier) * ExtraDrag);
                        VehicleRigidbody.AddForceAtPosition(drag * RBMass, CenterOfDrag.position, ForceMode.Force);
                    }
                    else
                    {
                        VehicleRigidbody.velocity = Vector3.Lerp(VehicleVel, FinalWind * StillWindMulti * Atmosphere, 1 - Mathf.Pow(0.5f, (AirFriction + SoundBarrier) * ExtraDrag * 90 * DeltaTime));
                    }
                    if (wrecked)
                    {
                        float negHealthPc = Mathf.Clamp01(Health / ExplodeHealth);
                        VehicleRigidbody.AddRelativeTorque(wreckedSpinForce_ * negHealthPc * /* StillWintMulti requires EngineOn + Grounded, so: */ Mathf.Min(AirSpeed * .1f, 1), ForceMode.Acceleration);
                        if (CenterOfLift)
                            VehicleRigidbody.AddForceAtPosition(VehicleTransform.rotation * LiftAcc * RBMass, CenterOfLift.position, ForceMode.Force);
                        else
                            VehicleRigidbody.AddRelativeForce(LiftAcc * RBMass * (1 - negHealthPc), ForceMode.Force);
                    }
                    else
                    {
                        if (CenterOfThrust)
                            VehicleRigidbody.AddForceAtPosition(VehicleTransform.rotation * ThrustAcc * RBMass, CenterOfThrust.position, ForceMode.Force);
                        else
                            VehicleRigidbody.AddRelativeForce(ThrustAcc * RBMass, ForceMode.Force);
                        if (CenterOfLift)
                            VehicleRigidbody.AddForceAtPosition(VehicleTransform.rotation * LiftAcc * RBMass, CenterOfLift.position, ForceMode.Force);
                        else
                            VehicleRigidbody.AddRelativeForce(LiftAcc * RBMass, ForceMode.Force);
                    }
                    VehicleRigidbody.AddRelativeTorque(VehicleTorque * RBMass, ForceMode.Force);
                    //apply pitching using pitch moment
                    if (PitchMoment)
                    { VehicleRigidbody.AddForceAtPosition(Pitching * RBMass, PitchMoment.position, ForceMode.Force); }
                    else
                    { VehicleRigidbody.AddRelativeTorque(Pitching * RBMass, ForceMode.Force); }
                    //apply yawing using yaw moment
                    if (YawMoment)
                    { VehicleRigidbody.AddForceAtPosition(Yawing * RBMass, YawMoment.position, ForceMode.Force); }
                    else
                    { VehicleRigidbody.AddRelativeTorque(-Yawing * RBMass, ForceMode.Force); }
                }
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
                VertGs = Gs_all.y / NumFUinAvgTime;
                AllGs = Gs_all.magnitude / NumFUinAvgTime;
                GDamageToTake += Mathf.Max((AllGs - MaxGs), 0);
                LastFrameVel = VehicleVel;
            }
        }
        [System.NonSerialized] public float GsAveragingTime = .1f;
        public int NumFUinAvgTime = 1;
        public Vector3 Gs_all;
        private Vector3[] FrameGs;
        private int GsFrameCheck;
        private bool wrecked = false;
        private Vector3 wreckedSpinForce_;
        public void SFEXT_G_Wrecked()
        {
            if (wrecked) { return; }
            SetEngineOff();
            wreckedSpinForce_ = new Vector3(Random.Range(-WreckedSpinForce.x, WreckedSpinForce.x), Random.Range(-WreckedSpinForce.y, WreckedSpinForce.y), Random.Range(-WreckedSpinForce.z, WreckedSpinForce.z));
            wrecked = true;
            int killerID = -1;
            byte killerWeaponType = 0;
            if (Utilities.IsValid(EntityControl.LastHitByPlayer))
            {
                if (Time.time == EntityControl.LastDamageSentTime)
                {
                    killerID = EntityControl.LastHitByPlayer.playerId;
                    killerWeaponType = EntityControl.LastHitWeaponType;
                }
            }
            if (SendKillEvents && IsOwner && EntityControl.Occupied && killerID > -1)
            { SendKillEvent(killerID, killerWeaponType); }
        }
        public void SFEXT_G_NotWrecked()
        {
            wrecked = false;
        }
        public void SendKillEvent(int killerID, byte weaponType)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(KillEvent), killerID, weaponType);
        }
        [NetworkCallable]
        public void KillEvent(int killerID, byte weaponType)
        {
            // this exists to tell the killer that they got a kill.
            if (killerID > -1)
            {
                VRCPlayerApi KillerAPI = VRCPlayerApi.GetPlayerById(killerID);
                if (Utilities.IsValid(KillerAPI))
                {
                    EntityControl.LastHitByPlayer = KillerAPI;
                    GameObject attackersVehicle = GameObject.Find(EntityControl.LastHitByPlayer.GetPlayerTag("SF_VehicleName"));
                    if (attackersVehicle)
                    {
                        EntityControl.LastAttacker = attackersVehicle.GetComponent<SaccEntity>();
                    }
                    else
                    {
                        EntityControl.LastAttacker = null;
                        return;
                    }
                }
                else
                {
                    EntityControl.LastHitByPlayer = null;
                    return;
                }
                EntityControl.LastHitWeaponType = weaponType;
                if (killerID == localPlayer.playerId)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_GotAKill");
                }
            }
        }
        public void NetworkExplode()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Explode));
        }
        [NetworkCallable]
        public void Explode()
        {
            if (EntityControl._dead) { return; }//can happen with prediction enabled if two people kill something at the same time
            FallAsleep();
            SetEngineOff();
            PlayerThrottle = 0;
            ThrottleInput = 0;
            EngineOutput = 0;
            MissilesIncomingHeat = 0;
            MissilesIncomingRadar = 0;
            MissilesIncomingOther = 0;
            VTOLAngle = VTOLDefaultValue;
            VTOLAngleInput = VTOLDefaultValue;
            if (HasAfterburner) { SetAfterburnerOff(); }
            Fuel = FullFuel;
            Atmosphere = 1;//vehiclemoving optimization requires this to be here
            Pitching = Vector3.zero;
            Yawing = Vector3.zero;
            VTOLAngleDegrees = VTOLMinAngle + (vtolangledif * VTOLAngle);

            if (!EntityControl.wrecked) { EntityControl.SetWrecked(); }
            EntityControl.dead = true;
            EntityControl.SendEventToExtensions("SFEXT_G_Explode");

            SendCustomEventDelayedSeconds(nameof(ReAppear), RespawnDelay + Time.fixedDeltaTime * 2);//the deltatime*2 makes sure it happens after the animation is over, probably (fix for it retaining some angular momentum on spawn)
            SendCustomEventDelayedSeconds(nameof(NotDead), RespawnDelay + InvincibleAfterSpawn);

            if (IsOwner)
            {
                LiftAcc = ThrustAcc = Vector3.zero;
                VehicleTorque = Vector3.zero;
                VehicleRigidbody.velocity = Vector3.zero;
                VehicleRigidbody.angularVelocity = Vector3.zero;
                if (!UsingManualSync)
                {
                    VehicleRigidbody.drag = 9999;
                    VehicleRigidbody.angularDrag = 9999;
                }
                Health = FullHealth;//turns off low health smoke
                Fuel = FullFuel;
                AoALiftPitch = 0;
                AoALiftYaw = 0;
                AngleOfAttackYaw = 0;
                AngleOfAttackPitch = 0;
                AngleOfAttack = 0;
                VelLift = VelLiftStart;
                VTOLAngleForward90 = 0;
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
            if (_EngineOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetEngineOn)); }
        }
        public void ReAppear()
        {
            EntityControl.SetWreckedFalse();
            EntityControl.SendEventToExtensions("SFEXT_G_ReAppear");
            numGrapplesAttached = 0;
            WakeUp();
            if (IsOwner)
            {
                SetRespawnPos();
            }
        }
        public void SetRespawnPos()
        {
            VehicleRigidbody.drag = 0;
            VehicleRigidbody.angularDrag = 0;
            LiftAcc = ThrustAcc = Vector3.zero;
            VehicleTorque = Vector3.zero;
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
            //these could get set after death by lag, probably
            MissilesIncomingHeat = 0;
            MissilesIncomingRadar = 0;
            MissilesIncomingOther = 0;
            Health = FullHealth;
            SetRespawnPos();
            EntityControl.SendEventToExtensions("SFEXT_O_MoveToSpawn");
        }
        private void WakeUp()
        {
            Asleep = false;
            EntityControl.SendEventToExtensions("SFEXT_L_WakeUp");
            VehicleRigidbody.WakeUp();
            EnableLiftSurfaces(IsOwner);
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
            EnableLiftSurfaces(false);
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
            VehicleRigidbody.position = VehicleTransform.position;
            EntityControl.Spawnposition = VehicleTransform.localPosition;
            EntityControl.Spawnrotation = VehicleTransform.localRotation;
            // inertia tensor wont be set properly if object is disabled
            if (SetCoM_ITR_initialized || !EntityControl.gameObject.activeInHierarchy) return;
            SendCustomEventDelayedSeconds(nameof(SetCoM_ITR), Time.fixedDeltaTime);//this has to be delayed because ?
        }
        public void SFEXT_L_OnEnable()
        {
            if (!EntityControl.Initialized) return;// don't set ITR if not initialized because the call in SetCoMMeshOffset() will do it
            if (VehicleAnimator) { VehicleAnimator.SetBool("EngineOn", _EngineOn); }
            if (!SetCoM_ITR_initialized)
                SetCoMMeshOffset();
        }
        bool SetCoM_ITR_initialized;
        public void SetCoM_ITR()
        {
            SetCoM_ITR_initialized = true;
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
            if (_EngineOn)
            {
                Vector2 Throttles = UnpackThrottles(ThrottleInput);
                Fuel = Mathf.Max(Fuel -
                        ((Mathf.Max(Throttles.x, MinFuelConsumption) * FuelConsumption)
                            + (Throttles.y * FuelConsumptionAB))
                                * Time.deltaTime, 0);
            }
            if (Fuel < LowFuel)
            {
                //max throttle scales down with amount of fuel below LowFuel
                ThrottleInput = PlayerThrottle = Mathf.Min(ThrottleInput, Fuel * LowFuelDivider);
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
            if (NoFuelLastFrame)
            {
                if (Fuel > 0)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendNotNoFuel));
                }
            }
            if (HasAfterburner)
            {
                if (!AfterburnerOn && ThrottleInput > ThrottleAfterburnerPoint)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetAfterburnerOn));
                }
                else if (AfterburnerOn && ThrottleInput <= ThrottleAfterburnerPoint)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetAfterburnerOff));
                }
            }
        }
        public void SetRotInputs()
        {
            //if moving backwards, controls invert (if thrustvectoring is set to 0 strength for that axis)
            if ((Vector3.Dot(AirVel, VehicleTransform.forward) > 0))//normal, moving forward
            {
                ReversingPitchStrength = 1;
                ReversingYawStrength = 1;
                ReversingRollStrength = 1;
            }
            else//moving backward. The 'Zero' values are set in start(). Explanation there.
            {
                ReversingPitchStrength = ReversingPitchStrengthZero;
                ReversingYawStrength = ReversingYawStrengthZero;
                ReversingRollStrength = ReversingRollStrengthZero;
            }

            pitch = Mathf.Clamp(RotationInputs.x, -1, 1) * PitchStrength * ReversingPitchStrength;
            yaw = Mathf.Clamp(-RotationInputs.y, -1, 1) * YawStrength * ReversingYawStrength;
            roll = Mathf.Clamp(RotationInputs.z, -1, 1) * RollStrength * ReversingRollStrength;

            if (pitch > 0)
            {
                pitch *= PitchDownStrMulti;
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
            GroundedLastFrame = true;
            Taxiing = true;
            EntityControl.SendEventToExtensions("SFEXT_G_TouchDown");
        }
        public void TouchDownWater()
        {
            //Debug.Log("TouchDownWater");
            if (FloatingLastFrame) { return; }
            FloatingLastFrame = true;
            Taxiing = true;
            EntityControl.SendEventToExtensions("SFEXT_G_TouchDownWater");
        }
        public void TakeOff()
        {
            //Debug.Log("TakeOff");
            Taxiing = false;
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
            if (IsOwner)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetEngineOff));
            }
        }
        public void SendNotNoFuel()
        {
            NoFuelLastFrame = false;
            EntityControl.SendEventToExtensions("SFEXT_G_NotNoFuel");
            if (IsOwner && EngineOnOnEnter && Occupied)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetEngineOn));
            }
        }
        public void SFEXT_G_ReSupply()
        {
            if ((Fuel < FullFuel - 10) || (Health != FullHealth))
            {
                EntityControl.ReSupplied++;//used to only play the sound if we're actually repairing/getting ammo/fuel
            }
            float addedHealth = FullHealth / RepairTime; ;
            PredictedHealth = Mathf.Min(PredictedHealth + addedHealth, FullHealth);

            if (IsOwner)
            {
                Fuel = Mathf.Min(Fuel + (FullFuel / RefuelTime), FullFuel);
                Health = Mathf.Min(Health + addedHealth, FullHealth);
                if (LowFuelLastFrame && Fuel > LowFuel)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendNotLowFuel));
                }
                if (NoFuelLastFrame && Fuel > 0)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendNotNoFuel));
                }
                if (EntityControl.wrecked && Health > 0)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(RepairWrecked));
                }
            }
        }
        public void SFEXT_G_RePair()
        {
            if (Health != FullHealth) { EntityControl.ReSupplied++; }
            if (IsOwner)
            {
                Health = Mathf.Min(Health + (FullHealth / RepairTime), FullHealth);
                if (EntityControl.wrecked && Health > 0)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(RepairWrecked));
                }
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
        public void RepairWrecked()
        {
            EntityControl.SetWreckedFalse();
            if (EngineOnOnEnter) { SetEngineOn(); }
        }
        public void SFEXT_G_RespawnButton()//called globally when using respawn button
        {
            if (IsOwner)
            {
                Atmosphere = 1;//vehiclemoving optimization requires this to be here
                               //synced variables
                Health = FullHealth;
                Fuel = FullFuel;
                EngineOutput = 0;
                VTOLAngle = VTOLDefaultValue;
                VTOLAngleInput = VTOLDefaultValue;
                VTOLAngleDegrees = VTOLMinAngle + (vtolangledif * VTOLAngle);
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
            if (LowFuelLastFrame)
            { SendNotLowFuel(); }
            if (NoFuelLastFrame)
            { SendNotNoFuel(); }
            LiftAcc = ThrustAcc = Vector3.zero;
            VehicleTorque = Vector3.zero;
            EntityControl.SetWreckedFalse();
        }
        float LastHitTime = -100, PredictedHealth;
        public void SFEXT_L_BulletHit()
        {
            if (!IsOwner || EntityControl.dead || EntityControl.invincible) { return; }
            if (Time.time - EntityControl.LastResupplyTime < 2) return;//disable prediction if vehicle has recently been healing
            if (PredictExplosion)
            {
                if (Time.time - LastHitTime > 2)
                {
                    LastHitTime = Time.time;
                    PredictedHealth = Mathf.Min(Health - EntityControl.LastHitDamage, FullHealth);
                    if (PredictedHealth <= ExplodeHealth)
                    {
                        Explode();
                    }
                }
                else
                {
                    LastHitTime = Time.time;
                    PredictedHealth = Mathf.Min(PredictedHealth - EntityControl.LastHitDamage, FullHealth);
                    if (PredictedHealth <= ExplodeHealth)
                    {
                        Explode();
                    }
                }
            }
        }
        public void SFEXT_G_BulletHit()
        {
            if (!IsOwner || EntityControl.dead || EntityControl.invincible) { return; }
            Health = Mathf.Min(Health - EntityControl.LastHitDamage, FullHealth);
            if (!wrecked && Health <= 0f)
            {
                EntityControl.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetWrecked");
            }
            else if (wrecked && Health > 0f) // healing bullets could exist ..
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(RepairWrecked));
            }
            else if (EntityControl.LastHitWeaponType == 2)
            {
                //missiles cause a torque
                Vector3 explosionforce = new Vector3(Random.Range(-MissilePushForce, MissilePushForce), Random.Range(-MissilePushForce, MissilePushForce), Random.Range(-MissilePushForce, MissilePushForce) * EntityControl.LastHitDamage) / FullHealth;
                VehicleRigidbody.AddTorque(explosionforce, ForceMode.VelocityChange);
            }
        }
        private float LastCollisionTime;
        const float MINCOLLISIONSOUNDDELAY = 0.1f;
        public void SFEXT_L_OnCollisionEnter()
        {
            if (!IsOwner) { return; }
            if (Asleep) { WakeUp(); }
            if (wrecked)
            {
                NetworkExplode();
            }
            Collision col = EntityControl.LastCollisionEnter;
            if (col == null) { return; }
            float colmag = col.impulse.magnitude / VehicleRigidbody.mass;
            float colmag_dmg = colmag;
            if (colmag_dmg > Crash_Damage_Speed)
            {
                if (colmag_dmg < Crash_Death_Speed)
                {
                    float dif = Crash_Death_Speed - Crash_Damage_Speed;
                    float newcolT = (colmag_dmg - Crash_Damage_Speed) / dif;
                    colmag_dmg = Mathf.Lerp(0, Crash_Death_Speed, newcolT);
                }
                float thisGDMG = (colmag_dmg / Crash_Death_Speed) * FullHealth;
                Health -= thisGDMG;

                if (Health <= 0)
                {
                    if (thisGDMG > FullHealth * 0.5f || Health <= ExplodeHealth)
                    {
                        if (Piloting) { EntityControl.SendEventToExtensions("SFEXT_O_Suicide"); }
                        NetworkExplode();
                    }
                }
            }
            if (Time.time - LastCollisionTime > MINCOLLISIONSOUNDDELAY)
            {
                if (colmag > BigCrashSpeed)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BigCrash));
                }
                else if (colmag > MediumCrashSpeed)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(MediumCrash));
                }
                else if (colmag > SmallCrashSpeed)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SmallCrash));
                }
            }
            LastCollisionTime = Time.time;
        }
        public void SmallCrash()
        {
            EntityControl.SendEventToExtensions("SFEXT_G_SmallCrash");
        }
        public void MediumCrash()
        {
            EntityControl.SendEventToExtensions("SFEXT_G_MediumCrash");
        }
        public void BigCrash()
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
            MissilesIncomingHeat = 0;
            MissilesIncomingRadar = 0;
            MissilesIncomingOther = 0;
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
            GDamageToTake = 0f;
            VertGs = 0f;
            AllGs = 0f;
            for (int i = 0; i < NumFUinAvgTime; i++) { FrameGs[i] = Vector3.zero; }
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
                    if (!_DisableThrottleControl)
                    {
                        PlayerThrottle = ThrottleInput = EngineOutputLastFrame = EngineOutput;
                    }
                }
                else// user was in the vehicle when they left or you took ownership with a grapple
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetEngineOff));
                    PlayerThrottle = ThrottleInput = 0;
                    EngineOutputLastFrame = EngineOutput;
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
            EnableLiftSurfaces(false);
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
            VTOLAngleInput = VTOLAngle;
            VTOLAngleDegrees = VTOLMinAngle + (vtolangledif * VTOLAngle);
            GDHitRigidbody = null;
            if (!_DisableThrottleControl)
            {
                if (_EngineOn)
                { PlayerThrottle = ThrottleInput = EngineOutputLastFrame = EngineOutput; }
                else
                {
                    PlayerThrottle = ThrottleInput = 0;
                    EngineOutputLastFrame = EngineOutput;
                }
            }

            Piloting = true;
            if (EntityControl._dead) { Health = FullHealth; }//dead is true for the first 5 seconds after spawn, this might help with spontaneous explosions

            //hopefully prevents explosions when you enter the vehicle
            GDamageToTake = 0f;
            VertGs = 0;
            AllGs = 0;
            VehicleRigidbody.velocity = CurrentVel;
            LastFrameVel = CurrentVel;
            if (EngineOnOnEnter && Fuel > 0 && !_PreventEngineToggle && !wrecked)
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
            roll = 0;
            pitch = 0;
            yaw = 0;
            LerpedPitch = 0;
            LerpedRoll = 0;
            LerpedYaw = 0;
            //reset everything
            Piloting = false;
            Taxiinglerper = 0;
            ThrottleGripLastFrame = false;
            JoystickGripLastFrame = false;
            JoyStickGrippingLastFrame_toggle = false;
            JoyStickReleaseCount = 0;
            GrabToggle = false;
            DoAAMTargeting = false;
            MissilesIncomingHeat = 0;
            MissilesIncomingRadar = 0;
            MissilesIncomingOther = 0;
            Pitching = Vector3.zero;
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
            if (numGrapplesAttached == 0)
            {
                foreach (WheelCollider wheel in VehicleWheelColliders)
                {
                    wheel.motorTorque = 0.00000000000000000000000000000000001f;
                    wheel.brakeTorque = 0;
                }
            }
            numGrapplesAttached++;
        }
        public void SFEXT_L_GrappleDetach()
        {
            numGrapplesAttached--;
            if (numGrapplesAttached < 0) { numGrapplesAttached = 0; }
            if (EngineOn) return;
            if (numGrapplesAttached != 0) return;
            foreach (WheelCollider wheel in VehicleWheelColliders)
            {
                wheel.motorTorque = 0f;
            }
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
            if (_DisablePhysicsAndInputs) { return; }
            float AtmosPos = CenterOfMass.position.y;
            if (UseAtmospherePositionOffset) { AtmosPos += AtmospherePositionOffset; }//saves one extern if not using it
            Atmosphere = Mathf.Clamp((1 - (AtmosPos / AtmoshpereFadeDistance)) + AtmosphereHeightThing, 0, 1);
            float TimeGustiness = Time.time * WindGustiness;
            float gustx = TimeGustiness + (VehicleTransform.position.x * WindTurbulanceScale);
            float gustz = TimeGustiness + (VehicleTransform.position.z * WindTurbulanceScale);
            FinalWind = Vector3.Normalize(new Vector3((Mathf.PerlinNoise(gustx + 9000, gustz) - .5f), /* (Mathf.PerlinNoise(gustx - 9000, gustz - 9000) - .5f) */0, (Mathf.PerlinNoise(gustx, gustz + 9999) - .5f))) * WindGustStrength;
            FinalWind = (FinalWind + Wind) * Atmosphere;
            AirVel = VehicleRigidbody.velocity - (FinalWind * StillWindMulti);
            AirSpeed = AirVel.magnitude;
            Vector3 VecForward = VehicleTransform.forward;
            AngleOfAttackPitch = Vector3.SignedAngle(VecForward, Vector3.ProjectOnPlane(AirVel, VehicleTransform.right), VehicleTransform.right) - ZeroLiftAoA;
            AngleOfAttackYaw = Vector3.SignedAngle(VecForward, Vector3.ProjectOnPlane(AirVel, VehicleTransform.up), VehicleTransform.up);
            //angle of attack stuff, pitch and yaw are calculated seperately
            //pitch and yaw each have a curve for when they are within the 'MaxAngleOfAttack' and a linear version up to 90 degrees, which are Max'd (using Mathf.Clamp) for the final result.
            //the linear version is used for high aoa, and is 0 when at 90 degrees, and 1(multiplied by HighAoaMinControl) at 0. When at more than 90 degrees, the control comes back with the same curve but the inputs are inverted. (unless thrust vectoring is enabled) The invert code is elsewhere.

            float absPitch = Mathf.Abs(AngleOfAttackPitch);
            float absYaw = Mathf.Abs(AngleOfAttackYaw);
            //AngleOfAttack = Vector3.Angle(VecForward, AirVel);
            //^ the reason I'm not doing this is because it would give a circular result instead of square, when the physics is square
            // which would change a bunch of stuff that uses the value, (Limits, Effects/Sounds)
            // Limits in particular is important because it effects the controls, based on the physics.
            AngleOfAttack = Mathf.Max(absPitch, absYaw);

            //for this part AoA maxes out at 90 and reduces again as move towards facing backwards
            if (absPitch > 90) absPitch = 180 - absPitch;//flying backwards
            AoALiftPitch = absPitch / MaxAngleOfAttackPitch;//angle of attack as 0-1 float, for backwards and forwards
            AoALiftPitch = 1 - Mathf.Pow(AoALiftPitch, AoaCurveStrength);//give it a curve

            float AoALiftPitchMin = absPitch * 0.0111111111f/* same as divide by 90 */;//linear version to 90 for high aoa
            AoALiftPitchMin = Mathf.Clamp01((1 - AoALiftPitchMin) * HighPitchAoaMinControl);
            AoALiftPitch = Mathf.Clamp(AoALiftPitch, AoALiftPitchMin, 1);

            if (absYaw > 90) absYaw = 180 - absYaw;//flying backwards
            AoALiftYaw = absYaw / MaxAngleOfAttackYaw;
            AoALiftYaw = 1 - Mathf.Pow(AoALiftYaw, AoaCurveStrength);//give it a curve

            float AoALiftYawMin = absYaw * 0.0111111111f/* same as divide by 90 */;//linear version to 90 for high aoa
            AoALiftYawMin = Mathf.Clamp01((1 - AoALiftYawMin) * HighYawAoaMinControl);
            AoALiftYaw = Mathf.Clamp(AoALiftYaw, AoALiftYawMin, 1);
        }
        private float GroundEffect(bool VTOL, Vector3 Position, Vector3 Direction, float GEStrength, float speedliftfac)
        {
            //Ground effect, extra lift caused by air pressure when close to the ground
            RaycastHit GE;
            if (Physics.Raycast(Position, Direction, out GE, GroundEffectMaxDistance, 2065 /* Default, Water and Environment */, QueryTriggerInteraction.Collide))
            {
                float GroundEffect = ((-GE.distance + GroundEffectMaxDistance) / GroundEffectMaxDistance) * GEStrength;
                if (VTOL) { return 1 + GroundEffect; }
                GroundEffect *= ExtraVelLift;
                VelLift = VelLiftStart + GroundEffect;
                VelLiftMax = Mathf.Max(VelLiftMaxStart, VTOL ? 999999999f : GroundEffectLiftMax);
            }
            else//set non-groundeffect'd vel lift values
            {
                if (VTOL) { return 1; }
                VelLift = VelLiftStart * ExtraVelLift;
                VelLiftMax = VelLiftMaxStart;
            }
            return Mathf.Min(speedliftfac * AoALiftPitch * VelLift, VelLiftMax);
        }
        private void SetVTOLRotValues()
        {
            if (VTOL360)
            {
                //handle interpolations from 0.99 to 0.01 properly
                //set value to between 0 and 1
                if (VTOLAngleInput >= 0)
                { VTOLAngleInput = VTOLAngleInput - Mathf.Floor(VTOLAngleInput); }
                else
                {
                    float AbsIn = Mathf.Abs(VTOLAngleInput);
                    VTOLAngleInput = 1 - (AbsIn - Mathf.Floor(AbsIn));
                }
                //set value above or below current VTOLAngle to make it interpolate in the shortest direction
                if (VTOLAngle > VTOLAngleInput)
                {
                    if (Mathf.Abs(VTOLAngle - VTOLAngleInput) > .5f)
                    { VTOLAngleInput += 1; }
                }
                else
                {
                    if (Mathf.Abs(VTOLAngle - VTOLAngleInput) > .5f)
                    { VTOLAngleInput -= 1; }
                }
            }
            else
            {
                VTOLAngleInput = Mathf.Clamp(VTOLAngleInput, 0, 1);
            }
            VTOLAngle = Mathf.MoveTowards(VTOLAngle, VTOLAngleInput, VTOLAngleDivider * Time.deltaTime);
            if (VTOLAngle < 0) { VTOLAngle++; }
            else if (VTOLAngle > 1) { VTOLAngle--; }
            VTOLAngleDegrees = VTOLMinAngle + (vtolangledif * VTOLAngle);
            float SpeedForVTOL = (Mathf.Min(Speed / VTOLLoseControlSpeed, 1));
            if (VTOLOnly || (VTOLAngle > 0))
            {
                if (VTOLOnly)
                {
                    if (_EngineOn)
                    {
                        PitchThrustVecMulti = 1;
                        YawThrustVecMulti = 1;
                        RollThrustVecMulti = 1;
                    }
                    else
                    {
                        PitchThrustVecMulti = 0;
                        YawThrustVecMulti = 0;
                        RollThrustVecMulti = 0;
                    }
                }
                else
                {
                    if (_EngineOn)
                    {
                        float SpeedForVTOL_Inverse_xVTOL = (1 - SpeedForVTOL) * VTOLAngleForward90;
                        //the thrust vec values are linearly scaled up the slower you go while in VTOL, from 0 at VTOLLoseControlSpeed
                        PitchThrustVecMulti = Mathf.Lerp(PitchThrustVecMultiStart, VTOLPitchThrustVecMulti, SpeedForVTOL_Inverse_xVTOL);
                        YawThrustVecMulti = Mathf.Lerp(YawThrustVecMultiStart, VTOLYawThrustVecMulti, SpeedForVTOL_Inverse_xVTOL);
                        RollThrustVecMulti = Mathf.Lerp(RollThrustVecMultiStart, VTOLRollThrustVecMulti, SpeedForVTOL_Inverse_xVTOL);

                        ReversingPitchStrengthZero = 1;
                        ReversingYawStrengthZero = 1;
                        ReversingRollStrengthZero = 1;
                    }
                    else
                    {
                        PitchThrustVecMulti = 0;
                        YawThrustVecMulti = 0;
                        RollThrustVecMulti = 0;

                        ReversingPitchStrengthZero = ReversingPitchStrengthZeroStart;
                        ReversingYawStrengthZero = ReversingYawStrengthZeroStart;
                        ReversingRollStrengthZero = ReversingRollStrengthZeroStart;
                    }
                }
            }
            else
            {
                if (_EngineOn)
                {
                    PitchThrustVecMulti = PitchThrustVecMultiStart;
                    YawThrustVecMulti = YawThrustVecMultiStart;
                    RollThrustVecMulti = RollThrustVecMultiStart;

                    ReversingPitchStrengthZero = ReversingPitchStrengthZeroStart;
                    ReversingYawStrengthZero = ReversingYawStrengthZeroStart;
                    ReversingRollStrengthZero = ReversingRollStrengthZeroStart;
                }
                else
                {
                    PitchThrustVecMulti = 0;
                    YawThrustVecMulti = 0;
                    RollThrustVecMulti = 0;

                    ReversingPitchStrengthZero = ReversingPitchStrengthZeroStart;
                    ReversingYawStrengthZero = ReversingYawStrengthZeroStart;
                    ReversingRollStrengthZero = ReversingRollStrengthZeroStart;
                }
            }
        }
        public Vector2 UnpackThrottles(float Throttle)
        {
            Throttle = Mathf.Abs(Throttle);
            //x = throttle amount (0-1), y = afterburner amount (0-1)
            return new Vector2(Mathf.Min(Throttle, ThrottleAfterburnerPoint) * ThrottleNormalizer,
            Mathf.Max((Mathf.Max(Throttle, ThrottleAfterburnerPoint) - ThrottleAfterburnerPoint) * ABNormalizer, 0));
        }
        public void EnableLiftSurfaces(bool enable)
        {
            if (enable == LiftSurfacesEnabled) { return; }
            LiftSurfacesEnabled = enable;
            for (int i = 0; i < LiftSurfaces.Length; i++)
            {
                LiftSurfaces[i].gameObject.SetActive(enable);
            }
        }
    }
}
