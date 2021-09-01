
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SaccEntity : UdonSharpBehaviour
{
    [Tooltip("Put all scripts used by this vehicle that use the event system into this list (excluding DFUNCs)")]
    public UdonSharpBehaviour[] ExtensionUdonBehaviours;
    [Tooltip("Function dial scripts that you wish to be on the left dial")]
    public UdonSharpBehaviour[] Dial_Functions_L;
    [Tooltip("Function dial scripts that you wish to be on the right dial")]
    public UdonSharpBehaviour[] Dial_Functions_R;
    [Tooltip("Layer to spherecast to find all triggers on to use as AAM targets")]
    public LayerMask AAMTargetsLayer = 1 << 25;//layer 25
    [Tooltip("Object that is enabled when entering vehicle in any seat")]
    public GameObject InVehicleOnly;
    [Tooltip("Object that is enabled when entering vehicle in pilot seat")]
    public GameObject PilotOnly;
    [Tooltip("To tell child scripts/rigidbodys where the center of the vehicle is")]
    public Transform CenterOfMass;
    [System.NonSerializedAttribute] public float LastHitTime = -100;
    [System.NonSerializedAttribute] public bool InEditor = true;
    VRC.SDK3.Components.VRCObjectSync VehicleObjectSync;
    private VRCPlayerApi localPlayer;
    [System.NonSerializedAttribute] public int PilotID;
    [System.NonSerializedAttribute] public string PilotName;
    [System.NonSerializedAttribute] public GameObject[] AAMTargets = new GameObject[80];
    [System.NonSerializedAttribute] public int NumAAMTargets = 0;
    private Vector2 RStickCheckAngle;
    private Vector2 LStickCheckAngle;
    private UdonSharpBehaviour CurrentSelectedFunctionL;
    private UdonSharpBehaviour CurrentSelectedFunctionR;
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
    [System.NonSerializedAttribute] public bool dead = false;
    [System.NonSerializedAttribute] public bool Piloting = false;
    [System.NonSerializedAttribute] public bool Passenger = false;
    [System.NonSerializedAttribute] public bool InVehicle = false;
    [System.NonSerializedAttribute] public bool InVR = false;
    private bool IsOwner;

    //old Leavebutton Stuff
    [System.NonSerializedAttribute] public int PilotSeat = -1;
    [System.NonSerializedAttribute] public int MySeat = -1;
    [System.NonSerializedAttribute] public int[] SeatedPlayers;
    [System.NonSerializedAttribute] public VRCStation[] VehicleStations;
    [System.NonSerializedAttribute] public int[] InsidePlayers;
    [System.NonSerializedAttribute] public SaccEntity LastAttacker;
    [System.NonSerializedAttribute] public float PilotExitTime;
    [System.NonSerializedAttribute] public float PilotEnterTime;
    private bool FindSeatsDone = false;
    //end of old Leavebutton stuff
    private void Start()
    {
        localPlayer = Networking.LocalPlayer;
        if (localPlayer != null)
        {
            InEditor = false;
            if (localPlayer.isMaster) { IsOwner = true; }
        }
        else
        {
            IsOwner = true;
            Piloting = true;
            InVehicle = true;
        }

        VehicleObjectSync = (VRC.SDK3.Components.VRCObjectSync)GetComponent(typeof(VRC.SDK3.Components.VRCObjectSync));
        if (CenterOfMass == null)
        { CenterOfMass = gameObject.transform; }

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
        if (LStickNumFuncs > 0) { angle = Quaternion.Euler(0, -((360 / LStickNumFuncs) / 2), 0) * angle; }
        LStickCheckAngle.x = angle.x;
        LStickCheckAngle.y = angle.z;

        angle = new Vector3(0, 0, -1);
        if (RStickNumFuncs > 0) { angle = Quaternion.Euler(0, -((360 / RStickNumFuncs) / 2), 0) * angle; }
        RStickCheckAngle.x = angle.x;
        RStickCheckAngle.y = angle.z;

        //if in edit mode without cyanemu
        if (InEditor)
        {
            PilotEnterPlaneLocal();
            PilotEnterPlaneGlobal(null);
        }
    }
    void OnParticleCollision(GameObject other)
    {
        if (other == null || dead) { return; }//avatars can't shoot you, and you can't get hurt when you're dead

        //this is to prevent more events than necessary being sent
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PlaneHit");


        //Try to find the saccentity that shot at us
        GameObject EnemyObjs = other;
        SaccEntity EnemyEntityControl = null;
        //search up the hierarchy to find the saccentity directly
        while (EnemyEntityControl == null && EnemyObjs.transform.parent != null)
        {
            EnemyObjs = EnemyObjs.transform.parent.gameObject;
            EnemyEntityControl = EnemyObjs.GetComponent<SaccEntity>();
        }
        LastAttacker = EnemyEntityControl;
        //if failed to find it, search up the hierarchy for an udonsharpbehaviour with a reference to the saccentity (for instantiated missiles etc)
        if (EnemyEntityControl == null)
        {
            EnemyObjs = other;
            UdonBehaviour EnemyUdonBehaviour = null;
            while (EnemyUdonBehaviour == null && EnemyObjs.transform.parent != null)
            {
                EnemyObjs = EnemyObjs.transform.parent.gameObject;
                EnemyUdonBehaviour = (UdonBehaviour)EnemyObjs.GetComponent(typeof(UdonBehaviour));
            }
            if (EnemyUdonBehaviour != null)
            { LastAttacker = (SaccEntity)EnemyUdonBehaviour.GetProgramVariable("EntityControl"); }
        }
    }
    public void PlaneHit()
    {
        SendEventToExtensions("SFEXT_G_BulletHit");
    }
    private void Update()
    {
        if (Piloting)
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
            if (InVR && LStickPos.magnitude > .7f && LStickDoDial)
            {
                float stickdir = Vector2.SignedAngle(LStickCheckAngle, LStickPos);

                //R stick value is manually synced using events because i don't want to use too many synced variables.
                //the value can be used in the animator to open bomb bay doors when bombs are selected, etc.
                stickdir = (stickdir - 180) * -1;
                int newselection = Mathf.FloorToInt(Mathf.Min(stickdir * LStickFuncDegreesDivider, LStickNumFuncs - 1));
                if (!LStickNULL[newselection])
                { LStickSelection = newselection; }
                //doing this in DFUNC scripts that need it instead so that we send less events
                /*                     if (VehicleAnimator.GetInteger(Lstickselection_STRING) != LStickSelection)
                                    {
                                        LStickSetAnimatorInt();
                                    } */
            }

            //RStick Selection wheel
            if (InVR && RStickPos.magnitude > .7f & RStickDoDial)
            {
                float stickdir = Vector2.SignedAngle(RStickCheckAngle, RStickPos);

                //R stick value is manually synced using events because i don't want to use too many synced variables.
                //the value can be used in the animator to open bomb bay doors when bombs are selected, etc.
                stickdir = (stickdir - 180) * -1;
                int newselection = Mathf.FloorToInt(Mathf.Min(stickdir * RStickFuncDegreesDivider, RStickNumFuncs - 1));
                if (!RStickNULL[newselection])
                { RStickSelection = newselection; }
                //doing this in DFUNC scripts that need it instead so that we send less events
                /*                     if (VehicleAnimator.GetInteger(Rstickselection_STRING) != RStickSelection)
                                    {
                                        RStickSetAnimatorInt();
                                    } */
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
                    else { CurrentSelectedFunctionL = null; }
                }
            }

            if (RStickSelection != RStickSelectionLastFrame)
            {
                //new function selected, send deselected to old one
                if (RStickSelectionLastFrame != -1 && Dial_Functions_R[RStickSelectionLastFrame] != null)
                {
                    Dial_Functions_R[RStickSelectionLastFrame].SendCustomEvent("DFUNC_Deselected");
                }
                //get udonbehaviour for newly selected function and then send selected
                if (RStickSelection > -1)
                {
                    if (Dial_Functions_R[RStickSelection] != null)
                    {
                        Dial_Functions_R[RStickSelection].SendCustomEvent("DFUNC_Selected");
                    }
                    else { CurrentSelectedFunctionR = null; }
                }
            }

            RStickSelectionLastFrame = RStickSelection;
            LStickSelectionLastFrame = LStickSelection;



        }

        if (InVehicle && !InEditor)
        {
            if (Input.GetKeyDown(KeyCode.Return) || (InVR && Input.GetButtonDown("Oculus_CrossPlatform_Button4")))
            { ExitStation(); }
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
            else
            {
                SendEventToExtensions("SFEXT_O_OwnershipTransfer");
            }
        }
    }
    public void PilotEnterPlaneLocal()//called from PilotSeat
    {
        Piloting = true;
        InVehicle = true;
        if (LStickNumFuncs == 1)
        { Dial_Functions_L[0].SendCustomEvent("DFUNC_Selected"); }
        if (RStickNumFuncs == 1)
        { Dial_Functions_R[0].SendCustomEvent("DFUNC_Selected"); }

        if (!InEditor && localPlayer.IsUserInVR()) { InVR = true; }//move me to start when they fix the bug
        //https://feedback.vrchat.com/vrchat-udon-closed-alpha-bugs/p/vrcplayerapiisuserinvr-for-the-local-player-is-not-returned-correctly-when-calle
        if (InVehicleOnly != null) { InVehicleOnly.SetActive(true); }
        if (PilotOnly != null) { PilotOnly.SetActive(true); }

        Networking.SetOwner(localPlayer, gameObject);
        TakeOwnerShipOfExtensions();
        SendEventToExtensions("SFEXT_O_PilotEnter");
    }
    public void PilotEnterPlaneGlobal(VRCPlayerApi player)
    {
        if (player != null)
        {
            PilotName = player.displayName;
            PilotID = player.playerId;
            PilotEnterTime = Time.time;
            SendEventToExtensions("SFEXT_G_PilotEnter");
        }
    }
    public void PilotExitPlane(VRCPlayerApi player)
    {
        PilotName = string.Empty;
        PilotID = -1;
        PilotExitTime = Time.time;
        LStickSelection = -1;
        RStickSelection = -1;
        LStickSelectionLastFrame = -1;
        RStickSelectionLastFrame = -1;
        SendEventToExtensions("SFEXT_G_PilotExit");
        if (player.isLocal)
        {
            Piloting = false;
            InVehicle = false;
            if (InVehicleOnly != null) { InVehicleOnly.SetActive(false); }
            if (PilotOnly != null) { PilotOnly.SetActive(false); }
            { SendEventToExtensions("SFEXT_O_PilotExit"); }
        }
    }
    public void PassengerEnterPlaneLocal()
    {
        Passenger = true;
        InVehicle = true;
        if (InVehicleOnly != null) { InVehicleOnly.SetActive(true); }
        SendEventToExtensions("SFEXT_P_PassengerEnter");
    }
    public void PassengerExitPlaneLocal()
    {
        Passenger = false;
        InVehicle = false;
        if (InVehicleOnly != null) { InVehicleOnly.SetActive(false); }
        SendEventToExtensions("SFEXT_P_PassengerExit");
    }
    public void PassengerEnterPlaneGlobal()
    {
        SendEventToExtensions("SFEXT_G_PassengerEnter");
    }
    public void PassengerExitPlaneGlobal()
    {
        SendEventToExtensions("SFEXT_G_PassengerExit");
    }
    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        //Owner sends events to sync the plane so late joiners don't see it flying with it's canopy open and stuff
        //only change things that aren't in the default state
        //only change effects which are very visible, this is just so that it looks alright for late joiners, not to sync everything perfectly.
        //syncing everything perfectly would probably require too many events to be sent.
        //planes will be fully synced when they explode or are respawned anyway.
        if (IsOwner)
        { SendEventToExtensions("SFEXT_O_PlayerJoined"); }
    }
    public override void OnPickup()
    {
        SendEventToExtensions("SFEXT_O_PickedUp");
    }
    public override void OnDrop()
    {
        SendEventToExtensions("SFEXT_O_Dropped");
    }
    public override void OnPickupUseDown()
    {
        SendEventToExtensions("SFEXT_O_PickUpUseDown");
    }
    private void FindAAMTargets()
    {
        //get array of AAM Targets
        Collider[] aamtargs = Physics.OverlapSphere(transform.position, 1000000, AAMTargetsLayer, QueryTriggerInteraction.Collide);
        int n = 0;

        //work out which index in the aamtargs array is our own plane by finding which one has this script as it's parent
        //allows for each team to have a different layer for AAMTargets
        int self = -1;
        n = 0;
        foreach (Collider target in aamtargs)
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
    public void TellDFUNCsLR()
    {
        foreach (UdonSharpBehaviour EXT in Dial_Functions_L)
        {
            if (EXT != null)
            { EXT.SendCustomEvent("DFUNC_LeftDial"); }
        }
        foreach (UdonSharpBehaviour EXT in Dial_Functions_R)
        {
            if (EXT != null)
            { EXT.SendCustomEvent("DFUNC_RightDial"); }
        }
    }
    public void TakeOwnerShipOfExtensions()
    {
        if (!InEditor)
        {
            foreach (UdonSharpBehaviour EXT in ExtensionUdonBehaviours)
            { if (EXT != null) { if (!localPlayer.IsOwner(EXT.gameObject)) { Networking.SetOwner(localPlayer, EXT.gameObject); } } }
            foreach (UdonSharpBehaviour EXT in Dial_Functions_L)
            { if (EXT != null) { if (!localPlayer.IsOwner(EXT.gameObject)) { Networking.SetOwner(localPlayer, EXT.gameObject); } } }
            foreach (UdonSharpBehaviour EXT in Dial_Functions_R)
            { if (EXT != null) { if (!localPlayer.IsOwner(EXT.gameObject)) { Networking.SetOwner(localPlayer, EXT.gameObject); } } }
        }
    }
    [RecursiveMethod]
    public void SendEventToExtensions(string eventname)
    {
        foreach (UdonSharpBehaviour EXT in ExtensionUdonBehaviours)
        {
            if (EXT != null)
            { EXT.SendCustomEvent(eventname); }
        }
        foreach (UdonSharpBehaviour EXT in Dial_Functions_L)
        {
            if (EXT != null)
            { EXT.SendCustomEvent(eventname); }
        }
        foreach (UdonSharpBehaviour EXT in Dial_Functions_R)
        {
            if (EXT != null)
            { EXT.SendCustomEvent(eventname); }
        }
    }
    public void ExitStation()
    {
        VehicleStations[MySeat].ExitStation(localPlayer);
    }
    public void FindSeats()
    {
        if (FindSeatsDone) { return; }
        VehicleStations = (VRC.SDK3.Components.VRCStation[])GetComponentsInChildren(typeof(VRC.SDK3.Components.VRCStation));
        SeatedPlayers = new int[VehicleStations.Length];
        for (int i = 0; i != SeatedPlayers.Length; i++)
        {
            SeatedPlayers[i] = -1;
        }
        FindSeatsDone = true;
    }
}
