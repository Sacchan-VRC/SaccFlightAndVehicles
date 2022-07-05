
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_Bomb : UdonSharpBehaviour
    {
        [SerializeField] public UdonSharpBehaviour SAVControl;
        public Animator BombAnimator;
        public GameObject Bomb;
        [Tooltip("How long it takes to fully reload from empty in seconds. Can be inaccurate because it can only reload by integers per resupply")]
        public float FullReloadTimeSec = 8;
        public Text HUDText_Bomb_ammo;
        public int NumBomb = 4;
        [Tooltip("Delay between bomb drops when holding the trigger")]
        public float BombHoldDelay = 0.5f;
        [Tooltip("Minimum delay between bomb drops")]
        public float BombDelay = 0f;
        [Tooltip("Points at which bombs appear, each succesive bomb appears at the next transform")]
        public Transform[] BombLaunchPoints;
        [Tooltip("Allow user to fire the weapon while the vehicle is on the ground taxiing?")]
        public bool AllowFiringWhenGrounded = false;
        public bool DoAnimBool = false;
        [Tooltip("Animator bool that is true when this function is selected")]
        public string AnimBoolName = "BombSelected";
        [Tooltip("Animator float that represents how many bombs are left")]
        public string AnimFloatName = "bombs";
        [Tooltip("Animator trigger that is set when a bomb is dropped")]
        public string AnimFiredTriggerName = "bomblaunched";
        [Tooltip("Should the boolean stay true if the pilot exits with it selected?")]
        public bool AnimBoolStayTrueOnExit;
        [Tooltip("Dropped bombs will be parented to this object, use if you happen to have some kind of moving origin system")]
        public Transform WorldParent;
        public Camera AtGCam;
        public GameObject AtGScreen;
        [UdonSynced, FieldChangeCallback(nameof(BombFire))] private ushort _BombFire;
        public ushort BombFire
        {
            set
            {
                if (value > _BombFire)//if _BombFire is higher locally, it's because a late joiner just took ownership or value was reset, so don't launch
                { LaunchBomb(); }
                _BombFire = value;
            }
            get => _BombFire;
        }
        private float boolToggleTime;
        private bool AnimOn = false;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        private bool UseLeftTrigger = false;
        private float Trigger;
        private bool TriggerLastFrame;
        private int BombPoint = 0;
        private float LastBombDropTime = -999f;
        [System.NonSerializedAttribute] public int FullBombs;
        private float FullBombsDivider;
        private Transform VehicleTransform;
        private float reloadspeed;
        private bool LeftDial = false;
        private bool Piloting = false;
        private bool OthersEnabled = false;
        private bool func_active = false;
        private int DialPosition = -999;
        private int NumChildrenStart;
        private VRCPlayerApi localPlayer;
        [System.NonSerializedAttribute] public Transform CenterOfMass;
        [System.NonSerializedAttribute] public bool IsOwner;
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        public void SFEXT_L_EntityStart()
        {
            FullBombs = NumBomb;
            if (BombHoldDelay < BombDelay) { BombHoldDelay = BombDelay; }
            FullBombsDivider = 1f / (NumBomb > 0 ? NumBomb : 10000000);
            reloadspeed = FullBombs / FullReloadTimeSec;
            EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
            BombAnimator = EntityControl.GetComponent<Animator>();
            CenterOfMass = EntityControl.CenterOfMass;
            VehicleTransform = EntityControl.transform;
            localPlayer = Networking.LocalPlayer;

            FindSelf();

            UpdateAmmoVisuals();

            NumChildrenStart = transform.childCount;
            if (Bomb)
            {
                int NumToInstantiate = Mathf.Min(FullBombs, 30);
                for (int i = 0; i < NumToInstantiate; i++)
                {
                    InstantiateWeapon();
                }
            }
        }
        private GameObject InstantiateWeapon()
        {
            GameObject NewWeap = VRCInstantiate(Bomb);
            NewWeap.transform.SetParent(transform);
            return NewWeap;
        }
        public void SFEXT_O_PilotEnter()
        {
            Piloting = true;
            if (HUDText_Bomb_ammo) { HUDText_Bomb_ammo.text = NumBomb.ToString("F0"); }
        }
        public void SFEXT_G_PilotExit()
        {
            if (OthersEnabled) { DisableForOthers(); }
            if (DoAnimBool && !AnimBoolStayTrueOnExit && AnimOn)
            { SetBoolOff(); }
        }
        public void SFEXT_O_PilotExit()
        {
            func_active = false;
            Piloting = false;
            gameObject.SetActive(false);
            if (AtGScreen) { AtGScreen.SetActive(false); }
            if (AtGCam) { AtGCam.gameObject.SetActive(false); }
        }
        public void SFEXT_P_PassengerEnter()
        {
            if (HUDText_Bomb_ammo) { HUDText_Bomb_ammo.text = NumBomb.ToString("F0"); }
        }
        public void DFUNC_Selected()
        {
            TriggerLastFrame = true;
            func_active = true;
            gameObject.SetActive(true);
            if (DoAnimBool && !AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOn)); }
            if (!OthersEnabled) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EnableForOthers)); }
            if (AtGScreen) AtGScreen.SetActive(true);
            if (AtGCam)
            {
                AtGCam.gameObject.SetActive(true);
                AtGCam.fieldOfView = 60;
                AtGCam.transform.localRotation = Quaternion.Euler(110, 0, 0);
            }
        }
        public void DFUNC_Deselected()
        {
            func_active = false;
            gameObject.SetActive(false);
            if (DoAnimBool && AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOff)); }
            if (OthersEnabled) { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(DisableForOthers)); }
            if (AtGScreen) { AtGScreen.SetActive(false); }
            if (AtGCam) { AtGCam.gameObject.SetActive(false); }
        }
        public void SFEXT_G_Explode()
        {
            BombPoint = 0;
            NumBomb = FullBombs;
            UpdateAmmoVisuals();
            if (DoAnimBool && AnimOn)
            { SetBoolOff(); }
        }
        public void SFEXT_G_RespawnButton()
        {
            NumBomb = FullBombs;
            UpdateAmmoVisuals();
            BombPoint = 0;
            if (DoAnimBool && AnimOn)
            { SetBoolOff(); }
        }
        public void SFEXT_G_ReSupply()
        {
            if (NumBomb != FullBombs)
            { SAVControl.SetProgramVariable("ReSupplied", (int)SAVControl.GetProgramVariable("ReSupplied") + 1); }
            NumBomb = (int)Mathf.Min(NumBomb + Mathf.Max(Mathf.Floor(reloadspeed), 1), FullBombs);
            BombPoint = 0;
            UpdateAmmoVisuals();
        }
        public void UpdateAmmoVisuals()
        {
            BombAnimator.SetFloat(AnimFloatName, (float)NumBomb * FullBombsDivider);
            if (HUDText_Bomb_ammo) { HUDText_Bomb_ammo.text = NumBomb.ToString("F0"); }
        }
        public void EnableForOthers()
        {
            if (!Piloting)
            { gameObject.SetActive(true); BombFire = 0; }
            OthersEnabled = true;
        }
        public void DisableForOthers()
        {
            if (!Piloting)
            { gameObject.SetActive(false); }
            OthersEnabled = false;
        }
        private void Update()
        {
            if (func_active)
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
                        if (NumBomb > 0 && (AllowFiringWhenGrounded || !(bool)SAVControl.GetProgramVariable("Taxiing")) && ((Time.time - LastBombDropTime) > BombDelay))
                        {
                            BombFire++;
                            RequestSerialization();
                            if ((bool)SAVControl.GetProgramVariable("IsOwner"))
                            { EntityControl.SendEventToExtensions("SFEXT_O_BombLaunch"); }
                        }
                    }
                    else if (NumBomb > 0 && ((Time.time - LastBombDropTime) > BombHoldDelay) && (AllowFiringWhenGrounded || !(bool)SAVControl.GetProgramVariable("Taxiing")))
                    {///launch every BombHoldDelay
                        BombFire++;
                        RequestSerialization();
                        if ((bool)SAVControl.GetProgramVariable("IsOwner"))
                        { EntityControl.SendEventToExtensions("SFEXT_O_BombLaunch"); }
                    }
                    TriggerLastFrame = true;
                }
                else { TriggerLastFrame = false; }
            }
        }
        public void LaunchBomb()
        {
            LastBombDropTime = Time.time;
            IsOwner = localPlayer.IsOwner(gameObject);
            if (NumBomb > 0) { NumBomb--; }
            BombAnimator.SetTrigger(AnimFiredTriggerName);
            if (Bomb)
            {
                GameObject NewBomb;
                if (transform.childCount - NumChildrenStart > 0)
                { NewBomb = transform.GetChild(NumChildrenStart).gameObject; }
                else
                { NewBomb = InstantiateWeapon(); }
                if (WorldParent) { NewBomb.transform.SetParent(WorldParent); }
                else { NewBomb.transform.SetParent(null); }
                NewBomb.transform.SetPositionAndRotation(BombLaunchPoints[BombPoint].position, BombLaunchPoints[BombPoint].rotation);
                NewBomb.SetActive(true);
                NewBomb.GetComponent<Rigidbody>().velocity = (Vector3)SAVControl.GetProgramVariable("CurrentVel");
                BombPoint++;
                if (BombPoint == BombLaunchPoints.Length) BombPoint = 0;
            }
            UpdateAmmoVisuals();
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
        public void SetBoolOn()
        {
            boolToggleTime = Time.time;
            AnimOn = true;
            BombAnimator.SetBool(AnimBoolName, AnimOn);
        }
        public void SetBoolOff()
        {
            boolToggleTime = Time.time;
            AnimOn = false;
            BombAnimator.SetBool(AnimBoolName, AnimOn);
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
}