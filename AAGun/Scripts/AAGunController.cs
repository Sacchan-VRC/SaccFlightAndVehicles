
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AAGunController : UdonSharpBehaviour
{
    public GameObject Rotator;
    public GameObject VehicleMainObj;
    public VRCStation AAGunSeatStation;
    public HUDControllerAAGun HUDControl;
    public Camera AACam;
    public bool HasZoom = true;
    public bool HasHUD = true;
    public bool HasMissiles = true;
    public float TurnSpeedMulti = 10;
    public float TurningResponse = .2f;
    public float StopSpeed = .95f;
    public float ZoomFov = .1f;
    public float ZoomOutFov = 110f;
    public float MissileReloadTime;
    [UdonSynced(UdonSyncMode.None)] public float Health = 100f;
    public GameObject AAM;
    public int NumAAM = 6;
    public float AAMMaxTargetDistance = 6000;
    public float AAMLockAngle = 15;
    public float AAMLockTime = 1.5f;
    public Transform AAMLaunchPoint;
    public LayerMask AAMTargetsLayer;
    public float PlaneHitBoxLayer = 17;//walkthrough
    public AudioSource AAMLocking;
    public AudioSource AAMLockedOn;
    public Transform JoyStick;
    private Animator AAGunAnimator;
    [System.NonSerializedAttribute] public bool dead;

    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.None)] public bool firing;
    [System.NonSerializedAttribute] public float FullHealth;
    [System.NonSerializedAttribute] public bool Manning;//like Piloting in the plane
    [System.NonSerializedAttribute] public VRCPlayerApi localPlayer;
    [System.NonSerializedAttribute] public float InputXLerper = 0f;
    [System.NonSerializedAttribute] public float InputYLerper = 0f;
    private Vector3 StartRot;
    private float RstickV;
    private float ZoomLevel;
    [System.NonSerializedAttribute] public bool InEditor = true;
    [System.NonSerializedAttribute] public bool IsOwner = false;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.None)] public int AAMTarget = 0;
    [System.NonSerializedAttribute] public GameObject[] AAMTargets = new GameObject[80];
    [System.NonSerializedAttribute] public int NumAAMTargets = 0;
    private int AAMTargetChecker = 0;
    [System.NonSerializedAttribute] public bool AAMHasTarget = false;
    private float AAMTargetedTimer = 2f;
    [System.NonSerializedAttribute] public bool AAMLocked = false;
    [System.NonSerializedAttribute] public float AAMLockTimer = 0;
    private float AAMLastFiredTime;
    [System.NonSerializedAttribute] public Vector3 AAMCurrentTargetDirection;
    [System.NonSerializedAttribute] public EngineController AAMCurrentTargetEngineControl;
    private float AAMTargetObscuredDelay;
    private bool InVR;
    Quaternion AAGunRotLastFrame;
    Quaternion JoystickZeroPoint;
    [System.NonSerializedAttribute] public bool RGripLastFrame = false;
    Vector2 VRPitchRollInput;
    private float FullAAMsDivider;
    private bool LTriggerLastFrame;
    private bool DoAAMTargeting;
    [System.NonSerializedAttribute] public int FullAAMs;
    [System.NonSerializedAttribute] public float ReloadTimer;
    private bool JoyStickNull = true;
    void Start()
    {
        localPlayer = Networking.LocalPlayer;
        if (localPlayer == null) { InEditor = true; Manning = true; IsOwner = true; }
        else
        {
            InEditor = false;
            if (localPlayer.IsUserInVR()) { InVR = true; }
        }

        Assert(Rotator != null, "Start: Rotator != null");
        Assert(VehicleMainObj != null, "Start: VehicleMainObj != null");
        Assert(AAGunSeatStation != null, "Start: AAGunSeatStation != null");
        Assert(AACam != null, "Start: AACam != null");
        Assert(AAM != null, "Start: AAM != null");
        Assert(AAMLocking != null, "Start: AAMLocking != null");
        Assert(AAMLockedOn != null, "Start: AAMLockedOn != null");
        Assert(JoyStick != null, "Start: JoyStick != null");



        if (JoyStick != null) { JoyStickNull = false; }

        if (VehicleMainObj != null) { AAGunAnimator = VehicleMainObj.GetComponent<Animator>(); }
        FullHealth = Health;
        StartRot = Rotator.transform.localRotation.eulerAngles;
        if (StopSpeed > 1) StopSpeed = 1;//stops instantly

        FullAAMsDivider = 1f / NumAAM;
        FullAAMs = NumAAM;

        //get array of AAM Targets
        RaycastHit[] aamtargs = Physics.SphereCastAll(VehicleMainObj.transform.position, 1000000, VehicleMainObj.transform.forward, 5, AAMTargetsLayer, QueryTriggerInteraction.Collide);
        int n = 0;

        //work out which index in the aamtargs array is our own plane by finding which one has this script as it's parent
        //allows for each team to have a different layer for AAMTargets
        int self = -1;
        n = 0;
        foreach (RaycastHit target in aamtargs)
        {
            if (target.transform.parent != null && target.transform.parent == transform)
            {
                self = n;
            }
            n++;
        }
        //populate AAMTargets list excluding our own plane
        n = 0;
        int foundself = 0;
        foreach (RaycastHit target in aamtargs)
        {
            if (n == self) { foundself = 1; n++; }
            else
            {
                AAMTargets[n - foundself] = target.collider.gameObject;
                n++;
            }
        }
        if (aamtargs.Length > 0)
        {
            if (foundself != 0)
            {
                NumAAMTargets = Mathf.Clamp(aamtargs.Length - 1, 0, 999);//one less because it found our own plane
            }
            else
            {
                NumAAMTargets = Mathf.Clamp(aamtargs.Length, 0, 999);
            }
        }
        else { NumAAMTargets = 0; }


        if (NumAAMTargets > 0)
        {
            n = 0;
            //create a unique number based on position in the hierarchy in order to sort the AAMTargets array later, to make sure they're the in the same order on all clients 
            float[] order = new float[NumAAMTargets];
            for (int i = 0; AAMTargets[n] != null; i++)
            {
                Transform parent = AAMTargets[n].transform;
                for (int x = 0; parent != null; x++)
                {
                    order[n] = float.Parse(order[n].ToString() + parent.transform.GetSiblingIndex().ToString());
                    parent = parent.transform.parent;
                }
                n++;
            }
            //sort AAMTargets array based on order
            SortTargets(AAMTargets, order);
        }
        else
        {
            AAMTargets[0] = gameObject;//this should prevent HUDController from crashing with a null reference while causing no ill effects
        }
    }
    void Update()
    {
        float DeltaTime = Time.deltaTime;
        if (!InEditor) { IsOwner = localPlayer.IsOwner(VehicleMainObj); }
        else { IsOwner = true; }
        if (IsOwner)
        {
            if (Health <= 0)
            {
                if (InEditor)
                {
                    Explode();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Explode");
                }
            }
            if (Manning)
            {
                //get inputs
                int Wf = Input.GetKey(KeyCode.W) ? 1 : 0; //inputs as ints
                int Sf = Input.GetKey(KeyCode.S) ? -1 : 0;
                int Af = Input.GetKey(KeyCode.A) ? -1 : 0;
                int Df = Input.GetKey(KeyCode.D) ? 1 : 0;

                float LstickV = 0;
                float RGrip = 0;
                float RTrigger = 0;
                float LTrigger = 0;
                if (!InEditor)
                {
                    RstickV = -Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
                    LstickV = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
                    RTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
                    LTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
                    RGrip = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
                }
                Vector3 JoystickPos;

                //virtual joystick
                if (InVR)
                {
                    if (RGrip > 0.75)
                    {
                        Quaternion PlaneRotDif = Rotator.transform.rotation * Quaternion.Inverse(AAGunRotLastFrame);//difference in plane's rotation since last frame
                        JoystickZeroPoint = PlaneRotDif * JoystickZeroPoint;//zero point rotates with the plane so it appears still to the pilot
                        if (!RGripLastFrame)//first frame you gripped joystick
                        {
                            PlaneRotDif = Quaternion.identity;
                            JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;//rotation of the controller relative to the plane when it was pressed
                        }
                        //difference between the plane and the hand's rotation, and then the difference between that and the JoystickZeroPoint
                        Quaternion JoystickDifference = (Quaternion.Inverse(Rotator.transform.rotation) * localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation) * Quaternion.Inverse(JoystickZeroPoint);
                        JoystickPos = (JoystickDifference * Rotator.transform.up);
                        VRPitchRollInput = new Vector2(JoystickPos.x, JoystickPos.z) * 1.41421f;

                        RGripLastFrame = true;
                        //making a circular joy stick square
                        //pitch and roll
                        if (Mathf.Abs(VRPitchRollInput.x) > Mathf.Abs(VRPitchRollInput.y))
                        {
                            if (Mathf.Abs(VRPitchRollInput.x) > 0)
                            {
                                float temp = VRPitchRollInput.magnitude / Mathf.Abs(VRPitchRollInput.x);
                                VRPitchRollInput *= temp;
                            }
                        }
                        else if (Mathf.Abs(VRPitchRollInput.y) > 0)
                        {
                            float temp = VRPitchRollInput.magnitude / Mathf.Abs(VRPitchRollInput.y);
                            VRPitchRollInput *= temp;
                        }
                    }
                    else
                    {
                        VRPitchRollInput = Vector3.zero;
                        RGripLastFrame = false;
                    }
                    AAGunRotLastFrame = Rotator.transform.rotation;
                }

                float InputY = Mathf.Clamp((VRPitchRollInput.x + Af + Df), -1, 1) * TurnSpeedMulti;
                float InputX = Mathf.Clamp((VRPitchRollInput.y + Wf + Sf), -1, 1) * TurnSpeedMulti;

                if (HasZoom)
                {
                    //Camera control
                    if (AACam != null) { ZoomLevel = AACam.fieldOfView / 90; }
                    if (Mathf.Abs(LstickV) > .1)
                    {
                        if (AACam != null) { AACam.fieldOfView = Mathf.Clamp(AACam.fieldOfView - 3.2f * LstickV * ZoomLevel, ZoomFov, ZoomOutFov); }
                    }
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        if (AACam != null) { AACam.fieldOfView = Mathf.Clamp(AACam.fieldOfView - 1.6f * ZoomLevel, ZoomFov, ZoomOutFov); }
                    }
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        if (AACam != null) { AACam.fieldOfView = Mathf.Clamp(AACam.fieldOfView + 1.6f * ZoomLevel, ZoomFov, ZoomOutFov); }
                    }
                }



                //only do friction if slowing down or trying to turn in the oposite direction
                if (InputY > 0 && InputYLerper < 0 || InputY < 0 && InputYLerper > 0 || Mathf.Abs(InputYLerper) > Mathf.Abs(InputY))
                {
                    InputYLerper = Mathf.Lerp(InputYLerper, InputY, TurningResponse * DeltaTime);
                    InputYLerper *= StopSpeed;
                }
                else { InputYLerper = Mathf.Lerp(InputYLerper, InputY, TurningResponse * DeltaTime); }

                if (InputX > 0 && InputXLerper < 0 || InputX < 0 && InputXLerper > 0 || Mathf.Abs(InputXLerper) > Mathf.Abs(InputX))
                {
                    InputXLerper = Mathf.Lerp(InputXLerper, InputX, TurningResponse * DeltaTime);
                    InputXLerper *= StopSpeed;
                }
                else { InputXLerper = Mathf.Lerp(InputXLerper, InputX, TurningResponse * DeltaTime); }


                float temprot = Rotator.transform.localRotation.eulerAngles.x;
                temprot += InputXLerper * ZoomLevel;
                if (temprot > 180) { temprot -= 360; }
                temprot = Mathf.Clamp(temprot, -89, 35);
                Rotator.transform.localRotation = Quaternion.Euler(new Vector3(temprot, Rotator.transform.localRotation.eulerAngles.y + (InputYLerper * ZoomLevel), 0));

                //Firing the gun
                if (RTrigger >= 0.75 || Input.GetKey(KeyCode.Space))
                {
                    firing = true;
                }
                else { firing = false; }

                if (HasHUD)
                {
                    DoAAMTargeting = true;

                    if (HasMissiles)
                    {
                        if (NumAAMTargets != 0)
                        {

                            if (AAMLockTimer > AAMLockTime && AAMHasTarget) AAMLocked = true;
                            else { AAMLocked = false; }

                            //firing AAM
                            if (LTrigger > 0.75 || (Input.GetKey(KeyCode.C)))
                            {
                                if (!LTriggerLastFrame)
                                {
                                    if (AAMLocked && Time.time - AAMLastFiredTime > 0.5)
                                    {
                                        AAMLastFiredTime = Time.time;
                                        if (InEditor)
                                        { LaunchAAM(); }
                                        else
                                        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchAAM"); }
                                        if (NumAAM == 0) { AAMLockTimer = 0; AAMLocked = false; }
                                    }
                                }
                                LTriggerLastFrame = true;
                            }
                            else LTriggerLastFrame = false;
                        }
                        else
                        {
                            firing = false;
                        }
                        //reloading AAMs
                        if (NumAAM == FullAAMs)
                        { ReloadTimer = 0; }
                        else if (NumAAM < FullAAMs)
                        { ReloadTimer += DeltaTime; }
                        if (ReloadTimer > MissileReloadTime)
                        {
                            if (InEditor)
                            { ReloadAAM(); }
                            else
                            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ReloadAAM"); }
                        }
                    }
                }
                else
                {
                    DoAAMTargeting = false;
                }

                //joystick movement
                if (!JoyStickNull)
                {
                    JoyStick.localRotation = Quaternion.Euler(new Vector3(InputX * 35f, 0, InputY * 35f));
                }
            }
        }
        if (firing)
        {
            AAGunAnimator.SetBool("firing", true);
        }
        else
        {
            AAGunAnimator.SetBool("firing", false);
        }
        if (!dead)
        {
            AAGunAnimator.SetFloat("health", Health / FullHealth);
        }
        else
        {
            AAGunAnimator.SetFloat("health", 1);//dead, set animator health to full so that there's no phantom healthsmoke
        }

    }
    private void FixedUpdate()
    {
        if (IsOwner)
        {
            if (DoAAMTargeting)
            {
                AAMTargeting(AAMLockAngle);
            }
        }
    }
    public void Explode()//all the things players see happen when the vehicle explodes
    {
        if (Manning)
        {
            if (AAGunSeatStation != null) { AAGunSeatStation.ExitStation(localPlayer); }
        }
        dead = true;
        AAGunAnimator.SetTrigger("explode");
        Health = FullHealth;//turns off low health smoke and stops it from calling Explode() every frame
        if (IsOwner)
        {
            Rotator.transform.localRotation = Quaternion.Euler(StartRot);
        }
    }
    private void AAMTargeting(float Lock_Angle)
    {
        float DeltaTime = Time.deltaTime;
        var AAMCurrentTargetPosition = AAMTargets[AAMTarget].transform.position;
        Vector3 HudControlPosition = HUDControl.transform.position;
        float AAMCurrentTargetAngle = Vector3.Angle(Rotator.transform.forward, (AAMCurrentTargetPosition - HudControlPosition));

        //check 1 target per frame to see if it's infront of us and worthy of being our current target
        var TargetChecker = AAMTargets[AAMTargetChecker];
        var TargetCheckerTransform = TargetChecker.transform;
        var TargetCheckerParent = TargetCheckerTransform.parent;

        Vector3 AAMNextTargetDirection = (TargetCheckerTransform.position - HudControlPosition);
        float NextTargetAngle = Vector3.Angle(Rotator.transform.forward, AAMNextTargetDirection);
        float NextTargetDistance = Vector3.Distance(HudControlPosition, TargetCheckerTransform.position);
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
                bool LineOfSightNext = Physics.Raycast(HudControlPosition, AAMNextTargetDirection, out hitnext, Mathf.Infinity, 133121 /* Default, Environment, and Walkthrough */, QueryTriggerInteraction.Ignore);

                /* Debug.Log(string.Concat("LoS ", LineOfSightNext));
                Debug.Log(string.Concat("RayCastLayer ", hitnext.collider.gameObject.layer == PlaneHitBoxLayer));
                Debug.Log(string.Concat("InAngle ", NextTargetAngle < Lock_Angle));
                Debug.Log(string.Concat("BelowMaxDist ", NextTargetDistance < AAMMaxTargetDistance));
                Debug.Log(string.Concat("LowerAngle ", NextTargetAngle < AAMCurrentTargetAngle));
                Debug.Log(string.Concat("CurrentTargTaxiing ", !AAMCurrentTargetEngineControlNull && AAMCurrentTargetEngineControl.Taxiing)); */
                if ((LineOfSightNext
                    && hitnext.collider.gameObject.layer == PlaneHitBoxLayer
                        && NextTargetAngle < Lock_Angle
                            && NextTargetDistance < AAMMaxTargetDistance
                                && NextTargetAngle < AAMCurrentTargetAngle)
                                    || (!AAMCurrentTargetEngineControlNull && AAMCurrentTargetEngineControl.Taxiing)) //prevent being unable to target next target if it's angle is higher than your current target and your current target happens to be taxiing and is therefore untargetable
                {
                    //found new target
                    AAMCurrentTargetAngle = NextTargetAngle;
                    AAMTarget = AAMTargetChecker;
                    AAMCurrentTargetPosition = AAMTargets[AAMTarget].transform.position;
                    AAMCurrentTargetEngineControl = NextTargetEngineControl;
                    AAMLockTimer = 0;
                    AAMTargetedTimer = .6f;//give the synced variable time to update before sending targeted
                    AAMCurrentTargetEngineControlNull = AAMCurrentTargetEngineControl == null ? true : false;
                    if (HUDControl != null)
                    {
                        HUDControl.GUN_TargetSpeedLerper = 0f;
                        HUDControl.GUN_TargetDirOld = AAMNextTargetDirection * 1.00001f; //so the difference isn't 0
                    }
                }
            }
        }
        //increase target checker ready for next frame
        AAMTargetChecker++;
        if (AAMTargetChecker == AAMTarget && AAMTarget == NumAAMTargets - 1)
            AAMTargetChecker = 0;
        else if (AAMTargetChecker == AAMTarget)
            AAMTargetChecker++;
        else if (AAMTargetChecker > NumAAMTargets - 1)
            AAMTargetChecker = 0;

        //if target is currently in front of plane, lock onto it
        if (AAMCurrentTargetEngineControlNull)
        { AAMCurrentTargetDirection = AAMCurrentTargetPosition - HudControlPosition; }
        else
        { AAMCurrentTargetDirection = AAMCurrentTargetEngineControl.CenterOfMass.position - HudControlPosition; }
        float AAMCurrentTargetDistance = AAMCurrentTargetDirection.magnitude;
        //check if target is active, and if it's enginecontroller is null(dummy target), or if it's not null(plane) make sure it's not taxiing or dead.
        //raycast to check if it's behind something
        RaycastHit hitcurrent;
        bool LineOfSightCur = Physics.Raycast(HudControlPosition, AAMCurrentTargetDirection, out hitcurrent, Mathf.Infinity, 133121 /* Default, Environment, and Walkthrough */, QueryTriggerInteraction.Ignore);
        //used to make lock remain for .25 seconds after target is obscured
        if (LineOfSightCur == false || hitcurrent.collider.gameObject.layer != PlaneHitBoxLayer)
        { AAMTargetObscuredDelay += DeltaTime; }
        else
        { AAMTargetObscuredDelay = 0; }
        if (AAMTargets[AAMTarget].activeInHierarchy
                && (AAMCurrentTargetEngineControlNull || (!AAMCurrentTargetEngineControl.Taxiing && !AAMCurrentTargetEngineControl.dead)))
        {
            if ((AAMTargetObscuredDelay < .25f)
                    && AAMCurrentTargetAngle < Lock_Angle
                        && AAMCurrentTargetDistance < AAMMaxTargetDistance)
            {
                AAMHasTarget = true;
                if (NumAAM > 0) AAMLockTimer += DeltaTime;
                //give enemy radar lock even if you're out of missiles
                if (!AAMCurrentTargetEngineControlNull)
                {
                    //target is a plane
                    AAMTargetedTimer += DeltaTime;
                    if (AAMTargetedTimer > 1)
                    {
                        AAMTargetedTimer = 0;
                        if (InEditor)
                            Targeted();
                        else
                            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Targeted");
                    }
                }
            }
            else
            {
                AAMTargetedTimer = 2f;//so it plays straight away next time it's targeted
                AAMLockTimer = 0;
                AAMHasTarget = false;
            }
        }
        else
        {
            AAMTargetedTimer = 2f;
            AAMLockTimer = 0;
            AAMHasTarget = false;
        }
        /*Debug.Log(string.Concat("AAMTargetObscuredDelay ", AAMTargetObscuredDelay));
        Debug.Log(string.Concat("LoS ", LineOfSightCur));
        Debug.Log(string.Concat("RayCastCorrectLayer ", !(hitcurrent.collider.gameObject.layer != PlaneHitBoxLayer)));
        Debug.Log(string.Concat("RayCastLayer ", hitcurrent.collider.gameObject.layer));
        Debug.Log(string.Concat("NotObscured ", AAMTargetObscuredDelay < .25f));
        Debug.Log(string.Concat("InAngle ", AAMCurrentTargetAngle < Lock_Angle));
        Debug.Log(string.Concat("BelowMaxDist ", AAMCurrentTargetDistance < AAMMaxTargetDistance)); */

        //Sounds
        if (AAMHasTarget && !AAMLocked)
        {
            AAMLocking.gameObject.SetActive(true);
            AAMLockedOn.gameObject.SetActive(false);
        }
        else if (AAMLocked)
        {
            AAMLocking.gameObject.SetActive(false);
            AAMLockedOn.gameObject.SetActive(true);
        }
        else
        {
            AAMLocking.gameObject.SetActive(false);
            AAMLockedOn.gameObject.SetActive(false);
        }
    }
    public void Targeted()
    {
        EngineController TargetEngine = null;
        if (AAMTargets[AAMTarget] != null && AAMTargets[AAMTarget].transform.parent != null)
            TargetEngine = AAMTargets[AAMTarget].transform.parent.GetComponent<EngineController>();
        if (TargetEngine != null)
        {
            if (TargetEngine.Piloting || TargetEngine.Passenger)
                TargetEngine.EffectsControl.PlaneAnimator.SetTrigger("radarlocked");
        }
    }
    public void LaunchAAM()
    {
        if (NumAAM > 0) { NumAAM--; }//so it doesn't go below 0 when desync occurs
        AAGunAnimator.SetTrigger("aamlaunched");
        GameObject NewAAM = VRCInstantiate(AAM);
        if (!(NumAAM % 2 == 0))
        {
            //invert local x coordinates of launch point, launch, then revert
            Vector3 temp = AAMLaunchPoint.localPosition;
            temp.x *= -1;
            AAMLaunchPoint.localPosition = temp;
            NewAAM.transform.position = AAMLaunchPoint.transform.position;
            NewAAM.transform.rotation = AAMLaunchPoint.transform.rotation;
            temp.x *= -1;
            AAMLaunchPoint.localPosition = temp;
        }
        else
        {
            NewAAM.transform.position = AAMLaunchPoint.transform.position;
            NewAAM.transform.rotation = AAMLaunchPoint.transform.rotation;
        }
        NewAAM.SetActive(true);
        NewAAM.GetComponent<Rigidbody>().velocity = Vector3.zero;

        AAGunAnimator.SetFloat("AAMs", (float)NumAAM * FullAAMsDivider);
    }
    public void ReloadAAM()
    {
        ReloadTimer = 0;
        if (NumAAM < FullAAMs)
        { NumAAM++; }
        AAGunAnimator.SetFloat("AAMs", (float)NumAAM * FullAAMsDivider);
    }
    void SortTargets(GameObject[] Targets, float[] order)
    {
        for (int i = 1; i < order.Length; i++)
        {
            for (int j = 0; j < (order.Length - i); j++)
            {
                if (order[j] > order[j + 1])
                {
                    var h = order[j + 1];
                    order[j + 1] = order[j];
                    order[j] = h;
                    var k = Targets[j + 1];
                    Targets[j + 1] = Targets[j];
                    Targets[j] = k;
                }
            }
        }
    }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogError("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}
