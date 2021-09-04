
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Brake : UdonSharpBehaviour
{
    [SerializeField] private UdonSharpBehaviour SAVControl;
    [Tooltip("Looping sound to play while brake is active")]
    [SerializeField] private AudioSource Airbrake_snd;
    [SerializeField] private Animator BrakeAnimator;
    [Tooltip("Because you have to hold the break, and the keyboardcontrols script can only send events, this option is here.")]
    [SerializeField] private KeyCode KeyboardControl = KeyCode.B;
    private bool UseLeftTrigger = false;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.None)] private float BrakeInput;
    private Rigidbody VehicleRigidbody;
    private bool HasAirBrake;
    [SerializeField] private float AirbrakeStrength = 4f;
    [SerializeField] private float GroundBrakeStrength = 6f;
    [Tooltip("Water brake functionality requires that floatscript is being used")]
    [SerializeField] private float WaterBrakeStrength = 1f;
    [SerializeField] private bool NoPilotAlwaysGroundBrake = true;
    [SerializeField] private float GroundBrakeSpeed = 40f;
    //other functions can set this +1 to disable breaking
    [System.NonSerializedAttribute] public int DisableGroundBrake = 0;
    private SaccEntity EntityControl;
    private float StartGroundBrakeStrength;
    private float StartWaterBrakeStrength;
    private int BRAKE_STRING = Animator.StringToHash("brake");
    private bool Airbrake_sndNULL;
    private float AirbrakeLerper;
    private bool Braking;
    private bool BrakingLastFrame;
    private float DragAdded = 0;
    private float NonLocalActiveDelay;//this var is for adding a min delay for disabling for non-local users to account for lag
    private bool Selected;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXT_L_EntityStart()
    {
        StartGroundBrakeStrength = GroundBrakeStrength;
        StartWaterBrakeStrength = WaterBrakeStrength;
        GroundBrakeStrength = 1;
        WaterBrakeStrength = 1;

        EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
        VehicleRigidbody = EntityControl.GetComponent<Rigidbody>();
        HasAirBrake = AirbrakeStrength != 0;
        Airbrake_sndNULL = Airbrake_snd == null;
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (localPlayer != null && !localPlayer.isMaster)
        { gameObject.SetActive(false); }
        else
        { gameObject.SetActive(true); }
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
        if (!NoPilotAlwaysGroundBrake)
        {
            if ((bool)SAVControl.GetProgramVariable("Taxiing"))
            {
                GroundBrakeStrength = StartGroundBrakeStrength;
                WaterBrakeStrength = 1;
            }
            else if ((bool)SAVControl.GetProgramVariable("Floating"))
            {
                GroundBrakeStrength = 1;
                WaterBrakeStrength = StartWaterBrakeStrength;
            }
        }
    }
    public void SFEXT_O_PilotExit()
    {
        BrakeInput = 0;
        SAVControl.SetProgramVariable("ExtraDrag", (float)SAVControl.GetProgramVariable("ExtraDrag") - DragAdded);
        DragAdded = 0;
        Selected = false;
        if (!NoPilotAlwaysGroundBrake)
        { GroundBrakeStrength = 0; WaterBrakeStrength = 0; }
    }
    public void SFEXT_G_Explode()
    {
        BrakeInput = 0;
        BrakeAnimator.SetFloat(BRAKE_STRING, 0);
        AirbrakeLerper = 0;
    }
    public void SFEXT_O_TakeOwnership()
    {
        gameObject.SetActive(true);
    }
    public void SFEXT_O_LoseOwnership()
    {
        gameObject.SetActive(false);
    }
    public void EnableForAnimation()
    {
        if (!(bool)SAVControl.GetProgramVariable("IsOwner"))
        {
            gameObject.SetActive(true);
            NonLocalActiveDelay = 3;
        }
    }
    public void DisableForAnimation()
    {
        BrakeAnimator.SetFloat(BRAKE_STRING, 0);
        AirbrakeLerper = 0;
        gameObject.SetActive(false);
    }
    public void SFEXT_G_TouchDownWater()
    {
        WaterBrakeStrength = StartWaterBrakeStrength;
        GroundBrakeStrength = 1;
    }
    public void SFEXT_G_TouchDown()
    {
        WaterBrakeStrength = 1;
        GroundBrakeStrength = StartGroundBrakeStrength;
    }
    private void Update()
    {
        float DeltaTime = Time.deltaTime;
        if ((bool)SAVControl.GetProgramVariable("IsOwner"))
        {
            float Speed = (float)SAVControl.GetProgramVariable("Speed");
            Vector3 CurrentVel = (Vector3)SAVControl.GetProgramVariable("CurrentVel");
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

                Rigidbody gdhr = (Rigidbody)SAVControl.GetProgramVariable("GDHitRigidbody");
                if (gdhr != null)
                {
                    float RBSpeed = ((Vector3)SAVControl.GetProgramVariable("CurrentVel") - gdhr.velocity).magnitude;
                    if ((bool)SAVControl.GetProgramVariable("Taxiing") && BrakeInput > 0 && RBSpeed < GroundBrakeSpeed * BrakeInput && DisableGroundBrake == 0)
                    {
                        VehicleRigidbody.velocity = Vector3.MoveTowards(VehicleRigidbody.velocity, gdhr.GetPointVelocity(EntityControl.CenterOfMass.position), BrakeInput * GroundBrakeStrength * DeltaTime);
                    }
                }
                else
                {
                    if ((bool)SAVControl.GetProgramVariable("Taxiing") && BrakeInput > 0 && Speed < GroundBrakeSpeed * BrakeInput && DisableGroundBrake == 0)
                    {
                        VehicleRigidbody.velocity = Vector3.MoveTowards(VehicleRigidbody.velocity, Vector3.zero, BrakeInput * GroundBrakeStrength * DeltaTime);
                        // VehicleRigidbody.velocity += -CurrentVel.normalized * BrakeInput * GroundBrakeStrength * DeltaTime;
                    }
                }
                //remove the drag added last frame to add the new value for this frame
                SAVControl.SetProgramVariable("ExtraDrag", (float)SAVControl.GetProgramVariable("ExtraDrag") - DragAdded);
                DragAdded = AirbrakeStrength * BrakeInput;
                SAVControl.SetProgramVariable("ExtraDrag", (float)SAVControl.GetProgramVariable("ExtraDrag") + DragAdded);

                //send events to other users to tell them to enable the function so they can see the animation
                Braking = BrakeInput > .1f;
                if (Braking && !BrakingLastFrame)
                {
                    if (!Airbrake_sndNULL && !Airbrake_snd.isPlaying) { Airbrake_snd.Play(); }
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "EnableForAnimation");
                }
                if (AirbrakeLerper < .03 && BrakeInput < .03)
                {
                    if (!Airbrake_sndNULL && Airbrake_snd.isPlaying) { Airbrake_snd.Stop(); }
                }
                BrakingLastFrame = Braking;
            }
            else
            {
                Rigidbody gdhr = null;
                { gdhr = (Rigidbody)SAVControl.GetProgramVariable("GDHitRigidbody"); }
                if (gdhr != null)
                {
                    float RBSpeed = ((Vector3)SAVControl.GetProgramVariable("CurrentVel") - gdhr.velocity).magnitude;
                    if ((bool)SAVControl.GetProgramVariable("Taxiing") && RBSpeed < GroundBrakeSpeed && DisableGroundBrake == 0)
                    {
                        VehicleRigidbody.velocity = Vector3.MoveTowards(VehicleRigidbody.velocity, gdhr.GetPointVelocity(EntityControl.CenterOfMass.position), GroundBrakeStrength * DeltaTime);
                    }
                }
                else
                {
                    if ((bool)SAVControl.GetProgramVariable("Taxiing") && Speed < GroundBrakeSpeed && DisableGroundBrake == 0)
                    {
                        VehicleRigidbody.velocity = Vector3.MoveTowards(VehicleRigidbody.velocity, Vector3.zero, BrakeInput * GroundBrakeStrength * DeltaTime);
                        // VehicleRigidbody.velocity += -CurrentVel.normalized * BrakeInput * GroundBrakeStrength * DeltaTime;
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
            }
        }

        AirbrakeLerper = Mathf.Lerp(AirbrakeLerper, BrakeInput, 1.3f * DeltaTime);
        BrakeAnimator.SetFloat(BRAKE_STRING, AirbrakeLerper);
        Airbrake_snd.pitch = BrakeInput * .2f + .9f;
        Airbrake_snd.volume = AirbrakeLerper * (float)SAVControl.GetProgramVariable("rotlift");
    }
}
