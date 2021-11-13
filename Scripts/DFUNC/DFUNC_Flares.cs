
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class DFUNC_Flares : UdonSharpBehaviour
{
    [SerializeField] private UdonSharpBehaviour SAVControl;
    public int NumFlares = 60;
    [Tooltip("Speed to launch flare particles at")]
    [SerializeField] private float FlareLaunchSpeed = 100;
    [SerializeField] private ParticleSystem[] FlareParticles;
    [Tooltip("How long a flare has an effect for")]
    [SerializeField] private float FlareActiveTime = 4f;
    [Tooltip("How long it takes to fully reload from empty in seconds. Can be inaccurate because it can only reload by integers per resupply")]
    [SerializeField] private float FullReloadTimeSec = 15;
    [SerializeField] private AudioSource FlareLaunch;
    [SerializeField] private Text HUDText_flare_ammo;
    private bool UseLeftTrigger = false;
    private int FullFlares;
    private float reloadspeed;
    private bool func_active;
    private bool TriggerLastFrame;
    [UdonSynced, FieldChangeCallback(nameof(sendlaunchflare))] private bool _SendLaunchFlare;
    private SaccEntity EntityControl;
    public bool sendlaunchflare
    {
        set
        {
            LaunchFlare();
            _SendLaunchFlare = value;
        }
        get => _SendLaunchFlare;
    }
    public void DFUNC_LeftDial() { UseLeftTrigger = true; }
    public void DFUNC_RightDial() { UseLeftTrigger = false; }
    public void DFUNC_Selected()
    {
        TriggerLastFrame = true;
        func_active = true;
    }
    public void DFUNC_Deselected()
    {
        func_active = false;
    }
    public void SFEXT_L_EntityStart()
    {
        FullFlares = NumFlares;
        reloadspeed = FullFlares / FullReloadTimeSec;
        if (HUDText_flare_ammo) { HUDText_flare_ammo.text = NumFlares.ToString("F0"); }
        EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
    }
    public void SFEXT_G_PilotEnter()
    { gameObject.SetActive(true); }
    public void SFEXT_G_PilotExit()
    { gameObject.SetActive(false); }
    public void SFEXT_O_PilotExit()
    {
        func_active = false;
    }
    public void SFEXT_G_RespawnButton()
    {
        NumFlares = FullFlares;
        if (HUDText_flare_ammo) { HUDText_flare_ammo.text = NumFlares.ToString("F0"); }
    }
    public void SFEXT_G_Explode()
    {
        NumFlares = FullFlares;
        if (HUDText_flare_ammo) { HUDText_flare_ammo.text = NumFlares.ToString("F0"); }
    }
    public void SFEXT_G_ReSupply()
    {
        if (NumFlares != FullFlares)
        { SAVControl.SetProgramVariable("ReSupplied", (int)SAVControl.GetProgramVariable("ReSupplied") + 1); }
        NumFlares = (int)Mathf.Min(NumFlares + Mathf.Max(Mathf.Floor(reloadspeed), 1), FullFlares);
        if (HUDText_flare_ammo) { HUDText_flare_ammo.text = NumFlares.ToString("F0"); }
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

            if (Trigger > 0.75)
            {
                if (!TriggerLastFrame)
                {
                    if (NumFlares > 0)
                    {
                        sendlaunchflare = !sendlaunchflare;
                        RequestSerialization();
                    }
                }
                TriggerLastFrame = true;
            }
            else { TriggerLastFrame = false; }
        }
    }
    public void LaunchFlare()
    {
        NumFlares--;
        FlareLaunch.Play();
        if (HUDText_flare_ammo) { HUDText_flare_ammo.text = NumFlares.ToString("F0"); }
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
        EntityControl.SendEventToExtensions("SFEXT_G_LaunchFlare");
    }
    public void KeyboardInput()
    {
        if (NumFlares > 0)
        {
            sendlaunchflare = !sendlaunchflare;
            RequestSerialization();
        }
    }
    public void RemoveFlare()
    {
        { SAVControl.SetProgramVariable("NumActiveFlares", (int)SAVControl.GetProgramVariable("NumActiveFlares") - 1); }
    }
}
