
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Hook : UdonSharpBehaviour
{
    [SerializeField] private bool UseLeftTrigger;
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private GameObject Dial_Funcon;
    [SerializeField] private AudioSource CableSnap;
    [SerializeField] private Transform HookDetector;
    [SerializeField] private float HookedBrakeStrength = 55f;
    [SerializeField] private float HookedCableSnapDistance = 120f;
    [SerializeField] private DFUNC_Brake BrakeFunction;
    [System.NonSerializedAttribute] public bool CableSnapNull;
    private bool BreakFunctionNULL;
    public LayerMask HookCableLayer;
    private bool Dial_FunconNULL = true;
    private bool TriggerLastFrame;
    private EffectsController EffectsControl;
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
    public void SFEXT_L_ECStart()
    {
        EffectsControl = EngineControl.EffectsControl;
        Dial_FunconNULL = Dial_Funcon == null;
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(HookDown);
        VehicleTransform = EngineControl.VehicleMainObj.GetComponent<Transform>();
        VehicleAnimator = EngineControl.VehicleMainObj.GetComponent<Animator>();
        VehicleRigidbody = EngineControl.VehicleMainObj.GetComponent<Rigidbody>();
        SetHookUp();
        BreakFunctionNULL = BrakeFunction == null;
    }
    public void DFUNC_Selected()
    {
        gameObject.SetActive(true);
    }
    public void DFUNC_Deselected()
    {
        if (!HookDown) { gameObject.SetActive(false); }
        TriggerLastFrame = false;
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
        if (DisableGroundBrake && !BreakFunctionNULL) { BrakeFunction.DisableGroundBrake -= 1; DisableGroundBrake = false; }
        gameObject.SetActive(false);
    }
    public void SFEXT_O_Explode()
    {
        Hooked = false;
        SetHookUp();
    }
    public void SFEXT_O_RespawnButton()
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
    private void Update()
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
        if (Hooked && EngineControl.Taxiing)
        {
            if (!DisableGroundBrake && !BreakFunctionNULL) { BrakeFunction.DisableGroundBrake += 1; DisableGroundBrake = true; }
            if (Vector3.Distance(VehicleTransform.position, HookedLoc) > HookedCableSnapDistance)//real planes take around 80-90 meters to stop on a carrier
            {
                //if you go further than HookedBrakeMaxDistance you snap the cable and it hurts your plane by the % of the amount of time left of the 2 seconds it should have taken to stop you.
                float HookedDelta = (Time.time - HookedTime);
                if (HookedDelta < 2)
                {
                    EngineControl.Health -= ((-HookedDelta + 2) / 2) * EngineControl.FullHealth;
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
            if (EngineControl.Speed > HookedBrakeStrength * DeltaTime)
            {
                VehicleRigidbody.velocity += -EngineControl.CurrentVel.normalized * HookedBrakeStrength * DeltaTime;
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
                if (EngineControl.Piloting && !EngineControl.InVR) { gameObject.SetActive(true); }
                if (!Dial_FunconNULL) { Dial_Funcon.SetActive(true); }
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetHookDown");
            }
            else
            {
                if (EngineControl.Piloting && !EngineControl.InVR) { gameObject.SetActive(false); }
                if (!Dial_FunconNULL) { Dial_Funcon.SetActive(false); }
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetHookUp");
            }
        }
    }
    public void SetHookDown()
    {
        HookDown = true;
        VehicleAnimator.SetBool(HOOKDOWN_STRING, true);

        if (EngineControl.IsOwner)
        {
            EngineControl.SendEventToExtensions("SFEXT_O_HookDown", false);
        }
    }
    public void SetHookUp()
    {
        HookDown = false;
        VehicleAnimator.SetBool(HOOKDOWN_STRING, false);
        Hooked = false;

        if (EngineControl.IsOwner)
        {
            EngineControl.SendEventToExtensions("SFEXT_O_HookUp", false);
        }
    }
    public void PlayCableSnap()
    {
        if (!CableSnapNull) { CableSnap.Play(); }
    }
}
