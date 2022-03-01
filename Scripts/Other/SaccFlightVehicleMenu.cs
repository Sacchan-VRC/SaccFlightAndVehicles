
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SaccFlightVehicleMenu : UdonSharpBehaviour
{
    public UdonSharpBehaviour[] SaccAirVehicles;
    public Slider JoyStickSensitivitySlider;
    public Text JoyStickSensitivitySliderNumber;
    public void SetJoystickSensitivity()
    {
        float sensvalue = JoyStickSensitivitySlider.value;
        Vector3 sens = new Vector3(sensvalue, sensvalue, sensvalue);
        JoyStickSensitivitySliderNumber.text = sensvalue.ToString("F0");
        foreach (UdonSharpBehaviour SAV in SaccAirVehicles)
        {
            SAV.SetProgramVariable("MaxJoyAngles", sens);
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
            SAV.SetProgramVariable("ThrottleSensitivity", sensvalue);
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
            SAV.SetProgramVariable("GripSensitivity", sensvalue);
        }
    }
    public Slider DialSensSlider;
    public Text DialSensSliderNumber;
    public void SetDialSensitivity()
    {
        float DialSens = DialSensSlider.value;
        DialSensSliderNumber.text = DialSens.ToString("F2");
        foreach (UdonSharpBehaviour SAV in SaccAirVehicles)
        {
            ((SaccEntity)SAV.GetProgramVariable("EntityControl")).SetProgramVariable("DialSensitivity", DialSens);
        }
    }
    public Toggle SwitchHandsToggle;
    private bool SwitchHandsDefault;
    public void SwitchHands()
    {
        foreach (UdonSharpBehaviour SAV in SaccAirVehicles)
        {
            SAV.SetProgramVariable("SwitchHandsJoyThrottle", !(bool)SAV.GetProgramVariable("SwitchHandsJoyThrottle"));
        }
    }
    public Toggle AutoEngineToggle;
    public void AutoEngineStart()
    {
        foreach (UdonSharpBehaviour SAV in SaccAirVehicles)
        {
            SAV.SetProgramVariable("EngineOnOnEnter", !(bool)SAV.GetProgramVariable("EngineOnOnEnter"));
            SAV.SetProgramVariable("EngineOffOnExit", !(bool)SAV.GetProgramVariable("EngineOffOnExit"));
        }
    }
    public Slider SaccFlightStrengthSlider;
    public Text SaccFlightStrengthSliderNumber;
    public UdonSharpBehaviour SaccBall;
    public void SetSaccFlightStrength()
    {
        float strvalue = SaccFlightStrengthSlider.value;
        SaccFlightStrengthSliderNumber.text = strvalue.ToString("F2");
        SaccBall.SetProgramVariable("_thruststrength", strvalue * 90);
    }
    public void Reset()
    {
        GripSensitivitySlider.value = 75f;
        ThrottleSensitivitySlider.value = 6f;
        JoyStickSensitivitySlider.value = 45f;
        SwitchHandsToggle.isOn = SwitchHandsDefault;
        SaccFlightStrengthSlider.value = .33f;
        SaccFlightStrengthSlider.value = .33f;
        DialSensSlider.value = .7f;
    }
    private void Start()
    {
        SwitchHandsDefault = SwitchHandsToggle.isOn;
    }
}
