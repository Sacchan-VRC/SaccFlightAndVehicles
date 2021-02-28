
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PassengerSeat : UdonSharpBehaviour
{
    public EngineController EngineControl;
    public GameObject LeaveButton;
    public GameObject SeatAdjuster;
    private LeaveVehicleButton LeaveButtonControl;
    private Transform PlaneMesh;
    private LayerMask Planelayer;
    private void Start()
    {
        Assert(EngineControl != null, "Start: EngineControl != null");
        Assert(LeaveButton != null, "Start: LeaveButton != null");
        Assert(SeatAdjuster != null, "Start: SeatAdjuster != null");

        LeaveButtonControl = LeaveButton.GetComponent<LeaveVehicleButton>();

        PlaneMesh = EngineControl.PlaneMesh.transform;
        Planelayer = PlaneMesh.gameObject.layer;
    }
    private void Interact()
    {
        EngineControl.PasengerEnterPlaneLocal();
        if (LeaveButton != null) { LeaveButton.SetActive(true); }
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(true); }
        EngineControl.localPlayer.UseAttachedStation();
    }
    public override void OnStationEntered(VRCPlayerApi player)
    {
        //voice range change to allow talking inside cockpit (after VRC patch 1008)
        if (player != null)
        {
            LeaveButtonControl.SeatedPlayer = player.playerId;
            if (player.isLocal)
            {
                foreach (LeaveVehicleButton crew in EngineControl.LeaveButtons)
                {
                    VRCPlayerApi guy = VRCPlayerApi.GetPlayerById(crew.SeatedPlayer);
                    if (guy != null)
                    {
                        SetVoiceInside(guy);
                    }
                }
            }
            else if (EngineControl.Piloting || EngineControl.Passenger)
            {
                SetVoiceInside(player);
            }
        }
    }
    public override void OnStationExited(VRCPlayerApi player)
    {
        LeaveButtonControl.SeatedPlayer = -1;
        if (player != null)
        {
            SetVoiceOutside(player);
            if (player.isLocal)
            {
                //undo voice distances of all players inside the vehicle
                foreach (LeaveVehicleButton crew in EngineControl.LeaveButtons)
                {
                    VRCPlayerApi guy = VRCPlayerApi.GetPlayerById(crew.SeatedPlayer);
                    if (guy != null)
                    {
                        SetVoiceOutside(guy);
                    }
                }
                if (EngineControl != null)
                {
                    if (EngineControl.EffectsControl != null) { EngineControl.EffectsControl.PlaneAnimator.SetBool("localpassenger", false); }
                    EngineControl.Passenger = false;
                    EngineControl.localPlayer.SetVelocity(EngineControl.CurrentVel);
                    EngineControl.MissilesIncoming = 0;
                    EngineControl.EffectsControl.PlaneAnimator.SetInteger("missilesincoming", 0);
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
    }

    private void SetVoiceInside(VRCPlayerApi Player)
    {
        Player.SetVoiceDistanceNear(999999);
        Player.SetVoiceDistanceFar(1000000);
        Player.SetVoiceGain(.6f);
    }
    private void SetVoiceOutside(VRCPlayerApi Player)
    {
        Player.SetVoiceDistanceNear(0);
        Player.SetVoiceDistanceFar(25);
        Player.SetVoiceGain(15);
    }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogWarning("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}
