
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DFUNC_ToggleEngine : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public float ToggleMinDelay = 6f;
        public float StartUpTime = 5f;
        public GameObject Dial_Funcon;
        [Space(10)]
        [Tooltip("AnimEngineStartupAnimBool is true when engine is starting, and remains true until engine is turned off")]
        public bool DoEngineStartupAnimBool;
        [Header("Only required if above is ticked")]
        public Animator EngineAnimator;
        public string AnimEngineStartupAnimBool = "EngineStarting";
        private SaccEntity EntityControl;
        private bool NoFuel;
        private int EngineStartCount;
        private int EngineStartCancelCount;
        private bool UseLeftTrigger = false;
        private bool TriggerLastFrame;
        private float ToggleTime;
        private float TriggerTapTime;
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        public void SFEXT_L_EntityStart()
        {
            EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
        }
        private void Update()
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
                    bool engon = (bool)SAVControl.GetProgramVariable("_EngineOn");
                    if (engon)
                    {
                        if (Time.time - TriggerTapTime < 0.4f)
                        {//double tap
                            ToggleEngine(engon);
                        }
                    }
                    else
                    {
                        ToggleEngine(engon);
                    }
                    TriggerTapTime = Time.time;
                }
                TriggerLastFrame = true;
            }
            else { TriggerLastFrame = false; }
        }
        public void ToggleEngine(bool EngOn)
        {
            if (Time.time - ToggleTime > ToggleMinDelay)
            {
                if (EngOn)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EngineOff));
                }
                else if (EngineStartCount > EngineStartCancelCount)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EngineStartupCancel));
                }
                else
                {
                    if (!NoFuel)
                    {
                        if (StartUpTime == 0)
                        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EngineOn)); }
                        else
                        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EngineStartup)); }
                    }
                }
            }
        }
        public void DFUNC_Selected()
        { gameObject.SetActive(true); }
        public void DFUNC_Deselected()
        { gameObject.SetActive(false); }
        public void SFEXT_O_PilotExit()
        { gameObject.SetActive(false); }
        public void EngineStartup()
        {
            EngineStartCount++;
            ToggleTime = Time.time;
            if (Dial_Funcon) { Dial_Funcon.SetActive(true); }
            SendCustomEventDelayedSeconds(nameof(EngineStartupFinish), StartUpTime);
            if (DoEngineStartupAnimBool)
            { EngineAnimator.SetBool(AnimEngineStartupAnimBool, true); }
            EntityControl.SendEventToExtensions("SFEXT_G_EngineStartup");
        }
        public void EngineStartupFinish()
        {
            if (EngineStartCount > 0) { EngineStartCount--; }
            if (EntityControl.IsOwner)
            {
                if (!NoFuel)
                {
                    if (EngineStartCount == 0 && EngineStartCancelCount == 0)
                    {
                        if (!EntityControl._dead)
                        {
                            if (!(bool)SAVControl.GetProgramVariable("_EngineOn") && !(bool)SAVControl.GetProgramVariable("_PreventEngineToggle"))
                            {
                                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(JustEngineOn)); }
                            }
                        }
                    }
                }
            }
            if (EngineStartCancelCount > 0)
            { EngineStartCancelCount--; }
        }
        public void EngineStartupCancel()
        {
            EngineStartCancelCount++;
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
            if (DoEngineStartupAnimBool)
            { EngineAnimator.SetBool(AnimEngineStartupAnimBool, false); }
            EntityControl.SendEventToExtensions("SFEXT_G_EngineStartupCancel");
        }
        public void SFEXT_G_ReAppear()
        {
            EngineOff();
            ResetStartup();
        }
        public void SFEXT_G_RespawnButton()
        {
            ResetStartup();
        }
        public void ResetStartup()
        {
            if (EngineStartCount > 0 && EngineStartCount != EngineStartCancelCount)
            {
                EngineStartCancelCount = EngineStartCount;
            }
        }
        public void EngineOn()
        {
            if (!(bool)SAVControl.GetProgramVariable("_EngineOn") && !(bool)SAVControl.GetProgramVariable("_PreventEngineToggle"))
            {
                ToggleTime = Time.time;
                SAVControl.SetProgramVariable("_EngineOn", true);
                if (DoEngineStartupAnimBool)
                { EngineAnimator.SetBool(AnimEngineStartupAnimBool, true); }
            }
        }
        public void JustEngineOn()
        {
            SAVControl.SetProgramVariable("_EngineOn", true);
        }
        public void EngineOff()
        {
            if ((bool)SAVControl.GetProgramVariable("_EngineOn") && !(bool)SAVControl.GetProgramVariable("_PreventEngineToggle"))
            {
                ToggleTime = Time.time - StartUpTime;
                SAVControl.SetProgramVariable("_EngineOn", false);
            }
        }
        public void SFEXT_G_EngineOff()
        {
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
            if (DoEngineStartupAnimBool)
            { EngineAnimator.SetBool(AnimEngineStartupAnimBool, false); }
        }
        public void SFEXT_G_EngineOn()
        {
            if (Dial_Funcon) { Dial_Funcon.SetActive(true); }
            if (DoEngineStartupAnimBool)
            { EngineAnimator.SetBool(AnimEngineStartupAnimBool, true); }
        }
        public void SFEXT_G_NoFuel()
        { NoFuel = true; }
        public void SFEXT_G_NotNoFuel()
        { NoFuel = false; }
        public void KeyboardInput()
        {
            ToggleEngine((bool)SAVControl.GetProgramVariable("_EngineOn"));
        }
    }
}