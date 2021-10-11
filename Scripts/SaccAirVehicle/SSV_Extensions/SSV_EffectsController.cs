
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class SSV_EffectsController : UdonSharpBehaviour
{
    [SerializeField] private UdonSharpBehaviour SSVControl;
    [Tooltip("Particle system that plays when vehicle enters water")]
    [SerializeField] private ParticleSystem SplashParticle;
    [Tooltip("Only play the splash particle if vehicle is faster than this. Meters/s")]
    [SerializeField] private float PlaySplashSpeed = 7;
    [SerializeField] private Transform[] FlatWaterEffects;
    [SerializeField] private UdonSharpBehaviour FloatScript;
    [System.NonSerializedAttribute] public Animator VehicleAnimator;
    [System.NonSerializedAttribute] public float DoEffects = 999f;//don't do effects before initialized
    private float brake;
    private float FullHealthDivider;
    private Vector3 OwnerRotationInputs;
    private VRCPlayerApi localPlayer;
    private bool InVR;
    private bool InEditor = true;
    private int OCCUPIED_STRING = Animator.StringToHash("occupied");
    private int YAWINPUT_STRING = Animator.StringToHash("yawinput");
    private int THROTTLE_STRING = Animator.StringToHash("throttle");
    private int ENGINEOUTPUT_STRING = Animator.StringToHash("engineoutput");
    private int HEALTH_STRING = Animator.StringToHash("health");
    private int MACH10_STRING = Animator.StringToHash("mach10");
    private int EXPLODE_STRING = Animator.StringToHash("explode");
    private int LOCALPILOT_STRING = Animator.StringToHash("localpilot");
    private int LOCALPASSENGER_STRING = Animator.StringToHash("localpassenger");
    private int BULLETHIT_STRING = Animator.StringToHash("bullethit");
    private int ONGROUND_STRING = Animator.StringToHash("onground");
    private int ONWATER_STRING = Animator.StringToHash("onwater");
    private int AFTERBURNERON_STRING = Animator.StringToHash("afterburneron");
    private int REAPPEAR_STRING = Animator.StringToHash("reappear");
    private int RESUPPLY_STRING = Animator.StringToHash("resupply");
    [SerializeField] private bool PrintAnimHashNamesOnStart;

    public void SFEXT_L_EntityStart()
    {
        FullHealthDivider = 1f / (float)SSVControl.GetProgramVariable("Health");

        VehicleAnimator = ((SaccEntity)SSVControl.GetProgramVariable("EntityControl")).GetComponent<Animator>();
        localPlayer = Networking.LocalPlayer;
        if (localPlayer == null)
        {
            VehicleAnimator.SetBool(OCCUPIED_STRING, true);
        }
        else { InEditor = false; }

        if (PrintAnimHashNamesOnStart)
        { PrintStringHashes(); }
        DoEffects = 6;
    }
    private void Update()
    {
        if (DoEffects > 10) { return; }

        //if a long way away just skip effects except large vapor effects
        Effects();
    }
    public void Effects()
    {
        Vector3 RotInputs = (Vector3)SSVControl.GetProgramVariable("RotationInputs");
        float DeltaTime = Time.deltaTime;
        if ((bool)SSVControl.GetProgramVariable("IsOwner"))
        {
            if (InVR)
            { OwnerRotationInputs = RotInputs; }//vr users use raw input
            else
            { OwnerRotationInputs = Vector3.MoveTowards(OwnerRotationInputs, RotInputs, 7 * DeltaTime); }//desktop users use value movetowards'd to prevent instant movement
            VehicleAnimator.SetFloat(YAWINPUT_STRING, (OwnerRotationInputs.y * 0.5f) + 0.5f);
            VehicleAnimator.SetFloat(THROTTLE_STRING, (float)SSVControl.GetProgramVariable("ThrottleInput"));
            VehicleAnimator.SetFloat(ENGINEOUTPUT_STRING, (float)SSVControl.GetProgramVariable("EngineOutput"));
        }
        else
        {
            float EngineOutput = (float)SSVControl.GetProgramVariable("EngineOutput");
            VehicleAnimator.SetFloat(YAWINPUT_STRING, (RotInputs.y * 0.5f) + 0.5f);
            VehicleAnimator.SetFloat(THROTTLE_STRING, EngineOutput);//non-owners use value that is similar, but smoothed and would feel bad if the pilot used it himself
            VehicleAnimator.SetFloat(ENGINEOUTPUT_STRING, EngineOutput);
        }
        if ((bool)SSVControl.GetProgramVariable("Occupied"))
        {
            DoEffects = 0f;
        }
        else { DoEffects += DeltaTime; }
        VehicleAnimator.SetFloat(HEALTH_STRING, (float)SSVControl.GetProgramVariable("Health") * FullHealthDivider);
        VehicleAnimator.SetFloat(MACH10_STRING, (float)SSVControl.GetProgramVariable("Speed") * 0.000291545189504373f);//should be airspeed but nonlocal players don't have it

        float watersurface = (float)FloatScript.GetProgramVariable("SurfaceHeight") + .02f;

        foreach (Transform trns in FlatWaterEffects)
        {
            Vector3 pos = trns.position;
            pos.y = watersurface;
            trns.position = pos;
            Vector3 rot = trns.eulerAngles;
            rot.z = 0; rot.x = 0;
            Quaternion newrot = Quaternion.Euler(rot);
            trns.rotation = newrot;
        }
    }
    public void SFEXT_G_PilotExit()
    {
        VehicleAnimator.SetBool(OCCUPIED_STRING, false);
    }
    public void SFEXT_G_PilotEnter()
    {
        DoEffects = 0f;
        VehicleAnimator.SetBool(OCCUPIED_STRING, true);
    }
    public void SFEXT_O_PilotEnter()
    {
        if (!InEditor) { InVR = localPlayer.IsUserInVR(); }
        VehicleAnimator.SetBool(LOCALPILOT_STRING, true);
    }
    public void SFEXT_O_PilotExit()
    {
        VehicleAnimator.SetBool(LOCALPILOT_STRING, false);
    }
    public void SFEXT_P_PassengerEnter()
    {
        VehicleAnimator.SetBool(LOCALPASSENGER_STRING, true);
    }
    public void SFEXT_P_PassengerExit()
    {
        VehicleAnimator.SetBool(LOCALPASSENGER_STRING, false);
    }
    public void SFEXT_G_ReAppear()
    {
        DoEffects = 6f; //wake up if was asleep
        VehicleAnimator.SetTrigger(REAPPEAR_STRING);
    }
    public void SFEXT_G_AfterburnerOn()
    {
        VehicleAnimator.SetBool(AFTERBURNERON_STRING, true);
    }
    public void SFEXT_G_AfterburnerOff()
    {
        VehicleAnimator.SetBool(AFTERBURNERON_STRING, false);
    }
    public void SFEXT_G_ReSupply()
    {
        VehicleAnimator.SetTrigger(RESUPPLY_STRING);
    }
    public void SFEXT_G_BulletHit()
    {
        WakeUp();
        VehicleAnimator.SetTrigger(BULLETHIT_STRING);
    }
    public void WakeUp()
    {
        DoEffects = 0f;
    }
    public void SFEXT_G_EnterWater()
    {
        if ((float)SSVControl.GetProgramVariable("Speed") > PlaySplashSpeed && SplashParticle) { SplashParticle.Play(); }
    }
    public void SFEXT_G_TakeOff()
    {
        VehicleAnimator.SetBool(ONGROUND_STRING, false);
        VehicleAnimator.SetBool(ONWATER_STRING, false);
    }
    public void SFEXT_G_TouchDown()
    {
        VehicleAnimator.SetBool(ONGROUND_STRING, true);
    }
    public void SFEXT_G_TouchDownWater()
    {
        VehicleAnimator.SetBool(ONWATER_STRING, true);
    }
    public void SFEXT_G_RespawnButton()
    {
        DoEffects = 6;
    }
    public void SFEXT_G_Explode()//old EffectsExplode()
    {
        VehicleAnimator.SetTrigger(EXPLODE_STRING);
        VehicleAnimator.SetFloat(YAWINPUT_STRING, .5f);
        VehicleAnimator.SetFloat(THROTTLE_STRING, 0);
        VehicleAnimator.SetFloat(ENGINEOUTPUT_STRING, 0);
        if (!InEditor) { VehicleAnimator.SetBool(OCCUPIED_STRING, false); }
        DoEffects = 0f;//keep awake
    }
    private void PrintStringHashes()
    {
        Debug.Log(string.Concat("OCCUPIED_STRING : ", OCCUPIED_STRING));
        Debug.Log(string.Concat("YAWINPUT_STRING : ", YAWINPUT_STRING));
        Debug.Log(string.Concat("THROTTLE_STRING : ", THROTTLE_STRING));
        Debug.Log(string.Concat("ENGINEOUTPUT_STRING : ", ENGINEOUTPUT_STRING));
        Debug.Log(string.Concat("HEALTH_STRING : ", HEALTH_STRING));
        Debug.Log(string.Concat("MACH10_STRING : ", MACH10_STRING));
        Debug.Log(string.Concat("EXPLODE_STRING : ", EXPLODE_STRING));
        Debug.Log(string.Concat("LOCALPILOT_STRING : ", LOCALPILOT_STRING));
        Debug.Log(string.Concat("LOCALPASSENGER_STRING : ", LOCALPASSENGER_STRING));
        Debug.Log(string.Concat("BULLETHIT_STRING : ", BULLETHIT_STRING));
        Debug.Log(string.Concat("ONGROUND_STRING : ", ONGROUND_STRING));
        Debug.Log(string.Concat("ONWATER_STRING : ", ONWATER_STRING));
        Debug.Log(string.Concat("AFTERBURNERON_STRING : ", AFTERBURNERON_STRING));
        Debug.Log(string.Concat("REAPPEAR_STRING : ", REAPPEAR_STRING));
        Debug.Log(string.Concat("RESUPPLY_STRING : ", RESUPPLY_STRING));
    }
}