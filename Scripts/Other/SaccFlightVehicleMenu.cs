
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SaccFlightVehicleMenu : UdonSharpBehaviour
    {
        public SaccEntity[] Vehicles;
        private SaccAirVehicle[] SaccAirVehicles;
        private SAV_SyncScript[] SAVSyncScripts;
        public Slider JoyStickSensitivitySlider;
        public Text JoyStickSensitivitySliderNumber;
        private void Start()
        {
            if (PassengerComfortModeToggle) { PassengerComfortModeDefault = PassengerComfortModeToggle.isOn; }
            if (SwitchHandsToggle) { SwitchHandsDefault = SwitchHandsToggle.isOn; }
            if (AutoEngineToggle) { AutoEngineDefault = AutoEngineToggle.isOn; }
            SaccAirVehicles = new SaccAirVehicle[Vehicles.Length];
            SAVSyncScripts = new SAV_SyncScript[Vehicles.Length];
            for (int i = 0; i < Vehicles.Length; i++)
            {
                SaccAirVehicles[i] = (SaccAirVehicle)Vehicles[i].GetExtention("SaccAirVehicle");
            }
            for (int i = 0; i < Vehicles.Length; i++)
            {
                SAVSyncScripts[i] = (SAV_SyncScript)Vehicles[i].GetExtention("SAV_SyncScript");
            }
            Reset();
        }
        public void SetJoystickSensitivity()
        {
            float sensvalue = JoyStickSensitivitySlider.value;
            Vector3 sens = new Vector3(sensvalue, sensvalue, sensvalue);
            JoyStickSensitivitySliderNumber.text = sensvalue.ToString("F0");
            foreach (UdonSharpBehaviour SAV in SaccAirVehicles)
            {
                if (SAV)
                { SAV.SetProgramVariable("MaxJoyAngles", sens); }
            }
        }
        public Slider ThrottleSensitivitySlider;
        public Text ThrottleSensitivitySliderNumber;
        public void SetThrottleSensitivity()
        {
            float sensvalue = ThrottleSensitivitySlider.value;
            ThrottleSensitivitySliderNumber.text = sensvalue.ToString("F0");
            foreach (UdonSharpBehaviour SAV in SaccAirVehicles)
            {
                if (SAV)
                { SAV.SetProgramVariable("ThrottleSensitivity", sensvalue); }
            }
        }
        public Slider GripSensitivitySlider;
        public Text GripSensitivitySliderNumber;
        public void SetGripSensitivity()
        {
            float sensvalue = GripSensitivitySlider.value * .01f;
            GripSensitivitySliderNumber.text = (sensvalue).ToString("F2");
            foreach (UdonSharpBehaviour SAV in SaccAirVehicles)
            {
                if (SAV)
                { SAV.SetProgramVariable("GripSensitivity", sensvalue); }
            }
        }
        public Slider DialSensSlider;
        public Text DialSensSliderNumber;
        public void SetDialSensitivity()
        {
            float DialSens = DialSensSlider.value;
            DialSensSliderNumber.text = DialSens.ToString("F2");
            for (int i = 0; i < Vehicles.Length; i++)
            {
                Vehicles[i].SetProgramVariable("DialSensitivity", DialSens);
            }
        }
        public Toggle SwitchHandsToggle;
        private bool SwitchHandsDefault;
        public void SwitchHands()
        {
            foreach (UdonSharpBehaviour SAV in SaccAirVehicles)
            {
                if (SAV)
                { SAV.SetProgramVariable("SwitchHandsJoyThrottle", !(bool)SAV.GetProgramVariable("SwitchHandsJoyThrottle")); }
            }
        }
        public Toggle AutoEngineToggle;
        private bool AutoEngineDefault;
        public void AutoEngineStart()
        {
            bool AutoEngine = AutoEngineToggle.isOn;
            foreach (UdonSharpBehaviour SAV in SaccAirVehicles)
            {
                if (SAV)
                {
                    SAV.SetProgramVariable("EngineOnOnEnter", AutoEngine);
                    SAV.SetProgramVariable("EngineOffOnExit", AutoEngine);
                }
            }
        }
        public Toggle PassengerComfortModeToggle;
        private bool PassengerComfortModeDefault;
        public void SetPassengerComfortMode()
        {
            bool PassengerComfortMode = PassengerComfortModeToggle.isOn;
            foreach (UdonSharpBehaviour SS in SAVSyncScripts)
            {
                if (SS)
                { SS.SetProgramVariable("PassengerComfortMode", PassengerComfortMode); }
            }
        }
        public Slider SaccFlightStrengthSlider;
        public Text SaccFlightStrengthSliderNumber;
        public UdonSharpBehaviour SaccFlight;
        public void SetSaccFlightStrength()
        {
            if (SaccFlight)
            {
                float strvalue = SaccFlightStrengthSlider.value;
                SaccFlightStrengthSliderNumber.text = strvalue.ToString("F2");
                SaccFlight.SetProgramVariable("_thruststrength", strvalue * 90);
            }
        }
        public void Reset()
        {
            GripSensitivitySlider.value = 75f;
            DialSensSlider.value = .7f;
            ThrottleSensitivitySlider.value = 6f;
            JoyStickSensitivitySlider.value = 45f;
            if (SwitchHandsToggle.isOn != SwitchHandsDefault) { SwitchHandsToggle.isOn = !SwitchHandsToggle.isOn; }
            AutoEngineToggle.isOn = AutoEngineDefault;
            PassengerComfortModeToggle.isOn = PassengerComfortModeDefault;
            SaccFlightStrengthSlider.value = .33f;
            SaccFlightStrengthSlider.value = .33f;
        }
    }
}