
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DFUNC_CarJump : UdonSharpBehaviour
    {
        public SaccGroundVehicle SGVControl;
        public float JumpForce = 5f;
        public float HoldJumpForce = 10f;
        public Animator JumpAnimator;
        public float MaxJumpLengthSecs = 0.2f;
        public KeyCode JumpKey;
        public AudioSource JumpSound;
        public string AnimatorTriggerName = string.Empty;
        private Rigidbody VehicleRigidBody;
        private Transform VehicleTransform;
        private bool Selected;
        private bool InVR;
        private bool UseLeftTrigger;
        private bool TriggerLastFrame;
        private bool Jumping;
        private bool DoAnimTrigger;
        private float JumpTime;
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        public void SFEXT_L_EntityStart()
        {
            VehicleRigidBody = (Rigidbody)SGVControl.GetProgramVariable("VehicleRigidbody");
            InVR = (bool)SGVControl.GetProgramVariable("InVR");
            VehicleTransform = VehicleRigidBody.transform;
            DoAnimTrigger = AnimatorTriggerName != string.Empty;
        }
        void Update()
        {
            if (!InVR || Selected)
            {
                float Trigger;
                if (UseLeftTrigger)
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                else
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

                if (Trigger > 0.75 || Input.GetKey(JumpKey))
                {
                    if (!TriggerLastFrame)
                    {
                        TriggerLastFrame = true;
                        if ((bool)SGVControl.GetProgramVariable("Grounded"))
                        {
                            Jump();
                        }
                    }
                }
                else { TriggerLastFrame = false; }
            }
        }
        public void DFUNC_Selected()
        {
            Selected = true;
            gameObject.SetActive(true);
        }
        public void DFUNC_Deselected()
        {
            Selected = false;
            gameObject.SetActive(false);
            TriggerLastFrame = false;
        }
        public void SFEXT_O_PilotEnter()
        {
            if (!InVR)
            {
                gameObject.SetActive(true);
            }
        }
        public void SFEXT_O_PilotExit()
        {
            gameObject.SetActive(false);
        }
        public void Jump()
        {
            JumpTime = Time.time;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Jump_Event));
            VehicleRigidBody.AddForce(VehicleTransform.up * JumpForce, ForceMode.VelocityChange);
            SendCustomEventDelayedFrames(nameof(JumpLoop), 1);
        }
        public void Jump_Event()
        {
            if (JumpSound) { JumpSound.PlayOneShot(JumpSound.clip); }
            if (DoAnimTrigger && JumpAnimator) { JumpAnimator.SetTrigger(AnimatorTriggerName); }
        }
        public void JumpLoop()
        {
            if (TriggerLastFrame)
            {
                VehicleRigidBody.AddForce(VehicleTransform.up * HoldJumpForce * Time.deltaTime, ForceMode.VelocityChange);

                if (Time.time - JumpTime < MaxJumpLengthSecs)
                {
                    SendCustomEventDelayedFrames(nameof(JumpLoop), 1);
                }
            }
        }
    }
}
