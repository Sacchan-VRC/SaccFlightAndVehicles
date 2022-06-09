
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[DefaultExecutionOrder(1000)]//after DFUNCs
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class SGV_GearBox : UdonSharpBehaviour
{
    public UdonSharpBehaviour SGVControl;
    [Tooltip("How far the stick has to be moved to change the gear")]
    public float GearChangeDistance = .7f;
    public bool LeftController = false;
    public bool ClutchLeftController = true;
    [Tooltip("Multiply all the gears at once")]
    public float FinalDrive = 1f;
    public float[] GearRatios = { -.04f, 0, .04f, .08f, .12f, .16f, .2f };
    [Tooltip("If input is above this amount, input is clamped to max")]
    public float UpperDeadZone = .95f;
    [Tooltip("If input is bleow this amount, input is clamped to min")]
    public float LowerDeadZone = .05f;
    [Tooltip("Set this to your neutral gear")]
    private SaccEntity EntityControl;
    [UdonSynced, FieldChangeCallback(nameof(CurrentGear))] public int _CurrentGear = 1;
    public int CurrentGear
    {
        set
        {
            SGVControl.SetProgramVariable("CurrentGear", value);
            SGVControl.SetProgramVariable("GearRatio", GearRatios[value] * FinalDrive);
            _CurrentGear = value;
            EntityControl.SendEventToExtensions("SFEXT_G_ChangeGear");
        }
        get => _CurrentGear;
    }
    [Header("Debug")]
    [System.NonSerializedAttribute] public float _ClutchOverride;
    [System.NonSerializedAttribute, FieldChangeCallback(nameof(ClutchOverride_))] public int ClutchOverride = 0;
    public int ClutchOverride_
    {
        set
        {
            _ClutchOverride = value > 0 ? 1f : 0f;
            ClutchOverride = value;
        }
        get => ClutchOverride;
    }
    private int NeutralGear;
    private bool InNeutralGear = false;
    private bool Piloting = false;
    private bool InVR = false;
    private bool StickUpLastFrame;
    private bool StickDownLastFrame;
    public void SFEXT_L_EntityStart()
    {
        EntityControl = (SaccEntity)SGVControl.GetProgramVariable("EntityControl");
        InVR = EntityControl.InVR;
        SGVControl.SetProgramVariable("GearRatio", GearRatios[_CurrentGear]);
        NeutralGear = _CurrentGear;
    }
    private void LateUpdate()
    {
        if (Piloting)
        {
            Vector2 StickPos = Vector2.zero;
            float Trigger = 0;
            if (LeftController)
            {
                // StickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickHorizontal");
                StickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryThumbstickVertical");
            }
            else
            {
                // StickPos.x = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
                StickPos.y = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryThumbstickVertical");
            }
            if (ClutchLeftController)
            {
                Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger");
            }
            else
            {
                Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
            }
            if (StickPos.y > GearChangeDistance)
            {
                if (!StickUpLastFrame)
                {
                    //GEARUP
                    GearUp();
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
                    //GEARDown
                    GearDown();
                    StickDownLastFrame = true;
                }
            }
            else
            {
                StickDownLastFrame = false;
            }
            float kbclutch = Input.GetKey(KeyCode.C) ? 1f : 0f;
            if (Trigger > UpperDeadZone)
            { Trigger = 1f; }
            if (Trigger < LowerDeadZone)
            { Trigger = 0f; }
            SGVControl.SetProgramVariable("Clutch", Mathf.Max(Trigger, kbclutch, _ClutchOverride));

            if (Input.GetKeyDown(KeyCode.E))
            {
                GearUp();
            }
            if (Input.GetKeyDown(KeyCode.Q))
            {
                GearDown();
            }
        }
    }
    public void GearUp()
    {
        if (_CurrentGear < GearRatios.Length - 1)
        {
            CurrentGear++;
            RequestSerialization();
        }
    }
    public void GearDown()
    {
        if (_CurrentGear != 0)
        {
            CurrentGear--;
            RequestSerialization();
        }
    }
    public void SFEXT_O_PilotEnter()
    {
        Piloting = true;
        RequestSerialization();
    }
    public void SFEXT_O_PilotExit()
    {
        Piloting = false;
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
