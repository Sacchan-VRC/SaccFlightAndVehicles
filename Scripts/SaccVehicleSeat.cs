
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class SaccVehicleSeat : UdonSharpBehaviour
{
    public SaccEntity EntityControl;
    [Tooltip("Gameobject with script that runs when you enter the seat to edjust your view position")]
    public bool IsPilotSeat = false;
    [Tooltip("Object that is enabled only when sitting in this seat")]
    public GameObject ThisSeatOnly;
    public bool AdjustSeat = true;
    public Transform TargetEyePosition;
    [UdonSynced, FieldChangeCallback(nameof(AdjustedPos))] private Vector2 _adjustedPos;
    public Vector2 AdjustedPos
    {
        set
        {
            _adjustedPos = value;
            SetRecievedSeatPosition();
        }
        get => _adjustedPos;
    }
    private float AdjustTime;
    private float LastSerTime;
    private bool CalibratedY = false;
    private bool CalibratedZ = false;
    private bool InSeat = false;
    private Vector3 SeatStartPos;
    [System.NonSerializedAttribute] public int ThisStationID;
    private bool SeatInitialized = false;
    private bool InEditor = true;
    private VRCPlayerApi localPlayer;
    private bool DoVoiceVolumeChange = true;
    [System.NonSerializedAttribute] public VRCStation Station;
    private Transform Seat;
    private Quaternion SeatStartRot;
    private void Start()
    {
        localPlayer = Networking.LocalPlayer;
        if (localPlayer != null) { InEditor = false; }
        Station = (VRC.SDK3.Components.VRCStation)GetComponent(typeof(VRC.SDK3.Components.VRCStation));
        Seat = Station.stationEnterPlayerLocation.transform;
        SeatStartRot = Seat.localRotation;
        SeatStartPos = Seat.localPosition;
        if (InEditor && ThisSeatOnly) { ThisSeatOnly.SetActive(true); }
    }
    public override void Interact()//entering the vehicle
    {
        if (!InEditor)
        {
            Seat.rotation = Quaternion.Euler(0, Seat.eulerAngles.y, 0);//fixes offset seated position when getting in a rolled/pitched vehicle in VR
            localPlayer.UseAttachedStation();
            Seat.localRotation = SeatStartRot;
        }
    }
    public override void OnStationEntered(VRCPlayerApi player)
    {
        if (!SeatInitialized) { InitializeSeat(); }//can't do this in start because EntityControl might not have initialized
        if (player != null)
        {
            DoVoiceVolumeChange = EntityControl.DoVoiceVolumeChange;
            EntityControl.SeatedPlayers[ThisStationID] = player.playerId;
            if (player.isLocal)
            {
                InSeat = true;
                if (!localPlayer.IsOwner(gameObject))
                { Networking.SetOwner(localPlayer, gameObject); }
                EntityControl.MySeat = ThisStationID;
                if (IsPilotSeat)
                { EntityControl.PilotEnterVehicleLocal(); }
                else
                { EntityControl.PassengerEnterVehicleLocal(); }
                if (ThisSeatOnly) { ThisSeatOnly.SetActive(true); }

                if (AdjustSeat && TargetEyePosition)
                {
                    CalibratedY = false;
                    CalibratedZ = false;
                    AdjustTime = 0;
                    SeatAdjustment();
                    SeatAdjustmentSerialization();
                }
                if (DoVoiceVolumeChange)
                {
                    foreach (int crew in EntityControl.SeatedPlayers)
                    {//get get a fresh VRCPlayerAPI every time to prevent players who left leaving a broken one behind and causing crashes
                        VRCPlayerApi guy = VRCPlayerApi.GetPlayerById(crew);
                        if (guy != null)
                        {
                            SetVoiceInside(guy);
                        }
                    }
                }
            }
            else if (EntityControl.InVehicle)
            {
                if (DoVoiceVolumeChange)
                {
                    SetVoiceInside(player);
                }
            }
            if (IsPilotSeat) { EntityControl.PilotEnterVehicleGlobal(player); }
        }
    }
    public override void OnStationExited(VRCPlayerApi player)
    {
        if (!SeatInitialized) { InitializeSeat(); }
        PlayerExitPlane(player);
        Seat.localPosition = SeatStartPos;
    }
    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (!SeatInitialized) { InitializeSeat(); }
        if (Utilities.IsValid(player) && player.playerId == EntityControl.SeatedPlayers[ThisStationID])
        {
            PlayerExitPlane(player);
        }
    }
    public void PlayerExitPlane(VRCPlayerApi player)
    {
        if (!SeatInitialized) { InitializeSeat(); }
        EntityControl.SeatedPlayers[ThisStationID] = -1;
        if (player != null)
        {
            DoVoiceVolumeChange = EntityControl.DoVoiceVolumeChange;
            if (IsPilotSeat) { EntityControl.PilotExitVehicle(player); }
            if (DoVoiceVolumeChange)
            {
                SetVoiceOutside(player);
            }
            if (player.isLocal)
            {
                InSeat = false;
                EntityControl.MySeat = -1;
                if (!IsPilotSeat)
                { EntityControl.PassengerExitVehicleLocal(); }
                if (DoVoiceVolumeChange)
                {
                    //undo voice distances of all players inside the vehicle
                    foreach (int crew in EntityControl.SeatedPlayers)
                    {
                        VRCPlayerApi guy = VRCPlayerApi.GetPlayerById(crew);
                        if (guy != null)
                        {
                            SetVoiceOutside(guy);
                        }
                    }
                }
                if (ThisSeatOnly) { ThisSeatOnly.SetActive(false); }
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
    private void InitializeSeat()
    {
        if (!EntityControl.Initialized) { return; }
        int x = 0;
        foreach (VRCStation station in EntityControl.VehicleStations)
        {
            if (station.gameObject == gameObject)
            {
                ThisStationID = x;
                if (IsPilotSeat) { EntityControl.PilotSeat = x; }
                break;
            }
            x++;
        }
        SeatInitialized = true;
    }

    //seat adjuster stuff
    public void SeatAdjustmentSerialization()
    {
        if (InSeat)
        {
            if (!InEditor)
            {
                RequestSerialization();
                if (EntityControl.InVehicle && (!CalibratedY || !CalibratedZ))
                {
                    SendCustomEventDelayedSeconds(nameof(SeatAdjustmentSerialization), .3f, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
                }
            }
        }
    }
    public void SeatAdjustment()
    {
        if (InSeat)
        {
            if (!InEditor)
            {
                //find head relative position ingame
                Vector3 TargetRelative = TargetEyePosition.InverseTransformDirection(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position - TargetEyePosition.position);
                if (!CalibratedY)
                {
                    if (Mathf.Abs(TargetRelative.y) > 0.01f)
                    {
                        Seat.position -= TargetEyePosition.up * FindNearestPowerOf2Below(TargetRelative.y) * Time.deltaTime * 3.3333333f;
                    }
                    else
                    {
                        if (AdjustTime > 1f)
                        {
                            CalibratedY = true;
                        }
                    }
                }
                if (!CalibratedZ)
                {
                    if (Mathf.Abs(TargetRelative.z) > 0.005f)
                    {
                        Seat.position -= TargetEyePosition.forward * FindNearestPowerOf2Below(TargetRelative.z) * Time.deltaTime * 3.3333333f;
                    }
                    else
                    {
                        if (AdjustTime > 1f)
                        {
                            CalibratedZ = true;
                        }
                    }
                }
                //remove floating point errors on x
                Vector3 seatpos = Seat.localPosition;
                seatpos.x = SeatStartPos.x;
                Seat.localPosition = seatpos;
                //set synced variable
                Vector3 newpos = Seat.localPosition;
                _adjustedPos.x = newpos.y;
                _adjustedPos.y = newpos.z;
                AdjustTime += Time.deltaTime;
                if (EntityControl.InVehicle && (!CalibratedY || !CalibratedZ))
                {
                    SendCustomEventDelayedFrames(nameof(SeatAdjustment), 1, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
                }
            }
        }
    }
    private float FindNearestPowerOf2Below(float target)
    {
        float targetAbs = Mathf.Abs(target);
        float x = .01f;
        while (x < targetAbs)
        { x *= 2; }
        x *= .5f;
        if (target > 0)
        { return x; }
        else
        { return -x; }
    }
    public void SetRecievedSeatPosition()
    {
        if (Seat)
        {
            Vector3 newpos = (new Vector3(SeatStartPos.x, _adjustedPos.x, _adjustedPos.y));
            Seat.localPosition = newpos;
        }
    }
}
