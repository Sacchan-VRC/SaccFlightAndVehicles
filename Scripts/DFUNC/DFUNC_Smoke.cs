
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class DFUNC_Smoke : UdonSharpBehaviour
{
    [SerializeField] UdonSharpBehaviour SAVControl;
    [Tooltip("Material to change the color value of to match smoke color")]
    [SerializeField] private Material SmokeColorIndicatorMaterial;
    [SerializeField] private ParticleSystem[] DisplaySmoke;
    [Tooltip("HUD Smoke indicator")]
    [SerializeField] private GameObject HUD_SmokeOnIndicator;
    [Tooltip("Object enabled when function is active (used on MFD)")]
    [SerializeField] private GameObject Dial_Funcon;
    private SaccEntity EntityControl;
    private bool UseLeftTrigger = false;
    private Transform VehicleTransform;
    private VRCPlayerApi localPlayer;
    private bool TriggerLastFrame;
    private float SmokeHoldTime;
    private bool SetSmokeLastFrame;
    private Vector3 SmokeZeroPoint;
    private ParticleSystem.EmissionModule[] DisplaySmokeem;
    private bool DisplaySmokeNull = true;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.Linear)] public Vector3 SmokeColor = Vector3.one;
    [System.NonSerializedAttribute] public bool Smoking = false;
    [System.NonSerializedAttribute] public Color SmokeColor_Color;
    private Vector3 TempSmokeCol = Vector3.zero;
    private bool Pilot;
    private bool Selected;
    private bool InEditor;
    private int NumSmokes;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXT_L_EntityStart()
    {
        localPlayer = Networking.LocalPlayer;
        InEditor = localPlayer == null;
        EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
        VehicleTransform = EntityControl.transform;
        if (Dial_Funcon) Dial_Funcon.SetActive(false);
        NumSmokes = DisplaySmoke.Length;
        if (NumSmokes > 0) DisplaySmokeNull = false;
        DisplaySmokeem = new ParticleSystem.EmissionModule[NumSmokes];

        for (int x = 0; x < DisplaySmokeem.Length; x++)
        { DisplaySmokeem[x] = DisplaySmoke[x].emission; }
    }
    public void DFUNC_Selected()
    {
        TriggerLastFrame = true;//To prevent function enabling if you hold the trigger when selecting it
        Selected = true;
        gameObject.SetActive(true);
    }
    public void DFUNC_Deselected()
    {
        if (!Smoking)
        { gameObject.SetActive(false); }
        Selected = false;
        TriggerLastFrame = false;
    }
    public void SFEXT_O_PilotEnter()
    {
        Pilot = true;
        if (Dial_Funcon) Dial_Funcon.SetActive(Smoking);
    }
    public void SFEXT_O_PilotExit()
    {
        Pilot = false;
        Selected = false;
        TriggerLastFrame = false;
        gameObject.SetActive(false);
        if (Smoking) SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetSmokingOff");
    }
    public void SFEXT_P_PassengerEnter()
    {
        if (Dial_Funcon) Dial_Funcon.SetActive(Smoking);
    }
    public void SFEXT_P_PassengerExit()
    {
    }
    public void SFEXT_G_Explode()
    {
        SetSmokingOff();
    }
    public void SFEXT_G_RespawnButton()
    {
        SetSmokingOff();
    }
    public void KeyboardInput()
    {
        ToggleSmoking();
    }
    private void LateUpdate()
    {
        if (Pilot)
        {
            float DeltaTime = Time.deltaTime;
            if (Selected)
            {
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
                    if (!TriggerLastFrame)
                    {
                        SmokeZeroPoint = HandPosSmoke;
                        TempSmokeCol = SmokeColor;

                        ToggleSmoking();
                        SmokeHoldTime = 0;
                    }
                    SmokeHoldTime += Time.deltaTime;
                    if (SmokeHoldTime > .4f)
                    {
                        //VR set smoke color
                        Vector3 SmokeDifference = (SmokeZeroPoint - HandPosSmoke) * -(float)SAVControl.GetProgramVariable("ThrottleSensitivity");
                        SmokeColor.x = Mathf.Clamp(TempSmokeCol.x + SmokeDifference.x, 0, 1);
                        SmokeColor.y = Mathf.Clamp(TempSmokeCol.y + SmokeDifference.y, 0, 1);
                        SmokeColor.z = Mathf.Clamp(TempSmokeCol.z + SmokeDifference.z, 0, 1);
                    }
                    TriggerLastFrame = true;
                }
                else { TriggerLastFrame = false; }
            }
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
            //Smoke Color Indicator
            SmokeColorIndicatorMaterial.color = SmokeColor_Color;
        }
        if (Smoking && !DisplaySmokeNull)
        {
            //everyone does this while smoke is active
            SmokeColor_Color = new Color(SmokeColor.x, SmokeColor.y, SmokeColor.z);
            Color SmokeCol = SmokeColor_Color;
            foreach (ParticleSystem smoke in DisplaySmoke)
            {
                var main = smoke.main;
                main.startColor = new ParticleSystem.MinMaxGradient(SmokeCol, SmokeCol * .8f);
            }
        }
    }
    public void SetSmokingOn()
    {
        Smoking = true;
        gameObject.SetActive(true);
        HUD_SmokeOnIndicator.SetActive(true);
        for (int x = 0; x < DisplaySmokeem.Length; x++)
        { DisplaySmokeem[x].enabled = true; }
        if (Dial_Funcon) Dial_Funcon.SetActive(true);
        if ((bool)SAVControl.GetProgramVariable("IsOwner"))
        {
            EntityControl.SendEventToExtensions("SFEXT_O_SmokeOn");
        }
    }
    public void SetSmokingOff()
    {
        Smoking = false;
        if (!Pilot)
        { gameObject.SetActive(false); }
        HUD_SmokeOnIndicator.SetActive(false);
        for (int x = 0; x < DisplaySmokeem.Length; x++)
        { DisplaySmokeem[x].enabled = false; }
        if (Dial_Funcon) Dial_Funcon.SetActive(false);
        if ((bool)SAVControl.GetProgramVariable("IsOwner"))
        {
            EntityControl.SendEventToExtensions("SFEXT_O_SmokeOff");
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
    public void SFEXT_O_TakeOwnership()
    {
        if (Smoking)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetSmokingOff");
        }
    }
    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if ((bool)SAVControl.GetProgramVariable("IsOwner"))
        {
            if (Smoking)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetSmokingOn");
            }
        }
    }
}
