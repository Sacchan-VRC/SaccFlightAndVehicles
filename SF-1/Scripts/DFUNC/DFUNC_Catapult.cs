
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Catapult : UdonSharpBehaviour
{
    [SerializeField] private bool UseLeftTrigger;
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private GameObject Dial_Funcon;
    [SerializeField] private AudioSource CatapultLock;
    [SerializeField] private AudioSource CatapultLaunch;
    [System.NonSerializedAttribute] private bool CatapultLaunchNull;
    private bool Dial_FunconNULL = true;
    private bool TriggerLastFrame;
    private int CatapultStatus;
    private int CatapultLayer = 24;
    private Transform VehicleTransform;
    private Transform CatapultTransform;
    private Quaternion CatapultLockRot;
    private Vector3 CatapultLockPos;
    private int CatapultDeadTimer;
    private Rigidbody VehicleRigidbody;
    public ParticleSystem CatapultSteam;
    private bool CatapultSteamNull = true;
    private float InVehicleThrustVolumeFactor;
    [System.NonSerializedAttribute] public bool CatapultLockNull;
    private Animator VehicleAnimator;
    private int ONCATAPULT_STRING = Animator.StringToHash("oncatapult");
    private float CatapultDetectorDist;
    private float PlaneCatapultDistance;
    private Quaternion CatapultRotLastFrame;
    private Vector3 CatapultPosLastFrame;
    private Animator CatapultAnimator;
    public void DFUNC_Selected()
    {
        gameObject.SetActive(true);
    }
    public void DFUNC_Deselected()
    {
        gameObject.SetActive(false);
    }
    public void SFEXT_L_ECStart()
    {
        Dial_FunconNULL = Dial_Funcon == null;
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(false);
        VehicleTransform = EngineControl.VehicleMainObj.GetComponent<Transform>();
        VehicleRigidbody = EngineControl.VehicleMainObj.GetComponent<Rigidbody>();
        VehicleAnimator = EngineControl.VehicleMainObj.GetComponent<Animator>();
        if (CatapultSteam != null) CatapultSteamNull = false;
        CatapultLockNull = (CatapultLock == null) ? true : false;
        CatapultLaunchNull = (CatapultLaunch == null) ? true : false;
    }
    public void SFEXT_O_PilotEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(CatapultStatus == 1);
        //if (!CatapultLaunchNull) CatapultLaunch.volume /= InVehicleThrustVolumeFactor;//not sure if this is good
        gameObject.SetActive(true);
    }
    private void SFEXT_O_PilotExit()
    {
        gameObject.SetActive(false);
        if (CatapultStatus == 1) { CatapultStatus = 0; }//keep launching if launching, otherwise unlock from catapult
        EngineControl.ConstantForceZero = false;
        //if (!CatapultLaunchNull) { CatapultLaunch.volume *= InVehicleThrustVolumeFactor; }//not sure if this is good
    }
    public void SFEXT_O_PassengerEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(CatapultStatus == 1);
    }
    public void SFEXT_O_CatapultLocked()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != null)
        {
            if (CatapultStatus == 0 && other.gameObject.layer == CatapultLayer)
            {
                CatapultTransform = other.transform;

                GameObject CatapultObjects = other.gameObject;
                while (!Utilities.IsValid(CatapultAnimator) && CatapultObjects.transform.parent != null)
                {
                    CatapultObjects = CatapultObjects.transform.parent.gameObject;
                    CatapultAnimator = CatapultObjects.GetComponent<Animator>();
                }
                //Hit detected, check if the plane is facing in the right direction..
                if (Vector3.Angle(VehicleTransform.forward, CatapultTransform.transform.forward) < 15)
                {
                    CatapultRotLastFrame = CatapultTransform.rotation;
                    CatapultPosLastFrame = CatapultTransform.position;
                    //then lock the plane to the catapult! Works with the catapult in any orientation whatsoever.
                    //match plane rotation to catapult excluding pitch because some planes have shorter front or back wheels
                    VehicleTransform.rotation = Quaternion.Euler(new Vector3(VehicleTransform.rotation.eulerAngles.x, CatapultTransform.rotation.eulerAngles.y, CatapultTransform.rotation.eulerAngles.z));

                    //move the plane to the catapult, excluding the y component (relative to the catapult), so we are 'above' it
                    Vector3 PCatDist = CatapultTransform.position - VehicleTransform.position;
                    PlaneCatapultDistance = CatapultTransform.transform.InverseTransformDirection(PCatDist).y;
                    VehicleTransform.position = CatapultTransform.position;
                    VehicleTransform.position -= CatapultTransform.up * PlaneCatapultDistance;

                    //move the plane back so that the catapult is aligned to the catapult detector
                    Vector3 CatDetDist = VehicleTransform.position - transform.position;
                    CatapultDetectorDist = VehicleTransform.InverseTransformDirection(CatDetDist).z;
                    VehicleTransform.position += CatapultTransform.forward * CatapultDetectorDist;

                    CatapultLockRot = VehicleTransform.rotation;//rotation to lock the plane to on the catapult
                    CatapultLockPos = VehicleTransform.position;
                    CatapultStatus = 1;//locked to catapult
                    EngineControl.ConstantForceZero = true;

                    //use dead to make plane invincible for 1 frame when entering the catapult to prevent damage which will be worse the higher your framerate is
                    EngineControl.dead = true;
                    CatapultDeadTimer = 4;

                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CatapultLockIn");
                }
            }
        }
    }

    private void Update()
    {
        switch (CatapultStatus)
        {
            case 0:
                break;
            case 1://locked on catapult
                   //dead == invincible, turn off once a frame has passed since attaching

                float Trigger;
                if (UseLeftTrigger)
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                else
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
                if (Trigger > 0.75)
                {
                    TriggerLastFrame = true;
                    if (!TriggerLastFrame)
                    {
                        CatapultStatus = 2;
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchCatapult");
                    }
                }
                else { TriggerLastFrame = false; }

                if (EngineControl.dead)
                {
                    CatapultDeadTimer -= 1;
                    if (CatapultDeadTimer == 0) EngineControl.dead = false;
                }


                Quaternion CatapultRotDif = CatapultTransform.rotation * Quaternion.Inverse(CatapultRotLastFrame);//difference in plane's rotation since last frame
                VehicleTransform.rotation = CatapultRotDif * VehicleTransform.rotation;
                VehicleTransform.position = CatapultTransform.position;
                VehicleTransform.position -= CatapultTransform.up * PlaneCatapultDistance;
                VehicleTransform.position += CatapultTransform.forward * CatapultDetectorDist;
                VehicleRigidbody.velocity = Vector3.zero;
                VehicleRigidbody.angularVelocity = Vector3.zero;
                CatapultRotLastFrame = CatapultTransform.rotation;
                break;
            case 2://launching
                Quaternion CatapultRotDif2 = CatapultTransform.rotation * Quaternion.Inverse(CatapultRotLastFrame);//difference in plane's rotation since last frame
                VehicleTransform.rotation = CatapultRotDif2 * VehicleTransform.rotation;
                VehicleTransform.position = CatapultTransform.position;
                VehicleTransform.position -= CatapultTransform.up * PlaneCatapultDistance;
                VehicleTransform.position += CatapultTransform.forward * CatapultDetectorDist;
                VehicleRigidbody.velocity = Vector3.zero;
                VehicleRigidbody.angularVelocity = Vector3.zero;
                CatapultRotLastFrame = CatapultTransform.rotation;
                if (!CatapultTransform.gameObject.activeInHierarchy)
                {
                    EngineControl.ConstantForceZero = false;
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CatapultLockOff");
                    EngineControl.dead = false;//just in case
                    CatapultStatus = 0;
                    EngineControl.Taxiinglerper = 0;
                    VehicleRigidbody.velocity = (CatapultTransform.position - CatapultPosLastFrame) / Time.deltaTime;
                    Vector3 CatapultRotDifrad = CatapultRotDif2.eulerAngles * Mathf.Deg2Rad;
                    Debug.Log(CatapultRotDifrad);
                    Debug.Log(string.Concat("angvel: ", VehicleRigidbody.angularVelocity));
                    VehicleRigidbody.angularVelocity = -CatapultRotDifrad;
                    Debug.Log(string.Concat("angvel2: ", VehicleRigidbody.angularVelocity));
                    EngineControl.dead = true;
                    SendCustomEventDelayedFrames("deadfalse", 4);
                }
                CatapultPosLastFrame = CatapultTransform.position;
                break;
        }
    }
    public void deadfalse()
    {
        EngineControl.dead = false;
    }
    public void KeyboardInput()
    {
        if (CatapultStatus == 1)
        {
            CatapultStatus = 2;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchCatapult");
        }
    }
    public void LaunchCatapult()
    {
        if (CatapultAnimator != null) { CatapultAnimator.SetTrigger("launch"); }
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(false);

        //CataPultLaunchEffects
        VehicleRigidbody.WakeUp();//i don't think it actually sleeps anyway but this might help other clients sync the launch faster idk
        if (CatapultSteam != null) { CatapultSteam.Play(); }
        if (!CatapultLaunchNull)
        {
            CatapultLaunch.Play();
        }
    }

    public void CatapultLockIn()
    {
        VehicleAnimator.SetBool(ONCATAPULT_STRING, true);
        VehicleRigidbody.Sleep();//don't think this actually helps
        if (!CatapultLockNull) { CatapultLock.Play(); }
    }
    public void CatapultLockOff()
    {
        VehicleAnimator.SetBool(ONCATAPULT_STRING, false);
    }
}

