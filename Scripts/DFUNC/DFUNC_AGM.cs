
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_AGM : UdonSharpBehaviour
    {
        [Tooltip("NOT required if 'Hand Held Mode' is enabled")]
        [SerializeField] public UdonSharpBehaviour SAVControl;
        public Animator AGMAnimator;
        [Tooltip("Camera script that is used to see the target")]
        public GameObject AGM;
        public int NumAGM = 4;
        public Text HUDText_AGM_ammo;
        [Tooltip("Camera that renders onto the AtGScreen")]
        public Camera AtGCam;
        [Tooltip("Screen that displays target, that is enabled when selected")]
        public GameObject AtGScreen;
        public float CamZoomScale = 100f;
        public float CamZoomScale_Locked = 60f;
        public GameObject Dial_Funcon;
        [Tooltip("How long it takes to fully reload from empty in seconds. Can be inaccurate because it can only reload by integers per resupply")]
        public float FullReloadTimeSec = 8;
        [Tooltip("Sound that plays when the AGM locks")]
        public AudioSource AGMLock;
        [Tooltip("Sound that plays when the AGM unlocks")]
        public AudioSource AGMUnlock;
        public LayerMask LockableLayers = 133121;
        [Tooltip("Allow user to fire the weapon while the vehicle is on the ground taxiing?")]
        public bool AllowFiringWhenGrounded = false;
        [Tooltip("Disable the weapon if wind is enabled, to prevent people gaining an unfair advantage")]
        public bool DisallowFireIfWind = false;
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
        public Transform WorldParent;
        [SerializeField] private bool HandHeldMode = false;
        [SerializeField] private SaccEntity _EntityControl;
        [UdonSynced, FieldChangeCallback(nameof(AGMFire))] private ushort _AGMFire;
        public ushort AGMFire
        {
            set
            {
                if (value > _AGMFire)//if _AGMFire is higher locally, it's because a late joiner just took ownership or value was reset, so don't launch
                { LaunchAGM(); }
                _AGMFire = value;
            }
            get => _AGMFire;
        }
        private float boolToggleTime;
        private bool AnimOn = false;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        private bool UseLeftTrigger = false;
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
        [System.NonSerializedAttribute, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(AGMTarget))] public Vector3 _AGMTarget;
        public Vector3 AGMTarget
        {
            set
            {
                RaycastHit[] hits = Physics.SphereCastAll(value, 150, Vector3.up, 0, LockableLayers, QueryTriggerInteraction.Ignore);
                float NearestDist = float.MaxValue;
                if (hits.Length > 0)
                {
                    foreach (RaycastHit hit in hits)
                    {
                        if (hit.collider)
                        {
                            float tempdist = Vector3.Distance(hit.collider.ClosestPoint(value), value);
                            if (tempdist < NearestDist)
                            {
                                NearestDist = tempdist;
                                TrackedTransform = hit.collider.transform;
                                TrackedObjectOffset = TrackedTransform.InverseTransformPoint(hit.collider.ClosestPoint(value));
                            }
                        }
                    }
                }
                else
                {//extreme lag/late joiners if object targeted was in air/sea/not near anything (or if trying to lock terrain apparently)
                    TrackedTransform = transform.root;//hopefully a non-moving object
                    TrackedObjectOffset = TrackedTransform.InverseTransformPoint(value);
                }
                _AGMTarget = value;
            }
            get => _AGMTarget;
        }
        private VRCPlayerApi localPlayer;
        private bool InVR;
        private bool InEditor;
        private Transform VehicleTransform;
        private Quaternion AGMCamRotSlerper;
        private Quaternion AGMCamRotLastFrame;
        private bool func_active;
        private bool DoAnimFiredTrigger = false;
        private float reloadspeed;
        private bool LeftDial = false;
        private int DialPosition = -999;
        private bool OthersEnabled;
        private bool Piloting;
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        public void SFEXT_L_EntityStart()
        {
            TrackedTransform = transform;//avoid null
            FullAGMs = NumAGM;
            reloadspeed = FullAGMs / FullReloadTimeSec;
            FullAGMsDivider = 1f / (NumAGM > 0 ? NumAGM : 10000000);
            localPlayer = Networking.LocalPlayer;
            InEditor = localPlayer == null;
            if (AnimFiredTriggerName != string.Empty) { DoAnimFiredTrigger = true; }
            if (HandHeldMode)
            {
                EntityControl = _EntityControl;
                IsOwner = _EntityControl.IsOwner;
            }
            else
            {
                EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
                IsOwner = (bool)SAVControl.GetProgramVariable("IsOwner");
            }
            VehicleTransform = EntityControl.transform;
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }

            FindSelf();

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
            if (HUDText_AGM_ammo) { HUDText_AGM_ammo.text = NumAGM.ToString("F0"); }
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
            if (!InEditor) { InVR = localPlayer.IsUserInVR(); }
            if (HUDText_AGM_ammo) { HUDText_AGM_ammo.text = NumAGM.ToString("F0"); }
        }
        public void SFEXT_P_PassengerEnter()
        {
            if (HUDText_AGM_ammo) { HUDText_AGM_ammo.text = NumAGM.ToString("F0"); }
        }
        public void SFEXT_G_PilotExit()
        {
            if (OthersEnabled) { DisableForOthers(); }
            if (DoAnimBool && !AnimBoolStayTrueOnExit && AnimOn)
            { SetBoolOff(); }
        }
        public void SFEXT_O_PilotExit()
        {
            AGMLocked = false;
            AtGScreen.SetActive(false);
            AtGCam.gameObject.SetActive(false);
            gameObject.SetActive(false);
            func_active = false;
            Piloting = false;
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
                { SAVControl.SetProgramVariable("ReSupplied", (int)SAVControl.GetProgramVariable("ReSupplied") + 1); }
            }
            NumAGM = (int)Mathf.Min(NumAGM + Mathf.Max(Mathf.Floor(reloadspeed), 1), FullAGMs);
            UpdateAmmoVisuals();
        }
        public void UpdateAmmoVisuals()
        {
            if (AGMAnimator) { AGMAnimator.SetFloat(AnimFloatName, (float)NumAGM * FullAGMsDivider); }
            if (HUDText_AGM_ammo) { HUDText_AGM_ammo.text = NumAGM.ToString("F0"); }
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
            gameObject.SetActive(true);
            AtGScreen.SetActive(true);
            AtGCam.gameObject.SetActive(true);
            if (DoAnimBool && !AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOn)); }
            if (!OthersEnabled) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EnableForOthers)); }
        }
        public void DFUNC_Deselected()
        {
            func_active = false;
            AtGScreen.SetActive(false);
            AtGCam.gameObject.SetActive(false);
            gameObject.SetActive(false);
            if (DoAnimBool && AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOff)); }
            if (OthersEnabled) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(DisableForOthers)); }
            AGMUnlockTimer = 0;
            AGMUnlocking = 0;
        }
        public void EnableForOthers()
        {
            if (!Piloting)
            { gameObject.SetActive(true); AGMFire = 0; }
            OthersEnabled = true;
        }
        public void DisableForOthers()
        {
            if (!Piloting)
            { gameObject.SetActive(false); }
            OthersEnabled = false;
        }
        private void RaycastLock()
        {
            RaycastHit lockpoint;
            if (Physics.Raycast(AtGCam.transform.position, AtGCam.transform.forward, out lockpoint, Mathf.Infinity, LockableLayers, QueryTriggerInteraction.Ignore))
            {
                AGMTarget = lockpoint.point;
                AGMLocked = true;
                AGMUnlocking = 0;
                RequestSerialization();
                if (Dial_Funcon) { Dial_Funcon.SetActive(true); }
            }
        }
        private void Update()
        {
            if (func_active)
            {
                float DeltaTime = Time.deltaTime;
                TriggerTapTime += DeltaTime;
                AGMUnlockTimer += DeltaTime * AGMUnlocking;//AGMUnlocking is 1 if it was locked and just pressed, else 0, (waits for double tap delay to disable)
                float Trigger;
                if (!HandHeldMode)
                {
                    if (UseLeftTrigger)
                    { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                    else
                    { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
                }
                else { Trigger = UseTrigger; }
                if (AGMUnlockTimer > 0.4f && AGMLocked == true)
                {
                    //disable for others because they no longer need to sync
                    AGMLocked = false;
                    AGMUnlockTimer = 0;
                    AGMUnlocking = 0;
                    if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
                    if (AGMUnlock)
                    { AGMUnlock.Play(); }
                }
                if (Trigger > 0.75 || (!HandHeldMode && Input.GetKey(KeyCode.Space)))
                {
                    if (!TriggerLastFrame)
                    {//new button press
                        if (TriggerTapTime < 0.4f)
                        {//double tap detected
                            if (AGMLocked)
                            {//locked on, launch missile
                                if (NumAGM > 0 && (HandHeldMode || (AllowFiringWhenGrounded || !(bool)SAVControl.GetProgramVariable("Taxiing"))))
                                {
                                    if (DisallowFireIfWind && !HandHeldMode)
                                    {
                                        if (((Vector3)SAVControl.GetProgramVariable("FinalWind")).magnitude > 0f)
                                        { return; }
                                    }
                                    AGMFire++;//launch AGM using set
                                    RequestSerialization();
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
                            RaycastHit[] agmtargs = Physics.SphereCastAll(AtGCam.transform.position, 150, AtGCam.transform.forward, Mathf.Infinity, AGMTargetsLayer);
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
                                        AGMTarget = target.collider.transform.position;
                                    }
                                }
                                //the spherecastall should really be a cone but this works for now
                                if (targetangle > 20)
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

                    if (HandHeldMode)
                    {
                        newangle = VehicleTransform.rotation;
                    }
                    else
                    {
                        if (InVR)
                        {
                            if (UseLeftTrigger)
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
                    }
                    float ZoomLevel = AtGCam.fieldOfView / 90;
                    AGMCamRotSlerper = Quaternion.Slerp(AGMCamRotSlerper, newangle, ZoomLevel * 220f * DeltaTime);

                    if (AtGCam)
                    {
                        AGMRotDif = Vector3.Angle(AtGCam.transform.rotation * Vector3.forward, AGMCamRotLastFrame * Vector3.forward);
                        AtGCam.transform.rotation = AGMCamRotSlerper;

                        Vector3 temp2 = AtGCam.transform.localRotation.eulerAngles;
                        temp2.z = 0;
                        AtGCam.transform.localRotation = Quaternion.Euler(temp2);
                    }
                    AGMCamRotLastFrame = newangle;
                }


                //AGMScreen
                float SmoothDeltaTime = Time.smoothDeltaTime;
                if (!AGMLocked)
                {
                    //if turning camera fast, zoom out
                    if (AGMRotDif < 2.5f)
                    {
                        RaycastHit camhit;
                        Physics.Raycast(AtGCam.transform.position, AtGCam.transform.forward, out camhit, Mathf.Infinity, 1);
                        //dolly zoom //Mathf.Atan(100 <--the 100 is the height of the camera frustrum at the target distance
                        float newzoom = Mathf.Clamp(2.0f * Mathf.Atan(CamZoomScale * 0.5f / Vector3.Distance(gameObject.transform.position, camhit.point)) * Mathf.Rad2Deg, 1.5f, 90);
                        AtGCam.fieldOfView = Mathf.Clamp(Mathf.Lerp(AtGCam.fieldOfView, newzoom, 1.5f * SmoothDeltaTime), 0.3f, 90);
                    }
                    else
                    {
                        float newzoom = 80;
                        AtGCam.fieldOfView = Mathf.Clamp(Mathf.Lerp(AtGCam.fieldOfView, newzoom, 5f * SmoothDeltaTime), 0.3f, 90); //zooming in is a bit slower than zooming out                       
                    }
                }
                else
                {
                    AtGCam.transform.LookAt(TrackedTransform.TransformPoint(TrackedObjectOffset), EntityControl.transform.up);
                    RaycastHit camhit;
                    Physics.Raycast(AtGCam.transform.position, AtGCam.transform.forward, out camhit, Mathf.Infinity, 1);
                    //dolly zoom //Mathf.Atan(60 <--the 60 is the height of the camera frustrum at the target distance
                    AtGCam.fieldOfView = Mathf.Max(Mathf.Lerp(AtGCam.fieldOfView, 2.0f * Mathf.Atan(60 * 0.5f / Vector3.Distance(gameObject.transform.position, camhit.point)) * Mathf.Rad2Deg, 5 * SmoothDeltaTime), 0.3f);
                }
            }
        }

        public void LaunchAGM()
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
                NewAGM.SetActive(true);
                if (!HandHeldMode)
                { NewAGM.GetComponent<Rigidbody>().velocity = (Vector3)SAVControl.GetProgramVariable("CurrentVel"); }
            }
            if (AGMAnimator)
            {
                AGMAnimator.SetTrigger(AnimFiredTriggerName);
                AGMAnimator.SetFloat(AnimFloatName, (float)NumAGM * FullAGMsDivider);
            }
            if (HUDText_AGM_ammo) { HUDText_AGM_ammo.text = NumAGM.ToString("F0"); }
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
            Debug.LogWarning("DFUNC_AGM: Can't find self in dial functions");
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
        public void SFEXT_O_OnDrop()
        {
            DFUNC_Deselected();
            SFEXT_O_PilotExit();
        }
        private float UseTrigger;
        public void UseTrigZero() { UseTrigger = 0; }
        public void SFEXT_O_OnPickupUseDown()
        {
            UseTrigger = 1;
            SendCustomEventDelayedFrames(nameof(UseTrigZero), 1, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
        }
    }
}