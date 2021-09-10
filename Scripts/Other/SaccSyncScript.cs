
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SaccSyncScript : UdonSharpBehaviour
{
    // whispers to Zwei, "it's okay"
    [SerializeField] private SaccAirVehicle SAVControl;
    [SerializeField] private Transform VehicleTransform;
    [Tooltip("In seconds")]
    [Range(0.05f, 1f)]
    [SerializeField] private float updateInterval = 0.1f;
    [Range(0.01f, 1f)]
    [SerializeField] private float lerpSharpness = 0.1f;
    [SerializeField] private float lerpVelSharpness = .1f;
    private VRCPlayerApi localPlayer;
    private float nextUpdateTime = 0;
    [UdonSynced(UdonSyncMode.None)] private int O_UpdateTime;
    [UdonSynced(UdonSyncMode.None)] private Vector3 O_position = Vector3.zero;
    [UdonSynced(UdonSyncMode.None)] private Quaternion O_rotation = Quaternion.identity;
    [UdonSynced(UdonSyncMode.None)] private Vector3 O_CurVel = Vector3.zero;
    private Vector3 O_LastCurVel = Vector3.zero;
    private Quaternion CurAngMom = Quaternion.identity;
    private Quaternion O_LastRotation = Quaternion.identity;
    private float Ping;
    private int L_LastUpdateTime;
    private int O_LastUpdateTime;
    //make everyone think they're the owner for the first frame so that don't set the position to 0,0,0 before SFEXT_L_EntityStart runs
    private bool IsOwner = true;
    private Vector3 lerpedCurVel;
    private Vector3 Acceleration;
    private Vector3 O_LastPosition;


    private void Start()
    {
        gameObject.SetActive(false);
    }
    public void SFEXT_L_EntityStart()
    {
        localPlayer = Networking.LocalPlayer;

        if (localPlayer != null && localPlayer.isMaster)
        { IsOwner = true; }
        else { IsOwner = false; }
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
        if (IsOwner)
        {
            if (Time.time > nextUpdateTime)
            {
                O_position = VehicleTransform.position;
                O_rotation = VehicleTransform.rotation;
                O_CurVel = SAVControl.CurrentVel;
                O_UpdateTime = Networking.GetServerTimeInMilliseconds();
                RequestSerialization();
                nextUpdateTime = (Time.time + updateInterval);
            }
        }
        else
        {
            float TimeSinceUpdate = ((float)(Networking.GetServerTimeInMilliseconds() - L_LastUpdateTime) * .001f);
            Vector3 PredictedPosition = O_position
                 + ((O_CurVel + Acceleration) * Ping)
                 + ((O_CurVel + Acceleration) * TimeSinceUpdate);
            //+ (CurSped * (Time.smoothDeltaTime * 3));
            //mPosLerper = Vector3.Lerp(mPosLerper, position, lerpSharpness);

            Quaternion PredictedRotation =
            (Quaternion.SlerpUnclamped(Quaternion.identity, CurAngMom, Ping + TimeSinceUpdate)
            * O_rotation);

            VehicleTransform.SetPositionAndRotation(PredictedPosition, PredictedRotation);
        }
    }
    public override void OnDeserialization()
    {
        if (O_UpdateTime != O_LastUpdateTime)//only do anything if OnDeserialization was for this script
        {
            float updatedelta = (O_UpdateTime - O_LastUpdateTime) * .001f;
            float speednormalizer = 1 / updatedelta;

            L_LastUpdateTime = Networking.GetServerTimeInMilliseconds();
            Ping = (float)(L_LastUpdateTime - O_UpdateTime) * .001f;
            Acceleration = O_CurVel - O_LastCurVel;
            //Acceleration *= speednormalizer;
            Debug.Log(string.Concat("Acceleration: ", Acceleration.ToString()));

            //rotate Acceleration by the difference in rotation of plane plane between last and this update to make it match the angle for the next frame better
            Quaternion PlaneRotDif = O_rotation * Quaternion.Inverse(O_LastRotation);
            Acceleration = PlaneRotDif * Acceleration;

            CurAngMom = Quaternion.SlerpUnclamped(Quaternion.identity, PlaneRotDif, speednormalizer);

            O_LastUpdateTime = O_UpdateTime;
            O_LastRotation = O_rotation;
            O_LastPosition = O_position;
            O_LastCurVel = O_CurVel;
        }
    }
}