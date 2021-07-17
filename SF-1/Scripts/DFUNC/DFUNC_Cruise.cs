
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Cruise : UdonSharpBehaviour
{
    [SerializeField] private bool UseLeftTrigger;
    [SerializeField] EngineController EngineControl;
    [SerializeField] private GameObject Dial_Funcon;
    private bool Dial_FunconNULL = true;
    private bool TriggerLastFrame;
    private Transform VehicleTransform;
    private VRCPlayerApi localPlayer;
    private float CruiseTemp;
    private float SpeedZeroPoint;
    private float TriggerTapTime = 1;
    public void SFEXT_L_ECStart()
    {
        localPlayer = Networking.LocalPlayer;
        VehicleTransform = EngineControl.VehicleMainObj.GetComponent<Transform>();
        Dial_FunconNULL = Dial_Funcon == null;
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(false);
    }
    public void DFUNC_Selected()
    {
        gameObject.SetActive(true);
    }
    public void DFUNC_Deselected()
    {
        gameObject.SetActive(false);
        TriggerTapTime = 1;
    }
    public void SFEXT_O_PilotEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(EngineControl.Cruise);
    }
    public void SFEXT_P_PassengerEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(EngineControl.Cruise);
    }
    public void SFEXT_O_PilotExit()
    {
        gameObject.SetActive(false);
        localPlayer.SetVelocity(localPlayer.GetVelocity() + VehicleTransform.up * 25);
        TriggerTapTime = 1;
    }
    private void LateUpdate()
    {
        float Trigger;
        if (UseLeftTrigger)
        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
        else
        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

        if (Trigger > 0.75)
        {
            float CruiseTemp = 0;
            //for setting speed in VR
            Vector3 handpos = VehicleTransform.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
            handpos = VehicleTransform.InverseTransformDirection(handpos);

            //enable and disable
            if (!TriggerLastFrame)
            {
                if (!EngineControl.Cruise)
                {
                    EngineControl.SetSpeed = EngineControl.AirSpeed;
                    EngineControl.Cruise = true;
                    if (!Dial_FunconNULL) { Dial_Funcon.SetActive(EngineControl.Cruise); }
                }
                if (TriggerTapTime > .4f)//no double tap
                {
                    TriggerTapTime = 0;
                }
                else//double tap detected, turn off cruise
                {
                    EngineControl.PlayerThrottle = EngineControl.ThrottleInput;
                    EngineControl.Cruise = false;
                    if (!Dial_FunconNULL) { Dial_Funcon.SetActive(EngineControl.Cruise); }
                }
                //end of enable disable

                //more set speed stuff
                SpeedZeroPoint = handpos.z;
                CruiseTemp = EngineControl.SetSpeed;
            }
            float SpeedDifference = (SpeedZeroPoint - handpos.z) * 250;
            EngineControl.SetSpeed = Mathf.Floor(Mathf.Clamp(CruiseTemp + SpeedDifference, 0, 2000));

            TriggerLastFrame = true;
        }
        else { TriggerLastFrame = false; }

        TriggerTapTime += Time.deltaTime;
    }
    public void KeyboardInput()
    {
        if (!EngineControl.Cruise)
        {
            EngineControl.SetSpeed = EngineControl.AirSpeed;
            EngineControl.Cruise = true;
            if (!Dial_FunconNULL) { Dial_Funcon.SetActive(EngineControl.Cruise); }
        }
        else
        {
            EngineControl.PlayerThrottle = EngineControl.ThrottleInput;
            EngineControl.Cruise = false;
            if (!Dial_FunconNULL) { Dial_Funcon.SetActive(EngineControl.Cruise); }
        }
    }
}
