
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNCP_Minigun : UdonSharpBehaviour
{
    [SerializeField] private SaccAirVehicle SAVControl;
    [SerializeField] private Transform VehicleTransform;
    [SerializeField] private Transform Minigun;
    [Tooltip("There is a separate particle system for doing damage that is only enabled for the user of the gun. This object is the parent of that particle system, is enabled when entering the seat, and disabled when exiting")]
    [SerializeField] private Transform GunDamageParticle_Parent;
    [SerializeField] [UdonSynced(UdonSyncMode.None)] private float GunAmmoInSeconds = 12;
    [Tooltip("How long it takes to fully reload from empty in seconds")]
    [SerializeField] private float FullReloadTimeSec = 20;
    [SerializeField] private string AnimatorFiringStringName;
    [SerializeField] private Animator GunAnimator;
    [Tooltip("Transform of which its X scale scales with ammo")]
    [SerializeField] private Transform AmmoBar;
    private bool AmmoBarNULL = true;
    private bool TriggerLastFrame;
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
    private int GUNFIRING_STRING;
    private float FullGunAmmoDivider;
    private Vector3 AmmoBarScaleStart;

    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void SFEXTP_L_EntityStart()
    {
        FullGunAmmoInSeconds = GunAmmoInSeconds;
        reloadspeed = FullGunAmmoInSeconds / FullReloadTimeSec;
        AmmoBarNULL = AmmoBar == null;
        if (!AmmoBarNULL) { AmmoBarScaleStart = AmmoBar.localScale; }
        FullGunAmmoInSeconds = GunAmmoInSeconds;
        FullGunAmmoDivider = 1f / (FullGunAmmoInSeconds > 0 ? FullGunAmmoInSeconds : 10000000);
        GUNFIRING_STRING = Animator.StringToHash(AnimatorFiringStringName);
        GunDamageParticle_Parent.gameObject.SetActive(false);
        localPlayer = Networking.LocalPlayer;
        if (localPlayer != null)
        {
            InVR = localPlayer.IsUserInVR();
        }
    }
    public void Activate()
    {
        gameObject.SetActive(true);
    }
    public void Deactivate()
    {
        gameObject.SetActive(false);
    }
    public void DFUNC_Selected()
    {
        Selected = true;
        gameObject.SetActive(true);
    }
    public void DFUNC_Deselected()
    {
        Selected = false;
        TriggerLastFrame = false;
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
        TriggerLastFrame = false;
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
    }
    public void SFEXTP_G_RespawnButton()
    {
        GunAmmoInSeconds = FullGunAmmoInSeconds;
        Minigun.localRotation = Quaternion.identity;
        GunRotation = Vector2.zero;
    }
    public void SFEXTP_G_ReSupply()
    {
        if (Selected)
        {
            if (GunAmmoInSeconds != FullGunAmmoInSeconds) { SAVControl.ReSupplied++; }
            GunAmmoInSeconds = Mathf.Min(GunAmmoInSeconds + reloadspeed, FullGunAmmoInSeconds);
        }
    }
    private void LateUpdate()
    {
        float DeltaTime = Time.deltaTime;
        if (Selected)
        {
            TimeSinceSerialization += DeltaTime;
            float Trigger;
            if (UseLeftTrigger)
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
            else
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
            if ((Trigger > 0.75 || (Input.GetKey(KeyCode.Space))) && GunAmmoInSeconds > 0)
            {
                if (!firing)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(GunStartFiring));
                    firing = true;
                }
                GunAmmoInSeconds = Mathf.Max(GunAmmoInSeconds - DeltaTime, 0);

                TriggerLastFrame = true;
            }
            else
            {
                if (firing)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(GunStopFiring));
                    firing = false;
                    TriggerLastFrame = false;
                }
            }
            if (TimeSinceSerialization > 1f)
            {
                TimeSinceSerialization = 0;
                RequestSerialization();
            }
        }
        else
        {
            if (firing)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(GunStopFiring));
                firing = false;
            }
            TriggerLastFrame = false;
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


        if (!AmmoBarNULL) AmmoBar.localScale = new Vector3((GunAmmoInSeconds * FullGunAmmoDivider) * AmmoBarScaleStart.x, AmmoBarScaleStart.y, AmmoBarScaleStart.z);
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
        TriggerLastFrame = false;
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(GunStopFiring));
    }
    public void GunStartFiring()
    {
        GunAnimator.SetBool(GUNFIRING_STRING, true);
    }
    public void GunStopFiring()
    {
        GunAnimator.SetBool(GUNFIRING_STRING, false);
    }
}
