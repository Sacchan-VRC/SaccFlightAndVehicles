
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SaccRadioBase : UdonSharpBehaviour
{
    public Transform[] AllPlanes;
    private SaccFlightAndVehicles.SaccEntity[] _AllPlanes_ENT;
    private SAV_Radio[] _AllPlanes_RD;
    public float VoiceNear = 199999;
    public float VoiceFar = 200000;
    // public float VoiceVolumetric = 1500;
    public float VoiceGain = .05f;
    // public float VoiceLowPass;
    [Tooltip("Make this text object darker when radio is disabled. Not required.")]
    public TextMeshProUGUI RadioEnabledTxt;
    public bool RadioEnabled = true;
    [Header("Debug, leave empty:")]
    public SaccFlightAndVehicles.SaccEntity MyVehicle;
    private int NextPlane_voicevol;
    void Start()
    {
        SendCustomEventDelayedSeconds(nameof(SetRadioVoiceVolumes), 5);
        _AllPlanes_ENT = new SaccFlightAndVehicles.SaccEntity[AllPlanes.Length];
        _AllPlanes_RD = new SAV_Radio[AllPlanes.Length];
        for (int i = 0; i < AllPlanes.Length; i++)
        {
            _AllPlanes_ENT[i] = (SaccFlightAndVehicles.SaccEntity)AllPlanes[i].GetComponent<SaccFlightAndVehicles.SaccEntity>();
            if (_AllPlanes_ENT[i]) { _AllPlanes_RD[i] = (SAV_Radio)_AllPlanes_ENT[i].GetExtention("SAV_Radio"); }
        }
    }
    public void SetRadioVoiceVolumes()
    {
        SendCustomEventDelayedFrames(nameof(SetRadioVoiceVolumes), 5);
        if (!MyVehicle || !RadioEnabled) { return; }
        NextPlane_voicevol++;
        if (NextPlane_voicevol == _AllPlanes_RD.Length) { NextPlane_voicevol = 0; }
        if (_AllPlanes_RD[NextPlane_voicevol])
        {
            if (!_AllPlanes_RD[NextPlane_voicevol].RadioOn || MyVehicle == _AllPlanes_ENT[NextPlane_voicevol]) { return; }
            for (int o = 0; o < _AllPlanes_ENT[NextPlane_voicevol].VehicleSeats.Length; o++)
            {
                VRCPlayerApi thisplayer = _AllPlanes_ENT[NextPlane_voicevol].VehicleSeats[o].SeatedPlayer;
                if (thisplayer != null)
                {
                    thisplayer.SetVoiceDistanceNear(VoiceNear);
                    thisplayer.SetVoiceDistanceFar(VoiceFar);
                    thisplayer.SetVoiceGain(VoiceGain);
                }
            }
        }
    }
    public void SetRadioVoiceVolumesDefault()
    {
        for (int i = 0; i < _AllPlanes_RD.Length; i++)
        {
            if (_AllPlanes_RD[i])
            {
                for (int o = 0; o < _AllPlanes_ENT[i].VehicleSeats.Length; o++)
                {
                    VRCPlayerApi thisplayer = _AllPlanes_ENT[i].VehicleSeats[o].SeatedPlayer;
                    if (thisplayer != null)
                    {
                        thisplayer.SetVoiceDistanceNear(0);
                        thisplayer.SetVoiceDistanceFar(25);
                        thisplayer.SetVoiceGain(15);
                    }
                }
            }
        }
    }
    public void ToggleRadio()
    {
        RadioEnabled = !RadioEnabled;
        if (RadioEnabledTxt) RadioEnabledTxt.color = RadioEnabled ? Color.white : Color.gray;
    }
}
