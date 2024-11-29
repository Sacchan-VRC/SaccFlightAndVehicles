
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
        [Header("Sync is only used for skid effects,\nset Synchronization Method:\nNone to save bandwidth\nManual to sync skid sounds/effects")]
        [Space(10)]
        public Rigidbody CarRigid;
        public SaccGroundVehicle SGVControl;
        public float SyncInterval = 0.3f;
        public Transform WheelPoint;
        public Transform WheelVisual;
        public Transform WheelVisual_Ground;
        public float SuspensionDistance;
        public float WheelRadius;
        public float SpringForceMulti = .25f;
        public float DampingForceMulti = 0.01111111f;
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
        [Range(0, 2), Tooltip("3 Different ways to calculate amount of engine force used when sliding + accelerating, for testing. 0 = old way, 1 = keeps more energy, 2 = loses more energy")]
        public int SkidRatioMode = 0;
        public float BrakeStrength = 500f;
        public float HandBrakeStrength = 70f;
        [Tooltip("How quickly the wheel matches the speed of the ground when in contact with it, high values will make the car skid more")]
        public float WheelWeight = 0.1f;
        [Tooltip("Only effects DriveWheels. Behaves like engine torque. How much forces on the wheel from the ground can influence the engine speed, low values will make the car skid more")]
        public float EngineInfluence = 225f;
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
        public float EngineRevs = 0;
        public bool Grounded;
        [System.NonSerialized] public float HandBrake;
        [System.NonSerialized] public float Brake;
        public bool Sleeping;
        private int NumStepsSec;
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
        public bool Piloting = false;
        public bool Debugslip = false;
        public bool CurrentlyDistant = true;

#if UNITY_EDITOR
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
            CarRigid.velocity = Vector3.zero;
            CarRigid.angularVelocity = Vector3.zero;
            SendCustomEventDelayedSeconds(nameof(DEBUGAccelCar_2), Time.fixedDeltaTime * 2);
            SGVControl.SetProgramVariable("Revs", 0f);
            accelstartpoint = CarRigid.position;
            EngineRevs = 0;
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
            EngineRevs = 0;
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
            EngineRevs = 0;
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
            EngineRevs = 0;
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
            NumStepsSec = (int)SGVControl.GetProgramVariable("NumStepsSec");
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
        float Steps_Error;
        public void Wheel_FixedUpdate()
        {
            if (Sleeping) { return; }
            if (Piloting && IsDriveWheel)//only do subframe Steps if driving
            {
                float StepsFloat = ((Time.fixedDeltaTime) * NumStepsSec);
                int steps = (int)((Time.fixedDeltaTime) * NumStepsSec);
                Steps_Error += StepsFloat - steps;
                if (Steps_Error > 1)
                {
                    int AddSteps = (int)Mathf.Floor(Steps_Error);//pretty sure this can never be anything but 1 unless refresh rate is changed during play maybe
                    steps += AddSteps;
                    Steps_Error = (Steps_Error - AddSteps);
                }
                Suspension();
                EngineRevs = (float)SGVControl.GetProgramVariable("Revs");
                if (steps < 1) { steps = 1; }//if refresh rate is above NumItsSec just run once per frame, nothing else we can do
                for (int i = 0; i < steps; i++)
                { WheelPhysics(steps); }

                //wheels slow down due to ?friction
                WheelRotationSpeedSurf = Mathf.Lerp(WheelRotationSpeedSurf, 0, 1 - Mathf.Pow(0.5f, Time.fixedDeltaTime * CurrentWheelSlowDown));
                WheelRotationSpeedRPS = WheelRotationSpeedSurf / WheelCircumference;
                WheelRotationSpeedRPM = WheelRotationSpeedRPS * 60f;
            }
            else
            {
                Suspension();
                WheelPhysics(1);
            }
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
                if (Vector3.Angle(SusOut.normal, Vector3.up) < 20 && !SGVControl.Bike_AutoSteer)
                { SusDirection = Vector3.Lerp(Vector3.up, SusOut.normal, (SGVControl.VehicleSpeed / 1f)); }
                else
                { SusDirection = SusOut.normal; }

                CheckSurface();
                //SUSPENSION//
                compression = 1f - ((SusOut.distance - ExtraRayCastDistance) / SuspensionDistance);
                //Spring force: More compressed = more force
                Vector3 SpringForce = (SusDirection/* WheelPoint.up */ * compression * SpringForceMulti) * fixedDT;
                float damping = (compression - compressionLast);
                compressionLast = compression;
                //Damping force: The more the difference in compression between updates, the more force
                Vector3 DampingForce = SusDirection/* WheelPoint.up */ * (damping * DampingForceMulti);
                //these are added together, but both contain deltatime, potential deltatime problem source?
                SusForce = SpringForce + DampingForce;//The total weight on this suspension

                float susdot = Vector3.Dot(transform.up, SusForce);
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
        private void WheelPhysics(int NumSteps)
        {
            float WheelPhysicsDelta = Time.fixedDeltaTime / NumSteps;
            float ForwardSpeed = 0f;
            float ForwardSideRatio = 0f;
            float ForceUsed = 0f;
            float ForwardSlip = 0f;
            Vector3 SkidVectorFX = Vector3.zero;


            if (IsDriveWheel && !GearNeutral)
            {
                WheelRotationSpeedRPM = Mathf.Lerp(WheelRotationSpeedRPM, EngineRevs * _GearRatio, 1 - Mathf.Pow(0.5f, (1f - Clutch) * ClutchStrength * WheelPhysicsDelta)  /* * Time.fixedDeltaTime * 90f */);
                WheelRotationSpeedRPS = WheelRotationSpeedRPM / 60f;
                WheelRotationSpeedSurf = WheelCircumference * WheelRotationSpeedRPS;
            }
            float WheelRotationSpeedSurfPrev = WheelRotationSpeedSurf;
            WheelRotationSpeedSurf = Mathf.MoveTowards(WheelRotationSpeedSurf, 0f, WheelPhysicsDelta * Brake * BrakeStrength);
            WheelRotationSpeedSurf = Mathf.MoveTowards(WheelRotationSpeedSurf, 0f, WheelPhysicsDelta * HandBrake * HandBrakeStrength);
            WheelRotationSpeedRPS = WheelRotationSpeedSurf / WheelCircumference;
            WheelRotationSpeedRPM = WheelRotationSpeedRPS * 60f;

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
            // float gripUsed = 0;
            if (Grounded)
            {
                //GRIP//
                //Wheel's velocity vector projected to be only forward/back
                Vector3 WheelForwardSpeed = Vector3.ProjectOnPlane(PointVelocity, WheelPoint.right);
                WheelForwardSpeed -= Vector3.Project(WheelForwardSpeed, WheelGroundUp);
                float ForwardSpeed_abs = WheelForwardSpeed.magnitude;
                ForwardSpeed = ForwardSpeed_abs;
                if (Vector3.Dot(WheelForwardSpeed, WheelPoint.forward) < 0f)
                {
                    ForwardSpeed = -ForwardSpeed;
                }
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
                    {
                        ForwardSideRatio = Vector3.Dot(ForwardSkid / FullSkidMag, FullSkid / FullSkidMag);
                    }
                    else
                    {
                        //these might produce different/more arcadey feel idk
                        float ForwardLen = ForwardSkid.magnitude;
                        float SideLen = SideSkid.magnitude;
                        float fullLen = ForwardLen + SideLen;
                        if (SkidRatioMode == 1)
                        {
                            ForwardSideRatio = ForwardLen / fullLen;
                        }
                        else
                        {
                            ForwardSideRatio = ForwardLen / FullSkidMag;
                        }
                    }
                }
                Vector3 GripForce3;
                //SusForce has deltatime built in
                float SusForceMag = SusForce.magnitude / NumSteps;
                float MaxGrip = (SusForceMag * CurrentGrip) / WheelPhysicsDelta;
                float MaxGripLat = MaxGrip * LateralGrip;
                Vector3 GripForceForward;
                Vector3 GripForcLat;

                if (SeparateLongLatGrip)
                {
                    float evalskid = ForwardSkid.magnitude / MaxGrip;
                    float gripPc = GripCurve.Evaluate(evalskid);
                    GripForceForward = -ForwardSkid.normalized * gripPc * MaxGrip;
                    // gripUsed = (gripPc / (evalskid > 0 ? evalskid : 1)) * ForwardSideRatio;

                    float evalskidLat = SideSkid.magnitude / MaxGripLat;
                    float gripPcLat = GripCurveLateral.Evaluate(evalskidLat);
                    GripForcLat = -SideSkid.normalized * gripPcLat * MaxGripLat;
                    GripForce3 = (GripForceForward + GripForcLat) * WheelPhysicsDelta;
                }
                else
                {
                    float evalskid = FullSkid.magnitude / MaxGrip;
                    float gripPc = GripCurve.Evaluate(evalskid);
                    GripForceForward = -FullSkid.normalized * gripPc * MaxGrip;
                    // gripUsed = (gripPc / (evalskid > 0 ? evalskid : 1)) * ForwardSideRatio;

                    float evalskidLat = FullSkid.magnitude / MaxGripLat;
                    float gripPcLat = GripCurveLateral.Evaluate(evalskidLat);
                    GripForcLat = -FullSkid.normalized * gripPcLat * MaxGripLat;
                    GripForce3 = Vector3.Lerp(GripForcLat, GripForceForward, ForwardSideRatio) * WheelPhysicsDelta;
                }

                // two way forces for wheel grip on rigidbodies, many problems
                /* float WeightRatio = 1;
                if (LastTouchedTransform_RB && !LastTouchedTransform_RB.isKinematic)
                {

                    WeightRatio = CarRigid.mass / (CarRigid.mass + LastTouchedTransform_RB.mass);
                    LastTouchedTransform_RB.AddForceAtPosition((-GripForce3 * WeightRatio), SusOut.point, ForceMode.VelocityChange);
                    GripForce3 *= 1 - WeightRatio;
                } */
                //Add the Grip forces to the rigidbody
                CarRigid.AddForceAtPosition(GripForce3, SusOut.point, ForceMode.VelocityChange);
                ForceUsed = GripForce3.magnitude * ForwardSideRatio * 100;
#if UNITY_EDITOR
                // draws the gripcurve at world origin
                // Debug.DrawLine(new Vector3(ForwardSkid.magnitude, 0, 0), new Vector3(ForwardSkid.magnitude, GripForceForward.magnitude, 0), Color.green, 5f);
                // Debug.DrawLine(new Vector3(ForwardSkid.magnitude, 0, 0), new Vector3(ForwardSkid.magnitude, -SusForceMag * 10, 0), Color.red, 5f);
                ForceVector = GripForce3;
                ForceUsedDBG = ForceUsed;
#endif
                //DEBUG
                /*                 if (PrintDebugValues)
                                {
                                    Debug.Log(string.Concat("ForwardSlip: ", ForwardSlip.ToString()));
                                    Debug.Log(string.Concat("FORWARDSKIDMAG: ", ForwardSkid.magnitude.ToString()));
                                    Debug.Log(string.Concat("SIDESKIDMAG: ", SideSkid.magnitude.ToString()));
                                    Debug.Log(string.Concat("FULLSKIDMAG: ", FullSkidMag.ToString()));
                                    Debug.Log(string.Concat("GripForce3.magnitude / Time.fixedDeltaTime: ", (GripForce3.magnitude / Time.fixedDeltaTime).ToString()));
                                    Debug.Log(string.Concat("GripForce: ", (ForceUsed / Time.fixedDeltaTime).ToString()));
                                    Debug.Log(string.Concat("SusForce.magnitude / Time.fixedDeltaTime: ", (SusForce.magnitude / Time.fixedDeltaTime).ToString()));//no delta problems
                                    Debug.Log(string.Concat("(FullSkidMag) / (Grip * (SusForce.magnitude / Time.fixedDeltaTime / 90f): ", (FullSkidMag / (_Grip * (SusForce.magnitude / Time.fixedDeltaTime / 90f))).ToString()));
                                    Debug.Log(string.Concat("_CRUVEEVAL : ", GripCurve.Evaluate((FullSkidMag) / (_Grip * (SusForce.magnitude / Time.fixedDeltaTime _/ 90f))).ToString()));
                                } */
                //ENDOFDEBUG

                //move wheel rotation speed towards its ground speed along its forward axis based on how much of it's forward skid that it gripped
                if (HandBrake != 1f)
                {
                    // ForwardSpeed is this frame's speed, really we should be using next frames speed
                    // (after the added force is calculated, but we can't do that with substeps anyway)
                    // I think it's better to use a lerp and the commented out 'gripUsed' as T but it doesn't seem to make much difference in practice
                    WheelRotationSpeedSurf = Mathf.MoveTowards(WheelRotationSpeedSurf, ForwardSpeed, (ForceUsed / WheelWeight));
                    WheelRotationSpeedRPS = WheelRotationSpeedSurf / WheelCircumference;
                    WheelRotationSpeedRPM = WheelRotationSpeedRPS * 60f;
                    // if (PrintDebugValues)
                    // {
                    //     Debug.Log(string.Concat("(Mathf.Abs(ForwardSlip)): ", ((Mathf.Abs(ForwardSlip))).ToString()));
                    //     Debug.Log(string.Concat("SlipGrip / Time.deltaTime: ", (SlipGrip / Time.deltaTime).ToString()));
                    // }
                }
                SkidVectorFX = FullSkid;
            }
            //move engine speed towards wheel speed
            if (IsDriveWheel && !GearNeutral)
            {
                SGVControl.Revs = Mathf.MoveTowards(SGVControl.Revs, (WheelRotationSpeedRPM / _GearRatio), ((ForceUsed * Mathf.Abs(_GearRatio)) * EngineInfluence * (1f - Clutch)));
            }

            SkidLength = SkidVectorFX.magnitude;//doesn't need to be done every substep but i don't think there's another way
        }
        private void LateUpdate()
        {
            if (!Sleeping)
            {
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
            WheelRotation += WheelRotationSpeedRPS * 360f * Time.deltaTime;
            Quaternion newrot = Quaternion.AngleAxis(WheelRotation, Vector3.right);
            WheelVisual.localRotation = newrot;
        }
        private void RotateWheelOther()
        {
            float speed = SGVControl.VehicleSpeed;
            WheelRotationSpeedRPS = speed / WheelCircumference;
            if ((bool)SGVControl.GetProgramVariable("MovingForward")) { WheelRotationSpeedRPS = -WheelRotationSpeedRPS; }
            WheelRotation += WheelRotationSpeedRPS * 360f * Time.deltaTime;
            Quaternion newrot = Quaternion.AngleAxis(WheelRotation, Vector3.right);
            WheelVisual.localRotation = newrot;
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
        }
#endif
    }
}