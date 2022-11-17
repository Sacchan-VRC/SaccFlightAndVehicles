
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
            RadioBase.SendCustomEvent("SetRadioVoiceVolumesDefault");
        }
    }
    public void Init()
    {
        Initialized = true;
        localPlayer = Networking.LocalPlayer;
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
            RadioBase.SendCustomEvent("SetRadioVoiceVolumesDefault");
        }
    }
}
