
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PilotSeat : UdonSharpBehaviour
{
    public GameObject VehicleMainObj;
    public EngineController EngineControl;
    public GameObject LeaveButton;
    public GameObject Saccflight;
    public GameObject Gun_pilot;
    public Transform PlaneMesh;
    public GameObject SeatAdjuster;
    public GameObject EnableOther;
    private void Interact()
    {
        if (Saccflight != null) { Saccflight.SetActive(false); }
        if (VehicleMainObj != null) { Networking.SetOwner(EngineControl.localPlayer, VehicleMainObj); }
        if (EngineControl != null)
        {
            Networking.SetOwner(EngineControl.localPlayer, EngineControl.gameObject);
            EngineControl.Piloting = true;
        }
        if (EngineControl.EffectsControl != null)
        {
            EngineControl.EffectsControl.isfiring = false;
            EngineControl.EffectsControl.Smoking = false;
            Networking.SetOwner(EngineControl.localPlayer, EngineControl.EffectsControl.gameObject);
            EngineControl.EffectsControl.RGriplastframetrigger = true; //prevent instant flares drop on enter
        }
        if (EngineControl.HUDControl != null) { Networking.SetOwner(EngineControl.localPlayer, EngineControl.HUDControl.gameObject); }
        if (LeaveButton != null) { LeaveButton.SetActive(true); }
        if (Gun_pilot != null) { Gun_pilot.SetActive(true); }
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(true); }
        if (EnableOther != null) { EnableOther.SetActive(true); }
        if (EngineControl.HUDControl != null) { EngineControl.HUDControl.gameObject.SetActive(true); }
        if (EngineControl.localPlayer != null) { EngineControl.localPlayer.UseAttachedStation(); }
        if (EngineControl.EffectsControl != null || EngineControl.SoundControl != null) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "WakeUp"); }
        //set plane to a layer that doesn't collide with its own bullets
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
        if (EngineControl != null) { EngineControl.Piloting = false; EngineControl.localPlayer.SetVelocity(EngineControl.CurrentVel); }
        if (Saccflight != null) { Saccflight.SetActive(true); }
        if (LeaveButton != null) { LeaveButton.SetActive(false); }
        if (EngineControl.EffectsControl != null)
        {
            EngineControl.EffectsControl.isfiring = false;
            EngineControl.EffectsControl.Smoking = false;
        }
        if (Gun_pilot != null) { Gun_pilot.SetActive(false); }
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(false); }
        if (EnableOther != null) { EnableOther.SetActive(false); }
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
        if (EngineControl.SoundControl != null) { EngineControl.SoundControl.soundsoff = false; }
        if (EngineControl.SoundControl != null) { EngineControl.SoundControl.PlaneIdle.gameObject.SetActive(true); }
        if (EngineControl.SoundControl != null) { EngineControl.SoundControl.PlaneDistant.gameObject.SetActive(true); }
        if (EngineControl.SoundControl != null) { EngineControl.SoundControl.PlaneAfterburner.gameObject.SetActive(true); }
        if (EngineControl.SoundControl != null) { EngineControl.SoundControl.PlaneWind.gameObject.SetActive(true); }
        if (EngineControl.SoundControl != null) { EngineControl.SoundControl.PlaneInside.gameObject.SetActive(true); }
    }
}
