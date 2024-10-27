
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_ThreeDThrustToggle : UdonSharpBehaviour
    {
        public SAV_ThreeDThrust ThreeDThrustScript;
        public GameObject Dial_Funcon;
        private bool TriggerLastFrame;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [System.NonSerializedAttribute] public SAV_PassengerFunctionsController PassengerFunctionsControl;
        private void ToggleThreeD()
        {
            bool active = !((bool)ThreeDThrustScript.GetProgramVariable("_ThreeDThrustActive"));
            if (Dial_Funcon) { Dial_Funcon.SetActive(active); }
            ThreeDThrustScript.SetProgramVariable("_ThreeDThrustActive", active);
        }
        public void SFEXT_L_EntityStart()
        {
            Dial_Funcon.SetActive(ThreeDThrustScript.DefaultEnabled);
        }
        public void KeyboardInput()
        {
            ToggleThreeD();
        }
        public void DFUNC_Selected()
        {
            gameObject.SetActive(true);
        }
        public void DFUNC_Deselected()
        {
            gameObject.SetActive(false);
        }
        public void SFEXT_G_Explode()
        {
            if ((bool)ThreeDThrustScript.GetProgramVariable("_ThreeDThrustActive"))
            {
                ToggleThreeD();
            }
        }
        public void SFEXT_G_RespawnButton()
        {
            if ((bool)ThreeDThrustScript.GetProgramVariable("_ThreeDThrustActive"))
            {
                ToggleThreeD();
            }
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
                if (!TriggerLastFrame)
                {
                    ToggleThreeD();
                }
                TriggerLastFrame = true;
            }
            else { TriggerLastFrame = false; }
        }
    }
}