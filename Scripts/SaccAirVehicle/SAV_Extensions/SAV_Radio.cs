
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using SaccFlightAndVehicles;
using VRC.Udon;
using TMPro;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SAV_Radio : UdonSharpBehaviour
    {
        public SaccEntity EntityControl;
        [System.NonSerialized] public SaccRadioBase RadioBase;
        // public bool RadioOn = true;
        [Header("Debug:")]
        [UdonSynced, FieldChangeCallback(nameof(Channel))] private byte _Channel;
        public byte Channel
        {
            set
            {
                _Channel = value;
                if (EntityControl.InVehicle)
                {
                    UpdateChannel();
                    RadioBase.SetAllVoiceVolumesDefault();
                }
                RadioBase.SetVehicleVolumeDefault(EntityControl);
            }
            get => _Channel;
        }
        private bool Initialized;
        private VRCPlayerApi localPlayer;
        private int CurrentOwnerID = -1;
        private bool ChannelSwapped;
        bool inVehicle;
        public void Init()
        {
            Initialized = true;
            localPlayer = Networking.LocalPlayer;
            VRCPlayerApi ownerAPI = Networking.GetOwner(EntityControl.gameObject);
            if (Utilities.IsValid(ownerAPI))
            {
                CurrentOwnerID = ownerAPI.playerId;
            }
            if (!ControlsRoot)
            {
                ControlsRoot = EntityControl.transform;
            }
            inVR = localPlayer.IsUserInVR();
            gameObject.SetActive(true);
            bool RadioOn = _Channel != 0;
            for (int i = 0; i < Dial_Funcon.Length; i++)
            {
                Dial_Funcon[i].SetActive(RadioOn);
            }
        }
        public void SFEXT_L_EntityStart()
        {
            if (!Initialized)
            { Init(); }
        }
        public void SFEXT_O_PilotEnter()
        {
            EnterVehicle();
        }
        public void SFEXT_G_PilotEnter()
        {
            if (EntityControl.Passenger) { SendCustomEventDelayedSeconds(nameof(UpdateChannel), 2); }
        }
        public void UpdateChannel()
        {
            if (!EntityControl.Using)
            {
                ChannelSwapped = true;
                RadioBase.SetProgramVariable("CurrentChannel", Channel);
            }
        }
        public void SFEXT_O_PilotExit()
        {
            ExitVehicle();
        }
        public void SFEXT_P_PassengerEnter()
        {
            EnterVehicle();
        }
        public void SFEXT_P_PassengerExit()
        {
            ExitVehicle();
        }
        public void EnterVehicle()
        {
            if (RadioBase)
            {
                RadioBase.SetProgramVariable("MyVehicleSetTimes", (int)RadioBase.GetProgramVariable("MyVehicleSetTimes") + 1);
                RadioBase.SetProgramVariable("MyVehicle", EntityControl);
                //if not pilot, set my channel on radiobase to vehicle's and set back on exit
                NewChannel();
                UpdateChannel();
            }
            UpdateChannelText();
            controlsRunning = true;
            Controls();
        }
        public void NewChannel()
        {
            if (EntityControl.Using)
            {
                Channel = (byte)RadioBase.GetProgramVariable("MyChannel");
                RequestSerialization();
            }
        }
        public void ExitVehicle()
        {
            controlsRunning = false;
            if (RadioBase)
            {
                if (ChannelSwapped)
                {
                    ChannelSwapped = false;
                    RadioBase.SetProgramVariable("CurrentChannel", (byte)RadioBase.GetProgramVariable("MyChannel"));
                }
                int mvst = (int)RadioBase.GetProgramVariable("MyVehicleSetTimes") - 1;
                RadioBase.SetProgramVariable("MyVehicleSetTimes", mvst);
                if (mvst == 0)
                {
                    RadioBase.SetProgramVariable("MyVehicle", null);
                    RadioBase.SendCustomEvent("SetAllVoiceVolumesDefault");
                }
            }
        }
        public void SFEXT_L_OwnershipTransfer()
        {
            //reset current owner's voice volume
            VRCPlayerApi ownerAPI = VRCPlayerApi.GetPlayerById(CurrentOwnerID);
            if (Utilities.IsValid(ownerAPI))
            {
                RadioBase.SetSingleVoiceVolumeDefault(ownerAPI);
            }
            CurrentOwnerID = Networking.GetOwner(EntityControl.gameObject).playerId;
        }
        public void SFEXT_O_OnPickup()
        {
            SFEXT_O_PilotEnter();
        }
        public void SFEXT_O_OnDrop()
        {
            SFEXT_O_PilotExit();
        }
        public void SFEXT_G_OnDrop()
        {
            VRCPlayerApi ownerAPI = Networking.GetOwner(EntityControl.gameObject);
            if (ownerAPI == null) { return; }
            if (CurrentOwnerID == ownerAPI.playerId)
            {
                //reset current owner's voice volume
                RadioBase.SetSingleVoiceVolumeDefault(ownerAPI);
            }
        }










        [SerializeField] private KeyCode ChannelUpKey = KeyCode.RightBracket;
        [SerializeField] private KeyCode ChannelDownKey = KeyCode.LeftBracket;
        [SerializeField] private TextMeshProUGUI ChannelNumber;
        [SerializeField] private Transform ControlsRoot;
        private bool Selected, UseLeftTrigger;
        [Tooltip("Objects enabled when function is active (used on MFD)")]
        public GameObject[] Dial_Funcon;
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        private bool TriggerLastFrame;
        private Quaternion VehicleRotLastFrame, JoystickZeroPoint;
        private float JoyStickValue;
        private Vector3 CompareAngleLastFrame;
        private int ChannelOnGrab, CurChannel;
        bool inVR;
        bool controlsRunning;
        public void Controls()
        {
            if (!controlsRunning) return;
            SendCustomEventDelayedFrames(nameof(Controls), 1);
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
                        TriggerLastFrame = true;
                        VehicleRotDif = Quaternion.identity;
                        if (UseLeftTrigger)
                        { JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation; }//rotation of the controller relative to the vehicle when it was pressed
                        else
                        { JoystickZeroPoint = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation; }
                        JoyStickValue = 0;
                        CompareAngleLastFrame = Vector3.up;
                        ChannelOnGrab = CurChannel = RadioBase.MyChannel;
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
            if (ChannelNumber)
            { ChannelNumber.text = RadioBase.ChannelText.text; }
            bool RadioOn = _Channel != 0;
            for (int i = 0; i < Dial_Funcon.Length; i++)
            {
                Dial_Funcon[i].SetActive(RadioOn);
            }
        }
        public void DFUNC_Selected()
        {
            TriggerLastFrame = true;
            Selected = true;
        }
        public void DFUNC_Deselected()
        {
            Selected = false;
        }
    }
}