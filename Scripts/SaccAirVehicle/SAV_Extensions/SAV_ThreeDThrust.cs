
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class SAV_ThreeDThrust : UdonSharpBehaviour
    {
        [Header("This script can be used as an Extension OR DFUNC")]
        [Header("Not Compatable with Cruise or Limits DFUNC")]
        [Header("Will behave strangely if used with VTOL in DFUNC mode")]
        public SaccFlightAndVehicles.SaccAirVehicle SAVControl;
        public Animator ThrustAnimator;
        public Transform ThrustArrow;
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
        [Header("Only for DFUNC Mode")]
        public bool UseThrottleAsForward = true;
        public bool ThrottleAsForward_NoBackThrust = false;
        private Transform VehicleMainObj;
        private Transform ControlsRoot;
        private Rigidbody VehicleRB;
        private bool ThreeDThrottleLastFrame;
        private Vector3 HandPosThrottle;
        private Vector3 HandPos;
        private Vector3 ThreeDVRThrottle;
        private Vector3 ThreeDKeybThrottle;
        private Vector3 ThreeDThrottle;
        [System.NonSerialized, UdonSynced(UdonSyncMode.Linear)] public Vector3 ThreeDThrottleInput;
        private Vector3 ThrottleZeroPoint;
        private VRCPlayerApi localPlayer;
        private bool InVR;
        private bool SwitchHandsJoyThrottle;
        private bool Selected;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [System.NonSerializedAttribute] public SAV_PassengerFunctionsController PassengerFunctionsControl;
        private bool UseAsDFUNC;
        private bool Piloting;
        private float ThrottleSensitivity;
        private bool IsOwner;
        private bool Occupied;
        private bool OverridingThrottle = false;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(ThreeDThrustActive))] public bool _ThreeDThrustActive = false;
        public bool ThreeDThrustActive//this can be toggled by a dfunc to switch between normal saccairvehicle flight to thruster only flight
        {
            set
            {
                if (!AllowMainEngineAndThrust)
                {
                    if (value && !_ThreeDThrustActive)
                    {
                        SAVControl.SetProgramVariable("ThrottleOverridden", (int)SAVControl.GetProgramVariable("ThrottleOverridden") + 1);
                    }
                    else if (!value && _ThreeDThrustActive)
                    {
                        SAVControl.SetProgramVariable("ThrottleOverridden", (int)SAVControl.GetProgramVariable("ThrottleOverridden") - 1);
                        SAVControl.SetProgramVariable("ThrottleOverride", 0f);
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
            localPlayer = Networking.LocalPlayer;
            VehicleMainObj = SAVControl.VehicleTransform;
            VehicleRB = VehicleMainObj.GetComponent<Rigidbody>();
            ControlsRoot = SAVControl.ControlsRoot;
            ThrottleSensitivity = SAVControl.ThrottleSensitivity;
            IsOwner = SAVControl.IsOwner;
            InVR = EntityControl.InVR;
            if (DefaultEnabled)
            {
                ThreeDThrustActive = true;
            }
            gameObject.SetActive(false);

            if (!UseThrottleAsForward && !UseAsDFUNC)
            {
                if (!OverridingThrottle && !AllowMainEngineAndThrust)
                {
                    OverridingThrottle = true;
                    SAVControl.SetProgramVariable("ThrottleOverridden", (int)SAVControl.GetProgramVariable("ThrottleOverridden") + 1);
                }
            }
        }
        public void SFEXT_G_PilotExit()
        {
            Occupied = false;
            gameObject.SetActive(false);
            if (ThrustAnimator)
            {
                ThrustAnimator.SetFloat("thrustx", .5f);
                ThrustAnimator.SetFloat("thrusty", .5f);
                ThrustAnimator.SetFloat("thrustz", .5f);
            }
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
            if (ThrustArrow) { ThrustArrow.gameObject.SetActive(InVR); }
        }
        public void SFEXT_O_PilotExit()
        {
            Piloting = false;
            Selected = false;
            ThreeDThrottle = Vector3.zero;
            ThreeDVRThrottle = Vector3.zero;
            ThreeDKeybThrottle = Vector3.zero;
            if (ThrustArrow) { ThrustArrow.gameObject.SetActive(false); }
            if (_ThreeDThrustActive) { SAVControl.SetProgramVariable("ThrottleOverride", 0f); }
        }
        private void ThrottleStuff(float Input)
        {
            if (Input > 0.75)
            {
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
                    if (ThrustArrow) { ThrustArrow.position = HandPos; }
                }
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
                if (!UseAsDFUNC) { SAVControl.PlayerThrottle = 0; }
                ThreeDThrottleLastFrame = false;
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
        private void LateUpdate()
        {
            if (IsOwner && EngineOn)
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
                        if (ThreeDThrottleInput.x > 0)
                        {
                            ThreeDThrottle = ControlsRoot.right * ThreeDThrottleInput.x * ThreeDThrottleStrengthX.x;
                        }
                        else
                        {
                            ThreeDThrottle = ControlsRoot.right * ThreeDThrottleInput.x * ThreeDThrottleStrengthX.y;
                        }
                        if (ThreeDThrottleInput.y > 0)
                        {
                            ThreeDThrottle += ControlsRoot.up * ThreeDThrottleInput.y * ThreeDThrottleStrengthY.x;
                        }
                        else
                        {
                            ThreeDThrottle += ControlsRoot.up * ThreeDThrottleInput.y * ThreeDThrottleStrengthY.y;
                        }
                        if (ThreeDThrottleInput.z > 0)
                        {
                            ThreeDThrottle += ControlsRoot.forward * ThreeDThrottleInput.z * ThreeDThrottleStrengthZ.x;
                        }
                        else
                        {
                            ThreeDThrottle += ControlsRoot.forward * ThreeDThrottleInput.z * ThreeDThrottleStrengthZ.y;
                        }
                    }
                    else//regular throttle is used for forward if it's a dfunc
                    {
                        if (ThreeDThrottleInput.x > 0)
                        {
                            ThreeDThrottle = ControlsRoot.right * ThreeDThrottleInput.x * ThreeDThrottleStrengthX.x;
                        }
                        else
                        {
                            ThreeDThrottle = ControlsRoot.right * ThreeDThrottleInput.x * ThreeDThrottleStrengthX.y;
                        }
                        if (SAVControl.VerticalThrottle)
                        {
                            if (ThreeDThrottleInput.y != 0)
                            {
                                if (UseThrottleAsForward)
                                {
                                    if (ThreeDThrottleInput.y > 0)
                                    {
                                        SAVControl.PlayerThrottle = Mathf.Max(ThreeDThrottleInput.y, 0);
                                    }
                                    else if (!ThrottleAsForward_NoBackThrust)
                                    {
                                        if (ThreeDThrottle.z > 0)
                                        {
                                            ThreeDThrottle += ControlsRoot.forward * Mathf.Min(ThreeDThrottleInput.y, 0) * ThreeDThrottleStrengthZ.x;
                                        }
                                        else
                                        {
                                            ThreeDThrottle += ControlsRoot.forward * Mathf.Min(ThreeDThrottleInput.y, 0) * ThreeDThrottleStrengthZ.y;
                                        }
                                        SAVControl.PlayerThrottle = 0;
                                    }
                                }
                                else
                                {
                                    if (ThreeDThrottleInput.y > 0)
                                    {
                                        ThreeDThrottle += ControlsRoot.forward * ThreeDThrottleInput.y * ThreeDThrottleStrengthY.x;
                                    }
                                    else
                                    {
                                        ThreeDThrottle += ControlsRoot.forward * ThreeDThrottleInput.y * ThreeDThrottleStrengthY.y;
                                    }
                                }
                            }
                            if (ThreeDThrottleInput.z > 0)
                            {
                                ThreeDThrottle += ControlsRoot.forward * ThreeDThrottleInput.z * ThreeDThrottleStrengthZ.x;
                            }
                            else
                            {
                                ThreeDThrottle += ControlsRoot.forward * ThreeDThrottleInput.z * ThreeDThrottleStrengthZ.y;
                            }
                        }
                        else
                        {
                            if (ThreeDThrottleInput.z != 0)
                            {
                                if (UseThrottleAsForward)
                                {
                                    if (ThreeDThrottleInput.z > 0)
                                    {
                                        SAVControl.PlayerThrottle = Mathf.Max(ThreeDThrottleInput.z, 0);
                                    }
                                    else if (!ThrottleAsForward_NoBackThrust)
                                    {
                                        if (ThreeDThrottleInput.z > 0)
                                        {
                                            ThreeDThrottle += ControlsRoot.forward * Mathf.Min(ThreeDThrottleInput.z, 0) * ThreeDThrottleStrengthZ.x;
                                        }
                                        else
                                        {
                                            ThreeDThrottle += ControlsRoot.forward * Mathf.Min(ThreeDThrottleInput.z, 0) * ThreeDThrottleStrengthZ.y;
                                        }
                                        SAVControl.PlayerThrottle = 0;
                                    }
                                }
                                else
                                {
                                    if (ThreeDThrottleInput.z > 0)
                                    {
                                        ThreeDThrottle += ControlsRoot.forward * ThreeDThrottleInput.z * ThreeDThrottleStrengthZ.x;
                                    }
                                    else
                                    {
                                        ThreeDThrottle += ControlsRoot.forward * ThreeDThrottleInput.z * ThreeDThrottleStrengthZ.y;
                                    }
                                }
                            }
                            if (ThreeDThrottleInput.y > 0)
                            {
                                ThreeDThrottle += ControlsRoot.up * ThreeDThrottleInput.y * ThreeDThrottleStrengthY.x;
                            }
                            else
                            {
                                ThreeDThrottle += ControlsRoot.up * ThreeDThrottleInput.y * ThreeDThrottleStrengthY.y;
                            }
                        }
                    }
                }
                if (InVR && ThrustArrow)
                {
                    ThrustArrow.localScale = (Vector3.one * ThreeDThrottleInput.magnitude) / ThrottleSensitivity;
                }
                if (!AllowMainEngineAndThrust)
                {
                    SAVControl.SetProgramVariable("ThrottleOverride", Mathf.Min(ThreeDThrottleInput.magnitude, 1));
                }
            }
            if (Occupied)
            {
                if (ThrustAnimator)
                {
                    ThrustAnimator.SetFloat("thrustx", ThreeDThrottleInput.x * .5f + .5f);
                    ThrustAnimator.SetFloat("thrusty", ThreeDThrottleInput.y * .5f + .5f);
                    ThrustAnimator.SetFloat("thrustz", ThreeDThrottleInput.z * .5f + .5f);
                }
            }
        }
        public void SFEXT_G_Wrecked()
        {
            if (ThrustArrow) { ThrustArrow.gameObject.SetActive(false); }
            ThreeDThrottle = ThreeDThrottleInput = Vector3.zero;
            setAnimatorDefault();
        }
        public void SFEXT_G_Explode()
        { setAnimatorDefault(); }
        private void setAnimatorDefault()
        {
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
            if (IsOwner)
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
    }
}