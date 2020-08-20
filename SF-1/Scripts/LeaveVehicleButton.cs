
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
        if (Input.GetKeyDown(KeyCode.T) || (Input.GetButtonDown("Oculus_CrossPlatform_Button4")))
        {
            ExitStation();
        }
    }

    public void ExitStation()
    {
        if (gameObject.activeSelf)//so we only exit our own seat
            if (Seat != null) { Seat.ExitStation(EngineControl.localPlayer); }
    }
}
