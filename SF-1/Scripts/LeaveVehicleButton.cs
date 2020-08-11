
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class LeaveVehicleButton : UdonSharpBehaviour
{
    public EngineController EngineControl;
    public VRCStation Seat;
    private void Interact()
    {
        if (EngineControl != null && EngineControl.CurrentVel.magnitude < 1)
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
        if (Input.GetKeyDown(KeyCode.T) || (Input.GetButtonDown("Oculus_CrossPlatform_Button4")))
        {
            ExitStation();
        }
    }

    public void ExitStation()
    {
        if (Seat != null) { Seat.ExitStation(EngineControl.localPlayer); }
    }
}
