
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class DFUNC_AAM : UdonSharpBehaviour
{
    [SerializeField] public UdonSharpBehaviour SAVControl;
    [SerializeField] private Animator AAMAnimator;
    [SerializeField] private int NumAAM = 6;
    [Tooltip("If target is within this angle of the direction the gun is aiming, it is lockable")]
    [SerializeField] private float AAMLockAngle = 15;
    [Tooltip("AAM takes this long to lock before it can fire (seconds)")]
    [SerializeField] private float AAMLockTime = 1.5f;
    [Tooltip("Minimum time between missile launches")]
    [SerializeField] private float AAMLaunchDelay = 0.5f;
    [Tooltip("How long it takes to fully reload from empty in seconds. Can be inaccurate because it can only reload by integers per resupply")]
    [SerializeField] private float FullReloadTimeSec = 10;
    [Tooltip("Set a boolean value in the animator when switching to this weapon?")]
    [SerializeField] private bool DoAnimBool = false;
    [Tooltip("Animator bool that is true when this function is selected")]
    [SerializeField] private string AnimBoolName = "AAMSelected";
    [Tooltip("Animator float that represents how many missiles are left")]
    [SerializeField] private string AnimFloatName = "AAMs";
    [Tooltip("Animator trigger that is set true when a missile is launched")]
    [SerializeField] private string AnimFiredTriggerName = "aamlaunched";
    [Tooltip("Should the boolean stay true if the pilot exits with it selected?")]
    [SerializeField] private bool AnimBoolStayTrueOnExit;
    [UdonSynced, FieldChangeCallback(nameof(AAMFire))] private short _AAMFire;
    public short AAMFire
    {
        set
        {
            _AAMFire = value;
            LaunchAAM();
        }
        get => _AAMFire;
    }
    private float boolToggleTime;
    private bool AnimOn = false;
    private SaccEntity EntityControl;
    private bool UseLeftTrigger = false;
    private int FullAAMs;
    private int NumAAMTargets;
    private float AAMLockTimer = 0;
    private bool AAMHasTarget = false;
    private bool AAMLocked = false;
    private bool TriggerLastFrame;
    private float AAMLastFiredTime = 0;
    private float FullAAMsDivider;
    public GameObject AAM;
    public Transform AAMLaunchPoint;
    float TimeSinceSerialization;
    private bool func_active = false;
    private bool Pilot = false;
    private bool OthersEnabled = false;
    public AudioSource AAMTargeting;
    public AudioSource AAMTargetLock;
    [System.NonSerializedAttribute] public bool IsOwner;
    [System.NonSerializedAttribute] public bool InEditor;
    private float reloadspeed;
    private bool LeftDial = false;
    private int DialPosition = -999;
    private VRCPlayerApi localPlayer;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXT_L_EntityStart()
    {
        FullAAMs = NumAAM;
        reloadspeed = FullAAMs / FullReloadTimeSec;
        FullAAMsDivider = 1f / (NumAAM > 0 ? NumAAM : 10000000);
        EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
        NumAAMTargets = EntityControl.NumAAMTargets;
        AAMTargets = EntityControl.AAMTargets;
        CenterOfMass = (Transform)EntityControl.CenterOfMass;
        VehicleTransform = EntityControl.transform;
        OutsideVehicleLayer = (int)SAVControl.GetProgramVariable("OutsideVehicleLayer");
        localPlayer = Networking.LocalPlayer;

        //HUD
        if (HUDControl)
        {
            distance_from_head = (float)HUDControl.GetProgramVariable("distance_from_head");
        }
        if (distance_from_head == 0) { distance_from_head = 1.333f; }

        FindSelf();

        HUDText_AAM_ammo.text = NumAAM.ToString("F0");
    }
    public void SFEXT_O_PilotEnter()
    {
        TriggerLastFrame = true;
        Pilot = true;
        HUDText_AAM_ammo.text = NumAAM.ToString("F0");
        //Make sure SAVeControl.AAMCurrentTargetSAVControl is correct
        var Target = AAMTargets[AAMTarget];
        if (Target && Target.transform.parent)
        {
            AAMCurrentTargetSAVControl = Target.transform.parent.GetComponent<SaccAirVehicle>();
        }
    }
    public void SFEXT_G_PilotExit()
    {
        if (OthersEnabled)
        { DisableForOthers(); }
        if (DoAnimBool && !AnimBoolStayTrueOnExit && AnimOn)
        { SetBoolOff(); }
    }
    public void SFEXT_O_PilotExit()
    {
        Pilot = false;
        gameObject.SetActive(false);
        AAMLockTimer = 0;
        AAMHasTarget = false;
        AAMLocked = false;
        func_active = false;
        AAMTargeting.gameObject.SetActive(false);
        AAMTargetLock.gameObject.SetActive(false);
        AAMTargetIndicator.localRotation = Quaternion.identity;
    }
    public void SFEXT_P_PassengerEnter()
    {
        HUDText_AAM_ammo.text = NumAAM.ToString("F0");
    }
    public void SFEXT_G_Explode()
    {
        NumAAM = FullAAMs;
        AAMAnimator.SetFloat(AnimFloatName, 1);
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
        { SAVControl.SetProgramVariable("ReSupplied", (int)SAVControl.GetProgramVariable("ReSupplied") + 1); }
        NumAAM = (int)Mathf.Min(NumAAM + Mathf.Max(Mathf.Floor(reloadspeed), 1), FullAAMs);
        AAMAnimator.SetFloat(AnimFloatName, (float)NumAAM * FullAAMsDivider);
        HUDText_AAM_ammo.text = NumAAM.ToString("F0");
    }
    public void SFEXT_G_RespawnButton()
    {
        NumAAM = FullAAMs;
        AAMAnimator.SetFloat(AnimFloatName, 1);
        if (DoAnimBool && AnimOn)
        { SetBoolOff(); }
    }
    public void SFEXT_G_TouchDown()
    {
        AAMLockTimer = 0;
        AAMTargetedTimer = 2;
    }
    public void DFUNC_Selected()
    {
        gameObject.SetActive(true);
        func_active = true;
        AAMTargetIndicator.gameObject.SetActive(true);
        if (!OthersEnabled)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EnableForOthers)); }

        if (DoAnimBool && !AnimOn)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOn)); }
    }
    public void DFUNC_Deselected()
    {
        TriggerLastFrame = true;
        gameObject.SetActive(false);
        AAMTargeting.gameObject.SetActive(false);
        AAMTargetLock.gameObject.SetActive(false);
        AAMLockTimer = 0;
        AAMHasTarget = false;
        AAMLocked = false;
        AAMTargetIndicator.localRotation = Quaternion.identity;
        AAMTargetIndicator.gameObject.SetActive(false);
        func_active = false;
        if (OthersEnabled)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(DisableForOthers)); }

        if (DoAnimBool && AnimOn)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOff)); }
    }
    //synced variables recieved while object is disabled do not get set until the object is enabled
    public void EnableForOthers()
    {
        if (!Pilot)
        {
            gameObject.SetActive(true);
        }
        OthersEnabled = true;
    }
    public void DisableForOthers()
    {
        if (!Pilot)
        {
            gameObject.SetActive(false);
        }
        OthersEnabled = false;
    }
    void Update()
    {
        if (func_active)
        {
            TimeSinceSerialization += Time.deltaTime;
            float Trigger;
            if (UseLeftTrigger)
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
            else
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

            if (NumAAMTargets != 0)
            {

                if (AAMLockTimer > AAMLockTime && AAMHasTarget) { AAMLocked = true; }
                else { AAMLocked = false; }

                //firing AAM
                if (Trigger > 0.75 || (Input.GetKey(KeyCode.Space)))
                {
                    if (!TriggerLastFrame)
                    {
                        if (AAMLocked && !(bool)SAVControl.GetProgramVariable("Taxiing") && Time.time - AAMLastFiredTime > AAMLaunchDelay)
                        {
                            AAMLastFiredTime = Time.time;
                            AAMFire++;//launch AAM using set
                            RequestSerialization();
                            if (NumAAM == 0) { AAMLockTimer = 0; AAMLocked = false; }
                            EntityControl.SendEventToExtensions("SFEXT_O_AAMLaunch");
                        }
                    }
                    TriggerLastFrame = true;
                }
                else TriggerLastFrame = false;
            }
            else { AAMLocked = false; AAMHasTarget = false; }


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

    //AAMTargeting
    [SerializeField] private UdonSharpBehaviour HUDControl;
    [Tooltip("Max distance an enemy can be targeted at")]
    [SerializeField] private float AAMMaxTargetDistance = 6000;
    [System.NonSerializedAttribute] public GameObject[] AAMTargets;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.None)] public int AAMTarget = 0;
    private int AAMTargetChecker = 0;
    [System.NonSerializedAttribute] public Transform CenterOfMass;
    private Transform VehicleTransform;
    private SaccAirVehicle AAMCurrentTargetSAVControl;
    private int OutsideVehicleLayer;
    private Vector3 AAMCurrentTargetDirection;
    private float AAMTargetedTimer = 2;
    private float AAMTargetObscuredDelay;
    private void FixedUpdate()//old AAMTargeting function
    {
        if (func_active)
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
                    bool LineOfSightNext = Physics.Raycast(HudControlPosition, AAMNextTargetDirection, out hitnext, 99999999, 133121 /* Default, Environment, and Walkthrough */, QueryTriggerInteraction.Ignore);

                    /*                 Debug.Log(string.Concat("LoS_next ", LineOfSightNext));
                                    if (hitnext.collider != null) Debug.Log(string.Concat("RayCastCorrectLayer_next ", (hitnext.collider.gameObject.layer == OutsidePlaneLayer)));
                                    if (hitnext.collider != null) Debug.Log(string.Concat("RayCastLayer_next ", hitnext.collider.gameObject.layer));
                                    Debug.Log(string.Concat("LowerAngle_next ", NextTargetAngle < AAMCurrentTargetAngle));
                                    Debug.Log(string.Concat("InAngle_next ", NextTargetAngle < 70));
                                    Debug.Log(string.Concat("BelowMaxDist_next ", NextTargetDistance < AAMMaxTargetDistance)); */

                    if ((LineOfSightNext
                        && hitnext.collider.gameObject.layer == OutsideVehicleLayer //did raycast hit an object on the layer planes are on?
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
                        AAMTargetedTimer = .9f;//send targeted .1s after targeting so it can't get spammed too fast (and doesnt send if you instantly target something else)
                        RequestSerialization();
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
            bool LineOfSightCur = Physics.Raycast(HudControlPosition, AAMCurrentTargetDirection, out hitcurrent, 99999999, 133121 /* Default, Environment, and Walkthrough */, QueryTriggerInteraction.Ignore);
            //used to make lock remain for .25 seconds after target is obscured
            if (LineOfSightCur == false || hitcurrent.collider.gameObject.layer != OutsideVehicleLayer)
            { AAMTargetObscuredDelay += DeltaTime; }
            else
            { AAMTargetObscuredDelay = 0; }

            if (!(bool)SAVControl.GetProgramVariable("Taxiing")
                && (AAMTargetObscuredDelay < .25f)
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
                        //give enemy radar lock even if you're out of missiles
                        if (AAMCurrentTargetSAVControl)
                        {
                            //target is a plane, send the 'targeted' event every second to make the target plane play a warning sound in the cockpit.
                            AAMTargetedTimer += DeltaTime;
                            if (AAMTargetedTimer > 1)
                            {
                                AAMTargetedTimer = 0;
                                AAMCurrentTargetSAVControl.EntityControl.SendEventToExtensions("SFEXT_L_AAMTargeted");
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



    //hud stuff
    [SerializeField] private Text HUDText_AAM_ammo;
    [Tooltip("Hud element to highlight current target")]
    [SerializeField] private Transform AAMTargetIndicator;
    private float distance_from_head;
    private void Hud()
    {
        //AAM Target Indicator
        if (AAMHasTarget)//GUN or AAM
        {
            AAMTargetIndicator.localScale = Vector3.one;
            AAMTargetIndicator.position = HUDControl.transform.position + AAMCurrentTargetDirection;
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
    public void LaunchAAM()
    {
        InEditor = (bool)SAVControl.GetProgramVariable("InEditor");
        if (!InEditor) { IsOwner = localPlayer.IsOwner(gameObject); } else { IsOwner = true; }
        if (NumAAM > 0) { NumAAM--; }//so it doesn't go below 0 when desync occurs
        AAMAnimator.SetTrigger(AnimFiredTriggerName);
        if (AAM)
        {
            GameObject NewAAM = Object.Instantiate(AAM);
            NewAAM.transform.SetPositionAndRotation(AAMLaunchPoint.position, AAMLaunchPoint.transform.rotation);
            NewAAM.SetActive(true);
            NewAAM.GetComponent<Rigidbody>().velocity = (Vector3)SAVControl.GetProgramVariable("CurrentVel");
        }
        AAMAnimator.SetFloat(AnimFloatName, (float)NumAAM * FullAAMsDivider);
        HUDText_AAM_ammo.text = NumAAM.ToString("F0");
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
    public void SetBoolOn()
    {
        boolToggleTime = Time.time;
        AnimOn = true;
        AAMAnimator.SetBool(AnimBoolName, AnimOn);
    }
    public void SetBoolOff()
    {
        boolToggleTime = Time.time;
        AnimOn = false;
        AAMAnimator.SetBool(AnimBoolName, AnimOn);
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
