
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_AGM : UdonSharpBehaviour
{
    [SerializeField] private bool UseLeftTrigger;
    [SerializeField] public EngineController EngineControl;
    [SerializeField] private Animator AGMAnimator;
    [SerializeField] private Camera AtGCam;
    [SerializeField] private Text HUDText_AGM_ammo;
    [SerializeField] private GameObject AtGScreen;
    [SerializeField] private GameObject Dial_Funcon;
    private bool Dial_FunconNULL = true;
    [System.NonSerializedAttribute] public bool AGMLocked;
    [System.NonSerializedAttribute] private int AGMUnlocking = 0;
    [System.NonSerializedAttribute] private float AGMUnlockTimer;
    [System.NonSerializedAttribute] public float AGMRotDif;
    [System.NonSerializedAttribute] public bool AGMUnlockNull;
    [System.NonSerializedAttribute] public bool AGMTargetLockNull;
    public AudioSource AGMLock;
    public AudioSource AGMUnlock;
    private bool TriggerLastFrame;
    private float TriggerTapTime;
    public GameObject AGM;
    public int NumAGM = 4;
    [System.NonSerializedAttribute] public int FullAGMs;
    public Transform AGMLaunchPoint;
    public LayerMask AGMTargetsLayer;
    private float FullAGMsDivider;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.None)] public Vector3 AGMTarget;
    private VRCPlayerApi localPlayer;
    private bool InVR;
    private bool InEditor;
    private Transform VehicleTransform;
    private Quaternion AGMCamRotSlerper;
    private Quaternion AGMCamLastFrame;
    private bool func_active;
    private int AGMLAUNCHED_STRING = Animator.StringToHash("agmlaunched");
    private int AGMS_STRING = Animator.StringToHash("AGMs");
    private bool LeftDial = false;
    private int DialPosition = -999;

    public void SFEXT_L_ECStart()
    {
        AGMUnlockNull = (AGMUnlock == null) ? true : false;
        AGMTargetLockNull = (AGMLock == null) ? true : false;
        FullAGMs = NumAGM;
        FullAGMsDivider = 1f / (NumAGM > 0 ? NumAGM : 10000000);
        localPlayer = EngineControl.localPlayer;
        InEditor = EngineControl.InEditor;
        VehicleTransform = EngineControl.VehicleTransform;
        AGMAnimator.SetFloat(AGMS_STRING, (float)NumAGM * FullAGMsDivider);
        Dial_FunconNULL = Dial_Funcon == null;
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(false);

        FindSelf();
    }
    public void SFEXT_O_PilotEnter()
    {
        AGMLocked = false;
        InVR = EngineControl.InVR;
        HUDText_AGM_ammo.text = NumAGM.ToString("F0");
    }
    public void SFEXT_P_PassengerEnter()
    {
        HUDText_AGM_ammo.text = NumAGM.ToString("F0");
    }
    public void SFEXT_O_PilotExit()
    {
        AGMLocked = false;
        AtGScreen.SetActive(false);
        AtGCam.gameObject.SetActive(false);
        gameObject.SetActive(false);
        func_active = false;
    }
    public void SFEXT_G_RespawnButton()
    {
        NumAGM = FullAGMs;
        AGMAnimator.SetFloat(AGMS_STRING, 1);
    }
    public void SFEXT_O_ReSupply()
    {
        NumAGM = (int)Mathf.Min(NumAGM + Mathf.Max(Mathf.Floor(FullAGMs / 5), 1), FullAGMs);
        AGMAnimator.SetFloat(AGMS_STRING, (float)NumAGM * FullAGMsDivider);
    }
    public void SFEXT_G_Explode()
    {
        NumAGM = FullAGMs;
        if (func_active)
        { DFUNC_Deselected(); }
    }
    public void DFUNC_Selected()
    {
        func_active = true;
        gameObject.SetActive(true);
    }
    public void DFUNC_Deselected()
    {
        func_active = false;
        AtGScreen.SetActive(false);
        AtGCam.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }
    //synced variables recieved while object is disabled do not get set until the object is enabled, 1 frame is fine.
    public void EnableForOthers()
    {
        gameObject.SetActive(true);
    }
    public void DisableForOthers()
    {
        gameObject.SetActive(false);
    }
    private void Update()
    {
        if (func_active)
        {
            float DeltaTime = Time.deltaTime;
            TriggerTapTime += DeltaTime;
            AGMUnlockTimer += DeltaTime * AGMUnlocking;//AGMUnlocking is 1 if it was locked and just pressed, else 0, (waits for double tap delay to disable)
            float Trigger;
            if (UseLeftTrigger)
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
            else
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
            if (AGMUnlockTimer > 0.4f && AGMLocked == true)
            {
                //disable for others because they no longer need to sync
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "DisableForOthers");
                AGMLocked = false;
                AGMUnlockTimer = 0;
                AGMUnlocking = 0;
                if (!Dial_FunconNULL) { Dial_Funcon.SetActive(false); }
                if (!AGMUnlockNull)
                { AGMUnlock.Play(); }
            }
            if (Trigger > 0.75 || (Input.GetKey(KeyCode.Space)))
            {
                if (!TriggerLastFrame)
                {//new button press
                    if (TriggerTapTime < 0.4f)
                    {//double tap detected
                        if (AGMLocked)
                        {//locked on, launch missile
                            if (NumAGM > 0 && !EngineControl.Taxiing)
                            {
                                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchAGM");
                                if (EngineControl.IsOwner)
                                { EngineControl.SendEventToExtensions("SFEXT_O_AGMLaunch", false); }
                            }
                            AGMUnlocking = 0;
                        }
                    }
                    else if (!AGMLocked)
                    {//lock onto a target
                        if (AtGCam != null)
                        {
                            //check for agmtargets to lcok to
                            float targetangle = 999;
                            RaycastHit lockpoint;
                            RaycastHit[] agmtargs = Physics.SphereCastAll(AtGCam.transform.position, 150, AtGCam.transform.forward, Mathf.Infinity, AGMTargetsLayer);
                            if (agmtargs.Length > 0)
                            {//found one or more, find lowest angle one
                                //find target with lowest angle from crosshair
                                foreach (RaycastHit target in agmtargs)
                                {
                                    Vector3 targetdirection = target.point - AtGCam.transform.position;
                                    float angle = Vector3.Angle(AtGCam.transform.forward, targetdirection);
                                    if (angle < targetangle)
                                    {
                                        //enable for others so they sync the variable
                                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "EnableForOthers");
                                        targetangle = angle;
                                        AGMTarget = target.collider.transform.position;
                                        AGMLocked = true;
                                        AGMUnlocking = 0;
                                        RequestSerialization();
                                        if (!Dial_FunconNULL) { Dial_Funcon.SetActive(false); }
                                    }
                                }
                            }
                            else
                            {//didn't find one, lock onto raycast point
                                Physics.Raycast(AtGCam.transform.position, AtGCam.transform.forward, out lockpoint, Mathf.Infinity, 133121 /* Default, Environment, and Walkthrough */, QueryTriggerInteraction.Ignore);
                                if (lockpoint.point != null)
                                {
                                    //enable for others so they sync the variable
                                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "EnableForOthers");
                                    if (!AGMUnlockNull)
                                    { AGMLock.Play(); }
                                    AGMTarget = lockpoint.point;
                                    AGMLocked = true;
                                    AGMUnlocking = 0;
                                    RequestSerialization();
                                    if (!Dial_FunconNULL) { Dial_Funcon.SetActive(false); }
                                }
                            }
                        }
                    }
                    else
                    {
                        TriggerTapTime = 0;
                        AGMUnlockTimer = 0;
                        AGMUnlocking = 1;
                    }
                }
                TriggerLastFrame = true;
            }
            else { TriggerLastFrame = false; }
            if (!AGMLocked)
            {
                Quaternion newangle;
                if (InVR)
                {
                    newangle = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation * Quaternion.Euler(0, 60, 0);
                }
                else if (!InEditor)//desktop mode
                {
                    newangle = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
                }
                else//editor
                {
                    newangle = VehicleTransform.rotation;
                }
                float ZoomLevel = AtGCam.fieldOfView / 90;
                AGMCamRotSlerper = Quaternion.Slerp(AGMCamRotSlerper, newangle, ZoomLevel * 220f * DeltaTime);

                if (AtGCam != null)
                {
                    AGMRotDif = Vector3.Angle(AtGCam.transform.rotation * Vector3.forward, AGMCamLastFrame * Vector3.forward);
                    // AGMRotDif = Vector3.Angle(AtGCam.transform.rotation * Vector3.forward, AGMCamRotSlerper * Vector3.forward);
                    AtGCam.transform.rotation = AGMCamRotSlerper;

                    Vector3 temp2 = AtGCam.transform.localRotation.eulerAngles;
                    temp2.z = 0;
                    AtGCam.transform.localRotation = Quaternion.Euler(temp2);
                }
                AGMCamLastFrame = newangle;
            }


            //AGMScreen
            float SmoothDeltaTime = Time.smoothDeltaTime;
            if (!AGMLocked)
            {
                AtGScreen.SetActive(true);
                AtGCam.gameObject.SetActive(true);
                //if turning camera fast, zoom out
                if (AGMRotDif < 2.5f)
                {
                    RaycastHit camhit;
                    Physics.Raycast(AtGCam.transform.position, AtGCam.transform.forward, out camhit, Mathf.Infinity, 1);
                    if (camhit.point != null)
                    {
                        //dolly zoom //Mathf.Atan(100 <--the 100 is the height of the camera frustrum at the target distance
                        float newzoom = Mathf.Clamp(2.0f * Mathf.Atan(100 * 0.5f / Vector3.Distance(gameObject.transform.position, camhit.point)) * Mathf.Rad2Deg, 1.5f, 90);
                        AtGCam.fieldOfView = Mathf.Clamp(Mathf.Lerp(AtGCam.fieldOfView, newzoom, 1.5f * SmoothDeltaTime), 0.3f, 90);
                    }
                }
                else
                {
                    float newzoom = 80;
                    AtGCam.fieldOfView = Mathf.Clamp(Mathf.Lerp(AtGCam.fieldOfView, newzoom, 3.5f * SmoothDeltaTime), 0.3f, 90); //zooming in is a bit slower than zooming out                       
                }
            }
            else
            {
                AtGScreen.SetActive(true);
                AtGCam.gameObject.SetActive(true);
                AtGCam.transform.LookAt(AGMTarget, EngineControl.VehicleMainObj.transform.up);

                RaycastHit camhit;
                Physics.Raycast(AtGCam.transform.position, AtGCam.transform.forward, out camhit, Mathf.Infinity, 1);
                if (camhit.point != null)
                {
                    //dolly zoom //Mathf.Atan(40 <--the 40 is the height of the camera frustrum at the target distance
                    AtGCam.fieldOfView = Mathf.Max(Mathf.Lerp(AtGCam.fieldOfView, 2.0f * Mathf.Atan(60 * 0.5f / Vector3.Distance(gameObject.transform.position, camhit.point)) * Mathf.Rad2Deg, 5 * SmoothDeltaTime), 0.3f);
                }
            }
        }
    }

    public void LaunchAGM()
    {
        if (NumAGM > 0) { NumAGM--; }
        AGMAnimator.SetTrigger(AGMLAUNCHED_STRING);
        if (AGM != null)
        {
            GameObject NewAGM = VRCInstantiate(AGM);
            if (!(NumAGM % 2 == 0))
            {
                Vector3 temp = AGMLaunchPoint.localPosition;
                temp.x *= -1;
                AGMLaunchPoint.localPosition = temp;
                NewAGM.transform.SetPositionAndRotation(AGMLaunchPoint.position, AGMLaunchPoint.transform.rotation);
                temp.x *= -1;
                AGMLaunchPoint.localPosition = temp;
            }
            else
            {
                NewAGM.transform.SetPositionAndRotation(AGMLaunchPoint.position, AGMLaunchPoint.transform.rotation);
            }
            NewAGM.SetActive(true);
            NewAGM.GetComponent<Rigidbody>().velocity = EngineControl.CurrentVel;
        }
        AGMAnimator.SetFloat(AGMS_STRING, (float)NumAGM * FullAGMsDivider);
        HUDText_AGM_ammo.text = NumAGM.ToString("F0");
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
