
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class EngineController : UdonSharpBehaviour
{
    public GameObject VehicleMainObj;
    public Transform GroundDetector;
    [System.NonSerializedAttribute] [HideInInspector] public Rigidbody VehicleRigidbody;
    public EffectsController EffectsControl;
    public SoundController SoundControl;
    public HUDController HUDControl;
    private ConstantForce VehicleConstantForce;
    public Transform CenterOfMass;
    public Transform PitchMoment;
    public bool AirplaneThrustVectoring = true;
    public float AirplaneThrustVecStr = 1;
    public float ThrottleStrengthForward = 25f;
    public float AirFriction = 0.036f;
    public float PitchStrength = 1.5f;
    public float YawStrength = 5f;
    public float RollStrength = 50f;
    public float AccelerationResponse = 4.5f;
    public float RotationResponse = 90f;
    public float VelStraightenStrPitch = 0.2f;
    public float VelStraightenStrYaw = 0.15f;
    public float TaxiRotationSpeed = 35f;
    public float AirplanePitchDownStrRatio = .8f;
    public float AirplaneLift = .8f;
    public float AirplaneVelLiftCoefficient = 1.4f;
    public float AirplanePullDownLiftRatio = .8f;
    public float AirplaneSidewaysLift = .17f;
    public float AirplaneVelPullUp = 0.065f;
    public float AirplaneAirRollFriction = 15f;
    public bool HasFlaps = true;
    public bool HasLandingGear = true;
    public float LandingGearDragMulti = 1.6f;
    public float FlapsDragMulti = 1.8f;
    public float FlapsLiftMulti = 1.35f;
    [UdonSynced(UdonSyncMode.None)] public float Health = 100f;
    public float SeaLevel = -100;
    Vector3 LastFrameLerpedInputAcc;
    Vector3 LastFrameLerpedInputRot;
    float LstickH;
    float LstickV;
    float RstickH;
    float RstickV;
    float RTrigger;
    float LTrigger;
    float downspeed;
    float sidespeed;
    float accelforward = 0f;
    [System.NonSerializedAttribute] [HideInInspector] public float speed20 = 0f;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public float ThrottleValue = 0f;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public Vector3 CurrentVel = new Vector3(0, 0, 0);
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public float Gs = 1f;
    float roll = 0f;
    float pitch = 0f;
    float yaw = 0f;
    [System.NonSerializedAttribute] [HideInInspector] public float FullHealth;
    [System.NonSerializedAttribute] [HideInInspector] public bool Taxiing = false;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool GearUp = false;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool Flaps = true;
    [System.NonSerializedAttribute] [HideInInspector] public float rollinput = 0f;
    [System.NonSerializedAttribute] [HideInInspector] public float pitchinput = 0f;
    [System.NonSerializedAttribute] [HideInInspector] public float yawinput = 0f;
    [System.NonSerializedAttribute] [HideInInspector] public bool Piloting = false;
    [System.NonSerializedAttribute] [HideInInspector] public bool Passenger = false;
    [System.NonSerializedAttribute] [HideInInspector] public Vector3 LastFrameVel = new Vector3(0, 0, 0);
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool Occupied = false; //this is true if someone is sitting in pilot seat
    [System.NonSerializedAttribute] [HideInInspector] public VRCPlayerApi localPlayer;
    [System.NonSerializedAttribute] [HideInInspector] public bool dead = false;
    [System.NonSerializedAttribute] [HideInInspector] public float AtmoshpereFadeDistance;
    [System.NonSerializedAttribute] [HideInInspector] public float AtmosphereHeightThing;
    public float AtmosphereThinningStart = 12192f; //40,000 feet
    public float AtmosphereThinningEnd = 19812; //65,000 feet
    private float Atmosphere;
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
        VehicleRigidbody.centerOfMass = CenterOfMass.localPosition * scaleratio;

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
                float Wf = 0; //inputs as floats
                float Af = 0;
                float Sf = 0;
                float Df = 0;
                float Qf = 0;
                float Ef = 0;
                float upf = 0;
                float downf = 0;
                float leftf = 0;
                float rightf = 0;
                float shiftf = 0;
                LstickH = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
                LstickV = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
                RstickH = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
                RstickV = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
                RTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
                LTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
                //MouseX = Input.GetAxisRaw("Mouse X");
                //MouseY = Input.GetAxisRaw("Mouse Y");
                if (Input.GetKey(KeyCode.W)) { Wf = 1; }
                if (Input.GetKey(KeyCode.A)) { Af = -1; }
                if (Input.GetKey(KeyCode.S)) { Sf = -1; }
                if (Input.GetKey(KeyCode.D)) { Df = 1; }
                if (Input.GetKey(KeyCode.Q)) { Qf = -1; }
                if (Input.GetKey(KeyCode.E)) { Ef = 1; }
                if (Input.GetKey(KeyCode.LeftShift)) { shiftf = 1; }
                if (Input.GetKey(KeyCode.UpArrow)) { upf = 1; }
                if (Input.GetKey(KeyCode.DownArrow)) { downf = -1; }
                if (Input.GetKey(KeyCode.LeftArrow)) { leftf = -1; }
                if (Input.GetKey(KeyCode.RightArrow)) { rightf = 1; }

                //inputs to forward thrust
                ThrottleValue = (Mathf.Max(RTrigger, shiftf)); //for throttle effects
                accelforward = ThrottleValue * ThrottleStrengthForward;

                //inputs to axis and clamp
                rollinput = Mathf.Clamp(((/*(MouseX * mousexsens) + */RstickH + Af + Df + leftf + rightf) * -1), -1, 1);//these are used by effectscontroller
                pitchinput = Mathf.Clamp((/*(MouseY * mouseysens + */LstickV + RstickV + Wf + Sf + downf + upf), -1, 1);
                yawinput = Mathf.Clamp((LstickH + Qf + Ef), -1, 1);

                roll = rollinput * RollStrength;
                pitch = pitchinput * PitchStrength;
                yaw = yawinput * YawStrength;

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
                    pitch *= AirplanePitchDownStrRatio;
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
                ThrottleValue = 0;
                accelforward = 0;
            }
            //used to create air resistance only in the relative down direction
            downspeed = Vector3.Dot(VehicleRigidbody.velocity, VehicleMainObj.transform.up) * -1;
            if (downspeed < 0)
            {
                downspeed *= AirplanePullDownLiftRatio;
            }

            //speed related values
            CurrentVel = VehicleRigidbody.velocity;//because rigidbody values aren't accessable by non-owner players
            speed20 = CurrentVel.magnitude / 20f;
            float SpeedLiftFactor = Mathf.LinearToGammaSpace(speed20 * AirplaneVelLiftCoefficient);

            //thrust vecotring airplanes have a minimum rotation speed
            if (AirplaneThrustVectoring)
            {
                roll *= Mathf.Max(AirplaneThrustVecStr, SpeedLiftFactor);
                pitch *= Mathf.Max(AirplaneThrustVecStr, SpeedLiftFactor);
                yaw *= Mathf.Max(AirplaneThrustVecStr, SpeedLiftFactor);
            }
            else
            {
                roll *= SpeedLiftFactor;
                pitch *= SpeedLiftFactor;
                yaw *= SpeedLiftFactor;
            }

            Atmosphere = Mathf.Clamp(-(CenterOfMass.position.y / AtmoshpereFadeDistance) + 1 + AtmosphereHeightThing, 0, 1);

            //used to add physics to plane's yaw (accel angvel towards velocity)
            sidespeed = Vector3.Dot(VehicleRigidbody.velocity, VehicleMainObj.transform.right);

            //Extra thrust up if moving local down, and Lerp the inputs for 'engine response'
            Vector3 InputAcc = (new Vector3(0f, 0f, accelforward * Atmosphere));
            Vector3 FinalInputAcc = Vector3.Lerp(LastFrameLerpedInputAcc, InputAcc, AccelerationResponse * Time.deltaTime);
            //Lerp the inputs for 'rotation response'
            Vector3 InputRot;
            if (PitchMoment != null) { InputRot = (new Vector3(0, yaw, roll)); } // pitch is done manually in fixedupdate to support pitch moment.
            else { InputRot = (new Vector3(pitch, yaw, roll)); }
            Vector3 FinalInputRot = Vector3.Lerp(LastFrameLerpedInputRot, InputRot, RotationResponse * Time.deltaTime);

            LastFrameLerpedInputAcc = FinalInputAcc;
            LastFrameLerpedInputRot = FinalInputRot;

            //flaps drag and lift
            float FlapsDrag = FlapsDragMulti;
            float FlapsLift = FlapsLiftMulti;
            if (!Flaps || !HasFlaps)
            {
                FlapsDrag = 1;
                FlapsLift = 1;
            }

            //do lift and rotate toward velocity for airplanes

            FinalInputAcc.x = ((sidespeed * AirplaneSidewaysLift) * -1) * SpeedLiftFactor;
            FinalInputAcc.y = downspeed * FlapsLift * AirplanePullDownLiftRatio * AirplaneLift * SpeedLiftFactor;
            FinalInputRot.x += downspeed * VelStraightenStrPitch + (speed20 * (AirplaneVelPullUp * -1));
            FinalInputRot.y += sidespeed * VelStraightenStrYaw;

            //roll friction
            Vector3 localAngularVelocity = transform.InverseTransformDirection(VehicleRigidbody.angularVelocity);
            FinalInputRot.z += -localAngularVelocity.z * AirplaneAirRollFriction;

            //final force input
            /*if (FinalInputRot.magnitude < .001 && FinalInputAcc.magnitude < .001) // let the rigidbody sleep //Except it doesn't work because wheel colliders are stupid
            {
                VehicleConstantForce.relativeTorque = new Vector3(0, 0, 0);
                VehicleConstantForce.relativeForce = new Vector3(0, 0, 0);
            }
            else
            {
                VehicleConstantForce.relativeForce = FinalInputAcc;
                VehicleConstantForce.relativeTorque = FinalInputRot;
            }*/
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
        //apply pitching if using a pitchmoment
        if (PitchMoment != null) { VehicleRigidbody.AddForceAtPosition(VehicleMainObj.transform.up * pitch * Atmosphere, PitchMoment.position, ForceMode.Force); }
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
