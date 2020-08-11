
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class LeaveAAGunButton : UdonSharpBehaviour
{
    public AAGunController AAGunControl;
    public VRCStation Seat;
    public void Interact()
    {
        ExitStation();
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
        if (Seat != null) { Seat.ExitStation(AAGunControl.localPlayer); }
    }
}
