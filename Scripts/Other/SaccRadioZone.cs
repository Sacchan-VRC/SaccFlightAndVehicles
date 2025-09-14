﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SaccRadioZone : UdonSharpBehaviour
    {
        [Header("Stand inside the radius to enable radio, can be attached to a moving object")]
        public SaccRadioBase RadioBase;
        public float ZoneRadius;
        public VRCPlayerApi[] playersinside = new VRCPlayerApi[100];
        [System.NonSerialized] public int numPlayersInside = 0;
        public TextMeshProUGUI ChannelText;
        [UdonSynced, SerializeField, FieldChangeCallback(nameof(Channel))] private byte _Channel = 1;
        public byte Channel
        {
            set
            {
                _Channel = value;
                if (InZone) { RadioBase.SetAllVoiceVolumesDefault(); UpdateChannel(); }
                if (ChannelText) { ChannelText.text = value.ToString(); }
            }
            get => _Channel;
        }
        [Header("For moveable/respawnable radiozone:")]
        [SerializeField] VRC.SDK3.Components.VRCObjectSync PickupObject;
        int nextplayer;
        float VoiceNear;
        float VoiceFar;
        float VoiceGain;
        bool InZone;
        private bool ChannelSwapped;
        Vector3 SpawnPos;
        Quaternion SpawnRot;
        void Start()
        {
            SendCustomEventDelayedFrames(nameof(CheckPlayersInside), Random.Range(0, 7));
            VoiceNear = RadioBase.VoiceNear;
            VoiceFar = RadioBase.VoiceFar;
            VoiceGain = RadioBase.VoiceGain;
            if (PickupObject)
            {
                SpawnPos = PickupObject.transform.localPosition;
                SpawnRot = PickupObject.transform.localRotation;
            }
        }
        public void CheckPlayersInside()
        {
            SendCustomEventDelayedFrames(nameof(CheckPlayersInside), 7);
            VRCPlayerApi[] players = new VRCPlayerApi[100];
            VRCPlayerApi.GetPlayers(players);
            int numplayers = VRCPlayerApi.GetPlayerCount();
            nextplayer++;
            if (nextplayer >= numplayers)
            { nextplayer = 0; }
            VRCPlayerApi nextAPI = players[nextplayer];
            if (Vector3.Distance(nextAPI.GetPosition(), transform.position) < ZoneRadius)
            {
                if (nextAPI.isLocal)
                {
                    RadioBase.MyZone = this; InZone = true;
                    UpdateChannel();
                }
                else
                { AddPlayer(nextAPI); }
            }
            else
            {
                if (nextAPI.isLocal)
                {
                    if (InZone)
                    {
                        RadioBase.MyZone = null;
                        InZone = false;
                        RadioBase.SetAllVoiceVolumesDefault();
                        ResetChannel();
                    }
                }
                else
                { RemovePlayer(nextAPI); }
            }
        }
        public void UpdateChannel()
        {
            ChannelSwapped = true;
            RadioBase.SetProgramVariable("CurrentChannel", _Channel);
        }
        public void ResetChannel()
        {
            ChannelSwapped = false;
            RadioBase.SetProgramVariable("CurrentChannel", (byte)RadioBase.GetProgramVariable("MyChannel"));
        }
        //this will break if a player enters a seat while within the trigger because OnPlayerTriggerExit doesn't run
        /*     public override void OnPlayerTriggerEnter(VRCPlayerApi player)
            {
                //add player to
                playersinside[numPlayersInside] = player;
                numPlayersInside++;
            }
            public override void OnPlayerTriggerExit(VRCPlayerApi player)
            {
                //if player is inside, remove
                RemovePlayer(player);
            }*/
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            //if player is inside, remove
            RemovePlayer(player);
        }
        public void AddPlayer(VRCPlayerApi player)
        {
            for (int i = 0; i < numPlayersInside; i++)
            {
                if (player == playersinside[i])
                {
                    //dont add if it's already there
                    return;
                }
            }
            playersinside[numPlayersInside] = player;
            numPlayersInside++;
        }
        public void RemovePlayer(VRCPlayerApi player)
        {
            for (int i = 0; i < numPlayersInside; i++)
            {
                if (player == playersinside[i])
                {
                    playersinside[i].SetVoiceDistanceNear(0);
                    playersinside[i].SetVoiceDistanceFar(25);
                    playersinside[i].SetVoiceGain(15);
                    //remove player
                    numPlayersInside--;
                    playersinside[i] = playersinside[numPlayersInside];
                    playersinside[numPlayersInside] = null;
                    break;
                }
            }
        }
        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, ZoneRadius);
        }
        public void IncreaseChannel()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            if (Channel + 1 > 16) { Channel = 1; }
            else
            {
                Channel++;
            }
            RequestSerialization();
        }
        public void DecreaseChannel()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            if (Channel - 1 < 1) { Channel = 16; }
            else
            {
                Channel--;
            }
            RequestSerialization();
        }
        public void RespawnRadio()
        {
            if (PickupObject)
            {
                VRC_Pickup pickup = PickupObject.GetComponent<VRC_Pickup>();
                if (pickup) { if (pickup.IsHeld) return; }
                Networking.SetOwner(Networking.LocalPlayer, PickupObject.gameObject);
                PickupObject.transform.localPosition = SpawnPos;
                PickupObject.transform.localRotation = SpawnRot;
                PickupObject.FlagDiscontinuity();
            }
        }
    }
}
