
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class StingerScript : UdonSharpBehaviour
{
    public SaccEntity EntityControl;
    public Transform HUD;
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
    [Tooltip("Minimum time between missile launches")]
    public float AAMLaunchDelay = 0.5f;
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
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.None)] public int AAMTarget = 0;
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
        NumAAMTargets = EntityControl.NumAAMTargets;
        AAMTargets = EntityControl.AAMTargets;
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

        NumChildrenStart = transform.childCount;
        int NumToInstantiate = Mathf.Min(FullAAMs, 10);
        for (int i = 0; i < NumToInstantiate; i++)
        {
            InstantiateWeapon();
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
        //Make sure SAVeControl.AAMCurrentTargetSAVControl is correct
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
        if (NumAAMTargets != 0)
        {
            //firing AAM
            if (AAMLocked && Time.time - AAMLastFiredTime > AAMLaunchDelay)
            {
                AAMLastFiredTime = Time.time;
                AAMFire++;//launch AAM using set
                RequestSerialization();
                if (NumAAM == 0 || LoseLockAfterShot) { AAMLockTimer = 0; AAMLocked = false; }
                EntityControl.SendEventToExtensions("SFEXT_O_AAMLaunch");
            }
        }
    }
    void Update()
    {
        if (Holding)
        {
            if (AAMLockTimer > AAMLockTime && AAMHasTarget) { AAMLocked = true; }
            else { AAMLocked = false; }
            //sound
            if (AAMLockTimer > 0 && !AAMLocked)
            {
                AAMTargeting.gameObject.SetActive(true);
                AAMTargetLock.gameObject.SetActive(false);
            }
            else if (AAMLocked)
            {
                AAMTargeting.gameObject.SetActive(false);
                AAMTargetLock.gameObject.SetActive(true);
            }
            else
            {
                AAMTargeting.gameObject.SetActive(false);
                AAMTargetLock.gameObject.SetActive(false);
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
                SaccAirVehicle NextTargetSAVontrol = null;

                if (TargetCheckerParent)
                {
                    NextTargetSAVontrol = TargetCheckerParent.GetComponent<SaccAirVehicle>();
                }
                //if target SAVontroller is null then it's a dummy target (or hierarchy isn't set up properly)
                if ((!NextTargetSAVontrol || (!NextTargetSAVontrol.Taxiing && !NextTargetSAVontrol.EntityControl.dead)))
                {
                    RaycastHit hitnext;
                    //raycast to check if it's behind something
                    bool LineOfSightNext = Physics.Raycast(HudControlPosition, AAMNextTargetDirection, out hitnext, 99999999, 133125 /* Default, Water, Environment, and Walkthrough */, QueryTriggerInteraction.Collide);

                    /*                 Debug.Log(string.Concat("LoS_next ", LineOfSightNext));
                                    if (hitnext.collider != null) Debug.Log(string.Concat("RayCastCorrectLayer_next ", (hitnext.collider.gameObject.layer == OutsidePlaneLayer)));
                                    if (hitnext.collider != null) Debug.Log(string.Concat("RayCastLayer_next ", hitnext.collider.gameObject.layer));
                                    Debug.Log(string.Concat("LowerAngle_next ", NextTargetAngle < AAMCurrentTargetAngle));
                                    Debug.Log(string.Concat("InAngle_next ", NextTargetAngle < 70));
                                    Debug.Log(string.Concat("BelowMaxDist_next ", NextTargetDistance < AAMMaxTargetDistance)); */

                    if ((LineOfSightNext
                        && hitnext.collider && hitnext.collider.gameObject.layer == VehicleLayer //did raycast hit an object on the layer planes are on?
                            && NextTargetAngle < AAMLockAngle
                                && NextTargetAngle < AAMCurrentTargetAngle)
                                    && NextTargetDistance < AAMMaxTargetDistance
                                        || ((AAMCurrentTargetSAVControl && AAMCurrentTargetSAVControl.Taxiing)//prevent being unable to switch target if it's angle is higher than your current target and your current target happens to be taxiing and is therefore untargetable
                                            || !AAMTargets[AAMTarget].activeInHierarchy))//same as above but if the target is destroyed
                    {
                        //found new target
                        AAMCurrentTargetAngle = NextTargetAngle;
                        AAMTarget = AAMTargetChecker;
                        AAMCurrentTargetPosition = AAMTargets[AAMTarget].transform.position;
                        AAMCurrentTargetSAVControl = NextTargetSAVontrol;
                        AAMLockTimer = 0;
                        AAMTargetedTimer = 99f;//send targeted straight away
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
            bool LineOfSightCur = Physics.Raycast(HudControlPosition, AAMCurrentTargetDirection, out hitcurrent, 99999999, 133125 /* Default, Water, Environment, and Walkthrough */, QueryTriggerInteraction.Collide);
            //used to make lock remain for .25 seconds after target is obscured
            if (!LineOfSightCur || (hitcurrent.collider && hitcurrent.collider.gameObject.layer != VehicleLayer))
            { AAMTargetObscuredDelay += DeltaTime; }
            else
            { AAMTargetObscuredDelay = 0; }

            if ((AAMTargetObscuredDelay < .25f)
                    && AAMCurrentTargetDistance < AAMMaxTargetDistance
                        && AAMTargets[AAMTarget].activeInHierarchy
                            && (!AAMCurrentTargetSAVControl || (!AAMCurrentTargetSAVControl.Taxiing && !AAMCurrentTargetSAVControl.EntityControl.dead)))
            {
                if ((AAMTargetObscuredDelay < .25f) && AAMCurrentTargetDistance < AAMMaxTargetDistance)
                {
                    AAMHasTarget = true;
                    if (AAMCurrentTargetAngle < AAMLockAngle && NumAAM > 0)
                    {
                        AAMLockTimer += DeltaTime;
                        if (AAMCurrentTargetSAVControl)
                        {
                            //target is a plane, send the 'targeted' event every second to make the target plane play a warning sound in the cockpit.
                            AAMTargetedTimer += DeltaTime;
                            if (AAMTargetedTimer > 1)
                            {
                                sendtargeted = !sendtargeted;
                                RequestSerialization();
                                AAMTargetedTimer = 0;
                            }
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
            NewAAM.transform.SetParent(null);
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
