
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
        public Transform Minigun;
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
        [UdonSynced(UdonSyncMode.None)] private Vector2 GunRotation;
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
        private float FullGunAmmoDivider;
        private Vector3 AmmoBarScaleStart;

        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        public void SFEXTP_L_EntityStart()
        {
            FullGunAmmoInSeconds = GunAmmoInSeconds;
            reloadspeed = FullGunAmmoInSeconds / FullReloadTimeSec;
            if (AmmoBar) { AmmoBarScaleStart = AmmoBar.localScale; }
            FullGunAmmoInSeconds = GunAmmoInSeconds;
            FullGunAmmoDivider = 1f / (FullGunAmmoInSeconds > 0 ? FullGunAmmoInSeconds : 10000000);
            GunDamageParticle_Parent.gameObject.SetActive(false);
            localPlayer = Networking.LocalPlayer;
            if (localPlayer != null)
            {
                InVR = localPlayer.IsUserInVR();
            }
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
            if (!InVR) { DFUNC_Selected(); }
            GunDamageParticle_Parent.gameObject.SetActive(true);
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Activate));
        }
        public void SFEXTP_O_UserExit()
        {
            func_active = false;
            Selected = false;
            GunDamageParticle_Parent.gameObject.SetActive(false);
            if (firing)
            {
                firing = false;
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(GunStopFiring));
            }
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Deactivate));
        }
        public void SFEXTP_G_Explode()
        {
            GunAmmoInSeconds = FullGunAmmoInSeconds;
            Minigun.localRotation = Quaternion.identity;
            GunRotation = Vector2.zero;
            GunStopFiring();
            Selected = false;
            GunDamageParticle_Parent.gameObject.SetActive(false);
            gameObject.SetActive(false);
            UpdateAmmoVisuals();
        }
        public void SFEXTP_G_RespawnButton()
        {
            GunAmmoInSeconds = FullGunAmmoInSeconds;
            Minigun.localRotation = Quaternion.identity;
            GunRotation = Vector2.zero;
            UpdateAmmoVisuals();
        }
        public void SFEXTP_G_ReSupply()
        {
            if (gameObject.activeInHierarchy)
            {
                if (func_active)
                {
                    if (GunAmmoInSeconds != FullGunAmmoInSeconds) { SAVControl.SetProgramVariable("ReSupplied", (int)SAVControl.GetProgramVariable("ReSupplied") + 1); }
                    GunAmmoInSeconds = Mathf.Min(GunAmmoInSeconds + reloadspeed, FullGunAmmoInSeconds);
                }
            }
            else
            {
                if (GunAmmoInSeconds != FullGunAmmoInSeconds) { SAVControl.SetProgramVariable("ReSupplied", (int)SAVControl.GetProgramVariable("ReSupplied") + 1); }
                GunAmmoInSeconds = Mathf.Min(GunAmmoInSeconds + reloadspeed, FullGunAmmoInSeconds);
            }
            UpdateAmmoVisuals();
        }
        public void UpdateAmmoVisuals()
        {
            if (AmmoBar) { AmmoBar.localScale = new Vector3((GunAmmoInSeconds * FullGunAmmoDivider) * AmmoBarScaleStart.x, AmmoBarScaleStart.y, AmmoBarScaleStart.z); }
        }
        private void LateUpdate()
        {
            float DeltaTime = Time.deltaTime;
            if (Selected)
            {
                float Trigger;
                if (UseLeftTrigger)
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                else
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
                if ((Trigger > 0.75 || (Input.GetKey(MinigunFireKey))) && GunAmmoInSeconds > 0)
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
            else
            {
                if (firing)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(GunStopFiring));
                    firing = false;
                }
            }
            if (func_active)
            {
                if (InVR)
                {
                    Vector3 lookpoint = ((Minigun.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position) * 500) + Minigun.position;
                    Minigun.LookAt(lookpoint, VehicleTransform.up);
                }
                else
                {
                    Minigun.rotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
                }
                if (TimeSinceSerialization > .3f)
                {
                    TimeSinceSerialization = 0;
                    GunRotation.x = Minigun.rotation.eulerAngles.x;
                    GunRotation.y = Minigun.rotation.eulerAngles.y;
                    RequestSerialization();
                }
                TimeSinceSerialization += DeltaTime;
            }
            else
            {
                Quaternion newrot = (Quaternion.Euler(new Vector3(GunRotation.x, GunRotation.y, 0)));
                Minigun.rotation = Quaternion.Slerp(Minigun.rotation, newrot, 4 * DeltaTime);
            }
            UpdateAmmoVisuals();
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
    }
}