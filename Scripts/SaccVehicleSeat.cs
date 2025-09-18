
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.UdonNetworkCalling;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SaccVehicleSeat : UdonSharpBehaviour
    {
        public SaccEntity EntityControl;
        [Tooltip("Gameobject with script that runs when you enter the seat to edjust your view position")]
        public bool IsPilotSeat = false;
        [Tooltip("Optional: Use to set up passenger seat with its own function dials")]
        public SAV_PassengerFunctionsController PassengerFunctions;
        [Tooltip("Objects that are enabled only when sitting in this seat")]
        public GameObject[] EnableInSeat;
        [Tooltip("Objects that are disabled only when sitting in this seat")]
        public GameObject[] DisableInSeat;
        public bool AdjustSeatPosition = true;
        public Transform TargetEyePosition;
        public bool AdjustSeatRotation = true; //YAWCALIBRATION
        [Tooltip("Calbrate rotation towards this transform's forward vector, leave empty to use this station's transform")]
        public Transform RotationCalibrationTarget; //YAWCALIBRATION
        [Tooltip("Let other scripts know that this seat is on the outside of the vehicle (stop sound changing when closing canopy)")]
        [SerializeField] private bool SeatOutSideVehicle;
        [Tooltip("How far to move the seat to the side when looking backwards in desktop")]
        [SerializeField] float HeadXOffset = 0.25f;
        [Tooltip("Disable the ability for desktop users to turn 180 degrees")]
        public bool Disable180Rotation = false;
        [SerializeField] Animator AnimBoolAnimator;
        [Tooltip("Boolean to set to true on the above animator when a player is sitting in this seat")]
        [SerializeField] string AnimBoolOnEnter;
        private Vector3 SeatAdjustedPosXY;
        float AdjustedRot;
        private Vector3 MyFinalSeatPose;
        private float AdjustTime;
        private bool CalibratedY = false;
        private bool CalibratedZ = false;
        private bool CalibratedYaw = false;//YAWCALIBRATION
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
        [System.NonSerialized] public int numOpenDoors;
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
            SeatAdjustedPosXY = SeatStartPos = Seat.localPosition;
            DT180SeatCalcCounter = Random.Range(0, 10);
            for (int i = 0; i < EntityControl.ExternalSeats.Length; i++)
            {
                if (Station == EntityControl.ExternalSeats[i])
                { ThisSeatExternal = true; break; }
            }
            if (InEditor)
            {
                for (int i = 0; i < EnableInSeat.Length; i++)
                { if (EnableInSeat[i]) EnableInSeat[i].SetActive(true); }
                for (int i = 0; i < DisableInSeat.Length; i++)
                { if (DisableInSeat[i]) DisableInSeat[i].SetActive(false); }
            }
            else
            {
                for (int i = 0; i < EnableInSeat.Length; i++)
                { if (EnableInSeat[i]) EnableInSeat[i].SetActive(false); }
                for (int i = 0; i < DisableInSeat.Length; i++)
                { if (DisableInSeat[i]) DisableInSeat[i].SetActive(true); }
            }
            if (PassengerFunctions) { PassengerFunctions.Station = Station; }
            if (SeatOutSideVehicle) { numOpenDoors++; }
        }
        public override void Interact()//entering the vehicle
        {
            if (!InEditor)
            {
                Seat.rotation = Quaternion.Euler(0, Seat.eulerAngles.y, 0);//fixes offset seated position when getting in a rolled/pitched vehicle in VR
                localPlayer.UseAttachedStation();
                Seat.localRotation = SeatStartRot;
                AdjustedRot = Seat.localEulerAngles.y;//YAWCALIBRATION
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
                    if (PassengerFunctions)
                    { PassengerFunctions.UserEnterVehicleLocal(); }
                    if (IsPilotSeat)
                    { EntityControl.PilotEnterVehicleLocal(); }
                    else
                    {
                        if (PassengerFunctions)
                        { PassengerFunctions.passengerFuncIgnorePassengerFlag = true; }
                        EntityControl.PassengerEnterVehicleLocal();
                    }
                    for (int i = 0; i < EnableInSeat.Length; i++)
                    { if (EnableInSeat[i]) EnableInSeat[i].SetActive(true); }
                    for (int i = 0; i < DisableInSeat.Length; i++)
                    { if (DisableInSeat[i]) DisableInSeat[i].SetActive(false); }

                    if (!Fake && AdjustSeatPosition && TargetEyePosition)
                    {
                        CalibratedY = false;
                        CalibratedZ = false;
                        //don't do rotation calibration if in desktop mode
                        if (AdjustSeatRotation && player.IsUserInVR()) { CalibratedYaw = false; } else { CalibratedYaw = true; }//YAWCALIBRATION
                        AdjustTime = 0;
                        SeatAdjustment();
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
                if (PassengerFunctions)
                { PassengerFunctions.UserEnterVehicleGlobal(player); }
                if (IsPilotSeat) { EntityControl.PilotEnterVehicleGlobal(player); }
                else
                {
                    if (PassengerFunctions)
                    { PassengerFunctions.passengerFuncIgnorePassengerFlag = true; }
                    EntityControl.PassengerEnterVehicleGlobal(player);
                }
                if (AnimBoolAnimator) { AnimBoolAnimator.SetBool(AnimBoolOnEnter, true); }
            }
        }
        public override void OnStationExited(VRCPlayerApi player)
        {
            if (!SeatInitialized) { InitializeSeat(); }
            if (player != null)
            {
                PlayerExitPlane(player);
                if (!Fake)
                {
                    Seat.localPosition = SeatAdjustedPosXY = SeatPosTarget = SeatStartPos;
                    Seat.localRotation = SeatRotTarget = SeatStartRot;
                    AdjustedRot = Seat.localEulerAngles.y;//YAWCALIBRATION
                }
                if (AnimBoolAnimator) { AnimBoolAnimator.SetBool(AnimBoolOnEnter, false); }
            }
        }
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (!SeatInitialized) { InitializeSeat(); }
            if (Utilities.IsValid(player) && player.playerId == EntityControl.SeatedPlayers[ThisStationID])
            {
                if (PassengerFunctions)
                    PassengerFunctions.pilotLeftFlag = true;
                else if (IsPilotSeat)
                    EntityControl.pilotLeftFlag = true;
                PlayerExitPlane(player);
                SendCustomEventDelayedFrames(nameof(resetPilotLeftFlag), 1);
            }
        }
        public void resetPilotLeftFlag()
        {
            if (PassengerFunctions) PassengerFunctions.pilotLeftFlag = false;
            else EntityControl.pilotLeftFlag = false;
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
                if (DoVoiceVolumeChange)
                {
                    SetVoiceOutside(player);
                }
                if (player.isLocal)
                {
                    InSeat = false;
                    EntityControl.MySeat = -1;
                    if (PassengerFunctions)
                    { PassengerFunctions.UserExitVehicleLocal(); }
                    if (!IsPilotSeat)
                    {
                        if (PassengerFunctions)
                        { PassengerFunctions.passengerFuncIgnorePassengerFlag = true; }
                        EntityControl.PassengerExitVehicleLocal();
                    }
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
                    for (int i = 0; i < EnableInSeat.Length; i++)
                    { if (EnableInSeat[i]) EnableInSeat[i].SetActive(false); }
                    for (int i = 0; i < DisableInSeat.Length; i++)
                    { if (DisableInSeat[i]) DisableInSeat[i].SetActive(true); }
                }
                if (IsPilotSeat) { EntityControl.PilotExitVehicle(player); }
                else
                {
                    if (PassengerFunctions)
                    { PassengerFunctions.passengerFuncIgnorePassengerFlag = true; }
                    EntityControl.PassengerExitVehicleGlobal(player);
                }
                if (PassengerFunctions)
                { PassengerFunctions.UserExitVehicleGlobal(); }
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
            if (!RotationCalibrationTarget) { RotationCalibrationTarget = transform; }//YAWCALIBRATION
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
                            SeatAdjustedPosXY -= Seat.InverseTransformDirection((TargetEyePosition.up / TargetEyePosition.lossyScale.y) * FindNearestPowerOf2Below(TargetRelative.y));//YAW CALIBRATION use transform instead of Seat (breaks AAGun)
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
                            SeatAdjustedPosXY -= Seat.InverseTransformDirection((TargetEyePosition.forward / TargetEyePosition.lossyScale.z) * FindNearestPowerOf2Below(TargetRelative.z));//YAW CALIBRATION use transform instead of Seat (breaks AAGun)
                        }
                        else
                        {
                            if (AdjustTime > 1f)
                            {
                                CalibratedZ = true;
                            }
                        }
                    }

                    Vector3 HeadForward = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward; //YAWCALIBRATION
                    float angle = Vector3.SignedAngle(RotationCalibrationTarget.forward, Vector3.ProjectOnPlane(HeadForward, transform.up), transform.up);
                    Vector3 seatrot = Seat.localEulerAngles;
                    if (!CalibratedYaw)
                    {
                        if (Mathf.Abs(angle) > 1f)
                        {
                            if (seatrot.y > 180) { seatrot.y -= 360; }
                            AdjustedRot = seatrot.y - (angle * .5f);
                        }
                        else
                        {
                            if (AdjustTime > 1f)
                            {
                                CalibratedYaw = true;
                            }
                        }
                    }
                    if (CalibratedY && CalibratedZ && CalibratedYaw)//YAWCALIBRATION
                    {
                        return;
                    }
                    MyFinalSeatPose = new Vector3(SeatAdjustedPosXY.y, SeatAdjustedPosXY.z, AdjustedRot);//YAWCALIBRATION
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(UpdateSeatPosition), MyFinalSeatPose);
                    AdjustTime += 0.3f;
                    SendCustomEventDelayedSeconds(nameof(SeatAdjustment), .3f, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
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
        [NetworkCallable]
        public void UpdateSeatPosition(Vector3 inputPose)
        {
            if (Seat)
            {
                Vector3 newpos = (new Vector3(SeatStartPos.x, inputPose.x, inputPose.y));
                Seat.localPosition = newpos;
                if (SeatedPlayer != null && SeatedPlayer.IsUserInVR())//YAWCALIBRATION
                {
                    Vector3 seatrot = Seat.localEulerAngles;
                    Quaternion newrot = Quaternion.Euler(new Vector3(seatrot.x, inputPose.z, seatrot.z));
                    Seat.localRotation = newrot;
                }
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
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (InSeat)
            {
                if (!CalibratedY || !CalibratedZ || !CalibratedYaw)//YAWCALIBRATION
                {
                    return;
                }
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(UpdateSeatPosition), MyFinalSeatPose);
            }
        }
    }
}