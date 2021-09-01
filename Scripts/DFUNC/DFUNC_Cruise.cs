
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Cruise : UdonSharpBehaviour
{
    [SerializeField] SaccAirVehicle SAVControl;
    [Tooltip("Object enabled when function is active (used on MFD)")]
    [SerializeField] private GameObject Dial_Funcon;
    [SerializeField] private Text HUDText_knotstarget;
    private SaccEntity EntityControl;
    private bool UseLeftTrigger = false;
    private bool Dial_FunconNULL = true;
    private bool HUDText_knotstargetNULL = true;
    private bool TriggerLastFrame;
    private Transform VehicleTransform;
    private VRCPlayerApi localPlayer;
    private float CruiseTemp;
    private float SpeedZeroPoint;
    private float TriggerTapTime = 1;
    [System.NonSerializedAttribute] public bool Cruise;
    private float CruiseProportional = .1f;
    private float CruiseIntegral = .1f;
    private float CruiseIntegrator;
    private float CruiseIntegratorMax = 5;
    private float CruiseIntegratorMin = -5;
    private float Cruiselastframeerror;
    private bool func_active;
    private bool Piloting;
    private bool InVR;
    private bool CruiseThrottleOverridden;
    [System.NonSerializedAttribute] public float SetSpeed;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXT_L_EntityStart()
    {
        localPlayer = Networking.LocalPlayer;
        if (localPlayer != null)
        { InVR = localPlayer.IsUserInVR(); }
        VehicleTransform = SAVControl.EntityControl.transform;
        EntityControl = SAVControl.EntityControl;
        HUDText_knotstargetNULL = HUDText_knotstarget == null;
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
        TriggerLastFrame = false;
    }
    public void SFEXT_O_PilotEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(Cruise);
        Piloting = true;
        if (!HUDText_knotstargetNULL) { HUDText_knotstarget.text = string.Empty; }
    }
    public void SFEXT_P_PassengerEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(Cruise);
        if (!HUDText_knotstargetNULL) { HUDText_knotstarget.text = string.Empty; }
    }
    public void SFEXT_O_PilotExit()
    {
        gameObject.SetActive(false);
        Piloting = false;
        TriggerTapTime = 1;
        TriggerLastFrame = false;
        if (Cruise)
        {
            SetCruiseOff();
        }
    }
    public void SFEXT_G_Explode()
    {
        if (Cruise)
        { SetCruiseOff(); }
    }
    public void SFEXT_G_TouchDown()
    {
        if (Cruise)
        { SetCruiseOff(); }
    }
    public void SetCruiseOn()
    {
        if (Cruise) { return; }
        if (Piloting)
        {
            gameObject.SetActive(true);
            func_active = true;
        }
        if (!CruiseThrottleOverridden) { SAVControl.ThrottleOverridden += 1; CruiseThrottleOverridden = true; }
        SetSpeed = SAVControl.AirSpeed;
        Cruise = true;
        if (!Dial_FunconNULL) { Dial_Funcon.SetActive(Cruise); }
        EntityControl.SendEventToExtensions("SFEXT_O_CruiseEnabled");
    }
    public void SetCruiseOff()
    {
        if (!Cruise) { return; }
        if (Piloting)
        {
            func_active = false;
            if (!InVR)
            { gameObject.SetActive(true); }
        }
        if (CruiseThrottleOverridden) { SAVControl.ThrottleOverridden -= 1; CruiseThrottleOverridden = false; }
        SAVControl.PlayerThrottle = SAVControl.ThrottleInput;
        Cruise = false;
        if (!Dial_FunconNULL) { Dial_Funcon.SetActive(Cruise); }
        EntityControl.SendEventToExtensions("SFEXT_O_CruiseDisabled");
    }
    private void LateUpdate()
    {
        if (Piloting)
        {
            if (InVR)
            {
                float Trigger;
                if (UseLeftTrigger)
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                else
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

                if (Trigger > 0.75)
                {
                    //for setting speed in VR
                    Vector3 handpos = VehicleTransform.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                    handpos = VehicleTransform.InverseTransformDirection(handpos);

                    //enable and disable
                    if (!TriggerLastFrame)
                    {
                        if (!Cruise)
                        {
                            if (!SAVControl.Taxiing)
                            { SetCruiseOn(); }
                        }
                        if (TriggerTapTime > .4f)//no double tap
                        {
                            TriggerTapTime = 0;
                        }
                        else//double tap detected, turn off cruise
                        {
                            SetCruiseOff();
                        }
                        SpeedZeroPoint = handpos.z;
                        CruiseTemp = SetSpeed;
                    }
                    float SpeedDifference = (SpeedZeroPoint - handpos.z) * 250;
                    SetSpeed = Mathf.Floor(Mathf.Clamp(CruiseTemp + SpeedDifference, 0, 2000));

                    TriggerLastFrame = true;
                }
                else { TriggerLastFrame = false; }
            }
            float DeltaTime = Time.deltaTime;
            float equals = Input.GetKey(KeyCode.Equals) ? DeltaTime * 10 : 0;
            float minus = Input.GetKey(KeyCode.Minus) ? DeltaTime * 10 : 0;
            SetSpeed = Mathf.Max(SetSpeed + (equals - minus), 0);

            if (func_active)
            {
                float error = (SetSpeed - SAVControl.AirSpeed);

                CruiseIntegrator += error * DeltaTime;
                CruiseIntegrator = Mathf.Clamp(CruiseIntegrator, CruiseIntegratorMin, CruiseIntegratorMax);

                //float Derivator = Mathf.Clamp(((error - lastframeerror) / DeltaTime),DerivMin, DerivMax);

                SAVControl.ThrottleOverride = Mathf.Clamp((CruiseProportional * error) + (CruiseIntegral * CruiseIntegrator), 0, 1);
                //ThrottleInput += Derivative * Derivator; //works but spazzes out real bad

                TriggerTapTime += DeltaTime;
            }
        }

        //Cruise Control target knots
        if (Cruise)
        {
            if (!HUDText_knotstargetNULL) { HUDText_knotstarget.text = ((SetSpeed) * 1.9438445f).ToString("F0"); }
        }
        else { if (!HUDText_knotstargetNULL) { HUDText_knotstarget.text = string.Empty; } }
    }
    public void KeyboardInput()
    {
        if (!Cruise)
        {
            SetCruiseOn();
        }
        else
        {
            SetCruiseOff();
        }
    }
}
