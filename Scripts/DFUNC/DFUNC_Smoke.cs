
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_Smoke : UdonSharpBehaviour
    {
        [SerializeField] UdonSharpBehaviour SAVControl;
        [Tooltip("Material to change the color value of to match smoke color")]
        public Material SmokeColorIndicatorMaterial;
        public ParticleSystem[] DisplaySmoke;
        [Tooltip("HUD Smoke indicator")]
        public GameObject HUD_SmokeOnIndicator;
        [Tooltip("Object enabled when function is active (used on MFD)")]
        public GameObject Dial_Funcon;
        public bool AllowChangeColor = true;
        [UdonSynced, FieldChangeCallback(nameof(SmokeOn))] private bool _smokeon;
        public bool SmokeOn
        {
            set
            {
                _smokeon = value;
                SetSmoking(value);
            }
            get => _smokeon;
        }
        private SaccEntity EntityControl;
        private bool UseLeftTrigger = false;
        private VRCPlayerApi localPlayer;
        private bool TriggerLastFrame;
        private float SmokeHoldTime;
        private bool SetSmokeLastFrame;
        private Vector3 SmokeZeroPoint;
        private ParticleSystem.EmissionModule[] DisplaySmokeem;
        [UdonSynced, System.NonSerializedAttribute] public Vector3 SmokeColor = Vector3.one;
        [System.NonSerializedAttribute] private Vector3 SmokeColorLast = Vector3.one;
        [System.NonSerializedAttribute] public bool localSmoking = false;
        [System.NonSerializedAttribute] public Color SmokeColor_Color;
        private Vector3 TempSmokeCol = Vector3.zero;
        private bool Pilot;
        private bool Selected;
        private bool InEditor; private int DialPosition;
        private bool LeftDial;
        private float LastSerialization;
        private Transform ControlsRoot;
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        public void SFEXT_L_EntityStart()
        {
            localPlayer = Networking.LocalPlayer;
            InEditor = localPlayer == null;
            EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
            ControlsRoot = (Transform)SAVControl.GetProgramVariable("ControlsRoot");
            if (Dial_Funcon) Dial_Funcon.SetActive(false);
            int NumSmokes = DisplaySmoke.Length;
            DisplaySmokeem = new ParticleSystem.EmissionModule[NumSmokes];

            for (int x = 0; x < DisplaySmokeem.Length; x++)
            { DisplaySmokeem[x] = DisplaySmoke[x].emission; }
            FindSelf();
        }
        public void DFUNC_Selected()
        {
            TriggerLastFrame = true;
            SmokeHoldTime = 0;
            Selected = true;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetActive));
        }
        public void DFUNC_Deselected()
        {
            if (!localSmoking)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetNotActive));
            }
            Selected = false;
        }
        public void SFEXT_O_PilotEnter()
        {
            Pilot = true;
            if (Dial_Funcon) { Dial_Funcon.SetActive(localSmoking); }
        }
        public void SFEXT_O_PilotExit()
        {
            Pilot = false;
            Selected = false;
            gameObject.SetActive(false);
        }
        public void SFEXT_G_PilotExit()
        {
            SetNotActive();
        }
        public void SFEXT_P_PassengerEnter()
        {
            if (Dial_Funcon) Dial_Funcon.SetActive(localSmoking);
        }
        public void SFEXT_G_Explode()
        {
            SetNotActive();
        }
        public void SFEXT_G_RespawnButton()
        {
            SetNotActive();
        }
        public void SetActive()
        {
            gameObject.SetActive(true);
        }
        public void SetNotActive()
        {
            SmokeOn = false;
            gameObject.SetActive(false);
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
                    if (Trigger > 0.75 || (Input.GetKey(KeyCode.Space)))
                    {
                        //you can change smoke colour by holding down the trigger and waving your hand around. x/y/z = r/g/b
                        Vector3 HandPosSmoke = ControlsRoot.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
                        HandPosSmoke = ControlsRoot.InverseTransformDirection(HandPosSmoke);
                        if (!TriggerLastFrame)
                        {
                            SmokeZeroPoint = HandPosSmoke;
                            TempSmokeCol = SmokeColor;

                            SmokeOn = !SmokeOn;
                            RequestSerialization();
                            SmokeHoldTime = 0;
                        }
                        SmokeHoldTime += Time.deltaTime;
                        if (SmokeHoldTime > .4f && AllowChangeColor)
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
                if (AllowChangeColor)
                {
                    if (localSmoking)
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
                        if (SmokeColor != SmokeColorLast && (Time.time - LastSerialization > .5f))
                        {
                            RequestSerialization();
                            LastSerialization = Time.time;
                        }
                        SmokeColorLast = SmokeColor;
                    }
                    //Smoke Color Indicator
                    SmokeColorIndicatorMaterial.color = SmokeColor_Color;
                }
            }
            if (localSmoking && AllowChangeColor)
            {
                //everyone does this while smoke is active
                SmokeColor_Color = new Color(SmokeColor.x, SmokeColor.y, SmokeColor.z);
                foreach (ParticleSystem smoke in DisplaySmoke)
                {
                    var main = smoke.main;
                    main.startColor = new ParticleSystem.MinMaxGradient(SmokeColor_Color, SmokeColor_Color * .8f);
                }
            }
        }
        public void KeyboardInput()
        {
            if (LeftDial)
            {
                if (EntityControl.LStickSelection == DialPosition)
                { EntityControl.LStickSelection = -1; }
                else
                { EntityControl.LStickSelection = DialPosition; }
            }
            else
            {
                if (EntityControl.RStickSelection == DialPosition)
                { EntityControl.RStickSelection = -1; }
                else
                { EntityControl.RStickSelection = DialPosition; }
            }
        }
        public void SetSmoking(bool smoking)
        {
            localSmoking = smoking;
            if (HUD_SmokeOnIndicator) { HUD_SmokeOnIndicator.SetActive(smoking); }
            for (int x = 0; x < DisplaySmokeem.Length; x++)
            { DisplaySmokeem[x].enabled = smoking; }
            if (Dial_Funcon) Dial_Funcon.SetActive(smoking);
            if ((bool)SAVControl.GetProgramVariable("IsOwner"))
            {
                if (smoking)
                { EntityControl.SendEventToExtensions("SFEXT_G_SmokeOn"); }
                else
                { EntityControl.SendEventToExtensions("SFEXT_G_SmokeOff"); }
            }
        }
        public void SFEXT_O_TakeOwnership()
        {
            if (localSmoking)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetNotActive));
            }
        }
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if ((bool)SAVControl.GetProgramVariable("IsOwner"))
            {
                if (localSmoking)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetActive));
                }
            }
        }
        private void FindSelf()
        {
            int x = 0;
            foreach (UdonSharpBehaviour usb in EntityControl.Dial_Functions_R)
            {
                if (this == usb)
                {
                    DialPosition = x;
                    return;
                }
                x++;
            }
            LeftDial = true;
            x = 0;
            foreach (UdonSharpBehaviour usb in EntityControl.Dial_Functions_L)
            {
                if (this == usb)
                {
                    DialPosition = x;
                    return;
                }
                x++;
            }
            DialPosition = -999;
            Debug.LogWarning("DFUNC_AAM: Can't find self in dial functions");
        }
    }
}