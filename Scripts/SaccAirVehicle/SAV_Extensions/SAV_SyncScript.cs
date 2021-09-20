
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class SAV_SyncScript : UdonSharpBehaviour
{
    // whispers to Zwei, "it's okay"
    [SerializeField] private UdonSharpBehaviour SAVControl;
    [SerializeField] private Transform VehicleTransform;
    [Tooltip("In seconds")]
    [Range(0.05f, 1f)]
    [SerializeField] private float updateInterval = 0.1f;
    [Tooltip("Should never be more than update interval")]
    [Range(0.01f, 1f)]
    [SerializeField] private float IdleMaxUpdateDelay = 3f;
    private VRCPlayerApi localPlayer;
    private float nextUpdateTime = 0;
    //[UdonSynced(UdonSyncMode.None)] private int O_UpdateTime;
    private int StartupTimeMS = 0;
    private double StartupTime;
    [UdonSynced(UdonSyncMode.None)] private double O_UpdateTime;
    [UdonSynced(UdonSyncMode.None)] private Vector3 O_Position = Vector3.zero;
    [UdonSynced(UdonSyncMode.None)] private short O_RotationX;
    [UdonSynced(UdonSyncMode.None)] private short O_RotationY;
    [UdonSynced(UdonSyncMode.None)] private short O_RotationZ;
    [UdonSynced(UdonSyncMode.None)] private Vector3 O_CurVel = Vector3.zero;
    private Vector3 O_Rotation;
    private Quaternion O_Rotation_Q = Quaternion.identity;
    private Vector3 O_LastCurVel = Vector3.zero;
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
    //make everyone think they're the owner for the first frame so that don't set the position to 0,0,0 before SFEXT_L_EntityStart runs
    private bool IsOwner = true;
    private Vector3 ExtrapolationDirection;
    private Vector3 LastExtrapolationDirection;
    private Vector3 L_PingAdjustedPosition;
    private Vector3 L_LastPingAdjustedPosition;
    private Vector3 lerpedCurVel;
    private Vector3 Acceleration;
    private Vector3 LastAcceleration;
    private Vector3 O_LastPosition;
    private float SmoothingTimeDivider;
    private float UpdateTime;
    private int UpdatesSentWhileStill;
    private Rigidbody VehicleRigid;
    private void Start()
    {
        if (StartupTimeMS == 0)//shouldn't be active until entitystart
        { gameObject.SetActive(false); }
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
        SmoothingTimeDivider = 1f / updateInterval;
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
        L_LastPingAdjustedPosition = L_PingAdjustedPosition = O_Position;
        O_LastRotation2 = O_LastRotation = O_Rotation_Q = VehicleTransform.rotation;

    }
    private void Update()
    {
        if (IsOwner)//send data
        {
            if (Time.time > nextUpdateTime)
            {
                if (!Networking.IsClogged || (bool)SAVControl.GetProgramVariable("Piloting"))
                {
                    bool Still = ((VehicleTransform.position - O_Position).magnitude < .35f * updateInterval) && Quaternion.Angle(VehicleTransform.rotation, O_Rotation_Q) < 5f * updateInterval;
                    if (!Still || UpdatesSentWhileStill < 2 || (Time.time - UpdateTime > IdleMaxUpdateDelay))
                    {
                        if (Still) { UpdatesSentWhileStill++; }
                        else { UpdatesSentWhileStill = 0; }
                        O_Position = VehicleTransform.position;
                        Vector3 rot = VehicleTransform.rotation.eulerAngles;

                        rot = new Vector3(rot.x > 180 ? rot.x - 360 : rot.x,
                         rot.y > 180 ? rot.y - 360 : rot.y,
                          rot.z > 180 ? rot.z - 360 : rot.z)
                          * 182.0444444444444f;//convert 360 to 65536

                        //this shouldn't need clamping but for some reason it does
                        O_RotationX = (short)Mathf.Clamp(rot.x, short.MinValue, short.MaxValue);
                        O_RotationY = (short)Mathf.Clamp(rot.y, short.MinValue, short.MaxValue);
                        O_RotationZ = (short)Mathf.Clamp(rot.z, short.MinValue, short.MaxValue);

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
            Vector3 PredictedPosition = L_PingAdjustedPosition
                 + ((ExtrapolationDirection) * TimeSinceUpdate);
            Quaternion PredictedRotation =
                (Quaternion.SlerpUnclamped(Quaternion.identity, CurAngMom, Ping + TimeSinceUpdate)
                * O_Rotation_Q);
            if (TimeSinceUpdate < updateInterval)
            {
                float TimeSincePreviousUpdate = (float)((((double)StartupTimeMS * .001f) + ((double)Time.realtimeSinceStartup - StartupTime)) - L_LastUpdateTime);
                Vector3 OldPredictedPosition = L_LastPingAdjustedPosition
                    + ((LastExtrapolationDirection) * TimeSincePreviousUpdate);

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
        if (O_UpdateTime != O_LastUpdateTime && !IsOwner)//only do anything if OnDeserialization was for this script
        {
            LastAcceleration = Acceleration;
            LastPing = Ping;
            L_LastUpdateTime = L_UpdateTime;
            LastCurAngMom = CurAngMom;
            float updatedelta = (float)(O_UpdateTime - O_LastUpdateTime);
            float speednormalizer = 1 / updatedelta;

            L_UpdateTime = ((double)StartupTimeMS * .001f) + ((double)Time.realtimeSinceStartup - StartupTime);
            Ping = (float)(L_UpdateTime - O_UpdateTime);
            //Curvel is 0 when launching from a catapult because it doesn't use rigidbody physics, so do it based on position
            Vector3 CurrentVelocity;
            if (O_CurVel.sqrMagnitude == 0)
            { CurrentVelocity = (O_Position - O_LastPosition) * speednormalizer; }
            else
            { CurrentVelocity = O_CurVel; }
            Acceleration = (CurrentVelocity - O_LastCurVel) * (1 + Ping);
            if (Vector3.Dot(Acceleration, LastAcceleration) < 0)//if direction of acceleration changed by more than 180 degrees, just set zero to prevent bounce effect, the vehicle likely just crashed into a wall.
            { Acceleration = Vector3.zero; }

            O_Rotation = new Vector3(O_RotationX, O_RotationY, O_RotationZ) * .0054931640625f;//65536 to 360

            O_Rotation_Q = Quaternion.Euler(O_Rotation);
            //rotate Acceleration by the difference in rotation of plane plane between last and this update to make it match the angle for the next frame better
            Quaternion PlaneRotDif = O_Rotation_Q * Quaternion.Inverse(O_LastRotation);
            Acceleration = PlaneRotDif * Acceleration;

            CurAngMom = Quaternion.SlerpUnclamped(Quaternion.identity, PlaneRotDif, speednormalizer);
            SAVControl.SetProgramVariable("CurrentVel", CurrentVelocity);

            L_LastPingAdjustedPosition = L_PingAdjustedPosition;
            L_PingAdjustedPosition = O_Position + ((CurrentVelocity + Acceleration) * Ping);

            LastExtrapolationDirection = ExtrapolationDirection;
            ExtrapolationDirection = CurrentVelocity + Acceleration;
            O_LastRotation2 = O_LastRotation;

            O_LastUpdateTime = O_UpdateTime;
            O_LastRotation = O_Rotation_Q;
            O_LastPosition = O_Position;
            O_LastCurVel = CurrentVelocity;
        }
    }
}