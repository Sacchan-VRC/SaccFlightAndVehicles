
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class SaccSyncScript : UdonSharpBehaviour
{
    // whispers to Zwei, "it's okay"
    [SerializeField] private UdonSharpBehaviour SAVControl;
    [SerializeField] private Transform VehicleTransform;
    [Tooltip("In seconds")]
    [Range(0.05f, 1f)]
    [SerializeField] private float updateInterval = 0.1f;
    [Tooltip("Should never be more than update interval")]
    [Range(0.01f, 1f)]
    [SerializeField] private float SmoothingTime = 0.1f;
    [SerializeField] private float IdleMaxUpdateDelay = 3f;
    private VRCPlayerApi localPlayer;
    private float nextUpdateTime = 0;
    //[UdonSynced(UdonSyncMode.None)] private int O_UpdateTime;
    private int StartupTimeMS;
    private double StartupTime;
    [UdonSynced(UdonSyncMode.None)] private double O_UpdateTime;
    [UdonSynced(UdonSyncMode.None)] private Vector3 O_Position = Vector3.zero;
    [UdonSynced(UdonSyncMode.None)] private ushort O_RotationX;
    [UdonSynced(UdonSyncMode.None)] private ushort O_RotationY;
    [UdonSynced(UdonSyncMode.None)] private ushort O_RotationZ;
    [UdonSynced(UdonSyncMode.None)] private Vector3 O_CurVel = Vector3.zero;
    private Vector3 O_Rotation;
    private Quaternion O_Rotation_Q = Quaternion.identity;
    private Vector3 O_LastCurVel = Vector3.zero;
    private Vector3 O_LastCurVel2 = Vector3.zero;
    private Quaternion CurAngMom = Quaternion.identity;
    private Quaternion LastCurAngMom = Quaternion.identity;
    private Quaternion O_LastRotation = Quaternion.identity;
    private Quaternion O_LastRotation2 = Quaternion.identity;
    private Quaternion RotationLerper = Quaternion.identity;
    private float Ping;
    private float LastPing;
    private double L_UpdateTime;
    private double L_LastUpdateTime;
    private double O_LastUpdateTime;
    private double O_LastUpdateTime2;
    //make everyone think they're the owner for the first frame so that don't set the position to 0,0,0 before SFEXT_L_EntityStart runs
    private bool IsOwner = true;
    private Vector3 lerpedCurVel;
    private Vector3 Acceleration;
    private Vector3 LastAcceleration;
    private Vector3 O_LastPosition;
    private Vector3 O_LastPosition2;
    private float SmoothingTimeDivider;
    private float UpdateTime;

    private void Start()
    {
        gameObject.SetActive(false);
    }
    public void SFEXT_L_EntityStart()
    {
        localPlayer = Networking.LocalPlayer;
        bool InEditor = localPlayer == null;
        if (!InEditor && localPlayer.isInstanceOwner)
        { IsOwner = true; }
        else if (!InEditor) { IsOwner = false; }
        else { IsOwner = true; }//play mode in editor
        nextUpdateTime = Time.time + Random.Range(0f, updateInterval);

        SmoothingTimeDivider = 1f / (float)SmoothingTime;
        StartupTimeMS = Networking.GetServerTimeInMilliseconds();
        StartupTime = Time.realtimeSinceStartup;
        gameObject.SetActive(true);
    }
    public void SFEXT_O_TakeOwnership()
    {
        IsOwner = true;
    }
    public void SFEXT_O_LoseOwnership()
    {
        IsOwner = false;
    }
    private void Update()
    {
        if (IsOwner)//send data
        {
            if (Time.time > nextUpdateTime)
            {
                if (!Networking.IsClogged || (bool)SAVControl.GetProgramVariable("Piloting"))
                {
                    if ((Time.time - UpdateTime > IdleMaxUpdateDelay) || ((VehicleTransform.position - O_Position).magnitude > .35f * updateInterval) || Quaternion.Angle(VehicleTransform.rotation, O_Rotation_Q) > 5f * updateInterval)
                    {
                        O_Position = VehicleTransform.position;
                        O_Rotation_Q = VehicleTransform.rotation;
                        Vector3 rot = VehicleTransform.rotation.eulerAngles * 182.0444444444444f;//convert 360 to 65536
                        O_RotationX = (ushort)Mathf.Floor(Mathf.Max(0, rot.x));//i don't know why but it can crash thinking it's below 0 so Max to 0
                        O_RotationY = (ushort)Mathf.Floor(Mathf.Max(0, rot.y));
                        O_RotationZ = (ushort)Mathf.Floor(Mathf.Max(0, rot.z));
                        O_CurVel = (Vector3)SAVControl.GetProgramVariable("CurrentVel");
                        O_UpdateTime = ((double)StartupTimeMS * .001f) + ((double)Time.realtimeSinceStartup - StartupTime);
                        RequestSerialization();
                        UpdateTime = Time.time;
                    }
                }
                nextUpdateTime = (Time.time + updateInterval);
            }
        }
        else//extrapolate and interpolate based on data
        {
            float TimeSinceUpdate = (float)((((double)StartupTimeMS * .001f) + ((double)Time.realtimeSinceStartup - StartupTime)) - L_UpdateTime);
            Vector3 PredictedPosition = O_Position
                 + ((O_CurVel + Acceleration) * Ping)
                 + ((O_CurVel + Acceleration) * TimeSinceUpdate);
            Quaternion PredictedRotation =
                (Quaternion.SlerpUnclamped(Quaternion.identity, CurAngMom, Ping + TimeSinceUpdate)
                * O_Rotation_Q);
            if (TimeSinceUpdate < updateInterval)
            {
                float TimeSincePreviousUpdate = (float)((((double)StartupTimeMS * .001f) + ((double)Time.realtimeSinceStartup - StartupTime)) - L_LastUpdateTime);
                Vector3 OldPredictedPosition = O_LastPosition2
                     + ((O_LastCurVel2 + LastAcceleration) * LastPing)
                     + ((O_LastCurVel2 + LastAcceleration) * TimeSincePreviousUpdate);

                Quaternion OldPredictedRotation =
                (Quaternion.SlerpUnclamped(Quaternion.identity, LastCurAngMom, LastPing + TimeSincePreviousUpdate)
                * O_LastRotation2);
                RotationLerper = Quaternion.Slerp(RotationLerper,
                 Quaternion.Slerp(OldPredictedRotation, PredictedRotation, TimeSinceUpdate * SmoothingTimeDivider),
                  Time.smoothDeltaTime * SmoothingTimeDivider);

                VehicleTransform.SetPositionAndRotation(
                    Vector3.Lerp(OldPredictedPosition, PredictedPosition, (float)TimeSinceUpdate * SmoothingTimeDivider),
                     RotationLerper);
            }
            else
            {
                RotationLerper = Quaternion.Slerp(RotationLerper, PredictedRotation, Time.smoothDeltaTime * SmoothingTimeDivider);
                VehicleTransform.SetPositionAndRotation(PredictedPosition, RotationLerper);
            }
        }
    }
    public override void OnDeserialization()
    {
        if (O_UpdateTime != O_LastUpdateTime)//only do anything if OnDeserialization was for this script
        {
            LastAcceleration = Acceleration;
            LastPing = Ping;
            L_LastUpdateTime = L_UpdateTime;
            LastCurAngMom = CurAngMom;
            float updatedelta = (float)(O_UpdateTime - O_LastUpdateTime) * .001f;
            float speednormalizer = 1 / updatedelta;

            L_UpdateTime = ((double)StartupTimeMS * .001f) + ((double)Time.realtimeSinceStartup - StartupTime);
            Ping = (float)(L_UpdateTime - O_UpdateTime);
            Acceleration = (O_CurVel - O_LastCurVel) * (1 + Ping);
            if (Vector3.Dot(Acceleration, LastAcceleration) < 0)//if direction of acceleration changed by more than 180 degrees, just set zero to prevent bounce effect, the vehicle likely just crashed into a wall.
            { Acceleration = Vector3.zero; }

            Vector3 recievedRotation = new Vector3(O_RotationX, O_RotationY, O_RotationZ) * .0054931640625f;//65536 to 360

            O_Rotation = new Vector3(recievedRotation.x > 180 ? recievedRotation.x - 360 : recievedRotation.x,
             recievedRotation.y > 180 ? recievedRotation.y - 360 : recievedRotation.y,
              recievedRotation.z > 180 ? recievedRotation.z - 360 : recievedRotation.z);

            O_Rotation_Q = Quaternion.Euler(O_Rotation);
            //rotate Acceleration by the difference in rotation of plane plane between last and this update to make it match the angle for the next frame better
            Quaternion PlaneRotDif = O_Rotation_Q * Quaternion.Inverse(O_LastRotation);
            Acceleration = PlaneRotDif * Acceleration;

            CurAngMom = Quaternion.SlerpUnclamped(Quaternion.identity, PlaneRotDif, speednormalizer);
            SAVControl.SetProgramVariable("CurrentVel", O_CurVel);

            O_LastUpdateTime2 = O_LastUpdateTime;
            O_LastRotation2 = O_LastRotation;
            O_LastPosition2 = O_LastPosition;
            O_LastCurVel2 = O_LastCurVel;

            O_LastUpdateTime = O_UpdateTime;
            O_LastRotation = O_Rotation_Q;
            O_LastPosition = O_Position;
            O_LastCurVel = O_CurVel;
        }
    }
}