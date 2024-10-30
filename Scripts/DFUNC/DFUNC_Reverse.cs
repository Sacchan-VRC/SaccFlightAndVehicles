
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    //Set to Continuous because going from None->NoVariableSync just leaves it on None internally until they fix it ¯\_(ツ)_/¯
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class DFUNC_Reverse : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public float ReversingThrottleMultiplier = -.5f;
        public GameObject Dial_Funcon;
        [SerializeField] bool ResetOnExit = false;
        [Tooltip("Would you like to set an animator bool to true while reversing?")]
        [SerializeField] Animator ReverseAnimator;
        [Tooltip("Name of the animator bool to set?")]
        [SerializeField] string AnimBoolName = "reversing";
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
            SAVControl.SetProgramVariable("InverThrustMultiplier", ReversingThrottleMultiplier);
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
            gameObject.SetActive(false);
        }
        public void SFEXT_G_PilotExit()
        {
            if (ResetOnExit && Reversing)
            { SetNotReversing(); }
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
        public void ToggleReverse()
        {
            if (!Reversing)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetReversing));
                EntityControl.SendEventToExtensions("SFEXT_O_StartReversing");
            }
            else
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetNotReversing));
                EntityControl.SendEventToExtensions("SFEXT_O_StopReversing");
            }
        }
        public void SetReversing()
        {
            if (Reversing) { return; }
            Reversing = true;
            if (ReverseAnimator) { ReverseAnimator.SetBool(AnimBoolName, true); }
            SAVControl.SetProgramVariable("InvertThrust", (int)SAVControl.GetProgramVariable("InvertThrust") + 1);
            if (Dial_Funcon) { Dial_Funcon.SetActive(true); }
        }
        public void SetNotReversing()
        {
            if (!Reversing) { return; }
            Reversing = false;
            if (ReverseAnimator) { ReverseAnimator.SetBool(AnimBoolName, false); }
            SAVControl.SetProgramVariable("InvertThrust", (int)SAVControl.GetProgramVariable("InvertThrust") - 1);
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
        }
        public void KeyboardInput()
        {
            ToggleReverse();
        }
        public void SFEXT_O_OnPlayerJoined()
        {
            if (Reversing)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetReversing)); }
        }
    }
}