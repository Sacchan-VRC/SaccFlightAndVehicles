
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class SAV_Radio : UdonSharpBehaviour
{
    public SaccEntity EntityControl;
    [System.NonSerialized] public SaccRadioBase RadioBase;
    public bool RadioOn = true;
    [Header("Debug:")]
    [UdonSynced, FieldChangeCallback(nameof(Channel))] private byte _Channel;
    public byte Channel
    {
        set
        {
            _Channel = value;
            if (EntityControl.InVehicle)
            {
                UpdateChannel();
                RadioBase.SetAllVoiceVolumesDefault();
            }
            RadioBase.SetVehicleVolumeDefault(EntityControl);
        }
        get => _Channel;
    }
    private bool Initialized;
    private VRCPlayerApi localPlayer;
    private int CurrentOwnerID;
    private bool ChannelSwapped;
    public void Init()
    {
        Initialized = true;
        localPlayer = Networking.LocalPlayer;
        CurrentOwnerID = Networking.GetOwner(EntityControl.gameObject).playerId;
    }
    public void SFEXT_L_EntityStart()
    {
        if (!Initialized)
        { Init(); }
    }
    public void SFEXT_O_PilotEnter()
    {
        EnterVehicle();
    }
    public void SFEXT_G_PilotEnter()
    {
        if (EntityControl.Passenger) { SendCustomEventDelayedSeconds(nameof(UpdateChannel), 2); }
    }
    public void UpdateChannel()
    {
        if (!EntityControl.Using)
        {
            ChannelSwapped = true;
            RadioBase.SetProgramVariable("CurrentChannel", Channel);
        }
    }
    public void SFEXT_O_PilotExit()
    {
        ExitVehicle();
    }
    public void SFEXT_P_PassengerEnter()
    {
        EnterVehicle();
    }
    public void SFEXT_P_PassengerExit()
    {
        ExitVehicle();
    }
    public void EnterVehicle()
    {
        if (RadioBase)
        {
            RadioBase.SetProgramVariable("MyVehicleSetTimes", (int)RadioBase.GetProgramVariable("MyVehicleSetTimes") + 1);
            RadioBase.SetProgramVariable("MyVehicle", EntityControl);
            //if not pilot, set my channel on radiobase to vehicle's and set back on exit
            NewChannel();
            UpdateChannel();
        }
    }
    public void NewChannel()
    {
        if (EntityControl.Piloting)
        {
            RadioOn = (bool)RadioBase.GetProgramVariable("RadioEnabled");
            if (RadioOn)
            { Channel = (byte)RadioBase.GetProgramVariable("MyChannel"); }
            else
            { Channel = 0; }
            RequestSerialization();
        }
    }
    public void ExitVehicle()
    {
        if (RadioBase)
        {
            if (ChannelSwapped)
            {
                ChannelSwapped = false;
                RadioBase.SetProgramVariable("CurrentChannel", (byte)RadioBase.GetProgramVariable("MyChannel"));
            }
            int mvst = (int)RadioBase.GetProgramVariable("MyVehicleSetTimes") - 1;
            RadioBase.SetProgramVariable("MyVehicleSetTimes", mvst);
            if (mvst == 0)
            {
                RadioBase.SetProgramVariable("MyVehicle", null);
                RadioBase.SendCustomEvent("SetAllVoiceVolumesDefault");
            }
        }
    }
    public void SFEXT_L_OwnershipTransfer()
    {
        //reset current owner's voice volume
        VRCPlayerApi ownerAPI = VRCPlayerApi.GetPlayerById(CurrentOwnerID);
        RadioBase.SetSingleVoiceVolumeDefault(ownerAPI);
        CurrentOwnerID = Networking.GetOwner(EntityControl.gameObject).playerId;
    }
    public void SFEXT_O_OnPickup()
    {
        SFEXT_O_PilotEnter();
    }
    public void SFEXT_O_OnDrop()
    {
        SFEXT_O_PilotExit();
    }
    public void SFEXT_G_OnDrop()
    {
        if (CurrentOwnerID == Networking.GetOwner(EntityControl.gameObject).playerId)
        {
            //reset current owner's voice volume
            RadioBase.SetProgramVariable("SetSingleVoiceVolumeID", CurrentOwnerID);
            RadioBase.SendCustomEvent("SetSingleVoiceVolumeDefault");
        }
    }
}
