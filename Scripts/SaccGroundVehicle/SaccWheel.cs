
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [DefaultExecutionOrder(1500)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class SaccWheel : UdonSharpBehaviour
    {
        public Rigidbody CarRigid;
        public SaccGroundVehicle SGVControl;
        public Transform WheelPoint;
        public Transform WheelVisual;
        public float SuspensionDistance;
        public float WheelRadius;
        public float SpringForceMulti = .25f;
        public float DampingForceMulti = 0.01111111f;
        public float DampingForce_BottomOutMulti = 5f;
        [Tooltip("Limit suspension force so that the car doesn't fly up when going up large step instantly")]
        public float MaxSuspensionForce = .2f;
        //[Tooltip("Limit Damping force so that the car doesn't behave strangely when leaving a ramp")]
        private float MaxNegativeDamping = 0f;
        [Tooltip("Extra height on the raycast origin to prevent the wheel from sticking through the floor")]
        public float ExtraRayCastDistance = .5f;
        public float Grip = 7f;
        public AnimationCurve GripCurve = AnimationCurve.Linear(0, 1, 1, 1);
        [Tooltip("Torque, kindof. How quickly the wheel matches the speed of the ground when in contact with it")]
        public float WheelWeight = 0.1f;
        public float BrakeStrength = 500f;
        public float HandBrakeStrength = 7f;
        public LayerMask WheelLayers;
        public float[] SurfaceType_Grips = { 1f, 0.7f, 0.2f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };
        public float[] SurfaceType_Slowdown = { 0.1f, 4f, 0.05f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f };
        public AudioSource[] SurfaceType_SkidSounds;
        public ParticleSystem[] SurfaceType_SkidParticles;
        public ParticleSystem.EmissionModule[] SurfaceType_SkidParticlesEM;
        public float[] SurfaceType_SkidParticles_NumParticles = { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
        public float ClutchStrength = .33f;
        [Tooltip("Lower number = less skid required for sound to start")]
        public float SkidSound_Min = 3f;
        [Tooltip("How quickly volume increases as skid speed increases")]
        public float SkidSound_VolumeIncrease = 0.5f;
        [Tooltip("How quickly pitch increases as skid speed increases")]
        public float SkidSound_PitchIncrease = 0.02f;
        public float SkidSound_Pitch = 1f;
        [Tooltip("Reduce volume of skid swhilst in the car")]
        public float SkidVolInVehicleMulti = .4f;
        [Tooltip("Skip sound and skid effects completely")]
        public bool DisableEffects = false;
        [Range(0, 2), Tooltip("3 Different ways to calculate amount of engine force used when sliding + accelerating, for testing. 0 = old way, 1 = keeps more energy, 2 = loses more energy")]
        public int SkidRatioMode = 0;
        [Header("Drive Wheels Only")]
        [Tooltip("How much the wheel slowing down/speeding up changes the engine speed")]
        public float EngineInfluence = 225f;
        [Header("Debug")]
        public float CurrentGrip = 7f;
        public float CurrentNumParticles = 0f;
        public float CurrentWheelSlowDown = 0f;
        private bool IsDriveWheel = false;
        private AudioSource SkidSound;
        private ParticleSystem SkidParticle;
        private ParticleSystem.EmissionModule SkidParticleEM;
        private Vector3 SkidVectorFX;
        [UdonSynced(UdonSyncMode.Linear)] private float SkidLength;
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
                if (value == 0f)
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
        public bool Debugslip = false;
        public bool CurrentlyDistant = true;

        //DEBUG
        // public bool SetVel;
        // public bool PrintDebugValues;
        // public Vector3 DebugMoveSpeed = Vector3.zero;
        // public bool DebugFreezeWheel = false;
        // public float AccelTestLength = 4;
        // public float DistResultPush;
        // public float DistResultAccel;
        // public float SpeedResultAccel;
        // private Vector3 pushstartpoint;
        // private Vector3 accelstartpoint;
        // public void DEBUGPushCar()
        // {
        //     pushstartpoint = CarRigid.position;
        //     CarRigid.velocity = CarRigid.transform.TransformDirection(DebugMoveSpeed);
        //     WheelRotationSpeedRPM = 0;
        //     WheelRotationSpeedRPS = 0;
        //     WheelRotationSpeedSurf = 0;
        // }
        // public void DEBUGAccelCar()
        // {
        //     CarRigid.velocity = Vector3.zero;
        //     CarRigid.angularVelocity = Vector3.zero;
        //     SendCustomEventDelayedSeconds(nameof(DEBUGAccelCar_2), Time.fixedDeltaTime * 2);
        //     SGVControl.SetProgramVariable("Revs", 0f);
        //     accelstartpoint = CarRigid.position;
        //     EngineRevs = 0;
        //     WheelRotationSpeedRPM = 0;
        //     WheelRotationSpeedRPS = 0;
        //     WheelRotationSpeedSurf = 0;
        // }
        // public void DEBUGAccelCar_2()
        // {
        //     CarRigid.velocity = Vector3.zero;
        //     CarRigid.angularVelocity = Vector3.zero;
        //     SendCustomEventDelayedSeconds(nameof(DEBUGMeasureAccel), AccelTestLength);
        //     SGVControl.SetProgramVariable("ACCELTEST", true);
        //     SGVControl.SetProgramVariable("Revs", 0f);
        //     accelstartpoint = CarRigid.position;
        //     EngineRevs = 0;
        //     WheelRotationSpeedRPM = 0;
        //     WheelRotationSpeedRPS = 0;
        //     WheelRotationSpeedSurf = 0;
        // }
        // private bool boolRevingUp;
        // private int RevUpCount;
        // public float RevUpResult;
        // public float RPMTestTarget = 500;
        // public void DEBUGAccelCar_Revup()
        // {
        //     CarRigid.velocity = Vector3.zero;
        //     CarRigid.angularVelocity = Vector3.zero;
        //     SendCustomEventDelayedSeconds(nameof(DEBUGAccelCar_Revup_2), Time.fixedDeltaTime * 2);
        //     SGVControl.SetProgramVariable("Revs", 0f);
        //     accelstartpoint = CarRigid.position;
        //     EngineRevs = 0;
        //     WheelRotationSpeedRPM = 0;
        //     WheelRotationSpeedRPS = 0;
        //     WheelRotationSpeedSurf = 0;
        // }
        // public void DEBUGAccelCar_Revup_2()
        // {
        //     CarRigid.velocity = Vector3.zero;
        //     CarRigid.angularVelocity = Vector3.zero;
        //     SGVControl.SetProgramVariable("ACCELTEST", true);
        //     SGVControl.SetProgramVariable("Revs", 0f);
        //     accelstartpoint = CarRigid.position;
        //     EngineRevs = 0;
        //     WheelRotationSpeedRPM = 0;
        //     WheelRotationSpeedRPS = 0;
        //     WheelRotationSpeedSurf = 0;
        //     boolRevingUp = true;
        //     RevUpCount = 0;
        // }
        // public void DEBUGMeasureAccel()
        // {
        //     SGVControl.SetProgramVariable("ACCELTEST", false);
        //     DistResultAccel = Vector3.Distance(accelstartpoint, CarRigid.position);
        //     SpeedResultAccel = CarRigid.velocity.magnitude;
        // }
        //ENDOFDEBUG
        void Start()
        {
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
            DoSurface = Random.Range(0, 6);
        }
        public void ChangeSurface()
        {
            if (SurfaceType < 0) { return; }
            CurrentGrip = Grip * SurfaceType_Grips[SurfaceType];
            CurrentWheelSlowDown = SurfaceType_Slowdown[SurfaceType];
            CurrentNumParticles = SurfaceType_SkidParticles_NumParticles[SurfaceType];
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
        private void FixedUpdate()
        {
            if (!IsOwner || Sleeping) { return; }
            RaycastHit SusOut;
            float compression = 0f;
            float ForwardSpeed = 0f;
            float ForwardSideRatio = 0f;
            float ForceUsed = 0f;
            float ForwardSlip = 0f;
            Vector3 SusForce = Vector3.zero;


            if (IsDriveWheel && !GearNeutral)
            {
                //somehow, not using deltatime here, and then also not using it at the part that updates SGVControl.Revs near the end of the fucntion works better than have it in both places.
                WheelRotationSpeedRPM = Mathf.Lerp(WheelRotationSpeedRPM, EngineRevs * _GearRatio, (1f - Clutch) * (1f - HandBrake) * ClutchStrength /* * Time.fixedDeltaTime * 90f */);
                WheelRotationSpeedRPS = WheelRotationSpeedRPM / 60f;
                WheelRotationSpeedSurf = WheelCircumference * WheelRotationSpeedRPS;
            }
            float WheelRotationSpeedSurfLPrev = WheelRotationSpeedSurf;
            WheelRotationSpeedSurf = Mathf.MoveTowards(WheelRotationSpeedSurf, 0f, Time.fixedDeltaTime * Brake * BrakeStrength);
            WheelRotationSpeedSurf = Mathf.Lerp(WheelRotationSpeedSurf, 0f, Time.fixedDeltaTime * HandBrake * HandBrakeStrength);
            WheelRotationSpeedRPS = WheelRotationSpeedSurf / WheelCircumference;
            WheelRotationSpeedRPM = WheelRotationSpeedRPS * 60f;

            //DEBUG
            // DistResultPush = Vector3.Distance(pushstartpoint, CarRigid.position);
            // if (SetVel) CarRigid.velocity = DebugMoveSpeed;
            // if (DebugFreezeWheel)
            // {
            //     WheelRotationSpeedRPM = 0;
            //     WheelRotationSpeedRPS = 0;
            //     WheelRotationSpeedSurf = 0;
            // }
            // if (boolRevingUp)
            // {
            //     RevUpCount++;
            //     if (WheelRotationSpeedRPM > RPMTestTarget)
            //     {
            //         boolRevingUp = false;
            //         RevUpResult = Time.fixedDeltaTime * (float)RevUpCount;

            //         SGVControl.SetProgramVariable("ACCELTEST", false);
            //     }
            // }
            //ENDOFDEBUG
            if (Physics.Raycast(WheelPoint.position + WheelPoint.up * ExtraRayCastDistance, -WheelPoint.up, out SusOut, SuspensionDistance + ExtraRayCastDistance, WheelLayers, QueryTriggerInteraction.Ignore))
            {
#if UNITY_EDITOR
                ContactPoint = SusOut.point;
#endif
                Grounded = true;
                //SusDirection is closer to straight up the slower vehicle is moving, so that it can stop
                if (Vector3.Angle(SusOut.normal, Vector3.up) < 20)
                {
                    SusDirection = Vector3.Lerp(Vector3.up, SusOut.normal, (SGVControl.VehicleSpeed / 1f));
                }
                else
                {
                    SusDirection = SusOut.normal;
                }

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
                //SUSPENSION//
                compression = 1f - ((SusOut.distance - ExtraRayCastDistance) / SuspensionDistance);
                //Spring force: More compressed = more force
                Vector3 SpringForce = (SusDirection/* WheelPoint.up */ * compression * SpringForceMulti) * Time.fixedDeltaTime;
                float damping = (compression - compressionLast);
                compressionLast = compression;
                if (compression > 1f)//bottomed out
                {
                    damping *= DampingForce_BottomOutMulti;
                }
                if (damping < -MaxNegativeDamping)
                {
                    damping = -MaxNegativeDamping;
                }
                //Damping force: The more the difference in compression between updates, the more force
                Vector3 DampingForce = SusDirection/* WheelPoint.up */ * (damping * DampingForceMulti);
                SusForce = SpringForce + DampingForce;//The total weight on this suspension
                                                      //limit sus force
                if (SusForce.magnitude / Time.fixedDeltaTime > MaxSuspensionForce)
                {
                    SusForce *= MaxSuspensionForce * Time.fixedDeltaTime / (SusForce).magnitude;
                }

                CarRigid.AddForceAtPosition(SusForce, WheelPoint.position, ForceMode.VelocityChange);

                //set wheel's visual position
                if (SusOut.distance > ExtraRayCastDistance)
                {
                    WheelVisual.position = SusOut.point + (WheelPoint.up * WheelRadius);
                }
                else
                {
                    WheelVisual.position = WheelPoint.position + (WheelPoint.up * WheelRadius);
                }
                //END OF SUSPENSION//

                //GRIP//
                //Wheel's velocity vector projected to the normal of the ground
                //WheelGroundSpeed is speed of the ground below the wheel
                Vector3 WheelGroundSpeed = Vector3.ProjectOnPlane(CarRigid.GetPointVelocity(SusOut.point), SusOut.normal);
                //Wheel's velocity vector projected to be only forward/back
                Vector3 WheelForwardSpeed = Vector3.ProjectOnPlane(WheelGroundSpeed, WheelPoint.right);
                float ForwardSpeed_abs = WheelForwardSpeed.magnitude;
                ForwardSpeed = ForwardSpeed_abs;
                if (Vector3.Dot(WheelForwardSpeed, WheelPoint.forward) < 0f)
                {
                    ForwardSpeed = -ForwardSpeed;
                }
                ForwardSlip = ForwardSpeed - WheelRotationSpeedSurf;
                //How much the wheel is slipping (difference between speed of wheel rotation at it's surface, and the speed of the ground beneath it), as a vector3
                Vector3 ForwardSkid = Vector3.ProjectOnPlane(WheelPoint.forward, SusOut.normal).normalized * ForwardSlip;

                Vector3 SideSkid = Vector3.ProjectOnPlane(WheelGroundSpeed, WheelPoint.forward);

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

                //SkidVectorFX is just used for effects, it has to take the forward after the grip forces
                //because we don't know how much of the forward skid will be gripped yet
                SkidVectorFX = SideSkid;

                Vector3 GripForce3;
                //SusForce has deltatime built in
                float SusForceMag = SusForce.magnitude;
                GripForce3 = -FullSkid.normalized * GripCurve.Evaluate((FullSkidMag) / (CurrentGrip * (SusForceMag / Time.fixedDeltaTime / 90f))) * CurrentGrip * SusForceMag;
                //Add the Grip forces to the rigidbody
                //Why /90? Who knows! Maybe offsetting something to do with delta time, no idea why it's needed.
                CarRigid.AddForceAtPosition(GripForce3 / 90f, SusOut.point, ForceMode.VelocityChange);
                ForceUsed = (GripForce3.magnitude * ForwardSideRatio);
#if UNITY_EDITOR
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
            }
            else
            {
                //wheel not touching ground
                if (SkidSoundPlayingLast) { StopSkidSound(); }
                if (SkidParticlePlayingLast) { StopSkidParticle(); }
                WheelVisual.position = WheelPoint.position - (WheelPoint.up * (SuspensionDistance - WheelRadius));
                Grounded = false;
                compressionLast = 0f;
            }

            //move wheel rotation speed towards its ground speed along its forward axis based on how much of it's forward skid that it gripped
            if (Grounded && HandBrake != 1f)
            {
                //this almost always goes to else, which means the wheels almost never visually wheelspin, but it still sounds like they do (because SkidVectorFX's forward componant is a hack).
                //setting wheelweight very high does allow for proper wheel spins, but the vehicle physics gets ruined
                WheelRotationSpeedSurf = Mathf.MoveTowards(WheelRotationSpeedSurf, ForwardSpeed, (ForceUsed / WheelWeight));
                WheelRotationSpeedRPS = WheelRotationSpeedSurf / WheelCircumference;
                WheelRotationSpeedRPM = WheelRotationSpeedRPS * 60f;
                // if (PrintDebugValues)
                // {
                //     Debug.Log(string.Concat("(Mathf.Abs(ForwardSlip)): ", ((Mathf.Abs(ForwardSlip))).ToString()));
                //     Debug.Log(string.Concat("SlipGrip / Time.deltaTime: ", (SlipGrip / Time.deltaTime).ToString()));
                // }
            }
            else
            {
                SkidVectorFX += (Vector3.forward * Mathf.Abs(ForwardSlip));
            }
            //wheels slow down due to ?friction
            WheelRotationSpeedSurf = Mathf.Lerp(WheelRotationSpeedSurf, 0, Time.fixedDeltaTime * CurrentWheelSlowDown);
            WheelRotationSpeedRPS = WheelRotationSpeedSurf / WheelCircumference;
            WheelRotationSpeedRPM = WheelRotationSpeedRPS * 60f;

            //move engine speed towards wheel speed
            //lerp engine speed towards wheel speed if the handbrake is on
            if (IsDriveWheel && !GearNeutral)
            {
                //gear ratio shouldnt be used here but would have to change engineinfluence in all prefabs to compensate
                SGVControl.Revs = Mathf.MoveTowards(SGVControl.Revs, (WheelRotationSpeedRPM / _GearRatio), ((ForceUsed * Mathf.Abs(_GearRatio)) * EngineInfluence * (1f - Clutch)));
                //lerp engine towards wheel speed of handbrake is being used
                SGVControl.Revs = Mathf.Lerp(SGVControl.Revs, (WheelRotationSpeedRPM / _GearRatio), HandBrake * (1f - Clutch) * 90f * Time.fixedDeltaTime);
            }
            SkidVectorFX += Vector3.forward * (WheelRotationSpeedSurf - WheelRotationSpeedSurfLPrev);//works but stupid
            //this would be a better way to do it, but slipgrip is broken so you would never get forward skidding sounds
            //SkidVectorFX += Vector3.forward * SlipGrip;
            SkidLength = SkidVectorFX.magnitude;
        }
        private void LateUpdate()
        {
            if (!Sleeping)
            {
                if (IsOwner)
                {
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
                    float skidvol = Mathf.Min((SkidLength - SkidSound_Min) * SkidSound_VolumeIncrease, 1);
                    if (skidvol > 0)
                    {
                        if (SkidSound)
                        {
                            if (!SkidSoundPlayingLast)
                            {
                                StartSkidSound();
                            }
                            SkidSound.volume = skidvol * SkidVolumeMulti;
                            SkidSound.pitch = (SkidLength * SkidSound_PitchIncrease) + SkidSound_Pitch;
                        }
                        if (SkidParticle)
                        {
                            if (!SkidParticlePlayingLast)
                            {
                                StartSkidParticle();
                            }
                            SkidParticleEM.rateOverTime = SkidLength * CurrentNumParticles;
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
            float speed = (float)SGVControl.GetProgramVariable("VehicleSpeed");
            WheelRotationSpeedRPS = speed / WheelCircumference;
            if ((bool)SGVControl.GetProgramVariable("MovingForward")) { WheelRotationSpeedRPS = -WheelRotationSpeedRPS; }
            WheelRotation += WheelRotationSpeedRPS * 360f * Time.deltaTime;
            Quaternion newrot = Quaternion.AngleAxis(WheelRotation, Vector3.right);
            WheelVisual.localRotation = newrot;
        }
        public void FallAsleep()
        {
            SkidLength = 0;
            Sleeping = true;
            StopSkidSound();
            StopSkidParticle();
        }
        public void WakeUp()
        {
            Sleeping = false;
        }
        private int DoSurface;
        private void Suspension_VisualOnly()
        {
            RaycastHit SusOut;
            if (Physics.Raycast(WheelPoint.position + WheelPoint.up * ExtraRayCastDistance, -WheelPoint.up, out SusOut, SuspensionDistance + ExtraRayCastDistance, WheelLayers, QueryTriggerInteraction.Ignore))
            {
                Grounded = true;
                //last character of surface object is its type
                //only check surface for vehicles that aren't mine once every 5 frames
                DoSurface++;
                if (DoSurface > 4)
                {
                    DoSurface = 0;
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
                if (SusOut.distance > ExtraRayCastDistance)
                {
                    WheelVisual.position = SusOut.point + (WheelPoint.up * WheelRadius);
                }
                else
                {
                    WheelVisual.position = WheelPoint.position + (WheelPoint.up * WheelRadius);
                }
            }
            else
            {
                StopSkidSound();
                StopSkidParticle();
                WheelVisual.position = WheelPoint.position - (WheelPoint.up * (SuspensionDistance - WheelRadius));
                Grounded = false;
            }
        }
        private void OnEnable()
        {
            WheelRotationSpeedRPS = 0;
            WheelRotationSpeedRPM = 0;
            WheelRotationSpeedSurf = 0;
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