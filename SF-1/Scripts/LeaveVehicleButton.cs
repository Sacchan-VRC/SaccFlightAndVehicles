
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class LeaveVehicleButton : UdonSharpBehaviour
{
    public EngineController EngineControl;
    public VRCStation Seat;
    public VRCPlayerApi SeatedPlayer;//used for messing with voice volumes
    private void Start()
    {
        Assert(EngineControl != null, "Start: EngineControl != null");
        Assert(Seat != null, "Start: Seat != null");
    }
    private void Interact()
    {
        if (EngineControl != null && EngineControl.Speed < 1)
        {
            ExitStation();
        }
        else if (EngineControl == null)
        {
            ExitStation();
        }
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || (Input.GetButtonDown("Oculus_CrossPlatform_Button4")))
        {
            ExitStation();
        }
    }

    public void ExitStation()
    {
        if (gameObject.activeSelf)//so we only exit our own seat when all seats are called
            if (Seat != null) { Seat.ExitStation(EngineControl.localPlayer); }
    }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogError("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}
