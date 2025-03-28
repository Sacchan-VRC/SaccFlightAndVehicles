using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_AAM : UdonSharpBehaviour
    {
        [SerializeField] public UdonSharpBehaviour SAVControl;
        public Animator AAMAnimator;
        [Tooltip("Desktop key for firing when selected")]
        public KeyCode FireKey = KeyCode.Space;
        [Tooltip("If on a pickup: Use VRChat's OnPickupUseDown functionality")]
        [SerializeField] bool use_OnPickupUseDown = false;
        [Range(0, 2)]
        [Tooltip("0 = Radar, 1 = Heat, 2 = Other. Controls what variable is added to in SaccAirVehicle to count incoming missiles, AND which variable to check for reduced tracking, (MissilesIncomingRadar NumActiveChaff, MissilesIncomingHeat NumActiveFlares, MissilesIncomingOther NumActiveOtherCM)")]
        public int MissileType = 1;
        public int NumAAM = 6;
        [Tooltip("If target is within this angle of the direction the gun is aiming, it is lockable")]
        public float AAMLockAngle = 15;
        [Tooltip("AAM takes this long to lock before it can fire (seconds)")]
        public float AAMLockTime = 1.5f;
        [Tooltip("Heatseekers only: How much faster is locking if the target has afterburner on? (AAMLockTime / value)")]
        public float LockTimeABDivide = 2f;
        [Tooltip("Heatseekers only: If target's engine throttle is 0%, what is the minimum number to divide lock time by, to prevent infinite lock time. (AAMLockTime / value)")]
        public float LockTimeMinDivide = .2f;
        [Tooltip("Minimum time between missile launches")]
        public float AAMLaunchDelay = 0;
        [Tooltip("How long it takes to fully reload from empty in seconds. Can be inaccurate because it can only reload by integers per resupply")]
        public float FullReloadTimeSec = 10;
        [Tooltip("Make enemy aircraft's animator set the 'targeted' trigger?")]
        public bool SendLockWarning = true;
        [Tooltip("Allow user to fire the weapon while the vehicle is on the ground taxiing?")]
        public bool AllowFiringWhenGrounded = false;
        [Tooltip("Disable the weapon if wind is enabled, to prevent people gaining an unfair advantage")]
        public bool DisallowFireIfWind = false;
        public bool AAMInheritVelocity = true;
        [Tooltip("Allow locking on target with no missiles left. Enable if creating FOX-1/3 missiles, otherwise your last missile will be unusable.")]
        public bool AllowNoAmmoLock = false;
        [Tooltip("Require re-lock after firing?")]
        public bool LoseLockWhenFired = false;
        [Tooltip("GameObject that is enabled by the missile script for 1 second when the missile enters pitbull mode to let the pilot know he no longer has to track the target. Use if creating FOX-3 missiles.")]
        public GameObject PitBullIndicator;
        [Tooltip("Send the boolean(AnimBoolName) true to the animator when selected?")]
        public bool DoAnimBool = false;
        [Tooltip("Animator bool that is true when this function is selected")]
        public string AnimBoolName = "AAMSelected";
        [Tooltip("Animator float that represents how many missiles are left")]
        public string AnimFloatName = "AAMs";
        [Tooltip("Animator trigger that is set when a missile is launched")]
        public string AnimFiredTriggerName = "aamlaunched";
        [Tooltip("Set animator bool with this name to true if missile has a lock")]
        public string AnimLockedOnBoolName = string.Empty;
        [Tooltip("Should the boolean stay true if the pilot exits with it selected?")]
        public bool AnimBoolStayTrueOnExit;
        [Tooltip("Make it only possible to lock if the angle you are looking at the back of the enemy plane is less than HighAspectPreventLock (for heatseekers)")]
        public bool HighAspectPreventLock;
        [Tooltip("Angle beyond which aspect is too high to lock")]
        public float HighAspectAngle = 85;
        [Tooltip("Object that is cloned and fired at the enemy")]
        public GameObject AAM;
        public Transform AAMLaunchPoint;
        [Tooltip("Sound that plays when missile is fired")]
        public AudioSource AAMLaunchSound;
        [Tooltip("Sound that plays when missile is selected and has no target")]
        public AudioSource AAMIdle;
        [Tooltip("Sound that plays when missile is has a target but no lock")]
        public AudioSource AAMTargeting;
        [Tooltip("Sound that plays when missile has a lock on a target")]
        public AudioSource AAMTargetLock;
        [Tooltip("Fired AAMs will be parented to this object, use if you happen to have some kind of moving origin system")]
        public Transform WorldParent;
        [Tooltip("If not empty, targeting will be done relative to this transform's forward")]
        public Transform TargetingTransform;
        private float HighAspectPreventLockAngleDot;
        [UdonSynced] private bool AAMFireNow;
        [UdonSynced] private bool SendTargeted;
        private float SendTargeted_Time;
        const float SENDTARGETED_INTERVAL = 1;
        private float AAMTargetedTime = 2;
        private float boolToggleTime;
        private bool AnimOn = false;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [System.NonSerializedAttribute] public SAV_PassengerFunctionsController PassengerFunctionsControl;
        [System.NonSerializedAttribute] public int FullAAMs;
        private int NumAAMTargets;
        private float AAMLockTimer = 0;
        private bool AAMHasTarget = false;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(AAMLocked)), UdonSynced] public bool _AAMLocked = false;
        public bool AAMLocked
        {
            set
            {
                _AAMLocked = value;
                if (DoLockedBool && AAMAnimator)
                {
                    AAMAnimator.SetBool(AnimLockedOnBoolName, value);
                }
            }
            get => _AAMLocked;
        }
        private bool DoLockedBool = false;
        private bool TriggerLastFrame;
        private float AAMLastFiredTime = -999;
        private float FullAAMsDivider;
        private bool func_active = false;
        private bool Pilot = false;
        [System.NonSerializedAttribute] public bool IsOwner;
        [System.NonSerializedAttribute] public bool InEditor;
        private float reloadspeed;
        private bool DoAnimFiredTrigger;
        private int NumChildrenStart;
        private VRCPlayerApi localPlayer;
        public void SFEXT_L_EntityStart()
        {
            FullAAMs = NumAAM;
            reloadspeed = FullAAMs / FullReloadTimeSec;
            FullAAMsDivider = 1f / (NumAAM > 0 ? NumAAM : 10000000);
            AAMTargets = EntityControl.AAMTargets;
            NumAAMTargets = AAMTargets.Length;
            CenterOfMass = (Transform)EntityControl.CenterOfMass;
            VehicleTransform = EntityControl.transform;
            VehicleRigid = EntityControl.GetComponent<Rigidbody>();
            OutsideVehicleLayer = EntityControl.OutsideVehicleLayer;
            localPlayer = Networking.LocalPlayer;
            InEditor = localPlayer == null;
            HighAspectPreventLockAngleDot = Mathf.Cos(HighAspectAngle * Mathf.Deg2Rad);
            IsOwner = EntityControl.IsOwner;
            if (AnimFiredTriggerName != string.Empty) { DoAnimFiredTrigger = true; }

            if (LockTimeABDivide <= 0)
            { LockTimeABDivide = 0.0001f; }
            if (LockTimeMinDivide <= 0)
            { LockTimeMinDivide = 0.0001f; }


            //HUD
            if (HUDControl)
            {
                distance_from_head = (float)HUDControl.GetProgramVariable("distance_from_head");
            }
            else
            { distance_from_head = 1.333f; }

            if (HUDText_AAM_ammo) { HUDText_AAM_ammo.text = NumAAM.ToString("F0"); }
            if (HUDText_AAM_ammo_TMP) { HUDText_AAM_ammo_TMP.text = NumAAM.ToString("F0"); }
            if (HUDText_AAM_ammo_TMPUGUI) { HUDText_AAM_ammo_TMPUGUI.text = NumAAM.ToString("F0"); }

            NumChildrenStart = transform.childCount;
            if (AAM)
            {
                int NumToInstantiate = Mathf.Min(FullAAMs, 10);
                for (int i = 0; i < NumToInstantiate; i++)
                {
                    InstantiateWeapon();
                }
            }
            if (!TargetingTransform)
            { TargetingTransform = VehicleTransform; }
            if (AnimLockedOnBoolName != string.Empty) { DoLockedBool = true; }
        }
        public void ReInitNumMissiles()//set FullAAMs then run this to change vehicles max AAMs
        {
            NumAAM = FullAAMs;
            reloadspeed = FullAAMs / FullReloadTimeSec;
            FullAAMsDivider = 1f / (NumAAM > 0 ? NumAAM : 10000000);
            if (HUDText_AAM_ammo) { HUDText_AAM_ammo.text = NumAAM.ToString("F0"); }
        }
        private GameObject InstantiateWeapon()
        {
            GameObject NewWeap = Object.Instantiate(AAM);
            NewWeap.transform.SetParent(transform);
            return NewWeap;
        }
        public void SFEXT_O_PilotEnter()
        {
            Pilot = true;
            if (HUDText_AAM_ammo) { HUDText_AAM_ammo.text = NumAAM.ToString("F0"); }
            //Make sure SAVeControl.AAMCurrentTargetSAVControl is correct
            var Target = AAMTargets[AAMTarget];
            if (Target && Target.transform.parent)
            {
                AAMCurrentTargetSAVControl = Target.transform.parent.GetComponent<SaccAirVehicle>();
            }
        }
        public void SFEXT_G_PilotEnter()
        {
            OnEnableDeserializationBlocker = true;
            SendCustomEventDelayedFrames(nameof(FireDisablerFalse), 10);
            gameObject.SetActive(true);
        }
        public void FireDisablerFalse() { OnEnableDeserializationBlocker = false; }
        public void SFEXT_G_PilotExit()
        {
            gameObject.SetActive(false);
            AAMLocked = false;
            PickupTrigger = 0;
        }
        public void SFEXT_O_PilotExit()
        {
            Pilot = false;
            AAMLockTimer = 0;
            AAMHasTarget = false;
            AAMLocked = false;
            func_active = false;
            if (AAMIdle) { AAMIdle.gameObject.SetActive(false); }
            if (AAMTargeting) { AAMTargeting.gameObject.SetActive(false); }
            if (AAMTargetLock) { AAMTargetLock.gameObject.SetActive(false); }
            if (AAMTargetIndicator)
            {
                AAMTargetIndicator.gameObject.SetActive(false);
                AAMTargetIndicator.localRotation = Quaternion.identity;
            }
        }

        public void SFEXT_P_PassengerEnter()
        {
            if (HUDText_AAM_ammo) { HUDText_AAM_ammo.text = NumAAM.ToString("F0"); }
        }
        public void SFEXT_G_Explode()
        {
            NumAAM = FullAAMs;
            UpdateAmmoVisuals();
            if (func_active)
            {
                DFUNC_Deselected();
            }
            if (DoAnimBool && AnimOn)
            { SetBoolOff(); }
        }
        public void SFEXT_G_ReSupply()
        {
            if (NumAAM != FullAAMs)
            { EntityControl.SetProgramVariable("ReSupplied", (int)EntityControl.GetProgramVariable("ReSupplied") + 1); }
            NumAAM = (int)Mathf.Min(NumAAM + Mathf.Max(Mathf.Floor(reloadspeed), 1), FullAAMs);
            UpdateAmmoVisuals();
        }
        public void SFEXT_G_ReArm() { SFEXT_G_ReSupply(); }
        public void UpdateAmmoVisuals()
        {
            if (AAMAnimator) { AAMAnimator.SetFloat(AnimFloatName, (float)NumAAM * FullAAMsDivider); }
            if (HUDText_AAM_ammo) { HUDText_AAM_ammo.text = NumAAM.ToString("F0"); }
        }
        public void SFEXT_G_RespawnButton()
        {
            NumAAM = FullAAMs;
            UpdateAmmoVisuals();
            if (DoAnimBool && AnimOn)
            { SetBoolOff(); }
        }
        public void SFEXT_G_TouchDown()
        {
            AAMLockTimer = 0;
        }
        public void SFEXT_O_OnPickup()
        {
            SFEXT_O_PilotEnter();
        }
        public void SFEXT_O_OnDrop()
        {
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
        public void SFEXT_O_TakeOwnership() { IsOwner = true; }
        public void SFEXT_O_LoseOwnership() { IsOwner = false; }
        public void DFUNC_Selected()
        {
            func_active = true;
            if (AAMTargetIndicator) { AAMTargetIndicator.gameObject.SetActive(true); }

            if (DoAnimBool && !AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOn)); }
        }
        public void DFUNC_Deselected()
        {
            TriggerLastFrame = true;
            if (AAMIdle) { AAMIdle.gameObject.SetActive(false); }
            if (AAMTargeting) { AAMTargeting.gameObject.SetActive(false); }
            if (AAMTargetLock) { AAMTargetLock.gameObject.SetActive(false); }
            PickupTrigger = 0;
            AAMLockTimer = 0;
            AAMHasTarget = false;
            AAMLocked = false;
            RequestSerialization();
            if (AAMTargetIndicator)
            {
                AAMTargetIndicator.localRotation = Quaternion.identity;
                AAMTargetIndicator.localScale = Vector3.one;
                AAMTargetIndicator.gameObject.SetActive(false);
            }
            func_active = false;

            if (DoAnimBool && AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOff)); }
        }
        void Update()
        {
            if (func_active)
            {
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
                bool lockedLast = AAMLocked;
                if (NumAAMTargets != 0)
                {
                    if (MissileType == 1)//heatseekers check engine output of target
                    {
                        if (AAMCurrentTargetSAVControl ?//if target is SaccAirVehicle, adjust lock time based on throttle status 
                        AAMLockTimer > AAMLockTime /
                        (AAMCurrentTargetSAVControl.AfterburnerOn ?
                                    LockTimeABDivide
                                    :
                                    Mathf.Clamp(AAMCurrentTargetSAVControl.EngineOutput / AAMCurrentTargetSAVControl.ThrottleAfterburnerPoint, LockTimeMinDivide, 1))
                        : AAMLockTimer > AAMLockTime && AAMHasTarget)//target is not a SaccAirVehicle
                        { AAMLocked = true; }
                        else { AAMLocked = false; }
                    }
                    else
                    {
                        if (AAMLockTimer > AAMLockTime && AAMHasTarget) { AAMLocked = true; }
                        else { AAMLocked = false; }
                    }
                    if (lockedLast != AAMLocked) { RequestSerialization(); }

                    //firing AAM
                    if (Trigger > 0.75 || Input.GetKey(FireKey))
                    {
                        if (!TriggerLastFrame)
                        {
                            if (DisallowFireIfWind && SAVControl)
                            {
                                if (((Vector3)SAVControl.GetProgramVariable("FinalWind")).sqrMagnitude > 0f)
                                { return; }
                            }
                            LaunchAAM_Owner();
                        }
                        TriggerLastFrame = true;
                    }
                    else TriggerLastFrame = false;
                }


                //sound
                if (!AAMLocked && AAMLockTimer > 0)
                {
                    if (AAMIdle) { AAMIdle.gameObject.SetActive(false); }
                    if (AAMTargeting && (NumAAM > 0 || AllowNoAmmoLock)) { AAMTargeting.gameObject.SetActive(true); }
                    if (AAMTargetLock) { AAMTargetLock.gameObject.SetActive(false); }
                }
                else if (AAMLocked)
                {
                    if (AAMIdle) { AAMIdle.gameObject.SetActive(false); }
                    if (AAMTargeting) { AAMTargeting.gameObject.SetActive(false); }
                    if (AAMTargetLock) { AAMTargetLock.gameObject.SetActive(true); }
                }
                else
                {
                    if (AAMIdle && (NumAAM > 0 || AllowNoAmmoLock)) { AAMIdle.gameObject.SetActive(true); }
                    if (AAMTargeting) { AAMTargeting.gameObject.SetActive(false); }
                    if (AAMTargetLock) { AAMTargetLock.gameObject.SetActive(false); }
                }
                Hud();
            }
        }

        //AAMTargeting
        public UdonSharpBehaviour HUDControl;
        [Tooltip("Max distance an enemy can be targeted at")]
        public float AAMMaxTargetDistance = 6000;
        [System.NonSerializedAttribute] public GameObject[] AAMTargets;
        [System.NonSerializedAttribute, UdonSynced(UdonSyncMode.None)] public int AAMTarget = 0;
        private int AAMTargetChecker = 0;
        [System.NonSerializedAttribute] public Transform CenterOfMass;
        private Transform VehicleTransform;
        private Rigidbody VehicleRigid;
        private SaccAirVehicle AAMCurrentTargetSAVControl;
        private int OutsideVehicleLayer;
        private Vector3 AAMCurrentTargetDirection;
        private float AAMTargetObscuredDelay;
        //public Transform TARGETDEBUG;
        /* everywhere that GetComponent<SaccAirVehicle>() is used should be changed to UdonSharpBehaviour for modularity's sake,
        but it seems that it's impossible until further udon/sharp updates, because it currently doesn't support checking if a variable exists before trying to get it */
        private void FixedUpdate()//old AAMTargeting function
        {
            if (func_active)
            {
                //TARGETDEBUG.position = AAMTargets[AAMTarget].transform.position;
                float DeltaTime = Time.fixedDeltaTime;
                var AAMCurrentTargetPosition = AAMTargets[AAMTarget].transform.position;
                Vector3 HudControlPosition = HUDControl ? HUDControl.transform.position : localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
                float AAMCurrentTargetAngle = Vector3.Angle(TargetingTransform.forward, (AAMCurrentTargetPosition - HudControlPosition));

                //check 1 target per frame to see if it's infront of us and worthy of being our current target
                var TargetChecker = AAMTargets[AAMTargetChecker];
                var TargetCheckerTransform = TargetChecker.transform;
                var TargetCheckerParent = TargetCheckerTransform.parent;

                Vector3 AAMNextTargetDirection = (TargetCheckerTransform.position - HudControlPosition);
                float NextTargetAngle = Vector3.Angle(TargetingTransform.forward, AAMNextTargetDirection);
                float NextTargetDistance = Vector3.Distance(CenterOfMass.position, TargetCheckerTransform.position);

                if (TargetChecker.activeInHierarchy)
                {
                    SaccAirVehicle NextTargetSAVControl = null;

                    if (TargetCheckerParent)
                    {
                        NextTargetSAVControl = TargetCheckerParent.GetComponent<SaccAirVehicle>();
                    }
                    //if target SAVontroller is null then it's a dummy target (or hierarchy isn't set up properly)
                    if ((!NextTargetSAVControl || (!NextTargetSAVControl.Taxiing && !NextTargetSAVControl.EntityControl._dead)))
                    {
                        RaycastHit hitnext;
                        //raycast to check if it's behind something
                        bool LineOfSightNext = Physics.Raycast(HudControlPosition, AAMNextTargetDirection, out hitnext, Mathf.Infinity, 133137 /* Default, Water, Environment, and Walkthrough */, QueryTriggerInteraction.Collide);
#if UNITY_EDITOR
                        if (hitnext.collider)
                            Debug.DrawLine(HudControlPosition, hitnext.point, Color.red);
                        else
                            Debug.DrawRay(HudControlPosition, AAMNextTargetDirection, Color.yellow);
#endif
                        /*                         Debug.Log(string.Concat("LoS_next ", LineOfSightNext));
                                                if (hitnext.collider)
                                                {
                                                    Debug.Log(string.Concat("RayCastCorrectLayer_next ", (hitnext.collider.gameObject.layer == OutsideVehicleLayer)));
                                                    Debug.Log(string.Concat("RayCastLayer_next ", hitnext.collider.gameObject.layer));
                                                }
                                                Debug.Log(string.Concat("LowerAngle_next ", NextTargetAngle < AAMCurrentTargetAngle));
                                                Debug.Log(string.Concat("InAngle_next ", NextTargetAngle < 70));
                                                Debug.Log(string.Concat("BelowMaxDist_next ", NextTargetDistance < AAMMaxTargetDistance)); */

                        if (LineOfSightNext
                            && hitnext.collider && hitnext.collider.gameObject.layer == OutsideVehicleLayer //did raycast hit an object on the layer planes are on?
                                && NextTargetAngle < AAMLockAngle
                                    && NextTargetAngle < AAMCurrentTargetAngle
                                        && NextTargetDistance < AAMMaxTargetDistance
                                            && (!NextTargetSAVControl ||//null check
                                                ((!HighAspectPreventLock || Vector3.Dot(NextTargetSAVControl.VehicleTransform.forward, AAMNextTargetDirection.normalized) > HighAspectPreventLockAngleDot)
                                                && (MissileType != 1 || NextTargetSAVControl._EngineOn)))
                                            || (AAMCurrentTargetSAVControl &&//null check
                                                                        (AAMCurrentTargetSAVControl.Taxiing ||//switch target if current target is taxiing
                                                                        (MissileType == 1 && !AAMCurrentTargetSAVControl._EngineOn && !AAMCurrentTargetSAVControl.EntityControl.wrecked)))//switch target if heatseeker and current target's engine is off unless target is wrecked(on fire)
                                                || !AAMTargets[AAMTarget].activeInHierarchy//switch target if current target is destroyed
                                                )
                        {
                            if (!TriggerLastFrame)
                            {
                                //found new target
                                AAMCurrentTargetAngle = NextTargetAngle;
                                AAMTarget = AAMTargetChecker;
                                AAMCurrentTargetPosition = AAMTargets[AAMTarget].transform.position;
                                AAMCurrentTargetSAVControl = NextTargetSAVControl;
                                AAMLockTimer = 0;
                            }
                        }
                    }
                }
                //increase target checker ready for next frame
                AAMTargetChecker++;
                if (AAMTargetChecker == AAMTarget && AAMTarget == NumAAMTargets - 1)
                { AAMTargetChecker = 0; }
                else if (AAMTargetChecker == AAMTarget)
                { AAMTargetChecker++; }
                else if (AAMTargetChecker > NumAAMTargets - 1)
                { AAMTargetChecker = 0; }

                //if target is currently in front of plane, lock onto it

                AAMCurrentTargetDirection = AAMCurrentTargetPosition - HudControlPosition;
                float AAMCurrentTargetDistance = AAMCurrentTargetDirection.magnitude;
                //check if target is active, and if it's SaccAirVehicle is null(dummy target), or if it's not null(plane) make sure it's not taxiing or dead.
                //raycast to check if it's behind something
                RaycastHit hitcurrent;
                bool LineOfSightCur = Physics.Raycast(HudControlPosition, AAMCurrentTargetDirection, out hitcurrent, Mathf.Infinity, 133137 /* Default, Water, Environment, and Walkthrough */, QueryTriggerInteraction.Collide);
#if UNITY_EDITOR
                if (hitcurrent.collider)
                    Debug.DrawLine(HudControlPosition, hitcurrent.point, Color.green);
                else
                    Debug.DrawRay(HudControlPosition, AAMNextTargetDirection, Color.blue);
#endif
                //used to make lock remain for .25 seconds after target is obscured
                if (!LineOfSightCur || (hitcurrent.collider && hitcurrent.collider.gameObject.layer != OutsideVehicleLayer))
                { AAMTargetObscuredDelay += DeltaTime; }
                else
                { AAMTargetObscuredDelay = 0; }

                if (
                    (!SAVControl || !(bool)SAVControl.GetProgramVariable("Taxiing") || AllowFiringWhenGrounded)
                    && (AAMTargetObscuredDelay < .25f)
                        && AAMCurrentTargetDistance < AAMMaxTargetDistance
                            && AAMTargets[AAMTarget].activeInHierarchy
                                && (!AAMCurrentTargetSAVControl ||
                                    (!AAMCurrentTargetSAVControl.Taxiing && !AAMCurrentTargetSAVControl.EntityControl._dead &&
                                        (MissileType != 1 || (AAMCurrentTargetSAVControl._EngineOn || AAMCurrentTargetSAVControl.EntityControl.wrecked))))//heatseekers cant lock if engine off unless wrecked (on fire)
                                    &&
                                        (!HighAspectPreventLock || !AAMCurrentTargetSAVControl || Vector3.Dot(AAMCurrentTargetSAVControl.VehicleTransform.forward, AAMCurrentTargetDirection.normalized) > HighAspectPreventLockAngleDot)
                                        )
                {
                    if ((AAMTargetObscuredDelay < .25f) && AAMCurrentTargetDistance < AAMMaxTargetDistance)
                    {
                        AAMHasTarget = true;
                        if (AAMCurrentTargetAngle < AAMLockAngle && (NumAAM > 0 || AllowNoAmmoLock))
                        {
                            AAMLockTimer += DeltaTime;
                            if (AAMCurrentTargetSAVControl)
                            {
                                //target is a plane, send the 'targeted' event every second to make the target plane play a warning sound in the cockpit.
                                if (SendLockWarning && Time.time - AAMTargetedTime > SENDTARGETED_INTERVAL)
                                {
                                    SendTargeted_Time = Time.time;
                                    RequestSerialization();
                                    AAMTargetedTime = Time.time;
                                }
                            }
                        }
                        else
                        {
                            AAMTargetedTime = 0;
                            AAMLockTimer = 0;
                        }
                    }
                }
                else
                {
                    AAMTargetedTime = 0f;
                    AAMLockTimer = 0;
                    AAMHasTarget = false;
                }
                /*                 Debug.Log(string.Concat("AAMTarget ", AAMTarget));
                                Debug.Log(string.Concat("HasTarget ", AAMHasTarget));
                                Debug.Log(string.Concat("AAMTargetObscuredDelay ", AAMTargetObscuredDelay));
                                Debug.Log(string.Concat("LoS ", LineOfSightCur));
                                if (hitcurrent.collider)
                                {
                                    Debug.Log(string.Concat("RayCastCorrectLayer ", (hitcurrent.collider.gameObject.layer == OutsideVehicleLayer)));
                                    Debug.Log(string.Concat("RayCastLayer ", hitcurrent.collider.gameObject.layer));
                                }
                                Debug.Log(string.Concat("NotObscured ", AAMTargetObscuredDelay < .25f));
                                Debug.Log(string.Concat("InAngle ", AAMCurrentTargetAngle < AAMLockAngle));
                                Debug.Log(string.Concat("BelowMaxDist ", AAMCurrentTargetDistance < AAMMaxTargetDistance)); */
            }
        }


        //hud stuff
        public Text HUDText_AAM_ammo;
        public TextMeshPro HUDText_AAM_ammo_TMP;
        public TextMeshProUGUI HUDText_AAM_ammo_TMPUGUI;
        [Tooltip("Hud element to highlight current target")]
        public Transform AAMTargetIndicator;
        private float distance_from_head;
        private void Hud()
        {
            //AAM Target Indicator
            if (AAMTargetIndicator)
            {
                if (AAMHasTarget)
                {
                    AAMTargetIndicator.localScale = Vector3.one;
                    AAMTargetIndicator.position = (HUDControl ? HUDControl.transform.position : localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position) + AAMCurrentTargetDirection;
                    AAMTargetIndicator.localPosition = AAMTargetIndicator.localPosition.normalized * distance_from_head;
                    Vector3 HudControlPosition = HUDControl ? HUDControl.transform.position : localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
                    if (AAMLocked)
                    {
                        AAMTargetIndicator.rotation = Quaternion.LookRotation(-(AAMTargetIndicator.position - HudControlPosition), VehicleTransform.transform.up);//This makes it not stretch when off to the side by fixing the rotation.
                    }
                    else
                    {
                        AAMTargetIndicator.rotation = Quaternion.LookRotation(AAMTargetIndicator.position - HudControlPosition, VehicleTransform.transform.up);//This makes it not stretch when off to the side by fixing the rotation.
                    }
                }
                else AAMTargetIndicator.localScale = Vector3.zero;
            }
            /////////////////
        }
        public void LaunchAAM()
        {
            if (AAMLaunchSound) { AAMLaunchSound.PlayOneShot(AAMLaunchSound.clip); }
            AAMLastFiredTime = Time.time;
            if (NumAAM > 0) { NumAAM--; }//so it doesn't go below 0 when desync occurs
            if (AAMAnimator && DoAnimFiredTrigger) { AAMAnimator.SetTrigger(AnimFiredTriggerName); }
            if (AAM)
            {
                GameObject NewAAM;
                if (transform.childCount - NumChildrenStart > 0)
                { NewAAM = transform.GetChild(NumChildrenStart).gameObject; }
                else
                { NewAAM = InstantiateWeapon(); }
                if (WorldParent) { NewAAM.transform.SetParent(WorldParent); }
                else { NewAAM.transform.SetParent(null); }
                NewAAM.transform.SetPositionAndRotation(AAMLaunchPoint.position, AAMLaunchPoint.transform.rotation);
                Rigidbody AAMRB = NewAAM.GetComponent<Rigidbody>();
                if (AAMRB && SAVControl)
                {
                    if (EntityControl.IsOwner && IsOwner)// these can be different for passenger functions      
                    {
                        //set launch position relative to rigidbody instead of transform so the physics matches
                        Vector3 LocalLaunchPoint = EntityControl.transform.InverseTransformDirection(NewAAM.transform.position - EntityControl.transform.position);
                        AAMRB.position = (VehicleRigid.rotation * LocalLaunchPoint) + VehicleRigid.position;
                        Quaternion WeaponRotDif = NewAAM.transform.rotation * Quaternion.Inverse(VehicleRigid.rotation);
                        AAMRB.rotation = WeaponRotDif * VehicleRigid.rotation;
                    }
                    else
                    {
                        AAMRB.position = NewAAM.transform.position + (Vector3)SAVControl.GetProgramVariable("CurrentVel") * Time.deltaTime;
                        AAMRB.rotation = NewAAM.transform.rotation;
                    }
                }
                NewAAM.SetActive(true);
                if (AAMInheritVelocity)
                {
                    if (AAMRB)
                    {
                        if (SAVControl)
                        { AAMRB.velocity = (Vector3)SAVControl.GetProgramVariable("CurrentVel"); }
                        else
                        { AAMRB.velocity = VehicleRigid.velocity; }
                    }
                }
                UdonSharpBehaviour USB = NewAAM.GetComponent<UdonSharpBehaviour>();
                if (USB)
                { USB.SendCustomEvent("EnableWeapon"); }
            }
            UpdateAmmoVisuals();
        }
        public void SetBoolOn()
        {
            boolToggleTime = Time.time;
            AnimOn = true;
            if (AAMAnimator) { AAMAnimator.SetBool(AnimBoolName, AnimOn); }
        }
        public void SetBoolOff()
        {
            boolToggleTime = Time.time;
            AnimOn = false;
            if (AAMAnimator) { AAMAnimator.SetBool(AnimBoolName, AnimOn); }
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
        private void LaunchAAM_Owner()
        {
            if (NumAAM > 0 && AAMLocked && Time.time - AAMLastFiredTime > AAMLaunchDelay)
            {
                FireNextSerialization = true;
                RequestSerialization();
                LaunchAAM();
                if (LoseLockWhenFired || (NumAAM == 0 && !AllowNoAmmoLock)) { AAMLockTimer = 0; AAMLocked = false; RequestSerialization(); }
                EntityControl.SendEventToExtensions("SFEXT_O_AAMLaunch");
            }
        }
        private bool FireNextSerialization = false;
        public override void OnPreSerialization()
        {
            if (FireNextSerialization)
            {
                FireNextSerialization = false;
                AAMFireNow = true;
            }
            else { AAMFireNow = false; }
            if (Time.time - SendTargeted_Time < SENDTARGETED_INTERVAL)
            { SendTargeted = true; }
            else { SendTargeted = false; }
        }
        bool OnEnableDeserializationBlocker;
        public override void OnDeserialization()
        {
            if (OnEnableDeserializationBlocker) { return; }
            if (AAMFireNow) { LaunchAAM(); }
            if (SendTargeted)
            {
                if (!Pilot)
                {
                    var Target = AAMTargets[AAMTarget];
                    if (Target && Target.transform.parent)
                    {
                        AAMCurrentTargetSAVControl = Target.transform.parent.GetComponent<SaccAirVehicle>();
                    }
                    if (AAMCurrentTargetSAVControl != null && AAMCurrentTargetSAVControl.EntityControl.InVehicle)
                    { AAMCurrentTargetSAVControl.EntityControl.SendEventToExtensions("SFEXT_L_AAMTargeted"); }
                }
            }
        }
    }
}