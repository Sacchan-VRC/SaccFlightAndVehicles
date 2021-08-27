
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SaccAAGunController : UdonSharpBehaviour
{
    public SaccEntity EntityControl;
    public GameObject Rotator;
    public SAAG_HUDController HUDControl;
    public VRCStation AAGunSeat;
    public GameObject AAM;
    public AudioSource AAMLocking;
    public AudioSource AAMLockedOn;
    public Transform JoyStick;
    [SerializeField] private float RespawnDelay = 20;
    [SerializeField] private float InvincibleAfterSpawn = 1;
    public float Health = 100f;
    public float TurnSpeedMulti = 5;
    public float TurnFriction = 4;
    public float UpAngleMax = 89;
    public float DownAngleMax = 35;
    public float TurningResponseDesktop = 3f;
    public float HPRepairDelay = 5f;
    public float HPRepairAmount = 5f;
    public float MissileReloadTime = 10;
    public float MGAmmoSeconds = 4;
    public float MGReloadSpeed = 1;
    public float MGReloadDelay = 2;
    private float MGAmmoRecharge = 0;
    [System.NonSerializedAttribute] public float MGAmmoFull = 4;
    private float FullMGDivider;
    public int NumAAM = 4;
    public float AAMMaxTargetDistance = 6000;
    public float AAMLockAngle = 20;
    public float AAMLockTime = 1.5f;
    public float AAMLaunchDelay = .5f;
    public Transform AAMLaunchPoint;
    public LayerMask AAMTargetsLayer;
    public float PlaneHitBoxLayer = 17;//walkthrough
    [SerializeField] private GameObject SeatAdjuster;
    [System.NonSerializedAttribute] public Animator AAGunAnimator;
    [System.NonSerializedAttribute] public bool dead;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.None)] public bool firing;
    [System.NonSerializedAttribute] public float FullHealth;
    [System.NonSerializedAttribute] public bool Manning;//like Piloting in the plane
    [System.NonSerializedAttribute] public VRCPlayerApi localPlayer;
    [System.NonSerializedAttribute] public float RotationSpeedX = 0f;
    [System.NonSerializedAttribute] public float RotationSpeedY = 0f;
    private Vector3 StartRot;
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
    [System.NonSerializedAttribute] public SaccAirVehicle AAMCurrentTargetEngineControl;
    private float AAMTargetObscuredDelay;
    [System.NonSerializedAttribute] public bool InVR;
    Quaternion AAGunRotLastFrame;
    Quaternion JoystickZeroPoint;
    [System.NonSerializedAttribute] public bool RGripLastFrame = false;
    Vector2 VRPitchYawInput;
    private float FullAAMsDivider;
    private float FullHealthDivider;
    private bool LTriggerLastFrame;
    [System.NonSerializedAttribute] public bool DoAAMTargeting;
    [System.NonSerializedAttribute] public int FullAAMs;
    [System.NonSerializedAttribute] public float AAMReloadTimer;
    [System.NonSerializedAttribute] public float HealthUpTimer;
    [System.NonSerializedAttribute] public float HPRepairTimer;
    private bool JoyStickNull = true;
    [System.NonSerializedAttribute] public float InputXKeyb;
    [System.NonSerializedAttribute] public float InputYKeyb;
    [System.NonSerializedAttribute] public float LastHealthUpdate = 0;
    [System.NonSerializedAttribute] public Transform CenterOfMass;
    private void Start()
    {
        if (FullHealth == 0)//check if it's been initialized
        { gameObject.SetActive(false); }//to stop crashes
    }
    public void SFEXT_L_EntityStart()
    {
        localPlayer = Networking.LocalPlayer;
        if (localPlayer == null) { InEditor = true; Manning = true; IsOwner = true; DoAAMTargeting = true; }
        else
        {
            InEditor = false;
            InVR = localPlayer.IsUserInVR();
            IsOwner = localPlayer.isMaster;
        }
        CenterOfMass = EntityControl.CenterOfMass;

        if (JoyStick != null) { JoyStickNull = false; }

        AAGunAnimator = EntityControl.GetComponent<Animator>();
        FullHealth = Health;
        FullHealthDivider = 1f / (Health > 0 ? Health : 10000000);
        StartRot = Rotator.transform.localRotation.eulerAngles;

        FullAAMs = NumAAM;
        FullAAMsDivider = 1f / (NumAAM > 0 ? NumAAM : 10000000);
        AAGunAnimator.SetFloat("AAMs", (float)NumAAM * FullAAMsDivider);
        MGAmmoFull = MGAmmoSeconds;
        FullMGDivider = 1f / (MGAmmoFull > 0 ? MGAmmoFull : 10000000);

        AAMTargets = EntityControl.AAMTargets;
        NumAAMTargets = EntityControl.NumAAMTargets;
        if (NumAAMTargets != 0) { DoAAMTargeting = true; }

        gameObject.SetActive(true);
    }
    void LateUpdate()
    {
        float DeltaTime = Time.deltaTime;
        if (IsOwner)
        {
            if (Manning)
            {
                //get inputs
                int Wf = Input.GetKey(KeyCode.W) ? 1 : 0; //inputs as ints
                int Sf = Input.GetKey(KeyCode.S) ? -1 : 0;
                int Af = Input.GetKey(KeyCode.A) ? -1 : 0;
                int Df = Input.GetKey(KeyCode.D) ? 1 : 0;

                float RGrip = 0;
                float RTrigger = 0;
                float LTrigger = 0;
                if (!InEditor)
                {
                    RTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
                    LTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
                    RGrip = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
                }
                Vector3 JoystickPosYaw;
                Vector3 JoystickPos;

                //virtual joystick
                if (InVR)
                {
                    if (RGrip > 0.75)
                    {
                        Quaternion RotDif = Rotator.transform.rotation * Quaternion.Inverse(AAGunRotLastFrame);//difference in vehicle's rotation since last frame
                        JoystickZeroPoint = RotDif * JoystickZeroPoint;//zero point rotates with the plane so it appears still to the pilot
                        if (!RGripLastFrame)//first frame you gripped joystick
                        {
                            RotDif = Quaternion.identity;
                            JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;//rotation of the controller relative to the plane when it was pressed
                        }
                        //difference between the plane and the hand's rotation, and then the difference between that and the JoystickZeroPoint
                        Quaternion JoystickDifference = (Quaternion.Inverse(Rotator.transform.rotation) * localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation) * Quaternion.Inverse(JoystickZeroPoint);
                        JoystickPosYaw = (JoystickDifference * Rotator.transform.forward);//angles to vector
                        JoystickPosYaw.y = 0;
                        JoystickPos = (JoystickDifference * Rotator.transform.up);
                        JoystickPos.y = 0;
                        VRPitchYawInput = new Vector2(JoystickPos.z, JoystickPosYaw.x) * 1.41421f;

                        RGripLastFrame = true;
                    }
                    else
                    {
                        JoystickPosYaw.x = 0;
                        VRPitchYawInput = Vector3.zero;
                        RGripLastFrame = false;
                    }
                    AAGunRotLastFrame = Rotator.transform.rotation;
                }
                int InX = (Wf + Sf);
                int InY = (Af + Df);
                if (InX > 0 && InputXKeyb < 0 || InX < 0 && InputXKeyb > 0) InputXKeyb = 0;
                if (InY > 0 && InputYKeyb < 0 || InY < 0 && InputYKeyb > 0) InputYKeyb = 0;
                InputXKeyb = Mathf.Lerp((InputXKeyb), InX, Mathf.Abs(InX) > 0 ? TurningResponseDesktop * DeltaTime : 1);
                InputYKeyb = Mathf.Lerp((InputYKeyb), InY, Mathf.Abs(InY) > 0 ? TurningResponseDesktop * DeltaTime : 1);

                float InputX = Mathf.Clamp((VRPitchYawInput.x + InputXKeyb), -1, 1);
                float InputY = Mathf.Clamp((VRPitchYawInput.y + InputYKeyb), -1, 1);
                //joystick model movement
                if (!JoyStickNull)
                {
                    JoyStick.localRotation = Quaternion.Euler(new Vector3(InputX * 25f, InputY * 25f, 0));
                }
                InputX *= TurnSpeedMulti;
                InputY *= TurnSpeedMulti;

                RotationSpeedX += -(RotationSpeedX * TurnFriction * DeltaTime) + InputX * DeltaTime;
                RotationSpeedY += -(RotationSpeedY * TurnFriction * DeltaTime) + InputY * DeltaTime;

                //rotate turret
                float temprot = Rotator.transform.localRotation.eulerAngles.x;
                temprot += RotationSpeedX;
                if (temprot > 180) { temprot -= 360; }
                if (temprot > DownAngleMax || temprot < -UpAngleMax) RotationSpeedX = 0;
                temprot = Mathf.Clamp(temprot, -UpAngleMax, DownAngleMax);//limit angles
                Rotator.transform.localRotation = Quaternion.Euler(new Vector3(temprot, Rotator.transform.localRotation.eulerAngles.y + (RotationSpeedY), 0));

                //Firing the gun
                if ((RTrigger >= 0.75 || Input.GetKey(KeyCode.Space)) && MGAmmoSeconds > 0)
                {
                    firing = true;
                    MGAmmoSeconds -= DeltaTime;
                    MGAmmoRecharge = MGAmmoSeconds - MGReloadDelay;
                }
                else//recharge the ammo
                {
                    firing = false;
                    MGAmmoRecharge = Mathf.Min(MGAmmoRecharge + (DeltaTime * MGReloadSpeed), MGAmmoFull);
                    MGAmmoSeconds = Mathf.Max(MGAmmoRecharge, MGAmmoSeconds);
                }

                if (DoAAMTargeting)
                {
                    if (AAMLockTimer > AAMLockTime && AAMHasTarget) AAMLocked = true;
                    else { AAMLocked = false; }
                    //firing AAM
                    if (LTrigger > 0.75 || (Input.GetKey(KeyCode.C)))
                    {
                        if (!LTriggerLastFrame)
                        {
                            if (AAMLocked && Time.time - AAMLastFiredTime > AAMLaunchDelay)
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

                //reloading AAMs
                if (NumAAM == FullAAMs)
                { AAMReloadTimer = 0; }
                else
                { AAMReloadTimer += DeltaTime; }
                if (AAMReloadTimer > MissileReloadTime)
                {
                    if (InEditor)
                    { ReloadAAM(); }
                    else
                    { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ReloadAAM"); }
                }
                //HP Repair
                if (Health == FullHealth)
                { HPRepairTimer = 0; }
                else
                { HPRepairTimer += DeltaTime; }
                if (HPRepairTimer > HPRepairDelay)
                {
                    if (InEditor)
                    { HPRepair(); }
                    else
                    { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "HPRepair"); }
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
        //Sounds
        if (AAMLockTimer > 0 && !AAMLocked && NumAAM > 0)
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
    private void FixedUpdate()
    {
        if (Manning && DoAAMTargeting)
        {
            AAMTargeting(AAMLockAngle);
        }
    }
    public void Explode()//all the things players see happen when the vehicle explodes
    {
        if (Manning && !InEditor)
        {
            if (AAGunSeat != null) { AAGunSeat.ExitStation(localPlayer); }
        }
        dead = true;
        firing = false;
        MGAmmoSeconds = MGAmmoFull;
        Health = FullHealth;//turns off low health smoke and stops it from calling Explode() every frame
        NumAAM = FullAAMs;
        AAGunAnimator.SetBool("firing", false);
        AAGunAnimator.SetFloat("AAMs", (float)FullAAMs * FullAAMsDivider);
        AAGunAnimator.SetFloat("health", 1);
        if (IsOwner)
        {
            Rotator.transform.localRotation = Quaternion.Euler(StartRot);
        }
        AAGunAnimator.SetTrigger("explode");

        SendCustomEventDelayedSeconds(nameof(ReAppear), RespawnDelay);
        SendCustomEventDelayedSeconds(nameof(NotDead), RespawnDelay + InvincibleAfterSpawn);
    }
    public void ReAppear()
    {
        AAGunAnimator.SetTrigger("reappear");
        dead = false;
        if (localPlayer == null || IsOwner)
        {
            Health = FullHealth;
        }
    }
    public void NotDead()
    {
        Health = FullHealth;
        EntityControl.dead = false;
    }
    public void SFEXT_G_BulletHit()
    {
        if (InEditor || IsOwner)
        {
            Health -= 10;
            if (Health <= 0)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Explode");
            }
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
            SaccAirVehicle NextTargetEngineControl = null;

            if (TargetCheckerParent)
            {
                NextTargetEngineControl = TargetCheckerParent.GetComponent<SaccAirVehicle>();
            }
            //if target EngineController is null then it's a dummy target (or hierarchy isn't set up properly)
            if ((!NextTargetEngineControl || (!NextTargetEngineControl.Taxiing && !NextTargetEngineControl.EntityControl.dead)))
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
                                    || ((!AAMCurrentTargetEngineControlNull && AAMCurrentTargetEngineControl.Taxiing) || !AAMTargets[AAMTarget].activeInHierarchy)) //prevent being unable to target next target if it's angle is higher than your current target and your current target happens to be taxiing and is therefore untargetable
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
        AAMCurrentTargetDirection = AAMCurrentTargetPosition - HudControlPosition;
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
                && (AAMCurrentTargetEngineControlNull || (!AAMCurrentTargetEngineControl.Taxiing && !AAMCurrentTargetEngineControl.EntityControl.dead)))
        {
            if ((AAMTargetObscuredDelay < .25f)
                        && AAMCurrentTargetDistance < AAMMaxTargetDistance)
            {
                AAMHasTarget = true;
                if (AAMCurrentTargetAngle < Lock_Angle && NumAAM > 0)
                {
                    AAMLockTimer += DeltaTime;
                    //dont give enemy radar lock if you're out of missiles (planes can do this though)
                    if (!AAMCurrentTargetEngineControlNull)
                    {
                        //target is a plane, send the 'targeted' event every second to make the target plane play a warning sound in the cockpit.
                        AAMTargetedTimer += DeltaTime;
                        if (AAMTargetedTimer > 1)
                        {
                            AAMTargetedTimer = 0;
                            if (InEditor)
                            { Targeted(); }
                            else
                            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Targeted"); }
                        }
                    }
                }
                else
                {
                    AAMTargetedTimer = 2f;
                    AAMLockTimer = 0;
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
        Debug.Log(string.Concat("RayCastCorrectLayer ", (hitcurrent.collider.gameObject.layer == PlaneHitBoxLayer)));
        Debug.Log(string.Concat("RayCastLayer ", hitcurrent.collider.gameObject.layer));
        Debug.Log(string.Concat("NotObscured ", AAMTargetObscuredDelay < .25f));
        Debug.Log(string.Concat("InAngle ", AAMCurrentTargetAngle < Lock_Angle));
        Debug.Log(string.Concat("BelowMaxDist ", AAMCurrentTargetDistance < AAMMaxTargetDistance)); */
    }
    public void Targeted()
    {
        SaccAirVehicle TargetEngine = null;
        if (AAMTargets[AAMTarget] != null && AAMTargets[AAMTarget].transform.parent != null)
            TargetEngine = AAMTargets[AAMTarget].transform.parent.GetComponent<SaccAirVehicle>();
        if (TargetEngine != null)
        {
            if (TargetEngine.Piloting || TargetEngine.Passenger)
            { TargetEngine.VehicleAnimator.SetTrigger("radarlocked"); }
        }
    }
    public void LaunchAAM()
    {
        if (NumAAM > 0) { NumAAM--; }//so it doesn't go below 0 when desync occurs
        AAGunAnimator.SetTrigger("aamlaunched");
        GameObject NewAAM = VRCInstantiate(AAM);
        if (!(NumAAM % 2 == 0))
        {
            //invert local x coordinates of launch point, launch, then revert, for odd numbered shots
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
        AAMReloadTimer = 0;
        NumAAM++;
        if (NumAAM > FullAAMs) { NumAAM = FullAAMs; }
        AAGunAnimator.SetFloat("AAMs", (float)NumAAM * FullAAMsDivider);
    }
    public void HPRepair()
    {
        HPRepairTimer = 0;
        Health += HPRepairAmount;
        if (Health > FullHealth) { Health = FullHealth; }
        AAGunAnimator.SetFloat("health", Health / FullHealth);
    }
    public void SFEXT_O_PilotEnter()
    {
        Manning = true;
        RotationSpeedX = 0;
        RotationSpeedY = 0;
        if (AAGunAnimator != null) AAGunAnimator.SetBool("inside", true);
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(true); }
        if (HUDControl != null) { HUDControl.GUN_TargetSpeedLerper = 0; }

        //Make sure EngineControl.AAMCurrentTargetEngineControl is correct
        var Target = AAMTargets[AAMTarget];
        if (Target && Target.transform.parent)
        {
            AAMCurrentTargetEngineControl = Target.transform.parent.GetComponent<SaccAirVehicle>();
        }
        if (localPlayer != null && localPlayer.IsUserInVR())
        {
            InVR = true;//has to be set on enter otherwise Built And Test thinks you're in desktop
        }
    }
    public void SFEXT_G_PilotEnter()
    {
        //Reload based on the amount of time passed while no one was inside
        float TimeSinceLast = (Time.time - LastHealthUpdate);
        LastHealthUpdate = Time.time;
        //This function is called by OnStationEntered(), currently there's a bug where OnStationEntered() is called multiple times, when entering a seat
        //this check stops it from doing anything more than once
        if (TimeSinceLast > 1)
        {
            NumAAM = Mathf.Min((NumAAM + ((int)(TimeSinceLast / MissileReloadTime))), FullAAMs);
            AAGunAnimator.SetFloat("AAMs", (float)NumAAM * FullAAMsDivider);
            Health = Mathf.Min((Health + (((int)(TimeSinceLast / HPRepairDelay))) * HPRepairAmount), FullHealth);
            AAGunAnimator.SetFloat("health", Health / FullHealth);
            MGAmmoRecharge += TimeSinceLast * MGReloadSpeed;
        }
    }
    public void SFEXT_G_PilotExit()
    {
        LastHealthUpdate = Time.time;
    }
    public void SFEXT_O_PilotExit()
    {
        Manning = false;
        firing = false;
        AAMLockTimer = 0;
        AAMHasTarget = false;
        DoAAMTargeting = false;
        AAGunAnimator.SetBool("inside", false);
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(false); }
    }
    public void SFEXT_O_TakeOwnership()
    {
        firing = false;
        IsOwner = true;
    }
    public void SFEXT_O_LoseOwnership()
    {
        IsOwner = false;
    }
}
