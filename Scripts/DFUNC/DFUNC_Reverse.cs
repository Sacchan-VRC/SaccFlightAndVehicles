
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_Reverse : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public float ReversingThrottleMultiplier = -.5f;
        public GameObject Dial_Funcon;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [System.NonSerializedAttribute] public SAV_PassengerFunctionsController PassengerFunctionsControl;
        private float StartThrottleStrength;
        private float StartABStrength;
        private float ReversingThrottleStrength;
        private float ReversingABStrength;
        private bool TriggerLastFrame;
        [System.NonSerializedAttribute] public bool Reversing;
        public void SFEXT_L_EntityStart()
        {
            StartThrottleStrength = (float)SAVControl.GetProgramVariable("ThrottleStrength");
            StartABStrength = (float)SAVControl.GetProgramVariable("ThrottleStrengthAB");
            ReversingThrottleStrength = StartThrottleStrength * ReversingThrottleMultiplier;
            ReversingABStrength = StartABStrength * ReversingThrottleMultiplier;
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
        }
        public void DFUNC_Selected()
        {
            TriggerLastFrame = true;
            gameObject.SetActive(true);
        }
        public void DFUNC_Deselected()
        {
            gameObject.SetActive(false);
        }
        public void SFEXT_O_PilotExit()
        {
            if (Reversing)
            { SetNotReversing(); }
            gameObject.SetActive(false);
        }
        private void Update()
        {
            float Trigger;
            if (LeftDial)
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
            else
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
            if (Trigger > 0.75)
            {
                if (!TriggerLastFrame) { ToggleReverse(); }
                TriggerLastFrame = true;
            }
            else { TriggerLastFrame = false; }
        }
        private void ToggleReverse()
        {
            if (!Reversing)
            { SetReversing(); }
            else
            { SetNotReversing(); }
        }
        private void SetReversing()
        {
            Reversing = true;
            SAVControl.SetProgramVariable("ThrottleStrength", ReversingThrottleStrength);
            SAVControl.SetProgramVariable("ThrottleStrengthAB", ReversingABStrength);
            if (Dial_Funcon) { Dial_Funcon.SetActive(true); }
            EntityControl.SendEventToExtensions("SFEXT_O_StartReversing");
        }
        private void SetNotReversing()
        {
            Reversing = false;
            SAVControl.SetProgramVariable("ThrottleStrength", StartThrottleStrength);
            SAVControl.SetProgramVariable("ThrottleStrengthAB", StartABStrength);
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
            EntityControl.SendEventToExtensions("SFEXT_O_StopReversing");
        }
        public void KeyboardInput()
        {
            ToggleReverse();
        }
    }
}