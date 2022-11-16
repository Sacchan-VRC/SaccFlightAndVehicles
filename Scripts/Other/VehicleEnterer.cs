
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VehicleEnterer : UdonSharpBehaviour
    {
        public Transform[] AllPlanes;
        private VRCPlayerApi localPlayer;
        void Start()
        {
            players_choosepilot = new VRCPlayerApi[100];
            localPlayer = Networking.LocalPlayer;
        }

        private int MyPilotID;//if this pilot enters, enter 2nd seat
        private int MyPilotCheck;//if this pilot enters, enter 2nd seat
        private VRCPlayerApi[] players_choosepilot;
        public TextMeshProUGUI PilotName;
        public void NextPilot()
        {
            int numPlayers = 0;
            int myID;
            VRCPlayerApi.GetPlayers(players_choosepilot);
            for (int i = 0; i < players_choosepilot.Length; i++)
            {
                VRCPlayerApi plyr = players_choosepilot[i];
                if (plyr == localPlayer) { myID = i; }
                if (plyr == null) { numPlayers = i; break; }
            }
            MyPilotCheck++;
            if (MyPilotCheck >= numPlayers)
            {
                MyPilotCheck = 0;
            }
            VRCPlayerApi newMyPilot = null;
            int numchecks = 0;
            while (newMyPilot == null)
            {
                newMyPilot = players_choosepilot[MyPilotCheck];
                if (newMyPilot != null)
                {
                    if (newMyPilot == localPlayer)
                    {
                        MyPilotID = -1;
                        PilotName.text = "Choose Pilot";
                        break;
                    }
                    MyPilotID = newMyPilot.playerId;
                    PilotName.text = newMyPilot.displayName;
                    break;
                }
                MyPilotCheck++;
                if (MyPilotCheck >= numPlayers)
                {
                    MyPilotCheck = 0;
                }
                numchecks++;
                if (numchecks >= numPlayers)
                {
                    MyPilotCheck = numPlayers;
                    PilotName.text = "Choose Pilot";
                    MyPilotID = -1;
                    Debug.Log("PrevPilot(): this while loop wouldn't have ended");
                    break;
                }
            }
        }
        public void PrevPilot()
        {
            int numPlayers = 0;
            VRCPlayerApi.GetPlayers(players_choosepilot);
            for (int i = 0; i < players_choosepilot.Length; i++)
            {
                if (players_choosepilot[i] == null) { numPlayers = i; break; }
            }
            MyPilotCheck--;
            if (MyPilotCheck < 0)
            {
                MyPilotCheck = numPlayers - 1;
            }

            VRCPlayerApi newMyPilot = null;
            int numchecks = 0;
            while (newMyPilot == null)
            {
                newMyPilot = players_choosepilot[MyPilotCheck];
                if (newMyPilot != null)
                {
                    if (newMyPilot == localPlayer)
                    {
                        MyPilotID = -1;
                        PilotName.text = "Choose Pilot";
                        break;
                    }
                    MyPilotID = newMyPilot.playerId;
                    PilotName.text = newMyPilot.displayName;
                    break;
                }
                MyPilotCheck--;
                if (MyPilotCheck < 0)
                {
                    MyPilotCheck = numPlayers - 1;
                }
                numchecks++;
                if (numchecks >= numPlayers)
                {
                    MyPilotCheck = numPlayers;
                    PilotName.text = "Choose Pilot";
                    MyPilotID = -1;
                    Debug.Log("PrevPilot(): this while loop wouldn't have ended");
                    break;
                }
            }
        }
        public void EnterVehicle()
        {
            for (int i = 0; i < AllPlanes.Length; i++)
            {
                SaccEntity ve = AllPlanes[i].GetComponent<SaccEntity>();
                if (ve)
                {
                    if (ve.Occupied)
                    {
                        if (ve.UsersID == MyPilotID)
                        {
                            var seats = ve.VehicleSeats;
                            if (seats.Length > 1)
                            {
                                for (int o = 0; o < seats.Length; o++)
                                {
                                    if (!seats[o].SeatOccupied)
                                    {
                                        ve.VehicleStations[seats[o].ThisStationID].UseStation(localPlayer);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (player.playerId == MyPilotID)
            {
                NextPilot();
            }
        }
    }
}