
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class EngineController : UdonSharpBehaviour
{
    public GameObject VehicleMainObj;
    public EffectsController EffectsControl;
    public SoundController SoundControl;
    public HUDController HUDControl;
    public Transform PlaneMesh;
    public int OnboardPlaneLayer = 19;
    public Transform CenterOfMass;
    public Transform GroundEffectEmpty;
    public Transform PitchMoment;
    public Transform YawMoment;
    public Transform GroundDetector;
    public Transform HookDetector;
    [UdonSynced(UdonSyncMode.None)] public float Health = 23f;
    public LayerMask ResupplyLayer;
    public LayerMask HookCableLayer;
    public Transform CatapultDetector;
    public LayerMask CatapultLayer;
    public GameObject AAM;
    public int NumAAM = 6;
    public float AAMMaxTargetDistance = 6000;
    public float AAMLockAngle = 15;
    public float AAMLockTime = 1.5f;
    public float AAMLaunchDelay = 0.5f;
    public Transform AAMLaunchPoint;
    public LayerMask AAMTargetsLayer;
    public GameObject AGM;
    public int NumAGM = 4;
    public Transform AGMLaunchPoint;
    public LayerMask AGMTargetsLayer;
    public Camera AtGCam;
    public GameObject Bomb;
    public int NumBomb = 4;
    public float BombHoldDelay = 0.5f;
    public float BombDelay = 0f;
    public Transform[] BombLaunchPoints;
    public Transform GunRecoilEmpty;
    [UdonSynced(UdonSyncMode.None)] public float GunAmmoInSeconds = 12;
    public float GunRecoil = 150;
    public Scoreboard_Kills KillsBoard;
    public bool RepeatingWorld = true;
    public float RepeatingWorldDistance = 20000;
    public bool HasAfterburner = true;
    public float ThrottleAfterburnerPoint = 0.8f;
    public bool VTOLOnly = false;
    public bool NoCanopy = false;
    [Header("Dial Functions Usable?")]
    public bool HasVTOLAngle = false;
    public bool HasLimits = true;
    public bool HasFlare = true;
    public bool HasCatapult = true;
    public bool HasBrake = true;
    public bool HasAltHold = true;
    /*     public bool HasTRIM = true; */
    public bool HasCanopy = true;
    public bool HasCruise = true;
    public bool HasGun = true;
    public bool HasAAM = true;
    public bool HasAGM = true;
    public bool HasBomb = true;
    public bool HasGear = true;
    public bool HasFlaps = true;
    public bool HasHook = true;
    public bool HasSmoke = true;
    [Header("Response:")]
    public float ThrottleStrength = 20f;
    public bool VerticalThrottle = false;
    public float ThrottleSensitivity = 6f;
    public float AfterburnerThrustMulti = 1.5f;
    public float AccelerationResponse = 4.5f;
    public float EngineSpoolDownSpeedMulti = .5f;
    public float AirFriction = 0.0004f;
    public float PitchStrength = 5f;
    public float PitchThrustVecMulti = 0f;
    public float PitchFriction = 24f;
    public float PitchConstantFriction = 0f;
    public float PitchResponse = 20f;
    public float ReversingPitchStrengthMulti = 2;
    public float YawStrength = 3f;
    public float YawThrustVecMulti = 0f;
    public float YawFriction = 15f;
    public float YawConstantFriction = 0f;
    public float YawResponse = 20f;
    public float ReversingYawStrengthMulti = 2.4f;
    public float RollStrength = 450f;
    public float RollThrustVecMulti = 0f;
    public float RollFriction = 90f;
    public float RollConstantFriction = 0f;
    public float RollResponse = 20f;
    public float ReversingRollStrengthMulti = 1.6f;//reversing = AoA > 90
    public float PitchDownStrMulti = .8f;
    public float PitchDownLiftMulti = .8f;
    public float InertiaTensorRotationMulti = 1;
    public bool InvertITRYaw = false;
    public float AdverseYaw = 0;
    public float AdverseRoll = 0;
    public float RotMultiMaxSpeed = 220f;
    //public float StickInputPower = 1.7f;
    public float VelStraightenStrPitch = 0.035f;
    public float VelStraightenStrYaw = 0.045f;
    public float MaxAngleOfAttackPitch = 25f;
    public float MaxAngleOfAttackYaw = 40f;
    public float AoaCurveStrength = 2f;//1 = linear, >1 = convex, <1 = concave
    public float HighPitchAoaMinControl = 0.2f;
    public float HighYawAoaMinControl = 0.2f;
    public float HighPitchAoaMinLift = 0.2f;
    public float HighYawAoaMinLift = 0.2f;
    public float TaxiRotationSpeed = 35f;
    public float TaxiRotationResponse = 2.5f;
    public float Lift = 0.00015f;
    public float SidewaysLift = .17f;
    public float MaxLift = 10f;
    public float VelLift = 1f;
    public float VelLiftMax = 10f;
    public float MaxGs = 40f;
    public float GDamage = 10f;
    public float LandingGearDragMulti = 1.3f;
    public float FlapsDragMulti = 1.4f;
    public float FlapsLiftMulti = 1.35f;
    public float AirbrakeStrength = 4f;
    public float GroundBrakeStrength = 6f;
    public float GroundBrakeSpeed = 40f;
    public float HookedBrakeStrength = 55f;
    public float HookedCableSnapDistance = 120f;
    public float GroundEffectMaxDistance = 7;
    public float GroundEffectStrength = 4;
    public float GroundEffectLiftMax = 999999;
    public float CatapultLaunchStrength = 50f;
    public float CatapultLaunchTime = 2f;
    public float GLimiter = 12f;
    public float AoALimiter = 15f;
    [Header("Response VTOL:")]
    public float VTOLAngleTurnRate = 90f;
    public float VTOLDefaultValue = 0;
    public bool VTOLAllowAfterburner = false;
    public float VTOLThrottleStrengthMulti = .7f;
    public float VTOLMinAngle = 0;
    public float VTOLMaxAngle = 90;
    public float VTOLPitchThrustVecMulti = .3f;
    public float VTOLYawThrustVecMulti = .3f;
    public float VTOLRollThrustVecMulti = .07f;
    public float VTOLLoseControlSpeed = 120;
    public float VTOLGroundEffectStrength = 4;
    [Header("Other:")]
    public float CanopyCloseTime = 1.8f;
    public float SeaLevel = -10f;
    public Vector3 Wind;
    public float WindGustStrength = 15;
    public float WindGustiness = 0.03f;
    public float WindTurbulanceScale = 0.0001f;
    public float SoundBarrierStrength = 0.0003f;
    public float SoundBarrierWidth = 20f;
    public float TouchDownSoundSpeed = 35;
    [UdonSynced(UdonSyncMode.None)] public float Fuel = 7200;
    public float FuelConsumption = 2;
    public float FuelConsumptionABMulti = 4.4f;

    [SerializeField] private float Compressing;
    [SerializeField] private float Rebound;
    [SerializeField] private float FloatForce;
    [SerializeField] private Transform[] FloatPoints;
    private float[] SuspensionCompression;
    private float[] SuspensionCompressionLastFrame;
    [SerializeField] private bool SeaPlane;
    [SerializeField] private float SuspMaxDist = .5f;
    [SerializeField] private float WaterSidewaysDrag = .1f;
    [SerializeField] private float WaterForwardDrag = .1f;


    //best to remove synced variables if you aren't using them
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.None)] public float BrakeInput;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public float EngineOutput = 0f;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public Vector3 CurrentVel = Vector3.zero;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public float Gs = 1f;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public float AngleOfAttack;//MAX of yaw & pitch aoa //used by effectscontroller and hudcontroller
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.None)] public int AAMTarget = 0;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public Vector3 SmokeColor = Vector3.one;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.None)] public bool IsFiringGun = false;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.None)] public bool Occupied = false; //this is true if someone is sitting in pilot seat
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.None)] public Vector3 AGMTarget;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public float VTOLAngle;

    private Animator PlaneAnimator;
    [System.NonSerializedAttribute] public int PilotID;
    [System.NonSerializedAttribute] public string PilotName;
    [System.NonSerializedAttribute] public bool FlightLimitsEnabled = true;
    [System.NonSerializedAttribute] public ConstantForce VehicleConstantForce;
    [System.NonSerializedAttribute] public Rigidbody VehicleRigidbody;
    [System.NonSerializedAttribute] public Transform VehicleTransform;
    [System.NonSerializedAttribute] public Color SmokeColor_Color;
    private VRC.SDK3.Components.VRCObjectSync VehicleObjectSync;
    private float LerpedRoll;
    private float LerpedPitch;
    private float LerpedYaw;
    [System.NonSerializedAttribute] public int RStickSelection = 0;
    [System.NonSerializedAttribute] public int LStickSelection = 0;
    [System.NonSerializedAttribute] public bool LGripLastFrame = false;
    [System.NonSerializedAttribute] public bool LTriggerLastFrame = false;
    [System.NonSerializedAttribute] public bool RTriggerLastFrame = false;
    Quaternion JoystickZeroPoint;
    Quaternion PlaneRotLastFrame;
    [System.NonSerializedAttribute] public float PlayerThrottle;
    private float TempThrottle;
    private float ThrottleZeroPoint;
    private float CruiseTemp;
    private float VTOLTemp;
    private float VTOLZeroPoint;
    [System.NonSerializedAttribute] public float SetSpeed;
    private float SpeedZeroPoint;
    private float SmokeHoldTime;
    private bool SetSmokeLastFrame;
    private Vector3 SmokeZeroPoint;
    private float EjectZeroPoint;
    [System.NonSerializedAttribute] public float EjectTimer = 1;
    [System.NonSerializedAttribute] public bool Ejected = false;
    [System.NonSerializedAttribute] public float LTriggerTapTime = 1;
    [System.NonSerializedAttribute] public float RTriggerTapTime = 1;
    /*     private bool DoTrim;
        private Vector3 HandPosTrim;
        private Vector3 TrimZeroPoint;
        private Vector2 TempTrim;
        private Vector2 TrimDifference;
        [System.NonSerializedAttribute] public Vector2 Trim; */
    [System.NonSerializedAttribute] public bool RGripLastFrame = false;
    [System.NonSerializedAttribute] public float ThrottleInput = 0f;
    private float roll = 0f;
    private float pitch = 0f;
    private float yaw = 0f;
    [System.NonSerializedAttribute] public float FullHealth;
    [System.NonSerializedAttribute] public bool Taxiing = false;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public Vector3 RotationInputs;
    [System.NonSerializedAttribute] public bool Piloting = false;
    [System.NonSerializedAttribute] public bool InEditor = true;
    [System.NonSerializedAttribute] public bool InVR = false;
    [System.NonSerializedAttribute] public bool Passenger = false;
    [System.NonSerializedAttribute] public Vector3 LastFrameVel = Vector3.zero;
    [System.NonSerializedAttribute] public VRCPlayerApi localPlayer;
    [System.NonSerializedAttribute] public bool dead = false;
    [System.NonSerializedAttribute] public float AtmoshpereFadeDistance;
    [System.NonSerializedAttribute] public float AtmosphereHeightThing;
    public float AtmosphereThinningStart = 12192f; //40,000 feet
    public float AtmosphereThinningEnd = 19812; //65,000 feet
    public float Atmosphere = 1;
    [System.NonSerializedAttribute] public float rotlift;
    [System.NonSerializedAttribute] public float AngleOfAttackPitch;
    [System.NonSerializedAttribute] public float AngleOfAttackYaw;
    private float AoALiftYaw;
    private float AoALiftPitch;
    private Vector3 Pitching;
    private Vector3 Yawing;
    [System.NonSerializedAttribute] public float Taxiinglerper;
    private float GearDrag;
    private float FlapsGearBrakeDrag;
    private float FlapsDrag;
    private float FlapsLift;
    private float ReversingPitchStrength;
    private float ReversingYawStrength;
    private float ReversingRollStrength;
    private float ReversingPitchStrengthZero;
    private float ReversingYawStrengthZero;
    private float ReversingRollStrengthZero;
    private float ReversingPitchStrengthZeroStart;
    private float ReversingYawStrengthZeroStart;
    private float ReversingRollStrengthZeroStart;
    [System.NonSerializedAttribute] public bool Cruise;
    private float CruiseProportional = .1f;
    private float CruiseIntegral = .1f;
    private float CruiseIntegrator;
    private float CruiseIntegratorMax = 5;
    private float CruiseIntegratorMin = -5;
    private float Cruiselastframeerror;
    private float AltHoldPitchProportional = 1f;
    private float AltHoldPitchIntegral = 1f;
    private float AltHoldPitchIntegrator;
    //private float AltHoldPitchIntegratorMax = .1f;
    //private float AltHoldPitchIntegratorMin = -.1f;
    //private float AltHoldPitchDerivative = 4;
    //private float AltHoldPitchDerivator;
    private float AltHoldPitchlastframeerror;
    private float AltHoldRollProportional = -.005f;
    [System.NonSerializedAttribute] public bool AltHold;
    [System.NonSerializedAttribute] public bool Hooked = false;
    [System.NonSerializedAttribute] public float HookedTime = 0f;
    private Vector3 HookedLoc;
    private Vector3 TempSmokeCol = Vector3.zero;
    [System.NonSerializedAttribute] public float Speed;
    [System.NonSerializedAttribute] public float AirSpeed;
    [System.NonSerializedAttribute] public bool IsOwner = false;
    private Vector3 FinalWind;//includes Gusts
    [System.NonSerializedAttribute] public Vector3 AirVel;
    private float StillWindMulti;//multiplies the speed of the wind by the speed of the plane when taxiing to prevent still planes flying away
    private int ThrustVecGrounded;
    private float SoundBarrier;
    [System.NonSerializedAttribute] private float Afterburner = 1;
    [System.NonSerializedAttribute] public int CatapultStatus = 0;
    private Vector3 CatapultLockPos;
    private Quaternion CatapultLockRot;
    private Transform CatapultTransform;
    private float CatapultLaunchTimeStart;
    [System.NonSerializedAttribute] public float CanopyCloseTimer = -200000;
    [System.NonSerializedAttribute] public GameObject[] AAMTargets = new GameObject[80];
    [System.NonSerializedAttribute] public int NumAAMTargets = 0;
    private int AAMTargetChecker = 0;
    [System.NonSerializedAttribute] public bool AAMHasTarget = false;
    private float AAMTargetedTimer = 2f;
    [System.NonSerializedAttribute] public bool AAMLocked = false;
    [System.NonSerializedAttribute] public float AAMLockTimer = 0;
    private float AAMLastFiredTime;
    [System.NonSerializedAttribute] public Vector3 AAMCurrentTargetDirection;
    [System.NonSerializedAttribute] public float FullFuel;
    [System.NonSerializedAttribute] public bool AGMLocked;
    [System.NonSerializedAttribute] private int AGMUnlocking = 0;
    [System.NonSerializedAttribute] private float AGMUnlockTimer;
    [System.NonSerializedAttribute] public float AGMRotDif;
    [System.NonSerializedAttribute] public int BombPoint = 0;
    private float LastBombDropTime = 0f;
    private Quaternion AGMCamRotSlerper;
    private float LastResupplyTime = 5;//can't resupply for the first 10 seconds after joining, fixes potential null ref if sending something to PlaneAnimator on first frame
    [System.NonSerializedAttribute] public int FullAAMs;
    [System.NonSerializedAttribute] public int FullAGMs;
    [System.NonSerializedAttribute] public int FullBombs;
    [System.NonSerializedAttribute] public float FullGunAmmo;
    [System.NonSerializedAttribute] public int MissilesIncoming = 0;
    [System.NonSerializedAttribute] public EngineController AAMCurrentTargetEngineControl;
    private bool WeaponSelected = false;
    private int CatapultDeadTimer = 0;//needed to be invincible for a frame when entering catapult
    [System.NonSerializedAttribute] public Vector3 Spawnposition;
    [System.NonSerializedAttribute] public Vector3 Spawnrotation;
    private int OutsidePlaneLayer;
    private float AAMTargetObscuredDelay;
    [System.NonSerializedAttribute] public bool DoAAMTargeting;
    private float TargetingAngle;
    private float FullAAMsDivider;
    private float FullAGMsDivider;
    private float FullBombsDivider;
    private Quaternion AGMCamLastFrame;
    bool Landed = false;//moved here from soundcontroller
    private float VelLiftStart;
    private HitDetector PlaneHitDetector;
    [System.NonSerializedAttribute] public float PilotExitTime;
    private int Planelayer;
    Transform[] PlaneMeshParts;
    private float VelLiftMaxStart;
    private bool HasAirBrake;//set to false if air brake strength is 0
    private float HandDistanceZLastFrame;
    private float EngineAngle;
    private float PitchThrustVecMultiStart;
    private float YawThrustVecMultiStart;
    private float RollThrustVecMultiStart;
    private bool VTOLenabled;
    private float VTOLAngleInput;
    private float VTOL90Degrees;
    private float throttleABPointDivider;
    private float VTOLAngleDivider;
    private float InverseThrottleABPointDivider;
    private float EngineOutputLastFrame;
    float VTOLAngle90;
    bool PlaneMoving = false;
    bool HasWheelColliders = false;
    private float vtolangledif;
    private bool GunRecoilEmptyNULL = true;

    private int HOOKED_STRING = Animator.StringToHash("hooked");
    private int FLARES_STRING = Animator.StringToHash("flares");
    private int AAMLAUNCHED_STRING = Animator.StringToHash("aamlaunched");
    private int AAMS_STRING = Animator.StringToHash("AAMs");
    private int AGMLAUNCHED_STRING = Animator.StringToHash("agmlaunched");
    private int AGMS_STRING = Animator.StringToHash("AGMs");
    private int BOMBLAUNCHED_STRING = Animator.StringToHash("bomblaunched");
    private int BOMBS_STRING = Animator.StringToHash("bombs");
    private int RADARLOCKED_STRING = Animator.StringToHash("radarlocked");
    private int ONCATAPULT_STRING = Animator.StringToHash("oncatapult");
    private int WEAPON_STRING = Animator.StringToHash("weapon");
    private int AFTERBURNERON_STRING = Animator.StringToHash("afterburneron");
    private int RESUPPLY_STRING = Animator.StringToHash("resupply");
    private int CANOPYOPEN_STRING = Animator.StringToHash("canopyopen");
    private int GEARUP_STRING = Animator.StringToHash("gearup");
    private int FLAPS_STRING = Animator.StringToHash("flaps");
    private int HOOKDOWN_STRING = Animator.StringToHash("hookdown");
    private int DISPLAYSMOKE_STRING = Animator.StringToHash("displaysmoke");
    private int BULLETHIT_STRING = Animator.StringToHash("bullethit");
    private int INSTANTGEARDOWN_STRING = Animator.StringToHash("instantgeardown");
    private int LOCALPILOT_STRING = Animator.StringToHash("localpilot");
    private int LOCALPASSENGER_STRING = Animator.StringToHash("localpassenger");
    private int OCCUPIED_STRING = Animator.StringToHash("occupied");
    private int RESPAWN_STRING = Animator.StringToHash("respawn");

    //float MouseX;
    //float MouseY;
    //float mouseysens = 1; //mouse input can't be used because it's used to look around even when in a seat
    //float mousexsens = 1;
    private void Start()
    {
        Assert(VehicleMainObj != null, "Start: VehicleMainObj != null");
        Assert(EffectsControl != null, "Start: EffectsControl != null");
        Assert(SoundControl != null, "Start: SoundControl != null");
        Assert(HUDControl != null, "Start: HUDControl != null");
        Assert(PlaneMesh != null, "Start: PlaneMesh != null");
        Assert(CenterOfMass != null, "Start: CenterOfMass != null");
        Assert(PitchMoment != null, "Start: PitchMoment != null");
        Assert(YawMoment != null, "Start: YawMoment != null");
        Assert(GroundDetector != null, "Start: GroundDetector != null");
        Assert(HookDetector != null, "Start: HookDetector != null");
        Assert(AtGCam != null, "Start: AGMCam != null");
        Assert(CatapultDetector != null, "Start: CatapultDetector != null");
        Assert(AAM != null, "Start: AAM != null");
        Assert(AAMLaunchPoint != null, "Start: AAMLaunchPoint != null");
        Assert(AGM != null, "Start: AGM != null");
        Assert(AGMLaunchPoint != null, "Start: AGMLaunchPoint != null");
        Assert(Bomb != null, "Start: Bomb != null");
        Assert(BombLaunchPoints.Length > 0, "Start: BombLaunchPoint.Length > 0");
        Assert(GroundEffectEmpty != null, "Start: GroundEffectEmpty != null");
        Assert(GunRecoilEmpty != null, "Start: GunRecoilEmpty != null");
        Assert(KillsBoard != null, "Start: KillsBoard != null");

        Planelayer = PlaneMesh.gameObject.layer;//get the layer of the plane as set by the world creator
        OutsidePlaneLayer = PlaneMesh.gameObject.layer;
        PlaneAnimator = VehicleMainObj.GetComponent<Animator>();
        //set these values at start in case they haven't been set correctly in editor
        if (!HasCanopy)
        {
            if (NoCanopy)
            {
                EffectsControl.CanopyOpen = false; CanopyOpening();
            }
            else
            {
                EffectsControl.CanopyOpen = true; CanopyClosing();
            }
        }
        else { EffectsControl.CanopyOpen = false; CanopyOpening(); }//always spawn with canopy open if has one
        SetGearDown();
        if (!HasFlaps) { SetFlapsOff(); }
        else { SetFlapsOn(); }
        SetHookUp();


        FullHealth = Health;
        FullFuel = Fuel;
        FullGunAmmo = GunAmmoInSeconds;
        FullAAMs = NumAAM;
        FullAGMs = NumAGM;
        FullBombs = NumBomb;

        VelLiftMaxStart = VelLiftMax;
        VelLiftStart = VelLift;
        CatapultLaunchTimeStart = CatapultLaunchTime;
        HasAirBrake = AirbrakeStrength != 0;

        PitchThrustVecMultiStart = PitchThrustVecMulti;
        YawThrustVecMultiStart = YawThrustVecMulti;
        RollThrustVecMultiStart = RollThrustVecMulti;

        PlaneMeshParts = PlaneMesh.GetComponentsInChildren<Transform>(true);
        PlaneHitDetector = VehicleMainObj.GetComponent<HitDetector>();
        VehicleTransform = VehicleMainObj.GetComponent<Transform>();
        VehicleRigidbody = VehicleMainObj.GetComponent<Rigidbody>();
        VehicleConstantForce = VehicleMainObj.GetComponent<ConstantForce>();
        localPlayer = Networking.LocalPlayer;
        if (localPlayer == null) { InEditor = true; Piloting = true; }
        else
        {
            InEditor = false;
            InVR = localPlayer.IsUserInVR();
        }

        if (!HasLimits) { FlightLimitsEnabled = false; }

        FindAAMTargets();

        //these two are only used in editor
        Spawnposition = VehicleTransform.position;
        Spawnrotation = VehicleTransform.rotation.eulerAngles;

        VehicleRigidbody.centerOfMass = VehicleTransform.InverseTransformDirection(CenterOfMass.position - VehicleTransform.position);//correct position if scaled
        VehicleRigidbody.inertiaTensorRotation = Quaternion.SlerpUnclamped(Quaternion.identity, VehicleRigidbody.inertiaTensorRotation, InertiaTensorRotationMulti);
        if (InvertITRYaw)
        {
            Vector3 ITR = VehicleRigidbody.inertiaTensorRotation.eulerAngles;
            ITR.x *= -1;
            VehicleRigidbody.inertiaTensorRotation = Quaternion.Euler(ITR);
        }
        if (BombHoldDelay < BombDelay) { BombHoldDelay = BombDelay; }

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

        FullAAMsDivider = 1f / (NumAAM > 0 ? NumAAM : 10000000);
        FullAGMsDivider = 1f / (NumAGM > 0 ? NumAGM : 10000000);
        FullBombsDivider = 1f / (NumBomb > 0 ? NumBomb : 10000000);

        if (VTOLOnly || HasVTOLAngle) { VTOLenabled = true; }
        VTOL90Degrees = Mathf.Min(90 / VTOLMaxAngle, 1);

        throttleABPointDivider = 1 / ThrottleAfterburnerPoint;
        vtolangledif = VTOLMaxAngle - VTOLMinAngle;
        VTOLAngleDivider = VTOLAngleTurnRate / vtolangledif;
        InverseThrottleABPointDivider = 1 / (1 - ThrottleAfterburnerPoint);

        VTOLAngle = VTOLAngleInput = VTOLDefaultValue;

        if (NoCanopy) { HasCanopy = false; }

        WheelCollider[] wc = PlaneMesh.GetComponentsInChildren<WheelCollider>(true);
        if (wc.Length != 0) HasWheelColliders = true;
        if (GroundEffectEmpty == null)
        {
            Debug.LogWarning("GroundEffectEmpty not found, using CenterOfMass instead");
            GroundEffectEmpty = CenterOfMass;
        }

        VehicleObjectSync = (VRC.SDK3.Components.VRCObjectSync)VehicleMainObj.GetComponent(typeof(VRC.SDK3.Components.VRCObjectSync));

        if (GunRecoilEmpty != null)
        {
            GunRecoilEmptyNULL = false;
        }

        SuspensionCompression = new float[FloatPoints.Length];
        SuspensionCompressionLastFrame = new float[FloatPoints.Length];
    }

    private void LateUpdate()
    {
        float DeltaTime = Time.deltaTime;
        if (!InEditor) { IsOwner = localPlayer.IsOwner(VehicleMainObj); }
        else { IsOwner = true; }
        if (!EffectsControl.GearUp && Physics.Raycast(GroundDetector.position, -GroundDetector.up, .44f, 2049 /* Default and Environment */, QueryTriggerInteraction.Ignore))
        { Taxiing = true; }
        else { Taxiing = false; }

        if (IsOwner)//works in editor or ingame
        {
            if (!dead)
            {
                if (CenterOfMass.position.y < SeaLevel)//kill plane if in sea
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Explode");
                }
                //G/crash Damage
                Health -= Mathf.Max((Gs - MaxGs) * DeltaTime * GDamage, 0f);//take damage of GDamage per second per G above MaxGs
                if (Health <= 0f)//plane is ded
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Explode");
                }
            }

            //synced variables because rigidbody values aren't accessable by non-owner players
            CurrentVel = VehicleRigidbody.velocity;
            Speed = CurrentVel.magnitude;
            if (Speed > .1f)//don't bother doing all this for planes that arent moving and it therefore wont even effect
            {
                PlaneMoving = true;//check this bool later for more optimizations
                WindAndAoA();//Planemoving is set true or false here
            }

            if (Piloting)
            {
                //gotta do these this if we're piloting but they didn't get done(specifically, hovering extremely slowly in a VTOL craft will cause control issues we don't)
                if (!PlaneMoving)
                { WindAndAoA(); }
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

                Occupied = true;
                //collect inputs
                int Wf = Input.GetKey(KeyCode.W) ? 1 : 0; //inputs as floats
                int Sf = Input.GetKey(KeyCode.S) ? -1 : 0;
                int Af = Input.GetKey(KeyCode.A) ? -1 : 0;
                int Df = Input.GetKey(KeyCode.D) ? 1 : 0;
                int Qf = Input.GetKey(KeyCode.Q) ? -1 : 0;
                int Ef = Input.GetKey(KeyCode.E) ? 1 : 0;
                int upf = Input.GetKey(KeyCode.UpArrow) ? 1 : 0;
                int downf = Input.GetKey(KeyCode.DownArrow) ? -1 : 0;
                int leftf = Input.GetKey(KeyCode.LeftArrow) ? -1 : 0;
                int rightf = Input.GetKey(KeyCode.RightArrow) ? 1 : 0;
                bool Shift = Input.GetKey(KeyCode.LeftShift);
                bool Ctrl = Input.GetKey(KeyCode.LeftControl);
                int Shiftf = Shift ? 1 : 0;
                int LeftControlf = Ctrl ? 1 : 0;
                Vector2 LStick = new Vector2(0, 0);
                Vector2 RStick = new Vector2(0, 0);
                float LGrip = 0;
                float RGrip = 0;
                float LTrigger = 0;
                float RTrigger = 0;
                if (!InEditor)
                {
                    LStick.x = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
                    LStick.y = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
                    RStick.x = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
                    RStick.y = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
                    LGrip = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger");
                    RGrip = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
                    LTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
                    RTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
                }
                //MouseX = Input.GetAxisRaw("Mouse X");
                //MouseY = Input.GetAxisRaw("Mouse Y");
                Vector3 JoystickPosYaw;
                Vector3 JoystickPos;
                Vector2 VRPitchRoll;


                //close canopy when moving fast, can't fly with it open
                if (Speed > 20 && EffectsControl.CanopyOpen && HasCanopy)
                {
                    if (CanopyCloseTimer < -100000)
                    {
                        SetCanopyClosed();
                    }
                }

                ///////////////////KEYBOARD CONTROLS////////////////////////////////////////////////////////          
                if (EffectsControl.Smoking)
                {
                    int keypad7 = Input.GetKey(KeyCode.Keypad7) ? 1 : 0;
                    int Keypad4 = Input.GetKey(KeyCode.Keypad4) ? 1 : 0;
                    int Keypad8 = Input.GetKey(KeyCode.Keypad8) ? 1 : 0;
                    int Keypad5 = Input.GetKey(KeyCode.Keypad5) ? 1 : 0;
                    int Keypad9 = Input.GetKey(KeyCode.Keypad9) ? 1 : 0;
                    int Keypad6 = Input.GetKey(KeyCode.Keypad6) ? 1 : 0;
                    SmokeColor.x = Mathf.Clamp(SmokeColor.x + ((keypad7 - Keypad4) * DeltaTime), 0, 1);
                    SmokeColor.y = Mathf.Clamp(SmokeColor.y + ((Keypad8 - Keypad5) * DeltaTime), 0, 1);
                    SmokeColor.z = Mathf.Clamp(SmokeColor.z + ((Keypad9 - Keypad6) * DeltaTime), 0, 1);
                }
                if (Input.GetKeyDown(KeyCode.F2) && HasCruise)
                {
                    SetSpeed = AirSpeed;
                    Cruise = !Cruise;
                }
                if (Input.GetKeyDown(KeyCode.F1) && HasLimits)
                {
                    ToggleLimits();
                }
                if (Input.GetKeyDown(KeyCode.C) && HasCatapult)
                {
                    if (CatapultStatus == 1)
                    {
                        CatapultStatus = 2;
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CatapultLaunchEffects");
                    }
                }
                if (Input.GetKeyDown(KeyCode.H) && HasHook)
                {
                    if (HookDetector != null)
                    {
                        ToggleHook();
                    }
                    Hooked = false;
                }
                if (Input.GetKeyDown(KeyCode.F3) && HasAltHold)
                {
                    AltHold = !AltHold;
                }
                if (Input.GetKeyDown(KeyCode.Z) && Speed < 20 && HasCanopy)
                {
                    ToggleCanopy();
                }

                //with keys 1-4 we select weapons, if they are already selected, deselect them.
                if (Input.GetKeyDown(KeyCode.Alpha1) && HasGun)
                {
                    if (RStickSelection == 1)
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RStick0");
                        RStickSelection = 0;
                    }
                    else
                    {
                        if (HUDControl != null) { HUDControl.GUN_TargetSpeedLerper = 0; }//reset targeting lerper
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RStick1");
                        RStickSelection = 1;
                    }
                }

                if (Input.GetKeyDown(KeyCode.Alpha2) && HasAAM)
                {
                    if (RStickSelection == 2)
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RStick0");
                        RStickSelection = 0;
                    }
                    else
                    {

                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RStick2");
                        RStickSelection = 2;
                    }
                }

                if (Input.GetKeyDown(KeyCode.Alpha3) && HasAGM)
                {
                    if (RStickSelection == 3)
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RStick0");
                        RStickSelection = 0;
                    }
                    else
                    {
                        AGMUnlocking = 0;
                        AGMUnlockTimer = 0;
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RStick3");
                        RStickSelection = 3;
                    }
                }

                if (Input.GetKeyDown(KeyCode.Alpha4) && HasBomb)
                {
                    if (RStickSelection == 4)
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RStick0");
                        RStickSelection = 0;
                    }
                    else
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RStick4");
                        RStickSelection = 4;
                    }
                }

                if (Input.GetKeyDown(KeyCode.G) && HasGear && CatapultStatus == 0)
                {
                    ToggleGear();
                }
                if (Input.GetKeyDown(KeyCode.F) && HasFlaps)
                {
                    ToggleFlaps();
                }
                if (Input.GetKeyDown(KeyCode.Alpha5) && HasSmoke)
                {
                    ToggleSmoking();
                }
                if (Input.GetKeyDown(KeyCode.X) && HasFlare)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchFlares");
                }
                //////////////////END OF KEYBOARD CONTROLS////////////////////////////////////////////////////////
                //brake, throttle, and afterburner are done later because they have to be to work
                if (VTOLenabled)
                {
                    if (HasVTOLAngle)
                    {
                        float pgup = Input.GetKey(KeyCode.PageUp) ? 1 : 0;
                        float pgdn = Input.GetKey(KeyCode.PageDown) ? 1 : 0;
                        VTOLAngleInput = Mathf.Clamp(VTOLAngleInput + ((pgdn - pgup) * (VTOLAngleDivider * Time.smoothDeltaTime)), 0, 1);
                    }
                    if (!(VTOLAngle == VTOLAngleInput && VTOLAngleInput == 0) || VTOLOnly)//only SetVTOLValues if it'll do anything
                    { SetVTOLValues(); }
                }
                //LStick Selection wheel
                if (InVR && LStick.magnitude > .7f)
                {
                    float stickdir = Vector2.SignedAngle(new Vector2(-0.382683432365f, 0.923879532511f), LStick);

                    if (stickdir > 135)//down
                    {
                        if (HasBrake)
                            LStickSelection = 5;
                    }
                    else if (stickdir > 90)//downleft
                    {
                        if (HasAltHold)
                            LStickSelection = 6;
                    }
                    else if (stickdir > 45)//left
                    {
                        if (HasCanopy)
                            LStickSelection = 7;
                    }
                    else if (stickdir > 0)//upleft
                    {
                        if (HasCruise)
                            LStickSelection = 8;
                    }
                    else if (stickdir > -45)//up
                    {
                        if (HasVTOLAngle)
                            LStickSelection = 1;
                    }
                    else if (stickdir > -90)//upright
                    {
                        if (HasLimits)
                            LStickSelection = 2;
                    }
                    else if (stickdir > -135)//right
                    {
                        if (HasFlare)
                            LStickSelection = 3;
                    }
                    else//downright
                    {
                        if (HasCatapult)
                            LStickSelection = 4;
                    }
                }

                //RStick Selection wheel
                if (InVR && RStick.magnitude > .7f)
                {
                    float stickdir = Vector2.SignedAngle(new Vector2(-0.382683432365f, 0.923879532511f), RStick);//that number is 22.5 degrees to the left of straight up
                    //R stick value is manually synced using events because i don't want to use too many synced variables.
                    //the value can be used in the animator to open bomb bay doors when bombs are selected, etc.
                    //The WeaponSelected variable helps us not send more broadcasts than we need to.
                    if (stickdir > 135)//down
                    {
                        if (HasGear)
                        {
                            if (WeaponSelected)
                            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RStick0"); }
                            RStickSelection = 5;
                        }
                    }
                    else if (stickdir > 90)//downleft
                    {
                        if (HasFlaps)
                        {
                            if (WeaponSelected)
                            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RStick0"); }
                            RStickSelection = 6;
                        }
                    }
                    else if (stickdir > 45)//left
                    {
                        if (HasHook)
                        {
                            if (WeaponSelected)
                            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RStick0"); }
                            RStickSelection = 7;
                        }
                    }
                    else if (stickdir > 0)//upleft
                    {
                        if (HasSmoke)
                        {
                            if (WeaponSelected)
                            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RStick0"); }
                            RStickSelection = 8;
                        }
                    }
                    else if (stickdir > -45)//up
                    {
                        if (HasGun && RStickSelection != 1)
                        {
                            if (HUDControl != null) { HUDControl.GUN_TargetSpeedLerper = 0; }//reset targeting lerper
                            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RStick1");
                            RStickSelection = 1;
                        }
                    }
                    else if (stickdir > -90)//upright
                    {
                        if (HasAAM && RStickSelection != 2)
                        {
                            AAMTargetedTimer = 2;
                            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RStick2");
                            RStickSelection = 2;
                        }
                    }
                    else if (stickdir > -135)//right
                    {
                        if (HasAGM && RStickSelection != 3)
                        {
                            AGMUnlocking = 0;
                            AGMUnlockTimer = 0;

                            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RStick3");
                            RStickSelection = 3;
                        }
                    }
                    else//downright
                    {
                        if (HasBomb && RStickSelection != 4)
                        {
                            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RStick4");
                            RStickSelection = 4;
                        }
                    }
                }


                LTriggerTapTime += DeltaTime;
                switch (LStickSelection)
                {
                    case 0://player just got in and hasn't selected anything
                        BrakeInput = 0;
                        break;
                    case 1://VTOL ANGLE
                        if (LTrigger > 0.75)
                        {
                            Vector3 handpos = VehicleTransform.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                            handpos = VehicleTransform.InverseTransformDirection(handpos);

                            if (!LTriggerLastFrame)
                            {
                                VTOLZeroPoint = handpos.z;
                                VTOLTemp = VTOLAngle;
                            }
                            float VTOLAngleDifference = (VTOLZeroPoint - handpos.z) * -ThrottleSensitivity;
                            VTOLAngleInput = Mathf.Clamp(VTOLTemp + VTOLAngleDifference, 0, 1);

                            LTriggerLastFrame = true;
                        }
                        else { LTriggerLastFrame = false; }
                        BrakeInput = 0;
                        break;
                    case 2://LIMIT
                        if (LTrigger > 0.75)
                        {
                            if (!LTriggerLastFrame)
                            {
                                ToggleLimits();
                            }

                            LTriggerLastFrame = true;
                        }
                        else { LTriggerLastFrame = false; }
                        BrakeInput = 0;
                        break;
                    case 3://Flare
                        if (LTrigger > 0.75)
                        {
                            if (!LTriggerLastFrame)
                            {
                                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchFlares");
                            }

                            AltHold = false;
                            IsFiringGun = false;
                            RTriggerLastFrame = true;
                        }
                        else { LTriggerLastFrame = false; }

                        BrakeInput = 0;
                        break;
                    case 4://Catapult
                        if (LTrigger > 0.75)
                        {
                            if (!LTriggerLastFrame)
                            {
                                if (CatapultStatus == 1)
                                {
                                    CatapultStatus = 2;
                                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CatapultLaunchEffects");
                                }
                            }

                            LTriggerLastFrame = true;
                        }
                        else { LTriggerLastFrame = false; }

                        BrakeInput = 0;
                        break;
                    case 5://Brake
                        if (!Taxiing)
                        {
                            if (HasAirBrake) { BrakeInput = LTrigger; }
                            else { BrakeInput = 0; }
                        }
                        else { BrakeInput = LTrigger; }

                        if (LTrigger > 0.75) { LTriggerLastFrame = true; }
                        else { LTriggerLastFrame = false; }
                        break;
                    case 6://Alt. Hold
                        if (LTrigger > 0.75)
                        {
                            if (!LTriggerLastFrame) AltHold = !AltHold;
                            LTriggerLastFrame = true;
                        }
                        else { LTriggerLastFrame = false; }
                        //this used to be TRIM
                        /*                             if (LTrigger > 0.75 || (Input.GetKey(KeyCode.Alpha4)))
                           {
                               if (!LTriggerLastFrame)
                               {
                                   if (InVR)
                                   {
                                       HandPosTrim = VehicleTransform.InverseTransformPoint(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position);
                                       TrimZeroPoint = HandPosTrim;
                                       TempTrim = new Vector2(Trim.y, Trim.x);//it's inverted because i want X to be pitch and y to be yaw
                                   }
                                   if (LTriggerTapTime > .4f)//no double tap
                                   {
                                       LTriggerTapTime = 0;
                                       DoTrim = true;
                                   }
                                   else//double tap detected, reset trim
                                   {
                                       DoTrim = false;
                                       Trim = new Vector2(0, 0);
                                   }
                               }
                               if (InVR && DoTrim)
                               {
                                   //VR Set Trim
                                   HandPosTrim = VehicleTransform.InverseTransformPoint(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position);
                                   TrimDifference = (TrimZeroPoint - HandPosTrim) * 2f;
                                   Trim.x = Mathf.Clamp(TempTrim.y + TrimDifference.y, -1, 1);
                                   Trim.y = Mathf.Clamp(TempTrim.x + -TrimDifference.x, -1, 1);
                               }
                               LTriggerLastFrame = true;
                           }
                           else { LTriggerLastFrame = false; } */
                        BrakeInput = 0;
                        break;
                    case 7://Canopy
                        if (LTrigger > 0.75)
                        {
                            if (!LTriggerLastFrame && Speed < 20)
                            {
                                ToggleCanopy();
                            }

                            //ejection
                            if (InVR)
                            {
                                Vector3 handposL = VehicleTransform.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                                handposL = VehicleTransform.InverseTransformDirection(handposL);
                                Vector3 handposR = VehicleTransform.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                                handposR = VehicleTransform.InverseTransformDirection(handposR);

                                if (!LTriggerLastFrame && (handposL.y - handposR.y) < 0.20f)
                                {
                                    EjectZeroPoint = handposL.y;
                                    EjectTimer = 0;
                                }
                                if (EjectZeroPoint - handposL.y > .5f && EjectTimer < 1)
                                {
                                    Ejected = true;
                                    HUDControl.ExitStation();
                                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CanopyOpening");
                                }
                            }

                            EjectTimer += DeltaTime;
                            LTriggerLastFrame = true;
                        }
                        else
                        {
                            LTriggerLastFrame = false;
                            EjectTimer = 2;
                        }
                        BrakeInput = 0;
                        break;
                    case 8://Cruise
                        if (LTrigger > 0.75)
                        {
                            //for setting speed in VR
                            Vector3 handpos = VehicleTransform.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                            handpos = VehicleTransform.InverseTransformDirection(handpos);

                            //enable and disable
                            if (!LTriggerLastFrame)
                            {
                                if (!Cruise)
                                {
                                    SetSpeed = AirSpeed;
                                    Cruise = true;
                                }
                                if (LTriggerTapTime > .4f)//no double tap
                                {
                                    LTriggerTapTime = 0;
                                }
                                else//double tap detected, turn off cruise
                                {
                                    Cruise = false;
                                    PlayerThrottle = ThrottleInput;
                                }
                                //end of enable disable

                                //more set speed stuff
                                SpeedZeroPoint = handpos.z;
                                CruiseTemp = SetSpeed;
                            }
                            float SpeedDifference = (SpeedZeroPoint - handpos.z) * 250;
                            SetSpeed = Mathf.Floor(Mathf.Clamp(CruiseTemp + SpeedDifference, 0, 2000));

                            LTriggerLastFrame = true;
                        }
                        else { LTriggerLastFrame = false; }
                        BrakeInput = 0;
                        break;
                }


                RTriggerTapTime += DeltaTime;
                switch (RStickSelection)
                {
                    case 0://player just got in and hasn't selected anything
                        AAMHasTarget = false;
                        AAMLocked = false;
                        AAMLockTimer = 0;
                        IsFiringGun = false;
                        DoAAMTargeting = false;
                        break;
                    case 1://GUN
                        if ((RTrigger > 0.75 || (Input.GetKey(KeyCode.Space))) && GunAmmoInSeconds > 0)
                        {
                            IsFiringGun = true;
                            GunAmmoInSeconds = Mathf.Max(GunAmmoInSeconds - DeltaTime, 0);
                            if (GunRecoilEmptyNULL)
                            {
                                VehicleRigidbody.AddRelativeForce(-Vector3.forward * GunRecoil * Time.smoothDeltaTime);
                            }
                            else
                            {
                                VehicleRigidbody.AddForceAtPosition(-GunRecoilEmpty.forward * GunRecoil * .01f/* so the strength is in the same range as above*/, GunRecoilEmpty.position, ForceMode.Force);
                            }
                            RTriggerLastFrame = true;
                        }
                        else { IsFiringGun = false; RTriggerLastFrame = false; }

                        TargetingAngle = 70;
                        DoAAMTargeting = true;//gun lead indiactor uses this
                        AAMLocked = false;
                        AAMLockTimer = 0;
                        break;
                    case 2://AAM
                        if (NumAAMTargets != 0)
                        {
                            DoAAMTargeting = true;
                            TargetingAngle = AAMLockAngle;

                            if (AAMLockTimer > AAMLockTime && AAMHasTarget) AAMLocked = true;
                            else { AAMLocked = false; }

                            //firing AAM
                            if (RTrigger > 0.75 || (Input.GetKey(KeyCode.Space)))
                            {
                                if (!RTriggerLastFrame)
                                {
                                    if (AAMLocked && !Taxiing && Time.time - AAMLastFiredTime > AAMLaunchDelay)
                                    {
                                        AAMLastFiredTime = Time.time;
                                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchAAM");
                                        if (NumAAM == 0) { AAMLockTimer = 0; AAMLocked = false; }
                                    }
                                }
                                RTriggerLastFrame = true;
                            }
                            else RTriggerLastFrame = false;
                        }
                        else { AAMLocked = false; AAMHasTarget = false; }
                        IsFiringGun = false;
                        break;
                    case 3://AGM
                        AGMUnlockTimer += DeltaTime * AGMUnlocking;//AGMUnlocking is 1 if it was locked and just pressed, else 0, (waits for double tap delay to disable)
                        if (AGMUnlockTimer > 0.4f && AGMLocked == true)
                        {
                            AGMLocked = false;
                            AGMUnlockTimer = 0;
                            AGMUnlocking = 0;
                            if (!SoundControl.AGMUnlockNull)
                                SoundControl.AGMUnlock.Play();
                        }
                        if (RTrigger > 0.75 || (Input.GetKey(KeyCode.Space)))
                        {
                            if (!RTriggerLastFrame)
                                if (RTriggerTapTime < 0.4f)
                                {
                                    if (AGMLocked)
                                    {
                                        //double tap detected
                                        if (NumAGM > 0 && !Taxiing)
                                        {
                                            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchAGM");
                                        }
                                        AGMUnlocking = 0;
                                    }
                                }
                                else if (!AGMLocked)
                                {
                                    if (AtGCam != null)
                                    {
                                        float targetangle = 999;
                                        RaycastHit lockpoint;
                                        RaycastHit[] agmtargs = Physics.SphereCastAll(AtGCam.transform.position, 150, AtGCam.transform.forward, Mathf.Infinity, AGMTargetsLayer);
                                        if (agmtargs.Length > 0)
                                        {
                                            //find target with lowest angle from crosshair
                                            foreach (RaycastHit target in agmtargs)
                                            {
                                                Vector3 targetdirection = target.point - AtGCam.transform.position;
                                                float angle = Vector3.Angle(AtGCam.transform.forward, targetdirection);
                                                if (angle < targetangle)
                                                {
                                                    targetangle = angle;
                                                    AGMTarget = target.collider.transform.position;
                                                    AGMLocked = true;
                                                    AGMUnlocking = 0;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Physics.Raycast(AtGCam.transform.position, AtGCam.transform.forward, out lockpoint, Mathf.Infinity, 133121 /* Default, Environment, and Walkthrough */, QueryTriggerInteraction.Ignore);
                                            if (lockpoint.point != null)
                                            {
                                                if (!SoundControl.AGMUnlockNull)
                                                { SoundControl.AGMLock.Play(); }
                                                AGMTarget = lockpoint.point;
                                                AGMLocked = true;
                                                AGMUnlocking = 0;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    RTriggerTapTime = 0;
                                    AGMUnlockTimer = 0;
                                    AGMUnlocking = 1;
                                }
                            RTriggerLastFrame = true;
                        }
                        else { RTriggerLastFrame = false; }
                        //AGM Camera, more in hudcontroller
                        if (!AGMLocked)
                        {
                            Quaternion newangle;
                            if (InVR)
                            {
                                newangle = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation * Quaternion.Euler(0, 60, 0);
                            }
                            else if (!InEditor)//desktop mode
                            {
                                newangle = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
                            }
                            else//editor
                            {
                                newangle = VehicleTransform.rotation;
                            }
                            float ZoomLevel = AtGCam.fieldOfView / 90;
                            AGMCamRotSlerper = Quaternion.Slerp(AGMCamRotSlerper, newangle, ZoomLevel * 220f * DeltaTime);

                            if (AtGCam != null)
                            {
                                AGMRotDif = Vector3.Angle(AtGCam.transform.rotation * Vector3.forward, AGMCamLastFrame * Vector3.forward);
                                // AGMRotDif = Vector3.Angle(AtGCam.transform.rotation * Vector3.forward, AGMCamRotSlerper * Vector3.forward);
                                AtGCam.transform.rotation = AGMCamRotSlerper;

                                Vector3 temp2 = AtGCam.transform.localRotation.eulerAngles;
                                temp2.z = 0;
                                AtGCam.transform.localRotation = Quaternion.Euler(temp2);
                            }
                            AGMCamLastFrame = newangle;
                        }


                        AAMHasTarget = false;
                        AAMLocked = false;
                        AAMLockTimer = 0;
                        IsFiringGun = false;
                        DoAAMTargeting = false;
                        break;
                    case 4://Bomb
                        if (RTrigger > 0.75 || (Input.GetKey(KeyCode.Space)))
                        {
                            if (!RTriggerLastFrame)
                            {
                                if (NumBomb > 0 && !Taxiing && ((Time.time - LastBombDropTime) > BombDelay))
                                {
                                    LastBombDropTime = Time.time;
                                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchBomb");
                                }
                            }
                            else//launch every BombHoldDelay
                                if (NumBomb > 0 && ((Time.time - LastBombDropTime) > BombHoldDelay) && !Taxiing)
                            {
                                {
                                    LastBombDropTime = Time.time;
                                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchBomb");
                                }
                            }

                            RTriggerLastFrame = true;
                        }
                        else { RTriggerLastFrame = false; }

                        AAMHasTarget = false;
                        AAMLocked = false;
                        AAMLockTimer = 0;
                        IsFiringGun = false;
                        DoAAMTargeting = false;
                        break;
                    case 5://GEAR
                        if (RTrigger > 0.75)
                        {
                            if (!RTriggerLastFrame && CatapultStatus == 0) { ToggleGear(); }
                            RTriggerLastFrame = true;
                        }
                        else { RTriggerLastFrame = false; }

                        AAMHasTarget = false;
                        AAMLocked = false;
                        AAMLockTimer = 0;
                        IsFiringGun = false;
                        DoAAMTargeting = false;
                        break;
                    case 6://flaps
                        if (RTrigger > 0.75)
                        {
                            if (!RTriggerLastFrame) ToggleFlaps();
                            RTriggerLastFrame = true;
                        }
                        else { RTriggerLastFrame = false; }

                        AAMHasTarget = false;
                        AAMLocked = false;
                        AAMLockTimer = 0;
                        IsFiringGun = false;
                        DoAAMTargeting = false;
                        break;
                    case 7://Hook
                        if (RTrigger > 0.75)
                        {
                            if (!RTriggerLastFrame)
                            {
                                if (HookDetector != null)
                                {
                                    ToggleHook();
                                }
                                Hooked = false;
                            }

                            RTriggerLastFrame = true;
                        }
                        else { RTriggerLastFrame = false; }

                        AAMHasTarget = false;
                        AAMLocked = false;
                        AAMLockTimer = 0;
                        IsFiringGun = false;
                        DoAAMTargeting = false;
                        break;
                    case 8://Smoke
                        if (RTrigger > 0.75)
                        {
                            //you can change smoke colour by holding down the trigger and waving your hand around. x/y/z = r/g/b
                            Vector3 HandPosSmoke = VehicleTransform.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                            HandPosSmoke = VehicleTransform.InverseTransformDirection(HandPosSmoke);
                            if (!RTriggerLastFrame)
                            {
                                SmokeZeroPoint = HandPosSmoke;
                                TempSmokeCol = SmokeColor;

                                ToggleSmoking();
                                SmokeHoldTime = 0;
                            }
                            SmokeHoldTime += DeltaTime;
                            if (SmokeHoldTime > .4f)
                            {
                                //VR Set Smoke

                                Vector3 SmokeDifference = (SmokeZeroPoint - HandPosSmoke) * -ThrottleSensitivity;
                                SmokeColor.x = Mathf.Clamp(TempSmokeCol.x + SmokeDifference.x, 0, 1);
                                SmokeColor.y = Mathf.Clamp(TempSmokeCol.y + SmokeDifference.y, 0, 1);
                                SmokeColor.z = Mathf.Clamp(TempSmokeCol.z + SmokeDifference.z, 0, 1);
                            }
                            RTriggerLastFrame = true;
                        }
                        else { RTriggerLastFrame = false; }

                        AAMHasTarget = false;
                        AAMLocked = false;
                        AAMLockTimer = 0;
                        IsFiringGun = false;
                        DoAAMTargeting = false;
                        break;
                }
                //keyboard control for brake
                if (Input.GetKey(KeyCode.B) && HasBrake)
                {
                    if (!Taxiing)
                    {
                        if (HasAirBrake) { BrakeInput = 1; }
                        else { BrakeInput = 0; }
                    }
                    else { BrakeInput = 1; }
                }
                //VR Joystick
                if (RGrip > 0.75)
                {
                    Quaternion PlaneRotDif = VehicleTransform.rotation * Quaternion.Inverse(PlaneRotLastFrame);//difference in plane's rotation since last frame
                    JoystickZeroPoint = PlaneRotDif * JoystickZeroPoint;//zero point rotates with the plane so it appears still to the pilot
                    if (!RGripLastFrame)//first frame you gripped joystick
                    {
                        PlaneRotDif = Quaternion.identity;
                        JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;//rotation of the controller relative to the plane when it was pressed
                    }
                    //difference between the plane and the hand's rotation, and then the difference between that and the JoystickZeroPoint
                    Quaternion JoystickDifference = (Quaternion.Inverse(VehicleTransform.rotation) * localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation) * Quaternion.Inverse(JoystickZeroPoint);
                    JoystickPosYaw = (JoystickDifference * VehicleTransform.forward);//angles to vector
                    JoystickPosYaw.y = 0;
                    JoystickPos = (JoystickDifference * VehicleTransform.up);
                    VRPitchRoll = new Vector2(JoystickPos.x, JoystickPos.z) * 1.41421f;

                    RGripLastFrame = true;
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
                    RGripLastFrame = false;
                }
                PlaneRotLastFrame = VehicleTransform.rotation;

                bool AfterburnerOn = EffectsControl.AfterburnerOn;
                //keyboard throttle controls and afterburner strength
                if (!HasAfterburner)
                {
                    PlayerThrottle = Mathf.Clamp(PlayerThrottle + ((Shiftf - LeftControlf) * .5f * DeltaTime), 0, 1);
                }
                else if (AfterburnerOn)
                {
                    PlayerThrottle = Mathf.Clamp(PlayerThrottle + ((Shiftf - LeftControlf) * .5f * DeltaTime), 0, 1);
                    Afterburner = (ThrottleInput - ThrottleAfterburnerPoint) * InverseThrottleABPointDivider * AfterburnerThrustMulti;//scale afterburner strength with amount above ThrottleAfterBurnerPoint
                }
                else
                {
                    PlayerThrottle = Mathf.Clamp(PlayerThrottle + ((Shiftf - LeftControlf) * .5f * DeltaTime), 0, ThrottleAfterburnerPoint);
                }
                //VR Throttle
                if (LGrip > 0.75)
                {
                    Vector3 handdistance = VehicleTransform.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                    handdistance = VehicleTransform.InverseTransformDirection(handdistance);
                    float HandThrottleAxis;
                    if (VerticalThrottle)
                    {
                        HandThrottleAxis = handdistance.y;
                    }
                    else
                    {
                        HandThrottleAxis = handdistance.z;
                    }

                    if (!LGripLastFrame)
                    {
                        ThrottleZeroPoint = HandThrottleAxis;
                        TempThrottle = PlayerThrottle;
                        HandDistanceZLastFrame = 0;
                    }
                    float ThrottleDifference = ThrottleZeroPoint - HandThrottleAxis;
                    ThrottleDifference *= ThrottleSensitivity;
                    bool VTOLandAB_Disallowed = (!VTOLAllowAfterburner && VTOLAngle != 0);/*don't allow VTOL AB disabled planes, false if attemping to*/

                    //Detent function to prevent you going into afterburner by accident (bit of extra force required to turn on AB (actually hand speed))
                    if (((HandDistanceZLastFrame - HandThrottleAxis) * ThrottleSensitivity > .05f)/*detent overcome*/ && !VTOLandAB_Disallowed || ((PlayerThrottle > ThrottleAfterburnerPoint/*already in afterburner*/&& !VTOLandAB_Disallowed) || !HasAfterburner))
                    {
                        PlayerThrottle = Mathf.Clamp(TempThrottle + ThrottleDifference, 0, 1);
                    }
                    else
                    {
                        PlayerThrottle = Mathf.Clamp(TempThrottle + ThrottleDifference, 0, ThrottleAfterburnerPoint);
                    }
                    HandDistanceZLastFrame = HandThrottleAxis;
                    LGripLastFrame = true;
                }
                else
                {
                    LGripLastFrame = false;
                }

                if (Taxiing)
                {
                    AAMLockTimer = 0;
                    AAMTargetedTimer = 2;
                    AngleOfAttack = 0;//prevent stall sound and aoavapor when on ground
                    Cruise = false;
                    AltHold = false;
                    //rotate if trying to yaw
                    Taxiinglerper = Mathf.Lerp(Taxiinglerper, RotationInputs.y * TaxiRotationSpeed * Time.smoothDeltaTime, TaxiRotationResponse * DeltaTime);
                    VehicleTransform.Rotate(Vector3.up, Taxiinglerper);

                    StillWindMulti = Mathf.Min(Speed / 10, 1);
                    ThrustVecGrounded = 0;

                    if (BrakeInput > 0 && Speed < GroundBrakeSpeed && !Hooked)
                    {
                        if (Speed > BrakeInput * GroundBrakeStrength * DeltaTime)
                        {
                            VehicleRigidbody.velocity += -CurrentVel.normalized * BrakeInput * GroundBrakeStrength * DeltaTime;
                        }
                        else
                        {
                            VehicleRigidbody.velocity = Vector3.zero;
                        }
                    }

                    if (Physics.Raycast(GroundDetector.position, VehicleTransform.TransformDirection(Vector3.down), 1f, ResupplyLayer))
                    {
                        if (Time.time - LastResupplyTime > 1)
                        {
                            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ResupplyPlane");
                        }
                    }
                    //check for catapult below us and attach if there is one    
                    if (HasCatapult && CatapultStatus == 0)
                    {
                        RaycastHit hit;
                        if (Physics.Raycast(CatapultDetector.position, CatapultDetector.TransformDirection(Vector3.down), out hit, 1f, CatapultLayer))
                        {
                            Transform CatapultTrigger = hit.collider.transform;//get the transform from the trigger hit

                            //Hit detected, check if the plane is facing in the right direction..
                            if (Vector3.Angle(VehicleTransform.forward, CatapultTrigger.transform.forward) < 15)
                            {
                                //then lock the plane to the catapult! Works with the catapult in any orientation whatsoever.
                                CatapultTransform = CatapultTrigger.transform;
                                //match plane rotation to catapult excluding pitch because some planes have shorter front or back wheels
                                VehicleTransform.rotation = Quaternion.Euler(new Vector3(VehicleTransform.rotation.eulerAngles.x, CatapultTransform.rotation.eulerAngles.y, CatapultTransform.rotation.eulerAngles.z));

                                //move the plane to the catapult, excluding the y component (relative to the catapult), so we are 'above' it
                                Vector3 PlaneCatapultDistance = CatapultTransform.position - VehicleTransform.position;
                                PlaneCatapultDistance = CatapultTransform.transform.InverseTransformDirection(PlaneCatapultDistance);
                                VehicleTransform.position = CatapultTransform.position;
                                VehicleTransform.position -= CatapultTransform.up * PlaneCatapultDistance.y;

                                //move the plane back so that the catapult is aligned to the catapult detector
                                Vector3 CatapultDetectorDist = VehicleTransform.position - CatapultDetector.position;
                                CatapultDetectorDist = VehicleTransform.InverseTransformDirection(CatapultDetectorDist);
                                VehicleTransform.position += CatapultTrigger.forward * CatapultDetectorDist.z;

                                CatapultLockRot = VehicleTransform.rotation;//rotation to lock the plane to on the catapult
                                CatapultLockPos = VehicleTransform.position;
                                CatapultStatus = 1;//locked to catapult

                                //use dead to make plane invincible for 1 frame when entering the catapult to prevent damage which will be worse the higher your framerate is
                                dead = true;
                                CatapultDeadTimer = 2;//to make

                                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CatapultLockIn");
                                /*  if (!SoundControl.CatapultLockNull)
                                 {
                                     //SoundControl.CatapultLock.play();
                                 } */
                            }
                        }
                    }
                }
                else
                {
                    StillWindMulti = 1;
                    ThrustVecGrounded = 1;
                    Taxiinglerper = 0;
                }
                //keyboard control for afterburner
                if (Input.GetKeyDown(KeyCode.T) && HasAfterburner && (VTOLAngle == 0 || VTOLAllowAfterburner))
                {
                    if (AfterburnerOn)
                        PlayerThrottle = ThrottleAfterburnerPoint;
                    else
                        PlayerThrottle = 1;
                }
                //Cruise PI Controller
                if (Cruise && !LGripLastFrame && !Shift && !Ctrl)
                {
                    float equals = Input.GetKey(KeyCode.Equals) ? DeltaTime * 10 : 0;
                    float minus = Input.GetKey(KeyCode.Minus) ? DeltaTime * 10 : 0;
                    SetSpeed = Mathf.Max(SetSpeed + (equals - minus), 0);

                    float error = (SetSpeed - AirSpeed);

                    CruiseIntegrator += error * DeltaTime;
                    CruiseIntegrator = Mathf.Clamp(CruiseIntegrator, CruiseIntegratorMin, CruiseIntegratorMax);

                    //float Derivator = Mathf.Clamp(((error - lastframeerror) / DeltaTime),DerivMin, DerivMax);

                    ThrottleInput = CruiseProportional * error;
                    ThrottleInput += CruiseIntegral * CruiseIntegrator;
                    //ThrottleInput += Derivative * Derivator; //works but spazzes out real bad
                    ThrottleInput = PlayerThrottle = Mathf.Clamp(ThrottleInput, 0, 1);
                }
                else//if cruise control disabled, use inputs
                {
                    if (!InVR)
                    {
                        if (LTrigger > .05f)//axis throttle input for people who wish to use it //.05 deadzone so it doesn't take effect for keyboard users with something plugged in
                            ThrottleInput = LTrigger;
                        else
                        {
                            ThrottleInput = PlayerThrottle;
                        }
                    }
                    else
                    {
                        ThrottleInput = PlayerThrottle;
                    }
                }
                Fuel = Mathf.Max(Fuel - ((FuelConsumption * Mathf.Max(ThrottleInput, 0.35f)) * DeltaTime), 0);
                if (Fuel < 200) ThrottleInput = Mathf.Clamp(ThrottleInput * (Fuel / 200), 0, 1);//decrease max throttle as fuel runs out

                if (HasAfterburner)
                {
                    if (ThrottleInput > ThrottleAfterburnerPoint && !AfterburnerOn)
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetAfterburnerOn");
                    }
                    else if (ThrottleInput <= ThrottleAfterburnerPoint && AfterburnerOn)
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetAfterburnerOff");
                    }
                }
                if (AltHold && !RGripLastFrame)//alt hold enabled, and player not holding joystick
                {
                    Vector3 localAngularVelocity = transform.InverseTransformDirection(VehicleRigidbody.angularVelocity);
                    //Altitude hold PI Controller

                    int upsidedown = Vector3.Dot(Vector3.up, VehicleTransform.up) > 0 ? 1 : -1;
                    float error = CurrentVel.normalized.y - (localAngularVelocity.x * upsidedown * 2.5f);//(Vector3.Dot(VehicleRigidbody.velocity.normalized, Vector3.up));

                    AltHoldPitchIntegrator += error * DeltaTime;
                    //AltHoldPitchIntegrator = Mathf.Clamp(AltHoldPitchIntegrator, AltHoldPitchIntegratorMin, AltHoldPitchIntegratorMax);
                    //AltHoldPitchDerivator = (error - AltHoldPitchlastframeerror) / DeltaTime;
                    AltHoldPitchlastframeerror = error;
                    RotationInputs.x = AltHoldPitchProportional * error;
                    RotationInputs.x += AltHoldPitchIntegral * AltHoldPitchIntegrator;
                    //RotationInputs.x += AltHoldPitchDerivative * AltHoldPitchDerivator; //works but spazzes out real bad
                    RotationInputs.x = Mathf.Clamp(RotationInputs.x, -1, 1);
                    AltHoldPitchlastframeerror = error;

                    //Roll
                    float ErrorRoll = VehicleTransform.localEulerAngles.z;
                    if (ErrorRoll > 180) { ErrorRoll -= 360; }

                    //lock upside down if rotated more than 90
                    if (ErrorRoll > 90)
                    {
                        ErrorRoll -= 180;
                        RotationInputs.x *= -1;
                    }
                    else if (ErrorRoll < -90)
                    {
                        ErrorRoll += 180;
                        RotationInputs.x *= -1;
                    }

                    RotationInputs.z = Mathf.Clamp(AltHoldRollProportional * ErrorRoll, -1, 1);

                    RotationInputs.y = 0;

                    //flight limit internally enabled when alt hold is enabled
                    float GLimitStrength = Mathf.Clamp(-(Gs / GLimiter) + 1, 0, 1);
                    float AoALimitStrength = Mathf.Clamp(-(Mathf.Abs(AngleOfAttack) / AoALimiter) + 1, 0, 1);
                    float Limits = Mathf.Min(GLimitStrength, AoALimitStrength);
                    RotationInputs.x *= Limits;
                }
                else//alt hold disabled, player has control
                {
                    if (!InVR)
                    {
                        //allow stick flight in desktop mode
                        VRPitchRoll = LStick;
                        JoystickPosYaw.x = RStick.x;
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
                    //'-input' are used by effectscontroller, and multiplied by 'strength' for final values
                    if (FlightLimitsEnabled && !Taxiing && AngleOfAttack < AoALimiter)//flight limits are enabled
                    {
                        float GLimitStrength = Mathf.Clamp(-(Gs / GLimiter) + 1, 0, 1);
                        float AoALimitStrength = Mathf.Clamp(-(Mathf.Abs(AngleOfAttack) / AoALimiter) + 1, 0, 1);
                        float Limits = Mathf.Min(GLimitStrength, AoALimitStrength);
                        RotationInputs.x = Mathf.Clamp(/*(MouseY * mouseysens + Lstick.y + */VRPitchRoll.y + Wf + Sf + downf + upf, -1, 1) * Limits;
                        RotationInputs.y = Mathf.Clamp(Qf + Ef + JoystickPosYaw.x, -1, 1) * Limits;
                    }
                    else//player is in full control
                    {
                        RotationInputs.x = Mathf.Clamp(/*(MouseY * mouseysens + Lstick.y + */VRPitchRoll.y + Wf + Sf + downf + upf, -1, 1);
                        RotationInputs.y = Mathf.Clamp(Qf + Ef + JoystickPosYaw.x, -1, 1);
                    }
                    //roll isn't subject to flight limits
                    RotationInputs.z = Mathf.Clamp(((/*(MouseX * mousexsens) + */VRPitchRoll.x + Af + Df + leftf + rightf) * -1), -1, 1);
                }

                //check for catching a cable with hook
                if (EffectsControl.HookDown)
                {
                    if (Physics.Raycast(HookDetector.position, Vector3.down, 2f, HookCableLayer) && !Hooked)
                    {
                        HookedLoc = VehicleTransform.position;
                        Hooked = true;
                        HookedTime = Time.time;
                        PlaneAnimator.SetTrigger(HOOKED_STRING);
                    }
                }
                //slow down if hooked and on the ground
                if (Hooked && Taxiing)
                {
                    if (Vector3.Distance(VehicleTransform.position, HookedLoc) > HookedCableSnapDistance)//real planes take around 80-90 meters to stop on a carrier
                    {
                        //if you go further than HookedBrakeMaxDistance you snap the cable and it hurts your plane by the % of the amount of time left of the 2 seconds it should have taken to stop you.
                        float HookedDelta = (Time.time - HookedTime);
                        if (HookedDelta < 2)
                        {
                            Health -= ((-HookedDelta + 2) / 2) * FullHealth;
                        }
                        Hooked = false;
                        //if you catch a cable but go airborne before snapping it, keep your hook out and then land somewhere else
                        //you would hear the cablesnap sound when you touchdown, so limit it to within 5 seconds of hooking
                        //this results in 1 frame's worth of not being able to catch a cable if hook stays down after being 'hooked', not snapping and then trying to hook again
                        //but that should be a very rare and unnoitcable(if it happens) occurance
                        if (HookedDelta < 5)
                        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PlayCableSnap"); }
                    }

                    if (Speed > HookedBrakeStrength * DeltaTime)
                    {
                        VehicleRigidbody.velocity += -CurrentVel.normalized * HookedBrakeStrength * DeltaTime;
                    }
                    else
                    {
                        VehicleRigidbody.velocity = Vector3.zero;
                    }
                    //Debug.Log("hooked");
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

                pitch = Mathf.Clamp(RotationInputs.x/*  + Trim.x */, -1, 1) * PitchStrength * ReversingPitchStrength;
                yaw = Mathf.Clamp(-RotationInputs.y/*  - Trim.y */, -1, 1) * YawStrength * ReversingYawStrength;
                roll = RotationInputs.z * RollStrength * ReversingRollStrength;


                if (pitch > 0)
                {
                    pitch *= PitchDownStrMulti;
                }

                //wheel colliders are broken, this workaround stops the plane from being 'sticky' when you try to start moving it. Heard it doesn't happen so bad if rigidbody weight is much higher.
                if (Speed < .2 && HasWheelColliders && ThrottleInput > 0)
                {
                    if (VTOLAngle > VTOL90Degrees)
                    { VehicleRigidbody.velocity = VehicleTransform.forward * -.25f; }
                    else
                    { VehicleRigidbody.velocity = VehicleTransform.forward * .25f; }
                }
            }
            else
            {
                Occupied = false;
                //brake is always on if the plane is on the ground
                if (Taxiing)
                {
                    StillWindMulti = Mathf.Min(Speed / 10, 1);
                    if (Speed > GroundBrakeStrength * DeltaTime)
                    {
                        VehicleRigidbody.velocity += -CurrentVel.normalized * GroundBrakeStrength * DeltaTime;
                    }
                    else VehicleRigidbody.velocity = Vector3.zero;
                }
                else { StillWindMulti = 1; }
                /*                 RotationInputs.x = Trim.x;
                                RotationInputs.y = Trim.y; */
            }

            //Lerp the inputs for 'engine response', throttle decrease response is slower than increase (EngineSpoolDownSpeedMulti)
            if (EngineOutput < ThrottleInput)
            { EngineOutput = Mathf.Lerp(EngineOutput, ThrottleInput, AccelerationResponse * DeltaTime); }
            else
            { EngineOutput = Mathf.Lerp(EngineOutput, ThrottleInput, AccelerationResponse * EngineSpoolDownSpeedMulti * DeltaTime); }

            float sidespeed = 0;
            float downspeed = 0;
            float forwardspeed = 0;
            float SpeedLiftFactor = 0;
            bool Flaps = false;

            if (PlaneMoving)//optimization
            {
                //used to create air resistance for updown and sideways if your movement direction is in those directions
                //to add physics to plane's yaw and pitch, accel angvel towards velocity, and add force to the plane
                //and add wind
                sidespeed = Vector3.Dot(AirVel, VehicleTransform.right);
                downspeed = -Vector3.Dot(AirVel, VehicleTransform.up);
                forwardspeed = Vector3.Dot(AirVel, VehicleTransform.forward);

                bool PitchDown = (downspeed < 0) ? true : false;//air is hitting plane from above
                if (PitchDown)
                {
                    downspeed *= PitchDownLiftMulti;

                    //speed related values
                    SpeedLiftFactor = Mathf.Min(AirSpeed * AirSpeed * Lift, MaxLift * PitchDownLiftMulti);//max lift is lower when pitching down
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

                Flaps = EffectsControl.Flaps;

                //flaps drag and lift
                if (Flaps)
                {
                    if (PitchDown)//flaps on, but plane's angle of attack is negative so they have no helpful effect
                    {
                        FlapsDrag = FlapsDragMulti;
                        FlapsLift = 1;
                    }
                    else//flaps on positive angle of attack, flaps are useful
                    {
                        FlapsDrag = FlapsDragMulti;
                        FlapsLift = FlapsLiftMulti;
                    }
                }
                //gear drag
                if (EffectsControl.GearUp) { GearDrag = 1; }
                else { GearDrag = LandingGearDragMulti; }
                FlapsGearBrakeDrag = (GearDrag + FlapsDrag + (BrakeInput * AirbrakeStrength)) - 1;//combine these so we don't have to do as much in fixedupdate
            }
            else
            {
                VelLift = pitch = yaw = roll = 0;
            }
            switch (CatapultStatus)
            {
                case 0://normal
                    if (PlaneMoving || Piloting)
                    {
                        //Create a Vector3 Containing the thrust, and rotate and adjust strength based on VTOL value
                        //engine output is multiplied by so that max throttle without afterburner is max strength (unrelated to vtol)
                        Vector3 FinalInputAcc;
                        float GroundEffectAndVelLift = 0;
                        if (VTOLenabled)
                        {
                            float thrust = (HasAfterburner ? Mathf.Min(EngineOutput * (throttleABPointDivider), 1) : EngineOutput) * ThrottleStrength * Afterburner * Atmosphere;

                            Vector3 VTOL180 = new Vector3(0, 0.01f, -1);//used as a rotation target for VTOL adjustment. Slightly below directly backward so that rotatetowards rotates on the correct axis
                            float VTOLAngle2 = VTOLMinAngle + (vtolangledif * VTOLAngle);//vtol angle in degrees
                                                                                         //rotate and scale Vector for VTOL thrust
                            if (VTOLOnly)//just use regular thrust strength if vtol only, as there should be no transition to plane flight
                            {
                                FinalInputAcc = Vector3.RotateTowards(Vector3.forward, VTOL180, Mathf.Deg2Rad * VTOLAngle2, 0) * thrust;
                            }
                            else//vehicle can transition from plane-like flight to helicopter-like flight, with different thrust values for each, with a smooth transition between them
                            {
                                float downthrust = thrust * VTOLThrottleStrengthMulti;
                                FinalInputAcc = Vector3.RotateTowards(Vector3.forward, VTOL180, Mathf.Deg2Rad * VTOLAngle2, 0) * Mathf.Lerp(thrust, downthrust, VTOLAngle90);
                            }
                            //add ground effect to the VTOL thrust
                            GroundEffectAndVelLift = GroundEffect(true, GroundEffectEmpty.position, -VehicleTransform.TransformDirection(FinalInputAcc), VTOLGroundEffectStrength, false, 1);
                            FinalInputAcc *= GroundEffectAndVelLift;

                            //Add Airplane Ground Effect
                            GroundEffectAndVelLift = GroundEffect(false, GroundEffectEmpty.position, -VehicleTransform.up, GroundEffectStrength, Flaps, SpeedLiftFactor);
                            //add lift and thrust
                            FinalInputAcc += new Vector3(-sidespeed * SidewaysLift * SpeedLiftFactor * AoALiftYaw * Atmosphere,// X Sideways
                                ((downspeed * FlapsLift * PitchDownLiftMulti * SpeedLiftFactor * AoALiftPitch) + GroundEffectAndVelLift) * Atmosphere,// Y Up
                                    0);//(HasAfterburner ? Mathf.Min(EngineOutput * (throttleABPointDivider), 1) : EngineOutput) * ThrottleStrength * Afterburner * Atmosphere);// Z Forward
                        }
                        else//Simpler version for non-VTOL craft
                        {
                            GroundEffectAndVelLift = GroundEffect(false, GroundEffectEmpty.position, -VehicleTransform.up, GroundEffectStrength, Flaps, SpeedLiftFactor);
                            FinalInputAcc = new Vector3(-sidespeed * SidewaysLift * SpeedLiftFactor * AoALiftYaw * Atmosphere,// X Sideways
                                ((downspeed * FlapsLift * PitchDownLiftMulti * SpeedLiftFactor * AoALiftPitch) + GroundEffectAndVelLift) * Atmosphere,// Y Up
                                    (HasAfterburner ? Mathf.Min(EngineOutput * 1.25f, 1) : EngineOutput) * ThrottleStrength * Afterburner * Atmosphere);// Z Forward);//
                        }
                        if (SeaPlane)
                        {
                            float depth = Floating();

                            FinalInputAcc.x += -sidespeed * WaterSidewaysDrag * depth;
                            FinalInputAcc.z += -forwardspeed * WaterForwardDrag * depth;
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
                        VehicleConstantForce.relativeForce = Vector3.zero;
                        VehicleConstantForce.relativeTorque = Vector3.zero;
                    }
                    break;
                case 1://locked on catapult
                       //dead == invincible, turn off once a frame has passed since attaching
                    if (dead)
                    {
                        CatapultDeadTimer -= 1;
                        if (CatapultDeadTimer == 0) dead = false;
                    }

                    VehicleConstantForce.relativeForce = Vector3.zero;
                    VehicleConstantForce.relativeTorque = Vector3.zero;

                    CatapultLaunchTime = CatapultLaunchTimeStart;
                    VehicleTransform.SetPositionAndRotation(CatapultLockPos, CatapultLockRot);
                    VehicleRigidbody.velocity = Vector3.zero;
                    VehicleRigidbody.angularVelocity = Vector3.zero;
                    break;
                case 2://launching
                    VehicleTransform.rotation = CatapultLockRot;
                    VehicleConstantForce.relativeForce = VehicleTransform.InverseTransformDirection(CatapultTransform.forward) * CatapultLaunchStrength;
                    //lock all movment except for forward movement
                    Vector3 temp = CatapultTransform.InverseTransformDirection(VehicleRigidbody.velocity);
                    temp.x = 0;
                    temp.y = 0;
                    temp = CatapultTransform.TransformDirection(temp);
                    VehicleRigidbody.velocity = temp;
                    VehicleRigidbody.angularVelocity = Vector3.zero;
                    VehicleConstantForce.relativeTorque = Vector3.zero;
                    CatapultLaunchTime -= DeltaTime;
                    if (CatapultLaunchTime < 0)
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CatapultLockOff");
                        dead = false;//just in case
                        CatapultStatus = 0;
                        Taxiinglerper = 0;
                    }
                    break;
            }
            SoundBarrier = (-Mathf.Clamp(Mathf.Abs(Speed - 343) / SoundBarrierWidth, 0, 1) + 1) * SoundBarrierStrength;

            //play a touchdown sound the frame we start taxiing
            if (Landed == false && Taxiing == true && Speed > TouchDownSoundSpeed)
            {
                if (SoundControl != null)
                {
                    SoundControl.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PlayTouchDownSound");
                }
                Landed = true;
            }
            else if (Taxiing == true)
            {
                Landed = true;
            }
            else
            {
                Landed = false;
            }
        }
        else//non-owners need to know these values
        {
            Speed = AirSpeed = CurrentVel.magnitude;//wind speed is local anyway, so just use ground speed for non-owners
            rotlift = Mathf.Min(Speed / RotMultiMaxSpeed, 1);//so passengers can hear the airbrake
            //VRChat doesn't set Angular Velocity to 0 when you're not the owner of a rigidbody (it seems),
            //causing spazzing, the script handles angular drag it itself, so when we're not owner of the plane, set this value non-zero to stop spazzing
            VehicleRigidbody.angularDrag = .5f;
            //AirVel = VehicleRigidbody.velocity - Wind;//wind isn't synced so this will be wrong
            //AirSpeed = AirVel.magnitude;
        }
        if (Occupied)
        {
            CanopyCloseTimer -= DeltaTime;
            SmokeColor_Color = new Color(SmokeColor.x, SmokeColor.y, SmokeColor.z);
        }
    }
    private void FixedUpdate()
    {
        if (IsOwner)
        {
            if (DoAAMTargeting)
            {
                AAMTargeting(TargetingAngle);
            }
            float DeltaTime = Time.fixedDeltaTime;
            //lerp velocity toward 0 to simulate air friction
            Vector3 VehicleVel = VehicleRigidbody.velocity;
            VehicleRigidbody.velocity = Vector3.Lerp(VehicleVel, FinalWind * StillWindMulti * Atmosphere, ((((AirFriction + SoundBarrier) * FlapsGearBrakeDrag)) * 90) * DeltaTime);
            //apply pitching using pitch moment
            VehicleRigidbody.AddForceAtPosition(Pitching, PitchMoment.position, ForceMode.Force);//deltatime is built into ForceMode.Force
            //apply yawing using yaw moment
            VehicleRigidbody.AddForceAtPosition(Yawing, YawMoment.position, ForceMode.Force);
            //calc Gs
            float gravity = 9.81f * DeltaTime;
            LastFrameVel.y -= gravity; //add gravity
            Gs = Vector3.Distance(LastFrameVel, VehicleVel) / gravity;
            LastFrameVel = VehicleVel;
        }
    }

    //In soundcontroller, CanopyCloseTimer < -100000 means play inside canopy sounds and between -100000 and 0 means play outside sounds.
    //The value is set above these numbers by the length of the animation, and delta time is removed from it each frame.
    private void ToggleCanopy()
    {
        if (CanopyCloseTimer <= -100000 - CanopyCloseTime)
        {
            SetCanopyClosed();
        }
        else if (CanopyCloseTimer < 0 && CanopyCloseTimer > -100000)
        {
            SetCanopyOpen();
        }
    }
    public void LaunchFlares()
    {
        PlaneAnimator.SetTrigger(FLARES_STRING);
    }

    public void LaunchAAM()
    {
        if (NumAAM > 0) { NumAAM--; }//so it doesn't go below 0 when desync occurs
        PlaneAnimator.SetTrigger(AAMLAUNCHED_STRING);
        if (AAM != null)
        {
            GameObject NewAAM = VRCInstantiate(AAM);
            if (!(NumAAM % 2 == 0))
            {
                //invert local x coordinates of launch point, launch, then revert
                Vector3 temp = AAMLaunchPoint.localPosition;
                temp.x *= -1;
                AAMLaunchPoint.localPosition = temp;
                NewAAM.transform.SetPositionAndRotation(AAMLaunchPoint.position, AAMLaunchPoint.transform.rotation);
                temp.x *= -1;
                AAMLaunchPoint.localPosition = temp;
            }
            else
            {
                NewAAM.transform.SetPositionAndRotation(AAMLaunchPoint.position, AAMLaunchPoint.transform.rotation);
            }
            NewAAM.SetActive(true);
            NewAAM.GetComponent<Rigidbody>().velocity = CurrentVel;
        }
        PlaneAnimator.SetFloat(AAMS_STRING, (float)NumAAM * FullAAMsDivider);
    }
    public void LaunchAGM()
    {
        if (NumAGM > 0) { NumAGM--; }
        PlaneAnimator.SetTrigger(AGMLAUNCHED_STRING);
        if (AGM != null)
        {
            GameObject NewAGM = VRCInstantiate(AGM);
            if (!(NumAGM % 2 == 0))
            {
                Vector3 temp = AGMLaunchPoint.localPosition;
                temp.x *= -1;
                AGMLaunchPoint.localPosition = temp;
                NewAGM.transform.SetPositionAndRotation(AGMLaunchPoint.position, AGMLaunchPoint.transform.rotation);
                temp.x *= -1;
                AGMLaunchPoint.localPosition = temp;
            }
            else
            {
                NewAGM.transform.SetPositionAndRotation(AGMLaunchPoint.position, AGMLaunchPoint.transform.rotation);
            }
            NewAGM.SetActive(true);
            NewAGM.GetComponent<Rigidbody>().velocity = CurrentVel;
        }
        PlaneAnimator.SetFloat(AGMS_STRING, (float)NumAGM * FullAGMsDivider);
    }
    public void LaunchBomb()
    {
        if (NumBomb > 0) { NumBomb--; }
        PlaneAnimator.SetTrigger(BOMBLAUNCHED_STRING);
        if (Bomb != null)
        {
            GameObject NewBomb = VRCInstantiate(Bomb);

            NewBomb.transform.SetPositionAndRotation(BombLaunchPoints[BombPoint].position, VehicleTransform.rotation);
            NewBomb.SetActive(true);
            NewBomb.GetComponent<Rigidbody>().velocity = CurrentVel;
            BombPoint++;
            if (BombPoint == BombLaunchPoints.Length) BombPoint = 0;
        }
        PlaneAnimator.SetFloat(BOMBS_STRING, (float)NumBomb * FullBombsDivider);
    }
    void SortTargets(GameObject[] Targets, float[] order)
    {
        for (int i = 1; i < order.Length; i++)
        {
            for (int j = 0; j < (order.Length - i); j++)
            {
                if (order[j] > order[j + 1])
                {
                    var h = order[j + 1];
                    order[j + 1] = order[j];
                    order[j] = h;
                    var k = Targets[j + 1];
                    Targets[j + 1] = Targets[j];
                    Targets[j] = k;
                }
            }
        }
    }
    public void Targeted()
    {
        EngineController TargetEngine = null;
        if (AAMTargets[AAMTarget] != null && AAMTargets[AAMTarget].transform.parent != null)
            TargetEngine = AAMTargets[AAMTarget].transform.parent.GetComponent<EngineController>();
        if (TargetEngine != null)
        {
            if (TargetEngine.Piloting || TargetEngine.Passenger)
            { TargetEngine.EffectsControl.PlaneAnimator.SetTrigger(RADARLOCKED_STRING); }
        }
    }
    public void CatapultLaunchEffects()
    {
        VehicleRigidbody.WakeUp(); //i don't think it actually sleeps anyway but this might help other clients sync the launch faster idk
        if (EffectsControl.CatapultSteam != null) { EffectsControl.CatapultSteam.Play(); }
        if (Piloting || Passenger)
        {
            if (!SoundControl.CatapultLaunchNull)
            {
                SoundControl.CatapultLaunch.Play();
            }
        }
        else
        {
            if (!SoundControl.CatapultLaunchNull)
            {
                SoundControl.CatapultLaunch.Play();
            }
        }
    }
    public void CatapultLockIn()
    {
        PlaneAnimator.SetBool(ONCATAPULT_STRING, true);
        VehicleRigidbody.Sleep();//don't think this actually helps
        if (!SoundControl.CatapultLockNull) { SoundControl.CatapultLock.Play(); }
    }
    public void CatapultLockOff()
    {
        PlaneAnimator.SetBool(ONCATAPULT_STRING, false);
    }
    //these are used for syncing weapon selection for bomb bay doors animation etc
    public void RStick0()//Rstick is something other than a weapon
    {
        WeaponSelected = false;
        PlaneAnimator.SetInteger(WEAPON_STRING, 0);
    }
    public void RStick1()//GUN
    {
        WeaponSelected = true;
        PlaneAnimator.SetInteger(WEAPON_STRING, 1);
    }
    public void RStick2()//AAM
    {
        WeaponSelected = true;
        PlaneAnimator.SetInteger(WEAPON_STRING, 2);
    }
    public void RStick3()//AGM
    {
        WeaponSelected = true;
        PlaneAnimator.SetInteger(WEAPON_STRING, 3);
    }
    public void RStick4()//Bomb
    {
        WeaponSelected = true;
        PlaneAnimator.SetInteger(WEAPON_STRING, 4);
    }
    public void Explode()//all the things players see happen when the vehicle explodes
    {
        WeaponSelected = false;
        dead = true;
        BrakeInput = 0;
        Cruise = false;
        CatapultStatus = 0;
        PlayerThrottle = 0;
        ThrottleInput = 0;
        EngineOutput = 0;
        MissilesIncoming = 0;
        VTOLAngle = VTOLDefaultValue;
        VTOLAngleInput = VTOLDefaultValue;
        if (HasAfterburner) { SetAfterburnerOff(); }
        if (HasSmoke) { SetSmokingOff(); }
        if (HasLimits) { SetLimitsOn(); }
        if (HasHook) { SetHookUp(); }
        if (HasGear) { SetGearDown(); }
        if (HasFlaps) { SetFlapsOn(); }
        //EngineControl.Trim = Vector2.zero;
        Hooked = false;
        BombPoint = 0;
        NumAAM = FullAAMs;
        NumAGM = FullAGMs;
        NumBomb = FullBombs;
        GunAmmoInSeconds = FullGunAmmo;
        Fuel = FullFuel;
        RStickSelection = 0;
        LStickSelection = 0;
        Atmosphere = 1;//planemoving optimization requires this to be here

        if (HasCanopy) { CanopyOpening(); }
        EffectsControl.EffectsExplode();
        SoundControl.Explode_Sound();

        if (IsOwner)
        {
            VehicleRigidbody.velocity = Vector3.zero;
            Health = FullHealth;//turns off low health smoke
            Fuel = FullFuel;
            AoALiftPitch = 0;
            AoALiftYaw = 0;
            AngleOfAttack = 0;
            VelLift = VelLiftStart;
        }

        //our killer increases their kills
        float time = Time.time;
        if (PlaneHitDetector.LastAttacker != null && (time - PlaneHitDetector.LastHitTime) < 5 && !Taxiing && ((time - PilotExitTime) < 5 || Occupied))
        {
            PlaneHitDetector.LastAttacker.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "IncreaseKills");
        }
        //Update Kills board (person with most kills will probably show as having one less kill than they really have until they die, because synced variables will update after this)
        //waiting for VRC networking patch to fix
        if (KillsBoard != null)
        {
            KillsBoard.UpdateScores();
        }
        //pilot and passengers are dropped out of the plane
        if ((Piloting || Passenger) && !InEditor)
        {
            HUDControl.ExitStation();
        }
    }
    public void IncreaseKills()
    {
        if (KillsBoard != null && Piloting)
        {
            KillsBoard.MyKills++;
            if (KillsBoard.MyKills > KillsBoard.MyBestKills)
            {
                KillsBoard.MyBestKills = KillsBoard.MyKills;
            }
            if (KillsBoard.MyKills > KillsBoard.TopKills)
            {
                if (InEditor)
                {
                    KillsBoard.TopKiller = "Player";
                    KillsBoard.TopKills = KillsBoard.MyKills;
                }
                else
                {
                    Networking.SetOwner(localPlayer, KillsBoard.gameObject);
                    KillsBoard.TopKiller = localPlayer.displayName;
                    KillsBoard.TopKills = KillsBoard.MyKills;
                }
            }
        }
    }
    private void AAMTargeting(float Lock_Angle)
    {
        float DeltaTime = Time.fixedDeltaTime;
        var AAMCurrentTargetPosition = AAMTargets[AAMTarget].transform.position;
        float AAMCurrentTargetAngle = Vector3.Angle(VehicleTransform.forward, (AAMCurrentTargetPosition - CenterOfMass.position));
        Vector3 HudControlPosition = HUDControl.transform.position;

        //check 1 target per frame to see if it's infront of us and worthy of being our current target
        var TargetChecker = AAMTargets[AAMTargetChecker];
        var TargetCheckerTransform = TargetChecker.transform;
        var TargetCheckerParent = TargetCheckerTransform.parent;

        Vector3 AAMNextTargetDirection = (TargetCheckerTransform.position - CenterOfMass.position);
        float NextTargetAngle = Vector3.Angle(VehicleTransform.forward, AAMNextTargetDirection);
        float NextTargetDistance = Vector3.Distance(CenterOfMass.position, TargetCheckerTransform.position);
        bool AAMCurrentTargetEngineControlNull = AAMCurrentTargetEngineControl == null ? true : false;

        if (TargetChecker.activeInHierarchy)
        {
            EngineController NextTargetEngineControl = null;

            if (TargetCheckerParent)
            {
                NextTargetEngineControl = TargetCheckerParent.GetComponent<EngineController>();
            }
            //if target EngineController is null then it's a dummy target (or hierarchy isn't set up properly)
            if ((!NextTargetEngineControl || (!NextTargetEngineControl.Taxiing && !NextTargetEngineControl.dead)))
            {
                RaycastHit hitnext;
                //raycast to check if it's behind something
                bool LineOfSightNext = Physics.Raycast(HudControlPosition, AAMNextTargetDirection, out hitnext, Mathf.Infinity, 133121 /* Default, Environment, and Walkthrough */, QueryTriggerInteraction.Ignore);

                if ((LineOfSightNext
                    && hitnext.collider.gameObject.layer == OutsidePlaneLayer //did raycast hit an object on the layer planes are on?
                        && NextTargetAngle < Lock_Angle
                            && NextTargetAngle < AAMCurrentTargetAngle)
                                && NextTargetDistance < AAMMaxTargetDistance
                                    || ((!AAMCurrentTargetEngineControlNull && AAMCurrentTargetEngineControl.Taxiing)//prevent being unable to switch target if it's angle is higher than your current target and your current target happens to be taxiing and is therefore untargetable
                                        || !AAMTargets[AAMTarget].activeInHierarchy))//same as above but if the target is destroyed
                {
                    //found new target
                    AAMCurrentTargetAngle = NextTargetAngle;
                    AAMTarget = AAMTargetChecker;
                    AAMCurrentTargetPosition = AAMTargets[AAMTarget].transform.position;
                    AAMCurrentTargetEngineControl = NextTargetEngineControl;
                    AAMLockTimer = 0;
                    AAMTargetedTimer = .6f;//give the synced variable(AAMTarget) time to update before sending targeted
                    AAMCurrentTargetEngineControlNull = AAMCurrentTargetEngineControl == null ? true : false;
                    if (HUDControl != null)
                    {
                        HUDControl.RelativeTargetVelLastFrame = Vector3.zero;
                        HUDControl.GUN_TargetSpeedLerper = 0f;
                        HUDControl.GUN_TargetDirOld = AAMNextTargetDirection * 1.00001f; //so the difference isn't 0
                    }
                }
            }
        }
        //increase target checker ready for next frame
        AAMTargetChecker++;
        if (AAMTargetChecker == AAMTarget && AAMTarget == NumAAMTargets - 1)
        { AAMTargetChecker = 0; }
        else if (AAMTargetChecker == AAMTarget)
        { AAMTargetChecker++; }
        else if (AAMTargetChecker > NumAAMTargets - 1)
        { AAMTargetChecker = 0; }

        //if target is currently in front of plane, lock onto it
        if (AAMCurrentTargetEngineControlNull)
        { AAMCurrentTargetDirection = AAMCurrentTargetPosition - HudControlPosition; }
        else
        { AAMCurrentTargetDirection = AAMCurrentTargetEngineControl.CenterOfMass.position - HudControlPosition; }
        float AAMCurrentTargetDistance = AAMCurrentTargetDirection.magnitude;
        //check if target is active, and if it's enginecontroller is null(dummy target), or if it's not null(plane) make sure it's not taxiing or dead.
        //raycast to check if it's behind something
        RaycastHit hitcurrent;
        bool LineOfSightCur = Physics.Raycast(HudControlPosition, AAMCurrentTargetDirection, out hitcurrent, Mathf.Infinity, 133121 /* Default, Environment, and Walkthrough */, QueryTriggerInteraction.Ignore);
        //used to make lock remain for .25 seconds after target is obscured
        if (LineOfSightCur == false || hitcurrent.collider.gameObject.layer != OutsidePlaneLayer)
        { AAMTargetObscuredDelay += DeltaTime; }
        else
        { AAMTargetObscuredDelay = 0; }

        if (!Taxiing
            && (AAMTargetObscuredDelay < .25f)
                && AAMCurrentTargetDistance < AAMMaxTargetDistance
                    && AAMTargets[AAMTarget].activeInHierarchy
                        && (AAMCurrentTargetEngineControlNull || (!AAMCurrentTargetEngineControl.Taxiing && !AAMCurrentTargetEngineControl.dead)))
        {
            if ((AAMTargetObscuredDelay < .25f) && AAMCurrentTargetDistance < AAMMaxTargetDistance)
            {
                AAMHasTarget = true;
                if (AAMCurrentTargetAngle < Lock_Angle && NumAAM > 0)
                {
                    AAMLockTimer += DeltaTime;
                    //give enemy radar lock even if you're out of missiles
                    if (!AAMCurrentTargetEngineControlNull && RStickSelection == 2)// Only send Targeted if using AAMs, not gun.
                    {
                        //target is a plane, send the 'targeted' event every second to make the target plane play a warning sound in the cockpit.
                        AAMTargetedTimer += DeltaTime;
                        if (AAMTargetedTimer > 1)
                        {
                            AAMTargetedTimer = 0;
                            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Targeted");
                        }
                    }
                }
                else
                {
                    AAMTargetedTimer = 2f;
                    AAMLockTimer = 0;
                }
            }
        }
        else
        {
            AAMTargetedTimer = 2f;
            AAMLockTimer = 0;
            AAMHasTarget = false;
        }
        /*         if (HUDControl.gameObject.activeInHierarchy)
                {
                    Debug.Log(string.Concat("AAMTarget ", AAMTarget));
                    Debug.Log(string.Concat("HasTarget ", AAMHasTarget));
                    Debug.Log(string.Concat("AAMTargetObscuredDelay ", AAMTargetObscuredDelay));
                    Debug.Log(string.Concat("LoS ", LineOfSightCur));
                    Debug.Log(string.Concat("RayCastCorrectLayer ", (hitcurrent.collider.gameObject.layer == OutsidePlaneLayer)));
                    Debug.Log(string.Concat("RayCastLayer ", hitcurrent.collider.gameObject.layer));
                    Debug.Log(string.Concat("NotObscured ", AAMTargetObscuredDelay < .25f));
                    Debug.Log(string.Concat("InAngle ", AAMCurrentTargetAngle < Lock_Angle));
                    Debug.Log(string.Concat("BelowMaxDist ", AAMCurrentTargetDistance < AAMMaxTargetDistance));
                } */
    }
    public void PlayCableSnap()
    {
        if (!SoundControl.CableSnapNull) { SoundControl.CableSnap.Play(); }
    }
    public void SetAfterburnerOn()
    {
        EffectsControl.AfterburnerOn = true;
        PlaneAnimator.SetBool(AFTERBURNERON_STRING, true);
        if ((Piloting || Passenger) && (CanopyCloseTimer < 0 && CanopyCloseTimer > -100000))
        {
            if (!SoundControl.ABOnInsideNull)
                SoundControl.ABOnInside.Play();
        }
        else
        {
            if (!SoundControl.ABOnOutsideNull)
                SoundControl.ABOnOutside.Play();
        }
        Afterburner = AfterburnerThrustMulti;
    }
    public void SetAfterburnerOff()
    {
        EffectsControl.AfterburnerOn = false;
        Afterburner = 1;
        PlaneAnimator.SetBool(AFTERBURNERON_STRING, false);
    }
    private void ToggleAfterburner()
    {
        bool AfterburnerOn = EffectsControl.AfterburnerOn;
        if (!AfterburnerOn && ThrottleInput > 0.8)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetAfterburnerOn");
        }
        else if (AfterburnerOn)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetAfterburnerOff");
        }
    }
    public void ResupplyPlane()
    {
        //only play the sound if we're actually repairing/getting ammo/fuel
        if (!SoundControl.ReloadingNull && (NumAAM != FullAAMs || NumAGM != FullAGMs || NumBomb != FullBombs || Fuel < FullFuel - 10 || GunAmmoInSeconds != FullGunAmmo || Health != FullHealth))
        {
            SoundControl.Reloading.Play();
        }
        LastResupplyTime = Time.time;
        NumAAM = (int)Mathf.Min(NumAAM + Mathf.Max(Mathf.Floor(FullAAMs / 10), 1), FullAAMs);
        NumAGM = (int)Mathf.Min(NumAGM + Mathf.Max(Mathf.Floor(FullAGMs / 5), 1), FullAGMs);
        NumBomb = (int)Mathf.Min(NumBomb + Mathf.Max(Mathf.Floor(FullBombs / 5), 1), FullBombs);

        PlaneAnimator.SetFloat(AAMS_STRING, (float)NumAAM * FullAAMsDivider);
        PlaneAnimator.SetFloat(AGMS_STRING, (float)NumAGM * FullAGMsDivider);
        PlaneAnimator.SetFloat(BOMBS_STRING, (float)NumBomb * FullBombsDivider);

        /*Debug.Log(string.Concat("fuel ", Fuel));
          Debug.Log(string.Concat("FullFuel ", FullFuel));
          Debug.Log(string.Concat("Health ", Health));
          Debug.Log(string.Concat("FullHealth ", FullHealth));
          Debug.Log(string.Concat("GunAmmoInSeconds ", GunAmmoInSeconds));
          Debug.Log(string.Concat("FullGunAmmo ", FullGunAmmo)); */
        Fuel = Mathf.Min(Fuel + (FullFuel / 25), FullFuel);
        GunAmmoInSeconds = Mathf.Min(GunAmmoInSeconds + (FullGunAmmo / 20), FullGunAmmo);
        Health = Mathf.Min(Health + (FullHealth / 30), FullHealth);
        PlaneAnimator.SetTrigger(RESUPPLY_STRING);
        BombPoint = 0;
    }
    public void SetCanopyOpen()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CanopyOpening");
    }
    public void SetCanopyClosed()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CanopyClosing");
    }
    public void CanopyOpening()
    {
        if (!EffectsControl.CanopyOpen)//this if statement prevents sound issues when this is called by OnPlayerJoined()
        {
            EffectsControl.CanopyOpen = true;
            if (CanopyCloseTimer > 0)
            { CanopyCloseTimer -= 100000 + CanopyCloseTime; }
            else
            { CanopyCloseTimer = -100000; }
            PlaneAnimator.SetBool(CANOPYOPEN_STRING, true);
        }
    }
    public void CanopyClosing()
    {
        if (EffectsControl.CanopyOpen)//this if statement prevents sound issues when this is called by OnPlayerJoined()
        {
            EffectsControl.CanopyOpen = false;
            if (CanopyCloseTimer > (-100000 - CanopyCloseTime) && CanopyCloseTimer < 0)
            { CanopyCloseTimer += 100000 + ((CanopyCloseTime * 2) + 0.1f); }//the 0.1 is for the delay in the animator that is needed because it's not set to write defaults
            else
            { CanopyCloseTimer = CanopyCloseTime; }
            PlaneAnimator.SetBool(CANOPYOPEN_STRING, false);
        }
    }
    public void SetGearUp()
    {
        EffectsControl.GearUp = true;
        PlaneAnimator.SetBool(GEARUP_STRING, true);
    }
    public void SetGearDown()
    {
        EffectsControl.GearUp = false;
        PlaneAnimator.SetBool(GEARUP_STRING, false);
    }
    public void ToggleGear()
    {
        if (!EffectsControl.GearUp)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetGearUp");
        }
        else
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetGearDown");
        }
    }
    public void SetFlapsOff()
    {
        EffectsControl.Flaps = false;
        PlaneAnimator.SetBool(FLAPS_STRING, false);
        FlapsDrag = 1;
        FlapsLift = 1;
    }
    public void SetFlapsOn()
    {
        EffectsControl.Flaps = true;
        PlaneAnimator.SetBool(FLAPS_STRING, true);
    }
    public void ToggleFlaps()
    {
        if (!EffectsControl.Flaps)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetFlapsOn");
        }
        else
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetFlapsOff");
        }
    }
    public void SetHookDown()
    {
        EffectsControl.HookDown = true;
        PlaneAnimator.SetBool(HOOKDOWN_STRING, true);
    }
    public void SetHookUp()
    {
        EffectsControl.HookDown = false;
        PlaneAnimator.SetBool(HOOKDOWN_STRING, false);
    }
    public void ToggleHook()
    {
        if (!EffectsControl.HookDown)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetHookDown");
        }
        else
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetHookUp");
        }
    }
    public void SetSmokingOn()
    {
        EffectsControl.Smoking = true;
        PlaneAnimator.SetBool(DISPLAYSMOKE_STRING, true);
    }
    public void SetSmokingOff()
    {
        EffectsControl.Smoking = false;
        PlaneAnimator.SetBool(DISPLAYSMOKE_STRING, false);
    }
    public void ToggleSmoking()
    {
        if (!EffectsControl.Smoking)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetSmokingOn");
        }
        else
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetSmokingOff");
        }
    }
    public void SetLimitsOn()
    {
        FlightLimitsEnabled = true;
    }
    public void SetLimitsOff()
    {
        FlightLimitsEnabled = false;
    }
    public void ToggleLimits()
    {
        if (!FlightLimitsEnabled)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetLimitsOn");
        }
        else
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetLimitsOff");
        }
    }
    public void RespawnStatusLocal()//called when using respawn button
    {
        Networking.SetOwner(localPlayer, VehicleMainObj);
        Networking.SetOwner(localPlayer, gameObject);
        Networking.SetOwner(localPlayer, EffectsControl.gameObject);
        //VehicleTransform.position = new Vector3(VehicleTransform.position.x, -10000, VehicleTransform.position.z);
        Atmosphere = 1;//planemoving optimization requires this to be here
        //synced variables
        Health = FullHealth;
        Fuel = FullFuel;
        GunAmmoInSeconds = FullGunAmmo;
        VTOLAngle = VTOLDefaultValue;
        VTOLAngleInput = VTOLDefaultValue;
        VehicleObjectSync.Respawn();//this works if done just locally
    }
    public void ResetStatus()//called globally when using respawn button
    {
        EffectsControl.DoEffects = 6;
        if (HasAfterburner) { SetAfterburnerOff(); }
        if (HasSmoke) { SetSmokingOff(); }
        if (HasLimits) { SetLimitsOn(); }
        if (HasHook) { SetHookUp(); }
        if (HasGear) { SetGearDown(); }
        if (HasFlaps) { SetFlapsOn(); }
        if (HasCanopy && !EffectsControl.CanopyOpen) { SetCanopyOpen(); }
        WeaponSelected = false;
        NumAAM = FullAAMs;
        NumAGM = FullAGMs;
        NumBomb = FullBombs;
        PlaneAnimator.SetFloat(AAMS_STRING, 1);
        PlaneAnimator.SetFloat(AGMS_STRING, 1);
        PlaneAnimator.SetFloat(BOMBS_STRING, 1);
        BombPoint = 0;
        //these two make it invincible and unable to be respawned again for 5s
        dead = true;
        PlaneAnimator.SetTrigger(RESPAWN_STRING);
    }
    public void PlaneHit()
    {
        if (InEditor || IsOwner)
        {
            Health -= 10;
        }
        if (EffectsControl != null)
        {
            EffectsControl.DoEffects = 0f;
            PlaneAnimator.SetTrigger(BULLETHIT_STRING);
        }

        if (SoundControl != null && !SoundControl.BulletHitNull)
        {
            int rand = Random.Range(0, SoundControl.BulletHit.Length);
            SoundControl.BulletHit[rand].pitch = Random.Range(.8f, 1.2f);
            SoundControl.BulletHit[rand].Play();
        }
    }
    public void Respawn_event()//called by Respawn()
    {
        PlayerThrottle = 0;//for editor test mode
        EngineOutput = 0;//^
        EffectsControl.DoEffects = 6f; //wake up if was asleep
        EffectsControl.PlaneAnimator.SetTrigger(INSTANTGEARDOWN_STRING);
        MissilesIncoming = 0;
        if (InEditor)
        {
            VehicleTransform.SetPositionAndRotation(Spawnposition, Quaternion.Euler(Spawnrotation));
            Health = FullHealth;
            EffectsControl.GearUp = false;
            EffectsControl.Flaps = true;
        }
        else if (IsOwner)
        {
            Health = FullHealth;
            //this should respawn it in VRC, doesn't work in editor
            VehicleTransform.position = new Vector3(VehicleTransform.position.x, -10000, VehicleTransform.position.z);
            EffectsControl.GearUp = false;
            EffectsControl.Flaps = true;
        }
    }
    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        //Owner sends events to sync the plane so late joiners don't see it flying with it's canopy open and stuff
        //only change things that aren't in the default state
        //only change effects which are very visible, this is just so that it looks alright for late joiners, not to sync everything perfectly.
        //syncing everything perfectly would probably require too many events to be sent.
        //planes will be fully synced when they explode or are respawned anyway.
        if (IsOwner)
        {
            if (HasCanopy)
            {
                if (!EffectsControl.CanopyOpen)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetCanopyClosed");
                }
            }
            if (HasSmoke)
            {
                if (EffectsControl.Smoking)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetSmokingOn");
                }
            }
            if (HasGear)
            {
                if (EffectsControl.GearUp)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetGearUp");
                }
            }
            if (HasFlaps)
            {
                if (!EffectsControl.Flaps)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetFlapsOff");
                }
            }
        }
    }
    public void PassengerEnterPlaneLocal()
    {
        Passenger = true;
        if (HUDControl != null) { HUDControl.gameObject.SetActive(true); }
        if (EffectsControl.CanopyOpen) CanopyCloseTimer = -100001;
        else CanopyCloseTimer = -1;

        if (EffectsControl != null) { EffectsControl.PlaneAnimator.SetBool(LOCALPASSENGER_STRING, true); }
        SetPlaneLayerInside();
    }
    public void PassengerExitPlaneLocal()
    {
        if (EffectsControl != null) { EffectsControl.PlaneAnimator.SetBool("localpassenger", false); }
        Passenger = false;
        localPlayer.SetVelocity(CurrentVel);
        MissilesIncoming = 0;
        EffectsControl.PlaneAnimator.SetInteger("missilesincoming", 0);

        if (HUDControl != null) { HUDControl.gameObject.SetActive(false); }
        SetPlaneLayerOutside();
    }
    public void PilotEnterPlaneLocal()//called from PilotSeat
    {
        //setting this as a workaround because it doesnt work reliably in Enginecontroller's Start()
        if (localPlayer.IsUserInVR()) { InVR = true; }

        Networking.SetOwner(localPlayer, gameObject);

        if (VehicleMainObj != null) { Networking.SetOwner(localPlayer, VehicleMainObj); }

        EngineOutput = 0;
        ThrottleInput = 0;
        PlayerThrottle = 0;
        IsFiringGun = false;
        VehicleRigidbody.angularDrag = 0;//set to something nonzero when you're not owner to prevent juddering motion on collisions
        VTOLAngleInput = VTOLAngle;

        Piloting = true;
        if (dead) { Health = FullHealth; }//dead is true for the first 5 seconds after spawn, this might help with spontaneous explosions

        if (EffectsControl != null)
        {
            //canopy closed/open sound
            if (EffectsControl.CanopyOpen) { CanopyCloseTimer = -100000 - CanopyCloseTime; }
            else CanopyCloseTimer = -CanopyCloseTime;//less than 0
            Networking.SetOwner(localPlayer, EffectsControl.gameObject);
            //SetSmokingOff();
            EffectsControl.PlaneAnimator.SetBool(LOCALPILOT_STRING, true);
        }
        if (HUDControl != null)
        {
            Networking.SetOwner(localPlayer, HUDControl.gameObject);
            HUDControl.gameObject.SetActive(true);
        }

        //hopefully prevents explosions when you enter the plane
        VehicleRigidbody.velocity = CurrentVel;
        Gs = 0;
        LastFrameVel = CurrentVel;

        SetPlaneLayerInside();

        //Make sure EngineControl.AAMCurrentTargetEngineControl is correct
        var Target = AAMTargets[AAMTarget];
        if (Target && Target.transform.parent)
        {
            AAMCurrentTargetEngineControl = Target.transform.parent.GetComponent<EngineController>();
        }

        if (KillsBoard != null)
        {
            KillsBoard.MyKills = 0;
        }
    }
    public void PilotEnterPlaneGlobal(VRCPlayerApi player)
    {
        PilotName = player.displayName;
        PilotID = player.playerId;

        if (EffectsControl != null)
        {
            PlaneAnimator.SetBool(OCCUPIED_STRING, true);
            EffectsControl.DoEffects = 0f;
        }
        dead = false;//Plane stops being invincible if someone gets in, also acts as redundancy incase someone missed the notdead respawn event
        if (SoundControl != null) { SoundControl.Wakeup(); }
    }
    public void PilotExitPlane(VRCPlayerApi player)
    {
        PilotExitTime = Time.time;
        PilotName = string.Empty;
        PilotID = -1;
        IsFiringGun = false;
        SetSmokingOff();
        SetAfterburnerOff();
        if (EffectsControl != null)
        {
            EffectsControl.EffectsLeavePlane();
        }
        if (player.isLocal)
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
            EjectTimer = 2;
            Hooked = false;
            BrakeInput = 0;
            LTriggerTapTime = 1;
            RTriggerTapTime = 1;
            Taxiinglerper = 0;
            LGripLastFrame = false;
            RGripLastFrame = false;
            LStickSelection = 0;
            RStickSelection = 0;
            BrakeInput = 0;
            LTriggerLastFrame = false;
            RTriggerLastFrame = false;
            AGMLocked = false;
            AAMHasTarget = false;
            DoAAMTargeting = false;
            MissilesIncoming = 0;
            AAMLockTimer = 0;
            AAMLocked = false;
            HUDControl.MenuSoundCheckLast = 0;
            if (Ejected)
            {
                localPlayer.SetVelocity(CurrentVel + VehicleTransform.up * 25);
                Ejected = false;
            }
            else { localPlayer.SetVelocity(CurrentVel); }
            if (CatapultStatus == 1) { CatapultStatus = 0; }//keep launching if launching, otherwise unlock from catapult

            if (HUDControl != null) { HUDControl.gameObject.SetActive(false); }
            //set plane's layer back
            SetPlaneLayerOutside();
        }
        if (KillsBoard != null)
        {
            Scoreboard_Kills killsboard = KillsBoard;
            if (killsboard.MyKills == killsboard.TopKills)
            {
                killsboard.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "UpdateScores");
            }
        }
    }
    public void SetPlaneLayerInside()
    {
        if (PlaneMesh != null)
        {
            Transform[] children = PlaneMesh.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                child.gameObject.layer = OnboardPlaneLayer;
            }
        }
    }
    public void SetPlaneLayerOutside()
    {
        if (PlaneMesh != null)
        {
            Transform[] children = PlaneMesh.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                child.gameObject.layer = Planelayer;
            }
        }
    }
    private void FindAAMTargets()
    {
        //get array of AAM Targets
        Collider[] aamtargs = Physics.OverlapSphere(CenterOfMass.position, 1000000, AAMTargetsLayer, QueryTriggerInteraction.Collide);
        int n = 0;

        //work out which index in the aamtargs array is our own plane by finding which one has this script as it's parent
        //allows for each team to have a different layer for AAMTargets
        int self = -1;
        n = 0;
        foreach (Collider target in aamtargs)
        {
            if (target.transform.parent != null && target.transform.parent == transform)
            {
                self = n;
            }
            n++;
        }
        //populate AAMTargets list excluding our own plane
        n = 0;
        int foundself = 0;
        foreach (Collider target in aamtargs)
        {
            if (n == self) { foundself = 1; n++; }
            else
            {
                AAMTargets[n - foundself] = target.gameObject;
                n++;
            }
        }
        if (aamtargs.Length > 0)
        {
            if (foundself != 0)
            {
                NumAAMTargets = Mathf.Clamp(aamtargs.Length - 1, 0, 999);//one less because it found our own plane
            }
            else
            {
                NumAAMTargets = Mathf.Clamp(aamtargs.Length, 0, 999);
            }
        }
        else { NumAAMTargets = 0; }


        if (NumAAMTargets > 0)
        {
            n = 0;
            //create a unique number based on position in the hierarchy in order to sort the AAMTargets array later, to make sure they're the in the same order on all clients 
            float[] order = new float[NumAAMTargets];
            for (int i = 0; AAMTargets[n] != null; i++)
            {
                Transform parent = AAMTargets[n].transform;
                for (int x = 0; parent != null; x++)
                {
                    order[n] = float.Parse(order[n].ToString() + parent.transform.GetSiblingIndex().ToString());
                    parent = parent.transform.parent;
                }
                n++;
            }
            //sort AAMTargets array based on order

            SortTargets(AAMTargets, order);
        }
        else
        {
            Debug.LogWarning(string.Concat(VehicleMainObj.name, ": NO AAM TARGETS FOUND"));
            AAMTargets[0] = HUDControl.gameObject;//this should prevent HUDController from crashing with a null reference while causing no ill effects
        }
    }
    private void WindAndAoA()
    {
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

        float AoALiftPitchMin = Mathf.Min(Mathf.Abs(AngleOfAttackPitch) / 90, Mathf.Abs(Mathf.Abs(AngleOfAttackPitch) - 180) / 90);//linear version to 90 for high aoa
        AoALiftPitchMin = Mathf.Clamp((-AoALiftPitchMin + 1) * HighPitchAoaMinControl, 0, 1);
        AoALiftPitch = Mathf.Clamp(AoALiftPitch, AoALiftPitchMin, 1);

        AoALiftYaw = Mathf.Min(Mathf.Abs(AngleOfAttackYaw) / MaxAngleOfAttackYaw, Mathf.Abs((Mathf.Abs(AngleOfAttackYaw) - 180)) / MaxAngleOfAttackYaw);
        AoALiftYaw = -AoALiftYaw + 1;
        AoALiftYaw = -Mathf.Pow((1 - AoALiftYaw), AoaCurveStrength) + 1;//give it a curve

        float AoALiftYawMin = Mathf.Min(Mathf.Abs(AngleOfAttackYaw) / 90, Mathf.Abs(Mathf.Abs(AngleOfAttackYaw) - 180) / 90);//linear version to 90 for high aoa
        AoALiftYawMin = Mathf.Clamp((-AoALiftPitchMin + 1) * HighYawAoaMinControl, 0, 1);
        AoALiftYaw = Mathf.Clamp(AoALiftYaw, AoALiftYawMin, 1);

        AngleOfAttack = Mathf.Max(Mathf.Abs(AngleOfAttackPitch), Mathf.Abs(AngleOfAttackYaw));
    }
    private float GroundEffect(bool VTOL, Vector3 Position, Vector3 Direction, float GEStrength, bool Flaps, float speedliftfac)
    {
        //Ground effect, extra lift caused by air pressure when close to the ground
        RaycastHit GE;
        if (Physics.Raycast(Position, Direction, out GE, GroundEffectMaxDistance, 2049 /* Default and Environment */, QueryTriggerInteraction.Collide))
        {
            float GroundEffect = ((-GE.distance + GroundEffectMaxDistance) / GroundEffectMaxDistance) * GEStrength;
            if (VTOL) { return 1 + GroundEffect; }
            if (Flaps) { GroundEffect *= FlapsLiftMulti; }
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
    private float Floating()
    {
        int i = 0;
        float x = 0;
        foreach (Transform FLOAT in FloatPoints)
        {
            RaycastHit hit;
            if (Physics.Raycast(FLOAT.position, -Vector3.up, out hit, SuspMaxDist, 1, QueryTriggerInteraction.Collide))
            {
                if (hit.collider.isTrigger)
                {
                    SuspensionCompression[i] = Mathf.Clamp(((hit.distance / SuspMaxDist) * -1) + 1, 0, 1);
                    float CompressionDifference = (SuspensionCompression[i] - SuspensionCompressionLastFrame[i]);
                    x += SuspensionCompression[i];
                    if (CompressionDifference > 0)
                    { CompressionDifference *= Compressing; }
                    else
                    { CompressionDifference *= Rebound; }

                    SuspensionCompressionLastFrame[i] = SuspensionCompression[i];

                    VehicleRigidbody.AddForceAtPosition((VehicleTransform.up * (((SuspensionCompression[i] * FloatForce) + CompressionDifference))), FloatPoints[i].position, ForceMode.Force);
                }
            }
            i++;
        }
        x /= i;
        return x;
    }
    private void SetVTOLValues()
    {
        VTOLAngle = Mathf.MoveTowards(VTOLAngle, VTOLAngleInput, VTOLAngleDivider * Time.smoothDeltaTime);
        float SpeedForVTOL = (Mathf.Min(Speed / VTOLLoseControlSpeed, 1));
        if (VTOLAngle > 0 && SpeedForVTOL != 1 || VTOLOnly)
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
                VTOLAngle90 = Mathf.Min(VTOLAngle / VTOL90Degrees, 1);
                float SpeedForVTOL_Inverse_xVTOL = ((SpeedForVTOL * -1) + 1) * VTOLAngle90;

                PitchThrustVecMulti = Mathf.Lerp(PitchThrustVecMultiStart, VTOLPitchThrustVecMulti, SpeedForVTOL_Inverse_xVTOL);
                YawThrustVecMulti = Mathf.Lerp(YawThrustVecMultiStart, VTOLYawThrustVecMulti, SpeedForVTOL_Inverse_xVTOL);
                RollThrustVecMulti = Mathf.Lerp(RollThrustVecMultiStart, VTOLRollThrustVecMulti, SpeedForVTOL_Inverse_xVTOL);

                ReversingPitchStrengthZero = 1;
                ReversingYawStrengthZero = 1;
                ReversingRollStrengthZero = 1;
            }


            if (!VTOLAllowAfterburner)
            {
                if (Afterburner != 1)
                { PlayerThrottle = ThrottleAfterburnerPoint; }
            }
            if (Cruise)
            { Cruise = false; }
            if (FlightLimitsEnabled)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetLimitsOff"); }
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
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogWarning("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}