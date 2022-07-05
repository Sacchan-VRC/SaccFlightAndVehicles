
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SAAG_SyncScript : UdonSharpBehaviour
    {
        // whispers to Zwei, "it's okay"
        public SaccAAGunController AAGunControl;
        public Transform Rotator;
        [Tooltip("In seconds")]
        [Range(0.05f, 1f)]
        public float updateInterval = 0.25f;
        private VRCPlayerApi localPlayer;
        private float nextUpdateTime = 0;
        private double StartupTime;
        [UdonSynced(UdonSyncMode.None)] private Vector2 O_GunRotation;
        [UdonSynced(UdonSyncMode.None)] private int O_UpdateTime = 0;
        private Vector3 O_Rotation;
        private Quaternion O_Rotation_Q = Quaternion.identity;
        private Vector3 O_LastCurVel = Vector3.zero;
        private Quaternion CurAngMom = Quaternion.identity;
        private Quaternion LastCurAngMom = Quaternion.identity;
        private Quaternion RotationLerper = Quaternion.identity;
        private int StartupTimeMS = 0;
        private int O_LastUpdateTime;
        private int L_UpdateTime;
        private int L_LastUpdateTime;
        private float LastPing;
        private float Ping;
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
        private bool Initialized = false;
        private Vector2 LastGunRotationSpeed;
        private Vector2 GunRotationSpeed;
        private Vector2 O_LastGunRotation2;
        private Vector2 O_LastGunRotation;
        private int O_LastUpdateTime2;
        //private Vector3 NonOwnerRotLerper;
        private float UpAngleMax = 89;
        private float DownAngleMax = 35;
        private Quaternion RotatorStartRot;
        private void Start()
        {
            if (!Initialized)//shouldn't be active until entitystart
            { gameObject.SetActive(false); }
        }
        public void SFEXT_L_EntityStart()
        {
            Initialized = true;
            localPlayer = Networking.LocalPlayer;
            bool InEditor = localPlayer == null;
            if (!InEditor && localPlayer.isMaster)
            { IsOwner = true; }
            else if (!InEditor) { IsOwner = false; }//late joiner
            else { IsOwner = true; }//play mode in editor
            nextUpdateTime = Time.time + Random.Range(0f, updateInterval);
            SmoothingTimeDivider = 1f / updateInterval;
            StartupTimeMS = Networking.GetServerTimeInMilliseconds();
            gameObject.SetActive(false);
            UpAngleMax = (float)AAGunControl.GetProgramVariable("UpAngleMax");
            DownAngleMax = (float)AAGunControl.GetProgramVariable("DownAngleMax");
            RotatorStartRot = Rotator.localRotation;
        }
        public void SFEXT_G_ReAppear()
        {
            Rotator.localRotation = RotatorStartRot;
        }
        public void SFEXT_O_TakeOwnership()
        {
            IsOwner = true;
        }
        public void SFEXT_O_LoseOwnership()
        {
            IsOwner = false;
        }
        public void SFEXT_G_Explode()
        {
            O_LastGunRotation2 = O_LastGunRotation = Vector2.zero;
            GunRotationSpeed = LastGunRotationSpeed = Vector2.zero;
        }
        public void SFEXT_G_PilotEnter()
        { gameObject.SetActive(true); }
        public void SFEXT_G_PilotExit()
        {
            gameObject.SetActive(false);
            GunRotationSpeed = LastGunRotationSpeed = Vector2.zero;
        }
        private void Update()
        {
            if (IsOwner)//send data
            {
                if (Time.time > nextUpdateTime)
                {
                    O_UpdateTime = Networking.GetServerTimeInMilliseconds();
                    O_GunRotation = new Vector2(Rotator.localEulerAngles.x, Rotator.localEulerAngles.y);
                    RequestSerialization();
                    nextUpdateTime = Time.time + updateInterval;
                }
            }
            else
            {
                float TimeSinceUpdate = ((float)(Networking.GetServerTimeInMilliseconds() - L_UpdateTime) * .001f);
                Vector2 PredictedRotation = O_GunRotation + (GunRotationSpeed * (Ping + TimeSinceUpdate));
                PredictedRotation.x = Mathf.Clamp(PredictedRotation.x, -UpAngleMax, DownAngleMax);

                Vector3 PredictedRotation_3 = new Vector3(PredictedRotation.x, PredictedRotation.y, 0);

                if (TimeSinceUpdate < updateInterval)
                {
                    float TimeSincePreviousUpdate = ((float)(Networking.GetServerTimeInMilliseconds() - L_LastUpdateTime) * .001f);

                    Vector2 OldPredictedRotation = O_LastGunRotation2 + (LastGunRotationSpeed * (LastPing + TimeSincePreviousUpdate));
                    OldPredictedRotation.x = Mathf.Clamp(OldPredictedRotation.x, -UpAngleMax, DownAngleMax);

                    Vector3 OldPredictedRotation_3 = new Vector3(OldPredictedRotation.x, OldPredictedRotation.y, 0);

                    Vector3 TargetRot = Vector3.Lerp(OldPredictedRotation_3, PredictedRotation_3, TimeSinceUpdate * SmoothingTimeDivider);
                    // NonOwnerRotLerper = Vector3.Lerp(NonOwnerRotLerper, TargetRot, Time.smoothDeltaTime * 10);
                    Rotator.localRotation = Quaternion.Euler(TargetRot);
                }
                else
                {
                    // NonOwnerRotLerper = Vector3.Lerp(NonOwnerRotLerper, PredictedRotation_3, Time.smoothDeltaTime * 10);
                    Rotator.localRotation = Quaternion.Euler(PredictedRotation_3);
                }
            }
        }
        public override void OnDeserialization()
        {
            if (O_UpdateTime != O_LastUpdateTime)//only do anything if OnDeserialization was for this script
            {
                if (O_GunRotation.x > 180) { O_GunRotation.x -= 360; }
                LastPing = Ping;
                L_LastUpdateTime = L_UpdateTime;
                float updatedelta = (O_UpdateTime - O_LastUpdateTime) * .001f;
                float speednormalizer = 1 / updatedelta;

                L_UpdateTime = Networking.GetServerTimeInMilliseconds();
                Ping = (L_UpdateTime - O_UpdateTime) * .001f;
                LastGunRotationSpeed = GunRotationSpeed;

                //check if going from rotation 0->360 and fix values for interpolation
                if (Mathf.Abs(O_GunRotation.y - O_LastGunRotation.y) > 180)
                {
                    if (O_GunRotation.y > O_LastGunRotation.y)
                    {
                        O_LastGunRotation.y += 360;
                    }
                    else
                    {
                        O_LastGunRotation.y -= 360;
                    }
                }
                GunRotationSpeed = (O_GunRotation - O_LastGunRotation) * speednormalizer;
                O_LastGunRotation2 = O_LastGunRotation;
                O_LastGunRotation = O_GunRotation;
                O_LastUpdateTime = O_UpdateTime;
            }
        }
    }
}