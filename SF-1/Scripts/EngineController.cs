
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class EngineController : UdonSharpBehaviour
{
    public GameObject VehicleMainObj;
    public LeaveVehicleButton[] LeaveButtons;
    public EffectsController EffectsControl;
    public SoundController SoundControl;
    public HUDController HUDControl;
    public Transform CenterOfMass;
    public Transform PitchMoment;
    public Transform YawMoment;
    public Transform GroundDetector;
    public Transform HookDetector;
    public Camera AGMCam;
    public LayerMask ResupplyLayer;
    public LayerMask HookCableLayer;
    public Transform CatapultDetector;
    public LayerMask CatapultLayer;
    public GameObject AAM;
    [UdonSynced(UdonSyncMode.None)] public int NumAAM;
    public float AAMMaxTargetDistance;
    public Transform AAMLaunchPoint;
    public LayerMask AAMTargetsLayer;
    public GameObject AGM;
    [UdonSynced(UdonSyncMode.None)] public int NumAGM;
    public Transform AGMLaunchPoint;
    public LayerMask AGMTargetsLayer;
    [UdonSynced(UdonSyncMode.None)] public float GunAmmoInSeconds;
    public bool HasCruise = true;
    public bool HasLimits = true;
    public bool HasCatapult = true;
    public bool HasHook = true;
    public bool HasBrake = true;
    public bool HasTRIM = true;
    public bool HasCanopy = true;
    public bool HasAfterBurner = true;
    public bool HasGun = true;
    public bool HasAAM = true;
    public bool HasAGM = true;
    public bool HasAltHold = true;
    public bool HasGear = true;
    public bool HasFlaps = true;
    public bool HasSmoke = true;
    public bool HasFlare = true;
    public bool CanCatapult = true;
    public float ThrottleStrength = 20f;
    public float AfterburnerThrustMulti = 1.5f;
    public float AccelerationResponse = 4.5f;
    public float EngineSpoolDownSpeedMulti = .5f;
    public float AirFriction = 0.0004f;
    public float PitchStrength = 5f;
    public float PitchThrustVecMulti = 0f;
    public float PitchFriction = 24f;
    public float PitchResponse = 12f;
    public float ReversingPitchStrengthMulti = 2;
    public float YawStrength = 3f;
    public float YawThrustVecMulti = 0f;
    public float YawFriction = 15f;
    public float YawResponse = 12f;
    public float ReversingYawStrengthMulti = 2.4f;
    public float RollStrength = 450f;
    public float RollThrustVecMulti = 0f;
    public float RollFriction = 90f;
    public float RollResponse = 12f;
    public float ReversingRollStrengthMulti = 1.6f;//reversing = AoA > 90
    public float PitchDownStrMulti = .8f;
    public float PitchDownLiftMulti = .8f;
    public float RotMultiMaxSpeed = 220f;
    //public float StickInputPower = 1.7f;
    public float VelStraightenStrPitch = 0.035f;
    public float VelStraightenStrYaw = 0.045f;
    public float MaxAngleOfAttackPitch = 25f;
    public float MaxAngleOfAttackYaw = 40f;
    public float AoaCurveStrength = 2f;//1 = linear, >1 = convex, <1 = concave
    public float HighAoaMinControlPitch = 0.2f;
    public float HighAoaMinControlYaw = 0.2f;
    public float HighPitchAoaMinLift = 0.2f;
    public float HighYawAoaMinLift = 0.2f;
    public float TaxiRotationSpeed = 35f;
    public float TaxiRotationResponse = 2.5f;
    public float Lift = 0.00015f;
    public float SidewaysLift = .17f;
    public float MaxLift = 10f;
    public float VelLift = 1f;
    public float MaxGs = 25f;
    public float GDamage = 5f;
    public float LandingGearDragMulti = 1.3f;
    public float FlapsDragMulti = 1.4f;
    public float FlapsLiftMulti = 1.35f;
    public float AirbrakeStrength = 4f;
    public float GroundBrakeStrength = 4f;
    public float HookedBrakeStrength = 65f;
    public float CatapultLaunchStrength = 50f;
    public float CatapultLaunchTime = 2f;
    public float TakeoffAssist = 5f;
    public float TakeoffAssistSpeed = 50f;
    public float GLimiter = 12f;
    public float AoALimit = 15f;
    public float CanopyCloseTime = 1.8f;
    [UdonSynced(UdonSyncMode.None)] public float Health = 23f;
    public float SeaLevel = -10f;
    public Vector3 Wind;
    public float WindGustStrength = 15;
    public float WindGustiness = 0.03f;
    public float WindTurbulanceScale = 0.0001f;
    public float SoundBarrierStrength = 0.0003f;
    public float SoundBarrierWidth = 20f;


    //best to remove synced variables if you aren't using them
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public float AirBrakeInput;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public float Throttle = 0f;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public Vector3 CurrentVel = new Vector3(0, 0, 0);
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public float Gs = 1f;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public float AngleOfAttack;//MAX of yaw & pitch aoa //used by effectscontroller
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public int AAMTarget = 0;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public Vector3 SmokeColor = new Vector3(1, 1, 1);
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool IsFiringGun = false;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool Occupied = false; //this is true if someone is sitting in pilot seat
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public Vector3 AGMTarget;



    [System.NonSerializedAttribute] [HideInInspector] public bool FlightLimitsEnabled = true;
    [System.NonSerializedAttribute] [HideInInspector] public ConstantForce VehicleConstantForce;
    [System.NonSerializedAttribute] [HideInInspector] public Rigidbody VehicleRigidbody;
    [System.NonSerializedAttribute] [HideInInspector] public Color SmokeColor_Color;
    private float LerpedRoll;
    private float LerpedPitch;
    private float LerpedYaw;
    private Vector2 LStick;
    private Vector2 RStick;
    [System.NonSerializedAttribute] [HideInInspector] public int LStickSelection = 0;
    [System.NonSerializedAttribute] [HideInInspector] public int RStickSelection = 0;
    private Vector2 VRPitchRollInput;
    private float LGrip;
    [System.NonSerializedAttribute] [HideInInspector] public bool LGripLastFrame = false;
    private float LTrigger;
    private float RTrigger;
    [System.NonSerializedAttribute] [HideInInspector] public bool LTriggerLastFrame = false;
    [System.NonSerializedAttribute] [HideInInspector] public bool RTriggerLastFrame = false;
    Vector3 JoystickPos;
    Vector3 JoystickPosYaw;
    Quaternion PlaneRotDif;
    Quaternion JoystickDifference;
    Quaternion JoystickZeroPoint;
    Quaternion PlaneRotLastFrame;
    private float ThrottleDifference;
    [System.NonSerializedAttribute] [HideInInspector] public float PlayerThrottle;
    private float TempThrottle;
    private float handpos;
    private float ThrottleZeroPoint;
    private float TempSpeed;
    private float TempZoom;
    private float ZoomZeroPoint;
    private float ZoomDifference;
    [System.NonSerializedAttribute] [HideInInspector] public float SetSpeed;
    private float SpeedZeroPoint;
    private float SmokeHoldTime;
    private bool SetSmokeLastFrame;
    private Vector3 HandPosSmoke;
    private Vector3 SmokeZeroPoint;
    private float EjectZeroPoint;
    [System.NonSerializedAttribute] [HideInInspector] public float EjectTimer = 1;
    [System.NonSerializedAttribute] [HideInInspector] public bool Ejected = false;
    [System.NonSerializedAttribute] [HideInInspector] public float LTriggerTapTime = 1;
    [System.NonSerializedAttribute] [HideInInspector] public float RTriggerTapTime = 1;
    private bool DoTrim;
    private Vector3 HandPosTrim;
    private Vector3 TrimZeroPoint;
    private Vector2 TempTrim;
    private Vector2 TrimDifference;
    [System.NonSerializedAttribute] [HideInInspector] public Vector2 Trim;
    private float RGrip;
    [System.NonSerializedAttribute] [HideInInspector] public bool RGripLastFrame = false;
    private float downspeed;
    private float sidespeed;
    [System.NonSerializedAttribute] [HideInInspector] public float ThrottleInput = 0f;
    private float roll = 0f;
    private float pitch = 0f;
    private float yaw = 0f;
    [System.NonSerializedAttribute] [HideInInspector] public float FullHealth;
    [System.NonSerializedAttribute] [HideInInspector] public bool Taxiing = false;
    [System.NonSerializedAttribute] [HideInInspector] public float RollInput = 0f;
    [System.NonSerializedAttribute] [HideInInspector] public float PitchInput = 0f;
    [System.NonSerializedAttribute] [HideInInspector] public float YawInput = 0f;
    [System.NonSerializedAttribute] [HideInInspector] public bool Piloting = false;
    [System.NonSerializedAttribute] [HideInInspector] public bool InEditor = true;
    [System.NonSerializedAttribute] [HideInInspector] public bool InVR = false;
    [System.NonSerializedAttribute] [HideInInspector] public bool Passenger = false;
    [System.NonSerializedAttribute] [HideInInspector] public Vector3 LastFrameVel = new Vector3(1, 0, 1);
    [System.NonSerializedAttribute] [HideInInspector] public VRCPlayerApi localPlayer;
    [System.NonSerializedAttribute] [HideInInspector] public bool dead = false;
    [System.NonSerializedAttribute] [HideInInspector] public float AtmoshpereFadeDistance;
    [System.NonSerializedAttribute] [HideInInspector] public float AtmosphereHeightThing;
    public float AtmosphereThinningStart = 12192f; //40,000 feet
    public float AtmosphereThinningEnd = 19812; //65,000 feet
    private float Atmosphere;
    private float rotlift;
    [System.NonSerializedAttribute] [HideInInspector] public float AngleOfAttackPitch;
    [System.NonSerializedAttribute] [HideInInspector] public float AngleOfAttackYaw;
    private float AoALiftYaw;
    private float AoALiftPitch;
    private Vector3 Pitching;
    private Vector3 Yawing;
    [System.NonSerializedAttribute] [HideInInspector] public float Taxiinglerper;
    private int Wf;
    private int Sf;
    private int Af;
    private int Df;
    private int Qf;
    private int Ef;
    private int Bf;
    private int upf;
    private int downf;
    private int leftf;
    private int rightf;
    private int Shiftf;
    private int LeftControlf;
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
    [System.NonSerializedAttribute] [HideInInspector] public bool Cruise;
    private float CruiseProportional = .1f;
    private float CruiseIntegral = .1f;
    private float CruiseIntegrator;
    private float CruiseIntegratorMax = 5;
    private float CruiseIntegratorMin = -5;
    private float Cruiselastframeerror;
    private float AltHoldPitchProportional = 1f;
    private float AltHoldPitchIntegral = 1f;
    private float AltHoldPitchIntegrator;
    private float AltHoldPitchIntegratorMax = .05f;
    private float AltHoldPitchIntegratorMin = -.05f;
    private float AltHoldPitchDerivative = 4;
    private float AltHoldPitchDerivator;
    private float AltHoldPitchlastframeerror;
    private float AltHoldRollProportional = -.005f;
    [System.NonSerializedAttribute] [HideInInspector] public bool LevelFlight;
    [System.NonSerializedAttribute] [HideInInspector] public float Hooked = -1f;
    private Vector3 HookedLoc;
    private Vector3 TempSmokeCol = Vector3.zero;
    [System.NonSerializedAttribute] [HideInInspector] public float Speed;
    [System.NonSerializedAttribute] [HideInInspector] public float AirSpeed;
    [System.NonSerializedAttribute] [HideInInspector] public bool IsOwner = false;
    private Vector3 FinalWind;//incl. Gusts
    [System.NonSerializedAttribute] [HideInInspector] public Vector3 AirVel;
    private float StillWindMulti;
    private int ThrustVecGrounded;
    private float SoundBarrier;
    [System.NonSerializedAttribute] [HideInInspector] private float Afterburner = 1;
    [System.NonSerializedAttribute] [HideInInspector] public int CatapultStatus = 0;
    private Vector3 CatapultLockPos;
    private Quaternion CatapultLockRot;
    private float StartPitchStrength;
    [System.NonSerializedAttribute] [HideInInspector] public float CanopyCloseTimer = -100000;
    [UdonSynced(UdonSyncMode.None)] public float Fuel = 7200;
    public float FuelConsumption = 2;
    public float FuelConsumptionABMulti = 4.4f;
    [System.NonSerializedAttribute] [HideInInspector] public GameObject[] AAMTargets = new GameObject[80];
    [System.NonSerializedAttribute] [HideInInspector] public int NumAAMTargets = 0;
    private int AAMTargetChecker = 0;
    [System.NonSerializedAttribute] [HideInInspector] public bool AAMHasTarget = false;
    [System.NonSerializedAttribute] [HideInInspector] public bool AAMLock = false;
    private float AAMLockTime = 1.5f;
    private float AAMLockTimer = 0;
    private float AAMLockAngle;
    private float AALastFiredTime;
    [System.NonSerializedAttribute] [HideInInspector] public Vector3 AAMCurrentTargetDirection;
    [System.NonSerializedAttribute] [HideInInspector] public float FullFuel;
    [System.NonSerializedAttribute] [HideInInspector] public bool AGMLocked;
    [System.NonSerializedAttribute] [HideInInspector] private int AGMUnlocking = 0;
    [System.NonSerializedAttribute] [HideInInspector] private float AGMUnlockTimer;
    [System.NonSerializedAttribute] [HideInInspector] public bool AAMLaunchOpositeSide = false;
    [System.NonSerializedAttribute] [HideInInspector] public bool AGMLaunchOpositeSide = false;
    [System.NonSerializedAttribute] [HideInInspector] public float AGMRotDif;
    private Quaternion AGMCamRotSlerper;
    private bool ResupplyingLastFrame = false;
    private float LastResupplyTime = 0;
    [System.NonSerializedAttribute] [HideInInspector] public int FullAAMs;
    [System.NonSerializedAttribute] [HideInInspector] public int FullAGMs;
    [System.NonSerializedAttribute] [HideInInspector] public float FullGunAmmo;
    private int PilotingInt;//1 if piloting 0 if not
    //float MouseX;
    //float MouseY;
    //float mouseysens = 1; //mouse input can't be used because it's used to look around even when in a seat
    //float mousexsens = 1;
    private void Start()
    {
        Assert(VehicleMainObj != null, "Start: VehicleMainObj != null");
        Assert(LeaveButtons.Length > 0, "Start: Leavebutton Set");
        Assert(EffectsControl != null, "Start: EffectsControl != null");
        Assert(SoundControl != null, "Start: SoundControl != null");
        Assert(HUDControl != null, "Start: HUDControl != null");
        Assert(CenterOfMass != null, "Start: CenterOfMass != null");
        Assert(PitchMoment != null, "Start: PitchMoment != null");
        Assert(YawMoment != null, "Start: YawMoment != null");
        Assert(GroundDetector != null, "Start: GroundDetector != null");
        Assert(HookDetector != null, "Start: HookDetector != null");
        Assert(AGMCam != null, "Start: AGMCam != null");
        Assert(CatapultDetector != null, "Start: CatapultDetector != null");
        Assert(AAM != null, "Start: AAM != null");
        Assert(AAMLaunchPoint != null, "Start: AAMLaunchPoint != null");
        Assert(AGM != null, "Start: AGM != null");
        Assert(AGMLaunchPoint != null, "Start: AGMLaunchPoint != null");


        FullHealth = Health;
        FullFuel = Fuel;
        FullGunAmmo = GunAmmoInSeconds;
        FullAAMs = NumAAM;
        FullAGMs = NumAGM;
        if (AAM != null) { AAMLockAngle = AAM.GetComponent<AAMController>().LockAngle; }

        StartPitchStrength = PitchStrength;//used for takeoff assist
        if (AtmosphereThinningStart > AtmosphereThinningEnd) { AtmosphereThinningEnd = AtmosphereThinningStart; }
        VehicleRigidbody = VehicleMainObj.GetComponent<Rigidbody>();
        VehicleConstantForce = VehicleMainObj.GetComponent<ConstantForce>();
        localPlayer = Networking.LocalPlayer;
        if (localPlayer == null) { InEditor = true; Piloting = true; }
        else
        {
            InEditor = false; if (localPlayer.IsUserInVR()) { InVR = true; }
        }


        //get array of AAM Targets
        RaycastHit[] aamtargs = Physics.SphereCastAll(CenterOfMass.transform.position, 1000000, VehicleMainObj.transform.forward, 5, AAMTargetsLayer, QueryTriggerInteraction.Collide);
        int n = 0;
        if (aamtargs.Length > 0)
        {
            NumAAMTargets = Mathf.Clamp(aamtargs.Length - 1, 0, 999);//one less because it found our own plane
        }

        //work out which index in the aamtargs array is our own plane by finding the closest target object to our plane
        float dist = 999999;
        int self = 0;
        n = 0;
        foreach (RaycastHit target in aamtargs)
        {
            float thisdist = Vector3.Distance(CenterOfMass.transform.position, target.collider.transform.position);
            if (thisdist < dist)
            {
                dist = thisdist;
                self = n;
            }
            n++;
        }
        //populate AAMTargets list excluding our own plane
        n = 0;
        int foundself = 0;
        foreach (RaycastHit target in aamtargs)
        {
            if (n == self) { foundself = 1; n++; }
            else
            {
                AAMTargets[n - foundself] = target.collider.gameObject;
                n++;
            }
        }
        n = 0;
        //create a unique number based on position in the hierarchy in order to sort the AAMTargets array later, to make sure they're the same among clients 
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
        if (AAMTargets.Length != 0)
        {
            SortTargets(AAMTargets, order);
        }


        float scaleratio = CenterOfMass.transform.lossyScale.magnitude / Vector3.one.magnitude;
        VehicleRigidbody.centerOfMass = CenterOfMass.localPosition * scaleratio;//correct position if scaled

        AtmoshpereFadeDistance = (AtmosphereThinningEnd + SeaLevel) - (AtmosphereThinningStart + SeaLevel); //for finding atmosphere thinning gradient
        AtmosphereHeightThing = (AtmosphereThinningStart + SeaLevel) / (AtmoshpereFadeDistance); //used to add back the height to the atmosphere after finding gradient

        //used to set each rotation axis' reversing behaviour to inverted if 0 thrust vectoring, and not inverted if thrust vectoring is non-zero.
        //the variables are called 'Zero' because they ask if thrustvec is set to 0.

        ReversingPitchStrengthZero = PitchThrustVecMulti == 0 ? -ReversingPitchStrengthMulti : 1;
        ReversingYawStrengthZero = YawThrustVecMulti == 0 ? -ReversingYawStrengthMulti : 1;
        ReversingRollStrengthZero = RollThrustVecMulti == 0 ? -ReversingRollStrengthMulti : 1;
    }

    private void LateUpdate()
    {
        if (!InEditor) IsOwner = localPlayer.IsOwner(VehicleMainObj);
        if ((!EffectsControl.GearUp) && Physics.Raycast(GroundDetector.position, GroundDetector.TransformDirection(Vector3.down), .44f, 1))
        {
            Taxiing = true;
        }
        else { Taxiing = false; }


        if (InEditor || IsOwner)//works in editor or ingame
        {
            CurrentVel = VehicleRigidbody.velocity;//because rigidbody values aren't accessable by non-owner players
            Speed = CurrentVel.magnitude;
            float gustx = (Time.time * WindGustiness) + (VehicleMainObj.transform.position.x * WindTurbulanceScale);
            float gustz = (Time.time * WindGustiness) + (VehicleMainObj.transform.position.z * WindTurbulanceScale);
            FinalWind = Vector3.Normalize(new Vector3((Mathf.PerlinNoise(gustx + 9000, gustz) - .5f), /* (Mathf.PerlinNoise(gustx - 9000, gustz - 9000) - .5f) */0, (Mathf.PerlinNoise(gustx, gustz + 9999) - .5f))) * WindGustStrength;
            FinalWind += Wind;
            AirVel = VehicleRigidbody.velocity - FinalWind;
            AirSpeed = AirVel.magnitude;
            if (!Piloting) { Occupied = false; } //should make vehicle respawnable if player disconnects while occupying
            AngleOfAttackPitch = Vector3.SignedAngle(VehicleMainObj.transform.forward, AirVel, VehicleMainObj.transform.right);
            AngleOfAttackYaw = Vector3.SignedAngle(VehicleMainObj.transform.forward, AirVel, VehicleMainObj.transform.up);

            //angle of attack stuff, pitch and yaw are calculated seperately
            //pitch and yaw each have a curve for when they are within the 'MaxAngleOfAttack' and a linear version up to 90 degrees, which are Max'd (using Mathf.Clamp) for the final result.
            //the linear version is used for high aoa, and is 0 when at 90 degrees, 1 at 0(multiplied by HighAoaMinControlx). When at more than 90 degrees, the control comes back with the same curve but the inputs are inverted. (unless thrust vectoring is enabled) The invert code is elsewhere.
            AoALiftPitch = Mathf.Min(Mathf.Abs(AngleOfAttackPitch) / MaxAngleOfAttackPitch, Mathf.Abs(Mathf.Abs(AngleOfAttackPitch) - 180) / MaxAngleOfAttackPitch);//angle of attack as 0-1 float, for backwards and forwards
            AoALiftPitch = -AoALiftPitch + 1;
            AoALiftPitch = -Mathf.Pow((1 - AoALiftPitch), AoaCurveStrength) + 1;//give it a curve

            float AoALiftPitchMin = Mathf.Min(Mathf.Abs(AngleOfAttackPitch) / 90, Mathf.Abs(Mathf.Abs(AngleOfAttackPitch) - 180) / 90);//linear version to 90 for high aoa
            AoALiftPitchMin = Mathf.Clamp((-AoALiftPitchMin + 1) * HighAoaMinControlPitch, 0, 1);
            AoALiftPitch = Mathf.Clamp(AoALiftPitch, AoALiftPitchMin, 1);

            AoALiftYaw = Mathf.Min(Mathf.Abs(AngleOfAttackYaw) / MaxAngleOfAttackYaw, Mathf.Abs((Mathf.Abs(AngleOfAttackYaw) - 180)) / MaxAngleOfAttackYaw);
            AoALiftYaw = -AoALiftYaw + 1;
            AoALiftYaw = -Mathf.Pow((1 - AoALiftYaw), AoaCurveStrength) + 1;//give it a curve

            float AoALiftYawMin = Mathf.Min(Mathf.Abs(AngleOfAttackYaw) / 90, Mathf.Abs(Mathf.Abs(AngleOfAttackYaw) - 180) / 90);//linear version to 90 for high aoa
            AoALiftYawMin = Mathf.Clamp((-AoALiftPitchMin + 1) * HighAoaMinControlYaw, 0, 1);
            AoALiftYaw = Mathf.Clamp(AoALiftYaw, AoALiftYawMin, 1);

            AngleOfAttack = Mathf.Max(AngleOfAttackPitch, AngleOfAttackYaw);

            //used to create air resistance for updown and sideways if your movement direction is in those directions
            //to add physics to plane's yaw and pitch, accel angvel towards velocity, and add force to the plane
            //and add wind
            sidespeed = Vector3.Dot(AirVel, VehicleMainObj.transform.right);
            downspeed = Vector3.Dot(AirVel, VehicleMainObj.transform.up) * -1;

            if (downspeed < 0)//air is hitting plane from above
            {
                downspeed *= PitchDownLiftMulti;
            }

            //speed related values
            float SpeedLiftFactor = Mathf.Clamp(AirVel.magnitude * AirVel.magnitude * Lift, 0, MaxLift);
            rotlift = AirSpeed / RotMultiMaxSpeed;//using a simple linear curve for increasing control as you move faster



            if (Piloting)
            {
                PilotingInt = 1;
                Occupied = true;
                //collect inputs
                Wf = Input.GetKey(KeyCode.W) ? 1 : 0; //inputs as floats
                Sf = Input.GetKey(KeyCode.S) ? -1 : 0;
                Af = Input.GetKey(KeyCode.A) ? -1 : 0;
                Df = Input.GetKey(KeyCode.D) ? 1 : 0;
                Qf = Input.GetKey(KeyCode.Q) ? -1 : 0;
                Ef = Input.GetKey(KeyCode.E) ? 1 : 0;
                upf = Input.GetKey(KeyCode.UpArrow) ? 1 : 0;
                downf = Input.GetKey(KeyCode.DownArrow) ? -1 : 0;
                leftf = Input.GetKey(KeyCode.LeftArrow) ? -1 : 0;
                rightf = Input.GetKey(KeyCode.RightArrow) ? 1 : 0;
                Shiftf = Input.GetKey(KeyCode.LeftShift) ? 1 : 0;
                LeftControlf = Input.GetKey(KeyCode.LeftControl) ? 1 : 0;
                LStick.x = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
                LStick.y = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
                RStick.x = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
                RStick.y = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
                LGrip = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger");
                RGrip = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
                LTrigger = LTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
                RTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
                //MouseX = Input.GetAxisRaw("Mouse X");
                //MouseY = Input.GetAxisRaw("Mouse Y");

                //close canopy when moving fast, can't fly with it open
                if (EffectsControl.CanopyOpen && Speed > 20)
                {
                    if (CanopyCloseTimer < -100000 + CanopyCloseTime)
                    {
                        EffectsControl.CanopyOpen = false;
                        if (InEditor) CanopyClosing();
                        else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CanopyClosing"); }
                    }
                }

                //RStick Selection wheel
                if (RStick.magnitude > .8f)
                {
                    float stickdir = Vector2.SignedAngle(new Vector2(-0.382683432365f, 0.923879532511f), RStick);

                    if (stickdir > 135)//down
                    {
                        if (HasGear)
                            RStickSelection = 5;
                    }
                    else if (stickdir > 90)//downleft
                    {
                        if (HasFlaps)
                            RStickSelection = 6;
                    }
                    else if (stickdir > 45)//left
                    {
                        if (HasSmoke)
                            RStickSelection = 7;
                    }
                    else if (stickdir > 0)//upleft
                    {
                        if (HasFlare)
                            RStickSelection = 8;
                    }
                    else if (stickdir > -45)//up
                    {
                        if (HasGun)
                            RStickSelection = 1;
                    }
                    else if (stickdir > -90)//upright
                    {
                        if (HasAAM)
                            RStickSelection = 2;
                    }
                    else if (stickdir > -135)//right
                    {
                        if (HasAGM)
                            RStickSelection = 3;
                    }
                    else//downright
                    {
                        if (HasAltHold)
                            RStickSelection = 4;
                    }
                }

                //LStick Selection wheel
                if (LStick.magnitude > .8f)
                {
                    float stickdir = Vector2.SignedAngle(new Vector2(-0.382683432365f, 0.923879532511f), LStick);

                    if (stickdir > 135)//down
                    {
                        if (HasBrake)
                            LStickSelection = 5;
                    }
                    else if (stickdir > 90)//downleft
                    {
                        if (HasTRIM)
                            LStickSelection = 6;
                    }
                    else if (stickdir > 45)//left
                    {
                        if (HasCanopy)
                            LStickSelection = 7;
                    }
                    else if (stickdir > 0)//upleft
                    {
                        if (HasAfterBurner)
                            LStickSelection = 8;
                    }
                    else if (stickdir > -45)//up
                    {
                        if (HasCruise)
                            LStickSelection = 1;
                    }
                    else if (stickdir > -90)//upright
                    {
                        if (HasLimits)
                            LStickSelection = 2;
                    }
                    else if (stickdir > -135)//right
                    {
                        if (HasCatapult)
                            LStickSelection = 3;
                    }
                    else//downright
                    {
                        if (HasHook)
                            LStickSelection = 4;
                    }
                }

                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    LStickSelection++;
                    if (LStickSelection > 8) LStickSelection = 1;
                }
                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    RStickSelection++;
                    if (RStickSelection > 8) RStickSelection = 1;
                }

                LTriggerTapTime += Time.deltaTime;
                {
                    switch (LStickSelection)
                    {
                        case 0://player just got in and hasn't selected anything
                            break;
                        case 1://Cruise
                            if (LTrigger > 0.75 || (Input.GetKey(KeyCode.Alpha4)))
                            {
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
                                }

                                //VR Set Speed
                                if (InVR)
                                {

                                    handpos = VehicleMainObj.transform.InverseTransformPoint(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position).z;
                                    if (!LTriggerLastFrame)
                                    {
                                        SpeedZeroPoint = handpos;
                                        TempSpeed = SetSpeed;
                                    }
                                    float SpeedDifference = (SpeedZeroPoint - handpos) * -600;
                                    SetSpeed = Mathf.Clamp(TempSpeed + SpeedDifference, 0, 2000);

                                }

                                LTriggerLastFrame = true;
                            }
                            else { LTriggerLastFrame = false; }
                            AirBrakeInput = 0;
                            break;
                        case 2://LIMIT
                            if (LTrigger > 0.75 || (Input.GetKey(KeyCode.Alpha4)))
                            {
                                if (!LTriggerLastFrame)
                                {
                                    FlightLimitsEnabled = !FlightLimitsEnabled;
                                }

                                LTriggerLastFrame = true;
                            }
                            else { LTriggerLastFrame = false; }
                            AirBrakeInput = 0;
                            break;
                        case 3://CATAPULT
                            if (LTrigger > 0.75 || (Input.GetKey(KeyCode.Alpha4)))
                            {
                                if (!LTriggerLastFrame)
                                {
                                    switch (CatapultStatus)
                                    {
                                        case 0://we're just taxiing, check if there's a catapult trigger below us (done elsewhere because it's done every frame when taxiing)
                                            break;
                                        case 1:
                                            CatapultStatus = 2; // launch the catapult
                                            break;
                                        case 2:
                                            break;
                                    }
                                }

                                LTriggerLastFrame = true;
                            }
                            else { LTriggerLastFrame = false; }
                            AirBrakeInput = 0;
                            break;
                        case 4://HOOK
                            if (LTrigger > 0.75 || (Input.GetKey(KeyCode.Alpha4)))
                            {
                                if (!LTriggerLastFrame)
                                {
                                    if (HookDetector != null)
                                    {
                                        EffectsControl.HookDown = !EffectsControl.HookDown;
                                    }
                                    Hooked = -1;
                                }

                                LTriggerLastFrame = true;
                            }
                            else { LTriggerLastFrame = false; }
                            AirBrakeInput = 0;
                            break;
                        case 5://Brake done elsewhere because it's analog
                            if (Input.GetKey(KeyCode.Alpha4))
                            {
                                AirBrakeInput = 1;
                            }
                            else AirBrakeInput = LTrigger;

                            if (LTrigger > 0.75 || (Input.GetKey(KeyCode.Alpha4))) { LTriggerLastFrame = true; }
                            else { LTriggerLastFrame = false; }
                            break;
                        case 6://Trim
                            if (LTrigger > 0.75 || (Input.GetKey(KeyCode.Alpha4)))
                            {
                                if (!LTriggerLastFrame)
                                {
                                    if (InVR)
                                    {
                                        HandPosTrim = VehicleMainObj.transform.InverseTransformPoint(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position);
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
                                    HandPosTrim = VehicleMainObj.transform.InverseTransformPoint(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position);
                                    TrimDifference = (TrimZeroPoint - HandPosTrim) * 2f;
                                    Trim.x = Mathf.Clamp(TempTrim.y + TrimDifference.y, -1, 1);
                                    Trim.y = Mathf.Clamp(TempTrim.x + -TrimDifference.x, -1, 1);
                                }
                                LTriggerLastFrame = true;
                            }
                            else { LTriggerLastFrame = false; }
                            AirBrakeInput = 0;
                            break;
                        case 7://Canopy
                            if (LTrigger > 0.75 || (Input.GetKey(KeyCode.Alpha4)))
                            {
                                if (!LTriggerLastFrame && Speed < 20)
                                {
                                    if (CanopyCloseTimer < (-100000 - CanopyCloseTime))
                                    {
                                        EffectsControl.CanopyOpen = false;
                                        if (InEditor) CanopyClosing();
                                        else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CanopyClosing"); }
                                    }
                                    else if (CanopyCloseTimer < 0 && CanopyCloseTimer > -10000)
                                    {
                                        EffectsControl.CanopyOpen = true;
                                        if (InEditor) CanopyOpening();
                                        else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CanopyOpening"); }
                                    }
                                }

                                //ejection
                                if (InVR)
                                {
                                    float handposL = VehicleMainObj.transform.InverseTransformPoint(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position).y;
                                    float handposR = VehicleMainObj.transform.InverseTransformPoint(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position).y;
                                    if (!LTriggerLastFrame && (handposL - handposR) < 0.15f)
                                    {
                                        EjectZeroPoint = handposL;
                                        EjectTimer = 0;
                                    }
                                    if (handposL - EjectZeroPoint > .5f && EjectTimer < 1)
                                    {
                                        Ejected = true;
                                        foreach (LeaveVehicleButton seat in LeaveButtons)
                                        {
                                            if (seat != null) seat.ExitStation();
                                        }
                                        EffectsControl.CanopyOpen = true;
                                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CanopyOpening");
                                    }
                                }

                                EjectTimer += Time.deltaTime;
                                LTriggerLastFrame = true;
                            }
                            else
                            {
                                LTriggerLastFrame = false;
                                EjectTimer = 2;
                            }
                            AirBrakeInput = 0;
                            break;
                        case 8://Afterburner
                            if (LTrigger > 0.75 || (Input.GetKey(KeyCode.Alpha4)))
                            {
                                if (!LTriggerLastFrame)
                                {
                                    EffectsControl.AfterburnerOn = !EffectsControl.AfterburnerOn;
                                    if (EffectsControl.AfterburnerOn)
                                    {
                                        Afterburner = AfterburnerThrustMulti;
                                        if (ThrottleInput > 0.6)
                                        {
                                            if (InEditor)
                                            {
                                                PlayABOnSound();
                                            }
                                            else SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PlayABOnSound");
                                        }
                                    }
                                    else { Afterburner = 1; }
                                }
                                LTriggerLastFrame = true;
                            }
                            else { LTriggerLastFrame = false; }
                            AirBrakeInput = 0;
                            break;
                    }
                }

                RTriggerTapTime += Time.deltaTime;
                switch (RStickSelection)
                {
                    case 0://player just got in and hasn't selected anything
                        break;
                    case 1://GUN
                        if ((RTrigger > 0.75 || Input.GetKey(KeyCode.Alpha5)) && GunAmmoInSeconds > 0)
                        {
                            IsFiringGun = true;
                            GunAmmoInSeconds = Mathf.Max(GunAmmoInSeconds - Time.deltaTime, 0);
                            RTriggerLastFrame = true;
                        }
                        else { IsFiringGun = false; RTriggerLastFrame = false; }
                        break;
                    case 2://AAM
                        if (NumAAMTargets != 0 && NumAAM > 0)
                        {
                            AAMCurrentTargetDirection = AAMTargets[AAMTarget].transform.position - CenterOfMass.transform.position;
                            float AAMCurrentTargetAngle = Vector3.Angle(VehicleMainObj.transform.forward, (AAMTargets[AAMTarget].transform.position - CenterOfMass.transform.position));
                            float AAMCurrentTargetDistance = AAMCurrentTargetDirection.magnitude;
                            EngineController CurrentTargetEngineControl = VehicleMainObj.GetComponent<EngineController>();//this will return null
                            if (AAMTargets[AAMTarget].transform.parent != null)
                                CurrentTargetEngineControl = AAMTargets[AAMTarget].transform.parent.GetComponent<EngineController>();
                            //current target locking if within lock angle and range
                            //if EngineController is null then it's a dummy target (or isn't set up properly)
                            if (CurrentTargetEngineControl == null || !CurrentTargetEngineControl.Taxiing && !CurrentTargetEngineControl.dead)
                            {
                                if (AAMCurrentTargetAngle < AAMLockAngle && AAMCurrentTargetDistance < AAMMaxTargetDistance && AAMTargets[AAMTarget].activeSelf)
                                {
                                    AAMLockTimer += Time.deltaTime;
                                    AAMHasTarget = true;
                                }
                                else
                                {
                                    AAMLockTimer = 0;
                                    AAMHasTarget = false;
                                }

                            }
                            else AAMHasTarget = false;
                            if (AAMLockTimer > AAMLockTime) AAMLock = true;
                            else { AAMLock = false; }

                            //check 1 target per frame to see if it's infront of us and worthy of being our current target
                            Vector3 AAMNextTargetDirection = (AAMTargets[AAMTargetChecker].transform.position - CenterOfMass.transform.position);
                            float nexttargetangle = Vector3.Angle(VehicleMainObj.transform.forward, AAMNextTargetDirection);
                            float NextTargetDistance = Vector3.Distance(CenterOfMass.position, AAMTargets[AAMTargetChecker].transform.position);
                            EngineController NextTargetEngineControl = VehicleMainObj.GetComponent<EngineController>();//this will return null
                            if (AAMTargets[AAMTargetChecker].transform.parent != null)
                                NextTargetEngineControl = AAMTargets[AAMTargetChecker].transform.parent.GetComponent<EngineController>();
                            //if EngineController is null then it's a dummy target (or isn't set up properly)
                            if (NextTargetEngineControl == null || !NextTargetEngineControl.Taxiing && !NextTargetEngineControl.dead)
                            {
                                if ((AAMTargets[AAMTargetChecker].activeSelf && nexttargetangle < AAMLockAngle)
                                 && NextTargetDistance < AAMMaxTargetDistance && nexttargetangle < AAMCurrentTargetAngle
                                && AAMTarget != AAMTargetChecker)
                                {
                                    AAMTarget = AAMTargetChecker;
                                    AAMLockTimer = 0;
                                }
                            }


                            AAMTargetChecker++;
                            if (AAMTargetChecker == NumAAMTargets)
                            {
                                AAMTargetChecker = 0;
                            }
                        }
                        else { AAMLock = false; AAMHasTarget = false; }

                        if (RTrigger > 0.75 || (Input.GetKey(KeyCode.Alpha5)))
                        {
                            if (!RTriggerLastFrame)
                            {
                                if (AAMLock && !Taxiing && Time.time - AALastFiredTime > 0.5)
                                {
                                    AALastFiredTime = Time.time;
                                    if (InEditor)
                                        LaunchAAM();
                                    else
                                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchAAM");
                                    NumAAM--;
                                }
                            }
                            RTriggerLastFrame = true;
                        }
                        else RTriggerLastFrame = false;

                        IsFiringGun = false;
                        break;
                    case 3://AGM
                        AGMUnlockTimer += Time.deltaTime * AGMUnlocking;//AGMUnlocking is 1 if it was locked and just pressed, else 0, (waits for double tap delay to disable)
                        if (AGMUnlockTimer > 0.4f)
                        {
                            AGMLocked = false; AGMUnlockTimer = 0;
                        }
                        if (RTrigger > 0.75 || (Input.GetKey(KeyCode.Alpha5)))
                        {
                            if (!RTriggerLastFrame)
                                if (RTriggerTapTime < 0.4f)
                                {
                                    if (AGMLocked)
                                    {
                                        //double tap detected
                                        if (NumAGM > 0)
                                        {
                                            if (InEditor)
                                                LaunchAGM();
                                            else
                                                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchAGM");
                                            NumAGM--;
                                        }
                                        AGMUnlocking = 0;
                                    }
                                }
                                else if (!AGMLocked)
                                {
                                    RaycastHit lockpoint;
                                    if (AGMCam != null)
                                    {
                                        float targetangle = 360;
                                        RaycastHit[] agmtargs = Physics.SphereCastAll(AGMCam.transform.position, 40, AGMCam.transform.forward, Mathf.Infinity, AGMTargetsLayer);
                                        if (agmtargs.Length > 0)
                                        {
                                            //find target with lowest angle from crosshair
                                            foreach (RaycastHit target in agmtargs)
                                            {
                                                Vector3 targetdirection = target.point - AGMCam.transform.position;
                                                float angle = Vector3.Angle(AGMCam.transform.forward, targetdirection);
                                                if (angle < targetangle)
                                                {
                                                    targetangle = angle;
                                                    AGMTarget = target.collider.gameObject.transform.position;
                                                    AGMLocked = true;
                                                    AGMUnlocking = 0;
                                                }
                                            }
                                        }
                                        else
                                        {

                                            Physics.Raycast(AGMCam.transform.position, AGMCam.transform.forward, out lockpoint, Mathf.Infinity, 1);
                                            if (lockpoint.point != null)
                                            {
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
                                    RTriggerTapTime = 0;
                                }
                            RTriggerLastFrame = true;
                        }
                        else { RTriggerLastFrame = false; }
                        //AGM Camera
                        if (!AGMLocked)
                        {
                            Quaternion temp = Quaternion.identity;
                            if (InVR)
                            {
                                temp = (localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation * Quaternion.Euler(0, 60, 0));
                            }
                            else if (!InEditor)//desktop mode
                            {
                                temp = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
                            }
                            else//editor
                            {
                                temp = VehicleMainObj.transform.rotation;
                            }
                            AGMRotDif = Vector3.Angle(AGMCam.transform.rotation * Vector3.forward, AGMCamRotSlerper * Vector3.forward);

                            AGMCamRotSlerper = Quaternion.Slerp(AGMCamRotSlerper, temp, 70f * Time.deltaTime);

                            if (AGMCam != null) AGMCam.transform.rotation = AGMCamRotSlerper;
                            //dunno if there's a better way to do this
                            Vector3 temp2 = AGMCam.transform.localRotation.eulerAngles;
                            temp2.z = 0;
                            if (AGMCam != null) AGMCam.transform.localRotation = Quaternion.Euler(temp2);
                        }


                        IsFiringGun = false;
                        break;
                    case 4://altitude hold
                        if (RTrigger > 0.75 || (Input.GetKey(KeyCode.Alpha5)))
                        {
                            if (!RTriggerLastFrame) LevelFlight = !LevelFlight;

                            IsFiringGun = false;
                            RTriggerLastFrame = true;
                        }
                        else { RTriggerLastFrame = false; }
                        IsFiringGun = false;
                        break;
                    case 5://GEAR
                        if (RTrigger > 0.75 || (Input.GetKey(KeyCode.Alpha5)))
                        {
                            if (!RTriggerLastFrame && CatapultStatus == 0) { EffectsControl.GearUp = !EffectsControl.GearUp; }

                            IsFiringGun = false;
                            RTriggerLastFrame = true;
                        }
                        else { IsFiringGun = false; RTriggerLastFrame = false; }
                        break;
                    case 6://flaps
                        if (RTrigger > 0.75 || (Input.GetKey(KeyCode.Alpha5)))
                        {
                            if (!RTriggerLastFrame) EffectsControl.Flaps = !EffectsControl.Flaps;

                            IsFiringGun = false;
                            RTriggerLastFrame = true;
                        }
                        else { RTriggerLastFrame = false; }
                        IsFiringGun = false;
                        break;
                    case 7://Display smoke
                        if (RTrigger > 0.75 || (Input.GetKey(KeyCode.Alpha5)))
                        {
                            //you can change smoke colour by holding down the trigger and waving your hand around. x/y/z = r/g/b
                            if (!RTriggerLastFrame)
                            {
                                if (InVR)
                                {
                                    HandPosSmoke = VehicleMainObj.transform.InverseTransformPoint(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position);
                                    SmokeZeroPoint = HandPosSmoke;
                                    TempSmokeCol = SmokeColor;
                                }
                                EffectsControl.Smoking = !EffectsControl.Smoking;
                                SmokeHoldTime = 0;
                            }
                            if (InVR)
                            {
                                SmokeHoldTime += Time.deltaTime;
                                if (SmokeHoldTime > .4f)
                                {

                                    //VR Set Smoke
                                    HandPosSmoke = VehicleMainObj.transform.InverseTransformPoint(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position);

                                    Vector3 SmokeDifference = (SmokeZeroPoint - HandPosSmoke) * 8f;
                                    SmokeColor.x = Mathf.Clamp(TempSmokeCol.x + SmokeDifference.x, 0, 1);
                                    SmokeColor.y = Mathf.Clamp(TempSmokeCol.y + SmokeDifference.y, 0, 1);
                                    SmokeColor.z = Mathf.Clamp(TempSmokeCol.z + SmokeDifference.z, 0, 1);
                                    if (SmokeColor.magnitude < .5)
                                    {
                                        SmokeColor = SmokeColor.normalized * 0.5f;
                                    }
                                }
                            }


                            IsFiringGun = false;
                            RTriggerLastFrame = true;
                        }
                        else { RTriggerLastFrame = false; }
                        IsFiringGun = false;
                        break;
                    case 8://flares
                        if (RTrigger > 0.75 || (Input.GetKey(KeyCode.Alpha5)))
                        {
                            if (!RTriggerLastFrame)
                            {
                                if (InEditor) { EffectsControl.PlaneAnimator.SetTrigger("flares"); }//editor
                                else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "DropFlares"); }//ingame
                            }

                            LevelFlight = false;
                            IsFiringGun = false;
                            RTriggerLastFrame = true;
                        }
                        else { RTriggerLastFrame = false; }
                        IsFiringGun = false;
                        break;
                }

                //VR Joystick
                if (RGrip > 0.75)
                {
                    PlaneRotDif = VehicleMainObj.transform.rotation * Quaternion.Inverse(PlaneRotLastFrame);//difference in plane's rotation since last frame
                    JoystickZeroPoint = PlaneRotDif * JoystickZeroPoint;//zero point rotates with the plane so it appears still to the pilot
                    if (!RGripLastFrame)//first frame you gripped joystick
                    {
                        PlaneRotDif = Quaternion.identity;
                        JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;//rotation of the controller relative to the plane when it was pressed
                    }
                    //difference between the plane and the hand's rotation, and then the difference between that and the JoystickZeroPoint
                    JoystickDifference = (Quaternion.Inverse(VehicleMainObj.transform.rotation) * localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation) * Quaternion.Inverse(JoystickZeroPoint);
                    JoystickPosYaw = (JoystickDifference * VehicleMainObj.transform.forward);//angles to vector
                    JoystickPosYaw.y = 0;
                    JoystickPos = (JoystickDifference * VehicleMainObj.transform.up);
                    VRPitchRollInput = new Vector2(JoystickPos.x, JoystickPos.z) * 1.41421f;

                    RGripLastFrame = true;
                    //making a circular joy stick square
                    //pitch and roll
                    if (Mathf.Abs(VRPitchRollInput.x) > Mathf.Abs(VRPitchRollInput.y))
                    {
                        if (Mathf.Abs(VRPitchRollInput.x) != 0)
                        {
                            float temp = VRPitchRollInput.magnitude / Mathf.Abs(VRPitchRollInput.x);
                            VRPitchRollInput *= temp;
                        }
                    }
                    else if (Mathf.Abs(VRPitchRollInput.y) != 0)
                    {
                        float temp = VRPitchRollInput.magnitude / Mathf.Abs(VRPitchRollInput.y);
                        VRPitchRollInput *= temp;
                    }
                    //yaw
                    if (Mathf.Abs(JoystickPosYaw.x) > Mathf.Abs(JoystickPosYaw.z))
                    {
                        if (Mathf.Abs(JoystickPosYaw.x) != 0)
                        {
                            float temp = JoystickPosYaw.magnitude / Mathf.Abs(JoystickPosYaw.x);
                            JoystickPosYaw *= temp;
                        }
                    }
                    else if (Mathf.Abs(JoystickPosYaw.z) != 0)
                    {
                        float temp = JoystickPosYaw.magnitude / Mathf.Abs(JoystickPosYaw.z);
                        JoystickPosYaw *= temp;
                    }

                }
                else
                {
                    JoystickPosYaw.x = 0;
                    VRPitchRollInput = Vector3.zero;
                    RGripLastFrame = false;
                }
                PlaneRotLastFrame = VehicleMainObj.transform.rotation;

                //VR Throttle
                if (LGrip > 0.75)
                {
                    handpos = VehicleMainObj.transform.InverseTransformPoint(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position).z;
                    if (!LGripLastFrame)
                    {
                        ThrottleZeroPoint = handpos;
                        TempThrottle = PlayerThrottle;
                    }
                    ThrottleDifference = ThrottleZeroPoint - handpos;
                    ThrottleDifference *= -6;

                    PlayerThrottle = Mathf.Clamp(TempThrottle + ThrottleDifference, 0, 1);
                    LGripLastFrame = true;
                }
                else
                {
                    LGripLastFrame = false;
                }

                PlayerThrottle = Mathf.Clamp(PlayerThrottle + ((Shiftf - LeftControlf) * .5f * Time.deltaTime), 0, 1);
                //if touching ground rotate if trying to turn
                if (Taxiing)
                {
                    AngleOfAttack = 0; // prevent stall sound and aoavapor when on ground
                    Cruise = false;
                    LevelFlight = false;

                    Taxiinglerper = Mathf.Lerp(Taxiinglerper, YawInput * TaxiRotationSpeed * Time.deltaTime, TaxiRotationResponse * Time.deltaTime);
                    VehicleMainObj.transform.Rotate(Vector3.up, Taxiinglerper);
                    StillWindMulti = Mathf.Clamp(Speed / 10, 0, 1);
                    ThrustVecGrounded = 0;

                    PitchStrength = StartPitchStrength + ((TakeoffAssist * Speed) / (TakeoffAssistSpeed));//stronger pitch when moving fast and taxiing to help with taking off

                    if (AirBrakeInput > 0 && Speed < 40 && Hooked < 0f)
                    {
                        VehicleRigidbody.velocity += -CurrentVel.normalized * AirBrakeInput * GroundBrakeStrength * Time.deltaTime;
                    }

                    if (Physics.Raycast(GroundDetector.position, VehicleMainObj.transform.TransformDirection(Vector3.down), 1f, ResupplyLayer))
                    {
                        if (!ResupplyingLastFrame)
                        {
                            LastResupplyTime = Time.time;
                            if (InEditor) SetLaunchOpositeSideFalse();
                            else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetLaunchOpositeSideFalse"); }
                        }
                        if (Time.time - LastResupplyTime > 1)
                        {
                            LastResupplyTime = Time.time;
                            NumAAM = (int)Mathf.Min(NumAAM + Mathf.Max(Mathf.Floor(FullAAMs / 10), 1), FullAAMs);
                            NumAGM = (int)Mathf.Min(NumAGM + Mathf.Max(Mathf.Floor(NumAGM / 5), 1), FullAGMs);
                            Fuel = Mathf.Min(Fuel + (FullFuel / 20), FullFuel);
                            GunAmmoInSeconds = Mathf.Min(GunAmmoInSeconds + (FullGunAmmo / 15), FullGunAmmo);
                            Health = Mathf.Min(Health + (FullHealth / 25), FullHealth);
                        }
                        ResupplyingLastFrame = true;
                    }
                    else ResupplyingLastFrame = false;
                    //check for catapult below us and attach if there is one    
                    if (CanCatapult && Speed < 15 && CatapultStatus == 0)
                    {
                        RaycastHit hit;
                        if (Physics.Raycast(CatapultDetector.position, CatapultDetector.TransformDirection(Vector3.down), out hit, 1f, CatapultLayer))
                        {
                            Transform CatapultTrigger = hit.collider.transform;//get the transform from the trigger hit

                            //Hit detected, check if the plane is facing in the right direction..
                            if (Vector3.Angle(VehicleMainObj.transform.forward, CatapultTrigger.transform.forward) < 15)
                            {
                                //then lock the plane to the catapult! Works with the catapult in any orientation whatsoever.
                                CatapultLockRot = CatapultTrigger.transform.rotation;//rotation to lock the plane to on the catapult
                                VehicleMainObj.transform.rotation = CatapultLockRot;//set the plane to the locked rotation so the next step is done at the right angle
                                Vector3 temp = VehicleMainObj.transform.InverseTransformPoint(CatapultTrigger.transform.position);//relative position of the catapult to our plane
                                temp.y = 0;//zero out height because we don't want to move up/down
                                temp = VehicleMainObj.transform.TransformPoint(temp);//convert relative coords back to global
                                VehicleMainObj.transform.position += VehicleMainObj.transform.position - temp;//move plane to catapult

                                //here we do the same thing as above but with our own catapult detector object so that the front wheel locks to the correct position on the catapult
                                temp = VehicleMainObj.transform.InverseTransformPoint(CatapultDetector.transform.position);
                                temp.y = 0;
                                temp = VehicleMainObj.transform.TransformPoint(temp);
                                VehicleMainObj.transform.position = CatapultTrigger.transform.position + (VehicleMainObj.transform.position - temp);

                                CatapultLockPos = VehicleMainObj.transform.position;
                                VehicleRigidbody.velocity = Vector3.zero;
                                CatapultStatus = 1;//locked to catapult

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
                    PitchStrength = StartPitchStrength;
                    StillWindMulti = 1;
                    ThrustVecGrounded = 1;
                    Taxiinglerper = 0;
                }
                //Cruise PI Controller
                if (Cruise && !LGripLastFrame)
                {
                    SetSpeed = Mathf.Clamp(SetSpeed + Shiftf - LeftControlf * 60 * Time.deltaTime, 0, 2000);

                    float error = (SetSpeed - AirSpeed);

                    CruiseIntegrator += error * Time.deltaTime;
                    CruiseIntegrator = Mathf.Clamp(CruiseIntegrator, CruiseIntegratorMin, CruiseIntegratorMax);

                    //float Derivator = Mathf.Clamp(((error - lastframeerror) / Time.deltaTime),DerivMin, DerivMax);

                    ThrottleInput = CruiseProportional * error;
                    ThrottleInput += CruiseIntegral * CruiseIntegrator;
                    //ThrottleInput += Derivative * Derivator; //works but spazzes out real bad
                    ThrottleInput = Mathf.Clamp(ThrottleInput, 0, 1);

                    Cruiselastframeerror = error;
                }
                else//if cruise control disabled, use inputs
                {
                    ThrottleInput = PlayerThrottle;
                }
                Fuel = Mathf.Clamp(Fuel - ((FuelConsumption * Mathf.Max(ThrottleInput, 0.35f)) * Time.deltaTime), 0, FullFuel);
                if (Fuel < 200) ThrottleInput = Mathf.Clamp(ThrottleInput * (Fuel / 200), 0, 1);

                if (ThrottleInput < .6f) { EffectsControl.AfterburnerOn = false; Afterburner = 1; }

                //Altitude hold PID Controller
                if (LevelFlight && !RGripLastFrame)//level flight enabled, and player not holding joystick
                {
                    FlightLimitsEnabled = true; //prevent the autopilot from killing you in various ways
                    Vector3 straight = new Vector3(VehicleRigidbody.velocity.x, 0, VehicleRigidbody.velocity.z);
                    Vector3 tempaxis = VehicleMainObj.transform.right;
                    tempaxis.x = 0;
                    float error = CurrentVel.normalized.y;//(Vector3.Dot(VehicleRigidbody.velocity.normalized, Vector3.up));

                    AltHoldPitchIntegrator += error * Time.deltaTime;
                    AltHoldPitchIntegrator = Mathf.Clamp(AltHoldPitchIntegrator, AltHoldPitchIntegratorMin, AltHoldPitchIntegratorMax);

                    AltHoldPitchDerivator = ((error - AltHoldPitchlastframeerror) / Time.deltaTime);

                    AltHoldPitchlastframeerror = error;

                    PitchInput = AltHoldPitchProportional * error;

                    PitchInput += AltHoldPitchIntegral * AltHoldPitchIntegrator;

                    PitchInput += AltHoldPitchDerivative * AltHoldPitchDerivator; //works but spazzes out real bad

                    PitchInput = Mathf.Clamp(PitchInput, -1, 1);

                    AltHoldPitchlastframeerror = error;

                    //Roll
                    error = VehicleMainObj.transform.localEulerAngles.z;
                    if (error > 180) { error -= 360; }

                    //lock upside down if rotated more than 90
                    if (error > 90)
                    {
                        error -= 180;
                        PitchInput *= -1;
                    }
                    else if (error < -90)
                    {
                        error += 180;
                        PitchInput *= -1;
                    }

                    RollInput = Mathf.Clamp(AltHoldRollProportional * error, -1, 1);

                    YawInput = 0;
                }
                else
                {
                    //'-input' are used by effectscontroller, and multiplied by 'strength' for final values
                    if (FlightLimitsEnabled && !Taxiing && AngleOfAttack < AoALimit)
                    {
                        float GLimitStrength = Mathf.Clamp(-(Gs / GLimiter) + 1, 0, 1);
                        float AoALimitStrength = Mathf.Clamp(-(Mathf.Abs(AngleOfAttack) / AoALimit) + 1, 0, 1);
                        float Limits = Mathf.Min(GLimitStrength, AoALimitStrength);
                        PitchInput = Mathf.Clamp(/*(MouseY * mouseysens + Lstick.y + */VRPitchRollInput.y + Wf + Sf + downf + upf, -1, 1) * Limits;
                        YawInput = Mathf.Clamp(Qf + Ef + JoystickPosYaw.x, -1, 1) * Limits;
                    }
                    else
                    {
                        PitchInput = Mathf.Clamp(/*(MouseY * mouseysens + Lstick.y + */VRPitchRollInput.y + Wf + Sf + downf + upf, -1, 1);
                        YawInput = Mathf.Clamp(Qf + Ef + JoystickPosYaw.x, -1, 1);
                    }
                    RollInput = Mathf.Clamp(((/*(MouseX * mousexsens) + */VRPitchRollInput.x + Af + Df + leftf + rightf) * -1), -1, 1);
                }


                //ability to adjust input to be more precise at low amounts. 'exponant'
                /* pitchinput = pitchinput > 0 ? Mathf.Pow(pitchinput, StickInputPower) : -Mathf.Pow(Mathf.Abs(pitchinput), StickInputPower);
                yawinput = yawinput > 0 ? Mathf.Pow(yawinput, StickInputPower) : -Mathf.Pow(Mathf.Abs(yawinput), StickInputPower);
                rollinput = rollinput > 0 ? Mathf.Pow(rollinput, StickInputPower) : -Mathf.Pow(Mathf.Abs(rollinput), StickInputPower); */

                //if moving backwards, controls invert (if thrustvectoring is set to 0 strength for that axis)
                if ((Vector3.Dot(AirVel, VehicleMainObj.transform.forward) > 0))//normal, moving forward
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

                //flip ur Vehicle to upright and stop rotating
                /*if (Input.GetButtonDown("Oculus_CrossPlatform_Button2") || (Input.GetKeyDown(KeyCode.T)))
                 {
                     VehicleMainObj.transform.rotation = Quaternion.Euler(VehicleMainObj.transform.rotation.eulerAngles.x, VehicleMainObj.transform.rotation.eulerAngles.y, 0f);
                     VehicleRigidbody.angularVelocity *= .3f;
                 }*/

                pitch = Mathf.Clamp(PitchInput + Trim.x, -1, 1) * PitchStrength * ReversingPitchStrength;
                yaw = Mathf.Clamp(-YawInput - Trim.y, -1, 1) * YawStrength * ReversingYawStrength;
                roll = RollInput * RollStrength * ReversingRollStrength;


                if (pitch > 0)
                {
                    pitch *= PitchDownStrMulti;
                }

                //wheel colliders are broken, this workaround stops the plane from being 'sticky' when you try to start moving it. Heard it doesn't happen so bad if rigidbody weight is realistic.
                if (Speed < .2 && ThrottleInput > 0)
                    VehicleRigidbody.velocity = VehicleRigidbody.transform.forward * 0.25f;
            }
            else
            {
                PilotingInt = 0;
                roll = 0;
                pitch = 0;
                yaw = 0;
                RollInput = 0;
                PitchInput = Trim.x;
                YawInput = Trim.y;
                ThrottleInput = 0;
            }

            //thrust vecotring airplanes have a minimum rotation speed
            float minlifttemp = rotlift * Mathf.Min(AoALiftPitch, AoALiftYaw);
            pitch *= Mathf.Max(PitchThrustVecMulti * ThrustVecGrounded, minlifttemp);
            yaw *= Mathf.Max(YawThrustVecMulti * ThrustVecGrounded, minlifttemp);
            roll *= Mathf.Max(RollThrustVecMulti * ThrustVecGrounded, minlifttemp);



            //rotation inputs are done, now we can set the minimum lift/drag when at high aoa, this should be high than 0 because if it's 0 you will have 0 drag when at 90 degree AoA.
            AoALiftPitch = Mathf.Clamp(AoALiftPitch, HighPitchAoaMinLift, 1);
            AoALiftYaw = Mathf.Clamp(AoALiftYaw, HighYawAoaMinLift, 1);


            Atmosphere = Mathf.Clamp(-(CenterOfMass.position.y / AtmoshpereFadeDistance) + 1 + AtmosphereHeightThing, 0, 1);

            //Lerp the inputs for 'engine response', throttle decrease response is slower than increase (EngineSpoolDownMulti)
            if (Throttle < ThrottleInput)
            {
                Throttle = Mathf.Lerp(Throttle, ThrottleInput, AccelerationResponse * Time.deltaTime); // ThrottleInput * ThrottleStrengthForward;
            }
            else
            {
                Throttle = Mathf.Lerp(Throttle, ThrottleInput, AccelerationResponse * EngineSpoolDownSpeedMulti * Time.deltaTime); // ThrottleInput * ThrottleStrengthForward;
            }

            //Lerp the inputs for 'rotation response'
            LerpedRoll = Mathf.Lerp(LerpedRoll, roll, RollResponse * Time.deltaTime);
            LerpedPitch = Mathf.Lerp(LerpedPitch, pitch, PitchResponse * Time.deltaTime);
            LerpedYaw = Mathf.Lerp(LerpedYaw, yaw, YawResponse * Time.deltaTime);

            //check for catching a cable with hook
            if (EffectsControl.HookDown)
            {
                if (Physics.Raycast(HookDetector.position, Vector3.down, 2f, HookCableLayer) && Hooked < 0 && Speed > 25)
                {
                    HookedLoc = VehicleMainObj.transform.position;
                    Hooked = 6f;
                    EffectsControl.PlaneAnimator.SetTrigger("hooked");
                }
                Hooked -= Time.deltaTime;
            }
            //slow down if hooked and on the ground
            if (Hooked > 0f && Taxiing)
            {
                if (Vector3.Distance(VehicleMainObj.transform.position, HookedLoc) > 90)//real planes take around 80-90 meters to stop on a carrier
                {
                    //if you go further than 90m you snap the cable and it hurts your plane by the % of the amount of time left of the 2 seconds it should have taken to stop you.
                    float damage = 0;
                    if (Hooked > 4)
                    {
                        damage = ((Hooked - 4) / 2) * FullHealth;
                    }
                    Health -= damage;
                    Hooked = -1;
                    //Debug.Log("snap");
                }
                VehicleRigidbody.velocity += -CurrentVel.normalized * HookedBrakeStrength * Time.deltaTime;
                //Debug.Log("hooked");
            }

            //flaps drag and lift
            if (EffectsControl.Flaps)
            {
                FlapsDrag = FlapsDragMulti;
                FlapsLift = FlapsLiftMulti;
            }
            else
            {
                FlapsDrag = 1;
                FlapsLift = 1;
            }
            //gear drag
            if (EffectsControl.GearUp) { GearDrag = 1; }
            else { GearDrag = LandingGearDragMulti; }
            FlapsGearBrakeDrag = (GearDrag + FlapsDrag + (AirBrakeInput * AirbrakeStrength)) - 1;//combine these so we don't have to do as much in fixedupdate

            switch (CatapultStatus)
            {
                case 0://normal
                       //do lift
                    Vector3 FinalInputAcc = new Vector3(-sidespeed * SidewaysLift * SpeedLiftFactor * AoALiftYaw * Atmosphere,// X Sideways
                        (downspeed * FlapsLift * PitchDownLiftMulti * SpeedLiftFactor * AoALiftPitch * Atmosphere) + (SpeedLiftFactor * AoALiftPitch * VelLift * Atmosphere),// Y Up
                            Throttle * ThrottleStrength * Atmosphere * Afterburner * Atmosphere);// Z Forward

                    //used to add rotation friction
                    Vector3 localAngularVelocity = transform.InverseTransformDirection(VehicleRigidbody.angularVelocity);

                    //roll + rotational frictions
                    Vector3 FinalInputRot = new Vector3(-localAngularVelocity.x * PitchFriction * rotlift * AoALiftPitch * AoALiftYaw * Atmosphere,// X Pitch
                        -localAngularVelocity.y * YawFriction * rotlift * AoALiftPitch * AoALiftYaw * Atmosphere,// Y Yaw
                            LerpedRoll + (-localAngularVelocity.z * RollFriction * rotlift * AoALiftPitch * AoALiftYaw * Atmosphere));// Z Roll

                    //create values for use in fixedupdate (control input and straightening forces)
                    Pitching = ((VehicleMainObj.transform.up * LerpedPitch * Atmosphere + (VehicleMainObj.transform.up * downspeed * VelStraightenStrPitch * AoALiftPitch * rotlift)) * 90) * PilotingInt;
                    Yawing = ((VehicleMainObj.transform.right * LerpedYaw * Atmosphere + (-VehicleMainObj.transform.right * sidespeed * VelStraightenStrYaw * AoALiftYaw * rotlift)) * 90) * PilotingInt;

                    VehicleConstantForce.relativeForce = FinalInputAcc;
                    VehicleConstantForce.relativeTorque = FinalInputRot;
                    break;
                case 1://locked on catapult
                    VehicleConstantForce.relativeForce = Vector3.zero;
                    VehicleConstantForce.relativeTorque = Vector3.zero;

                    CatapultLaunchTime = 2;
                    VehicleMainObj.transform.position = CatapultLockPos;
                    VehicleMainObj.transform.rotation = CatapultLockRot;
                    VehicleRigidbody.velocity = Vector3.zero;
                    VehicleRigidbody.angularVelocity = Vector3.zero;
                    break;
                case 2://launching
                    VehicleMainObj.transform.rotation = CatapultLockRot;
                    VehicleConstantForce.relativeForce = new Vector3(0, 0, CatapultLaunchStrength);
                    //lock all movment except for forward movement
                    Vector3 temp = VehicleMainObj.transform.InverseTransformDirection(VehicleRigidbody.velocity);
                    temp.x = 0;
                    temp.y = 0;
                    temp = VehicleMainObj.transform.TransformDirection(temp);
                    VehicleRigidbody.velocity = temp;
                    VehicleRigidbody.angularVelocity = Vector3.zero;
                    VehicleConstantForce.relativeTorque = Vector3.zero;

                    CatapultLaunchTime -= Time.deltaTime;

                    if (CatapultLaunchTime < 0) CatapultStatus = 0;
                    break;
            }

            SoundBarrier = (-Mathf.Clamp(Mathf.Abs(Speed - 343) / SoundBarrierWidth, 0, 1) + 1) * SoundBarrierStrength;
        }
        else//non-owners need to know these values
        {
            Speed = CurrentVel.magnitude;
            //VRChat doesn't set Angular Velocity to 0 when you're not the owner of a rigidbody (it seems),
            //causing spazzing, the script handles angular drag it itself, so when we're not owner of the plane, set this value to stop spazzing
            VehicleRigidbody.angularDrag = .1f;
            //AirVel = VehicleRigidbody.velocity - Wind;
            //AirSpeed = AirVel.magnitude;
        }
        SmokeColor_Color = new Color(SmokeColor.x, SmokeColor.y, SmokeColor.z);
        CanopyCloseTimer -= Time.deltaTime;
    }
    private void FixedUpdate()
    {
        if (InEditor || IsOwner)
        {
            //lerp velocity toward 0 to simulate air friction
            VehicleRigidbody.velocity = Vector3.Lerp(VehicleRigidbody.velocity, FinalWind * StillWindMulti, ((((AirFriction + SoundBarrier) * FlapsGearBrakeDrag) * Atmosphere) * 90) * Time.deltaTime);
            //apply pitching using pitch moment
            VehicleRigidbody.AddForceAtPosition(Pitching * Time.deltaTime, PitchMoment.position, ForceMode.Force);
            //apply yawing using yaw moment
            VehicleRigidbody.AddForceAtPosition(Yawing * Time.deltaTime, YawMoment.position, ForceMode.Force);
            //calc Gs
            LastFrameVel.y += (-9.81f * Time.deltaTime); //add gravity
            Gs = Vector3.Distance(LastFrameVel, VehicleRigidbody.velocity) / (9.81f * Time.deltaTime);
            LastFrameVel = VehicleRigidbody.velocity;
        }
    }
    private void OnOwnershipTransferred()
    {
        LastFrameVel = VehicleRigidbody.velocity; //hopefully prevents explosions as soon as you enter the plane
    }
    //In soundcontroller, CanopyCloseTimer < -100000 means play inside canopy sounds and between -100000 and 0 means play outside sounds.
    //The value is set above these numbers by the length of the animation, and delta time is removed from it each frame.
    //This code adds or removes 100000 based on the situation, + the time it takes for the animation to play.
    //If the Opening animation is playing when you tell it to close it keeps the time from that animation so that the timing of the sound changing is always correct.
    public void CanopyOpening()
    {
        if (CanopyCloseTimer > 0)
            CanopyCloseTimer -= 100000 + CanopyCloseTime;
        else
            CanopyCloseTimer = -100000;
    }
    public void CanopyClosing()
    {
        if (CanopyCloseTimer > (-100000 - CanopyCloseTime) && CanopyCloseTimer < 0)
            CanopyCloseTimer += 100000 + ((CanopyCloseTime * 2) + 0.1f);//the 0.1 is for the delay in the animator that is needed because it's not set to write defaults
        else
            CanopyCloseTimer = CanopyCloseTime;
    }
    public void PlayABOnSound()
    {
        SoundControl.PlaneABOn.Play();
    }
    public void LaunchAGM()
    {
        GameObject AGMMissile = VRCInstantiate(AGM);
        if (AGMLaunchOpositeSide)
        {
            Vector3 temp = AGMLaunchPoint.localPosition;
            temp.x *= -1;
            AGMLaunchPoint.localPosition = temp;
            AGMMissile.transform.position = AGMLaunchPoint.transform.position;
            AGMMissile.transform.rotation = AGMLaunchPoint.transform.rotation;
            temp.x *= -1;
            AGMLaunchPoint.localPosition = temp;
            AGMLaunchOpositeSide = !AGMLaunchOpositeSide;
        }
        else
        {
            AGMMissile.transform.position = AGMLaunchPoint.transform.position;
            AGMMissile.transform.rotation = AGMLaunchPoint.transform.rotation;
            AGMLaunchOpositeSide = !AGMLaunchOpositeSide;
        }
        AGMMissile.SetActive(true);
        AGMMissile.GetComponent<Rigidbody>().velocity = CurrentVel;
    }
    public void LaunchAAM()
    {
        GameObject AAMMissile = VRCInstantiate(AAM);
        if (AAMLaunchOpositeSide)
        {
            //invert x coordinates of launch point, launch, then revert
            Vector3 temp = AAMLaunchPoint.localPosition;
            temp.x *= -1;
            AAMLaunchPoint.localPosition = temp;
            AAMMissile.transform.position = AAMLaunchPoint.transform.position;
            AAMMissile.transform.rotation = AAMLaunchPoint.transform.rotation;
            temp.x *= -1;
            AAMLaunchPoint.localPosition = temp;
            AAMLaunchOpositeSide = !AAMLaunchOpositeSide;
        }
        else
        {
            AAMMissile.transform.position = AAMLaunchPoint.transform.position;
            AAMMissile.transform.rotation = AAMLaunchPoint.transform.rotation;
            AAMLaunchOpositeSide = !AAMLaunchOpositeSide;
        }
        AAMMissile.SetActive(true);
        AAMMissile.GetComponent<Rigidbody>().velocity = CurrentVel;
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
    public void SetLaunchOpositeSideFalse()//for resupplying
    {
        AAMLaunchOpositeSide = false;
        AGMLaunchOpositeSide = false;
    }
    //thx guribo for udon assert
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogError("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}