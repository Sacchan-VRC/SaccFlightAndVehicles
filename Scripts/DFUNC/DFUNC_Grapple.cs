
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_Grapple : UdonSharpBehaviour
    {
        public Transform Hook;
        public Transform HookRopePoint;
        [Tooltip("Object enabled when function is active (used on MFD)")]
        public GameObject Dial_Funcon;
        public float HookSpeed = 300f;
        public float SwingStrength = 20f;
        public UdonSharpBehaviour SAVControl;
        private SaccFlightAndVehicles.SaccEntity EntityControl;
        private Rigidbody VehicleRB;
        [Tooltip("Hook launches from here, in this transform's forward direction")]
        public Transform HookLaunchPoint;
        [Tooltip("Pull grappled rigidbodies towards the vehicle?")]
        public bool TwoWayForces = true;
        [Tooltip("Force is applied by the client in the vehicle that got hooked, less technical issues, but very janky movement")]
        public bool TwoWayForces_LocalForceMode = true;
        [Tooltip("Apply the forces to this vehicle at ForceApplyPoint or CoM?")]
        public bool UseForceApplyPoint = true;
        [Tooltip("Apply the forces at this point, requires tickbox above to be ticked")]
        public Transform ForceApplyPoint;
        public float PullReductionStrength = 5f;
        [Tooltip("Snap rigidbody target connection points to just above their CoM?")]
        public bool HoldTargetUpright = false;
        [Tooltip("Select the function instead of just instantly firing it with keyboard input?")]
        public bool KeyboardSelectMode = false;
        public LineRenderer Rope_Line;
        public Transform RopeBasePoint;
        public LayerMask HookLayers;
        public float HookStrength = 10f;
        public float MaxExtraStrByDist = 10f;
        public float HookRange = 340f;
        public float SphereCastAccuracy = 0.125f;
        public AnimationCurve PullStrOverDist;
        public AudioSource HookLaunch;
        public AudioSource HookAttach;
        public AudioSource HookReelIn;
        public GameObject[] EnableOnSelect;
        private float HookLaunchTime;
        private Vector3 HookStartPos;
        private Transform HookedTransform;
        private Vector3 HookedTransformOffset;
        private Collider HookedCollider;
        private GameObject HookedGameObject;
        private Rigidbody HookedRB;
        private bool InVr;
        private SaccFlightAndVehicles.SaccEntity HookedEntity;
        //these 2 variables are only used if TwoWayForces_LocalForceMode is true
        private bool NonLocalAttached_Pilot;//so pilot of this vehicle knows if the vehicle that's attached is owner by someone else
        private bool NonLocalAttached;//if you are in a vehicle that is attached
        private bool PlayReelIn = false;
        private bool Occupied = false;
        private bool LeftDial = false;
        private int DialPosition = -999;
        private bool Overriding_DisallowOwnerShipTransfer = false;
        [UdonSynced, FieldChangeCallback(nameof(HookAttachPoint))] private Vector3 _HookAttachPoint;
        public Vector3 HookAttachPoint
        {
            set
            {
                if (!Initialized) { _HookAttachPoint = value; return; }
                if (!_HookLaunched)//hook launch and attach was recieved on same update
                {
                    LaunchHook(true);
                }
                float spheresize = SphereCastAccuracy;
                int hitlen = 0;
                RaycastHit[] hits = new RaycastHit[0];
                bool HitSelf = false;
                while (spheresize < 17 && hitlen == 0 && !HitSelf)
                {
                    hits = Physics.SphereCastAll(value, spheresize, Vector3.up, 0, HookLayers, QueryTriggerInteraction.Ignore);
                    spheresize *= 2;
                    hitlen = hits.Length;

                    if (hitlen > 0)
                    {
                        HookedTransform = null;
                        if (Dial_Funcon) { Dial_Funcon.SetActive(true); }
                        foreach (RaycastHit hit in hits)
                        {
                            HitSelf = false;
                            if (hit.collider)
                            {
                                float NearestDist = float.MaxValue;
                                float tempdist = Vector3.Distance(hit.collider.ClosestPoint(value), value);
                                if (tempdist < NearestDist)
                                {
                                    if (HookedEntity) { UndoHookOverrides(); }
                                    if (hit.collider.attachedRigidbody)
                                    {
                                        HookedEntity = hit.collider.attachedRigidbody.GetComponent<SaccFlightAndVehicles.SaccEntity>();
                                        if (HookedEntity == EntityControl) { HitSelf = true; continue; } //skip if raycast finds own vehicle
                                        else
                                        { HookedEntity.SendEventToExtensions("SFEXT_L_WakeUp"); }
                                    }
                                    else { HookedEntity = null; }
                                    NearestDist = tempdist;
                                    HookedCollider = hit.collider;
                                    HookedGameObject = hit.collider.gameObject;
                                    HookedTransform = hit.collider.transform;
                                    HookedTransformOffset = HookedTransform.InverseTransformPoint(hit.collider.ClosestPoint(value));
                                    if (TwoWayForces)
                                    {
                                        HookedRB = HookedCollider.attachedRigidbody;
                                        if (HookedRB)
                                        {
                                            if (HookedRB.isKinematic && !HookedEntity)
                                            {
                                                HookedRB = null;//don't try to pull kinematic objects by weight ratio
                                            }
                                            else
                                            {
                                                if (HoldTargetUpright)
                                                {
                                                    HookedTransform = HookedRB.transform;
                                                    Vector3 targCoMPos = HookedRB.position + HookedRB.centerOfMass;
                                                    Vector3 abovedist = (HookedTransform.up * Vector3.Distance(targCoMPos, HookLaunchPoint.position) / 2f);
                                                    Vector3 raypoint = targCoMPos + abovedist;
                                                    Vector3 raydir = targCoMPos - raypoint;
                                                    RaycastHit hit2;
                                                    if (Physics.Raycast(raypoint, raydir, out hit2, abovedist.magnitude + 10f, HookLayers, QueryTriggerInteraction.Ignore))
                                                    {
                                                        HookedTransformOffset = HookedTransform.InverseTransformPoint(hit2.point);
                                                    }
                                                }
                                                if (TwoWayForces_LocalForceMode)
                                                {
                                                    var objpickup = (VRC.SDK3.Components.VRCPickup)HookedRB.GetComponent(typeof(VRC.SDK3.Components.VRCPickup));
                                                    if (IsOwner)
                                                    {
                                                        if ((!objpickup || !objpickup.IsHeld) && (!HookedEntity || !HookedEntity.Occupied))
                                                        {
                                                            Networking.SetOwner(Networking.LocalPlayer, HookedRB.gameObject);
                                                        }
                                                        else
                                                        {
                                                            NonLocalAttached_Pilot = true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (Networking.LocalPlayer.IsOwner(HookedRB.gameObject))
                                                        {
                                                            NonLocalAttached = true;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (IsOwner)
                                                    {
                                                        Networking.SetOwner(Networking.LocalPlayer, HookedRB.gameObject);
                                                    }
                                                }
                                                //people cant take ownership while vehicle is being held.
                                                //localforcemode is only active if someone is in the vehicle when its grabbed
                                                if (HookedEntity && (!TwoWayForces_LocalForceMode || !HookedEntity.Occupied))
                                                {
                                                    if (!Overriding_DisallowOwnerShipTransfer)
                                                    {
                                                        HookedEntity.SetProgramVariable("DisallowOwnerShipTransfer", (int)HookedEntity.GetProgramVariable("DisallowOwnerShipTransfer") + 1);
                                                        Overriding_DisallowOwnerShipTransfer = true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (!HookedTransform)//nothing was found (terrain colliders dont work with spherecastall?)
                        {
                            HookWorldPos(value);
                        }
                    }
                    else
                    {//extreme lag/late joiners if object targeted was in air/sea/not near anything (or if trying to lock terrain apparently)
                        HookWorldPos(value);
                    }
                }
                Vector3 hookedpoint = HookedTransform.TransformPoint(HookedTransformOffset);
                HookLength = Vector3.Distance(HookLaunchPoint.position, hookedpoint);
                HookAttached = true;
                SetHookPos();
                HookAttach.Play();
                _HookAttachPoint = hookedpoint;
            }
            get => _HookAttachPoint;
        }
        private void HookWorldPos(Vector3 val)
        {
            HookedTransform = transform.root;//should be a non-moving object
            HookedTransformOffset = HookedTransform.InverseTransformPoint(val);
            HookedCollider = null;
            HookedRB = null;
        }
        public void SetHookPos()
        {
            if (HookAttached)
            {
                if (IsOwner)
                {
                    //jittery if done in fixedupdate
                    if (Rope_Line)
                    {
                        Rope_Line.SetPosition(0, RopeBasePoint.position);
                        Rope_Line.SetPosition(1, HookRopePoint.position);
                    }
                }
                else
                {
                    if ((HookedCollider && !HookedCollider.enabled) || !HookedTransform.gameObject.activeInHierarchy) { ResetHook(); return; }
                    Hook.position = HookedTransform.TransformPoint(HookedTransformOffset);
                    if (Rope_Line)
                    {
                        Rope_Line.SetPosition(0, RopeBasePoint.position);
                        Rope_Line.SetPosition(1, HookRopePoint.position);
                    }
                }
                SendCustomEventDelayedFrames(nameof(SetHookPos), 1);
            }
        }
        private Quaternion HookStartRot;
        private Quaternion HookLaunchRot;
        private float HookLength;
        private bool HookAttached = false;
        private Vector3 LaunchVec;
        private float LaunchSpeed;
        private Transform HookParentStart;
        private bool IsOwner;
        private bool Initialized;
        [UdonSynced, FieldChangeCallback(nameof(HookLaunched))] private bool _HookLaunched;
        public bool HookLaunched
        {
            set
            {
                if (value)
                {
                    _HookLaunched = value;
                    if (!HookAttached)
                    { LaunchHook(false); }
                }
                else
                {
                    ResetHook();
                    _HookLaunched = value;
                    if (!Occupied && !IsOwner) { gameObject.SetActive(false); }
                }
            }
            get => _HookLaunched;
        }
        public void SFEXT_L_EntityStart()
        {
            Initialized = true;
            if (!ForceApplyPoint) { ForceApplyPoint = HookLaunchPoint; }
            VehicleRB = (Rigidbody)SAVControl.GetProgramVariable("VehicleRigidbody");
            EntityControl = (SaccFlightAndVehicles.SaccEntity)SAVControl.GetProgramVariable("EntityControl");
            IsOwner = (bool)SAVControl.GetProgramVariable("IsOwner");
            HookedTransform = transform;//avoid null
            HookParentStart = Hook.parent;
            HookStartPos = Hook.localPosition;
            HookStartRot = Hook.localRotation;
            InVr = Networking.LocalPlayer.IsUserInVR();
            foreach (GameObject obj in EnableOnSelect) { obj.SetActive(false); }
            FindSelf();
        }
        public void LaunchHook(bool InstantHit)
        {
            if (!Initialized) { return; }
            Rope_Line.gameObject.SetActive(true);
            HookLaunchTime = Time.time;
            HookLaunchRot = Hook.rotation;
            LaunchVec = (Vector3)SAVControl.GetProgramVariable("CurrentVel") + HookLaunchPoint.forward * HookSpeed;
            LaunchSpeed = LaunchVec.magnitude;
            Hook.parent = VehicleRB.transform.parent;
            Hook.position = HookLaunchPoint.position;
            if (!InstantHit) { HookFlyLoop(); }
            HookLaunch.Play();
        }
        public void HookFlyLoop()
        {
            if (!HookLaunched || HookAttached)
            {
                return;
            }
            RaycastHit hookhit;
            if (IsOwner)
            {
                if (Physics.Raycast(Hook.position, LaunchVec, out hookhit, LaunchSpeed * Time.deltaTime, HookLayers, QueryTriggerInteraction.Ignore))
                {
                    HookAttachPoint = hookhit.point;
                    RequestSerialization();
                    Hook.position = hookhit.point;
                    return;
                }
            }
            Hook.position += LaunchVec * Time.deltaTime;
            HookLength = Vector3.Distance(HookLaunchPoint.position, Hook.position);
            if (IsOwner && HookLength > HookRange)
            {
                HookLaunched = false;
                if (!Occupied) { SendCustomEventDelayedSeconds(nameof(DisableThis), 2f); }
                RequestSerialization();
                return;
            }
            if (Rope_Line)
            {
                Rope_Line.SetPosition(0, RopeBasePoint.position);
                Rope_Line.SetPosition(1, HookRopePoint.position);
            }
            SendCustomEventDelayedFrames(nameof(HookFlyLoop), 1);
        }
        private void UndoHookOverrides()
        {
            if (Overriding_DisallowOwnerShipTransfer)
            {
                HookedEntity.SetProgramVariable("DisallowOwnerShipTransfer", (int)HookedEntity.GetProgramVariable("DisallowOwnerShipTransfer") - 1);
                Overriding_DisallowOwnerShipTransfer = false;
            }
        }
        public void ResetHook()
        {
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
            Rope_Line.gameObject.SetActive(false);
            Hook.parent = HookParentStart;
            HookAttached = false;
            NonLocalAttached = false;
            NonLocalAttached_Pilot = false;
            Hook.localPosition = HookStartPos;
            Hook.localRotation = HookStartRot;
            if (HookedEntity)
            {
                UndoHookOverrides();
                if (HookedEntity.Using)
                {
                    Networking.SetOwner(Networking.LocalPlayer, HookedEntity.gameObject);
                }
                HookedEntity = null;
            }
            if (PlayReelIn) { HookReelIn.Play(); }
        }
        public void UpdateRopeLine()
        {
            if (Rope_Line)
            {
                Rope_Line.SetPosition(0, RopeBasePoint.position);
                Rope_Line.SetPosition(1, HookRopePoint.position);
            }
        }
        private void FixedUpdate()
        {
            if (HookAttached && IsOwner)
            {
                if ((HookedCollider && !HookedCollider.enabled)
                 || !HookedTransform.gameObject.activeInHierarchy
                 || (HookedEntity && (HookedEntity.dead || (!HookedEntity.IsOwner && TwoWayForces && !TwoWayForces_LocalForceMode))))
                {
                    HookLaunched = false;
                    RequestSerialization(); return;
                }
                Hook.position = _HookAttachPoint = HookedTransform.TransformPoint(HookedTransformOffset);

                float dist = Vector3.Distance(HookLaunchPoint.position, _HookAttachPoint);
                float PullReduction = 0f;

                float SwingForce = dist - HookLength;
                if (SwingForce < 0)
                {
                    PullReduction = SwingForce;
                    SwingForce = 0;
                }
                else { SwingForce *= SwingStrength; }
                HookLength = Mathf.Min(dist, HookRange);

                Vector3 forceDirection = (_HookAttachPoint - HookLaunchPoint.position).normalized;
                float WeightRatio = 1;

                if (HookedRB)
                {
                    WeightRatio = HookedRB.mass / (HookedRB.mass + VehicleRB.mass);
                    Vector3 forceDirection_HookedRB = (HookLaunchPoint.position - _HookAttachPoint).normalized;
                    HookedRB.AddForceAtPosition((forceDirection_HookedRB * HookStrength * PullStrOverDist.Evaluate(dist) * Time.deltaTime + (forceDirection_HookedRB * SwingForce) + (forceDirection_HookedRB * PullReduction * PullReductionStrength)) * (1f - WeightRatio), _HookAttachPoint, ForceMode.VelocityChange);
                }
                if (UseForceApplyPoint)
                {
                    VehicleRB.AddForceAtPosition((forceDirection * HookStrength * PullStrOverDist.Evaluate(dist) * Time.deltaTime + (forceDirection * SwingForce) + (forceDirection * PullReduction * PullReductionStrength)) * WeightRatio, ForceApplyPoint.position, ForceMode.VelocityChange);
                }
                else
                {
                    VehicleRB.AddForce((forceDirection * HookStrength * PullStrOverDist.Evaluate(dist) * Time.deltaTime + (forceDirection * SwingForce) + (forceDirection * PullReduction * PullReductionStrength)) * WeightRatio, ForceMode.VelocityChange);
                }
            }
            else if (NonLocalAttached)
            {
                if (HookedRB)
                {
                    float dist = Vector3.Distance(HookLaunchPoint.position, _HookAttachPoint);
                    float PullReduction = 0f;

                    float SwingForce = dist - HookLength;
                    if (SwingForce < 0)
                    {
                        PullReduction = SwingForce;
                        SwingForce = 0;
                    }
                    else { SwingForce *= SwingStrength; }
                    HookLength = dist;

                    Vector3 forceDirection = (_HookAttachPoint - HookLaunchPoint.position).normalized;

                    float WeightRatio = HookedRB.mass / (HookedRB.mass + VehicleRB.mass);
                    Vector3 forceDirection_HookedRB = (HookLaunchPoint.position - _HookAttachPoint).normalized;
                    HookedRB.AddForceAtPosition((forceDirection_HookedRB * HookStrength * PullStrOverDist.Evaluate(dist) * Time.deltaTime + (forceDirection_HookedRB * SwingForce) + (forceDirection_HookedRB * PullReduction * PullReductionStrength)) * (1f - WeightRatio), _HookAttachPoint, ForceMode.VelocityChange);
                }
            }
        }
        public void SFEXT_G_Explode()
        {
            if (IsOwner)
            {
                if (_HookLaunched)
                { HookLaunched = false; RequestSerialization(); }
            }
            //make sure this happens because the one in the HookLaunched Set may not be reliable because synced variables are faster than events
            SendCustomEventDelayedSeconds(nameof(DisableThis), 2f);
        }
        public void DisableThis() { if (!Occupied) { gameObject.SetActive(false); } }
        public void SFEXT_O_PilotExit()
        {
            Selected = false;
            if (!InVr && !KeyboardSelectMode) { foreach (GameObject obj in EnableOnSelect) { obj.SetActive(false); } }
        }
        public void SFEXT_O_TakeOwnership()
        {
            IsOwner = true;
        }
        public void SFEXT_O_LoseOwnership()
        {
            IsOwner = false;
        }
        public void SFEXT_O_PilotEnter()
        {
            if (!InVr && !KeyboardSelectMode) { foreach (GameObject obj in EnableOnSelect) { obj.SetActive(true); } }
        }
        public void SFEXT_G_PilotEnter()
        {
            Occupied = true;
            gameObject.SetActive(true);
        }
        public void SFEXT_O_RespawnButton()
        {
            PlayReelIn = false;
            HookLaunched = false;
            PlayReelIn = true;
            RequestSerialization();
            SendCustomEventDelayedSeconds(nameof(DisableThis), 2f);
        }
        public void SFEXT_G_PilotExit()
        {
            Occupied = false;
            if (!_HookLaunched)
            { SendCustomEventDelayedSeconds(nameof(DisableThis), 2f); }
        }
        public void KeyboardInput()
        {
            if (KeyboardSelectMode)
            {
                if (LeftDial)
                {
                    if (EntityControl.LStickSelection == DialPosition)
                    { EntityControl.LStickSelection = -1; }
                    else
                    { EntityControl.LStickSelection = DialPosition; }
                }
                else
                {
                    if (EntityControl.RStickSelection == DialPosition)
                    { EntityControl.RStickSelection = -1; }
                    else
                    { EntityControl.RStickSelection = DialPosition; }
                }
            }
            else
            {
                FireHook();
            }
        }
        public void FireHook()
        {
            HookLaunched = !HookLaunched;
            RequestSerialization();
        }
        private void FindSelf()
        {
            int x = 0;
            foreach (UdonSharpBehaviour usb in EntityControl.Dial_Functions_R)
            {
                if (this == usb)
                {
                    DialPosition = x;
                    return;
                }
                x++;
            }
            LeftDial = true;
            x = 0;
            foreach (UdonSharpBehaviour usb in EntityControl.Dial_Functions_L)
            {
                if (this == usb)
                {
                    DialPosition = x;
                    return;
                }
                x++;
            }
            DialPosition = -999;
            Debug.LogWarning("DFUNC_AAM: Can't find self in dial functions");
        }
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        private bool TriggerLastFrame;
        private bool Selected;
        private bool UseLeftTrigger;
        public void DFUNC_Selected()
        {
            Selected = true;
            foreach (GameObject obj in EnableOnSelect) { obj.SetActive(true); }
        }
        public void DFUNC_Deselected()
        {
            Selected = false;
            foreach (GameObject obj in EnableOnSelect) { obj.SetActive(false); }
        }
        private void Update()
        {
            if (Selected)
            {
                float Trigger;
                if (UseLeftTrigger)
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                else
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
                if (Trigger > 0.75 || Input.GetKey(KeyCode.Space))
                {
                    if (!TriggerLastFrame)
                    {
                        FireHook();
                    }
                    TriggerLastFrame = true;
                }
                else { TriggerLastFrame = false; }
            }
        }
    }
}