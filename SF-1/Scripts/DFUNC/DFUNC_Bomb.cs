
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Bomb : UdonSharpBehaviour
{
    [SerializeField] private bool UseLeftTrigger;
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private Animator BombAnimator;
    [SerializeField] private GameObject Bomb;
    [SerializeField] private Text HUDText_Bomb_ammo;
    private float Trigger;
    private bool TriggerLastFrame;
    public int NumBomb = 4;
    public float BombHoldDelay = 0.5f;
    public float BombDelay = 0f;
    public Transform[] BombLaunchPoints;
    [System.NonSerializedAttribute] public int BombPoint = 0;
    private float LastBombDropTime = 0f;
    [System.NonSerializedAttribute] public int FullBombs;
    private float FullBombsDivider;
    private int BOMBLAUNCHED_STRING = Animator.StringToHash("bomblaunched");
    private int BOMBS_STRING = Animator.StringToHash("bombs");
    private Transform VehicleTransform;
    private bool LeftDial = false;
    private int DialPosition = -999;
    public void SFEXT_L_ECStart()
    {
        FullBombs = NumBomb;
        if (BombHoldDelay < BombDelay) { BombHoldDelay = BombDelay; }
        FullBombsDivider = 1f / (NumBomb > 0 ? NumBomb : 10000000);
        BombAnimator = EngineControl.VehicleMainObj.GetComponent<Animator>();
        VehicleTransform = EngineControl.VehicleMainObj.transform;
        BombAnimator.SetFloat(BOMBS_STRING, (float)NumBomb * FullBombsDivider);

        FindSelf();

        HUDText_Bomb_ammo.text = NumBomb.ToString("F0");
    }
    public void SFEXT_O_PilotEnter()
    {
        HUDText_Bomb_ammo.text = NumBomb.ToString("F0");
    }
    public void SFEXT_O_PilotExit()
    {
        gameObject.SetActive(false);
    }
    public void SFEXT_P_PassengerEnter()
    {
        HUDText_Bomb_ammo.text = NumBomb.ToString("F0");
    }
    public void DFUNC_Selected()
    {
        gameObject.SetActive(true);
    }
    public void DFUNC_Deselected()
    {
        gameObject.SetActive(false);
    }
    public void SFEXT_G_Explode()
    {
        BombPoint = 0;
        NumBomb = FullBombs;
    }
    public void SFEXT_G_RespawnButton()
    {
        NumBomb = FullBombs;
        BombAnimator.SetFloat(BOMBS_STRING, 1);
        BombPoint = 0;
    }
    public void SFEXT_O_ReSupply()
    {
        NumBomb = (int)Mathf.Min(NumBomb + Mathf.Max(Mathf.Floor(FullBombs / 5), 1), FullBombs);

        BombAnimator.SetFloat(BOMBS_STRING, (float)NumBomb * FullBombsDivider);
        BombPoint = 0;
    }
    private void Update()
    {
        float Trigger;
        if (UseLeftTrigger)
        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
        else
        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
        if (Trigger > 0.75 || (Input.GetKey(KeyCode.Space)))
        {
            if (!TriggerLastFrame)
            {
                if (NumBomb > 0 && !EngineControl.Taxiing && ((Time.time - LastBombDropTime) > BombDelay))
                {
                    LastBombDropTime = Time.time;
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchBomb");
                    if (EngineControl.IsOwner)
                    { EngineControl.SendEventToExtensions("SFEXT_O_BombLaunch", false); }
                }
            }
            else//launch every BombHoldDelay
                if (NumBomb > 0 && ((Time.time - LastBombDropTime) > BombHoldDelay) && !EngineControl.Taxiing)
            {
                {
                    LastBombDropTime = Time.time;
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchBomb");
                    if (EngineControl.IsOwner)
                    { EngineControl.SendEventToExtensions("SFEXT_O_BombLaunch", false); }
                }
            }

            TriggerLastFrame = true;
        }
        else { TriggerLastFrame = false; }
    }
    public void LaunchBomb()
    {
        if (NumBomb > 0) { NumBomb--; }
        BombAnimator.SetTrigger(BOMBLAUNCHED_STRING);
        if (Bomb != null)
        {
            GameObject NewBomb = VRCInstantiate(Bomb);

            NewBomb.transform.SetPositionAndRotation(BombLaunchPoints[BombPoint].position, VehicleTransform.rotation);
            NewBomb.SetActive(true);
            NewBomb.GetComponent<Rigidbody>().velocity = EngineControl.CurrentVel;
            BombPoint++;
            if (BombPoint == BombLaunchPoints.Length) BombPoint = 0;
        }
        BombAnimator.SetFloat(BOMBS_STRING, (float)NumBomb * FullBombsDivider);
        HUDText_Bomb_ammo.text = NumBomb.ToString("F0");
    }
    private void FindSelf()
    {
        int x = 0;
        foreach (UdonSharpBehaviour usb in EngineControl.Dial_Functions_R)
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
        foreach (UdonSharpBehaviour usb in EngineControl.Dial_Functions_L)
        {
            if (this == usb)
            {
                DialPosition = x;
                return;
            }
            x++;
        }
        DialPosition = -999;
        return;
    }
    public void KeyboardInput()
    {
        if (LeftDial)
        {
            if (EngineControl.LStickSelection == DialPosition)
            { EngineControl.LStickSelection = -1; }
            else
            { EngineControl.LStickSelection = DialPosition; }
        }
        else
        {
            if (EngineControl.RStickSelection == DialPosition)
            { EngineControl.RStickSelection = -1; }
            else
            { EngineControl.RStickSelection = DialPosition; }
        }
    }
}
