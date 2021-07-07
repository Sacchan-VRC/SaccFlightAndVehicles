
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Gun : UdonSharpBehaviour
{
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private Animator Animator;
    [SerializeField] private Transform GunRecoilEmpty;
    [SerializeField] private Transform GunDamageParticle;
    [SerializeField] private GameObject HudCrosshairGun;
    [SerializeField] private GameObject HudCrosshair;
    [SerializeField] private bool UseLeftTrigger = false;
    [SerializeField] private float FullReloadTimeSec = 20;
    [SerializeField] private float FullGunAmmoInSeconds = 12;
    [UdonSynced(UdonSyncMode.None)] private float gunammo = 12;
    private float GunRecoil;
    private Rigidbody VehicleRigidbody;
    private bool RTriggerLastFrame;
    private bool GunRecoilEmptyNULL = true;
    private float timesinceupdate = 0;
    private bool firing;
    private int GUNFIRING_STRING = Animator.StringToHash("gunfiring");
    private int GUNAMMO_STRING = Animator.StringToHash("gunammo");
    private bool Passenger;
    private float FullGunAmmoDivider;
    private bool active = false;
    private float reloadspeed;
    private bool Initialized = false;
    public void Update()
    {
        if (!Passenger && active)
        {
            float DeltaTime = Time.deltaTime;
            timesinceupdate += DeltaTime;
            float Trigger;
            if (UseLeftTrigger)
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
            else
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
            if ((Trigger > 0.75 || (Input.GetKey(KeyCode.Space))) && gunammo > 0)
            {
                if (!firing)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "GunStartFiring");
                    firing = true;
                }
                gunammo = Mathf.Max(gunammo - DeltaTime, 0);
                if (GunRecoilEmptyNULL)
                {
                    VehicleRigidbody.AddRelativeForce(-Vector3.forward * GunRecoil * Time.smoothDeltaTime);
                }
                else
                {
                    VehicleRigidbody.AddForceAtPosition(-GunRecoilEmpty.forward * GunRecoil * .01f/* so the strength is in the same range as above*/, GunRecoilEmpty.position, ForceMode.Force);
                }
                RTriggerLastFrame = true;
            }
            else
            {
                if (firing)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "GunStopFiring");
                    firing = false;
                    RTriggerLastFrame = false;
                }
            }
            if (timesinceupdate > 1f)
            {
                timesinceupdate = 0;
                RequestSerialization();
            }
            Hud();
        }
        Animator.SetFloat(GUNAMMO_STRING, gunammo * FullGunAmmoDivider);
    }
    public void DFUNC_Selected()
    {
        gameObject.SetActive(true);
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetActive");
    }
    public void DFUNC_Deselected()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetInactive");
    }
    public void SFEXT_PassengerEnter()
    {
        if (EngineControl.Passenger && active)
        { SetActive(); }
    }
    public void SFEXT_PassengerExit()
    {
        gameObject.SetActive(false);
    }
    public void SFEXT_PilotEnter()
    {
        GunDamageParticle.gameObject.SetActive(true);
    }
    public void SFEXT_PilotExit()
    {
        firing = false;
        RTriggerLastFrame = false;
        RequestSerialization();
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetInactive");
        gameObject.SetActive(false);
        GunDamageParticle.gameObject.SetActive(false);
    }
    public void SFEXT_ReSupply()
    {
        gunammo = Mathf.Min(gunammo + reloadspeed, FullGunAmmoInSeconds);
        //enginecontrol.resupplyint +=1;
    }
    public void SetActive()
    {
        HudCrosshairGun.SetActive(true);
        HudCrosshair.SetActive(false);
        active = true;
        if (EngineControl.Passenger)
        { gameObject.SetActive(true); }
        if (!Initialized)
        { Initialize(); }
    }
    public void SetInactive()
    {
        HudCrosshairGun.SetActive(false);
        HudCrosshair.SetActive(true);
        AAMTargetIndicator.gameObject.SetActive(false);
        GUNLeadIndicator.gameObject.SetActive(false);
        Animator.SetBool(GUNFIRING_STRING, false);
        active = false;
        gameObject.SetActive(false);
    }
    public void SFEXT_TakeOwnership()
    {
        if (!EngineControl.Piloting)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetInactive");
        }
    }
    public void GunStartFiring()
    {
        Animator.SetBool(GUNFIRING_STRING, true);
    }
    public void GunStopFiring()
    {
        Animator.SetBool(GUNFIRING_STRING, false);
    }
    private void Initialize()
    {
        reloadspeed = FullGunAmmoInSeconds / FullReloadTimeSec;

        //Targeting
        VehicleRigidbody = EngineControl.VehicleMainObj.GetComponent<Rigidbody>();
        GunRecoil = EngineControl.GunRecoil;
        GunRecoilEmptyNULL = GunRecoilEmpty == null;
        FullGunAmmoDivider = 1f / (FullGunAmmoInSeconds > 0 ? FullGunAmmoInSeconds : 10000000);
        AAMTargets = EngineControl.AAMTargets;
        NumAAMTargets = EngineControl.NumAAMTargets;
        VehicleTransform = EngineControl.VehicleMainObj.transform;
        HUDControl = EngineControl.HUDControl;
        CenterOfMass = EngineControl.CenterOfMass;
        OutsidePlaneLayer = LayerMask.NameToLayer("Walkthrough");

        //HUD
        distance_from_head = HUDControl.distance_from_head;


        Initialized = true;
    }
    private GameObject[] AAMTargets;
    private Transform VehicleTransform;
    private int AAMTarget;
    private int AAMTargetChecker;
    private HUDController HUDControl;
    private Transform CenterOfMass;
    private EngineController AAMCurrentTargetEngineControl;
    private int OutsidePlaneLayer;
    [SerializeField] private float AAMMaxTargetDistance = 6000;
    private float AAMLockTimer;
    private float AAMTargetedTimer;
    private int NumAAMTargets;
    private Vector3 AAMCurrentTargetDirection;
    private float AAMTargetObscuredDelay;
    private bool AAMHasTarget;
    private void FixedUpdate()//this is just the old  AAMTargeting adjusted slightly
    //there's some unnecessary stuff in here because it doesn't need to do missile related stuff any more 
    {
        float DeltaTime = Time.fixedDeltaTime;
        var AAMCurrentTargetPosition = AAMTargets[AAMTarget].transform.position;
        float AAMCurrentTargetAngle = Vector3.Angle(VehicleTransform.forward, (AAMCurrentTargetPosition - CenterOfMass.position));
        Vector3 HudControlPosition = HUDControl.transform.position;

        //check 1 target per frame to see if it's infront of us and worthy of being our current target
        var TargetChecker = AAMTargets[AAMTargetChecker];
        var TargetCheckerTransform = TargetChecker.transform;
        var TargetCheckerParent = TargetCheckerTransform.parent;

        Vector3 AAMNextTargetDirection = (TargetCheckerTransform.position - HudControlPosition);
        float NextTargetAngle = Vector3.Angle(VehicleTransform.forward, AAMNextTargetDirection);
        float NextTargetDistance = Vector3.Distance(CenterOfMass.position, TargetCheckerTransform.position);
        bool AAMCurrentTargetEngineControlNull = AAMCurrentTargetEngineControl == null ? true : false;

        if (TargetChecker.activeInHierarchy)
        {
            EngineController NextTargetEngineControl = null;

            if (TargetCheckerParent)
            {
                NextTargetEngineControl = TargetCheckerParent.GetComponent<EngineController>();
            }
            //if target EngineController is null then it's a dummy target (or hierarchy isn't set up properly)
            if ((!NextTargetEngineControl || (!NextTargetEngineControl.Taxiing && !NextTargetEngineControl.dead)))
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
                                && NextTargetDistance < AAMMaxTargetDistance
                                    || ((!AAMCurrentTargetEngineControlNull && AAMCurrentTargetEngineControl.Taxiing)//prevent being unable to switch target if it's angle is higher than your current target and your current target happens to be taxiing and is therefore untargetable
                                        || !AAMTargets[AAMTarget].activeInHierarchy))//same as above but if the target is destroyed
                {
                    //found new target
                    AAMCurrentTargetAngle = NextTargetAngle;
                    AAMTarget = AAMTargetChecker;
                    AAMCurrentTargetPosition = AAMTargets[AAMTarget].transform.position;
                    AAMCurrentTargetEngineControl = NextTargetEngineControl;
                    AAMLockTimer = 0;
                    AAMTargetedTimer = .6f;//give the synced variable(AAMTarget) time to update before sending targeted
                    AAMCurrentTargetEngineControlNull = AAMCurrentTargetEngineControl == null ? true : false;
                    if (HUDControl != null)
                    {
                        HUDControl.RelativeTargetVelLastFrame = Vector3.zero;
                        HUDControl.GUN_TargetSpeedLerper = 0f;
                        HUDControl.GUN_TargetDirOld = AAMNextTargetDirection * 1.00001f; //so the difference isn't 0
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
        if (AAMCurrentTargetEngineControlNull)
        { AAMCurrentTargetDirection = AAMCurrentTargetPosition - HudControlPosition; }
        else
        { AAMCurrentTargetDirection = AAMCurrentTargetEngineControl.CenterOfMass.position - HudControlPosition; }
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

        if (!EngineControl.Taxiing
            && (AAMTargetObscuredDelay < .25f)
                && AAMCurrentTargetDistance < AAMMaxTargetDistance
                    && AAMTargets[AAMTarget].activeInHierarchy
                        && (AAMCurrentTargetEngineControlNull || (!AAMCurrentTargetEngineControl.Taxiing && !AAMCurrentTargetEngineControl.dead)))
        {
            if ((AAMTargetObscuredDelay < .25f) && AAMCurrentTargetDistance < AAMMaxTargetDistance)
            {
                AAMHasTarget = true;
                if (AAMCurrentTargetAngle < 70)//lock angle
                {
                    AAMLockTimer += DeltaTime;
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
    [SerializeField] private Transform AAMTargetIndicator;
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
        if (AAMHasTarget && active)//GUN or AAM
        {
            AAMTargetIndicator.gameObject.SetActive(true);
            AAMTargetIndicator.position = transform.position + AAMCurrentTargetDirection;
            AAMTargetIndicator.localPosition = AAMTargetIndicator.localPosition.normalized * distance_from_head;
        }
        else AAMTargetIndicator.gameObject.SetActive(false);



        //GUN Lead Indicator
        if (AAMHasTarget && active)
        {
            GUNLeadIndicator.gameObject.SetActive(true);
            Vector3 TargetDir;
            if (AAMCurrentTargetEngineControl == null)//target is a dummy target
            { TargetDir = AAMTargets[AAMTarget].transform.position - transform.position; }
            else
            { TargetDir = CenterOfMass.position - transform.position; }
            GUN_TargetDirOld = Vector3.Lerp(GUN_TargetDirOld, TargetDir, .2f);

            Vector3 RelativeTargetVel = TargetDir - GUN_TargetDirOld;
            float BulletPlusPlaneSpeed = (EngineControl.CurrentVel + (VehicleTransform.forward * BulletSpeed) - (RelativeTargetVel * .25f)).magnitude;
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
            GUNLeadIndicator.position = transform.position + PredictedPos;
            GUNLeadIndicator.localPosition = GUNLeadIndicator.localPosition.normalized * distance_from_head;

            RelativeTargetVelLastFrame = RelativeTargetVel;
        }
        else GUNLeadIndicator.gameObject.SetActive(false);
        /////////////////
    }
}



//soundcontroller
/*         if (!GunSoundNull)
        {
            if (EngineControl.IsFiringGun && !silent)
            {
                GunSound.pitch = Mathf.Min(Doppler, 2f);
                if (!GunSound.isPlaying)
                {
                    GunSound.Play();
                }
                if (Doppler > 50f)
                {
                    GunSound.volume = Mathf.Lerp(GunSound.volume, 0, 3f * DeltaTime);
                }
                else
                {
                    GunSound.volume = Mathf.Lerp(GunSound.volume, GunSoundInitialVolume, 9f * DeltaTime);
                }
            }
            else if (!EngineControl.IsFiringGun || silent && GunSound.isPlaying)
            {
                GunSound.Stop();
            }
        } */

