
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [DefaultExecutionOrder(1500)]
    // [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SaccWheel : UdonSharpBehaviour
    {
        [Header("Sync is only used for skid effects,\nset Synchronization Method:\nNone to save bandwidth (Use for tanks)\nManual to sync skid sounds/effects\nDo Not Use Continuous")]
        [Space(10)]
        public Rigidbody CarRigid;
        public SaccGroundVehicle SGVControl;
        public float SyncInterval = 0.3f;
        public Transform WheelPoint;
        public Transform WheelVisual;
        public Transform WheelVisual_Ground;
        [Tooltip("For if wheel is part of a caterpillar track, so wheels can match rotation with each other, prevent (visual) wheelspinning")]
        public SaccWheel WheelVisual_RotationSource;
        public float SuspensionDistance;
        public float WheelRadius;
        public float SpringForceMulti = 8f;
        public float Damping_Bump = 0.75f;
        public float Damping_Rebound = 0.7f;
        [Tooltip("Limit suspension force so that the car doesn't jump up when going over a step")]
        public float MaxSusForce = 60f;
        // [Tooltip("Limit Damping when suspension is decomopressing?")]
        // public float MaxNegDamping = 999999f;
        [Tooltip("Extra height on the raycast origin to prevent the wheel from sticking through the floor")]
        public float ExtraRayCastDistance = .5f;
        public float Grip = 350f;
        public AnimationCurve GripCurve = AnimationCurve.Linear(0, 1, 1, 1);
        [Tooltip("Multiply forward grip by this value for sideways grip")]
        public float LateralGrip = 1f;
        public AnimationCurve GripCurveLateral = AnimationCurve.Linear(0, 1, 1, 1);
        [Tooltip("!!THE LATERAL GRIP VARS ARE STILL USED WHEN THIS IS FALSE, JUST DIFFERENTLY!!\nCompletely separate wheel's sideways and forward grip calculations (More arcadey)")]
        public bool SeparateLongLatGrip = false;
        [Tooltip("Choose how much separation there is\n0 is still different from SeparateLongLatGrip being disabled\nRECOMMENDED: SkidRatioMode 1")]
        public float LongLatSeparation = 1;
        [Tooltip("How quickly grip falls off with roll")]
        public float WheelRollGrip_Power = 1;
        [Range(0, 2), Tooltip("3 Different ways to calculate amount of engine force used when sliding + accelerating, for testing. 0 = old way, 1 = keeps more energy, 2 = loses more energy")]
        public int SkidRatioMode = 0;
        public float BrakeStrength = .8f;
        public float HandBrakeStrength = 1.4f;
        [Tooltip("How quickly the wheel matches the speed of the ground when in contact with it, low values will make the car skid more")]
        public float WheelGroundInfluence = 1000f;
        [Tooltip("Only effects DriveWheels. Behaves like engine torque. How much forces on the wheel from the ground can influence the engine speed, low values will make the car skid more")]
        public float EngineInfluence = 80000f;
        [Tooltip("Max angle of ground at which vehicle can park on without sliding down")]
        [SerializeField] float MaxParkingIncline = 30;
        public LayerMask WheelLayers;
        public float ClutchStrength = 100f;
        [Tooltip("Skip sound and skid effects completely")]
        public bool DisableEffects = false;
        public float[] SurfaceType_Grips = { 1f, 0.7f, 0.2f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };
        public float[] SurfaceType_Slowdown = { 0.1f, 4f, 0.05f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f };
        public AudioSource[] SurfaceType_SkidSounds;
        public ParticleSystem[] SurfaceType_SkidParticles;
        public ParticleSystem.EmissionModule[] SurfaceType_SkidParticlesEM;
        public float[] SurfaceType_SkidParticles_Amount = { .3f, .3f, .3f, .3f, .3f, .3f, .3f, .3f, .3f, .3f };
        private float SkidSound_Min_THREEQUARTER, SkidSound_Min_TWOTHRID; // sync a bit before the skid speed so it's more accurate
        [Tooltip("Lower number = less skid required for sound to start")]
        public float SkidSound_Min = 3f;
        [Tooltip("How quickly volume increases as skid speed increases")]
        public float SkidSound_VolumeIncrease = 0.5f;
        [Tooltip("How quickly pitch increases as skid speed increases")]
        public float SkidSound_PitchIncrease = 0.02f;
        public float SkidSound_Pitch = 1f;
        [Tooltip("Reduce volume of skid swhilst in the car")]
        public float SkidVolInVehicleMulti = .4f;
        [Header("Debug")]
        public Transform LastTouchedTransform;
        public Rigidbody LastTouchedTransform_RB;
        public Vector3 LastTouchedTransform_Position;
        public Vector3 LastTouchedTransform_Speed;
        public float CurrentGrip = 7f;
        public float CurrentNumParticles = 0f;
        public float CurrentWheelSlowDown = 0f;
        [System.NonSerialized] public bool IsDriveWheel = false;
        [System.NonSerialized] public bool IsSteerWheel = false;
        [System.NonSerialized] public bool IsOtherWheel = false;
        private AudioSource SkidSound;
        private ParticleSystem SkidParticle;
        private ParticleSystem.EmissionModule SkidParticleEM;
        [UdonSynced(UdonSyncMode.Linear)] private float SkidLength;
        private float SkidLength_Smoothed;
        public float SkidLength_SmoothStep = 0.11f;
        private bool SyncSkid_Running;
        private bool SkidLength_SkiddingLast;
        private float lastSync;
        public float Clutch = 1f;
        public float WheelRotation;
        public float WheelRotationSpeedRPS;
        public float WheelRotationSpeedRPM;
        public float WheelRotationSpeedSurf;
        public bool Grounded;
        [System.NonSerialized] public float HandBrake;
        [System.NonSerialized] public float Brake;
        public bool Sleeping;
        private int SurfaceType = -1;
        private float SkidVolumeMulti = 1;
        private bool SkidSoundPlayingLast;
        private bool SkidParticlePlayingLast;
        private Vector3 SusDirection;
        private Renderer WheelRenderer;
        [FieldChangeCallback(nameof(GearRatio))] public float _GearRatio = 0f;
        public float GearRatio
        {
            set
            {
                if (value == 0f && !(bool)SGVControl.GetProgramVariable("TankMode"))
                {
                    GearNeutral = true;
                }
                else
                {
                    GearNeutral = false;
                }
                _GearRatio = value;
            }
            get => _GearRatio;
        }
        public bool GearNeutral;
        private float WheelDiameter;
        private float WheelCircumference;
        private float compressionLast;
        public bool IsOwner = false;
        public bool CurrentlyDistant = true;

#if UNITY_EDITOR
        bool running;
        public bool SetVel;
        public bool PrintDebugValues;
        public Vector3 DebugMoveSpeed = Vector3.zero;
        public bool DebugFreezeWheel = false;
        public float AccelTestLength = 4;
        public float DistResultPush;
        public float DistResultAccel;
        public float SpeedResultAccel;
        private Vector3 pushstartpoint;
        private Vector3 accelstartpoint;
        public void DEBUGPushCar()
        {
            pushstartpoint = CarRigid.position;
            CarRigid.velocity = CarRigid.transform.TransformDirection(DebugMoveSpeed);
            WheelRotationSpeedRPM = 0;
            WheelRotationSpeedRPS = 0;
            WheelRotationSpeedSurf = 0;
        }
        public void DEBUGAccelCar()
        {
            SGVControl.SendCustomEvent("setStepsSec");
            CarRigid.velocity = Vector3.zero;
            CarRigid.angularVelocity = Vector3.zero;
            SendCustomEventDelayedSeconds(nameof(DEBUGAccelCar_2), Time.fixedDeltaTime * 2);
            SGVControl.SetProgramVariable("Revs", 0f);
            accelstartpoint = CarRigid.position;
            WheelRotationSpeedRPM = 0;
            WheelRotationSpeedRPS = 0;
            WheelRotationSpeedSurf = 0;
        }
        public void DEBUGAccelCar_2()
        {
            CarRigid.velocity = Vector3.zero;
            CarRigid.angularVelocity = Vector3.zero;
            SendCustomEventDelayedSeconds(nameof(DEBUGMeasureAccel), AccelTestLength);
            SGVControl.SetProgramVariable("ACCELTEST", true);
            SGVControl.SetProgramVariable("Revs", 0f);
            accelstartpoint = CarRigid.position;
            WheelRotationSpeedRPM = 0;
            WheelRotationSpeedRPS = 0;
            WheelRotationSpeedSurf = 0;
        }
        private bool boolRevingUp;
        private int RevUpCount;
        public float RevUpResult;
        public float RPMTestTarget = 500;
        public void DEBUGAccelCar_Revup()
        {
            CarRigid.velocity = Vector3.zero;
            CarRigid.angularVelocity = Vector3.zero;
            SendCustomEventDelayedSeconds(nameof(DEBUGAccelCar_Revup_2), Time.fixedDeltaTime * 2);
            SGVControl.SetProgramVariable("Revs", 0f);
            accelstartpoint = CarRigid.position;
            WheelRotationSpeedRPM = 0;
            WheelRotationSpeedRPS = 0;
            WheelRotationSpeedSurf = 0;
        }
        public void DEBUGAccelCar_Revup_2()
        {
            CarRigid.velocity = Vector3.zero;
            CarRigid.angularVelocity = Vector3.zero;
            SGVControl.SetProgramVariable("ACCELTEST", true);
            SGVControl.SetProgramVariable("Revs", 0f);
            accelstartpoint = CarRigid.position;
            WheelRotationSpeedRPM = 0;
            WheelRotationSpeedRPS = 0;
            WheelRotationSpeedSurf = 0;
            boolRevingUp = true;
            RevUpCount = 0;
        }
        public void DEBUGMeasureAccel()
        {
            SGVControl.SetProgramVariable("ACCELTEST", false);
            DistResultAccel = Vector3.Distance(accelstartpoint, CarRigid.position);
            SpeedResultAccel = CarRigid.velocity.magnitude;
        }
#endif
        void Start()
        {
            SkidSound_Min_THREEQUARTER = SkidSound_Min * .75f;
            SkidSound_Min_TWOTHRID = SkidSound_Min * .66f;
            WheelRenderer = (Renderer)SGVControl.GetProgramVariable("MainObjectRenderer");
            if (!WheelRenderer)
            {
                SaccEntity EC = (SaccEntity)SGVControl.GetProgramVariable("EntityControl");
                WheelRenderer = (Renderer)EC.GetComponentInChildren(typeof(Renderer));
                Debug.LogWarning(EC.gameObject.name + "'s SaccGroundVehicle's 'Main Object Renderer' is not set");
            }
            WheelDiameter = WheelRadius * 2f;
            WheelCircumference = WheelDiameter * Mathf.PI;
            GearRatio = _GearRatio;
            if (SurfaceType_SkidSounds.Length > 0)
            {
                if (SurfaceType_SkidSounds[0])
                { SkidSound = SurfaceType_SkidSounds[0]; }
            }
            SurfaceType_SkidParticlesEM = new ParticleSystem.EmissionModule[SurfaceType_SkidParticles.Length];
            for (int i = 0; i < SurfaceType_SkidParticles.Length; i++)
            {
                if (SurfaceType_SkidParticles[i])
                { SurfaceType_SkidParticlesEM[i] = SurfaceType_SkidParticles[i].emission; }
            }
            if (SurfaceType_SkidParticles.Length > 0)
            {
                if (SurfaceType_SkidParticles[0])
                {
                    SkidParticle = SurfaceType_SkidParticles[0];
                    SkidParticleEM = SurfaceType_SkidParticlesEM[0];
                }
            }
            DoSurface = Random.Range(0, 4);
            DisableEffects = SurfaceType_SkidSounds.Length == 0 && SurfaceType_SkidParticles.Length == 0;
#if UNITY_EDITOR
            running = true;
#endif
        }
        public void ChangeSurface()
        {
            if (SurfaceType < 0) { return; }
            CurrentGrip = Grip * SurfaceType_Grips[SurfaceType];
            CurrentWheelSlowDown = SurfaceType_Slowdown[SurfaceType];
            CurrentNumParticles = SurfaceType_SkidParticles_Amount[SurfaceType];
            StopSkidSound();
            if (SurfaceType < SurfaceType_SkidSounds.Length)
            {
                if (SurfaceType_SkidSounds[SurfaceType])
                {
                    SkidSound = SurfaceType_SkidSounds[SurfaceType];
                }
            }
            else
            {
                SkidSound = null;
            }

            StopSkidParticle();
            if (SurfaceType < SurfaceType_SkidParticles.Length)
            {
                if (SurfaceType_SkidParticles[SurfaceType])
                {
                    SkidParticle = SurfaceType_SkidParticles[SurfaceType];
                    SkidParticleEM = SurfaceType_SkidParticlesEM[SurfaceType];
                }
            }
            else
            {
                SkidParticle = null;
            }
        }
        public void Wheel_FixedUpdate()
        {
            if (Sleeping) { return; }
            Suspension();
            WheelPhysics();
        }
        private void Suspension()
        {
            float compression = 0f;
            if (Physics.Raycast(WheelPoint.position + WheelPoint.up * ExtraRayCastDistance, -WheelPoint.up, out SusOut, SuspensionDistance + ExtraRayCastDistance, WheelLayers, QueryTriggerInteraction.Ignore))
            {
                float fixedDT = Time.fixedDeltaTime;
                Vector3 PointVel = (SusOut.point - GroundPointLast) / fixedDT;
                GroundPointLast = SusOut.point;
                GetTouchingTransformSpeed();
                if (Grounded)
                {
                    PointVelocity = PointVel - LastTouchedTransform_Speed;
                }
                else
                {
                    PointVelocity = CarRigid.GetPointVelocity(SusOut.point) - LastTouchedTransform_Speed;
                }
                Grounded = true;
                //SusDirection is closer to straight up the slower vehicle is moving, so that it can stop on slopes
                if (Vector3.Angle(SusOut.normal, Vector3.up) < MaxParkingIncline && !SGVControl.Bike_AutoSteer)
                { SusDirection = Vector3.Lerp(Vector3.up, SusOut.normal, (SGVControl.VehicleSpeed / 1f)); }
                else
                { SusDirection = SusOut.normal; }

                CheckSurface();
                //SUSPENSION//
                compression = 1f - ((SusOut.distance - ExtraRayCastDistance) / SuspensionDistance);
                //Spring force: More compressed = more force
                Vector3 SpringForce = SusDirection * compression * SpringForceMulti * fixedDT;
                float damping = compression - compressionLast;
                damping *= damping > 0 ? Damping_Bump : Damping_Rebound;
                compressionLast = compression;
                //Damping force: The more the difference in compression between updates, the more force
                Vector3 DampingForce = SusDirection * damping/*  * Vector3.Dot(SusOut.normal, WheelPoint.up) */;
                //these are added together, but both contain deltatime, potential deltatime problem source?
                SusForce = SpringForce + DampingForce;//The total weight on this suspension

                if (SusForce.magnitude / fixedDT > MaxSusForce)
                {
                    SusForce = SusForce.normalized * MaxSusForce * fixedDT;
                }

                float susdot = Vector3.Dot(WheelPoint.up, SusForce);
                if (susdot > 0)// don't let the suspension force push the car down
                { CarRigid.AddForceAtPosition(SusForce, WheelPoint.position, ForceMode.VelocityChange); }

                //set wheel's visual position
                if (SusOut.distance > ExtraRayCastDistance)
                {
                    WheelVisual.position = SusOut.point + (WheelPoint.up * WheelRadius);
                    if (WheelVisual_Ground) { WheelVisual_Ground.position = SusOut.point; }
                }
                else
                {
                    WheelVisual.position = WheelPoint.position + (WheelPoint.up * WheelRadius);
                    if (WheelVisual_Ground) { WheelVisual_Ground.position = WheelPoint.position; }
                }
                //END OF SUSPENSION//
                //GRIP//
                //Wheel's velocity vector projected to the normal of the ground
                WheelGroundUp = Vector3.ProjectOnPlane(SusOut.normal, WheelPoint.right).normalized;
#if UNITY_EDITOR
                ContactPoint = SusOut.point;
#endif
            }
            else
            {
                //wheel not touching ground
                if (SkidSoundPlayingLast) { StopSkidSound(); }
                if (SkidParticlePlayingLast) { StopSkidParticle(); }
                WheelVisual.position = WheelPoint.position - (WheelPoint.up * (SuspensionDistance - WheelRadius));
                if (WheelVisual_Ground) { WheelVisual_Ground.position = WheelPoint.position - (WheelPoint.up * (SuspensionDistance)); }
                SusForce = Vector3.zero;
                Grounded = false;
                compressionLast = 0f;
            }
        }
        RaycastHit SusOut;
        Vector3 SusForce;
        Vector3 WheelGroundUp = Vector3.up;
        Vector3 GroundPointLast;
        Vector3 PointVelocity;
        private void WheelPhysics()
        {
            float DeltaTime = Time.fixedDeltaTime;
            float ForwardSpeed = 0f;
            float ForwardSideRatio = 0f;
            float ForceUsed = 0f;
            float ForwardSlip = 0f;
            Vector3 SkidVectorFX = Vector3.zero;

            if (IsDriveWheel && !GearNeutral)
            {
                float EngineRevs = (float)SGVControl.GetProgramVariable("Revs");
                WheelRotationSpeedRPM = Mathf.Lerp(WheelRotationSpeedRPM, EngineRevs * _GearRatio, 1 - Mathf.Pow(0.5f, (1f - Clutch) * ClutchStrength));
                WheelRotationSpeedRPS = WheelRotationSpeedRPM / 60f;
                WheelRotationSpeedSurf = WheelCircumference * WheelRotationSpeedRPS;
            }

#if UNITY_EDITOR
            DistResultPush = Vector3.Distance(pushstartpoint, CarRigid.position);
            if (SetVel) CarRigid.velocity = DebugMoveSpeed;
            if (DebugFreezeWheel)
            {
                WheelRotationSpeedRPM = 0;
                WheelRotationSpeedRPS = 0;
                WheelRotationSpeedSurf = 0;
            }
            if (boolRevingUp)
            {
                RevUpCount++;
                if (WheelRotationSpeedRPM > RPMTestTarget)
                {
                    boolRevingUp = false;
                    RevUpResult = Time.fixedDeltaTime * (float)RevUpCount;
                    SGVControl.SetProgramVariable("ACCELTEST", false);
                }
            }
#endif
            if (Grounded)
            {
                //Wheel's velocity vector projected to be only forward/back
                Vector3 WheelForwardSpeed = Vector3.ProjectOnPlane(PointVelocity, WheelPoint.right);
                WheelForwardSpeed -= Vector3.Project(WheelForwardSpeed, WheelGroundUp);
                float ForwardSpeed_abs = WheelForwardSpeed.magnitude;
                ForwardSpeed = ForwardSpeed_abs;
                if (Vector3.Dot(WheelForwardSpeed, WheelPoint.forward) < 0f)
                { ForwardSpeed = -ForwardSpeed; }

                ForwardSlip = ForwardSpeed - WheelRotationSpeedSurf;
                //How much the wheel is slipping (difference between speed of wheel rotation at it's surface, and the speed of the ground beneath it), as a vector3
                Vector3 ForwardSkid = Vector3.ProjectOnPlane(WheelPoint.forward, SusOut.normal).normalized * ForwardSlip;

                Vector3 SideSkid = Vector3.ProjectOnPlane(PointVelocity, WheelForwardSpeed);
                SideSkid = Vector3.ProjectOnPlane(SideSkid, SusOut.normal);

                //add both skid axis together to get total 'skid'
                Vector3 FullSkid = SideSkid + ForwardSkid;
                float FullSkidMag = FullSkid.magnitude;
                //find out how much of the skid is on the forward axis 
                if (FullSkidMag != 0)
                {
                    if (SkidRatioMode == 0)
                        ForwardSideRatio = Vector3.Dot(ForwardSkid / FullSkidMag, FullSkid / FullSkidMag);
                    else
                    {
                        //these might produce different/more arcadey feel idk
                        float ForwardLen = ForwardSkid.magnitude;
                        float SideLen = SideSkid.magnitude;
                        float fullLen = ForwardLen + SideLen;
                        if (SkidRatioMode == 1)
                            ForwardSideRatio = ForwardLen / fullLen;
                        else
                            ForwardSideRatio = ForwardLen / FullSkidMag;
                    }
                }
                Vector3 GripForce3;
                //SusForce has deltatime built in
                float SusForceMag = SusForce.magnitude;
                float MaxGrip = (SusForceMag * CurrentGrip) / DeltaTime;
                float MaxGripLat = MaxGrip * LateralGrip;
                Vector3 GripForceForward;
                Vector3 GripForcLat;
                float WheelRollGrip = Mathf.Max(Mathf.Pow(Vector3.Dot(transform.up, SusOut.normal), WheelRollGrip_Power), .3f);

                if (SeparateLongLatGrip)
                {
                    float evalskid = ForwardSkid.magnitude / MaxGrip;
                    float gripPc = GripCurve.Evaluate(evalskid);
                    GripForceForward = -ForwardSkid.normalized * gripPc * MaxGrip * WheelRollGrip;

                    float evalskidLat = SideSkid.magnitude / MaxGripLat;
                    float gripPcLat = GripCurveLateral.Evaluate(evalskidLat);
                    GripForcLat = -SideSkid.normalized * gripPcLat * MaxGripLat * WheelRollGrip;
                    GripForce3 = (GripForceForward + GripForcLat) * DeltaTime;
                    Vector3 newgrip = Vector3.Slerp(GripForcLat, GripForceForward, ForwardSideRatio) * DeltaTime;
                    GripForce3 = Vector3.Lerp(newgrip, GripForce3, LongLatSeparation);
                    gripPc = Mathf.Lerp(gripPc * ForwardSideRatio, gripPc, LongLatSeparation);
                    ForceUsed = ForwardSkid.magnitude * DeltaTime;
                }
                else
                {
                    float evalskid = FullSkid.magnitude / MaxGrip;
                    float gripPc = GripCurve.Evaluate(evalskid);
                    GripForceForward = -FullSkid.normalized * gripPc * MaxGrip * WheelRollGrip;

                    float evalskidLat = FullSkid.magnitude / MaxGripLat;
                    float gripPcLat = GripCurveLateral.Evaluate(evalskidLat);
                    GripForcLat = -FullSkid.normalized * gripPcLat * MaxGripLat * WheelRollGrip;
                    GripForce3 = Vector3.Lerp(GripForcLat, GripForceForward, ForwardSideRatio) * DeltaTime;
                    ForceUsed = (GripForceForward * ForwardSideRatio).magnitude * DeltaTime;
                }
                CarRigid.AddForceAtPosition(GripForce3, SusOut.point, ForceMode.VelocityChange);

#if UNITY_EDITOR
                ForceVector = GripForce3;
                ForceUsedDBG = ForceUsed;
#endif
                //move wheel rotation speed towards its ground speed along its forward axis based on how much of it's forward 'skid' that it gripped
                WheelRotationSpeedSurf = Mathf.MoveTowards(WheelRotationSpeedSurf, ForwardSpeed, Mathf.Abs(ForceUsed) * WheelGroundInfluence);
                //brake
                //WheelGroundInfluence is multiplied in so that changing WheelGroundInfluence doesn't change brake's effect
                WheelRotationSpeedSurf = Mathf.MoveTowards(WheelRotationSpeedSurf, 0f, DeltaTime * Brake * BrakeStrength * WheelGroundInfluence);
                WheelRotationSpeedSurf = Mathf.MoveTowards(WheelRotationSpeedSurf, 0f, DeltaTime * HandBrake * HandBrakeStrength * WheelGroundInfluence);
                //wheels slow down due to ?friction
                WheelRotationSpeedSurf = Mathf.Lerp(WheelRotationSpeedSurf, 0, 1 - Mathf.Pow(0.5f, DeltaTime * CurrentWheelSlowDown));
                WheelRotationSpeedRPS = WheelRotationSpeedSurf / WheelCircumference;
                WheelRotationSpeedRPM = WheelRotationSpeedRPS * 60f;

                SkidVectorFX = FullSkid;
            }
            // adjust engine speed
            if (IsDriveWheel && !GearNeutral)
            {
                bool slowing = (ForwardSlip < 0 && (_GearRatio > 0)) || ((ForwardSlip > 0) && (_GearRatio < 0));
                // (slowing ? 1 : -(1f - Clutch)) means use clutch if speeding up the engine, but don't use clutch if slowing down the engine.
                // because clutch was already used in the input in the slowing down case.
                // if removed, using the clutch can give a speed boost since the engine doesn't slow down by the correct amount relative to force produced.
                float ThisEngineForceUsed = ForceUsed * Mathf.Abs(_GearRatio) * EngineInfluence * (slowing ? 1 : -(1f - Clutch));
                SGVControl.SetProgramVariable("EngineForceUsed", (float)SGVControl.GetProgramVariable("EngineForceUsed") + ThisEngineForceUsed);
            }
            SkidLength = SkidVectorFX.magnitude;
        }
        // [SerializeField] AnimationCurve GripOverRoll;
        private void LateUpdate()
        {
            if (Sleeping) return;
            if (IsOwner)
            {
                if (Time.time - lastSync > SyncInterval)
                {
                    bool Skidding = SkidLength < SkidSound_Min_THREEQUARTER;
                    if (!(!Skidding && !SkidLength_SkiddingLast))//if last send was (not skidding) and it's still (not skidding) don't send
                    {
                        lastSync = Time.time;
                        SkidLength_SkiddingLast = Skidding;
                        RequestSerialization();
                    }
                }
                SkidLength_Smoothed = SkidLength;
                if (WheelRenderer.isVisible)
                {
                    RotateWheelOwner();
                }
            }
            else
            {
                if (WheelRenderer.isVisible)
                {
                    RotateWheelOther();
                    Suspension_VisualOnly();
                }
            }
            if (DisableEffects) { return; }
            if (Grounded && !CurrentlyDistant)
            {
                float skidvol = Mathf.Min((SkidLength_Smoothed - SkidSound_Min) * SkidSound_VolumeIncrease, 1);
                if (skidvol > 0)
                {
                    if (SkidSound)
                    {
                        if (!SkidSoundPlayingLast)
                        {
                            StartSkidSound();
                        }
                        SkidSound.volume = skidvol * SkidVolumeMulti;
                        SkidSound.pitch = (SkidLength_Smoothed * SkidSound_PitchIncrease) + SkidSound_Pitch;
                    }
                    if (SkidParticle)
                    {
                        if (!SkidParticlePlayingLast)
                        {
                            StartSkidParticle();
                        }
                        SkidParticleEM.rateOverTime = SkidLength_Smoothed * CurrentNumParticles;
                    }
                }
                else
                {
                    if (SkidSoundPlayingLast)
                    { StopSkidSound(); }
                    if (SkidParticlePlayingLast)
                    { StopSkidParticle(); }
                }
            }
            else
            {
                if (SkidSoundPlayingLast)
                { StopSkidSound(); }
                if (SkidParticlePlayingLast)
                { StopSkidParticle(); }
            }
        }
        public void PlayerEnterVehicle()
        {
            SkidVolumeMulti = SkidVolInVehicleMulti;
        }
        public void PlayerExitVehicle()
        {
            SkidVolumeMulti = 1;
        }
        public void StartSkidParticle()
        {
            if (SkidParticle)
            {
                SkidParticleEM.enabled = true;
            }
            SkidParticlePlayingLast = true;
        }
        public void StopSkidParticle()
        {
            if (SkidParticle)
            {
                SkidParticleEM.enabled = false;
            }
            SkidParticlePlayingLast = false;
        }

        public void StartSkidSound()
        {
            if (SkidSound)
            {
                SkidSound.gameObject.SetActive(true);
                SkidSound.time = Random.Range(0, SkidSound.clip.length);
            }
            SkidSoundPlayingLast = true;
        }
        public void StopSkidSound()
        {
            if (SkidSound)
            {
                SkidSound.gameObject.SetActive(false);
            }
            SkidSoundPlayingLast = false;
        }
        private void RotateWheelOwner()
        {
            if (WheelVisual_RotationSource)
                WheelVisual.localRotation = WheelVisual_RotationSource.WheelVisual.localRotation;
            else
            {
                WheelRotation += WheelRotationSpeedRPS * 360f * Time.deltaTime;
                Quaternion newrot = Quaternion.AngleAxis(WheelRotation, Vector3.right);
                WheelVisual.localRotation = newrot;
            }
        }
        private void RotateWheelOther()
        {
            if (WheelVisual_RotationSource)
                WheelVisual.localRotation = WheelVisual_RotationSource.WheelVisual.localRotation;
            else
            {
                float speed = SGVControl.VehicleSpeed;
                WheelRotationSpeedRPS = speed / WheelCircumference;
                if ((bool)SGVControl.GetProgramVariable("MovingForward")) { WheelRotationSpeedRPS = -WheelRotationSpeedRPS; }
                WheelRotation += WheelRotationSpeedRPS * 360f * Time.deltaTime;
                Quaternion newrot = Quaternion.AngleAxis(WheelRotation, Vector3.right);
                WheelVisual.localRotation = newrot;
            }
        }
        public void FallAsleep()
        {
            SkidLength = SkidLength_Smoothed = 0;
            LastTouchedTransform_Speed = Vector3.zero;
            Sleeping = true;
            StopSkidSound();
            StopSkidParticle();
        }
        public void WakeUp()
        {
            Sleeping = false;
            LastTouchedTransform_Speed = Vector3.zero;
        }
        private void GetTouchingTransformSpeed()
        {
            //Surface Movement
            if (SusOut.collider.transform != LastTouchedTransform)
            {
                LastTouchedTransform = SusOut.collider.transform;
                LastTouchedTransform_Position = LastTouchedTransform.position;
                LastTouchedTransform_RB = SusOut.collider.attachedRigidbody;
                if (LastTouchedTransform_RB && !LastTouchedTransform_RB.isKinematic)
                {
                    LastTouchedTransform_Speed = LastTouchedTransform_RB.GetPointVelocity(SusOut.point);
                }
                else
                {
                    LastTouchedTransform_Speed = Vector3.zero;
                }
            }
            else
            {
                if (LastTouchedTransform_RB && !LastTouchedTransform_RB.isKinematic)
                {
                    LastTouchedTransform_Speed = LastTouchedTransform_RB.GetPointVelocity(SusOut.point);
                }
                else
                {
                    LastTouchedTransform_Speed = (LastTouchedTransform.position - LastTouchedTransform_Position) / Time.fixedDeltaTime;
                    LastTouchedTransform_Position = LastTouchedTransform.position;
                }
            }
        }
        private int DoSurface;
        private void CheckSurface()
        {
            //last character of surface object is its type
            int SurfLastChar = SusOut.collider.gameObject.name[SusOut.collider.gameObject.name.Length - 1];
            if (SurfLastChar >= '0' && SurfLastChar <= '9')
            {
                if (SurfaceType != SurfLastChar - '0')
                {
                    SurfaceType = SurfLastChar - '0';
                    ChangeSurface();
                }
            }
            else
            {
                if (SurfaceType != 0)
                {
                    SurfaceType = 0;
                    ChangeSurface();
                }
            }
        }
        private void Suspension_VisualOnly()
        {
            if (Physics.Raycast(WheelPoint.position + WheelPoint.up * ExtraRayCastDistance, -WheelPoint.up, out SusOut, SuspensionDistance + ExtraRayCastDistance, WheelLayers, QueryTriggerInteraction.Ignore))
            {
                // disabled because not worth it just to see wheels spinning properly on other people's cars when they're on a moving object
                // also needs code in other places to work properly (subtract from value from Velocity)
                // GetTouchingTransformSpeed();

                Grounded = true;
                //only check surface for vehicles that aren't mine once every 5 frames
                DoSurface++;
                if (DoSurface > 4)
                {
                    //Surface Type
                    DoSurface = 0;
                    CheckSurface();
                }
                if (SusOut.distance > ExtraRayCastDistance)
                {
                    WheelVisual.position = SusOut.point + (WheelPoint.up * WheelRadius);
                    if (WheelVisual_Ground) { WheelVisual_Ground.position = SusOut.point; }
                }
                else
                {
                    WheelVisual.position = WheelPoint.position + (WheelPoint.up * WheelRadius);
                    if (WheelVisual_Ground) { WheelVisual_Ground.position = WheelPoint.position; }
                }
            }
            else
            {
                StopSkidSound();
                StopSkidParticle();
                WheelVisual.position = WheelPoint.position - (WheelPoint.up * (SuspensionDistance - WheelRadius));
                if (WheelVisual_Ground) { WheelVisual_Ground.position = WheelPoint.position - (WheelPoint.up * (SuspensionDistance)); }
                Grounded = false;
            }
        }
        private void OnEnable()
        {
            WheelRotationSpeedRPS = 0;
            WheelRotationSpeedRPM = 0;
            WheelRotationSpeedSurf = 0;
        }
        public void UpdateOwner()
        {
            bool IsOwner_New = (bool)SGVControl.GetProgramVariable("IsOwner");
            /*             if (IsOwner && !IsOwner_New)
                        {
                            //lose ownership
                        }
                        else  */
            // if (!IsOwner && IsOwner_New)
            // {
            //take ownership
            GroundPointLast = WheelPoint.position; // prevent 1 frame skid from last owned position
            // }
            IsOwner = IsOwner_New;
        }
        public void ResetGrip() { GroundPointLast = WheelPoint.position; }
        public void SyncSkid()
        {
            if (!SyncSkid_Running) { return; }
            if (IsOwner)
            {
                SyncSkid_Running = false;
                return;
            }
            if (SkidLength < SkidSound_Min_THREEQUARTER && SkidLength_Smoothed < SkidSound_Min_THREEQUARTER)
            {
                SkidLength = 0;
                SyncSkid_Running = false;
                return;
            }
            SkidLength_Smoothed = Mathf.SmoothStep(SkidLength_Smoothed, SkidLength, SkidLength_SmoothStep);
            SendCustomEventDelayedFrames(nameof(SyncSkid), 1);
        }
        public override void OnDeserialization()
        {
            if (SkidLength > SkidSound_Min_THREEQUARTER && !SyncSkid_Running)
            {
                SyncSkid_Running = true;
                SyncSkid();
            }
        }
#if UNITY_EDITOR
        [Header("Editor Only, use in play mode")]
        public bool ShowWheelForceLines;
        public float ForceUsedDBG;
        public Vector3 ContactPoint;
        public Vector3 ForceVector;
        private void OnDrawGizmosSelected()
        {
            if (ShowWheelForceLines)
            {
                Gizmos.DrawLine(ContactPoint, ContactPoint + ForceVector);
            }
            Matrix4x4 newmatrix = transform.localToWorldMatrix;
            Gizmos.matrix = Matrix4x4.TRS(newmatrix.GetPosition(), newmatrix.rotation, Vector3.one);// saccwheel does not respect scale
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Vector3.up * ExtraRayCastDistance * .5f, new Vector3(.01f, ExtraRayCastDistance, .01f));
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(-Vector3.up * SuspensionDistance * .5f, new Vector3(.01f, SuspensionDistance, .01f));
            Gizmos.color = Color.white;
            //flatten matrix and draw a sphere to draw a circle for the wheel
            newmatrix = transform.localToWorldMatrix;
            Vector3 scale = new Vector3(0, 1, 1);// Flatten the x scale to make disc + saccwheel does not respect object scale so 1
            Gizmos.matrix = Matrix4x4.TRS(newmatrix.GetPosition(), newmatrix.rotation, scale);
            // UnityEditor.Handles.DrawWireDisc(transform.position + transform.up * WheelRadius, transform.right, WheelRadius); not exposed
            if (running)
                Gizmos.DrawWireSphere(Quaternion.Inverse(transform.rotation) * (WheelVisual.position - transform.position), WheelRadius);
            else
                Gizmos.DrawWireSphere(Vector3.up * WheelRadius, WheelRadius);
        }
#endif
    }
}