
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SaccAirVehicle : UdonSharpBehaviour
{
    [Tooltip("Base object reference")]
    public SaccEntity EntityControl;
    [Tooltip("The object containing all non-trigger colliders for the vehicle, their layers are changed when entering and exiting")]
    public Transform PlaneMesh;
    [Tooltip("Layer to set the colliders to when entering vehicle")]
    public int OnboardVehicleLayer = 19;
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
    [Tooltip("HP of the plane, bullets do 10 damage per hit")]
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
    [Tooltip("Disable Thrust/VTOL rotation values transition calculations and assume VTOL mode always (for helicopters)")]
    public bool VTOLOnly = false;
    [Header("Response:")]
    [Tooltip("Vehicle thrust at max throttle without afterburner")]
    public float ThrottleStrength = 20f;
    [Tooltip("Make VR Throttle motion controls use the Y axis instead of the Z axis for adjustment (Helicopter collective)")]
    public bool VerticalThrottle = false;
    [Tooltip("Multiply how much the VR throttle moves relative to hand movement")]
    public float ThrottleSensitivity = 6f;
    [Tooltip("How much more thrust the vehicle has when in full afterburner")]
    public float AfterburnerThrustMulti = 1.5f;
    [Tooltip("How quickly the vehicle throttles up after throttle is increased (Lerp)")]
    public float AccelerationResponse = 4.5f;
    [Tooltip("How quickly the vehicle throttles down relative to how fast it throttles up after throttle is decreased")]
    public float EngineSpoolDownSpeedMulti = .5f;
    [Tooltip("How much the plane slows down (Speed lerped towards 0)")]
    public float AirFriction = 0.0004f;
    [Tooltip("Pitch force multiplier, (gets stronger with airspeed)")]
    public float PitchStrength = 5f;
    [Tooltip("Pitch rotation force (as multiple of PitchStrength) (doesn't get stronger with airspeed, useful for helicopters and ridiculous jets). Setting this to a non - zero value disables inversion of joystick pitch controls when vehicle is travelling backwards")]
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
    [Tooltip("Yaw rotation force (as multiple of YawStrength) (doesn't get stronger with airspeed, useful for helicopters and ridiculous jets). Setting this to a non - zero value disables inversion of joystick pitch controls when vehicle is travelling backwards")]
    public float YawThrustVecMulti = 0f;
    [Tooltip("Force that stops vehicle from yawing, (gets stronger with airspeed)")]
    public float YawFriction = 15f;
    [Tooltip("Force that stops vehicle from yawing, (doesn't get stronger with airspeed)")]
    public float YawConstantFriction = 0f;
    [Tooltip("How quickly the vehicle responds to changes in joystick's yaw (Lerp)")]
    public float YawResponse = 20f;
    [Tooltip("If the vehicle is moving backwards, Yaw strength is multiplied by this. No effect if YawThrustVecMulti is above 0")]
    public float ReversingYawStrengthMulti = 2.4f;
    [Tooltip("Yaw force multiplier, (gets stronger with airspeed)")]
    public float RollStrength = 450f;
    [Tooltip("Roll rotation force (as multiple of RollStrength) (doesn't get stronger with airspeed, useful for helicopters and ridiculous jets). Setting this to a non - zero value disables inversion of joystick pitch controls when vehicle is travelling backwards")]
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
    [Tooltip("Rolling added to the vehicle with changes in throttle")]
    public float AdverseRoll = 0;
    [Tooltip("Rotational inputs are multiplied by current speed to make flying at low speeds feel heavier. Above the speed input here, all inputs will be at 100%. Linear. (Meters/second)")]
    public float RotMultiMaxSpeed = 220f;
    [Tooltip("How much the the vehicle's nose is pulled toward the direction of movement on the pitch axis")]
    public float VelStraightenStrPitch = 0.035f;
    [Tooltip("How much the the vehicle's nose is pulled toward the direction of movement on the yaw axis")]
    public float VelStraightenStrYaw = 0.045f;
    [Tooltip("Angle of attack above which the plane will lose control")]
    public float MaxAngleOfAttackPitch = 25f;
    [Tooltip("Angle of attack above which the plane will lose control")]
    public float MaxAngleOfAttackYaw = 40f;
    [Tooltip("Shape of the angle of attack lift curve. 1= linear, high number = curve more vertical at the beginning, See this to understand (the 2 in the input represents this value, ignore everything outside the 0-1 range in the graph): https://www.wolframalpha.com/input/?i=-%28%281-x%29%5E2%29%2B1")]
    public float AoaCurveStrength = 2f;//1 = linear, >1 = convex, <1 = concave
    [Tooltip("The angle of attack curve is augmented by being MAX'd(taking the higher value) with a linear curve that is multiplied by this number. Use this value to decide how much control the plane has when beyond it's 'max' angle of attack. See AoALiftCurve.png. Pitch AoA and Yaw AoA are calculated seperately, control is reduced based on the worse value.")]
    public float HighPitchAoaMinControl = 0.2f;
    [Tooltip("See above")]
    public float HighYawAoaMinControl = 0.2f;
    [Tooltip("When the plane is is at a high angle of attack you can give it a minimum amount of lift/drag, so that it doesn't just lose all air resistance.")]
    public float HighPitchAoaMinLift = 0.2f;
    [Tooltip("See above")]
    public float HighYawAoaMinLift = 0.2f;
    [Tooltip("Degrees per second the vehicle rotates on the ground. Uses simple object rotation with a lerp, no real physics to it.")]
    public float TaxiRotationSpeed = 35f;
    [Tooltip("How lerped the taxi movement rotation is")]
    public float TaxiRotationResponse = 2.5f;
    [Tooltip("Make taxiing more realistic by not allowing vehicle to rotate on the spot")]
    public bool DisallowTaxiRotationWhileStill = false;
    [Tooltip("When the above is ticked, This is the speed at which the plane will reach its full turning speed. Meters/second.")]
    public float TaxiFullTurningSpeed = 20f;
    [Tooltip("Adjust how steep the lift curve is. Higher = more lift")]
    public float Lift = 0.00015f;
    [Tooltip("How much angle of attack on yaw turns the vehicle. Yaw steering strength in air")]
    public float SidewaysLift = .17f;
    [Tooltip("Maximum value for lift, as it's exponential it's wise to stop it at some point?")]
    public float MaxLift = 10f;
    [Tooltip("Push the vehicle up based on speed. Used to counter the fact that without it, the plane's nose will droop down due to gravity. Slower planes need a higher value.")]
    public float VelLift = 1f;
    [Tooltip("Maximum Vel Lift, to stop the nose being pushed up. Technically should probably be 9.81 to counter gravity exactly")]
    public float VelLiftMax = 10f;
    [Tooltip("Vehicle will take damage if experiences more Gs that this (Internally Gs are calculated in all directions, the HUD shows only vertical Gs so it will differ slightly")]
    public float MaxGs = 40f;
    [Tooltip("Damage taken Per G above maxGs, per second.\n(Gs - MaxGs) * GDamage = damage/second")]
    public float GDamage = 10f;
    [Tooltip("Length of the trace that looks for the ground to calculate ground effect")]
    public float GroundEffectMaxDistance = 7;
    [Tooltip("Multiply the force of the ground effect")]
    public float GroundEffectStrength = 4;
    [Tooltip("Limit the force that can be applied by ground effect")]
    public float GroundEffectLiftMax = 9999999;
    [Header("Response VTOL:")]
    [Tooltip("Degrees per second which the angle of the thrusters on the vehicle rotate toward desired angle")]
    public float VTOLAngleTurnRate = 90f;
    [Tooltip("Position between VTOL Min Angle and VTOL Max Angle that the plane is at by default. 0 = min, 1 = max.")]
    public float VTOLDefaultValue = 0;
    [Tooltip("Allow after burner whilst VTOL is engaged, (VTOL angle is not 0), VTOL Min Angle must be 0 for afterburner to work if this is unticked.")]
    public bool VTOLAllowAfterburner = false;
    [Tooltip("Multiply throttle strength by this value whilst vehicle is in VTOL mode, at VTOL angle of 90 degrees, this value is used, between 0 and 90 degrees the value is linearly transitioned towards this value, above 90 degrees it remains at this value")]
    public float VTOLThrottleStrengthMulti = .7f;
    [Tooltip("Minimum angle of thrust direction, 0 = straight backwards, 90 = straight down, 180 = straight forwards")]
    public float VTOLMinAngle = 0;
    [Tooltip("Maximum angle of thrust direction, 0 = straight backwards, 90 = straight down, 180 = straight forwards")]
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
    [Header("Other:")]
    [Tooltip("Adjusts all values that would need to be adjusted if you changed the mass automatically on Start(). Including all wheel colliders suspension values")]
    [SerializeField] private bool AutoAdjustValuesToMass = true;
    [Tooltip("Zero height of the calculation of atmosphere thickness and HUD altitude display")]
    public float SeaLevel = -10f;
    [Tooltip("Wind speed on each axis")]
    public Vector3 Wind;
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
    public float FuelConsumption = 2;
    [Tooltip("Multiply FuelConsumption by this number when at full afterburner Scales with afterburner level")]
    public float FuelConsumptionABMulti = 3f;
    [Tooltip("Number of resupply ticks it takes to refuel fully from zero")]
    public float RefuelTime = 25;
    [Tooltip("Number of resupply ticks it takes to repair fully from zero")]
    public float RepairTime = 30;
    [Tooltip("Time until vehicle reappears after exploding")]
    public float RespawnDelay = 10;
    [Tooltip("Time after reappearing the plane is invincible for")]
    public float InvincibleAfterSpawn = 2.5f;
    [Tooltip("Damage taken when hit by a bullet")]
    public float BulletDamageTaken = 10f;
    [Tooltip("Locally destroy target if prediction thinks you killed them, should only ever cause problems if you have a system that repairs vehicles during a fight")]
    public bool PredictDamage = true;
    [Tooltip("Multiply how much damage is done by missiles")]
    public float MissileDamageTakenMultiplier = 1f;
    [Tooltip("Strength of force that pushes the vehicle when exploding")]
    [SerializeField] private float MissilePushForce = 1f;
    [Tooltip("Altitude above 'Sea Level' at which the atmosphere starts thinning, In meters. 12192 = 40,000~ feet")]
    public float AtmosphereThinningStart = 12192f; //40,000 feet
    [Tooltip("Altitude above 'Sea Level' at which the atmosphere reaches zero thickness. In meters. 19812 = 65,000~ feet")]
    public float AtmosphereThinningEnd = 19812; //65,000 feet
    [System.NonSerializedAttribute] public float AllGs;


    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public float EngineOutput = 0f;
    [System.NonSerializedAttribute] public Vector3 CurrentVel = Vector3.zero;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public float VertGs = 1f;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public float AngleOfAttack;//MAX of yaw & pitch aoa //used by effectscontroller and hudcontroller
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.None)] public bool Occupied = false; //this is true if someone is sitting in pilot seat
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public float VTOLAngle;

    [System.NonSerializedAttribute] public Animator VehicleAnimator;
    [System.NonSerializedAttribute] public ConstantForce VehicleConstantForce;
    [System.NonSerializedAttribute] public Rigidbody VehicleRigidbody;
    [System.NonSerializedAttribute] public Transform VehicleTransform;
    private VRC.SDK3.Components.VRCObjectSync VehicleObjectSync;
    private GameObject VehicleGameObj;
    [System.NonSerializedAttribute] public Transform CenterOfMass;
    private float LerpedRoll;
    private float LerpedPitch;
    private float LerpedYaw;
    [System.NonSerializedAttribute] public bool ThrottleGripLastFrame = false;
    [System.NonSerializedAttribute] public bool JoystickGripLastFrame = false;
    Quaternion JoystickZeroPoint;
    Quaternion PlaneRotLastFrame;
    [System.NonSerializedAttribute] public float PlayerThrottle;
    private float TempThrottle;
    private float ThrottleZeroPoint;
    private float ThrottlePlayspaceLastFrame;
    [System.NonSerializedAttribute] public float ThrottleInput = 0f;
    private float roll = 0f;
    private float pitch = 0f;
    private float yaw = 0f;
    [System.NonSerializedAttribute] public float FullHealth;
    [System.NonSerializedAttribute] public bool Taxiing = false;
    [System.NonSerializedAttribute] public bool Floating = false;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public Vector3 RotationInputs;
    [System.NonSerializedAttribute] public bool Piloting = false;
    [System.NonSerializedAttribute] public bool Passenger = false;
    [System.NonSerializedAttribute] public bool InEditor = true;
    [System.NonSerializedAttribute] public bool InVR = false;
    [System.NonSerializedAttribute] public Vector3 LastFrameVel = Vector3.zero;
    [System.NonSerializedAttribute] public VRCPlayerApi localPlayer;
    [System.NonSerializedAttribute] public float AtmoshpereFadeDistance;
    [System.NonSerializedAttribute] public float AtmosphereHeightThing;
    [System.NonSerializedAttribute] public float Atmosphere = 1;
    [System.NonSerializedAttribute] public float rotlift;
    [System.NonSerializedAttribute] public float AngleOfAttackPitch;
    [System.NonSerializedAttribute] public float AngleOfAttackYaw;
    private float AoALiftYaw;
    private float AoALiftPitch;
    private Vector3 Pitching;
    private Vector3 Yawing;
    [System.NonSerializedAttribute] public float Taxiinglerper;
    [System.NonSerializedAttribute] public float ExtraDrag = 1;
    [System.NonSerializedAttribute] public float ExtraLift = 1;
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
    private Vector3 FinalWind;//includes Gusts
    [System.NonSerializedAttribute] public Vector3 AirVel;
    private float StillWindMulti;//multiplies the speed of the wind by the speed of the plane when taxiing to prevent still planes flying away
    private int ThrustVecGrounded;
    private float SoundBarrier;
    [System.NonSerializedAttribute] public float FullFuel;
    private float LowFuelDivider;
    private float LastResupplyTime = 5;//can't resupply for the first 10 seconds after joining, fixes potential null ref if sending something to PlaneAnimator on first frame
    [System.NonSerializedAttribute] public float FullGunAmmo;
    //use these for whatever, Only MissilesIncomingHeat is used by the prefab
    [System.NonSerializedAttribute] public int MissilesIncomingHeat = 0;
    [System.NonSerializedAttribute] public int MissilesIncomingRadar = 0;
    [System.NonSerializedAttribute] public int MissilesIncomingOther = 0;
    [System.NonSerializedAttribute] public Vector3 Spawnposition;
    [System.NonSerializedAttribute] public Quaternion Spawnrotation;
    [System.NonSerializedAttribute] public int OutsidePlaneLayer;
    [System.NonSerializedAttribute] public bool DoAAMTargeting;
    [System.NonSerializedAttribute] public Rigidbody GDHitRigidbody;
    [System.NonSerializedAttribute] public bool UsingManualSync;
    bool FloatingLastFrame = false;
    bool GroundedLastFrame = false;
    private float VelLiftStart;
    private int VehicleLayer;
    private float VelLiftMaxStart;
    private bool HasAirBrake;//set to false if air brake strength is 0
    private float HandDistanceZLastFrame;
    private float EngineAngle;
    private float PitchThrustVecMultiStart;
    private float YawThrustVecMultiStart;
    private float RollThrustVecMultiStart;
    [System.NonSerializedAttribute] public bool InVTOL;
    [System.NonSerializedAttribute] public bool VTOLenabled;
    [System.NonSerializedAttribute] public float VTOLAngleInput;
    private float VTOL90Degrees;//1=(90 degrees OR maxVTOLAngle if it's lower than 90) used for transition thrust values 
    private float ThrottleNormalizer;
    private float VTOLAngleDivider;
    private float ABNormalizer;
    private float EngineOutputLastFrame;
    float VTOLAngle90;
    bool HasWheelColliders = false;
    private float TaxiFullTurningSpeedDivider;
    private float vtolangledif;
    Vector3 VTOL180 = new Vector3(0, 0.01f, -1);//used as a rotation target for VTOL adjustment. Slightly below directly backward so that rotatetowards rotates on the correct axis
    [System.NonSerializedAttribute] public float ThrottleStrengthAB;
    [System.NonSerializedAttribute] public float FuelConsumptionAB;
    private bool VTolAngle90Plus;
    [System.NonSerializedAttribute] public bool AfterburnerOn;
    [System.NonSerializedAttribute] public bool PitchDown;//air is hitting plane from the top
    private float GDamageToTake;
    [System.NonSerializedAttribute] public float LastHitTime = -100;
    [System.NonSerializedAttribute] public float PredictedHealth;
    [System.NonSerializedAttribute] public SaccEntity LastAttacker;


    [System.NonSerializedAttribute] public int NumActiveFlares;
    [System.NonSerializedAttribute] public int NumActiveChaff;
    [System.NonSerializedAttribute] public int NumActiveOtherCM;
    //this stuff can be used by DFUNCs
    //if these == 0 then they are not disabled. Being an int allows more than one extension to disable it at a time
    [System.NonSerializedAttribute] public float Limits = 1;
    [System.NonSerializedAttribute] public int DisablePhysicsAndInputs = 0;
    [System.NonSerializedAttribute] public int OverrideConstantForce = 0;
    [System.NonSerializedAttribute] public Vector3 CFRelativeForceOverride;
    [System.NonSerializedAttribute] public Vector3 CFRelativeTorqueOverride;
    [System.NonSerializedAttribute] public int DisableTaxiRotation = 0;
    [System.NonSerializedAttribute] public int DisableGroundDetection = 0;
    [System.NonSerializedAttribute] public int ThrottleOverridden = 0;
    [System.NonSerializedAttribute] public float ThrottleOverride;
    [System.NonSerializedAttribute] public int JoystickOverridden = 0;
    [System.NonSerializedAttribute] public Vector3 JoystickOverride;


    [System.NonSerializedAttribute] public int ReSupplied = 0;
    public void SFEXT_L_EntityStart()
    {
        VehicleGameObj = EntityControl.gameObject;
        VehicleTransform = EntityControl.transform;
        VehicleRigidbody = EntityControl.GetComponent<Rigidbody>();
        VehicleConstantForce = EntityControl.GetComponent<ConstantForce>();

        Spawnposition = VehicleTransform.position;
        Spawnrotation = VehicleTransform.rotation;

        localPlayer = Networking.LocalPlayer;
        if (localPlayer == null)
        {
            InEditor = true;
            Piloting = true;
            IsOwner = true;
            Occupied = true;
            VehicleRigidbody.drag = 0;
            VehicleRigidbody.angularDrag = 0;
        }
        else
        {
            InEditor = false;
            InVR = localPlayer.IsUserInVR();
            if (localPlayer.isMaster)
            {
                IsOwner = true;
                VehicleRigidbody.drag = 0;
                VehicleRigidbody.angularDrag = 0;
            }
            else
            {
                VehicleRigidbody.drag = 9999;
                VehicleRigidbody.angularDrag = 9999;
            }
        }

        WheelCollider[] wc = PlaneMesh.GetComponentsInChildren<WheelCollider>(true);
        if (wc.Length != 0) { HasWheelColliders = true; }

        if (AutoAdjustValuesToMass)
        {
            //values that should feel the same no matter the weight of the aircraft
            float RBMass = VehicleRigidbody.mass;
            ThrottleStrength *= RBMass;
            PitchStrength *= RBMass;
            PitchFriction *= RBMass;
            PitchConstantFriction *= RBMass;
            YawStrength *= RBMass;
            YawFriction *= RBMass;
            YawConstantFriction *= RBMass;
            RollStrength *= RBMass;
            RollFriction *= RBMass;
            RollConstantFriction *= RBMass;
            VelStraightenStrPitch *= RBMass;
            VelStraightenStrYaw *= RBMass;
            Lift *= RBMass;
            MaxLift *= RBMass;
            VelLiftMax *= RBMass;
            AdverseRoll *= RBMass;
            AdverseYaw *= RBMass;
            foreach (WheelCollider wheel in wc)
            {
                JointSpring SusiSpring = wheel.suspensionSpring;
                SusiSpring.spring *= RBMass;
                SusiSpring.damper *= RBMass;
                wheel.suspensionSpring = SusiSpring;
            }
        }
        VehicleLayer = PlaneMesh.gameObject.layer;//get the layer of the plane as set by the world creator
        OutsidePlaneLayer = PlaneMesh.gameObject.layer;
        VehicleAnimator = EntityControl.GetComponent<Animator>();

        FullHealth = Health;
        FullFuel = Fuel;

        VelLiftMaxStart = VelLiftMax;
        VelLiftStart = VelLift;

        PitchThrustVecMultiStart = PitchThrustVecMulti;
        YawThrustVecMultiStart = YawThrustVecMulti;
        RollThrustVecMultiStart = RollThrustVecMulti;

        CenterOfMass = EntityControl.CenterOfMass;
        VehicleRigidbody.centerOfMass = VehicleTransform.InverseTransformDirection(CenterOfMass.position - VehicleTransform.position);//correct position if scaled
        VehicleRigidbody.inertiaTensorRotation = Quaternion.SlerpUnclamped(Quaternion.identity, VehicleRigidbody.inertiaTensorRotation, InertiaTensorRotationMulti);
        if (InvertITRYaw)
        {
            Vector3 ITR = VehicleRigidbody.inertiaTensorRotation.eulerAngles;
            ITR.x *= -1;
            VehicleRigidbody.inertiaTensorRotation = Quaternion.Euler(ITR);
        }

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
        }
        else
        {
            ReversingPitchStrengthZeroStart = ReversingPitchStrengthZero = PitchThrustVecMulti == 0 ? -ReversingPitchStrengthMulti : 1;
            ReversingYawStrengthZeroStart = ReversingYawStrengthZero = YawThrustVecMulti == 0 ? -ReversingYawStrengthMulti : 1;
            ReversingRollStrengthZeroStart = ReversingRollStrengthZero = RollThrustVecMulti == 0 ? -ReversingRollStrengthMulti : 1;
        }


        if (VTOLOnly) { VTOLenabled = true; }
        VTOL90Degrees = Mathf.Min(90 / VTOLMaxAngle, 1);

        if (!HasAfterburner) { ThrottleAfterburnerPoint = 1; }
        ThrottleNormalizer = 1 / ThrottleAfterburnerPoint;
        ABNormalizer = 1 / (1 - ThrottleAfterburnerPoint);

        FuelConsumptionAB = (FuelConsumption * FuelConsumptionABMulti) - FuelConsumption;
        ThrottleStrengthAB = (ThrottleStrength * AfterburnerThrustMulti) - ThrottleStrength;

        vtolangledif = VTOLMaxAngle - VTOLMinAngle;
        VTOLAngleDivider = VTOLAngleTurnRate / vtolangledif;
        VTOLAngle = VTOLAngleInput = VTOLDefaultValue;

        if (GroundDetectorRayDistance == 0)
        { DisableGroundDetection++; }

        if (GroundEffectEmpty == null)
        {
            Debug.LogWarning("GroundEffectEmpty not found, using CenterOfMass instead");
            GroundEffectEmpty = CenterOfMass;
        }

        VehicleObjectSync = (VRC.SDK3.Components.VRCObjectSync)EntityControl.gameObject.GetComponent(typeof(VRC.SDK3.Components.VRCObjectSync));
        if (VehicleObjectSync == null)
        {
            UsingManualSync = true;
        }

        LowFuelDivider = 1 / LowFuel;

        //thrust is lerped towards VTOLThrottleStrengthMulti by VTOLAngle, unless VTOLMaxAngle is greater than 90 degrees, then it's lerped by 90=1
        VTolAngle90Plus = VTOLMaxAngle > 90;

        if (DisallowTaxiRotationWhileStill)
        {
            TaxiFullTurningSpeedDivider = 1 / TaxiFullTurningSpeed;
        }
    }
    private void LateUpdate()
    {
        float DeltaTime = Time.deltaTime;
        if (IsOwner)//works in editor or ingame
        {
            if (!EntityControl.dead)
            {
                //G/crash Damage
                Health -= Mathf.Max((GDamageToTake) * DeltaTime * GDamage, 0f);//take damage of GDamage per second per G above MaxGs
                GDamageToTake = 0;
                if (Health <= 0f)//plane is ded
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Explode));

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
            if (DisableGroundDetection == 0)
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
            }

            //synced variables because rigidbody values aren't accessable by non-owner players
            CurrentVel = VehicleRigidbody.velocity;
            Speed = CurrentVel.magnitude;
            bool PlaneMoving = false;
            if (Speed > .1f)//don't bother doing all this for planes that arent moving and it therefore wont even effect
            {
                PlaneMoving = true;//check this bool later for more optimizations
                WindAndAoA();
            }

            if (Piloting)
            {
                //gotta do these this if we're piloting but it didn't get done(specifically, hovering extremely slowly in a VTOL craft will cause control issues we don't)
                if (!PlaneMoving)
                { WindAndAoA(); PlaneMoving = true; }
                if (RepeatingWorld)
                {
                    if (CenterOfMass.position.z > RepeatingWorldDistance)
                    {
                        Vector3 vehpos = VehicleTransform.position;
                        vehpos.z -= RepeatingWorldDistance * 2;
                        VehicleTransform.position = vehpos;
                    }
                    else if (CenterOfMass.position.z < -RepeatingWorldDistance)
                    {
                        Vector3 vehpos = VehicleTransform.position;
                        vehpos.z += RepeatingWorldDistance * 2;
                        VehicleTransform.position = vehpos;
                    }
                    else if (CenterOfMass.position.x > RepeatingWorldDistance)
                    {
                        Vector3 vehpos = VehicleTransform.position;
                        vehpos.x -= RepeatingWorldDistance * 2;
                        VehicleTransform.position = vehpos;
                    }
                    else if (CenterOfMass.position.x < -RepeatingWorldDistance)
                    {
                        Vector3 vehpos = VehicleTransform.position;
                        vehpos.x += RepeatingWorldDistance * 2;
                        VehicleTransform.position = vehpos;
                    }
                }

                if (DisablePhysicsAndInputs == 0)
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
                    Vector3 JoystickPosYaw;
                    Vector3 JoystickPos;
                    Vector2 VRPitchRoll;

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
                    //VR Joystick                
                    if (JoyStickGrip > 0.75)
                    {
                        Quaternion PlaneRotDif = VehicleTransform.rotation * Quaternion.Inverse(PlaneRotLastFrame);//difference in plane's rotation since last frame
                        PlaneRotLastFrame = VehicleTransform.rotation;
                        JoystickZeroPoint = PlaneRotDif * JoystickZeroPoint;//zero point rotates with the plane so it appears still to the pilot
                        if (!JoystickGripLastFrame)//first frame you gripped joystick
                        {
                            EntityControl.SendEventToExtensions("SFEXT_O_JoystickGrabbed");
                            PlaneRotDif = Quaternion.identity;
                            if (SwitchHandsJoyThrottle)
                            { JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation; }//rotation of the controller relative to the plane when it was pressed
                            else
                            { JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation; }
                        }
                        //difference between the plane and the hand's rotation, and then the difference between that and the JoystickZeroPoint
                        Quaternion JoystickDifference;
                        if (SwitchHandsJoyThrottle)
                        { JoystickDifference = (Quaternion.Inverse(VehicleTransform.rotation) * localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation) * Quaternion.Inverse(JoystickZeroPoint); }
                        else { JoystickDifference = (Quaternion.Inverse(VehicleTransform.rotation) * localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation) * Quaternion.Inverse(JoystickZeroPoint); }

                        JoystickPosYaw = (JoystickDifference * VehicleTransform.forward);//angles to vector
                        JoystickPosYaw.y = 0;
                        JoystickPos = (JoystickDifference * VehicleTransform.up);
                        VRPitchRoll = new Vector2(JoystickPos.x, JoystickPos.z) * 1.41421f;

                        JoystickGripLastFrame = true;
                        //making a circular joy stick square
                        //pitch and roll
                        if (Mathf.Abs(VRPitchRoll.x) > Mathf.Abs(VRPitchRoll.y))
                        {
                            if (Mathf.Abs(VRPitchRoll.x) > 0)
                            {
                                float temp = VRPitchRoll.magnitude / Mathf.Abs(VRPitchRoll.x);
                                VRPitchRoll *= temp;
                            }
                        }
                        else if (Mathf.Abs(VRPitchRoll.y) > 0)
                        {
                            float temp = VRPitchRoll.magnitude / Mathf.Abs(VRPitchRoll.y);
                            VRPitchRoll *= temp;
                        }
                        //yaw
                        if (Mathf.Abs(JoystickPosYaw.x) > Mathf.Abs(JoystickPosYaw.z))
                        {
                            if (Mathf.Abs(JoystickPosYaw.x) > 0)
                            {
                                float temp = JoystickPosYaw.magnitude / Mathf.Abs(JoystickPosYaw.x);
                                JoystickPosYaw *= temp;
                            }
                        }
                        else if (Mathf.Abs(JoystickPosYaw.z) > 0)
                        {
                            float temp = JoystickPosYaw.magnitude / Mathf.Abs(JoystickPosYaw.z);
                            JoystickPosYaw *= temp;
                        }

                    }
                    else
                    {
                        JoystickPosYaw.x = 0;
                        VRPitchRoll = Vector3.zero;
                        if (JoystickGripLastFrame)//first frame you gripped joystick
                        { EntityControl.SendEventToExtensions("SFEXT_O_JoystickDropped"); }
                        JoystickGripLastFrame = false;
                    }

                    if (HasAfterburner)
                    {
                        if (AfterburnerOn)
                        { PlayerThrottle = Mathf.Clamp(PlayerThrottle + ((Shifti - LeftControli) * .5f * DeltaTime), 0, 1f); }
                        else
                        { PlayerThrottle = Mathf.Clamp(PlayerThrottle + ((Shifti - LeftControli) * .5f * DeltaTime), 0, ThrottleAfterburnerPoint); }
                    }
                    else
                    { PlayerThrottle = Mathf.Clamp(PlayerThrottle + ((Shifti - LeftControli) * .5f * DeltaTime), 0, 1f); }
                    //VR Throttle
                    if (ThrottleGrip > 0.75)
                    {
                        Vector3 handdistance;
                        if (SwitchHandsJoyThrottle)
                        { handdistance = VehicleTransform.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position; }
                        else { handdistance = VehicleTransform.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position; }
                        handdistance = VehicleTransform.InverseTransformDirection(handdistance);

                        Vector3 PlaySpaceDistance = transform.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position;
                        PlaySpaceDistance = VehicleTransform.InverseTransformDirection(PlaySpaceDistance);

                        float HandThrottleAxis;
                        if (VerticalThrottle)
                        {
                            HandThrottleAxis = handdistance.y;
                            /*    - (PlaySpaceDistance.y - ThrottlePlayspaceLastFrame);
                              ThrottlePlayspaceLastFrame = PlaySpaceDistance.y; */
                        }
                        else
                        {
                            HandThrottleAxis = handdistance.z;
                            /*     - (PlaySpaceDistance.y - ThrottlePlayspaceLastFrame);
                               ThrottlePlayspaceLastFrame = PlaySpaceDistance.z; */
                        }

                        if (!ThrottleGripLastFrame)
                        {
                            EntityControl.SendEventToExtensions("SFEXT_O_ThrottleGrabbed");
                            ThrottleZeroPoint = HandThrottleAxis;
                            TempThrottle = PlayerThrottle;
                            HandDistanceZLastFrame = 0;
                        }
                        float ThrottleDifference = ThrottleZeroPoint - HandThrottleAxis;
                        ThrottleDifference *= ThrottleSensitivity;
                        bool VTOLandAB_Disallowed = (!VTOLAllowAfterburner && VTOLAngle != 0);/*don't allow VTOL AB disabled planes, false if attemping to*/

                        //Detent function to prevent you going into afterburner by accident (bit of extra force required to turn on AB (actually hand speed))
                        if (((HandDistanceZLastFrame - HandThrottleAxis) * ThrottleSensitivity > .05f)/*detent overcome*/ && !VTOLandAB_Disallowed && Fuel > LowFuel || ((PlayerThrottle > ThrottleAfterburnerPoint/*already in afterburner*/&& !VTOLandAB_Disallowed) || !HasAfterburner))
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
                        }
                        ThrottleGripLastFrame = false;
                    }

                    if (DisableTaxiRotation == 0 && Taxiing)
                    {
                        AngleOfAttack = 0;//prevent stall sound and aoavapor when on ground
                                          //rotate if trying to yaw
                        float TaxiingStillMulti = 1;
                        if (DisallowTaxiRotationWhileStill)
                        { TaxiingStillMulti = Mathf.Min(Speed * TaxiFullTurningSpeedDivider, 1); }
                        Taxiinglerper = Mathf.Lerp(Taxiinglerper, RotationInputs.y * TaxiRotationSpeed * Time.smoothDeltaTime * TaxiingStillMulti, TaxiRotationResponse * DeltaTime);
                        VehicleTransform.Rotate(Vector3.up, Taxiinglerper);

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
                    if (Input.GetKeyDown(AfterBurnerKey) && HasAfterburner && (VTOLAngle == 0 || VTOLAllowAfterburner))
                    {
                        if (AfterburnerOn)
                            PlayerThrottle = ThrottleAfterburnerPoint;
                        else
                            PlayerThrottle = 1;
                    }
                    if (ThrottleOverridden > 0 && !ThrottleGripLastFrame)
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

                    Vector2 Throttles = UnpackThrottles(ThrottleInput);
                    Fuel = Mathf.Max(Fuel -
                                        ((Mathf.Max(Throttles.x, 0.25f) * FuelConsumption)
                                            + (Throttles.y * FuelConsumptionAB)) * DeltaTime, 0);


                    if (Fuel < LowFuel) { ThrottleInput = ThrottleInput * (Fuel * LowFuelDivider); }//decrease max throttle as fuel runs out

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
                    if (JoystickOverridden > 0 && !JoystickGripLastFrame)//joystick override enabled, and player not holding joystick
                    {
                        RotationInputs = JoystickOverride;
                    }
                    else//joystick override disabled, player has control
                    {
                        if (!InVR)
                        {
                            //allow stick flight in desktop mode
                            Vector2 LStickPos = new Vector2(0, 0);
                            Vector2 RStickPos = new Vector2(0, 0);
                            if (!InEditor)
                            {
                                LStickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
                                LStickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
                                RStickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
                                RStickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
                            }
                            VRPitchRoll = LStickPos;
                            JoystickPosYaw.x = RStickPos.x;
                            //make stick input square
                            if (Mathf.Abs(VRPitchRoll.x) > Mathf.Abs(VRPitchRoll.y))
                            {
                                if (Mathf.Abs(VRPitchRoll.x) > 0)
                                {
                                    float temp = VRPitchRoll.magnitude / Mathf.Abs(VRPitchRoll.x);
                                    VRPitchRoll *= temp;
                                }
                            }
                            else if (Mathf.Abs(VRPitchRoll.y) > 0)
                            {
                                float temp = VRPitchRoll.magnitude / Mathf.Abs(VRPitchRoll.y);
                                VRPitchRoll *= temp;
                            }
                        }

                        RotationInputs.x = Mathf.Clamp(VRPitchRoll.y + Wi + Si + downi + upi, -1, 1) * Limits;
                        RotationInputs.y = Mathf.Clamp(Qi + Ei + JoystickPosYaw.x, -1, 1) * Limits;
                        //roll isn't subject to flight limits
                        RotationInputs.z = Mathf.Clamp(((VRPitchRoll.x + Ai + Di + lefti + righti) * -1), -1, 1);
                    }

                    //ability to adjust input to be more precise at low amounts. 'exponant'
                    /* RotationInputs.x = RotationInputs.x > 0 ? Mathf.Pow(RotationInputs.x, StickInputPower) : -Mathf.Pow(Mathf.Abs(RotationInputs.x), StickInputPower);
                    RotationInputs.y = RotationInputs.y > 0 ? Mathf.Pow(RotationInputs.y, StickInputPower) : -Mathf.Pow(Mathf.Abs(RotationInputs.y), StickInputPower);
                    RotationInputs.z = RotationInputs.z > 0 ? Mathf.Pow(RotationInputs.z, StickInputPower) : -Mathf.Pow(Mathf.Abs(RotationInputs.z), StickInputPower); */

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
                    roll = RotationInputs.z * RollStrength * ReversingRollStrength;


                    if (pitch > 0)
                    {
                        pitch *= PitchDownStrMulti;
                    }

                    //wheel colliders are broken, this workaround stops the plane from being 'sticky' when you try to start moving it.
                    if (Speed < .2 && HasWheelColliders && ThrottleInput > 0)
                    {
                        if (VTOLAngle > VTOL90Degrees)
                        { VehicleRigidbody.velocity = VehicleTransform.forward * -.25f; }
                        else
                        { VehicleRigidbody.velocity = VehicleTransform.forward * .25f; }
                    }

                    if (VTOLenabled)
                    {
                        if (!(VTOLAngle == VTOLAngleInput && VTOLAngleInput == 0) || VTOLOnly)//only SetVTOLValues if it'll do anything
                        {
                            SetVTOLValues();
                            if (!InVTOL)
                            { EntityControl.SendEventToExtensions("SFEXT_O_EnterVTOL"); }
                            InVTOL = true;
                        }
                        else
                        {
                            if (InVTOL)
                            { EntityControl.SendEventToExtensions("SFEXT_O_ExitVTOL"); }
                            InVTOL = false;
                        }
                    }
                }
            }
            else
            {
                //brake is always on if the plane is on the ground
                if (Taxiing)
                {
                    StillWindMulti = Mathf.Min(Speed * .1f, 1);
                }
                else { StillWindMulti = 1; }
            }

            if (DisablePhysicsAndInputs == 0)
            {
                //Lerp the inputs for 'engine response', throttle decrease response is slower than increase (EngineSpoolDownSpeedMulti)
                if (EngineOutput < ThrottleInput)
                { EngineOutput = Mathf.Lerp(EngineOutput, ThrottleInput, AccelerationResponse * DeltaTime); }
                else
                { EngineOutput = Mathf.Lerp(EngineOutput, ThrottleInput, AccelerationResponse * EngineSpoolDownSpeedMulti * DeltaTime); }

                float sidespeed = 0;
                float downspeed = 0;
                float SpeedLiftFactor = 0;

                if (PlaneMoving)//optimization
                {
                    //used to create air resistance for updown and sideways if your movement direction is in those directions
                    //to add physics to plane's yaw and pitch, accel angvel towards velocity, and add force to the plane
                    //and add wind
                    sidespeed = Vector3.Dot(AirVel, VehicleTransform.right);
                    downspeed = -Vector3.Dot(AirVel, VehicleTransform.up);

                    PitchDown = (downspeed < 0) ? true : false;//air is hitting plane from above
                    if (PitchDown)
                    {
                        downspeed *= PitchDownLiftMulti;
                        SpeedLiftFactor = Mathf.Min(AirSpeed * AirSpeed * Lift, MaxLift * PitchDownLiftMulti);
                    }
                    else
                    {
                        SpeedLiftFactor = Mathf.Min(AirSpeed * AirSpeed * Lift, MaxLift);
                    }
                    rotlift = Mathf.Min(AirSpeed / RotMultiMaxSpeed, 1);//using a simple linear curve for increasing control as you move faster

                    //thrust vectoring airplanes have a minimum rotation control
                    float minlifttemp = rotlift * Mathf.Min(AoALiftPitch, AoALiftYaw);
                    pitch *= Mathf.Max(PitchThrustVecMulti * ThrustVecGrounded, minlifttemp);
                    yaw *= Mathf.Max(YawThrustVecMulti * ThrustVecGrounded, minlifttemp);
                    roll *= Mathf.Max(RollThrustVecMulti * ThrustVecGrounded, minlifttemp);

                    //rotation inputs are done, now we can set the minimum lift/drag when at high aoa, this should be higher than 0 because if it's 0 you will have 0 drag when at 90 degree AoA.
                    AoALiftPitch = Mathf.Clamp(AoALiftPitch, HighPitchAoaMinLift, 1);
                    AoALiftYaw = Mathf.Clamp(AoALiftYaw, HighYawAoaMinLift, 1);

                    //Lerp the inputs for 'rotation response'
                    LerpedRoll = Mathf.Lerp(LerpedRoll, roll, RollResponse * DeltaTime);
                    LerpedPitch = Mathf.Lerp(LerpedPitch, pitch, PitchResponse * DeltaTime);
                    LerpedYaw = Mathf.Lerp(LerpedYaw, yaw, YawResponse * DeltaTime);
                }
                else
                {
                    VelLift = pitch = yaw = roll = 0;
                }

                if ((PlaneMoving) && OverrideConstantForce == 0)
                {
                    //Create a Vector3 Containing the thrust, and rotate and adjust strength based on VTOL value
                    //engine output is multiplied so that max throttle without afterburner is max strength (unrelated to vtol)
                    Vector3 FinalInputAcc = new Vector3(-sidespeed * SidewaysLift * SpeedLiftFactor * AoALiftYaw,// X Sideways
                            (downspeed * ExtraLift * PitchDownLiftMulti * SpeedLiftFactor * AoALiftPitch),// Y Up
                            0);//Z Forward

                    float GroundEffectAndVelLift = 0;

                    Vector2 Outputs = UnpackThrottles(EngineOutput);
                    float Thrust = (Mathf.Min(Outputs.x)//Throttle
                    * ThrottleStrength
                    + Mathf.Max((Outputs.y), 0)//Afterburner throttle
                    * ThrottleStrengthAB);


                    if (VTOLenabled)
                    {
                        //float thrust = EngineOutput * ThrottleStrength * AfterburnerThrottle * AfterburnerThrustMulti * Atmosphere;
                        float VTOLAngle2 = VTOLMinAngle + (vtolangledif * VTOLAngle);//vtol angle in degrees

                        Vector3 VTOLInputAcc;//rotate and scale Vector for VTOL thrust
                        if (VTOLOnly)//just use regular thrust strength if vtol only, no transition to plane flight
                        {
                            VTOLInputAcc = Vector3.RotateTowards(Vector3.forward, VTOL180, VTOLAngle2 * Mathf.Deg2Rad, 0) * Thrust;
                        }
                        else//vehicle can transition from plane-like flight to helicopter-like flight, with different thrust values for each, with a smooth transition between them
                        {
                            float downthrust = Thrust * VTOLThrottleStrengthMulti;
                            VTOLInputAcc = Vector3.RotateTowards(Vector3.forward, VTOL180, VTOLAngle2 * Mathf.Deg2Rad, 0) * Mathf.Lerp(Thrust, Thrust * VTOLThrottleStrengthMulti, VTolAngle90Plus ? VTOLAngle90 : VTOLAngle);
                        }
                        //add ground effect to the VTOL thrust
                        GroundEffectAndVelLift = GroundEffect(true, GroundEffectEmpty.position, -VehicleTransform.TransformDirection(VTOLInputAcc), VTOLGroundEffectStrength, 1);
                        VTOLInputAcc *= GroundEffectAndVelLift;

                        //Add Airplane Ground Effect
                        GroundEffectAndVelLift = GroundEffect(false, GroundEffectEmpty.position, -VehicleTransform.up, GroundEffectStrength, SpeedLiftFactor);
                        //add lift and thrust

                        FinalInputAcc += VTOLInputAcc;
                        FinalInputAcc.y += GroundEffectAndVelLift;
                        FinalInputAcc *= Atmosphere;
                    }
                    else//Simpler version for non-VTOL craft
                    {
                        GroundEffectAndVelLift = GroundEffect(false, GroundEffectEmpty.position, -VehicleTransform.up, GroundEffectStrength, SpeedLiftFactor);

                        FinalInputAcc.y += GroundEffectAndVelLift;
                        FinalInputAcc.z += Thrust;
                        FinalInputAcc *= Atmosphere;
                    }

                    float outputdif = (EngineOutput - EngineOutputLastFrame);
                    float ADVYaw = outputdif * AdverseYaw;
                    float ADVRoll = outputdif * AdverseRoll;
                    EngineOutputLastFrame = EngineOutput;
                    //used to add rotation friction
                    Vector3 localAngularVelocity = transform.InverseTransformDirection(VehicleRigidbody.angularVelocity);


                    //roll + rotational frictions
                    Vector3 FinalInputRot = new Vector3((-localAngularVelocity.x * PitchFriction * rotlift * AoALiftPitch * AoALiftYaw * Atmosphere) - (localAngularVelocity.x * PitchConstantFriction),// X Pitch
                        (-localAngularVelocity.y * YawFriction * rotlift * AoALiftPitch * AoALiftYaw) + ADVYaw * Atmosphere - (localAngularVelocity.y * YawConstantFriction),// Y Yaw
                            ((LerpedRoll + (-localAngularVelocity.z * RollFriction * rotlift * AoALiftPitch * AoALiftYaw)) + ADVRoll * Atmosphere) - (localAngularVelocity.z * RollConstantFriction));// Z Roll

                    //create values for use in fixedupdate (control input and straightening forces)
                    Pitching = ((((VehicleTransform.up * LerpedPitch) + (VehicleTransform.up * downspeed * VelStraightenStrPitch * AoALiftPitch * rotlift)) * Atmosphere));
                    Yawing = ((((VehicleTransform.right * LerpedYaw) + (VehicleTransform.right * -sidespeed * VelStraightenStrYaw * AoALiftYaw * rotlift)) * Atmosphere));

                    VehicleConstantForce.relativeForce = FinalInputAcc;
                    VehicleConstantForce.relativeTorque = FinalInputRot;
                }
                else
                {
                    VehicleConstantForce.relativeForce = CFRelativeForceOverride;
                    VehicleConstantForce.relativeTorque = CFRelativeTorqueOverride;
                }
            }

            SoundBarrier = (-Mathf.Clamp(Mathf.Abs(Speed - 343) / SoundBarrierWidth, 0, 1) + 1) * SoundBarrierStrength;
        }
        else//non-owners need to know these values
        {
            Speed = AirSpeed = CurrentVel.magnitude;//wind speed is local anyway, so just use ground speed for non-owners
            rotlift = Mathf.Min(Speed / RotMultiMaxSpeed, 1);//so passengers can hear the airbrake
                                                             //AirVel = VehicleRigidbody.velocity - Wind;//wind isn't synced so this will be wrong
                                                             //AirSpeed = AirVel.magnitude;
        }
    }
    private void FixedUpdate()
    {
        if (IsOwner)
        {
            float DeltaTime = Time.fixedDeltaTime;
            //lerp velocity toward 0 to simulate air friction
            Vector3 VehicleVel = VehicleRigidbody.velocity;
            VehicleRigidbody.velocity = Vector3.Lerp(VehicleVel, FinalWind * StillWindMulti * Atmosphere, ((((AirFriction + SoundBarrier) * ExtraDrag)) * 90) * DeltaTime);
            //apply pitching using pitch moment
            VehicleRigidbody.AddForceAtPosition(Pitching, PitchMoment.position, ForceMode.Force);//deltatime is built into ForceMode.Force
                                                                                                 //apply yawing using yaw moment
            VehicleRigidbody.AddForceAtPosition(Yawing, YawMoment.position, ForceMode.Force);
            //calc Gs
            float gravity = 9.81f * DeltaTime;
            LastFrameVel.y -= gravity; //add gravity
            AllGs = Vector3.Distance(LastFrameVel, VehicleVel) / gravity;
            GDamageToTake += Mathf.Max((AllGs - MaxGs), 0);

            Vector3 Gs3 = VehicleTransform.InverseTransformDirection(VehicleVel - LastFrameVel);
            VertGs = Gs3.y / gravity;
            LastFrameVel = VehicleVel;
        }
    }
    public void Explode()//all the things players see happen when the vehicle explodes
    {
        EntityControl.dead = true;
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
        Atmosphere = 1;//planemoving optimization requires this to be here
        Pitching = Vector3.zero;
        Yawing = Vector3.zero;

        EntityControl.SendEventToExtensions("SFEXT_G_Explode");

        SendCustomEventDelayedSeconds(nameof(ReAppear), RespawnDelay);
        SendCustomEventDelayedSeconds(nameof(NotDead), RespawnDelay + InvincibleAfterSpawn);

        if (IsOwner)
        {
            VehicleRigidbody.velocity = Vector3.zero;
            VehicleRigidbody.angularVelocity = Vector3.zero;
            VehicleRigidbody.drag = 9999;
            VehicleRigidbody.angularDrag = 9999;
            Health = FullHealth;//turns off low health smoke
            Fuel = FullFuel;
            AoALiftPitch = 0;
            AoALiftYaw = 0;
            AngleOfAttack = 0;
            VelLift = VelLiftStart;
            VTOLAngle90 = 0;
            SendCustomEventDelayedSeconds("MoveToSpawn", RespawnDelay - 3);
            EntityControl.SendEventToExtensions("SFEXT_O_Explode");
        }

        //pilot and passengers are dropped out of the plane
        if ((Piloting || Passenger) && !InEditor)
        {
            EntityControl.ExitStation();
        }
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
    }
    public void ReAppear()
    {
        EntityControl.SendEventToExtensions("SFEXT_G_ReAppear");
        if (IsOwner)
        {
            VehicleRigidbody.drag = 0;
            VehicleRigidbody.angularDrag = 0;
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
        if (InEditor || UsingManualSync)
        {
            VehicleTransform.SetPositionAndRotation(Spawnposition, Spawnrotation);
        }
        else
        {
            VehicleObjectSync.Respawn();
        }
        EntityControl.SendEventToExtensions("SFEXT_O_MoveToSpawn");
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
        AfterburnerOn = true;
        EntityControl.SendEventToExtensions("SFEXT_G_AfterburnerOn");
    }
    public void SetAfterburnerOff()
    {
        AfterburnerOn = false;
        EntityControl.SendEventToExtensions("SFEXT_G_AfterburnerOff");
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
    public void SFEXT_O_RespawnButton()//called when using respawn button
    {
        if (!Occupied && !EntityControl.dead)
        {
            Networking.SetOwner(localPlayer, EntityControl.gameObject);
            EntityControl.TakeOwnerShipOfExtensions();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ResetStatus");
            IsOwner = true;
            Atmosphere = 1;//planemoving optimization requires this to be here
                           //synced variables
            Health = FullHealth;
            Fuel = FullFuel;
            VTOLAngle = VTOLDefaultValue;
            VTOLAngleInput = VTOLDefaultValue;
            if (InEditor || UsingManualSync)
            {
                VehicleTransform.SetPositionAndRotation(Spawnposition, Spawnrotation);
                VehicleRigidbody.velocity = Vector3.zero;
            }
            else
            {
                VehicleObjectSync.Respawn();
            }
            VehicleRigidbody.angularVelocity = Vector3.zero;//editor needs this
        }
    }
    public void ResetStatus()//called globally when using respawn button
    {
        if (HasAfterburner) { SetAfterburnerOff(); }
        //these two make it invincible and unable to be respawned again for 5s
        EntityControl.dead = true;
        SendCustomEventDelayedSeconds(nameof(NotDead), InvincibleAfterSpawn);
        EntityControl.SendEventToExtensions("SFEXT_G_RespawnButton");
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
        LastHitTime = Time.time;
        if (IsOwner)
        {
            Health -= BulletDamageTaken;
            if (PredictDamage && Health <= 0)//the attacker calls the explode function in this case
            {
                Health = 0.1f;
                //if two people attacked us, and neither predicted they killed us but we took enough damage to die, we must still die.
                SendCustomEventDelayedSeconds(nameof(CheckLaggyKilled), .25f);//give enough time for the explode event to happen if they did predict we died, otherwise do it ourself
            }
        }
    }
    public void CheckLaggyKilled()
    {
        //Check if we still have the amount of health set to not send explode when killed, and if we do send explode
        if (Health == 0.1f)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Explode));
        }
    }
    //Add .001 to each value of damage taken to prevent float comparison bullshit
    public void SFEXT_L_MissileHit25()
    {
        if (PredictDamage)
        { MissileDamagePrediction(.251f); }
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendMissileHit25));
    }
    public void SendMissileHit25()
    {
        EntityControl.SendEventToExtensions("SFEXT_G_MissileHit25");
    }
    public void SFEXT_G_MissileHit25()
    {
        if (IsOwner)
        { TakeMissileDamage(.251f); }
        LastHitTime = Time.time;
    }
    public void SFEXT_L_MissileHit50()
    {
        if (PredictDamage)
        { MissileDamagePrediction(.501f); }
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendMissileHit50));
    }
    public void SendMissileHit50()
    {
        EntityControl.SendEventToExtensions("SFEXT_G_MissileHit50");
    }
    public void SFEXT_G_MissileHit50()
    {
        if (IsOwner)
        { TakeMissileDamage(.501f); }
        LastHitTime = Time.time;
    }
    public void SFEXT_L_MissileHit75()
    {
        if (PredictDamage)
        { MissileDamagePrediction(.751f); }
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendMissileHit75));
    }
    public void SendMissileHit75()
    {
        EntityControl.SendEventToExtensions("SFEXT_G_MissileHit75");
    }
    public void SFEXT_G_MissileHit75()
    {
        if (IsOwner)
        { TakeMissileDamage(.751f); }
        LastHitTime = Time.time;
    }
    public void SFEXT_L_MissileHit100()
    {
        if (PredictDamage)
        { MissileDamagePrediction(1.001f); }
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendMissileHit100));
    }
    public void SendMissileHit100()
    {
        EntityControl.SendEventToExtensions("SFEXT_G_MissileHit100");
    }
    public void SFEXT_G_MissileHit100()
    {
        if (IsOwner)
        { TakeMissileDamage(1.001f); }
        LastHitTime = Time.time;
    }
    public void TakeMissileDamage(float damage)
    {
        Health -= ((FullHealth * damage) * MissileDamageTakenMultiplier);
        if (PredictDamage && Health <= 0)
        { Health = 0.1f; }//the attacker calls the explode function in this case
        Vector3 explosionforce = new Vector3(Random.Range(-MissilePushForce, MissilePushForce), Random.Range(-MissilePushForce, MissilePushForce), Random.Range(-MissilePushForce, MissilePushForce)) * damage;
        VehicleRigidbody.AddTorque(explosionforce, ForceMode.VelocityChange);
    }
    private void MissileDamagePrediction(float Damage)
    {
        if (Time.time - LastHitTime > 2)
        {
            PredictedHealth = Health - ((FullHealth * Damage) * MissileDamageTakenMultiplier);
            if (PredictedHealth <= 0)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Explode));
            }
        }
        else
        {
            PredictedHealth -= ((FullHealth * Damage) * MissileDamageTakenMultiplier);
            if (PredictedHealth <= 0)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Explode));
            }
        }
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
        MissilesIncomingHeat = 0;
        MissilesIncomingRadar = 0;
        MissilesIncomingOther = 0;
        SetCollidersLayer(VehicleLayer);
    }
    public void SFEXT_O_TakeOwnership()
    {
        IsOwner = true;
        VehicleRigidbody.velocity = CurrentVel;
        VehicleRigidbody.drag = 0;
        VehicleRigidbody.angularDrag = 0;
    }
    public void SFEXT_O_LoseOwnership()
    {
        IsOwner = false;
        VehicleRigidbody.drag = 9999;
        VehicleRigidbody.angularDrag = 9999;
    }
    public void SFEXT_O_PilotEnter()
    {
        //setting this as a workaround because it doesnt work reliably in Start()
        if (!InEditor)
        {
            InVR = localPlayer.IsUserInVR();//move me to start when they fix the bug
                                            //https://feedback.vrchat.com/vrchat-udon-closed-alpha-bugs/p/vrcplayerapiisuserinvr-for-the-local-player-is-not-returned-correctly-when-calle
        }

        EngineOutput = 0;
        ThrottleInput = 0;
        PlayerThrottle = 0;
        VTOLAngleInput = VTOLAngle;
        GDHitRigidbody = null;

        Piloting = true;
        if (EntityControl.dead) { Health = FullHealth; }//dead is true for the first 5 seconds after spawn, this might help with spontaneous explosions

        //hopefully prevents explosions when you enter the plane
        VehicleRigidbody.velocity = CurrentVel;
        VertGs = 0;
        AllGs = 0;
        LastFrameVel = CurrentVel;

        SetCollidersLayer(OnboardVehicleLayer);
    }
    public void SFEXT_G_PilotEnter()
    {
        Occupied = true;
        EntityControl.dead = false;//Plane stops being invincible if someone gets in, also acts as redundancy incase someone missed the notdead event
    }
    public void SFEXT_G_PilotExit()
    {
        Occupied = false;
        SetAfterburnerOff();
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
        RotationInputs = Vector3.zero;
        ThrottleInput = 0;
        //reset everything
        Piloting = false;
        Taxiinglerper = 0;
        ThrottleGripLastFrame = false;
        JoystickGripLastFrame = false;
        DoAAMTargeting = false;
        MissilesIncomingHeat = 0;
        MissilesIncomingRadar = 0;
        MissilesIncomingOther = 0;
        Pitching = Vector3.zero;
        Yawing = Vector3.zero;
        localPlayer.SetVelocity(CurrentVel);

        //set vehicle's collider's layers back
        SetCollidersLayer(VehicleLayer);
    }
    public void SetCollidersLayer(int NewLayer)
    {
        if (PlaneMesh)
        {
            Transform[] children = PlaneMesh.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                child.gameObject.layer = NewLayer;
            }
        }
    }
    private void WindAndAoA()
    {
        if (DisablePhysicsAndInputs != 0) { return; }
        Atmosphere = Mathf.Clamp(-(CenterOfMass.position.y / AtmoshpereFadeDistance) + 1 + AtmosphereHeightThing, 0, 1);
        float TimeGustiness = Time.time * WindGustiness;
        float gustx = TimeGustiness + (VehicleTransform.position.x * WindTurbulanceScale);
        float gustz = TimeGustiness + (VehicleTransform.position.z * WindTurbulanceScale);
        FinalWind = Vector3.Normalize(new Vector3((Mathf.PerlinNoise(gustx + 9000, gustz) - .5f), /* (Mathf.PerlinNoise(gustx - 9000, gustz - 9000) - .5f) */0, (Mathf.PerlinNoise(gustx, gustz + 9999) - .5f))) * WindGustStrength;
        FinalWind = (FinalWind + Wind) * Atmosphere;
        AirVel = VehicleRigidbody.velocity - (FinalWind * StillWindMulti);
        AirSpeed = AirVel.magnitude;
        Vector3 VecForward = VehicleTransform.forward;
        AngleOfAttackPitch = Vector3.SignedAngle(VecForward, AirVel, VehicleTransform.right);
        AngleOfAttackYaw = Vector3.SignedAngle(VecForward, AirVel, VehicleTransform.up);

        //angle of attack stuff, pitch and yaw are calculated seperately
        //pitch and yaw each have a curve for when they are within the 'MaxAngleOfAttack' and a linear version up to 90 degrees, which are Max'd (using Mathf.Clamp) for the final result.
        //the linear version is used for high aoa, and is 0 when at 90 degrees, and 1(multiplied by HighAoaMinControl) at 0. When at more than 90 degrees, the control comes back with the same curve but the inputs are inverted. (unless thrust vectoring is enabled) The invert code is elsewhere.
        AoALiftPitch = Mathf.Min(Mathf.Abs(AngleOfAttackPitch) / MaxAngleOfAttackPitch, Mathf.Abs(Mathf.Abs(AngleOfAttackPitch) - 180) / MaxAngleOfAttackPitch);//angle of attack as 0-1 float, for backwards and forwards
        AoALiftPitch = -AoALiftPitch + 1;
        AoALiftPitch = -Mathf.Pow((1 - AoALiftPitch), AoaCurveStrength) + 1;//give it a curve

        float AoALiftPitchMin = Mathf.Min(Mathf.Abs(AngleOfAttackPitch) * 0.0111111111f/* same as divide by 90 */, Mathf.Abs(Mathf.Abs(AngleOfAttackPitch) - 180) * 0.0111111111f/* same as divide by 90 */);//linear version to 90 for high aoa
        AoALiftPitchMin = Mathf.Clamp((-AoALiftPitchMin + 1) * HighPitchAoaMinControl, 0, 1);
        AoALiftPitch = Mathf.Clamp(AoALiftPitch, AoALiftPitchMin, 1);

        AoALiftYaw = Mathf.Min(Mathf.Abs(AngleOfAttackYaw) / MaxAngleOfAttackYaw, Mathf.Abs((Mathf.Abs(AngleOfAttackYaw) - 180)) / MaxAngleOfAttackYaw);
        AoALiftYaw = -AoALiftYaw + 1;
        AoALiftYaw = -Mathf.Pow((1 - AoALiftYaw), AoaCurveStrength) + 1;//give it a curve

        float AoALiftYawMin = Mathf.Min(Mathf.Abs(AngleOfAttackYaw) * 0.0111111111f/* same as divide by 90 */, Mathf.Abs(Mathf.Abs(AngleOfAttackYaw) - 180) * 0.0111111111f/* same as divide by 90 */);//linear version to 90 for high aoa
        AoALiftYawMin = Mathf.Clamp((-AoALiftPitchMin + 1) * HighYawAoaMinControl, 0, 1);
        AoALiftYaw = Mathf.Clamp(AoALiftYaw, AoALiftYawMin, 1);

        AngleOfAttack = Mathf.Max(Mathf.Abs(AngleOfAttackPitch), Mathf.Abs(AngleOfAttackYaw));
    }
    private float GroundEffect(bool VTOL, Vector3 Position, Vector3 Direction, float GEStrength, float speedliftfac)
    {
        //Ground effect, extra lift caused by air pressure when close to the ground
        RaycastHit GE;
        if (Physics.Raycast(Position, Direction, out GE, GroundEffectMaxDistance, 2065 /* Default, Water and Environment */, QueryTriggerInteraction.Collide))
        {
            float GroundEffect = ((-GE.distance + GroundEffectMaxDistance) / GroundEffectMaxDistance) * GEStrength;
            if (VTOL) { return 1 + GroundEffect; }
            GroundEffect *= ExtraLift;
            VelLift = VelLiftStart + GroundEffect;
            VelLiftMax = Mathf.Max(VelLiftMaxStart, VTOL ? 99999f : GroundEffectLiftMax);
        }
        else//set non-groundeffect'd vel lift values
        {
            if (VTOL) { return 1; }
            VelLift = VelLiftStart;
            VelLiftMax = VelLiftMaxStart;
        }
        return Mathf.Min(speedliftfac * AoALiftPitch * VelLift, VelLiftMax);
    }
    private void SetVTOLValues()
    {
        VTOLAngle = Mathf.MoveTowards(VTOLAngle, VTOLAngleInput, VTOLAngleDivider * Time.smoothDeltaTime);
        float SpeedForVTOL = (Mathf.Min(Speed / VTOLLoseControlSpeed, 1));
        if ((VTOLAngle > 0 && SpeedForVTOL != 1 || VTOLOnly))
        {
            if (VTOLOnly)
            {
                VTOLAngle90 = 1;
                PitchThrustVecMulti = 1;
                YawThrustVecMulti = 1;
                RollThrustVecMulti = 1;
            }
            else
            {
                VTOLAngle90 = Mathf.Min(VTOLAngle / VTOL90Degrees, 1);//used to lerp values as vtol angle goes towards 90 degrees instead of max vtol angle which can be above 90

                float SpeedForVTOL_Inverse_xVTOL = ((SpeedForVTOL * -1) + 1) * VTOLAngle90;
                //the thrust vec values are linearly scaled up the slow you go while in VTOL, from 0 at VTOLLoseControlSpeed
                PitchThrustVecMulti = Mathf.Lerp(PitchThrustVecMultiStart, VTOLPitchThrustVecMulti, SpeedForVTOL_Inverse_xVTOL);
                YawThrustVecMulti = Mathf.Lerp(YawThrustVecMultiStart, VTOLYawThrustVecMulti, SpeedForVTOL_Inverse_xVTOL);
                RollThrustVecMulti = Mathf.Lerp(RollThrustVecMultiStart, VTOLRollThrustVecMulti, SpeedForVTOL_Inverse_xVTOL);

                ReversingPitchStrengthZero = 1;
                ReversingYawStrengthZero = 1;
                ReversingRollStrengthZero = 1;
            }

            if (!VTOLAllowAfterburner)
            {
                if (AfterburnerOn)
                { PlayerThrottle = ThrottleAfterburnerPoint; }
            }
        }
        else
        {
            PitchThrustVecMulti = PitchThrustVecMultiStart;
            YawThrustVecMulti = YawThrustVecMultiStart;
            RollThrustVecMulti = RollThrustVecMultiStart;

            ReversingPitchStrengthZero = ReversingPitchStrengthZeroStart;
            ReversingYawStrengthZero = ReversingYawStrengthZeroStart;
            ReversingRollStrengthZero = ReversingRollStrengthZeroStart;
        }
    }
    public Vector2 UnpackThrottles(float Throttle)
    {
        //x = throttle amount (0-1), y = afterburner amount (0-1)
        return new Vector2(Mathf.Min(Throttle, ThrottleAfterburnerPoint) * ThrottleNormalizer,
        Mathf.Max((Mathf.Max(Throttle, ThrottleAfterburnerPoint) - ThrottleAfterburnerPoint) * ABNormalizer, 0));
    }
}