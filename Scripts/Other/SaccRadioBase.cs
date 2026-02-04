
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

namespace SaccFlightAndVehicles
{
    [DefaultExecutionOrder(100000)]// initialize after everything
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SaccRadioBase : UdonSharpBehaviour
    {
        [Header("Vehicles must have SAV_Radio extension for this to work")]
        public float VoiceNear = 199999;
        public float VoiceFar = 200000;
        // public float VoiceVolumetric = 1500;
        public float VoiceGain = .05f;
        // public float VoiceLowPass;
        [Tooltip("Make this text object darker when radio is disabled. Not required.")]
        public TextMeshProUGUI RadioEnabledTxt;
        [Tooltip("All SAV_Radio scripts")]
        public TextMeshProUGUI ChannelText;
        public TextMeshProUGUI ChannelText_ListenOnly;
        [Header("Debug, leave empty (auto filled):")]
        public SAV_Radio[] AllRadios_RD;
        public SaccRadioZone[] RadioZones;
        public SAV_Radio MyRadio;
        public byte CurrentChannel = 1;
        public byte MyChannel = 1;
        public byte CurrentChannel_ListenOnly = 0;
        public byte MyChannel_ListenOnly = 0;
        private int NextRadio;
        [System.NonSerialized] public int MyRadioSetTimes;//number of times MyVehicle has been set (for when holding 2 objects with radio, and dropping one) //Used by SAV_Radio
        [System.NonSerialized] public SaccRadioZone MyZone;
        private int NextZone;
        private int NumZones;
        private bool DoZones = false;
        private bool DoEntities = true;
        void Start()
        {
            SendCustomEventDelayedSeconds(nameof(SetRadioVoiceVolumes), 5);
            CurrentChannel = MyChannel = 1;
            if (ChannelText) { ChannelText.text = MyChannel == 0 ? "X" : MyChannel.ToString(); }
            if (ChannelText_ListenOnly) { ChannelText_ListenOnly.text = MyChannel_ListenOnly == 0 ? "X" : MyChannel_ListenOnly.ToString(); }
            string radioname = GetUdonTypeName<SAV_Radio>();
            PruneRadiosArray();
            for (int i = 0; i < AllRadios_RD.Length; i++)
            {
                AllRadios_RD[i].RadioBase = this;
                // AllRadios_RD[i].Init();
            }
            for (int i = 0; i < RadioZones.Length; i++)
            {
                RadioZones[i].RadioBase = this;
                RadioZones[i].Initialize();
            }
            NumZones = RadioZones.Length;
            if (NumZones != 0) { DoZones = true; }
        }
        private void PruneRadiosArray()
        {
            int len = AllRadios_RD.Length;
            bool[] valid = new bool[len];
            int numvalid = 0;
            for (int i = 0; i < len; i++)
            {
                if (AllRadios_RD[i])
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
                    RD_New[i - offset] = AllRadios_RD[i];
                }
            }
            AllRadios_RD = RD_New;
            if (AllRadios_RD.Length == 0)
            {
                Debug.LogWarning("RadioBase: No Entities with SAV_Radio found");
                DoEntities = false;
            }
        }
        public void SetRadioVoiceVolumes()
        {
            SendCustomEventDelayedFrames(nameof(SetRadioVoiceVolumes), 5);
            if (!MyRadio && !MyZone) { return; }
            if (DoZones)
            { SendCustomEventDelayedFrames(nameof(SetRadioVoiceVolumes_Zones), 2); }//separate in frames for optimization
            if (!DoEntities) return;
            NextRadio++;
            if (NextRadio == AllRadios_RD.Length) { NextRadio = 0; }
            SAV_Radio NextRadio_R = AllRadios_RD[NextRadio];
            if (MyRadio == NextRadio_R
                || (byte)NextRadio_R.Channel == 0
                || ((byte)NextRadio_R.Channel != CurrentChannel && (byte)NextRadio_R.Channel != CurrentChannel_ListenOnly)
                || (CurrentChannel + CurrentChannel_ListenOnly) == 0
                ) { return; }
            for (int o = 0; o < NextRadio_R.RadioSeats.Length; o++)
            {
                if (!NextRadio_R.RadioSeats[o]) continue;
                VRCPlayerApi thisplayer = NextRadio_R.RadioSeats[o].SeatedPlayer;
                if (Utilities.IsValid(thisplayer))
                {
                    thisplayer.SetVoiceDistanceNear(VoiceNear);
                    thisplayer.SetVoiceDistanceFar(VoiceFar);
                    thisplayer.SetVoiceGain(VoiceGain);
                }
            }
            if ((NextRadio_R.EntityControl.EntityPickup && NextRadio_R.EntityControl.EntityPickup.IsHeld) || NextRadio_R.EntityControl.CustomPickup_Synced_isHeld)
            {
                VRCPlayerApi thisplayer = Networking.GetOwner(NextRadio_R.gameObject);
                if (Utilities.IsValid(thisplayer))
                {
                    thisplayer.SetVoiceDistanceNear(VoiceNear);
                    thisplayer.SetVoiceDistanceFar(VoiceFar);
                    thisplayer.SetVoiceGain(VoiceGain);
                }
            }
        }
        public void UpdateVehicle(SAV_Radio VehicleRadio)
        {
            if (!MyRadio && !MyZone) { return; }
            if (MyRadio == VehicleRadio
                || VehicleRadio.Channel == 0
                || ((byte)VehicleRadio.Channel != CurrentChannel && (byte)VehicleRadio.Channel != CurrentChannel_ListenOnly)
                || (CurrentChannel + CurrentChannel_ListenOnly) == 0) { return; }
            for (int o = 0; o < VehicleRadio.RadioSeats.Length; o++)
            {
                if (!VehicleRadio.RadioSeats[o]) continue;
                VRCPlayerApi thisplayer = VehicleRadio.RadioSeats[o].SeatedPlayer;
                if (Utilities.IsValid(thisplayer))
                {
                    thisplayer.SetVoiceDistanceNear(VoiceNear);
                    thisplayer.SetVoiceDistanceFar(VoiceFar);
                    thisplayer.SetVoiceGain(VoiceGain);
                }
            }
            if ((VehicleRadio.EntityControl.EntityPickup && VehicleRadio.EntityControl.EntityPickup.IsHeld) || VehicleRadio.EntityControl.CustomPickup_Synced_isHeld)
            {
                VRCPlayerApi thisplayer = Networking.GetOwner(VehicleRadio.gameObject);
                if (Utilities.IsValid(thisplayer))
                {
                    thisplayer.SetVoiceDistanceNear(VoiceNear);
                    thisplayer.SetVoiceDistanceFar(VoiceFar);
                    thisplayer.SetVoiceGain(VoiceGain);
                }
            }
        }
        public void SetRadioVoiceVolumes_Zones()
        {
            if (!MyRadio && !MyZone) { return; }
            NextZone++;
            if (NextZone >= NumZones) { NextZone = 0; }
            SaccRadioZone NextRZ = RadioZones[NextZone];
            VRCPlayerApi[] RZ_players = NextRZ.playersinside;
            if (NextRZ.Channel == 0
            || CurrentChannel != NextRZ.Channel && CurrentChannel_ListenOnly != NextRZ.Channel
            || (CurrentChannel + CurrentChannel_ListenOnly) == 0)
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
        public void SetAllVoiceVolumesDefault(byte resetchannel)
        {
            for (int i = 0; i < AllRadios_RD.Length; i++)
            {
                if (!AllRadios_RD[i].Initialized || AllRadios_RD[i]._Channel != resetchannel) continue;
                for (int o = 0; o < AllRadios_RD[i].RadioSeats.Length; o++)
                {
                    if (MyRadio)
                    {
                        if (MyRadio.EntityControl.DoVoiceVolumeChange && AllRadios_RD[i].EntityControl == MyRadio.EntityControl) continue;
                    }
                    if (!AllRadios_RD[i].RadioSeats[o]) continue;
                    VRCPlayerApi thisplayer = AllRadios_RD[i].RadioSeats[o].SeatedPlayer;
                    if (Utilities.IsValid(thisplayer))
                    {
                        thisplayer.SetVoiceDistanceNear(0);
                        thisplayer.SetVoiceDistanceFar(25);
                        thisplayer.SetVoiceGain(15);
                    }
                }
                if ((AllRadios_RD[i].EntityControl.EntityPickup && AllRadios_RD[i].EntityControl.EntityPickup.IsHeld) || AllRadios_RD[i].EntityControl.CustomPickup_Synced_isHeld)
                {
                    VRCPlayerApi thisplayer = Networking.GetOwner(AllRadios_RD[i].gameObject);
                    if (Utilities.IsValid(thisplayer))
                    {
                        thisplayer.SetVoiceDistanceNear(0);
                        thisplayer.SetVoiceDistanceFar(25);
                        thisplayer.SetVoiceGain(15);
                    }
                }
            }
            for (int i = 0; i < RadioZones.Length; i++)
            {
                for (int o = 0; o < RadioZones[i].numPlayersInside; o++)
                {
                    RadioZones[i].playersinside[o].SetVoiceDistanceNear(0);
                    RadioZones[i].playersinside[o].SetVoiceDistanceFar(25);
                    RadioZones[i].playersinside[o].SetVoiceGain(15);
                }
            }
        }
        public void SetVehicleVolumeDefault(SAV_Radio Vehicle)
        {
            for (int i = 0; i < Vehicle.RadioSeats.Length; i++)
            {
                if (!Vehicle.RadioSeats[i]) continue;
                SetSingleVoiceVolumeDefault(Vehicle.RadioSeats[i].SeatedPlayer);
            }
        }
        public void SetSingleVoiceVolumeDefault(VRCPlayerApi player)
        {
            if (!Utilities.IsValid(player)) { return; }
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
            if (ChannelText) { ChannelText.text = MyChannel == 0 ? "X" : MyChannel.ToString(); }
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
            if (ChannelText) { ChannelText.text = MyChannel == 0 ? "X" : MyChannel.ToString(); }
            CurrentChannel = MyChannel;
            UpdateRadioScripts();
        }
        public void SetChannel(int inChannel)
        {
            inChannel = mod_noneg(inChannel, 17);
            CurrentChannel = MyChannel = (byte)(inChannel);
            if (ChannelText) { ChannelText.text = MyChannel == 0 ? "X" : MyChannel.ToString(); }
            UpdateRadioScripts();
        }
        public void IncreaseChannel_ListenOnly()
        {
            if (MyChannel_ListenOnly + 1 > 16) { MyChannel_ListenOnly = 0; }
            else
            {
                MyChannel_ListenOnly++;
            }
            if (ChannelText_ListenOnly) { ChannelText_ListenOnly.text = MyChannel_ListenOnly == 0 ? "X" : MyChannel_ListenOnly.ToString(); }
            CurrentChannel_ListenOnly = MyChannel_ListenOnly;
            UpdateRadioScripts_ListenOnly();
        }
        public void DecreaseChannel_ListenOnly()
        {
            if (MyChannel_ListenOnly - 1 < 0) { MyChannel_ListenOnly = 16; }
            else
            {
                MyChannel_ListenOnly--;
            }
            if (ChannelText_ListenOnly) { ChannelText_ListenOnly.text = MyChannel_ListenOnly == 0 ? "X" : MyChannel_ListenOnly.ToString(); }
            CurrentChannel_ListenOnly = MyChannel_ListenOnly;
            UpdateRadioScripts_ListenOnly();
        }
        public void SetChannel_ListenOnly(int inChannel)
        {
            inChannel = mod_noneg(inChannel, 17);
            CurrentChannel_ListenOnly = MyChannel_ListenOnly = (byte)(inChannel);
            if (ChannelText_ListenOnly) { ChannelText_ListenOnly.text = MyChannel_ListenOnly == 0 ? "X" : MyChannel_ListenOnly.ToString(); }
            UpdateRadioScripts_ListenOnly();
        }
        int mod_noneg(int x, int m)
        {
            return (x % m + m) % m;
        }
        void UpdateRadioScripts()
        {
            for (int i = 0; i < AllRadios_RD.Length; i++)
            {
                AllRadios_RD[i].NewChannel();
            }
        }
        void UpdateRadioScripts_ListenOnly()
        {
            for (int i = 0; i < AllRadios_RD.Length; i++)
            {
                AllRadios_RD[i].NewChannel_ListenOnly();
            }
        }
    }
}