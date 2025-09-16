
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SaccRacingTrigger : UdonSharpBehaviour
    {
        public string PlaneName;
        [Tooltip("Set automatically on build if left empty")]
        public SaccRaceToggleButton RaceToggler;
        public GameObject[] DisabledRaces;
        public GameObject[] InstanceRecordDisallowedRaces;
        public TextMeshProUGUI Racename_text;
        public TextMeshProUGUI Time_text;
        public TextMeshProUGUI SplitTime_text;
        [Tooltip("This game object is enabled while waiting for race to start")]
        public GameObject CountDownAnimation;
        private int NumCountDowns;
        private int NumCountDownDisables;
        private SaccEntity Vehicle_EntityControl;//This script is not an SaccEntity extension behaviour
        private SGV_GearBox Vehicle_GearBox;
        private bool OverridingClutch;
        [System.NonSerialized, FieldChangeCallback(nameof(TrackForward))] public bool _TrackForward = true;
        public bool TrackForward
        {
            set
            {
                _TrackForward = value;
            }
            get => _TrackForward;
        }
        [System.NonSerialized] public float[] CurrentSplits;
        private bool CurrentTrackAllowReverse;
        private SaccRaceCourseAndScoreboard CurrentCourse;
        private int CurrentCourseSelection = -1;
        private int NextCheckpoint;
        private bool InProgress;
        private int TrackDirection = 1;
        private int FirstCheckPoint = 1;
        private int FinalCheckpoint;
        private bool RaceOn;
        private float RaceStartTime = 0;
        private float RaceTime;
        private Animator CurrentCheckPointAnimator;
        private Animator NextCheckPointAnimator;
        private VRCPlayerApi localPlayer;
        private bool InEditor = false;
        private float LastTime;
        private LayerMask ThisObjLayer;
        private float LastFrameCheckpointDist;
        private CapsuleCollider ThisCapsuleCollider;
        private Rigidbody PlaneRigidbody;
        private Vector3 PlaneClosestPos;
        private Vector3 PlaneClosestPosLastFrame;
        private float PlaneDistanceFromCheckPoint;
        private float PlaneDistanceFromCheckPointLastFrame;
        private Vector3 PlaneClosestPosInverseFromPlane;
        private float RaceFinishTime;
        private bool LoopFinalSplit = false;
        private bool Initialized = false;
        private bool RaceCountdown = false;
        private bool FreezeCar = false;
        private bool AirStart;
        private void Initialize()
        {
            Initialized = true;
            GameObject Objs = gameObject;
            //checking if a rigidbody is null in a while loop doesnt work in udon for some reason, use official vrchat workaround
            while (!Utilities.IsValid(PlaneRigidbody) && Objs.transform.parent)
            {
                Objs = Objs.transform.parent.gameObject;
                PlaneRigidbody = Objs.GetComponent<Rigidbody>();
            }
            Vehicle_EntityControl = PlaneRigidbody.GetComponent<SaccEntity>();
            Vehicle_GearBox = (SGV_GearBox)Vehicle_EntityControl.GetExtention(GetUdonTypeName<SGV_GearBox>());
            ThisCapsuleCollider = gameObject.GetComponent<CapsuleCollider>();
            ThisObjLayer = 1 << gameObject.layer;
            localPlayer = Networking.LocalPlayer;
            if (localPlayer == null) { InEditor = true; }
            TimerTextCounter++; SetTimerEmpty();
            SplitTextCounter++; SetSplitEmpty();
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
        private void Update()
        {
            if (RaceOn && !RaceCountdown)
            {
                RaceTime = Time.time - RaceStartTime;
                if (Time_text) { Time_text.text = SecsToMinsSec(RaceTime); }
            }
        }
        private int CurrentSplit = 0;
        private void UpdateSplitTime(bool RaceEnded = false)
        {
            CurrentSplits[CurrentSplit] = (Time.time - RaceStartTime - CalcSubFrameTime());
            float splitdif;
            if (TrackForward)
            {
                splitdif = CurrentSplits[CurrentSplit] - CurrentCourse.SplitTimes[CurrentSplit];
                bool faster = splitdif < 0;
                bool firstrun = CurrentCourse.MyBestTime == 0f;
                if (SplitTime_text) { SplitTime_text.text = (!firstrun ? (faster ? "<color=green>" : "<color=red>+") + SecsToMinsSec(splitdif) : "<color=green>") + (RaceEnded ? "\n" : "\n<color=white>") + SecsToMinsSec(CurrentSplits[CurrentSplit]); }
            }
            else
            {
                splitdif = CurrentSplits[CurrentSplit] - CurrentCourse.SplitTimes_R[CurrentSplit];
                bool faster = splitdif < 0;
                bool firstrun = CurrentCourse.MyBestTime_R == 0f;
                if (SplitTime_text) { SplitTime_text.text = (!firstrun ? (faster ? "<color=green>" : "<color=red>+") + SecsToMinsSec(splitdif) : "<color=green>") + (RaceEnded ? "\n" : "\n<color=white>") + SecsToMinsSec(CurrentSplits[CurrentSplit]); }
            }
            CurrentSplit++;
            SetSplitEmptyDelayed(3f);
        }
        int TimerTextCounter = 0;
        private void SetTimerEmptyDelayed(float delay)
        {
            TimerTextCounter++;
            SendCustomEventDelayedSeconds(nameof(SetTimerEmpty), delay);
        }
        public void SetTimerEmpty()
        {
            TimerTextCounter--;
            if (TimerTextCounter != 0f) { return; }
            if (Time_text) { Time_text.text = string.Empty; }
        }
        int SplitTextCounter = 0;
        private void SetSplitEmptyDelayed(float delay)
        {
            SplitTextCounter++;
            SendCustomEventDelayedSeconds(nameof(SetSplitEmpty), delay);
        }
        public void SetSplitEmpty()
        {
            SplitTextCounter--;
            if (SplitTextCounter != 0f) { return; }
            if (SplitTime_text) { SplitTime_text.text = string.Empty; }
        }
        private void FixedUpdate()
        {
            Collider checkpoint = CurrentCheckPointAnimator.gameObject.GetComponent<Collider>();
            //save values from last frame, for comparing distance
            PlaneDistanceFromCheckPointLastFrame = PlaneDistanceFromCheckPoint;
            PlaneClosestPosLastFrame = PlaneClosestPos;
            //getclosest point on checkpoint trigger collider to plane
            //find closest point on plane to that^
            PlaneClosestPos = ThisCapsuleCollider.ClosestPoint(checkpoint.ClosestPoint(transform.position));

            //Raycast from plane in direction of movement of the closest point on the plane to the checkpoint, which will hit the checkpoint if moving toward it.
            PlaneClosestPosInverseFromPlane = transform.InverseTransformPoint(PlaneClosestPos);
            RaycastHit hit;
            ThisCapsuleCollider.enabled = false;//somehow this raycast can hit itself if we don't
            if (Physics.Raycast(PlaneClosestPos, PlaneRigidbody.GetPointVelocity(PlaneClosestPos), out hit, 50, ThisObjLayer, QueryTriggerInteraction.Collide))
            {
                PlaneDistanceFromCheckPoint = hit.distance;
            }
            ThisCapsuleCollider.enabled = true;
            if (RaceCountdown)
            {
                RaceStartTime = Time.time;
                if (FreezeCar)
                {
                    MoveCarToStart();
                    return;
                }
                Vector3 vel = PlaneRigidbody.velocity;
                vel.x = 0; vel.z = 0;
                PlaneRigidbody.velocity = vel;
                Vector3 angvel = PlaneRigidbody.angularVelocity;
                vel.y = 0; vel.z = 0;
                PlaneRigidbody.angularVelocity = angvel;
                Vector3 newpos = PlaneRigidbody.position;
                if (TrackForward)
                {
                    if (CurrentCourse.StartPoint)
                    {
                        newpos = new Vector3(CurrentCourse.StartPoint.position.x, PlaneRigidbody.position.y, CurrentCourse.StartPoint.position.z);
                    }
                }
                else
                {
                    if (CurrentCourse.StartPoint_Reverse)
                    {
                        newpos = new Vector3(CurrentCourse.StartPoint_Reverse.position.x, PlaneRigidbody.position.y, CurrentCourse.StartPoint_Reverse.position.z);
                    }
                }
                PlaneRigidbody.position = newpos;
            }
        }
        public void MoveCarToStart()
        {
            PlaneRigidbody.velocity = Vector3.zero;
            PlaneRigidbody.angularVelocity = Vector3.zero;
            if (TrackForward)
            {
                if (CurrentCourse.StartPoint)
                {
                    PlaneRigidbody.position = CurrentCourse.StartPoint.position;
                    PlaneRigidbody.rotation = CurrentCourse.StartPoint.rotation;
                }
                else { Debug.LogWarning(CurrentCourse.name + ": Race Start Point is null"); }
            }
            else
            {
                if (CurrentCourse.StartPoint_Reverse)
                {
                    PlaneRigidbody.position = CurrentCourse.StartPoint_Reverse.position;
                    PlaneRigidbody.rotation = CurrentCourse.StartPoint_Reverse.rotation;
                }
                else { Debug.LogWarning(CurrentCourse.name + ": Reverse Race Start Point is null"); }
            }
        }
        public void SetFreezeCarFalse()
        {
            FreezeCar = false;
            SendCustomEventDelayedSeconds(nameof(SetDeadFalse), Time.fixedDeltaTime * 2f);
        }
        public void SetDeadFalse() { Vehicle_EntityControl.dead = false; }
        public void SetRaceCountdownFalse()
        {
            NumCountDowns--;
            NumCountDownDisables++;
            if (AirStart)
            {
                SetFreezeCarFalse();
            }
            float launchspd = CurrentCourse.LaunchSpeed;
            if (launchspd > 0f)
            { PlaneRigidbody.velocity = PlaneRigidbody.transform.forward * CurrentCourse.LaunchSpeed; }
            SendCustomEventDelayedSeconds(nameof(DisableCountDownAnimation), 3f);
            if (NumCountDowns != 0) { return; }
            RaceCountdown = false;
            if (OverridingClutch)
            {
                OverridingClutch = false;
                if (Vehicle_GearBox)
                {
                    Vehicle_GearBox.ClutchOverride_--;
                }
            }
        }
        public void DisableCountDownAnimation()
        {
            NumCountDownDisables--;
            if (NumCountDownDisables != 0) { return; }
            if (CountDownAnimation)
            { CountDownAnimation.SetActive(false); }
        }
        void ReportTime()
        {
            Vehicle_EntityControl.SendEventToExtensions("SFEXT_L_FinishRace");
            //Debug.Log("Finish Race!");
            RaceFinishTime = Time.time;
            RaceTime = LastTime = (RaceFinishTime - RaceStartTime - CalcSubFrameTime());
            if (TrackForward || !CurrentTrackAllowReverse)//track was finished forward
            {
                //if i am not on board, AND my time is better than the worst time on board OR
                //if i am on board, and my time is better than my previous time
                //add submit my time
                int mypos = CurrentCourse.CheckIfOnBoard(localPlayer.displayName, ref CurrentCourse.PlayerNames);
                if (mypos < 0)//i am not on the board
                {
                    if (CurrentCourse.PlayerTimes.Length > 0)
                    {
                        if (RaceTime < CurrentCourse.PlayerTimes[CurrentCourse.PlayerTimes.Length - 1] || CurrentCourse.PlayerTimes.Length < CurrentCourse.MaxRecordedTimes)
                        { SendMyTime(false); }
                    }
                    else
                    { SendMyTime(false); }
                }
                else//i am on the board
                {
                    // if (RaceTime < CurrentCourse.PlayerTimes[mypos])
                    { SendMyTime(false); }
                }
            }
            else//track was finished backward
            {
                int mypos = CurrentCourse.CheckIfOnBoard(localPlayer.displayName, ref CurrentCourse.PlayerNames_R);
                if (mypos < 0)//i am not on the board
                {
                    if (CurrentCourse.PlayerTimes_R.Length > 0)
                    {
                        if (RaceTime < CurrentCourse.PlayerTimes_R[CurrentCourse.PlayerTimes_R.Length - 1] || CurrentCourse.PlayerTimes_R.Length < CurrentCourse.MaxRecordedTimes)
                        { SendMyTime(true); }
                    }
                    else
                    { SendMyTime(true); }
                }
                else//i am on the board
                {
                    // if (RaceTime < CurrentCourse.PlayerTimes_R[mypos])
                    { SendMyTime(true); }
                }
            }
            if (TrackForward)
            {
                bool TimeImproved = RaceTime < CurrentCourse.MyBestTime || CurrentCourse.MyBestTime == 0;
                if (!CurrentCourse.LoopRace)
                {
                    string TimeColor = TimeImproved ? "<color=green>" : "<color=red>";
                    if (Time_text) { Time_text.text = TimeColor + SecsToMinsSec(RaceTime); }
                }
                if (TimeImproved || CurrentCourse.MyBestTime == 0f)
                {
                    CurrentCourse.MyBestTime = RaceTime;
                    CurrentCourse.SplitTimes = CurrentSplits;
                }
            }
            else
            {
                bool TimeImproved = RaceTime < CurrentCourse.MyBestTime_R || CurrentCourse.MyBestTime_R == 0;
                if (!CurrentCourse.LoopRace)
                {
                    string TimeColor = TimeImproved ? "<color=green>" : "<color=red>";
                    if (Time_text) { Time_text.text = TimeColor + SecsToMinsSec(RaceTime); }
                }
                if (TimeImproved || CurrentCourse.MyBestTime_R == 0f)
                {
                    CurrentCourse.MyBestTime_R = RaceTime;
                    CurrentCourse.SplitTimes_R = CurrentSplits;
                }
            }
            RaceTime = 0;
            SetTimerEmptyDelayed(6f);
        }
        void OnTriggerEnter(Collider other)
        {
            if (
                (Time.time - RaceFinishTime < 2f && !CurrentCourse.LoopRace)
                || (CurrentCourseSelection == -1 || (other && other.gameObject != CurrentCourse.RaceCheckpoints[NextCheckpoint]))
                ) { return; }
            if (NextCheckpoint == FinalCheckpoint)//end of the race
            {
                NextCheckpoint = FirstCheckPoint;
                if (Utilities.IsValid(CurrentCheckPointAnimator))
                { CurrentCheckPointAnimator.SetBool("Current", false); }
                StartCheckPointAnims();
                if (CurrentCourse.LoopRace)
                {
                    LoopFinalSplit = true;
                    UpdateSplitTime();
                }
                else
                {
                    UpdateSplitTime(true);
                    CurrentSplit = 0;
                    if (InProgress)
                    {
                        RaceToggler.RacesInProgress--;
                        InProgress = false;
                        CurrentCourse.RaceInProgress = false;
                    }
                    RaceOn = false;
                    ReportTime();
                }
            }
            else if (NextCheckpoint == FirstCheckPoint)//starting the race
            {
                if (LoopFinalSplit)
                {
                    UpdateSplitTime(true);
                    ReportTime();
                    LoopFinalSplit = false;
                }
                if (CurrentCourse.StartFromStill)
                {
                    if (PlaneRigidbody)
                    {
                        RaceCountdown = true;
                        FreezeCar = true;
                        NumCountDowns++;
                        Vehicle_EntityControl.dead = true;//make invincible for teleport
                        SendCustomEventDelayedSeconds(nameof(SetRaceCountdownFalse), CurrentCourse.CountDownLength);
                        if (!AirStart)
                        { SendCustomEventDelayedSeconds(nameof(SetFreezeCarFalse), .2f); }
                        if (CountDownAnimation)
                        { CountDownAnimation.SetActive(true); }
                        if (!OverridingClutch)
                        {
                            OverridingClutch = true;
                            if (Vehicle_GearBox)
                            {
                                Vehicle_GearBox.ClutchOverride_++;
                            }
                        }
                    }
                }
                Vehicle_EntityControl.SendEventToExtensions("SFEXT_L_StartRace");
                RaceTime = 0;
                CurrentSplits = new float[CurrentCourse.RaceCheckpoints.Length];
                CurrentSplit = 0;

                //Debug.Log("Start Race!");
                RaceStartTime = Time.time - CalcSubFrameTime();
                RaceOn = true;
                if (!InProgress)
                {
                    RaceToggler.RacesInProgress++;
                    InProgress = true;
                    CurrentCourse.RaceInProgress = true;
                }
                NextCheckpoint += TrackDirection;
                if (CurrentCourse.LoopRace)
                {
                    if (!TrackForward)
                    {
                        if (NextCheckpoint < 0)
                        { NextCheckpoint = CurrentCourse.RaceCheckpoints.Length - 1; }
                    }
                    else
                    {
                        if (NextCheckpoint > CurrentCourse.RaceCheckpoints.Length - 1)
                        { NextCheckpoint = 0; }
                    }
                }
                ProgressCheckPointAnims();
            }
            else
            {
                //Debug.Log("CheckPoint!");
                NextCheckpoint += TrackDirection;
                ProgressCheckPointAnims();
                UpdateSplitTime();
                //check if the next checkpoint is the end of the race, because if it is we need to get subframe time
            }
        }
        private float CalcSubFrameTime()
        {
            float subframetime = 0;
            //get world space position of the point that was closest to the checkpoint the frame before the race was finished
            Vector3 LastFrameClosestPosThisFrame = transform.TransformPoint(PlaneClosestPosInverseFromPlane);
            //get the speed of the plane by comparing the two
            float speed = Vector3.Distance(PlaneClosestPosLastFrame, LastFrameClosestPosThisFrame);
            //check if the plane is travelling further per frame than the distance to the checkpoint from the last frame, just incase the raycast somehow missed
            //(only do subframe time if it'll be valid)
            if (speed > PlaneDistanceFromCheckPointLastFrame)
            {
                float passedratio = PlaneDistanceFromCheckPointLastFrame / speed;
                subframetime = -Time.fixedDeltaTime * passedratio + Time.fixedDeltaTime;
            }
            return subframetime;
        }
        private void SendMyTime(bool reverse)
        {
            CurrentCourse.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, "NewRecord", RaceTime, PlaneName, localPlayer.displayName, reverse);
        }
        public void SetCourse()
        {
            TurnOffCurrentCheckPoints();
            CurrentCourseSelection = RaceToggler.CurrentCourseSelection;
            if (CurrentCourseSelection != -1)
            {
                if (Racename_text)
                {
                    Racename_text.text = RaceToggler.Races[CurrentCourseSelection].RaceName;
                    if (!TrackForward) { Racename_text.text += " (R)"; }
                }
                CurrentCourse = RaceToggler.Races[CurrentCourseSelection].GetComponent<SaccRaceCourseAndScoreboard>();
                AirStart = CurrentCourse.AirStart;
                CurrentTrackAllowReverse = CurrentCourse.AllowReverse;
                if (TrackForward || !CurrentTrackAllowReverse)
                {
                    NextCheckpoint = FirstCheckPoint = 0;
                    FinalCheckpoint = CurrentCourse.RaceCheckpoints.Length - 1;
                    TrackDirection = 1;
                }
                else
                {
                    if (CurrentCourse.LoopRace)
                    {
                        NextCheckpoint = FirstCheckPoint = 0;
                        FinalCheckpoint = 1;
                        TrackDirection = -1;
                    }
                    else
                    {
                        NextCheckpoint = FirstCheckPoint = CurrentCourse.RaceCheckpoints.Length - 1;
                        FinalCheckpoint = 0;
                        TrackDirection = -1;
                    }
                }
                if (InEditor || gameObject.activeInHierarchy) { StartCheckPointAnims(); }//don't turn on lights when switching course unless in editor for testing, or pressing while in a vehicle
            }
        }
        public void SetUpNewRace()
        {
            if (!Initialized) { Initialize(); }
            RaceOn = false;
            RaceTime = 0;
            LoopFinalSplit = false;
            if (CheckDisallowedRace())
            {
                CurrentCourseSelection = -1;
                return;
            }
            SetCourse();
        }
        void OnEnable()//Happens when you get in a vehicle, if a race is enabled
        {
            SetUpNewRace();
            StartCheckPointAnims();
        }
        void OnDisable()
        {
            TurnOffCurrentCheckPoints();
            TimerTextCounter++; SetTimerEmpty();
            if (CountDownAnimation)
            { CountDownAnimation.SetActive(false); }
            if (OverridingClutch)
            {
                OverridingClutch = false;
                if (Vehicle_GearBox)
                {
                    Vehicle_GearBox.ClutchOverride_--;
                }
            }
            RaceCountdown = false;
            SetFreezeCarFalse();
            if (RaceOn)
            {
                Vehicle_EntityControl.SendEventToExtensions("SFEXT_L_CancelRace");
            }
            RaceOn = false;
        }
        public void TurnOffCurrentCheckPoints()
        {
            if (CurrentCourse)
            {
                if (InProgress)
                {
                    RaceToggler.RacesInProgress--;
                    InProgress = false;
                    CurrentCourse.RaceInProgress = false;
                }
                if (Utilities.IsValid(CurrentCheckPointAnimator))
                {
                    CurrentCheckPointAnimator.SetBool("Current", false);
                    CurrentCheckPointAnimator.SetTrigger("Reset");
                }
                if (Utilities.IsValid(NextCheckPointAnimator))
                {
                    NextCheckPointAnimator.SetBool("Next", false);
                    NextCheckPointAnimator.SetTrigger("Reset");
                }
            }
        }
        void ProgressCheckPointAnims()
        {
            if (Utilities.IsValid(CurrentCheckPointAnimator))
            { CurrentCheckPointAnimator.SetBool("Current", false); }

            if (Utilities.IsValid(NextCheckPointAnimator))
            {
                NextCheckPointAnimator.SetBool("Next", false);
                NextCheckPointAnimator.SetBool("Current", true);
            }
            CurrentCheckPointAnimator = NextCheckPointAnimator;
            int next2 = NextCheckpoint + TrackDirection;
            if (CurrentCourse.LoopRace && next2 == CurrentCourse.RaceCheckpoints.Length) { next2 = 0; }
            if (next2 > -1 && next2 < CurrentCourse.RaceCheckpoints.Length)
            {
                NextCheckPointAnimator = CurrentCourse.RaceCheckpoints[next2].GetComponent<Animator>();
                if (Utilities.IsValid(NextCheckPointAnimator))
                {
                    NextCheckPointAnimator.SetBool("Reverse", !_TrackForward);
                    NextCheckPointAnimator.SetBool("Next", true);
                }
            }
        }
        void StartCheckPointAnims()
        {
            if (CurrentCourseSelection == -1) { return; }
            if (CurrentCourse)
            {
                if (CurrentCourse.RaceCheckpoints.Length > 0)
                {
                    CurrentCheckPointAnimator = CurrentCourse.RaceCheckpoints[FirstCheckPoint].GetComponent<Animator>();
                    if (Utilities.IsValid(CurrentCheckPointAnimator))
                    {
                        CurrentCheckPointAnimator.SetBool("Reverse", !_TrackForward);
                        CurrentCheckPointAnimator.SetBool("Current", true);
                        CurrentCheckPointAnimator.SetBool("Next", false);
                    }
                }
                if (CurrentCourse.RaceCheckpoints.Length > 1)
                {
                    NextCheckPointAnimator = CurrentCourse.RaceCheckpoints[FirstCheckPoint + (FirstCheckPoint + TrackDirection < 0 ? CurrentCourse.RaceCheckpoints.Length - 1 : TrackDirection)].GetComponent<Animator>();
                    if (Utilities.IsValid(NextCheckPointAnimator))
                    {
                        NextCheckPointAnimator.SetBool("Reverse", !_TrackForward);
                        NextCheckPointAnimator.SetBool("Next", true);
                    }
                }
            }
        }
        private bool CheckDisallowedRace()
        {
            int ccs = RaceToggler.CurrentCourseSelection;
            if (ccs == -1) { return true; }
            foreach (GameObject race in DisabledRaces)
            {
                if (race == RaceToggler.Races[ccs].gameObject)
                {
                    return true;
                }
            }
            return false;
        }
        private bool CheckRecordDisallowedRace()
        {
            foreach (GameObject race in InstanceRecordDisallowedRaces)
            {
                if (race == RaceToggler.Races[CurrentCourseSelection].gameObject)
                {
                    return true;
                }
            }
            return false;
        }
    }
}