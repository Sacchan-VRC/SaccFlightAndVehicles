
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
    private int ThisStationID;
    private bool SeatInitialized = false;
    private Transform Seat;
    private Quaternion SeatStartRot;
    private VRCPlayerApi localPlayer;
    private void Start()
    {
        localPlayer = Networking.LocalPlayer;
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

        EngineControl.MySeat = ThisStationID;
        if (PassengerOnly != null) { PassengerOnly.SetActive(true); }
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(true); }
    }
    public override void OnStationEntered(VRCPlayerApi player)
    {
        if (!SeatInitialized) { InitializeSeat(); }//can't do this in start because hudcontrol might not have initialized
        EngineControl.PassengerEnterPlaneGlobal();

        //voice range change to allow talking inside cockpit (after VRC patch 1008)
        if (player != null)
        {
            EngineControl.SeatedPlayers[ThisStationID] = player.playerId;
            if (player.isLocal)
            {
                foreach (int crew in EngineControl.SeatedPlayers)
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
        if (player.playerId == EngineControl.SeatedPlayers[ThisStationID])
        {
            PlayerExitPlane(player);
        }
    }
    public void PlayerExitPlane(VRCPlayerApi player)
    {
        if (!SeatInitialized) { InitializeSeat(); }
        EngineControl.PassengerExitPlaneGlobal();

        EngineControl.SeatedPlayers[ThisStationID] = -1;
        if (player != null)
        {
            SetVoiceOutside(player);
            if (player.isLocal)
            {
                EngineControl.MySeat = -1;
                EngineControl.PassengerExitPlaneLocal();
                //undo voice distances of all players inside the vehicle
                foreach (int crew in EngineControl.SeatedPlayers)
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
        EngineControl.FindSeats();
        int x = 0;
        foreach (VRCStation station in EngineControl.VehicleStations)
        {
            if (station.gameObject == gameObject)
            {
                ThisStationID = x;
            }
            x++;
        }
        SeatInitialized = true;
    }
}
