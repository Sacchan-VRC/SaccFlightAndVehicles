
using System;
using SaccFlightAndVehicles;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DFUNC_TakeControl : UdonSharpBehaviour
    {
        private SaccVehicleSeat[] VehicleSeats;
        public SaccVehicleSeat ThisSVSeat;
        [Tooltip("Require the user to hold down the button to take control?")]
        [SerializeField] private bool HoldToTake = false;
        [Tooltip("These tramsforms will be moved to their corresponding _CoPosition when control is switched")]
        public Transform[] MoveTransforms;
        public Transform[] MoveTransforms_CoPosition;
        public KeyCode TakeControlKey = KeyCode.Space;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [SerializeField] private SAV_Radio PilotRadio;
        int PilotRadio_SeatIndex = -1;
        bool PilotRadio_DoSwap;
        [SerializeField] private SAV_Radio ThisSeatRadio;
        int ThisSeatRadio_SeatIndex = -1;
        bool ThisSeatRadio_DoSwap = false;
        private SaccVehicleSeat PilotSVSeat;
        private bool TriggerLastFrame;
        private bool IsUser;
        private bool Swapped;
        private GameObject PilotThisSeatOnly;
        private GameObject ThisThisSeatOnly;
        private SAV_PassengerFunctionsController PilotPassengerFunctions;
        private SAV_PassengerFunctionsController ThisPassengerFunctions;
        private GameObject[] PilotEnableInSeat;
        private GameObject[] ThisEnableInSeat;
        private VRCPlayerApi SeatAPI;
        private VRCPlayerApi PilotSeatAPI;
        private SaccAirVehicle SAVControl;
        private Vector3[] MoveTransformsPos_Orig;
        private Quaternion[] MoveTransformsRot_Orig;
        public void SFEXT_L_EntityStart()
        {
            VehicleSeats = EntityControl.VehicleSeats;
            PilotSVSeat = EntityControl.VehicleStations[EntityControl.PilotSeat].GetComponent<SaccVehicleSeat>();
            PilotEnableInSeat = PilotSVSeat.EnableInSeat;
            ThisEnableInSeat = ThisSVSeat.EnableInSeat;
            PilotPassengerFunctions = PilotSVSeat.PassengerFunctions;
            ThisPassengerFunctions = ThisSVSeat.PassengerFunctions;
            SAVControl = (SaccAirVehicle)EntityControl.GetExtention(GetUdonTypeName<SaccAirVehicle>());
            int mtlen = MoveTransforms.Length;
            MoveTransformsPos_Orig = new Vector3[mtlen];
            MoveTransformsRot_Orig = new Quaternion[mtlen];
            for (int i = 0; i < mtlen; i++)
            {
                if (MoveTransforms[i])
                {
                    MoveTransformsPos_Orig[i] = MoveTransforms[i].localPosition;
                    MoveTransformsRot_Orig[i] = MoveTransforms[i].localRotation;
                }
            }
            bool thisSVSeatFound = false;
            bool PilotSVSeatFound = false;
            if (PilotRadio && PilotRadio.RadioSeats.Length > 0)
            {
                for (int i = 0; i < PilotRadio.RadioSeats.Length; i++)
                {
                    if (PilotRadio.RadioSeats[i] == PilotSVSeat)
                    {
                        PilotRadio_SeatIndex = i;
                        PilotSVSeatFound = true;
                    }
                    if (PilotRadio.RadioSeats[i] == ThisSVSeat)
                    { thisSVSeatFound = true; }
                    if (thisSVSeatFound && PilotSVSeatFound) { break; }
                }
                if (!thisSVSeatFound) PilotRadio_DoSwap = true;
            }

            thisSVSeatFound = false;
            PilotSVSeatFound = false;
            if (ThisSeatRadio && ThisSeatRadio.RadioSeats.Length > 0)
            {
                for (int i = 0; i < ThisSeatRadio.RadioSeats.Length; i++)
                {
                    if (ThisSeatRadio.RadioSeats[i] == ThisSVSeat)
                    {
                        ThisSeatRadio_SeatIndex = i;
                        thisSVSeatFound = true;
                    }
                    if (ThisSeatRadio.RadioSeats[i] == PilotSVSeat)
                    { PilotSVSeatFound = true; }
                    if (thisSVSeatFound && PilotSVSeatFound) { break; }
                }
                if (!PilotSVSeatFound) ThisSeatRadio_DoSwap = true;
            }
        }
        public void TakeControl()
        {
            if (gameObject.activeInHierarchy || !SAVControl.Occupied)
            {
                if (!Swapped)
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Swap_Event)); }
                else
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(UnSwap_Event)); }
            }
        }
        public void Swap_Event()
        {
            bool autoEngineEnter = true;
            bool autoEngineExit = true;
            bool DoAutoEngine = false;
            if (SAVControl && SAVControl.Occupied)
            {
                DoAutoEngine = true;
                autoEngineEnter = SAVControl.EngineOnOnEnter;
                autoEngineExit = SAVControl.EngineOffOnExit;
                SAVControl.EngineOnOnEnter = false;
                SAVControl.EngineOffOnExit = false;
            }
            SeatAPI = ThisSVSeat.SeatedPlayer;
            PilotSeatAPI = PilotSVSeat.SeatedPlayer;
            ThisSVSeat.Fake = true;
            PilotSVSeat.Fake = true;
            ThisSVSeat.OnStationExited(SeatAPI);
            PilotSVSeat.OnStationExited(PilotSeatAPI);

            PilotSVSeat.EnableInSeat = ThisEnableInSeat;
            ThisSVSeat.EnableInSeat = PilotEnableInSeat;
            PilotSVSeat.PassengerFunctions = ThisPassengerFunctions;
            ThisSVSeat.PassengerFunctions = PilotPassengerFunctions;
            PilotSVSeat.IsPilotSeat = false;
            ThisSVSeat.IsPilotSeat = true;

            if (PilotRadio_DoSwap)
            { PilotRadio.RadioSeats[PilotRadio_SeatIndex] = ThisSVSeat; }
            if (ThisSeatRadio_DoSwap)
            { ThisSeatRadio.RadioSeats[ThisSeatRadio_SeatIndex] = PilotSVSeat; }

            ThisSVSeat.OnStationEntered(SeatAPI);
            PilotSVSeat.OnStationEntered(PilotSeatAPI);
            ThisSVSeat.Fake = false;
            PilotSVSeat.Fake = false;
            if (DoAutoEngine)
            {
                SAVControl.EngineOnOnEnter = autoEngineEnter;
                SAVControl.EngineOffOnExit = autoEngineExit;
            }
            for (int i = 0; i < MoveTransforms.Length; i++)
            {
                if (MoveTransforms[i])
                {
                    MoveTransforms[i].position = MoveTransforms_CoPosition[i].position;
                    MoveTransforms[i].rotation = MoveTransforms_CoPosition[i].rotation;
                }
            }
            Swapped = true;
        }
        public void UnSwap_Event()
        {
            bool autoEngineEnter = true;
            bool autoEngineExit = true;
            bool DoAutoEngine = false;
            if (SAVControl && SAVControl.Occupied)
            {
                DoAutoEngine = true;
                autoEngineEnter = SAVControl.EngineOnOnEnter;
                autoEngineExit = SAVControl.EngineOffOnExit;
                SAVControl.EngineOnOnEnter = false;
                SAVControl.EngineOffOnExit = false;
            }
            SeatAPI = ThisSVSeat.SeatedPlayer;
            PilotSeatAPI = PilotSVSeat.SeatedPlayer;
            ThisSVSeat.Fake = true;
            PilotSVSeat.Fake = true;
            ThisSVSeat.OnStationExited(SeatAPI);
            PilotSVSeat.OnStationExited(PilotSeatAPI);

            PilotSVSeat.EnableInSeat = PilotEnableInSeat;
            ThisSVSeat.EnableInSeat = ThisEnableInSeat;
            PilotSVSeat.PassengerFunctions = PilotPassengerFunctions;
            ThisSVSeat.PassengerFunctions = ThisPassengerFunctions;
            PilotSVSeat.IsPilotSeat = true;
            ThisSVSeat.IsPilotSeat = false;

            if (PilotRadio_DoSwap)
            { PilotRadio.RadioSeats[PilotRadio_SeatIndex] = PilotSVSeat; }
            if (ThisSeatRadio_DoSwap)
            { ThisSeatRadio.RadioSeats[ThisSeatRadio_SeatIndex] = ThisSVSeat; }

            ThisSVSeat.OnStationEntered(SeatAPI);
            PilotSVSeat.OnStationEntered(PilotSeatAPI);
            ThisSVSeat.Fake = false;
            PilotSVSeat.Fake = false;
            if (DoAutoEngine)
            {
                SAVControl.EngineOnOnEnter = autoEngineEnter;
                SAVControl.EngineOffOnExit = autoEngineExit;
            }
            for (int i = 0; i < MoveTransforms.Length; i++)
            {
                if (MoveTransforms[i])
                {
                    MoveTransforms[i].localPosition = MoveTransformsPos_Orig[i];
                    MoveTransforms[i].localRotation = MoveTransformsRot_Orig[i];
                }
            }
            Swapped = false;
        }
        public void ResetSwap()
        {
            if (Swapped)
            {
                UnSwap_Event();
            }
        }
        public void SFEXT_G_ReAppear()
        {
            ResetSwap();
        }
        public void SFEXT_G_RespawnButton()
        {
            ResetSwap();
        }
        public void SFEXT_O_PilotEnter()
        {
            IsUser = true;
            TriggerPressTime = Time.time + 1000f;// prevent activation if you hold the trigger when getting in
        }
        public void SFEXT_O_PilotExit()
        {
            IsUser = false;
            gameObject.SetActive(false);
        }
        public void DFUNC_Selected()
        {
            TriggerLastFrame = true;
            gameObject.SetActive(true);
        }
        public void DFUNC_Deselected()
        {
            gameObject.SetActive(false);
        }
        private float TriggerPressTime;
        private void Update()
        {
            float Trigger;
            if (LeftDial)
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
            else
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
            if (Trigger > 0.75 || Input.GetKey(TakeControlKey))
            {
                if (!TriggerLastFrame)
                {
                    if (HoldToTake)
                    {
                        TriggerPressTime = Time.time;
                    }
                    else { TakeControl(); }
                    TriggerLastFrame = true;
                }
                if (HoldToTake && Time.time - TriggerPressTime > 1)
                {
                    TakeControl();
                }
            }
            else { TriggerLastFrame = false; }
        }
        public void LateJoinerSwap()
        {
            if (!Swapped)
            {
                Swapped = true;
            }
        }
        public void SFEXT_O_PlayerJoined()
        {
            if (Networking.LocalPlayer.IsOwner(gameObject) && Swapped)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(LateJoinerSwap)); }
        }
        public void KeyboardInput()
        {
            if (EntityControl.VehicleSeats[EntityControl.MySeat].PassengerFunctions)
            {
                EntityControl.VehicleSeats[EntityControl.MySeat].PassengerFunctions.ToggleStickSelection(this);
            }
            else
            {
                EntityControl.ToggleStickSelection(this);
            }
        }
    }
}