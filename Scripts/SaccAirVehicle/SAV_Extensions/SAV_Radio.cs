
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using TMPro;
using VRC.SDK3.UdonNetworkCalling;

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
        public GameObject[] Dial_Funcons;
        [System.NonSerialized] public SaccRadioBase RadioBase;
        [System.NonSerialized] public bool PTT_MODE; // set true if DFUNC_RadioPTT is in use
        [System.NonSerialized] public bool PTT_ACTIVE;
        // public bool RadioOn = true;
        [Header("Debug")]
        [UdonSynced, FieldChangeCallback(nameof(Channel))] public byte _Channel;
        public byte Channel
        {
            set
            {
                byte lastChannel = _Channel;
                _Channel = value;
                if (ImOnRadio)
                {
                    UpdateChannel();
                    if ((Mathf.Abs(value - lastChannel) < 50) || value == 0) // don't reset volumes if enabling/disabling PTT, reset if switching channel
                    {
                        if (lastChannel != Channel_ListenOnly) { RadioBase.SetAllVoiceVolumesDefault(lastChannel); }
                        RadioBase.SetAllVoiceVolumesDefault(lastChannel);
                    }
                }
                RadioBase.SetVehicleVolumeDefault(this);
                RadioBase.UpdateVehicle(this);
                if (inVehicle)
                    UpdateChannelText();
            }
            get => _Channel;
        }
        [System.NonSerialized] public bool Initialized;
        private VRCPlayerApi localPlayer;
        private int CurrentOwnerID = -1;
        private bool ChannelSwapped_ListenOnly;
        private bool ChannelSwapped;
        bool inVehicle;
        public void Init()
        {
            Initialized = true;
            localPlayer = Networking.LocalPlayer;
            VRCPlayerApi ownerAPI = EntityControl.OwnerAPI;
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

            for (int i = 0; i < Dial_Funcons.Length; i++) { Dial_Funcons[i].SetActive(false); }
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
            if (!localPlayer.IsOwner(gameObject))
            {
                ChannelSwapped = true;
                RadioBase.SetProgramVariable("CurrentChannel", Channel);
                if (UseListenOnlyChannel)
                {
                    ChannelSwapped_ListenOnly = true;
                    RadioBase.SetProgramVariable("CurrentChannel_ListenOnly", Channel_ListenOnly);
                }
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
            if (RadioSeats.Length == 0) ImOnRadio = true; // pickups
            else
            {
                for (int i = 0; i < RadioSeats.Length; i++)
                {
                    if (RadioSeats[i].SeatedPlayer == localPlayer)
                    {
                        ImOnRadio = true;
                        break;
                    }
                }
            }
            UpdateChannelText();
            if (!ImOnRadio) { return; }
            if (RadioBase)
            {
                RadioBase.SetProgramVariable("MyRadioSetTimes", (int)RadioBase.GetProgramVariable("MyRadioSetTimes") + 1);
                RadioBase.SetProgramVariable("MyRadio", this);
                //if not pilot, set my channel on radiobase to vehicle's and set back on exit
                if (ForceChannel_b && (EntityControl.VehicleSeats[EntityControl.MySeat].PassengerFunctions ? EntityControl.VehicleSeats[EntityControl.MySeat].PassengerFunctions.Using : EntityControl.Using))
                {
                    myPrevChannel = RadioBase.MyChannel;
                    ForceChannel_swapped = true;
                    RadioBase.SetChannel(ForceChannel);
                }
                else { NewChannel(); NewChannel_ListenOnly(); }
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
            if (localPlayer.IsOwner(gameObject))
            {
                // PTT_MODE requires DFUNC_RadioPTT
                // PTT_MODE has your radiobase channel set to what is selected as normal so that you hear that channel
                // but sets your synced radio channel to +200 when you're not talking so that others see you as not on their channel and don't hear you
                // it sets it back -200 to the 'real' value while you are holding PTT so that everyone in the channel can hear you
                byte newChannel = (byte)RadioBase.GetProgramVariable("MyChannel");
                if (PTT_MODE && newChannel != 0 && !PTT_ACTIVE) newChannel += 200;
                if (newChannel != Channel) Channel = newChannel;
                RequestSerialization();
            }
        }
        public void NewChannel_ListenOnly()
        {
            if (!UseListenOnlyChannel) return;
            if (localPlayer.IsOwner(gameObject))
            {
                byte newChannel_ListenOnly = (byte)RadioBase.GetProgramVariable("MyChannel_ListenOnly");
                Channel_ListenOnly = newChannel_ListenOnly;
                RequestSerialization();
            }
        }
        public void ExitVehicle()
        {
            inVehicle = controlsRunning = false;
            if (!ImOnRadio) return;
            ImOnRadio = false;
            if (RadioBase)
            {
                if (ChannelSwapped)
                {
                    ChannelSwapped = false;
                    RadioBase.SetProgramVariable("CurrentChannel", (byte)RadioBase.GetProgramVariable("MyChannel"));
                }
                if (ChannelSwapped_ListenOnly)
                {
                    ChannelSwapped_ListenOnly = false;
                    RadioBase.SetProgramVariable("CurrentChannel_ListenOnly", (byte)RadioBase.GetProgramVariable("MyChannel_ListenOnly"));
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
                    RadioBase.SetAllVoiceVolumesDefault(Channel);
                    if (UseListenOnlyChannel) { RadioBase.SetAllVoiceVolumesDefault(Channel_ListenOnly); }
                    RadioBase.SetProgramVariable("MyRadio", null);
                }
            }
        }
        public void PTT_ON()
        {
            if (!PTT_ACTIVE)
            {
                PTT_ACTIVE = true;
                if (Channel >= 200)
                { Channel -= 200; }
                NewChannel();
            }
        }
        void PTT_OFF()
        {
            if (PTT_ACTIVE)
            {
                PTT_ACTIVE = false;
                if (Channel <= 55)
                { Channel += 200; }
                NewChannel();
            }
        }
        [NetworkCallable]
        public void Call_PTT_ON()
        {
            if (localPlayer.IsOwner(gameObject))
                PTT_ON();
        }
        [NetworkCallable]
        public void Call_PTT_OFF()
        {
            if (localPlayer.IsOwner(gameObject))
                PTT_OFF();
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







        //
        // DFUNC STUFF:
        //

        [Header("Optional, DFUNC Mode:")]
        [SerializeField] private KeyCode ChannelUpKey = KeyCode.RightBracket;
        [SerializeField] private KeyCode ChannelDownKey = KeyCode.LeftBracket;
        [SerializeField] private KeyCode ChannelUpKey_Listen = KeyCode.Backslash;
        [SerializeField] private KeyCode ChannelDownKey_Listen = KeyCode.Quote;
        [SerializeField] private TextMeshProUGUI ChannelNumber_UGUI;
        [SerializeField] private TextMeshPro ChannelNumber;
        [SerializeField] private TextMeshProUGUI ChannelNumber_UGUI_ListenOnly;
        [SerializeField] private TextMeshPro ChannelNumber_ListenOnly;
        [SerializeField] private Transform ControlsRoot;
        private bool Selected;
        private bool TriggerLastFrame;
        private Quaternion VehicleRotLastFrame, JoystickZeroPoint;
        private float JoyStickValueRoll;
        private float JoyStickValueYaw;
        private Vector3 CompareAngleLastFrameRoll;
        private Vector3 CompareAngleLastFrameYaw;
        private int ChannelOnGrab, CurChannel, ChannelOnGrab_Listen, CurChannel_Listen;
        bool InVR;
        bool controlsRunning;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
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
                if (UseListenOnlyChannel)
                {
                    if (Input.GetKeyDown(ChannelUpKey_Listen))
                    {
                        RadioBase.IncreaseChannel_ListenOnly();
                    }
                    if (Input.GetKeyDown(ChannelDownKey_Listen))
                    {
                        RadioBase.DecreaseChannel_ListenOnly();
                    }
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
                        JoyStickValueRoll = 0;
                        JoyStickValueYaw = 0;
                        CompareAngleLastFrameRoll = Vector3.up;
                        ChannelOnGrab = CurChannel = RadioBase.MyChannel;
                        CompareAngleLastFrameYaw = Vector3.forward;
                        ChannelOnGrab_Listen = CurChannel_Listen = RadioBase.MyChannel_ListenOnly;
                    }
                    //difference between the vehicle and the hand's rotation, and then the difference between that and the JoystickZeroPoint
                    Quaternion JoystickDifference;
                    JoystickDifference = Quaternion.Inverse(ControlsRoot.rotation) *
                        (LeftDial ? localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation
                                                : localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation)
                    * Quaternion.Inverse(JoystickZeroPoint)
                     * ControlsRoot.rotation;

                    //Grab and roll to change Channel
                    Vector3 JoystickPosRoll = (JoystickDifference * Vector3.up);
                    Vector3 CompareAngleRoll = Vector3.ProjectOnPlane(JoystickPosRoll, Vector3.forward);
                    JoyStickValueRoll -= (Vector3.SignedAngle(CompareAngleLastFrameRoll, CompareAngleRoll, Vector3.forward));
                    CompareAngleLastFrameRoll = CompareAngleRoll;
                    int channelIncreaseAmount = (int)Mathf.Clamp(JoyStickValueRoll / 15f, int.MinValue, int.MaxValue);
                    int newChannel = ChannelOnGrab + channelIncreaseAmount;
                    if (CurChannel != newChannel)
                    {
                        if (LeftDial)
                        { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .05f, .222f, 35); }
                        else
                        { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .222f, 35); }
                        RadioBase.SetChannel(newChannel);
                        CurChannel = newChannel;
                    }

                    //Grab and yaw to change Channel_ListenOnly
                    if (UseListenOnlyChannel)
                    {
                        Vector3 JoystickPosYaw = (JoystickDifference * Vector3.forward);
                        Vector3 CompareAngleYaw = Vector3.ProjectOnPlane(JoystickPosYaw, Vector3.up);
                        JoyStickValueYaw += (Vector3.SignedAngle(CompareAngleLastFrameYaw, CompareAngleYaw, Vector3.up));
                        CompareAngleLastFrameYaw = CompareAngleYaw;
                        int channelIncreaseAmount_Listen = (int)Mathf.Clamp(JoyStickValueYaw / 15f, int.MinValue, int.MaxValue);
                        int newChannel_Listen = ChannelOnGrab_Listen + channelIncreaseAmount_Listen;
                        if (CurChannel_Listen != newChannel_Listen)
                        {
                            if (LeftDial)
                            { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, .05f, .222f, 35); }
                            else
                            { localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, .05f, .222f, 35); }
                            RadioBase.SetChannel_ListenOnly(newChannel_Listen);
                            CurChannel_Listen = newChannel_Listen;
                        }
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
            channeltxt = unPTT_Channel == 0 ? "X" : unPTT_Channel.ToString();
            if (ChannelNumber_UGUI) { ChannelNumber_UGUI.text = channeltxt; }
            if (ChannelNumber) { ChannelNumber.text = channeltxt; }
            if (UseListenOnlyChannel)
            {
                string channeltxt_ListenOnly = Channel_ListenOnly == 0 ? "X" : Channel_ListenOnly.ToString();
                if (ChannelNumber_UGUI_ListenOnly) { ChannelNumber_UGUI_ListenOnly.text = channeltxt_ListenOnly; }
                if (ChannelNumber_ListenOnly) { ChannelNumber_ListenOnly.text = channeltxt_ListenOnly; }
            }
            bool PTT_Active = Channel < 55 && Channel != 0;
            for (int i = 0; i < Dial_Funcons.Length; i++) { Dial_Funcons[i].SetActive(PTT_Active); }
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




        /////////////////////////// LISTEN ONLY
        [Header("Listen Only channel: You must uncomment 'UdonSynced' to\nsync the channel if you want it to be changeable")]
        [Tooltip("If ticked, radios will be able to use both a talking channel and the listen only channel")]
        public bool UseListenOnlyChannel;
        //UNCOMMENT UdonSynced and tick UseListenOnlyChannel to use Listen Only channel and sync its value
        //I did it this way because most worlds will never want to use this and I don't want to waste network data
        [/* UdonSynced, */ FieldChangeCallback(nameof(Channel_ListenOnly))] public byte _Channel_ListenOnly = 0;
        public byte Channel_ListenOnly
        {
            set
            {
                byte lastChannel = _Channel_ListenOnly;
                _Channel_ListenOnly = value;
                if (ImOnRadio)
                {
                    UpdateChannel();
                    if (lastChannel != Channel) { RadioBase.SetAllVoiceVolumesDefault(lastChannel); }
                }
                if (inVehicle)
                    UpdateChannelText();
            }
            get => _Channel_ListenOnly;
        }
    }
}