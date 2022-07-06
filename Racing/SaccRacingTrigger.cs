
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class SaccRacingTrigger : UdonSharpBehaviour
{
    public string PlaneName;
    [Tooltip("Set automatically on build if left empty")]
    public SaccRaceToggleButton Button;
    public GameObject[] DisabledRaces;
    public GameObject[] InstanceRecordDisallowedRaces;
    public Text TimeText_Cockpit;
    [System.NonSerialized, FieldChangeCallback(nameof(TrackForward))] public bool _TrackForward = true;
    public bool TrackForward
    {
        set
        {
            _TrackForward = value;
            SetUpNewRace();
        }
        get => _TrackForward;
    }
    private bool CurrentTrackAllowReverse;
    private SaccRaceCourseAndScoreboard CurrentCourse;
    private int CurrentCourseSelection = -1;
    private int NextCheckpoint;
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
    private bool DoSubFrameTimeCheck;
    private LayerMask ThisObjLayer;
    private float LastFrameCheckpointDist;
    private CapsuleCollider ThisCapsuleCollider;
    private Rigidbody PlaneRigidbody;
    private Vector3 PlaneClosestPos;
    private Vector3 PlaneClosestPosLastFrame;
    private float PlaneDistanceFromCheckPoint;
    private float PlaneDistanceFromCheckPointLastFrame;
    private Vector3 PlaneClosestPosInverseFromPlane;
    private bool Initialized = false;
    private void Initialize()
    {
        Initialized = true;
        _TrackForward = true;//Why is this needed?
        GameObject Objs = gameObject;
        //checking if a rigidbody is null in a while loop doesnt work in udon for some reason, use official vrchat workaround
        while (!Utilities.IsValid(PlaneRigidbody) && Objs.transform.parent)
        {
            Objs = Objs.transform.parent.gameObject;
            PlaneRigidbody = Objs.GetComponent<Rigidbody>();
        }
        ThisCapsuleCollider = gameObject.GetComponent<CapsuleCollider>();
        ThisObjLayer = 1 << gameObject.layer;
        localPlayer = Networking.LocalPlayer;
        if (localPlayer == null) { InEditor = true; }
    }
    public void SendUpdateTimes()
    { CurrentCourse.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "UpdateTimes"); }
    public void SetTimerEmpty()
    { TimeText_Cockpit.text = string.Empty; }
    void Start()
    { SetTimerEmpty(); }
    private void Update()
    {
        if (RaceOn)
        {
            RaceTime = Time.time - RaceStartTime;
            TimeText_Cockpit.text = RaceTime.ToString();
        }
    }
    private void FixedUpdate()
    {
        if (DoSubFrameTimeCheck)
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
                //Debug.Log("racetrigger hit");
                //Debug.Log(string.Concat("dist: ", hit.distance));
            }
            /*             else
                        {
                            Debug.Log("racetrigger miss");
                        } */
            ThisCapsuleCollider.enabled = true;
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (CurrentCourseSelection != -1 && (other && other.gameObject == CurrentCourse.RaceCheckpoints[NextCheckpoint]))
        {
            if (NextCheckpoint == FinalCheckpoint)//end of the race
            {
                RaceTime = 0;
                float subframetime = 0;
                //get world space position of the point that was closest to the checkpoint the frame before the race was finished
                Vector3 LastFrameClosestPosThisFrame = transform.TransformPoint(PlaneClosestPosInverseFromPlane);
                //get the speed of the plane by comparing the two
                float speed = Vector3.Distance(PlaneClosestPosLastFrame, LastFrameClosestPosThisFrame);
                //check if the plane is travelling further per frame than the distance to the checkpoint from the last frame, just incase the raycast somehow missed
                //(only do subframe time if it'll be valid)
                if (speed > PlaneDistanceFromCheckPointLastFrame)
                {
                    //get the amount of time we need to remove from current time to make it sub-frame accurate
                    float passedratio = PlaneDistanceFromCheckPointLastFrame / speed;
                    subframetime = -Time.fixedDeltaTime * passedratio + Time.fixedDeltaTime;
                }

                //Debug.Log("Finish Race!");
                RaceTime = LastTime = (Time.time - RaceStartTime - subframetime);
                RaceOn = false;
                NextCheckpoint = FirstCheckPoint;

                if (Utilities.IsValid(CurrentCheckPointAnimator))
                { CurrentCheckPointAnimator.SetBool("Current", false); }
                StartCheckPointAnims();



                CurrentCourse.MyVehicleType = PlaneName;
                if (TrackForward || !CurrentTrackAllowReverse)
                {
                    CurrentCourse.MyTime = RaceTime;
                    CurrentCourse.UpdateMyLastTime();
                    if (RaceTime < CurrentCourse.MyRecordTime)
                    {
                        CurrentCourse.MyRecordTime = CurrentCourse.MyTime = RaceTime;
                        CurrentCourse.UpdateMyRecord();

                        if (!CheckRecordDisallowedRace() && RaceTime < CurrentCourse.BestTime)
                        {
                            if (!InEditor && !localPlayer.IsOwner(CurrentCourse.gameObject))
                            {
                                Networking.SetOwner(localPlayer, CurrentCourse.gameObject);
                            }
                            CurrentCourse.BestTime = RaceTime;
                            CurrentCourse.UpdateInstanceRecord();
                            CurrentCourse.RequestSerialization();
                        }
                    }
                }
                else
                {
                    CurrentCourse.MyTimeReverse = RaceTime;
                    CurrentCourse.UpdateMyLastTime();
                    if (RaceTime < CurrentCourse.MyRecordTimeReverse)
                    {
                        CurrentCourse.MyRecordTimeReverse = CurrentCourse.MyTimeReverse = RaceTime;
                        CurrentCourse.UpdateMyRecord();

                        if (!CheckRecordDisallowedRace() && RaceTime < CurrentCourse.BestTimeReverse)
                        {
                            if (!InEditor && !localPlayer.IsOwner(CurrentCourse.gameObject))
                            {
                                Networking.SetOwner(localPlayer, CurrentCourse.gameObject);
                            }
                            CurrentCourse.BestTimeReverse = RaceTime;
                            CurrentCourse.UpdateInstanceRecordReverse();
                            CurrentCourse.RequestSerialization();
                        }
                    }
                }


                RaceTime = 0;
                TimeText_Cockpit.text = LastTime.ToString();
                SendCustomEventDelayedSeconds(nameof(SetTimerEmpty), 2);
            }
            else if (NextCheckpoint == FirstCheckPoint)//starting the race
            {
                //subframe accuracy is done on the first and last checkpoint, the code for the last checkpoint(above) is commented
                RaceTime = 0;
                float subframetime = 0;
                Vector3 LastFrameClosestPosThisFrame = transform.TransformPoint(PlaneClosestPosInverseFromPlane);
                float speed = Vector3.Distance(PlaneClosestPosLastFrame, LastFrameClosestPosThisFrame);
                if (speed > PlaneDistanceFromCheckPointLastFrame)
                {
                    float passedratio = PlaneDistanceFromCheckPointLastFrame / speed;
                    subframetime = -Time.fixedDeltaTime * passedratio + Time.fixedDeltaTime;
                }

                //Debug.Log("Start Race!");
                RaceStartTime = Time.time - subframetime;
                RaceOn = true;
                NextCheckpoint += TrackDirection;
                ProgressCheckPointAnims();
                if (NextCheckpoint == FinalCheckpoint)
                {
                    DoSubFrameTimeCheck = true;//in case the course only has 2 checkpoints
                }
                else
                {
                    DoSubFrameTimeCheck = false;//we don't need to do subframe times for middle checkpoints
                }
            }
            else
            {
                //Debug.Log("CheckPoint!");
                NextCheckpoint += TrackDirection;
                ProgressCheckPointAnims();

                //check if the next checkpoint is the end of the race, because if it is we need to get subframe time
                if (NextCheckpoint == FinalCheckpoint)
                {
                    DoSubFrameTimeCheck = true;
                }
            }
        }
    }
    public void SetCourse()
    {
        CurrentCourseSelection = Button.CurrentCourseSelection;
        if (CurrentCourseSelection != -1)
        {
            DoSubFrameTimeCheck = true;
            CurrentCourse = Button.Races[CurrentCourseSelection].GetComponent<SaccRaceCourseAndScoreboard>();
            CurrentTrackAllowReverse = CurrentCourse.AllowReverse;
            if (TrackForward || !CurrentTrackAllowReverse)
            {
                NextCheckpoint = FirstCheckPoint = 0;
                FinalCheckpoint = CurrentCourse.RaceCheckpoints.Length - 1;
                TrackDirection = 1;
            }
            else
            {
                NextCheckpoint = FirstCheckPoint = CurrentCourse.RaceCheckpoints.Length - 1;
                FinalCheckpoint = 0;
                TrackDirection = -1;
            }
            if (InEditor || gameObject.activeInHierarchy) { StartCheckPointAnims(); }//don't turn on lights when switching course unless in editor for testing, or pressing while in a vehicle
        }
        else
        {
            DoSubFrameTimeCheck = false;
        }
    }
    public void SetUpNewRace()
    {
        if (!Initialized) { Initialize(); }
        RaceOn = false;
        RaceTime = 0;
        if (CheckDisallowedRace())
        {
            CurrentCourseSelection = -1;
            return;
        }
        SetCourse();
    }
    void OnEnable()//Happens when you get in a plane, if a race is enabled
    {
        SetUpNewRace();
        StartCheckPointAnims();
        SetTimerEmpty();
    }
    void OnDisable()
    {
        if (CurrentCourse)
        {
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
        if (next2 > -1 && next2 < CurrentCourse.RaceCheckpoints.Length)
        {
            NextCheckPointAnimator = CurrentCourse.RaceCheckpoints[NextCheckpoint + TrackDirection].GetComponent<Animator>();
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
                }
            }
            if (CurrentCourse.RaceCheckpoints.Length > 1)
            {
                NextCheckPointAnimator = CurrentCourse.RaceCheckpoints[FirstCheckPoint + TrackDirection].GetComponent<Animator>();
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
        int ccs = Button.CurrentCourseSelection;
        if (ccs == -1) { return true; }
        foreach (GameObject race in DisabledRaces)
        {
            if (race == Button.Races[ccs].gameObject)
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
            if (race == Button.Races[CurrentCourseSelection].gameObject)
            {
                return true;
            }
        }
        return false;
    }
}
