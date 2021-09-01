
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Hook : UdonSharpBehaviour
{
    [SerializeField] private SaccAirVehicle SAVControl;
    [Tooltip("Object enabled when function is active (used on MFD)")]
    [SerializeField] private GameObject Dial_Funcon;
    [SerializeField] private AudioSource CableSnap;
    [SerializeField] private Transform HookDetector;
    [Tooltip("Strength of force slowing down the vehicle when it's snagged on a cable")]
    [SerializeField] private float HookedBrakeStrength = 55f;
    [Tooltip("Distance from the initial snag point that the cable will 'snap' and the vehicle will be released (and damaged) if it hasnt stopped")]
    [SerializeField] private float HookedCableSnapDistance = 120f;
    [Tooltip("If this vehicle has a brake function, need a reference to it to disable it when this function is braking the vehicle")]
    [SerializeField] private DFUNC_Brake BrakeFunction;
    private SaccEntity EntityControl;
    private bool UseLeftTrigger = false;
    [System.NonSerializedAttribute] public bool CableSnapNull;
    private bool BreakFunctionNULL;
    public LayerMask HookCableLayer;
    private bool Dial_FunconNULL = true;
    private bool TriggerLastFrame;
    [System.NonSerializedAttribute] private bool Hooked = false;
    [System.NonSerializedAttribute] private float HookedTime = 0f;
    private Vector3 HookedLoc;
    private Transform VehicleTransform;
    private int HOOKED_STRING = Animator.StringToHash("hooked");
    private Animator VehicleAnimator;
    private Rigidbody VehicleRigidbody;
    [System.NonSerializedAttribute] private bool HookDown = false;
    private int HOOKDOWN_STRING = Animator.StringToHash("hookdown");
    private bool DisableGroundBrake;
    private bool func_active;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXT_L_EntityStart()
    {
        Dial_FunconNULL = Dial_Funcon == null;
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(HookDown);
        VehicleTransform = SAVControl.EntityControl.transform;
        VehicleAnimator = SAVControl.EntityControl.GetComponent<Animator>();
        VehicleRigidbody = SAVControl.EntityControl.GetComponent<Rigidbody>();
        EntityControl = SAVControl.EntityControl;
        SetHookUp();
        BreakFunctionNULL = BrakeFunction == null;
    }
    public void DFUNC_Selected()
    {
        gameObject.SetActive(true);
        func_active = true;
    }
    public void DFUNC_Deselected()
    {
        if (!HookDown) { gameObject.SetActive(false); }
        TriggerLastFrame = false;
        func_active = false;
    }
    public void SFEXT_O_PilotEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(HookDown);
        if (HookDown) { gameObject.SetActive(true); }
    }
    public void SFEXT_O_PilotExit()
    {
        gameObject.SetActive(false);
        TriggerLastFrame = false;
        Hooked = false;
        func_active = false;
        if (DisableGroundBrake && !BreakFunctionNULL) { BrakeFunction.DisableGroundBrake -= 1; DisableGroundBrake = false; }
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
    public void SFEXT_O_PassengerEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(HookDown);
    }
    public void KeyboardInput()
    {
        ToggleHook();
    }
    public void SFEXT_O_PlayerJoined()
    {
        if (HookDown)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetHookDown"); }
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
                VehicleAnimator.SetTrigger(HOOKED_STRING);
            }
        }
        //slow down if hooked and on the ground
        if (Hooked && SAVControl.Taxiing)
        {
            if (!DisableGroundBrake && !BreakFunctionNULL) { BrakeFunction.DisableGroundBrake += 1; DisableGroundBrake = true; }
            if (Vector3.Distance(VehicleTransform.position, HookedLoc) > HookedCableSnapDistance)//real planes take around 80-90 meters to stop on a carrier
            {
                //if you go further than HookedBrakeMaxDistance you snap the cable and it hurts your plane by the % of the amount of time left of the 2 seconds it should have taken to stop you.
                float HookedDelta = (Time.time - HookedTime);
                if (HookedDelta < 2)
                {
                    SAVControl.Health -= ((-HookedDelta + 2) / 2) * SAVControl.FullHealth;
                }
                Hooked = false;
                //if you catch a cable but go airborne before snapping it, keep your hook out and then land somewhere else
                //you would hear the cablesnap sound when you touchdown, so limit it to within 5 seconds of hooking
                //this results in 1 frame's worth of not being able to catch a cable if hook stays down after being 'hooked', not snapping and then trying to hook again
                //but that should be a very rare and unnoitcable(if it happens) occurance
                if (HookedDelta < 5)
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PlayCableSnap"); }
            }
            float DeltaTime = Time.deltaTime;
            if (SAVControl.Speed > HookedBrakeStrength * DeltaTime)
            {
                VehicleRigidbody.velocity += -SAVControl.CurrentVel.normalized * HookedBrakeStrength * DeltaTime;
            }
            else
            {
                VehicleRigidbody.velocity = Vector3.zero;
            }
            //Debug.Log("hooked");
        }
        else
        {
            if (DisableGroundBrake && !BreakFunctionNULL) { BrakeFunction.DisableGroundBrake -= 1; DisableGroundBrake = false; }
        }
    }

    public void ToggleHook()
    {
        if (HookDetector != null)
        {
            if (!HookDown)
            {
                if (SAVControl.Piloting && !SAVControl.InVR) { gameObject.SetActive(true); }
                if (!Dial_FunconNULL) { Dial_Funcon.SetActive(true); }
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetHookDown");
            }
            else
            {
                if (SAVControl.Piloting && !SAVControl.InVR) { gameObject.SetActive(false); }
                if (!Dial_FunconNULL) { Dial_Funcon.SetActive(false); }
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetHookUp");
            }
        }
    }
    public void SetHookDown()
    {
        HookDown = true;
        VehicleAnimator.SetBool(HOOKDOWN_STRING, true);

        if (SAVControl.IsOwner)
        {
            EntityControl.SendEventToExtensions("SFEXT_O_HookDown");
        }
    }
    public void SetHookUp()
    {
        HookDown = false;
        VehicleAnimator.SetBool(HOOKDOWN_STRING, false);
        Hooked = false;

        if (SAVControl.IsOwner)
        {
            EntityControl.SendEventToExtensions("SFEXT_O_HookUp");
        }
    }
    public void PlayCableSnap()
    {
        if (!CableSnapNull) { CableSnap.Play(); }
    }
}
