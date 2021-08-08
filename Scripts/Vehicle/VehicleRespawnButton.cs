
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class VehicleRespawnButton : UdonSharpBehaviour
{
    public EngineController EngineControl;
    private void Interact()
    {
        if (!EngineControl.Occupied && !EngineControl.dead)
        {
            EngineControl.RespawnStatusLocal();
            EngineControl.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ResetStatus");
        }
    }
}