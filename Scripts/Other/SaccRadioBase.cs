
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SaccRadioBase : UdonSharpBehaviour
    {
        [Header("Vehicles must have SAV_Radio extension for this to work")]
        private SAV_Radio[] _AllPlanes_RD;
        public float VoiceNear = 199999;
        public float VoiceFar = 200000;
        // public float VoiceVolumetric = 1500;
        public float VoiceGain = .05f;
        // public float VoiceLowPass;
        [Tooltip("Make this text object darker when radio is disabled. Not required.")]
        public TextMeshProUGUI RadioEnabledTxt;
        public bool RadioEnabled = true;
        private byte CurrentChannel = 1;
        public byte MyChannel = 1;
        [Header("All Planes and RadioZones are filled automatically on build.")]
        public Transform[] AllPlanes;
        public SaccRadioZone[] RadioZones;
        public TextMeshProUGUI ChannelText;
        [Header("Debug, leave empty:")]
        public SaccEntity MyVehicle;
        [System.NonSerialized] public int MyVehicleSetTimes;//number of times MyVehicle has been set (for when holding 2 objects with radio, and dropping one) //Used by SAV_Radio
        [System.NonSerialized] public SaccRadioZone MyZone;
        private int NextPlane;
        private int NextZone;
        private int NumZones;
        private bool DoZones = false;
        void Start()
        {
            SendCustomEventDelayedSeconds(nameof(SetRadioVoiceVolumes), 5);
            CurrentChannel = MyChannel = 1;
            if (ChannelText) { ChannelText.text = MyChannel.ToString(); }
            SaccEntity[] _AllPlanes_ENT = new SaccEntity[AllPlanes.Length];
            _AllPlanes_RD = new SAV_Radio[AllPlanes.Length];
            string radioname = GetUdonTypeName<SAV_Radio>();
            for (int i = 0; i < AllPlanes.Length; i++)
            {
                _AllPlanes_ENT[i] = (SaccEntity)AllPlanes[i].GetComponent<SaccEntity>();
                if (_AllPlanes_ENT[i]) { _AllPlanes_RD[i] = (SAV_Radio)_AllPlanes_ENT[i].GetExtention(radioname); }
            }
            PruneRadiosArray();
            for (int i = 0; i < _AllPlanes_RD.Length; i++)
            {
                _AllPlanes_RD[i].RadioBase = this;
                _AllPlanes_RD[i].Init();
            }
            NumZones = RadioZones.Length;
            if (NumZones != 0) { DoZones = true; }
        }
        private void PruneRadiosArray()
        {
            int len = _AllPlanes_RD.Length;
            bool[] valid = new bool[len];
            int numvalid = 0;
            for (int i = 0; i < len; i++)
            {
                if (_AllPlanes_RD[i])
                {
                    valid[i] = true;
                    numvalid++;
                }
            }
            SAV_Radio[] RD_New = new SAV_Radio[numvalid];
            int offset = 0;
            for (int i = 0; i < len; i++)
            {
                if (!valid[i])
                {
                    offset++;
                }
                else
                {
                    RD_New[i - offset] = _AllPlanes_RD[i];
                }
            }
            _AllPlanes_RD = RD_New;
        }
        public void SetRadioVoiceVolumes()
        {
            SendCustomEventDelayedFrames(nameof(SetRadioVoiceVolumes), 5);
            if ((!MyVehicle || !RadioEnabled) && !MyZone) { return; }
            if (DoZones)
            { SendCustomEventDelayedFrames(nameof(SetRadioVoiceVolumes_Zones), 2); }//separate in frames for optimization
            NextPlane++;
            if (NextPlane == _AllPlanes_RD.Length) { NextPlane = 0; }
            SaccEntity NextVehicle = _AllPlanes_RD[NextPlane].EntityControl;
            if (MyVehicle == _AllPlanes_RD[NextPlane].EntityControl
                || (byte)_AllPlanes_RD[NextPlane].Channel != CurrentChannel
                || CurrentChannel == 0) { return; }
            for (int o = 0; o < NextVehicle.VehicleSeats.Length; o++)
            {
                VRCPlayerApi thisplayer = NextVehicle.VehicleSeats[o].SeatedPlayer;
                if (thisplayer != null)
                {
                    thisplayer.SetVoiceDistanceNear(VoiceNear);
                    thisplayer.SetVoiceDistanceFar(VoiceFar);
                    thisplayer.SetVoiceGain(VoiceGain);
                }
            }
            if (NextVehicle.EntityPickup && NextVehicle.EntityPickup.IsHeld)
            {
                VRCPlayerApi thisplayer = Networking.GetOwner(NextVehicle.gameObject);
                if (thisplayer != null)
                {
                    thisplayer.SetVoiceDistanceNear(VoiceNear);
                    thisplayer.SetVoiceDistanceFar(VoiceFar);
                    thisplayer.SetVoiceGain(VoiceGain);
                }
            }
        }
        public void SetRadioVoiceVolumes_Zones()
        {
            if ((!MyVehicle || !RadioEnabled) && !MyZone) { return; }
            NextZone++;
            if (NextZone >= NumZones) { NextZone = 0; }
            SaccRadioZone NextRZ = RadioZones[NextZone];
            VRCPlayerApi[] RZ_players = NextRZ.playersinside;
            if (CurrentChannel != NextRZ.Channel)
            {
                for (int i = 0; i < NextRZ.numPlayersInside; i++)
                {
                    RZ_players[i].SetVoiceDistanceNear(0);
                    RZ_players[i].SetVoiceDistanceFar(25);
                    RZ_players[i].SetVoiceGain(15);
                }
                return;
            }
            if (NextRZ != MyZone)
            {
                for (int i = 0; i < NextRZ.numPlayersInside; i++)
                {
                    RZ_players[i].SetVoiceDistanceNear(VoiceNear);
                    RZ_players[i].SetVoiceDistanceFar(VoiceFar);
                    RZ_players[i].SetVoiceGain(VoiceGain);
                }
            }
        }
        public void SetAllVoiceVolumesDefault()
        {
            VRCPlayerApi[] AllPlayers = new VRCPlayerApi[100];
            VRCPlayerApi.GetPlayers(AllPlayers);
            int numplayers = VRCPlayerApi.GetPlayerCount();
            for (int i = 0; i < numplayers; i++)
            {
                AllPlayers[i].SetVoiceDistanceNear(0);
                AllPlayers[i].SetVoiceDistanceFar(25);
                AllPlayers[i].SetVoiceGain(15);
            }
        }
        public void SetVehicleVolumeDefault(SaccEntity Vehicle)
        {
            for (int i = 0; i < Vehicle.VehicleSeats.Length; i++)
            {
                SetSingleVoiceVolumeDefault(Vehicle.VehicleSeats[i].SeatedPlayer);
            }
        }
        public void SetSingleVoiceVolumeDefault(VRCPlayerApi player)
        {
            if (player == null) { return; }
            player.SetVoiceDistanceNear(0);
            player.SetVoiceDistanceFar(25);
            player.SetVoiceGain(15);
        }
        public void ToggleRadio()
        {
            RadioEnabled = !RadioEnabled;
            if (RadioEnabledTxt) RadioEnabledTxt.color = RadioEnabled ? Color.white : Color.gray;
            UpdateRadioScripts();
        }
        public void IncreaseChannel()
        {
            if (MyChannel + 1 > 16) { MyChannel = 1; }
            else
            {
                MyChannel++;
            }
            if (ChannelText) { ChannelText.text = MyChannel.ToString(); }
            CurrentChannel = MyChannel;
            UpdateRadioScripts();
        }
        public void DecreaseChannel()
        {
            if (MyChannel - 1 < 1) { MyChannel = 16; }
            else
            {
                MyChannel--;
            }
            if (ChannelText) { ChannelText.text = MyChannel.ToString(); }
            CurrentChannel = MyChannel;
            UpdateRadioScripts();
        }
        public void SetChannel(int inChannel)
        {
            inChannel--;
            inChannel = mod(inChannel, 16);
            inChannel++;
            CurrentChannel = MyChannel = (byte)(inChannel);
            if (ChannelText) { ChannelText.text = MyChannel.ToString(); }
            UpdateRadioScripts();
        }
        int mod(int x, int m)
        {
            return (x % m + m) % m;
        }
        void UpdateRadioScripts()
        {
            for (int i = 0; i < _AllPlanes_RD.Length; i++)
            {
                _AllPlanes_RD[i].NewChannel();
            }
        }
    }
}