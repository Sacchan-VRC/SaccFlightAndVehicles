
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class EngineController : UdonSharpBehaviour
{
    public GameObject VehicleMainObj;
    [SerializeField] private GameObject InVehicleOnly;
    [SerializeField] private GameObject PilotOnly;
    public UdonSharpBehaviour[] ExtensionUdonBehaviours;
    public UdonSharpBehaviour[] Dial_Functions_L;
    public UdonSharpBehaviour[] Dial_Functions_R;
    private UdonSharpBehaviour CurrentSelectedFunctionL;
    private UdonSharpBehaviour CurrentSelectedFunctionR;
    public Transform PlaneMesh;
    public int OnboardPlaneLayer = 19;
    public Transform CenterOfMass;
    public Transform GroundEffectEmpty;
    public Transform PitchMoment;
    public Transform YawMoment;
    public Transform GroundDetector;
    [UdonSynced(UdonSyncMode.None)] public float Health = 23f;
    public LayerMask ResupplyLayer;
    public LayerMask AAMTargetsLayer;
    public Transform GunRecoilEmpty;
    public float GunRecoil = 150;
    public Scoreboard_Kills KillsBoard;
    public bool RepeatingWorld = true;
    public float RepeatingWorldDistance = 20000;
    [SerializeField] private bool SwitchHandsJoyThrottle = false;
    public bool HasAfterburner = true;
    public float ThrottleAfterburnerPoint = 0.8f;
    public bool VTOLOnly = false;
    public bool NoCanopy = false;
    public bool HasVTOLAngle = false;
    public bool HasLimits = true;
    public bool HasAltHold = true;
    public bool HasCanopy = true;
    public bool HasCruise = true;
    public bool HasFlaps = true;
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
    public float GroundEffectMaxDistance = 7;
    public float GroundEffectStrength = 4;
    public float GroundEffectLiftMax = 9999999;
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
    [Tooltip("Adjusts all values that would need to be adjusted if you changed the mass automatically on Start(). Including all wheel colliders")]
    [SerializeField] private bool AutoAdjustValuesToMass = true;
    public float SeaLevel = -10f;
    public Vector3 Wind;
    public float WindGustStrength = 15;
    public float WindGustiness = 0.03f;
    public float WindTurbulanceScale = 0.0001f;
    public float SoundBarrierStrength = 0.0003f;
    public float SoundBarrierWidth = 20f;
    [UdonSynced(UdonSyncMode.None)] public float Fuel = 7200;
    public float FuelConsumption = 2;
    public float FuelConsumptionABMulti = 3f;
    public float RefuelTime;
    public float RepairTime;
    public float RespawnDelay = 10;
    public float InvincibleAfterSpawn = 2.5f;


    //best to remove synced variables if you aren't using them
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public float EngineOutput = 0f;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public Vector3 CurrentVel = Vector3.zero;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public float Gs = 1f;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public float AngleOfAttack;//MAX of yaw & pitch aoa //used by effectscontroller and hudcontroller
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.None)] public bool Occupied = false; //this is true if someone is sitting in pilot seat
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public float VTOLAngle;

    [System.NonSerializedAttribute] public Animator VehicleAnimator;
    [System.NonSerializedAttribute] public int PilotID;
    [System.NonSerializedAttribute] public string PilotName;
    [System.NonSerializedAttribute] public bool FlightLimitsEnabled = true;
    [System.NonSerializedAttribute] public ConstantForce VehicleConstantForce;
    [System.NonSerializedAttribute] public Rigidbody VehicleRigidbody;
    [System.NonSerializedAttribute] public Transform VehicleTransform;
    private VRC.SDK3.Components.VRCObjectSync VehicleObjectSync;
    private float LerpedRoll;
    private float LerpedPitch;
    private float LerpedYaw;
    [System.NonSerializedAttribute] public int RStickSelection = -1;
    [System.NonSerializedAttribute] public int LStickSelection = -1;
    [System.NonSerializedAttribute] public int RStickSelectionLastFrame = -1;
    [System.NonSerializedAttribute] public int LStickSelectionLastFrame = -1;
    [System.NonSerializedAttribute] public bool ThrottleGripLastFrame = false;
    [System.NonSerializedAttribute] public bool JoystickGripLastFrame = false;
    [System.NonSerializedAttribute] public bool LTriggerLastFrame = false;
    [System.NonSerializedAttribute] public bool RTriggerLastFrame = false;
    Quaternion JoystickZeroPoint;
    Quaternion PlaneRotLastFrame;
    [System.NonSerializedAttribute] public float PlayerThrottle;
    private float TempThrottle;
    private float ThrottleZeroPoint;
    [System.NonSerializedAttribute] public float SetSpeed;
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
    [System.NonSerializedAttribute] public float Speed;
    [System.NonSerializedAttribute] public float AirSpeed;
    [System.NonSerializedAttribute] public bool IsOwner = false;
    private Vector3 FinalWind;//includes Gusts
    [System.NonSerializedAttribute] public Vector3 AirVel;
    private float StillWindMulti;//multiplies the speed of the wind by the speed of the plane when taxiing to prevent still planes flying away
    private int ThrustVecGrounded;
    private float SoundBarrier;
    [System.NonSerializedAttribute] public GameObject[] AAMTargets = new GameObject[80];
    [System.NonSerializedAttribute] public int NumAAMTargets = 0;
    [System.NonSerializedAttribute] public float FullFuel;
    private float LowFuel;
    private float LowFuelDivider;
    private float LastResupplyTime = 5;//can't resupply for the first 10 seconds after joining, fixes potential null ref if sending something to PlaneAnimator on first frame
    [System.NonSerializedAttribute] public float FullGunAmmo;
    [System.NonSerializedAttribute] public int MissilesIncomingHeat = 0;
    [System.NonSerializedAttribute] public int MissilesIncomingRadar = 0;
    [System.NonSerializedAttribute] public int MissilesIncomingOther = 0;
    [System.NonSerializedAttribute] public Vector3 Spawnposition;
    [System.NonSerializedAttribute] public Vector3 Spawnrotation;
    private int OutsidePlaneLayer;
    [System.NonSerializedAttribute] public bool DoAAMTargeting;
    bool Landed = false;
    private float VelLiftStart;
    private HitDetector PlaneHitDetector;
    [System.NonSerializedAttribute] public float PilotExitTime;
    private int Planelayer;
    private float VelLiftMaxStart;
    private bool HasAirBrake;//set to false if air brake strength is 0
    private float HandDistanceZLastFrame;
    private float EngineAngle;
    private float PitchThrustVecMultiStart;
    private float YawThrustVecMultiStart;
    private float RollThrustVecMultiStart;
    private bool VTOLenabled;
    [System.NonSerializedAttribute] public float VTOLAngleInput;
    private float VTOL90Degrees;//1=(90 degrees OR maxVTOLAngle if it's lower than 90) used for transition thrust values 
    private float ThrottleNormalizer;
    private float VTOLAngleDivider;
    private float ABNormalizer;
    private float EngineOutputLastFrame;
    float VTOLAngle90;
    bool PlaneMoving = false;
    bool HasWheelColliders = false;
    private float vtolangledif;
    Vector3 VTOL180 = new Vector3(0, 0.01f, -1);//used as a rotation target for VTOL adjustment. Slightly below directly backward so that rotatetowards rotates on the correct axis
    private bool GunRecoilEmptyNULL = true;
    [System.NonSerializedAttribute] public float ThrottleStrengthAB;
    [System.NonSerializedAttribute] public float FuelConsumptionAB;
    private Vector2 RStickCheckAngle;
    private Vector2 LStickCheckAngle;
    [System.NonSerializedAttribute] public float LStickFuncDegrees;
    [System.NonSerializedAttribute] public float RStickFuncDegrees;
    [System.NonSerializedAttribute] public int LStickNumFuncs;
    [System.NonSerializedAttribute] public int RStickNumFuncs;
    private bool VTolAngle90Plus;
    [System.NonSerializedAttribute] public bool[] LStickNULL;
    [System.NonSerializedAttribute] public bool[] RStickNULL;
    [System.NonSerializedAttribute] public bool AfterburnerOn;
    [System.NonSerializedAttribute] public bool PitchDown;//air is hitting plane from the top
    [System.NonSerializedAttribute] public int NumActiveFlares;
    [System.NonSerializedAttribute] public int NumActiveChaff;
    [System.NonSerializedAttribute] public int NumActiveOtherCM;
    //this stuff can be used by DFUNCs
    //if these == 0 then they are not disabled. Being an int allows more than one extension to disable it at a time
    [System.NonSerializedAttribute] public int SetConstantForceZero = 0;
    [System.NonSerializedAttribute] public int DisableGearToggle = 0;
    [System.NonSerializedAttribute] public int DisableTaxiRotation = 0;
    [System.NonSerializedAttribute] public int DisableGroundDetector = 0;


    [System.NonSerializedAttribute] public int ReSupplied = 0;

    private int AAMLAUNCHED_STRING = Animator.StringToHash("aamlaunched");
    private int RADARLOCKED_STRING = Animator.StringToHash("radarlocked");
    private int Lstickselection_STRING = Animator.StringToHash("Lstickselection");
    private int Rstickselection_STRING = Animator.StringToHash("Rstickselection");
    private int AFTERBURNERON_STRING = Animator.StringToHash("afterburneron");
    private int RESUPPLY_STRING = Animator.StringToHash("resupply");
    private int HOOKDOWN_STRING = Animator.StringToHash("hookdown");
    private int INSTANTGEARDOWN_STRING = Animator.StringToHash("instantgeardown");
    private int LOCALPILOT_STRING = Animator.StringToHash("localpilot");
    private int LOCALPASSENGER_STRING = Animator.StringToHash("localpassenger");
    private int OCCUPIED_STRING = Animator.StringToHash("occupied");
    private int REAPPEAR_STRING = Animator.StringToHash("reappear");

    //old Leavebutton Stuff
    [System.NonSerializedAttribute] public int PilotSeat = -1;
    [System.NonSerializedAttribute] public int MySeat = -1;
    [System.NonSerializedAttribute] public int[] SeatedPlayers;
    [System.NonSerializedAttribute] public VRCStation[] VehicleStations;
    [System.NonSerializedAttribute] public int[] InsidePlayers;
    private bool FindSeatsDone = false;
    //end of old Leavebutton stuff
    private void Start()
    {
        localPlayer = Networking.LocalPlayer;
        if (localPlayer == null)
        {
            InEditor = true;
            Piloting = true;
            InVehicleOnly.SetActive(true);//for testing in editor without cyanemu
        }
        else
        {
            InEditor = false;
            InVR = localPlayer.IsUserInVR();
        }

        PlaneHitDetector = VehicleMainObj.GetComponent<HitDetector>();
        VehicleTransform = VehicleMainObj.GetComponent<Transform>();
        VehicleRigidbody = VehicleMainObj.GetComponent<Rigidbody>();
        VehicleConstantForce = VehicleMainObj.GetComponent<ConstantForce>();
        WheelCollider[] wc = PlaneMesh.GetComponentsInChildren<WheelCollider>(true);
        if (wc.Length != 0) { HasWheelColliders = true; }

        if (AutoAdjustValuesToMass)
        {
            //values that should feel the same no matter the weight of the aircraft
            float RBMass = VehicleRigidbody.mass;
            ThrottleStrength *= RBMass;
            PitchStrength *= RBMass;
            PitchFriction *= RBMass;
            YawStrength *= RBMass;
            YawFriction *= RBMass;
            RollStrength *= RBMass;
            RollFriction *= RBMass;
            Lift *= RBMass;
            MaxLift *= RBMass;
            VelLiftMax *= RBMass;
            VelStraightenStrPitch *= RBMass;
            VelStraightenStrYaw *= RBMass;
            foreach (WheelCollider wheel in wc)
            {
                JointSpring SusiSpring = wheel.suspensionSpring;
                SusiSpring.spring *= RBMass;
                SusiSpring.damper *= RBMass;
                wheel.suspensionSpring = SusiSpring;
            }
        }

        Assert(VehicleMainObj != null, "Start: VehicleMainObj != null");
        Assert(PlaneMesh != null, "Start: PlaneMesh != null");
        Assert(CenterOfMass != null, "Start: CenterOfMass != null");
        Assert(PitchMoment != null, "Start: PitchMoment != null");
        Assert(YawMoment != null, "Start: YawMoment != null");
        Assert(GroundDetector != null, "Start: GroundDetector != null");
        Assert(GroundEffectEmpty != null, "Start: GroundEffectEmpty != null");
        Assert(GunRecoilEmpty != null, "Start: GunRecoilEmpty != null");
        Assert(KillsBoard != null, "Start: KillsBoard != null");

        Planelayer = PlaneMesh.gameObject.layer;//get the layer of the plane as set by the world creator
        OutsidePlaneLayer = PlaneMesh.gameObject.layer;
        VehicleAnimator = VehicleMainObj.GetComponent<Animator>();
        //set these values at start in case they haven't been set correctly in editor


        FullHealth = Health;
        FullFuel = Fuel;

        VelLiftMaxStart = VelLiftMax;
        VelLiftStart = VelLift;

        PitchThrustVecMultiStart = PitchThrustVecMulti;
        YawThrustVecMultiStart = YawThrustVecMulti;
        RollThrustVecMultiStart = RollThrustVecMulti;

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


        if (VTOLOnly || HasVTOLAngle) { VTOLenabled = true; }
        VTOL90Degrees = Mathf.Min(90 / VTOLMaxAngle, 1);

        if (!HasAfterburner) { ThrottleAfterburnerPoint = 1; }
        ThrottleNormalizer = 1 / ThrottleAfterburnerPoint;
        ABNormalizer = 1 / (1 - ThrottleAfterburnerPoint);

        FuelConsumptionAB = (FuelConsumption * FuelConsumptionABMulti) - FuelConsumption;
        ThrottleStrengthAB = (ThrottleStrength * AfterburnerThrustMulti) - ThrottleStrength;

        vtolangledif = VTOLMaxAngle - VTOLMinAngle;
        VTOLAngleDivider = VTOLAngleTurnRate / vtolangledif;
        VTOLAngle = VTOLAngleInput = VTOLDefaultValue;

        if (NoCanopy) { HasCanopy = false; }

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
        LowFuel = 200;//FullFuel * .13888888f;//to match the old default settings
        LowFuelDivider = 1 / LowFuel;

        //thrust is lerped towards VTOLThrottleStrengthMulti by VTOLAngle, unless VTOLMaxAngle is greater than 90 degrees, then it's lerped by 90=1
        VTolAngle90Plus = VTOLMaxAngle > 90;

        LStickNumFuncs = Dial_Functions_L.Length;
        RStickNumFuncs = Dial_Functions_R.Length;
        LStickFuncDegrees = 360 / (float)LStickNumFuncs;
        RStickFuncDegrees = 360 / (float)RStickNumFuncs;
        LStickNULL = new bool[LStickNumFuncs];
        RStickNULL = new bool[RStickNumFuncs];
        int u = 0;
        foreach (UdonSharpBehaviour usb in Dial_Functions_L)
        {
            if (usb == null) { LStickNULL[u] = true; }
            u++;
        }
        u = 0;
        foreach (UdonSharpBehaviour usb in Dial_Functions_R)
        {
            if (usb == null) { RStickNULL[u] = true; }
            u++;
        }
        //work out angle to check against for function selection because straight up is the middle of a function
        Vector3 angle = new Vector3(0, 0, -1);
        angle = Quaternion.Euler(0, -((360 / LStickNumFuncs) / 2), 0) * angle;
        LStickCheckAngle.x = angle.x;
        LStickCheckAngle.y = angle.z;

        angle = new Vector3(0, 0, -1);
        angle = Quaternion.Euler(0, -((360 / RStickNumFuncs) / 2), 0) * angle;
        RStickCheckAngle.x = angle.x;
        RStickCheckAngle.y = angle.z;

        TellDFUNCsLR();
        SendEventToExtensions("SFEXT_L_ECStart");
        if (InEditor)
        {
            PilotEnterPlaneLocal();
            PilotEnterPlaneGlobal(null);
        }
    }
    public void TouchDown()
    {
        Taxiing = true;
        SendEventToExtensions("SFEXT_G_TouchDown");
    }
    public void TakeOff()
    {
        Taxiing = false;
        SendEventToExtensions("SFEXT_G_TakeOff");
    }
    private void LateUpdate()
    {
        float DeltaTime = Time.deltaTime;
        if (!InEditor) { IsOwner = localPlayer.IsOwner(VehicleMainObj); }
        else { IsOwner = true; }

        if (IsOwner)//works in editor or ingame
        {
            if (DisableGroundDetector == 0 && DisableGroundDetector == 0 && Physics.Raycast(GroundDetector.position, -GroundDetector.up, .44f, 2049 /* Default and Environment */, QueryTriggerInteraction.Ignore))
            {//play a touchdown sound the frame we start taxiing
                if (Landed == false)
                {
                    Landed = true;
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "TouchDown");
                }
                Taxiing = true;
            }
            else
            {
                if (Landed == true)
                {
                    Landed = false;
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "TakeOff");
                }
                Taxiing = false;
            }

            if (!dead)
            {
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
            else { PlaneMoving = false; }

            if (Piloting)
            {
                Occupied = true;
                //gotta do these this if we're piloting but it didn't get done(specifically, hovering extremely slowly in a VTOL craft will cause control issues we don't)
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
                Vector2 LStickPos = new Vector2(0, 0);
                Vector2 RStickPos = new Vector2(0, 0);
                float LGrip = 0;
                float RGrip = 0;
                float LTrigger = 0;
                float RTrigger = 0;
                if (!InEditor)
                {
                    LStickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
                    LStickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
                    RStickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
                    RStickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
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

                if (VTOLenabled)
                {
                    if (!(VTOLAngle == VTOLAngleInput && VTOLAngleInput == 0) || VTOLOnly)//only SetVTOLValues if it'll do anything
                    { SetVTOLValues(); }
                }

                //LStick Selection wheel
                if (InVR && LStickPos.magnitude > .7f)
                {
                    float stickdir = Vector2.SignedAngle(LStickCheckAngle, LStickPos);

                    //R stick value is manually synced using events because i don't want to use too many synced variables.
                    //the value can be used in the animator to open bomb bay doors when bombs are selected, etc.
                    stickdir = (stickdir - 180) * -1;
                    int newselection = Mathf.FloorToInt(Mathf.Min(stickdir / LStickFuncDegrees, LStickNumFuncs - 1));
                    if (!LStickNULL[newselection])
                    { LStickSelection = newselection; }
                    //doing this in DFUNC scripts that need it instead so that we send less events
                    /*                     if (VehicleAnimator.GetInteger(Lstickselection_STRING) != LStickSelection)
                                        {
                                            LStickSetAnimatorInt();
                                        } */
                }

                //RStick Selection wheel
                if (InVR && RStickPos.magnitude > .7f)
                {
                    float stickdir = Vector2.SignedAngle(RStickCheckAngle, RStickPos);

                    //R stick value is manually synced using events because i don't want to use too many synced variables.
                    //the value can be used in the animator to open bomb bay doors when bombs are selected, etc.
                    stickdir = (stickdir - 180) * -1;
                    int newselection = Mathf.FloorToInt(Mathf.Min(stickdir / RStickFuncDegrees, RStickNumFuncs - 1));
                    if (!RStickNULL[newselection])
                    { RStickSelection = newselection; }
                    //doing this in DFUNC scripts that need it instead so that we send less events
                    /*                     if (VehicleAnimator.GetInteger(Rstickselection_STRING) != RStickSelection)
                                        {
                                            RStickSetAnimatorInt();
                                        } */
                }


                if (LStickSelection != LStickSelectionLastFrame)
                {
                    //new function selected, send deselected to old one
                    if (LStickSelectionLastFrame != -1 && Dial_Functions_L[LStickSelectionLastFrame] != null)
                    {
                        Dial_Functions_L[LStickSelectionLastFrame].SendCustomEvent("DFUNC_Deselected");
                    }
                    //get udonbehaviour for newly selected function and then send selected
                    if (LStickSelection > -1)
                    {
                        if (Dial_Functions_L[LStickSelection] != null)
                        {
                            Dial_Functions_L[LStickSelection].SendCustomEvent("DFUNC_Selected");
                        }
                        else { CurrentSelectedFunctionL = null; }
                    }
                }

                if (RStickSelection != RStickSelectionLastFrame)
                {
                    //new function selected, send deselected to old one
                    if (RStickSelectionLastFrame != -1 && Dial_Functions_R[RStickSelectionLastFrame] != null)
                    {
                        Dial_Functions_R[RStickSelectionLastFrame].SendCustomEvent("DFUNC_Deselected");
                    }
                    //get udonbehaviour for newly selected function and then send selected
                    if (RStickSelection > -1)
                    {
                        if (Dial_Functions_R[RStickSelection] != null)
                        {
                            Dial_Functions_R[RStickSelection].SendCustomEvent("DFUNC_Selected");
                        }
                        else { CurrentSelectedFunctionR = null; }
                    }
                }

                RStickSelectionLastFrame = RStickSelection;
                LStickSelectionLastFrame = LStickSelection;

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
                    JoystickGripLastFrame = false;
                }

                if (HasAfterburner)
                {
                    if (AfterburnerOn)
                    { PlayerThrottle = Mathf.Clamp(PlayerThrottle + ((Shifti - LeftControli) * .5f * DeltaTime), 0, 1f); }
                    else
                    { PlayerThrottle = Mathf.Clamp(PlayerThrottle + ((Shifti - LeftControli) * .5f * DeltaTime), 0, .8f); }
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
                    ThrottleGripLastFrame = false;
                }

                if (Taxiing)
                {
                    AngleOfAttack = 0;//prevent stall sound and aoavapor when on ground
                    Cruise = false;
                    AltHold = false;
                    if (DisableTaxiRotation == 0)
                    {
                        //rotate if trying to yaw
                        Taxiinglerper = Mathf.Lerp(Taxiinglerper, RotationInputs.y * TaxiRotationSpeed * Time.smoothDeltaTime, TaxiRotationResponse * DeltaTime);
                        VehicleTransform.Rotate(Vector3.up, Taxiinglerper);
                    }

                    StillWindMulti = Mathf.Min(Speed / 10, 1);
                    ThrustVecGrounded = 0;
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
                if (Cruise && !ThrottleGripLastFrame && !Shift && !Ctrl)
                {
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
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetAfterburnerOn");
                    }
                    else if (ThrottleInput <= ThrottleAfterburnerPoint && AfterburnerOn)
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetAfterburnerOff");
                    }
                }
                if (AltHold && !JoystickGripLastFrame)//alt hold enabled, and player not holding joystick
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
                    //'-input' are used by effectscontroller, and multiplied by 'strength' for final values
                    if (FlightLimitsEnabled && !Taxiing && AngleOfAttack < AoALimiter)//flight limits are enabled
                    {
                        float GLimitStrength = Mathf.Clamp(-(Gs / GLimiter) + 1, 0, 1);
                        float AoALimitStrength = Mathf.Clamp(-(Mathf.Abs(AngleOfAttack) / AoALimiter) + 1, 0, 1);
                        float Limits = Mathf.Min(GLimitStrength, AoALimitStrength);
                        RotationInputs.x = Mathf.Clamp(/*(MouseY * mouseysens + Lstick.y + */VRPitchRoll.y + Wi + Si + downi + upi, -1, 1) * Limits;
                        RotationInputs.y = Mathf.Clamp(Qi + Ei + JoystickPosYaw.x, -1, 1) * Limits;
                    }
                    else//player is in full control
                    {
                        RotationInputs.x = Mathf.Clamp(/*(MouseY * mouseysens + Lstick.y + */VRPitchRoll.y + Wi + Si + downi + upi, -1, 1);
                        RotationInputs.y = Mathf.Clamp(Qi + Ei + JoystickPosYaw.x, -1, 1);
                    }
                    //roll isn't subject to flight limits
                    RotationInputs.z = Mathf.Clamp(((/*(MouseX * mousexsens) + */VRPitchRoll.x + Ai + Di + lefti + righti) * -1), -1, 1);
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

                //wheel colliders are broken, this workaround stops the plane from being 'sticky' when you try to start moving it. Heard it doesn't happen so bad if rigidbody weight is much higher.
                if (Speed < .2 && HasWheelColliders && ThrottleInput > 0)
                {
                    if (VTOLAngle > VTOL90Degrees)
                    { VehicleRigidbody.velocity = VehicleTransform.forward * -.25f; }
                    else
                    { VehicleRigidbody.velocity = VehicleTransform.forward * .25f; }
                }
                //Replacement for leavebutton
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("Oculus_CrossPlatform_Button4"))
                {
                    ExitStation();
                }
            }
            else
            {
                Occupied = false;
                //brake is always on if the plane is on the ground
                if (Taxiing)
                {
                    StillWindMulti = Mathf.Min(Speed / 10, 1);
                }
                else { StillWindMulti = 1; }
            }

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

                //ExtraDrag = (GearDrag + FlapsDrag + (BrakeInput * AirbrakeStrength)) - 1;//combine these so we don't have to do as much in fixedupdate
            }
            else
            {
                VelLift = pitch = yaw = roll = 0;
            }

            if ((PlaneMoving || Piloting) && SetConstantForceZero == 0)
            {
                //Create a Vector3 Containing the thrust, and rotate and adjust strength based on VTOL value
                //engine output is multiplied so that max throttle without afterburner is max strength (unrelated to vtol)
                Vector3 FinalInputAcc;
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
                                                                                 //rotate and scale Vector for VTOL thrust
                    if (VTOLOnly)//just use regular thrust strength if vtol only, as there should be no transition to plane flight
                    {
                        FinalInputAcc = Vector3.RotateTowards(Vector3.forward, VTOL180, VTOLAngle2 * Mathf.Deg2Rad, 0) * Thrust;
                    }
                    else//vehicle can transition from plane-like flight to helicopter-like flight, with different thrust values for each, with a smooth transition between them
                    {
                        float downthrust = Thrust * VTOLThrottleStrengthMulti;
                        FinalInputAcc = Vector3.RotateTowards(Vector3.forward, VTOL180, VTOLAngle2 * Mathf.Deg2Rad, 0) * Mathf.Lerp(Thrust, Thrust * VTOLThrottleStrengthMulti, VTolAngle90Plus ? VTOLAngle90 : VTOLAngle);
                    }
                    //add ground effect to the VTOL thrust
                    GroundEffectAndVelLift = GroundEffect(true, GroundEffectEmpty.position, -VehicleTransform.TransformDirection(FinalInputAcc), VTOLGroundEffectStrength, 1);
                    FinalInputAcc *= GroundEffectAndVelLift;

                    //Add Airplane Ground Effect
                    GroundEffectAndVelLift = GroundEffect(false, GroundEffectEmpty.position, -VehicleTransform.up, GroundEffectStrength, SpeedLiftFactor);
                    //add lift and thrust
                    FinalInputAcc += new Vector3(-sidespeed * SidewaysLift * SpeedLiftFactor * AoALiftYaw * Atmosphere,// X Sideways
                        ((downspeed * ExtraLift * PitchDownLiftMulti * SpeedLiftFactor * AoALiftPitch) + GroundEffectAndVelLift) * Atmosphere,// Y Up
                            0);
                }
                else//Simpler version for non-VTOL craft
                {
                    GroundEffectAndVelLift = GroundEffect(false, GroundEffectEmpty.position, -VehicleTransform.up, GroundEffectStrength, SpeedLiftFactor);
                    FinalInputAcc = new Vector3(-sidespeed * SidewaysLift * SpeedLiftFactor * AoALiftYaw * Atmosphere,// X Sideways
                        ((downspeed * ExtraLift * PitchDownLiftMulti * SpeedLiftFactor * AoALiftPitch) + GroundEffectAndVelLift) * Atmosphere,// Y Up
                            Thrust);// Z Forward);
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

            SoundBarrier = (-Mathf.Clamp(Mathf.Abs(Speed - 343) / SoundBarrierWidth, 0, 1) + 1) * SoundBarrierStrength;
        }
        else//non-owners need to know these values
        {
            Speed = AirSpeed = CurrentVel.magnitude;//wind speed is local anyway, so just use ground speed for non-owners
            rotlift = Mathf.Min(Speed / RotMultiMaxSpeed, 1);//so passengers can hear the airbrake
                                                             //VRChat doesn't set Angular Velocity to 0 when you're not the owner of a rigidbody,
                                                             //causing spazzing, the script handles angular drag it itself, so when we're not owner of the plane, set this value non-zero to stop spazzing
            VehicleRigidbody.angularDrag = .5f;
            //AirVel = VehicleRigidbody.velocity - Wind;//wind isn't synced so this will be wrong
            //AirSpeed = AirVel.magnitude;

        }
        if (Passenger)
        {
            //Replacement for leavebuttons below this point
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("Oculus_CrossPlatform_Button4"))
            { ExitStation(); }
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
            Gs = Vector3.Distance(LastFrameVel, VehicleVel) / gravity;
            LastFrameVel = VehicleVel;
        }
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
    public void Explode()//all the things players see happen when the vehicle explodes
    {
        dead = true;
        Cruise = false;
        PlayerThrottle = 0;
        ThrottleInput = 0;
        EngineOutput = 0;
        MissilesIncomingHeat = 0;
        MissilesIncomingRadar = 0;
        MissilesIncomingOther = 0;
        VTOLAngle = VTOLDefaultValue;
        VTOLAngleInput = VTOLDefaultValue;
        if (HasAfterburner) { SetAfterburnerOff(); }
        if (HasLimits) { SetLimitsOn(); }
        Fuel = FullFuel;
        Atmosphere = 1;//planemoving optimization requires this to be here
        VehicleAnimator.SetInteger(Lstickselection_STRING, -1);
        VehicleAnimator.SetInteger(Rstickselection_STRING, -1);

        SendCustomEventDelayedSeconds("ReAppear", RespawnDelay);
        SendCustomEventDelayedSeconds("NotDead", RespawnDelay + InvincibleAfterSpawn);
        SendEventToExtensions("SFEXT_G_Explode");

        if (IsOwner)
        {
            VehicleRigidbody.velocity = Vector3.zero;
            Health = FullHealth;//turns off low health smoke
            Fuel = FullFuel;
            AoALiftPitch = 0;
            AoALiftYaw = 0;
            AngleOfAttack = 0;
            VelLift = VelLiftStart;
            VTOLAngle90 = 0;
            SendCustomEventDelayedSeconds("MoveToSpawn", RespawnDelay - 3);

            SendEventToExtensions("SFEXT_O_Explode");
        }

        //our killer increases their kills
        float time = Time.time;
        if (PlaneHitDetector.LastAttacker != null && (time - PlaneHitDetector.LastHitTime) < 5 && !Taxiing && ((time - PilotExitTime) < 5 || Occupied))
        {
            SendEventToExtensions("SFEXT_O_GotKilled");
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
            ExitStation();
        }
    }
    public void ReAppear()
    {
        VehicleAnimator.SetTrigger("reappear");
    }
    public void NotDead()
    {
        Health = FullHealth;
        dead = false;
    }
    public void MoveToSpawn()
    {
        PlayerThrottle = 0;//for editor test mode
        EngineOutput = 0;//^
        //these could get set after death by lag, probably
        MissilesIncomingHeat = 0;
        MissilesIncomingRadar = 0;
        MissilesIncomingOther = 0;
        if (IsOwner)
        {
            Health = FullHealth;
            VehicleObjectSync.Respawn();//this works if done just locally;
            if (IsOwner)
            {
                SendEventToExtensions("SFEXT_O_MoveToSpawn");
            }
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
        SendEventToExtensions("SFEXT_O_GotAKill");
    }
    public void SetAfterburnerOn()
    {
        AfterburnerOn = true;
        VehicleAnimator.SetBool(AFTERBURNERON_STRING, true);

        if (IsOwner)
        {
            SendEventToExtensions("SFEXT_O_AfterburnerOn");
        }
    }
    public void SetAfterburnerOff()
    {
        AfterburnerOn = false;

        VehicleAnimator.SetBool(AFTERBURNERON_STRING, false);

        if (IsOwner)
        {
            SendEventToExtensions("SFEXT_O_AfterburnerOff");
        }
    }
    private void ToggleAfterburner()
    {
        if (!AfterburnerOn && ThrottleInput > ThrottleAfterburnerPoint)
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
        ReSupplied = 0;//used to know if other scripts resupplied
        if ((Fuel < FullFuel - 10 || Health != FullHealth))
        {
            ReSupplied += 1;//used to only play the sound if we're actually repairing/getting ammo/fuel
        }
        SendEventToExtensions("SFEXT_G_ReSupply");//extensions increase the ReSupplied value too

        LastResupplyTime = Time.time;

        if (IsOwner)
        {
            Fuel = Mathf.Min(Fuel + (FullFuel / RefuelTime), FullFuel);
            Health = Mathf.Min(Health + (FullHealth / RepairTime), FullHealth);
        }
        VehicleAnimator.SetTrigger(RESUPPLY_STRING);
    }
    public void ResupplyPlane_FuelOnly()//not done and unused
    {
        ReSupplied = 0;//used to know if other scripts resupplied
        if (IsOwner)
        { SendEventToExtensions("SFEXT_O_ReFuel"); }


        Fuel = Mathf.Min(Fuel + (FullFuel / RefuelTime), FullFuel);
    }
    public void SetLimitsOn()
    {
        FlightLimitsEnabled = true;

        if (IsOwner)
        {
            SendEventToExtensions("SFEXT_O_LimitsOn");
        }
    }
    public void SetLimitsOff()
    {
        FlightLimitsEnabled = false;

        if (IsOwner)
        {
            SendEventToExtensions("SFEXT_O_LimitsOff");
        }
    }
    public void ToggleLimits()
    {
        if (!FlightLimitsEnabled)
        {
            if (VTOLAngle != VTOLDefaultValue) return;
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
        //VehicleTransform.position = new Vector3(VehicleTransform.position.x, -10000, VehicleTransform.position.z);
        Atmosphere = 1;//planemoving optimization requires this to be here
                       //synced variables
        Health = FullHealth;
        Fuel = FullFuel;
        VTOLAngle = VTOLDefaultValue;
        VTOLAngleInput = VTOLDefaultValue;
        VehicleObjectSync.Respawn();//this works if done just locally


        TakeOwnerShipOfExtensions();
        SendEventToExtensions("SFEXT_O_RespawnButton");
    }
    public void ResetStatus()//called globally when using respawn button
    {
        if (HasAfterburner) { SetAfterburnerOff(); }
        if (HasLimits) { SetLimitsOn(); }
        //these two make it invincible and unable to be respawned again for 5s
        dead = true;


        SendEventToExtensions("SFEXT_G_RespawnButton");
    }
    public void PlaneHit()
    {
        if (InEditor || IsOwner)
        {
            Health -= 10;
        }
        if (IsOwner)
        {
            SendEventToExtensions("SFEXT_O_PlaneHit");
        }
    }
    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        //Owner sends events to sync the plane so late joiners don't see it flying with it's canopy open and stuff
        //only change things that aren't in the default state
        //only change effects which are very visible, this is just so that it looks alright for late joiners, not to sync everything perfectly.
        //syncing everything perfectly would probably require too many events to be sent.
        //planes will be fully synced when they explode or are respawned anyway.
        SendEventToExtensions("SFEXT_O_PlayerJoined");
    }
    public void PassengerEnterPlaneLocal()
    {
        Passenger = true;
        if (InVehicleOnly != null) { InVehicleOnly.SetActive(true); }

        VehicleAnimator.SetBool(LOCALPASSENGER_STRING, true);
        SetPlaneLayerInside();

        SendEventToExtensions("SFEXT_P_PassengerEnter");
    }
    public void PassengerExitPlaneLocal()
    {
        Passenger = false;
        if (InVehicleOnly != null) { InVehicleOnly.SetActive(false); }
        localPlayer.SetVelocity(CurrentVel);
        MissilesIncomingHeat = 0;
        MissilesIncomingRadar = 0;
        MissilesIncomingOther = 0;
        VehicleAnimator.SetInteger("missilesincoming", 0);
        VehicleAnimator.SetBool("localpassenger", false);

        SetPlaneLayerOutside();

        SendEventToExtensions("SFEXT_P_PassengerExit");
    }
    public void PassengerEnterPlaneGlobal()
    {
        SendEventToExtensions("SFEXT_G_PassengerEnter");
    }
    public void PassengerExitPlaneGlobal()
    {
        SendEventToExtensions("SFEXT_G_PassengerExit");
    }
    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        if (player.isLocal)
        {
            VehicleRigidbody.velocity = CurrentVel;
            SetOwnerships();
            SendEventToExtensions("SFEXT_O_TakeOwnership");
        }
        else
        {
            if (IsOwner)
            {
                SendEventToExtensions("SFEXT_O_LoseOwnership");
            }
            else
            {
                SendEventToExtensions("SFEXT_O_OwnershipTransfer");
            }
        }
    }
    public void SetOwnerships()
    {
        if (!localPlayer.IsOwner(gameObject)) { Networking.SetOwner(localPlayer, gameObject); }
        if (!localPlayer.IsOwner(VehicleMainObj)) { Networking.SetOwner(localPlayer, VehicleMainObj); }
        foreach (UdonSharpBehaviour obj in ExtensionUdonBehaviours)
        {
            if (obj != null && !localPlayer.IsOwner(obj.gameObject)) { Networking.SetOwner(localPlayer, obj.gameObject); }
        }
        IsOwner = true;
    }
    public void PilotEnterPlaneLocal()//called from PilotSeat
    {
        //setting this as a workaround because it doesnt work reliably in Start()
        if (!InEditor)
        {
            if (localPlayer.IsUserInVR()) { InVR = true; }

            Networking.SetOwner(localPlayer, gameObject);
            SetOwnerships();
        }
        if (InVehicleOnly != null) { InVehicleOnly.SetActive(true); }
        if (PilotOnly != null) { PilotOnly.SetActive(true); }

        EngineOutput = 0;
        ThrottleInput = 0;
        PlayerThrottle = 0;
        VehicleRigidbody.angularDrag = 0;//set to something nonzero when you're not owner to prevent juddering motion on collisions
        VTOLAngleInput = VTOLAngle;

        Piloting = true;
        if (dead) { Health = FullHealth; }//dead is true for the first 5 seconds after spawn, this might help with spontaneous explosions

        VehicleAnimator.SetBool(LOCALPILOT_STRING, true);

        //hopefully prevents explosions when you enter the plane
        VehicleRigidbody.velocity = CurrentVel;
        Gs = 0;
        LastFrameVel = CurrentVel;

        SetPlaneLayerInside();


        if (KillsBoard != null)
        {
            KillsBoard.MyKills = 0;
        }

        TakeOwnerShipOfExtensions();
        SendEventToExtensions("SFEXT_O_PilotEnter");
    }
    public void PilotEnterPlaneGlobal(VRCPlayerApi player)
    {
        if (player != null)
        {
            PilotName = player.displayName;
            PilotID = player.playerId;
        }

        VehicleAnimator.SetBool(OCCUPIED_STRING, true);
        dead = false;//Plane stops being invincible if someone gets in, also acts as redundancy incase someone missed the notdead respawn event
        SendEventToExtensions("SFEXT_G_PilotEnter");
    }
    public void PilotExitPlane(VRCPlayerApi player)
    {
        PilotExitTime = Time.time;
        PilotName = string.Empty;
        PilotID = -1;
        SetAfterburnerOff();

        SendEventToExtensions("SFEXT_G_PilotExit");
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
            Taxiinglerper = 0;
            ThrottleGripLastFrame = false;
            JoystickGripLastFrame = false;
            LStickSelection = -1;
            RStickSelection = -1;
            LStickSelectionLastFrame = -1;
            RStickSelectionLastFrame = -1;
            LTriggerLastFrame = false;
            RTriggerLastFrame = false;
            DoAAMTargeting = false;
            MissilesIncomingHeat = 0;
            MissilesIncomingRadar = 0;
            MissilesIncomingOther = 0;
            VehicleAnimator.SetBool(LOCALPILOT_STRING, false);
            localPlayer.SetVelocity(CurrentVel);

            if (InVehicleOnly != null) { InVehicleOnly.SetActive(false); }
            if (PilotOnly != null) { PilotOnly.SetActive(false); }

            //set plane's layer back
            SetPlaneLayerOutside();

            SendEventToExtensions("SFEXT_O_PilotExit");
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
            AAMTargets[0] = gameObject;//this should prevent HUDController from crashing with a null reference while causing no ill effects
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
    public void TellDFUNCsLR()
    {
        foreach (UdonSharpBehaviour EXT in Dial_Functions_L)
        {
            if (EXT != null)
            { EXT.SendCustomEvent("DFUNC_LeftDial"); }
        }
        foreach (UdonSharpBehaviour EXT in Dial_Functions_R)
        {
            if (EXT != null)
            { EXT.SendCustomEvent("DFUNC_RightDial"); }
        }
    }
    public void TakeOwnerShipOfExtensions()
    {
        if (!InEditor)
        {
            foreach (UdonSharpBehaviour EXT in ExtensionUdonBehaviours)
            { if (EXT != null) { if (!localPlayer.IsOwner(EXT.gameObject)) { Networking.SetOwner(localPlayer, EXT.gameObject); } } }
            foreach (UdonSharpBehaviour EXT in Dial_Functions_L)
            { if (EXT != null) { if (!localPlayer.IsOwner(EXT.gameObject)) { Networking.SetOwner(localPlayer, EXT.gameObject); } } }
            foreach (UdonSharpBehaviour EXT in Dial_Functions_R)
            { if (EXT != null) { if (!localPlayer.IsOwner(EXT.gameObject)) { Networking.SetOwner(localPlayer, EXT.gameObject); } } }
        }
    }
    public void SendEventToExtensions(string eventname)
    {
        foreach (UdonSharpBehaviour EXT in ExtensionUdonBehaviours)
        {
            if (EXT != null)
            { EXT.SendCustomEvent(eventname); }
        }
        foreach (UdonSharpBehaviour EXT in Dial_Functions_L)
        {
            if (EXT != null)
            { EXT.SendCustomEvent(eventname); }
        }
        foreach (UdonSharpBehaviour EXT in Dial_Functions_R)
        {
            if (EXT != null)
            { EXT.SendCustomEvent(eventname); }
        }
    }

    public void ExitStation()
    {
        VehicleStations[MySeat].ExitStation(localPlayer);
    }
    public void FindSeats()
    {
        if (FindSeatsDone) { return; }
        VehicleStations = (VRC.SDK3.Components.VRCStation[])VehicleMainObj.GetComponentsInChildren(typeof(VRC.SDK3.Components.VRCStation));
        SeatedPlayers = new int[VehicleStations.Length];
        for (int i = 0; i != SeatedPlayers.Length; i++)
        {
            SeatedPlayers[i] = -1;
        }
        FindSeatsDone = true;
    }
    public Vector2 UnpackThrottles(float Throttle)
    {
        //x = throttle amount, y = afterburner amount
        return new Vector2(Mathf.Min(Throttle, ThrottleAfterburnerPoint) * ThrottleNormalizer,
        Mathf.Max((Mathf.Max(Throttle, ThrottleAfterburnerPoint) - ThrottleAfterburnerPoint) * ABNormalizer, 0));
    }
    //these can be used for syncing weapon selection for bomb bay doors animation etc
    public void RemoveOPtherCM()
    {
        NumActiveFlares--;
    }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogWarning("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}