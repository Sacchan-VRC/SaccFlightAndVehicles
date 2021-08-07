
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class VehicleRespawnButton : UdonSharpBehaviour
{
    public EngineController EngineControl;
    private void Start()
    {
        Assert(EngineControl != null, "Start: EngineControl != null");
    }
    private void Interact()
    {
        if (!EngineControl.Occupied && !EngineControl.dead)
        {
            EngineControl.RespawnStatusLocal();
            EngineControl.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ResetStatus");
        }
    }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogWarning("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}