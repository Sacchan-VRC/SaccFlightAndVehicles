
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DFUNC_Horn : UdonSharpBehaviour
    {
        public AudioSource Horn;
        private bool UseLeftTrigger;
        private bool TriggerLastFrame;
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
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(PlayHorn));
                }
                TriggerLastFrame = true;
            }
            else { TriggerLastFrame = false; }
        }
        public void PlayHorn()
        {
            if (Horn)
            {
                Horn.Play();
            }
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
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(PlayHorn));
        }
    }
}