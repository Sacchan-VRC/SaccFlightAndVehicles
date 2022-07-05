
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class StingerScript : UdonSharpBehaviour
    {
        public SaccEntity EntityControl;
        public Transform HUD;
        [Tooltip("0 = Radar, 1 = Heat, 2 = Other. Controls what variable is added to in SaccAirVehicle to count incoming missiles, AND which variable to check for reduced tracking, (MissilesIncomingHeat NumActiveFlares, MissilesIncomingRadar NumActiveChaff, MissilesIncomingOther NumActiveOtherCM)")]
        public int MissileType = 1;
        public float AAMMaxTargetDistance;
        public int VehicleLayer = 17;
        public Animator StingerAnimator;
        public GameObject AAM;
        public Transform AAMLaunchPoint;
        public AudioSource AAMTargeting;
        public int NumAAM = 6;
        [Tooltip("If target is within this angle of the direction the gun is aiming, it is lockable")]
        public float AAMLockAngle = 15;
        [Tooltip("AAM takes this long to lock before it can fire (seconds)")]
        public float AAMLockTime = 2.5f;
        [Tooltip("Heatseekers only: How much faster is locking if the target has afterburner on? (AAMLockTime / value)")]
        public float LockTimeABDivide = 2f;
        [Tooltip("Heatseekers only: If target's engine throttle is 0%, what is the minimum number to divide lock time by, to prevent infinite lock time. (AAMLockTime / value)")]
        public float LockTimeMinDivide = .2f;
        [Tooltip("Minimum time between missile launches")]
        public float AAMLaunchDelay = 0.5f;
        [Tooltip("Make enemy aircraft's animator set the 'targeted' trigger?")]
        public bool SendLockWarning = true;
        [Tooltip("How long it takes to fully reload from empty in seconds. Can be inaccurate because it can only reload by integers per resupply")]
        public float FullReloadTimeSec = 10;
        public AudioSource AAMTargetLock;
        [Tooltip("Animator trigger that is set true when a missile is launched")]
        public string AnimFiredTriggerName = "aamlaunched";
        [Tooltip("Animator float that represents how many missiles are left")]
        public string AnimFloatName = "AAMs";
        [UdonSynced, FieldChangeCallback(nameof(AAMFire))] private short _AAMFire;
        //hud stuff
        public Text HUDText_AAM_ammo;
        [Tooltip("Hud element to highlight current target")]
        public Transform AAMTargetIndicator;
        public AudioSource FireSound;
        [Tooltip("Require re-lock after firing?")]
        public bool LoseLockAfterShot = true;
        [Tooltip("Make it only possible to lock if the angle you are looking at the back of the enemy plane is less than HighAspectPreventLock (for heatseekers)")]
        public bool HighAspectPreventLock;
        [Tooltip("Angle beyond which aspect is too high to lock")]
        public float HighAspectAngle = 85;
        [Tooltip("Allow locking on target with no missiles left. Enable if creating FOX-1/3 missiles, otherwise your last missile will be unusable.")]
        public bool AllowNoAmmoLock = false;
        [Tooltip("GameObject that is enabled by the missile script for 1 second when the missile enters pitbull mode to let the pilot know he no longer has to track the target. Use if creating FOX-3 missiles.")]
        public GameObject PitBullIndicator;
        [Tooltip("Fired projectiles will be parented to this object, use if you happen to have some kind of moving origin system")]
        public Transform WorldParent;
        private float HighAspectPreventLockAngleDot;
        private bool TriggerLastFrame;
        private float distance_from_head = 1.333333f;
        private VRC.SDK3.Components.VRCObjectSync StingerObjectSync;
        private VRC_Pickup StingerPickup;
        private Collider[] StingerColliders;
        public short AAMFire
        {
            set
            {
                if (value > _AAMFire)//if _AAMFire is higher locally, it's because a late joiner just took ownership or value was reset, so don't launch
                { LaunchAAM(); }
                _AAMFire = value;
            }
            get => _AAMFire;
        }
        [UdonSynced, FieldChangeCallback(nameof(sendtargeted))] private bool _SendTargeted;
        public bool sendtargeted
        {
            set
            {
                if (!Holding)
                {
                    var Target = AAMTargets[AAMTarget];
                    if (Target && Target.transform.parent)
                    {
                        AAMCurrentTargetSAVControl = Target.transform.parent.GetComponent<SaccAirVehicle>();
                    }
                    if (AAMCurrentTargetSAVControl != null)
                    { AAMCurrentTargetSAVControl.EntityControl.SendEventToExtensions("SFEXT_L_AAMTargeted"); }
                }
                _SendTargeted = value;
            }
            get => _SendTargeted;
        }
        [System.NonSerializedAttribute] public Transform CenterOfMass;
        private Transform StingerTransform;
        [System.NonSerializedAttribute] public bool IsOwner = true;
        private bool Holding = false;
        private int FullAAMs;
        private int NumAAMTargets;
        [System.NonSerializedAttribute][UdonSynced(UdonSyncMode.None)] public int AAMTarget = 0;
        private int AAMTargetChecker;
        private float AAMLockTimer = 0;
        private bool AAMHasTarget = false;
        private bool AAMLocked = false;
        private bool InEditor = true;
        private float AAMLastFiredTime = 0;
        private float FullAAMsDivider;
        private SaccAirVehicle AAMCurrentTargetSAVControl;
        float TimeSinceSerialization;
        [System.NonSerializedAttribute] public GameObject[] AAMTargets;
        private float reloadspeed;
        private float AAMTargetedTimer;
        private float AAMTargetObscuredDelay;
        private Vector3 AAMCurrentTargetDirection;
        private VRCPlayerApi localPlayer;
        private Rigidbody StingerRigid;
        private bool active = false;
        private int NumChildrenStart;
        [System.NonSerializedAttribute] public Vector3 Spawnposition;
        [System.NonSerializedAttribute] public Quaternion Spawnrotation;
        public void SFEXT_L_EntityStart()
        {
            FullAAMs = NumAAM;
            reloadspeed = FullAAMs / FullReloadTimeSec;
            FullAAMsDivider = 1f / (NumAAM > 0 ? NumAAM : 10000000);
            AAMTargets = EntityControl.AAMTargets;
            NumAAMTargets = AAMTargets.Length;
            CenterOfMass = EntityControl.CenterOfMass;
            StingerTransform = EntityControl.transform;
            StingerColliders = EntityControl.gameObject.GetComponents<Collider>();
            localPlayer = Networking.LocalPlayer;
            InEditor = localPlayer == null;
            StingerRigid = EntityControl.GetComponent<Rigidbody>();
            if (HUD) { HUD.gameObject.SetActive(false); }
            if (!active) { gameObject.SetActive(false); }
            else { gameObject.SetActive(true); }
            StingerObjectSync = (VRC.SDK3.Components.VRCObjectSync)EntityControl.gameObject.GetComponent(typeof(VRC.SDK3.Components.VRCObjectSync));
            Spawnposition = StingerTransform.position;
            Spawnrotation = StingerTransform.rotation;
            StingerPickup = (VRC_Pickup)EntityControl.gameObject.GetComponent(typeof(VRC.SDK3.Components.VRCPickup));
            HighAspectPreventLockAngleDot = Mathf.Cos(HighAspectAngle * Mathf.Deg2Rad);

            NumChildrenStart = transform.childCount;
            if (AAM)
            {
                int NumToInstantiate = Mathf.Min(FullAAMs, 10);
                for (int i = 0; i < NumToInstantiate; i++)
                {
                    InstantiateWeapon();
                }
            }
        }
        private GameObject InstantiateWeapon()
        {
            GameObject NewWeap = VRCInstantiate(AAM);
            NewWeap.transform.SetParent(transform);
            return NewWeap;
        }
        public void SFEXT_O_OnPickup()
        {
            Holding = true;
            if (HUDText_AAM_ammo) { HUDText_AAM_ammo.text = NumAAM.ToString("F0"); }
            //Make sure SAVControl.AAMCurrentTargetSAVControl is correct
            var Target = AAMTargets[AAMTarget];
            if (Target && Target.transform.parent)
            {
                AAMCurrentTargetSAVControl = Target.transform.parent.GetComponent<SaccAirVehicle>();
            }
            if (HUD) { HUD.gameObject.SetActive(true); }
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EnableScript));
            RequestSerialization();
        }
        public void SFEXT_O_OnDrop()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(DisableScript));
            if (HUD) { HUD.gameObject.SetActive(false); }
            AAMTargeting.gameObject.SetActive(false);
            AAMTargetLock.gameObject.SetActive(false);
            AAMLockTimer = 0;
        }
        public void SFEXT_O_TakeOwnership()
        {
            IsOwner = true;
            if (!Holding)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(DisableScript)); }
        }
        public void SFEXT_O_LoseOwnership()
        {
            IsOwner = false;
            AAMTargeting.gameObject.SetActive(false);
            AAMTargetLock.gameObject.SetActive(false);
        }
        public void SFEXT_G_RespawnButton()//called globally when using respawn button
        {
            NumAAM = FullAAMs;
        }
        public void SFEXT_O_RespawnButton()//called when using respawn button
        {
            if (!active)
            {
                Networking.SetOwner(localPlayer, EntityControl.gameObject);
                EntityControl.TakeOwnerShipOfExtensions();
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ResetStatus));
                IsOwner = true;
                //synced variables
                if (InEditor)
                {
                    StingerTransform.SetPositionAndRotation(Spawnposition, Spawnrotation);
                    StingerRigid.velocity = Vector3.zero;
                }
                else
                {
                    StingerObjectSync.Respawn();
                }
                StingerRigid.angularVelocity = Vector3.zero;//editor needs this
            }
        }
        public void EnableScript()
        {
            AAMFire = 0;
            gameObject.SetActive(true);
            active = true;
            if (Holding)
            {
                EntityControl.gameObject.layer = 9;
                foreach (Collider stngcol in StingerColliders)
                {
                    stngcol.isTrigger = true;
                }
            }
            else
            {
                foreach (Collider stngcol in StingerColliders)
                {
                    stngcol.enabled = false;
                }
            }
        }
        public void DisableScript()
        {
            gameObject.SetActive(false);
            active = false;
            Holding = false;
            EntityControl.gameObject.layer = 13;
            foreach (Collider stngcol in StingerColliders)
            {
                stngcol.isTrigger = false;
                stngcol.enabled = true;
            }
        }
        public void SFEXT_O_OnPickupUseDown()
        {
            TriggerLastFrame = true;
            if (NumAAMTargets != 0)
            {
                //firing AAM
                if (NumAAM > 0 && AAMLocked && Time.time - AAMLastFiredTime > AAMLaunchDelay)
                {
                    AAMFire++;//launch AAM using set
                    RequestSerialization();
                    if (LoseLockAfterShot || (NumAAM == 0 && !AllowNoAmmoLock)) { AAMLockTimer = 0; AAMLocked = false; }
                    EntityControl.SendEventToExtensions("SFEXT_O_AAMLaunch");
                }
            }
        }
        public void SFEXT_O_OnPickupUseUp()
        {
            TriggerLastFrame = false;
        }
        void Update()
        {
            if (Holding)
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
                //sound
                if (!AAMLocked && AAMLockTimer > 0)
                {
                    if (AAMTargeting && (NumAAM > 0 || AllowNoAmmoLock)) { AAMTargeting.gameObject.SetActive(true); }
                    if (AAMTargetLock) { AAMTargetLock.gameObject.SetActive(false); }
                }
                else if (AAMLocked)
                {
                    if (AAMTargeting) { AAMTargeting.gameObject.SetActive(false); }
                    if (AAMTargetLock) { AAMTargetLock.gameObject.SetActive(true); }
                }
                else
                {
                    if (AAMTargeting) { AAMTargeting.gameObject.SetActive(false); }
                    if (AAMTargetLock) { AAMTargetLock.gameObject.SetActive(false); }
                }
                Hud();
            }
        }
        private void Hud()
        {
            //AAM Target Indicator
            if (AAMHasTarget)//GUN or AAM
            {
                AAMTargetIndicator.localScale = Vector3.one;
                AAMTargetIndicator.position = HUD.position + AAMCurrentTargetDirection;
                AAMTargetIndicator.localPosition = AAMTargetIndicator.localPosition.normalized * distance_from_head;
                if (AAMLocked)
                {
                    AAMTargetIndicator.localRotation = Quaternion.Euler(new Vector3(0, 180, 0));//back of mesh is locked version
                }
                else
                {
                    AAMTargetIndicator.localRotation = Quaternion.identity;
                }
            }
            else AAMTargetIndicator.localScale = Vector3.zero;
            /////////////////
        }
        private void FixedUpdate()//old AAMTargeting function
        {
            if (Holding)
            {
                float DeltaTime = Time.fixedDeltaTime;
                var AAMCurrentTargetPosition = AAMTargets[AAMTarget].transform.position;
                Vector3 HudControlPosition = HUD.position;
                float AAMCurrentTargetAngle = Vector3.Angle(StingerTransform.forward, (AAMCurrentTargetPosition - HudControlPosition));

                //check 1 target per frame to see if it's infront of us and worthy of being our current target
                var TargetChecker = AAMTargets[AAMTargetChecker];
                var TargetCheckerTransform = TargetChecker.transform;
                var TargetCheckerParent = TargetCheckerTransform.parent;

                Vector3 AAMNextTargetDirection = (TargetCheckerTransform.position - HudControlPosition);
                float NextTargetAngle = Vector3.Angle(StingerTransform.forward, AAMNextTargetDirection);
                float NextTargetDistance = Vector3.Distance(CenterOfMass.position, TargetCheckerTransform.position);

                if (TargetChecker.activeInHierarchy)
                {
                    SaccAirVehicle NextTargetSAVControl = null;

                    if (TargetCheckerParent)
                    {
                        NextTargetSAVControl = TargetCheckerParent.GetComponent<SaccAirVehicle>();
                    }
                    //if target SAVontroller is null then it's a dummy target (or hierarchy isn't set up properly)
                    if ((!NextTargetSAVControl || (!NextTargetSAVControl.Taxiing && !NextTargetSAVControl.EntityControl.dead)))
                    {
                        RaycastHit hitnext;
                        //raycast to check if it's behind something
                        bool LineOfSightNext = Physics.Raycast(HudControlPosition, AAMNextTargetDirection, out hitnext, 99999999, 133137 /* Default, Water, Environment, and Walkthrough */, QueryTriggerInteraction.Collide);

                        /*                 Debug.Log(string.Concat("LoS_next ", LineOfSightNext));
                                        if (hitnext.collider != null) Debug.Log(string.Concat("RayCastCorrectLayer_next ", (hitnext.collider.gameObject.layer == OutsidePlaneLayer)));
                                        if (hitnext.collider != null) Debug.Log(string.Concat("RayCastLayer_next ", hitnext.collider.gameObject.layer));
                                        Debug.Log(string.Concat("LowerAngle_next ", NextTargetAngle < AAMCurrentTargetAngle));
                                        Debug.Log(string.Concat("InAngle_next ", NextTargetAngle < 70));
                                        Debug.Log(string.Concat("BelowMaxDist_next ", NextTargetDistance < AAMMaxTargetDistance)); */

                        if (LineOfSightNext
                            && hitnext.collider && hitnext.collider.gameObject.layer == VehicleLayer //did raycast hit an object on the layer planes are on?
                                && NextTargetAngle < AAMLockAngle
                                    && NextTargetAngle < AAMCurrentTargetAngle
                                        && NextTargetDistance < AAMMaxTargetDistance
                                            && (!HighAspectPreventLock || (NextTargetSAVControl && Vector3.Dot(NextTargetSAVControl.VehicleTransform.forward, AAMNextTargetDirection.normalized) > HighAspectPreventLockAngleDot))
                                            || (AAMCurrentTargetSAVControl &&//null check
                                                                        (AAMCurrentTargetSAVControl.Taxiing ||//switch target if current target is taxiing
                                                                        (MissileType == 0 && !AAMCurrentTargetSAVControl.EngineOn)))//switch target if heatseeker and current target's engine is off
                                                || !AAMTargets[AAMTarget].activeInHierarchy//same as above but if the target is destroyed
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
                                AAMTargetedTimer = 99f;//send targeted straight away
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
                bool LineOfSightCur = Physics.Raycast(HudControlPosition, AAMCurrentTargetDirection, out hitcurrent, 99999999, 133137 /* Default, Water, Environment, and Walkthrough */, QueryTriggerInteraction.Collide);
                //used to make lock remain for .25 seconds after target is obscured
                if (!LineOfSightCur || (hitcurrent.collider && hitcurrent.collider.gameObject.layer != VehicleLayer))
                { AAMTargetObscuredDelay += DeltaTime; }
                else
                { AAMTargetObscuredDelay = 0; }

                if ((AAMTargetObscuredDelay < .25f)
                        && AAMCurrentTargetDistance < AAMMaxTargetDistance
                            && AAMTargets[AAMTarget].activeInHierarchy
                                && (!AAMCurrentTargetSAVControl ||
                                    (!AAMCurrentTargetSAVControl.Taxiing && !AAMCurrentTargetSAVControl.EntityControl.dead &&
                                        (MissileType != 0 || AAMCurrentTargetSAVControl.EngineOn)))//heatseekers cant lock if engine off
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
                                if (SendLockWarning && AAMTargetedTimer > 1)
                                {
                                    sendtargeted = !sendtargeted;
                                    RequestSerialization();
                                    AAMTargetedTimer = 0;
                                }
                                AAMTargetedTimer += DeltaTime;
                            }
                        }
                        else
                        {
                            AAMTargetedTimer = 2f;
                            AAMLockTimer = 0;
                        }
                    }
                }
                else
                {
                    AAMTargetedTimer = 2f;
                    AAMLockTimer = 0;
                    AAMHasTarget = false;
                }
                /*         if (HUDControl.gameObject.activeInHierarchy)
                        {
                            Debug.Log(string.Concat("AAMTarget ", AAMTarget));
                            Debug.Log(string.Concat("HasTarget ", AAMHasTarget));
                            Debug.Log(string.Concat("AAMTargetObscuredDelay ", AAMTargetObscuredDelay));
                            Debug.Log(string.Concat("LoS ", LineOfSightCur));
                            Debug.Log(string.Concat("RayCastCorrectLayer ", (hitcurrent.collider.gameObject.layer == OutsidePlaneLayer)));
                            Debug.Log(string.Concat("RayCastLayer ", hitcurrent.collider.gameObject.layer));
                            Debug.Log(string.Concat("NotObscured ", AAMTargetObscuredDelay < .25f));
                            Debug.Log(string.Concat("InAngle ", AAMCurrentTargetAngle < Lock_Angle));
                            Debug.Log(string.Concat("BelowMaxDist ", AAMCurrentTargetDistance < AAMMaxTargetDistance));
                        } */
            }
        }
        public void LaunchAAM()
        {
            if (FireSound) { FireSound.PlayOneShot(FireSound.clip); }
            if (!InEditor) { IsOwner = localPlayer.IsOwner(gameObject); } else { IsOwner = true; }
            if (NumAAM > 0) { NumAAM--; }//so it doesn't go below 0 when desync occurs
            if (StingerAnimator) { StingerAnimator.SetTrigger(AnimFiredTriggerName); }
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
                NewAAM.SetActive(true);
            }
            if (StingerAnimator) { StingerAnimator.SetFloat(AnimFloatName, (float)NumAAM * FullAAMsDivider); }
            if (HUDText_AAM_ammo) { HUDText_AAM_ammo.text = NumAAM.ToString("F0"); }
        }
        public void ResetStatus()//called globally when using respawn button
        {
            EntityControl.SendEventToExtensions("SFEXT_G_RespawnButton");
        }
    }
}