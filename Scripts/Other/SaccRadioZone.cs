
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
        [System.NonSerialized] public SaccRadioBase RadioBase;
        public float ZoneRadius;
        public VRCPlayerApi[] playersinside = new VRCPlayerApi[100];
        [System.NonSerialized] public int numPlayersInside = 0;
        public TextMeshProUGUI ChannelText;
        public TextMeshProUGUI ChannelText_ListenOnly;
        [UdonSynced, SerializeField, FieldChangeCallback(nameof(Channel_ListenOnly))] private byte _Channel_ListenOnly = 0;
        public byte Channel_ListenOnly
        {
            set
            {
                byte lastChannel = _Channel_ListenOnly;
                _Channel_ListenOnly = value;
                if (InZone)
                {
                    if (lastChannel != Channel) { RadioBase.SetAllVoiceVolumesDefault(lastChannel); }
                    UpdateChannel_ListenOnly();
                }
                if (ChannelText_ListenOnly) { ChannelText_ListenOnly.text = value == 0 ? "X" : value.ToString(); }
            }
            get => _Channel_ListenOnly;
        }
        [UdonSynced, SerializeField, FieldChangeCallback(nameof(Channel))] private byte _Channel = 1;
        public byte Channel
        {
            set
            {
                byte lastChannel = _Channel;
                _Channel = value;
                if (InZone)
                {
                    if (lastChannel != Channel_ListenOnly) { RadioBase.SetAllVoiceVolumesDefault(lastChannel); }
                    UpdateChannel();
                }
                if (ChannelText) { ChannelText.text = value == 0 ? "X" : value.ToString(); }
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
        private bool ChannelSwapped_ListenOnly;
        Vector3 SpawnPos;
        Quaternion SpawnRot;
        public void Initialize()
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
            if (ChannelText) { ChannelText.text = Channel == 0 ? "X" : Channel.ToString(); }
            if (ChannelText_ListenOnly) { ChannelText_ListenOnly.text = Channel_ListenOnly == 0 ? "X" : Channel_ListenOnly.ToString(); }
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
                    UpdateChannel(); UpdateChannel_ListenOnly();
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
                        RadioBase.SetAllVoiceVolumesDefault(Channel);
                        RadioBase.SetAllVoiceVolumesDefault(Channel_ListenOnly);
                        ResetChannel(); ResetChannel_ListenOnly();
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
        public void UpdateChannel_ListenOnly()
        {
            ChannelSwapped_ListenOnly = true;
            RadioBase.SetProgramVariable("CurrentChannel_ListenOnly", _Channel_ListenOnly);
        }
        public void ResetChannel_ListenOnly()
        {
            ChannelSwapped_ListenOnly = false;
            RadioBase.SetProgramVariable("CurrentChannel_ListenOnly", (byte)RadioBase.GetProgramVariable("MyChannel_ListenOnly"));
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
            if (Channel + 1 > 16) { Channel = 0; }
            else
            {
                Channel++;
            }
            RequestSerialization();
        }
        public void DecreaseChannel()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            if ((int)(Channel - 1) < 0) { Channel = 16; }
            else
            {
                Channel--;
            }
            RequestSerialization();
        }
        public void IncreaseChannel_ListenOnly()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            if (Channel_ListenOnly + 1 > 16) { Channel_ListenOnly = 0; }
            else
            {
                Channel_ListenOnly++;
            }
            RequestSerialization();
        }
        public void DecreaseChannel_ListenOnly()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            if ((int)(Channel_ListenOnly - 1) < 0) { Channel_ListenOnly = 16; }
            else
            {
                Channel_ListenOnly--;
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
