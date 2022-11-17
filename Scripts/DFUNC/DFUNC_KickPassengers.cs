
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DFUNC_KickPassengers : UdonSharpBehaviour
    {
        public SaccEntity EntityControl;
        private bool UseLeftTrigger;
        private bool TriggerLastFrame;
        private bool Passenger;
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        void Update()
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
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(KickPassengers));
                }
                TriggerLastFrame = true;
            }
            else { TriggerLastFrame = false; }
        }
        public void KickPassengers()
        {
            if (Passenger)
            {
                EntityControl.ExitStation();
            }
        }
        public void SFEXT_P_PassengerEnter()
        {
            Passenger = true;
        }
        public void SFEXT_P_PassengerExit()
        {
            Passenger = false;
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
        public void KeyboardInput()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(KickPassengers));
        }
    }
}