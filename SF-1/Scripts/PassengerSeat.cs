
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PassengerSeat : UdonSharpBehaviour
{
    public EngineController EngineControl;
    public GameObject SeatAdjuster;
    public GameObject PassengerOnly;
    private Transform PlaneMesh;
    private LayerMask Planelayer;
    private HUDController HUDControl;
    private int ThisStationID;
    private bool SeatInitialized = false;
    private Transform Seat;
    private Quaternion SeatStartRot;
    [SerializeField] private GameObject[] SetOwnerObjects;
    private VRCPlayerApi localPlayer;
    private void Start()
    {
        Assert(EngineControl != null, "Start: EngineControl != null");
        Assert(PassengerOnly != null, "Start: LeaveButton != null");
        Assert(SeatAdjuster != null, "Start: SeatAdjuster != null");

        localPlayer = Networking.LocalPlayer;
        HUDControl = EngineControl.HUDControl;
        PlaneMesh = EngineControl.PlaneMesh.transform;
        Planelayer = PlaneMesh.gameObject.layer;

        Seat = ((VRC.SDK3.Components.VRCStation)GetComponent(typeof(VRC.SDK3.Components.VRCStation))).stationEnterPlayerLocation.transform;
        SeatStartRot = Seat.localRotation;
    }
    private void Interact()
    {
        EngineControl.PassengerEnterPlaneLocal();

        Seat.rotation = Quaternion.Euler(0, Seat.eulerAngles.y, 0);//fixes offset seated position when getting in a rolled/pitched vehicle
        localPlayer.UseAttachedStation();
        Seat.localRotation = SeatStartRot;

        HUDControl.MySeat = ThisStationID;
        if (PassengerOnly != null) { PassengerOnly.SetActive(true); }
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(true); }

        foreach (GameObject obj in SetOwnerObjects)
        {
            if (!localPlayer.IsOwner(obj.gameObject))
            { Networking.SetOwner(localPlayer, obj); }
        }
    }
    public override void OnStationEntered(VRCPlayerApi player)
    {
        if (!SeatInitialized) { InitializeSeat(); }//can't do this in start because hudcontrol might not have initialized

        //voice range change to allow talking inside cockpit (after VRC patch 1008)
        if (player != null)
        {
            HUDControl.SeatedPlayers[ThisStationID] = player.playerId;
            if (player.isLocal)
            {
                foreach (int crew in HUDControl.SeatedPlayers)
                {
                    VRCPlayerApi guy = VRCPlayerApi.GetPlayerById(crew);
                    if (guy != null)
                    {
                        SetVoiceInside(guy);
                    }
                }
            }
            else if (EngineControl.Piloting || EngineControl.Passenger)
            {
                SetVoiceInside(player);
            }
        }
    }
    public override void OnStationExited(VRCPlayerApi player)
    {
        if (!SeatInitialized) { InitializeSeat(); }
        PlayerExitPlane(player);
    }
    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (!SeatInitialized) { InitializeSeat(); }
        if (player.playerId == HUDControl.SeatedPlayers[ThisStationID])
        {
            PlayerExitPlane(player);
        }
    }
    public void PlayerExitPlane(VRCPlayerApi player)
    {
        if (!SeatInitialized) { InitializeSeat(); }

        HUDControl.SeatedPlayers[ThisStationID] = -1;
        HUDControl.MySeat = -1;
        if (player != null)
        {
            SetVoiceOutside(player);
            if (player.isLocal)
            {
                EngineControl.PassengerExitPlaneLocal();
                //undo voice distances of all players inside the vehicle
                foreach (int crew in HUDControl.SeatedPlayers)
                {
                    VRCPlayerApi guy = VRCPlayerApi.GetPlayerById(crew);
                    if (guy != null)
                    {
                        SetVoiceOutside(guy);
                    }
                }
                if (PassengerOnly != null) { PassengerOnly.SetActive(false); }
                if (SeatAdjuster != null) { SeatAdjuster.SetActive(false); }
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
        HUDControl.FindSeats();
        int x = 0;
        foreach (VRCStation station in HUDControl.VehicleStations)
        {
            if (station.gameObject == gameObject)
            {
                ThisStationID = x;
            }
            x++;
        }
        SeatInitialized = true;
    }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogWarning("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}
