
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GunnerSeat : UdonSharpBehaviour
{
    public EngineController EngineController;
    public GameObject LeaveButton;
    public GameObject Saccflight;
    public GameObject GunObject;
    public GameObject GunController;
    private GunShipGunController GunControl;

    private void Interact()
    {
        if (GunControl != null) { GunControl.Manning = true; }
        if (Saccflight != null) { Saccflight.SetActive(false); }
        if (GunObject != null) { Networking.SetOwner(EngineController.localPlayer, GunObject); }
        if (GunController != null) { Networking.SetOwner(EngineController.localPlayer, GunController); }
        if (LeaveButton != null) { LeaveButton.SetActive(true); }
        EngineController.Passenger = true;
        EngineController.localPlayer.UseAttachedStation();
    }
    private void Start()
    {
        if (GunController != null) { GunControl = GunController.GetComponent<GunShipGunController>(); }
    }
    public void GunnerLeave()
    {
        EngineController.localPlayer.SetVelocity(EngineController.CurrentVel);
        EngineController.Passenger = false;
        if (Saccflight != null) { Saccflight.SetActive(true); }
        if (GunControl != null) { GunControl.Manning = false; }
        if (LeaveButton != null) { LeaveButton.SetActive(false); }
    }
}
