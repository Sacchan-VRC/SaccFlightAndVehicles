
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class SaccVehicleSeat : UdonSharpBehaviour
{
    [SerializeField] private SaccEntity EntityControl;
    [Tooltip("Gameobject with script that runs when you enter the seat to edjust your view position")]
    [SerializeField] private bool IsPilotSeat = false;
    [Tooltip("Object that is enabled only when sitting in this seat")]
    [SerializeField] private GameObject ThisSeatOnly;
    [SerializeField] private bool AdjustSeat = true;
    [SerializeField] private Transform TargetEyePosition;
    [UdonSynced, FieldChangeCallback(nameof(AdjustedPos))] private Vector2 _adjustedPos;
    public Vector2 AdjustedPos
    {
        set
        {
            _adjustedPos = value;
            SetRecievedSeatPosition();
        }
        get => _adjustedPos;
    }
    private float AdjustTime;
    private bool DoSeatAdjustment = false;
    private bool CalibratedY = false;
    private bool CalibratedZ = false;
    private Vector3 SeatStartPos;
    private int ThisStationID;
    private bool SeatInitialized = false;
    private bool InEditor = true;
    private VRCPlayerApi localPlayer;
    private Transform Seat;
    private Quaternion SeatStartRot;
    private bool InVehicle;
    private void Start()
    {
        localPlayer = Networking.LocalPlayer;
        if (localPlayer != null) { InEditor = false; }
        Seat = ((VRC.SDK3.Components.VRCStation)GetComponent(typeof(VRC.SDK3.Components.VRCStation))).stationEnterPlayerLocation.transform;
        SeatStartRot = Seat.localRotation;
        SeatStartPos = Seat.localPosition;
    }
    public override void Interact()//entering the vehicle
    {
        Networking.SetOwner(localPlayer, gameObject);
        if (!SeatInitialized) { InitializeSeat(); }
        EntityControl.MySeat = ThisStationID;

        if (IsPilotSeat)
        { EntityControl.PilotEnterVehicleLocal(); }
        else
        { EntityControl.PassengerEnterVehicleLocal(); }
        if (ThisSeatOnly) { ThisSeatOnly.SetActive(true); }
        Seat.rotation = Quaternion.Euler(0, Seat.eulerAngles.y, 0);//fixes offset seated position when getting in a rolled/pitched vehicle in VR
        localPlayer.UseAttachedStation();
        Seat.localRotation = SeatStartRot;
        InVehicle = true;
        if (AdjustSeat && TargetEyePosition)
        {
            CalibratedY = false;
            CalibratedZ = false;
            AdjustTime = 0;
            SeatAdjustment();
        }
    }
    public override void OnStationEntered(VRCPlayerApi player)
    {
        if (!SeatInitialized) { InitializeSeat(); }//can't do this in start because EntityControl might not have initialized
        if (player != null)
        {
            if (IsPilotSeat) { EntityControl.PilotEnterVehicleGlobal(player); }
            //voice range change to allow talking inside cockpit (after VRC patch 1008)
            EntityControl.SeatedPlayers[ThisStationID] = player.playerId;
            if (player.isLocal)
            {
                foreach (int crew in EntityControl.SeatedPlayers)
                {//get get a fresh VRCPlayerAPI every time to prevent players who left leaving a broken one behind and causing crashes
                    VRCPlayerApi guy = VRCPlayerApi.GetPlayerById(crew);
                    if (guy != null)
                    {
                        SetVoiceInside(guy);
                    }
                }
            }
            else if (EntityControl.InVehicle)
            {
                SetVoiceInside(player);
            }
        }
    }
    public override void OnStationExited(VRCPlayerApi player)
    {
        if (!SeatInitialized) { InitializeSeat(); }
        PlayerExitPlane(player);
        Seat.localPosition = SeatStartPos;
    }
    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (!SeatInitialized) { InitializeSeat(); }
        if (player.playerId == EntityControl.SeatedPlayers[ThisStationID])
        {
            PlayerExitPlane(player);
        }
    }
    public void PlayerExitPlane(VRCPlayerApi player)
    {
        if (!SeatInitialized) { InitializeSeat(); }
        EntityControl.SeatedPlayers[ThisStationID] = -1;
        if (player != null)
        {
            if (IsPilotSeat) { EntityControl.PilotExitVehicle(player); }
            SetVoiceOutside(player);
            if (player.isLocal)
            {
                EntityControl.MySeat = -1;
                if (!IsPilotSeat)
                { EntityControl.PassengerExitVehicleLocal(); }
                //undo voice distances of all players inside the vehicle
                foreach (int crew in EntityControl.SeatedPlayers)
                {
                    VRCPlayerApi guy = VRCPlayerApi.GetPlayerById(crew);
                    if (guy != null)
                    {
                        SetVoiceOutside(guy);
                    }
                }
                if (ThisSeatOnly) { ThisSeatOnly.SetActive(false); }
                InVehicle = false;
            }
        }
    }
    private void SetVoiceInside(VRCPlayerApi Player)
    {
        Player.SetVoiceDistanceNear(999999);
        Player.SetVoiceDistanceFar(1000000);
        Player.SetVoiceGain(.6f);
    }
    private void SetVoiceOutside(VRCPlayerApi Player)
    {
        Player.SetVoiceDistanceNear(0);
        Player.SetVoiceDistanceFar(25);
        Player.SetVoiceGain(15);
    }
    private void InitializeSeat()
    {
        int x = 0;
        foreach (VRCStation station in EntityControl.VehicleStations)
        {
            if (station.gameObject == gameObject)
            {
                ThisStationID = x;
                if (IsPilotSeat) { EntityControl.PilotSeat = x; }
                break;
            }
            x++;
        }
        SeatInitialized = true;
    }

    //seat adjuster stuff
    public void SeatAdjustment()
    {
        if (!InEditor)
        {
            AdjustTime += .3f;
            //find head relative position ingame
            Vector3 TargetRelative = TargetEyePosition.InverseTransformPoint(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position);
            if (!CalibratedY)
            {
                if (Mathf.Abs(TargetRelative.y) > 0.01f)
                {
                    Seat.position -= TargetEyePosition.up * FindNearestPowerOf2Below(TargetRelative.y);
                }
                else
                {
                    if (AdjustTime > 1f)
                    {
                        CalibratedY = true;
                    }
                }
            }
            if (!CalibratedZ)
            {
                if (Mathf.Abs(TargetRelative.z) > 0.01f)
                {
                    Seat.position -= TargetEyePosition.forward * FindNearestPowerOf2Below(TargetRelative.z);
                }
                else
                {
                    if (AdjustTime > 1f)
                    {
                        CalibratedZ = true;
                    }
                }
            }
            Vector3 newpos = Seat.localPosition;
            _adjustedPos.x = newpos.y;
            _adjustedPos.y = newpos.z;
            RequestSerialization();
            if (InVehicle && (!CalibratedY || !CalibratedZ))
            {
                SendCustomEventDelayedSeconds(nameof(SeatAdjustment), .3f);
            }
        }
    }
    private float FindNearestPowerOf2Below(float target)
    {
        float targetAbs = Mathf.Abs(target);
        float x = .01f;
        while (x < targetAbs)
        { x *= 2; }
        if (target > 0)
        { return x; }
        else
        { return -x; }
    }
    public void SetRecievedSeatPosition()
    {
        Vector3 newpos = (new Vector3(0, _adjustedPos.x, _adjustedPos.y));
        Seat.localPosition = newpos;
    }
}
