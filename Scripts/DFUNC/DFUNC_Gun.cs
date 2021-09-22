
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class DFUNC_Gun : UdonSharpBehaviour
{
    [SerializeField] private UdonSharpBehaviour SAVControl;
    [SerializeField] private Animator GunAnimator;
    [Tooltip("Transform of which its X scale scales with ammo")]
    [SerializeField] private Transform AmmoBar;
    [Tooltip("Position at which recoil forces are added, not required for recoil to work. Only use this if you want the vehicle to rotate when shooting")]
    [SerializeField] private Transform GunRecoilEmpty;
    [Tooltip("There is a separate particle system for doing damage that is only enabled for the user of the gun. This object is the parent of that particle system, is enabled when entering the seat, and disabled when exiting")]
    [SerializeField] private Transform GunDamageParticle;
    [Tooltip("Crosshair to switch to when gun is selected")]
    [SerializeField] private GameObject HudCrosshairGun;
    [Tooltip("Vehicle's normal crosshair")]
    [SerializeField] private GameObject HudCrosshair;
    [Tooltip("How long it takes to fully reload from empty in seconds")]
    [SerializeField] private float FullReloadTimeSec = 20;
    [SerializeField] [UdonSynced(UdonSyncMode.None)] private float GunAmmoInSeconds = 12;
    [SerializeField] private float GunRecoil = 150;
    [Tooltip("Set a boolean value in the animator when switching to this weapon?")]
    [SerializeField] private bool DoAnimBool = false;
    [SerializeField] private string AnimBoolName = "GunSelected";
    [Tooltip("Should the boolean stay true if the pilot exits with it selected?")]
    [SerializeField] private bool AnimBoolStayTrueOnExit;
    private SaccEntity EntityControl;
    private float boolToggleTime;
    private bool AnimOn;
    private int AnimBool_STRING;
    private bool UseLeftTrigger = false;
    private float FullGunAmmoInSeconds = 12;
    private Rigidbody VehicleRigidbody;
    private bool GunRecoilEmptyNULL = true;
    [UdonSynced, FieldChangeCallback(nameof(Firing))] private bool _firing;
    public bool Firing
    {
        set
        {
            _firing = value;
            GunAnimator.SetBool(GUNFIRING_STRING, value);
        }
        get => _firing;
    }
    private int GUNFIRING_STRING = Animator.StringToHash("gunfiring");
    private int GUNAMMO_STRING = Animator.StringToHash("gunammo");
    private float FullGunAmmoDivider;
    private bool Selected = false;
    private float reloadspeed;
    private bool LeftDial = false;
    private bool Piloting = false;
    private int DialPosition = -999;
    private Vector3 AmmoBarScaleStart;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXT_L_EntityStart()
    {
        AnimBool_STRING = Animator.StringToHash(AnimBoolName);

        reloadspeed = FullGunAmmoInSeconds / FullReloadTimeSec;
        FullGunAmmoInSeconds = GunAmmoInSeconds;
        AmmoBarScaleStart = AmmoBar.localScale;

        EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
        VehicleRigidbody = EntityControl.GetComponent<Rigidbody>();
        GunRecoilEmptyNULL = GunRecoilEmpty == null;
        FullGunAmmoDivider = 1f / (FullGunAmmoInSeconds > 0 ? FullGunAmmoInSeconds : 10000000);
        AAMTargets = EntityControl.AAMTargets;
        NumAAMTargets = EntityControl.NumAAMTargets;
        VehicleTransform = EntityControl.transform;
        CenterOfMass = EntityControl.CenterOfMass;
        OutsidePlaneLayer = LayerMask.NameToLayer("Walkthrough");
        GunRecoil *= VehicleRigidbody.mass;

        FindSelf();

        //HUD
        if (HUDControl != null)
        {
            distance_from_head = (float)HUDControl.GetProgramVariable("distance_from_head");
        }
        if (distance_from_head == 0) { distance_from_head = 1.333f; }
    }
    public void DFUNC_Selected()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Set_Active));
        Selected = true;
        if (DoAnimBool && !AnimOn)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOn)); }
    }
    public void DFUNC_Deselected()
    {
        if (Selected)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Set_Inactive)); }
        Selected = false;
        if (DoAnimBool && AnimOn)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOff)); }
    }
    public void SFEXT_O_PilotEnter()
    {
        GunDamageParticle.gameObject.SetActive(true);
        Piloting = true;
    }
    public void SFEXT_G_PilotExit()
    {
        if (DoAnimBool && !AnimBoolStayTrueOnExit && AnimOn)
        { SetBoolOff(); }
    }
    public void SFEXT_O_PilotExit()
    {
        Piloting = false;
        RequestSerialization();
        if (Selected) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Set_Inactive)); }
        Selected = false;
        GunDamageParticle.gameObject.SetActive(false);
    }
    public void SFEXT_G_ReSupply()
    {
        if (GunAmmoInSeconds != FullGunAmmoInSeconds)
        { SAVControl.SetProgramVariable("ReSupplied", (int)SAVControl.GetProgramVariable("ReSupplied") + 1); }
        GunAmmoInSeconds = Mathf.Min(GunAmmoInSeconds + reloadspeed, FullGunAmmoInSeconds);
        AmmoBar.localScale = new Vector3((GunAmmoInSeconds * FullGunAmmoDivider) * AmmoBarScaleStart.x, AmmoBarScaleStart.y, AmmoBarScaleStart.z);
    }
    public void SFEXT_G_RespawnButton()
    {
        GunAmmoInSeconds = FullGunAmmoInSeconds;
        AmmoBar.localScale = new Vector3((GunAmmoInSeconds * FullGunAmmoDivider) * AmmoBarScaleStart.x, AmmoBarScaleStart.y, AmmoBarScaleStart.z);
        if (DoAnimBool && AnimOn)
        { SetBoolOff(); }
    }
    public void Set_Active()
    {
        HudCrosshairGun.SetActive(true);
        HudCrosshair.SetActive(false);
        gameObject.SetActive(true);
    }
    public void Set_Inactive()
    {
        HudCrosshairGun.SetActive(false);
        HudCrosshair.SetActive(true);
        TargetIndicator.gameObject.SetActive(false);
        GUNLeadIndicator.gameObject.SetActive(false);
        GunAnimator.SetBool(GUNFIRING_STRING, false);
        Firing = false;
        gameObject.SetActive(false);
    }
    public void SFEXT_O_TakeOwnership()
    {
        if (_firing)//if someone times out, tell weapon to stop firing if you take ownership.
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Set_Inactive)); }
    }
    public void SFEXT_G_Explode()
    {
        GunAmmoInSeconds = FullGunAmmoInSeconds;
        if (DoAnimBool && AnimOn)
        { SetBoolOff(); }
    }
    public void LateUpdate()
    {
        if (Piloting && Selected)
        {
            float DeltaTime = Time.deltaTime;
            float Trigger;
            if (UseLeftTrigger)
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
            else
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
            if ((Trigger > 0.75 || (Input.GetKey(KeyCode.Space))) && GunAmmoInSeconds > 0)
            {
                if (!_firing)
                {
                    Firing = true;
                    RequestSerialization();
                    if ((bool)SAVControl.GetProgramVariable("IsOwner"))
                    { EntityControl.SendEventToExtensions("SFEXT_O_GunStartFiring"); }
                }
                GunAmmoInSeconds = Mathf.Max(GunAmmoInSeconds - DeltaTime, 0);
                if (GunRecoilEmptyNULL)
                {
                    VehicleRigidbody.AddRelativeForce(-Vector3.forward * GunRecoil * Time.smoothDeltaTime);
                }
                else
                {
                    VehicleRigidbody.AddForceAtPosition(-GunRecoilEmpty.forward * GunRecoil * .01f/* so the strength is in a similar range as above*/, GunRecoilEmpty.position, ForceMode.Force);
                }
            }
            else
            {
                if (_firing)
                {
                    Firing = false;
                    RequestSerialization();
                    if ((bool)SAVControl.GetProgramVariable("IsOwner"))
                    { EntityControl.SendEventToExtensions("SFEXT_O_GunStopFiring"); }
                }
            }
            Hud();
        }
        AmmoBar.localScale = new Vector3((GunAmmoInSeconds * FullGunAmmoDivider) * AmmoBarScaleStart.x, AmmoBarScaleStart.y, AmmoBarScaleStart.z);
    }
    private GameObject[] AAMTargets;
    private Transform VehicleTransform;
    private int AAMTarget;
    private int AAMTargetChecker;
    [SerializeField] private UdonSharpBehaviour HUDControl;
    private Transform CenterOfMass;
    private SaccAirVehicle AAMCurrentTargetSAVControl;
    private int OutsidePlaneLayer;
    [SerializeField] private float MaxTargetDistance = 6000;
    private float AAMLockTimer;
    private int NumAAMTargets;
    private Vector3 AAMCurrentTargetDirection;
    private float AAMTargetObscuredDelay;
    private bool GUNHasTarget;
    private void FixedUpdate()//this is just the old  AAMTargeting adjusted slightly
                              //there may unnecessary stuff in here because it doesn't need to do missile related stuff any more 
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
        bool AAMCurrentTargetSAVControlNull = AAMCurrentTargetSAVControl == null;

        if (TargetChecker.activeInHierarchy)
        {
            SaccAirVehicle NextTargetSAVControl = null;

            if (TargetCheckerParent)
            {
                NextTargetSAVControl = TargetCheckerParent.GetComponent<SaccAirVehicle>();
            }
            //if target EngineController is null then it's a dummy target (or hierarchy isn't set up properly)
            if ((!NextTargetSAVControl || (!NextTargetSAVControl.Taxiing && !NextTargetSAVControl.EntityControl.dead)))
            {
                RaycastHit hitnext;
                //raycast to check if it's behind something
                bool LineOfSightNext = Physics.Raycast(HudControlPosition, AAMNextTargetDirection, out hitnext, 99999999, 133121 /* Default, Environment, and Walkthrough */, QueryTriggerInteraction.Ignore);

                /*                 Debug.Log(string.Concat("LoS_next ", LineOfSightNext));
                                if (hitnext.collider != null) Debug.Log(string.Concat("RayCastCorrectLayer_next ", (hitnext.collider.gameObject.layer == OutsidePlaneLayer)));
                                if (hitnext.collider != null) Debug.Log(string.Concat("RayCastLayer_next ", hitnext.collider.gameObject.layer));
                                Debug.Log(string.Concat("LowerAngle_next ", NextTargetAngle < AAMCurrentTargetAngle));
                                Debug.Log(string.Concat("InAngle_next ", NextTargetAngle < 70));
                                Debug.Log(string.Concat("BelowMaxDist_next ", NextTargetDistance < AAMMaxTargetDistance)); */

                if ((LineOfSightNext
                    && hitnext.collider.gameObject.layer == OutsidePlaneLayer //did raycast hit an object on the layer planes are on?
                        && NextTargetAngle < 70//lock angle
                            && NextTargetAngle < AAMCurrentTargetAngle)
                                && NextTargetDistance < MaxTargetDistance
                                    || ((!AAMCurrentTargetSAVControlNull && AAMCurrentTargetSAVControl.Taxiing)//prevent being unable to switch target if it's angle is higher than your current target and your current target happens to be taxiing and is therefore untargetable
                                        || !AAMTargets[AAMTarget].activeInHierarchy))//same as above but if the target is destroyed
                {
                    //found new target
                    AAMCurrentTargetAngle = NextTargetAngle;
                    AAMTarget = AAMTargetChecker;
                    AAMCurrentTargetPosition = AAMTargets[AAMTarget].transform.position;
                    AAMCurrentTargetSAVControl = NextTargetSAVControl;
                    AAMLockTimer = 0;
                    AAMCurrentTargetSAVControlNull = AAMCurrentTargetSAVControl == null;
                    if (HUDControl != null)
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
        else if (AAMTargetChecker > NumAAMTargets - 1)
        { AAMTargetChecker = 0; }

        //if target is currently in front of plane, lock onto it
        if (AAMCurrentTargetSAVControlNull)
        { AAMCurrentTargetDirection = AAMCurrentTargetPosition - HudControlPosition; }
        else
        { AAMCurrentTargetDirection = AAMCurrentTargetSAVControl.CenterOfMass.position - HudControlPosition; }
        float AAMCurrentTargetDistance = AAMCurrentTargetDirection.magnitude;
        //check if target is active, and if it's enginecontroller is null(dummy target), or if it's not null(plane) make sure it's not taxiing or dead.
        //raycast to check if it's behind something
        RaycastHit hitcurrent;
        bool LineOfSightCur = Physics.Raycast(HudControlPosition, AAMCurrentTargetDirection, out hitcurrent, 99999999, 133121 /* Default, Environment, and Walkthrough */, QueryTriggerInteraction.Ignore);
        //used to make lock remain for .25 seconds after target is obscured
        if (LineOfSightCur == false || hitcurrent.collider.gameObject.layer != OutsidePlaneLayer)
        { AAMTargetObscuredDelay += DeltaTime; }
        else
        { AAMTargetObscuredDelay = 0; }

        if (!(bool)SAVControl.GetProgramVariable("Taxiing")
            && (AAMTargetObscuredDelay < .25f)
                && AAMCurrentTargetDistance < MaxTargetDistance
                    && AAMTargets[AAMTarget].activeInHierarchy
                        && (AAMCurrentTargetSAVControlNull || (!AAMCurrentTargetSAVControl.Taxiing && !AAMCurrentTargetSAVControl.EntityControl.dead)))
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



    //hud stuff
    [SerializeField] private Transform TargetIndicator;
    [SerializeField] private Transform GUNLeadIndicator;
    private float GUN_TargetSpeedLerper;
    private Vector3 RelativeTargetVelLastFrame;
    private Vector3 RelativeTargetVel;
    private Vector3 GUN_TargetDirOld;
    private float distance_from_head;
    [SerializeField] private float BulletSpeed;
    private void Hud()
    {
        float SmoothDeltaTime = Time.smoothDeltaTime;
        if (GUNHasTarget)
        {
            TargetIndicator.gameObject.SetActive(true);
            TargetIndicator.position = HUDControl.transform.position + AAMCurrentTargetDirection;
            TargetIndicator.localPosition = TargetIndicator.localPosition.normalized * distance_from_head;
        }
        else TargetIndicator.gameObject.SetActive(false);



        //GUN Lead Indicator
        if (GUNHasTarget)
        {
            Vector3 HudControlPosition = HUDControl.transform.position;
            GUNLeadIndicator.gameObject.SetActive(true);
            Vector3 TargetDir;
            if (AAMCurrentTargetSAVControl == null)//target is a dummy target
            { TargetDir = AAMTargets[AAMTarget].transform.position - HudControlPosition; }
            else
            { TargetDir = AAMCurrentTargetSAVControl.CenterOfMass.position - HudControlPosition; }
            GUN_TargetDirOld = Vector3.Lerp(GUN_TargetDirOld, TargetDir, .2f);

            Vector3 RelativeTargetVel = TargetDir - GUN_TargetDirOld;
            float BulletPlusPlaneSpeed = ((Vector3)SAVControl.GetProgramVariable("CurrentVel") + (VehicleTransform.forward * BulletSpeed) - (RelativeTargetVel * .25f)).magnitude;
            Vector3 TargetAccel = RelativeTargetVel - RelativeTargetVelLastFrame;
            //GUN_TargetDirOld is around 4 frames worth of distance behind a moving target (lerped by .2) in order to smooth out the calculation for unsmooth netcode
            //multiplying the result by .25(to get back to 1 frames worth) seems to actually give an accurate enough result to use in prediction
            GUN_TargetSpeedLerper = Mathf.Lerp(GUN_TargetSpeedLerper, (RelativeTargetVel.magnitude * .25f) / SmoothDeltaTime, 15 * SmoothDeltaTime);
            float BulletHitTime = TargetDir.magnitude / BulletPlusPlaneSpeed;
            //normalize lerped relative target velocity vector and multiply by lerped speed
            Vector3 RelTargVelNormalized = RelativeTargetVel.normalized;
            Vector3 PredictedPos = (TargetDir
                + ((RelTargVelNormalized * GUN_TargetSpeedLerper)//Linear 
                                                                 //the .125 in the next line is combined .25 for undoing the lerp, and .5 for the acceleration formula
                    + (TargetAccel * .125f * BulletHitTime)
                        + new Vector3(0, 9.81f * .5f * BulletHitTime, 0))//Bulletdrop
                            * BulletHitTime);
            GUNLeadIndicator.position = HUDControl.transform.position + PredictedPos;
            GUNLeadIndicator.localPosition = GUNLeadIndicator.localPosition.normalized * distance_from_head;

            RelativeTargetVelLastFrame = RelativeTargetVel;
        }
        else GUNLeadIndicator.gameObject.SetActive(false);
        /////////////////
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
        Debug.LogWarning("DFUNC_Gun: Can't find self in dial functions");
    }
    public void SetBoolOn()
    {
        boolToggleTime = Time.time;
        AnimOn = true;
        GunAnimator.SetBool(AnimBool_STRING, AnimOn);
    }
    public void SetBoolOff()
    {
        boolToggleTime = Time.time;
        AnimOn = false;
        GunAnimator.SetBool(AnimBool_STRING, AnimOn);
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
}