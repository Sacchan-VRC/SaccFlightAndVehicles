
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
    public float StickInputPower = 1.7f;
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
    public bool HasLandingGear = true;
    public float LandingGearDragMulti = 1.6f;
    public bool HasFlaps = true;
    public float FlapsDragMulti = 1.8f;
    public float FlapsLiftMulti = 1.35f;
    [UdonSynced(UdonSyncMode.None)] public float Health = 100f;
    public float SeaLevel = -100;
    private ConstantForce VehicleConstantForce;
    private float InputAcc;
    private float LerpedRoll;
    private float LerpedPitch;
    private float LerpedYaw;
    Vector2 Lstick;
    Vector2 Rstick = new Vector2(0, 0);
    private float LGrip;
    public bool LGripLastFrame = false;
    Vector3 JoystickPos;
    Quaternion PlaneRotDif;
    Quaternion PlaneRotAtPress;
    Quaternion PlaneRotDifSincePressed;
    Quaternion JoystickDifference;
    Quaternion JoystickZeroPoint;
    Quaternion PlaneRotLastFrame;
    float ThrottleDifference;
    float VRThrottle;
    float TempThrottle;
    float handpos;
    float ThrottleZeroPoint;
    private float RGrip;
    public bool RGripLastFrame = false;
    private float downspeed;
    private float sidespeed;
    [System.NonSerializedAttribute] [HideInInspector] private float ThrottleInput = 0f;
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
    private int upf;
    private int downf;
    private int leftf;
    private int rightf;
    private int shiftf;
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
    public Transform DEBUGGER;
    public bool testjoy;
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
    private void Update()
    {
        if (GroundDetector != null)
        {
            if (Physics.Raycast(GroundDetector.position, GroundDetector.TransformDirection(Vector3.down), .44f) && (!GearUp || !HasLandingGear))
            {
                Taxiing = true;
            }
            else { Taxiing = false; }
        }

        if (localPlayer == null || (localPlayer.IsOwner(VehicleMainObj)))//works in editor or ingame
        {
            if ((localPlayer == null) || (localPlayer.IsOwner(VehicleMainObj) && !Piloting)) { Occupied = false; } //should make vehicle respawnable if player disconnects while occupying

            if (localPlayer == null || Piloting)
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
                shiftf = Input.GetKey(KeyCode.LeftShift) ? 1 : 0;
                Lstick.x = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
                //Lstick.y = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
                Rstick.x = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
                Rstick.y = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
                LGrip = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger");
                RGrip = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
                //MouseX = Input.GetAxisRaw("Mouse X");
                //MouseY = Input.GetAxisRaw("Mouse Y");

                //VR Joystickd
                if (RGrip > 0.75)
                //if (testjoy)
                {
                    PlaneRotDif = VehicleMainObj.transform.rotation * Quaternion.Inverse(PlaneRotLastFrame);
                    PlaneRotDifSincePressed = PlaneRotDif * PlaneRotDifSincePressed;
                    JoystickZeroPoint = PlaneRotDif * JoystickZeroPoint;//zero point rotates with the plane
                    if (!RGripLastFrame)
                    {
                        PlaneRotDifSincePressed = VehicleMainObj.transform.rotation;
                        JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;
                        //JoystickZeroPoint = DEBUGGER.rotation;
                    }
                    // Quaternion temprot = Quaternion.Inverse(PlaneRotDifSincePressed) * DEBUGGER.rotation;
                    Quaternion temprot = Quaternion.Inverse(PlaneRotDifSincePressed) * localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;
                    JoystickDifference = temprot * Quaternion.Inverse(JoystickZeroPoint);
                    JoystickPos = (JoystickDifference * VehicleMainObj.transform.up);
                    Rstick = new Vector2(JoystickPos.x, JoystickPos.z) * 1.41421f;

                    RGripLastFrame = true;
                }
                else
                {
                    JoystickPos = Vector3.zero;
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
                        TempThrottle = VRThrottle;
                    }
                    ThrottleDifference = ThrottleZeroPoint - handpos;
                    ThrottleDifference *= -3;

                    VRThrottle = Mathf.Clamp(TempThrottle + ThrottleDifference, 0, 1);
                    LGripLastFrame = true;
                }
                else if (LGrip < 0.75)
                {
                    LGripLastFrame = false;
                }

                //inputs to forward thrust
                ThrottleInput = (Mathf.Max(VRThrottle, shiftf)); //for throttle effects

                //making a circular control stick square for better control
                if (Mathf.Abs(Rstick.x) > Mathf.Abs(Rstick.y))
                {
                    if (Mathf.Abs(Rstick.x) != 0)
                    {
                        temp = Rstick.magnitude / Mathf.Abs(Rstick.x);
                        Rstick *= temp;
                    }
                }
                else
                {
                    if (Mathf.Abs(Rstick.y) != 0)
                    {
                        temp = Rstick.magnitude / Mathf.Abs(Rstick.y);
                        Rstick *= temp;
                    }
                }
                /*//make square for left stick
                if (Mathf.Abs(Lstick.x) > Mathf.Abs(Lstick.y))
                {
                    if (Mathf.Abs(Lstick.x) != 0)
                    {
                        float temp = Lstick.magnitude / Mathf.Abs(Lstick.x);
                        Lstick.x *= temp;
                        Lstick.y *= temp;
                    }
                }
                else
                {
                    if (Mathf.Abs(Lstick.y) != 0)
                    {
                        float temp = Lstick.magnitude / Mathf.Abs(Lstick.y);
                        Lstick.y *= temp;
                        Lstick.x *= temp;
                    }
                } */


                //combine inputs and clamp
                //these are used by effectscontroller
                pitchinput = Mathf.Clamp((/*(MouseY * mouseysens + Lstick.y + */Rstick.y + Wf + Sf + downf + upf), -1, 1);
                yawinput = Mathf.Clamp((Lstick.x + Qf + Ef), -1, 1);
                rollinput = Mathf.Clamp(((/*(MouseX * mousexsens) + */Rstick.x + Af + Df + leftf + rightf) * -1), -1, 1);

                //ability to adjust input to be more precise at low amounts. 'exponant'
                pitchinput = pitchinput > 0 ? Mathf.Pow(pitchinput, StickInputPower) : -Mathf.Pow(Mathf.Abs(pitchinput), StickInputPower);
                yawinput = yawinput > 0 ? Mathf.Pow(yawinput, StickInputPower) : -Mathf.Pow(Mathf.Abs(yawinput), StickInputPower);
                rollinput = rollinput > 0 ? Mathf.Pow(rollinput, StickInputPower) : -Mathf.Pow(Mathf.Abs(rollinput), StickInputPower);

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

                //gear controls
                if ((Input.GetKeyDown(KeyCode.G)) || (Input.GetButtonDown("Oculus_CrossPlatform_PrimaryThumbstick")) && HasLandingGear)
                {
                    GearUp = !GearUp;
                }
                //flaps controls
                if (((Input.GetKeyDown(KeyCode.F) || (Input.GetButtonDown("Oculus_CrossPlatform_SecondaryThumbstick"))) || (Input.GetButtonDown("Fire2"))) && HasFlaps)
                {
                    Flaps = !Flaps;
                }
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
            CurrentVel = VehicleRigidbody.velocity;//because rigidbody values aren't accessable by non-owner players
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
            if (!Flaps || !HasFlaps)
            {
                FlapsDrag = 1;
                FlapsLift = 1;
            }
            //gear drag
            if (GearUp || !HasLandingGear)
            {
                GearDrag = 1;
            }
            else
            {
                GearDrag = LandingGearDragMulti;
            }
            FlapsGearDrag = (GearDrag + FlapsDrag) - 1;
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
            Pitching = VehicleMainObj.transform.up * LerpedPitch * Atmosphere + (VehicleMainObj.transform.up * downspeed * VelStraightenStrPitch * AoALiftPitch * rotlift);
            Yawing = VehicleMainObj.transform.right * LerpedYaw * Atmosphere + (-VehicleMainObj.transform.right * sidespeed * VelStraightenStrYaw * AoALiftYaw * rotlift);

            FinalInputRot *= Atmosphere;//Atmosphere thickness
            FinalInputAcc *= Atmosphere;
            VehicleConstantForce.relativeForce = FinalInputAcc;
            VehicleConstantForce.relativeTorque = FinalInputRot;

            //lerp velocity toward 0 to simulate air friction
            VehicleRigidbody.velocity = Vector3.Lerp(VehicleRigidbody.velocity, Vector3.zero, (AirFriction * FlapsGearDrag) * Atmosphere * Time.deltaTime);
        }
    }
    private void FixedUpdate()
    {
        //apply pitching
        VehicleRigidbody.AddForceAtPosition(Pitching, PitchMoment.position, ForceMode.Force);
        //apply yawing
        VehicleRigidbody.AddForceAtPosition(Yawing, YawMoment.position, ForceMode.Force);
        //calc Gs
        if (localPlayer == null || localPlayer.IsOwner(VehicleMainObj))
        {
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
