
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using UnityEngine.UI;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_RadioChannel : UdonSharpBehaviour
    {
        [SerializeField] private SaccRadioBase RadioBase;
        [SerializeField] private KeyCode ChannelUpKey = KeyCode.RightBracket;
        [SerializeField] private KeyCode ChannelDownKey = KeyCode.LeftBracket;
        [SerializeField] private TextMeshProUGUI ChannelNumber_TMP;
        [SerializeField] private Text ChannelNumber_text;
        [SerializeField] private Transform ControlsRoot;
        [System.NonSerialized] public SaccEntity EntityControl;
        private bool Selected, UseLeftTrigger;
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        private bool TriggerLastFrame;
        private Quaternion VehicleRotLastFrame, JoystickZeroPoint;
        private VRCPlayerApi localPlayer;
        private float JoyStickValue;
        private Vector3 CompareAngleLastFrame;
        private int ChannelOnGrab, CurChannel;
        bool inVR;
        public void SFEXT_L_EntityStart()
        {
            localPlayer = Networking.LocalPlayer;
            if (!ControlsRoot)
            {
                ControlsRoot = EntityControl.transform;
            }
            inVR = localPlayer.IsUserInVR();
        }
        private void Update()
        {
            if (!inVR)
            {
                if (Input.GetKeyDown(ChannelUpKey))
                {
                    RadioBase.IncreaseChannel();
                    UpdateChannelText();
                }
                if (Input.GetKeyDown(ChannelDownKey))
                {
                    RadioBase.DecreaseChannel();
                    UpdateChannelText();
                }
            }
            else if (Selected)
            {
                float Trigger;
                if (UseLeftTrigger)
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                else
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
                if (Trigger > .75f)
                {
                    //copy of code from SaccSeaVehicle's steering wheel
                    Quaternion VehicleRotDif = ControlsRoot.rotation * Quaternion.Inverse(VehicleRotLastFrame);//difference in vehicle's rotation since last frame
                    VehicleRotLastFrame = ControlsRoot.rotation;
                    JoystickZeroPoint = VehicleRotDif * JoystickZeroPoint;//zero point rotates with the vehicle so it appears still to the pilot
                    if (!TriggerLastFrame)//first frame you gripped joystick
                    {
                        VehicleRotDif = Quaternion.identity;
                        if (UseLeftTrigger)
                        { JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation; }//rotation of the controller relative to the vehicle when it was pressed
                        else
                        { JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation; }
                        JoyStickValue = 0;
                        CompareAngleLastFrame = Vector3.up;
                        ChannelOnGrab = CurChannel = RadioBase.MyChannel;
                        TriggerLastFrame = true;
                    }
                    //difference between the vehicle and the hand's rotation, and then the difference between that and the JoystickZeroPoint
                    Quaternion JoystickDifference;
                    JoystickDifference = Quaternion.Inverse(ControlsRoot.rotation) *
                        (UseLeftTrigger ? localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation
                                                : localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation)
                    * Quaternion.Inverse(JoystickZeroPoint)
                     * ControlsRoot.rotation;

                    Vector3 JoystickPosYaw = (JoystickDifference * Vector3.up);
                    Vector3 CompareAngle = Vector3.ProjectOnPlane(JoystickPosYaw, Vector3.forward);
                    JoyStickValue -= (Vector3.SignedAngle(CompareAngleLastFrame, CompareAngle, Vector3.forward));
                    CompareAngleLastFrame = CompareAngle;
                    int channelIncreaseAmount = (int)Mathf.Clamp(JoyStickValue / 15f, int.MinValue, int.MaxValue);
                    int NewChannel = ChannelOnGrab + channelIncreaseAmount;
                    if (CurChannel != NewChannel)
                    {
                        if (UseLeftTrigger)
                        { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .05f, .222f, 35); }
                        else
                        { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .222f, 35); }
                        RadioBase.SetChannel(NewChannel);
                        UpdateChannelText();
                        CurChannel = NewChannel;
                    }
                }
                else { TriggerLastFrame = false; }
            }
        }
        private void UpdateChannelText()
        {
            if (ChannelNumber_text)
            { ChannelNumber_text.text = RadioBase.MyChannel.ToString(); }
            if (ChannelNumber_TMP)
            { ChannelNumber_TMP.text = RadioBase.MyChannel.ToString(); }
        }
        public void DFUNC_Selected()
        {
            TriggerLastFrame = true;
            Selected = true;
            gameObject.SetActive(true);
        }
        public void DFUNC_Deselected()
        {
            Selected = false;
            gameObject.SetActive(false);
        }
        public void SFEXT_O_PilotEnter()
        {
            if (!inVR)
            { gameObject.SetActive(true); }
            UpdateChannelText();
        }
        public void SFEXT_O_PilotExit()
        {
            if (!inVR)
            { gameObject.SetActive(false); }
        }
        public void KeyboardInput()
        {
            RadioBase.IncreaseChannel();
            UpdateChannelText();
        }
    }
}