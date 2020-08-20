
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PassengerSeat : UdonSharpBehaviour
{
    public EngineController EngineControl;
    public GameObject LeaveButton;
    public GameObject SeatAdjuster;
    //public GameObject Saccflight;
    public GameObject EnableOther;
    private void Interact()
    {
        //if (Saccflight != null) { Saccflight.SetActive(false); }
        EngineControl.Passenger = true;
        Networking.SetOwner(EngineControl.localPlayer, gameObject);
        if (LeaveButton != null) { LeaveButton.SetActive(true); }
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(true); }
        if (EnableOther != null) { EnableOther.SetActive(true); }
        if (EngineControl.HUDControl != null) { EngineControl.HUDControl.gameObject.SetActive(true); }
        if (EngineControl.CanopyOpen) EngineControl.CanopyCloseTimer = -100001;
        else EngineControl.CanopyCloseTimer = -1;
        EngineControl.localPlayer.UseAttachedStation();
    }
    public void PassengerLeave()
    {
        if (EngineControl != null)
        {
            EngineControl.Passenger = false;
            EngineControl.localPlayer.SetVelocity(EngineControl.CurrentVel);
        }
        //if (Saccflight != null) { Saccflight.SetActive(true); }
        if (LeaveButton != null) { LeaveButton.SetActive(false); }
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(false); }
        if (EnableOther != null) { EnableOther.SetActive(false); }
        if (EngineControl.HUDControl != null) { EngineControl.HUDControl.gameObject.SetActive(false); }
    }
}
