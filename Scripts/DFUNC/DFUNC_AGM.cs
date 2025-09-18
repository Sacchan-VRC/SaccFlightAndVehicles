
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using VRC.SDK3.UdonNetworkCalling;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DFUNC_AGM : UdonSharpBehaviour
    {
        [Tooltip("NOT required if 'Hand Held Mode' is enabled")]
        [SerializeField] public UdonSharpBehaviour SAVControl;
        public Animator AGMAnimator;
        [Tooltip("Desktop key for firing when selected")]
        public KeyCode FireKey = KeyCode.Space;
        [Tooltip("Camera script that is used to see the target")]
        public GameObject AGM;
        public int NumAGM = 4;
        public Text HUDText_AGM_ammo;
        public TextMeshPro HUDText_AGM_ammo_TMP;
        public TextMeshProUGUI HUDText_AGM_ammo_TMPUGUI;
        [Tooltip("Camera that renders onto the AtGScreen")]
        public Camera AtGCam;
        [Tooltip("Screen that displays target, that is enabled when selected")]
        public GameObject AtGScreen;
        [Tooltip("Lower = slower response")]
        public float CamTurnSmoothness = 100f;
        public float CamZoomScale = 100f;
        public float CamZoomScale_Locked = 60f;
        public GameObject Dial_Funcon;
        [Tooltip("How long it takes to fully reload from empty in seconds. Can be inaccurate because it can only reload by integers per resupply")]
        public float FullReloadTimeSec = 8;
        [Tooltip("Sound that plays when the AGM locks")]
        public AudioSource AGMLock;
        [Tooltip("Sound that plays when the AGM unlocks")]
        public AudioSource AGMUnlock;
        public LayerMask LockableLayermask = -2147350527; // Default, Environment, Walkthrough, OnBoardVehicleLayer
        [Tooltip("Allow user to fire the weapon while the vehicle is on the ground taxiing?")]
        public bool AllowFiringWhenGrounded = false;
        [Tooltip("Disable the weapon if wind is enabled, to prevent people gaining an unfair advantage")]
        public bool DisallowFireIfWind = false;
        public bool AGMInheritVelocity = true;
        [Tooltip("Send the boolean(AnimBoolName) true to the animator when selected?")]
        public bool DoAnimBool = false;
        [Tooltip("Animator bool that is true when this function is selected")]
        public string AnimBoolName = "AGMSelected";
        [Tooltip("Animator float that represents how many missiles are left")]
        public string AnimFloatName = "AGMs";
        [Tooltip("Animator trigger that is set when a missile is launched")]
        public string AnimFiredTriggerName = "agmlaunched";
        [Tooltip("Should the boolean stay true if the pilot exits with it selected?")]
        public bool AnimBoolStayTrueOnExit;
        [Tooltip("Fired AGMs will be parented to this object, use if you happen to have some kind of moving origin system")]
        public Transform AGMLaunchPoint;
        public LayerMask AGMTargetsLayer = 1 << 26;
        [Tooltip("If not empty, targeting will be done relative to this transform's forward")]
        public Transform TargetingTransform;
        public Transform WorldParent;
        [Tooltip("If on a pickup: Use VRChat's OnPickupUseDown functionality")]
        [SerializeField] bool use_OnPickupUseDown = false;
        private float boolToggleTime;
        private bool AnimOn = false;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [System.NonSerializedAttribute] public SAV_PassengerFunctionsController PassengerFunctionsControl;
        [System.NonSerializedAttribute] public bool AGMLocked;
        [System.NonSerializedAttribute] public bool IsOwner;
        [System.NonSerializedAttribute] public Transform TrackedTransform;
        [System.NonSerializedAttribute] public Vector3 TrackedObjectOffset;
        private int AGMUnlocking = 0;
        private float AGMUnlockTimer;
        private float AGMRotDif;
        private bool TriggerLastFrame;
        private float TriggerTapTime;
        [System.NonSerializedAttribute] public int FullAGMs;
        private float FullAGMsDivider;
        private int NumChildrenStart;
        // public Transform Debug_LockPointRaw, Debug_TransformLock;
        [System.NonSerializedAttribute] public Vector3 AGMTarget;

        [NetworkCallable]
        public void SetTarget(Vector3 inputLocation)
        {
            // if (Debug_LockPointRaw) Debug_LockPointRaw.position = value;
            RaycastHit[] hits = Physics.SphereCastAll(inputLocation, 150, Vector3.up, 0, LockableLayermask, QueryTriggerInteraction.Ignore);
            float NearestDist = float.MaxValue;
            if (hits.Length > 0)
            {
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider)
                    {
                        MeshCollider mc = hit.collider.GetComponent<MeshCollider>();
                        float tempdist;
                        if (mc && !mc.convex)
                            tempdist = Vector3.Distance(hit.collider.transform.position, inputLocation);
                        else
                            tempdist = Vector3.Distance(hit.collider.ClosestPoint(inputLocation), inputLocation);
                        if (tempdist < NearestDist)
                        {
                            NearestDist = tempdist;
                            TrackedTransform = hit.collider.transform;
                            TrackedObjectOffset = TrackedTransform.InverseTransformPoint(hit.collider.ClosestPoint(inputLocation));
                        }
                    }
                }
            }
            else
            {//extreme lag/late joiners if object targeted was in air/sea/not near anything (or if trying to lock terrain apparently)
                TrackedTransform = transform.root;//hopefully a non-moving object
                TrackedObjectOffset = TrackedTransform.InverseTransformPoint(inputLocation);
            }
            AGMTarget = inputLocation;
        }
        private VRCPlayerApi localPlayer;
        private bool InVR;
        private bool InEditor;
        private Transform VehicleTransform;
        private Rigidbody VehicleRigid;
        private Quaternion AGMCamRotSlerper;
        private Quaternion AGMCamRotLastFrame;
        private bool func_active;
        private bool DoAnimFiredTrigger = false;
        private float reloadspeed;
        private bool Piloting;
        public void SFEXT_L_EntityStart()
        {
            TrackedTransform = transform;//avoid null
            FullAGMs = NumAGM;
            reloadspeed = FullAGMs / FullReloadTimeSec;
            FullAGMsDivider = 1f / (NumAGM > 0 ? NumAGM : 10000000);
            localPlayer = Networking.LocalPlayer;
            InEditor = localPlayer == null;
            if (AnimFiredTriggerName != string.Empty) { DoAnimFiredTrigger = true; }
            IsOwner = EntityControl.IsOwner;
            VehicleTransform = EntityControl.transform;
            VehicleRigid = EntityControl.GetComponent<Rigidbody>();
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
            if (AtGScreen) AtGScreen.SetActive(false);
            InVR = EntityControl.InVR;

            UpdateAmmoVisuals();

            NumChildrenStart = transform.childCount;
            if (AGM)
            {
                int NumToInstantiate = Mathf.Min(FullAGMs, 10);
                for (int i = 0; i < NumToInstantiate; i++)
                {
                    InstantiateWeapon();
                }
            }
        }
        public void ReInitNumMissiles()//set FullAGMs then run this to change vehicles max AGMs
        {
            NumAGM = FullAGMs;
            reloadspeed = FullAGMs / FullReloadTimeSec;
            FullAGMsDivider = 1f / (NumAGM > 0 ? NumAGM : 10000000);
            UpdateAmmoVisuals();
        }
        private GameObject InstantiateWeapon()
        {
            GameObject NewWeap = Object.Instantiate(AGM);
            NewWeap.transform.SetParent(transform);
            return NewWeap;
        }
        public void SFEXT_O_PilotEnter()
        {
            TriggerLastFrame = true;
            AGMLocked = false;
            Piloting = true;
            InVR = EntityControl.InVR;
            UpdateAmmoVisuals();
        }
        public void SFEXT_P_PassengerEnter()
        {
            UpdateAmmoVisuals();
        }
        public void SFEXT_G_PilotEnter()
        {
            gameObject.SetActive(true);
        }
        public void SFEXT_G_PilotExit()
        {
            gameObject.SetActive(false);
            if (DoAnimBool && !AnimBoolStayTrueOnExit && AnimOn)
            { SetBoolOff(); }
        }
        public void SFEXT_O_PilotExit()
        {
            AGMLocked = false;
            if (AtGScreen) AtGScreen.SetActive(false);
            AtGCam.gameObject.SetActive(false);
            gameObject.SetActive(false);
            func_active = false;
            Piloting = false;
            PickupTrigger = 0;
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
        }
        public void SFEXT_G_RespawnButton()
        {
            NumAGM = FullAGMs;
            UpdateAmmoVisuals();
            if (DoAnimBool && AnimOn)
            { SetBoolOff(); }
        }
        public void SFEXT_G_ReSupply()
        {
            if (NumAGM != FullAGMs)
            {
                if (SAVControl)
                { EntityControl.SetProgramVariable("ReSupplied", (int)EntityControl.GetProgramVariable("ReSupplied") + 1); }
            }
            NumAGM = (int)Mathf.Min(NumAGM + Mathf.Max(Mathf.Floor(reloadspeed), 1), FullAGMs);
            UpdateAmmoVisuals();
        }
        public void SFEXT_G_ReArm() { SFEXT_G_ReSupply(); }
        public void UpdateAmmoVisuals()
        {
            if (AGMAnimator) { AGMAnimator.SetFloat(AnimFloatName, (float)NumAGM * FullAGMsDivider); }
            if (HUDText_AGM_ammo) { HUDText_AGM_ammo.text = NumAGM.ToString("F0"); }
            if (HUDText_AGM_ammo_TMP) { HUDText_AGM_ammo_TMP.text = NumAGM.ToString("F0"); }
            if (HUDText_AGM_ammo_TMPUGUI) { HUDText_AGM_ammo_TMPUGUI.text = NumAGM.ToString("F0"); }
        }
        public void SFEXT_G_Explode()
        {
            NumAGM = FullAGMs;
            UpdateAmmoVisuals();
            if (func_active)
            { DFUNC_Deselected(); }
            if (DoAnimBool && AnimOn)
            { SetBoolOff(); }
        }
        public void SFEXT_O_TakeOwnership() { IsOwner = true; }
        public void SFEXT_O_LoseOwnership() { IsOwner = false; }
        public void DFUNC_Selected()
        {
            TriggerLastFrame = true;
            func_active = true;
            if (AtGScreen) AtGScreen.SetActive(true);
            AtGCam.gameObject.SetActive(true);
            if (DoAnimBool && !AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOn)); }
        }
        public void DFUNC_Deselected()
        {
            func_active = false;
            PickupTrigger = 0;
            if (AtGScreen) AtGScreen.SetActive(false);
            AtGCam.gameObject.SetActive(false);
            if (DoAnimBool && AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOff)); }
            AGMUnlockTimer = 0;
            AGMUnlocking = 0;
        }
        private void RaycastLock()
        {
            RaycastHit lockpoint;
            int layerm = LockableLayermask;
            layerm &= ~(1 << EntityControl.OnboardVehicleLayer);// remove your own vehicle from the raycast layers
            if (Physics.Raycast(AtGCam.transform.position, AtGCam.transform.forward, out lockpoint, Mathf.Infinity, layerm, QueryTriggerInteraction.Ignore))
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetTarget), lockpoint.point);
                AGMLocked = true;
                AGMUnlocking = 0;
                RequestSerialization();
                if (Dial_Funcon) { Dial_Funcon.SetActive(true); }
            }
        }
        private void Update()
        {
            // if (Debug_TransformLock) Debug_TransformLock.position = TrackedTransform.TransformPoint(TrackedObjectOffset);
            if (func_active)
            {
                float DeltaTime = Time.deltaTime;
                TriggerTapTime += DeltaTime;
                AGMUnlockTimer += DeltaTime * AGMUnlocking;//AGMUnlocking is 1 if it was locked and just pressed, else 0, (waits for double tap delay to disable)
                float Trigger;
                if (use_OnPickupUseDown)
                    Trigger = PickupTrigger;
                else
                {
                    if (LeftDial)
                    { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                    else
                    { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
                }
                if (AGMUnlockTimer > 0.4f && AGMLocked)
                {
                    //disable for others because they no longer need to sync
                    AGMLocked = false;
                    AGMUnlockTimer = 0;
                    AGMUnlocking = 0;
                    if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
                    if (AGMUnlock)
                    { AGMUnlock.Play(); }
                }
                if (Trigger > 0.75 || Input.GetKey(FireKey))
                {
                    if (!TriggerLastFrame)
                    {//new button press
                        if (TriggerTapTime < 0.4f)
                        {//double tap detected
                            if (AGMLocked)
                            {//locked on, launch missile
                                if (NumAGM > 0 && (AllowFiringWhenGrounded || !SAVControl || !(bool)SAVControl.GetProgramVariable("Taxiing")))
                                {
                                    if (DisallowFireIfWind)
                                    {
                                        if (SAVControl && ((Vector3)SAVControl.GetProgramVariable("FinalWind")).sqrMagnitude > 0f)
                                        { return; }
                                    }
                                    LaunchAGM_Owner();
                                    TriggerTapTime += 0.4f;//dont count every tap after first double tap as another double tap
                                    if (IsOwner)
                                    { EntityControl.SendEventToExtensions("SFEXT_O_AGMLaunch"); }
                                }
                                AGMUnlocking = 0;
                            }
                        }
                        else if (!AGMLocked)
                        {//lock onto a target

                            //check for agmtargets to lock to
                            float targetangle = 999;
                            float spherecastLength = Mathf.Infinity;
                            RaycastHit hit;
                            if (Physics.Raycast(AtGCam.transform.position, AtGCam.transform.forward, out hit, Mathf.Infinity, 2065 /* Default, Water and Environment */))
                            {
                                spherecastLength = hit.distance;
                            }
                            RaycastHit[] agmtargs = Physics.SphereCastAll(AtGCam.transform.position, 150, AtGCam.transform.forward, spherecastLength, AGMTargetsLayer);
                            if (agmtargs.Length > 0)
                            {//found one or more, find lowest angle one
                             //find target with lowest angle from crosshair
                                foreach (RaycastHit target in agmtargs)
                                {
                                    Vector3 targetdirection = target.point - AtGCam.transform.position;
                                    float angle = Vector3.Angle(AtGCam.transform.forward, targetdirection);
                                    if (angle < targetangle)
                                    {
                                        targetangle = angle;
                                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetTarget), target.collider.transform.position);
                                    }
                                }
                                //the spherecastall should really be a cone but this works for now
                                if (targetangle > 5)
                                { RaycastLock(); }
                                else
                                {
                                    AGMLocked = true;
                                    AGMUnlocking = 0;
                                    if (Dial_Funcon) { Dial_Funcon.SetActive(true); }
                                    RequestSerialization();
                                }
                                if (AGMLocked && AGMLock)
                                { AGMLock.Play(); }
                            }
                            else
                            {//didn't find one, lock onto raycast point
                                RaycastLock();
                                if (AGMLocked && AGMLock)
                                { AGMLock.Play(); }
                            }
                        }
                        else
                        {
                            TriggerTapTime = 0;
                            AGMUnlockTimer = 0;
                            AGMUnlocking = 1;
                        }
                    }
                    TriggerLastFrame = true;
                }
                else { TriggerLastFrame = false; }
                if (!AGMLocked)
                {
                    Quaternion newangle;

                    if (TargetingTransform)
                    {
                        newangle = TargetingTransform.rotation;
                    }
                    else if (EntityControl.Holding)
                    {
                        newangle = VehicleTransform.rotation;
                    }
                    else if (InVR)
                    {
                        if (LeftDial)
                        { newangle = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation * Quaternion.Euler(0, 60, 0); }
                        else
                        { newangle = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation * Quaternion.Euler(0, 60, 0); }
                    }
                    else if (!InEditor)//desktop mode
                    {
                        newangle = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
                    }
                    else//editor
                    {
                        newangle = VehicleTransform.rotation;
                    }

                    float ZoomLevel = AtGCam.fieldOfView / 90;
                    AGMCamRotSlerper = Quaternion.Slerp(AGMCamRotSlerper, newangle, 1 - Mathf.Pow(0.5f, ZoomLevel * CamTurnSmoothness * DeltaTime));

                    AGMRotDif = Vector3.Angle(AtGCam.transform.rotation * Vector3.forward, AGMCamRotLastFrame * Vector3.forward);
                    AtGCam.transform.rotation = AGMCamRotSlerper;

                    Vector3 temp2 = AtGCam.transform.localRotation.eulerAngles;
                    temp2.z = 0;
                    AtGCam.transform.localRotation = Quaternion.Euler(temp2);
                    AGMCamRotLastFrame = newangle;
                }


                //AGMScreen
                float deltaTime = Time.deltaTime;
                if (!AGMLocked)
                {
                    //if turning camera fast, zoom out
                    if (AGMRotDif < 2.5f)
                    {
                        RaycastHit camhit;
                        Physics.Raycast(AtGCam.transform.position, AtGCam.transform.forward, out camhit, Mathf.Infinity, 1);
                        //dolly zoom //Mathf.Atan(100 <--the 100 is the height of the camera frustrum at the target distance
                        float newzoom = Mathf.Clamp(2.0f * Mathf.Atan(CamZoomScale * 0.5f / Vector3.Distance(gameObject.transform.position, camhit.point)) * Mathf.Rad2Deg, 1.5f, 90);
                        AtGCam.fieldOfView = Mathf.Clamp(Mathf.Lerp(AtGCam.fieldOfView, newzoom, 1 - Mathf.Pow(0.5f, 3f * deltaTime)), 0.3f, 90);
                    }
                    else
                    {
                        float newzoom = 80;
                        AtGCam.fieldOfView = Mathf.Clamp(Mathf.Lerp(AtGCam.fieldOfView, newzoom, 1 - Mathf.Pow(0.5f, 10f * deltaTime)), 0.3f, 90); //zooming in is a bit slower than zooming out                       
                    }
                }
                else
                {
                    AtGCam.transform.LookAt(TrackedTransform.TransformPoint(TrackedObjectOffset), EntityControl.transform.up);
                    RaycastHit camhit;
                    Physics.Raycast(AtGCam.transform.position, AtGCam.transform.forward, out camhit, Mathf.Infinity, 1);
                    //dolly zoom //Mathf.Atan(60 <--the 60 is the height of the camera frustrum at the target distance
                    AtGCam.fieldOfView = Mathf.Max(Mathf.Lerp(AtGCam.fieldOfView, 2.0f * Mathf.Atan(60 * 0.5f / Vector3.Distance(gameObject.transform.position, camhit.point)) * Mathf.Rad2Deg, 1 - Mathf.Pow(0.5f, 10 * deltaTime)), 0.3f);
                }
            }
        }
        [NetworkCallable]
        public void LaunchAGMs_Event()
        {
            LaunchAGM();
        }
        void LaunchAGM()
        {
            if (AGMAnimator && DoAnimFiredTrigger) { AGMAnimator.SetTrigger(AnimFiredTriggerName); }
            if (NumAGM > 0) { NumAGM--; }
            if (AGM)
            {
                GameObject NewAGM;
                if (transform.childCount - NumChildrenStart > 0)
                { NewAGM = transform.GetChild(NumChildrenStart).gameObject; }
                else
                { NewAGM = InstantiateWeapon(); }
                if (WorldParent) { NewAGM.transform.SetParent(WorldParent); }
                else { NewAGM.transform.SetParent(null); }
                NewAGM.transform.SetPositionAndRotation(AGMLaunchPoint.position, AGMLaunchPoint.rotation);
                Rigidbody AGMRB = NewAGM.GetComponent<Rigidbody>();
                if (AGMRB)
                {
                    if (EntityControl.IsOwner && IsOwner)// these can be different for passenger functions      
                    {
                        //set launch position relative to rigidbody instead of transform so the physics matches
                        Vector3 LocalLaunchPoint = EntityControl.transform.InverseTransformDirection(NewAGM.transform.position - EntityControl.transform.position);
                        AGMRB.position = (VehicleRigid.rotation * LocalLaunchPoint) + VehicleRigid.position;
                        Quaternion WeaponRotDif = NewAGM.transform.rotation * Quaternion.Inverse(VehicleRigid.rotation);
                        AGMRB.rotation = WeaponRotDif * VehicleRigid.rotation;
                    }
                    else
                    {
                        AGMRB.position = NewAGM.transform.position;
                        AGMRB.rotation = NewAGM.transform.rotation;
                    }
                }
                NewAGM.SetActive(true);
                if (AGMInheritVelocity)
                {
                    if (AGMRB)
                    {
                        if (SAVControl)
                        { AGMRB.velocity = (Vector3)SAVControl.GetProgramVariable("CurrentVel"); }
                        else
                        { AGMRB.velocity = VehicleRigid.velocity; }
                    }
                }
                UdonSharpBehaviour USB = NewAGM.GetComponent<UdonSharpBehaviour>();
                if (USB)
                { USB.SendCustomEvent("EnableWeapon"); }
            }
            if (AGMAnimator)
            {
                AGMAnimator.SetTrigger(AnimFiredTriggerName);
                AGMAnimator.SetFloat(AnimFloatName, (float)NumAGM * FullAGMsDivider);
            }
            UpdateAmmoVisuals();
        }
        public void SetBoolOn()
        {
            boolToggleTime = Time.time;
            AnimOn = true;
            if (AGMAnimator) { AGMAnimator.SetBool(AnimBoolName, AnimOn); }
        }
        public void SetBoolOff()
        {
            boolToggleTime = Time.time;
            AnimOn = false;
            if (AGMAnimator) { AGMAnimator.SetBool(AnimBoolName, AnimOn); }
        }
        public void KeyboardInput()
        {
            if (PassengerFunctionsControl)
            {
                if (LeftDial) PassengerFunctionsControl.ToggleStickSelectionLeft(this);
                else PassengerFunctionsControl.ToggleStickSelectionRight(this);
            }
            else
            {
                if (LeftDial) EntityControl.ToggleStickSelectionLeft(this);
                else EntityControl.ToggleStickSelectionRight(this);
            }
        }
        public void SFEXT_O_OnDrop()
        {
            DFUNC_Deselected();
            SFEXT_O_PilotExit();
        }
        public void SFEXT_G_OnPickup() { SFEXT_G_PilotEnter(); }
        public void SFEXT_G_OnDrop() { SFEXT_G_PilotExit(); }
        private float PickupTrigger;
        public void SFEXT_O_OnPickupUseDown()
        {
            PickupTrigger = 1;
        }
        public void SFEXT_O_OnPickupUseUp()
        {
            PickupTrigger = 0;
        }
        [NetworkCallable]
        public void LaunchAGM_Owner()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(LaunchAGMs_Event));
        }
        public void UpdateLaterJoiner(Vector3 inputLocation, int playerID)
        {
            if (!Utilities.IsValid(localPlayer)) return;
            if (localPlayer.playerId != playerID) return;
            SetTarget(inputLocation);
        }
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (!Piloting) return;
            int playerID = player.playerId;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(UpdateLaterJoiner), TrackedTransform.TransformPoint(TrackedObjectOffset), playerID);
        }
    }
}