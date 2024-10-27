//This script is used to set gearbox to reverse if it's in automatic world.
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_GearBoxReverse : UdonSharpBehaviour
    {
        [Header("This function just toggles reverse on automatic gearboxes.")]
        public UdonSharpBehaviour GearBox;
        public GameObject Dial_funcon;
        private bool Reversing;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [System.NonSerializedAttribute] public SAV_PassengerFunctionsController PassengerFunctionsControl;
        private bool TriggerLastFrame;
        public void SFEXT_L_EntityStart()
        {
            gameObject.SetActive(false);
            if (Dial_funcon) { Dial_funcon.SetActive(Reversing); }
        }
        public void DFUNC_Selected() { gameObject.SetActive(true); }
        public void DFUNC_Deselected() { gameObject.SetActive(false); }
        private void Update()
        {
            float Trigger;
            if (LeftDial)
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
            else
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
            if (Trigger > 0.75)
            {
                if (!TriggerLastFrame)
                {
                    Toggle();
                }
                TriggerLastFrame = true;
            }
            else { TriggerLastFrame = false; }
        }
        public void SFEXT_O_PilotEnter()
        {
            Reversing = (bool)GearBox.GetProgramVariable("_AutomaticReversing");
        }
        public void SFEXT_O_PilotExit()
        {
            gameObject.SetActive(false);
        }
        public void Toggle()
        {
            Reversing = !(bool)GearBox.GetProgramVariable("_AutomaticReversing");
            GearBox.SetProgramVariable("_AutomaticReversing", Reversing);
            if (Dial_funcon) { Dial_funcon.SetActive(Reversing); }
        }
        public void KeyboardInput()
        {
            Toggle();
        }
    }
}