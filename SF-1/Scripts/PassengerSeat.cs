
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PassengerSeat : UdonSharpBehaviour
{
    public EngineController EngineControl;
    public GameObject LeaveButton;
    public GameObject SeatAdjuster;
    private Transform PlaneMesh;
    private LayerMask Planelayer;
    private void Start()
    {
        Assert(EngineControl != null, "Start: EngineControl != null");
        Assert(LeaveButton != null, "Start: LeaveButton != null");
        Assert(SeatAdjuster != null, "Start: SeatAdjuster != null");

        PlaneMesh = EngineControl.PlaneMesh.transform;
        Planelayer = PlaneMesh.gameObject.layer;
    }
    private void Interact()
    {
        EngineControl.Passenger = true;
        Networking.SetOwner(EngineControl.localPlayer, gameObject);
        if (LeaveButton != null) { LeaveButton.SetActive(true); }
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(true); }
        if (EngineControl.HUDControl != null) { EngineControl.HUDControl.gameObject.SetActive(true); }
        if (EngineControl.EffectsControl.CanopyOpen) EngineControl.CanopyCloseTimer = -100001;
        else EngineControl.CanopyCloseTimer = -1;
        EngineControl.localPlayer.UseAttachedStation();
        if (PlaneMesh != null)
        {
            Transform[] children = PlaneMesh.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                child.gameObject.layer = EngineControl.OnboardPlaneLayer;
            }
        }
    }
    public override void OnStationExited(VRCPlayerApi player)
    {
        if (player.isLocal)
        {
            if (EngineControl != null)
            {
                EngineControl.Passenger = false;
                EngineControl.localPlayer.SetVelocity(EngineControl.CurrentVel);
                EngineControl.MissilesIncoming = 0;
            }
            if (LeaveButton != null) { LeaveButton.SetActive(false); }
            if (SeatAdjuster != null) { SeatAdjuster.SetActive(false); }
            if (EngineControl.HUDControl != null) { EngineControl.HUDControl.gameObject.SetActive(false); }
            if (PlaneMesh != null)
            {
                Transform[] children = PlaneMesh.GetComponentsInChildren<Transform>();
                foreach (Transform child in children)
                {
                    child.gameObject.layer = Planelayer;
                }
            }
        }
    }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogError("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}
