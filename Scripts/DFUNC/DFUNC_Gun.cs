
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_Gun : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public Animator GunAnimator;
        [Tooltip("Animator bool that is true when the gun is firing")]
        public string GunFiringBoolName = "gunfiring";
        [Tooltip("Desktop key for firing when selected")]
        public KeyCode FireKey = KeyCode.Space;
        [Tooltip("Desktop key for firing when not selected")]
        public KeyCode FireNowKey = KeyCode.None;
        [Tooltip("Transform of which its X scale scales with ammo")]
        public Transform[] AmmoBars;
        [Tooltip("Position at which recoil forces are added, not required for recoil to work. Only use this if you want the vehicle to rotate when shooting")]
        public Transform GunRecoilEmpty;
        [Tooltip("There is a separate particle system for doing damage that is only enabled for the user of the gun. This object is the parent of that particle system, is enabled when entering the seat, and disabled when exiting")]
        public Transform GunDamageParticle;
        [Tooltip("Crosshair to switch to when gun is selected")]
        public GameObject HudCrosshairGun;
        [Tooltip("Vehicle's normal crosshair")]
        public GameObject HudCrosshair;
        [Tooltip("How long it takes to fully reload from empty in seconds")]
        public float FullReloadTimeSec = 20;
        [UdonSynced(UdonSyncMode.None)] public float GunAmmoInSeconds = 12;
        public float RecoilForce = 1;
        [Tooltip("Set a boolean value in the animator when switching to this weapon?")]
        public bool DoAnimBool = false;
        [Tooltip("Animator bool that is true when this function is selected")]
        public string AnimBoolName = "GunSelected";
        [Tooltip("Should the boolean stay true if the pilot exits with it selected?")]
        public bool AnimBoolStayTrueOnExit;
        [Tooltip("Allow gun to fire while vehicle is on the ground?")]
        public bool AllowFiringGrounded = true;
        [Tooltip("Disable the weapon if wind is enabled, to prevent people gaining an unfair advantage")]
        public bool DisallowFireIfWind = false;
        [Tooltip("Enable these objects when GUN selected")]
        public GameObject[] EnableOnSelected;
        [Tooltip("On desktop mode, fire even when not selected if OnPickupUseDown is pressed")]
        [SerializeField] bool DT_UseToFire;
        private bool Grounded;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [System.NonSerializedAttribute] public SAV_PassengerFunctionsController PassengerFunctionsControl;
        private bool AnimOn;
        private int AnimBool_STRING;
        [System.NonSerializedAttribute] public float FullGunAmmoInSeconds;
        private Rigidbody VehicleRigidbody;
        [System.NonSerializedAttribute, UdonSynced, FieldChangeCallback(nameof(Firing))] public bool _firing;
        public bool Firing
        {
            set
            {
                if (value && EntityControl.IsOwner && RecoilForce > 0 && !EntityControl.Piloting)
                {
                    EntityControl.SendEventToExtensions("SFEXT_L_WakeUp");
                }
                GunAnimator.SetBool(GunFiringBoolName, value);
                _firing = value;
            }
            get => _firing;
        }
        private float FullGunAmmoDivider;
        private bool Selected = false;
        bool inVR;
        private bool Selected_HUD = false;
        private float reloadspeed;
        private bool Piloting = false;
        private Vector3 AmmoBarScaleStart;
        private Vector3[] AmmoBarScaleStarts;
        public void SFEXT_L_EntityStart()
        {
            FullGunAmmoInSeconds = GunAmmoInSeconds;
            reloadspeed = FullGunAmmoInSeconds / FullReloadTimeSec;

            AmmoBarScaleStarts = new Vector3[AmmoBars.Length];
            for (int i = 0; i < AmmoBars.Length; i++)
            {
                AmmoBarScaleStarts[i] = AmmoBars[i].localScale;
            }

            VehicleRigidbody = EntityControl.GetComponent<Rigidbody>();
            IsOwner = EntityControl.IsOwner;
            FullGunAmmoDivider = 1f / (FullGunAmmoInSeconds > 0 ? FullGunAmmoInSeconds : 10000000);
            AAMTargets = EntityControl.AAMTargets;
            NumAAMTargets = AAMTargets.Length;
            VehicleTransform = EntityControl.transform;
            CenterOfMass = EntityControl.CenterOfMass;
            OutsideVehicleLayer = EntityControl.OutsideVehicleLayer;
            if (GunDamageParticle) GunDamageParticle.gameObject.SetActive(false);

            //HUD
            if (HUDControl)
            {
                distance_from_head = (float)HUDControl.GetProgramVariable("distance_from_head");
            }
            if (distance_from_head == 0) { distance_from_head = 1.333f; }
        }
        public void ReInitAmmo()//set FullAAMs then run this to change vehicles max gun ammo
        {
            GunAmmoInSeconds = FullGunAmmoInSeconds;
            reloadspeed = FullGunAmmoInSeconds / FullReloadTimeSec;
            FullGunAmmoDivider = 1f / (FullGunAmmoInSeconds > 0 ? FullGunAmmoInSeconds : 10000000);
            UpdateAmmoVisuals();
        }
        public void DFUNC_Selected()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Set_Selected));
            Selected = true;
            if (DoAnimBool && !AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOn)); }
        }
        public void DFUNC_Deselected()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Set_Unselected));
            Selected = false;
            HoldingTrigger_Held = 0;
            if (_firing)
            {
                Firing = false;
                RequestSerialization();
            }
            if (DoAnimBool && AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOff)); }
        }
        public void SFEXT_O_PilotEnter()
        {
            Piloting = true;
            inVR = EntityControl.InVR;
            if (GunDamageParticle) { GunDamageParticle.gameObject.SetActive(true); }
            if (_firing) { Firing = false; }
            gameObject.SetActive(true);
            RequestSerialization();
        }
        public void SFEXT_G_PilotEnter()
        {
            Set_Active();
        }
        public void SFEXT_G_PilotExit()
        {
            Set_Inactive();
            Set_Unselected();
            if (DoAnimBool && !AnimBoolStayTrueOnExit && AnimOn)
            { SetBoolOff(); }
        }
        public void SFEXT_O_PilotExit()
        {
            Piloting = false;
            if (Selected) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Set_Unselected)); }//unselect 
            Selected = false;
            if (GunDamageParticle) { GunDamageParticle.gameObject.SetActive(false); }
        }
        public void SFEXT_O_ReSupply()
        {
            GunAmmoInSeconds = Mathf.Min(GunAmmoInSeconds + reloadspeed, FullGunAmmoInSeconds);
            RequestSerialization();
            UpdateAmmoVisuals();
        }
        public void SFEXT_G_ReSupply()
        {
            if (SAVControl && GunAmmoInSeconds != FullGunAmmoInSeconds)
            { SAVControl.SetProgramVariable("ReSupplied", (int)SAVControl.GetProgramVariable("ReSupplied") + 1); }
            UpdateAmmoVisuals();
        }

        public void UpdateAmmoVisuals()
        {
            for (int i = 0; i < AmmoBars.Length; i++)
            {
                AmmoBars[i].localScale = new Vector3((GunAmmoInSeconds * FullGunAmmoDivider) * AmmoBarScaleStarts[i].x, AmmoBarScaleStarts[i].y, AmmoBarScaleStarts[i].z);
            }
        }
        public void SFEXT_O_RespawnButton()
        {
            GunAmmoInSeconds = Mathf.Min(GunAmmoInSeconds + reloadspeed, FullGunAmmoInSeconds);
            RequestSerialization();
            UpdateAmmoVisuals();
        }
        public void SFEXT_G_RespawnButton()
        {
            GunAmmoInSeconds = Mathf.Min(GunAmmoInSeconds + reloadspeed, FullGunAmmoInSeconds);
            UpdateAmmoVisuals();
            if (DoAnimBool && AnimOn)
            { SetBoolOff(); }
        }
        public void Set_Selected()
        {
            if (HudCrosshairGun) { HudCrosshairGun.SetActive(true); }
            if (HudCrosshair) { HudCrosshair.SetActive(false); }
            for (int i = 0; i < EnableOnSelected.Length; i++)
            { EnableOnSelected[i].SetActive(true); }
            Selected_HUD = true;
        }
        public void Set_Unselected()
        {
            if (HudCrosshairGun) { HudCrosshairGun.SetActive(false); }
            if (HudCrosshair) { HudCrosshair.SetActive(true); }
            if (TargetIndicator) { TargetIndicator.gameObject.SetActive(false); }
            if (GUNLeadIndicator) { GUNLeadIndicator.gameObject.SetActive(false); }
            for (int i = 0; i < EnableOnSelected.Length; i++)
            { EnableOnSelected[i].SetActive(false); }
            Selected_HUD = false;
        }
        public void Set_Active()
        {
            gameObject.SetActive(true);
        }
        public void Set_Inactive()
        {
            GunAnimator.SetBool(GunFiringBoolName, false);
            Firing = false;
            gameObject.SetActive(false);
        }
        bool IsOwner;
        public void SFEXT_O_TakeOwnership()
        {
            IsOwner = true;
            if (gameObject.activeSelf)//if someone times out, tell weapon to stop firing if you take ownership.
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Set_Inactive)); }
            if (Selected_HUD)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Set_Unselected)); }
        }
        public void SFEXT_G_Explode()
        {
            GunAmmoInSeconds = FullGunAmmoInSeconds;
            if (DoAnimBool && AnimOn)
            { SetBoolOff(); }
        }
        public void LateUpdate()
        {
            if (Piloting)
            {
                if (Selected || Input.GetKey(FireNowKey) || (!inVR && DT_UseToFire))
                {
                    float DeltaTime = Time.deltaTime;
                    float Trigger = 0;
                    if (EntityControl.Holding || !inVR && DT_UseToFire)
                        Trigger = HoldingTrigger_Held;
                    else if (Selected)
                    {
                        if (LeftDial)
                        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                        else
                        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
                    }
                    if ((!Grounded || AllowFiringGrounded) && ((Trigger > 0.75 || (Input.GetKey(FireKey) || Input.GetKey(FireNowKey))) && GunAmmoInSeconds > 0))
                    {
                        if (DisallowFireIfWind)
                        {
                            if (((Vector3)SAVControl.GetProgramVariable("FinalWind")).magnitude > 0f)
                            { return; }
                        }
                        if (!_firing)
                        {
                            Firing = true;
                            RequestSerialization();
                            // EntityControl.SendEventToExtensions("SFEXT_O_GunStartFiring");
                        }
                        GunAmmoInSeconds = Mathf.Max(GunAmmoInSeconds - DeltaTime, 0);
                    }
                    else
                    {
                        if (_firing)
                        {
                            Firing = false;
                            RequestSerialization();
                            // EntityControl.SendEventToExtensions("SFEXT_O_GunStopFiring");
                        }
                    }
                    if (HUDControl)
                    { Hud(); }
                    UpdateAmmoVisuals();
                }
                else if (_firing)
                {
                    Firing = false;
                    RequestSerialization();
                    // EntityControl.SendEventToExtensions("SFEXT_O_GunStopFiring");
                }
            }
            if (_firing && IsOwner)
            {
                if (!GunRecoilEmpty)
                {
                    VehicleRigidbody.AddRelativeForce(-Vector3.forward * RecoilForce, ForceMode.Acceleration);
                }
                else
                {
                    VehicleRigidbody.AddForceAtPosition(-GunRecoilEmpty.forward * RecoilForce, GunRecoilEmpty.position, ForceMode.Acceleration);
                }
            }
        }
        private GameObject[] AAMTargets;
        private Transform VehicleTransform;
        private int AAMTarget;
        private int AAMTargetChecker;
        public UdonSharpBehaviour HUDControl;
        private Transform CenterOfMass;
        private SaccAirVehicle AAMCurrentTargetSAVControl;
        private int OutsideVehicleLayer;
        public float MaxTargetDistance = 6000;
        private float AAMLockTimer;
        private int NumAAMTargets;
        private Vector3 AAMCurrentTargetDirection;
        private float AAMTargetObscuredDelay;
        private bool GUNHasTarget;
        private void FixedUpdate()//this is just the old  AAMTargeting adjusted slightly
                                  //there may unnecessary stuff in here because it doesn't need to do missile related stuff any more 
        {
            if (Selected && HUDControl)
            {
                float DeltaTime = Time.fixedDeltaTime;
                var AAMCurrentTargetPosition = AAMTargets[AAMTarget].transform.position;
                Vector3 HudControlPosition = HUDControl.transform.position;
                float AAMCurrentTargetAngle = Vector3.Angle(VehicleTransform.forward, (AAMCurrentTargetPosition - HudControlPosition));

                //check 1 target per frame to see if it's infront of us and worthy of being our current target
                var TargetChecker = AAMTargets[AAMTargetChecker];
                var TargetCheckerTransform = TargetChecker.transform;
                var TargetCheckerParent = TargetCheckerTransform.parent;

                Vector3 AAMNextTargetDirection = (TargetCheckerTransform.position - HudControlPosition);
                float NextTargetAngle = Vector3.Angle(VehicleTransform.forward, AAMNextTargetDirection);
                float NextTargetDistance = Vector3.Distance(CenterOfMass.position, TargetCheckerTransform.position);

                if (TargetChecker.activeInHierarchy)
                {
                    SaccAirVehicle NextTargetSAVControl = null;

                    if (TargetCheckerParent)
                    {
                        NextTargetSAVControl = TargetCheckerParent.GetComponent<SaccAirVehicle>();
                    }
                    //if target EngineController is null then it's a dummy target (or hierarchy isn't set up properly)
                    if ((!NextTargetSAVControl || (!NextTargetSAVControl.Taxiing && !NextTargetSAVControl.EntityControl._dead)))
                    {
                        RaycastHit hitnext;
                        //raycast to check if it's behind something
                        bool LineOfSightNext = Physics.Raycast(HudControlPosition, AAMNextTargetDirection, out hitnext, 99999999, 133137 /* Default, Water, Environment, and Walkthrough */, QueryTriggerInteraction.Collide);
#if UNITY_EDITOR
                        if (hitnext.collider)
                            Debug.DrawLine(HudControlPosition, hitnext.point, Color.red);
                        else
                            Debug.DrawRay(HudControlPosition, AAMNextTargetDirection, Color.yellow);
#endif
                        /*                 Debug.Log(string.Concat("LoS_next ", LineOfSightNext));
                                        if (hitnext.collider != null) Debug.Log(string.Concat("RayCastCorrectLayer_next ", (hitnext.collider.gameObject.layer == OutsidePlaneLayer)));
                                        if (hitnext.collider != null) Debug.Log(string.Concat("RayCastLayer_next ", hitnext.collider.gameObject.layer));
                                        Debug.Log(string.Concat("LowerAngle_next ", NextTargetAngle < AAMCurrentTargetAngle));
                                        Debug.Log(string.Concat("InAngle_next ", NextTargetAngle < 70));
                                        Debug.Log(string.Concat("BelowMaxDist_next ", NextTargetDistance < AAMMaxTargetDistance)); */

                        if (LineOfSightNext
                            && (hitnext.collider && hitnext.collider.gameObject.layer == OutsideVehicleLayer) //did raycast hit an object on the layer planes are on?
                                && NextTargetAngle < 70//lock angle
                                    && NextTargetAngle < AAMCurrentTargetAngle
                                        && NextTargetDistance < MaxTargetDistance
                                            || ((AAMCurrentTargetSAVControl && AAMCurrentTargetSAVControl.Taxiing)//prevent being unable to switch target if it's angle is higher than your current target and your current target happens to be taxiing and is therefore untargetable
                                                || !AAMTargets[AAMTarget].activeInHierarchy))//same as above but if the target is destroyed
                        {
                            //found new target
                            AAMCurrentTargetAngle = NextTargetAngle;
                            AAMTarget = AAMTargetChecker;
                            AAMCurrentTargetPosition = AAMTargets[AAMTarget].transform.position;
                            AAMCurrentTargetSAVControl = NextTargetSAVControl;
                            AAMLockTimer = 0;
                            if (HUDControl)
                            {
                                RelativeTargetVelLastFrame = Vector3.zero;
                                GUN_TargetSpeedLerper = 0f;
                                GUN_TargetDirOld = AAMNextTargetDirection * 1.00001f; //so the difference isn't 0
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
                else if (AAMTargetChecker == NumAAMTargets)
                { AAMTargetChecker = 0; }

                //if target is currently in front of plane, lock onto it
                if (!AAMCurrentTargetSAVControl)
                { AAMCurrentTargetDirection = AAMCurrentTargetPosition - HudControlPosition; }
                else
                { AAMCurrentTargetDirection = AAMCurrentTargetSAVControl.CenterOfMass.position - HudControlPosition; }
                float AAMCurrentTargetDistance = AAMCurrentTargetDirection.magnitude;
                //check if target is active, and if it's enginecontroller is null(dummy target), or if it's not null(plane) make sure it's not taxiing or dead.
                //raycast to check if it's behind something
                RaycastHit hitcurrent;
                bool LineOfSightCur = Physics.Raycast(HudControlPosition, AAMCurrentTargetDirection, out hitcurrent, 99999999, 133137 /* Default, Water, Environment, and Walkthrough */, QueryTriggerInteraction.Collide);
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

                if (!(bool)SAVControl.GetProgramVariable("Taxiing")
                    && (AAMTargetObscuredDelay < .25f)
                        && AAMCurrentTargetDistance < MaxTargetDistance
                            && AAMTargets[AAMTarget].activeInHierarchy
                                && (!AAMCurrentTargetSAVControl || (!AAMCurrentTargetSAVControl.Taxiing && !AAMCurrentTargetSAVControl.EntityControl._dead)))
                {
                    if ((AAMTargetObscuredDelay < .25f) && AAMCurrentTargetDistance < MaxTargetDistance)
                    {
                        GUNHasTarget = true;
                        if (AAMCurrentTargetAngle < 70)//lock angle
                        {
                            AAMLockTimer += DeltaTime;
                        }
                        else
                        {
                            AAMLockTimer = 0;
                        }
                    }
                }
                else
                {
                    AAMLockTimer = 0;
                    GUNHasTarget = false;
                }
                /*         Debug.Log(string.Concat("AAMTarget ", AAMTarget));
                        Debug.Log(string.Concat("HasTarget ", AAMHasTarget));
                        Debug.Log(string.Concat("AAMTargetObscuredDelay ", AAMTargetObscuredDelay));
                        Debug.Log(string.Concat("LoS ", LineOfSightCur));
                        Debug.Log(string.Concat("RayCastCorrectLayer ", (hitcurrent.collider.gameObject.layer == OutsidePlaneLayer)));
                        Debug.Log(string.Concat("RayCastLayer ", hitcurrent.collider.gameObject.layer));
                        Debug.Log(string.Concat("NotObscured ", AAMTargetObscuredDelay < .25f));
                        Debug.Log(string.Concat("InAngle ", AAMCurrentTargetAngle < 70));
                        Debug.Log(string.Concat("BelowMaxDist ", AAMCurrentTargetDistance < AAMMaxTargetDistance)); */
            }
        }
        public void SFEXT_G_TouchDown() { Grounded = true; }
        public void SFEXT_G_TouchDownWater() { Grounded = true; }
        public void SFEXT_G_TakeOff() { Grounded = false; }
        private int HoldingTrigger_Held = 0;
        public void SFEXT_O_OnPickupUseDown()
        {
            HoldingTrigger_Held = 1;
        }
        public void SFEXT_O_OnPickupUseUp()
        {
            HoldingTrigger_Held = 0;
        }
        public void SFEXT_O_OnPickup()
        {
            SFEXT_O_PilotEnter();
        }
        public void SFEXT_O_OnDrop()
        {
            SFEXT_O_PilotExit();
            HoldingTrigger_Held = 0;
        }
        public void SFEXT_G_OnPickup() { SFEXT_G_PilotEnter(); }
        public void SFEXT_G_OnDrop() { SFEXT_G_PilotExit(); }
        //hud stuff
        public Transform TargetIndicator;
        public Transform GUNLeadIndicator;
        [Range(0.01f, 1)]
        [Tooltip("1 = max accuracy, 0.01 = smooth but innacurate")]
        [SerializeField] private float GunLeadResponsiveness = 1f;
        private float GUN_TargetSpeedLerper;
        private Vector3 RelativeTargetVelLastFrame;
        private Vector3 RelativeTargetVel;
        private Vector3 GUN_TargetDirOld;
        private float distance_from_head;
        [Tooltip("Put the speed from the bullet particle system in here so that the lead indicator works with the correct offset")]
        public float BulletSpeed;
        private void Hud()
        {
            if (GUNHasTarget)
            {
                if (TargetIndicator)
                {
                    //Target Indicator
                    TargetIndicator.gameObject.SetActive(true);
                    TargetIndicator.position = HUDControl.transform.position + AAMCurrentTargetDirection;
                    TargetIndicator.localPosition = TargetIndicator.localPosition.normalized * distance_from_head;
                    TargetIndicator.rotation = Quaternion.LookRotation(TargetIndicator.position - HUDControl.transform.position, VehicleTransform.transform.up);//This makes it not stretch when off to the side by fixing the rotation.
                }

                if (GUNLeadIndicator)
                {
                    //GUN Lead Indicator
                    float deltaTime = Time.deltaTime;
                    Vector3 HudControlPosition = HUDControl.transform.position;
                    GUNLeadIndicator.gameObject.SetActive(true);
                    Vector3 TargetPos;
                    if (!AAMCurrentTargetSAVControl)//target is a dummy target
                    { TargetPos = AAMTargets[AAMTarget].transform.position; }
                    else
                    { TargetPos = AAMCurrentTargetSAVControl.CenterOfMass.position; }
                    Vector3 TargetDir = TargetPos - HudControlPosition;

                    Vector3 RelativeTargetVel = TargetDir - GUN_TargetDirOld;

                    GUN_TargetDirOld = Vector3.Lerp(GUN_TargetDirOld, TargetDir, GunLeadResponsiveness);
                    GUN_TargetSpeedLerper = RelativeTargetVel.magnitude * GunLeadResponsiveness / deltaTime;

                    float interceptTime = vintercept(HudControlPosition, BulletSpeed, TargetPos, RelativeTargetVel.normalized * GUN_TargetSpeedLerper);
                    Vector3 PredictedPos = (TargetPos + (RelativeTargetVel.normalized * GUN_TargetSpeedLerper) * interceptTime);

                    //Bulletdrop, technically incorrect implementation because it should be integrated into vintercept() but that'd be very difficult
                    Vector3 gravity = new Vector3(0, -Physics.gravity.y * .5f * interceptTime * interceptTime, 0);
                    // Vector3 TargetAccel = RelativeTargetVel - RelativeTargetVelLastFrame;
                    // Vector3 accel = ((TargetAccel / Time.deltaTime) * 0.5f * interceptTime * interceptTime); // accel causes jitter
                    PredictedPos += gravity /* + accel */;

                    GUNLeadIndicator.position = PredictedPos;
                    //move lead indicator to match the distance of the rest of the hud
                    GUNLeadIndicator.localPosition = GUNLeadIndicator.localPosition.normalized * distance_from_head;
                    GUNLeadIndicator.rotation = Quaternion.LookRotation(GUNLeadIndicator.position - HUDControl.transform.position, VehicleTransform.transform.up);//This makes it not stretch when off to the side by fixing the rotation.

                    RelativeTargetVelLastFrame = RelativeTargetVel;
                }
            }
            else
            {
                if (TargetIndicator)
                { TargetIndicator.gameObject.SetActive(false); }
                if (GUNLeadIndicator)
                { GUNLeadIndicator.gameObject.SetActive(false); }
            }
            /////////////////
        }

        //not mine
        float vintercept(Vector3 fireorg, float missilespeed, Vector3 tgtorg, Vector3 tgtvel)
        {
            if (missilespeed <= 0)
                return (tgtorg - fireorg).magnitude / missilespeed;

            float tgtspd = tgtvel.magnitude;
            Vector3 dir = fireorg - tgtorg;
            float d = dir.magnitude;
            float a = missilespeed * missilespeed - tgtspd * tgtspd;
            float b = 2 * Vector3.Dot(dir, tgtvel);
            float c = -d * d;

            float t = 0;
            if (a == 0)
            {
                if (b == 0)
                    return 0f;
                else
                    t = -c / b;
            }
            else
            {
                float s0 = b * b - 4 * a * c;
                if (s0 <= 0)
                    return 0f;
                float s = Mathf.Sqrt(s0);
                float div = 1.0f / (2f * a);
                float t1 = -(s + b) * div;
                float t2 = (s - b) * div;
                if (t1 <= 0 && t2 <= 0)
                    return 0f;
                t = (t1 > 0 && t2 > 0) ? Mathf.Min(t1, t2) : Mathf.Max(t1, t2);
            }
            return t;
        }
        public void SetBoolOn()
        {
            AnimOn = true;
            GunAnimator.SetBool(AnimBoolName, AnimOn);
        }
        public void SetBoolOff()
        {
            AnimOn = false;
            GunAnimator.SetBool(AnimBoolName, AnimOn);
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
    }
}
