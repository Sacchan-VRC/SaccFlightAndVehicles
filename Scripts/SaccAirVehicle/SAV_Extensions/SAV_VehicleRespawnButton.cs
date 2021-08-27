
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SAV_VehicleRespawnButton : UdonSharpBehaviour
{
    public SaccAirVehicle EngineControl;
    private SaccEntity EntityControl;
    private void Start()
    {
        EntityControl = EngineControl.EntityControl;
    }
    private void Interact()
    {
        if (!EngineControl.Occupied && !EntityControl.dead)
        {
            EntityControl.RespawnStatusLocal();
            EntityControl.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ResetStatus");
        }
    }
}