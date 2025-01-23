
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
        private SAV_Radio[] _AllEntities_RD;
        public float VoiceNear = 199999;
        public float VoiceFar = 200000;
        // public float VoiceVolumetric = 1500;
        public float VoiceGain = .05f;
        // public float VoiceLowPass;
        [Tooltip("Make this text object darker when radio is disabled. Not required.")]
        public TextMeshProUGUI RadioEnabledTxt;
        public bool RadioEnabled_ = true;
        private byte CurrentChannel = 1;
        public byte MyChannel = 1;
        [Header("All Planes and RadioZones are filled automatically on build.")]
        public Transform[] AllPlanes;
        public SaccRadioZone[] RadioZones;
        public TextMeshProUGUI ChannelText;
        [Header("Debug, leave empty:")]
        public SaccEntity MyEntity;
        [System.NonSerialized] public int MyVehicleSetTimes;//number of times MyVehicle has been set (for when holding 2 objects with radio, and dropping one) //Used by SAV_Radio
        [System.NonSerialized] public SaccRadioZone MyZone;
        private int NextEntity;
        private int NextZone;
        private int NumZones;
        private bool DoZones = false;
        void Start()
        {
            SendCustomEventDelayedSeconds(nameof(SetRadioVoiceVolumes), 5);
            CurrentChannel = MyChannel = 1;
            if (ChannelText) { ChannelText.text = MyChannel.ToString(); }
            SaccEntity[] _AllPlanes_ENT = new SaccEntity[AllPlanes.Length];
            _AllEntities_RD = new SAV_Radio[AllPlanes.Length];
            string radioname = GetUdonTypeName<SAV_Radio>();
            for (int i = 0; i < AllPlanes.Length; i++)
            {
                _AllPlanes_ENT[i] = (SaccEntity)AllPlanes[i].GetComponent<SaccEntity>();
                if (_AllPlanes_ENT[i]) { _AllEntities_RD[i] = (SAV_Radio)_AllPlanes_ENT[i].GetExtention(radioname); }
            }
            PruneRadiosArray();
            for (int i = 0; i < _AllEntities_RD.Length; i++)
            {
                _AllEntities_RD[i].RadioBase = this;
                _AllEntities_RD[i].Init();
            }
            NumZones = RadioZones.Length;
            if (NumZones != 0) { DoZones = true; }
        }
        private void PruneRadiosArray()
        {
            int len = _AllEntities_RD.Length;
            bool[] valid = new bool[len];
            int numvalid = 0;
            for (int i = 0; i < len; i++)
            {
                if (_AllEntities_RD[i])
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
                    RD_New[i - offset] = _AllEntities_RD[i];
                }
            }
            _AllEntities_RD = RD_New;
        }
        public void SetRadioVoiceVolumes()
        {
            SendCustomEventDelayedFrames(nameof(SetRadioVoiceVolumes), 5);
            if ((!MyEntity || !RadioEnabled_) && !MyZone) { return; }
            if (DoZones)
            { SendCustomEventDelayedFrames(nameof(SetRadioVoiceVolumes_Zones), 2); }//separate in frames for optimization
            NextEntity++;
            if (NextEntity == _AllEntities_RD.Length) { NextEntity = 0; }
            SaccEntity NextEntity_SE = _AllEntities_RD[NextEntity].EntityControl;
            if (MyEntity == NextEntity_SE
                || (byte)_AllEntities_RD[NextEntity].Channel != CurrentChannel
                || CurrentChannel == 0) { return; }
            for (int o = 0; o < NextEntity_SE.VehicleSeats.Length; o++)
            {
                VRCPlayerApi thisplayer = NextEntity_SE.VehicleSeats[o].SeatedPlayer;
                if (thisplayer != null)
                {
                    thisplayer.SetVoiceDistanceNear(VoiceNear);
                    thisplayer.SetVoiceDistanceFar(VoiceFar);
                    thisplayer.SetVoiceGain(VoiceGain);
                }
            }
            if ((NextEntity_SE.EntityPickup && NextEntity_SE.EntityPickup.IsHeld) || NextEntity_SE.CustomPickup_Synced_isHeld)
            {
                VRCPlayerApi thisplayer = Networking.GetOwner(NextEntity_SE.gameObject);
                if (thisplayer != null)
                {
                    thisplayer.SetVoiceDistanceNear(VoiceNear);
                    thisplayer.SetVoiceDistanceFar(VoiceFar);
                    thisplayer.SetVoiceGain(VoiceGain);
                }
            }
        }
        public void UpdateVehicle(SaccEntity Vehicle)
        {
            if ((!MyEntity || !RadioEnabled_) && !MyZone) { return; }
            SaccEntity NextEntity_SE = Vehicle;
            SAV_Radio NextEntity_R = (SAV_Radio)NextEntity_SE.GetExtention(GetUdonTypeName<SAV_Radio>());
            if (MyEntity == NextEntity_SE
                || !NextEntity_R
                || (byte)NextEntity_R.Channel != CurrentChannel
                || CurrentChannel == 0) { return; }
            for (int o = 0; o < NextEntity_SE.VehicleSeats.Length; o++)
            {
                VRCPlayerApi thisplayer = NextEntity_SE.VehicleSeats[o].SeatedPlayer;
                if (thisplayer != null)
                {
                    thisplayer.SetVoiceDistanceNear(VoiceNear);
                    thisplayer.SetVoiceDistanceFar(VoiceFar);
                    thisplayer.SetVoiceGain(VoiceGain);
                }
            }
            if ((NextEntity_SE.EntityPickup && NextEntity_SE.EntityPickup.IsHeld) || NextEntity_SE.CustomPickup_Synced_isHeld)
            {
                VRCPlayerApi thisplayer = Networking.GetOwner(NextEntity_SE.gameObject);
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
            if ((!MyEntity || !RadioEnabled_) && !MyZone) { return; }
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
        public void IncreaseChannel()
        {
            if (MyChannel + 1 > 16) { MyChannel = 0; }
            else
            {
                MyChannel++;
            }
            if (ChannelText) { ChannelText.text = MyChannel == 0 ? "OFF" : MyChannel.ToString(); }
            CurrentChannel = MyChannel;
            UpdateRadioScripts();
        }
        public void DecreaseChannel()
        {
            if (MyChannel - 1 < 0) { MyChannel = 16; }
            else
            {
                MyChannel--;
            }
            if (ChannelText) { ChannelText.text = MyChannel == 0 ? "OFF" : MyChannel.ToString(); }
            CurrentChannel = MyChannel;
            UpdateRadioScripts();
        }
        public void SetChannel(int inChannel)
        {
            inChannel = mod_noneg(inChannel, 17);
            CurrentChannel = MyChannel = (byte)(inChannel);
            if (ChannelText) { ChannelText.text = MyChannel == 0 ? "OFF" : MyChannel.ToString(); }
            UpdateRadioScripts();
        }
        int mod_noneg(int x, int m)
        {
            return (x % m + m) % m;
        }
        void UpdateRadioScripts()
        {
            for (int i = 0; i < _AllEntities_RD.Length; i++)
            {
                _AllEntities_RD[i].NewChannel();
            }
        }
    }
}