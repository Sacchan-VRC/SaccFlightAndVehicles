
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(10)]
    public class SAV_SyncScript : UdonSharpBehaviour
    {
        // whispers to Zwei, "it's okay"
        public UdonSharpBehaviour SAVControl;
        [Tooltip("Delay between updates in seconds")]
        [Range(0.05f, 1f)]
        public float updateInterval = 0.2f;
        [Tooltip("Delay between updates in seconds when the sync has entered idle mode")]
        public float IdleModeUpdateInterval = 3f;
        [Tooltip("Freeze the vehicle's position when it's dead? Turn off for boats that sink etc")]
        public bool FreezePositionOnDeath = true;
        [Tooltip("How quickly to lerp rotation to new extrapolated target rotation, it might help to reduce this in high-lag situations with planes that can roll quickly")]
        public float RotationSyncAgressiveness = 10f;
        [Tooltip("Multiply velocity vectors recieved while in idle mode, useful for stopping sea vehicles from extrapolating above and below the water")]
        public float IdleModeVelMultiplier = .4f;
        [Tooltip("If vehicle moves less than this distance since it's last update, it'll be considered to be idle, may need to be increased for vehicles that want to be idle on water. If the vehicle floats away sometimes, this value is probably too big")]
        public float IdleMovementRange = .35f;
        [Tooltip("If vehicle rotates less than this many degrees since it's last update, it'll be considered to be idle")]
        public float IdleRotationRange = 5f;
        [Tooltip("Angle Difference between movement direction and rigidbody velocity that will cause the vehicle to teleport instead of interpolate")]
        public float TeleportAngleDifference = 20;
        [Tooltip("Maximum amount of extrapolation for high ping players, will brake formation flying for high ping players if set lower than their ping = 0.1 = 100ms, useful for dogfight worlds")]
        public float MaxPingExtrapolationInSeconds = 999f;
        [Tooltip("Set maximum extrapolation to 0 for passengers to reduce uncomfortable movement, passengers will not see formation flying properly.")]
        public bool PassengerComfortMode;
        private Transform VehicleTransform;
        private double nextUpdateTime = double.MaxValue;
        private int StartupTimeMS = 0;
        private double dblStartupTimeMS = 0;
        private double StartupTime;
        [UdonSynced(UdonSyncMode.None)] private double O_UpdateTime;
        [UdonSynced(UdonSyncMode.None)] private Vector3 O_Position;
        [UdonSynced(UdonSyncMode.None)] private short O_RotationX;
        [UdonSynced(UdonSyncMode.None)] private short O_RotationY;
        [UdonSynced(UdonSyncMode.None)] private short O_RotationZ;
        //sending velocity improves quality but will cause laggy movment if someone has very low fps.
        [UdonSynced(UdonSyncMode.None)] private Vector3 O_CurVel = Vector3.zero;
        private Vector3 O_CurVelLast = Vector3.zero;
        private Vector3 O_Rotation;
        private Quaternion O_Rotation_Q = Quaternion.identity;
        private Vector3 CurrentVelocityLast = Vector3.zero;
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
        private Vector3 LastExtrapolationDirection = Vector3.zero;
        private Vector3 L_PingAdjustedPosition = Vector3.zero;
        private Vector3 L_LastPingAdjustedPosition;
        private Vector3 lerpedCurVel;
        private Vector3 Acceleration = Vector3.zero;
        private Vector3 LastAcceleration;
        private Vector3 O_LastPosition;
        private float SmoothingTimeDivider;
        private double UpdateTime;
        private int UpdatesSentWhileStill;
        private Rigidbody VehicleRigid;
        private bool Initialized = false;
        private bool IdleUpdateMode;
        private bool IdleUpdateMode_Last;
        private bool Piloting;
        private bool Occupied;
        private float CurrentUpdateInterval;
        private int EnterIdleModeNumber;
        private float PrevMaxExtrap;
        System.Diagnostics.Stopwatch StartStopWatch = new System.Diagnostics.Stopwatch();//the most accurate timer AFAIK
        public void SFEXT_L_EntityStart()
        {
            Initialized = true;
            VehicleTransform = ((SaccEntity)SAVControl.GetProgramVariable("EntityControl")).transform;
            VehicleRigid = (Rigidbody)SAVControl.GetProgramVariable("VehicleRigidbody");
            L_LastPingAdjustedPosition = L_PingAdjustedPosition = O_Position = VehicleTransform.position;
            O_LastRotation2 = O_LastRotation = O_Rotation_Q = VehicleTransform.rotation;
            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            bool InEditor = localPlayer == null;
            if (!InEditor)
            {
                if (localPlayer.isMaster)
                {
                    IsOwner = true;
                    VehicleRigid.WakeUp();
                    VehicleRigid.drag = 0;
                    VehicleRigid.angularDrag = 0;
                }
                else
                {
                    IsOwner = false;
                    VehicleRigid.Sleep();
                    VehicleRigid.drag = 9999;
                    VehicleRigid.angularDrag = 9999;
                }
            }
            else
            {//play mode in editor
                IsOwner = true;
                VehicleRigid.WakeUp();
                VehicleRigid.drag = 9999;
                VehicleRigid.angularDrag = 9999;
            }
            SmoothingTimeDivider = 1f / updateInterval;
            StartupTimeMS = Networking.GetServerTimeInMilliseconds();
            dblStartupTimeMS = (double)StartupTimeMS * .001f;
            System.DateTime offsetDateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            StartupTime = (System.DateTime.UtcNow - offsetDateTime).TotalSeconds;
            StartStopWatch.Start();

            CurrentUpdateInterval = updateInterval;
            EnterIdleModeNumber = Mathf.FloorToInt(IdleModeUpdateInterval / updateInterval);//enter idle after IdleModeUpdateInterval seconds of being still
                                                                                            //script is disabled for 5 seconds to make sure nothing moves before everything is initialized
            SendCustomEventDelayedSeconds(nameof(ActivateScript), 5);
        }
        public void ActivateScript()
        {
            gameObject.SetActive(true);
            if (IsOwner)
            { VehicleRigid.constraints = RigidbodyConstraints.None; }
            nextUpdateTime = StartStopWatch.Elapsed.TotalSeconds + (double)Random.Range(0f, updateInterval);
        }
        public void SFEXT_O_TakeOwnership()
        {
            IsOwner = true;
            VehicleRigid.WakeUp();
            VehicleRigid.constraints = RigidbodyConstraints.None;
            VehicleRigid.drag = 0;
            VehicleRigid.angularDrag = 0;
        }
        public void SFEXT_O_LoseOwnership()
        {
            IsOwner = false;
            L_LastPingAdjustedPosition = L_PingAdjustedPosition = O_Position;
            RotationLerper = O_LastRotation2 = O_LastRotation = O_Rotation_Q;
            LastCurAngMom = CurAngMom = Quaternion.identity;
            LastExtrapolationDirection = VehicleRigid.velocity;
            VehicleRigid.Sleep();
            VehicleRigid.constraints = RigidbodyConstraints.FreezePosition;
            VehicleRigid.drag = 9999;
            VehicleRigid.angularDrag = 9999;
            UpdatesSentWhileStill = 0;
            IdleUpdateMode_Last = false;
        }
        public void SFEXT_O_PilotEnter()
        {
            Piloting = true;
            if (IdleUpdateMode) { nextUpdateTime = 0; }
        }
        public void SFEXT_G_PilotEnter()
        {
            if (IdleUpdateMode)
            { ExitIdleMode(); }
            Occupied = true;
        }
        public void SFEXT_G_PilotExit()
        { Occupied = false; }
        public void SFEXT_G_TakeOff()
        { if (IdleUpdateMode) { ExitIdleMode(); } }
        public void SFEXT_L_OwnershipTransfer()
        { if (IdleUpdateMode) { ExitIdleMode(); } }
        public void SFEXT_O_PilotExit()
        { Piloting = false; }
        public void SFEXT_O_RespawnButton()
        {
            nextUpdateTime = 0;
        }
        public void SFEXT_G_RespawnButton()
        {
            ExitIdleMode();
            UpdatesSentWhileStill = 0;
            //make it teleport instead of interpolating
            ExtrapolationDirection = Vector3.zero;
            LastExtrapolationDirection = Vector3.zero;
            VehicleTransform.position = L_LastPingAdjustedPosition = L_PingAdjustedPosition = O_LastPosition = O_Position;
            RotationLerper = VehicleTransform.rotation = O_LastRotation2 = O_LastRotation = O_Rotation_Q;
            CurrentVelocityLast = Vector3.zero;
            LastAcceleration = Acceleration = Vector3.zero;
        }
        private void Update()
        {
            if (IsOwner)//send data
            {
                if (StartStopWatch.Elapsed.TotalSeconds > nextUpdateTime - (Time.deltaTime * .5f))
                {
                    if (!Networking.IsClogged || Piloting)
                    {
                        //check if the vehicle has moved enough from it's last sent location and rotation to bother exiting idle mode
                        bool Still = !Piloting && (((VehicleTransform.position - O_Position).magnitude < IdleMovementRange) && Quaternion.Angle(VehicleTransform.rotation, O_Rotation_Q) < IdleRotationRange);

                        if (Still)
                        {
                            UpdatesSentWhileStill++;
                            if (UpdatesSentWhileStill > EnterIdleModeNumber)
                            { IdleUpdateMode = true; }
                        }
                        else
                        {
                            UpdatesSentWhileStill = 0;
                            IdleUpdateMode = false;
                        }
                        if (IdleUpdateMode)
                        {
                            if (!IdleUpdateMode_Last)
                            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EnterIdleMode)); }
                        }
                        else
                        {
                            if (IdleUpdateMode_Last)
                            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ExitIdleMode)); }
                        }
                        IdleUpdateMode_Last = IdleUpdateMode;
                        //never use rigidbody values for position or rotation because the interpolation/extrapolation from update is needed for it to be smooth
                        O_Position = VehicleTransform.position;
                        O_Rotation_Q = VehicleTransform.rotation;
                        //convert each euler angle to shorts to save bandwidth
                        Vector3 rot = O_Rotation_Q.eulerAngles;
                        rot = new Vector3(rot.x > 180 ? rot.x - 360 : rot.x,
                         rot.y > 180 ? rot.y - 360 : rot.y,
                          rot.z > 180 ? rot.z - 360 : rot.z)
                          * 182.0444444444444f;//convert 0-360 to 0-65536
                                               //this shouldn't need clamping but for some reason it does
                        O_RotationX = (short)Mathf.Clamp(rot.x, short.MinValue, short.MaxValue);
                        O_RotationY = (short)Mathf.Clamp(rot.y, short.MinValue, short.MaxValue);
                        O_RotationZ = (short)Mathf.Clamp(rot.z, short.MinValue, short.MaxValue);

                        O_CurVel = VehicleRigid.velocity;
                        //update time is a double so that it can interact with (int)Networking.GetServerTimeInMilliseconds() without innacuracy
                        //update time is the Networking.GetServerTimeInMilliseconds() taken from SFEXT_L_EntityStart() + real time as float since that to make
                        //the sub-millisecond error constant to eliminate jitter
                        O_UpdateTime = (dblStartupTimeMS) + (StartStopWatch.Elapsed.TotalSeconds);//send servertime of update
                        RequestSerialization();
                        UpdateTime = StartStopWatch.Elapsed.TotalSeconds;
                    }
                    nextUpdateTime = (StartStopWatch.Elapsed.TotalSeconds + (double)(IdleUpdateMode ? IdleModeUpdateInterval : updateInterval));
                }
            }
            else//extrapolate and interpolate based on recieved data
            {
                //Extrapolate position forward by amount that'd make it match the current position on the other client (assuming straight movement)
                //Do this for the last two updates recieved, and interpolate between them over the length of the Update Interval
                //The interpolation should reach 100% the current extrapolaton hopefully at the exact moment the next update is recieved, otherwise continue extrapolating the last update until an update comes

                //time since recieving last update
                float TimeSinceUpdate = (float)((dblStartupTimeMS + (
                        StartStopWatch.Elapsed.TotalSeconds)) - L_UpdateTime);
                //extrapolated position based on time passed since update
                Vector3 PredictedPosition = L_PingAdjustedPosition
                     + (ExtrapolationDirection * TimeSinceUpdate);
                //extrapolated rotation based on time passed since update
                Quaternion PredictedRotation =
                    (Quaternion.SlerpUnclamped(Quaternion.identity, CurAngMom, Ping + TimeSinceUpdate)
                    * O_Rotation_Q);
                //If interpolation hasn't finished, calculate extrapolation of last update
                if (TimeSinceUpdate < CurrentUpdateInterval)
                {
                    //time since recieving previous update
                    float TimeSincePreviousUpdate = (float)((dblStartupTimeMS + (
                            StartStopWatch.Elapsed.TotalSeconds)) - L_LastUpdateTime);
                    //extrapolated position based on data from previous update using time passed since previous update
                    Vector3 OldPredictedPosition = L_LastPingAdjustedPosition
                        + (LastExtrapolationDirection * TimeSincePreviousUpdate);
                    //extrapolated rotation based on data from previous update using time passed since previous update
                    Quaternion OldPredictedRotation =
                        (Quaternion.SlerpUnclamped(Quaternion.identity, LastCurAngMom, LastPing + TimeSincePreviousUpdate)
                        * O_LastRotation2);
                    //Rotation is slerped to smooth it out, because it's not as important as position, and because sudden rotations create large over-predictions anyway
                    //Slerp towards a slerp(interpolation) of last 2 extrapolations
                    RotationLerper = Quaternion.Slerp(RotationLerper,
                     Quaternion.Slerp(OldPredictedRotation, PredictedRotation, TimeSinceUpdate * SmoothingTimeDivider),
                      IdleUpdateMode ? Time.smoothDeltaTime : Time.smoothDeltaTime * RotationSyncAgressiveness);

                    //Set position to a lerp(interpolation) of last 2 extrapolations  
                    //never set position using rigidbody.position because it's 1 frame lagged due to waiting for a physics update before setting
                    VehicleTransform.SetPositionAndRotation(
                        Vector3.Lerp(OldPredictedPosition, PredictedPosition, (float)TimeSinceUpdate * SmoothingTimeDivider),
                           RotationLerper);
                }
                else
                {
                    //interpolation is over, just move position and rotation towards last extrapolation
                    RotationLerper = Quaternion.Slerp(RotationLerper, PredictedRotation, Time.smoothDeltaTime * SmoothingTimeDivider);
                    VehicleTransform.SetPositionAndRotation(PredictedPosition, RotationLerper);
                }
            }
        }
        public void EnterIdleMode()
        {
            if (IdleUpdateMode || Occupied) { return; }
            IdleUpdateMode = true;
            CurrentUpdateInterval = IdleModeUpdateInterval;
            SmoothingTimeDivider = 1f / CurrentUpdateInterval;
            LastExtrapolationDirection *= IdleModeVelMultiplier;
        }
        public void ExitIdleMode()
        {
            if (!IdleUpdateMode) { return; }
            IdleUpdateMode = false;
            CurrentUpdateInterval = updateInterval;
            SmoothingTimeDivider = 1f / CurrentUpdateInterval;
        }
        public void SFEXT_O_OnPlayerJoined()
        {
            if (IdleUpdateMode)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EnterIdleMode)); }
        }
        public override void OnDeserialization()
        {
            if (O_UpdateTime != O_LastUpdateTime && !IsOwner)//only do anything if OnDeserialization was for this script
            {
                LastAcceleration = Acceleration;
                LastPing = Ping;
                L_LastUpdateTime = L_UpdateTime;
                LastCurAngMom = CurAngMom;
                //time between this update and last
                float updatedelta = (float)(O_UpdateTime - O_LastUpdateTime);
                float speednormalizer = 1 / updatedelta;

                //local time update was recieved
                L_UpdateTime = (dblStartupTimeMS) + (StartStopWatch.Elapsed.TotalSeconds);
                //Ping is time between server time update was sent, and the local time the update was recieved
                Ping = Mathf.Min((float)(L_UpdateTime - O_UpdateTime), MaxPingExtrapolationInSeconds);
                //Curvel is 0 when launching from a catapult because it doesn't use rigidbody physics, so do it based on position
                Vector3 CurrentVelocity;
                bool SetVelZero = false;
                if (O_CurVel.sqrMagnitude == 0)
                {
                    if (O_CurVelLast.sqrMagnitude != 0)
                    { CurrentVelocity = Vector3.zero; SetVelZero = true; }
                    else
                    { CurrentVelocity = (O_Position - O_LastPosition) * speednormalizer; }
                }
                else
                { CurrentVelocity = O_CurVel; }
                O_CurVelLast = O_CurVel;
                //if direction of acceleration changed by more than 90 degrees, just set zero to prevent bounce effect, the vehicle likely just crashed into a wall.
                Acceleration = (CurrentVelocity - CurrentVelocityLast);//acceleration is difference in velocity
                if (IdleUpdateMode || Vector3.Dot(Acceleration, LastAcceleration) < 0 || SetVelZero)
                { Acceleration = Vector3.zero; }

                //convert short back to angle (0-65536 to 0-360)
                O_Rotation_Q = Quaternion.Euler(new Vector3(O_RotationX, O_RotationY, O_RotationZ) * .0054931640625f);
                //rotate Acceleration by the difference in rotation of vehicle between last and this update to make it match the angle for the next update better
                Quaternion PlaneRotDif = O_Rotation_Q * Quaternion.Inverse(O_LastRotation);
                Acceleration = PlaneRotDif * Acceleration;

                //current angular momentum as a quaternion
                CurAngMom = Quaternion.SlerpUnclamped(Quaternion.identity, PlaneRotDif, speednormalizer);
                //tell the SaccAirVehicle the velocity value because it doesn't sync it itself
                SAVControl.SetProgramVariable("CurrentVel", CurrentVelocity);

                L_LastPingAdjustedPosition = L_PingAdjustedPosition;
                L_PingAdjustedPosition = O_Position + ((CurrentVelocity + Acceleration) * Ping);

                LastExtrapolationDirection = ExtrapolationDirection;
                ExtrapolationDirection = CurrentVelocity + Acceleration;
                if (IdleUpdateMode) { ExtrapolationDirection *= IdleModeVelMultiplier; }
                O_LastRotation2 = O_LastRotation;//O_LastRotation2 is needed for use in Update() as O_LastRotation is the same as O_Rotation_Q there

                O_LastUpdateTime = O_UpdateTime;
                O_LastRotation = O_Rotation_Q;
                O_LastPosition = O_Position;
                CurrentVelocityLast = CurrentVelocity;

                //float MoveDot = Vector3.Dot(Movement, O_CurVel);
                //if we're going one way but moved the other, we must have teleported.
                //set values to the same thing for Current and Last to make teleportation instead of interpolation
                if (Vector3.Angle(O_Position - O_LastPosition, O_CurVel) > TeleportAngleDifference
                //|| MoveDot > 2 || MoveDot < 0//also teleport if we moved way faster than we should have //disabled because it probably makes very slow framerate people teleport all the time
                )
                {
                    L_LastUpdateTime = L_UpdateTime;
                    L_LastPingAdjustedPosition = L_PingAdjustedPosition;
                    LastExtrapolationDirection = ExtrapolationDirection;
                    LastCurAngMom = CurAngMom;
                    LastPing = Ping;
                    O_LastRotation2 = O_LastRotation = O_Rotation_Q;
                    O_LastPosition = O_Position;
                }
            }
        }
        public void SFEXT_O_Explode()//all the things players see happen when the vehicle explodes
        {
            if (IsOwner && FreezePositionOnDeath)
            {
                VehicleRigid.drag = 9999;
                VehicleRigid.angularDrag = 9999;
            }
        }
        public void SFEXT_G_ReAppear()
        {
            if (IsOwner)
            {
                VehicleRigid.drag = 0;
                VehicleRigid.angularDrag = 0;
            }
        }
        public void SFEXT_O_MoveToSpawn()
        {
            if (IsOwner)
            {
                VehicleRigid.drag = 9999;
                VehicleRigid.angularDrag = 9999;
            }
        }
        public void SFEXT_P_PassengerEnter()
        {
            if (PassengerComfortMode)
            {
                PrevMaxExtrap = MaxPingExtrapolationInSeconds;
                MaxPingExtrapolationInSeconds = 0;
            }
        }
        public void SFEXT_P_PassengerExit()
        {
            if (PassengerComfortMode)
            {
                MaxPingExtrapolationInSeconds = PrevMaxExtrap;
            }
        }
    }
}