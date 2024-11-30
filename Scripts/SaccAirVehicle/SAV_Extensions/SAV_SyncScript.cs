
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
        [Tooltip("If vehicle moves less than this distance since it's last update, it'll be considered to be idle, may need to be increased for vehicles that want to be idle on water. If the vehicle floats away sometimes, this value is probably too big")]
        public float IdleMovementRange = .35f;
        [Tooltip("If vehicle rotates less than this many degrees since it's last update, it'll be considered to be idle")]
        public float IdleRotationRange = 5f;
        [Tooltip("Angle Difference between movement direction and rigidbody velocity that will cause the vehicle to teleport instead of interpolate")]
        public float TeleportAngleDifference = 20;
        [Tooltip("How much vehicle accelerates extra towards its 'raw' position when not owner in order to correct positional errors")]
        public float CorrectionTime = 8f;
        [Tooltip("How quickly non-owned vehicle's velocity vector lerps towards its new value")]
        public float SpeedLerpTime = 4f;
        [Tooltip("Strength of force to stop correction overshooting target")]
        public float CorrectionDStrength = 1.666666f;
        [Tooltip("How much vehicle accelerates extra towards its 'raw' rotation when not owner in order to correct rotational errors")]
        public float CorrectionTime_Rotation = 1f;
        [Tooltip("How quickly non-owned vehicle's rotation slerps towards its new value")]
        public float RotationSpeedLerpTime = 10f;
        [Tooltip("Teleports owned vehicles forward by real time * velocity if frame takes too long to render and simulation slows down. Prevents other players from seeing you warp.")]
        public bool AntiWarp = true;
        private bool _AntiWarp = false;
        [Tooltip("Enable physics whilst not owner of the vehicle, can prevent some clipping through walls/ground, probably some performance hit. Not recommended for Quest")]
        public bool NonOwnerEnablePhysics = false;
        [Header("Fill SyncRigid to enable Object Mode (No SAVControl)")]
        public Rigidbody SyncRigid;
        private bool ObjectMode;
        [Header("DEBUG:")]
        [Tooltip("LEAVE THIS EMPTY UNLESS YOU WANT TO TEST THE NETCODE OFFLINE WITH CLIENT SIM")]
        public Transform SyncTransform;
        [Tooltip("LEAVE THIS EMPTY UNLESS YOU WANT TO TEST THE NETCODE OFFLINE WITH CLIENT SIM")]
        public Transform SyncTransform_Raw;
        private Transform VehicleTransform;
        private double nextUpdateTime = double.MaxValue;
        [UdonSynced] private double O_UpdateTime;
        [UdonSynced] private Vector3 O_Position;
        //the reason it's using a quat for rotation instead of euler angles is because it has to rotate twice to come back to the same value
        //giving a greater range for reconstruction of angular velocity, since that isn't synced
        [UdonSynced] private short O_RotationW;
        [UdonSynced] private short O_RotationX;
        [UdonSynced] private short O_RotationY;
        [UdonSynced] private short O_RotationZ;
        [UdonSynced] private Vector3 O_CurVel = Vector3.zero;
        private Vector3 O_CurVelLast = Vector3.zero;
        private Vector3 O_Rotation;
        private Quaternion O_Rotation_Q = Quaternion.identity;
        private Quaternion CurAngMom = Quaternion.identity;
        private Quaternion CurAngMomAcceleration = Quaternion.identity;
        private Quaternion LastCurAngMom = Quaternion.identity;
        private Quaternion O_LastRotation = Quaternion.identity;
        private Quaternion RotationLerper = Quaternion.identity;
        private float Ping;
        private double L_UpdateTime;
        private double O_LastUpdateTime;
        //make everyone think they're the owner for the first frame so that don't set the position to 0,0,0 before SFEXT_L_EntityStart runs
        private bool IsOwner = false;
        private Vector3 ExtrapolationDirection;
        private Quaternion RotationExtrapolationDirection;
        private Vector3 lerpedCurVel;
        private Vector3 Acceleration = Vector3.zero;
        private Vector3 LastAcceleration;
        private Vector3 O_LastPosition;
        private int UpdatesSentWhileStill;
        private Rigidbody VehicleRigid;
        private bool Initialized = false;
        public bool IdleUpdateMode;
        private bool Piloting;
        private bool Occupied;
        private int EnterIdleModeNumber;
        private double lastframetime;
        private double lastframetime_extrap;
        private Vector3 poslasframe;
        private Vector3 Extrapolation_Raw;
        private Quaternion RotExtrapolation_Raw = Quaternion.identity;
        private double StartupServerTime;
        Vector3 L_CurVel;
        Vector3 L_CurVelLast;
        private double StartupLocalTime;
        private Vector3 ExtrapDirection_Smooth;
        private Quaternion RotExtrapDirection_Smooth;
#if UNITY_EDITOR
        private bool TestMode;
#endif
        private float ErrorLastFrame;
        private float StartDrag;
        private float StartAngDrag;
        [System.NonSerialized] public SaccEntity EntityControl;
        private void Start()
        {
            if (SyncRigid)//object mode
            {
                ObjectMode = true;
                VehicleRigid = SyncRigid;
                VehicleTransform = SyncRigid.transform;
                if (!SyncTransform)
                { SyncTransform = VehicleRigid.transform; }
                if (!Initialized) SFEXT_L_EntityStart();
                return;
            }

            // prevent crash & incorrect movement if this object was left enabled
            if (!VehicleRigid)
            {
                VehicleRigid = ((SaccEntity)SAVControl.GetProgramVariable("EntityControl")).GetComponent<Rigidbody>();
                if (!VehicleTransform)
                { VehicleTransform = VehicleRigid.transform; }
                InitSyncValues();
            }

            if (!SyncTransform)
            { SyncTransform = VehicleRigid.transform; }
#if UNITY_EDITOR
            else { TestMode = true; }
#endif

        }
        public void SFEXT_L_EntityStart()
        {
            Initialized = true;
            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            bool InEditor = !Utilities.IsValid(localPlayer);
            if (SyncRigid)
            {
                ObjectMode = true;
                VehicleRigid = SyncRigid;
                VehicleTransform = SyncRigid.transform;
            }
            else
            {
                VehicleTransform = EntityControl.transform;
                VehicleRigid = EntityControl.VehicleRigidbody;
            }
            StartDrag = VehicleRigid.drag;
            StartAngDrag = VehicleRigid.angularDrag;
            if (!InEditor)
            {
                if (localPlayer.isMaster)
                {
                    IsOwner = true;
                    if (ObjectMode)
                    {
                        VehicleRigid.drag = StartDrag;
                        VehicleRigid.angularDrag = StartAngDrag;
                    }
                    else
                    {
                        VehicleRigid.drag = 0;
                        VehicleRigid.angularDrag = 0;
                    }
                }
                else
                {
                    IsOwner = false;
                    VehicleRigid.drag = 9999;
                    VehicleRigid.angularDrag = 9999;
                }
            }
            else
            {//play mode in editor
                IsOwner = true;
                VehicleRigid.drag = 9999;
                VehicleRigid.angularDrag = 9999;
            }
            if (gameObject.activeInHierarchy) { InitSyncValues(); }//this gameobject shouldn't be active at start, but some people might still have it active from older versions
            EnterIdleModeNumber = Mathf.FloorToInt(IdleModeUpdateInterval / updateInterval);//enter idle after IdleModeUpdateInterval seconds of being still
            // script activation is delayed to allow all scripts on this vehicle to activate first
            // 10 frames to be safe, 4 is the minimum for car wheels to not behave strangely (ingame only) if they're touching the ground at Start(), reason unknown.
            SendCustomEventDelayedFrames(nameof(ActivateScript), 10);
            InitSyncValues();
        }
        public void ActivateScript()
        {
            InitSyncValues();
            gameObject.SetActive(true);
            VehicleRigid.constraints = RigidbodyConstraints.None;
            SetPhysics();
            _AntiWarp = AntiWarp; //prevent from running early as it causes vehicle to teleport 500ft in the air for some reason
        }
        private void InitSyncValues()
        {
            ResetSyncTimes();
            Extrapolation_Raw = O_Position = VehicleTransform.position;
            O_LastRotation = O_Rotation_Q = VehicleTransform.rotation;
            double time = (StartupServerTime + (double)(Time.time - StartupLocalTime));
            nextUpdateTime = time + Random.Range(0f, updateInterval);
            O_LastUpdateTime = L_UpdateTime = lastframetime = lastframetime_extrap = time;
            O_LastUpdateTime -= updateInterval;
            O_LastPosition = O_Position = VehicleTransform.position;
        }
        public void SFEXT_L_OwnershipTransfer()
        { ExitIdleMode(); }
        public void SFEXT_O_TakeOwnership()
        {
            TakeOwnerStuff();
        }
        public void SFEXT_O_LoseOwnership()
        {
            LoseOwnerStuff();
        }
        private void TakeOwnerStuff()
        {
            L_UpdateTime = lastframetime = StartupServerTime + (double)(Time.time - StartupLocalTime);
            IsOwner = true;
            VehicleRigid.isKinematic = false;
            VehicleRigid.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            if (ObjectMode)
            {
                VehicleRigid.drag = StartDrag;
                VehicleRigid.angularDrag = StartAngDrag;
            }
            else
            {
                Vector3 cvel = (Vector3)SAVControl.GetProgramVariable("CurrentVel");
                VehicleRigid.velocity = cvel;
                SAVControl.SetProgramVariable("LastFrameVel", cvel);
                VehicleRigid.drag = 0;
                VehicleRigid.angularDrag = 0;
            }
            nextUpdateTime = StartupServerTime + (double)(Time.time - StartupLocalTime) - .01f;
            UpdatesSentWhileStill = 0;
        }
        private void LoseOwnerStuff()
        {
            IsOwner = false;
            O_LastUpdateTime = L_UpdateTime = lastframetime_extrap = StartupServerTime + (double)(Time.time - StartupLocalTime);
            O_LastUpdateTime -= updateInterval;
            Extrapolation_Raw = O_Position;
            ExtrapDirection_Smooth = O_CurVel;
            RotExtrapolation_Raw = RotationLerper = O_LastRotation = O_Rotation_Q;
            LastCurAngMom = CurAngMom = Quaternion.identity;
            if (!NonOwnerEnablePhysics) { VehicleRigid.isKinematic = true; }
            VehicleRigid.collisionDetectionMode = CollisionDetectionMode.Discrete;
            VehicleRigid.drag = 9999;
            VehicleRigid.angularDrag = 9999;
        }
        public void SFEXT_O_PilotEnter()
        {
            Piloting = true;
            nextUpdateTime = StartupServerTime + (double)(Time.time - StartupLocalTime) - .01f;
            ExitIdleMode();
        }
        public void SFEXT_G_PilotEnter()
        {
            Occupied = true;
        }
        public void SFEXT_G_PilotExit()
        { Occupied = false; }
        public void SFEXT_O_PilotExit()
        { Piloting = false; }
        public void SFEXT_O_RespawnButton()
        {
            ResetSyncTimes();
            nextUpdateTime = StartupServerTime + (double)(Time.time - StartupLocalTime) - .01f;
        }
        public void SFEXT_G_RespawnButton()
        {
            ExitIdleMode();
            //make it teleport instead of interpolating
            ExtrapolationDirection = Vector3.zero;
            Extrapolation_Raw = O_LastPosition = O_Position;
            RotationLerper = O_LastRotation = O_Rotation_Q;
            if (!IsOwner)
            {
                VehicleTransform.position = O_Position;
                VehicleTransform.rotation = O_Rotation_Q;
            }
            ExtrapDirection_Smooth = Vector3.zero;
            RotExtrapDirection_Smooth = Quaternion.identity;
            L_CurVelLast = Vector3.zero;
            LastAcceleration = Acceleration = Vector3.zero;
        }
        float lastFrameTime_hitchtest;
        private void Update()
        {
            if (IsOwner)//send data
            {
                //uncomment to test hitching
                // int i = 0;
                // if (Input.GetKeyDown(KeyCode.V))
                // {
                //     while (Time.realtimeSinceStartup - lastFrameTime_hitchtest < 1f)
                //     {
                //         i++;
                //     }
                // }
                // lastFrameTime_hitchtest = Time.realtimeSinceStartup;
                double time = (StartupServerTime + (double)(Time.time - StartupLocalTime));
                if (Time.deltaTime > .099f)
                {
                    ResetSyncTimes();
                    time = Networking.GetServerTimeInSeconds();//because we just ResetSyncTimes()'d
                    if (_AntiWarp && !DisableAntiWarp)//let's see if we can fix the movement jerkiness for observers if the FPS is extremely low
                    {
                        // ANTIWARP DOES NOT WORK IN CLIENTSIM, TEST IN-GAME
                        double acctime = time;
                        double accuratedelta = acctime - lastframetime;
                        Vector3 RigidMovedAmount = VehicleRigid.velocity * Time.deltaTime;
                        float DistanceTravelled = RigidMovedAmount.magnitude;

                        if (DistanceTravelled < (VehicleRigid.velocity * (float)accuratedelta).magnitude)
                        {
                            if (!Physics.Raycast(VehicleRigid.position, VehicleRigid.velocity, ((VehicleRigid.velocity * (float)accuratedelta) - RigidMovedAmount).magnitude, 133121 /* Default, Environment, and Walkthrough */, QueryTriggerInteraction.Ignore))
                            {
                                //smooth, but the extrapolation gets added each time (i think) causing vehicle to be faster (10%~)
                                //VehicleTransform.position += (VehicleRigid.velocity * (float)accuratedelta) - RigidMovedAmount;
                                //it's more correct to use RB position, but then you're removing the RB extrapolation and things get jerky.
                                //When setting rigidbody position, although it looks more jerky when flying side-by-side, it's more accurate speed-wise
                                //and hopefully doesn't cause rapid speed-up-slow-down if you keep on transitioning in and out of the parent if statement.
                                //Setting transform position to rigidbody position+, so that position is correct if data is sent this frame (the result should be the jerky, speed-accurate one)
                                VehicleTransform.position = VehicleRigid.position + (VehicleRigid.velocity * (float)accuratedelta) - RigidMovedAmount;
                                VehicleRigid.position = VehicleTransform.position;
                                //is there a best of both worlds solution?
                            }
                        }
                    }
                }
                lastframetime = time;
                if (time > nextUpdateTime)
                {
                    if (!Networking.IsClogged || Piloting)
                    {
                        //check if the vehicle has moved enough from it's last sent location and rotation to bother exiting idle mode
                        bool Still = !Piloting && (((VehicleTransform.position - O_Position).magnitude < IdleMovementRange) && Quaternion.Angle(VehicleTransform.rotation, O_Rotation_Q) < IdleRotationRange);

                        if (Still)
                        {
                            UpdatesSentWhileStill++;
                            if (UpdatesSentWhileStill > EnterIdleModeNumber)
                            { EnterIdleMode(); }
                        }
                        else
                        {
                            ExitIdleMode();
                        }
                        //never use rigidbody values for position or rotation because the interpolation/extrapolation from update is needed for it to be smooth
                        O_Position = VehicleTransform.position;
                        O_Rotation_Q = VehicleTransform.rotation;
                        //convert each quat to shorts to save bandwidth
                        float smv = short.MaxValue;
                        O_RotationX = (short)(O_Rotation_Q.x * smv);
                        O_RotationY = (short)(O_Rotation_Q.y * smv);
                        O_RotationZ = (short)(O_Rotation_Q.z * smv);
                        O_RotationW = (short)(O_Rotation_Q.w * smv);

                        O_CurVel = VehicleRigid.velocity;

                        O_UpdateTime = time;//send servertime of update
                        RequestSerialization();
                    }
                    nextUpdateTime = time + (IdleUpdateMode ? IdleModeUpdateInterval : updateInterval);
                }
#if UNITY_EDITOR
                if (TestMode)
                {
                    ExtrapolationAndSmoothing();
                }
#endif
            }
            else//extrapolate and interpolate based on received data
            {
                ExtrapolationAndSmoothing();
            }
        }
        private void ExtrapolationAndSmoothing()
        {
#if UNITY_EDITOR
            if (Deserialized)
            {
                Deserialized = false;
                OnDeserialization();
            }
#endif
            float deltatime = Time.deltaTime;
            double time;
            Vector3 Deriv = Vector3.zero;
            Vector3 Correction = (Extrapolation_Raw - SyncTransform.position) * CorrectionTime;
            float Error = Vector3.Distance(SyncTransform.position, Extrapolation_Raw);
            if (deltatime > .099f)
            {
                time = Networking.GetServerTimeInSeconds();
                deltatime = (float)(time - lastframetime_extrap);
                ResetSyncTimes();
            }
            else { time = StartupServerTime + (double)(Time.time - StartupLocalTime); }
            //like a PID derivative. Makes movement a bit jerky because the 'raw' target is jerky.
            if (Error < ErrorLastFrame)
            {
                Deriv = -Correction.normalized * (ErrorLastFrame - Error) * CorrectionDStrength / deltatime;
            }
            ErrorLastFrame = Error;
            lastframetime_extrap = Networking.GetServerTimeInSeconds();
            float TimeSinceUpdate = (float)((time - L_UpdateTime) / updateInterval);
            //extrapolated position based on time passed since update
            Vector3 VelEstimate = L_CurVel + (Acceleration * TimeSinceUpdate);
            ExtrapDirection_Smooth = Vector3.Lerp(ExtrapDirection_Smooth, VelEstimate + Correction + Deriv, SpeedLerpTime * deltatime);

            //rotate using similar method to movement (no deriv, correction is done with a simple slerp after)
            Quaternion FrameRotAccel = RealSlerp(Quaternion.identity, CurAngMomAcceleration, TimeSinceUpdate);
            Quaternion AngMomEstimate = FrameRotAccel * CurAngMom;
            RotExtrapDirection_Smooth = RealSlerp(RotExtrapDirection_Smooth, AngMomEstimate, RotationSpeedLerpTime * deltatime);

            //apply positional update
            Extrapolation_Raw = O_Position + (ExtrapolationDirection * (float)(time - O_UpdateTime));
            SyncTransform.position += ExtrapDirection_Smooth * deltatime;
            //apply rotational update
            Quaternion FrameRotExtrap = RealSlerp(Quaternion.identity, RotationExtrapolationDirection, deltatime);
            RotExtrapolation_Raw = FrameRotExtrap * RotExtrapolation_Raw;
            Quaternion FrameRotExtrap_Smooth = RealSlerp(Quaternion.identity, RotExtrapDirection_Smooth, deltatime);
            SyncTransform.rotation = FrameRotExtrap_Smooth * SyncTransform.rotation;
            //correct rotational desync
            SyncTransform.rotation = RealSlerp(SyncTransform.rotation, RotExtrapolation_Raw, CorrectionTime_Rotation * deltatime);
#if UNITY_EDITOR
            if (SyncTransform_Raw)
            {
                SyncTransform_Raw.position = Extrapolation_Raw;
                SyncTransform_Raw.rotation = RotExtrapolation_Raw;
            }
#endif
        }
        private void EnterIdleMode()
        { IdleUpdateMode = true; }
        private void ExitIdleMode()
        { IdleUpdateMode = false; UpdatesSentWhileStill = 0; }
#if UNITY_EDITOR
        [Tooltip("Doesn't work properly, can't wait beyond update interval.")]
        public float LagSimDelay;
        private float LagSimTime;
        private bool LagSimWait;
        public float DBGPING;
        private void FixedUpdate()
        {
            if (TestMode)
            { DeserializationCheck(); }
        }
        private bool Deserialized = false;
        private void DeserializationCheck()
        {
            if (O_UpdateTime != O_LastUpdateTime)
            {
                if (!LagSimWait)
                {
                    if (LagSimDelay != 0)
                    {
                        LagSimWait = true;
                        LagSimTime = Time.time;
                        return;
                    }
                }
                else
                {
                    if (Time.time - LagSimTime > LagSimDelay)
                    {
                        LagSimWait = false;
                    }
                    else
                    {
                        return;
                    }
                }
                Deserialized = true;
            }
        }
#endif
        public override void OnDeserialization()
        {
            //time between this update and last
            float updatedelta = (float)(O_UpdateTime - O_LastUpdateTime);
            if (updatedelta < 0.0001f)
            {
                O_LastUpdateTime = O_UpdateTime;
                return;
            }
            float speednormalizer = 1 / updatedelta;

            LastAcceleration = Acceleration;
            LastCurAngMom = CurAngMom;

            //local time update was received
            L_UpdateTime = StartupServerTime + (double)(Time.time - StartupLocalTime);
            //Ping is time between server time update was sent, and the local time the update was received
            Ping = (float)(L_UpdateTime - O_UpdateTime);
#if UNITY_EDITOR
            DBGPING = Ping;
#endif
            //Curvel is 0 when launching from a catapult because it doesn't use rigidbody physics, so do it based on position
            bool SetVelZero = false;
            if (O_CurVel.sqrMagnitude == 0)
            {
                if (O_CurVelLast.sqrMagnitude != 0)
                { L_CurVel = Vector3.zero; SetVelZero = true; }
                else
                { L_CurVel = (O_Position - O_LastPosition) * speednormalizer; }
            }
            else
            { L_CurVel = O_CurVel; }
            O_CurVelLast = O_CurVel;
            Acceleration = L_CurVel - L_CurVelLast;

            float smv = short.MaxValue;
            O_Rotation_Q = new Quaternion(O_RotationX / smv, O_RotationY / smv, O_RotationZ / smv, O_RotationW / smv);

            //rotate Acceleration by the difference in rotation of vehicle between last and this update to make it match the angle for the next update better
            Quaternion PlaneRotDif = O_Rotation_Q * Quaternion.Inverse(O_LastRotation);
            Acceleration = (PlaneRotDif * Acceleration) * .5f;//not sure why it's 0.5, but it seems correct from testing
            Acceleration += Acceleration * (Ping / updatedelta);

            //current angular momentum as a quaternion
            CurAngMom = RealSlerp(Quaternion.identity, PlaneRotDif, speednormalizer);
            CurAngMomAcceleration = CurAngMom * Quaternion.Inverse(LastCurAngMom);

            //if direction of acceleration changed by more than 90 degrees, just set zero to prevent bounce effect, the vehicle likely just crashed into a wall.
            //+ if idlemode, disable acceleration because it brakes
            if (Vector3.Dot(Acceleration, LastAcceleration) < 0 || SetVelZero || L_CurVel.magnitude < IdleMovementRange)
            { Acceleration = Vector3.zero; CurAngMomAcceleration = Quaternion.identity; }

            RotationExtrapolationDirection = CurAngMomAcceleration * CurAngMom;
            Quaternion PingRotExtrap = RealSlerp(Quaternion.identity, RotationExtrapolationDirection, Ping);
            Quaternion L_PingAdjustedRotation = PingRotExtrap * O_Rotation_Q;
            RotExtrapolation_Raw = L_PingAdjustedRotation;

            //tell the SaccAirVehicle the velocity value because it doesn't sync it itself
            if (!ObjectMode) { SAVControl.SetProgramVariable("CurrentVel", L_CurVel); }
            ExtrapolationDirection = L_CurVel + Acceleration;

            // Extrapolation_Raw = L_PingAdjustedPosition - (ExtrapolationDirection * Time.deltaTime);//undo 1 frame worth of movement because its done again in update()

            //if we're going one way but moved the other, we must have teleported.
            //set values to the same thing for Current and Last to make teleportation instead of interpolation
            if (Vector3.Angle(O_Position - O_LastPosition, O_CurVel) > TeleportAngleDifference && L_CurVel.magnitude > 30f)
            {
                LastCurAngMom = CurAngMom;
                SyncTransform.position = Extrapolation_Raw;
            }
            O_LastUpdateTime = O_UpdateTime;
            O_LastRotation = O_Rotation_Q;
            O_LastPosition = O_Position;
            L_CurVelLast = L_CurVel;
        }
        public void ResetSyncTimes()
        {
            StartupServerTime = Networking.GetServerTimeInSeconds();
            StartupLocalTime = Time.time;
        }
        public void SFEXT_O_Explode()
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
        public void SFEXT_L_SetPhysics() { SetPhysics(); }//not used in base prefab
        public void SetPhysics()
        {
            if (IsOwner)
            {
                VehicleRigid.isKinematic = false;
                VehicleRigid.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
            else
            {
                if (!NonOwnerEnablePhysics) { VehicleRigid.isKinematic = true; }
                VehicleRigid.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }
        }
        public void SFEXT_L_OnCollisionEnter() { ExitIdleMode(); }
        private bool DisableAntiWarp;
        public void SFEXT_L_FinishRace() { DisableAntiWarp = false; }
        public void SFEXT_L_StartRace() { DisableAntiWarp = true; }
        public void SFEXT_L_CancelRace() { DisableAntiWarp = false; }
        //unity slerp always uses shortest route to orientation rather than slerping to the actual quat. This undoes that
        public Quaternion RealSlerp(Quaternion p, Quaternion q, float t)
        {
            if (Quaternion.Dot(p, q) < 0)
            {
                float angle = Quaternion.Angle(p, q);//quaternion.angle also checks shortest route
                if (angle == 0f) { return p; }
                float newvalue = (360f - angle) / angle;
                return Quaternion.SlerpUnclamped(p, q, -t * newvalue);
            }
            else return Quaternion.SlerpUnclamped(p, q, t);
        }
    }
}