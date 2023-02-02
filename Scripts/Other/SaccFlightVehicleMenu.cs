
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
        private SaccAirVehicle[] SaccAirVehicles;
        private SaccSeaVehicle[] SaccSeaVehicles;
        private SaccGroundVehicle[] SaccGroundVehicles;
        private SGV_GearBox[] SGVGearBoxs;
        private SAV_SyncScript[] SAVSyncScripts;
        public Slider JoystickSensitivitySlider;
        public Text JoyStickSensitivitySliderNumber;
        private float[] DefaultSteeringDegrees;
        private void Start()
        {

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
            //get default values of toggles and sliders, and run their callback to make sure they match the menu at start
            //toggles
            if (AutoEngineToggle) { AutoEngineDefault = AutoEngineToggle.isOn; ToggleAutoEngineStart(); }
            if (AutomaticGearsToggle) { AutomaticGearsDefault = AutomaticGearsToggle.isOn; ToggleAutomaticGears(); }
            if (InvertVRGearChangeToggle) { InvertVRGearChangeDefault = InvertVRGearChangeToggle.isOn; ToggleInvertVRGearChange(); }
            if (LeftGripClutchToggle) { LeftGripClutchDefault = LeftGripClutchToggle.isOn; ToggleLeftGripClutch(); }
            if (SwitchHandsToggle) { SwitchHandsDefault = SwitchHandsToggle.isOn; }//don't run this one because it can be on or off by default and running it would toggle it
            if (DoubleTapToExit) { DoubleTapToExitDefault = DoubleTapToExit.isOn; ToggleDoubleTapToExit(); }
            if (StickSteering) { StickSteeringDefault = StickSteering.isOn; ToggleStickSteering(); }
            //sliders
            if (DialSensSlider) { DialSensDefault = DialSensSlider.value; SetDialSensitivity(); }
            if (GripSensitivitySlider) { GripSensitivityDefault = GripSensitivitySlider.value; SetGripSensitivity(); }
            if (JoystickSensitivitySlider) { JoyStickSensitivityDefault = JoystickSensitivitySlider.value; SetJoystickSensitivity(); }
            if (KeyboardThrottleSensitivitySlider) { KeyboardThrottleSensitivityDefault = KeyboardThrottleSensitivitySlider.value; SetKeyboardThrottleSensitivity(); }
            if (SteeringSensSlider) { SteeringSensDefault = SteeringSensSlider.value; SetSteeringSensitivity(); }
            if (ThrottleSensitivitySlider) { ThrottleSensitivityDefault = ThrottleSensitivitySlider.value; SetThrottleSensitivity(); }
            if (SaccFlightStrengthSlider) { SaccFlightStrengthDefault = SaccFlightStrengthSlider.value; SetSaccFlightStrength(); }
        }
        public Toggle AutoEngineToggle;
        private bool AutoEngineDefault;
        public void ToggleAutoEngineStart()
        {
            bool AutoEngine = AutoEngineToggle.isOn;
            foreach (UdonSharpBehaviour SAV in SaccAirVehicles)
            {
                if (SAV)
                {
                    if (((SaccEntity)SAV.GetProgramVariable("EntityControl")).GetExtention(GetUdonTypeName<DFUNC_ToggleEngine>()))
                    {
                        SAV.SetProgramVariable("EngineOnOnEnter", AutoEngine);
                        SAV.SetProgramVariable("EngineOffOnExit", AutoEngine);
                    }
                }
            }
            foreach (UdonSharpBehaviour SSV in SaccSeaVehicles)
            {
                if (SSV)
                {
                    if (((SaccEntity)SSV.GetProgramVariable("EntityControl")).GetExtention(GetUdonTypeName<DFUNC_ToggleEngine>()))
                    {
                        SSV.SetProgramVariable("EngineOnOnEnter", AutoEngine);
                        SSV.SetProgramVariable("EngineOffOnExit", AutoEngine);
                    }
                }
            }
        }
        public Toggle AutomaticGearsToggle;
        private bool AutomaticGearsDefault;
        public void ToggleAutomaticGears()
        {
            bool auto = AutomaticGearsToggle.isOn;
            foreach (UdonSharpBehaviour SGVg in SGVGearBoxs)
            {
                if (SGVg)
                {
                    if ((bool)SGVg.GetProgramVariable("AllowMenuToToggleAutomatic"))
                    {
                        SGVg.SetProgramVariable("Automatic", auto);
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
        public Toggle SwitchHandsToggle;
        private bool SwitchHandsDefault;
        public void ToggleSwitchHands()
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
        [System.NonSerialized] public float DialSensDefault;
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
        private float GripSensitivityDefault;
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
        private float JoyStickSensitivityDefault;
        public void SetJoystickSensitivity()
        {
            float sensvalue = JoystickSensitivitySlider.value;
            Vector3 sens = new Vector3(sensvalue, sensvalue, sensvalue);
            JoyStickSensitivitySliderNumber.text = sensvalue.ToString("F0");
            foreach (UdonSharpBehaviour SAV in SaccAirVehicles)
            {
                if (SAV)
                { SAV.SetProgramVariable("MaxJoyAngles", sens); }
            }
        }
        [System.NonSerialized] public float SteeringSensDefault;
        public Slider SteeringSensSlider;
        public Text SteeringSenseSliderNumber;
        public void SetSteeringSensitivity()
        {
            float SteeringSens = SteeringSensSlider.value;
            SteeringSenseSliderNumber.text = SteeringSens.ToString("F2");
            for (int i = 0; i < Vehicles.Length; i++)
            {
                if (SaccGroundVehicles[i])
                { SaccGroundVehicles[i].SetProgramVariable("SteeringWheelDegrees", DefaultSteeringDegrees[i] / SteeringSens); }
            }
        }
        [System.NonSerialized] public float ThrottleSensitivityDefault;
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
        [System.NonSerialized] public float KeyboardThrottleSensitivityDefault;
        public Slider KeyboardThrottleSensitivitySlider;
        public Text KeyboardThrottleSensitivitySliderNumber;
        public void SetKeyboardThrottleSensitivity()
        {
            float sensvalue = KeyboardThrottleSensitivitySlider.value;
            KeyboardThrottleSensitivitySliderNumber.text = sensvalue.ToString("F2");
            foreach (UdonSharpBehaviour SAV in SaccAirVehicles)
            {
                if (SAV)
                { SAV.SetProgramVariable("KeyboardThrottleSens", sensvalue); }
            }
            foreach (UdonSharpBehaviour SSV in SaccSeaVehicles)
            {
                if (SSV)
                { SSV.SetProgramVariable("KeyboardThrottleSens", sensvalue); }
            }
        }
        public Toggle DoubleTapToExit;
        private bool DoubleTapToExitDefault;
        public void ToggleDoubleTapToExit()
        {
            bool DoubleTap = DoubleTapToExit.isOn;
            foreach (UdonSharpBehaviour SE in Vehicles)
            {
                if (SE)
                {
                    SE.SetProgramVariable("DoubleTapToExit", DoubleTap);
                }
            }
        }
        public Toggle StickSteering;
        private bool StickSteeringDefault;
        public void ToggleStickSteering()
        {
            bool stickSteer = StickSteering.isOn;
            foreach (UdonSharpBehaviour SGV in SaccGroundVehicles)
            {
                if (SGV)
                {
                    SGV.SetProgramVariable("UseStickSteering", stickSteer);
                }
            }
        }
        [System.NonSerialized] public float SaccFlightStrengthDefault;
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
            //set values on buttons which in turn runs the callbacks
            if (GripSensitivitySlider) { GripSensitivitySlider.value = GripSensitivityDefault; }
            if (DialSensSlider) { DialSensSlider.value = DialSensDefault; }
            if (ThrottleSensitivitySlider) { ThrottleSensitivitySlider.value = ThrottleSensitivityDefault; }
            if (JoystickSensitivitySlider) { JoystickSensitivitySlider.value = JoyStickSensitivityDefault; }
            if (KeyboardThrottleSensitivitySlider) { KeyboardThrottleSensitivitySlider.value = KeyboardThrottleSensitivityDefault; }
            if (SteeringSensSlider) { SteeringSensSlider.value = SteeringSensDefault; }
            if (SwitchHandsToggle) { if (SwitchHandsToggle.isOn != SwitchHandsDefault) { SwitchHandsToggle.isOn = SwitchHandsDefault; } }
            if (AutomaticGearsToggle) { AutomaticGearsToggle.isOn = AutomaticGearsDefault; }
            if (InvertVRGearChangeToggle) { InvertVRGearChangeToggle.isOn = InvertVRGearChangeDefault; }
            if (LeftGripClutchToggle) { LeftGripClutchToggle.isOn = LeftGripClutchDefault; }
            if (AutoEngineToggle) { AutoEngineToggle.isOn = AutoEngineDefault; }
            if (DoubleTapToExit) { DoubleTapToExit.isOn = DoubleTapToExitDefault; }
            if (StickSteering) { StickSteering.isOn = StickSteeringDefault; }
            if (SaccFlight) { SaccFlightStrengthSlider.value = SaccFlightStrengthDefault; }
        }
        [Header("Debug")]
        [Tooltip("Automatically filled, use this to check what is in the target list during play mode")]
        public SaccEntity[] Vehicles;
    }
}