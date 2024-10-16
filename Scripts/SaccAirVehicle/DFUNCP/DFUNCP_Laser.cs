﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class DFUNCP_Laser : UdonSharpBehaviour
{
    [SerializeField] public UdonSharpBehaviour SAVControl;
    public Animator LaserAnimator;
    [Tooltip("Optional projectile object to spawn")]
    public GameObject Bomb;
    [Tooltip("Min. time between shots")]
    public float FireDelay = 3;
    [Tooltip("How long after pressing the trigger the weapon fires")]
    public float TriggerFireDelay = 0;
    [Tooltip("Camera that renders onto the AtGScreen")]
    public Camera AtGCam;
    public Transform LaserBarrel;
    [Tooltip("Screen that displays target, that is enabled when selected")]
    public GameObject AtGScreen;
    public GameObject Gunparticle_Gunner;
    public GameObject Dial_Funcon;
    [Tooltip("Allow user to fire the weapon while the vehicle is on the ground taxiing?")]
    public bool AllowFiringWhenGrounded = false;
    public KeyCode FireKey = KeyCode.Space;
    public AudioSource LaserFireSound;
    [SerializeField] private UdonSharpBehaviour[] ToggleBoolDisabler;
    private bool AnimOn;

    [Tooltip("Send the boolean(AnimBoolName) true to the animator when selected?")]
    public bool DoAnimBool = false;
    [Tooltip("Animator bool that is true when this function is selected")]
    public string AnimBoolName = "LaserSelected";
    [Tooltip("Animator trigger that is set when a missile is launched")]
    public string AnimFiredTriggerName = string.Empty;
    private bool DoAnimFiredTrigger = false;
    /*     [Tooltip("Should the boolean stay true if the pilot exits with it selected?")]
        public bool AnimBoolStayTrueOnExit; */
    [Tooltip("Dropped bombs will be parented to this object, use if you happen to have some kind of moving origin system")]
    public Transform WorldParent;
    public bool StickATGScrToFace_DT = true;
    [UdonSynced(UdonSyncMode.None)] private bool LaserFireNow = false;
    public float ATGScrDist = .5f;
    private float boolToggleTime;
    [System.NonSerialized] public SaccFlightAndVehicles.SaccEntity EntityControl;
    public SaccFlightAndVehicles.SAV_PassengerFunctionsController PassengerFunctionsController;
    private bool UseLeftTrigger = false;
    [UdonSynced(UdonSyncMode.None)] private Vector2 GunRotation;
    [System.NonSerializedAttribute] public bool IsOwner;
    private float AGMRotDif;
    private bool TriggerLastFrame;
    private VRCPlayerApi localPlayer;
    private bool InVR;
    private bool InEditor;
    private Transform VehicleTransform;
    private Quaternion AGMCamRotSlerper;
    private Quaternion AGMCamRotLastFrame;
    private bool func_active;
    private float reloadspeed;
    private float FiredTime;
    private bool LeftDial = false;
    private float TimeSinceSerialization;
    private int DialPosition = -999;
    private int NumChildrenStart;
    private Quaternion AtGscreenStartRot;
    private Vector3 AtGscreenStartPos;
    private bool OthersEnabled;
    [System.NonSerialized] public bool Using;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXTP_L_EntityStart()
    {
        localPlayer = Networking.LocalPlayer;
        InEditor = localPlayer == null;
        EntityControl = (SaccFlightAndVehicles.SaccEntity)SAVControl.GetProgramVariable("EntityControl");
        VehicleTransform = EntityControl.transform;
        if (Dial_Funcon) Dial_Funcon.SetActive(false);
        NumChildrenStart = transform.childCount;
        if (AnimFiredTriggerName != string.Empty) { DoAnimFiredTrigger = true; }
        if (Bomb)
        {
            int NumToInstantiate = 1;
            for (int i = 0; i < NumToInstantiate; i++)
            {
                InstantiateWeapon();
            }
        }

        FindSelf();

        AtGscreenStartRot = AtGScreen.transform.localRotation;
        AtGscreenStartPos = AtGScreen.transform.localPosition;
    }
    private GameObject InstantiateWeapon()
    {
        GameObject NewWeap = Instantiate(Bomb);
        NewWeap.transform.SetParent(transform);
        return NewWeap;
    }
    public void SFEXTP_O_UserEnter()
    {
        TriggerLastFrame = true;
        IsOwner = Using = true;
        if (!InEditor) { InVR = localPlayer.IsUserInVR(); }
        if (!OthersEnabled) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EnableForOthers)); }
        if (DialPosition == -999) { DFUNC_Selected(); }
        if (Gunparticle_Gunner) { Gunparticle_Gunner.SetActive(true); }
    }
    public void SFEXTP_O_UserExit()
    {
        AtGScreen.SetActive(false);
        AtGCam.gameObject.SetActive(false);
        gameObject.SetActive(false);
        func_active = false;
        IsOwner = Using = false;
        if (OthersEnabled) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(DisableForOthers)); }
        if (DialPosition == -999) { DFUNC_Deselected(); }
        if (Gunparticle_Gunner) { Gunparticle_Gunner.SetActive(false); }
        AtGScreen.transform.localRotation = AtGscreenStartRot;
        AtGScreen.transform.localPosition = AtGscreenStartPos;
    }
    public void SFEXTP_G_Explode()
    {
        GunRotation = Vector2.zero;
        if (DoAnimBool && AnimOn)
        { SetBoolOff(); }
        if (func_active)
        { DFUNC_Deselected(); }
    }
    public void DFUNC_Selected()
    {
        AtGScreen.SetActive(true);
        AtGCam.gameObject.SetActive(true);
        TriggerLastFrame = true;
        func_active = true;
        OnEnableDeserializationBlocker = true;
        gameObject.SetActive(true);
        SendCustomEventDelayedSeconds(nameof(FireDisablerFalse), .1f);
        if (DoAnimBool && !AnimOn)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOn)); }
    }
    public void FireDisablerFalse() { OnEnableDeserializationBlocker = false; }
    public void DFUNC_Deselected()
    {
        func_active = false;
        AtGScreen.SetActive(false);
        AtGCam.gameObject.SetActive(false);
        gameObject.SetActive(false);
        AtGScreen.transform.localRotation = AtGscreenStartRot;
        AtGScreen.transform.localPosition = AtGscreenStartPos;
        if (DoAnimBool && AnimOn)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOff)); }
    }
    public void EnableForOthers()
    {
        if (!Using)
        {
            OnEnableDeserializationBlocker = true;
            gameObject.SetActive(true);
            SendCustomEventDelayedSeconds(nameof(FireDisablerFalse), .1f);
        }
        OthersEnabled = true;
    }
    public void DisableForOthers()
    {
        if (!Using)
        { gameObject.SetActive(false); }
        OthersEnabled = false;
    }
    public void SFEXTP_G_RespawnButton()
    {
        LaserBarrel.localRotation = Quaternion.identity;
        GunRotation = Vector2.zero;
    }
    public override void PostLateUpdate()
    {
        if (func_active)
        {
            float DeltaTime = Time.deltaTime;
            TimeSinceSerialization += DeltaTime;
            float Trigger;
            if (UseLeftTrigger)
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
            else
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

            if (Trigger > 0.75 || (Input.GetKey(FireKey)))
            {
                if (!TriggerLastFrame)
                {//new button press
                    if (Time.time - FiredTime > FireDelay)
                    {
                        if ((AllowFiringWhenGrounded || !(bool)SAVControl.GetProgramVariable("Taxiing")))
                        {
                            PullTrigger();
                        }
                    }
                }
                TriggerLastFrame = true;
            }
            else { TriggerLastFrame = false; }

            //AGMScreen
            float SmoothDeltaTime = Time.smoothDeltaTime;
            Quaternion newangle;
            if (InVR)
            {
                if (UseLeftTrigger)
                { newangle = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation * Quaternion.Euler(0, 60, 0); }
                else
                { newangle = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation * Quaternion.Euler(0, 60, 0); }
            }
            else if (!InEditor)//desktop mode
            {
                var head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                newangle = head.rotation;
                if (StickATGScrToFace_DT)
                {
                    AtGScreen.transform.position = head.position + ((newangle * Vector3.forward) * ATGScrDist);
                    AtGScreen.transform.rotation = newangle;
                }
            }
            else//editor
            {
                newangle = VehicleTransform.rotation;
            }
            float ZoomLevel = AtGCam.fieldOfView / 90;
            AGMCamRotSlerper = Quaternion.Slerp(AGMCamRotSlerper, newangle, ZoomLevel * 220f * DeltaTime);


            AGMRotDif = Vector3.Angle(AtGCam.transform.rotation * Vector3.forward, AGMCamRotLastFrame * Vector3.forward);
            AtGCam.transform.rotation = AGMCamRotSlerper;

            Vector3 temp2 = AtGCam.transform.localRotation.eulerAngles;
            temp2.z = 0;
            if (temp2.x > 90) { temp2.x = 0; }
            AtGCam.transform.localRotation = Quaternion.Euler(temp2);
            AGMCamRotLastFrame = newangle;
            LaserBarrel.rotation = AtGCam.transform.rotation;

            //if turning camera fast, zoom out
            if (AGMRotDif < 2.5f)
            {
                RaycastHit camhit;
                Physics.Raycast(AtGCam.transform.position, AtGCam.transform.forward, out camhit, Mathf.Infinity, 1);
                //dolly zoom //Mathf.Atan(100 <--the 100 is the height of the camera frustrum at the target distance
                float newzoom = Mathf.Clamp(2.0f * Mathf.Atan(100 * 0.5f / Vector3.Distance(gameObject.transform.position, camhit.point)) * Mathf.Rad2Deg, 1.5f, 90);
                AtGCam.fieldOfView = Mathf.Clamp(Mathf.Lerp(AtGCam.fieldOfView, newzoom, 1.5f * SmoothDeltaTime), 0.3f, 90);
            }
            else
            {
                float newzoom = 80;
                AtGCam.fieldOfView = Mathf.Clamp(Mathf.Lerp(AtGCam.fieldOfView, newzoom, 5f * SmoothDeltaTime), 0.3f, 90); //zooming in is a bit slower than zooming out                       
            }
            if (TimeSinceSerialization > .3f)
            {
                TimeSinceSerialization = 0;
                GunRotation.x = LaserBarrel.rotation.eulerAngles.x;
                GunRotation.y = LaserBarrel.rotation.eulerAngles.y;
                RequestSerialization();
            }
        }
        else
        {
            Quaternion newrot = (Quaternion.Euler(new Vector3(GunRotation.x, GunRotation.y, 0)));
            LaserBarrel.rotation = Quaternion.Slerp(LaserBarrel.rotation, newrot, 4 * Time.deltaTime);
        }
    }
    private void PullTrigger()
    {
        FiredTime = Time.time;
        for (int i = 0; i < ToggleBoolDisabler.Length; i++)
        {
            bool animon = (bool)ToggleBoolDisabler[i].GetProgramVariable("AnimOn");
            if (animon)
            {
                ToggleBoolDisabler[i].SendCustomEvent("SetBoolOff");
            }
        }
        if (LaserFireSound) { LaserFireSound.PlayOneShot(LaserFireSound.clip); }
        if (TriggerFireDelay == 0)
        {
            FireLaser_Owner();
        }
        else
        {
            SendCustomEventDelayedSeconds(nameof(FireLaser_Owner), TriggerFireDelay);
        }
    }
    public void FireLaser_Owner()
    {
        FireNextSerialization = true;
        RequestSerialization();
        FireLaser();
    }
    public void FireLaser()
    {
        if (DoAnimFiredTrigger) { LaserAnimator.SetTrigger(AnimFiredTriggerName); }
        if (Bomb)
        {
            GameObject NewBomb;
            if (transform.childCount - NumChildrenStart > 0)
            { NewBomb = transform.GetChild(NumChildrenStart).gameObject; }
            else
            { NewBomb = InstantiateWeapon(); }
            if (WorldParent) { NewBomb.transform.SetParent(WorldParent); }
            else { NewBomb.transform.SetParent(null); }
            NewBomb.transform.SetPositionAndRotation(LaserBarrel.position, LaserBarrel.rotation);
            NewBomb.SetActive(true);
            Rigidbody bombrigid = NewBomb.GetComponent<Rigidbody>();
            if (bombrigid)
            {
                bombrigid.velocity = (Vector3)SAVControl.GetProgramVariable("CurrentVel");
            }
        }
    }
    private void FindSelf()
    {
        if (!PassengerFunctionsController) { return; }
        int x = 0;
        foreach (UdonSharpBehaviour usb in PassengerFunctionsController.Dial_Functions_R)
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
        foreach (UdonSharpBehaviour usb in PassengerFunctionsController.Dial_Functions_L)
        {
            if (this == usb)
            {
                DialPosition = x;
                return;
            }
            x++;
        }
        DialPosition = -999;
        Debug.Log("DFUNCP_Laser: Can't find self in dial functions");
    }
    public void SetBoolOn()
    {
        boolToggleTime = Time.time;
        AnimOn = true;
        if (LaserAnimator) { LaserAnimator.SetBool(AnimBoolName, AnimOn); }
    }
    public void SetBoolOff()
    {
        boolToggleTime = Time.time;
        AnimOn = false;
        if (LaserAnimator) { LaserAnimator.SetBool(AnimBoolName, AnimOn); }
    }
    public void KeyboardInput()
    {
        if (DialPosition == -999) return;
        if (LeftDial)
        {
            if (PassengerFunctionsController.LStickSelection == DialPosition)
            { PassengerFunctionsController.LStickSelection = -1; }
            else
            { PassengerFunctionsController.LStickSelection = DialPosition; }
        }
        else
        {
            if (PassengerFunctionsController.RStickSelection == DialPosition)
            { PassengerFunctionsController.RStickSelection = -1; }
            else
            { PassengerFunctionsController.RStickSelection = DialPosition; }
        }
    }
    private bool FireNextSerialization = false;
    public override void OnPreSerialization()
    {
        if (FireNextSerialization)
        {
            FireNextSerialization = false;
            LaserFireNow = true;
        }
        else { LaserFireNow = false; }
    }
    bool OnEnableDeserializationBlocker;
    public override void OnDeserialization()
    {
        if (OnEnableDeserializationBlocker) { OnEnableDeserializationBlocker = false; return; }
        if (LaserFireNow) { FireLaser(); }
    }
}
