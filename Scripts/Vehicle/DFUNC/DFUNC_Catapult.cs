
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Catapult : UdonSharpBehaviour
{
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private GameObject Dial_Funcon;
    [SerializeField] private AudioSource CatapultLock;
    [SerializeField] private float MaxAttachAngle = 15;
    private bool UseLeftTrigger = false;
    [System.NonSerializedAttribute] private bool CatapultLaunchNull;
    private bool Dial_FunconNULL = true;
    private bool TriggerLastFrame;
    private bool Selected;
    private bool OnCatapult;
    private bool Launching = false;
    private bool Pilot = false;
    private int CatapultLayer = 24;
    private Transform VehicleTransform;
    private Transform CatapultTransform;
    private int CatapultDeadTimer;
    private Rigidbody VehicleRigidbody;
    private bool CatapultSteamNull = true;
    private float InVehicleThrustVolumeFactor;
    [System.NonSerializedAttribute] public bool CatapultLockNull;
    private Animator VehicleAnimator;
    private int ONCATAPULT_STRING = Animator.StringToHash("oncatapult");
    private float PlaneCatapultBackDistance;
    private float PlaneCatapultUpDistance;
    private Quaternion PlaneCatapultRotDif;
    private Quaternion CatapultRotLastFrame;
    private Vector3 CatapultPosLastFrame;
    private Animator CatapultAnimator;
    //these bools exist to make sure this script only ever adds/removes 1 from the value in enginecontroller
    private bool DisableTaxiRotation = false;
    private bool DisableGearToggle = false;
    private bool SetConstantForceZero = false;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXT_L_ECStart()
    {
        Dial_FunconNULL = Dial_Funcon == null;
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(false);
        VehicleTransform = EngineControl.VehicleMainObj.GetComponent<Transform>();
        VehicleRigidbody = EngineControl.VehicleMainObj.GetComponent<Rigidbody>();
        VehicleAnimator = EngineControl.VehicleMainObj.GetComponent<Animator>();
        CatapultLockNull = (CatapultLock == null) ? true : false;
    }
    public void DFUNC_Selected()
    {
        Selected = true;
    }
    public void DFUNC_Deselected()
    {
        Selected = false;
        TriggerLastFrame = false;
    }
    public void SFEXT_O_PilotEnter()
    {
        gameObject.SetActive(true);
        Pilot = true;
        if (DisableGearToggle) { EngineControl.DisableGearToggle -= 1; DisableGearToggle = false; }
        if (DisableTaxiRotation) { EngineControl.DisableTaxiRotation -= 1; DisableTaxiRotation = false; }
        TriggerLastFrame = false;
        OnCatapult = false;
    }
    public void SFEXT_O_PilotExit()
    {
        if (!Launching)
        {
            gameObject.SetActive(false);
            Pilot = false;
            if (OnCatapult) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CatapultLockOff"); }
        }
        OnCatapult = false;
        Selected = false;
        TriggerLastFrame = false;
        if (DisableGearToggle) { EngineControl.DisableGearToggle -= 1; DisableGearToggle = false; }
        if (DisableTaxiRotation) { EngineControl.DisableTaxiRotation -= 1; DisableTaxiRotation = false; }
    }
    public void SFEXT_O_PassengerEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(OnCatapult);
    }
    public void SFEXT_O_TakeOwnership()
    {
        if (OnCatapult) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CatapultLockOff"); }
    }
    public void SFEXT_O_LoseOwnership()
    {
        Launching = false;
        OnCatapult = false;
        gameObject.SetActive(false);
        Pilot = false;
        if (DisableGearToggle) { EngineControl.DisableGearToggle -= 1; DisableGearToggle = false; }
        if (DisableTaxiRotation) { EngineControl.DisableTaxiRotation -= 1; DisableTaxiRotation = false; }
        if (SetConstantForceZero) { EngineControl.SetConstantForceZero -= 1; SetConstantForceZero = false; }
    }
    public void SFEXT_O_Explode()
    {
        if (DisableGearToggle) { EngineControl.DisableGearToggle -= 1; DisableGearToggle = false; }
        if (DisableTaxiRotation) { EngineControl.DisableTaxiRotation -= 1; DisableTaxiRotation = false; }
        if (SetConstantForceZero) { EngineControl.SetConstantForceZero -= 1; SetConstantForceZero = false; }
    }
    private void EnableOneFrameToFindAnimator()
    {
        if (!EngineControl.IsOwner)
        {
            gameObject.SetActive(true);
            SendCustomEventDelayedFrames("DisableThisObjNonOnwer", 1);
        }
    }
    private void DisableThisObjNonOnwer()
    {
        if (!EngineControl.IsOwner)
        {
            gameObject.SetActive(false);
        }
    }
    private void FindCatapultAnimator(GameObject other)
    {
        GameObject CatapultObjects = other.gameObject;
        CatapultAnimator = null;
        while (!Utilities.IsValid(CatapultAnimator) && CatapultObjects.transform.parent != null)
        {
            CatapultObjects = CatapultObjects.transform.parent.gameObject;
            CatapultAnimator = CatapultObjects.GetComponent<Animator>();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (Pilot)
        {
            if (other != null)
            {
                if (!OnCatapult)
                {
                    if (other.gameObject.layer == CatapultLayer)
                    {
                        FindCatapultAnimator(other.gameObject);
                        CatapultTransform = other.transform;
                        //Hit detected, check if the plane is facing in the right direction..
                        if (Vector3.Angle(VehicleTransform.forward, CatapultTransform.transform.forward) < MaxAttachAngle)
                        {
                            CatapultPosLastFrame = CatapultTransform.position;
                            //then lock the plane to the catapult! Works with the catapult in any orientation whatsoever.
                            //match plane rotation to catapult excluding pitch because some planes have shorter front or back wheels
                            VehicleTransform.rotation = Quaternion.Euler(new Vector3(VehicleTransform.rotation.eulerAngles.x, CatapultTransform.rotation.eulerAngles.y, CatapultTransform.rotation.eulerAngles.z));

                            //move the plane to the catapult, excluding the y component (relative to the catapult), so we are 'above' it
                            PlaneCatapultUpDistance = CatapultTransform.transform.InverseTransformDirection(CatapultTransform.position - VehicleTransform.position).y;
                            VehicleTransform.position = CatapultTransform.position;
                            VehicleTransform.position -= CatapultTransform.up * PlaneCatapultUpDistance;

                            //move the plane back so that the catapult is aligned to the catapult detector
                            PlaneCatapultBackDistance = VehicleTransform.InverseTransformDirection(VehicleTransform.position - transform.position).z;
                            VehicleTransform.position += CatapultTransform.forward * PlaneCatapultBackDistance;

                            PlaneCatapultRotDif = CatapultTransform.rotation * Quaternion.Inverse(VehicleTransform.rotation);

                            if (!DisableGearToggle) { EngineControl.DisableGearToggle += 1; DisableGearToggle = true; }
                            if (!DisableTaxiRotation) { EngineControl.DisableTaxiRotation += 1; DisableTaxiRotation = true; }
                            if (!SetConstantForceZero) { EngineControl.SetConstantForceZero += 1; SetConstantForceZero = true; }
                            //use dead to make plane invincible for x frames when entering the catapult to prevent taking G damage from stopping instantly
                            EngineControl.dead = true;
                            CatapultDeadTimer = 5;

                            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CatapultLockIn");
                        }
                    }
                }
            }
        }
        else//should only ever be true after EnableOneFrameToFindAnimator is called via network event
        {
            if (other != null)
            {
                FindCatapultAnimator(other.gameObject);
            }
        }

    }

    private void Update()
    {
        if (Pilot && OnCatapult)
        {
            if (!Launching && Selected)
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
                        Launching = true;
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PreLaunchCatapult");
                        EngineControl.SendEventToExtensions("SFEXT_O_LaunchFromCatapult");
                    }
                    TriggerLastFrame = true;
                }
                else { TriggerLastFrame = false; }
            }
            if (EngineControl.dead)
            {
                CatapultDeadTimer -= 1;
                if (CatapultDeadTimer == 0) EngineControl.dead = false;
            }

            VehicleTransform.rotation = PlaneCatapultRotDif * CatapultTransform.rotation;
            VehicleTransform.position = CatapultTransform.position;
            VehicleTransform.position -= CatapultTransform.up * PlaneCatapultUpDistance;
            VehicleTransform.position += CatapultTransform.forward * PlaneCatapultBackDistance;
            VehicleRigidbody.velocity = Vector3.zero;
            VehicleRigidbody.angularVelocity = Vector3.zero;
            Quaternion CatapultRotDif = CatapultTransform.rotation * Quaternion.Inverse(CatapultRotLastFrame);
            if (Launching && !CatapultTransform.gameObject.activeInHierarchy)
            {
                float DeltaTime = Time.deltaTime;
                TriggerLastFrame = false;
                Launching = false;
                if (DisableGearToggle) { EngineControl.DisableGearToggle -= 1; DisableGearToggle = false; }
                if (DisableTaxiRotation) { EngineControl.DisableTaxiRotation -= 1; DisableTaxiRotation = false; }
                if (SetConstantForceZero) { EngineControl.SetConstantForceZero -= 1; SetConstantForceZero = false; }
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CatapultLockOff");
                EngineControl.Taxiinglerper = 0;
                VehicleRigidbody.velocity = (CatapultTransform.position - CatapultPosLastFrame) / DeltaTime;
                Vector3 CatapultRotDifEULER = CatapultRotDif.eulerAngles;
                //.eulerangles is dumb (convert 0 - 360 to -180 - 180)
                if (CatapultRotDifEULER.x > 180) { CatapultRotDifEULER.x -= 360; }
                if (CatapultRotDifEULER.y > 180) { CatapultRotDifEULER.y -= 360; }
                if (CatapultRotDifEULER.z > 180) { CatapultRotDifEULER.z -= 360; }
                Vector3 CatapultRotDifrad = (CatapultRotDifEULER * Mathf.Deg2Rad) / DeltaTime;
                VehicleRigidbody.angularVelocity = CatapultRotDifrad;
                EngineControl.dead = true;
                SendCustomEventDelayedFrames("deadfalse", 5);
            }
            CatapultRotLastFrame = CatapultTransform.rotation;
            CatapultPosLastFrame = CatapultTransform.position;
        }
    }
    public void deadfalse()
    {
        EngineControl.dead = false;
        if (!EngineControl.Piloting)
        {
            gameObject.SetActive(false);
            Pilot = false;
        }
    }
    public void KeyboardInput()
    {
        if (OnCatapult && !Launching)
        {
            Launching = true;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PreLaunchCatapult");
        }
    }
    public void PreLaunchCatapult()
    {
        if (!EngineControl.IsOwner) { EnableOneFrameToFindAnimator(); }
        SendCustomEventDelayedFrames("LaunchCatapult", 3);
    }
    public void LaunchCatapult()
    {
        if (CatapultAnimator != null) { CatapultAnimator.SetTrigger("launch"); }
        if (!Dial_FunconNULL) { Dial_Funcon.SetActive(false); }

        VehicleRigidbody.WakeUp();//i don't think it actually sleeps anyway but this might help other clients sync the launch faster idk
    }

    public void CatapultLockIn()
    {
        OnCatapult = true;
        VehicleAnimator.SetBool(ONCATAPULT_STRING, true);
        VehicleRigidbody.Sleep();//don't think this actually helps
        if (!CatapultLockNull) { CatapultLock.Play(); }
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(true);
    }
    public void CatapultLockOff()
    {
        OnCatapult = false;
        VehicleAnimator.SetBool(ONCATAPULT_STRING, false);
    }
}