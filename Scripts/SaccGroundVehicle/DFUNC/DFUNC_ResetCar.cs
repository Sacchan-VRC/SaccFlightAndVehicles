
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_ResetCar : UdonSharpBehaviour
    {
        private SaccGroundVehicle SGVControl;
        [Tooltip("Height added to vehicle when it's reset")]
        public float AddedHeight = 0f;
        [Tooltip("Vehicle must be moving below this speed to allow reset, meters/sec")]
        public float AllowRespawnSpeed = 9999999f;
        [Tooltip("Set vehicle's speed to zero when reset")]
        public bool StopCarOnReset = false;
        [SerializeField] private float ResetMinDelay = 0;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [System.NonSerializedAttribute] public SAV_PassengerFunctionsController PassengerFunctionsControl;
        private float RespawnTime;
        private Transform VehicleTransform;
        private bool Selected;
        private bool InVR;
        private bool TriggerLastFrame;
        private Rigidbody VehicleRigidbody;
        public void SFEXT_L_EntityStart()
        {
            VehicleTransform = EntityControl.transform;
            InVR = EntityControl.InVR;
            SGVControl = (SaccGroundVehicle)EntityControl.GetExtention(GetUdonTypeName<SaccGroundVehicle>());
            VehicleRigidbody = EntityControl.VehicleRigidbody;
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
                if (LeftDial)
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
            Rigidbody rb = EntityControl.GetComponent<Rigidbody>();
            if (Time.time - RespawnTime < ResetMinDelay ||
                rb.velocity.magnitude > AllowRespawnSpeed
            ) { return; }
            RespawnTime = Time.time;
            if (StopCarOnReset)
            {
                if (rb) { rb.velocity = Vector3.zero; }
            }
            VehicleTransform.rotation = Quaternion.Euler(new Vector3(0f, VehicleTransform.rotation.eulerAngles.y, 0f));
            VehicleRigidbody.rotation = VehicleTransform.rotation;
            VehicleTransform.position += Vector3.up * AddedHeight;
            VehicleRigidbody.position = VehicleTransform.position;
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