
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Brake : UdonSharpBehaviour
{
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private AudioSource Airbrake_snd;
    [SerializeField] private Animator BrakeAnimator;
    [SerializeField] private KeyCode KeyboardControl = KeyCode.B;
    private bool UseLeftTrigger = false;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.None)] private float BrakeInput;
    private Rigidbody VehicleRigidbody;
    private bool HasAirBrake;
    [SerializeField] private float AirbrakeStrength = 4f;
    [SerializeField] private float GroundBrakeStrength = 6f;
    [SerializeField] private float GroundBrakeSpeed = 40f;
    //other functions can set this +1 to disable breaking
    [System.NonSerializedAttribute] public int DisableGroundBrake = 0;
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
    public void SFEXT_L_ECStart()
    {
        VehicleRigidbody = EngineControl.VehicleMainObj.GetComponent<Rigidbody>();
        HasAirBrake = AirbrakeStrength != 0;
        Airbrake_sndNULL = Airbrake_snd == null;
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (localPlayer!= null && !localPlayer.isMaster)
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
    public void SFEXT_O_PilotExit()
    {
        BrakeInput = 0;
        EngineControl.ExtraDrag -= DragAdded;
        DragAdded = 0;
        Selected = false;
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
        if (!EngineControl.IsOwner)
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
    private void Update()
    {
        float DeltaTime = Time.deltaTime;
        if (EngineControl.IsOwner)
        {
            float Speed = EngineControl.Speed;
            Vector3 CurrentVel = EngineControl.CurrentVel;
            if (EngineControl.Piloting)
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

                if (EngineControl.Taxiing && BrakeInput > 0 && Speed < GroundBrakeSpeed * BrakeInput && DisableGroundBrake == 0)
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
                //remove the drag added last frame to add the new value for this frame
                EngineControl.ExtraDrag -= DragAdded;
                DragAdded = AirbrakeStrength * BrakeInput;
                EngineControl.ExtraDrag += DragAdded;

                //send events to other users to tell them to enable the function so they can see the animation
                if (BrakeInput > .1f)
                { Braking = true; }
                else
                { Braking = false; }
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
                if (EngineControl.Taxiing)
                {
                    if (Speed > GroundBrakeStrength * DeltaTime)
                    {
                        VehicleRigidbody.velocity += -CurrentVel.normalized * GroundBrakeStrength * DeltaTime;
                    }
                    else VehicleRigidbody.velocity = Vector3.zero;
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
        Airbrake_snd.volume = AirbrakeLerper * EngineControl.rotlift;
    }
}
