
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SaccRaceCourseAndScoreboard : UdonSharpBehaviour
    {
        [Tooltip("Used by other scripts to get the races name.")]
        public string RaceName;
        public SaccRaceTimeReporter TimeReporter;
        [Tooltip("All checkpoint objects for this race in order, animations are sent to them as they are passed")]
        public GameObject[] RaceCheckpoints;
        [Tooltip("Laps mode. To finish the race you must enter the first checkpoint again, which starts the race again")]
        public bool LoopRace;
        [Tooltip("Parent of all objects related to this race, including scoreboard and checkpoints")]
        public GameObject RaceObjects;
        public bool AllowReverse = true;
        public AudioSource NewTimeAdded_Snd;
        public AudioSource NewTopRecord_Snd;
        public string LineHeightTxt = "<line-height=.286>";
        [Tooltip("If RaceToggleButton.AutomaticRaceSelection is enabled, enable race when you are within this distance of the beginning")]
        public float Autotoggler_EnableDist_Forward = 100;
        [Tooltip("If RaceToggleButton.AutomaticRaceSelection is enabled, enable race in reverse mode when you are within this distance of the End")]
        public float Autotoggler_EnableDist_Reverse = 100;
        public GameObject[] StartPointFX;
        public GameObject[] EndPointFX;
        [System.NonSerializedAttribute] public float[] SplitTimes;
        [System.NonSerializedAttribute] public float[] SplitTimes_R;
        [System.NonSerializedAttribute] public string MyLastTime = string.Empty;
        [System.NonSerializedAttribute] public string MyLastTime_R = string.Empty;
        public void UpdateMyLastTime()
        {
            if (!MyLastTime_text) { return; }
            if (TimeReporter._MyLastTime != 0f)
            {
                MyLastTime_text.text = MyLastTime = "My Last Time : " + SecsToMinsSec(TimeReporter._MyLastTime) + " In: " + TimeReporter.MyLastVehicle.ToString();
            }
            else { MyLastTime_text.text = string.Empty; }
            if (!MyLastTime_R_text) { return; }
            if (AllowReverse && TimeReporter._MyLastTime_R != 0f)
            {
                MyLastTime_R_text.text = MyLastTime_R = "My Last Time : " + SecsToMinsSec(TimeReporter._MyLastTime_R) + " In: " + TimeReporter.MyLastVehicle_R.ToString();
            }
            else { MyLastTime_R_text.text = string.Empty; }
        }
        [System.NonSerialized] public float MyBestTime;
        [System.NonSerialized] public float MyBestTime_R;
        [Header("Scoreboard:")]
        [Tooltip("Record the top MaxRecordedTimes number of records, forget about the rest")]
        public int MaxRecordedTimes = 15;
        public TextMeshProUGUI Names_text;
        public TextMeshProUGUI Times_text;
        public TextMeshProUGUI Vehicles_text;
        public TextMeshProUGUI MyLastTime_text;
        public TextMeshProUGUI Names_R_text;
        public TextMeshProUGUI Times_R_text;
        public TextMeshProUGUI Vehicles_R_text;
        public TextMeshProUGUI MyLastTime_R_text;
        [Header("Debug:")]
        [UdonSynced] public float[] PlayerTimes;
        [UdonSynced] public string[] PlayerVehicles;
        [UdonSynced] public string[] PlayerNames;
        [UdonSynced] public float[] PlayerTimes_R;
        [UdonSynced] public string[] PlayerVehicles_R;
        [UdonSynced] public string[] PlayerNames_R;
        [System.NonSerialized] public bool RaceInProgress;
        private void Start()
        {
            UpdateMyLastTime();
            UpdateScoreBoards_Vis();
            SendCustomEventDelayedSeconds(nameof(UpdateScoreBoards_Vis), 15);
            SplitTimes = new float[RaceCheckpoints.Length];
            SplitTimes_R = new float[RaceCheckpoints.Length];
        }
        public void AddNewPlayerToBoard(string playername, float time, string vehicle, ref string[] playernames, ref float[] playertimes, ref string[] playervehicles)
        {
            if (playertimes.Length > 0)
            {
                if (time < playertimes[0])
                {
                    if (NewTopRecord_Snd)
                    { { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(PlayNewRecordSound)); } }
                }
                else
                {
                    if (NewTimeAdded_Snd)
                    { { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(PlayNewTimeSound)); } }
                }
            }
            else
            {
                if (NewTopRecord_Snd)
                { { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(PlayNewRecordSound)); } }
            }

            if (playertimes.Length < MaxRecordedTimes)
            {
                int len = playertimes.Length;
                int onemore = len + 1;
                string[] pn2 = new string[onemore];
                playernames.CopyTo(pn2, 0);
                float[] pt2 = new float[onemore];
                playertimes.CopyTo(pt2, 0);
                string[] pv2 = new string[onemore];
                playervehicles.CopyTo(pv2, 0);
                playernames = pn2;
                playertimes = pt2;
                playervehicles = pv2;
                playernames[len] = playername;
                playertimes[len] = time;
                playervehicles[len] = vehicle;
                //+1 to each array
                //set new values to new players record
                SortScoreboard(ref playernames, ref playertimes, ref playervehicles);
            }
            else
            {
                //replace slowest time
                int last = playertimes.Length - 1;
                playertimes[last] = time;
                playervehicles[last] = vehicle;
                playernames[last] = playername;
                SortScoreboard(ref PlayerNames, ref playertimes, ref playervehicles);
            }
        }
        public void PlayNewTimeSound()
        {
            if (NewTimeAdded_Snd) { NewTimeAdded_Snd.Play(); }
        }
        public void PlayNewRecordSound()
        {
            if (NewTopRecord_Snd) { NewTopRecord_Snd.Play(); }
        }
        public int CheckIfOnBoard(string playername, ref string[] playernames)
        {
            for (int i = 0; i < playernames.Length; i++)
            {
                if (playername == playernames[i])
                { return i; }
            }
            return -1;
        }
        // public bool SortAsc = true;
        public void SortScoreboard(ref string[] playernames, ref float[] playertimes, ref string[] playervehicles)//currently sorts backwards
        {
            int length = playertimes.Length;
            for (int i = 1; i < length; i++)
            {
                var keynames = playernames[i];
                var keytimes = playertimes[i];
                var keyvehicles = playervehicles[i];
                var flag = false;
                // if (SortAsc)
                // {
                for (int j = i - 1; j >= 0 && flag != true;)
                {
                    if (keytimes < playertimes[j])
                    {
                        int jp1 = j + 1;
                        playernames[jp1] = playernames[j];
                        playertimes[jp1] = playertimes[j];
                        playervehicles[jp1] = playervehicles[j];
                        j--; jp1--;
                        playernames[jp1] = keynames;
                        playertimes[jp1] = keytimes;
                        playervehicles[jp1] = keyvehicles;
                    }
                    else flag = true;
                }
                // }
                // else
                // {
                //     for (int j = i - 1; j >= 0 && flag != true;)
                //     {
                //         if (keytimes > playertimes[j])
                //         {
                //             int jp1 = j + 1;
                //             playernames[jp1] = playernames[j];
                //             playertimes[jp1] = playertimes[j];
                //             playervehicles[jp1] = playervehicles[j];
                //             j--; jp1--;
                //             playernames[jp1] = keynames;
                //             playertimes[jp1] = keytimes;
                //             playervehicles[jp1] = keyvehicles;
                //         }
                //         else flag = true;
                //     }
                // }
            }
            SendCustomEventDelayedFrames(nameof(SendScoreboardUpdate_Delayed), 1);
        }
        public void NewRecord()//master runs this
        {
            float newtime = TimeReporter._ReportedTime;
            string newvehicle = TimeReporter.ReportedVehicle;
            string playername = Networking.GetOwner(TimeReporter.gameObject).displayName;
            bool reverse = TimeReporter.Reported_RaceReverse;
            int posonboard;
            if (reverse)
            { posonboard = CheckIfOnBoard(playername, ref PlayerNames_R); }
            else
            { posonboard = CheckIfOnBoard(playername, ref PlayerNames); }
            if (posonboard > -1)//on board
            {
                bool NewTopRcrd = false;
                if (reverse)
                {
                    if (newtime > PlayerTimes_R[posonboard]) { return; }
                    if (newtime < PlayerTimes_R[0]) { NewTopRcrd = true; }
                    PlayerNames_R[posonboard] = playername;
                    PlayerTimes_R[posonboard] = newtime;
                    PlayerVehicles_R[posonboard] = newvehicle;
                    SortScoreboard(ref PlayerNames_R, ref PlayerTimes_R, ref PlayerVehicles_R);
                }
                else
                {
                    if (newtime > PlayerTimes[posonboard]) { return; }
                    if (newtime < PlayerTimes[0]) { NewTopRcrd = true; }
                    PlayerNames[posonboard] = playername;
                    PlayerTimes[posonboard] = newtime;
                    PlayerVehicles[posonboard] = newvehicle;
                    SortScoreboard(ref PlayerNames, ref PlayerTimes, ref PlayerVehicles);
                }
                if (NewTopRcrd)
                {
                    if (NewTopRecord_Snd)
                    { { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(PlayNewRecordSound)); } }
                }
                else
                {
                    if (NewTimeAdded_Snd)
                    { { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(PlayNewTimeSound)); } }
                }
            }
            else//not on board
            {
                if (reverse)
                {
                    if (PlayerTimes_R.Length < MaxRecordedTimes || newtime < PlayerTimes_R[PlayerTimes_R.Length - 1])
                    {
                        AddNewPlayerToBoard(playername, newtime, newvehicle, ref PlayerNames_R, ref PlayerTimes_R, ref PlayerVehicles_R);
                    }
                }
                else
                {
                    if (PlayerTimes.Length < MaxRecordedTimes || newtime < PlayerTimes[PlayerTimes.Length - 1])
                    {
                        AddNewPlayerToBoard(playername, newtime, newvehicle, ref PlayerNames, ref PlayerTimes, ref PlayerVehicles);
                    }
                }
            }
            RequestSerialization();
        }
        public void SendScoreboardUpdate_Delayed()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(UpdateScoreBoards_Vis));
        }
        public void UpdateScoreBoards_Vis()
        {
            //forward
            if (!Times_text) { return; }
            Names_text.text = LineHeightTxt + "Names\n";
            for (int i = 0; i < PlayerNames.Length; i++)
            {
                Names_text.text += PlayerNames[i] + "\n";
            }
            Times_text.text = LineHeightTxt + "Times\n";
            for (int i = 0; i < PlayerTimes.Length; i++)
            {
                Times_text.text += SecsToMinsSec(PlayerTimes[i]) + "\n";
            }
            Vehicles_text.text = LineHeightTxt + "Vehicles\n";
            for (int i = 0; i < PlayerVehicles.Length; i++)
            {
                Vehicles_text.text += PlayerVehicles[i] + "\n";
            }
            //reverse
            Names_R_text.text = LineHeightTxt + "Names\n";
            for (int i = 0; i < PlayerNames_R.Length; i++)
            {
                Names_R_text.text += PlayerNames_R[i] + "\n";
            }
            Times_R_text.text = LineHeightTxt + "Times\n";
            for (int i = 0; i < PlayerTimes_R.Length; i++)
            {
                Times_R_text.text += SecsToMinsSec(PlayerTimes_R[i]) + "\n";
            }
            Vehicles_R_text.text = LineHeightTxt + "Vehicles\n";
            for (int i = 0; i < PlayerVehicles_R.Length; i++)
            {
                Vehicles_R_text.text += PlayerVehicles_R[i] + "\n";
            }
        }
        private string SecsToMinsSec(float Seconds)
        {
            bool neg = false;
            if (Seconds < 0) { Seconds = -Seconds; neg = true; }
            float mins = Mathf.Floor(Seconds / 60f);
            float secs = Seconds % 60f;
            if (mins > 0)
            {
                if (neg) { mins = -mins; }
                return mins.ToString("F0") + ":" + (secs < 10 ? secs.ToString("F3").PadLeft(6, '0') : secs.ToString("F3"));
            }
            if (neg) { secs = -secs; }
            return secs.ToString("F3");
        }
    }
}