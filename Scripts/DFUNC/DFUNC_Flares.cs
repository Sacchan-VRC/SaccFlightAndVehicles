
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class DFUNC_Flares : UdonSharpBehaviour
{
    [SerializeField] private UdonSharpBehaviour SAVControl;
    [SerializeField] private int NumFlares = 60;
    [Tooltip("Speed to launch flare particles at")]
    [SerializeField] private float FlareLaunchSpeed = 100;
    [SerializeField] private ParticleSystem[] FlareParticles;
    [Tooltip("How long a flare has an effect for")]
    [SerializeField] private float FlareActiveTime = 4f;
    [Tooltip("How long it takes to fully reload from empty in seconds. Can be inaccurate because it can only reload by integers per resupply")]
    [SerializeField] private float FullReloadTimeSec = 15;
    [SerializeField] private AudioSource FlareLaunch;
    [SerializeField] private Text HUDText_flare_ammo;
    private bool HUDText_flare_ammoNULL = true;
    private bool UseLeftTrigger = false;
    private int FullFlares;
    private float reloadspeed;
    private bool TriggerLastFrame;
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void DFUNC_Selected()
    {
        gameObject.SetActive(true);
    }
    public void DFUNC_Deselected()
    {
        gameObject.SetActive(false);
        TriggerLastFrame = false;
    }
    public void SFEXT_L_EntityStart()
    {
        FullFlares = NumFlares;
        reloadspeed = FullFlares / FullReloadTimeSec;
        HUDText_flare_ammoNULL = HUDText_flare_ammo == null;
        if (!HUDText_flare_ammoNULL) { HUDText_flare_ammo.text = NumFlares.ToString("F0"); }
    }
    public void SFEXT_O_PilotExit()
    {
        gameObject.SetActive(false);
        TriggerLastFrame = false;
    }
    public void SFEXT_G_RespawnButton()
    {
        NumFlares = FullFlares;
        if (!HUDText_flare_ammoNULL) { HUDText_flare_ammo.text = NumFlares.ToString("F0"); }
    }
    public void SFEXT_G_Explode()
    {
        NumFlares = FullFlares;
        if (!HUDText_flare_ammoNULL) { HUDText_flare_ammo.text = NumFlares.ToString("F0"); }
    }
    public void SFEXT_G_ReSupply()
    {
        if (NumFlares != FullFlares)
        { SAVControl.SetProgramVariable("ReSupplied", (int)SAVControl.GetProgramVariable("ReSupplied") + 1); }
        NumFlares = (int)Mathf.Min(NumFlares + Mathf.Max(Mathf.Floor(reloadspeed), 1), FullFlares);
        if (!HUDText_flare_ammoNULL) { HUDText_flare_ammo.text = NumFlares.ToString("F0"); }
    }
    private void Update()
    {
        float Trigger;
        if (UseLeftTrigger)
        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
        else
        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

        if (Trigger > 0.75)
        {
            if (!TriggerLastFrame)
            {
                if (NumFlares > 0)
                { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchFlare"); }
            }
            TriggerLastFrame = true;
        }
        else { TriggerLastFrame = false; }
    }
    public void LaunchFlare()
    {
        NumFlares--;
        FlareLaunch.Play();
        if (!HUDText_flare_ammoNULL) { HUDText_flare_ammo.text = NumFlares.ToString("F0"); }
        int d = FlareParticles.Length;
        for (int x = 0; x < d; x++)
        {
            //this is to make flare particles inherit the velocity of the aircraft they were launched from (inherit doesn't work because non-owners don't have access to rigidbody velocity.)
            var emitParams = new ParticleSystem.EmitParams();
            Vector3 curspd = (Vector3)SAVControl.GetProgramVariable("CurrentVel");
            emitParams.velocity = curspd + (FlareParticles[x].transform.forward * FlareLaunchSpeed);
            FlareParticles[x].Emit(emitParams, 1);
        }
        { SAVControl.SetProgramVariable("NumActiveFlares", (int)SAVControl.GetProgramVariable("NumActiveFlares") + 1); }
        SendCustomEventDelayedSeconds("RemoveFlare", FlareActiveTime);
    }
    public void KeyboardInput()
    {
        if (NumFlares > 0)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchFlare"); }
    }
    public void RemoveFlare()
    {
        { SAVControl.SetProgramVariable("NumActiveFlares", (int)SAVControl.GetProgramVariable("NumActiveFlares") - 1); }
    }
}
