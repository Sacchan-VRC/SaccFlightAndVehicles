using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using VRC.SDK3.UdonNetworkCalling;

namespace SaccFlightAndVehicles
{
    public class DFUNC_Bomb : UdonSharpBehaviour
    {
        [SerializeField] public UdonSharpBehaviour SAVControl;
        public Animator BombAnimator;
        public GameObject Bomb;
        [Tooltip("Desktop key for firing when selected")]
        public KeyCode FireKey = KeyCode.Space;
        [Tooltip("Desktop key for firing when not selected")]
        public KeyCode FireNowKey = KeyCode.None;
        [Tooltip("How many bombs to create at Start() so they don't have to be created later")]
        public int NumPreInstatiated = 5;
        [Tooltip("How long it takes to fully reload from empty in seconds. Can be inaccurate because it can only reload by integers per resupply")]
        public float FullReloadTimeSec = 8;
        public Text HUDText_Bomb_ammo;
        public TextMeshPro HUDText_Bomb_ammo_TMP;
        public TextMeshProUGUI HUDText_Bomb_ammo_TMPUGUI;
        public int NumBomb = 4;
        public int NumBomb_PerShot = 1;
        [Tooltip("Delay between bomb drops when holding the trigger")]
        public float BombHoldDelay = 0.5f;
        [Tooltip("Minimum delay between bomb drops")]
        public float BombDelay = 0f;
        [Tooltip("Points at which bombs appear, each succesive bomb appears at the next transform")]
        public Transform[] BombLaunchPoints;
        public AudioSource LaunchSound;
        public AudioSource LaunchSound_Interior;
        public AudioSource ReloadSound;
        [Tooltip("Play the Reload sound after BombDelay + this (negative values encouraged)")]
        public float ReloadSound_offset = 0f;
        public ParticleSystem LaunchParticle;
        public int LaunchParticle_num = 15;
        [Tooltip("Allow user to fire the weapon while the vehicle is on the ground taxiing?")]
        public bool AllowFiringWhenGrounded = false;
        [Tooltip("Disable the weapon if wind is enabled, to prevent people gaining an unfair advantage")]
        public bool DisallowFireIfWind = false;
        [Tooltip("How much the vehicle should be pushed back when dropping a 'bomb' (useful for making cannons)")]
        public float Recoil = 0f;
        [Tooltip("Backwards vector of this transform is the direction along which the recoil force is applied (backwards so it can default to VehicleTransform)")]
        public Transform RecoilDirection;
        public bool BombInheritVelocity = true;
        public bool DoAnimBool = false;
        [Tooltip("Animator bool that is true when this function is selected")]
        public string AnimBoolName = string.Empty;
        [Tooltip("Animator float that represents how many bombs are left")]
        public string AnimFloatName = string.Empty;
        [Tooltip("Animator trigger that is set when a bomb is dropped")]
        public string AnimFiredTriggerName = string.Empty;
        [Tooltip("Should the boolean stay true if the pilot exits with it selected?")]
        public bool AnimBoolStayTrueOnExit;
        [Tooltip("KeyboardInput function fires instantly instead of selecting the DFUNC")]
        public bool KeyboardInput_InstantFire = false;
        public bool HandHeld_UseEventToFire = false;
        [Tooltip("On desktop mode, fire even when not selected if OnPickupUseDown is pressed")]
        [SerializeField] bool DT_UseToFire;
        [Tooltip("Drop a bomb when vehicle explodes?")]
        [SerializeField] bool DropBombOnDeath;
        [Tooltip("Should the bomb instantly explode after being dropped on vehicle death? (can use to create a damaging vehicle explosion)")]
        [SerializeField] bool DropBombOnDeath_InstantExplode;
        private bool Held = false;
        [Tooltip("Dropped bombs will be parented to this object, use if you happen to have some kind of moving origin system")]
        public Transform WorldParent;
        public Camera AtGCam;
        [Tooltip("Set rotation and FOV of AtGCam when selected? (Do not use with TrajectoryPredictor)")]
        public GameObject[] EnableOnSelected;
        [Header("")]
        [Tooltip("Delegate firing sync to EXT_Turret script to sync shoot direction properly.\nIf using FireSyncDelegate, syncmode can be set to None")]
        public UdonBehaviour FireSyncDelegate;
        private float boolToggleTime;
        private bool AnimOn = false;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [System.NonSerializedAttribute] public SAV_PassengerFunctionsController PassengerFunctionsControl;
        private float Trigger;
        private bool TriggerLastFrame;
        private int BombPoint = 0;
        private float LastBombDropTime = -999f;
        [System.NonSerializedAttribute] public int FullBombs;
        private float FullBombsDivider;
        private Transform VehicleTransform;
        private Rigidbody VehicleRigid;
        private float reloadspeed;
        private bool Piloting = false;
        private bool Selected = false;
        private int NumChildrenStart;
        private bool DoAnimFiredTrigger = false;
        private VRCPlayerApi localPlayer;
        [System.NonSerializedAttribute] public Transform CenterOfMass;
        [System.NonSerializedAttribute] public bool IsOwner;
        bool inVR;
        private UdonSharpBehaviour SoundControl;
        public void SFEXT_L_EntityStart()
        {
            FullBombs = NumBomb;
            if (BombHoldDelay < BombDelay) { BombHoldDelay = BombDelay; }
            FullBombsDivider = 1f / (NumBomb > 0 ? NumBomb : 10000000);
            reloadspeed = FullBombs / FullReloadTimeSec;
            VehicleRigid = EntityControl.GetComponent<Rigidbody>();
            BombAnimator = EntityControl.GetComponent<Animator>();
            CenterOfMass = EntityControl.CenterOfMass;
            VehicleTransform = EntityControl.transform;
            localPlayer = Networking.LocalPlayer;
            IsOwner = EntityControl.IsOwner;
            if (!RecoilDirection) { RecoilDirection = VehicleTransform; }
            if (AnimFiredTriggerName != string.Empty) { DoAnimFiredTrigger = true; }
            EntityColliders = EntityControl.gameObject.GetComponentsInChildren<Collider>();
            StartEntityLayer = EntityControl.gameObject.layer;
            if (FireSyncDelegate) { FireSyncDelegate.SetProgramVariable("DelegateFireCallback", this); }
            SoundControl = EntityControl.GetExtention(GetUdonTypeName<SAV_SoundController>());
            if (!SoundControl)
                SoundControl = EntityControl.GetExtention(GetUdonTypeName<SGV_EffectsController>());

            UpdateAmmoVisuals();
            for (int i = 0; i < EnableOnSelected.Length; i++) { EnableOnSelected[i].SetActive(false); }

            NumChildrenStart = transform.childCount;
            if (Bomb)
            {
                for (int i = 0; i < NumPreInstatiated; i++)
                {
                    InstantiateWeapon();
                }
            }
        }
        private GameObject InstantiateWeapon()
        {
            GameObject NewWeap = Object.Instantiate(Bomb);
            NewWeap.transform.SetParent(transform);
            return NewWeap;
        }
        public void SFEXT_O_PilotEnter()
        {
            Piloting = true;
            inVR = EntityControl.InVR;
            UpdateAmmoVisuals();
            LastBombDropTime = Mathf.Max(LastBombDropTime, Time.time - BombHoldDelay + 0.5f);
        }
        private Collider[] EntityColliders;
        private int StartEntityLayer;
        public void SFEXT_G_PilotEnter()
        {
            gameObject.SetActive(true);
            if (EntityControl.EntityPickup)
            {
                if (EntityControl.Holding)
                {
                    EntityControl.gameObject.layer = 9;
                    foreach (Collider stngcol in EntityColliders)
                    { stngcol.isTrigger = true; }
                }
                else
                {
                    foreach (Collider stngcol in EntityColliders)
                    { stngcol.enabled = false; }
                }
            }
        }
        public void SFEXT_G_PilotExit()
        {
            DisableThisObject();
            if (DoAnimBool && !AnimBoolStayTrueOnExit && AnimOn)
            { SetBoolOff(); }
            if (EntityControl.EntityPickup)
            {
                EntityControl.gameObject.layer = StartEntityLayer;
                foreach (Collider stngcol in EntityColliders)
                {
                    stngcol.isTrigger = false;
                    stngcol.enabled = true;
                }
            }
        }
        public void SFEXT_G_OnPickup() { SFEXT_G_PilotEnter(); }
        public void SFEXT_G_OnDrop() { SFEXT_G_PilotExit(); }
        public void SFEXT_O_PilotExit()
        {
            Selected = false;
            Piloting = false;
            for (int i = 0; i < EnableOnSelected.Length; i++) { EnableOnSelected[i].SetActive(false); }
            if (AtGCam) { AtGCam.gameObject.SetActive(false); }
        }
        public void SFEXT_P_PassengerEnter()
        {
            UpdateAmmoVisuals();
        }
        public void DFUNC_Selected()
        {
            TriggerLastFrame = true;
            Selected = EntityControl.InVR || !KeyboardInput_InstantFire;
            if (DoAnimBool && !AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOn)); }
            for (int i = 0; i < EnableOnSelected.Length; i++) { EnableOnSelected[i].SetActive(true); }
            if (AtGCam)
            {
                AtGCam.gameObject.SetActive(true);
            }
        }
        public void DFUNC_Deselected()
        {
            Selected = false;
            PickupTrigger = 0;
            if (DoAnimBool && AnimOn)
            { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SetBoolOff)); }
            for (int i = 0; i < EnableOnSelected.Length; i++) { EnableOnSelected[i].SetActive(false); }
            if (AtGCam) { AtGCam.gameObject.SetActive(false); }
        }
        public void SFEXT_G_Explode()
        {
            BombPoint = 0;
            NumBomb = FullBombs;
            UpdateAmmoVisuals();
            if (DoAnimBool && AnimOn)
            { SetBoolOff(); }
            if (DropBombOnDeath && NumBomb > 0)
            {
                LaunchBombs_Event();
                NumBomb = FullBombs;
                BombPoint = 0;
            }
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
            if (SAVControl && NumBomb != FullBombs)
            { EntityControl.SetProgramVariable("ReSupplied", (int)EntityControl.GetProgramVariable("ReSupplied") + 1); }
            NumBomb = (int)Mathf.Min(NumBomb + Mathf.Max(Mathf.Floor(reloadspeed), 1), FullBombs);
            BombPoint = 0;
            UpdateAmmoVisuals();
        }
        public void SFEXT_G_ReArm() { SFEXT_G_ReSupply(); }
        public void SFEXT_O_TakeOwnership() { IsOwner = true; }
        public void SFEXT_O_LoseOwnership() { IsOwner = false; }
        private int PickupTrigger = 0;
        public void SFEXT_O_OnPickupUseDown()
        {
            if (HandHeld_UseEventToFire)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(LaunchBombs_Event));
                return;
            }
            PickupTrigger = 1;
            return;
        }
        void TryToFire()
        {
            if (NumBomb > 0 && (AllowFiringWhenGrounded || !SAVControl || !(bool)SAVControl.GetProgramVariable("Taxiing")) && ((Time.time - LastBombDropTime) > BombDelay))
            {
                LaunchBomb_Owner();
            }
        }
        private void LaunchBomb_Owner()
        {
            LastBombDropTime = Time.time;
            if (!FireSyncDelegate)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(LaunchBombs_Event));
            }
            else
            {
                FireSyncDelegate.SendCustomEvent("DelegateFire");
            }
        }
        public void DelegatedFire()
        {
            LaunchBombs_Event();
        }
        [NetworkCallable]
        public void LaunchBombs_Event()
        {
            for (int i = 0; i < NumBomb_PerShot; i++)
            {
                LaunchBomb();
            }
            if (EntityControl.InVehicle && ReloadSound) SendCustomEventDelayedSeconds(nameof(PlayReloadSound), ReloadSound_offset);
        }
        public void SFEXT_O_OnPickupUseUp()
        {
            PickupTrigger = 0;
        }
        public void SFEXT_O_OnPickup()
        {
            Held = true;
            SFEXT_O_PilotEnter();
            UpdateAmmoVisuals();
        }
        public void SFEXT_O_OnDrop()
        {
            Held = false;
            PickupTrigger = 0;
            SFEXT_O_PilotExit();
            DFUNC_Deselected();
        }
        public void UpdateAmmoVisuals()
        {
            if (BombAnimator && AnimFloatName != string.Empty) { BombAnimator.SetFloat(AnimFloatName, (float)NumBomb * FullBombsDivider); }
            if (HUDText_Bomb_ammo) { HUDText_Bomb_ammo.text = NumBomb.ToString("F0"); }
            if (HUDText_Bomb_ammo_TMP) { HUDText_Bomb_ammo_TMP.text = NumBomb.ToString("F0"); }
            if (HUDText_Bomb_ammo_TMPUGUI) { HUDText_Bomb_ammo_TMPUGUI.text = NumBomb.ToString("F0"); }
        }
        public void DisableThisObject()
        {
            gameObject.SetActive(false);
        }
        private void Update()
        {
            if (!(Piloting || Held)) return;
            if (Selected || Input.GetKey(FireNowKey) || (!inVR && DT_UseToFire))
            {
                float Trigger = 0;
                if (EntityControl.Holding || !inVR && DT_UseToFire)
                    Trigger = PickupTrigger;
                else if (Selected)
                {
                    if (LeftDial)
                    { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                    else
                    { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
                }
                if ((Trigger > 0.75 || Input.GetKey(FireKey) || Input.GetKey(FireNowKey)))
                {
                    if (!TriggerLastFrame)
                    {
                        if (DisallowFireIfWind)
                        {
                            if (SAVControl && ((Vector3)SAVControl.GetProgramVariable("FinalWind")).sqrMagnitude > 0f)
                            { return; }
                        }
                        TryToFire();
                    }
                    else if (NumBomb > 0 && ((Time.time - LastBombDropTime) > BombHoldDelay) && (AllowFiringWhenGrounded || (!SAVControl || !(bool)SAVControl.GetProgramVariable("Taxiing"))))
                    {///launch every BombHoldDelay
                        LaunchBomb_Owner();
                    }
                    TriggerLastFrame = true;
                }
                else { TriggerLastFrame = false; }
            }
            else { TriggerLastFrame = false; }
        }
        public void LaunchBomb()
        {
            LastBombDropTime = Time.time;
            if (NumBomb > 0) { NumBomb--; }
            if (BombAnimator && DoAnimFiredTrigger) { BombAnimator.SetTrigger(AnimFiredTriggerName); }
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
                Rigidbody BombRB = NewBomb.GetComponent<Rigidbody>();
                if (BombRB)
                {
                    if (EntityControl.IsOwner && IsOwner)// these can be different for passenger functions      
                    {
                        //set launch position relative to rigidbody instead of transform so the physics matches
                        Vector3 LocalLaunchPoint = EntityControl.transform.InverseTransformDirection(NewBomb.transform.position - EntityControl.transform.position);
                        BombRB.position = (VehicleRigid.rotation * LocalLaunchPoint) + VehicleRigid.position;
                        Quaternion WeaponRotDif = NewBomb.transform.rotation * Quaternion.Inverse(VehicleRigid.rotation);
                        BombRB.rotation = WeaponRotDif * VehicleRigid.rotation;
                    }
                    else
                    {
                        BombRB.position = NewBomb.transform.position;
                        BombRB.rotation = NewBomb.transform.rotation;
                    }
                }
                NewBomb.SetActive(true);
                if (BombInheritVelocity)
                {
                    if (BombRB)
                    {
                        if (SAVControl)
                        { BombRB.velocity = (Vector3)SAVControl.GetProgramVariable("CurrentVel"); }
                        else
                        { BombRB.velocity = VehicleRigid.velocity; }
                    }
                }
                UdonSharpBehaviour USB = NewBomb.GetComponent<UdonSharpBehaviour>();
                if (USB)
                {
                    USB.SendCustomEvent("EnableWeapon");
                    if (EntityControl.dead && DropBombOnDeath_InstantExplode)
                    {
                        USB.SendCustomEvent("Explode");
                    }
                }
                BombPoint++;
                if (BombPoint == BombLaunchPoints.Length) BombPoint = 0;
            }
            if (EntityControl.IsOwner && !Held && Recoil > 0)
            {
                if (!EntityControl.Piloting) { EntityControl.SendEventToExtensions("SFEXT_L_WakeUp"); }
                VehicleRigid.AddForceAtPosition(-RecoilDirection.forward * Recoil, RecoilDirection.position, ForceMode.VelocityChange);
            }
            if (LaunchSound_Interior)
            {
                if (SoundControl && (bool)SoundControl.GetProgramVariable("AllDoorsClosed"))
                {
                    LaunchSound_Interior.PlayOneShot(LaunchSound_Interior.clip);
                }
                else if (LaunchSound) { LaunchSound.PlayOneShot(LaunchSound.clip); }
            }
            else if (LaunchSound) { LaunchSound.PlayOneShot(LaunchSound.clip); }
            if (LaunchParticle) { LaunchParticle.Emit(LaunchParticle_num); }

            UpdateAmmoVisuals();
            if (IsOwner)
            { EntityControl.SendEventToExtensions("SFEXT_O_BombLaunch"); }
        }
        public void PlayReloadSound()
        {
            ReloadSound.PlayOneShot(ReloadSound.clip);
        }
        public void SetBoolOn()
        {
            boolToggleTime = Time.time;
            AnimOn = true;
            if (BombAnimator) { BombAnimator.SetBool(AnimBoolName, AnimOn); }
        }
        public void SetBoolOff()
        {
            boolToggleTime = Time.time;
            AnimOn = false;
            if (BombAnimator) { BombAnimator.SetBool(AnimBoolName, AnimOn); }
        }
        public void KeyboardInput()
        {
            if (KeyboardInput_InstantFire)
            {
                TryToFire();
            }
            else
            {
                if (PassengerFunctionsControl)
                {
                    if (LeftDial) PassengerFunctionsControl.ToggleStickSelectionLeft(this);
                    else PassengerFunctionsControl.ToggleStickSelectionRight(this);
                }
                else
                {
                    if (LeftDial) EntityControl.ToggleStickSelectionLeft(this);
                    else EntityControl.ToggleStickSelectionRight(this);
                }
            }
        }
    }
}