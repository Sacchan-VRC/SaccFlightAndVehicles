
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNCP_Minigun : UdonSharpBehaviour
{
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private Transform VehicleTransform;
    [SerializeField] private Transform Minigun;
    [SerializeField] private Transform GunDamageParticle_Parent;
    [SerializeField] [UdonSynced(UdonSyncMode.None)] private float GunAmmoInSeconds = 12;
    [SerializeField] private float FullReloadTimeSec = 20;
    [SerializeField] private string AnimatorFiringStringName;
    [SerializeField] private Animator GunAnimator;
    [SerializeField] private Transform AmmoBar;
    private bool TriggerLastFrame;
    [UdonSynced(UdonSyncMode.None)] private Vector2 GunRotation;
    private bool InVR;
    private VRCPlayerApi localPlayer;
    private bool UseLeftTrigger;
    private bool func_active;
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
    public void SFEXTP_L_ECStart()
    {
        FullGunAmmoInSeconds = GunAmmoInSeconds;
        reloadspeed = FullGunAmmoInSeconds / FullReloadTimeSec;
        AmmoBarScaleStart = AmmoBar.localScale;
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
    public void DFUNC_Selected()
    {
        GunDamageParticle_Parent.gameObject.SetActive(true);
        func_active = true;
        gameObject.SetActive(true);
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Activate");
    }
    public void DFUNC_Deselected()
    {
        if (func_active)
        {
            RequestSerialization();
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Deactivate");
            func_active = false;
            GunDamageParticle_Parent.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }
    }
    public void Activate()
    {
        gameObject.SetActive(true);
    }
    public void Dectivate()
    {
        gameObject.SetActive(false);
        if (func_active)
        {
            if (firing)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "GunStopFiring");
            }
        }
    }
    public void SFEXTP_O_UserExit()
    {
        DFUNC_Deselected();
    }
    public void SFEXTP_G_Explode()
    {
        GunAmmoInSeconds = FullGunAmmoInSeconds;
        Minigun.rotation = Quaternion.identity;
        GunRotation = Vector2.zero;
    }
    public void SFEXTP_G_RespawnButton()
    {
        GunAmmoInSeconds = FullGunAmmoInSeconds;
        Minigun.rotation = Quaternion.identity;
        GunRotation = Vector2.zero;
    }
    public void SFEXTP_G_ReSupply()
    {
        if (GunAmmoInSeconds != FullGunAmmoInSeconds) { EngineControl.ReSupplied++; }
        GunAmmoInSeconds = Mathf.Min(GunAmmoInSeconds + reloadspeed, FullGunAmmoInSeconds);
    }
    private void LateUpdate()
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
            if ((Trigger > 0.75 || (Input.GetKey(KeyCode.Space))) && GunAmmoInSeconds > 0)
            {
                if (!firing)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "GunStartFiring");
                    firing = true;
                }
                GunAmmoInSeconds = Mathf.Max(GunAmmoInSeconds - DeltaTime, 0);

                TriggerLastFrame = true;
            }
            else
            {
                if (firing)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "GunStopFiring");
                    firing = false;
                    TriggerLastFrame = false;
                }
            }
            if (TimeSinceSerialization > 1f)
            {
                TimeSinceSerialization = 0;
                RequestSerialization();
            }


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
            Minigun.rotation = Quaternion.Slerp(Minigun.rotation, newrot, 4 * Time.deltaTime);
        }
        AmmoBar.localScale = new Vector3((GunAmmoInSeconds * FullGunAmmoDivider) * AmmoBarScaleStart.x, AmmoBarScaleStart.y, AmmoBarScaleStart.z);
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
