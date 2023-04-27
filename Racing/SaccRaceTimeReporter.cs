
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SaccRaceTimeReporter : UdonSharpBehaviour
    {
        public SaccRaceCourseAndScoreboard SB;
        [Header("Debug values:")]
        [UdonSynced, FieldChangeCallback(nameof(ReportedTime))] public float _ReportedTime;
        public float ReportedTime
        {
            set
            {
                _ReportedTime = value;
                if (Networking.LocalPlayer.IsOwner(SB.gameObject))
                { SendCustomEventDelayedFrames(nameof(DelayedCheck), 1); }
                if (Networking.LocalPlayer.IsOwner(gameObject))
                {
                    SendCustomEventDelayedFrames(nameof(SetUpCheck), 1);
                    if (!CheckingReception)
                    {
                        CheckingReception = true;
                        SendCustomEventDelayedSeconds(nameof(CheckIfRecordRecieved), 4);
                    }
                }
            }
            get => _ReportedTime;
        }
        public void DelayedCheck()
        {
            SB.NewRecord();
        }
        [UdonSynced] public string ReportedVehicle;
        [UdonSynced] public bool Reported_RaceReverse = false;
        [FieldChangeCallback(nameof(MyLastTime))] public float _MyLastTime = 0f;
        public float MyLastTime
        {
            set
            {
                _MyLastTime = value;
                SB.UpdateMyLastTime();
            }
            get => _MyLastTime;
        }
        [FieldChangeCallback(nameof(MyLastTime_R))] public float _MyLastTime_R = 0f;
        public float MyLastTime_R
        {
            set
            {
                _MyLastTime_R = value;
                SB.UpdateMyLastTime();
            }
            get => _MyLastTime_R;
        }
        private bool CheckingReception;
        private float Checking_time;
        private string Checking_vehicle;
        private bool Checking_reverse;
        public string MyLastVehicle = "None";
        public string MyLastVehicle_R = "None";
        public bool MyLastRace_Reverse = false;
        public void SetUpCheck()
        {
            Checking_reverse = MyLastRace_Reverse;
            if (MyLastRace_Reverse)
            {
                Checking_time = _MyLastTime_R;
                Checking_vehicle = MyLastVehicle;
            }
            else
            {
                Checking_time = MyLastTime;
                Checking_vehicle = MyLastVehicle_R;
            }
        }
        public void CheckIfRecordRecieved()
        {
            if (CheckingReception)
            {
                if (!Checking_reverse)
                {
                    int mypos = SB.CheckIfOnBoard(Networking.LocalPlayer.displayName, ref SB.PlayerNames);
                    if (mypos < 0)//i am not on the board
                    {
                        if (SB.PlayerTimes.Length > 0)
                        {
                            if (Checking_time < SB.PlayerTimes[SB.PlayerTimes.Length - 1] || SB.PlayerTimes.Length < SB.MaxRecordedTimes)
                            { SendMyTime(false); }
                            else { CheckingReception = false; return; }
                        }
                        else
                        { SendMyTime(false); }
                    }
                    else//i am on the board
                    {
                        if (Checking_time < SB.PlayerTimes[mypos])
                        { SendMyTime(false); }
                        else { CheckingReception = false; return; }
                    }
                }
                else
                {
                    int mypos = SB.CheckIfOnBoard(Networking.LocalPlayer.displayName, ref SB.PlayerNames_R);
                    if (mypos < 0)//i am not on the board
                    {
                        if (SB.PlayerTimes_R.Length > 0)
                        {
                            if (Checking_time < SB.PlayerTimes_R[SB.PlayerTimes_R.Length - 1] || SB.PlayerTimes_R.Length < SB.MaxRecordedTimes)
                            { SendMyTime(true); }
                            else { CheckingReception = false; return; }
                        }
                        else
                        { SendMyTime(true); }
                    }
                    else//i am on the board
                    {
                        if (Checking_time < SB.PlayerTimes_R[mypos])
                        { SendMyTime(true); }
                        else { CheckingReception = false; return; }
                    }
                }

                SendCustomEventDelayedSeconds(nameof(CheckIfRecordRecieved), 4);
            }
        }
        private void SendMyTime(bool reverse)
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            ReportedVehicle = Checking_vehicle;
            Reported_RaceReverse = Checking_reverse;
            ReportedTime = Checking_time;
            RequestSerialization();
        }
    }
}
