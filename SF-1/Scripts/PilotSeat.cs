
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PilotSeat : UdonSharpBehaviour
{
    public EngineController EngineControl;
    public GameObject LeaveButton;
    public GameObject Gun_pilot;
    public Transform PlaneMesh;
    public GameObject SeatAdjuster;
    private void Start()
    {
        Assert(EngineControl != null, "Start: EngineControl != null");
        Assert(LeaveButton != null, "Start: LeaveButton != null");
        Assert(Gun_pilot != null, "Start: Gun_pilot != null");
        Assert(PlaneMesh != null, "Start: PlaneMesh != null");
        Assert(SeatAdjuster != null, "Start: SeatAdjuster != null");
    }
    private void Interact()//entering the plane
    {
        if (EngineControl.VehicleMainObj != null) { Networking.SetOwner(EngineControl.localPlayer, EngineControl.VehicleMainObj); }
        if (LeaveButton != null) { LeaveButton.SetActive(true); }
        if (EngineControl != null)
        {
            Networking.SetOwner(EngineControl.localPlayer, EngineControl.gameObject);
            EngineControl.Piloting = true;
            if (EngineControl.EffectsControl.CanopyOpen) EngineControl.CanopyCloseTimer = -100001;//has to be less than -100000
            else EngineControl.CanopyCloseTimer = -1;//less than 0
            if (EngineControl.dead) EngineControl.Health = 100;//dead is true for the first 5 seconds after spawn, this might help with spontaneous explosions
        }
        if (EngineControl.EffectsControl != null)
        {
            EngineControl.IsFiringGun = false;
            EngineControl.EffectsControl.Smoking = false;
            Networking.SetOwner(EngineControl.localPlayer, EngineControl.EffectsControl.gameObject);
            EngineControl.LGripLastFrame = false; //prevent instant flares drop on enter
        }
        if (EngineControl.HUDControl != null)
        {
            Networking.SetOwner(EngineControl.localPlayer, EngineControl.HUDControl.gameObject);
            EngineControl.HUDControl.gameObject.SetActive(true);
        }
        if (Gun_pilot != null) { Gun_pilot.SetActive(true); }
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(true); }
        if (EngineControl.localPlayer != null) { EngineControl.localPlayer.UseAttachedStation(); }
        if (EngineControl.EffectsControl != null || EngineControl.SoundControl != null) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "WakeUp"); }
        if (PlaneMesh != null)
        {
            Transform[] children = PlaneMesh.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                child.gameObject.layer = 19;
            }
        }
    }
    public void PilotLeave()
    {
        if (EngineControl != null)
        {
            EngineControl.Piloting = false;
            if (EngineControl.Ejected)
            {
                EngineControl.localPlayer.SetVelocity(EngineControl.CurrentVel + EngineControl.VehicleMainObj.transform.up * 25);
                EngineControl.Ejected = false;
            }
            else EngineControl.localPlayer.SetVelocity(EngineControl.CurrentVel);
            EngineControl.EjectTimer = 2;
            EngineControl.Hooked = -1;
            EngineControl.AirBrakeInput = 0;
            EngineControl.LTriggerTapTime = 1;
            EngineControl.RTriggerTapTime = 1;
            EngineControl.Taxiinglerper = 0;
            EngineControl.PlayerThrottle = 0;
            EngineControl.LGripLastFrame = false;
            EngineControl.RGripLastFrame = false;
            EngineControl.LStickSelection = 0;
            EngineControl.RStickSelection = 0;
            EngineControl.AirBrakeInput = 0;
            EngineControl.LTriggerLastFrame = false;
            EngineControl.RTriggerLastFrame = false;
            EngineControl.HUDControl.MenuSoundCheckLast = 0;
            EngineControl.AGMLocked = false;
            EngineControl.AAMHasTarget = false;
            EngineControl.AAMLocked = false;
            EngineControl.TargetingMe = 0;
            EngineControl.MissilesIncoming = 0;
            if (EngineControl.CatapultStatus == 2) { }//keep launching if launching
            else EngineControl.CatapultStatus = 0;//else unhook from catapult
        }
        if (LeaveButton != null) { LeaveButton.SetActive(false); }
        if (EngineControl.EffectsControl != null)
        {
            EngineControl.IsFiringGun = false;
            EngineControl.EffectsControl.Smoking = false;
        }
        if (Gun_pilot != null) { Gun_pilot.SetActive(false); }
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(false); }
        if (EngineControl.HUDControl != null) { EngineControl.HUDControl.gameObject.SetActive(false); }
        //set plane's layer back
        if (PlaneMesh != null)
        {
            Transform[] children = PlaneMesh.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                child.gameObject.layer = 17;
            }
        }
    }
    public void WakeUp()
    {
        if (EngineControl.EffectsControl != null) { EngineControl.EffectsControl.DoEffects = 0f; }
        if (EngineControl.SoundControl != null) { EngineControl.SoundControl.DoSound = 0f; }
    }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogError("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}
