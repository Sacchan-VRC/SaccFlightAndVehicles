
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AAGunSeat : UdonSharpBehaviour
{
    public AAGunController AAGunControl;
    public GameObject HUDControl;
    public GameObject Saccflight;
    public GameObject SeatAdjuster;
    private Animator AAGunAnimator;
    void Start()
    {
        if (AAGunControl.VehicleMainObj != null) { AAGunAnimator = AAGunControl.VehicleMainObj.GetComponent<Animator>(); }
    }
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
        if (AAGunAnimator != null) AAGunAnimator.SetBool("inside", true);
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(true); }
        if (AAGunControl.localPlayer != null) { AAGunControl.localPlayer.UseAttachedStation(); }
    }
    public void GunnerLeave()
    {
        if (Saccflight != null) { Saccflight.SetActive(true); }
        if (AAGunControl != null)
        {
            AAGunControl.Manning = false;
            AAGunControl.firing = false;
        }
        if (AAGunAnimator != null) { AAGunAnimator.SetBool("inside", true); }
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(false); }
        //set X rotation 0 so people don't get the seat bug when they enter again
        AAGunControl.Rotator.transform.localRotation = Quaternion.Euler(new Vector3(0, AAGunControl.Rotator.transform.localRotation.y, AAGunControl.Rotator.transform.localRotation.z));
    }
    private void OnOwnershipTransferred()
    {
        AAGunControl.firing = false;
    }
}
