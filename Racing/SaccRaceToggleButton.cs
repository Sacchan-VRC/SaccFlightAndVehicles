
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SaccRaceToggleButton : UdonSharpBehaviour
    {
        [HideInInspector] public SaccRacingTrigger[] RacingTriggers;
        [HideInInspector] public SaccRaceCourseAndScoreboard[] Races;
        [Tooltip("Can be used to set a default course -1 = none")]
        public int LastCourseSelection = -1;
        public int CurrentCourseSelection = -1;
        private bool Reverse = false;
        public bool _AutomaticRaceSelection = true;
        public bool AutomaticRaceSelection
        {
            set
            {
                if (_AutomaticRaceSelection == value) { return; }
                _AutomaticRaceSelection = value;
                if (value)
                {
                    RaceSelectionLoop();
                }
                else
                {
                    if (CurrentCourseSelection != -1)
                    {
                        LastCourseSelection = CurrentCourseSelection;
                        CurrentCourseSelection = -1;
                        SetRace();
                    }
                    AutoEnableRace_NextRace = 0;
                    ClosestRaceDist = 99999f;
                }
            }
            get => _AutomaticRaceSelection;
        }
        public GameObject EnableWhenNoneSelected;
        private bool RacesInProgress_;
        [System.NonSerialized] public int _RacesInProgress = 0;//should only be 0 or 1, but wont break if goes higher somehow
        public int RacesInProgress
        {
            set
            {
                RacesInProgress_ = value > 0;
                _RacesInProgress = value;
            }
            get => _RacesInProgress;
        }
        private void Start()
        {
            if (CurrentCourseSelection == -1) //-1 = all races disabled
            {
                foreach (SaccRaceCourseAndScoreboard race in Races)
                {
                    race.RaceObjects.SetActive(false);
                }
            }
            else
            {
                if (CurrentCourseSelection != -1)
                {
                    foreach (SaccRacingTrigger RaceTrig in RacingTriggers)
                    { RaceTrig.gameObject.SetActive(true); }
                }
            }
            if (AutomaticRaceSelection)
            { RaceSelectionLoop(); }
            else
            { SetRace(); }
        }
        public override void Interact()
        {
            NextRace();
        }
        public void NextRace()
        {
            LastCourseSelection = CurrentCourseSelection;
            if (CurrentCourseSelection == Races.Length - 1)
            { CurrentCourseSelection = -1; }
            else { CurrentCourseSelection++; }

            SetRace();
        }
        public void PreviousRace()
        {
            LastCourseSelection = CurrentCourseSelection;
            if (CurrentCourseSelection == -1)
            { CurrentCourseSelection = Races.Length - 1; }
            else { CurrentCourseSelection--; }

            SetRace();
        }
        public void SetRace()
        {
            if (LastCourseSelection != -1)
            {
                Races[LastCourseSelection].RaceInProgress = false;
                for (int i = 0; i < Races[LastCourseSelection].RaceCheckpoints.Length; i++)
                { Races[LastCourseSelection].RaceCheckpoints[i].GetComponent<Animator>().WriteDefaultValues(); }
                Races[LastCourseSelection].RaceObjects.SetActive(false);
            }
            if (CurrentCourseSelection != -1)//-1 = all races disabled
            {
                if (EnableWhenNoneSelected) { EnableWhenNoneSelected.SetActive(false); }
                SaccRaceCourseAndScoreboard race = Races[CurrentCourseSelection].GetComponent<SaccRaceCourseAndScoreboard>();
                race.RaceObjects.SetActive(true);
                // race.UpdateTimes();

                foreach (SaccRacingTrigger RaceTrig in RacingTriggers)
                {
                    RaceTrig.gameObject.SetActive(true);
                }
            }
            else
            {
                if (EnableWhenNoneSelected) { EnableWhenNoneSelected.SetActive(true); }
                foreach (SaccRacingTrigger RaceTrig in RacingTriggers)
                {
                    RaceTrig.gameObject.SetActive(false);
                }
            }
            foreach (SaccRacingTrigger RaceTrig in RacingTriggers)
            {
                RaceTrig.SetUpNewRace();
            }
        }
        public void ToggleReverse()
        {
            if (!Reverse)
            { SetTrack_Reverse(); }
            else
            { SetTrack_Forward(); }
            SetRace();
        }
        public void SetTrack_Reverse()
        {
            Reverse = true;
            foreach (SaccRacingTrigger RaceTrig in RacingTriggers)
            {
                RaceTrig.SetProgramVariable("_TrackForward", false);
            }
        }
        public void SetTrack_Forward()
        {
            Reverse = false;
            foreach (SaccRacingTrigger RaceTrig in RacingTriggers)
            {
                RaceTrig.SetProgramVariable("_TrackForward", true);
            }
        }
        private int AutoEnableRace_NextRace = 0;
        private int ClosestRace = 0;
        private bool ClosestRace_forward = false;
        private float ClosestRaceDist = 99999f;
        public void ToggleAutoRaceSelection()
        { AutomaticRaceSelection = !AutomaticRaceSelection; }
        public void RaceSelectionLoop()
        {
            if (!_AutomaticRaceSelection) { return; }
            SendCustomEventDelayedFrames(nameof(RaceSelectionLoop), 1);
            if (RacesInProgress > 0)
            {
                AutoEnableRace_NextRace = 0;
                ClosestRaceDist = 99999f;
                return;
            }
            float checkdiststart = Vector3.Distance(Races[AutoEnableRace_NextRace].RaceCheckpoints[0].transform.position, Networking.LocalPlayer.GetPosition());
            float checkdistend = Vector3.Distance(Races[AutoEnableRace_NextRace].RaceCheckpoints[Races[AutoEnableRace_NextRace].RaceCheckpoints.Length - 1].transform.position, Networking.LocalPlayer.GetPosition());
            if (checkdiststart < checkdistend || !Races[AutoEnableRace_NextRace].AllowReverse || Races[AutoEnableRace_NextRace].LoopRace)
            {
                if (checkdiststart < ClosestRaceDist)
                {
                    if (Races[AutoEnableRace_NextRace].LoopRace)
                    {
                        Vector3 relplayerpos = Networking.LocalPlayer.GetPosition() - Races[AutoEnableRace_NextRace].RaceCheckpoints[0].transform.position;
                        if (Vector3.Dot(Races[AutoEnableRace_NextRace].RaceCheckpoints[0].transform.forward, relplayerpos) > 1)
                        { ClosestRace_forward = false; }
                        else
                        { ClosestRace_forward = true; }
                        ClosestRaceDist = checkdiststart;
                        ClosestRace = AutoEnableRace_NextRace;
                    }
                    else
                    {
                        ClosestRaceDist = checkdiststart;
                        ClosestRace_forward = true;
                        ClosestRace = AutoEnableRace_NextRace;
                    }
                }
            }
            else
            {
                if (checkdistend < ClosestRaceDist)
                {
                    ClosestRaceDist = checkdistend;
                    ClosestRace_forward = false;
                    ClosestRace = AutoEnableRace_NextRace;
                }
            }
            AutoEnableRace_NextRace++;
            if (AutoEnableRace_NextRace >= Races.Length)
            {
                //set up new race
                bool DirectionCheck = !ClosestRace_forward != Reverse;
                if (ClosestRace_forward && Reverse)
                { SetTrack_Forward(); }
                else if (!ClosestRace_forward && !Reverse)
                { SetTrack_Reverse(); }

                if (ClosestRace_forward || Races[ClosestRace].LoopRace)
                {
                    if (ClosestRaceDist > Races[ClosestRace].Autotoggler_EnableDist_Forward)
                    { ClosestRace = -1; }
                }
                else
                {
                    if (ClosestRaceDist > Races[ClosestRace].Autotoggler_EnableDist_Reverse)
                    { ClosestRace = -1; }
                }
                bool coursechanged = LastCourseSelection != ClosestRace;
                if (coursechanged)
                {
                    LastCourseSelection = CurrentCourseSelection;
                    CurrentCourseSelection = ClosestRace;
                }
                if (coursechanged || DirectionCheck)
                {
                    SetRace();
                }
                AutoEnableRace_NextRace = 0;
                ClosestRaceDist = 99999f;
            }
        }
    }
}