
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_AltHold : UdonSharpBehaviour
{
    [SerializeField] private SaccAirVehicle SAVControl;
    [SerializeField] private GameObject HudHold;
    [SerializeField] private GameObject Dial_Funcon;
    private SaccEntity EntityControl;
    private bool UseLeftTrigger = false;
    private bool Dial_FunconNULL = true;
    private bool HudHoldNULL = true;
    private bool TriggerLastFrame;
    private bool AltHold;
    private Rigidbody VehicleRigidbody;
    private Transform VehicleTransform;
    private Vector3 RotationInputs;
    private float AltHoldPitchProportional = 1f;
    private float AltHoldPitchIntegral = 1f;
    private float AltHoldPitchIntegrator;
    private float AltHoldPitchlastframeerror;
    private float AltHoldRollProportional = -.005f;
    private bool Piloting;
    private bool InVR;
    private bool Selected;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXT_L_EntityStart()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (localPlayer != null)
        { InVR = localPlayer.IsUserInVR(); }
        Dial_FunconNULL = Dial_Funcon == null;
        HudHoldNULL = HudHold == null;
        VehicleRigidbody = SAVControl.VehicleRigidbody;
        VehicleTransform = SAVControl.VehicleTransform;
        EntityControl = SAVControl.EntityControl;
        if (!Dial_FunconNULL) { Dial_Funcon.SetActive(false); }
    }
    public void DFUNC_Selected()
    {
        gameObject.SetActive(true);
        Selected = true;
    }
    public void DFUNC_Deselected()
    {
        if (!AltHold) { gameObject.SetActive(false); }
        TriggerLastFrame = false;
        Selected = false;
    }
    public void SFEXT_O_PilotEnter()
    {
        gameObject.SetActive(false);
        Piloting = true;
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(AltHold);
    }
    public void SFEXT_O_PassengerEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(AltHold);
    }
    public void SFEXT_O_PilotExit()
    {
        gameObject.SetActive(false);
        Selected = false;
        Piloting = false;
        if (AltHold)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(DeactivateAltHold)); }
    }
    public void SFEXT_G_TouchDown()
    {
        if (AltHold)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(DeactivateAltHold)); }
    }
    public void SFEXT_O_PlayerJoined()
    {
        if (AltHold)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ActivateAltHold));
        }
    }
    public void ActivateAltHold()
    {
        if (AltHold) { return; }
        AltHold = true;
        SAVControl.JoystickOverridden += 1;
        if (!Dial_FunconNULL) { Dial_Funcon.SetActive(AltHold); }
        if (!HudHoldNULL) { HudHold.SetActive(AltHold); }
        if (Piloting) { EntityControl.SendEventToExtensions("SFEXT_O_AltHoldOn"); }
    }
    public void DeactivateAltHold()
    {
        if (!AltHold) { return; }
        if (!InVR || !Selected) { gameObject.SetActive(false); }
        AltHold = false;
        if (!Dial_FunconNULL) { Dial_Funcon.SetActive(AltHold); }
        if (!HudHoldNULL) { HudHold.SetActive(AltHold); }
        SAVControl.JoystickOverridden -= 1;
        SAVControl.JoystickOverride = Vector3.zero;
        RotationInputs = Vector3.zero;
        if (Piloting) { EntityControl.SendEventToExtensions("SFEXT_O_AltHoldOff"); }
    }
    private void Update()
    {
        float Trigger;
        if (UseLeftTrigger)
        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
        else
        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

        if (Trigger > 0.75)
        {
            if (!TriggerLastFrame)
            {
                if (AltHold)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(DeactivateAltHold));
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ActivateAltHold));
                }
            }
            TriggerLastFrame = true;
        }
        else { TriggerLastFrame = false; }

        if (AltHold && Piloting)
        {
            float DeltaTime = Time.deltaTime;
            Vector3 localAngularVelocity = VehicleTransform.InverseTransformDirection(VehicleRigidbody.angularVelocity);
            //Altitude hold PI Controller

            int upsidedown = Vector3.Dot(Vector3.up, VehicleTransform.up) > 0 ? 1 : -1;
            float error = SAVControl.CurrentVel.normalized.y - (localAngularVelocity.x * upsidedown * 2.5f);

            AltHoldPitchIntegrator += error * DeltaTime;
            //AltHoldPitchIntegrator = Mathf.Clamp(AltHoldPitchIntegrator, AltHoldPitchIntegratorMin, AltHoldPitchIntegratorMax);
            //AltHoldPitchDerivator = (error - AltHoldPitchlastframeerror) / DeltaTime;
            AltHoldPitchlastframeerror = error;
            RotationInputs.x = AltHoldPitchProportional * error;
            RotationInputs.x += AltHoldPitchIntegral * AltHoldPitchIntegrator;
            //RotationInputs.x += AltHoldPitchDerivative * AltHoldPitchDerivator; //works but spazzes out real bad
            RotationInputs.x = Mathf.Clamp(RotationInputs.x, -1, 1);
            AltHoldPitchlastframeerror = error;

            //Roll
            float ErrorRoll = VehicleTransform.localEulerAngles.z;
            if (ErrorRoll > 180) { ErrorRoll -= 360; }

            //lock upside down if rotated more than 90
            if (ErrorRoll > 90)
            {
                ErrorRoll -= 180;
                RotationInputs.x *= -1;
            }
            else if (ErrorRoll < -90)
            {
                ErrorRoll += 180;
                RotationInputs.x *= -1;
            }

            RotationInputs.z = Mathf.Clamp(AltHoldRollProportional * ErrorRoll, -1, 1);

            RotationInputs.y = 0;

            //flight limit internally enabled when alt hold is enabled
            float GLimitStrength = Mathf.Clamp(-(SAVControl.VertGs / SAVControl.GLimiter) + 1, 0, 1);
            float AoALimitStrength = Mathf.Clamp(-(Mathf.Abs(SAVControl.AngleOfAttack) / SAVControl.AoALimiter) + 1, 0, 1);
            float Limits = Mathf.Min(GLimitStrength, AoALimitStrength);
            RotationInputs.x *= Limits;

            SAVControl.JoystickOverride = RotationInputs;
        }
    }
    public void KeyboardInput()
    {
        if (AltHold)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(DeactivateAltHold));
        }
        else
        {
            if (SAVControl.VTOLAngle != SAVControl.VTOLDefaultValue) { return; }
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ActivateAltHold));
            gameObject.SetActive(true);
        }
    }
}