
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
[DefaultExecutionOrder(-10)]
public class SaccEntity : UdonSharpBehaviour
{
    [Tooltip("Put all scripts used by this vehicle that use the event system into this list (excluding DFUNCs)")]
    public UdonSharpBehaviour[] ExtensionUdonBehaviours;
    [Tooltip("Function dial scripts that you wish to be on the left dial")]
    public UdonSharpBehaviour[] Dial_Functions_L;
    [Tooltip("Function dial scripts that you wish to be on the right dial")]
    public UdonSharpBehaviour[] Dial_Functions_R;
    [Tooltip("Should there be a function at the top middle of the function dial[ ]? Or a divider[x]? Useful for adjusting function positions with an odd number of functions")]
    public bool LeftDialDivideStraightUp = false;
    [Tooltip("See above")]
    public bool RightDialDivideStraightUp = false;
    [Tooltip("Layer to spherecast to find all triggers on to use as AAM targets")]
    public LayerMask AAMTargetsLayer = 1 << 25;//layer 25
    [Tooltip("Object that is enabled when entering vehicle in any seat")]
    public GameObject InVehicleOnly;
    [Tooltip("Object that is enabled when holding this object")]
    public GameObject HoldingOnly;
    [Tooltip("To tell child scripts/rigidbodys where the center of the vehicle is")]
    public Transform CenterOfMass;
    [Tooltip("Oneshot sound played each time function selection changes")]
    public AudioSource SwitchFunctionSound;
    public Transform LStickDisplayHighlighter;
    public Transform RStickDisplayHighlighter;
    [Tooltip("Any objects in this list get set inactive after 10 seconds, used to disable AAMTarget object for vehicles that should never be targetable but that should be in the targets list for the camera etc")]
    public GameObject[] DisableAfter10Seconds;
    [System.NonSerializedAttribute] public bool InEditor = true;
    private VRCPlayerApi localPlayer;
    [System.NonSerializedAttribute] public bool Piloting;
    [System.NonSerializedAttribute] public int UsersID;
    [System.NonSerializedAttribute] public string UsersName;
    [System.NonSerializedAttribute] public GameObject[] AAMTargets = new GameObject[80];
    [System.NonSerializedAttribute] public int NumAAMTargets = 0;
    private Vector2 RStickCheckAngle;
    private Vector2 LStickCheckAngle;
    [System.NonSerializedAttribute] public GameObject LastHitParticle;
    [System.NonSerializedAttribute] public float LStickFuncDegrees;
    [System.NonSerializedAttribute] public float RStickFuncDegrees;
    [System.NonSerializedAttribute] public float LStickFuncDegreesDivider;
    [System.NonSerializedAttribute] public float RStickFuncDegreesDivider;
    [System.NonSerializedAttribute] public int LStickNumFuncs;
    [System.NonSerializedAttribute] public int RStickNumFuncs;
    [System.NonSerializedAttribute] public bool LStickDoDial;
    [System.NonSerializedAttribute] public bool RStickDoDial;
    [System.NonSerializedAttribute] public bool[] LStickNULL;
    [System.NonSerializedAttribute] public bool[] RStickNULL;
    [System.NonSerializedAttribute] public int RStickSelection = -1;
    [System.NonSerializedAttribute] public int LStickSelection = -1;
    [System.NonSerializedAttribute] public int RStickSelectionLastFrame = -1;
    [System.NonSerializedAttribute] public int LStickSelectionLastFrame = -1;
    [System.NonSerializedAttribute] private bool _dead = false;
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
    [System.NonSerializedAttribute] public bool Passenger = false;
    [System.NonSerializedAttribute] public bool InVehicle = false;
    [System.NonSerializedAttribute] public bool InVR = false;
    [System.NonSerializedAttribute] public bool IsOwner;
    [System.NonSerializedAttribute] public bool Initialized;

    //old Leavebutton Stuff
    [System.NonSerializedAttribute] public int PilotSeat = -1;
    [System.NonSerializedAttribute] public int MySeat = -1;
    [System.NonSerializedAttribute] public int[] SeatedPlayers;
    [System.NonSerializedAttribute] public VRCStation[] VehicleStations;
    [System.NonSerializedAttribute] public int[] InsidePlayers;
    [System.NonSerializedAttribute] public SaccEntity LastAttacker;
    [System.NonSerializedAttribute] public float PilotExitTime;
    [System.NonSerializedAttribute] public float PilotEnterTime;
    [System.NonSerializedAttribute] public bool Holding;
    //end of old Leavebutton stuff
    private void Start()
    {
        Initialized = true;
        localPlayer = Networking.LocalPlayer;
        if (localPlayer != null)
        {
            InEditor = false;
            if (localPlayer.isMaster) { IsOwner = true; }
        }
        else
        {
            IsOwner = true;
            Using = true;
            InVehicle = true;
        }

        if (!CenterOfMass)
        {
            CenterOfMass = gameObject.transform;
            Debug.Log(string.Concat(gameObject.name, ": ", "No Center Of Mass Set"));
        }

        VehicleStations = (VRC.SDK3.Components.VRCStation[])GetComponentsInChildren(typeof(VRC.SDK3.Components.VRCStation));
        SeatedPlayers = new int[VehicleStations.Length];
        for (int i = 0; i != SeatedPlayers.Length; i++)
        {
            SeatedPlayers[i] = -1;
        }
        FindAAMTargets();

        TellDFUNCsLR();
        SendEventToExtensions("SFEXT_L_EntityStart");


        //Dial Stuff
        LStickNumFuncs = Dial_Functions_L.Length;
        RStickNumFuncs = Dial_Functions_R.Length;
        LStickDoDial = LStickNumFuncs > 1;
        RStickDoDial = RStickNumFuncs > 1;
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
                RStickCheckAngle.x = 0;
                RStickCheckAngle.y = -1;
            }
            else
            {
                angle = Quaternion.Euler(0, -((360 / RStickNumFuncs) / 2), 0) * angle;
                RStickCheckAngle.x = angle.x;
                RStickCheckAngle.y = angle.z;
            }
        }
        SendCustomEventDelayedSeconds(nameof(Disable10), 10);
        //if in editor play mode without cyanemu
        if (InEditor)
        {
            PilotEnterVehicleLocal();
            PilotEnterVehicleGlobal(null);
        }
    }
    void OnParticleCollision(GameObject other)
    {
        if (!other || dead) { return; }//avatars can't hurt you, and you can't get hurt when you're dead

        LastHitParticle = other;
        SendEventToExtensions("SFEXT_L_BulletHit");

        //Try to find the saccentity that shot at us
        GameObject EnemyObjs = other;
        SaccEntity EnemyEntityControl = null;
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
            EnemyObjs = other;
            UdonBehaviour EnemyUdonBehaviour = null;
            while (!EnemyUdonBehaviour && EnemyObjs.transform.parent)
            {
                EnemyObjs = EnemyObjs.transform.parent.gameObject;
                EnemyUdonBehaviour = (UdonBehaviour)EnemyObjs.GetComponent(typeof(UdonBehaviour));
            }
            if (EnemyUdonBehaviour)
            { LastAttacker = (SaccEntity)EnemyUdonBehaviour.GetProgramVariable("EntityControl"); }
        }
    }
    private void Update()
    {
        if (Using)
        {
            Vector2 LStickPos = new Vector2(0, 0);
            Vector2 RStickPos = new Vector2(0, 0);
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
            if (LStickDoDial)
            {
                if (InVR && LStickPos.magnitude > .7f)
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
                    if (SwitchFunctionSound) { SwitchFunctionSound.Play(); }
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
            if (RStickDoDial)
            {
                if (InVR && RStickPos.magnitude > .7f)
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
                    if (SwitchFunctionSound) { SwitchFunctionSound.Play(); }
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

        if (InVehicle)
        {
            if (Input.GetKeyDown(KeyCode.Return) || (InVR && Input.GetButtonDown("Oculus_CrossPlatform_Button4")))
            { if (!InEditor) ExitStation(); }
        }
    }
    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        if (player.isLocal)
        {
            IsOwner = true;
            SendEventToExtensions("SFEXT_O_TakeOwnership");
        }
        else
        {
            if (IsOwner)
            {
                IsOwner = false;
                SendEventToExtensions("SFEXT_O_LoseOwnership");
            }
        }
        SendEventToExtensions("SFEXT_L_OwnershipTransfer");
    }
    public void PilotEnterVehicleLocal()//called from PilotSeat
    {
        Using = true;
        Piloting = true;
        InVehicle = true;
        if (LStickNumFuncs == 1)
        {
            Dial_Functions_L[0].SendCustomEvent("DFUNC_Selected");
        }
        if (RStickNumFuncs == 1)
        {
            Dial_Functions_R[0].SendCustomEvent("DFUNC_Selected");
        }
        if (LStickDisplayHighlighter)
        { LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0); }
        if (RStickDisplayHighlighter)
        { RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0); }

        if (!InEditor && localPlayer.IsUserInVR()) { InVR = true; }//move me to start when they fix the bug
        //https://feedback.vrchat.com/vrchat-udon-closed-alpha-bugs/p/vrcplayerapiisuserinvr-for-the-local-player-is-not-returned-correctly-when-calle
        if (InVehicleOnly) { InVehicleOnly.SetActive(true); }

        Networking.SetOwner(localPlayer, gameObject);
        TakeOwnerShipOfExtensions();
        SendEventToExtensions("SFEXT_O_PilotEnter");
    }
    public void PilotEnterVehicleGlobal(VRCPlayerApi player)
    {
        if (player != null)
        {
            UsersName = player.displayName;
            UsersID = player.playerId;
            PilotEnterTime = Time.time;
            SendEventToExtensions("SFEXT_G_PilotEnter");
        }
    }
    public void PilotExitVehicle(VRCPlayerApi player)
    {
        //do this one frame later to ensure any script running pilotexit, explode, etc has access to the values
        SendCustomEventDelayedFrames(nameof(SetUserNull), 1);
        PilotExitTime = Time.time;
        LStickSelection = -1;
        RStickSelection = -1;
        LStickSelectionLastFrame = -1;
        RStickSelectionLastFrame = -1;
        SendEventToExtensions("SFEXT_G_PilotExit");
        if (player.isLocal)
        {
            Using = false;
            Piloting = false;
            InVehicle = false;
            if (InVehicleOnly) { InVehicleOnly.SetActive(false); }
            { SendEventToExtensions("SFEXT_O_PilotExit"); }
        }
    }
    public void SetUserNull()
    {
        UsersName = string.Empty;
        UsersID = -1;
    }
    public void PassengerEnterVehicleLocal()
    {
        Passenger = true;
        InVehicle = true;
        if (LStickDisplayHighlighter)
        { LStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0); }
        if (RStickDisplayHighlighter)
        { RStickDisplayHighlighter.localRotation = Quaternion.Euler(0, 180, 0); }
        if (!InEditor && localPlayer.IsUserInVR()) { InVR = true; }//move me to start when they fix the bug
        if (InVehicleOnly) { InVehicleOnly.SetActive(true); }
        SendEventToExtensions("SFEXT_P_PassengerEnter");
    }
    public void PassengerExitVehicleLocal()
    {
        Passenger = false;
        InVehicle = false;
        if (InVehicleOnly) { InVehicleOnly.SetActive(false); }
        SendEventToExtensions("SFEXT_P_PassengerExit");
    }
    public void PassengerEnterVehicleGlobal()
    {
        SendEventToExtensions("SFEXT_G_PassengerEnter");
    }
    public void PassengerExitVehicleGlobal()
    {
        SendEventToExtensions("SFEXT_G_PassengerExit");
    }
    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (IsOwner)
        { SendEventToExtensions("SFEXT_O_OnPlayerJoined"); }
    }
    public override void OnPickup()
    {
        Holding = true;
        Using = true;
        if (HoldingOnly) { HoldingOnly.SetActive(true); }
        TakeOwnerShipOfExtensions();
        SendEventToExtensions("SFEXT_O_OnPickup");
    }
    public override void OnDrop()
    {
        Holding = false;
        Using = false;
        if (HoldingOnly) { HoldingOnly.SetActive(false); }
        SendEventToExtensions("SFEXT_O_OnDrop");
    }
    public override void OnPickupUseDown()
    {
        SendEventToExtensions("SFEXT_O_OnPickupUseDown");
    }
    private void FindAAMTargets()
    {
        //get array of AAM Targets
        Collider[] aamtargs = Physics.OverlapSphere(transform.position, 1000000, AAMTargetsLayer, QueryTriggerInteraction.Collide);
        int n = 0;

        //work out which index in the aamtargs array is our own vehicle by finding which one has this script as it's parent
        //allows for each team to have a different layer for AAMTargets
        int self = -1;
        n = 0;
        foreach (Collider target in aamtargs)
        {
            if (target.transform.parent && target.transform.parent == transform)
            {
                self = n;
            }
            n++;
        }
        //populate AAMTargets list excluding our own vehicle
        n = 0;
        int foundself = 0;
        foreach (Collider target in aamtargs)
        {
            if (n == self) { foundself = 1; n++; }
            else
            {
                AAMTargets[n - foundself] = target.gameObject;
                n++;
            }
        }
        if (aamtargs.Length > 0)
        {
            if (foundself != 0)
            {
                NumAAMTargets = Mathf.Clamp(aamtargs.Length - 1, 0, 999);//one less because it found our own vehicle
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
            for (int i = 0; AAMTargets[n]; i++)
            {
                Transform parent = AAMTargets[n].transform;
                for (int x = 0; parent; x++)
                {
                    order[n] = float.Parse($"{(int)order[n]}{parent.transform.GetSiblingIndex()}");
                    parent = parent.transform.parent;
                }
                n++;
            }
            //sort AAMTargets array based on order

            SortTargets(AAMTargets, order);
        }
        else
        {
            Debug.LogWarning(string.Concat(gameObject.name, ": NO AAM TARGETS FOUND"));
            AAMTargets[0] = gameObject;//this should prevent HUDController from crashing with a null reference while causing no ill effects
        }
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
    public void Disable10()
    {
        foreach (GameObject obj in DisableAfter10Seconds)
        {
            if (obj) { obj.SetActive(false); }
        }
    }
    public void TellDFUNCsLR()
    {
        foreach (UdonSharpBehaviour EXT in Dial_Functions_L)
        {
            if (EXT)
            { EXT.SendCustomEvent("DFUNC_LeftDial"); }
        }
        foreach (UdonSharpBehaviour EXT in Dial_Functions_R)
        {
            if (EXT)
            { EXT.SendCustomEvent("DFUNC_RightDial"); }
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
        }
    }
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
    }
    public void ExitStation()
    {
        if (MySeat > -1 && MySeat < VehicleStations.Length)
        { VehicleStations[MySeat].ExitStation(localPlayer); }
    }
}
