
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PilotSeat : UdonSharpBehaviour
{
    public EngineController EngineControl;
    public GameObject LeaveButton;
    public GameObject Gun_pilot;
    public GameObject SeatAdjuster;
    private LeaveVehicleButton LeaveButtonControl;
    private Transform PlaneMesh;
    private LayerMask Planelayer = 0;
    private void Start()
    {
        Assert(EngineControl != null, "Start: EngineControl != null");
        Assert(LeaveButton != null, "Start: LeaveButton != null");
        Assert(Gun_pilot != null, "Start: Gun_pilot != null");
        Assert(SeatAdjuster != null, "Start: SeatAdjuster != null");

        LeaveButtonControl = LeaveButton.GetComponent<LeaveVehicleButton>();

        PlaneMesh = EngineControl.PlaneMesh.transform;
        //get the layer of the plane as set by the world creator
        Planelayer = PlaneMesh.gameObject.layer;
    }
    private void Interact()//entering the plane
    {
        //setting this as a workaround because it doesnt work reliably in Enginecontroller's Start()
        if (EngineControl.localPlayer.IsUserInVR()) { EngineControl.InVR = true; }

        EngineControl.localPlayer.UseAttachedStation();
        Networking.SetOwner(EngineControl.localPlayer, EngineControl.gameObject);

        if (EngineControl.VehicleMainObj != null) { Networking.SetOwner(EngineControl.localPlayer, EngineControl.VehicleMainObj); }
        if (LeaveButton != null) { LeaveButton.SetActive(true); }

        EngineControl.Throttle = 0;
        EngineControl.ThrottleInput = 0;
        EngineControl.PlayerThrottle = 0;
        EngineControl.IsFiringGun = false;
        EngineControl.VehicleRigidbody.angularDrag = 0;//set to something nonzero when you're not owner to prevent juddering motion on collisions

        EngineControl.Piloting = true;
        if (EngineControl.dead) EngineControl.Health = 100;//dead is true for the first 5 seconds after spawn, this might help with spontaneous explosions

        if (EngineControl.EffectsControl != null)
        {
            //canopy closed/open sound
            if (EngineControl.EffectsControl.CanopyOpen) { EngineControl.CanopyCloseTimer = -100000 - EngineControl.CanopyCloseTime; }
            else EngineControl.CanopyCloseTimer = -EngineControl.CanopyCloseTime;//less than 0
            Networking.SetOwner(EngineControl.localPlayer, EngineControl.EffectsControl.gameObject);
            EngineControl.EffectsControl.Smoking = false;
        }
        if (EngineControl.HUDControl != null)
        {
            Networking.SetOwner(EngineControl.localPlayer, EngineControl.HUDControl.gameObject);
            EngineControl.HUDControl.gameObject.SetActive(true);
        }
        if (Gun_pilot != null) { Gun_pilot.SetActive(true); }
        if (SeatAdjuster != null) { SeatAdjuster.SetActive(true); }
        if (PlaneMesh != null)
        {
            Transform[] children = PlaneMesh.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                child.gameObject.layer = EngineControl.OnboardPlaneLayer;
            }
        }
        //hopefully prevents explosions when you enter the plane
        EngineControl.VehicleRigidbody.velocity = EngineControl.CurrentVel;
        EngineControl.Gs = 0;
        EngineControl.LastFrameVel = EngineControl.CurrentVel;


        if (EngineControl.EffectsControl != null) { EngineControl.EffectsControl.PlaneAnimator.SetBool("localpilot", true); }

        //Make sure EngineControl.AAMCurrentTargetEngineControl is correct
        var Target = EngineControl.AAMTargets[EngineControl.AAMTarget];
        if (Target && Target.transform.parent)
        {
            EngineControl.AAMCurrentTargetEngineControl = Target.transform.parent.GetComponent<EngineController>();
        }
    }
    public override void OnStationEntered(VRCPlayerApi player)
    {
        if (player != null)
        {
            EngineControl.PilotName = player.displayName;
            EngineControl.PilotID = player.playerId;

            //voice range change to allow talking inside cockpit (after VRC patch 1008)
            LeaveButtonControl.SeatedPlayer = player.playerId;
            if (player.isLocal)
            {
                foreach (LeaveVehicleButton crew in EngineControl.LeaveButtons)
                {//get get a fresh VRCPlayerAPI every time to prevent players who left leaving a broken one behind and causing crashes
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
        if (EngineControl.EffectsControl != null)
        {
            EngineControl.EffectsControl.PlaneAnimator.SetBool("occupied", true);
            EngineControl.EffectsControl.DoEffects = 0f;
        }
        EngineControl.dead = false;//Plane stops being invincible if someone gets in, also acts as redundancy incase someone missed the notdead respawn event
                                   //wakeup potentially sleeping controllers
        if (EngineControl.SoundControl != null) { EngineControl.SoundControl.Wakeup(); }
    }
    public override void OnStationExited(VRCPlayerApi player)
    {
        EngineControl.SetSmokingOff();
        EngineControl.SetAfterburnerOff();
        EngineControl.PilotName = string.Empty;
        EngineControl.PilotID = -1;
        EngineControl.IsFiringGun = false;
        LeaveButtonControl.SeatedPlayer = -1;
        if (EngineControl.EffectsControl != null)
        {
            EngineControl.EffectsControl.EffectsLeavePlane();
        }
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
                EngineControl.Piloting = false;
                if (EngineControl.Ejected)
                {
                    EngineControl.localPlayer.SetVelocity(EngineControl.CurrentVel + EngineControl.VehicleMainObj.transform.up * 25);
                    EngineControl.Ejected = false;
                }
                else { EngineControl.localPlayer.SetVelocity(EngineControl.CurrentVel); }
                EngineControl.EjectTimer = 2;
                EngineControl.Hooked = false;
                EngineControl.BrakeInput = 0;
                EngineControl.LTriggerTapTime = 1;
                EngineControl.RTriggerTapTime = 1;
                EngineControl.Taxiinglerper = 0;
                EngineControl.LGripLastFrame = false;
                EngineControl.RGripLastFrame = false;
                EngineControl.LStickSelection = 0;
                EngineControl.RStickSelection = 0;
                EngineControl.BrakeInput = 0;
                EngineControl.LTriggerLastFrame = false;
                EngineControl.RTriggerLastFrame = false;
                EngineControl.HUDControl.MenuSoundCheckLast = 0;
                EngineControl.AGMLocked = false;
                EngineControl.AAMHasTarget = false;
                EngineControl.DoAAMTargeting = false;
                EngineControl.MissilesIncoming = 0;
                EngineControl.AAMLockTimer = 0;
                EngineControl.AAMLocked = false;
                EngineControl.ZeroControlValues();
                if (EngineControl.CatapultStatus == 1) { EngineControl.CatapultStatus = 0; }//keep launching if launching, otherwise unlock from catapult

                if (LeaveButton != null) { LeaveButton.SetActive(false); }
                if (Gun_pilot != null) { Gun_pilot.SetActive(false); }
                if (SeatAdjuster != null) { SeatAdjuster.SetActive(false); }
                if (EngineControl.HUDControl != null) { EngineControl.HUDControl.gameObject.SetActive(false); }
                //set plane's layer back
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
            Debug.LogError("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}
