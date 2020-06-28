
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
        if (Seat != null) { Seat.ExitStation(AAGunControl.localPlayer); }
    }
}
