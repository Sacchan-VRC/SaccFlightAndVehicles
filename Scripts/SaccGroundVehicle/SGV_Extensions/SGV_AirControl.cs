
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SGV_AirControl : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SGVControl;
        public float RotationStrengthPitch = 3f;
        public float RotationStrengthRoll = 4f;
        public float RotationStrengthYaw = 2f;
        [Tooltip("Use the left hand to control the joystick and the right hand to control the throttle?")]
        public bool SwitchHandsJoyThrottle = false;
        [Tooltip("Joystick sensitivity. Angle at which joystick will reach maximum deflection in VR")]
        public Vector3 MaxJoyAngles = new Vector3(45, 45, 45);
        public Animator VehicleAnimator;
        private SaccEntity EntityControl;
        private Transform ControlsRoot;
        [System.NonSerializedAttribute] public bool JoystickGripLastFrame = false;
        private Transform VehicleTransform;
        private Rigidbody VehicleRigidbody;
        private bool SGVControlsDisabled = false;
        private bool Grounded;
        private bool AirControlReady;
        private bool HoldingJoyStick;
        private bool WheelHeld = false;
        private bool WheelHeldR = false;
        private bool WheelHeldL = false;
        private bool JoyMoving;
        private float JoyOut = 0;
        private float JoyOutTarget = 0;
        Quaternion VehicleRotLastFrame;
        Quaternion JoystickZeroPoint;
        private int AIRCONTROL_STRING = Animator.StringToHash("aircontrol");
        private int JOYOUT_STRING = Animator.StringToHash("joyout");
        private int PITCH_STRING = Animator.StringToHash("pitch");
        private int YAW_STRING = Animator.StringToHash("yaw");
        private int ROLL_STRING = Animator.StringToHash("roll");
        public float GripSensitivity = .75f;
        private VRCPlayerApi localPlayer;

        public void SFEXT_L_EntityStart()
        {
            EntityControl = (SaccEntity)SGVControl.GetProgramVariable("EntityControl");
            VehicleRigidbody = (Rigidbody)SGVControl.GetProgramVariable("VehicleRigidbody");
            GripSensitivity = (float)SGVControl.GetProgramVariable("GripSensitivity");
            ControlsRoot = (Transform)SGVControl.GetProgramVariable("ControlsRoot");
            localPlayer = Networking.LocalPlayer;
            VehicleTransform = VehicleRigidbody.transform;
        }
        private void Update()
        {
            if (!Grounded && AirControlReady || HoldingJoyStick)
            {
                int Wi = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0;
                int Si = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? -1 : 0;
                int Ai = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0;
                int Di = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? -1 : 0;
                int Qi = Input.GetKey(KeyCode.Q) ? -1 : 0;
                int Ei = Input.GetKey(KeyCode.E) ? 1 : 0;


                Vector3 VRJoystickPos = Vector3.zero;
                float JoyStickGrip;
                if (SwitchHandsJoyThrottle)
                {
                    JoyStickGrip = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger");
                }
                else
                {
                    JoyStickGrip = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
                }
                //VR Joystick
                if (JoyStickGrip > GripSensitivity)
                {
                    Quaternion VehicleRotDif = ControlsRoot.rotation * Quaternion.Inverse(VehicleRotLastFrame);//difference in vehicle's rotation since last frame
                    VehicleRotLastFrame = ControlsRoot.rotation;
                    JoystickZeroPoint = VehicleRotDif * JoystickZeroPoint;//zero point rotates with the vehicle so it appears still to the pilot
                    if (!JoystickGripLastFrame)//first frame you gripped joystick
                    {
                        JoystickGrabbed();
                        VehicleRotDif = Quaternion.identity;
                        if (SwitchHandsJoyThrottle)
                        {
                            JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation;
                            localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .05f, .07f, 35);
                        }//rotation of the controller relative to the plane when it was pressed
                        else
                        {
                            JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;
                            localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .07f, 35);
                        }
                    }
                    JoystickGripLastFrame = true;
                    //difference between the vehicle and the hand's rotation, and then the difference between that and the JoystickZeroPoint, finally rotated by the vehicles rotation to turn it back to vehicle space
                    Quaternion JoystickDifference;
                    JoystickDifference = Quaternion.Inverse(ControlsRoot.rotation) *
                        (SwitchHandsJoyThrottle ? localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation
                                                : localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation)
                    * Quaternion.Inverse(JoystickZeroPoint)
                     * ControlsRoot.rotation;

                    //create normalized vectors facing towards the 'forward' and 'up' directions of the joystick
                    Vector3 JoystickPosYaw = (JoystickDifference * Vector3.forward);
                    Vector3 JoystickPos = (JoystickDifference * Vector3.up);
                    //use acos to convert the relevant elements of the array into radians, re-center around zero, then normalize between -1 and 1 and dovide for desired deflection
                    //the clamp is there because rotating a vector3 can cause it to go a miniscule amount beyond length 1, resulting in NaN (crashes vrc)
                    VRJoystickPos.x = -((Mathf.Acos(Mathf.Clamp(JoystickPos.z, -1, 1)) - 1.5707963268f) * Mathf.Rad2Deg) / MaxJoyAngles.x;
                    VRJoystickPos.y = -((Mathf.Acos(Mathf.Clamp(JoystickPosYaw.x, -1, 1)) - 1.5707963268f) * Mathf.Rad2Deg) / MaxJoyAngles.y;
                    VRJoystickPos.z = ((Mathf.Acos(Mathf.Clamp(JoystickPos.x, -1, 1)) - 1.5707963268f) * Mathf.Rad2Deg) / MaxJoyAngles.z;
                }
                else
                {
                    VRJoystickPos = Vector3.zero;
                    if (JoystickGripLastFrame)//first frame you let go of joystick
                    {
                        JoystickDropped();
                        if (SwitchHandsJoyThrottle)
                        { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .05f, .07f, 35); }
                        else
                        { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .07f, 35); }
                    }
                    JoystickGripLastFrame = false;
                }

                float pitch = Mathf.Clamp(VRJoystickPos.x + Wi + Si, -1, 1);
                float yaw = Mathf.Clamp(VRJoystickPos.y + Qi + Ei, -1, 1);
                float roll = Mathf.Clamp(VRJoystickPos.z + Ai + Di, -1, 1);
                Vector3 RotForce = new Vector3(
                pitch * RotationStrengthPitch
                , yaw * RotationStrengthYaw
                , roll * RotationStrengthRoll
                );

                VehicleRigidbody.AddRelativeTorque(RotForce, ForceMode.Acceleration);

                VehicleAnimator.SetFloat(PITCH_STRING, pitch * .5f + .5f);
                VehicleAnimator.SetFloat(YAW_STRING, yaw * .5f + .5f);
                VehicleAnimator.SetFloat(ROLL_STRING, roll * .5f + .5f);
            }
        }
        public void SFEXT_O_Grounded()
        {
            Grounded = true;
            AirControlReady = false;
            if (!HoldingJoyStick)
            {
                JoystickGripLastFrame = false;
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(DisableJoystick));
            }
        }
        public void DisableJoystick()
        {
            JoyOutTarget = 0f;
            if (!JoyMoving)
            {
                JoyMoving = true;
                MoveJoy();
            }
            if (SGVControlsDisabled)
            {
                SGVControl.SetProgramVariable("DisableInput", (int)SGVControl.GetProgramVariable("DisableInput") - 1);
                VehicleAnimator.SetBool(AIRCONTROL_STRING, false);
                SGVControlsDisabled = false;
            }
            ResetAnim();
        }
        public void SFEXT_O_Airborne()
        {
            Grounded = false;
            if (!WheelHeld)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EnableJoystick));
            }
        }
        public void SFEXT_O_WheelDroppedR()
        {
            WheelHeldR = false;
            if (!WheelHeldL)
            {
                WheelHeld = false;
                if (!Grounded)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EnableJoystick));
                }
            }
        }
        public void SFEXT_O_WheelDroppedL()
        {
            WheelHeldL = false;
            if (!WheelHeldR)
            {
                WheelHeld = false;
                if (!Grounded)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EnableJoystick));
                }
            }
        }
        public void SFEXT_O_WheelGrabbedR()
        {
            WheelHeldR = true;
            WheelHeld = true;
        }
        public void SFEXT_O_WheelGrabbedL()
        {
            WheelHeldL = true;
            WheelHeld = true;
        }
        public void SFEXT_G_Explode()
        {
            ResetAnim();
            DisableJoystick();
        }
        public void SFEXT_G_PilotExit()
        {
            ResetAnim();
            DisableJoystick();
        }
        public void JoystickGrabbed()
        {
            HoldingJoyStick = true;
            EntityControl.SendEventToExtensions("SFEXT_O_SecondaryJoystickGrabbed");
        }
        public void JoystickDropped()
        {
            HoldingJoyStick = false;

            if (Grounded)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(DisableJoystick));
            }
            EntityControl.SendEventToExtensions("SFEXT_O_SecondaryJoystickDropped");
        }
        public void EnableJoystick()
        {
            JoyOutTarget = 1;
            AirControlReady = true;
            if (!JoyMoving)
            {
                JoyMoving = true;
                MoveJoy();
            }
            if (!SGVControlsDisabled)
            {
                SGVControl.SetProgramVariable("DisableInput", (int)SGVControl.GetProgramVariable("DisableInput") + 1);
                VehicleAnimator.SetBool(AIRCONTROL_STRING, true);
                SGVControlsDisabled = true;
            }
        }
        public void MoveJoy()
        {
            if (JoyMoving)
            {
                JoyOut = Mathf.MoveTowards(JoyOut, JoyOutTarget, 2 * Time.deltaTime);
                VehicleAnimator.SetFloat(JOYOUT_STRING, JoyOut);
                if (JoyOut == 1f || JoyOut == 0f)
                {
                    JoyMoving = false;
                }
                else
                {
                    SendCustomEventDelayedFrames(nameof(MoveJoy), 1);
                }
            }
        }
        private void ResetAnim()
        {
            VehicleAnimator.SetBool(AIRCONTROL_STRING, false);
            VehicleAnimator.SetFloat(PITCH_STRING, .5f);
            VehicleAnimator.SetFloat(YAW_STRING, .5f);
            VehicleAnimator.SetFloat(ROLL_STRING, .5f);
        }
    }
}