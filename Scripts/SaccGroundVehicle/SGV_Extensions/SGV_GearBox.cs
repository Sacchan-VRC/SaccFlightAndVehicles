
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [DefaultExecutionOrder(1000)]//after DFUNCs
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SGV_GearBox : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SGVControl;
        public KeyCode GearUpKey = KeyCode.E;
        public KeyCode GearDownKey = KeyCode.Q;
        public KeyCode ClutchKey = KeyCode.C;
        [Tooltip("How far the stick has to be moved to change the gear")]
        public float GearChangeDistance = .7f;
        [Tooltip("Automatically change gears?")]
        public bool Automatic = false;
        [Tooltip("Allow the vehicle menu option to toggle gear functionality between automatic and manual")]
        public bool AllowMenuToToggleAutomatic = true;
        [Tooltip("How often the automatic mode can change gear")]
        public float AutomaticGearChangeDelay = .25f;
        [Tooltip("In automatic mode, when revs are above this percentage, the gear will increase")]
        public float GearChangeRevsUpper = .8f;
        [Tooltip("In automatic mode, when revs are below this percentage, the gear will decrease")]
        public float GearChangeRevsLower = .3f;
        [Tooltip("Use left controller stick to change gear instead?")]
        public bool GearsLeftController = false;
        [Tooltip("Use the left controller grip for clutch? Disable for right")]
        public bool ClutchLeftController = true;
        [Tooltip("Disable clutch input entirely")]
        public bool ClutchDisabled = false;
        [Tooltip("Multiply all the gears at once")]
        public float FinalDrive = 1f;
        public float[] GearRatios = { -.04f, 0, .04f, .08f, .12f, .16f, .2f };
        [Tooltip("If clutch input is above this amount, input is clamped to max")]
        public float UpperDeadZone = .95f;
        [Tooltip("If clutch input is bleow this amount, input is clamped to min")]
        public float LowerDeadZone = .05f;
        [Tooltip("Set this to your neutral gear")]
        [System.NonSerialized]
        public bool InvertVRGearChangeDirection;
        private SaccEntity EntityControl;
        [UdonSynced, FieldChangeCallback(nameof(CurrentGear))] public int _CurrentGear = 1;
        public int CurrentGear
        {
            set
            {
                SGVControl.SetProgramVariable("CurrentGear", value);
                SGVControl.SetProgramVariable("GearRatio", GearRatios[value] * FinalDrive);
                if (value == NeutralGear)
                { InNeutralGear = true; }
                else { InNeutralGear = false; }
                if (value == GearRatios.Length - 1)
                { InMaxGear = true; }
                else
                { InMaxGear = false; }
                if (value == 0)
                { InMinGear = true; }
                else
                { InMinGear = false; }
                if (value > _CurrentGear)
                { EntityControl.SendEventToExtensions("SFEXT_G_CarGearUp"); }
                else
                { EntityControl.SendEventToExtensions("SFEXT_G_CarGearDown"); }
                EntityControl.SendEventToExtensions("SFEXT_G_CarChangeGear");
                _CurrentGear = value;
            }
            get => _CurrentGear;
        }
        [Header("Debug")]
        [System.NonSerializedAttribute] public bool _ClutchOverrideOne = false;
        [System.NonSerializedAttribute] public float _ClutchOverride;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(ClutchOverride_))] public int ClutchOverride = 0;
        public int ClutchOverride_
        {
            set
            {
                if (value > 0)
                {
                    _ClutchOverride = 1f;
                    _ClutchOverrideOne = true;
                }
                else
                {
                    _ClutchOverride = 0f;
                    _ClutchOverrideOne = false;
                }
                ClutchOverride = value;
            }
            get => ClutchOverride;
        }
        private int NeutralGear;
        private bool InNeutralGear = false;
        private bool InMaxGear = false;
        private bool InMinGear = false;
        private bool Piloting = false;
        private bool InVR = false;
        private bool StickUpLastFrame;
        private bool StickDownLastFrame;
        [System.NonSerializedAttribute, FieldChangeCallback(nameof(AutomaticReversing))] public bool _AutomaticReversing = false;
        public bool AutomaticReversing
        {
            set
            {
                SetGear(NeutralGear);
                LastGearChangeTime = 0;
                _AutomaticReversing = value;
            }
            get => _AutomaticReversing;
        }
        private float LastGearChangeTime;
        private float RevLimiter;
        public void SFEXT_L_EntityStart()
        {
            EntityControl = (SaccEntity)SGVControl.GetProgramVariable("EntityControl");
            InVR = EntityControl.InVR;
            SGVControl.SetProgramVariable("GearRatio", GearRatios[_CurrentGear]);
            RevLimiter = (float)SGVControl.GetProgramVariable("RevLimiter");
            for (int i = 0; i < GearRatios.Length; i++)
            {
                if (GearRatios[i] == 0f)
                {
                    NeutralGear = i;
                    break;
                }
            }
            CurrentGear = NeutralGear;
        }
        private void LateUpdate()
        {
            if (Piloting)
            {
                Vector2 StickPos = Vector2.zero;
                float Trigger = 0;
                if (GearsLeftController)
                {
                    // StickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
                    StickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
                }
                else
                {
                    // StickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
                    StickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
                }
                if (StickPos.y > GearChangeDistance)
                {
                    if (!StickUpLastFrame)
                    {
                        //GEARUP / forward gear
                        if (Automatic)
                        {
                            if (_AutomaticReversing) { AutomaticReversing = false; }
                        }
                        else { if (InvertVRGearChangeDirection) GearDown(); else GearUp(); }
                        StickUpLastFrame = true;
                    }
                }
                else
                {
                    StickUpLastFrame = false;
                }
                if (StickPos.y < -GearChangeDistance)
                {
                    if (!StickDownLastFrame)
                    {
                        //GEARDown / reverse gear
                        if (Automatic)
                        {
                            if (!_AutomaticReversing) { AutomaticReversing = true; }
                        }
                        else
                        { if (InvertVRGearChangeDirection) GearUp(); else GearDown(); }
                        StickDownLastFrame = true;
                    }
                }
                else
                {
                    StickDownLastFrame = false;
                }
                if (Automatic)
                {

                    if (Input.GetKeyDown(GearUpKey))
                    {
                        AutomaticReversing = !AutomaticReversing;
                    }
                    if (Time.time - LastGearChangeTime > AutomaticGearChangeDelay)
                    {
                        float normRevs = (float)SGVControl.GetProgramVariable("Revs") / RevLimiter;
                        if ((!AutomaticReversing && !InMaxGear && normRevs > GearChangeRevsUpper) || (AutomaticReversing && !InNeutralGear && normRevs < GearChangeRevsLower))
                        {
                            if (!_ClutchOverrideOne)
                            {
                                GearUp();
                            }
                        }
                        else if ((!AutomaticReversing && !InNeutralGear && !InMinGear && normRevs < GearChangeRevsLower) || (AutomaticReversing && !InMinGear && normRevs > GearChangeRevsUpper))
                        {
                            GearDown();
                        }
                    }
                    SGVControl.SetProgramVariable("Clutch", _ClutchOverride);
                }
                else
                {
                    float kbclutch = 0;
                    if (!ClutchDisabled)
                    {
                        if (ClutchLeftController)
                        {
                            Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger");
                        }
                        else
                        {
                            Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
                        }
                        kbclutch = Input.GetKey(ClutchKey) ? 1f : 0f;
                    }
                    if (Trigger > UpperDeadZone)
                    { Trigger = 1f; }
                    if (Trigger < LowerDeadZone)
                    { Trigger = 0f; }
                    SGVControl.SetProgramVariable("Clutch", Mathf.Max(Trigger, kbclutch, _ClutchOverride));

                    if (Input.GetKeyDown(GearUpKey))
                    {
                        GearUp();
                    }
                    if (Input.GetKeyDown(GearDownKey))
                    {
                        GearDown();
                    }
                }
            }
        }
        public void GearUp()
        {
            if (_CurrentGear < GearRatios.Length - 1)
            {
                CurrentGear++;
                LastGearChangeTime = Time.time;
                RequestSerialization();
            }
        }
        public void GearDown()
        {
            if (_CurrentGear != 0)
            {
                CurrentGear--;
                LastGearChangeTime = Time.time;
                RequestSerialization();
            }
        }
        public void SetGear(int newgear)
        {
            CurrentGear = Mathf.Clamp(newgear, 0, GearRatios.Length);
            LastGearChangeTime = Time.time;
            RequestSerialization();
        }
        public void SFEXT_O_PilotEnter()
        {
            Piloting = true;
            RequestSerialization();
        }
        public void SFEXT_O_PilotExit()
        {
            Piloting = false;
            AutomaticReversing = false;
        }
        public void SFEXT_G_PilotEnter()
        {
            gameObject.SetActive(true);
        }
        public void SFEXT_G_PilotExit()
        {
            CurrentGear = NeutralGear;
            gameObject.SetActive(false);
        }
        public void SFEXT_G_Explode()
        {
            CurrentGear = NeutralGear;
        }
        public void SFEXT_G_RespawnButton()
        {
            CurrentGear = NeutralGear;
        }
    }
}