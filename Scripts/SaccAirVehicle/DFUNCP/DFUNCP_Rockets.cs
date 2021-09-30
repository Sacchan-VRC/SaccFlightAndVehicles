
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class DFUNCP_Rockets : UdonSharpBehaviour
{
    [SerializeField] private SaccAirVehicle SAVControl;
    [SerializeField] private GameObject Rocket;
    [Tooltip("How long it takes to fully reload from empty in seconds. Can be inaccurate because it can only reload by integers per resupply")]
    [SerializeField] private float FullReloadTimeSec = 8;
    [SerializeField] private int NumRocket = 4;
    [Tooltip("How often rocket fires if the trigger is held down")]
    [SerializeField] private float RocketHoldDelay = 0.5f;
    [Tooltip("Minimum time between firing rockets")]
    [SerializeField] private float RocketDelay = 0f;
    [SerializeField] private Transform[] RocketLaunchPoints;
    [Tooltip("Transform of which its X scale scales with ammo")]
    [SerializeField] private Transform AmmoBar;
    private bool UseLeftTrigger = false;
    private float Trigger;
    private bool TriggerLastFrame;
    private int RocketPoint = 0;
    private float LastRocketDropTime = 0f;
    private int FullRockets;
    private float FullRocketsDivider;
    private Transform VehicleTransform;
    private float reloadspeed;
    private Vector3 AmmoBarScaleStart;
    private VRCPlayerApi localPlayer;
    private bool InVR;
    private bool IsOwner;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXTP_L_EntityStart()
    {
        FullRockets = NumRocket;
        reloadspeed = FullRockets / FullReloadTimeSec;
        FullRocketsDivider = 1f / (NumRocket > 0 ? NumRocket : 10000000);
        if (AmmoBar) { AmmoBarScaleStart = AmmoBar.localScale; }
        if (RocketHoldDelay < RocketDelay) { RocketHoldDelay = RocketDelay; }
        VehicleTransform = SAVControl.EntityControl.transform;

        localPlayer = Networking.LocalPlayer;
        if (localPlayer != null)
        {
            InVR = localPlayer.IsUserInVR();
        }
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
    public void SFEXTP_O_UserEnter()
    {
        if (!InVR)
        {
            DFUNC_Selected();
        }
    }
    public void SFEXTP_O_UserExit()
    {
        DFUNC_Deselected();
    }
    public void SFEXTP_G_Explode()
    {
        RocketPoint = 0;
        NumRocket = FullRockets;
    }
    public void SFEXTP_G_RespawnButton()
    {
        NumRocket = FullRockets;
        RocketPoint = 0;
    }
    public void SFEXTP_G_ReSupply()
    {
        if (NumRocket != FullRockets) { SAVControl.ReSupplied++; }
        NumRocket = (int)Mathf.Min(NumRocket + Mathf.Max(Mathf.Floor(reloadspeed), 1), FullRockets);
        RocketPoint = 0;
        if (AmmoBar) { AmmoBar.localScale = new Vector3((NumRocket * FullRocketsDivider) * AmmoBarScaleStart.x, AmmoBarScaleStart.y, AmmoBarScaleStart.z); }
    }
    private void Update()
    {
        float Trigger;
        if (UseLeftTrigger)
        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
        else
        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
        if (Trigger > 0.75 || (Input.GetKey(KeyCode.C)))
        {
            if (!TriggerLastFrame)
            {
                if (NumRocket > 0 && ((Time.time - LastRocketDropTime) > RocketDelay))
                {
                    LastRocketDropTime = Time.time;
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchRocket");
                }
            }
            else//launch every RocketHoldDelay
                if (NumRocket > 0 && ((Time.time - LastRocketDropTime) > RocketHoldDelay))
            {
                {
                    LastRocketDropTime = Time.time;
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchRocket");
                }
            }

            TriggerLastFrame = true;
        }
        else { TriggerLastFrame = false; }
    }
    public void LaunchRocket()
    {
        IsOwner = localPlayer.IsOwner(gameObject);
        if (NumRocket > 0) { NumRocket--; }
        if (AmmoBar) { AmmoBar.localScale = new Vector3((NumRocket * FullRocketsDivider) * AmmoBarScaleStart.x, AmmoBarScaleStart.y, AmmoBarScaleStart.z); }
        if (Rocket != null)
        {
            GameObject NewRocket = Object.Instantiate(Rocket);

            NewRocket.transform.SetPositionAndRotation(RocketLaunchPoints[RocketPoint].position, RocketLaunchPoints[RocketPoint].rotation);
            NewRocket.SetActive(true);
            NewRocket.GetComponent<Rigidbody>().velocity = SAVControl.CurrentVel;
            RocketPoint++;
            if (RocketPoint == RocketLaunchPoints.Length) RocketPoint = 0;
        }
    }
}
