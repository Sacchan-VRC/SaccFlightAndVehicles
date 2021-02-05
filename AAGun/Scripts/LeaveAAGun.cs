
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class LeaveAAGunButton : UdonSharpBehaviour
{
    public AAGunController AAGunControl;
    public VRCStation Seat;
    private void Start()
    {
        Assert(AAGunControl != null, "Start: AAGunControl != null");
        Assert(Seat != null, "Start: Seat != null");
    }
    public void Interact()
    {
        ExitStation();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("Oculus_CrossPlatform_Button4"))
        {
            ExitStation();
        }
    }

    public void ExitStation()
    {
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
