using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class EXT_Turret : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        [Tooltip("Transform to base your controls on, should be facing the same direction as the seat. If left empty it will be set to the Horizontal Rotator.")]
        public Transform ControlsRoot;
        public Transform TurretRotatorHor;
        public Transform TurretRotatorVert;
        public float TurnSpeedMultiX = 600;
        public float TurnSpeedMultiY = 600;
        [Tooltip("Joystick sensitivity. Angle at which joystick will reach maximum deflection in VR")]
        public Vector3 MaxJoyAngles = new Vector3(45, 45, 45);
        [Tooltip("Lerp rotational inputs by this amount when used in desktop mode so the aim isn't too twitchy")]
        public float TurningResponseDesktop = 2f;
        [Tooltip("Rotation slowdown per frame")]
        [Range(0, 100)]
        public float TurnFriction = 4f;
        [Tooltip("Angle above the horizon that this gun can look")]
        public float UpAngleMax = 89;
        [Tooltip("Angle below the horizon that this gun can look")]
        public float DownAngleMax = 0;
        [Tooltip("Angle that this gun can look to the left and right, set to 180 to freely spin")]
        public float SideAngleMax = 180;
        [Tooltip("In seconds")]
        [Range(0.05f, 1f)]
        public float updateInterval = 0.25f;
        [Tooltip("Stabilize the turrets movement? Easier to aim in moving vehicles")]
        public bool Stabilize;

        [Tooltip("Audio source that plays when rotating")]
        public AudioSource RotatingSound;
        [SerializeField] bool PlayRotatingSoundForOthers = false;
        [SerializeField] bool RotatingSound_HorizontalOnly;
        public float RotatingSoundMulti = .02f;
        [Header("Networking:")]
        [Tooltip("How much vehicle accelerates extra towards its 'raw' position when not owner in order to correct positional errors")]
        public float CorrectionTime = 8f;
        [Tooltip("How quickly non-owned vehicle's velocity vector lerps towards its new value")]
        public float SpeedLerpTime = 25f;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [System.NonSerializedAttribute] public SAV_PassengerFunctionsController PassengerFunctionsControl;
        private Transform VehicleTransform;
        private float InputXKeyb;
        private float InputYKeyb;
        private float RotationSpeedX = 0f;
        private float RotationSpeedY = 0f;
        private Vector3 AmmoBarScaleStart;
        private bool InEditor = true;
        private bool RGripLastFrame;
        private bool InVR;
        private VRCPlayerApi localPlayer;

        private double O_LastUpdateTime;
        private double L_UpdateTime;
        private double nextUpdateTime = double.MaxValue;
        private double StartupTime;
        private Vector2 LastGunRotationSpeed;
        private Vector2 GunRotationSpeed;
        private Vector2 GunRotationAcceleration;
        private Vector2 L_LastGunRotation;
        private double O_LastUpdateTime2;
        private float SmoothingTimeDivider;
        private bool ClampHor = false;
        private bool Occupied;
        private bool Manning;
        private float RotateSoundVol;
        private Rigidbody VehicleRigid;
        Quaternion ControlsRotLastFrame;
        Quaternion JoystickZeroPoint;
        [System.NonSerializedAttribute] public bool IsOwner;//required by the bomb script, not actually related to being the owner of the object
        private Vector3 LastForward_HOR;
        private Vector3 LastForward_VERT;
        [UdonSynced(UdonSyncMode.None)] private Vector2 O_GunRotation;
        private Vector2 L_GunRotation;
        [UdonSynced] private double O_UpdateTime;
        private Vector2 SND_RotLerper;
        Vector3 ExtrapDirection_Smooth;
        float updateDelta = 0.25f;
        private double lastframetime_extrap;
        private double StartupServerTime;
        private double StartupLocalTime;
        private float ErrorLastFrame;
        [Header("For debug, leave empty for normal use")]
        [SerializeField] private Transform HORSYNC;
        [SerializeField] private Transform VERTSYNC;
#if UNITY_EDITOR
        [SerializeField] private Transform HORSYNC_RAW;
        [SerializeField] private Transform VERTSYNC_RAW;
#endif
        public void SFEXT_L_EntityStart()
        {
#if UNITY_EDITOR
            if (HORSYNC || VERTSYNC)
            { NetTestMode = true; }
            else { NetTestMode = false; }
#endif
            if (!HORSYNC) { HORSYNC = TurretRotatorHor; }
            if (!VERTSYNC) { VERTSYNC = TurretRotatorVert; }
            localPlayer = Networking.LocalPlayer;
            InVR = EntityControl.InVR;
            InEditor = localPlayer == null;
            VehicleTransform = EntityControl.transform;
            VehicleRigid = EntityControl.GetComponent<Rigidbody>();
            if (!ControlsRoot) { ControlsRoot = TurretRotatorHor; }

            SmoothingTimeDivider = 1f / updateInterval;
            if (SideAngleMax < 180f) { ClampHor = true; }

            if (RotatingSound) { RotateSoundVol = RotatingSound.volume; }
            InitSyncValues();
        }
        public void SFEXT_O_PilotEnter()
        {
            SendCustomEventDelayedFrames(nameof(ResetSyncTimes), 1);// the frame the pilot enters is more likely to be a longer frame, so reset afterwards
            IsOwner = true;
            Manning = true;
            InVR = EntityControl.InVR;
            nextUpdateTime = StartupServerTime + (double)(Time.time - StartupLocalTime) - .01f;
        }
        public void SFEXT_G_PilotEnter()
        {
            gameObject.SetActive(true);
            if (RotatingSound && (PlayRotatingSoundForOthers || Manning))
            { RotatingSound.Play(); }
        }
        public void SFEXT_O_PilotExit()
        {
            IsOwner = false;
            SendCustomEventDelayedFrames(nameof(ManningFalse), 1);
            RotationSpeedX = 0;
            RotationSpeedY = 0;
        }
        public void SFEXT_G_PilotExit()
        {
            gameObject.SetActive(false);
            if (RotatingSound && RotatingSound.isPlaying) { RotatingSound.Stop(); }
        }
        public void ManningFalse()
        { Manning = false; }//if this is in SFEXT_O_UserExit rather than here update runs for one frame with it false before it's disabled
        public void SFEXT_G_RespawnButton()
        {
            ResetSyncTimes();
            HORSYNC.localRotation = Quaternion.identity;
            VERTSYNC.localRotation = Quaternion.identity;
        }
        private void Update()
        {
            if (Manning)
            {
                //ROTATION
                double time = StartupServerTime + (double)(Time.time - StartupLocalTime);
                float deltaTime = Time.deltaTime;
                if (deltaTime > .099f)
                {
                    ResetSyncTimes();
                    //no antiwarp stuff, probably not needed
                }
                //get inputs
                int Wf = Input.GetKey(KeyCode.W) ? 1 : 0; //inputs as ints
                int Sf = Input.GetKey(KeyCode.S) ? -1 : 0;
                int Af = Input.GetKey(KeyCode.A) ? -1 : 0;
                int Df = Input.GetKey(KeyCode.D) ? 1 : 0;

                float RGrip = 0;
                float RTrigger = 0;
                float LTrigger = 0;
                if (!InEditor)
                {
                    RTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
                    LTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
                    RGrip = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
                }
                //virtual joystick
                Vector2 VRPitchYawInput = Vector2.zero;
                if (InVR)
                {
                    if (RGrip > 0.75)
                    {
                        Quaternion RotDif = ControlsRoot.rotation * Quaternion.Inverse(ControlsRotLastFrame);//difference in vehicle's rotation since last frame
                        JoystickZeroPoint = RotDif * JoystickZeroPoint;//zero point rotates with the plane so it appears still to the pilot
                        if (!RGripLastFrame)//first frame you gripped joystick
                        {
                            RotDif = Quaternion.identity;
                            JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;//rotation of the controller relative to the plane when it was pressed
                        }
                        RGripLastFrame = true;
                        //difference between the vehicle and the hand's rotation, and then the difference between that and the JoystickZeroPoint, finally rotated by the vehicles rotation to turn it back to vehicle space
                        Quaternion JoystickDifference = (Quaternion.Inverse(ControlsRoot.rotation) * localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation) * Quaternion.Inverse(JoystickZeroPoint) * ControlsRoot.rotation;
                        //create normalized vectors facing towards the 'forward' and 'up' directions of the joystick
                        Vector3 JoystickPosYaw = (JoystickDifference * Vector3.forward);
                        //use acos to convert the relevant elements of the array into radians, re-center around zero, then normalize between -1 and 1 and multiply for desired deflection
                        //the clamp is there because rotating a vector3 can cause it to go a miniscule amount beyond length 1, resulting in NaN (crashes vrc)
                        VRPitchYawInput.x = ((Mathf.Acos(Mathf.Clamp(JoystickPosYaw.y, -1, 1)) - 1.5707963268f) * Mathf.Rad2Deg) / MaxJoyAngles.x;
                        VRPitchYawInput.y = -((Mathf.Acos(Mathf.Clamp(JoystickPosYaw.x, -1, 1)) - 1.5707963268f) * Mathf.Rad2Deg) / MaxJoyAngles.y;
                    }
                    else
                    {
                        VRPitchYawInput = Vector3.zero;
                        RGripLastFrame = false;
                    }
                    ControlsRotLastFrame = ControlsRoot.rotation;
                }
                int InX = (Wf + Sf);
                int InY = (Af + Df);
                if (InX > 0 && InputXKeyb < 0 || InX < 0 && InputXKeyb > 0) InputXKeyb = 0;
                if (InY > 0 && InputYKeyb < 0 || InY < 0 && InputYKeyb > 0) InputYKeyb = 0;
                InputXKeyb = Mathf.Lerp(InputXKeyb, InX, Mathf.Abs(InX) > 0 ? TurningResponseDesktop * deltaTime : 1);
                InputYKeyb = Mathf.Lerp(InputYKeyb, InY, Mathf.Abs(InY) > 0 ? TurningResponseDesktop * deltaTime : 1);

                float InputX = Mathf.Clamp((VRPitchYawInput.x + InputXKeyb), -1, 1);
                float InputY = Mathf.Clamp((VRPitchYawInput.y + InputYKeyb), -1, 1);

                InputX *= TurnSpeedMultiX * deltaTime;
                InputY *= TurnSpeedMultiY * deltaTime;

                float RotationDifferenceY = 0;
                float RotationDifferenceX = 0;
                if (Stabilize)
                {
                    RotationDifferenceY = Vector3.SignedAngle(VehicleTransform.forward, Vector3.ProjectOnPlane(LastForward_HOR, VehicleTransform.up), VehicleTransform.up);
                    LastForward_HOR = VehicleTransform.forward;

                    RotationDifferenceX = Vector3.SignedAngle(TurretRotatorHor.forward, Vector3.ProjectOnPlane(LastForward_VERT, TurretRotatorHor.right), TurretRotatorHor.right);
                    LastForward_VERT = TurretRotatorHor.forward;
                }
                float friction = TurnFriction * deltaTime;
                RotationSpeedX += -(RotationSpeedX * friction) + (InputX);
                RotationSpeedY += -(RotationSpeedY * friction) + (InputY);

                //rotate turret
                Vector3 rothor = TurretRotatorHor.localRotation.eulerAngles;
                Vector3 rotvert = TurretRotatorVert.localRotation.eulerAngles;

                float NewX = rotvert.x;
                NewX += (RotationSpeedX * deltaTime) + RotationDifferenceX;
                if (NewX > 180) { NewX -= 360; }
                if (NewX > DownAngleMax || NewX < -UpAngleMax) RotationSpeedX = 0;
                NewX = Mathf.Clamp(NewX, -UpAngleMax, DownAngleMax);//limit angles

                float NewY = rothor.y;
                NewY += (RotationSpeedY * deltaTime) + RotationDifferenceY;
                if (NewY > 180) { NewY -= 360; }
                if (NewY > SideAngleMax || NewY < -SideAngleMax) RotationSpeedY = 0;
                NewY = Mathf.Clamp(NewY, -SideAngleMax, SideAngleMax);//limit angles

                TurretRotatorHor.localRotation = Quaternion.Euler(new Vector3(0, NewY, 0));
                TurretRotatorVert.localRotation = Quaternion.Euler(new Vector3(NewX, 0, 0));


                if (time > nextUpdateTime)
                {
                    O_UpdateTime = time;
                    if (Stabilize)
                    { O_GunRotation = new Vector2(TurretRotatorVert.localEulerAngles.x, TurretRotatorHor.eulerAngles.y); }
                    else
                    { O_GunRotation = new Vector2(TurretRotatorVert.localEulerAngles.x, TurretRotatorHor.localEulerAngles.y); }
                    RequestSerialization();
                    nextUpdateTime = time + updateInterval;
#if UNITY_EDITOR
                    if (NetTestMode) { OnDeserialization(); }
#endif
                }
                if (RotatingSound)
                {
                    float turnvol = new Vector2(RotatingSound_HorizontalOnly ? 0 : RotationSpeedX, RotationSpeedY).magnitude * RotatingSoundMulti;
                    RotatingSound.volume = Mathf.Min(turnvol, RotateSoundVol);
                    RotatingSound.pitch = turnvol;
                }
            }
            else
            {
                Extrapolation();
            }
#if UNITY_EDITOR
            if (NetTestMode)
            { Extrapolation(); }
#endif
        }
#if UNITY_EDITOR
        public bool NetTestMode;
#endif
        Vector2 Extrapolation_Raw;
        private void Extrapolation()
        {
            float deltaTime = Time.deltaTime;
            double time;
            if (deltaTime > .099f)
            {
                time = Networking.GetServerTimeInSeconds();
                deltaTime = (float)(time - lastframetime_extrap);
                ResetSyncTimes();
            }
            else { time = StartupServerTime + (double)(Time.time - StartupLocalTime); }
            lastframetime_extrap = Networking.GetServerTimeInSeconds();

            Vector2 turretRot = new Vector2(VERTSYNC.localEulerAngles.x, HORSYNC.eulerAngles.y);
            if (turretRot.y > 180) { turretRot.y -= 360; }

            //prevent wrong interpolation direction when crossing 0-360 / 180--180
            if (!ClampHor)
            {
                if (Mathf.Abs(Extrapolation_Raw.y - turretRot.y) > 180)
                {
                    if (Extrapolation_Raw.y > turretRot.y)
                        turretRot.y += 360;
                    else
                        turretRot.y -= 360;
                }
            }
            if (Mathf.Abs(Extrapolation_Raw.x - turretRot.x) > 180)
            {
                if (Extrapolation_Raw.x > turretRot.x)
                    turretRot.x += 360;
                else
                    turretRot.x -= 360;
            }
            float TimeSinceUpdate = (float)(time - L_UpdateTime);
            float updateTimeNormalized = TimeSinceUpdate / updateDelta;

            Vector2 VelEstimate = GunRotationSpeed;
            Vector2 Correction = Extrapolation_Raw - turretRot;

            Correction *= CorrectionTime;

            ExtrapDirection_Smooth = Vector2.Lerp(ExtrapDirection_Smooth, VelEstimate + Correction, SpeedLerpTime * deltaTime);


            float newroty;
            if (Stabilize) newroty = HORSYNC.rotation.eulerAngles.y + ExtrapDirection_Smooth.y * deltaTime;
            else newroty = HORSYNC.localRotation.eulerAngles.y + ExtrapDirection_Smooth.y * deltaTime;
            if (newroty > 180) newroty -= 360;

            Vector2 rawprediction = GunRotationSpeed * TimeSinceUpdate;
            if (ClampHor)
            {
                // prevent smooth extrapolation from overshooting
                if (ExtrapDirection_Smooth.y > 0)//moving right
                {
                    if (newroty > SideAngleMax)
                    { newroty = SideAngleMax; }
                }
                else//moving left
                {
                    if (-newroty > SideAngleMax)
                    { newroty = -SideAngleMax; }
                }
                // prevent raw extrapolation from overshooting
                float maxturn;
                if (L_GunRotation.y < 180)//looking right
                {
                    if (rawprediction.y > 0)//moving right
                    {
                        maxturn = SideAngleMax - L_GunRotation.y;
                        if (rawprediction.y > maxturn)
                        { rawprediction.y = maxturn; }
                    }
                    else//moving left
                    {
                        maxturn = L_GunRotation.y + SideAngleMax;
                        if (-rawprediction.y > maxturn)
                        { rawprediction.y = -maxturn; }
                    }
                }
                else//looking left
                {
                    if (rawprediction.y > 0)//moving right
                    {
                        maxturn = 360 - L_GunRotation.y + SideAngleMax;
                        if (rawprediction.y > maxturn)
                        { rawprediction.y = maxturn; }
                    }
                    else//moving left
                    {
                        maxturn = SideAngleMax - (360 - L_GunRotation.y);
                        if (-rawprediction.y > maxturn)
                        { rawprediction.y = -maxturn; }
                    }
                }
            }
            Extrapolation_Raw = O_GunRotation + rawprediction;
            if (Extrapolation_Raw.y > 180) { Extrapolation_Raw.y -= 360; }
            if (Extrapolation_Raw.x > 180) { Extrapolation_Raw.x -= 360; }
            Extrapolation_Raw.x = Mathf.Clamp(Extrapolation_Raw.x, -UpAngleMax, DownAngleMax);

            if (Stabilize)
            {
                HORSYNC.rotation = Quaternion.Euler(new Vector3(0, newroty, 0));
                HORSYNC.localRotation = Quaternion.Euler(new Vector3(0, HORSYNC.localEulerAngles.y, 0));
#if UNITY_EDITOR
                if (HORSYNC_RAW) HORSYNC_RAW.rotation = Quaternion.Euler(new Vector3(0, Extrapolation_Raw.y, 0));
                if (HORSYNC_RAW) HORSYNC_RAW.localRotation = Quaternion.Euler(new Vector3(0, HORSYNC_RAW.localEulerAngles.y, 0));
#endif
            }
            else
            {
                HORSYNC.localRotation = Quaternion.Euler(new Vector3(0, newroty, 0));
#if UNITY_EDITOR
                if (HORSYNC_RAW) HORSYNC_RAW.localRotation = Quaternion.Euler(new Vector3(0, Extrapolation_Raw.y, 0));
#endif
            }

            float newrotx = VERTSYNC.localRotation.eulerAngles.x + ExtrapDirection_Smooth.x * deltaTime;
            if (newrotx > 180) newrotx -= 360;
            newrotx = Mathf.Clamp(newrotx, -UpAngleMax, DownAngleMax);
            VERTSYNC.localRotation = Quaternion.Euler(new Vector3(newrotx, 0, 0));
#if UNITY_EDITOR
            if (VERTSYNC_RAW) VERTSYNC_RAW.localRotation = Quaternion.Euler(new Vector3(Extrapolation_Raw.x, 0, 0));
#endif

            if (RotatingSound)
            {
                SND_RotLerper = Vector2.Lerp(SND_RotLerper, GunRotationSpeed, 1 - Mathf.Pow(0.5f, 5 * deltaTime));
                if (RotatingSound_HorizontalOnly) { SND_RotLerper.x = 0; }
                float turnvol = SND_RotLerper.magnitude * RotatingSoundMulti;
                RotatingSound.volume = Mathf.Min(turnvol, RotateSoundVol);
                RotatingSound.pitch = turnvol;
            }
        }
        public override void OnDeserialization()
        {
            float uDelta = (float)(O_UpdateTime - O_LastUpdateTime);
            if (uDelta < 0.0001f)
            {
                O_LastUpdateTime = O_UpdateTime;
                return;
            }
            updateDelta = uDelta;
            float speednormalizer = 1 / updateDelta;
            L_UpdateTime = O_UpdateTime;

            L_LastGunRotation = L_GunRotation;
            L_GunRotation = O_GunRotation;
            if (L_GunRotation.x > 180) { L_GunRotation.x -= 360; }

            Vector2 LGRTemp = L_LastGunRotation;
            if (LGRTemp.y > 180) { LGRTemp.y -= 360; }

            //check if going from rotation 0->360 and fix values for interpolation
            if (Mathf.Abs(L_GunRotation.y - L_LastGunRotation.y) > 180)
            {
                if (L_GunRotation.y > L_LastGunRotation.y)
                    L_LastGunRotation.y += 360;
                else
                    L_LastGunRotation.y -= 360;
            }
            Vector2 LGRSTemp = GunRotationSpeed;
            GunRotationSpeed = L_GunRotation - L_LastGunRotation;
            LastGunRotationSpeed = LGRSTemp;

            if (ClampHor)
            {
                //prevent interpolating in the wrong direction if max angle set
                if (Mathf.Abs(LGRTemp.y + GunRotationSpeed.y) > SideAngleMax)
                    GunRotationSpeed.y *= -1;
            }
            GunRotationSpeed *= speednormalizer;
            GunRotationAcceleration = (GunRotationSpeed - LastGunRotationSpeed) * speednormalizer;
            O_LastUpdateTime = O_UpdateTime;
        }
        private void InitSyncValues()
        {
            ResetSyncTimes();
            double time = StartupServerTime + (double)(Time.time - StartupLocalTime);
            nextUpdateTime = time + Random.Range(0f, updateInterval);
            O_LastUpdateTime = L_UpdateTime = lastframetime_extrap = time;
            O_LastUpdateTime -= updateInterval;

            if (Stabilize)
            { O_GunRotation = new Vector2(TurretRotatorVert.localEulerAngles.x, TurretRotatorHor.eulerAngles.y); }
            else
            { O_GunRotation = new Vector2(TurretRotatorVert.localEulerAngles.x, TurretRotatorHor.localEulerAngles.y); }
        }
        public void ResetSyncTimes()
        {
            StartupServerTime = Networking.GetServerTimeInSeconds();
            StartupLocalTime = Time.time;
        }
    }
}