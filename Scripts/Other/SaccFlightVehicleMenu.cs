
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
        private SaccSeaVehicle[] SaccSeaVehicles;
        private SaccGroundVehicle[] SaccGroundVehicles;
        private SGV_GearBox[] SGVGearBoxs;
        private SAV_SyncScript[] SAVSyncScripts;
        public Slider JoyStickSensitivitySlider;
        public Text JoyStickSensitivitySliderNumber;
        private float[] DefaultSteeringDegrees;
        private void Start()
        {
            if (PassengerComfortModeToggle) { PassengerComfortModeDefault = PassengerComfortModeToggle.isOn; }
            if (SwitchHandsToggle) { SwitchHandsDefault = SwitchHandsToggle.isOn; }
            if (AutoEngineToggle) { AutoEngineDefault = AutoEngineToggle.isOn; }
            if (AutomaticGearsToggle) { AutomaticGearsDefault = AutomaticGearsToggle.isOn; }
            if (InvertVRGearChangeToggle) { InvertVRGearChangeDefault = InvertVRGearChangeToggle.isOn; }
            if (LeftGripClutchToggle) { LeftGripClutchDefault = LeftGripClutchToggle.isOn; }
            SaccAirVehicles = new SaccAirVehicle[Vehicles.Length];
            SaccSeaVehicles = new SaccSeaVehicle[Vehicles.Length];
            SaccGroundVehicles = new SaccGroundVehicle[Vehicles.Length];
            SGVGearBoxs = new SGV_GearBox[Vehicles.Length];
            SAVSyncScripts = new SAV_SyncScript[Vehicles.Length];
            for (int i = 0; i < Vehicles.Length; i++)
            {
                SaccAirVehicles[i] = (SaccAirVehicle)Vehicles[i].GetExtention(GetUdonTypeName<SaccAirVehicle>());
                SaccSeaVehicles[i] = (SaccSeaVehicle)Vehicles[i].GetExtention(GetUdonTypeName<SaccSeaVehicle>());
                SaccGroundVehicles[i] = (SaccGroundVehicle)Vehicles[i].GetExtention(GetUdonTypeName<SaccGroundVehicle>());
                SAVSyncScripts[i] = (SAV_SyncScript)Vehicles[i].GetExtention(GetUdonTypeName<SAV_SyncScript>());
                SGVGearBoxs[i] = (SGV_GearBox)Vehicles[i].GetExtention(GetUdonTypeName<SGV_GearBox>());
            }
            DefaultSteeringDegrees = new float[Vehicles.Length];
            for (int i = 0; i < Vehicles.Length; i++)
            {
                if (SaccGroundVehicles[i])
                {
                    DefaultSteeringDegrees[i] = (float)SaccGroundVehicles[i].GetProgramVariable("SteeringWheelDegrees");
                }
                else { DefaultSteeringDegrees[i] = 1f; }
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
            foreach (UdonSharpBehaviour SSV in SaccSeaVehicles)
            {
                if (SSV)
                { SSV.SetProgramVariable("ThrottleSensitivity", sensvalue); }
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
            foreach (UdonSharpBehaviour SSV in SaccSeaVehicles)
            {
                if (SSV)
                { SSV.SetProgramVariable("GripSensitivity", sensvalue); }
            }
            foreach (UdonSharpBehaviour SGV in SaccGroundVehicles)
            {
                if (SGV)
                { SGV.SetProgramVariable("GripSensitivity", sensvalue); }
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
            foreach (UdonSharpBehaviour SSV in SaccSeaVehicles)
            {
                if (SSV)
                { SSV.SetProgramVariable("SwitchHandsJoyThrottle", !(bool)SSV.GetProgramVariable("SwitchHandsJoyThrottle")); }
            }
            /* foreach (UdonSharpBehaviour SGV in SaccGroundVehicles)
            {
                if (SGV)
                { SGV.SetProgramVariable("SwitchHandsJoyThrottle", !(bool)SGV.GetProgramVariable("SwitchHandsJoyThrottle")); }
            } */
        }
        public Toggle AutomaticGearsToggle;
        private bool AutomaticGearsDefault;
        public void ToggleAutomaticGears()
        {
            foreach (UdonSharpBehaviour SGVg in SGVGearBoxs)
            {
                if (SGVg)
                {
                    if ((bool)SGVg.GetProgramVariable("AllowMenuToToggleAutomatic"))
                    {
                        SGVg.SetProgramVariable("Automatic", !(bool)SGVg.GetProgramVariable("Automatic"));
                    }
                }
            }
        }
        public Toggle InvertVRGearChangeToggle;
        private bool InvertVRGearChangeDefault;
        public void ToggleInvertVRGearChange()
        {
            bool InvertVRGearChange = InvertVRGearChangeToggle.isOn;
            foreach (UdonSharpBehaviour SGVg in SGVGearBoxs)
            {
                if (SGVg)
                {
                    SGVg.SetProgramVariable("InvertVRGearChangeDirection", InvertVRGearChange);
                }
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
            foreach (UdonSharpBehaviour SSV in SaccSeaVehicles)
            {
                if (SSV)
                {
                    SSV.SetProgramVariable("EngineOnOnEnter", AutoEngine);
                    SSV.SetProgramVariable("EngineOffOnExit", AutoEngine);
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
        public Toggle LeftGripClutchToggle;
        private bool LeftGripClutchDefault;
        public void ToggleLeftGripClutch()
        {
            bool LeftGripClutch = LeftGripClutchToggle.isOn;
            foreach (UdonSharpBehaviour SGVg in SGVGearBoxs)
            {
                if (SGVg)
                { SGVg.SetProgramVariable("ClutchDisabled", !LeftGripClutch); }
            }
            foreach (UdonSharpBehaviour SGV in SaccGroundVehicles)
            {
                if (SGV)
                { SGV.SetProgramVariable("SteeringHand_Left", !LeftGripClutch); }
            }
        }
        public Slider SteeringSenseSlider;
        public Text SteeringSenseSliderNumber;
        public void SetSteeringSenseitivity()
        {
            float SteeringSens = SteeringSenseSlider.value;
            SteeringSenseSliderNumber.text = SteeringSens.ToString("F2");
            for (int i = 0; i < Vehicles.Length; i++)
            {
                if (SaccGroundVehicles[i])
                { SaccGroundVehicles[i].SetProgramVariable("SteeringWheelDegrees", DefaultSteeringDegrees[i] / SteeringSens); }
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
            SteeringSenseSlider.value = 1f;
            if (SwitchHandsToggle) { if (SwitchHandsToggle.isOn != SwitchHandsDefault) { SwitchHandsToggle.isOn = SwitchHandsDefault; } }
            if (AutomaticGearsToggle) { if (AutomaticGearsToggle.isOn != AutomaticGearsDefault) { AutomaticGearsToggle.isOn = AutomaticGearsDefault; } }
            if (InvertVRGearChangeToggle) { if (InvertVRGearChangeToggle.isOn != InvertVRGearChangeDefault) { InvertVRGearChangeToggle.isOn = InvertVRGearChangeDefault; } }
            if (LeftGripClutchToggle) { if (LeftGripClutchToggle.isOn != LeftGripClutchDefault) { LeftGripClutchToggle.isOn = LeftGripClutchDefault; } }
            AutoEngineToggle.isOn = AutoEngineDefault;
            PassengerComfortModeToggle.isOn = PassengerComfortModeDefault;
            SaccFlightStrengthSlider.value = .33f;
        }
    }
}