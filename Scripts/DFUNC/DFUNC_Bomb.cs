
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_Bomb : UdonSharpBehaviour
    {
        [SerializeField] public UdonSharpBehaviour SAVControl;
        public Animator BombAnimator;
        public GameObject Bomb;
        [Tooltip("How long it takes to fully reload from empty in seconds. Can be inaccurate because it can only reload by integers per resupply")]
        public float FullReloadTimeSec = 8;
        public Text HUDText_Bomb_ammo;
        public int NumBomb = 4;
        [Tooltip("Delay between bomb drops when holding the trigger")]
        public float BombHoldDelay = 0.5f;
        [Tooltip("Minimum delay between bomb drops")]
        public float BombDelay = 0f;
        [Tooltip("Points at which bombs appear, each succesive bomb appears at the next transform")]
        public Transform[] BombLaunchPoints;
        [Tooltip("Allow user to fire the weapon while the vehicle is on the ground taxiing?")]
        public bool AllowFiringWhenGrounded = false;
        public bool DoAnimBool = false;
        [Tooltip("Animator bool that is true when this function is selected")]
        public string AnimBoolName = "BombSelected";
        [Tooltip("Animator float that represents how many bombs are left")]
        public string AnimFloatName = "bombs";
        [Tooltip("Animator trigger that is set when a bomb is dropped")]
        public string AnimFiredTriggerName = "bomblaunched";
        [Tooltip("Should the boolean stay true if the pilot exits with it selected?")]
        public bool AnimBoolStayTrueOnExit;
        [Tooltip("Dropped bombs will be parented to this object, use if you happen to have some kind of moving origin system")]
        public Transform WorldParent;
        public Camera AtGCam;
        public GameObject AtGScreen;
        [UdonSynced, FieldChangeCallback(nameof(BombFire))] private ushort _BombFire;
        public ushort BombFire
        {
            set
            {
                if (value > _BombFire)//if _BombFire is higher locally, it's because a late joiner just took ownership or value was reset, so don't launch
                { LaunchBomb(); }
                _BombFire = value;
            }
            get => _BombFire;
        }
        private float boolToggleTime;
        private bool AnimOn = false;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        private bool UseLeftTrigger = false;
        private float Trigger;
        private bool TriggerLastFrame;
        private int BombPoint = 0;
        private float LastBombDropTime = -999f;
        [System.NonSerializedAttribute] public int FullBombs;
        private float FullBombsDivider;
        private Transform VehicleTransform;
        private float reloadspeed;
        private bool LeftDial = false;
        private bool Piloting = false;
        private bool OthersEnabled = false;
        private bool func_active = false;
        private int DialPosition = -999;
        private int NumChildrenStart;
        private VRCPlayerApi localPlayer;
        [System.NonSerializedAttribute] public Transform CenterOfMass;
        [System.NonSerializedAttribute] public bool IsOwner;
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }

        //CCIP stuff from here on
        [Header("KitKat's stuff from here on")]
        [Tooltip("If the AGM cam will display where the bomb will hit even though CCIP and CCRP are off.")]
        [SerializeField] bool PredictiveBombCam = true;
        [SerializeField] SaccAirVehicle LinkedAirVehicle;
        [SerializeField] SAV_BombController LinkedBombController;
        [SerializeField] Rigidbody AircraftRigidbody;
        [SerializeField] Rigidbody BombRigidbody;
        [SerializeField] GameObject BombLaunchPoint;
        [Tooltip("This is where you link an empty or something, it is nessecary for the prediction to work.")]
        [SerializeField] GameObject PredictedImpact;
        [Header("CCIP")]
        [Tooltip("Disable anything related to CCIP?")]
        [SerializeField] bool DoCCIP = false;
        [SerializeField] GameObject HudCCIP;
        [SerializeField] GameObject TopOfCCIPline;
        [SerializeField] Transform LinkedHudVelocityVector;
        [Header("CCRP")]
        [Tooltip("Disable anything related to CCRP?")]
        [SerializeField] bool DoCCRP = false;
        [SerializeField] GameObject HudCCRP;
        [SerializeField] GameObject LineRotator;
        [SerializeField] GameObject CrosshairRotator;
        [SerializeField] GameObject TimingRotator;
        [SerializeField] Transform CCRP_Targets;
        [SerializeField] int MaxiterationStep = 100;
        [SerializeField] int IterationsBeforeCollisionCheck = 1;
        [Tooltip("The distance between where a bomb will land and where a possible target is located, if the ditance is below this value the HUD will tell you how to hit the target, and the script enters CCRP mode (more about that on line 123)")]
        [SerializeField] float MinDistanceToTarget = 700;
        [Tooltip("How fast the red distance diamond goes down the line. A higher number allows the pilot to be more accurate.")]
        public float multiplier = 0.05f;
        [Tooltip("How close the pilot needs needs to aim to the target before the script drops the bomb. In meters of course.")]
        public float CCRP_Acc_Threshold = 5;

        Vector3 groundzero;
        Vector3[] DebugPosLine;
        Vector2 CurrentCCRPtarget;
        Vector3 CCIPLookRot;

        public float distance_from_head = 1.333f;

        float ClosestDistance;
        float CCRPHeading = 0;
        float DistanceRelease = 0;

        float iterationTime;
        float MaxTotalDropTime;

        bool Selected;
        bool hitdetect = false;
        bool CCRPmode = false; //I added "CCRPmode" this mode makes it so when the trigger/spacebar is held the plane automatically drops the bomb whenever the bomb will hit close enough to the target.
        bool CCRPfired;

        float Gravity = Physics.gravity.magnitude * -1;

        bool DFUNC_Setup_ERR = false;

        private void Start()
        {
            if (!LinkedAirVehicle || !LinkedBombController || !AircraftRigidbody || !BombRigidbody || !BombLaunchPoint || !PredictedImpact)
            {
                if (PredictiveBombCam || DoCCIP || DoCCRP) { Debug.LogError("Vital dependencies not linked, CCIP and CCRP will be disabled and predictive bomb camera will be static."); }
                DFUNC_Setup_ERR = true;
            }
            if(!HudCCIP || !TopOfCCIPline || !LinkedHudVelocityVector)
            {
                if (DoCCIP) { Debug.LogError("CCIP HUD elements are not set up correctly, CCIP disabled."); }
                DoCCIP = false;
            }
            if (!HudCCRP || !LineRotator || !CrosshairRotator || !TimingRotator || !CCRP_Targets)
            {
                if (DoCCRP) { Debug.LogError("CCRP is not set up correctly, CCRP disabled."); }
                DoCCRP = false;
            }
            if(DFUNC_Setup_ERR)
            {
                DoCCIP = false;
                DoCCRP = false;
            }
            if (PredictedImpact) PredictedImpact.SetActive(false);
            if (PredictiveBombCam && !PredictedImpact) { Debug.LogError("Predicted Impact empty is not linked."); }
            if (HudCCIP) { HudCCIP.SetActive(DoCCIP); }
            if (HudCCRP) { HudCCRP.SetActive(DoCCRP); }
        }
        //CCIP stuff ends here

        public void SFEXT_L_EntityStart()
        {
            FullBombs = NumBomb;
            if (BombHoldDelay < BombDelay) { BombHoldDelay = BombDelay; }
            FullBombsDivider = 1f / (NumBomb > 0 ? NumBomb : 10000000);
            reloadspeed = FullBombs / FullReloadTimeSec;
            EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
            BombAnimator = EntityControl.GetComponent<Animator>();
            CenterOfMass = EntityControl.CenterOfMass;
            VehicleTransform = EntityControl.transform;
            localPlayer = Networking.LocalPlayer;

            FindSelf();

            UpdateAmmoVisuals();

            NumChildrenStart = transform.childCount;
            if (Bomb)
            {
                int NumToInstantiate = Mathf.Min(FullBombs, 30);
                for (int i = 0; i < NumToInstantiate; i++)
                {
                    InstantiateWeapon();
                }
            }

            //CCIP stuff
            if (LinkedBombController)
            {
            float MaxTotalDropTime = LinkedBombController.MaxLifetime;
            iterationTime = MaxTotalDropTime / MaxiterationStep;
            } else if (DoCCIP || DoCCRP || PredictiveBombCam) { Debug.LogError("Bomb controller not linked."); }
            //CCIP stuff ends here
        }
        private GameObject InstantiateWeapon()
        {
            GameObject NewWeap = VRCInstantiate(Bomb);
            NewWeap.transform.SetParent(transform);
            return NewWeap;
        }
        public void SFEXT_O_PilotEnter()
        {
            Piloting = true;
            if (HUDText_Bomb_ammo) { HUDText_Bomb_ammo.text = NumBomb.ToString("F0"); }
        }
        public void SFEXT_G_PilotExit()
        {
            if (OthersEnabled) { DisableForOthers(); }
            if (DoAnimBool && !AnimBoolStayTrueOnExit && AnimOn)
            { SetBoolOff(); }
        }
        public void SFEXT_O_PilotExit()
        {
            func_active = false;
            Piloting = false;
            gameObject.SetActive(false);
            if (AtGScreen) { AtGScreen.SetActive(false); }
            if (AtGCam) { AtGCam.gameObject.SetActive(false); }
        }
        public void SFEXT_P_PassengerEnter()
        {
            if (HUDText_Bomb_ammo) { HUDText_Bomb_ammo.text = NumBomb.ToString("F0"); }
        }
        public void DFUNC_Selected()
        {
            TriggerLastFrame = true;
            func_active = true;
            gameObject.SetActive(true);
            if (DoAnimBool && !AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOn)); }
            if (!OthersEnabled) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EnableForOthers)); }
            if (AtGScreen) AtGScreen.SetActive(true);
            if (AtGCam)
            {
                AtGCam.gameObject.SetActive(true);
                AtGCam.fieldOfView = 60;
                AtGCam.transform.localRotation = Quaternion.Euler(110, 0, 0);
            }
        }
        public void DFUNC_Deselected()
        {
            func_active = false;
            gameObject.SetActive(false);
            if (DoAnimBool && AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOff)); }
            if (OthersEnabled) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(DisableForOthers)); }
            if (AtGScreen) { AtGScreen.SetActive(false); }
            if (AtGCam) { AtGCam.gameObject.SetActive(false); }
        }
        public void SFEXT_G_Explode()
        {
            BombPoint = 0;
            NumBomb = FullBombs;
            UpdateAmmoVisuals();
            if (DoAnimBool && AnimOn)
            { SetBoolOff(); }
        }
        public void SFEXT_G_RespawnButton()
        {
            NumBomb = FullBombs;
            UpdateAmmoVisuals();
            BombPoint = 0;
            if (DoAnimBool && AnimOn)
            { SetBoolOff(); }
        }
        public void SFEXT_G_ReSupply()
        {
            if (NumBomb != FullBombs)
            { SAVControl.SetProgramVariable("ReSupplied", (int)SAVControl.GetProgramVariable("ReSupplied") + 1); }
            NumBomb = (int)Mathf.Min(NumBomb + Mathf.Max(Mathf.Floor(reloadspeed), 1), FullBombs);
            BombPoint = 0;
            UpdateAmmoVisuals();
        }
        public void UpdateAmmoVisuals()
        {
            BombAnimator.SetFloat(AnimFloatName, (float)NumBomb * FullBombsDivider);
            if (HUDText_Bomb_ammo) { HUDText_Bomb_ammo.text = NumBomb.ToString("F0"); }
        }
        public void EnableForOthers()
        {
            if (!Piloting)
            { gameObject.SetActive(true); BombFire = 0; }
            OthersEnabled = true;
        }
        public void DisableForOthers()
        {
            if (!Piloting)
            { gameObject.SetActive(false); }
            OthersEnabled = false;
        }
        //CCIP stuff
        private void Update()
        {
            if (HudCCIP && DoCCIP)
            {
                HudCCIP.SetActive(func_active);
            }
            if (func_active)
            {
                if (!DFUNC_Setup_ERR) { SimulateTrajectory(); }
                if (DoCCRP)
                {
                    GetCCRPtarget();
                    HudCCRP.SetActive(CCRPmode);
                }
                if (!DFUNC_Setup_ERR) { HUD(); }

                if (UseLeftTrigger)
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                else
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
                if (Trigger > 0.75 || (Input.GetKey(KeyCode.Space)))
                {
                    if (!CCRPmode)
                    {
                        if (!TriggerLastFrame)
                        {
                            if (NumBomb > 0 && (AllowFiringWhenGrounded || !(bool)SAVControl.GetProgramVariable("Taxiing")) && ((Time.time - LastBombDropTime) > BombDelay))
                            {
                                BombFireFunc();
                            }
                        }
                        else if (NumBomb > 0 && ((Time.time - LastBombDropTime) > BombHoldDelay) && !CCRPfired && (AllowFiringWhenGrounded || !(bool)SAVControl.GetProgramVariable("Taxiing"))) //!CCRPfired here so if you keep holding trigger after strafing a ccrp target it won't start dropping bombs afterwards unless you re-press the trigger.
                        {//launch every BombHoldDelay
                            BombFireFunc();
                        }
                    }
                    else if (!CCRPfired)
                    {
                        //CCRP hold to fire when the bomb will land close enough to the target.
                        Vector2 groundzeroCoordinate = new Vector2(groundzero.x, groundzero.z);
                        if (Vector2.Distance(groundzeroCoordinate, CurrentCCRPtarget) < CCRP_Acc_Threshold && NumBomb > 0 && (AllowFiringWhenGrounded || !(bool)SAVControl.GetProgramVariable("Taxiing")))
                        {
                            BombFireFunc();
                            CCRPfired = true;
                        }
                    }
                    TriggerLastFrame = true;
                }
                else { TriggerLastFrame = false; CCRPfired = false; }
            }
        }
        void BombFireFunc()
        {
            BombFire++;
            RequestSerialization();
            if ((bool)SAVControl.GetProgramVariable("IsOwner"))
            { EntityControl.SendEventToExtensions("SFEXT_O_BombLaunch"); }
        }
        //CCIP calculation happens here
        public void SimulateTrajectory()
        {
            hitdetect = false;
            Vector3 Velocity = AircraftRigidbody.velocity;
            Vector3 Pos = BombLaunchPoint.transform.position; //The starting point of the trajectory calculation.
            float drag = BombRigidbody.drag;

            for (int i = 0; i < MaxiterationStep; i++) //Iterates through the trajectory.
            {
                Pos.y += Velocity.y * iterationTime + Gravity * iterationTime * iterationTime * 0.5f; //Accounts for gravity and predicts the next position at that timestep.
                Pos.x += Velocity.x * iterationTime;
                Pos.z += Velocity.z * iterationTime;
                Velocity.y += Gravity * iterationTime; //Updates the velocity.
                Velocity = Velocity * (1 - iterationTime * drag); //This works pretty well but is slightly off...
                Vector3 PosOffset = Velocity * iterationTime; //This is the actual position the bomb will be at the current timestep.

                if (i > IterationsBeforeCollisionCheck && !hitdetect) //Iterations before collisioncheck is to prevent the plane from marking itself as a hit. It then does a raycast that extends for 2m further than the next position.
                {
                    Ray ray = new Ray(origin: Pos, direction: PosOffset);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, PosOffset.magnitude + 2f))
                    {
                        hitdetect = true;
                        groundzero = hit.point;
                        PredictedImpact.transform.position = hit.point; //This empty is nessecary for CCRP release prediction to work.
                    }
                }
                if (HudCCIP && DoCCIP) { HudCCIP.SetActive(hitdetect); } //Makes the hud element go away if the prediction didn't find a groundzero within the bomblifetime.
                if (HudCCRP && DoCCRP) { HudCCRP.SetActive(hitdetect); }
                if (!hitdetect) { CCRPmode = false; }
            }
        }
        void GetCCRPtarget() //Iterates through all the empties in CCRP_Targets and gets the closest target to groundzero
        {
            ClosestDistance = float.PositiveInfinity;
            Transform ClosestTarget = null;
            foreach (Transform Target in CCRP_Targets)
            {
                float distance = Vector3.Distance(groundzero, Target.position);
                if (distance < ClosestDistance)
                {
                    ClosestDistance = distance;
                    ClosestTarget = Target;
                }
            }
            CurrentCCRPtarget = new Vector2(ClosestTarget.position.x, ClosestTarget.position.z);
            if (ClosestDistance < MinDistanceToTarget)
            {
                if (DoCCRP && hitdetect) { CCRPmode = true; }
            }
            else
            {
                CCRPmode = false;
            }
        }
        void HUD()
        {
            if (PredictiveBombCam)
            {
                CCIPLookRot = (groundzero - AircraftRigidbody.position);
                AtGCam.transform.LookAt(groundzero);
                AtGCam.fieldOfView = Mathf.Clamp(90f / (CCIPLookRot.magnitude * CCIPLookRot.magnitude), 2f, 60f);
            }
            if (DoCCIP)
            {
                HudCCIP.transform.rotation = Quaternion.LookRotation(CCIPLookRot);
                TopOfCCIPline.transform.position = LinkedHudVelocityVector.position;
            }
            if (DoCCRP)
            {
                Vector2 AircraftCoordinate = new Vector2(AircraftRigidbody.position.x, AircraftRigidbody.position.z);
                Vector2 HitCoordinate = new Vector2(groundzero.x, groundzero.z);
                Vector2 CCRPLookRot = (CurrentCCRPtarget - AircraftCoordinate);
                CCRPHeading = Mathf.Atan2(CCRPLookRot.y, CCRPLookRot.x) * -Mathf.Rad2Deg + 90f;
                LineRotator.transform.rotation = Quaternion.Euler(new Vector3(0, CCRPHeading, 0));

                float angle;
                Vector3 Nosepos = AircraftRigidbody.transform.forward;
                angle = Mathf.Clamp(Vector3.Angle(Nosepos, Vector3.up) - 90, -90, 90);

                CrosshairRotator.transform.rotation = Quaternion.Euler(new Vector3(angle, CCRPHeading, 0));

                PredictedImpact.transform.rotation = LineRotator.transform.rotation;
                DistanceRelease = Mathf.Clamp(PredictedImpact.transform.InverseTransformPoint(new Vector3(CurrentCCRPtarget.x, 0, CurrentCCRPtarget.y)).z * -multiplier, -90, 90);
                TimingRotator.transform.localRotation = Quaternion.Euler(new Vector3(DistanceRelease, 0, 0));
                if (Mathf.Abs(DistanceRelease) < 60)
                {
                    TimingRotator.SetActive(true);
                }
                else { TimingRotator.SetActive(false); }
            }
        }
        //CCIP stuff ends here

        public void LaunchBomb()
        {
            LastBombDropTime = Time.time;
            IsOwner = localPlayer.IsOwner(gameObject);
            if (NumBomb > 0) { NumBomb--; }
            BombAnimator.SetTrigger(AnimFiredTriggerName);
            if (Bomb)
            {
                GameObject NewBomb;
                if (transform.childCount - NumChildrenStart > 0)
                { NewBomb = transform.GetChild(NumChildrenStart).gameObject; }
                else
                { NewBomb = InstantiateWeapon(); }
                if (WorldParent) { NewBomb.transform.SetParent(WorldParent); }
                else { NewBomb.transform.SetParent(null); }
                NewBomb.transform.SetPositionAndRotation(BombLaunchPoints[BombPoint].position, BombLaunchPoints[BombPoint].rotation);
                NewBomb.SetActive(true);
                NewBomb.GetComponent<Rigidbody>().velocity = (Vector3)SAVControl.GetProgramVariable("CurrentVel");
                BombPoint++;
                if (BombPoint == BombLaunchPoints.Length) BombPoint = 0;
            }
            UpdateAmmoVisuals();
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
            Debug.LogWarning("DFUNC_Bomb: Can't find self in dial functions");
        }
        public void SetBoolOn()
        {
            boolToggleTime = Time.time;
            AnimOn = true;
            BombAnimator.SetBool(AnimBoolName, AnimOn);
        }
        public void SetBoolOff()
        {
            boolToggleTime = Time.time;
            AnimOn = false;
            BombAnimator.SetBool(AnimBoolName, AnimOn);
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
}
