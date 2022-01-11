
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class DFUNC_Hook : UdonSharpBehaviour
{
    [Tooltip("Object enabled when function is active (used on MFD)")]
    public GameObject Dial_Funcon;
    public AudioSource CableSnap;
    public Transform HookDetector;
    [Tooltip("Strength of force slowing down the vehicle when it's snagged on a cable")]
    public float HookedBrakeStrength = 55f;
    [Tooltip("Distance from the initial snag point that the cable will 'snap' and the vehicle will be released (and damaged) if it hasnt stopped")]
    public float HookedCableSnapDistance = 120f;
    [Tooltip("If this vehicle has a brake function, need a reference to it to disable it when this function is braking the vehicle")]
    [System.NonSerialized] public SaccEntity EntityControl;
    private UdonSharpBehaviour SAVControl;
    private UdonSharpBehaviour BrakeFunction;
    private bool UseLeftTrigger = false;
    public LayerMask HookCableLayer;
    private bool TriggerLastFrame;
    [System.NonSerializedAttribute] public bool Hooked = false;
    private float HookedTime = 0f;
    private Vector3 HookedLoc;
    private Transform VehicleTransform;
    private Animator VehicleAnimator;
    private Rigidbody VehicleRigidbody;
    [System.NonSerializedAttribute] public bool HookDown = false;
    private bool DisableGroundBrake;
    private bool func_active;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXT_L_EntityStart()
    {
        if (Dial_Funcon) Dial_Funcon.SetActive(false);
        SAVControl = EntityControl.GetExtention(GetUdonTypeName<SaccAirVehicle>());
        BrakeFunction = EntityControl.GetExtention(GetUdonTypeName<DFUNC_Brake>());
        VehicleTransform = EntityControl.transform;
        VehicleAnimator = EntityControl.GetComponent<Animator>();
        VehicleRigidbody = EntityControl.GetComponent<Rigidbody>();
        SetHookUp();
    }
    public void DFUNC_Selected()
    {
        TriggerLastFrame = true;
        gameObject.SetActive(true);
        func_active = true;
    }
    public void DFUNC_Deselected()
    {
        if (!HookDown) { gameObject.SetActive(false); }
        func_active = false;
    }
    public void SFEXT_O_PilotEnter()
    {
        if (Dial_Funcon) Dial_Funcon.SetActive(HookDown);
        if (HookDown) { gameObject.SetActive(true); }
    }
    public void SFEXT_O_PilotExit()
    {
        Hooked = false;
        func_active = false;
        if (DisableGroundBrake && BrakeFunction)
        {
            BrakeFunction.SetProgramVariable("DisableGroundBrake", (int)BrakeFunction.GetProgramVariable("DisableGroundBrake") - 1);
            DisableGroundBrake = false;
        }
        gameObject.SetActive(false);
    }
    public void SFEXT_G_Explode()
    {
        Hooked = false;
        SetHookUp();
    }
    public void SFEXT_G_RespawnButton()
    {
        SetHookUp();
    }
    public void SFEXT_L_PassengerEnter()
    {
        if (Dial_Funcon) { Dial_Funcon.SetActive(HookDown); }
    }
    public void KeyboardInput()
    {
        ToggleHook();
    }
    public void SFEXT_O_OnPlayerJoined()
    {
        if (HookDown)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetHookDown)); }
    }
    private void Update()
    {
        if (func_active)
        {
            float Trigger;
            if (UseLeftTrigger)
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
            else
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

            if (Trigger > 0.75)
            {
                if (!TriggerLastFrame)
                {
                    ToggleHook();
                    Hooked = false;
                }
                TriggerLastFrame = true;
            }
            else { TriggerLastFrame = false; }
        }
        //check for catching a cable with hook
        if (HookDown)
        {
            if (Physics.Raycast(HookDetector.position, Vector3.down, 2f, HookCableLayer) && !Hooked)
            {
                HookedLoc = VehicleTransform.position;
                Hooked = true;
                HookedTime = Time.time;
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetHooked)); }
            }
        }
        //slow down if hooked and on the ground
        if (Hooked && (bool)SAVControl.GetProgramVariable("Taxiing"))
        {
            if (!DisableGroundBrake && BrakeFunction)
            {
                BrakeFunction.SetProgramVariable("DisableGroundBrake", (int)BrakeFunction.GetProgramVariable("DisableGroundBrake") + 1);
                DisableGroundBrake = true;
            }
            if (Vector3.Distance(VehicleTransform.position, HookedLoc) > HookedCableSnapDistance)//real planes take around 80-90 meters to stop on a carrier
            {
                //if you go further than HookedBrakeMaxDistance you snap the cable and it hurts your plane by the % of the amount of time left of the 2 seconds it should have taken to stop you.
                float HookedDelta = (Time.time - HookedTime);
                if (HookedDelta < 2)
                {
                    SAVControl.SetProgramVariable("Health", (float)SAVControl.GetProgramVariable("Health") - (((-HookedDelta + 2) / 2) * (float)SAVControl.GetProgramVariable("FullHealth")));
                    //SAVControl.Health -= ((-HookedDelta + 2) / 2) * SAVControl.FullHealth;
                }
                Hooked = false;
                //if you catch a cable but go airborne before snapping it, keep your hook out and then land somewhere else
                //you would hear the cablesnap sound when you touchdown, so limit it to within 5 seconds of hooking
                //this results in 1 frame's worth of not being able to catch a cable if hook stays down after being 'hooked', not snapping and then trying to hook again
                //but that should be a very rare and unnoitcable(if it happens) occurance
                if (HookedDelta < 5)
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(PlayCableSnap)); }
            }
            float DeltaTime = Time.deltaTime;
            if ((float)SAVControl.GetProgramVariable("Speed") > HookedBrakeStrength * DeltaTime)
            {
                VehicleRigidbody.velocity += -((Vector3)SAVControl.GetProgramVariable("CurrentVel")).normalized * HookedBrakeStrength * DeltaTime;
            }
            else
            {
                VehicleRigidbody.velocity = Vector3.zero;
            }
            //Debug.Log("hooked");
        }
    }
    public void ToggleHook()
    {
        if (HookDetector)
        {
            if (!HookDown)
            {
                if ((bool)SAVControl.GetProgramVariable("Piloting") && !(bool)SAVControl.GetProgramVariable("InVR")) { gameObject.SetActive(true); }
                if (Dial_Funcon) { Dial_Funcon.SetActive(true); }
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetHookDown));
            }
            else
            {
                if ((bool)SAVControl.GetProgramVariable("Piloting") && !(bool)SAVControl.GetProgramVariable("InVR")) { gameObject.SetActive(false); }
                if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetHookUp));
            }
        }
    }
    public void SetHooked()
    {
        VehicleAnimator.SetTrigger("hooked");
    }
    public void SetHookDown()
    {
        HookDown = true;
        VehicleAnimator.SetBool("hookdown", true);

        if ((bool)SAVControl.GetProgramVariable("IsOwner"))
        {
            EntityControl.SendEventToExtensions("SFEXT_O_HookDown");
        }
    }
    public void SetHookUp()
    {
        HookDown = false;
        VehicleAnimator.SetBool("hookdown", false);
        Hooked = false;

        if ((bool)SAVControl.GetProgramVariable("IsOwner"))
        {
            EntityControl.SendEventToExtensions("SFEXT_O_HookUp");
        }
        if (DisableGroundBrake && BrakeFunction)
        {
            BrakeFunction.SetProgramVariable("DisableGroundBrake", (int)BrakeFunction.GetProgramVariable("DisableGroundBrake") - 1);
            DisableGroundBrake = false;
        }
    }
    public void PlayCableSnap()
    {
        if (CableSnap) { CableSnap.Play(); }
    }
}
