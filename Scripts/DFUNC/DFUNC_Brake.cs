
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_Brake : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        [Tooltip("Looping sound to play while brake is active")]
        public AudioSource Airbrake_snd;
        [Tooltip("Will Crash if not set")]
        public Animator BrakeAnimator;
        [Tooltip("Position the ground brake force will be applied at")]
        public Transform GroundBrakeForcePosition;
        [Tooltip("Because you have to hold the break, and the keyboardcontrols script can only send events, this option is here.")]
        public KeyCode KeyboardControl = KeyCode.B;
        private bool UseLeftTrigger = false;
        [System.NonSerializedAttribute, UdonSynced(UdonSyncMode.None)] public float BrakeInput;
        private Rigidbody VehicleRigidbody;
        private bool HasAirBrake;
        public float AirbrakeStrength = 4f;
        public float GroundBrakeStrength = 6;
        [Tooltip("Water brake functionality requires that floatscript is being used")]
        public float WaterBrakeStrength = 1f;
        public bool NoPilotAlwaysGroundBrake = true;
        [Tooltip("Speed below which the ground break works meters/s")]
        public float GroundBrakeSpeed = 40f;
        //other functions can set this +1 to disable breaking
        [System.NonSerializedAttribute] public bool _DisableGroundBrake;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(DisableGroundBrake_))] public int DisableGroundBrake = 0;
        public int DisableGroundBrake_
        {
            set
            {
                _DisableGroundBrake = value > 0;
                DisableGroundBrake = value;
            }
            get => DisableGroundBrake;
        }
        private SaccEntity EntityControl;
        private float BrakeStrength;
        private int BRAKE_STRING = Animator.StringToHash("brake");
        private bool Braking;
        private bool Asleep;
        private bool BrakingLastFrame;
        private float LastDrag = 0;
        private float AirbrakeLerper;
        private float NonLocalActiveDelay;//this var is for adding a min delay for disabling for non-local users to account for lag
        private bool Selected;
        private bool IsOwner;
        private bool InVehicle;
        private float NextUpdateTime;
        private float RotMultiMaxSpeedDivider;
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        public void SFEXT_L_EntityStart()
        {
            EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
            VehicleRigidbody = EntityControl.GetComponent<Rigidbody>();
            HasAirBrake = AirbrakeStrength != 0;
            RotMultiMaxSpeedDivider = 1 / (float)SAVControl.GetProgramVariable("RotMultiMaxSpeed");
            IsOwner = (bool)SAVControl.GetProgramVariable("IsOwner");
            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            if (localPlayer != null && !localPlayer.isMaster)
            { gameObject.SetActive(false); }
            else
            { gameObject.SetActive(true); }
            if (!GroundBrakeForcePosition) { GroundBrakeForcePosition = EntityControl.CenterOfMass; }
        }
        public void DFUNC_Selected()
        {
            Selected = true;
        }
        public void DFUNC_Deselected()
        {
            BrakeInput = 0;
            Selected = false;
        }
        public void SFEXT_O_PilotEnter()
        {
            InVehicle = true;
            if (!NoPilotAlwaysGroundBrake)
            {
                if ((bool)SAVControl.GetProgramVariable("Floating"))
                {
                    BrakeStrength = WaterBrakeStrength;
                }
                else if ((bool)SAVControl.GetProgramVariable("Taxiing"))
                {
                    BrakeStrength = GroundBrakeStrength;
                }
            }
        }
        public void SFEXT_O_PilotExit()
        {
            InVehicle = false;
            BrakeInput = 0;
            RequestSerialization();
            Selected = false;
            if (!NoPilotAlwaysGroundBrake)
            { BrakeStrength = 0; }
            if (Airbrake_snd)
            {
                Airbrake_snd.pitch = 0f;
                Airbrake_snd.volume = 0f;
            }
        }
        public void SFEXT_P_PassengerEnter()
        {
            InVehicle = true;
        }
        public void SFEXT_P_PassengerExit()
        {
            InVehicle = false;
            if (Airbrake_snd)
            {
                Airbrake_snd.pitch = 0f;
                Airbrake_snd.volume = 0f;
            }
        }
        public void SFEXT_G_Explode()
        {
            BrakeInput = 0;
            BrakeAnimator.SetFloat(BRAKE_STRING, 0);
        }
        public void SFEXT_O_TakeOwnership()
        {
            gameObject.SetActive(true);
            IsOwner = true;
        }
        public void SFEXT_O_LoseOwnership()
        {
            gameObject.SetActive(false);
            IsOwner = false;
        }
        public void EnableForAnimation()
        {
            if (!IsOwner)
            {
                if (Airbrake_snd) { Airbrake_snd.Play(); }
                gameObject.SetActive(true);
                NonLocalActiveDelay = 3;
            }
        }
        public void DisableForAnimation()
        {
            BrakeAnimator.SetFloat(BRAKE_STRING, 0);
            BrakeInput = 0;
            AirbrakeLerper = 0;
            if (Airbrake_snd)
            {
                Airbrake_snd.pitch = 0;
                Airbrake_snd.volume = 0;
            }
            gameObject.SetActive(false);
        }
        public void SFEXT_G_TouchDownWater()
        {
            BrakeStrength = WaterBrakeStrength;
        }
        public void SFEXT_G_TouchDown()
        {
            BrakeStrength = GroundBrakeStrength;
        }
        public void SFEXT_L_WakeUp() { Asleep = false; }
        public void SFEXT_L_FallAsleep() { Asleep = true; }
        private void Update()
        {
            float DeltaTime = Time.deltaTime;
            if (IsOwner)
            {
                if (!Asleep)
                {
                    float Speed = (float)SAVControl.GetProgramVariable("Speed");
                    Vector3 CurrentVel = (Vector3)SAVControl.GetProgramVariable("CurrentVel");
                    bool Taxiing = (bool)SAVControl.GetProgramVariable("Taxiing");
                    if ((bool)SAVControl.GetProgramVariable("Piloting"))
                    {
                        float KeyboardBrakeInput = 0;
                        float VRBrakeInput = 0;

                        if (Selected)
                        {
                            float Trigger;
                            if (UseLeftTrigger)
                            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                            else
                            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

                            VRBrakeInput = Trigger;
                        }

                        if (Input.GetKey(KeyboardControl))
                        {
                            KeyboardBrakeInput = 1;
                        }
                        BrakeInput = Mathf.Max(VRBrakeInput, KeyboardBrakeInput);
                        if (Taxiing)
                        {
                            //ground brake checks if vehicle is on top of a rigidbody, and if it is, brakes towards its speed rather than zero
                            //does not work if owner of vehicle does not own the rigidbody 
                            Rigidbody gdhr = (Rigidbody)SAVControl.GetProgramVariable("GDHitRigidbody");
                            if (gdhr)
                            {
                                float RBSpeed = ((Vector3)SAVControl.GetProgramVariable("CurrentVel") - gdhr.velocity).magnitude;
                                if (BrakeInput > 0 && RBSpeed < GroundBrakeSpeed && !_DisableGroundBrake)
                                {
                                    Vector3 speed = (VehicleRigidbody.GetPointVelocity(GroundBrakeForcePosition.position) - gdhr.velocity).normalized;
                                    speed = Vector3.ProjectOnPlane(speed, EntityControl.transform.up);
                                    Vector3 BrakeForce = speed.normalized * BrakeInput * BrakeStrength * DeltaTime;
                                    if (speed.sqrMagnitude < BrakeForce.sqrMagnitude)
                                    { BrakeForce = speed; }
                                    VehicleRigidbody.AddForceAtPosition(-speed * BrakeInput * BrakeStrength * DeltaTime, GroundBrakeForcePosition.position, ForceMode.VelocityChange);
                                }
                            }
                            else
                            {
                                if (BrakeInput > 0 && Speed < GroundBrakeSpeed && !_DisableGroundBrake)
                                {
                                    Vector3 speed = VehicleRigidbody.GetPointVelocity(GroundBrakeForcePosition.position);
                                    speed = Vector3.ProjectOnPlane(speed, EntityControl.transform.up);
                                    Vector3 BrakeForce = speed.normalized * BrakeInput * BrakeStrength * DeltaTime;
                                    if (speed.sqrMagnitude < BrakeForce.sqrMagnitude)
                                    { BrakeForce = speed; }//this'll stop the vehicle exactly
                                    VehicleRigidbody.AddForceAtPosition(-BrakeForce, GroundBrakeForcePosition.position, ForceMode.VelocityChange);
                                }
                            }
                        }
                        if (!HasAirBrake && !(bool)SAVControl.GetProgramVariable("Taxiing"))
                        {
                            BrakeInput = 0;
                        }
                        //remove the drag added last frame to add the new value for this frame
                        float extradrag = (float)SAVControl.GetProgramVariable("ExtraDrag");
                        float newdrag = (AirbrakeStrength * BrakeInput);
                        float dragtoadd = -LastDrag + newdrag;
                        extradrag += dragtoadd;
                        LastDrag = newdrag;
                        SAVControl.SetProgramVariable("ExtraDrag", extradrag);

                        //send events to other users to tell them to enable the script so they can see the animation
                        Braking = BrakeInput > .02f;
                        if (Braking)
                        {
                            if (!BrakingLastFrame)
                            {
                                if (Airbrake_snd && !Airbrake_snd.isPlaying) { Airbrake_snd.Play(); }
                                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EnableForAnimation));
                            }
                            if (Time.time > NextUpdateTime)
                            {
                                RequestSerialization();
                                NextUpdateTime = Time.time + .4f;
                            }
                        }
                        else
                        {
                            if (BrakingLastFrame)
                            {
                                float brk = BrakeInput;
                                BrakeInput = 0;
                                RequestSerialization();
                                BrakeInput = brk;
                            }
                        }
                        if (AirbrakeLerper < .03 && BrakeInput < .03)
                        {
                            if (Airbrake_snd && Airbrake_snd.isPlaying) { Airbrake_snd.Stop(); }
                        }
                        BrakingLastFrame = Braking;
                    }
                    else
                    {
                        if (Taxiing)
                        {
                            //outside of vehicle, simpler version, ground brake always max
                            Rigidbody gdhr = null;
                            { gdhr = (Rigidbody)SAVControl.GetProgramVariable("GDHitRigidbody"); }
                            if (gdhr)
                            {
                                float RBSpeed = ((Vector3)SAVControl.GetProgramVariable("CurrentVel") - gdhr.velocity).magnitude;
                                if (RBSpeed < GroundBrakeSpeed && !_DisableGroundBrake)
                                {
                                    VehicleRigidbody.velocity = Vector3.MoveTowards(VehicleRigidbody.velocity, gdhr.GetPointVelocity(EntityControl.CenterOfMass.position), BrakeStrength * DeltaTime);
                                }
                            }
                            else
                            {
                                if (Speed < GroundBrakeSpeed && !_DisableGroundBrake)
                                {
                                    VehicleRigidbody.velocity = Vector3.MoveTowards(VehicleRigidbody.velocity, Vector3.zero, BrakeStrength * DeltaTime);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                //this object is enabled for non-owners only while animating
                NonLocalActiveDelay -= DeltaTime;
                if (NonLocalActiveDelay < 0 && AirbrakeLerper < 0.01)
                {
                    DisableForAnimation();
                    return;
                }
            }
            AirbrakeLerper = Mathf.Lerp(AirbrakeLerper, BrakeInput, 2f * DeltaTime);
            BrakeAnimator.SetFloat(BRAKE_STRING, AirbrakeLerper);
            if (InVehicle && Airbrake_snd)
            {
                Airbrake_snd.pitch = AirbrakeLerper * .2f + .9f;
                Airbrake_snd.volume = AirbrakeLerper * Mathf.Min((float)SAVControl.GetProgramVariable("Speed") * RotMultiMaxSpeedDivider, 1);
            }
        }
    }
}