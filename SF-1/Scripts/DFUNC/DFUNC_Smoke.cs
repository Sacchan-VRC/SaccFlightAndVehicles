
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Smoke : UdonSharpBehaviour
{
    [SerializeField] EngineController EngineControl;
    [SerializeField] private Animator SmokeAnimator;
    [SerializeField] private Material SmokeColorIndicator;
    [SerializeField] private bool UseLeftTrigger;
    [SerializeField] private GameObject Dial_Funcon;
    private Transform VehicleTransform;
    private bool Dial_FunconNULL = true;
    private VRCPlayerApi localPlayer;
    private bool TriggerLastFrame;
    private float SmokeHoldTime;
    private bool SetSmokeLastFrame;
    private Vector3 SmokeZeroPoint;
    public ParticleSystem[] DisplaySmoke;
    private bool DisplaySmokeNull = true;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public Vector3 SmokeColor = Vector3.one;
    [System.NonSerializedAttribute] public bool Smoking = false;
    private int DISPLAYSMOKE_STRING = Animator.StringToHash("displaysmoke");
    [System.NonSerializedAttribute] public Color SmokeColor_Color;
    private bool Pilot;
    private bool InPlane;
    public void SFEXT_L_ECStart()
    {
        localPlayer = Networking.LocalPlayer;
        VehicleTransform = EngineControl.VehicleMainObj.GetComponent<Transform>();
        Dial_FunconNULL = Dial_Funcon == null;
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(false);
        if (DisplaySmoke.Length > 0) DisplaySmokeNull = false;
    }
    private void LateUpdate()
    {
        if (InPlane)
        {
            if (Pilot)
            {
                float DeltaTime = Time.deltaTime;
                float Trigger;
                if (UseLeftTrigger)
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                else
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
                if (Trigger > 0.75)
                {
                    //you can change smoke colour by holding down the trigger and waving your hand around. x/y/z = r/g/b
                    Vector3 HandPosSmoke = VehicleTransform.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                    HandPosSmoke = VehicleTransform.InverseTransformDirection(HandPosSmoke);
                    Vector3 smokcol = SmokeColor;
                    Vector3 TempSmokeCol = Vector3.zero;
                    if (!TriggerLastFrame)
                    {
                        SmokeZeroPoint = HandPosSmoke;
                        TempSmokeCol = smokcol;

                        ToggleSmoking();
                        SmokeHoldTime = 0;
                    }
                    SmokeHoldTime += Time.deltaTime;
                    if (SmokeHoldTime > .4f)
                    {
                        //VR Set Smoke

                        Vector3 SmokeDifference = (SmokeZeroPoint - HandPosSmoke) * -EngineControl.ThrottleSensitivity;
                        smokcol.x = Mathf.Clamp(TempSmokeCol.x + SmokeDifference.x, 0, 1);
                        smokcol.y = Mathf.Clamp(TempSmokeCol.y + SmokeDifference.y, 0, 1);
                        smokcol.z = Mathf.Clamp(TempSmokeCol.z + SmokeDifference.z, 0, 1);
                    }
                    TriggerLastFrame = true;
                }
                else { TriggerLastFrame = false; }

                if (Smoking)
                {
                    int keypad7 = Input.GetKey(KeyCode.Keypad7) ? 1 : 0;
                    int Keypad4 = Input.GetKey(KeyCode.Keypad4) ? 1 : 0;
                    int Keypad8 = Input.GetKey(KeyCode.Keypad8) ? 1 : 0;
                    int Keypad5 = Input.GetKey(KeyCode.Keypad5) ? 1 : 0;
                    int Keypad9 = Input.GetKey(KeyCode.Keypad9) ? 1 : 0;
                    int Keypad6 = Input.GetKey(KeyCode.Keypad6) ? 1 : 0;
                    SmokeColor.x = Mathf.Clamp(SmokeColor.x + ((keypad7 - Keypad4) * DeltaTime), 0, 1);
                    SmokeColor.y = Mathf.Clamp(SmokeColor.y + ((Keypad8 - Keypad5) * DeltaTime), 0, 1);
                    SmokeColor.z = Mathf.Clamp(SmokeColor.z + ((Keypad9 - Keypad6) * DeltaTime), 0, 1);
                }
            }
            //Smoke Color Indicator
            SmokeColorIndicator.color = SmokeColor_Color;
        }


        //everyone does this while smoke is active
        if (Smoking && !DisplaySmokeNull)
        {
            Color SmokeCol = SmokeColor_Color;
            foreach (ParticleSystem smoke in DisplaySmoke)
            {
                var main = smoke.main;
                main.startColor = new ParticleSystem.MinMaxGradient(SmokeCol, SmokeCol * .8f);
            }
        }
        SmokeColor_Color = new Color(SmokeColor.x, SmokeColor.y, SmokeColor.z);
    }
    public void DFUNC_Selected()
    {
        gameObject.SetActive(true);
    }
    public void DFUNC_Deselected()
    {
        if (!Smoking)
        { gameObject.SetActive(false); }
    }

    public void Set_Active()
    { gameObject.SetActive(true); }
    public void Set_Inactive()
    {
        if (!Pilot)
        { gameObject.SetActive(false); }
    }
    public void SFEXT_O_PilotEnter()
    {
        Pilot = true;
        InPlane = true;
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(Smoking);
    }
    public void SFEXT_O_PilotExit()
    {
        Pilot = false;
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetSmokingOff");
    }
    public void SFEXT_O_PassengerEnter()
    {
        InPlane = true;
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(Smoking);
    }
    public void KeyboardInput()
    {
        ToggleSmoking();
    }
    public void SFEXT_O_SmokeOn()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Set_Active");
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(true);
    }
    public void SFEXT_O_SmokeOff()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Set_Inactive");
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(false);
    }
    public void SFEXT_G_RespawnButton()
    {
        SetSmokingOff();
    }
    public void SetSmokingOn()
    {
        Smoking = true;
        SmokeAnimator.SetBool(DISPLAYSMOKE_STRING, true);

        if (EngineControl.IsOwner)
        {
            EngineControl.SendEventToExtensions("SFEXT_O_SmokeOn", false);
        }
    }
    public void SetSmokingOff()
    {
        Smoking = false;
        SmokeAnimator.SetBool(DISPLAYSMOKE_STRING, false);

        if (EngineControl.IsOwner)
        {
            EngineControl.SendEventToExtensions("SFEXT_O_SmokeOff", false);
        }
    }
    public void ToggleSmoking()
    {
        if (!Smoking)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetSmokingOn");
        }
        else
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetSmokingOff");
        }
    }
    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (EngineControl.IsOwner)
        {
            if (Smoking)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetSmokingOn");
            }
        }
    }
}
