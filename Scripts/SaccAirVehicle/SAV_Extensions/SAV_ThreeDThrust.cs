
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.UdonNetworkCalling;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SAV_ThreeDThrust : UdonSharpBehaviour
    {
        [Header("This script can be used as an Extension OR DFUNC")]
        [Header("Not Compatable with Cruise or Limits DFUNC")]
        [Header("Will behave strangely if used with VTOL in DFUNC mode")]
        public SaccAirVehicle SAVControl;
        public Animator ThrustAnimator;
        public Transform ThrustArrow;
        [Tooltip("Multiply arrow scale, 1 = matches hand position")]
        [SerializeField] float ThrustArrowMaxSize = 1f;
        [SerializeField] bool showArrow_Desktop;
        [SerializeField] KeyCode ThrustForwardKey = KeyCode.I;
        [SerializeField] KeyCode ThrustBackKey = KeyCode.K;
        [SerializeField] KeyCode ThrustRightKey = KeyCode.L;
        [SerializeField] KeyCode ThrustLeftKey = KeyCode.J;
        [SerializeField] KeyCode ThrustUpKey = KeyCode.O;
        [SerializeField] KeyCode ThrustDownKey = KeyCode.U;
        public Vector2 ThreeDThrottleStrengthX = new Vector2(20f, 20f);
        public Vector2 ThreeDThrottleStrengthY = new Vector2(20f, 20f);
        public Vector2 ThreeDThrottleStrengthZ = new Vector2(20f, 20f);
        public bool DefaultEnabled = true;
        public bool DoForwardBackThrust = true;
        public bool DoSideThrust = true;
        public bool DoUPDownThrust = true;
        public bool AllowMainEngineAndThrust = false;
        [Tooltip("Set vehicle's throttle value to match the magnitude of the 3DThrust input, recommend setting vehicles ThrottleStrength to 0 if using this otherwise it'll always move forward, use for HUD visual like the S-GRVR.")]
        public bool ThrottleMatch3DThrust = false;
        [SerializeField] bool RequireEngine = true;
        [SerializeField] bool DisableSAVJoystick = false;
        [SerializeField] bool DisableSAVThrottle = false;
        [SerializeField] bool SyncThrust = true;
        [Space]
        [SerializeField] bool ControlsWind = false;
        [Tooltip("Will only sync wind if ControlsWind is true")]
        [SerializeField] bool SyncWind = true;
        [SerializeField] bool AllowVerticalWind = false;
        [SerializeField] float MaxWindMagnitude = 100f;
        [SerializeField][Range(0f, 1f)] float WindGustRatio = 0.2f;
        [Tooltip("Set wind Gustiness when using? To override any other WindControl in the world. -1 to disable")]
        [SerializeField] float SetGustiness = 0.03f;
        [Tooltip("Set wind Turbulance when using? To override any other WindControl in the world. -1 to disable")]
        [SerializeField] float SetTurbulance = .0001f;
        [Header("Only for DFUNC Mode")]
        public bool UseThrottleAsForward = true;
        public bool ThrottleAsForward_NoBackThrust = false;
        [Space]
        [SerializeField] float updateInterval = 0.3f;
        [SerializeField] float NetworkSmoothTime = 8;
        float LastUpdateTime;
        private Transform VehicleMainObj;
        private Transform ControlsRoot;
        private Rigidbody VehicleRB;
        private bool ThreeDThrottleLastFrame;
        private Vector3 HandPosThrottle;
        private Vector3 ThreeDVRThrottle;
        private Vector3 ThreeDKeybThrottle;
        private Vector3 ThreeDThrottle;
        [System.NonSerialized] public Vector3 ThreeDThrottleInput;
        private Vector3 ThrottleZeroPoint;
        private VRCPlayerApi localPlayer;
        private bool InVR;
        private bool SwitchHandsJoyThrottle;
        private bool Selected;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        private bool UseAsDFUNC;
        private bool Piloting;
        private float ThrottleSensitivity;
        private float GripSensitivity;
        private bool IsOwner;
        private bool Occupied;
        [System.NonSerialized] public bool OverridingThrottle = false;
        [System.NonSerialized] public bool OverridingThrottleControl = false;
        [System.NonSerialized] public bool OverridingJoystickControl = false;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(ThreeDThrustActive))] public bool _ThreeDThrustActive = false;
        public bool ThreeDThrustActive//this can be toggled by a dfunc to switch between normal saccairvehicle flight to thruster only flight
        {
            set
            {
                if (!AllowMainEngineAndThrust)
                {
                    if (value && !_ThreeDThrustActive)
                    {
                        if (!OverridingThrottle)
                        {
                            OverridingThrottle = true;
                            SAVControl.SetProgramVariable("ThrottleOverridden", (int)SAVControl.GetProgramVariable("ThrottleOverridden") + 1);
                        }
                    }
                    else if (!value && _ThreeDThrustActive)
                    {
                        if (OverridingThrottle)
                        {
                            OverridingThrottle = false;
                            SAVControl.SetProgramVariable("ThrottleOverridden", (int)SAVControl.GetProgramVariable("ThrottleOverridden") - 1);
                            SAVControl.SetProgramVariable("ThrottleOverride", 0f);
                        }
                    }
                }
                if (Piloting)
                { gameObject.SetActive(value); }
                _ThreeDThrustActive = value;
            }
            get => _ThreeDThrustActive;
        }
        public void SFEXT_L_EntityStart()
        {
            if (DialPosition > -1) { UseAsDFUNC = true; DefaultEnabled = false; }
            if (ControlsWind) { AllowMainEngineAndThrust = true; }
            localPlayer = Networking.LocalPlayer;
            VehicleMainObj = SAVControl.VehicleTransform;
            VehicleRB = VehicleMainObj.GetComponent<Rigidbody>();
            ControlsRoot = SAVControl.ControlsRoot;
            ThrottleSensitivity = SAVControl.ThrottleSensitivity;
            GripSensitivity = SAVControl.GripSensitivity;
            IsOwner = SAVControl.IsOwner;
            InVR = EntityControl.InVR;
            if (DefaultEnabled)
            {
                ThreeDThrustActive = true;
            }
            gameObject.SetActive(false);

            if (!UseThrottleAsForward && !UseAsDFUNC && !ControlsWind)
            {
                if (!OverridingThrottle && !AllowMainEngineAndThrust && DefaultEnabled)
                {
                    OverridingThrottle = true;
                    SAVControl.SetProgramVariable("ThrottleOverridden", (int)SAVControl.GetProgramVariable("ThrottleOverridden") + 1);
                }
            }
            if (DisableSAVThrottle)
            {
                if (!OverridingThrottleControl)
                {
                    OverridingThrottleControl = true;
                    SAVControl.SetProgramVariable("DisableThrottleControl", (int)SAVControl.GetProgramVariable("DisableThrottleControl") + 1);
                }
            }
            if (DisableSAVJoystick)
            {
                if (!OverridingJoystickControl)
                {
                    OverridingJoystickControl = true;
                    SAVControl.SetProgramVariable("DisableJoystickControl", (int)SAVControl.GetProgramVariable("DisableJoystickControl") + 1);
                }
            }
        }
        public void SFEXT_G_PilotExit()
        {
            Occupied = false;
            gameObject.SetActive(false);
            ThreeDThrottleInput = Vector3.zero;
            setAnimatorDefault();
        }
        public void SFEXT_G_PilotEnter()
        {
            Occupied = true;
            if (_ThreeDThrustActive || UseAsDFUNC)
            { gameObject.SetActive(true); }
        }
        public void SFEXT_O_PilotEnter()
        {
            Piloting = true;
            InVR = SAVControl.InVR;
            SwitchHandsJoyThrottle = SAVControl.SwitchHandsJoyThrottle;
            if (!InVR && showArrow_Desktop && ThrustArrow) { ThrustArrow.gameObject.SetActive(true); }
        }
        public void SFEXT_O_PilotExit()
        {
            Piloting = false;
            Selected = false;
            ThreeDThrottle = Vector3.zero;
            ThreeDVRThrottle = Vector3.zero;
            ThreeDKeybThrottle = Vector3.zero;
            if (_ThreeDThrustActive) { SAVControl.SetProgramVariable("ThrottleOverride", 0f); }
            if (ControlsWind && SyncWind)
            {
                lastSentVector = SAVControl.Wind;
                LastUpdateTime = Time.time;
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Others, nameof(SetWind), SAVControl.Wind);
            }
            if (ThrustArrow) { ThrustArrow.gameObject.SetActive(false); }
        }
        private void ThrottleStuff(float Input)
        {
            if (Input > GripSensitivity)
            {
                Vector3 HandPos;
                if (UseAsDFUNC)
                {
                    if (LeftDial)
                    { HandPos = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position; }
                    else
                    { HandPos = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position; }
                }
                else
                {
                    if (SAVControl.SwitchHandsJoyThrottle)
                    { HandPos = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position; }
                    else
                    { HandPos = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position; }
                }
                HandPosThrottle = ControlsRoot.transform.InverseTransformPoint(HandPos);
                if (!ThreeDThrottleLastFrame)
                {
                    ThrottleZeroPoint = HandPosThrottle;
                    if (ThrustArrow)
                    {
                        ThrustArrow.position = HandPos;
                        if (InVR && !ThrustArrow.gameObject.activeSelf)
                        { ThrustArrow.gameObject.SetActive(true); }
                    }
                }
                if (!ControlsWind)
                    ThrustArrow.LookAt(HandPos);
                Vector3 ThreeDThrottleDifference = (ThrottleZeroPoint - HandPosThrottle) * ThrottleSensitivity;
                ThreeDVRThrottle.x = Mathf.Clamp(-ThreeDThrottleDifference.x, -1, 1);
                ThreeDVRThrottle.y = -Mathf.Clamp(ThreeDThrottleDifference.y, -1, 1);
                ThreeDVRThrottle.z = Mathf.Clamp(-ThreeDThrottleDifference.z, -1, 1);

                ThreeDThrottleLastFrame = true;
            }
            else if (ThreeDThrottleLastFrame)
            {
                ThreeDVRThrottle = Vector3.zero;
                if (!UseAsDFUNC && UseThrottleAsForward && !ControlsWind) { SAVControl.PlayerThrottle = 0; }
                ThreeDThrottleLastFrame = false;
                if (ThrustArrow)
                {
                    if (InVR && ThrustArrow.gameObject.activeSelf)
                    { ThrustArrow.gameObject.SetActive(false); }
                }
            }
        }
        public void DFUNC_Selected()
        {
            Selected = true;
        }
        public void DFUNC_Deselected()
        {
            Selected = false;
            SAVControl.ThrottleOverride = 0;
            ThreeDThrottle = Vector3.zero;
            ThreeDVRThrottle = Vector3.zero;
            ThreeDKeybThrottle = Vector3.zero;
        }
        Vector3 lastSentVector;
        private void LateUpdate()
        {
            if (IsOwner && (EngineOn || !RequireEngine))
            {
                if (InVR)
                {
                    if (UseAsDFUNC)
                    {
                        if (Selected)
                        {
                            float ThreeDGrab;
                            if (LeftDial)
                            { ThreeDGrab = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                            else
                            { ThreeDGrab = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
                            ThrottleStuff(ThreeDGrab);
                        }
                    }
                    else
                    {
                        float ThreeDGrab;
                        if (SwitchHandsJoyThrottle)
                        { ThreeDGrab = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger"); }
                        else
                        { ThreeDGrab = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger"); }
                        ThrottleStuff(ThreeDGrab);
                    }
                }
                int Ii = Input.GetKey(ThrustForwardKey) ? 1 : 0;
                int Ki = Input.GetKey(ThrustBackKey) ? -1 : 0;
                int Ji = Input.GetKey(ThrustLeftKey) ? -1 : 0;
                int Li = Input.GetKey(ThrustRightKey) ? 1 : 0;
                int Ui = Input.GetKey(ThrustDownKey) ? -1 : 0;
                int Oi = Input.GetKey(ThrustUpKey) ? 1 : 0;
                ThreeDKeybThrottle.x = Ji + Li;
                ThreeDKeybThrottle.y = Ui + Oi;
                ThreeDKeybThrottle.z = Ii + Ki;

                if (ControlsWind)
                {
                    ThreeDThrottleInput = Vector3.zero;
                    ThreeDThrottleInput.x = Mathf.Clamp(ThreeDVRThrottle.x + ThreeDKeybThrottle.x, -1, 1);
                    ThreeDThrottleInput.y = Mathf.Clamp(ThreeDVRThrottle.y + ThreeDKeybThrottle.y, -1, 1);
                    ThreeDThrottleInput.z = Mathf.Clamp(ThreeDVRThrottle.z + ThreeDKeybThrottle.z, -1, 1);

                    Vector3 windInput = ThreeDThrottleInput;
                    windInput.x *= (windInput.x > 0) ? ThreeDThrottleStrengthX.x : ThreeDThrottleStrengthX.y;
                    windInput.y *= (windInput.y > 0) ? ThreeDThrottleStrengthY.x : ThreeDThrottleStrengthY.y;
                    windInput.z *= (windInput.z > 0) ? ThreeDThrottleStrengthZ.x : ThreeDThrottleStrengthZ.y;

                    windInput = ControlsRoot.rotation * windInput;

                    if (!AllowVerticalWind)
                    {
                        windInput.y = ThreeDThrottleInput.y = 0;
                    }
                    Vector3 newWind = SAVControl.Wind + windInput * (1 - WindGustRatio) * Time.deltaTime;
                    float windMag = newWind.magnitude;
                    if (windMag > MaxWindMagnitude)
                    {
                        newWind *= MaxWindMagnitude / windMag;
                        windMag = MaxWindMagnitude;
                    }
                    SetWind(newWind);
                    UpdateThrustArrow();
                    if (SyncWind)
                    {
                        if (lastSentVector != newWind)
                        {
                            if (Time.time - LastUpdateTime > updateInterval)
                            {
                                lastSentVector = newWind;
                                LastUpdateTime = Time.time;
                                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Others, nameof(SetWind), newWind);
                            }
                        }
                    }
                    return;
                }

                if (InWater) { ThreeDThrottle = ThreeDThrottleInput = Vector3.zero; }
                else
                {
                    ThreeDThrottleInput = Vector3.zero;
                    if (DoSideThrust)
                    { ThreeDThrottleInput.x = Mathf.Clamp(ThreeDVRThrottle.x + ThreeDKeybThrottle.x, -1, 1); }
                    if (DoUPDownThrust)
                    { ThreeDThrottleInput.y = Mathf.Clamp(ThreeDVRThrottle.y + ThreeDKeybThrottle.y, -1, 1); }
                    if (DoForwardBackThrust)
                    { ThreeDThrottleInput.z = Mathf.Clamp(ThreeDVRThrottle.z + ThreeDKeybThrottle.z, -1, 1); }

                    if (!UseAsDFUNC)
                    {
                        ThreeDThrottle = ControlsRoot.right * ThreeDThrottleInput.x *
                            (ThreeDThrottleInput.x > 0 ? ThreeDThrottleStrengthX.x : ThreeDThrottleStrengthX.y);

                        ThreeDThrottle += ControlsRoot.up * ThreeDThrottleInput.y *
                            (ThreeDThrottleInput.y > 0 ? ThreeDThrottleStrengthY.x : ThreeDThrottleStrengthY.y);

                        ThreeDThrottle += ControlsRoot.forward * ThreeDThrottleInput.z *
                            (ThreeDThrottleInput.z > 0 ? ThreeDThrottleStrengthZ.x : ThreeDThrottleStrengthZ.y);
                    }
                    else
                    {
                        ThreeDThrottle = ControlsRoot.right * ThreeDThrottleInput.x *
                            (ThreeDThrottleInput.x > 0 ? ThreeDThrottleStrengthX.x : ThreeDThrottleStrengthX.y);

                        if (SAVControl.VerticalThrottle)
                        {
                            if (UseThrottleAsForward)
                            {
                                if (ThreeDThrottleInput.y > 0)
                                {
                                    SAVControl.PlayerThrottle = Mathf.Max(ThreeDThrottleInput.y, 0);
                                }
                                else SAVControl.PlayerThrottle = 0;
                                if (!ThrottleAsForward_NoBackThrust && ThreeDThrottleInput.y < 0)
                                {
                                    ThreeDThrottle += ControlsRoot.forward * ThreeDThrottleInput.y *
                                        (ThreeDThrottle.z > 0 ? ThreeDThrottleStrengthZ.x : ThreeDThrottleStrengthZ.y);
                                    SAVControl.PlayerThrottle = 0;
                                }
                            }
                            else
                            {
                                ThreeDThrottle += ControlsRoot.forward * ThreeDThrottleInput.y *
                                    (ThreeDThrottleInput.y > 0 ? ThreeDThrottleStrengthY.x : ThreeDThrottleStrengthY.y);
                            }
                            ThreeDThrottle += ControlsRoot.forward * ThreeDThrottleInput.z *
                                (ThreeDThrottleInput.z > 0 ? ThreeDThrottleStrengthZ.x : ThreeDThrottleStrengthZ.y);
                        }
                        else
                        {
                            if (UseThrottleAsForward)
                            {
                                if (ThreeDThrottleInput.z > 0)
                                {
                                    SAVControl.PlayerThrottle = Mathf.Max(ThreeDThrottleInput.z, 0);
                                }
                                else SAVControl.PlayerThrottle = 0;
                                if (!ThrottleAsForward_NoBackThrust && ThreeDThrottleInput.z < 0)
                                {
                                    ThreeDThrottle += ControlsRoot.forward * ThreeDThrottleInput.z *
                                        (ThreeDThrottle.z > 0 ? ThreeDThrottleStrengthZ.x : ThreeDThrottleStrengthZ.y);
                                    SAVControl.PlayerThrottle = 0;
                                }
                            }
                            else
                            {
                                ThreeDThrottle += ControlsRoot.forward * ThreeDThrottleInput.z *
                                    (ThreeDThrottleInput.z > 0 ? ThreeDThrottleStrengthZ.x : ThreeDThrottleStrengthZ.y);
                            }
                            ThreeDThrottle += ControlsRoot.up * ThreeDThrottleInput.y *
                                (ThreeDThrottleInput.y > 0 ? ThreeDThrottleStrengthY.x : ThreeDThrottleStrengthY.y);
                        }
                    }
                }
                UpdateThrustArrow();
                if (!AllowMainEngineAndThrust && ThrottleMatch3DThrust)
                {
                    SAVControl.SetProgramVariable("ThrottleOverride", Mathf.Min(ThreeDThrottleInput.magnitude, 1));
                }
                if (SyncThrust)
                {
                    if (lastSentVector != ThreeDThrottleInput)
                    {
                        if (Time.time - LastUpdateTime > updateInterval)
                        {
                            lastSentVector = ThreeDThrottleInput;
                            LastUpdateTime = Time.time;
                            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Others, nameof(NetworkUpdate), ThreeDThrottleInput);
                        }
                    }
                }
            }
            if (Occupied)
            {
                UpdateAnimator();
            }
        }
        void UpdateAnimator()
        {
            if (ThrustAnimator)
            {
                ThrustAnimator.SetFloat("thrustx", ThreeDThrottleInput.x * .5f + .5f);
                ThrustAnimator.SetFloat("thrusty", ThreeDThrottleInput.y * .5f + .5f);
                ThrustAnimator.SetFloat("thrustz", ThreeDThrottleInput.z * .5f + .5f);
            }
        }
        [NetworkCallable]
        public void SetWind(Vector3 inputWind)
        {
            if (!IsOwner) { lastSentVector = inputWind; }
            SAVControl.Wind = inputWind;
            float windMag = inputWind.magnitude;
            if (WindGustRatio > 0)
                SAVControl.WindGustStrength = windMag * WindGustRatio;
            if (SetGustiness > 0)
                SAVControl.WindGustiness = SetGustiness;
            if (SetTurbulance > 0)
                SAVControl.WindTurbulanceScale = SetTurbulance;
        }
        [NetworkCallable]
        public void NetworkUpdate(Vector3 NewThrustValues)
        {
            if (!Occupied) { setAnimatorDefault(); return; }
            lastSentVector = NewThrustValues;
            ThreeDThrottleInput_LerpTarget = NewThrustValues;
            if (!SmoothNetworkUpdateRunning)
            {
                SmoothNetworkUpdateRunning = true;
                SmoothNetworkUpdate();
            }
        }
        Vector3 ThreeDThrottleInput_LerpTarget;
        bool SmoothNetworkUpdateRunning;
        public void SmoothNetworkUpdate()
        {
            if (!SmoothNetworkUpdateRunning) return;

            if (ThreeDThrottleInput_LerpTarget.sqrMagnitude == 0)
            {
                ThreeDThrottleInput = Vector3.MoveTowards(ThreeDThrottleInput, ThreeDThrottleInput_LerpTarget, Time.deltaTime * 1.5f);
            }
            else
            {
                ThreeDThrottleInput = Vector3.Lerp(ThreeDThrottleInput, ThreeDThrottleInput_LerpTarget, 1 - Mathf.Pow(0.5f, Time.deltaTime * 4));
            }
            SendCustomEventDelayedFrames(nameof(SmoothNetworkUpdate), 1);
            if (ThreeDThrottleInput.sqrMagnitude == 0)
            {
                SmoothNetworkUpdateRunning = false;

            }
            UpdateAnimator();
        }
        void UpdateThrustArrow()
        {
            if (!ThrustArrow) return;
            if (InVR)
            {
                if (ControlsWind)
                {
                    ThrustArrow.LookAt(ThrustArrow.position + SAVControl.Wind);
                    ThrustArrow.localScale = Vector3.one * (SAVControl.Wind.magnitude / MaxWindMagnitude / ThrottleSensitivity) * ThrustArrowMaxSize;
                }
                else
                {
                    ThrustArrow.localScale = Vector3.one * (ThreeDThrottleInput.magnitude / ThrottleSensitivity) * ThrustArrowMaxSize;
                }
            }
            else if (showArrow_Desktop)
            {
                Quaternion headRot = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
                Vector3 headPlusOffset = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position +
                headRot * Vector3.forward * 0.5f +
                headRot * Vector3.down * 0.2f
                ;
                if (ThrustArrow)
                {
                    ThrustArrow.position = headPlusOffset;
                    if (ControlsWind)
                    {
                        ThrustArrow.LookAt(ThrustArrow.position + SAVControl.Wind);
                        ThrustArrow.localScale = Vector3.one * (SAVControl.Wind.magnitude / MaxWindMagnitude / ThrottleSensitivity) * ThrustArrowMaxSize;
                    }
                    else
                    {
                        ThrustArrow.LookAt(ThrustArrow.position + ControlsRoot.rotation * ThreeDThrottleInput);
                        ThrustArrow.localScale = Vector3.one * (ThreeDThrottleInput.magnitude / ThrottleSensitivity) * ThrustArrowMaxSize;
                    }
                }
            }
        }
        public void SFEXT_G_Wrecked()
        {
            ThreeDThrottle = ThreeDThrottleInput = Vector3.zero;
            setAnimatorDefault();
        }
        public void SFEXT_G_Explode()
        {
            setAnimatorDefault();
            resetWind();
        }
        private void setAnimatorDefault()
        {
            SmoothNetworkUpdateRunning = false;
            ThreeDThrottleInput_LerpTarget = Vector3.zero;
            if (ThrustAnimator)
            {
                ThrustAnimator.SetFloat("thrustx", .5f);
                ThrustAnimator.SetFloat("thrusty", .5f);
                ThrustAnimator.SetFloat("thrustz", .5f);
            }
        }
        private bool InWater;
        public void SFEXT_G_EnterWater()
        { InWater = true; }
        public void SFEXT_G_ExitWater()
        { InWater = false; }
        private bool EngineOn;
        public void SFEXT_G_EngineOn()
        {
            EngineOn = true;
        }
        public void SFEXT_G_EngineOff()
        {
            EngineOn = false;
        }
        private void FixedUpdate()
        {
            if (IsOwner && !ControlsWind)
            {
                VehicleRB.AddForce(ThreeDThrottle, ForceMode.Acceleration);
            }
        }
        public void SFEXT_O_TakeOwnership()
        {
            IsOwner = true;
        }
        public void SFEXT_O_LoseOwnership()
        {
            IsOwner = false;
        }
        public void SFEXT_G_RespawnButton()
        {
            resetWind();
        }
        void resetWind()
        {
            if (!ControlsWind) return;
            SAVControl.Wind = Vector3.zero;
            if (WindGustRatio > 0)
                SAVControl.WindGustStrength = 0;
            if (SetGustiness > 0)
                SAVControl.WindGustiness = SetGustiness;
            if (SetTurbulance > 0)
                SAVControl.WindTurbulanceScale = SetTurbulance;
        }
    }
}