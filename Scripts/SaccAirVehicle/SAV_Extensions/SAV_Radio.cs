
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
    public bool RadioOn = true;
    [UdonSynced] public byte Channel = 0;
    private bool Initialized;
    private VRCPlayerApi localPlayer;
    private int CurrentOwnerID;
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
        EnterVehicle();
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
            if (EntityControl.IsOwner)
            {
                RadioOn = (bool)RadioBase.GetProgramVariable("RadioEnabled");
                if (RadioOn)
                { Channel = (byte)RadioBase.GetProgramVariable("Channel"); }
                else
                { Channel = 0; }
                RequestSerialization();
            }
        }
    }
    public void ExitVehicle()
    {
        if (RadioBase)
        {
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
