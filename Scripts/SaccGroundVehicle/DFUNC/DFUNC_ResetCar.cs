
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_ResetCar : UdonSharpBehaviour
    {
        public SaccEntity EntityControl;
        private SaccGroundVehicle SGVControl;
        [Tooltip("Height added to vehicle when it's reset")]
        public float AddedHeight = 0f;
        private Transform VehicleTransform;
        private bool Selected;
        private bool InVR;
        private bool UseLeftTrigger;
        private bool TriggerLastFrame;
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        public void SFEXT_L_EntityStart()
        {
            VehicleTransform = EntityControl.transform;
            InVR = EntityControl.InVR;
            SGVControl = (SaccGroundVehicle)EntityControl.GetExtention(GetUdonTypeName<SaccGroundVehicle>());
        }
        public void DFUNC_Selected()
        {
            Selected = true;
            TriggerLastFrame = true;
            gameObject.SetActive(true);
        }
        public void DFUNC_Deselected()
        {
            Selected = false;
            gameObject.SetActive(false);
        }
        public void SFEXT_O_PilotExit()
        {
            Selected = false;
            gameObject.SetActive(false);
        }
        void Update()
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
                        ResetCar();
                    }
                    TriggerLastFrame = true;
                }
                else { TriggerLastFrame = false; }
            }
        }
        private void ResetCar()
        {
            VehicleTransform.rotation = Quaternion.Euler(new Vector3(0f, VehicleTransform.rotation.eulerAngles.y, 0f));
            VehicleTransform.position += Vector3.up * AddedHeight;
            if (SGVControl)
            {
                SGVControl.YawInput = 0;
            }
        }
        public void KeyboardInput()
        {
            ResetCar();
        }
    }
}