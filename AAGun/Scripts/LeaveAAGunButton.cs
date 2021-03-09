
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class LeaveAAGunButton : UdonSharpBehaviour
{
    public AAGunController AAGunControl;
    public VRCStation Seat;
    private bool InSeat;//used to make the player exit the aagun if the button to leave it is disabled for any reason
    private void Start()
    {
        Assert(AAGunControl != null, "Start: AAGunControl != null");
        Assert(Seat != null, "Start: Seat != null");
    }
    private void OnDisable()
    {
        if (InSeat)
        {
            ExitStation();
        }
    }
    private void Interact()
    {
        InSeat = true;
        ExitStation();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("Oculus_CrossPlatform_Button4"))
        {
            ExitStation();
        }
    }

    public void ExitStation()
    {
        InSeat = false;
        if (Seat != null) { Seat.ExitStation(AAGunControl.localPlayer); }
    }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogWarning("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}
