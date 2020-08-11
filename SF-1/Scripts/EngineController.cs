
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
    public Transform GroundDetector;
    public Transform CenterOfMass;
    public Transform PitchMoment;
    public Transform YawMoment;
    public float ThrottleStrength = 25f;
    public float AccelerationResponse = 4.5f;
    public float EngineSpoolDownSpeedMulti = .5f;
    public float AirFriction = 0.036f;
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
    public float Lift = 0.000112f;
    public float SidewaysLift = .17f;
    public float MaxVelLift = 10f;
    public float VelPullUp = 1f;
    public float LandingGearDragMulti = 1.6f;
    public float FlapsDragMulti = 1.8f;
    public float FlapsLiftMulti = 1.35f;
    public float AirbrakeStrength = 3f;
    [System.NonSerializedAttribute] [HideInInspector] public bool SafeFlightLimitsEnabled = true;
    private float GLimitStrength;
    public float GLimit;
    public float AoALimit;
    private float AoALimitStrength;
    [UdonSynced(UdonSyncMode.None)] public float Health = 100f;
    public float SeaLevel = -100;
    private ConstantForce VehicleConstantForce;
    private float InputAcc;
    private float LerpedRoll;
    private float LerpedPitch;
    private float LerpedYaw;
    private Vector2 LStick;
    private Vector2 RStick;
    [System.NonSerializedAttribute] [HideInInspector] public int RStickSelection = 0;
    [System.NonSerializedAttribute] [HideInInspector] public int LStickSelection = 0;
    private Vector2 VRRollYawInput;
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
    [System.NonSerializedAttribute] [HideInInspector] public float VRThrottle;
    private float TempThrottle;
    private float handpos;
    private float ThrottleZeroPoint;
    private float TempSpeed;
    [System.NonSerializedAttribute] [HideInInspector] public float SetSpeed;
    private float SpeedDifference;
    private float SpeedZeroPoint;
    private float holdtime;
    private bool SetSmokeLastFrame;
    private Vector3 handpossmoke;
    private Vector3 SmokeZeroPoint;
    private Vector3 TempSmokeCol;
    private Vector3 SmokeDifference;
    private float AirBrakeInput;
    [System.NonSerializedAttribute] [HideInInspector] public bool SetSpeedLast = false;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public float AirBrake;
    private float AirBrakeDrag;
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
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public float AngleOfAttack;//MAX of yaw & pitch //used by effectscontroller
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
    private float GearDrag;
    private float FlapsGearDrag;
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
    private float CruiseProportional = .1f;
    private float CruiseIntegral = .1f;
    //private float Derivative;
    private float CruiseIntegrator;
    private float CruiseIntegratorMax = 5;
    private float CruiseIntegratorMin = -5;
    private float Cruiselastframeerror;
    private float AltHoldPitchProportional = 1f;
    private float AltHoldPitchIntegral = 1f;
    private float AltHoldPitchIntegrator;
    private float AltHoldPitchIntegratorMax = .01f;
    private float AltHoldPitchIntegratorMin = -.01f;
    private float AltHoldPitchDerivative = 4;
    private float AltHoldPitchDerivator;
    private float AltHoldPitchlastframeerror;
    private float AltHoldRollProportional = -.005f;
    private bool LevelFlight;

    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public Vector3 SmokeColor = new Vector3(1, 1, 1);

    //float MouseX;
    //float MouseY;
    //float mouseysens = 1; //mouse input can't be used because it's used to look around even when in a seat
    //float mousexsens = 1;
    private void Start()
    {
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
        if (GroundDetector != null)
        {
            if (Physics.Raycast(GroundDetector.position, GroundDetector.TransformDirection(Vector3.down), .44f, 1) && (!GearUp))
            {
                Taxiing = true;
            }
            else { Taxiing = false; }
        }

        if (InEditor || (localPlayer.IsOwner(VehicleMainObj)))//works in editor or ingame
        {
            CurrentVel = VehicleRigidbody.velocity;//because rigidbody values aren't accessable by non-owner players
            if ((InEditor) || !Piloting) { Occupied = false; } //should make vehicle respawnable if player disconnects while occupying

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
                LStick.x = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
                LStick.y = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
                RStick.x = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
                RStick.y = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
                LGrip = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger");
                RGrip = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
                LTrigger = LTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
                //MouseX = Input.GetAxisRaw("Mouse X");
                //MouseY = Input.GetAxisRaw("Mouse Y");

                //RStick Selection wheel
                if (RStick.magnitude > .5f)
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
                if (LStick.magnitude > .5f)
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

                if (LTrigger > 0.75 || (Input.GetKey(KeyCode.Alpha4)))
                {
                    switch (LStickSelection)
                    {
                        case 0://nothing
                            break;
                        case 1://set speed

                            if (!SetSpeedLast)
                            {
                                SetSpeed = CurrentVel.magnitude;
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

                            SetSpeedLast = true;

                            AirBrakeInput = 0;
                            LTriggerLastFrame = true;
                            break;
                        case 2://Nothing yet
                            AirBrakeInput = 0;
                            SetSpeedLast = false;
                            break;
                        case 3://SAFE
                            if (!LTriggerLastFrame)
                            {
                                SafeFlightLimitsEnabled = !SafeFlightLimitsEnabled;
                            }

                            AirBrakeInput = 0;
                            SetSpeedLast = false;
                            LTriggerLastFrame = true;
                            break;
                        case 4://HOOK

                            AirBrakeInput = 0;
                            SetSpeedLast = false;
                            LTriggerLastFrame = true;
                            break;
                        case 5://Brake done elsewhere because it's analog

                            SetSpeedLast = false;
                            LTriggerLastFrame = true;
                            break;
                        case 6://Trim

                            AirBrakeInput = 0;
                            SetSpeedLast = false;
                            LTriggerLastFrame = true;
                            break;
                        case 7://eject?

                            AirBrakeInput = 0;
                            SetSpeedLast = false;
                            LTriggerLastFrame = true;
                            break;
                        case 8://Afterburner

                            AirBrakeInput = 0;
                            SetSpeedLast = false;
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

                AirBrake = Mathf.Lerp(AirBrake, Mathf.Max(AirBrakeInput, Bf), 5 * Time.deltaTime);










                RTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");

                if (RTrigger > 0.75 || (Input.GetKey(KeyCode.Alpha5)))
                {
                    switch (RStickSelection)
                    {
                        case 0://nothing

                            EffectsControl.IsFiringGun = false;
                            RTriggerLastFrame = true;
                            break;
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
                            if (!RTriggerLastFrame) GearUp = !GearUp;

                            EffectsControl.IsFiringGun = false;
                            RTriggerLastFrame = true;
                            break;
                        case 6://flaps
                            if (!RTriggerLastFrame) Flaps = !Flaps;

                            EffectsControl.IsFiringGun = false;
                            RTriggerLastFrame = true;
                            break;
                        case 7://smoke
                            if (!RTriggerLastFrame)
                            {
                                if (InVR)
                                {
                                    handpossmoke = VehicleMainObj.transform.InverseTransformPoint(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position);
                                    SmokeZeroPoint = handpossmoke;
                                    TempSmokeCol = SmokeColor;
                                }
                                EffectsControl.Smoking = !EffectsControl.Smoking;
                                holdtime = 0;
                            }
                            if (InVR)
                            {
                                holdtime += Time.deltaTime;
                                if (holdtime > 1)
                                {

                                    //VR Set Smoke
                                    handpossmoke = VehicleMainObj.transform.InverseTransformPoint(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position);

                                    SmokeDifference = (SmokeZeroPoint - handpossmoke) * 8f;
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
                    if (!RGripLastFrame)//first frame you pressed it?
                    {
                        PlaneRotDif = Quaternion.identity;
                        JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;//rotation of the controller relative to the plane when it was pressed
                    }
                    //difference between the plane and the hand's rotation, and then the difference between that and the JoystickZeroPoint
                    JoystickDifference = (Quaternion.Inverse(VehicleMainObj.transform.rotation) * localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation) * Quaternion.Inverse(JoystickZeroPoint);
                    JoystickPosYaw = (JoystickDifference * VehicleMainObj.transform.forward);//angles to vector
                    JoystickPos = (JoystickDifference * VehicleMainObj.transform.up);
                    VRRollYawInput = new Vector2(JoystickPos.x, JoystickPos.z) * 1.41421f;//easier just to override the right stick, this way the squaring gets applied too

                    RGripLastFrame = true;
                    //making a circular joy stick square
                    if (Mathf.Abs(VRRollYawInput.x) > Mathf.Abs(VRRollYawInput.y))
                    {
                        if (Mathf.Abs(VRRollYawInput.x) != 0)
                        {
                            temp = VRRollYawInput.magnitude / Mathf.Abs(VRRollYawInput.x);
                            VRRollYawInput *= temp;
                        }
                    }
                    else
                    {
                        if (Mathf.Abs(VRRollYawInput.y) != 0)
                        {
                            temp = VRRollYawInput.magnitude / Mathf.Abs(VRRollYawInput.y);
                            VRRollYawInput *= temp;
                        }
                    }
                }
                else
                {
                    JoystickPosYaw.x = 0;
                    VRRollYawInput = Vector3.zero;
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

                    VRThrottle = Mathf.Clamp(TempThrottle + ThrottleDifference, 0, 1);
                    LGripLastFrame = true;
                }
                else
                {
                    LGripLastFrame = false;
                }


                //SetSpeed PI Controller
                if (SetSpeedLast && !LGripLastFrame)
                {
                    SetSpeed += Shiftf * 90 * Time.deltaTime;
                    if (Input.GetKey(KeyCode.LeftControl)) SetSpeed -= 90 * Time.deltaTime;

                    float error = (SetSpeed - CurrentVel.magnitude);

                    CruiseIntegrator += error * Time.deltaTime;
                    CruiseIntegrator = Mathf.Clamp(CruiseIntegrator, CruiseIntegratorMin, CruiseIntegratorMax);

                    //float Derivator = Mathf.Clamp(((error - lastframeerror) / Time.deltaTime),DerivMin, DerivMax);

                    ThrottleInput = CruiseProportional * error;
                    ThrottleInput += CruiseIntegral * CruiseIntegrator;
                    //ThrottleInput += Derivative * Derivator; //works but spazzes out real bad
                    ThrottleInput = Mathf.Clamp(ThrottleInput, 0, 1);


                    Cruiselastframeerror = error;
                }
                else
                {
                    ThrottleInput = (Mathf.Max(VRThrottle, Shiftf)); //for throttle effects
                }











                if (LevelFlight && !RGripLastFrame)
                {
                    SafeFlightLimitsEnabled = true;
                    Vector3 straight = new Vector3(VehicleRigidbody.velocity.x, 0, VehicleRigidbody.velocity.z);
                    Vector3 tempaxis = VehicleMainObj.transform.right;
                    tempaxis.x = 0;
                    float error = (Vector3.Dot(VehicleRigidbody.velocity.normalized, Vector3.up));

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
                    if (SafeFlightLimitsEnabled && !Taxiing && AngleOfAttack < AoALimit)
                    {
                        GLimitStrength = Mathf.Clamp(-(Gs / GLimit) + 1, 0, 1);
                        AoALimitStrength = Mathf.Clamp(-(Mathf.Abs(AngleOfAttack) / AoALimit) + 1, 0, 1);
                        pitchinput = Mathf.Clamp((/*(MouseY * mouseysens + Lstick.y + */VRRollYawInput.y + Wf + Sf + downf + upf), -1, 1) * Mathf.Min(GLimitStrength, AoALimitStrength);
                        yawinput = Mathf.Clamp((Qf + Ef + JoystickPosYaw.x), -1, 1) * Mathf.Min(GLimitStrength, AoALimitStrength);
                    }
                    else
                    {
                        pitchinput = Mathf.Clamp((/*(MouseY * mouseysens + Lstick.y + */VRRollYawInput.y + Wf + Sf + downf + upf), -1, 1);
                        yawinput = Mathf.Clamp((Qf + Ef + JoystickPosYaw.x), -1, 1);
                    }
                    rollinput = Mathf.Clamp(((/*(MouseX * mousexsens) + */VRRollYawInput.x + Af + Df + leftf + rightf) * -1), -1, 1);
                }
                //combine inputs and clamp
                //these are used by effectscontroller


                //ability to adjust input to be more precise at low amounts. 'exponant'
                /* pitchinput = pitchinput > 0 ? Mathf.Pow(pitchinput, StickInputPower) : -Mathf.Pow(Mathf.Abs(pitchinput), StickInputPower);
                yawinput = yawinput > 0 ? Mathf.Pow(yawinput, StickInputPower) : -Mathf.Pow(Mathf.Abs(yawinput), StickInputPower);
                rollinput = rollinput > 0 ? Mathf.Pow(rollinput, StickInputPower) : -Mathf.Pow(Mathf.Abs(rollinput), StickInputPower); */

                //if moving backwards, controls invert (if thrustvectoring is set to 0 strength for that axis)
                if ((Vector3.Dot(VehicleRigidbody.velocity, VehicleMainObj.transform.forward) > 0))//normal, moving forward
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

                pitch = pitchinput * PitchStrength * ReversingPitchStrength;
                yaw = -yawinput * YawStrength * ReversingYawStrength;
                roll = rollinput * RollStrength * ReversingRollStrength;

                //flip ur Vehicle to upright and stop rotating
                /*if (Input.GetButtonDown("Oculus_CrossPlatform_Button2") || (Input.GetKeyDown(KeyCode.T)))
                 {
                     VehicleMainObj.transform.rotation = Quaternion.Euler(VehicleMainObj.transform.rotation.eulerAngles.x, VehicleMainObj.transform.rotation.eulerAngles.y, 0f);
                     VehicleRigidbody.angularVelocity *= .3f;
                 }*/

                if (pitch > 0)
                {
                    pitch *= PitchDownStrMulti;
                }
                //check of touching ground then rotate if trying to turn

                if (Taxiing)
                {
                    Taxiinglerper = Mathf.Lerp(Taxiinglerper, yawinput * TaxiRotationSpeed * Time.deltaTime, TaxiRotationResponse * Time.deltaTime);
                    VehicleMainObj.transform.Rotate(Vector3.up, Taxiinglerper);
                }
                else
                {
                    Taxiinglerper = 0;
                }

            }
            else
            {
                roll = 0;
                pitch = 0;
                yaw = 0;
                rollinput = 0;
                pitchinput = 0;
                yawinput = 0;
                ThrottleInput = 0;
            }
            //used to create air resistance only in the relative down direction
            downspeed = Vector3.Dot(VehicleRigidbody.velocity, VehicleMainObj.transform.up) * -1;
            if (downspeed < 0)
            {
                downspeed *= PitchDownLiftMulti;
            }

            AngleOfAttackPitch = Vector3.SignedAngle(VehicleMainObj.transform.forward, CurrentVel, VehicleMainObj.transform.right);
            AngleOfAttackYaw = Vector3.SignedAngle(VehicleMainObj.transform.forward, CurrentVel, VehicleMainObj.transform.up);

            //angle of attack stuff, pitch and yaw are calculated seperately
            //pitch and yaw each have a curve for when they are within the 'MaxAngleOfAttack' and a linear version up to 90 degrees, which are Max'd (using Mathf.Clamp) for the final result.
            //the linear version is used for high aoa, and is 0 when at 90 degrees, 1 at 0(multiplied by HighAoaMinControlx). When at more than 90 degrees, the control comes back with the same curve but the inputs are inverted. (unless thrust vectoring is enabled) The invert code is elsewhere.
            AoALiftPitch = Mathf.Min(Mathf.Abs(AngleOfAttackPitch) / MaxAngleOfAttackPitch, Mathf.Abs(Mathf.Abs(AngleOfAttackPitch) - 180) / MaxAngleOfAttackPitch);//angle of attack as 0-1 float, for backwards and forwards
            AoALiftPitch = -AoALiftPitch;
            AoALiftPitch++;
            AoALiftPitch = -Mathf.Pow((1 - AoALiftPitch), AoaCurveStrength) + 1;//give it a curve

            AoALiftPitchMin = Mathf.Min(Mathf.Abs(AngleOfAttackPitch) / 90, Mathf.Abs(Mathf.Abs(AngleOfAttackPitch) - 180) / 90);//linear version to 90 for high aoa
            AoALiftPitchMin = -AoALiftPitchMin;
            AoALiftPitchMin++;
            AoALiftPitchMin *= HighAoaMinControlPitch;
            AoALiftPitchMin = Mathf.Clamp(AoALiftPitchMin, 0, 1);
            AoALiftPitch = Mathf.Clamp(AoALiftPitch, AoALiftPitchMin, 1);

            AoALiftYaw = Mathf.Min(Mathf.Abs(AngleOfAttackYaw) / MaxAngleOfAttackYaw, Mathf.Abs((Mathf.Abs(AngleOfAttackYaw) - 180)) / MaxAngleOfAttackYaw);
            AoALiftYaw = -AoALiftYaw;
            AoALiftYaw++;
            AoALiftYaw = -Mathf.Pow((1 - AoALiftYaw), AoaCurveStrength) + 1;//give it a curve

            AoALiftYawMin = Mathf.Min(Mathf.Abs(AngleOfAttackYaw) / 90, Mathf.Abs(Mathf.Abs(AngleOfAttackYaw) - 180) / 90);//linear version to 90 for high aoa
            AoALiftYawMin = -AoALiftPitchMin;
            AoALiftYawMin++;
            AoALiftYawMin *= HighAoaMinControlYaw;
            AoALiftYawMin = Mathf.Clamp(AoALiftYawMin, 0, 1);
            AoALiftYaw = Mathf.Clamp(AoALiftYaw, AoALiftYawMin, 1);

            AngleOfAttack = Mathf.Max(AngleOfAttackPitch, AngleOfAttackYaw);
            //speed related values
            SpeedLiftFactor = Mathf.Clamp(CurrentVel.magnitude * CurrentVel.magnitude * Lift, 0, MaxVelLift);
            rotlift = CurrentVel.magnitude / RotMultiMaxSpeed;//using a simple linear curve for increasing control as you move faster

            //thrust vecotring airplanes have a minimum rotation speed
            minlifttemp = rotlift * Mathf.Min(AoALiftPitch, AoALiftYaw);
            pitch *= Mathf.Max(PitchThrustVecStr, minlifttemp);
            yaw *= Mathf.Max(YawThrustVecStr, minlifttemp);
            roll *= Mathf.Max(RollThrustVecStr, minlifttemp);



            //rotation inputs are done, now we can set the minimum lift/drag when at high aoa, this should be high than 0 because if it's 0 you will have 0 drag when at 90 degree AoA.
            AoALiftPitch = Mathf.Clamp(AoALiftPitch, HighPitchAoaMinLift, 1);
            AoALiftYaw = Mathf.Clamp(AoALiftYaw, HighYawAoaMinLift, 1);


            Atmosphere = Mathf.Clamp(-(CenterOfMass.position.y / AtmoshpereFadeDistance) + 1 + AtmosphereHeightThing, 0, 1);

            //used to add physics to plane's yaw (accel angvel towards velocity)
            sidespeed = Vector3.Dot(VehicleRigidbody.velocity, VehicleMainObj.transform.right);

            //Lerp the inputs for 'engine response', throttle decrease response is slower than increase (EngineSpoolDownMulti)
            if (Throttle < ThrottleInput)
            {
                Throttle = Mathf.Lerp(Throttle, ThrottleInput, AccelerationResponse * Time.deltaTime); // ThrottleInput * ThrottleStrengthForward;
            }
            else
            {
                Throttle = Mathf.Lerp(Throttle, ThrottleInput, AccelerationResponse * EngineSpoolDownSpeedMulti * Time.deltaTime); // ThrottleInput * ThrottleStrengthForward;
            }
            InputAcc = Throttle * ThrottleStrength * Atmosphere;

            //Lerp the inputs for 'rotation response'
            LerpedRoll = Mathf.Lerp(LerpedRoll, roll, RollResponse * Time.deltaTime);
            LerpedPitch = Mathf.Lerp(LerpedPitch, pitch, PitchResponse * Time.deltaTime);
            LerpedYaw = Mathf.Lerp(LerpedYaw, yaw, YawResponse * Time.deltaTime);

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
            AirBrakeDrag = AirBrake * AirbrakeStrength;
            FlapsGearDrag = (GearDrag + FlapsDrag + AirBrakeDrag) - 1;
            //do lift
            Vector3 FinalInputAcc = new Vector3(-sidespeed * SidewaysLift * SpeedLiftFactor * AoALiftYaw,// X Side
                downspeed * FlapsLift * PitchDownLiftMulti * SpeedLiftFactor * AoALiftPitch + (SpeedLiftFactor * AoALiftPitch * VelPullUp),// Y Up
                    InputAcc);// Z Forward

            //used to add rotation friction
            Vector3 localAngularVelocity = transform.InverseTransformDirection(VehicleRigidbody.angularVelocity);

            Vector3 FinalInputRot = new Vector3((-localAngularVelocity.x * PitchFriction * rotlift * AoALiftPitch * AoALiftYaw),// X Pitch
                (-localAngularVelocity.y * YawFriction * rotlift * AoALiftPitch * AoALiftYaw),// Y Yaw
                    LerpedRoll + (-localAngularVelocity.z * RollFriction * rotlift * AoALiftPitch * AoALiftYaw));// Z Roll

            //create values for use in fixedupdate (control input and straightening forces)
            Pitching = (VehicleMainObj.transform.up * LerpedPitch * Atmosphere + (VehicleMainObj.transform.up * downspeed * VelStraightenStrPitch * AoALiftPitch * rotlift)) * 90;
            Yawing = (VehicleMainObj.transform.right * LerpedYaw * Atmosphere + (-VehicleMainObj.transform.right * sidespeed * VelStraightenStrYaw * AoALiftYaw * rotlift)) * 90;

            FinalInputRot *= Atmosphere;//Atmosphere thickness
            FinalInputAcc *= Atmosphere;
            VehicleConstantForce.relativeForce = FinalInputAcc;
            VehicleConstantForce.relativeTorque = FinalInputRot;
        }
    }
    private void FixedUpdate()
    {
        if (InEditor || localPlayer.IsOwner(VehicleMainObj))
        {
            //lerp velocity toward 0 to simulate air friction
            VehicleRigidbody.velocity = Vector3.Lerp(VehicleRigidbody.velocity, Vector3.zero, (((AirFriction * FlapsGearDrag) * Atmosphere) * 90) * Time.deltaTime);
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
}
