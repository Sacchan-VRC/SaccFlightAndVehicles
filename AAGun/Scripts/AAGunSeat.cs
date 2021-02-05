
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AAGunSeat : UdonSharpBehaviour
{
    public AAGunController AAGunControl;
    public GameObject HUDControl;
    public GameObject SeatAdjuster;
    private Animator AAGunAnimator;
    void Start()
    {
        Assert(AAGunControl != null, "Start: AAGunControl != null");
        Assert(HUDControl != null, "Start: HUDControl != null");
        Assert(SeatAdjuster != null, "Start: SeatAdjuster != null");

        if (AAGunControl.VehicleMainObj != null) { AAGunAnimator = AAGunControl.VehicleMainObj.GetComponent<Animator>(); }
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
        if (AAGunControl.localPlayer != null) { AAGunControl.localPlayer.UseAttachedStation(); }
        if (AAGunControl.HUDControl != null) { AAGunControl.HUDControl.GUN_TargetSpeedLerper = 0; }

        if (AAGunControl.NumAAMTargets != 0) { AAGunControl.DoAAMTargeting = true; }

        //Make sure EngineControl.AAMCurrentTargetEngineControl is correct
        var Target = AAGunControl.AAMTargets[AAGunControl.AAMTarget];
        if (Target && Target.transform.parent)
        {
            AAGunControl.AAMCurrentTargetEngineControl = Target.transform.parent.GetComponent<EngineController>();
        }
    }
    public override void OnStationEntered(VRCPlayerApi player)
    {
        AAGunControl.EnterSetStatus();
    }
    public override void OnStationExited(VRCPlayerApi player)
    {
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
            //set X rotation 0 so people don't get the seat orientation bug when they enter again
            AAGunControl.Rotator.transform.localRotation = Quaternion.Euler(new Vector3(0, AAGunControl.Rotator.transform.localRotation.eulerAngles.y, 0));
        }
    }
    private void OnOwnershipTransferred()
    {
        AAGunControl.firing = false;
    }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogWarning("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}
