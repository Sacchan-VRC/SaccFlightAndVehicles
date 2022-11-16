
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DFUNC_Limits : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public GameObject HudLimit;
        public bool DefaultLimitsOn = true;
        [Tooltip("Object enabled when function is active (used on MFD)")]
        public GameObject Dial_Funcon;
        [Tooltip("Below this number of positive Gs, G Limiter has zero effect")]
        public float GLimiter_Begin_Pos = 35f;
        [Tooltip("Linearly decrease control limit up to this many positive Gs")]
        public float GLimiter_Pos = 40f;
        [Tooltip("Below this number of negative Gs, G Limiter has zero effect")]
        public float GLimiter_Begin_Neg = -1f;
        [Tooltip("Linearly decrease control limit up to this many negative Gs. IF SET <0, VALUES WILL BE SET TO MATCH POSITIVE VALUES")]
        public float GLimiter_Neg = -1f;
        [Tooltip("Below this number of AoA, AoA Limiter has zero effect. IF SET <0, VALUES WILL BE SET TO MATCH POSITIVE VALUES")]
        public float AoALimiter_Begin = 10f;
        [Tooltip("Try to stop pilot pulling this much AoA")]
        public float AoALimiter = 14f;
        public bool DisableGLimiter = false;
        public bool DisableAoALimiter = false;
        private SaccEntity EntityControl;
        private bool UseLeftTrigger = false;
        private bool TriggerLastFrame;
        private bool InVR;
        private bool Piloting;
        private bool Grounded = true;
        private bool Selected;
        private float GLimiterRange_Pos;
        private float GLimiterRange_Neg;
        private float AoALimiterRange;
        [System.NonSerializedAttribute] public bool FlightLimitsEnabled = true;
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        public void SFEXT_L_EntityStart()
        {
            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            if (localPlayer != null)
            { InVR = localPlayer.IsUserInVR(); }
            EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
            if (!DefaultLimitsOn) { SetLimitsOff(); }
            GLimiterRange_Pos = GLimiter_Pos - GLimiter_Begin_Pos;
            if (GLimiter_Neg < 0 || GLimiter_Begin_Neg < 0)
            {
                GLimiter_Neg = GLimiter_Pos;
                GLimiter_Begin_Neg = GLimiter_Begin_Pos;
            }
            GLimiterRange_Neg = GLimiter_Neg - GLimiter_Begin_Neg;
            AoALimiterRange = AoALimiter - AoALimiter_Begin;
        }
        public void DFUNC_Selected()
        {
            TriggerLastFrame = true;
            Selected = true;
            gameObject.SetActive(true);
        }
        public void DFUNC_Deselected()
        {
            Selected = false;
            if (!FlightLimitsEnabled) { gameObject.SetActive(false); }
        }
        public void SFEXT_O_PilotEnter()
        {
            if (FlightLimitsEnabled) { gameObject.SetActive(true); }
            Piloting = true;
            if (Dial_Funcon) Dial_Funcon.SetActive(FlightLimitsEnabled);
        }
        public void SFEXT_O_PilotExit()
        {
            gameObject.SetActive(false);
            Selected = false;
            Piloting = false;
        }
        public void SFEXT_O_EnterVTOL()
        {
            if (FlightLimitsEnabled)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetLimitsOff)); }
        }
        public void SFEXT_G_Explode()
        {
            gameObject.SetActive(false);
            if (DefaultLimitsOn)
            { SetLimitsOn(); }
            else
            { SetLimitsOff(); }
        }
        public void SetLimitsOn()
        {
            if (FlightLimitsEnabled) { return; }
            if (Piloting && !InVR) { gameObject.SetActive(true); }
            FlightLimitsEnabled = true;
            if (HudLimit) { HudLimit.SetActive(true); }
            if (Dial_Funcon) { Dial_Funcon.SetActive(true); }
            EntityControl.SendEventToExtensions("SFEXT_O_LimitsOn");
        }
        public void SetLimitsOff()
        {
            if (!FlightLimitsEnabled) { return; }
            if (Piloting && !InVR) { gameObject.SetActive(false); }
            FlightLimitsEnabled = false;
            if (HudLimit) { HudLimit.SetActive(false); }
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
            SAVControl.SetProgramVariable("Limits", 1f);
            EntityControl.SendEventToExtensions("SFEXT_O_LimitsOff");
        }
        public void SFEXT_L_PassengerEnter()
        {
            if (Dial_Funcon) { Dial_Funcon.SetActive(FlightLimitsEnabled); }
        }
        public void SFEXT_G_RespawnButton()
        {
            if (DefaultLimitsOn) { SetLimitsOn(); }
            else { SetLimitsOff(); }
        }
        public void SFEXT_O_OnPlayerJoined()
        {
            if (!FlightLimitsEnabled && DefaultLimitsOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetLimitsOff)); }
            else if (FlightLimitsEnabled && !DefaultLimitsOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetLimitsOn)); }
        }
        private void Update()
        {
            if (Selected)
            {
                float Trigger;
                if (UseLeftTrigger)
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                else
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

                if (Trigger > 0.75)
                {
                    if (!TriggerLastFrame)
                    {
                        ToggleLimits();
                    }
                    TriggerLastFrame = true;
                }
                else { TriggerLastFrame = false; }
            }
            if (FlightLimitsEnabled && Piloting && !Grounded)
            {
                float GLimitStrength = 1f;
                float AoALimitStrength = 1f;
                if (!DisableGLimiter)
                {
                    float vertGs = (float)SAVControl.GetProgramVariable("VertGs");
                    if (vertGs > 0)
                    { GLimitStrength = Mathf.Clamp(1 - ((-GLimiter_Begin_Pos + Mathf.Abs((float)SAVControl.GetProgramVariable("VertGs"))) / GLimiterRange_Pos), 0, 1); }
                    else
                    { GLimitStrength = Mathf.Clamp(1 - ((-GLimiter_Begin_Neg + Mathf.Abs((float)SAVControl.GetProgramVariable("VertGs"))) / GLimiterRange_Neg), 0, 1); }
                }
                if (!DisableAoALimiter) { AoALimitStrength = Mathf.Clamp(1 - ((-AoALimiter_Begin + Mathf.Abs((float)SAVControl.GetProgramVariable("AngleOfAttack"))) / AoALimiterRange), 0, 1); }
                SAVControl.SetProgramVariable("Limits", Mathf.Min(GLimitStrength, AoALimitStrength));
            }
        }
        public void SFEXT_G_TakeOff()
        { Grounded = false; }
        public void SFEXT_G_TouchDown()
        { Grounded = true; SAVControl.SetProgramVariable("Limits", 1f); }
        public void SFEXT_G_TouchDownWater()
        { Grounded = true; SAVControl.SetProgramVariable("Limits", 1f); }
        public void KeyboardInput()
        {
            ToggleLimits();
        }
        public void ToggleLimits()
        {
            if (!FlightLimitsEnabled)
            {
                if ((float)SAVControl.GetProgramVariable("VTOLAngle") != (float)SAVControl.GetProgramVariable("VTOLDefaultValue")) { return; }
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetLimitsOn));
            }
            else
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetLimitsOff));
            }
        }
    }
}
