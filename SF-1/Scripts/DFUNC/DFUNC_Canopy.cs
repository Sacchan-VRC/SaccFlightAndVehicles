
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Canopy : UdonSharpBehaviour
{
    [SerializeField] private bool UseLeftTrigger;
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private GameObject Dial_Funcon;
    private EffectsController EffectsControl;
    private bool Dial_FunconNULL = true;
    private bool TriggerLastFrame;
    private Transform VehicleTransform;
    private VRCPlayerApi localPlayer;
    private float EjectZeroPoint;
    [System.NonSerializedAttribute] public float EjectTimer = 1;
    private HUDController HUDControl;
    [System.NonSerializedAttribute] public bool Ejected = false;
    private bool InVR;
    public void SFEXT_L_ECStart()
    {
        localPlayer = Networking.LocalPlayer;
        HUDControl = EngineControl.HUDControl;
        EffectsControl = EngineControl.EffectsControl;
        VehicleTransform = EngineControl.VehicleMainObj.GetComponent<Transform>();
        Dial_FunconNULL = Dial_Funcon == null;
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(EffectsControl.CanopyOpen);
    }
    public void DFUNC_Selected()
    {
        gameObject.SetActive(true);
    }
    public void DFUNC_Deselected()
    {
        gameObject.SetActive(false);
    }
    public void SFEXT_O_PilotEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(EffectsControl.CanopyOpen);
        InVR = EngineControl.InVR;
    }
    public void SFEXT_O_PilotExit()
    {
        gameObject.SetActive(false);
        if (Ejected)
        {
            localPlayer.SetVelocity(localPlayer.GetVelocity() + VehicleTransform.up * 25);
            Ejected = false;
        }
    }
    public void SFEXT_O_RespawnButton()
    {
        EngineControl.CanopyOpening();
    }
    public void SFEXT_O_PassengerEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(EffectsControl.CanopyOpen);
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
            if (!TriggerLastFrame && EngineControl.Speed < 20)
            {
                if (EngineControl.CanopyCloseTimer <= -100000 - EngineControl.CanopyCloseTime)
                {
                    if (!Dial_FunconNULL) { Dial_Funcon.SetActive(false); }
                }
                else if (EngineControl.CanopyCloseTimer < 0 && EngineControl.CanopyCloseTimer > -100000)
                {
                    if (!Dial_FunconNULL) { Dial_Funcon.SetActive(true); }
                }
                EngineControl.ToggleCanopy();
            }

            //ejection
            if (InVR)
            {
                Vector3 handposL = VehicleTransform.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                handposL = VehicleTransform.InverseTransformDirection(handposL);
                Vector3 handposR = VehicleTransform.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                handposR = VehicleTransform.InverseTransformDirection(handposR);

                if (!TriggerLastFrame && (handposL.y - handposR.y) < 0.20f)
                {
                    EjectZeroPoint = handposL.y;
                    EjectTimer = 0;
                }
                if (EjectZeroPoint - handposL.y > .5f && EjectTimer < 1)
                {
                    Ejected = true;
                    HUDControl.ExitStation();
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CanopyOpening");
                }
            }

            EjectTimer += Time.deltaTime;
            TriggerLastFrame = true;
        }
        else
        {
            TriggerLastFrame = false;
            EjectTimer = 2;
        }
    }
    public void KeyboardInput()
    {
        if (EngineControl.Speed < 20)
        {
            if (EngineControl.CanopyCloseTimer <= -100000 - EngineControl.CanopyCloseTime)
            {
                if (!Dial_FunconNULL) { Dial_Funcon.SetActive(false); }
            }
            else if (EngineControl.CanopyCloseTimer < 0 && EngineControl.CanopyCloseTimer > -100000)
            {
                if (!Dial_FunconNULL) { Dial_Funcon.SetActive(true); }
            }
            EngineControl.ToggleCanopy();
        }
    }
}
