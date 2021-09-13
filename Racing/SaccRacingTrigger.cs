
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class SaccRacingTrigger : UdonSharpBehaviour
{
    public string PlaneName;
    public SaccRaceToggleButton Button;
    public GameObject[] DisabledRaces;
    public GameObject[] InstanceRecordDisallowedRaces;
    public Text TimeText_Cockpit;
    private SaccRaceCourseAndScoreboard CurrentCourse;
    private int CurrentCourseSelection = -1;
    private int NextCheckpoint;
    private int FinalCheckpoint;
    private bool RaceOn;
    private float RaceEndCheck = 0;
    private float RaceStartTime = 0;
    private float RaceTime;
    private Animator CurrentCheckPointAnimator;
    private Animator NextCheckPointAnimator;
    private VRCPlayerApi localPlayer;
    private bool InEditor = false;
    private float LastTime;
    private bool FinishedRace = true;
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
        GameObject Objs = gameObject;
        //checking if a rigidbody is null in a while loop doesnt work in udon for some reason, use official vrchat workaround
        while (!Utilities.IsValid(PlaneRigidbody) && Objs.transform.parent != null)
        {
            Objs = Objs.transform.parent.gameObject;
            PlaneRigidbody = Objs.GetComponent<Rigidbody>();
        }
        ThisCapsuleCollider = gameObject.GetComponent<CapsuleCollider>();
        ThisObjLayer = 1 << gameObject.layer;
        localPlayer = Networking.LocalPlayer;
        if (localPlayer == null) { InEditor = true; }
    }
    private void Update()
    {
        if (RaceOn)
        {
            RaceEndCheck = 0;
            RaceTime = Time.time - RaceStartTime;
            TimeText_Cockpit.text = RaceTime.ToString();
        }
        else
        {
            bool TwoSecAfterRace = RaceEndCheck > 2f;
            if (FinishedRace && TwoSecAfterRace)//send the record update event 2 seconds after finishing the race so that the record synced string has time to sync before updating
            {
                FinishedRace = false;
                if (InEditor)
                {
                    CurrentCourse.UpdateTimes();
                }
                else
                {
                    CurrentCourse.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "UpdateTimes");
                }
                TimeText_Cockpit.text = LastTime.ToString();
            }
            else if (!TwoSecAfterRace)
            {
                RaceEndCheck += Time.deltaTime;
                TimeText_Cockpit.text = LastTime.ToString();
            }
            else
            {
                TimeText_Cockpit.text = string.Empty;
            }
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
        if (CurrentCourseSelection != -1 && (other != null && other.gameObject == CurrentCourse.RaceCheckpoints[NextCheckpoint]))
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
                NextCheckpoint = 0;
                FinishedRace = true;

                if (CurrentCheckPointAnimator != null)
                { CurrentCheckPointAnimator.SetBool("Current", false); }
                StartCheckPointAnims();

                CurrentCourse.MyTime = RaceTime;
                CurrentCourse.MyPlaneType = PlaneName;
                CurrentCourse.UpdateMyLastTime();


                if (RaceTime < CurrentCourse.MyRecordTime)
                {
                    CurrentCourse.MyRecordTime = CurrentCourse.MyTime = RaceTime;
                    CurrentCourse.UpdateMyRecord();
                }

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
                RaceTime = 0;
            }
            else if (NextCheckpoint == 0)//starting the race
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
                FinishedRace = false;
                RaceOn = true;
                NextCheckpoint++;
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
                NextCheckpoint++;
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
        if (CurrentCourseSelection != Button.CurrentCourseSelection)
        {

            CurrentCourseSelection = Button.CurrentCourseSelection;
            if (CurrentCourseSelection != -1)
            {
                DoSubFrameTimeCheck = true;
                CurrentCourse = Button.Races[CurrentCourseSelection].GetComponent<SaccRaceCourseAndScoreboard>();
                FinalCheckpoint = CurrentCourse.RaceCheckpoints.Length - 1;
                if (InEditor || gameObject.activeInHierarchy) { StartCheckPointAnims(); }//don't turn on lights when switching course unless in editor for testing, or pressing while in a plane
            }
            else
            {
                DoSubFrameTimeCheck = false;
            }
        }
    }
    public void SetUpNewRace()
    {
        if (!Initialized) { Initialize(); }
        FinishedRace = false;//don't send time on disable unless race finished
        RaceOn = false;
        RaceTime = 0;
        RaceEndCheck = 9999f;
        NextCheckpoint = 0;
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
    }
    void OnDisable()
    {
        if (CurrentCourse != null)
        {
            if (CurrentCourseSelection != -1 && FinishedRace)
            {
                FinishedRace = false;
                if (InEditor)
                {
                    CurrentCourse.UpdateTimes();
                }
                else
                {
                    CurrentCourse.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "UpdateTimes");
                }
            }
            if (CurrentCheckPointAnimator != null)
            {
                CurrentCheckPointAnimator.SetBool("Current", false);
                CurrentCheckPointAnimator.SetTrigger("Reset");
            }
            if (NextCheckPointAnimator != null)
            {
                NextCheckPointAnimator.SetBool("Next", false);
                NextCheckPointAnimator.SetTrigger("Reset");
            }
        }
    }
    void ProgressCheckPointAnims()
    {
        if (CurrentCheckPointAnimator != null)
        { CurrentCheckPointAnimator.SetBool("Current", false); }

        if (NextCheckPointAnimator != null)
        {
            NextCheckPointAnimator.SetBool("Next", false);
            NextCheckPointAnimator.SetBool("Current", true);
        }
        CurrentCheckPointAnimator = NextCheckPointAnimator;
        if (NextCheckpoint + 1 < CurrentCourse.RaceCheckpoints.Length)
        {
            NextCheckPointAnimator = CurrentCourse.RaceCheckpoints[NextCheckpoint + 1].GetComponent<Animator>();
            if (NextCheckPointAnimator != null)
            {
                NextCheckPointAnimator.SetBool("Next", true);
            }
        }
    }
    void StartCheckPointAnims()
    {
        if (CurrentCourseSelection == -1) { return; }
        if (CurrentCourse != null)
        {
            if (CurrentCourse.RaceCheckpoints.Length > 0)
            {
                CurrentCheckPointAnimator = CurrentCourse.RaceCheckpoints[0].GetComponent<Animator>();
                if (CurrentCheckPointAnimator != null)
                {
                    CurrentCheckPointAnimator.SetBool("Current", true);
                }
            }
            if (CurrentCourse.RaceCheckpoints.Length > 1)
            {
                NextCheckPointAnimator = CurrentCourse.RaceCheckpoints[1].GetComponent<Animator>();
                if (NextCheckPointAnimator != null)
                {
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
