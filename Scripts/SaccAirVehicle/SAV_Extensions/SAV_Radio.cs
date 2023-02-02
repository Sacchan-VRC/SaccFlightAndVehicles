
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class SAV_Radio : UdonSharpBehaviour
{
    public SaccEntity EntityControl;
    public UdonSharpBehaviour RadioBase;
    [UdonSynced] public bool RadioOn = true;
    private bool Initialized;
    private VRCPlayerApi localPlayer;
    private int CurrentOwnerID;
    public void SFEXT_P_PassengerEnter()
    {
        if (RadioBase)
        {
            RadioBase.SetProgramVariable("MyVehicle", EntityControl);
        }
    }
    public void SFEXT_P_PassengerExit()
    {
        if (RadioBase)
        {
            RadioBase.SetProgramVariable("MyVehicle", null);
            RadioBase.SendCustomEvent("SetAllVoiceVolumesDefault");
        }
    }
    public void Init()
    {
        Initialized = true;
        localPlayer = Networking.LocalPlayer;
        CurrentOwnerID = Networking.GetOwner(EntityControl.gameObject).playerId;
    }
    void Start()
    {
        if (!Initialized)
        { Init(); }
    }
    public void SFEXT_L_EntityStart()
    {
        if (!Initialized)
        { Init(); }
    }
    public void SFEXT_O_PilotEnter()
    {
        if (RadioBase)
        {
            RadioBase.SetProgramVariable("MyVehicle", EntityControl);
            RadioOn = (bool)RadioBase.GetProgramVariable("RadioEnabled");
            RequestSerialization();
        }
    }
    public void SFEXT_O_PilotExit()
    {
        if (RadioBase)
        {
            RadioBase.SetProgramVariable("MyVehicle", null);
            RadioBase.SendCustomEvent("SetAllVoiceVolumesDefault");
        }
    }
    public void SFEXT_L_OwnershipTransfer()
    {
        //reset current owner's voice volume
        RadioBase.SetProgramVariable("SingleVVPlayerID", CurrentOwnerID);
        RadioBase.SendCustomEvent("SetSingleVoiceVolumeDefault");
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
