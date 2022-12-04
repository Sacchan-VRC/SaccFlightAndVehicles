
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNCP_Minigun : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public Transform VehicleTransform;
        [Tooltip("Transform that rotates the gun")]
        public Transform Minigun;
        [Tooltip("OPTIONAL: Use a separate transform for the pitch rotation")]
        public Transform MinigunPitch;
        [Tooltip("There is a separate particle system for doing damage that is only enabled for the user of the gun. This object is the parent of that particle system, is enabled when entering the seat, and disabled when exiting")]
        public Transform GunDamageParticle_Parent;
        [SerializeField][UdonSynced(UdonSyncMode.None)] private float GunAmmoInSeconds = 12;
        [Tooltip("How long it takes to fully reload from empty in seconds")]
        public float FullReloadTimeSec = 20;
        public string AnimatorFiringStringName;
        public Animator GunAnimator;
        [Tooltip("Transform of which its X scale scales with ammo")]
        public Transform AmmoBar;
        public KeyCode MinigunFireKey = KeyCode.Space;

        [Tooltip("Just use the direction that hand is pointing to aim?")]
        public bool Aim_HandDirection;
        [Tooltip("Limit the angle the gun can turn to?")]
        public bool LimitTurnAngle;
        public float AngleLimitLeft = 45f;
        public float AngleLimitRight = 45f;
        public float AngleLimitUp = 45f;
        public float AngleLimitDown = 45f;
        public bool AllowGroundedFiring = true;
        [Header("Projectile mode options:")]
        [Tooltip("The weapon fires a projectile?")]
        public bool UseProjectileMode;
        public GameObject Projectile;
        public int ProjectileAmmo = 30;
        [Tooltip("Minimum delay between firing")]
        public float FireDelay = 1f;
        [Tooltip("Delay between firing when holding the trigger")]
        public float FireHoldDelay = 1f;
        public Transform[] FirePoints;
        [Tooltip("Fired projectiles will be parented to this object, use if you happen to have some kind of moving origin system")]
        public Transform WorldParent;
        public AudioSource FireSound;
        public bool SendAnimTrigger;
        public string AnimTriggerName;
        private int ProjectileAmmoFULL;
        private int NumChildrenStart;
        private float LastFireTime;
        private bool TriggerLastFrame;
        [UdonSynced(UdonSyncMode.None)] private Vector2 GunRotation;
        [System.NonSerialized] SaccEntity EntityControl;
        [System.NonSerialized] bool IsOwner;
        private bool InVR;
        private VRCPlayerApi localPlayer;
        private bool UseLeftTrigger;
        private bool func_active;
        private bool Selected;
        private int NumBulletParticles;
        private float reloadspeed;
        private float FullGunAmmoInSeconds;
        private float TimeSinceSerialization;
        private bool firing;
        private Vector3 AmmoBarScaleStart;
        private Quaternion NonOwnerGunAngleSlerper;
        private bool Grounded;

        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        public void SFEXTP_L_EntityStart()
        {
            EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
            if (UseProjectileMode)
            {
                ProjectileAmmoFULL = ProjectileAmmo;
                NumChildrenStart = transform.childCount;
                reloadspeed = (float)ProjectileAmmoFULL / FullReloadTimeSec;
            }
            else
            {
                FullGunAmmoInSeconds = GunAmmoInSeconds;
                reloadspeed = FullGunAmmoInSeconds / FullReloadTimeSec;
                FullGunAmmoInSeconds = GunAmmoInSeconds;
                if (GunDamageParticle_Parent) { GunDamageParticle_Parent.gameObject.SetActive(false); }
            }
            if (AmmoBar) { AmmoBarScaleStart = AmmoBar.localScale; AmmoBar.gameObject.SetActive(false); }

            localPlayer = Networking.LocalPlayer;
            if (localPlayer != null)
            {
                InVR = localPlayer.IsUserInVR();
            }
            if (Projectile)
            {
                int NumToInstantiate = Mathf.Min(ProjectileAmmoFULL, 10);
                for (int i = 0; i < NumToInstantiate; i++)
                {
                    InstantiateWeapon();
                }
            }
        }
        private GameObject InstantiateWeapon()
        {
            GameObject NewWeap = VRCInstantiate(Projectile);
            NewWeap.transform.SetParent(transform);
            return NewWeap;
        }
        public void Activate() { gameObject.SetActive(true); }
        public void Deactivate() { gameObject.SetActive(false); }
        public void DFUNC_Selected()
        {
            Selected = true;
            gameObject.SetActive(true);
        }
        public void DFUNC_Deselected()
        {
            Selected = false;
            if (firing)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(GunStopFiring));
            }
        }
        public void SFEXTP_O_UserEnter()
        {
            func_active = true;
            TriggerLastFrame = true;
            if (!InVR) { DFUNC_Selected(); }
            if (GunDamageParticle_Parent) { GunDamageParticle_Parent.gameObject.SetActive(true); }
            AmmoBar.gameObject.SetActive(true);
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Activate));
            UpdateAmmoVisuals();
        }
        public void SFEXTP_O_UserExit()
        {
            func_active = false;
            Selected = false;
            if (GunDamageParticle_Parent) { GunDamageParticle_Parent.gameObject.SetActive(false); }
            AmmoBar.gameObject.SetActive(false);
            if (firing)
            {
                firing = false;
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(GunStopFiring));
            }
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Deactivate));
            NonOwnerGunAngleSlerper = Quaternion.Euler(new Vector3(GunRotation.x, GunRotation.y, 0));
        }
        public void SFEXTP_G_Explode()
        {
            GunAmmoInSeconds = FullGunAmmoInSeconds;
            ProjectileAmmo = ProjectileAmmoFULL;
            Minigun.localRotation = Quaternion.identity;
            GunRotation = Vector2.zero;
            GunStopFiring();
            Selected = false;
            if (GunDamageParticle_Parent) { GunDamageParticle_Parent.gameObject.SetActive(false); }
            gameObject.SetActive(false);
        }
        public void SFEXTP_G_RespawnButton()
        {
            GunAmmoInSeconds = FullGunAmmoInSeconds;
            Minigun.localRotation = Quaternion.identity;
            GunRotation = Vector2.zero;
        }
        public void SFEXTP_G_ReSupply()
        {
            if (UseProjectileMode)
            {
                if (GunAmmoInSeconds != FullGunAmmoInSeconds) { SAVControl.SetProgramVariable("ReSupplied", (int)SAVControl.GetProgramVariable("ReSupplied") + 1); }
                GunAmmoInSeconds = Mathf.Min(GunAmmoInSeconds + reloadspeed, FullGunAmmoInSeconds);
            }
            else
            {
                if (ProjectileAmmo != ProjectileAmmoFULL) { SAVControl.SetProgramVariable("ReSupplied", (int)SAVControl.GetProgramVariable("ReSupplied") + 1); }
                ProjectileAmmo = (int)Mathf.Min(ProjectileAmmo + Mathf.Max(Mathf.Floor(reloadspeed), 1), ProjectileAmmoFULL);
                UpdateAmmoVisuals();
            }
        }
        public void UpdateAmmoVisuals()
        {
            if (UseProjectileMode)
            {
                if (AmmoBar) { AmmoBar.localScale = new Vector3(((float)ProjectileAmmo / (float)ProjectileAmmoFULL) * AmmoBarScaleStart.x, AmmoBarScaleStart.y, AmmoBarScaleStart.z); }
            }
            else
            {
                if (AmmoBar) { AmmoBar.localScale = new Vector3((GunAmmoInSeconds / FullGunAmmoInSeconds) * AmmoBarScaleStart.x, AmmoBarScaleStart.y, AmmoBarScaleStart.z); }
            }
        }
        private void LateUpdate()
        {
            float DeltaTime = Time.deltaTime;
            if (func_active)
            {
                if (Selected)
                {
                    float Trigger;
                    if (UseLeftTrigger)
                    { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                    else
                    { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
                    if (UseProjectileMode)
                    {
                        if ((Trigger > 0.75 || (Input.GetKey(MinigunFireKey))) && (AllowGroundedFiring || !Grounded))
                        {
                            if (!TriggerLastFrame)
                            {
                                if (ProjectileAmmo > 0 && ((Time.time - LastFireTime) > FireDelay))
                                {
                                    LastFireTime = Time.time;
                                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(FireGun));
                                }
                            }
                            else if (ProjectileAmmo > 0 && ((Time.time - LastFireTime) > FireHoldDelay))
                            {//launch every FireHoldDelay
                                LastFireTime = Time.time;
                                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(FireGun));
                            }
                            TriggerLastFrame = true;
                        }
                        else { TriggerLastFrame = false; }
                    }
                    else
                    {
                        if (((Trigger > 0.75 || (Input.GetKey(MinigunFireKey))) && GunAmmoInSeconds > 0) && (AllowGroundedFiring || !Grounded))
                        {
                            if (!firing)
                            {
                                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(GunStartFiring));
                                firing = true;
                            }
                            GunAmmoInSeconds = Mathf.Max(GunAmmoInSeconds - DeltaTime, 0);
                        }
                        else
                        {
                            if (firing)
                            {
                                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(GunStopFiring));
                                firing = false;
                            }
                        }
                    }
                }
                else
                {
                    if (firing)
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(GunStopFiring));
                        firing = false;
                    }
                }
                if (InVR)
                {
                    if (Aim_HandDirection)
                    {
                        if (UseLeftTrigger)
                        { Minigun.rotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation * Quaternion.Euler(0, 60, 0); }
                        else
                        { Minigun.rotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation * Quaternion.Euler(0, 60, 0); }
                    }
                    else
                    {
                        Vector3 lookpoint;
                        if (UseLeftTrigger)
                        {
                            lookpoint = ((Minigun.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position) * 500) + Minigun.position;
                        }
                        else
                        {
                            lookpoint = ((Minigun.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position) * 500) + Minigun.position;
                        }
                        Minigun.LookAt(lookpoint, VehicleTransform.up);
                    }
                }
                else
                {
                    Minigun.rotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
                }
                if (LimitTurnAngle)
                {
                    float AngleHor = Minigun.localEulerAngles.y;
                    if (AngleHor > 180) { AngleHor -= 360; }
                    float AngleVert = Minigun.localEulerAngles.x;
                    if (AngleVert > 180) { AngleVert -= 360; }

                    if (AngleHor > 0)
                    {
                        if (AngleHor > AngleLimitRight)
                        {
                            Minigun.localRotation = Quaternion.Euler(new Vector3(AngleVert, AngleLimitRight, 0));
                        }
                    }
                    else
                    {
                        if (AngleHor < -AngleLimitLeft)
                        {
                            Minigun.localRotation = Quaternion.Euler(new Vector3(AngleVert, -AngleLimitLeft, 0));
                        }
                    }
                    AngleHor = Minigun.localEulerAngles.y;
                    if (AngleHor > 180) { AngleHor -= 360; }

                    if (AngleVert > 0)
                    {
                        if (AngleVert > AngleLimitDown)
                        {
                            Minigun.localRotation = Quaternion.Euler(new Vector3(AngleLimitDown, AngleHor, 0));
                        }
                    }
                    else
                    {
                        if (AngleVert < -AngleLimitUp)
                        {
                            Minigun.localRotation = Quaternion.Euler(new Vector3(-AngleLimitUp, AngleHor, 0));
                        }
                    }
                }

                //set gun's roll to 0 and do pitch rotator if used
                Vector3 mgrotH = Minigun.localEulerAngles;
                if (MinigunPitch)
                {
                    mgrotH.z = 0;
                    Vector3 mgrotP = mgrotH;
                    mgrotH.x = 0;
                    mgrotP.y = 0;
                    MinigunPitch.localRotation = Quaternion.Euler(mgrotP);
                }
                else
                { mgrotH.z = 0; }
                Minigun.localRotation = Quaternion.Euler(mgrotH);

                if (TimeSinceSerialization > .3f)
                {
                    TimeSinceSerialization = 0;
                    if (MinigunPitch)
                    {
                        GunRotation.x = MinigunPitch.rotation.eulerAngles.x;
                        GunRotation.y = MinigunPitch.rotation.eulerAngles.y;
                    }
                    else
                    {
                        GunRotation.x = Minigun.rotation.eulerAngles.x;
                        GunRotation.y = Minigun.rotation.eulerAngles.y;
                    }
                    RequestSerialization();
                }
                TimeSinceSerialization += DeltaTime;
                UpdateAmmoVisuals();
            }
            else
            {
                Quaternion mgrot = Quaternion.Euler(new Vector3(GunRotation.x, GunRotation.y, 0));
                NonOwnerGunAngleSlerper = Quaternion.Slerp(NonOwnerGunAngleSlerper, mgrot, 4 * DeltaTime);
                Minigun.rotation = NonOwnerGunAngleSlerper;
                Vector3 mgrotH = Minigun.localEulerAngles;
                if (MinigunPitch)
                {
                    mgrotH.x = 0;
                    mgrotH.z = 0;
                    Minigun.localRotation = Quaternion.Euler(mgrotH);
                    MinigunPitch.rotation = NonOwnerGunAngleSlerper;
                    Vector3 mgrotP = MinigunPitch.localEulerAngles;
                    mgrotP.y = 0;
                    mgrotP.z = 0;
                    MinigunPitch.localRotation = Quaternion.Euler(mgrotP);
                }
                else
                {
                    mgrotH.z = 0;
                    Minigun.localRotation = Quaternion.Euler(mgrotH);
                }
            }
        }
        public void FireGun()
        {
            IsOwner = localPlayer.IsOwner(gameObject);
            int fp = FirePoints.Length;
            if (ProjectileAmmo > 0) { ProjectileAmmo--; }
            for (int x = 0; x < fp; x++)
            {
                GameObject proj;
                if (transform.childCount - NumChildrenStart > 0)
                { proj = transform.GetChild(NumChildrenStart).gameObject; }
                else
                { proj = InstantiateWeapon(); }
                if (WorldParent) { proj.transform.SetParent(WorldParent); }
                else { proj.transform.SetParent(null); }
                proj.transform.SetPositionAndRotation(FirePoints[x].position, FirePoints[x].rotation);
                proj.SetActive(true);
                proj.GetComponent<Rigidbody>().velocity = (Vector3)SAVControl.GetProgramVariable("CurrentVel");
            }
            if (FireSound)
            {
                FireSound.pitch = Random.Range(.94f, 1.08f);
                FireSound.PlayOneShot(FireSound.clip);
            }
            UpdateAmmoVisuals();
            if (SendAnimTrigger) { GunAnimator.SetTrigger(AnimTriggerName); }
        }
        private void OnDisable()
        {
            if (func_active)
            {
                if (firing)
                {
                    SendCustomEventDelayedFrames(nameof(Disable_Stopfiring), 1);//because lateupdate runs for one more frame after this for some reason
                }
            }
        }
        public void Disable_Stopfiring()
        {
            firing = false;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(GunStopFiring));
        }
        public void GunStartFiring()
        {
            GunAnimator.SetBool(AnimatorFiringStringName, true);
        }
        public void GunStopFiring()
        {
            GunAnimator.SetBool(AnimatorFiringStringName, false);
        }
        public void SFEXTP_G_TouchDown()
        {
            Grounded = true;
        }
        public void SFEXTP_G_TouchDownWater()
        {
            Grounded = true;
        }
        public void SFEXTP_G_TakeOff()
        {
            Grounded = false;
        }
    }
}