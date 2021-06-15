
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PassengerSeat : UdonSharpBehaviour
{
    public EngineController EngineControl;
    public GameObject LeaveButton;
    public GameObject SeatAdjuster;
    private Transform PlaneMesh;
    private LayerMask Planelayer;
    private HUDController HUDControl;
    private int ThisStationID;
    private bool firsttime = true;
    private void Start()
    {
        Assert(EngineControl != null, "Start: EngineControl != null");
        Assert(LeaveButton != null, "Start: LeaveButton != null");
        Assert(SeatAdjuster != null, "Start: SeatAdjuster != null");

        HUDControl = EngineControl.HUDControl;
        PlaneMesh = EngineControl.PlaneMesh.transform;
        Planelayer = PlaneMesh.gameObject.layer;
    }
    private void Interact()
    {
        if (firsttime)//can't do this in start because hducontrol might not have initialized
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
            firsttime = false;
        }

        EngineControl.PassengerEnterPlaneLocal();
        HUDControl.MySeat = ThisStationID;
        if (LeaveButton != null) { LeaveButton.SetActive(true); }
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(true); }
        EngineControl.localPlayer.UseAttachedStation();
    }
    public override void OnStationEntered(VRCPlayerApi player)
    {
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
        PlayerExitPlane(player);
    }
    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (player.playerId == HUDControl.SeatedPlayers[ThisStationID])
        {
            PlayerExitPlane(player);
        }
    }
    public void PlayerExitPlane(VRCPlayerApi player)
    {
        HUDControl.SeatedPlayers[ThisStationID] = -1;
        if (player != null)
        {
            SetVoiceOutside(player);
            if (player.isLocal)
            {
                EngineControl.PassengerExitPlaneLocal();
                //undo voice distances of all players inside the vehicle
                foreach (LeaveVehicleButton crew in EngineControl.LeaveButtons)
                {
                    VRCPlayerApi guy = VRCPlayerApi.GetPlayerById(crew.SeatedPlayer);
                    if (guy != null)
                    {
                        SetVoiceOutside(guy);
                    }
                }
                if (LeaveButton != null) { LeaveButton.SetActive(false); }
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
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogWarning("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}
