
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
    public bool ThrustVectoring = false;
    public float ThrustVecStr = 0.25f;
    public float ThrottleStrength = 25f;
    public float AccelerationResponse = 4.5f;
    public float EngineSpoolDownSpeedMulti = .5f;
    public float AirFriction = 0.036f;
    public float RollStrength = 450f;
    public float RollFriction = 90f;
    public float RollResponse = 12f;
    public float RollStrengthReversingMulti = 1.6f;
    public float PitchStrength = 5f;
    public float PitchFriction = 24f;
    public float PitchResponse = 12f;
    public float PitchStrengthReversingMulti = 2;
    public float YawStrength = 3f;
    public float YawFriction = 15f;
    public float YawResponse = 12f;
    public float YawStrengthReversingMulti = 2.4f;
    public float InputPower = 1.7f;
    public float VelStraightenStrPitch = 0.035f;
    public float VelStraightenStrYaw = 0.045f;
    public float MaxAngleOfAttack = 25f;
    public float MaxAngleOfAttackYaw = 40f;
    public float HighAoAMinControlPitch = 0.2f;
    public float HighAoAMinControlYaw = 0.2f;
    public float HighPitchAoAMinLift = 0.2f;
    public float HighYawAoaMinLift = 0.2f;
    public float TaxiRotationSpeed = 35f;
    public float PitchDownStrRatio = .8f;
    public float Lift = .8f;
    public float VelLiftCoefficient = 0.00014f;
    public float MaxVelLift = 10f;
    public float PullDownLiftRatio = .8f;
    public float SidewaysLift = .17f;
    public float VelPullUp = 1f;
    public bool HasFlaps = true;
    public bool HasLandingGear = true;
    public float LandingGearDragMulti = 1.6f;
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
    private float RTrigger;
    private float LTrigger;
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
    public float RotMultiMaxSpeed = 220f;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public float AngleOfAttack;
    [System.NonSerializedAttribute] [HideInInspector] public float AngleOfAttackYaw;
    private float AoALiftYaw;
    private float AoALiftPitch;
    private Vector3 Pitching;
    private Vector3 Yawing;
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
            float GearDrag = LandingGearDragMulti;
            if ((localPlayer == null) || (localPlayer.IsOwner(VehicleMainObj) && !Piloting)) { Occupied = false; } //should make vehicle respawnable if player disconnects while occupying

            if (localPlayer == null || Piloting)
            {
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
                int shiftf = Input.GetKey(KeyCode.LeftShift) ? 1 : 0;
                Lstick.x = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
                //Lstick.y = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
                Rstick.x = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
                Rstick.y = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
                RTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
                LTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
                //MouseX = Input.GetAxisRaw("Mouse X");
                //MouseY = Input.GetAxisRaw("Mouse Y");

                //inputs to forward thrust
                ThrottleInput = (Mathf.Max(RTrigger, shiftf)); //for throttle effects

                //making a circular control stick square for better control
                if (Mathf.Abs(Rstick.x) > Mathf.Abs(Rstick.y))
                {
                    if (Mathf.Abs(Rstick.x) != 0)
                    {
                        float temp = Rstick.magnitude / Mathf.Abs(Rstick.x);
                        Rstick *= temp;
                    }
                }
                else
                {
                    if (Mathf.Abs(Rstick.y) != 0)
                    {
                        float temp = Rstick.magnitude / Mathf.Abs(Rstick.y);
                        Rstick *= temp;
                    }
                }
                /*//make circular for left stick
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


                //inputs to axis and clamp
                rollinput = Mathf.Clamp(((/*(MouseX * mousexsens) + */Rstick.x + Af + Df + leftf + rightf) * -1), -1, 1);//these are used by effectscontroller
                pitchinput = Mathf.Clamp((/*(MouseY * mouseysens + Lstick.y + */Rstick.y + Wf + Sf + downf + upf), -1, 1);
                yawinput = Mathf.Clamp((Lstick.x + Qf + Ef), -1, 1);

                //ability to adjust input to be more precise at low amounts 'exponant'
                rollinput = rollinput > 0 ? Mathf.Pow(rollinput, InputPower) : -Mathf.Pow(Mathf.Abs(rollinput), InputPower);
                pitchinput = pitchinput > 0 ? Mathf.Pow(pitchinput, InputPower) : -Mathf.Pow(Mathf.Abs(pitchinput), InputPower);
                yawinput = yawinput > 0 ? Mathf.Pow(yawinput, InputPower) : -Mathf.Pow(Mathf.Abs(yawinput), InputPower);



                //if moving backwards, controls invert (if thrustvectoring is disabled)
                if ((Vector3.Dot(VehicleRigidbody.velocity, VehicleMainObj.transform.forward) > 0) || ThrustVectoring)
                {
                    roll = rollinput * RollStrength;
                    pitch = pitchinput * PitchStrength;
                    yaw = -yawinput * YawStrength;
                }
                else
                {
                    roll = rollinput * RollStrength * -RollStrengthReversingMulti;
                    pitch = pitchinput * PitchStrength * -PitchStrengthReversingMulti;
                    yaw = -yawinput * YawStrength * -YawStrengthReversingMulti;
                }


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
                    pitch *= PitchDownStrRatio;
                }
                //check of touching ground then rotate if trying to turn

                if (Taxiing)
                {
                    Vector3 taxirot = VehicleMainObj.transform.rotation.eulerAngles;
                    VehicleMainObj.transform.Rotate(Vector3.up, (yawinput * TaxiRotationSpeed) * Time.deltaTime);
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
                downspeed *= PullDownLiftRatio;
            }

            AngleOfAttack = Vector3.SignedAngle(VehicleMainObj.transform.forward, CurrentVel, VehicleMainObj.transform.right);
            AngleOfAttackYaw = Vector3.SignedAngle(VehicleMainObj.transform.forward, CurrentVel, VehicleMainObj.transform.up);

            //angle of attack stuff, pitch and yaw are calculated seperately
            //pitch and yaw each have a curve for when they are within the 'MaxAngleOfAttack' and a linear version up to 90 degrees, which are Max'd (using Mathf.Clamp) for the final result.
            //the linear version is used for high aoa, and is 0 when at 90 degrees. When at more than 90 degrees, the control comes back with the same curve but the inputs are inverted. (unless thrust vectoring is enabled) The invert code is elsewhere.
            AoALiftPitch = Mathf.Min(Mathf.Abs(AngleOfAttack) / MaxAngleOfAttack, Mathf.Abs(Mathf.Abs(AngleOfAttack) - 180) / MaxAngleOfAttack);//angle of attack as 0-1 float, for backwards and forwards
            AoALiftPitch = -AoALiftPitch;
            AoALiftPitch++;
            AoALiftPitch = -Mathf.Pow((1 - AoALiftPitch), 1.6f) + 1;//give it a curve

            float AoALiftPitchMin = Mathf.Min(Mathf.Abs(AngleOfAttack) / 90, Mathf.Abs(Mathf.Abs(AngleOfAttack) - 180) / 90);//linear version to 90 for high aoa
            AoALiftPitchMin = -AoALiftPitchMin;
            AoALiftPitchMin++;
            AoALiftPitchMin *= HighAoAMinControlPitch;
            AoALiftPitchMin = Mathf.Clamp(AoALiftPitchMin, 0, 1);
            AoALiftPitch = Mathf.Clamp(AoALiftPitch, AoALiftPitchMin, 1);

            AoALiftYaw = Mathf.Min(Mathf.Abs(AngleOfAttackYaw) / MaxAngleOfAttackYaw, Mathf.Abs((Mathf.Abs(AngleOfAttackYaw) - 180)) / MaxAngleOfAttackYaw);
            AoALiftYaw = -AoALiftYaw;
            AoALiftYaw++;
            AoALiftYaw = -Mathf.Pow((1 - AoALiftYaw), 1.6f) + 1;//give it a curve

            float AoALiftYawMin = Mathf.Min(Mathf.Abs(AngleOfAttackYaw) / 90, Mathf.Abs(Mathf.Abs(AngleOfAttackYaw) - 180) / 90);//linear version to 90 for high aoa
            AoALiftYawMin = -AoALiftPitchMin;
            AoALiftYawMin++;
            AoALiftYawMin *= HighAoAMinControlYaw;
            AoALiftYawMin = Mathf.Clamp(AoALiftYawMin, 0, 1);
            AoALiftYaw = Mathf.Clamp(AoALiftYaw, AoALiftYawMin, 1);

            //speed related values
            CurrentVel = VehicleRigidbody.velocity;//because rigidbody values aren't accessable by non-owner players
            float SpeedLiftFactor = Mathf.Clamp(CurrentVel.magnitude * CurrentVel.magnitude * VelLiftCoefficient, 0, MaxVelLift);
            rotlift = CurrentVel.magnitude / RotMultiMaxSpeed;//using a simple linear curve for increased control while moving faster, just feels better this way

            //thrust vecotring airplanes have a minimum rotation speed
            if (ThrustVectoring)
            {
                roll *= Mathf.Max(ThrustVecStr, rotlift * AoALiftPitch);
                pitch *= Mathf.Max(ThrustVecStr, rotlift * AoALiftPitch);
                yaw *= Mathf.Max(ThrustVecStr, rotlift * AoALiftPitch);
            }
            else
            {
                roll *= rotlift * AoALiftPitch;
                pitch *= rotlift * AoALiftPitch;
                yaw *= rotlift * AoALiftPitch;
            }

            //rotation inputs are done, now we can set the minimum lift/drag when at high aoa, this should be high than 0 because if it's 0 you will have 0 drag when at 90 degree AoA.
            AoALiftPitch = Mathf.Clamp(AoALiftPitch, HighPitchAoAMinLift, 1);
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
            float FlapsDrag = FlapsDragMulti;
            float FlapsLift = FlapsLiftMulti;
            if (!Flaps || !HasFlaps)
            {
                FlapsDrag = 1;
                FlapsLift = 1;
            }
            //do lift
            Vector3 FinalInputAcc = new Vector3(((sidespeed * SidewaysLift) * -1) * SpeedLiftFactor * AoALiftYaw,// X
                downspeed * FlapsLift * PullDownLiftRatio * Lift * SpeedLiftFactor * AoALiftPitch + (SpeedLiftFactor * AoALiftPitch * VelPullUp),// Y
                    InputAcc);// Z

            //create values for use in fixedupdate (control input and straightening forces)
            Pitching = VehicleMainObj.transform.up * LerpedPitch * Atmosphere + (VehicleMainObj.transform.up * downspeed * VelStraightenStrPitch * AoALiftPitch * rotlift);
            Yawing = VehicleMainObj.transform.right * LerpedYaw * Atmosphere + (-VehicleMainObj.transform.right * sidespeed * VelStraightenStrYaw * AoALiftYaw * rotlift);

            //used to add rotation friction
            Vector3 localAngularVelocity = transform.InverseTransformDirection(VehicleRigidbody.angularVelocity);

            Vector3 FinalInputRot = new Vector3((-localAngularVelocity.x * PitchFriction * rotlift * AoALiftPitch * AoALiftYaw),// X Pitch
                (-localAngularVelocity.y * YawFriction * rotlift * AoALiftPitch * AoALiftYaw),// Y Yaw
                    LerpedRoll + (-localAngularVelocity.z * RollFriction * rotlift * AoALiftPitch * AoALiftYaw));// Z Roll

            FinalInputRot *= Atmosphere;//Atmospheric thickness
            FinalInputAcc *= Atmosphere;
            VehicleConstantForce.relativeForce = FinalInputAcc;
            VehicleConstantForce.relativeTorque = FinalInputRot;


            //gear drag
            if (GearUp || !HasLandingGear)
            {
                GearDrag = 1;
            }
            float FlapsGearDrag = (GearDrag + FlapsDrag) - 1;


            //lerp velocity toward '0' to simulate air friction
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
