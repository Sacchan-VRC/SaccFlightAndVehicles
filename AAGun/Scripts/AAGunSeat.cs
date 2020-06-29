
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AAGunSeat : UdonSharpBehaviour
{
    public AAGunController AAGunControl;
    public GameObject HUDControl;
    public GameObject LeaveButton;
    public GameObject Saccflight;
    public GameObject Gun_pilot;
    public GameObject SeatAdjuster;
    private void Interact()
    {
        if (Saccflight != null) { Saccflight.SetActive(false); }
        if (AAGunControl != null)
        {
            Networking.SetOwner(AAGunControl.localPlayer, AAGunControl.VehicleMainObj);
            Networking.SetOwner(AAGunControl.localPlayer, AAGunControl.gameObject);
            Networking.SetOwner(AAGunControl.localPlayer, AAGunControl.Rotator);
            AAGunControl.Manning = true;
        }
        if (LeaveButton != null) { LeaveButton.SetActive(true); }
        if (HUDControl != null) { HUDControl.SetActive(true); }
        if (Gun_pilot != null) { Gun_pilot.SetActive(true); }
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(true); }
        if (AAGunControl.localPlayer != null) { AAGunControl.localPlayer.UseAttachedStation(); }
    }
    public void PilotLeave()
    {
        if (Saccflight != null) { Saccflight.SetActive(true); }
        if (LeaveButton != null) { LeaveButton.SetActive(false); }
        if (HUDControl != null) { HUDControl.SetActive(false); }
        if (AAGunControl != null)
        {
            AAGunControl.Manning = false;
            AAGunControl.firing = false;
        }
        if (Gun_pilot != null) { Gun_pilot.SetActive(false); }
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(false); }
    }
    private void OnOwnershipTransferred()
    {
        AAGunControl.firing = false;
    }
}
