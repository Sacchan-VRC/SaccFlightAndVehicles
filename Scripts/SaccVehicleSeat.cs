
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class SaccPilotSeat : UdonSharpBehaviour
{
    [SerializeField] private SaccEntity EntityControl;
    [Tooltip("Gameobject with script that runs when you enter the seat to edjust your view position")]
    [SerializeField] private GameObject SeatAdjuster;
    [SerializeField] private bool IsPilotSeat = false;
    [Tooltip("Object that is enabled only when sitting in this seat")]
    [SerializeField] private GameObject ThisSeatOnly;
    private int ThisStationID;
    private bool SeatInitialized = false;
    private VRCPlayerApi localPlayer;
    private void Start()
    {
        localPlayer = Networking.LocalPlayer;

    }
    private void Interact()//entering the plane
    {
        if (!SeatInitialized) { InitializeSeat(); }
        EntityControl.MySeat = ThisStationID;

        if (IsPilotSeat)
        { EntityControl.PilotEnterVehicleLocal(); }
        else
        { EntityControl.PassengerEnterVehicleLocal(); }
        if (ThisSeatOnly != null) { ThisSeatOnly.SetActive(true); }
        localPlayer.UseAttachedStation();
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(true); }
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
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(false); }
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
                if (ThisSeatOnly != null) { ThisSeatOnly.SetActive(false); }
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
}
