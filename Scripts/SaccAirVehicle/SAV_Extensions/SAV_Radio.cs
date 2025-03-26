
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
        [System.NonSerialized] public SaccEntity EntityControl;
        [Tooltip("Force this radio to always be on this channel (-1 to disable)")]
        [SerializeField] int ForceChannel = -1;
        [Tooltip("Leave empty to use all seats")]
        public SaccVehicleSeat[] RadioSeats;
        bool ForceChannel_b = false;
        byte myPrevChannel;
        bool ForceChannel_swapped = false;
        bool DFUNCMODE = false;
        [System.NonSerialized] public SaccRadioBase RadioBase;
        [System.NonSerialized] public bool PTT_MODE; // set true if DFUNC_RadioPTT is in use
        [System.NonSerialized] public DFUNC_RadioPTT PTTControl; // set true if DFUNC_RadioPTT is in use
        // public bool RadioOn = true;
        [Header("Debug")]
        [UdonSynced, FieldChangeCallback(nameof(Channel))] public byte _Channel;
        public byte Channel
        {
            set
            {
                _Channel = value;
                if (ImOnRadio)
                {
                    UpdateChannel();
                    RadioBase.SetAllVoiceVolumesDefault();
                }
                RadioBase.SetVehicleVolumeDefault(this);
                RadioBase.UpdateVehicle(this);
                if (inVehicle)
                    UpdateChannelText();
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
            InVR = localPlayer.IsUserInVR();
            gameObject.SetActive(true);
            if (ForceChannel > -1)
            {
                if (ForceChannel > 255) ForceChannel = 255;
                ForceChannel_b = true;
            }
            if (RadioSeats.Length == 0)
            {
                RadioSeats = EntityControl.VehicleSeats;
            }
            if (DialPosition != -999) { DFUNCMODE = true; }
        }
        public void SFEXT_L_EntityStart()
        {
            if (!Initialized)
            { Init(); }
        }
        public void SFEXT_O_PilotEnter()
        {
            Piloting = true;
            InVR = EntityControl.InVR;
            EnterVehicle();
        }
        public void SFEXT_G_PilotEnter()
        {
            if (ImOnRadio) { SendCustomEventDelayedSeconds(nameof(UpdateChannel), 2); }
        }
        public void UpdateChannel()
        {
            if (!(PassengerFunctionsControl ? PassengerFunctionsControl.Using : EntityControl.Using))
            {
                ChannelSwapped = true;
                RadioBase.SetProgramVariable("CurrentChannel", Channel);
            }
        }
        bool Piloting;
        public void SFEXT_O_PilotExit()
        {
            Piloting = false;
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
        bool ImOnRadio = false;
        public void EnterVehicle()
        {
            inVehicle = true;
            for (int i = 0; i < RadioSeats.Length; i++)
            {
                if (RadioSeats[i].SeatedPlayer == localPlayer)
                {
                    ImOnRadio = true;
                    break;
                }
            }
            UpdateChannelText();
            if (!ImOnRadio) { return; }
            if (RadioBase)
            {
                RadioBase.SetProgramVariable("MyRadioSetTimes", (int)RadioBase.GetProgramVariable("MyRadioSetTimes") + 1);
                RadioBase.SetProgramVariable("MyRadio", this);
                //if not pilot, set my channel on radiobase to vehicle's and set back on exit
                if (ForceChannel_b && (PassengerFunctionsControl ? PassengerFunctionsControl.Using : EntityControl.Using))
                {
                    myPrevChannel = RadioBase.MyChannel;
                    ForceChannel_swapped = true;
                    RadioBase.SetChannel(ForceChannel);
                }
                else { NewChannel(); }
                UpdateChannel();
            }
            if (DFUNCMODE && !controlsRunning && Piloting)
            {
                controlsRunning = true;
                Controls();
            }
        }
        public void NewChannel()
        {
            if (PassengerFunctionsControl ? PassengerFunctionsControl.Using : EntityControl.Using)
            {
                // PTT_MODE requires DFUNC_RadioPTT
                // PTT_MODE has your radiobase channel set to what is selected as normal so that you hear that channel
                // but sets your synced radio channel to +200 when you're not talking so that others see you as not on their channel and don't hear you
                // it sets it back -200 to the 'real' value while you are holding PTT so that everyone in the channel can hear you
                byte newChannel = (byte)RadioBase.GetProgramVariable("MyChannel");
                if (PTT_MODE && newChannel != 0 && !PTTControl.PTT_ACTIVE) newChannel += 200;
                Channel = newChannel;
                RequestSerialization();
            }
        }
        public void ExitVehicle()
        {
            inVehicle = false;
            if (!ImOnRadio) return;
            ImOnRadio = controlsRunning = false;
            if (RadioBase)
            {
                if (ChannelSwapped)
                {
                    ChannelSwapped = false;
                    RadioBase.SetProgramVariable("CurrentChannel", (byte)RadioBase.GetProgramVariable("MyChannel"));
                }
                if (ForceChannel_swapped)
                {
                    ForceChannel_swapped = false;
                    RadioBase.SetChannel(myPrevChannel);
                }
                int mrst = (int)RadioBase.GetProgramVariable("MyRadioSetTimes") - 1;
                RadioBase.SetProgramVariable("MyRadioSetTimes", mrst);
                if (mrst == 0)
                {
                    RadioBase.SetAllVoiceVolumesDefault();
                    RadioBase.SetProgramVariable("MyRadio", null);
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
            CurrentOwnerID = Networking.GetOwner(gameObject).playerId;
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
            VRCPlayerApi ownerAPI = Networking.GetOwner(gameObject);
            if (ownerAPI == null) { return; }
            if (CurrentOwnerID == ownerAPI.playerId)
            {
                //reset current owner's voice volume
                RadioBase.SetSingleVoiceVolumeDefault(ownerAPI);
            }
        }







        //
        // DFUNC STUFF:
        //

        [Header("Optional, DFUNC Mode:")]
        [SerializeField] private KeyCode ChannelUpKey = KeyCode.RightBracket;
        [SerializeField] private KeyCode ChannelDownKey = KeyCode.LeftBracket;
        [SerializeField] private TextMeshProUGUI ChannelNumber_UGUI;
        [SerializeField] private TextMeshPro ChannelNumber;
        [SerializeField] private Transform ControlsRoot;
        private bool Selected;
        private bool TriggerLastFrame;
        private Quaternion VehicleRotLastFrame, JoystickZeroPoint;
        private float JoyStickValue;
        private Vector3 CompareAngleLastFrame;
        private int ChannelOnGrab, CurChannel;
        bool InVR;
        bool controlsRunning;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SAV_PassengerFunctionsController PassengerFunctionsControl;
        public void Controls()
        {
            if (!controlsRunning) return;
            SendCustomEventDelayedFrames(nameof(Controls), 1);
            if (!InVR)
            {
                if (Input.GetKeyDown(ChannelUpKey))
                {
                    RadioBase.IncreaseChannel();
                }
                if (Input.GetKeyDown(ChannelDownKey))
                {
                    RadioBase.DecreaseChannel();
                }
            }
            else if (Selected)
            {
                float Trigger;
                if (LeftDial)
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
                        if (LeftDial)
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
                        (LeftDial ? localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation
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
                        if (LeftDial)
                        { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .05f, .222f, 35); }
                        else
                        { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .222f, 35); }
                        RadioBase.SetChannel(NewChannel);
                        CurChannel = NewChannel;
                    }
                }
                else { TriggerLastFrame = false; }
            }
        }
        private void UpdateChannelText()
        {
            string channeltxt;
            int unPTT_Channel = Channel;
            if (unPTT_Channel >= 200)
                unPTT_Channel -= 200;
            if (unPTT_Channel == 0) channeltxt = "OFF";
            else channeltxt = unPTT_Channel.ToString();

            if (ChannelNumber_UGUI) { ChannelNumber_UGUI.text = channeltxt; }
            if (ChannelNumber) { ChannelNumber.text = channeltxt; }
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