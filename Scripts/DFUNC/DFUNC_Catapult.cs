
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
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
        public string AnimTriggerLaunchName = "catapultlaunch";
        private SaccEntity EntityControl;
        private bool UseLeftTrigger = false;
        private bool TriggerLastFrame;
        private bool Selected;
        [System.NonSerializedAttribute] public bool OnCatapult;
        [System.NonSerializedAttribute] public bool Launching = false;
        private bool Piloting = false;
        private Transform VehicleTransform;
        [System.NonSerializedAttribute] public Transform CatapultTransform;
        private int CatapultDeadTimer;
        private Rigidbody VehicleRigidbody;
        private float InVehicleThrustVolumeFactor;
        private Animator VehicleAnimator;
        private Vector3 PlaneCatapultOffset;
        private Quaternion PlaneCatapultRotDif;
        private Quaternion CatapultRotLastFrame;
        private Vector3 CatapultPosLastFrame;
        private Animator CatapultAnimator;
        //these bools exist to make sure this script only ever adds/removes 1 from the value in enginecontroller
        private bool DisableTaxiRotation = false;
        private bool DisableGearToggle = false;
        private bool OverrideConstantForce = false;
        private bool InEditor;
        private bool IsOwner;
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        public void SFEXT_L_EntityStart()
        {
            InEditor = Networking.LocalPlayer == null;
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
            EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
            VehicleTransform = EntityControl.transform;
            VehicleRigidbody = EntityControl.GetComponent<Rigidbody>();
            VehicleAnimator = EntityControl.GetComponent<Animator>();
            IsOwner = (bool)SAVControl.GetProgramVariable("IsOwner");
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
            Piloting = true;
            DisableOverrides();
        }
        public void SFEXT_O_PilotExit()
        {
            if (!Launching)
            {
                gameObject.SetActive(false);
                Piloting = false;
                if (OnCatapult) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(CatapultLockOff)); }
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
            if (OnCatapult) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(CatapultLockOff)); }
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
        private void EnableOneFrameToFindAnimator()
        {
            if (!IsOwner)
            {
                gameObject.SetActive(true);
                SendCustomEventDelayedFrames(nameof(DisableThisObjNonOnwer), 1);
            }
        }
        private void DisableThisObjNonOnwer()
        {
            if (!IsOwner)
            { gameObject.SetActive(false); }
        }
        private bool FindCatapultAnimator(GameObject other)
        {
            if (OnCatapult) { return false; }//Why is this needed?
            GameObject CatapultObjects = other.gameObject;
            CatapultAnimator = null;
            CatapultAnimator = other.GetComponent<Animator>();
            while (CatapultAnimator == null && CatapultObjects.transform.parent)
            {
                CatapultObjects = CatapultObjects.transform.parent.gameObject;
                CatapultAnimator = CatapultObjects.GetComponent<Animator>();
            }
            return (CatapultAnimator != null);
        }
        private void OnTriggerEnter(Collider other)
        {
            if (Piloting && !EntityControl._dead)
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
                                    CatapultPosLastFrame = CatapultTransform.position;
                                    //then lock the plane to the catapult! Works with the catapult in any orientation whatsoever.
                                    //match plane rotation to catapult excluding pitch because some planes have shorter front or back wheels
                                    Quaternion newrotation = Quaternion.Euler(new Vector3(VehicleTransform.rotation.eulerAngles.x, CatapultTransform.rotation.eulerAngles.y, CatapultTransform.rotation.eulerAngles.z));
                                    //flip the plane 360 degrees if the quaternion is the wrong way round, so that syncscript doesnt make other players see you do a 360
                                    bool InvertQuat = Quaternion.Dot(VehicleTransform.rotation, newrotation) < 0;
                                    VehicleTransform.rotation = newrotation;
                                    if (InvertQuat)
                                    {
                                        VehicleTransform.Rotate(new Vector3(0, 360, 0));
                                    }
                                    //move the plane to the catapult, excluding the y component (relative to the catapult), so we are 'above' it
                                    float PlaneCatapultUpDistance = CatapultTransform.transform.InverseTransformDirection(CatapultTransform.position - VehicleTransform.position).y;
                                    VehicleTransform.position = CatapultTransform.position;
                                    VehicleTransform.position -= CatapultTransform.up * PlaneCatapultUpDistance;

                                    //move the plane back so that the catapult is aligned to the catapult detector
                                    float PlaneCatapultBackDistance = VehicleTransform.InverseTransformDirection(VehicleTransform.position - transform.position).z;
                                    VehicleTransform.position += CatapultTransform.forward * PlaneCatapultBackDistance;

                                    PlaneCatapultOffset = -(CatapultTransform.up * PlaneCatapultUpDistance) + (CatapultTransform.forward * PlaneCatapultBackDistance);
                                    PlaneCatapultRotDif = VehicleTransform.rotation * Quaternion.Inverse(CatapultTransform.rotation);

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
                                    if (!OverrideConstantForce)
                                    {
                                        SAVControl.SetProgramVariable("OverrideConstantForce", (int)SAVControl.GetProgramVariable("OverrideConstantForce") + 1);
                                        SAVControl.SetProgramVariable("CFRelativeForceOverride", Vector3.zero);
                                        SAVControl.SetProgramVariable("CFRelativeTorqueOverride", Vector3.zero);
                                        OverrideConstantForce = true;
                                    }
                                    //use dead to make plane invincible for x frames when entering the catapult to prevent taking G damage from stopping instantly
                                    EntityControl.dead = true;
                                    CatapultDeadTimer = 5;

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
                    FindCatapultAnimator(other.gameObject);
                }
            }
        }
        private void Update()
        {
            if ((Piloting && OnCatapult) || Launching)
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
                            CatapultLaunchNow();
                        }
                        TriggerLastFrame = true;
                    }
                    else { TriggerLastFrame = false; }
                }
                if (EntityControl._dead)
                {
                    CatapultDeadTimer -= 1;
                    if (CatapultDeadTimer == 0) { EntityControl.dead = false; }
                }

                VehicleTransform.rotation = PlaneCatapultRotDif * CatapultTransform.rotation;
                VehicleTransform.position = CatapultTransform.position + PlaneCatapultOffset;
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
                    VehicleRigidbody.velocity = (CatapultTransform.position - CatapultPosLastFrame) / DeltaTime;
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
            if (OverrideConstantForce)
            {
                SAVControl.SetProgramVariable("OverrideConstantForce", (int)SAVControl.GetProgramVariable("OverrideConstantForce") - 1);
                OverrideConstantForce = false;
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
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(PreLaunchCatapult));
                EntityControl.SendEventToExtensions("SFEXT_O_LaunchFromCatapult");
            }
        }
        public void PreLaunchCatapult()
        {
            if (!IsOwner) { EnableOneFrameToFindAnimator(); }
            SendCustomEventDelayedFrames(nameof(LaunchCatapult), 3);
            if (VehicleAnimator) { VehicleAnimator.SetTrigger(AnimTriggerLaunchName); }
        }
        public void LaunchCatapult()
        {
            if (Utilities.IsValid(CatapultAnimator))
            { CatapultAnimator.SetTrigger("launch"); }
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
            EntityControl.SendEventToExtensions("SFEXT_G_LaunchFromCatapult");
        }
        public void CatapultLockIn()
        {
            OnCatapult = true;
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