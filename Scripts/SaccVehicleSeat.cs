
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
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
        [Tooltip("How far to move the head to the side when looking backwards in desktop")]
        [SerializeField] float HeadXOffset = 0.25f;
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
        private bool CalibratedY = false;
        private bool CalibratedZ = false;
        private bool InSeat = false;
        private bool SeatOccupied = false;
        [System.NonSerializedAttribute] public int ThisStationID;
        private bool SeatInitialized = false;
        private bool InEditor = true;
        private VRCPlayerApi localPlayer;
        private VRCPlayerApi SeatedPlayer;
        private bool DoVoiceVolumeChange = true;
        [System.NonSerializedAttribute] public VRCStation Station;
        private Transform Seat;
        private Vector3 SeatPosTarget;
        private Quaternion SeatRotTarget;
        private Vector3 SeatStartPos;
        private Quaternion SeatStartRot;
        private int DT180SeatCalcCounter;
        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            if (localPlayer != null) { InEditor = false; }
            Station = (VRC.SDK3.Components.VRCStation)GetComponent(typeof(VRC.SDK3.Components.VRCStation));
            Seat = Station.stationEnterPlayerLocation;
            SeatStartRot = Seat.localRotation;
            SeatStartPos = Seat.localPosition;
            if (InEditor && ThisSeatOnly) { ThisSeatOnly.SetActive(true); }
            DT180SeatCalcCounter = Random.Range(0, 10);
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
                SeatOccupied = true;
                DoVoiceVolumeChange = EntityControl.DoVoiceVolumeChange;
                EntityControl.SeatedPlayers[ThisStationID] = player.playerId;
                SeatedPlayer = player;
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
                    if (!player.IsUserInVR()) { ThreeSixtySeat(); }
                }
                else
                {
                    if (EntityControl.InVehicle)
                    {
                        if (DoVoiceVolumeChange)
                        {
                            SetVoiceInside(player);
                        }
                    }
                    if (!player.IsUserInVR()) { ThreeSixtySeat(); }
                }
                if (IsPilotSeat) { EntityControl.PilotEnterVehicleGlobal(player); }
                else
                { EntityControl.PassengerEnterVehicleGlobal(); }
            }
        }
        public override void OnStationExited(VRCPlayerApi player)
        {
            if (!SeatInitialized) { InitializeSeat(); }
            PlayerExitPlane(player);
            Seat.localPosition = SeatPosTarget = SeatStartPos;
            Seat.localRotation = SeatRotTarget = SeatStartRot;
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
            SeatedPlayer = null;
            if (player != null)
            {
                SeatOccupied = false;
                DoVoiceVolumeChange = EntityControl.DoVoiceVolumeChange;
                if (IsPilotSeat) { EntityControl.PilotExitVehicle(player); }
                else { EntityControl.PassengerExitVehicleGlobal(); }
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
                            Seat.position -= TargetEyePosition.up * FindNearestPowerOf2Below(TargetRelative.y);
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
                        if (Mathf.Abs(TargetRelative.z) > 0.01f)
                        {
                            Seat.position -= TargetEyePosition.forward * FindNearestPowerOf2Below(TargetRelative.z);
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
                    AdjustTime += 0.3f;
                    RequestSerialization();
                    if (EntityControl.InVehicle && (!CalibratedY || !CalibratedZ))
                    {
                        SendCustomEventDelayedSeconds(nameof(SeatAdjustment), .3f, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
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
        //Thanks to iffn, absolute legend https://github.com/iffn/iffns360ChairForVRChat
        public void ThreeSixtySeat()
        {
            if (!SeatOccupied) { return; }
            Quaternion headRotation;
            if (InSeat)
            {
                //Rotation:
                headRotation = SeatedPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
                Quaternion relativeHeadRotation = Quaternion.Inverse(Seat.rotation) * headRotation;
                float headHeading = relativeHeadRotation.eulerAngles.y;
                //Debug.Log(headRotation.eulerAngles + " - " + transform.rotation.eulerAngles);
                Seat.localRotation = Quaternion.Euler(headHeading * Vector3.up);
                //Offset:
                float xOffset = 0;
                if (headHeading > 45 && headHeading < 180)
                {
                    xOffset = Remap(iMin: 45, iMax: 90, oMin: 0, oMax: HeadXOffset, iValue: headHeading);
                }
                else if (headHeading < 315 && headHeading > 180)
                {
                    xOffset = -Remap(iMin: 315, iMax: 270, oMin: 0, oMax: HeadXOffset, iValue: headHeading);
                }
                //Debug.Log($"{headHeading} -> {xOffset}");
                Seat.localPosition = new Vector3(SeatStartPos.x + xOffset, Seat.localPosition.y, Seat.localPosition.z);
                SendCustomEventDelayedFrames(nameof(ThreeSixtySeat), 1, VRC.Udon.Common.Enums.EventTiming.LateUpdate);//Tracking date is not ready in Update
            }
            else
            {
                if (DT180SeatCalcCounter > 10)//only calculate this stuff once every 10 frames for other players to save performance
                {
                    DT180SeatCalcCounter = 0;
                    headRotation = SeatedPlayer.GetBoneRotation(HumanBodyBones.Head);
                    Quaternion relativeHeadRotation = Quaternion.Inverse(Seat.rotation) * headRotation;
                    float headHeading = relativeHeadRotation.eulerAngles.y;
                    SeatRotTarget = Quaternion.Euler(headHeading * Vector3.up);
                    //Offset:
                    float xOffset = 0;
                    if (headHeading > 45 && headHeading < 180)
                    {
                        xOffset = Remap(iMin: 45, iMax: 90, oMin: 0, oMax: HeadXOffset, iValue: headHeading);
                    }
                    else if (headHeading < 315 && headHeading > 180)
                    {
                        xOffset = -Remap(iMin: 315, iMax: 270, oMin: 0, oMax: HeadXOffset, iValue: headHeading);
                    }
                    SeatPosTarget = new Vector3(SeatStartPos.x + xOffset, Seat.localPosition.y, Seat.localPosition.z);
                }
                DT180SeatCalcCounter++;

                Seat.localRotation = Quaternion.RotateTowards(Seat.localRotation, SeatRotTarget, 240f * Time.deltaTime);
                Seat.localPosition = Vector3.MoveTowards(Seat.localPosition, SeatPosTarget, Time.deltaTime);
                SendCustomEventDelayedFrames(nameof(ThreeSixtySeat), 1);
            }
        }
        public float Remap(float iMin, float iMax, float oMin, float oMax, float iValue)
        {
            float t = Mathf.InverseLerp(iMin, iMax, iValue);
            return Mathf.Lerp(oMin, oMax, t);
        }
    }
}