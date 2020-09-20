
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
        if (AAGunControl != null)
        {
            Networking.SetOwner(AAGunControl.localPlayer, AAGunControl.VehicleMainObj);
            Networking.SetOwner(AAGunControl.localPlayer, AAGunControl.gameObject);
            Networking.SetOwner(AAGunControl.localPlayer, AAGunControl.Rotator);
            AAGunControl.Manning = true;
        }
        if (AAGunAnimator != null) AAGunAnimator.SetBool("inside", true);
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(true); }
        AAGunControl.InputXLerper = 0;
        AAGunControl.InputYLerper = 0;
        if (AAGunControl.localPlayer != null) { AAGunControl.localPlayer.UseAttachedStation(); }
    }
    public override void OnStationExited(VRCPlayerApi player)
    {
        if (player.isLocal)
        {
            if (AAGunControl != null)
            {
                AAGunControl.Manning = false;
                AAGunControl.firing = false;
            }
            if (AAGunAnimator != null) { AAGunAnimator.SetBool("inside", false); }
            if (AAGunAnimator != null) { AAGunAnimator.SetBool("firing", false); }
            if (SeatAdjuster != null) { SeatAdjuster.SetActive(false); }
            //set X rotation 0 so people don't get the seat bug when they enter again
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
            Debug.LogError("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}
