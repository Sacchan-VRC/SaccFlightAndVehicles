
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
        [Header("Removing ThisSeatOnly next version. EnableInSeat replaces it.")]
        [Tooltip("Object that is enabled only when sitting in this seat")]
        public GameObject ThisSeatOnly;
        [Tooltip("Objects that are enabled only when sitting in this seat")]
        public GameObject[] EnableInSeat;
        public bool AdjustSeatPosition = true;
        // public bool AdjustSeatRotation = true; //YAWCALIBRATION
        public Transform TargetEyePosition;
        [Tooltip("Let other scripts know that this seat is on the outside of the vehicle (stop sound changing when closing canopy)")]
        public bool SeatOutSideVehicle;
        [Tooltip("How far to move the seat to the side when looking backwards in desktop")]
        [SerializeField] float HeadXOffset = 0.25f;
        [Tooltip("Disable the ability for desktop users to turn 180 degrees")]
        public bool Disable180Rotation = false;
        // [Tooltip("Calbrate rotation towards this transform's forward vector, leave empty to use this station's transform")]
        // public Transform RotationCalibrationTarget; //YAWCALIBRATION
        private Vector3 SeatAdjustedPos;
        [UdonSynced, FieldChangeCallback(nameof(AdjustedPos))] private Vector2 _adjustedPos;// xy = seat up and forward
        // Disabled Yaw calibration, each commented bit of code is labeled with //YAWCALIBRATION
        // Disabled because it's +1 synced float for every seat, and makes vehicle seat setup more complex
        // Yaw calibration is incomplete, it needs another transform reference to make the AAgun's seat work properly
        // [UdonSynced, FieldChangeCallback(nameof(AdjustedPos))] private Vector3 _adjustedPos;// xy = seat up and forward, z = yaw
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
        // private bool CalibratedYaw = false;//YAWCALIBRATION
        private bool InSeat = false;
        [System.NonSerializedAttribute] public bool SeatOccupied = false;
        [System.NonSerializedAttribute] public int ThisStationID;
        private bool SeatInitialized = false;
        private bool InEditor = true;
        private VRCPlayerApi localPlayer;
        [System.NonSerializedAttribute] public VRCPlayerApi SeatedPlayer;
        private bool DoVoiceVolumeChange = true;
        [System.NonSerializedAttribute] public VRCStation Station;
        [System.NonSerializedAttribute] public bool Fake;//'Fake' exit from seat disables stuf flike seat adjuster, used for pilot swapping
        private Transform Seat;
        private Vector3 SeatPosTarget;
        private Quaternion SeatRotTarget;
        private Vector3 SeatStartPos;
        private Quaternion SeatStartRot;
        private int DT180SeatCalcCounter;
        private bool ThisSeatExternal = false;
        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            if (localPlayer != null) { InEditor = false; }
            Station = (VRC.SDK3.Components.VRCStation)GetComponent(typeof(VRC.SDK3.Components.VRCStation));
            Seat = Station.stationEnterPlayerLocation;
            SeatStartRot = Seat.localRotation;
            SeatAdjustedPos = SeatStartPos = Seat.localPosition;
            if (InEditor)
            {
                if (ThisSeatOnly) { ThisSeatOnly.SetActive(true); }
                for (int i = 0; i < EnableInSeat.Length; i++)
                { if (EnableInSeat[i]) EnableInSeat[i].SetActive(true); }
            }
            DT180SeatCalcCounter = Random.Range(0, 10);
            for (int i = 0; i < EntityControl.ExternalSeats.Length; i++)
            {
                if (Station == EntityControl.ExternalSeats[i])
                { ThisSeatExternal = true; break; }
            }
        }
        public override void Interact()//entering the vehicle
        {
            if (!InEditor)
            {
                Seat.rotation = Quaternion.Euler(0, Seat.eulerAngles.y, 0);//fixes offset seated position when getting in a rolled/pitched vehicle in VR
                localPlayer.UseAttachedStation();
                Seat.localRotation = SeatStartRot;
                // _adjustedPos.z = Seat.localEulerAngles.y;//YAWCALIBRATION
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
                    EntityControl.MySeatIsExternal = ThisSeatExternal;
                    InSeat = true;
                    if (!localPlayer.IsOwner(gameObject))
                    { Networking.SetOwner(localPlayer, gameObject); }
                    EntityControl.MySeat = ThisStationID;
                    if (IsPilotSeat)
                    { EntityControl.PilotEnterVehicleLocal(); }
                    else
                    { EntityControl.PassengerEnterVehicleLocal(); }
                    if (ThisSeatOnly) { ThisSeatOnly.SetActive(true); }
                    for (int i = 0; i < EnableInSeat.Length; i++)
                    { if (EnableInSeat[i]) EnableInSeat[i].SetActive(true); }

                    if (!Fake && AdjustSeatPosition && TargetEyePosition)
                    {
                        CalibratedY = false;
                        CalibratedZ = false;
                        //don't do rotation calibration if in desktop mode
                        // if (AdjustSeatRotation && player.IsUserInVR()) { CalibratedYaw = false; } else { CalibratedYaw = true; }//YAWCALIBRATION
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
                else
                {
                    if (EntityControl.InVehicle)
                    {
                        if (DoVoiceVolumeChange)
                        {
                            SetVoiceInside(player);
                        }
                    }
                }
                if (!player.IsUserInVR() && !Fake && !Disable180Rotation) { ThreeSixtySeat(); }
                if (IsPilotSeat) { EntityControl.PilotEnterVehicleGlobal(player); }
                else
                { EntityControl.PassengerEnterVehicleGlobal(); }
            }
        }
        public override void OnStationExited(VRCPlayerApi player)
        {
            if (!SeatInitialized) { InitializeSeat(); }
            PlayerExitPlane(player);
            if (!Fake)
            {
                Seat.localPosition = SeatAdjustedPos = SeatPosTarget = SeatStartPos;
                Seat.localRotation = SeatRotTarget = SeatStartRot;
                // _adjustedPos.z = Seat.localEulerAngles.y;//YAWCALIBRATION
            }
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
                    for (int i = 0; i < EnableInSeat.Length; i++)
                    { if (EnableInSeat[i]) EnableInSeat[i].SetActive(false); }
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
        public void InitializeSeat()
        {
            if (!EntityControl.Initialized) { return; }
            // if (!RotationCalibrationTarget) { RotationCalibrationTarget = transform; }//YAWCALIBRATION
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
                    if (EntityControl.InVehicle && (!CalibratedY || !CalibratedZ/*  || !CalibratedYaw */))//YAWCALIBRATION
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
                            SeatAdjustedPos -= Seat.InverseTransformDirection(TargetEyePosition.up * FindNearestPowerOf2Below(TargetRelative.y));//YAW CALIBRATION use transform instead of Seat (breaks AAGun)
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
                            SeatAdjustedPos -= Seat.InverseTransformDirection(TargetEyePosition.forward * FindNearestPowerOf2Below(TargetRelative.z));//YAW CALIBRATION use transform instead of Seat (breaks AAGun)
                        }
                        else
                        {
                            if (AdjustTime > 1f)
                            {
                                CalibratedZ = true;
                            }
                        }
                    }

                    /* Vector3 HeadForward = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward; //YAWCALIBRATION
                    float angle = Vector3.SignedAngle(RotationCalibrationTarget.forward, Vector3.ProjectOnPlane(HeadForward, transform.up), transform.up);
                    Vector3 seatrot = Seat.localEulerAngles;
                    if (!CalibratedYaw)
                    {
                        if (Mathf.Abs(angle) > 1f)
                        {
                            if (seatrot.y > 180) { seatrot.y -= 360; }
                            _adjustedPos.z = seatrot.y - (angle * .5f);
                        }
                        else
                        {
                            if (AdjustTime > 1f)
                            {
                                CalibratedYaw = true;
                            }
                        }
                    } */
                    _adjustedPos = new Vector2(SeatAdjustedPos.y, SeatAdjustedPos.z/* , _adjustedPos.z */);//YAWCALIBRATION
                    AdjustedPos = _adjustedPos;
                    AdjustTime += 0.3f;
                    RequestSerialization();
                    if (EntityControl.InVehicle && (!CalibratedY || !CalibratedZ /* || !CalibratedYaw */))//YAWCALIBRATION
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
                /* if (SeatedPlayer != null && SeatedPlayer.IsUserInVR())
                {
                    Vector3 seatrot = Seat.localEulerAngles;
                    Quaternion newrot = Quaternion.Euler(new Vector3(seatrot.x, _adjustedPos.z, seatrot.z));
                    Seat.localRotation = newrot;
                } */
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