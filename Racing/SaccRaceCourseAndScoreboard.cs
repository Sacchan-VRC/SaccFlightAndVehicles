
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
        [Tooltip("Race starts from a standstill?")]
        public bool StartFromStill = false;
        [Tooltip("Race starts in the air? (only works if StartFromStill=true)")]
        public bool AirStart = true;
        [Tooltip("How long til race starts?")]
        public float CountDownLength = 3f;
        [Tooltip("Launch the vehicle at this speed when the race starts (only works if StartFromStill is true)")]
        public float LaunchSpeed = 0f;
        [Tooltip("If StartFromStill=true, teleport car here to start")]
        public Transform StartPoint;
        public Transform StartPoint_Reverse;
        public GameObject[] StartPointFX;
        public GameObject[] EndPointFX;
        [System.NonSerializedAttribute] public float[] SplitTimes;
        [System.NonSerializedAttribute] public float[] SplitTimes_R;
        [System.NonSerialized] public float MyBestTime;
        [System.NonSerialized] public float MyBestTime_R;
        [Header("Scoreboard:")]
        [Tooltip("Record the top MaxRecordedTimes number of records, forget about the rest")]
        public int MaxRecordedTimes = 15;
        public TextMeshProUGUI ScoreboardText;
        public TextMeshProUGUI ScoreboardText_R;
        [SerializeField] string CollumnPos0 = "<pos=0>";
        [SerializeField] string CollumnPos1 = "<pos=0.4>";
        [SerializeField] string CollumnPos2 = "<pos=2.7>";
        [SerializeField] string CollumnPos3 = "<pos=4.5>";
        [SerializeField] string CollumnPos4 = "<pos=6>";
        [SerializeField] string CollumnPos5 = "<pos=7.3>";
        [SerializeField] string CollumnPos6 = "<pos=8.6>";
        [SerializeField] string CollumnPos7 = "<pos=9.4>";
        [SerializeField] string EvenColor = "<color=#FFFFFF>";
        [SerializeField] string OddColor = "<color=#D8D8D8>";
        [Header("Debug:")]
        [UdonSynced] public float[] PlayerTimes;
        [UdonSynced] public float[] PlayerTimes_MostRecent;
        [UdonSynced] public string[] PlayerVehicles;
        [UdonSynced] public string[] PlayerNames;
        [UdonSynced] public ushort[] PlayerLaps;
        [UdonSynced] public float[] PlayerTimes_R;
        [UdonSynced] public float[] PlayerTimes_MostRecent_R;
        [UdonSynced] public string[] PlayerVehicles_R;
        [UdonSynced] public string[] PlayerNames_R;
        [UdonSynced] public ushort[] PlayerLaps_R;
        [System.NonSerialized] public bool RaceInProgress;
        private void Start()
        {
            UpdateScoreBoards_Vis();
            SplitTimes = new float[RaceCheckpoints.Length];
            SplitTimes_R = new float[RaceCheckpoints.Length];
        }
        public void AddNewPlayerToBoard(string playername, float time, string vehicle, ref string[] playernames, ref float[] playertimes, ref string[] playervehicles, ref float[] playertimes_mostrecent, ref ushort[] playerlaps)
        {
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
                float[] mr2 = new float[onemore];
                playertimes_mostrecent.CopyTo(mr2, 0);
                ushort[] l2 = new ushort[onemore];
                playerlaps.CopyTo(l2, 0);
                playernames = pn2;
                playertimes = pt2;
                playervehicles = pv2;
                playertimes_mostrecent = mr2;
                playerlaps = l2;
                playernames[len] = playername;
                playertimes[len] = time;
                playertimes_mostrecent[len] = time;
                playervehicles[len] = vehicle;
                playerlaps[len] = 1;
                //+1 to each array
                //set new values to new players record
            }
            else
            {
                //replace slowest time
                int last = playertimes.Length - 1;
                playertimes[last] = time;
                playertimes_mostrecent[last] = time;
                playervehicles[last] = vehicle;
                playernames[last] = playername;
                playerlaps[last] = 1;
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
        public void SortScoreboard(ref string[] playernames, ref float[] playertimes, ref string[] playervehicles, ref float[] playertimes_mostrecent, ref ushort[] playerlaps)//currently sorts backwards
        {
            int length = playertimes.Length;
            for (int i = 1; i < length; i++)
            {
                var keynames = playernames[i];
                var keytimes = playertimes[i];
                var keyvehicles = playervehicles[i];
                var keymostrecent = playertimes_mostrecent[i];
                var keylaps = playerlaps[i];
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
                        playertimes_mostrecent[jp1] = playertimes_mostrecent[j];
                        playerlaps[jp1] = playerlaps[j];
                        j--; jp1--;
                        playernames[jp1] = keynames;
                        playertimes[jp1] = keytimes;
                        playervehicles[jp1] = keyvehicles;
                        playertimes_mostrecent[jp1] = keymostrecent;
                        playerlaps[jp1] = keylaps;
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
        }
        public void NewRecord()//owner runs this
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
            bool NewTopRcrd = false;
            if (PlayerTimes.Length > 0)
            { if (newtime < PlayerTimes[0]) { NewTopRcrd = true; } }
            else NewTopRcrd = true;
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
            if (posonboard > -1)//on board
            {
                if (reverse)
                {
                    if (newtime > PlayerTimes_R[posonboard])
                    {
                        PlayerTimes_MostRecent_R[posonboard] = newtime;
                        PlayerLaps_R[posonboard] = (ushort)(PlayerLaps_R[posonboard] + 1);
                    }
                    else
                    {
                        PlayerNames_R[posonboard] = playername;
                        PlayerTimes_MostRecent_R[posonboard] = newtime;
                        PlayerTimes_R[posonboard] = newtime;
                        PlayerVehicles_R[posonboard] = newvehicle;
                        PlayerLaps_R[posonboard] = (ushort)(PlayerLaps_R[posonboard] + 1);
                    }
                }
                else
                {
                    if (newtime > PlayerTimes[posonboard])
                    {
                        PlayerTimes_MostRecent[posonboard] = newtime;
                        PlayerLaps[posonboard] = (ushort)(PlayerLaps[posonboard] + 1);
                    }
                    else
                    {
                        PlayerNames[posonboard] = playername;
                        PlayerTimes_MostRecent[posonboard] = newtime;
                        PlayerTimes[posonboard] = newtime;
                        PlayerVehicles[posonboard] = newvehicle;
                        PlayerLaps[posonboard] = (ushort)(PlayerLaps[posonboard] + 1);
                    }
                }
            }
            else//not on board
            {
                if (reverse)
                {
                    if (PlayerTimes_R.Length < MaxRecordedTimes || newtime < PlayerTimes_R[PlayerTimes_R.Length - 1])
                    {
                        AddNewPlayerToBoard(playername, newtime, newvehicle, ref PlayerNames_R, ref PlayerTimes_R, ref PlayerVehicles_R, ref PlayerTimes_MostRecent_R, ref PlayerLaps_R);
                    }
                }
                else
                {
                    if (PlayerTimes.Length < MaxRecordedTimes || newtime < PlayerTimes[PlayerTimes.Length - 1])
                    {
                        AddNewPlayerToBoard(playername, newtime, newvehicle, ref PlayerNames, ref PlayerTimes, ref PlayerVehicles, ref PlayerTimes_MostRecent, ref PlayerLaps);
                    }
                }
            }
            SortScoreboard(ref PlayerNames, ref PlayerTimes, ref PlayerVehicles, ref PlayerTimes_MostRecent, ref PlayerLaps);
            RequestSerialization();
            UpdateScoreBoards_Vis();
        }
        public override void OnDeserialization()
        {
            UpdateScoreBoards_Vis();
        }
        public void UpdateScoreBoards_Vis()
        {
            if (ScoreboardText)
            {
                ScoreboardText.text = LineHeightTxt + CollumnPos0 + "#" + CollumnPos1 + "Name" + CollumnPos2 + "Vehicle" + CollumnPos3 + "Best Time" + CollumnPos4 + "Delay" + CollumnPos5 + "Gap" + CollumnPos6 + "Laps" + CollumnPos7 + "Last Time\n";
                for (int i = 0; i < PlayerNames.Length; i++)
                {
                    string gap;
                    string delay;
                    if (i == 0)
                    {
                        gap = "--";
                        delay = "--";
                    }
                    else
                    {
                        gap = (PlayerTimes[i] - PlayerTimes[i - 1]).ToString("F3");
                        delay = (PlayerTimes[i] - PlayerTimes[0]).ToString("F3");
                    }

                    if (i % 2 == 0)
                        ScoreboardText.text += EvenColor;
                    else
                        ScoreboardText.text += OddColor;
                    ScoreboardText.text += CollumnPos0 + (i + 1).ToString("F0") + CollumnPos1 + PlayerNames[i] + CollumnPos2 + PlayerVehicles[i] + CollumnPos3 + SecsToMinsSec(PlayerTimes[i]) + CollumnPos4 + delay + CollumnPos5 + gap + CollumnPos6 + PlayerLaps[i] + CollumnPos7 + SecsToMinsSec(PlayerTimes_MostRecent[i]) + "\n";
                }
            }
            if (ScoreboardText_R)
            {
                ScoreboardText_R.text = LineHeightTxt + CollumnPos0 + "#" + CollumnPos1 + "Name" + CollumnPos2 + "Vehicle" + CollumnPos3 + "Best Time" + CollumnPos4 + "Delay" + CollumnPos5 + "Gap" + CollumnPos6 + "Laps" + CollumnPos7 + "Last Time\n";
                for (int i = 0; i < PlayerNames_R.Length; i++)
                {
                    string gap;
                    string delay;
                    if (i == 0)
                    {
                        gap = "--";
                        delay = "--";
                    }
                    else
                    {
                        gap = (PlayerTimes_R[i] - PlayerTimes_R[i - 1]).ToString("F3");
                        delay = (PlayerTimes_R[i] - PlayerTimes_R[0]).ToString("F3");
                    }

                    if (i % 2 == 0)
                        ScoreboardText_R.text += EvenColor;
                    else
                        ScoreboardText_R.text += OddColor;
                    ScoreboardText_R.text += CollumnPos0 + (i + 1).ToString("F0") + CollumnPos1 + PlayerNames_R[i] + CollumnPos2 + PlayerVehicles_R[i] + CollumnPos3 + SecsToMinsSec(PlayerTimes_R[i]) + CollumnPos4 + delay + CollumnPos5 + gap + CollumnPos6 + PlayerLaps_R[i] + CollumnPos7 + SecsToMinsSec(PlayerTimes_MostRecent_R[i]) + "\n";
                }
            }
        }
        public void ResetScoreboard()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            PlayerTimes = new float[0];
            PlayerTimes_MostRecent = new float[0];
            PlayerVehicles = new string[0];
            PlayerNames = new string[0];
            PlayerLaps = new ushort[0];
            SortScoreboard(ref PlayerNames, ref PlayerTimes, ref PlayerVehicles, ref PlayerTimes_MostRecent, ref PlayerLaps);
            RequestSerialization();
            UpdateScoreBoards_Vis();
        }
        public void ResetScoreboard_R()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            PlayerTimes_R = new float[0];
            PlayerTimes_MostRecent_R = new float[0];
            PlayerVehicles_R = new string[0];
            PlayerNames_R = new string[0];
            PlayerLaps_R = new ushort[0];
            SortScoreboard(ref PlayerNames_R, ref PlayerTimes_R, ref PlayerVehicles_R, ref PlayerTimes_MostRecent_R, ref PlayerLaps_R);
            RequestSerialization();
            UpdateScoreBoards_Vis();
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