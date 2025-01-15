using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(-10)]
    public class SaccEntity : UdonSharpBehaviour
    {
        [Tooltip("Put all scripts used by this vehicle that use the event system into this list (excluding DFUNCs and PassengerFunctionsControllers)")]
        public UdonSharpBehaviour[] ExtensionUdonBehaviours;
        [Tooltip("Function dial scripts that you wish to be on the left dial")]
        public UdonSharpBehaviour[] Dial_Functions_L;
        [Tooltip("Function dial scripts that you wish to be on the right dial")]
        public UdonSharpBehaviour[] Dial_Functions_R;
        [Tooltip("Pointer on the dial")]
        public Transform LStickDisplayHighlighter;
        [Tooltip("Pointer on the dial")]
        public Transform RStickDisplayHighlighter;
        [Tooltip("How far the stick has to be pushed to select a function")]
        public float DialSensitivity = 0.7f;
        [Tooltip("Should there be a function at the top middle of the function dial[ ]? Or a divider[x]? Useful for adjusting function positions with an odd number of functions")]
        public bool LeftDialDivideStraightUp = false;
        [Tooltip("See above")]
        public bool RightDialDivideStraightUp = false;
        [Tooltip("Layer to find all objects on to use as AAM targets")]
        public LayerMask AAMTargetsLayer = 1 << 25;//layer 25
        [Tooltip("Objects that are enabled when entering vehicle in any seat")]
        public GameObject[] EnableInVehicle;
        [Tooltip("Objects that are disabled when entering vehicle in any seat")]
        public GameObject[] DisableInVehicle;
        [Tooltip("Objects that are enabled when holding this object")]
        public GameObject[] EnableWhenHolding;
        [Tooltip("Objects that are disabled when holding this object")]
        public GameObject[] DisableWhenHolding;
        [Tooltip("Objects that are enabled when owner of this object")]
        public GameObject[] EnableWhenOwner;
        [Tooltip("Optional: Use a transform as respawn point")]
        public Transform RespawnPoint;
        [Tooltip("To tell child scripts/rigidbodys where the center of the vehicle is")]
        public Transform CenterOfMass;
        [Tooltip("Change voice volumes for players who are in the vehicle together? (checked by SaccVehicleSeat)")]
        public bool DoVoiceVolumeChange = true;
        [Tooltip("Double tap the exit vehicle button to exit the vehicle?")]
        public bool DoubleTapToExit = false;
        [Tooltip("Ignore particles hitting the object?")]
        public bool DisableBulletHitEvent = false;
        [Tooltip("Using the particle damage system, ignore damage events below this level of damage")]
        [Range(-9, 14)]
        public int BulletArmorLevel = -9;
        [Header("Selection Sound")]

        [Tooltip("Oneshot sound played each time function selection changes")]
        public AudioSource SwitchFunctionSound;
        public bool PlaySelectSoundLeft = true;
        public bool PlaySelectSoundRight = true;
        [Tooltip("You can add seats that are NOT a child of this object, if you want to control the vehicle from outside")]
        public VRCStation[] ExternalSeats;
        [Tooltip("Enable the Interact functionality on this object?")]
        [FieldChangeCallback(nameof(EnableInteract))] public bool _EnableInteract = false;
        public bool EnableInteract
        {
            set
            {
                _EnableInteract = value;
                updateDisableInteractive();
            }
            get => _EnableInteract;
        }
        [Tooltip("Interact runs the custom pickup code in SaccEntity? (allows dual wielding on desktop, and holding without holding down grip)")]
        public bool Interact_CustomPickup = true;
        [Tooltip("Same as what VRC_Pickup's AllowSteal does")]
        public bool CustomPickup_AllowSteal = true;
        [Tooltip("Match rotation of hand?")]
        public bool CustomPickup_SetRotation = true;
        [Tooltip("Grab the object here")]
        public Transform CustomPickup_GrabPosition;
        public KeyCode CustomPickup_DropKey = KeyCode.G;
        public Vector3 CustomPickup_RotationOffsetVR = new Vector3(0, 60, 90);
        public Vector3 CustomPickup_RotationOffsetDesktop = new Vector3(0, 35, 90);
        [Tooltip("Disable collision when held by changing the layer and setting the colliders to trigger")]
        [SerializeField] bool Pickup_DisableCollisionOnGrab;
        [Space(8)]
        [Tooltip("Vehicle scripts set vehicle to this layer when you're inside (also used by scripts to find vehicles with raycasts)")]
        public int OnboardVehicleLayer = 31;
        [Tooltip("Vehicle scripts set vehicle to this layer when you're outside (also used by scripts to find vehicles with raycasts)")]
        public int OutsideVehicleLayer = 17;
        private int StartEntityLayer;
        [Header("For debugging, auto filled on build")]
        [Tooltip("These are automatically collected from the vehicle's seat scripts")]
        public SAV_PassengerFunctionsController[] PassengerFunctionControllers;
        public GameObject[] AAMTargets;
        [System.NonSerializedAttribute] public bool InEditor = true;//false if in clientsim
        private VRCPlayerApi localPlayer;
        [System.NonSerialized] public VRCPlayerApi OwnerAPI;
        [System.NonSerializedAttribute] public VRC_Pickup EntityPickup;
        [System.NonSerializedAttribute] public VRC.SDK3.Components.VRCObjectSync EntityObjectSync;
        [System.NonSerializedAttribute] public Rigidbody VehicleRigidbody;
        [System.NonSerializedAttribute] public Collider[] EntityColliders;
        [System.NonSerializedAttribute] public bool Piloting;
        [System.NonSerializedAttribute] public int UsersID;
        [System.NonSerializedAttribute] public string UsersName;
        private Vector2 RStickCheckAngle;
        private Vector2 LStickCheckAngle;
        [System.NonSerializedAttribute] public bool MySeatIsExternal;
        [System.NonSerializedAttribute] public GameObject LastHitParticle;
        bool hasPassengerFunctions;
        [System.NonSerializedAttribute] public float LStickFuncDegrees;
        [System.NonSerializedAttribute] public float RStickFuncDegrees;
        [System.NonSerializedAttribute] public float LStickFuncDegreesDivider;
        [System.NonSerializedAttribute] public float RStickFuncDegreesDivider;
        [System.NonSerializedAttribute] public int LStickNumFuncs;
        [System.NonSerializedAttribute] public int RStickNumFuncs;
        [System.NonSerializedAttribute] public bool DoDialLeft;
        [System.NonSerializedAttribute] public bool DoDialRight;
        string HierarchyName;
        //specially used by limits function
        //this stuff can be used by DFUNCs
        //if these == 0 then they are not disabled. Being an int allows more than one extension to disable it at a time
        //the bools exists to save externs every frame
        [System.NonSerializedAttribute] public bool _DisableLeftDial;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(DisableLeftDial_))] public int DisableLeftDial = 0;
        public int DisableLeftDial_
        {
            set
            {
                _DisableLeftDial = value > 0;
                DisableLeftDial = value;
            }
            get => DisableLeftDial;
        }
        [System.NonSerializedAttribute] public bool _DisableRightDial;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(DisableRightDial_))] public int DisableRightDial = 0;
        public int DisableRightDial_
        {
            set
            {
                _DisableRightDial = value > 0;
                DisableRightDial = value;
            }
            get => DisableRightDial;
        }
        [System.NonSerialized] public bool _DisallowOwnerShipTransfer;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(DisallowOwnerShipTransfer_))] public int DisallowOwnerShipTransfer = 0;
        public int DisallowOwnerShipTransfer_
        {
            set
            {
                _DisallowOwnerShipTransfer = value > 0;
                DisallowOwnerShipTransfer = value;
            }
            get => DisallowOwnerShipTransfer;
        }
        [System.NonSerializedAttribute] public bool KeepAwake = false;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(KeepAwake_))] public int _KeepAwake = 0;
        public int KeepAwake_
        {
            set
            {
                if (value > 0 && _KeepAwake == 0)
                {
                    SendEventToExtensions("SFEXT_L_KeepAwake");
                }
                else if (value == 0 && _KeepAwake > 0)
                {
                    SendEventToExtensions("SFEXT_L_KeepAwakeFalse");
                }
                KeepAwake = value > 0;
                _KeepAwake = value;
            }
            get => _KeepAwake;
        }
        [System.NonSerializedAttribute] public bool[] LStickNULL;
        [System.NonSerializedAttribute] public bool[] RStickNULL;
        [System.NonSerializedAttribute] public int RStickSelection = -1;
        [System.NonSerializedAttribute] public int LStickSelection = -1;
        [System.NonSerializedAttribute] public int RStickSelectionLastFrame = -1;
        [System.NonSerializedAttribute] public int LStickSelectionLastFrame = -1;
        [System.NonSerializedAttribute] public bool _wrecked = false;
        public bool wrecked
        {
            set
            {
                if (value)
                { SendEventToExtensions("SFEXT_G_Wrecked"); }
                else
                { SendEventToExtensions("SFEXT_G_NotWrecked"); }
                _wrecked = value;
            }
            get => _wrecked;
        }
        [System.NonSerializedAttribute] public bool _dead = false;
        public bool dead
        {
            set
            {
                if (value)
                { SendEventToExtensions("SFEXT_G_Dead"); }
                else
                { SendEventToExtensions("SFEXT_G_NotDead"); }
                _dead = value;
            }
            get => _dead;
        }
        [System.NonSerializedAttribute] public bool Using = false;
        [System.NonSerializedAttribute] public bool Occupied = false;
        [System.NonSerializedAttribute] public bool Passenger = false;
        [System.NonSerializedAttribute] public bool InVehicle = false;
        [System.NonSerializedAttribute] public bool InVR = false;
        [System.NonSerializedAttribute] public bool IsOwner;
        [System.NonSerializedAttribute] public bool Initialized;
        [System.NonSerializedAttribute] public int PilotSeat = -1;
        [System.NonSerializedAttribute] public int MySeat = -1;
        [System.NonSerializedAttribute] public int[] SeatedPlayers;
        [System.NonSerializedAttribute] public VRCStation[] VehicleStations;
        [System.NonSerializedAttribute] public SaccVehicleSeat[] VehicleSeats;
        [System.NonSerializedAttribute] public SaccEntity LastAttacker;
        [System.NonSerializedAttribute] public float PilotExitTime;
        [System.NonSerializedAttribute] public float PilotEnterTime;
        [System.NonSerializedAttribute] public bool Holding;
        [System.NonSerializedAttribute] public bool CoMSet = false;
        public void Init() { Start(); }
        private void Start()
        {
            if (Initialized) { return; }
            Initialized = true;
            localPlayer = Networking.LocalPlayer;
            if (localPlayer != null)
            {
                InEditor = false;
                IsOwner = localPlayer.isMaster;
                InVR = localPlayer.IsUserInVR();
            }
            else
            {
                Holding = true;
                Piloting = true;
                IsOwner = true;
                Using = true;
                InVehicle = true;
                Occupied = true;
            }
            for (int i = 0; i < EnableWhenOwner.Length; i++)
            { if (EnableWhenOwner[i]) { EnableWhenOwner[i].SetActive(IsOwner); } }
            Spawnposition = transform.localPosition;
            Spawnrotation = transform.localRotation;
            VehicleRigidbody = GetComponent<Rigidbody>();

            if (!CustomPickup_GrabPosition) { CustomPickup_GrabPosition = transform; }
            updateDisableInteractive();

            VehicleStations = GetComponentsInChildren<VRCStation>(true);
            //add EXTRASEATS to VehicleStations list
            if (ExternalSeats.Length > 0)
            {
                var temp = VehicleStations;
                VehicleStations = new VRCStation[temp.Length + ExternalSeats.Length];
                for (int i = 0; i < temp.Length; i++)
                { VehicleStations[i] = temp[i]; }
                for (int i = temp.Length; i < temp.Length + ExternalSeats.Length; i++)
                { VehicleStations[i] = ExternalSeats[i - temp.Length]; }
            }
            SeatedPlayers = new int[VehicleStations.Length];
            for (int i = 0; i != SeatedPlayers.Length; i++)
            { SeatedPlayers[i] = -1; }
            VehicleSeats = new SaccVehicleSeat[VehicleStations.Length];
            int numPassengerFunctions = 0;
            int[] passengerFuncsIndexs = new int[100];//xD
            for (int i = 0; i < VehicleSeats.Length; i++)
            {
                VehicleSeats[i] = (SaccVehicleSeat)VehicleStations[i].GetComponent<SaccVehicleSeat>();
                if (VehicleSeats[i])
                {
                    VehicleSeats[i].InitializeSeat();
                    //store which seats have passengerfunctions
                    if (VehicleSeats[i].PassengerFunctions)
                    {
                        passengerFuncsIndexs[numPassengerFunctions] = i;
                        numPassengerFunctions++;
                    }
                }
            }
            //get passenger function controllers from the seats
            PassengerFunctionControllers = new SAV_PassengerFunctionsController[numPassengerFunctions];
            for (int i = 0; i < numPassengerFunctions; i++)
            {
                PassengerFunctionControllers[i] = VehicleSeats[passengerFuncsIndexs[i]].PassengerFunctions;
            }
            EntityPickup = (VRC_Pickup)gameObject.GetComponent<VRC_Pickup>();
            EntityObjectSync = (VRC.SDK3.Components.VRCObjectSync)gameObject.GetComponent(typeof(VRC.SDK3.Components.VRCObjectSync));

            EntityColliders = gameObject.GetComponentsInChildren<Collider>();
            StartEntityLayer = gameObject.layer;

            //Dial Stuff
            LStickNumFuncs = Dial_Functions_L.Length;
            RStickNumFuncs = Dial_Functions_R.Length;
            DoDialLeft = LStickNumFuncs > 1;
            DoDialRight = RStickNumFuncs > 1;
            DisableLeftDial_ = 0;
            DisableRightDial_ = 0;
            LStickFuncDegrees = 360 / Mathf.Max((float)LStickNumFuncs, 1);
            RStickFuncDegrees = 360 / Mathf.Max((float)RStickNumFuncs, 1);
            LStickFuncDegreesDivider = 1 / LStickFuncDegrees;
            RStickFuncDegreesDivider = 1 / RStickFuncDegrees;
            LStickNULL = new bool[LStickNumFuncs];
            RStickNULL = new bool[RStickNumFuncs];
            int u = 0;
            foreach (UdonSharpBehaviour usb in Dial_Functions_L)
            {
                if (usb == null) { LStickNULL[u] = true; }
                u++;
            }
            u = 0;
            foreach (UdonSharpBehaviour usb in Dial_Functions_R)
            {
                if (usb == null) { RStickNULL[u] = true; }
                u++;
            }
            //work out angle to check against for function selection because straight up is the middle of a function
            Vector3 angle = new Vector3(0, 0, -1);
            if (LStickNumFuncs > 1)
            {
                if (LeftDialDivideStraightUp)
                {
                    LStickCheckAngle.x = 0;
                    LStickCheckAngle.y = -1;
                }
                else
                {
                    angle = Quaternion.Euler(0, -((360 / LStickNumFuncs) / 2), 0) * angle;
                    LStickCheckAngle.x = angle.x;
                    LStickCheckAngle.y = angle.z;
                }
            }

            angle = new Vector3(0, 0, -1);
            if (RStickNumFuncs > 1)
            {
                if (RightDialDivideStraightUp)
                {
                    RStickCheckAngle = Vector2.down;
                }
                else
                {
                    angle = Quaternion.Euler(0, -((360 / RStickNumFuncs) / 2), 0) * angle;
                    RStickCheckAngle.x = angle.x;
                    RStickCheckAngle.y = angle.z;
                }
            }

            foreach (UdonSharpBehaviour EXT in ExtensionUdonBehaviours)
            {
                if (EXT) EXT.SetProgramVariable("EntityControl", this);
            }
            foreach (UdonSharpBehaviour EXT in Dial_Functions_L)
            {
                if (EXT) EXT.SetProgramVariable("EntityControl", this);
            }
            foreach (UdonSharpBehaviour EXT in Dial_Functions_R)
            {
                if (EXT) EXT.SetProgramVariable("EntityControl", this);
            }
            foreach (UdonSharpBehaviour EXT in PassengerFunctionControllers)
            {
                if (EXT) EXT.SetProgramVariable("EntityControl", this);
            }
            hasPassengerFunctions = PassengerFunctionControllers.Length > 0;
            TellDFUNCsLR();
            OwnerAPI = Networking.GetOwner(gameObject);

            HierarchyName = gameObject.name;
            Transform VehicleNameTemp = gameObject.transform;
            while (VehicleNameTemp.parent)
            {
                VehicleNameTemp = VehicleNameTemp.parent;
                HierarchyName = VehicleNameTemp.gameObject.name + "/" + HierarchyName;
            }

            SendEventToExtensions("SFEXT_L_EntityStart");

            if (!CoMSet)
                SetCoM();
            if (!CenterOfMass) { CenterOfMass = transform; }

            //if in editor play mode without clientsim
            if (InEditor)
            {
                PilotEnterVehicleLocal();
                PilotEnterVehicleGlobal(null);
            }
        }
        void OnParticleCollision(GameObject other)
        {
            if (!other || dead || DisableBulletHitEvent) { return; }//avatars can't hurt you, and you can't get hurt when you're dead
            LastHitParticle = other;

            int dmg = 1;
            if (other.transform.childCount > 0)
            {
                string pname = other.transform.GetChild(0).name;
                getDamageValue(pname, ref dmg);
            }
            if (dmg < BulletArmorLevel) return;
            WeaponDamageVehicle(dmg, other);
        }
        void getDamageValue(string name, ref int dmg)
        {
            int index = name.LastIndexOf(':');
            if (index > -1)
            {
                name = name.Substring(index);
                if (name.Length == 3)
                {
                    if (name[1] == 'x')
                    {
                        if (name[2] >= '0' && name[2] <= '9')
                        {
                            dmg = name[2] - 48;
                            LastHitBulletDamageMulti = 1 / (float)(dmg);
                            dmg = -dmg;
                        }
                    }
                    else if (name[1] >= '0' && name[1] <= '9')
                    {
                        if (name[2] >= '0' && name[2] <= '9')
                        {
                            dmg = 10 * (name[1] - 48);
                            dmg += name[2] - 48;
                            LastHitBulletDamageMulti = dmg == 1 ? 1 : Mathf.Pow(2, dmg - 1);
                        }
                    }
                }
            }
        }
        public void WeaponDamageVehicle(int dmg, GameObject damagingObject)
        {
            //Try to find the saccentity that shot at us
            GameObject EnemyObjs = damagingObject;
            SaccEntity EnemyEntityControl = damagingObject.GetComponent<SaccEntity>();
            //search up the hierarchy to find the saccentity directly
            while (!EnemyEntityControl && EnemyObjs.transform.parent)
            {
                EnemyObjs = EnemyObjs.transform.parent.gameObject;
                EnemyEntityControl = EnemyObjs.GetComponent<SaccEntity>();
            }
            LastAttacker = EnemyEntityControl;
            //if failed to find it, search up the hierarchy for an udonsharpbehaviour with a reference to the saccentity (for instantiated missiles etc)
            if (!EnemyEntityControl)
            {
                EnemyObjs = damagingObject;
                UdonBehaviour EnemyUdonBehaviour = (UdonBehaviour)EnemyObjs.GetComponent(typeof(UdonBehaviour));
                while (!EnemyUdonBehaviour && EnemyObjs.transform.parent)
                {
                    EnemyObjs = EnemyObjs.transform.parent.gameObject;
                    EnemyUdonBehaviour = (UdonBehaviour)EnemyObjs.GetComponent(typeof(UdonBehaviour));
                }
                if (EnemyUdonBehaviour)
                { LastAttacker = (SaccEntity)EnemyUdonBehaviour.GetProgramVariable("EntityControl"); }
            }
            SendEventToExtensions("SFEXT_L_BulletHit");
            SendDamageEvent(dmg);
            if (LastAttacker && LastAttacker != this) { LastAttacker.SendEventToExtensions("SFEXT_L_DamageFeedback"); }
        }
        public void InVehicleControls()
        {
            if (!(InVehicle || Using)) { return; }
            SendCustomEventDelayedFrames(nameof(InVehicleControls), 1);
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (!InEditor)
                {
                    ExitVehicleCheck();
                }
            }
            if (!Using) { return; }
            Vector2 LStickPos = Vector2.zero;
            Vector2 RStickPos = Vector2.zero;
            float LTrigger = 0;
            float RTrigger = 0;
            if (!InEditor)
            {
                LStickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
                LStickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
                RStickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
                RStickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
                LTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
                RTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
            }

            //LStick Selection wheel
            if (DoDialLeft && !_DisableLeftDial)
            {
                if (InVR && LStickPos.magnitude > DialSensitivity)
                {
                    float stickdir = Vector2.SignedAngle(LStickCheckAngle, LStickPos);

                    stickdir = -(stickdir - 180);
                    int newselection = Mathf.FloorToInt(Mathf.Min(stickdir * LStickFuncDegreesDivider, LStickNumFuncs - 1));
                    if (!LStickNULL[newselection])
                    { LStickSelection = newselection; }
                }
                if (LStickSelection != LStickSelectionLastFrame)
                {
                    //new function selected, send deselected to old one
                    if (LStickSelectionLastFrame != -1 && Dial_Functions_L[LStickSelectionLastFrame] != null)
                    {
                        Dial_Functions_L[LStickSelectionLastFrame].SendCustomEvent("DFUNC_Deselected");
                    }
                    //get udonbehaviour for newly selected function and then send selected
                    if (LStickSelection > -1)
                    {
                        if (Dial_Functions_L[LStickSelection] != null)
                        {
                            Dial_Functions_L[LStickSelection].SendCustomEvent("DFUNC_Selected");
                        }
                    }
                    if (PlaySelectSoundLeft && SwitchFunctionSound) { SwitchFunctionSound.Play(); }
                    if (LStickDisplayHighlighter)
                    {
                        if (LStickSelection < 0)
                        { LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0); }
                        else
                        {
                            LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, -LStickFuncDegrees * LStickSelection);
                        }
                    }
                    LStickSelectionLastFrame = LStickSelection;
                }
            }

            //RStick Selection wheel
            if (DoDialRight && !_DisableRightDial)
            {
                if (InVR && RStickPos.magnitude > DialSensitivity)
                {
                    float stickdir = Vector2.SignedAngle(RStickCheckAngle, RStickPos);

                    stickdir = -(stickdir - 180);
                    int newselection = Mathf.FloorToInt(Mathf.Min(stickdir * RStickFuncDegreesDivider, RStickNumFuncs - 1));
                    if (!RStickNULL[newselection])
                    { RStickSelection = newselection; }
                }
                if (RStickSelection != RStickSelectionLastFrame)
                {
                    //new function selected, send deselected to old one
                    if (RStickSelectionLastFrame != -1 && Dial_Functions_R[RStickSelectionLastFrame])
                    {
                        Dial_Functions_R[RStickSelectionLastFrame].SendCustomEvent("DFUNC_Deselected");
                    }
                    //get udonbehaviour for newly selected function and then send selected
                    if (RStickSelection > -1)
                    {
                        if (Dial_Functions_R[RStickSelection])
                        {
                            Dial_Functions_R[RStickSelection].SendCustomEvent("DFUNC_Selected");
                        }
                    }
                    if (PlaySelectSoundRight && SwitchFunctionSound) { SwitchFunctionSound.Play(); }
                    if (RStickDisplayHighlighter)
                    {
                        if (RStickSelection < 0)
                        { RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0); }
                        else
                        {
                            RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 0, -RStickFuncDegrees * RStickSelection);
                        }
                    }
                    RStickSelectionLastFrame = RStickSelection;
                }
            }
        }
        private float LastJumpInput = 0f;
        public override void InputJump(bool value, VRC.Udon.Common.UdonInputEventArgs args)
        {
            if (InVehicle && InVR && args.boolValue)
            {
                ExitVehicleCheck();
            }
        }
        public void ExitVehicleCheck()
        {
            if (!DoubleTapToExit)
            { ExitStation(); }
            else
            {
                if (Time.time - LastJumpInput < .3f)
                { ExitStation(); return; }
                LastJumpInput = Time.time;
            }
        }
        private void OnEnable()
        {
            SendEventToExtensions("SFEXT_L_OnEnable");
            ConstantForce cf = GetComponent<ConstantForce>();
            if (cf)
            {
                cf.relativeForce = Vector3.zero;
                cf.relativeTorque = Vector3.zero;
            }
        }
        private void OnDisable()
        {
            SendEventToExtensions("SFEXT_L_OnDisable");
        }
        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            OwnerAPI = player;
            if (player.isLocal)
            {
                IsOwner = true;
                for (int i = 0; i < EnableWhenOwner.Length; i++)
                { if (EnableWhenOwner[i]) { EnableWhenOwner[i].SetActive(true); } }
                TakeOwnerShipOfExtensions();
                SendEventToExtensions("SFEXT_O_TakeOwnership");
                if (CustomPickup_Synced_isHeld && !CustomPickup_localHeld)
                {
                    //if we took ownership and CustomPickup_Synced_isHeld is true, check if we're holding it by checking these tags
                    //if we are not then we most likely took ownership of it from someone who quit or timed out while holding it
                    //we send the drop event
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendEvent_Drop));
                }
            }
            else
            {
                if (IsOwner)
                {
                    IsOwner = false;
                    for (int i = 0; i < EnableWhenOwner.Length; i++)
                    { if (EnableWhenOwner[i]) { EnableWhenOwner[i].SetActive(false); } }
                    SendEventToExtensions("SFEXT_O_LoseOwnership");
                    if (CustomPickup_localHeld)
                    {
                        //someone snatched it out of our hand
                        snatched = true;
                        CustomPickup_Drop();
                    }
                }
            }
            SendEventToExtensions("SFEXT_L_OwnershipTransfer");
        }
        private void EnableInVehicle_Enable()
        {
            for (int i = 0; i < EnableInVehicle.Length; i++)
            { if (EnableInVehicle[i]) EnableInVehicle[i].SetActive(true); }
        }
        private void EnableInVehicle_Disable()
        {
            for (int i = 0; i < EnableInVehicle.Length; i++)
            { if (EnableInVehicle[i]) EnableInVehicle[i].SetActive(false); }
        }
        private void DisableInVehicle_Enable()
        {
            for (int i = 0; i < DisableInVehicle.Length; i++)
            { if (DisableInVehicle[i]) DisableInVehicle[i].SetActive(true); }
        }
        private void DisableInVehicle_Disable()
        {
            for (int i = 0; i < DisableInVehicle.Length; i++)
            { if (DisableInVehicle[i]) DisableInVehicle[i].SetActive(false); }
        }
        private void EnableWhenHolding_Enable()
        {
            for (int i = 0; i < EnableWhenHolding.Length; i++)
            { if (EnableWhenHolding[i]) EnableWhenHolding[i].SetActive(true); }
        }
        private void EnableWhenHolding_Disable()
        {
            for (int i = 0; i < EnableWhenHolding.Length; i++)
            { if (EnableWhenHolding[i]) EnableWhenHolding[i].SetActive(false); }
        }
        private void DisableWhenHolding_Enable()
        {
            for (int i = 0; i < DisableWhenHolding.Length; i++)
            { if (DisableWhenHolding[i]) DisableWhenHolding[i].SetActive(true); }
        }
        private void DisableWhenHolding_Disable()
        {
            for (int i = 0; i < DisableWhenHolding.Length; i++)
            { if (DisableWhenHolding[i]) DisableWhenHolding[i].SetActive(false); }
        }
        public void PilotEnterVehicleLocal()//called from PilotSeat
        {
            Using = true;
            Piloting = true;
            if (!InEditor) { InVR = localPlayer.IsUserInVR(); }
            InVehicle = true; SendCustomEventDelayedFrames(nameof(InVehicleControls), 1);
            Occupied = true;
            localPlayer.SetPlayerTag("SF_LocalPiloting", "T");
            localPlayer.SetPlayerTag("SF_LocalInVehicle", "T");
            if (LStickNumFuncs == 1)
            {
                LStickSelection = 0;
                Dial_Functions_L[LStickSelection].SendCustomEvent("DFUNC_Selected");
            }
            if (RStickNumFuncs == 1)
            {
                LStickSelection = 0;
                Dial_Functions_R[LStickSelection].SendCustomEvent("DFUNC_Selected");
            }
            if (LStickDisplayHighlighter)
            { LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0); }
            if (RStickDisplayHighlighter)
            { RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0); }

            EnableInVehicle_Enable();
            DisableInVehicle_Disable();
            if (!_DisallowOwnerShipTransfer)
            {
                Networking.SetOwner(localPlayer, gameObject);
                TakeOwnerShipOfExtensions();
            }
            SendEventToExtensions("SFEXT_O_PilotEnter");
        }
        public void PilotEnterVehicleGlobal(VRCPlayerApi player)
        {
            if (player != null)
            {
                Occupied = true;
                UsersName = player.displayName;
                UsersID = player.playerId;
                PilotEnterTime = Time.time;
                player.SetPlayerTag("SF_InVehicle", "T");
                player.SetPlayerTag("SF_IsPilot", "T");
                player.SetPlayerTag("SF_VehicleName", HierarchyName);
                SendEventToExtensions("SFEXT_G_PilotEnter");
            }
        }
        [System.NonSerialized] public bool pilotLeftFlag;
        public void PilotExitVehicle(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                Using = false;
                Piloting = false;
                InVehicle = false;
                localPlayer.SetPlayerTag("SF_LocalPiloting", string.Empty);
                localPlayer.SetPlayerTag("SF_LocalInVehicle", string.Empty);
                EnableInVehicle_Disable();
                DisableInVehicle_Enable();
                SendEventToExtensions("SFEXT_O_PilotExit");
            }
            player.SetPlayerTag("SF_InVehicle", string.Empty);
            player.SetPlayerTag("SF_IsPilot", string.Empty);
            player.SetPlayerTag("SF_VehicleName", string.Empty);
            PilotExitTime = Time.time;
            LStickSelection = -1;
            RStickSelection = -1;
            LStickSelectionLastFrame = -1;
            RStickSelectionLastFrame = -1;
            SendEventToExtensions("SFEXT_G_PilotExit");
            Occupied = false;
            UsersName = string.Empty;
            UsersID = -1;
        }
        public void PassengerEnterVehicleLocal()
        {
            Passenger = true;
            InVehicle = true; SendCustomEventDelayedFrames(nameof(InVehicleControls), 1);
            localPlayer.SetPlayerTag("SF_LocalInVehicle", "T");
            if (LStickDisplayHighlighter)
            { LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0); }
            if (RStickDisplayHighlighter)
            { RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0); }
            if (!InEditor && localPlayer.IsUserInVR()) { InVR = true; }//move me to start when they fix the bug
            EnableInVehicle_Enable();
            DisableInVehicle_Disable();
            SendEventToExtensions("SFEXT_P_PassengerEnter");
        }
        public void PassengerExitVehicleLocal()
        {
            Passenger = false;
            InVehicle = false;
            localPlayer.SetPlayerTag("SF_LocalInVehicle", string.Empty);
            EnableInVehicle_Disable();
            DisableInVehicle_Enable();
            SendEventToExtensions("SFEXT_P_PassengerExit");
        }
        public void PassengerEnterVehicleGlobal(VRCPlayerApi player)
        {
            player.SetPlayerTag("SF_InVehicle", "T");
            player.SetPlayerTag("SF_VehicleName", HierarchyName);
            SendEventToExtensions("SFEXT_G_PassengerEnter");
        }
        public void PassengerExitVehicleGlobal(VRCPlayerApi player)
        {
            player.SetPlayerTag("SF_InVehicle", string.Empty);
            player.SetPlayerTag("SF_VehicleName", string.Empty);
            SendEventToExtensions("SFEXT_G_PassengerExit");
        }
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (IsOwner)
            {
                if (player == localPlayer) { return; }
                SendEventToExtensions("SFEXT_O_OnPlayerJoined");
                if (Holding)
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendEvent_Pickup)); }
            }
        }
        [System.NonSerialized] public int Pickup_Hand = 0;
        [System.NonSerialized] public bool Pickup_LeftHand;
        [System.NonSerialized] public bool CustomPickup_localHeld;
        public void CustomPickup_Grab()
        {
            if (string.IsNullOrEmpty(localPlayer.GetPlayerTag("SFCP_R")))
            {
                Pickup_Hand = 2;
                localPlayer.SetPlayerTag("SFCP_R", "T");
            }
            else if (string.IsNullOrEmpty(localPlayer.GetPlayerTag("SFCP_L")))
            {
                Pickup_Hand = 1;
                localPlayer.SetPlayerTag("SFCP_L", "T");
            }
            else { return; }
            int.TryParse(localPlayer.GetPlayerTag("SFCP_N"), out int numHolding);
            Networking.SetOwner(localPlayer, gameObject);
            CustomPickup_localHeld = true;
            numHolding++;
            localPlayer.SetPlayerTag("SFCP_N", numHolding.ToString());
            OnPickup();
            updateDisableInteractive();
            SendCustomEventDelayedFrames(nameof(CustomPickup_HoldLoop), 2);//so we don't pick it up and instantly fire (GetMouseButtonDown)
        }
        int numHolding_last;
        public void CustomPickup_HoldLoop()
        {
            Vector3 grabOffset = CustomPickup_GrabPosition.position - transform.position;
            if (Pickup_LeftHand)
            {
                transform.position = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position - grabOffset;
                if (CustomPickup_SetRotation)
                {
                    if (InVR)
                    { transform.rotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation * Quaternion.Euler(CustomPickup_RotationOffsetVR); }
                    else
                    { transform.rotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation * Quaternion.Euler(CustomPickup_RotationOffsetDesktop); }
                }
            }
            else
            {
                transform.position = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position - grabOffset;
                if (CustomPickup_SetRotation)
                {
                    if (InVR)
                    { transform.rotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation * Quaternion.Euler(CustomPickup_RotationOffsetVR); }
                    else
                    { transform.rotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation * Quaternion.Euler(CustomPickup_RotationOffsetDesktop); }
                }
            }

            float RStickPosY = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
            if (Input.GetKeyDown(CustomPickup_DropKey) || !IsOwner || RStickPosY < -0.9f)
            {
                CustomPickup_Drop();
                return;
            }
            int numHolding = int.Parse(localPlayer.GetPlayerTag("SFCP_N"));
            if (numHolding > numHolding_last) { OnPickupUseUp(); }
            numHolding_last = numHolding;
            if (!Pickup_LeftHand && numHolding == 1)
            {
                if (Input.GetMouseButtonDown(0))
                { OnPickupUseDown(); }
                if (Input.GetMouseButtonUp(0))
                { OnPickupUseUp(); }
            }
            else
            {
                if (Input.GetMouseButtonDown(Pickup_Hand - 1))
                { OnPickupUseDown(); }
                if (Input.GetMouseButtonUp(Pickup_Hand - 1))
                { OnPickupUseUp(); }
            }
            SendCustomEventDelayedFrames(nameof(CustomPickup_HoldLoop), 1, VRC.Udon.Common.Enums.EventTiming.Update);
        }
        private void CustomPickup_Drop()
        {
            if (!CustomPickup_localHeld) { return; }
            if (Pickup_LeftHand)
            { localPlayer.SetPlayerTag("SFCP_L", string.Empty); }
            else
            { localPlayer.SetPlayerTag("SFCP_R", string.Empty); }
            int.TryParse(localPlayer.GetPlayerTag("SFCP_N"), out int numHolding);
            numHolding = Mathf.Max(numHolding - 1, 0);
            localPlayer.SetPlayerTag("SFCP_N", numHolding.ToString());
            CustomPickup_localHeld = false;
            updateDisableInteractive();
            OnDrop();
        }
        public void updateDisableInteractive()
        {
            DisableInteractive = CustomPickup_localHeld || !EnableInteract || (CustomPickup_Synced_isHeld && !CustomPickup_AllowSteal);
        }
        public override void OnPickup()
        {
            Holding = true;
            Using = true;
            if (!Interact_CustomPickup) Pickup_Hand = (int)EntityPickup.currentHand;
            Pickup_LeftHand = Pickup_Hand == 1;
            EnableWhenHolding_Enable();
            DisableWhenHolding_Disable();
            if (!_DisallowOwnerShipTransfer) { TakeOwnerShipOfExtensions(); }
            if (LStickNumFuncs == 1)
            { Dial_Functions_L[0].SendCustomEvent("DFUNC_Selected"); }
            if (RStickNumFuncs == 1)
            { Dial_Functions_R[0].SendCustomEvent("DFUNC_Selected"); }
            if (Pickup_DisableCollisionOnGrab)
            {
                foreach (Collider col in EntityColliders)
                {
                    col.gameObject.layer = 9;
                    col.isTrigger = true;
                }
            }
            if (LStickDisplayHighlighter)
            { LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0); }
            if (RStickDisplayHighlighter)
            { RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0); }
            SendEventToExtensions("SFEXT_O_OnPickup");
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendEvent_Pickup));
            SendCustomEventDelayedFrames(nameof(InVehicleControls), 1);
        }
        public override void Interact()
        {
            if (Interact_CustomPickup) { CustomPickup_Grab(); }
            else { SendEventToExtensions("SFEXT_O_Interact"); }
        }
        bool snatched;
        public override void OnDrop()
        {
            if (!IsOwner) { snatched = true; }
            Holding = false;
            Using = false;
            EnableWhenHolding_Disable();
            DisableWhenHolding_Enable();
            SendEventToExtensions("SFEXT_O_OnDrop");
            if (Pickup_DisableCollisionOnGrab)
            {
                foreach (Collider col in EntityColliders)
                {
                    col.gameObject.layer = StartEntityLayer;
                    col.isTrigger = false;
                }
            }
            if (snatched)//Don't send drop if it was snatched because the drop event will arrive after the other player's grab event
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendEvent_Snatched)); snatched = false; }
            else
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendEvent_Drop)); }
        }
        [System.NonSerialized, FieldChangeCallback(nameof(CustomPickup_Synced_isHeld))] private bool _CustomPickup_Synced_isHeld = false;//can be used if the VRCPickup script is not in use
        public bool CustomPickup_Synced_isHeld
        {
            set
            {
                _CustomPickup_Synced_isHeld = value;
                updateDisableInteractive();
            }
            get => _CustomPickup_Synced_isHeld;
        }
        public void SendEvent_Pickup()
        {
            CustomPickup_Synced_isHeld = true;
            SendEventToExtensions("SFEXT_G_OnPickup");
        }
        public void SendEvent_Snatched()
        {
            SendEventToExtensions("SFEXT_G_OnSnatched");
        }
        public void SendEvent_Drop()
        {
            CustomPickup_Synced_isHeld = false;
            SendEventToExtensions("SFEXT_G_OnDrop");
        }
        public override void OnPickupUseDown()
        {
            SendEventToExtensions("SFEXT_O_OnPickupUseDown");
        }
        public override void OnPickupUseUp()
        {
            SendEventToExtensions("SFEXT_O_OnPickupUseUp");
        }
        [System.NonSerialized] VRCPlayerApi LastPlayerCollisionEnter;
        public override void OnPlayerCollisionEnter(VRCPlayerApi player)
        {
            LastPlayerCollisionEnter = player;
            SendEventToExtensions("SFEXT_L_OnPlayerCollisionEnter");
        }
        [System.NonSerialized] VRCPlayerApi LastPlayerCollisionExit;
        public override void OnPlayerCollisionExit(VRCPlayerApi player)
        {
            LastPlayerCollisionEnter = player;
            SendEventToExtensions("SFEXT_L_OnPlayerCollisionExit");
        }
        public void SetCoM()
        {
            //WARNING: Setting this will reset ITR in SaccAirVehicle etc.
            if (CenterOfMass)
            {
                if (VehicleRigidbody)
                {
                    VehicleRigidbody.centerOfMass = transform.InverseTransformDirection(CenterOfMass.position - transform.position);//correct position if scaled}
                }
            }
            else { Debug.LogWarning(gameObject.name + ": No CoM set"); }
        }
        [System.NonSerialized] public Collision LastCollisionEnter;
        private void OnCollisionEnter(Collision Col)
        {
            LastCollisionEnter = Col;
            SendEventToExtensions("SFEXT_L_OnCollisionEnter");
        }
        [System.NonSerialized] public Collider LastTriggerEnter;
        private void OnTriggerEnter(Collider Trig)
        {
            LastTriggerEnter = Trig;
            SendEventToExtensions("SFEXT_L_OnTriggerEnter");
        }
        [System.NonSerialized] public Collision LastCollisionExit;
        private void OnCollisionExit(Collision Col)
        {
            LastCollisionExit = Col;
            SendEventToExtensions("SFEXT_L_OnCollisionExit");
        }
        [System.NonSerialized] public Collider LastTriggerExit;
        private void OnTriggerExit(Collider Trig)
        {
            LastTriggerExit = Trig;
            SendEventToExtensions("SFEXT_L_OnTriggerExit");
        }

        [System.NonSerialized] public int DialFuncPos;
        public void TellDFUNCsLR()
        {
            for (DialFuncPos = 0; DialFuncPos < Dial_Functions_L.Length; DialFuncPos++)
            {
                if (Dial_Functions_L[DialFuncPos])
                {
                    Dial_Functions_L[DialFuncPos].SetProgramVariable("LeftDial", true);
                    Dial_Functions_L[DialFuncPos].SetProgramVariable("DialPosition", DialFuncPos);
                }
            }
            for (DialFuncPos = 0; DialFuncPos < Dial_Functions_R.Length; DialFuncPos++)
            {
                if (Dial_Functions_R[DialFuncPos])
                {
                    Dial_Functions_R[DialFuncPos].SetProgramVariable("LeftDial", false);
                    Dial_Functions_R[DialFuncPos].SetProgramVariable("DialPosition", DialFuncPos);
                }
            }
        }
        public void TakeOwnerShipOfExtensions()
        {
            if (!InEditor)
            {
                foreach (UdonSharpBehaviour EXT in ExtensionUdonBehaviours)
                { if (EXT) { if (!localPlayer.IsOwner(EXT.gameObject)) { Networking.SetOwner(localPlayer, EXT.gameObject); } } }
                foreach (UdonSharpBehaviour EXT in Dial_Functions_L)
                { if (EXT) { if (!localPlayer.IsOwner(EXT.gameObject)) { Networking.SetOwner(localPlayer, EXT.gameObject); } } }
                foreach (UdonSharpBehaviour EXT in Dial_Functions_R)
                { if (EXT) { if (!localPlayer.IsOwner(EXT.gameObject)) { Networking.SetOwner(localPlayer, EXT.gameObject); } } }
                foreach (SAV_PassengerFunctionsController EXT in PassengerFunctionControllers)
                { if (EXT && !EXT.Occupied) { EXT.TakeOwnerShipOfExtensions(); } }
            }
        }
        [System.NonSerialized] public bool passengerFuncIgnorePassengerFlag;
        [RecursiveMethod]
        public void SendEventToExtensions(string eventname)
        {
            if (!Initialized) { return; }
            foreach (UdonSharpBehaviour EXT in ExtensionUdonBehaviours)
            {
                if (EXT)
                { EXT.SendCustomEvent(eventname); }
            }
            foreach (UdonSharpBehaviour EXT in Dial_Functions_L)
            {
                if (EXT)
                { EXT.SendCustomEvent(eventname); }
            }
            foreach (UdonSharpBehaviour EXT in Dial_Functions_R)
            {
                if (EXT)
                { EXT.SendCustomEvent(eventname); }
            }
            if (hasPassengerFunctions)
            {
                if (passengerFuncIgnorePassengerFlag)
                {
                    passengerFuncIgnorePassengerFlag = false;
                    if (eventname.Contains("_Passenger"))
                    {
                        // Don't send PassengerEnter etc to PassengerFunctionsController when the passenger controlling the functions enters
                        // For those functions to work this passenger is sent as 'pilot' from SaccVehicleSeat.PassengerFunctions
                        // SFEXT_P_PassengerEnter
                        // SFEXT_P_PassengerExit
                        // SFEXT_G_PassengerEnter
                        // SFEXT_G_PassengerExit
                        return;
                    }
                }
                foreach (SAV_PassengerFunctionsController EXT in PassengerFunctionControllers)
                {
                    if (EXT)
                    {
                        EXT.SendCustomEvent(eventname);
                        if (
                            eventname == "SFEXT_L_EntityStart" ||
                            eventname == "SFEXT_O_LoseOwnership"
                        ) continue;
                        if (EXT.Occupied && eventname == "SFEXT_O_TakeOwnership") continue;
                        //Pilot from SaccEntity is the Pilot of the vehicle
                        //DFUNCs assume that the pilot of the vehicle should control the DFUNC
                        //but since this controls passenger functions we turn the pilot events into passenger events
                        string passengerEventName = eventname;
                        passengerEventName = passengerEventName.Replace("G_PilotEnter", "G_PassengerEnter")
                        .Replace("G_PilotExit", "G_PassengerExit")
                        .Replace("O_PilotEnter", "P_PassengerEnter")
                        .Replace("O_PilotExit", "P_PassengerExit");

                        EXT.SendEventToExtensions_Gunner(passengerEventName);
                    }
                }
            }
        }
        public void ExitStation()
        {
            if (MySeat > -1 && MySeat < VehicleStations.Length)
            { VehicleStations[MySeat].ExitStation(localPlayer); }
        }
        [System.NonSerializedAttribute] public Vector3 Spawnposition;
        [System.NonSerializedAttribute] public Quaternion Spawnrotation;
        private float lastRespawnTime;
        public void EntityRespawn()//can be used by simple items to respawn
        {
            if (Time.time - lastRespawnTime < 3) { return; }
            VRCPlayerApi currentOwner = Networking.GetOwner(gameObject);
            bool BlockedCheck = (currentOwner != null && currentOwner.GetBonePosition(HumanBodyBones.Hips) == Vector3.zero);
            if (Occupied || _dead || BlockedCheck) { return; }
            if (!Occupied && !_dead && (!EntityPickup || !EntityPickup.IsHeld) && !CustomPickup_Synced_isHeld)
            {
                lastRespawnTime = Time.time;
                Networking.SetOwner(localPlayer, gameObject);
                IsOwner = true;
                if (EntityObjectSync)
                {
                    EntityObjectSync.Respawn();
                }
                else
                {
                    transform.localPosition = Spawnposition;
                    transform.localRotation = Spawnrotation;
                }
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;//editor needs this
                    rb.position = transform.position;
                    rb.rotation = transform.rotation;
                }
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendRespawn));
            }
        }
        public void SendRespawn() { SendEventToExtensions("SFEXT_G_RespawnButton"); }
        // ToDo: Use static to better performance on U#1.0
        // public static UdonSharpBehaviour GetExtention(SaccEntity entity, string udonTypeName)
        public UdonSharpBehaviour GetExtention(string udonTypeName)
        {
            SaccEntity entity = this;

            foreach (var extention in entity.ExtensionUdonBehaviours)
            {
                if (extention && extention.GetUdonTypeName() == udonTypeName) return extention;
            }
            foreach (var extention in entity.Dial_Functions_L)
            {
                if (extention && extention.GetUdonTypeName() == udonTypeName) return extention;
            }
            foreach (var extention in entity.Dial_Functions_R)
            {
                if (extention && extention.GetUdonTypeName() == udonTypeName) return extention;
            }
            return null;
        }

        // ToDo: Use static to better performance on U#1.0
        // public static UdonSharpBehaviour[] GetExtentions(SaccEntity entity, string udonTypeName)
        public UdonSharpBehaviour[] GetExtentions(string udonTypeName)
        {
            SaccEntity entity = this;

            var result = new UdonSharpBehaviour[entity.ExtensionUdonBehaviours.Length + entity.Dial_Functions_L.Length + entity.Dial_Functions_R.Length];
            var count = 0;
            foreach (var extention in entity.ExtensionUdonBehaviours)
            {
                if (extention && extention.GetUdonTypeName() == udonTypeName)
                {
                    result[count++] = extention;
                }
            }
            foreach (var extention in entity.Dial_Functions_L)
            {
                if (extention && extention.GetUdonTypeName() == udonTypeName)
                {
                    result[count++] = extention;
                }
            }
            foreach (var extention in entity.Dial_Functions_R)
            {
                if (extention && extention.GetUdonTypeName() == udonTypeName)
                {
                    result[count++] = extention;
                }
            }

            var finalResult = new UdonSharpBehaviour[count];
            System.Array.Copy(result, finalResult, count);

            return finalResult;
        }
        public void ToggleStickSelectionLeft(UdonSharpBehaviour dfunc)
        {
            var index = System.Array.IndexOf(Dial_Functions_L, dfunc);
            if (LStickSelection == index)
            {
                LStickSelection = -1;
                dfunc.SendCustomEvent("DFUNC_Deselected");
            }
            else
            {
                LStickSelection = index;
                dfunc.SendCustomEvent("DFUNC_Selected");
            }
        }

        public void ToggleStickSelectionRight(UdonSharpBehaviour dfunc)
        {
            var index = System.Array.IndexOf(Dial_Functions_R, dfunc);
            if (RStickSelection == index)
            {
                RStickSelection = -1;
                dfunc.SendCustomEvent("DFUNC_Deselected");
            }
            else
            {
                RStickSelection = index;
                dfunc.SendCustomEvent("DFUNC_Selected");
            }
        }
        public void SetDeadFor(float deadtime)
        {
            dead = true;
            SendCustomEventDelayedSeconds(nameof(UnsetSetDead), deadtime);
        }
        public void UnsetSetDead() { dead = false; }
        public void SendDamageEvent(int dmg)
        {
            switch (dmg)
            {
                case 2:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage2x));
                    break;
                case 3:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage4x));
                    break;
                case 4:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage8x));
                    break;
                case 5:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage16x));
                    break;
                case 6:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage32x));
                    break;
                case 7:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage64x));
                    break;
                case 8:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage128x));
                    break;
                case 9:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage256x));
                    break;
                case 10:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage512x));
                    break;
                case 11:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage1024x));
                    break;
                case 12:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage2048x));
                    break;
                case 13:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage4096x));
                    break;
                case 14:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamage8192x));
                    break;

                case -2:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageHalf));
                    break;
                case -3:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageThird));
                    break;
                case -4:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageQuarter));
                    break;
                case -5:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageFifth));
                    break;
                case -6:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageSixth));
                    break;
                case -7:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageSeventh));
                    break;
                case -8:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageEighth));
                    break;
                case -9:
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageNinth));
                    break;
                default:
                    if (dmg != 1) { Debug.LogWarning("Invalid bullet damage, using default"); }
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(BulletDamageDefault));
                    break;
            }
        }
        [System.NonSerializedAttribute] public float LastHitBulletDamageMulti = 1;
        [System.NonSerializedAttribute] public byte LastHitBulletType = 0;// 0=bullet 1=tank shell
        public void BulletDamageNinth()
        {
            LastHitBulletDamageMulti = .11111111111111f;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void BulletDamageEighth()
        {
            LastHitBulletDamageMulti = .125f;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void BulletDamageSeventh()
        {
            LastHitBulletDamageMulti = .14285714285714f;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void BulletDamageSixth()
        {
            LastHitBulletDamageMulti = .16666666666666f;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void BulletDamageFifth()
        {
            LastHitBulletDamageMulti = .2f;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void BulletDamageQuarter()
        {
            LastHitBulletDamageMulti = .25f;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void BulletDamageThird()
        {
            LastHitBulletDamageMulti = .33333333333333f;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void BulletDamageHalf()
        {
            LastHitBulletDamageMulti = .5f;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void BulletDamageDefault()
        {
            LastHitBulletDamageMulti = 1;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void BulletDamage2x()
        {
            LastHitBulletDamageMulti = 2;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void BulletDamage4x()
        {
            LastHitBulletDamageMulti = 4;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void BulletDamage8x()
        {
            LastHitBulletDamageMulti = 8;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void BulletDamage16x()
        {
            LastHitBulletDamageMulti = 16;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void BulletDamage32x()
        {
            LastHitBulletDamageMulti = 32;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void BulletDamage64x()
        {
            LastHitBulletDamageMulti = 64;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void BulletDamage128x()
        {
            LastHitBulletDamageMulti = 128;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void BulletDamage256x()
        {
            LastHitBulletDamageMulti = 256;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void BulletDamage512x()
        {
            LastHitBulletDamageMulti = 512;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void BulletDamage1024x()
        {
            LastHitBulletDamageMulti = 1024;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void BulletDamage2048x()
        {
            LastHitBulletDamageMulti = 2048;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void BulletDamage4096x()
        {
            LastHitBulletDamageMulti = 4096;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void BulletDamage8192x()
        {
            LastHitBulletDamageMulti = 8192;
            SendEventToExtensions("SFEXT_G_BulletHit");
        }
        public void SetWrecked()
        {
            wrecked = true;
        }
        public void SetWreckedFalse()
        {
            wrecked = false;
        }
        public void ReSupply()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ReSupply_Event));
        }
        [System.NonSerialized] public uint ReSupplied;
        private float LastResupplyTime = 0;
        public void ReSupply_Event()
        {
            ReSupplied = 0;//used to know if other scripts resupplied
            SendEventToExtensions("SFEXT_G_ReSupply");//extensions increase the ReSupplied value too
            LastResupplyTime = Time.time;
        }
        public void ReFuel()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ReFuel_Event));
        }
        public void ReFuel_Event()
        {
            SendEventToExtensions("SFEXT_G_ReFuel");//extensions increase the ReSupplied value too
        }
    }
}