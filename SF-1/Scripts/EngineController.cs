
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class EngineController : UdonSharpBehaviour
{
    public GameObject VehicleMainObj;
    [System.NonSerializedAttribute] [HideInInspector] public Rigidbody VehicleRigidbody;
    public EffectsController EffectsControl;
    public SoundController SoundControl;
    public HUDController HUDControl;
    public Transform CenterOfMass;
    public Transform PitchMoment;
    public Transform YawMoment;
    public Transform GroundDetector;
    public Transform HookDetector;
    public LayerMask HookRopeLayer;
    public Transform CatapultDetector;
    public LayerMask CatapultLayer;
    public float ThrottleStrength = 25f;
    public float AfterburnerThrustMulti = 1.5f;
    public float AccelerationResponse = 4.5f;
    public float EngineSpoolDownSpeedMulti = .5f;
    public float AirFriction = 0.0004f;
    public float PitchStrength = 5f;
    public float PitchThrustVecStr = 0f;
    public float PitchFriction = 24f;
    public float PitchResponse = 12f;
    public float ReversingPitchStrengthMulti = 2;
    public float YawStrength = 3f;
    public float YawThrustVecStr = 0f;
    public float YawFriction = 15f;
    public float YawResponse = 12f;
    public float ReversingYawStrengthMulti = 2.4f;
    public float RollStrength = 450f;
    public float RollThrustVecStr = 0f;
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
    public float HookedBrakeStrength = 65f;
    public float CatapultLaunchStrength = 50f;
    public float CatapultLaunchTime = 2f;
    public float TakeoffAssist = 10f;
    [System.NonSerializedAttribute] [HideInInspector] public bool FlightLimitsEnabled = true;
    private float GLimitStrength;
    public float GLimit = 12f;
    public float AoALimit = 15f;
    public float CanopyCloseTime = 1.8f;
    private float AoALimitStrength;
    [UdonSynced(UdonSyncMode.None)] public float Health = 23f;
    public float SeaLevel = -10f;
    [System.NonSerializedAttribute] [HideInInspector] public ConstantForce VehicleConstantForce;
    private float LerpedRoll;
    private float LerpedPitch;
    private float LerpedYaw;
    private Vector2 LStick;
    private Vector2 RStick;
    [System.NonSerializedAttribute] [HideInInspector] public int LStickSelection = 5;
    [System.NonSerializedAttribute] [HideInInspector] public int RStickSelection = 1;
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
    [System.NonSerializedAttribute] [HideInInspector] public float SetSpeed;
    private float SpeedDifference;
    private float SpeedZeroPoint;
    private float SmokeHoldTime;
    private bool SetSmokeLastFrame;
    private Vector3 HandPosSmoke;
    private Vector3 SmokeZeroPoint;
    private Vector3 TempSmokeCol;
    private Vector3 SmokeDifference;
    [System.NonSerializedAttribute] [HideInInspector] public float LTriggerTapTime = 1;
    private bool DoTrim;
    private Vector3 HandPosTrim;
    private Vector3 TrimZeroPoint;
    private Vector2 TempTrim;
    private Vector2 TrimDifference;
    [System.NonSerializedAttribute] [HideInInspector] public Vector2 Trim;
    [System.NonSerializedAttribute] [HideInInspector] public float AirBrakeInput;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public float AirBrake;
    private float RGrip;
    [System.NonSerializedAttribute] [HideInInspector] public bool RGripLastFrame = false;
    private float downspeed;
    private float sidespeed;
    [System.NonSerializedAttribute] [HideInInspector] public float ThrottleInput = 0f;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public float Throttle = 0f;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public Vector3 CurrentVel = new Vector3(0, 0, 0);
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public float Gs = 1f;
    private float roll = 0f;
    private float pitch = 0f;
    private float yaw = 0f;
    [System.NonSerializedAttribute] [HideInInspector] public float FullHealth;
    [System.NonSerializedAttribute] [HideInInspector] public bool Taxiing = false;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool GearUp = false;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool Flaps = true;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool HookDown = false;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool Smoking = false;
    [System.NonSerializedAttribute] [HideInInspector] public float rollinput = 0f;
    [System.NonSerializedAttribute] [HideInInspector] public float pitchinput = 0f;
    [System.NonSerializedAttribute] [HideInInspector] public float yawinput = 0f;
    [System.NonSerializedAttribute] [HideInInspector] public bool Piloting = false;
    [System.NonSerializedAttribute] [HideInInspector] public bool InEditor = true;
    [System.NonSerializedAttribute] [HideInInspector] public bool InVR = false;
    [System.NonSerializedAttribute] [HideInInspector] public bool Passenger = false;
    [System.NonSerializedAttribute] [HideInInspector] public Vector3 LastFrameVel = new Vector3(1, 0, 1);
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool Occupied = false; //this is true if someone is sitting in pilot seat
    [System.NonSerializedAttribute] [HideInInspector] public VRCPlayerApi localPlayer;
    [System.NonSerializedAttribute] [HideInInspector] public bool dead = false;
    [System.NonSerializedAttribute] [HideInInspector] public float AtmoshpereFadeDistance;
    [System.NonSerializedAttribute] [HideInInspector] public float AtmosphereHeightThing;
    public float AtmosphereThinningStart = 12192f; //40,000 feet
    public float AtmosphereThinningEnd = 19812; //65,000 feet
    private float Atmosphere;
    private float rotlift;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public float AngleOfAttack;//MAX of yaw & pitch aoa //used by effectscontroller
    [System.NonSerializedAttribute] [HideInInspector] public float AngleOfAttackPitch;
    [System.NonSerializedAttribute] [HideInInspector] public float AngleOfAttackYaw;
    private float AoALiftYaw;
    private float AoALiftPitch;
    private Vector3 Pitching;
    private Vector3 Yawing;
    [System.NonSerializedAttribute] [HideInInspector] public float Taxiinglerper;
    private float AoALiftYawMin;
    private float AoALiftPitchMin;
    private float SpeedLiftFactor;
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
    private float temp;
    private float minlifttemp;
    private float ReversingPitchStrength;
    private float ReversingYawStrength;
    private float ReversingRollStrength;
    private float ReversingPitchStrengthZero;
    private float ReversingYawStrengthZero;
    private float ReversingRollStrengthZero;
    public bool Cruise;
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
    [System.NonSerializedAttribute] [HideInInspector] public float Hooked;
    private Vector3 HookedLoc;
    private float HookedDrag;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public Vector3 SmokeColor = new Vector3(1, 1, 1);
    [System.NonSerializedAttribute] [HideInInspector] public float Speed;
    [System.NonSerializedAttribute] [HideInInspector] public float AirSpeed;
    [System.NonSerializedAttribute] [HideInInspector] public bool IsOwner = false;
    public Vector3 Wind;
    public Vector3 AirVel;
    private float StillWindMulti;
    private float SoundBarrier;
    public float SoundBarrierStrength = 0.0003f;
    public float SoundBarrierWidth = 20f;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool AfterburnerOn;
    [System.NonSerializedAttribute] [HideInInspector] private float Afterburner = 1;
    [System.NonSerializedAttribute] [HideInInspector] public int CatapultStatus = 0;
    private Vector3 CatapultLockPos;
    private Quaternion CatapultLockRot;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool CanopyOpen = true;
    private float StartPitchStrength;
    [System.NonSerializedAttribute] [HideInInspector] public float CanopyCloseTimer = -100000;
    [UdonSynced(UdonSyncMode.None)] public float Fuel = 7200;
    public float FuelConsumption = 2;
    public float FuelConsumptionABMulti = 4.4f;
    [System.NonSerializedAttribute] [HideInInspector] public float FullFuel;

    //float MouseX;
    //float MouseY;
    //float mouseysens = 1; //mouse input can't be used because it's used to look around even when in a seat
    //float mousexsens = 1;
    private void Start()
    {
        StartPitchStrength = PitchStrength;
        if (AtmosphereThinningStart > AtmosphereThinningEnd) { AtmosphereThinningEnd = AtmosphereThinningStart; }
        VehicleRigidbody = VehicleMainObj.GetComponent<Rigidbody>();
        VehicleConstantForce = VehicleMainObj.GetComponent<ConstantForce>();
        localPlayer = Networking.LocalPlayer;
        if (localPlayer == null) { InEditor = true; Piloting = true; }
        else
        {
            InEditor = false; if (localPlayer.IsUserInVR()) { InVR = true; }
        }

        float scaleratio = CenterOfMass.transform.lossyScale.magnitude / Vector3.one.magnitude;
        VehicleRigidbody.centerOfMass = CenterOfMass.localPosition * scaleratio;//correct position if scaled

        FullHealth = Health;
        FullFuel = Fuel;

        AtmoshpereFadeDistance = (AtmosphereThinningEnd + SeaLevel) - (AtmosphereThinningStart + SeaLevel); //for finding atmosphere thinning gradient
        AtmosphereHeightThing = (AtmosphereThinningStart + SeaLevel) / (AtmoshpereFadeDistance); //used to add back the height to the atmosphere after finding gradient

        //used to set each rotation axis' reversing behaviour to inverted if 0 thrust vectoring, and not inverted if thrust vectoring is non-zero.
        //the variables are called 'Zero' because they ask if thrustvec is set to 0.
        ReversingPitchStrengthZero = PitchThrustVecStr == 0 ? -ReversingPitchStrengthMulti : 1;
        ReversingYawStrengthZero = YawThrustVecStr == 0 ? -ReversingYawStrengthMulti : 1;
        ReversingRollStrengthZero = RollThrustVecStr == 0 ? -ReversingRollStrengthMulti : 1;
    }

    private void LateUpdate()
    {
        if (!InEditor) IsOwner = localPlayer.IsOwner(VehicleMainObj);
        if (GroundDetector != null)
        {
            if ((!GearUp) && Physics.Raycast(GroundDetector.position, GroundDetector.TransformDirection(Vector3.down), .44f, 1))
            {
                Taxiing = true;
            }
            else { Taxiing = false; }
        }

        if (InEditor || IsOwner)//works in editor or ingame
        {
            CurrentVel = VehicleRigidbody.velocity;//because rigidbody values aren't accessable by non-owner players
            Speed = CurrentVel.magnitude;
            AirVel = VehicleRigidbody.velocity - Wind;
            AirSpeed = AirVel.magnitude; ;
            if ((InEditor) || !Piloting) { Occupied = false; } //should make vehicle respawnable if player disconnects while occupying
            AngleOfAttackPitch = Vector3.SignedAngle(VehicleMainObj.transform.forward, AirVel, VehicleMainObj.transform.right);
            AngleOfAttackYaw = Vector3.SignedAngle(VehicleMainObj.transform.forward, AirVel, VehicleMainObj.transform.up);

            //angle of attack stuff, pitch and yaw are calculated seperately
            //pitch and yaw each have a curve for when they are within the 'MaxAngleOfAttack' and a linear version up to 90 degrees, which are Max'd (using Mathf.Clamp) for the final result.
            //the linear version is used for high aoa, and is 0 when at 90 degrees, 1 at 0(multiplied by HighAoaMinControlx). When at more than 90 degrees, the control comes back with the same curve but the inputs are inverted. (unless thrust vectoring is enabled) The invert code is elsewhere.
            AoALiftPitch = Mathf.Min(Mathf.Abs(AngleOfAttackPitch) / MaxAngleOfAttackPitch, Mathf.Abs(Mathf.Abs(AngleOfAttackPitch) - 180) / MaxAngleOfAttackPitch);//angle of attack as 0-1 float, for backwards and forwards
            AoALiftPitch = -AoALiftPitch + 1;
            AoALiftPitch = -Mathf.Pow((1 - AoALiftPitch), AoaCurveStrength) + 1;//give it a curve

            AoALiftPitchMin = Mathf.Min(Mathf.Abs(AngleOfAttackPitch) / 90, Mathf.Abs(Mathf.Abs(AngleOfAttackPitch) - 180) / 90);//linear version to 90 for high aoa
            AoALiftPitchMin = Mathf.Clamp((-AoALiftPitchMin + 1) * HighAoaMinControlPitch, 0, 1);
            AoALiftPitch = Mathf.Clamp(AoALiftPitch, AoALiftPitchMin, 1);

            AoALiftYaw = Mathf.Min(Mathf.Abs(AngleOfAttackYaw) / MaxAngleOfAttackYaw, Mathf.Abs((Mathf.Abs(AngleOfAttackYaw) - 180)) / MaxAngleOfAttackYaw);
            AoALiftYaw = -AoALiftYaw + 1;
            AoALiftYaw = -Mathf.Pow((1 - AoALiftYaw), AoaCurveStrength) + 1;//give it a curve

            AoALiftYawMin = Mathf.Min(Mathf.Abs(AngleOfAttackYaw) / 90, Mathf.Abs(Mathf.Abs(AngleOfAttackYaw) - 180) / 90);//linear version to 90 for high aoa
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
            SpeedLiftFactor = Mathf.Clamp(AirVel.magnitude * AirVel.magnitude * Lift, 0, MaxLift);
            rotlift = AirSpeed / RotMultiMaxSpeed;//using a simple linear curve for increasing control as you move faster



            if (Piloting)
            {
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

                CanopyCloseTimer -= Time.deltaTime;

                //RStick Selection wheel
                if (RStick.magnitude > .8f)
                {
                    float stickdir = Vector2.SignedAngle(new Vector2(-0.382683432365f, 0.923879532511f), RStick);

                    if (stickdir > 135)//down
                    {
                        RStickSelection = 5;
                    }
                    else if (stickdir > 90)//downleft
                    {
                        RStickSelection = 6;
                    }
                    else if (stickdir > 45)//left
                    {
                        RStickSelection = 7;
                    }
                    else if (stickdir > 0)//upleft
                    {
                        RStickSelection = 8;
                    }
                    else if (stickdir > -45)//up
                    {
                        RStickSelection = 1;
                    }
                    else if (stickdir > -90)//upright
                    {
                        RStickSelection = 2;
                    }
                    else if (stickdir > -135)//right
                    {
                        RStickSelection = 3;
                    }
                    else//downright
                    {
                        RStickSelection = 4;
                    }
                }

                //LStick Selection wheel
                if (LStick.magnitude > .8f)
                {
                    float stickdir = Vector2.SignedAngle(new Vector2(-0.382683432365f, 0.923879532511f), LStick);

                    if (stickdir > 135)//down
                    {
                        LStickSelection = 5;
                    }
                    else if (stickdir > 90)//downleft
                    {
                        LStickSelection = 6;
                    }
                    else if (stickdir > 45)//left
                    {
                        LStickSelection = 7;
                    }
                    else if (stickdir > 0)//upleft
                    {
                        LStickSelection = 8;
                    }
                    else if (stickdir > -45)//up
                    {
                        LStickSelection = 1;
                    }
                    else if (stickdir > -90)//upright
                    {
                        LStickSelection = 2;
                    }
                    else if (stickdir > -135)//right
                    {
                        LStickSelection = 3;
                    }
                    else//downright
                    {
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
                if (LTrigger > 0.75 || (Input.GetKey(KeyCode.Alpha4)))
                {
                    switch (LStickSelection)
                    {
                        case 1://set speed
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
                                SpeedDifference = (SpeedZeroPoint - handpos) * -600;
                                SetSpeed = Mathf.Clamp(TempSpeed + SpeedDifference, 0, 2000);

                            }

                            AirBrakeInput = 0;
                            LTriggerLastFrame = true;
                            break;
                        case 2://LIMIT
                            if (!LTriggerLastFrame)
                            {
                                FlightLimitsEnabled = !FlightLimitsEnabled;
                            }

                            LTriggerLastFrame = true;
                            AirBrakeInput = 0;
                            break;
                        case 3://CATAPULT
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

                            AirBrakeInput = 0;
                            LTriggerLastFrame = true;
                            break;
                        case 4://HOOK
                            if (!LTriggerLastFrame)
                            {
                                if (HookDetector != null)
                                {
                                    HookDown = !HookDown;
                                }
                                Hooked = 0;
                            }

                            AirBrakeInput = 0;
                            LTriggerLastFrame = true;
                            break;
                        case 5://Brake done elsewhere because it's analog

                            LTriggerLastFrame = true;
                            break;
                        case 6://Trim
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

                            AirBrakeInput = 0;
                            LTriggerLastFrame = true;
                            break;
                        case 7://Canopy
                            if (!LTriggerLastFrame)
                            {
                                if (Speed < 20)
                                {
                                    if (CanopyCloseTimer < -100000 + CanopyCloseTime)
                                    {
                                        CanopyOpen = false;
                                        if (InEditor) CanopyClosing();
                                        else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CanopyClosing"); }
                                    }
                                    else if (CanopyCloseTimer < 0 + CanopyCloseTime && CanopyCloseTimer > -10000 + CanopyCloseTime + 1)
                                    {
                                        CanopyOpen = true;
                                        if (InEditor) CanopyOpening();
                                        else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CanopyOpening"); }
                                    }
                                }
                            }
                            AirBrakeInput = 0;
                            LTriggerLastFrame = true;
                            break;
                        case 8://Afterburner
                            if (!LTriggerLastFrame)
                            {
                                AfterburnerOn = !AfterburnerOn;
                                if (AfterburnerOn)
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

                            AirBrakeInput = 0;
                            LTriggerLastFrame = true;
                            break;
                    }
                }
                else
                {
                    LTriggerLastFrame = false;
                }

                if (LStickSelection == 5)
                {
                    if (Input.GetKey(KeyCode.Alpha4))
                    {
                        Bf = 1;
                    }
                    else
                    {
                        Bf = 0;
                    }
                    AirBrakeInput = LTrigger;
                }
                else
                {
                    Bf = 0;
                }
                AirBrake = Mathf.Max(AirBrakeInput, Bf);

                if (RTrigger > 0.75 || (Input.GetKey(KeyCode.Alpha5)))
                {
                    switch (RStickSelection)
                    {
                        case 1://machinegun

                            EffectsControl.IsFiringGun = true;
                            RTriggerLastFrame = true;
                            break;
                        case 2://AirtoAirMissile

                            EffectsControl.IsFiringGun = false;
                            RTriggerLastFrame = true;
                            break;
                        case 3://AGM/bomb

                            EffectsControl.IsFiringGun = false;
                            RTriggerLastFrame = true;
                            break;
                        case 4://altitude hold
                            if (!RTriggerLastFrame) LevelFlight = !LevelFlight;

                            EffectsControl.IsFiringGun = false;
                            RTriggerLastFrame = true;
                            break;
                        case 5://GEAR
                            if (!RTriggerLastFrame) { GearUp = !GearUp; }

                            EffectsControl.IsFiringGun = false;
                            RTriggerLastFrame = true;
                            break;
                        case 6://flaps
                            if (!RTriggerLastFrame) Flaps = !Flaps;

                            EffectsControl.IsFiringGun = false;
                            RTriggerLastFrame = true;
                            break;
                        case 7://smoke
                               //you can change smoke colour by holding down the trigger and waving your hand around. x/y/z = r/g/b
                            if (!RTriggerLastFrame)
                            {
                                if (InVR)
                                {
                                    HandPosSmoke = VehicleMainObj.transform.InverseTransformPoint(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position);
                                    SmokeZeroPoint = HandPosSmoke;
                                    TempSmokeCol = SmokeColor;
                                }
                                Smoking = !Smoking;
                                SmokeHoldTime = 0;
                            }
                            if (InVR)
                            {
                                SmokeHoldTime += Time.deltaTime;
                                if (SmokeHoldTime > 1)
                                {

                                    //VR Set Smoke
                                    HandPosSmoke = VehicleMainObj.transform.InverseTransformPoint(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position);

                                    SmokeDifference = (SmokeZeroPoint - HandPosSmoke) * 8f;
                                    SmokeColor.x = Mathf.Clamp(TempSmokeCol.x + SmokeDifference.x, 0, 1);
                                    SmokeColor.y = Mathf.Clamp(TempSmokeCol.y + SmokeDifference.y, 0, 1);
                                    SmokeColor.z = Mathf.Clamp(TempSmokeCol.z + SmokeDifference.z, 0, 1);
                                    if (SmokeColor.magnitude < .3)
                                    {
                                        SmokeColor = SmokeColor.normalized * 0.3f;
                                    }
                                }
                            }


                            EffectsControl.IsFiringGun = false;
                            RTriggerLastFrame = true;
                            break;
                        case 8://flares
                            if (!RTriggerLastFrame)
                            {
                                if (InEditor) { EffectsControl.PlaneAnimator.SetTrigger("flares"); }//editor
                                else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "DropFlares"); }//ingame
                            }

                            LevelFlight = false;
                            EffectsControl.IsFiringGun = false;
                            RTriggerLastFrame = true;
                            break;
                    }
                }
                else
                {

                    EffectsControl.IsFiringGun = false;
                    RTriggerLastFrame = false;
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
                    JoystickPos = (JoystickDifference * VehicleMainObj.transform.up);
                    VRPitchRollInput = new Vector2(JoystickPos.x, JoystickPos.z) * 1.41421f;

                    RGripLastFrame = true;
                    //making a circular joy stick square
                    if (Mathf.Abs(VRPitchRollInput.x) > Mathf.Abs(VRPitchRollInput.y))
                    {
                        if (Mathf.Abs(VRPitchRollInput.x) != 0)
                        {
                            temp = VRPitchRollInput.magnitude / Mathf.Abs(VRPitchRollInput.x);
                            VRPitchRollInput *= temp;
                        }
                    }
                    else
                    {
                        if (Mathf.Abs(VRPitchRollInput.y) != 0)
                        {
                            temp = VRPitchRollInput.magnitude / Mathf.Abs(VRPitchRollInput.y);
                            VRPitchRollInput *= temp;
                        }
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
                        TempThrottle = ThrottleInput;
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
                    ThrottleInput = PlayerThrottle;//for throttle effects
                }
                Fuel = Mathf.Clamp(Fuel - ((FuelConsumption * Mathf.Max(ThrottleInput, 0.35f)) * Time.deltaTime), 0, FullFuel);
                if (Fuel < 200) ThrottleInput = Mathf.Clamp(ThrottleInput * (Fuel / 200), 0, 1);
                if (ThrottleInput < .6f) { AfterburnerOn = false; Afterburner = 1; }

                //Altitude hold PID Controller
                if (LevelFlight && !RGripLastFrame)
                {
                    FlightLimitsEnabled = true;
                    Vector3 straight = new Vector3(VehicleRigidbody.velocity.x, 0, VehicleRigidbody.velocity.z);
                    Vector3 tempaxis = VehicleMainObj.transform.right;
                    tempaxis.x = 0;
                    float error = CurrentVel.normalized.y;//(Vector3.Dot(VehicleRigidbody.velocity.normalized, Vector3.up));

                    AltHoldPitchIntegrator += error * Time.deltaTime;
                    AltHoldPitchIntegrator = Mathf.Clamp(AltHoldPitchIntegrator, AltHoldPitchIntegratorMin, AltHoldPitchIntegratorMax);

                    AltHoldPitchDerivator = ((error - AltHoldPitchlastframeerror) / Time.deltaTime);

                    AltHoldPitchlastframeerror = error;

                    pitchinput = AltHoldPitchProportional * error;

                    pitchinput += AltHoldPitchIntegral * AltHoldPitchIntegrator;

                    pitchinput += AltHoldPitchDerivative * AltHoldPitchDerivator; //works but spazzes out real bad

                    pitchinput = Mathf.Clamp(pitchinput, -1, 1);

                    AltHoldPitchlastframeerror = error;

                    //Roll
                    error = VehicleMainObj.transform.localEulerAngles.z;
                    if (error > 180) { error -= 360; }

                    //lock upside down if rotated more than 90
                    if (error > 90)
                    {
                        error -= 180;
                        pitchinput *= -1;
                    }
                    else if (error < -90)
                    {
                        error += 180;
                        pitchinput *= -1;
                    }

                    rollinput = Mathf.Clamp(AltHoldRollProportional * error, -1, 1);

                    yawinput = 0;
                }
                else
                {
                    if (FlightLimitsEnabled && !Taxiing && AngleOfAttack < AoALimit)
                    {
                        float Limits = Mathf.Min(GLimitStrength, AoALimitStrength);
                        GLimitStrength = Mathf.Clamp(-(Gs / GLimit) + 1, 0, 1);
                        AoALimitStrength = Mathf.Clamp(-(Mathf.Abs(AngleOfAttack) / AoALimit) + 1, 0, 1);
                        pitchinput = Mathf.Clamp(/*(MouseY * mouseysens + Lstick.y + */VRPitchRollInput.y + Wf + Sf + downf + upf, -1, 1) * Limits;
                        yawinput = Mathf.Clamp(Qf + Ef + JoystickPosYaw.x, -1, 1) * Limits;
                    }
                    else
                    {
                        pitchinput = Mathf.Clamp(/*(MouseY * mouseysens + Lstick.y + */VRPitchRollInput.y + Wf + Sf + downf + upf, -1, 1);
                        yawinput = Mathf.Clamp(Qf + Ef + JoystickPosYaw.x, -1, 1);
                    }
                    rollinput = Mathf.Clamp(((/*(MouseX * mousexsens) + */VRPitchRollInput.x + Af + Df + leftf + rightf) * -1), -1, 1);
                }
                //combine inputs and clamp
                //these are used by effectscontroller


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
                else//moving backward. The 'Zero' values are set in start().
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

                //if touching ground rotate if trying to turn
                if (Taxiing)
                {
                    Taxiinglerper = Mathf.Lerp(Taxiinglerper, yawinput * TaxiRotationSpeed * Time.deltaTime, TaxiRotationResponse * Time.deltaTime);
                    VehicleMainObj.transform.Rotate(Vector3.up, Taxiinglerper);
                    StillWindMulti = Mathf.Clamp(Speed / 10, 0, 1);

                    PitchStrength = StartPitchStrength + (TakeoffAssist * rotlift);//stronger pitch when moving fast and taxiing to help with taking off
                    //check for catapult below us
                    if (Speed < 15 && CatapultStatus == 0)
                    {
                        RaycastHit[] hit = Physics.RaycastAll(CatapultDetector.position, CatapultDetector.TransformDirection(Vector3.down), .44f, CatapultLayer);
                        if (hit.Length > 0)
                        {
                            //it should use Raycast rather than RaycastAll but udon doesn't return the hit fully from raycast it seems.
                            /*      RaycastHit hit;
                                    Physics.Raycast(GroundDetector.position, GroundDetector.TransformDirection(Vector3.down), out hit, .44f, 1);
                                    GameObject HitObject = hit.transform.gameObject;
                                    Debug.Log(HitObject.transform.position); */
                            Transform CatapultTrigger = hit[0].transform;//get the transform from the trigger hit
                            if (Vector3.Angle(VehicleMainObj.transform.forward, CatapultTrigger.transform.forward) < 15)//Hit detected!, check if the plane is facing in the right direction..
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
                                CatapultStatus = 1;//locked to catapult

                                if (SoundControl != null)
                                {
                                    //SoundControl.CatapultLock.play();
                                }
                            }
                        }
                    }
                }
                else
                {
                    PitchStrength = StartPitchStrength;
                    StillWindMulti = 1;
                    Taxiinglerper = 0;
                }
                if (CanopyOpen && Speed > 20)
                {
                    if (CanopyCloseTimer < -100000 + CanopyCloseTime)
                    {
                        CanopyOpen = false;
                        if (InEditor) CanopyClosing();
                        else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CanopyClosing"); }
                    }
                }

                pitch = Mathf.Clamp(pitchinput + Trim.x, -1, 1) * PitchStrength * ReversingPitchStrength;
                yaw = Mathf.Clamp(-yawinput - Trim.y, -1, 1) * YawStrength * ReversingYawStrength;
                roll = rollinput * RollStrength * ReversingRollStrength;


                if (pitch > 0)
                {
                    pitch *= PitchDownStrMulti;
                }
            }
            else
            {
                roll = 0;
                pitch = 0;
                yaw = 0;
                rollinput = 0;
                pitchinput = Trim.x;
                yawinput = Trim.y;
                ThrottleInput = 0;
            }

            //thrust vecotring airplanes have a minimum rotation speed
            minlifttemp = rotlift * Mathf.Min(AoALiftPitch, AoALiftYaw);
            pitch *= Mathf.Max(PitchThrustVecStr, minlifttemp);
            yaw *= Mathf.Max(YawThrustVecStr, minlifttemp);
            roll *= Mathf.Max(RollThrustVecStr, minlifttemp);



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

            if (HookDown)
            {
                if (Physics.Raycast(HookDetector.position, Vector3.down, 2f, HookRopeLayer) && Hooked < 0 && Speed > 25)
                {
                    HookedLoc = VehicleMainObj.transform.position;
                    Hooked = 6f;
                    if (EffectsControl != null)
                    {
                        EffectsControl.PlaneAnimator.SetTrigger("hooked");
                    }
                }
                Hooked -= Time.deltaTime;
            }
            if (Hooked > 0f && Taxiing)
            {
                if (Vector3.Distance(VehicleMainObj.transform.position, HookedLoc) > 90)//real planes take around 80-90 meters to stop on a carrier
                {
                    //if you go further than 90m you snap the rope and it hurts your plane by the % of the amount of time left of the 2 seconds it should have taken to stop you.
                    float damage = 0;
                    if (Hooked > 4)
                    {
                        damage = ((Hooked - 4) / 2) * FullHealth;
                    }
                    Health -= damage;
                    Hooked = 0;
                    //Debug.Log("snap");
                }
                if (Speed > HookedBrakeStrength * Time.deltaTime)
                {
                    VehicleRigidbody.velocity += -CurrentVel.normalized * HookedBrakeStrength * Time.deltaTime;
                }
                else
                {
                    VehicleRigidbody.velocity = Vector3.zero;
                }
                //Debug.Log("hooked");
            }
            else
            {
                HookedDrag = 0;
            }

            //flaps drag and lift
            FlapsDrag = FlapsDragMulti;
            FlapsLift = FlapsLiftMulti;
            if (!Flaps)
            {
                FlapsDrag = 1;
                FlapsLift = 1;
            }
            //gear drag
            if (GearUp)
            {
                GearDrag = 1;
            }
            else
            {
                GearDrag = LandingGearDragMulti;
            }
            FlapsGearBrakeDrag = (GearDrag + FlapsDrag + (AirBrake * AirbrakeStrength)) - 1;

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
                    Pitching = (VehicleMainObj.transform.up * LerpedPitch * Atmosphere + (VehicleMainObj.transform.up * downspeed * VelStraightenStrPitch * AoALiftPitch * rotlift)) * 90;
                    Yawing = (VehicleMainObj.transform.right * LerpedYaw * Atmosphere + (-VehicleMainObj.transform.right * sidespeed * VelStraightenStrYaw * AoALiftYaw * rotlift)) * 90;

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
            //AirVel = VehicleRigidbody.velocity - Wind;
            //AirSpeed = AirVel.magnitude;
        }
    }
    private void FixedUpdate()
    {
        if (InEditor || IsOwner)
        {
            //lerp velocity toward 0 to simulate air friction
            VehicleRigidbody.velocity = Vector3.Lerp(VehicleRigidbody.velocity, Wind * StillWindMulti, (((((AirFriction * FlapsGearBrakeDrag) * Atmosphere)) + SoundBarrier) * 90) * Time.deltaTime);
            if (Piloting)
            {
                //apply pitching
                VehicleRigidbody.AddForceAtPosition(Pitching * Time.deltaTime, PitchMoment.position, ForceMode.Force);
                //apply yawing
                VehicleRigidbody.AddForceAtPosition(Yawing * Time.deltaTime, YawMoment.position, ForceMode.Force);
            }
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
}
