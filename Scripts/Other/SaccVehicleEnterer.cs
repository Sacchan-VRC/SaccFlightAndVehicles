
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SaccVehicleEnterer : UdonSharpBehaviour
    {
        public Transform[] AllPlanes;
        private VRCPlayerApi localPlayer;
        void Start()
        {
            players_choosepilot = new VRCPlayerApi[100];
            localPlayer = Networking.LocalPlayer;
            if (MessageText) MessageText.gameObject.SetActive(false);
        }

        private int MyPilotID;
        private int MyPilotCheck;
        private VRCPlayerApi[] players_choosepilot;
        public TextMeshProUGUI PilotName;
        public TextMeshProUGUI MessageText;
        public void NextPilot()
        {
            int numPlayers = 0;
            players_choosepilot = new VRCPlayerApi[100];
            VRCPlayerApi.GetPlayers(players_choosepilot);
            for (int i = 0; i < players_choosepilot.Length; i++)
            {
                if (players_choosepilot[i] == null) { numPlayers = i; break; }
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
                        MyPilotID = -2;
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
                    MyPilotID = -2;
                    break;
                }
            }
        }
        public void PrevPilot()
        {
            int numPlayers = 0;
            players_choosepilot = new VRCPlayerApi[100];
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
                        MyPilotID = -2; // -1 is means no player in seat on the entity's SeatedPlayers array
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
                    MyPilotID = -2;
                    break;
                }
            }
        }
        public void EnterVehicle()
        {
            if (MyPilotID == -2)
            {
                if (MessageText) MessageText.gameObject.SetActive(true);
                if (MessageText) MessageText.text = "Choose a Pilot";
                numMessages++;
                SendCustomEventDelayedSeconds(nameof(DisableMessage), 0.5f);
                return;
            }
            bool breaknow = false;
            bool vehicleFull = false;
            for (int i = 0; i < AllPlanes.Length; i++)
            {
                if (breaknow) { break; }
                SaccEntity ve = AllPlanes[i].GetComponent<SaccEntity>();
                if (ve)
                {
                    for (int j = 0; j < ve.SeatedPlayers.Length; j++)
                    {
                        if (breaknow) { break; }
                        if (ve.SeatedPlayers[j] == MyPilotID)
                        {
                            var seats = ve.VehicleSeats;
                            if (seats.Length > 1)
                            {
                                for (int o = 0; o < seats.Length; o++)
                                {
                                    if (!seats[o].SeatOccupied)
                                    {
                                        seats[o].Interact();
                                        return;
                                    }
                                }
                                vehicleFull = true;
                                breaknow = true;
                                break;
                            }
                            vehicleFull = true;
                            breaknow = true;
                            break;
                        }
                    }
                }
            }
            if (vehicleFull)
            {
                if (MessageText) MessageText.gameObject.SetActive(true);
                if (MessageText) MessageText.text = "Vehicle Full";
                numMessages++;
                SendCustomEventDelayedSeconds(nameof(DisableMessage), 0.5f);
            }
            else
            {
                if (MessageText) MessageText.gameObject.SetActive(true);
                if (MessageText) MessageText.text = "Not In Vehicle";
                numMessages++;
                SendCustomEventDelayedSeconds(nameof(DisableMessage), 0.5f);
            }
        }
        int numMessages;
        public void DisableMessage()
        {
            numMessages--;
            if (numMessages != 0) return;
            if (MessageText) MessageText.gameObject.SetActive(false);
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