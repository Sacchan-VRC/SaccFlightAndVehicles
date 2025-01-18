using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [DefaultExecutionOrder(100)]//after syncscript
    public class EXT_Turret : UdonSharpBehaviour
    {
        public Transform TurretRotatorHor;
        public Transform TurretRotatorVert;
        [Tooltip("Transform to base your controls on, should be facing the same direction as the seat. If left empty it will be set to the Horizontal Rotator.")]
        public Transform ControlsRoot;
        [Tooltip("Optional transform to define the forward direction of the turret")]
        public Transform TurretForwardEmpty;
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
        public float networkSmoothing = 6;
        [FieldChangeCallback(nameof(Stabilize_))] public bool Stabilize;
        [Tooltip("Stabilize the turrets movement? Easier to aim in moving vehicles")]
        public bool Stabilize_
        {
            set
            {
                LastForward_HOR = TurretForwardEmpty.forward;
                LastForward_VERT = TurretRotatorHor.forward;
                Stabilize = value;
            }
            get => Stabilize;
        }
        [Tooltip("Audio source that plays when rotating")]
        public AudioSource RotatingSound;
        [SerializeField] float RotatingSound_maxpitch = 2f;
        [SerializeField] bool PlayRotatingSoundForOthers = false;
        [SerializeField] bool RotatingSound_HorizontalOnly;
        [SerializeField] bool UseVirtualJoystick = true;
        [SerializeField] bool VJoy_RollAsYaw = false;
        [SerializeField] bool UseControlStickL;
        [SerializeField] bool UseControlStickR;
        [SerializeField] float ControlStickSensitivityL = 1f;
        [SerializeField] float ControlStickSensitivityR = 1f;
        [SerializeField] KeyCode KeyboardSpeedModifier = KeyCode.LeftShift;
        [SerializeField] float K_ModifierSpeed = 0.2f;
        public float RotatingSoundMulti = .02f;
        [Header("Networking:")]
        [Tooltip("How much vehicle accelerates extra towards its 'raw' position when not owner in order to correct positional errors")]
        public float CorrectionTime = 2f;
        [Tooltip("How quickly non-owned vehicle's velocity vector lerps towards its new value")]
        public float SpeedLerpTime = 25f;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [System.NonSerializedAttribute] public SAV_PassengerFunctionsController PassengerFunctionsControl;
        [Header("Syncmode can be set to None if using DelegateFireCallback")]
        [System.NonSerialized] public UdonSharpBehaviour DelegateFireCallback;
        private float InputXKeyb;
        private float InputYKeyb;
        private float RotationSpeedX = 0f;
        private float RotationSpeedY = 0f;
        private bool InEditor = true;
        private bool RGripLastFrame;
        private bool InVR;
        private VRCPlayerApi localPlayer;
        private float L_UpdateTime;
        private float L_LastUpdateTime;
        private float nextUpdateTime = float.MaxValue;
        private Quaternion GunRotationSpeed;
        private float SmoothingTimeDivider;
        private bool ClampHor = false;
        private bool Manning;
        private float RotateSoundVol;
        private Rigidbody VehicleRigid;
        Quaternion ControlsRotLastFrame;
        Quaternion JoystickZeroPoint;
        [System.NonSerializedAttribute] public bool IsOwner;//required by the bomb script, not actually related to being the owner of the object
        private Vector3 LastForward_HOR;
        private Vector3 LastForward_VERT;
        [UdonSynced] private short O_RotationX;
        [UdonSynced] private short O_RotationY;
        [UdonSynced] private short O_RotationZ;
        [UdonSynced] private bool TeleportAndFire;
        private Quaternion L_GunRotation;
        private Quaternion L_LastGunRotation;
        private float SND_RotLerper;
        float updateDelta = 0.25f;
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
            if (!TurretForwardEmpty) { TurretForwardEmpty = EntityControl.transform; }
            localPlayer = Networking.LocalPlayer;
            InVR = EntityControl.InVR;
            InEditor = localPlayer == null;
            VehicleRigid = EntityControl.GetComponent<Rigidbody>();
            if (!ControlsRoot) { ControlsRoot = TurretRotatorHor; }

            SmoothingTimeDivider = 1f / updateInterval;
            if (SideAngleMax < 180f) { ClampHor = true; }

            if (RotatingSound) { RotateSoundVol = RotatingSound.volume; }
            Stabilize_ = Stabilize;
            //enable for 5 seconds to sync turret rotation to late joiners
            if (EntityControl.IsOwner) { OwnerSend(); }
            else
            {
                gameObject.SetActive(true);
                SendCustomEventDelayedSeconds(nameof(InitalSyncDisable), 5f);
            }
        }
        public void InitalSyncDisable()
        {
            if (!Manned)
            { gameObject.SetActive(false); }
        }
        public void SFEXT_O_PilotEnter()
        {
            IsOwner = true;
            Manning = true;
            InVR = EntityControl.InVR;
            LastForward_HOR = TurretForwardEmpty.forward;
            LastForward_VERT = TurretRotatorHor.forward;
            nextUpdateTime = Time.time - .01f;
        }
        bool Manned;
        public void SFEXT_G_PilotEnter()
        {
            Manned = true;
            justEnabled = true;
            GunRotationSpeed = Quaternion.identity;
            SND_RotLerper = 0;
            gameObject.SetActive(true);
            if (RotatingSound && (PlayRotatingSoundForOthers || Manning))
            { RotatingSound.Play(); }
            RotExtrapolation_Raw = Extrapolation_Smooth = L_LastGunRotation = L_GunRotation = TurretRotatorVert.rotation;
            L_LastVehicleRotation = TurretForwardEmpty.rotation;
        }
        public void SFEXT_O_PilotExit()
        {
            IsOwner = false;
            SendCustomEventDelayedFrames(nameof(ManningFalse), 1);//if this isnt delayed update runs for one frame with Manning false before it's disabled
        }
        public void SFEXT_G_PilotExit()
        {
            Manned = false;
            gameObject.SetActive(false);
            if (RotatingSound && RotatingSound.isPlaying) { RotatingSound.Stop(); }
        }
        public void ManningFalse()
        {
            RotationSpeedX = 0;
            RotationSpeedY = 0;
            Manning = false;
        }
        public void SFEXT_G_RespawnButton()
        {
            RotExtrapolation_Raw = Extrapolation_Smooth = L_LastGunRotation = L_GunRotation = TurretRotatorVert.localRotation = TurretRotatorHor.localRotation = Quaternion.identity;
        }
        public void SFEXT_G_ReAppear()
        {
            HORSYNC.localRotation = Quaternion.identity;
            VERTSYNC.localRotation = Quaternion.identity;
        }
        private void Update()
        {
            if (Manning)
            {
                //ROTATION
                float time = Time.time;
                float deltaTime = Time.deltaTime;
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
                if (InVR && UseVirtualJoystick)
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
                        Vector3 JoystickPos = (JoystickDifference * Vector3.up);
                        //use acos to convert the relevant elements of the array into radians, re-center around zero, then normalize between -1 and 1 and multiply for desired deflection
                        //the clamp is there because rotating a vector3 can cause it to go a miniscule amount beyond length 1, resulting in NaN (crashes vrc)
                        VRPitchYawInput.x = ((Mathf.Acos(Mathf.Clamp(JoystickPosYaw.y, -1, 1)) - 1.5707963268f) * Mathf.Rad2Deg) / MaxJoyAngles.x;
                        if (VJoy_RollAsYaw)
                            VRPitchYawInput.y = -((Mathf.Acos(Mathf.Clamp(JoystickPos.x, -1, 1)) - 1.5707963268f) * Mathf.Rad2Deg) / MaxJoyAngles.y;
                        else
                            VRPitchYawInput.y = -((Mathf.Acos(Mathf.Clamp(JoystickPosYaw.x, -1, 1)) - 1.5707963268f) * Mathf.Rad2Deg) / MaxJoyAngles.y;
                    }
                    else
                    {
                        VRPitchYawInput = Vector3.zero;
                        RGripLastFrame = false;
                    }
                    ControlsRotLastFrame = ControlsRoot.rotation;
                }
                if (UseControlStickL)
                {
                    VRPitchYawInput.x -= Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical") * ControlStickSensitivityL;
                    VRPitchYawInput.y += Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal") * ControlStickSensitivityL;
                }
                if (UseControlStickR)
                {
                    VRPitchYawInput.x -= Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical") * ControlStickSensitivityR;
                    VRPitchYawInput.y += Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal") * ControlStickSensitivityR;
                }

                float InX = (Wf + Sf);
                float InY = (Af + Df);
                if (InX > 0 && InputXKeyb < 0 || InX < 0 && InputXKeyb > 0) InputXKeyb = 0;
                if (InY > 0 && InputYKeyb < 0 || InY < 0 && InputYKeyb > 0) InputYKeyb = 0;
                if (Input.GetKey(KeyboardSpeedModifier))
                {
                    InX *= K_ModifierSpeed;
                    InY *= K_ModifierSpeed;
                }
                InputXKeyb = Mathf.Lerp(InputXKeyb, InX, Mathf.Abs(InX) > 0 ? 1 - Mathf.Pow(0.5f, TurningResponseDesktop * deltaTime) : 1);
                InputYKeyb = Mathf.Lerp(InputYKeyb, InY, Mathf.Abs(InY) > 0 ? 1 - Mathf.Pow(0.5f, TurningResponseDesktop * deltaTime) : 1);


                float InputX = Mathf.Clamp((VRPitchYawInput.x + InputXKeyb), -1, 1);
                float InputY = Mathf.Clamp((VRPitchYawInput.y + InputYKeyb), -1, 1);

                InputX *= TurnSpeedMultiX * deltaTime;
                InputY *= TurnSpeedMultiY * deltaTime;

                float RotationDifferenceY = 0;
                float RotationDifferenceX = 0;
                if (Stabilize)
                {
                    RotationDifferenceY = Vector3.SignedAngle(TurretForwardEmpty.forward, Vector3.ProjectOnPlane(LastForward_HOR, TurretForwardEmpty.up), TurretForwardEmpty.up);
                    LastForward_HOR = TurretForwardEmpty.forward;

                    RotationDifferenceX = Vector3.SignedAngle(TurretRotatorHor.forward, Vector3.ProjectOnPlane(LastForward_VERT, TurretRotatorHor.right), TurretRotatorHor.right);
                    LastForward_VERT = TurretRotatorHor.forward;
                }
                float friction = TurnFriction * deltaTime;
                RotationSpeedX += -(RotationSpeedX * friction) + (InputX);
                RotationSpeedY += -(RotationSpeedY * friction) + (InputY);

                //rotate turret
                Vector3 rothor = TurretRotatorHor.localEulerAngles;
                Vector3 rotvert = TurretRotatorVert.localEulerAngles;

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
                    TeleportAndFire = false;
                    OwnerSend();
                }
                if (RotatingSound)
                {
                    float turnvol = new Vector2(RotatingSound_HorizontalOnly ? 0 : RotationSpeedX, RotationSpeedY).magnitude * RotatingSoundMulti;
                    RotatingSound.volume = Mathf.Min(turnvol, RotateSoundVol);
                    RotatingSound.pitch = Mathf.Min(turnvol, RotatingSound_maxpitch);
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
        public void OwnerSend()
        {
            Quaternion sendrot = TurretRotatorVert.rotation;
            if (sendrot.w < 0)
            { sendrot = sendrot = sendrot * Quaternion.Euler(0, 360, 0); } // ensure w componant is positive
            float smv = short.MaxValue;
            O_RotationX = (short)(sendrot.x * smv);
            O_RotationY = (short)(sendrot.y * smv);
            O_RotationZ = (short)(sendrot.z * smv);
            RequestSerialization();
            nextUpdateTime = Time.time + updateInterval;
#if UNITY_EDITOR
            if (NetTestMode) { OnDeserialization(); }
#endif
        }
        Quaternion RotExtrapolation_Raw;
        Quaternion Extrapolation_Smooth;
        private void Extrapolation()
        {
            float deltaTime = Time.deltaTime;

            Quaternion FrameRotExtrap = Quaternion.Slerp(Quaternion.identity, GunRotationSpeed, deltaTime);
            RotExtrapolation_Raw = FrameRotExtrap * RotExtrapolation_Raw;
            Extrapolation_Smooth = FrameRotExtrap * Extrapolation_Smooth;
            Extrapolation_Smooth = Quaternion.Slerp(Extrapolation_Smooth, RotExtrapolation_Raw, 1 - Mathf.Pow(0.5f, networkSmoothing * deltaTime));

            Vector3 lookDirHor = Vector3.ProjectOnPlane(Extrapolation_Smooth * Vector3.forward, HORSYNC.up);
            Vector3 lookDirVert = Extrapolation_Smooth * Vector3.forward;
            float turretAngleHor = Vector3.SignedAngle(TurretForwardEmpty.forward, lookDirHor, HORSYNC.up);
            //prevent going past angle limits
            if (turretAngleHor > SideAngleMax)
            {
                Quaternion unrotate = Quaternion.AngleAxis(SideAngleMax - turretAngleHor, HORSYNC.up);
                lookDirHor = unrotate * lookDirHor;
                lookDirVert = unrotate * lookDirVert;
            }
            else if (turretAngleHor < -SideAngleMax)
            {
                Quaternion unrotate = Quaternion.AngleAxis(-SideAngleMax - turretAngleHor, HORSYNC.up);
                lookDirHor = unrotate * lookDirHor;
                lookDirVert = unrotate * lookDirVert;
            }
            float turretAngleVert = Vector3.SignedAngle(HORSYNC.forward, lookDirVert, HORSYNC.right);
            if (turretAngleVert < -UpAngleMax)
            {
                Quaternion unrotate = Quaternion.AngleAxis(-UpAngleMax - turretAngleVert, HORSYNC.right);
                lookDirVert = unrotate * lookDirVert;
            }
            else if (turretAngleVert > DownAngleMax)
            {
                Quaternion unrotate = Quaternion.AngleAxis(DownAngleMax - turretAngleVert, HORSYNC.right);
                lookDirVert = unrotate * lookDirVert;
            }

            HORSYNC.LookAt(HORSYNC.position + lookDirHor, HORSYNC.up);
            VERTSYNC.LookAt(VERTSYNC.position + lookDirVert, HORSYNC.up);
#if UNITY_EDITOR
            if (HORSYNC_RAW) HORSYNC_RAW.LookAt(HORSYNC_RAW.position + Vector3.ProjectOnPlane(RotExtrapolation_Raw * Vector3.forward, HORSYNC_RAW.up), HORSYNC_RAW.up);
            if (VERTSYNC_RAW) VERTSYNC_RAW.LookAt(VERTSYNC_RAW.position + RotExtrapolation_Raw * Vector3.forward, HORSYNC_RAW.up);
#endif
            if (RotatingSound && PlayRotatingSoundForOthers)
            {
                SND_RotLerper = Mathf.Lerp(SND_RotLerper, GunRotationSpeed_angle, 1 - Mathf.Pow(0.5f, 5 * deltaTime));
                float turnvol = SND_RotLerper * RotatingSoundMulti;
                RotatingSound.volume = Mathf.Min(turnvol, RotateSoundVol);
                RotatingSound.pitch = Mathf.Min(turnvol, RotatingSound_maxpitch);
            }
        }
        bool justEnabled;
        float GunRotationSpeed_angle;
        Quaternion L_LastVehicleRotation;
        public override void OnDeserialization()
        {
            float uDelta = Time.time - L_LastUpdateTime;
            if (uDelta < 0.0001f || justEnabled)
            {
                L_LastUpdateTime = Time.time;
                justEnabled = false;
                return;
            }
            updateDelta = uDelta;
            float speednormalizer = 1 / uDelta;
            L_UpdateTime = Time.time;

            L_LastGunRotation = L_GunRotation;
            float smv = short.MaxValue;
            Vector3 quatParts = new Vector3(O_RotationX / smv, O_RotationY / smv, O_RotationZ / smv); //undo short compression
            float quatW = Mathf.Sqrt(1 - (quatParts.x * quatParts.x + quatParts.y * quatParts.y + quatParts.z * quatParts.z));// re calculate w
            L_GunRotation = new Quaternion(quatParts.x, quatParts.y, quatParts.z, quatW);
            RotExtrapolation_Raw = L_GunRotation;

            GunRotationSpeed = L_GunRotation * Quaternion.Inverse(L_LastGunRotation);
            GunRotationSpeed = Quaternion.LerpUnclamped(Quaternion.identity, GunRotationSpeed, speednormalizer);
            L_LastUpdateTime = L_UpdateTime;

            Quaternion VehicleRotationDif = TurretForwardEmpty.rotation * Quaternion.Inverse(L_LastVehicleRotation);
            L_LastVehicleRotation = TurretForwardEmpty.rotation;
            Quaternion compareRotation = L_LastGunRotation;
            float GunRotationSpeed_angley;
            float GunRotationSpeed_anglex = 0;
            compareRotation = VehicleRotationDif * compareRotation;
            // subtract dif in TurretForwardEmpty's rot from this caculation
            if (RotatingSound_HorizontalOnly)
                GunRotationSpeed_angley = Vector3.Angle(Vector3.ProjectOnPlane(compareRotation * Vector3.forward, HORSYNC.up), Vector3.ProjectOnPlane(L_GunRotation * Vector3.forward, HORSYNC.up));
            else
            {
                GunRotationSpeed_angley = Vector3.Angle(Vector3.ProjectOnPlane(compareRotation * Vector3.forward, HORSYNC.up), Vector3.ProjectOnPlane(L_GunRotation * Vector3.forward, HORSYNC.up));
                GunRotationSpeed_anglex = Vector3.Angle(Vector3.ProjectOnPlane(compareRotation * Vector3.forward, HORSYNC.right), Vector3.ProjectOnPlane(L_GunRotation * Vector3.forward, HORSYNC.right));
            }
            GunRotationSpeed_angle = GunRotationSpeed_angley + GunRotationSpeed_anglex;
            GunRotationSpeed_angle *= speednormalizer;

            if (TeleportAndFire)
            {
                Extrapolation_Smooth = L_GunRotation;
                Vector3 lookDirHor = Vector3.ProjectOnPlane(Extrapolation_Smooth * Vector3.forward, HORSYNC.up);
                Vector3 lookDirVert = Extrapolation_Smooth * Vector3.forward;
                HORSYNC.LookAt(HORSYNC.position + lookDirHor, HORSYNC.up);
                VERTSYNC.LookAt(VERTSYNC.position + lookDirVert, HORSYNC.up);
                DelegateFireCallback.SendCustomEvent("DelegatedFire");
            }
        }
        public void DelegateFire()
        {
            TeleportAndFire = true;
            OwnerSend();
#if UNITY_EDITOR
            if (!NetTestMode)
            {
#endif
                DelegateFireCallback.SendCustomEvent("DelegatedFire");
#if UNITY_EDITOR
            }
#endif
        }
    }
}