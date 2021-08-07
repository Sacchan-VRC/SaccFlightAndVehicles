
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNCP_Rockets : UdonSharpBehaviour
{
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private PassengerFunctionsController PassengerFunctionsControl;
    [SerializeField] private GameObject Rocket;
    [Tooltip("How long it takes to fully reload from 0 in seconds. Can be inaccurate because it can only reload by integers per resupply")]
    [SerializeField] private float FullReloadTimeSec = 8;
    [SerializeField] private int NumRocket = 4;
    [SerializeField] private float RocketHoldDelay = 0.5f;
    [SerializeField] private float RocketDelay = 0f;
    [SerializeField] private Transform[] RocketLaunchPoints;
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
    private bool LeftDial = false;
    private int DialPosition = -999;
    private Vector3 AmmoBarScaleStart;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXTP_L_ECStart()
    {
        AmmoBarScaleStart = AmmoBar.localScale;
        FullRockets = NumRocket;
        if (RocketHoldDelay < RocketDelay) { RocketHoldDelay = RocketDelay; }
        FullRocketsDivider = 1f / (NumRocket > 0 ? NumRocket : 10000000);
        reloadspeed = FullRockets / FullReloadTimeSec;
        VehicleTransform = EngineControl.VehicleMainObj.transform;
    }
    public void SFEXTP_O_UserExit()
    {
        gameObject.SetActive(false);
        TriggerLastFrame = false;
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
        if (NumRocket != FullRockets)
        {
            EngineControl.ReSupplied++;
        }
        NumRocket = (int)Mathf.Min(NumRocket + Mathf.Max(Mathf.Floor(reloadspeed), 1), NumRocket);
        RocketPoint = 0;
        AmmoBar.localScale = new Vector3((NumRocket * FullRocketsDivider) * AmmoBarScaleStart.x, AmmoBarScaleStart.y, AmmoBarScaleStart.z);
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
        if (NumRocket > 0) { NumRocket--; }

        AmmoBar.localScale = new Vector3((NumRocket * FullRocketsDivider) * AmmoBarScaleStart.x, AmmoBarScaleStart.y, AmmoBarScaleStart.z);
        if (Rocket != null)
        {
            GameObject NewRocket = VRCInstantiate(Rocket);

            NewRocket.transform.SetPositionAndRotation(RocketLaunchPoints[RocketPoint].position, RocketLaunchPoints[RocketPoint].rotation);
            NewRocket.SetActive(true);
            NewRocket.GetComponent<Rigidbody>().velocity = EngineControl.CurrentVel;
            RocketPoint++;
            if (RocketPoint == RocketLaunchPoints.Length) RocketPoint = 0;
        }
    }
}
