
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AAGunSeat : UdonSharpBehaviour
{
    [SerializeField] private AAGunController AAGunControl;
    private HUDControllerAAGun HUDControl;
    [SerializeField] private GameObject SeatAdjuster;
    private Animator AAGunAnimator;
    private VRCPlayerApi localPlayer;
    private int ThisStationID;
    private bool firsttime = true;
    private Transform Seat;
    private Quaternion SeatStartRot;
    void Start()
    {
        if (AAGunControl.VehicleMainObj != null) { AAGunAnimator = AAGunControl.VehicleMainObj.GetComponent<Animator>(); }
        HUDControl = AAGunControl.HUDControl;

        Seat = ((VRC.SDK3.Components.VRCStation)GetComponent(typeof(VRC.SDK3.Components.VRCStation))).stationEnterPlayerLocation.transform;
        SeatStartRot = Seat.localRotation;

        localPlayer = Networking.LocalPlayer;
    }
    private void Interact()
    {
        Networking.SetOwner(AAGunControl.localPlayer, AAGunControl.VehicleMainObj);
        Networking.SetOwner(AAGunControl.localPlayer, AAGunControl.gameObject);
        Networking.SetOwner(AAGunControl.localPlayer, AAGunControl.Rotator);
        AAGunControl.Manning = true;
        AAGunControl.RotationSpeedX = 0;
        AAGunControl.RotationSpeedY = 0;
        if (AAGunAnimator != null) AAGunAnimator.SetBool("inside", true);
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(true); }
        Seat.rotation = Quaternion.Euler(0, Seat.eulerAngles.y, 0);//fixes offset seated position when getting in a rolled/pitched vehicle
        AAGunControl.localPlayer.UseAttachedStation();
        Seat.localRotation = SeatStartRot;
        if (AAGunControl.HUDControl != null) { AAGunControl.HUDControl.GUN_TargetSpeedLerper = 0; }

        if (AAGunControl.NumAAMTargets != 0) { AAGunControl.DoAAMTargeting = true; }

        //Make sure EngineControl.AAMCurrentTargetEngineControl is correct
        var Target = AAGunControl.AAMTargets[AAGunControl.AAMTarget];
        if (Target && Target.transform.parent)
        {
            AAGunControl.AAMCurrentTargetEngineControl = Target.transform.parent.GetComponent<EngineController>();
        }
        if (localPlayer != null && localPlayer.IsUserInVR())
        {
            AAGunControl.InVR = true;//has to be set on enter otherwise Built And Test thinks you're in desktop
        }
    }
    public override void OnStationEntered(VRCPlayerApi player)
    {
        if (firsttime) { InitializeSeat(); }//can't do this in start because hudcontrol might not have initialized
        AAGunControl.EnterSetStatus();
    }
    public override void OnStationExited(VRCPlayerApi player)
    {
        if (firsttime) { InitializeSeat(); }
        AAGunControl.LastHealthUpdate = Time.time;
        if (player.isLocal)
        {
            AAGunControl.Manning = false;
            AAGunControl.firing = false;
            AAGunControl.AAMLockTimer = 0;
            AAGunControl.AAMHasTarget = false;
            AAGunControl.DoAAMTargeting = false;
            AAGunAnimator.SetBool("inside", false);
            if (SeatAdjuster != null) { SeatAdjuster.SetActive(false); }
        }
    }
    private void OnOwnershipTransferred()
    {
        AAGunControl.firing = false;
    }
    private void InitializeSeat()
    {
        HUDControl.FindSeats();
        int x = 0;
        foreach (VRCStation station in HUDControl.VehicleStations)
        {
            if (station.gameObject == gameObject)
            {
                ThisStationID = x;
                HUDControl.PilotSeat = x;
            }
            x++;
        }
        firsttime = false;
    }
}
