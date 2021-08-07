
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Flares : UdonSharpBehaviour
{
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private int NumFlares = 60;
    [Tooltip("How long a flare has an effect for")]
    [SerializeField] private ParticleSystem[] FlareParticles;
    [SerializeField] private float FlareActiveTime = 4f;
    [Tooltip("How long it takes to fully reload from 0 in seconds. Can be inaccurate because it can only reload by integers per resupply")]
    [SerializeField] private float FullReloadTimeSec = 15;
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
    public void SFEXT_L_ECStart()
    {
        FullFlares = NumFlares;
        reloadspeed = FullFlares / FullReloadTimeSec;
    }
    public void SFEXT_O_PilotExit()
    {
        gameObject.SetActive(false);
        TriggerLastFrame = false;
    }
    public void SFEXT_G_Explode()
    {
        NumFlares = FullFlares;
    }
    public void SFEXT_G_ReSupply()
    {
        if (NumFlares != FullFlares) { EngineControl.ReSupplied++; }
        NumFlares = (int)Mathf.Min(NumFlares + Mathf.Max(Mathf.Floor(reloadspeed), 1), FullFlares);
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
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchFlare");
            }
            TriggerLastFrame = true;
        }
        else { TriggerLastFrame = false; }
    }
    public void LaunchFlare()
    {
        NumFlares--;
        int d = FlareParticles.Length;
        for (int x = 0; x < d; x++)
        { FlareParticles[x].Play(); }
        EngineControl.NumActiveFlares++;
        EngineControl.SendCustomEventDelayedSeconds("RemoveFlare", FlareActiveTime);
    }
    public void KeyboardInput()
    {
        if (NumFlares > 0)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchFlare"); }
    }
    public void RemoveFlare()
    {
        EngineControl.NumActiveFlares--;
    }
}
