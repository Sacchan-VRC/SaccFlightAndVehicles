
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Bomb : UdonSharpBehaviour
{
    [SerializeField] private SaccAirVehicle SAVControl;
    [SerializeField] private Animator BombAnimator;
    [SerializeField] private GameObject Bomb;
    [Tooltip("How long it takes to fully reload from empty in seconds. Can be inaccurate because it can only reload by integers per resupply")]
    [SerializeField] private float FullReloadTimeSec = 8;
    [SerializeField] private Text HUDText_Bomb_ammo;
    [SerializeField] private int NumBomb = 4;
    [Tooltip("Delay between bomb drops when holding the trigger")]
    [SerializeField] private float BombHoldDelay = 0.5f;
    [Tooltip("Minimum delay between bomb drops")]
    [SerializeField] private float BombDelay = 0f;
    [Tooltip("Points at which bombs appear, each succesive bomb appears at the next transform")]
    [SerializeField] private Transform[] BombLaunchPoints;
    private SaccEntity EntityControl;
    private bool UseLeftTrigger = false;
    private float Trigger;
    private bool TriggerLastFrame;
    private int BombPoint = 0;
    private float LastBombDropTime = 0f;
    private int FullBombs;
    private float FullBombsDivider;
    private int BOMBLAUNCHED_STRING = Animator.StringToHash("bomblaunched");
    private int BOMBS_STRING = Animator.StringToHash("bombs");
    private Transform VehicleTransform;
    private float reloadspeed;
    private bool LeftDial = false;
    private int DialPosition = -999;
    private VRCPlayerApi localPlayer;
    private bool IsOwner;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXT_L_EntityStart()
    {
        FullBombs = NumBomb;
        if (BombHoldDelay < BombDelay) { BombHoldDelay = BombDelay; }
        FullBombsDivider = 1f / (NumBomb > 0 ? NumBomb : 10000000);
        reloadspeed = FullBombs / FullReloadTimeSec;
        BombAnimator = SAVControl.EntityControl.GetComponent<Animator>();
        VehicleTransform = SAVControl.EntityControl.transform;
        BombAnimator.SetFloat(BOMBS_STRING, (float)NumBomb * FullBombsDivider);
        EntityControl = SAVControl.EntityControl;
        localPlayer = Networking.LocalPlayer;

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
        TriggerLastFrame = false;
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
        TriggerLastFrame = false;
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
    public void SFEXT_G_ReSupply()
    {
        if (NumBomb != FullBombs) { SAVControl.ReSupplied++; }
        NumBomb = (int)Mathf.Min(NumBomb + Mathf.Max(Mathf.Floor(reloadspeed), 1), FullBombs);
        BombAnimator.SetFloat(BOMBS_STRING, (float)NumBomb * FullBombsDivider);
        BombPoint = 0;
        HUDText_Bomb_ammo.text = NumBomb.ToString("F0");
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
                if (NumBomb > 0 && !SAVControl.Taxiing && ((Time.time - LastBombDropTime) > BombDelay))
                {
                    LastBombDropTime = Time.time;
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchBomb");
                    if (SAVControl.IsOwner)
                    { EntityControl.SendEventToExtensions("SFEXT_O_BombLaunch"); }
                }
            }
            else//launch every BombHoldDelay
                if (NumBomb > 0 && ((Time.time - LastBombDropTime) > BombHoldDelay) && !SAVControl.Taxiing)
            {
                {
                    LastBombDropTime = Time.time;
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchBomb");
                    if (SAVControl.IsOwner)
                    { EntityControl.SendEventToExtensions("SFEXT_O_BombLaunch"); }
                }
            }

            TriggerLastFrame = true;
        }
        else { TriggerLastFrame = false; }
    }
    public void LaunchBomb()
    {
        IsOwner = localPlayer.IsOwner(gameObject);
        if (NumBomb > 0) { NumBomb--; }
        BombAnimator.SetTrigger(BOMBLAUNCHED_STRING);
        if (Bomb != null)
        {
            GameObject NewBomb = VRCInstantiate(Bomb);

            NewBomb.transform.SetPositionAndRotation(BombLaunchPoints[BombPoint].position, VehicleTransform.rotation);
            NewBomb.SetActive(true);
            NewBomb.GetComponent<Rigidbody>().velocity = SAVControl.CurrentVel;
            BombPoint++;
            if (BombPoint == BombLaunchPoints.Length) BombPoint = 0;
        }
        BombAnimator.SetFloat(BOMBS_STRING, (float)NumBomb * FullBombsDivider);
        HUDText_Bomb_ammo.text = NumBomb.ToString("F0");
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
        Debug.LogWarning("DFUNC_Bomb: Can't find self in dial functions");
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
}
