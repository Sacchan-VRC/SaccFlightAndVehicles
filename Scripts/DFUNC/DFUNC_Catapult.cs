using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [DefaultExecutionOrder(10000)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DFUNC_Catapult : UdonSharpBehaviour
    {
        [Header("The position of this gameobject is important. It decides what point on the plane locks to the catapult.")]
        public UdonSharpBehaviour SAVControl;
        [Tooltip("Object enabled when function is active (used on MFD)")]
        public GameObject Dial_Funcon;
        [Tooltip("Oneshot sound played when attaching to catapult")]
        public AudioSource CatapultLock;
        [Tooltip("Maximum angular difference between vehicle and catapult allowed when attaching")]
        public float MaxAttachAngle = 15;
        [Tooltip("Layer to check for catapult triggers on")]
        public int CatapultLayer = 24;
        [Tooltip("Reference to the landing gear function so we can tell it to be disabled when on a catapult")]
        public UdonSharpBehaviour GearFunc;
        [Tooltip("Allow Catapult to be used not as DFUNC, just launch automatically")]
        public bool AutoLaunch = false;
        [Tooltip("Launch automatically only after going to full throttle? (SAV only)")]
        public bool AutoLaunch_FullThrottle = false;
        [Tooltip("How long after attaching does it take to launch automatically? Should have a minimum of about 1 to allow time for sync")]
        [SerializeField] private float AutoLaunchDelay = 1f;
        public string AnimTriggerLaunchName = "catapultlaunch";
        [Tooltip("Align vehicle to catapult when attaching to it?")]
        [SerializeField] private bool AlignToCatapult = true;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [System.NonSerializedAttribute] public SAV_PassengerFunctionsController PassengerFunctionsControl;
        private bool TriggerLastFrame;
        private bool Selected;
        [System.NonSerializedAttribute] public bool OnCatapult;
        [System.NonSerializedAttribute] public bool Launching = false;
        private bool Piloting = false;
        private Transform VehicleTransform;
        [System.NonSerializedAttribute] public Transform CatapultTransform;
        private Rigidbody VehicleRigidbody;
        private float InVehicleThrustVolumeFactor;
        private Animator VehicleAnimator;
        private Vector3 PlaneCatapultOffset;
        private Quaternion PlaneCatapultRotDif;
        private Quaternion CatapultRotLastFrame;
        private Vector3 CatapultPosLastFrame;
        private Animator CatapultAnimator;
        BoxCollider thisCollider;
        //these bools exist to make sure this script only ever adds/removes 1 from the value in enginecontroller
        private bool DisableTaxiRotation = false;
        private bool DisableGearToggle = false;
        private bool DisablePhysicsApplication = false;
        private bool InEditor;
        private bool IsOwner;
        private float AttachTime;
        private float FullThrottleTime;
        private bool Launching_AB;
        public void SFEXT_L_EntityStart()
        {
            InEditor = Networking.LocalPlayer == null;
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
            VehicleTransform = EntityControl.transform;
            VehicleRigidbody = EntityControl.GetComponent<Rigidbody>();
            VehicleAnimator = EntityControl.GetComponent<Animator>();
            thisCollider = GetComponent<BoxCollider>();
            {
                IsOwner = EntityControl.IsOwner;
            }
            colliderSmall();
        }
        // non-owners have a bigger collider so that they can find catapults if the position isn't synced perfectly
        void colliderLarge()
        {
            Vector3 colSize = thisCollider.size;
            colSize.x = 8;
            colSize.z = 40;
            thisCollider.size = colSize;
        }
        void colliderSmall()
        {
            Vector3 colSize = thisCollider.size;
            colSize.x = 0;
            colSize.z = 0;
            thisCollider.size = colSize;
        }
        public void DFUNC_Selected()
        {
            TriggerLastFrame = true;
            Selected = true;
        }
        public void DFUNC_Deselected()
        {
            Selected = false;
        }
        public void SFEXT_O_PilotEnter()
        {
            gameObject.SetActive(true);
            enabledToFindAnimator = false;
            Piloting = true;
            DisableOverrides();
        }
        public void SFEXT_G_PilotExit()
        {
            if (OnCatapult)
                CatapultLockOff();
        }
        public void SFEXT_O_PilotExit()
        {
            if (!Launching)
            {
                gameObject.SetActive(false);
                Piloting = false;
            }
            Selected = false;
            DisableOverrides();
        }
        public void SFEXT_L_PassengerEnter()
        {
            if (Dial_Funcon) Dial_Funcon.SetActive(OnCatapult);
        }
        public void SFEXT_O_TakeOwnership()
        {
            IsOwner = true;
            colliderSmall();
        }
        public void SFEXT_O_LoseOwnership()
        {
            IsOwner = false;
            Launching = false;
            OnCatapult = false;
            Piloting = false;
            gameObject.SetActive(false);
            DisableOverrides();
        }
        public void SFEXT_O_Explode()
        {
            OnCatapult = false;
            DisableOverrides();
        }
        public void SFEXT_G_RespawnButton()
        {
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
        }
        bool enabledToFindAnimator;
        private void EnableToFindAnimator()
        {
            if (!IsOwner)
            {
                enabledToFindAnimator = true;
                colliderLarge();
                gameObject.SetActive(true);
                SendCustomEventDelayedSeconds(nameof(FindAnimator_Disable), 3f);
            }
        }
        public void FindAnimator_Disable()
        {
            if (enabledToFindAnimator)
            {
                colliderSmall();
                enabledToFindAnimator = false;
                gameObject.SetActive(false);
            }
        }
        private bool FindCatapultAnimator(GameObject other)
        {
            if (OnCatapult && CatapultAnimator) return true;
            GameObject CatapultObjects = other.gameObject;
            CatapultAnimator = null;
            CatapultAnimator = other.GetComponent<Animator>();
            while (CatapultAnimator == null && CatapultObjects.transform.parent)
            {
                CatapultObjects = CatapultObjects.transform.parent.gameObject;
                CatapultAnimator = CatapultObjects.GetComponent<Animator>();
            }
            return CatapultAnimator != null;
        }
        private void OnTriggerEnter(Collider other)
        {
            if (EntityControl._dead) return;
            if (Piloting)
            {
                if (!OnCatapult)
                {
                    if (other)
                    {
                        if (other.gameObject.layer == CatapultLayer)
                        {
                            if (FindCatapultAnimator(other.gameObject))
                            {
                                CatapultTransform = other.transform;
                                //Hit detected, check if the plane is facing in the right direction..
                                if (Vector3.Angle(VehicleTransform.forward, CatapultTransform.transform.forward) < MaxAttachAngle)
                                {
                                    OnCatapult = true;
                                    AttachTime = Time.time;
                                    Launching_AB = false;
                                    CatapultPosLastFrame = CatapultTransform.position;
                                    //then lock the plane to the catapult! Works with the catapult in any orientation whatsoever.
                                    //match plane rotation to catapult excluding pitch because some planes have shorter front or back wheels

                                    if (AlignToCatapult)
                                    {
                                        Quaternion newrotation = Quaternion.Euler(new Vector3(VehicleTransform.rotation.eulerAngles.x, CatapultTransform.rotation.eulerAngles.y, CatapultTransform.rotation.eulerAngles.z));
                                        if (Quaternion.Dot(VehicleTransform.rotation, newrotation) < 0)
                                        {
                                            //flip to match the quat so we don't get messed up interpolations on remote clients
                                            newrotation = newrotation * Quaternion.Euler(0, 360, 0);
                                        }
                                        VehicleTransform.rotation = newrotation;
                                        //move the plane to the catapult, excluding the y component (relative to the catapult), so we are 'above' it

                                        float PlaneCatapultUpDistance = CatapultTransform.transform.InverseTransformDirection(CatapultTransform.position - VehicleTransform.position).y;
                                        VehicleTransform.position = CatapultTransform.position - (CatapultTransform.up * PlaneCatapultUpDistance);
                                        //move the plane back so that the catapult is aligned to the catapult detector
                                        float PlaneCatapultBackDistance = VehicleTransform.InverseTransformDirection(VehicleTransform.position - transform.position).z;
                                        VehicleTransform.position += CatapultTransform.forward * PlaneCatapultBackDistance;
                                        PlaneCatapultOffset = -(CatapultTransform.up * PlaneCatapultUpDistance) + (CatapultTransform.forward * PlaneCatapultBackDistance);
                                        PlaneCatapultRotDif = VehicleTransform.rotation * Quaternion.Inverse(CatapultTransform.rotation);

                                        VehicleRigidbody.position = VehicleTransform.position;
                                        VehicleRigidbody.rotation = VehicleTransform.rotation;
                                    }
                                    else
                                    {
                                        PlaneCatapultOffset = VehicleTransform.position - CatapultTransform.position;
                                        PlaneCatapultRotDif = VehicleTransform.rotation * Quaternion.Inverse(CatapultTransform.rotation);
                                    }

                                    if (!DisableGearToggle && GearFunc)
                                    {
                                        GearFunc.SetProgramVariable("DisableGearToggle", (int)GearFunc.GetProgramVariable("DisableGearToggle") + 1);
                                        DisableGearToggle = true;
                                    }
                                    if (!DisableTaxiRotation)
                                    {
                                        SAVControl.SetProgramVariable("DisableTaxiRotation", (int)SAVControl.GetProgramVariable("DisableTaxiRotation") + 1);
                                        DisableTaxiRotation = true;
                                    }
                                    if (!DisablePhysicsApplication)
                                    {
                                        SAVControl.SetProgramVariable("DisablePhysicsApplication", (int)SAVControl.GetProgramVariable("DisablePhysicsApplication") + 1);
                                        DisablePhysicsApplication = true;
                                    }
                                    //use dead to make plane invincible for x frames when entering the catapult to prevent taking G damage from stopping instantly
                                    EntityControl.dead = true;
                                    SendCustomEventDelayedFrames(nameof(deadfalse), 5);

                                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(CatapultLockIn));
                                }
                            }
                        }
                    }
                }
            }
            else//should only ever be true after EnableOneFrameToFindAnimator is called via network event
            {
                if (other)
                {
                    if (FindCatapultAnimator(other.gameObject))
                    {
                        FindAnimator_Disable();
                    }
                }
            }
        }
        private void Update()
        {
            if ((Piloting && OnCatapult) || Launching)
            {
                if (AutoLaunch)
                {
                    if (AutoLaunch_FullThrottle)
                    {
                        if (!Launching)
                        {
                            if (SAVControl && (float)SAVControl.GetProgramVariable("ThrottleInput") == 1f)
                            {
                                if (!Launching_AB)
                                {
                                    Launching_AB = true;
                                    FullThrottleTime = Time.time;
                                }
                            }
                            else { Launching_AB = false; }
                            if (Launching_AB && Time.time - FullThrottleTime > AutoLaunchDelay)
                            {
                                CatapultLaunchNow();
                            }
                        }
                    }
                    else if (!Launching)
                    {
                        if (Time.time - AttachTime > AutoLaunchDelay)
                        {
                            CatapultLaunchNow();
                        }
                    }
                }
                else
                {
                    if (!Launching && Selected)
                    {
                        float Trigger;
                        if (LeftDial)
                        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                        else
                        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
                        if (Trigger > 0.75)
                        {
                            if (!TriggerLastFrame)
                            {
                                CatapultLaunchNow();
                            }
                            TriggerLastFrame = true;
                        }
                        else { TriggerLastFrame = false; }
                    }
                }

                VehicleTransform.rotation = PlaneCatapultRotDif * CatapultTransform.rotation;
                VehicleTransform.position = CatapultTransform.position + PlaneCatapultOffset;
                VehicleRigidbody.position = VehicleTransform.position;
                VehicleRigidbody.rotation = VehicleTransform.rotation;
                VehicleRigidbody.velocity = Vector3.zero;
                VehicleRigidbody.angularVelocity = Vector3.zero;
                Quaternion CatapultRotDif = CatapultTransform.rotation * Quaternion.Inverse(CatapultRotLastFrame);
                if (Launching && !CatapultTransform.gameObject.activeInHierarchy)
                {
                    //catapult has finished it's animation, throw the plane
                    float DeltaTime = Time.deltaTime;
                    Launching = false;
                    DisableOverrides();
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(CatapultLockOff));
                    SAVControl.SetProgramVariable("Taxiinglerper", 0f);
                    // allow world creators to set an exact launch speed in case they want to make a fair race or something
                    Vector3 launchVel = (CatapultTransform.position - CatapultPosLastFrame) / DeltaTime;
                    if (CatapultAnimator)
                    {
                        string catName = CatapultAnimator.gameObject.name;
                        if (catName.Contains("speed="))
                        {
                            string[] splitCat = catName.Split("=");
                            if (splitCat.Length > 1)
                            {
                                float launchSpeed;
                                if (float.TryParse(splitCat[1], out launchSpeed))
                                {
                                    launchVel = launchVel.normalized * launchSpeed;
                                }
                            }
                        }
                    }
                    VehicleRigidbody.velocity = launchVel;
                    Vector3 CatapultRotDifEULER = CatapultRotDif.eulerAngles;
                    //.eulerangles is dumb (convert 0 - 360 to -180 - 180)
                    if (CatapultRotDifEULER.x > 180) { CatapultRotDifEULER.x -= 360; }
                    if (CatapultRotDifEULER.y > 180) { CatapultRotDifEULER.y -= 360; }
                    if (CatapultRotDifEULER.z > 180) { CatapultRotDifEULER.z -= 360; }
                    Vector3 CatapultRotDifrad = (CatapultRotDifEULER * Mathf.Deg2Rad) / DeltaTime;
                    VehicleRigidbody.angularVelocity = CatapultRotDifrad;
                    EntityControl.dead = true;
                    SendCustomEventDelayedFrames(nameof(deadfalse), 5);
                }
                CatapultRotLastFrame = CatapultTransform.rotation;
                CatapultPosLastFrame = CatapultTransform.position;
            }
        }
        private void DisableOverrides()
        {
            if (DisableGearToggle && GearFunc)
            {
                GearFunc.SetProgramVariable("DisableGearToggle", (int)GearFunc.GetProgramVariable("DisableGearToggle") - 1);
                DisableGearToggle = false;
            }
            if (DisableTaxiRotation)
            {
                SAVControl.SetProgramVariable("DisableTaxiRotation", (int)SAVControl.GetProgramVariable("DisableTaxiRotation") - 1);
                DisableTaxiRotation = false;
            }
            if (DisablePhysicsApplication)
            {
                SAVControl.SetProgramVariable("DisablePhysicsApplication", (int)SAVControl.GetProgramVariable("DisablePhysicsApplication") - 1);
                DisablePhysicsApplication = false;
            }
        }
        public void deadfalse()
        {
            EntityControl.dead = false;
            if (!Piloting)
            {
                SFEXT_O_PilotExit();
            }
        }
        public void KeyboardInput()
        {
            CatapultLaunchNow();
        }
        public void CatapultLaunchNow()
        {
            if (IsOwner && OnCatapult && !Launching)
            {
                Launching = true;
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(LaunchCatapult));
                EntityControl.SendEventToExtensions("SFEXT_O_LaunchFromCatapult");
            }
        }
        public void LaunchCatapult()
        {
            if (VehicleAnimator) { VehicleAnimator.SetTrigger(AnimTriggerLaunchName); }
            if (Utilities.IsValid(CatapultAnimator))
            { CatapultAnimator.SetTrigger("launch"); }
            CatapultAnimator = null;
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
            EntityControl.SendEventToExtensions("SFEXT_G_LaunchFromCatapult");
        }
        public void CatapultLockIn()
        {
            if (!IsOwner)
            {
                EnableToFindAnimator();
                OnCatapult = true;
            }
            if (VehicleAnimator) { VehicleAnimator.SetBool("oncatapult", true); }
            if (CatapultLock) { CatapultLock.Play(); }
            if (Dial_Funcon) { Dial_Funcon.SetActive(true); }
            EntityControl.SendEventToExtensions("SFEXT_G_CatapultLockIn");
        }
        public void CatapultLockOff()
        {
            OnCatapult = false;
            if (VehicleAnimator) { VehicleAnimator.SetBool("oncatapult", false); }
            EntityControl.SendEventToExtensions("SFEXT_G_CatapultLockOff");
        }
    }
}