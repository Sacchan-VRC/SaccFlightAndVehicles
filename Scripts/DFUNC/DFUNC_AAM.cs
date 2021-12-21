
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class DFUNC_AAM : UdonSharpBehaviour
{
    [SerializeField] public UdonSharpBehaviour SAVControl;
    public Animator AAMAnimator;
    public int NumAAM = 6;
    [Tooltip("If target is within this angle of the direction the gun is aiming, it is lockable")]
    public float AAMLockAngle = 15;
    [Tooltip("AAM takes this long to lock before it can fire (seconds)")]
    public float AAMLockTime = 1.5f;
    [Tooltip("Minimum time between missile launches")]
    public float AAMLaunchDelay = 0;
    [Tooltip("How long it takes to fully reload from empty in seconds. Can be inaccurate because it can only reload by integers per resupply")]
    public float FullReloadTimeSec = 10;
    [Tooltip("Allow user to fire the weapon while the vehicle is on the ground taxiing?")]
    public bool AllowFiringWhenGrounded = false;
    [Tooltip("Send the boolean(AnimBoolName) true to the animator when selected?")]
    public bool DoAnimBool = false;
    [Tooltip("Animator bool that is true when this function is selected")]
    public string AnimBoolName = "AAMSelected";
    [Tooltip("Animator float that represents how many missiles are left")]
    public string AnimFloatName = "AAMs";
    [Tooltip("Animator trigger that is set when a missile is launched")]
    public string AnimFiredTriggerName = "aamlaunched";
    [Tooltip("Should the boolean stay true if the pilot exits with it selected?")]
    public bool AnimBoolStayTrueOnExit;
    [UdonSynced, FieldChangeCallback(nameof(AAMFire))] private ushort _AAMFire;
    public ushort AAMFire
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
            if (!Pilot)
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
    private float boolToggleTime;
    private bool AnimOn = false;
    [System.NonSerializedAttribute] public SaccEntity EntityControl;
    private bool UseLeftTrigger = false;
    [System.NonSerializedAttribute] public int FullAAMs;
    private int NumAAMTargets;
    private float AAMLockTimer = 0;
    private bool AAMHasTarget = false;
    [System.NonSerializedAttribute] public bool AAMLocked = false;
    private bool TriggerLastFrame;
    private float AAMLastFiredTime = -999;
    private float FullAAMsDivider;
    public GameObject AAM;
    public Transform AAMLaunchPoint;
    float TimeSinceSerialization;
    private bool func_active = false;
    private bool Pilot = false;
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
        InEditor = localPlayer == null;

        //HUD
        if (HUDControl)
        {
            distance_from_head = (float)HUDControl.GetProgramVariable("distance_from_head");
        }
        else
        { distance_from_head = 1.333f; }

        FindSelf();

        if (HUDText_AAM_ammo) { HUDText_AAM_ammo.text = NumAAM.ToString("F0"); }
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
        RequestSerialization();
    }
    public void SFEXT_G_PilotEnter()
    { gameObject.SetActive(true); AAMFire = 0; }
    public void SFEXT_G_PilotExit()
    { gameObject.SetActive(false); }
    public void SFEXT_O_PilotExit()
    {
        Pilot = false;
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
        if (HUDText_AAM_ammo) { HUDText_AAM_ammo.text = NumAAM.ToString("F0"); }
    }
    public void SFEXT_G_Explode()
    {
        NumAAM = FullAAMs;
        if (AAMAnimator) { AAMAnimator.SetFloat(AnimFloatName, 1); }
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
        if (AAMAnimator) { AAMAnimator.SetFloat(AnimFloatName, (float)NumAAM * FullAAMsDivider); }
        if (HUDText_AAM_ammo) { HUDText_AAM_ammo.text = NumAAM.ToString("F0"); }
    }
    public void SFEXT_G_RespawnButton()
    {
        NumAAM = FullAAMs;
        if (AAMAnimator) { AAMAnimator.SetFloat(AnimFloatName, 1); }
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
        func_active = true;
        AAMTargetIndicator.gameObject.SetActive(true);

        if (DoAnimBool && !AnimOn)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOn)); }
    }
    public void DFUNC_Deselected()
    {
        TriggerLastFrame = true;
        AAMTargeting.gameObject.SetActive(false);
        AAMTargetLock.gameObject.SetActive(false);
        AAMLockTimer = 0;
        AAMHasTarget = false;
        AAMLocked = false;
        AAMTargetIndicator.localRotation = Quaternion.identity;
        AAMTargetIndicator.localScale = Vector3.one;
        AAMTargetIndicator.gameObject.SetActive(false);
        func_active = false;

        if (DoAnimBool && AnimOn)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOff)); }
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
                        if (AAMLocked && Time.time - AAMLastFiredTime > AAMLaunchDelay)
                        {
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
    public UdonSharpBehaviour HUDControl;
    [Tooltip("Max distance an enemy can be targeted at")]
    public float AAMMaxTargetDistance = 6000;
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
    /* everywhere that GetComponent<SaccAirVehicle>() is used should be changed to UdonBehaviour for modularity's sake,
    but it seems that it's impossible until further udon/sharp updates, because it currently doesn't support checking if a variable exists before trying to get it */
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
                    bool LineOfSightNext = Physics.Raycast(HudControlPosition, AAMNextTargetDirection, out hitnext, 99999999, 133125 /* Default, Water, Environment, and Walkthrough */, QueryTriggerInteraction.Collide);

                    /*                 Debug.Log(string.Concat("LoS_next ", LineOfSightNext));
                                    if (hitnext.collider != null) Debug.Log(string.Concat("RayCastCorrectLayer_next ", (hitnext.collider.gameObject.layer == OutsidePlaneLayer)));
                                    if (hitnext.collider != null) Debug.Log(string.Concat("RayCastLayer_next ", hitnext.collider.gameObject.layer));
                                    Debug.Log(string.Concat("LowerAngle_next ", NextTargetAngle < AAMCurrentTargetAngle));
                                    Debug.Log(string.Concat("InAngle_next ", NextTargetAngle < 70));
                                    Debug.Log(string.Concat("BelowMaxDist_next ", NextTargetDistance < AAMMaxTargetDistance)); */

                    if ((LineOfSightNext
                        && hitnext.collider && hitnext.collider.gameObject.layer == OutsideVehicleLayer //did raycast hit an object on the layer planes are on?
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
                        AAMCurrentTargetSAVControl = NextTargetSAVControl;
                        AAMLockTimer = 0;
                        AAMTargetedTimer = .99f;//don't send targeted this frame incase new target is found next frame
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
            if (!LineOfSightCur || (hitcurrent.collider && hitcurrent.collider.gameObject.layer != OutsideVehicleLayer))
            { AAMTargetObscuredDelay += DeltaTime; }
            else
            { AAMTargetObscuredDelay = 0; }

            if ((!(bool)SAVControl.GetProgramVariable("Taxiing") || AllowFiringWhenGrounded)
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
                        if (AAMCurrentTargetSAVControl)
                        {
                            //target is a plane, send the 'targeted' event every second to make the target plane play a warning sound in the cockpit.
                            if (AAMTargetedTimer > 1)
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



    //hud stuff
    public Text HUDText_AAM_ammo;
    [Tooltip("Hud element to highlight current target")]
    public Transform AAMTargetIndicator;
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
        AAMLastFiredTime = Time.time;
        if (!InEditor) { IsOwner = localPlayer.IsOwner(gameObject); } else { IsOwner = true; }
        if (NumAAM > 0) { NumAAM--; }//so it doesn't go below 0 when desync occurs
        if (AAMAnimator) { AAMAnimator.SetTrigger(AnimFiredTriggerName); }
        if (AAM)
        {
            GameObject NewAAM = VRCInstantiate(AAM);
            NewAAM.transform.SetPositionAndRotation(AAMLaunchPoint.position, AAMLaunchPoint.transform.rotation);
            NewAAM.SetActive(true);
            NewAAM.GetComponent<Rigidbody>().velocity = (Vector3)SAVControl.GetProgramVariable("CurrentVel");
        }
        if (AAMAnimator) { AAMAnimator.SetFloat(AnimFloatName, (float)NumAAM * FullAAMsDivider); }
        if (HUDText_AAM_ammo) { HUDText_AAM_ammo.text = NumAAM.ToString("F0"); }
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
