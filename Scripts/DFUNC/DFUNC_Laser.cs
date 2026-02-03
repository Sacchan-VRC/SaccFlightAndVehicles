
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.UdonNetworkCalling;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_Laser : UdonSharpBehaviour
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
        [Tooltip("Gun object whos rotation is synced for shooting in the right direction")]
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

        [Tooltip("Send the boolean(AnimBoolName) true to the animator when selected?")]
        public bool DoAnimBool = false;
        [Tooltip("Animator bool that is true when this function is selected")]
        public string AnimBoolName = "LaserSelected";
        [Tooltip("Animator trigger that is set when a missile is launched")]
        public string AnimFiredTriggerName = string.Empty;
        private bool DoAnimFiredTrigger = false;
        [Tooltip("Should the boolean stay true if the pilot exits with it selected?")]
        public bool AnimBoolStayTrueOnExit;
        [Tooltip("Dropped bombs will be parented to this object, use if you happen to have some kind of moving origin system")]
        public Transform WorldParent;
        public bool StickATGScrToFace_DT = true;
        public float ATGScrDist = .5f;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
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
        private float TimeSinceSerialization;
        private int NumChildrenStart;
        private Quaternion AtGscreenStartRot;
        private Vector3 AtGscreenStartPos;
        [System.NonSerialized] public bool Using;
        public void SFEXT_L_EntityStart()
        {
            localPlayer = Networking.LocalPlayer;
            InEditor = localPlayer == null;
            InVR = EntityControl.InVR;
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

            AtGscreenStartRot = AtGScreen.transform.localRotation;
            AtGscreenStartPos = AtGScreen.transform.localPosition;
        }
        private GameObject InstantiateWeapon()
        {
            GameObject NewWeap = Instantiate(Bomb);
            NewWeap.transform.SetParent(transform);
            return NewWeap;
        }
        public void SFEXT_O_PilotEnter()
        {
            TriggerLastFrame = true;
            IsOwner = Using = true;
            InVR = EntityControl.InVR;
            if (Gunparticle_Gunner) { Gunparticle_Gunner.SetActive(true); }
        }
        byte numUsers;
        public void SFEXT_G_PilotEnter()
        {
            numUsers++;
            if (numUsers > 1) return;

            gameObject.SetActive(true);
        }
        public void SFEXT_O_PilotExit()
        {
            AtGScreen.SetActive(false);
            AtGCam.gameObject.SetActive(false);
            func_active = false;
            IsOwner = Using = false;
            if (Gunparticle_Gunner) { Gunparticle_Gunner.SetActive(false); }
            AtGScreen.transform.localRotation = AtGscreenStartRot;
            AtGScreen.transform.localPosition = AtGscreenStartPos;
            if (DoAnimBool)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOff), AnimBoolStayTrueOnExit); }
        }
        public void SFEXT_G_PilotExit()
        {
            numUsers--;
            if (numUsers != 0) return;
            else boolOnTimes = 0;

            gameObject.SetActive(false);
        }
        public void SFEXT_G_Explode()
        {
            GunRotation = Vector2.zero;
            if (DoAnimBool)
            { SetBoolOff(false); }
            if (func_active)
            { DFUNC_Deselected(); }
        }
        public void DFUNC_Selected()
        {
            AtGScreen.SetActive(true);
            AtGCam.gameObject.SetActive(true);
            TriggerLastFrame = true;
            func_active = true;
            gameObject.SetActive(true);
            if (DoAnimBool)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOn)); }
        }
        public void DFUNC_Deselected()
        {
            func_active = false;
            AtGScreen.SetActive(false);
            AtGCam.gameObject.SetActive(false);
            gameObject.SetActive(false);
            AtGScreen.transform.localRotation = AtGscreenStartRot;
            AtGScreen.transform.localPosition = AtGscreenStartPos;
            if (DoAnimBool)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOff), false); }
        }
        public void SFEXT_G_RespawnButton()
        {
            LaserBarrel.localRotation = Quaternion.identity;
            GunRotation = Vector2.zero;
            if (DoAnimBool)
            { SetBoolOff(false); }
        }
        public override void PostLateUpdate()
        {
            if (func_active)
            {
                float DeltaTime = Time.deltaTime;
                TimeSinceSerialization += DeltaTime;
                float Trigger;
                if (LeftDial)
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
                float deltaTime = Time.deltaTime;
                Quaternion newangle;
                if (InVR)
                {
                    if (LeftDial)
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
                    AtGCam.fieldOfView = Mathf.Clamp(Mathf.Lerp(AtGCam.fieldOfView, newzoom, 1.5f * deltaTime), 0.3f, 90);
                }
                else
                {
                    float newzoom = 80;
                    AtGCam.fieldOfView = Mathf.Clamp(Mathf.Lerp(AtGCam.fieldOfView, newzoom, 5f * deltaTime), 0.3f, 90); //zooming in is a bit slower than zooming out                       
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
                LaserBarrel.rotation = Quaternion.Slerp(LaserBarrel.rotation, newrot, 1 - Mathf.Pow(0.5f, 4 * Time.deltaTime));
            }
        }
        private void PullTrigger()
        {
            FiredTime = Time.time;
            if (TriggerFireDelay == 0)
            {
                FireLaser_Owner();
            }
            else
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(FireLaser_SoundEvent));
                SendCustomEventDelayedSeconds(nameof(FireLaser_Owner), TriggerFireDelay);
            }
        }
        public void FireLaser_Owner()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(FireLaser_Event));
        }
        [NetworkCallable]
        public void FireLaser_SoundEvent()
        {
            if (LaserFireSound) { LaserFireSound.PlayOneShot(LaserFireSound.clip); }
        }
        [NetworkCallable]
        public void FireLaser_Event()
        {
            //temporarily set IsOwner to the correct player so that the projectile gets the correct owner in cases where firer is not script owner
            bool swappedOwner = false;
            if ((NetworkCalling.CallingPlayer.isLocal && !IsOwner) || (!NetworkCalling.CallingPlayer.isLocal && IsOwner))
            {
                IsOwner = !IsOwner;
                swappedOwner = true;
            }
            if (TriggerFireDelay == 0) { FireLaser_SoundEvent(); }
            FireLaser();
            if (swappedOwner) IsOwner = !IsOwner;
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
                UdonSharpBehaviour USB = NewBomb.GetComponent<UdonSharpBehaviour>();
                if (USB)
                { USB.SendCustomEvent("EnableWeapon"); }
            }
            for (int i = 0; i < ToggleBoolDisabler.Length; i++)
            {
                bool animon = (bool)ToggleBoolDisabler[i].GetProgramVariable("AnimOn");
                if (animon)
                {
                    ToggleBoolDisabler[i].SendCustomEvent("SetBoolOff");
                }
            }
        }
        int boolOnTimes;
        public void SetBoolOn()
        {
            boolOnTimes++;
            if (boolOnTimes > 1) return;
            if (LaserAnimator) { LaserAnimator.SetBool(AnimBoolName, true); }
        }
        [NetworkCallable]
        public void SetBoolOff(bool LeaveOn)
        {
            boolOnTimes = Mathf.Max(boolOnTimes - 1, 0);
            if (boolOnTimes != 0 || LeaveOn) return;
            if (LaserAnimator) { LaserAnimator.SetBool(AnimBoolName, false); }
        }
        public void KeyboardInput()
        {
            if (EntityControl.VehicleSeats[EntityControl.MySeat].PassengerFunctions)
            {
                EntityControl.VehicleSeats[EntityControl.MySeat].PassengerFunctions.ToggleStickSelection(this);
            }
            else
            {
                EntityControl.ToggleStickSelection(this);
            }
        }
    }
}