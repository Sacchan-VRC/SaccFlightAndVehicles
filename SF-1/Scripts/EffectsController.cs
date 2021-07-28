
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class EffectsController : UdonSharpBehaviour
{
    public GameObject VehicleMainObj;
    public EngineController EngineControl;
    public Transform FrontWheel;
    private bool VehicleMainObjNull = true;
    private bool EngineControlNull = true;
    private bool JoyStickNull = true;
    [System.NonSerializedAttribute] public bool FrontWheelNull = true;



    //these used to be synced variables, might move them back to EngineController some time
    [System.NonSerializedAttribute] public bool AfterburnerOn;
    [System.NonSerializedAttribute] public bool CanopyOpen = true;
    [System.NonSerializedAttribute] public bool GearUp = false;

    private bool vapor;
    private float Gs_trail = 1000; //ensures it wont cause effects at first frame
    [System.NonSerializedAttribute] public Animator PlaneAnimator;
    [System.NonSerializedAttribute] public float AirbrakeLerper;
    [System.NonSerializedAttribute] public float DoEffects = 6f; //4 seconds before sleep so late joiners see effects if someone is already piloting
    private float brake;
    [System.NonSerializedAttribute] public bool LargeEffectsOnly = false;
    private float FullHealthDivider;
    private Vector3 OwnerRotationInputs;
    private bool InEditor;
    private int OCCUPIED_STRING = Animator.StringToHash("occupied");
    private int PITCHINPUT_STRING = Animator.StringToHash("pitchinput");
    private int YAWINPUT_STRING = Animator.StringToHash("yawinput");
    private int ROLLINPUT_STRING = Animator.StringToHash("rollinput");
    private int THROTTLE_STRING = Animator.StringToHash("throttle");
    private int ENGINEOUTPUT_STRING = Animator.StringToHash("engineoutput");
    private int VTOLANGLE_STRING = Animator.StringToHash("vtolangle");
    private int HEALTH_STRING = Animator.StringToHash("health");
    private int AOA_STRING = Animator.StringToHash("AoA");
    private int BRAKE_STRING = Animator.StringToHash("brake");
    private int MACH10_STRING = Animator.StringToHash("mach10");
    private int GS_STRING = Animator.StringToHash("Gs");
    private int GS_TRAIL_STRING = Animator.StringToHash("Gs_trail");
    private int WEAPON_STRING = Animator.StringToHash("weapon");
    private int BOMB_STRING = Animator.StringToHash("bombs");
    private int AAMS_STRING = Animator.StringToHash("AAMs");
    private int AGMS_STRING = Animator.StringToHash("AGMs");
    private int RESPAWN_STRING = Animator.StringToHash("respawn");
    private int INSTANTGEARDOWN_STRING = Animator.StringToHash("instantgeardown");
    private int EXPLODE_STRING = Animator.StringToHash("explode");
    private int MISSILESINCOMING_STRING = Animator.StringToHash("missilesincoming");
    private int LOCALPILOT_STRING = Animator.StringToHash("localpilot");


    private void Start()
    {
        Assert(VehicleMainObj != null, "Start: VehicleMainObj != null");
        Assert(EngineControl != null, "Start: EngineControl != null");

        if (VehicleMainObj != null) VehicleMainObjNull = false;
        if (EngineControl != null) EngineControlNull = false;
        if (FrontWheel != null) FrontWheelNull = false;

        FullHealthDivider = 1f / EngineControl.Health;

        PlaneAnimator = VehicleMainObj.GetComponent<Animator>();
        if (Networking.LocalPlayer == null)
        {
            InEditor = true;
            PlaneAnimator.SetBool(OCCUPIED_STRING, true);
        }
    }
    private void Update()
    {
        if (DoEffects > 10) { return; }

        //if a long way away just skip effects except large vapor effects
        if (LargeEffectsOnly = (EngineControl.SoundControl.ThisFrameDist > 2000f && !EngineControl.IsOwner)) { LargeEffects(); return; }
        Effects();
        LargeEffects();
    }
    public void Effects()
    {
        Vector3 RotInputs = EngineControl.RotationInputs;
        float DeltaTime = Time.deltaTime;
        if (EngineControl.IsOwner)
        {
            if (EngineControl.InVR)
            { OwnerRotationInputs = RotInputs; }//vr users use raw input
            else
            { OwnerRotationInputs = Vector3.MoveTowards(OwnerRotationInputs, RotInputs, 7 * DeltaTime); }//desktop users use value movetowards'd to prevent instant movement
            PlaneAnimator.SetFloat(PITCHINPUT_STRING, (OwnerRotationInputs.x * 0.5f) + 0.5f);
            PlaneAnimator.SetFloat(YAWINPUT_STRING, (OwnerRotationInputs.y * 0.5f) + 0.5f);
            PlaneAnimator.SetFloat(ROLLINPUT_STRING, (OwnerRotationInputs.z * 0.5f) + 0.5f);
            PlaneAnimator.SetFloat(THROTTLE_STRING, EngineControl.ThrottleInput);
            PlaneAnimator.SetFloat(ENGINEOUTPUT_STRING, EngineControl.EngineOutput);
        }
        else
        {
            float EngineOutput = EngineControl.EngineOutput;
            PlaneAnimator.SetFloat(PITCHINPUT_STRING, (RotInputs.x * 0.5f) + 0.5f);
            PlaneAnimator.SetFloat(YAWINPUT_STRING, (RotInputs.y * 0.5f) + 0.5f);
            PlaneAnimator.SetFloat(ROLLINPUT_STRING, (RotInputs.z * 0.5f) + 0.5f);
            PlaneAnimator.SetFloat(THROTTLE_STRING, EngineOutput);//non-owners use value that is similar, but smoothed and would feel bad if the pilot used it himself
            PlaneAnimator.SetFloat(ENGINEOUTPUT_STRING, EngineOutput);
        }
        if (EngineControl.Occupied == true)
        {
            DoEffects = 0f;
            if (!FrontWheelNull)
            {
                if (EngineControl.Taxiing)
                {
                    FrontWheel.localRotation = Quaternion.Euler(new Vector3(0, -RotInputs.y * 80 * (-Mathf.Min((EngineControl.Speed / 10), 1) + 1), 0));
                }
                else FrontWheel.localRotation = Quaternion.identity;
            }
        }
        else { DoEffects += DeltaTime; }


        PlaneAnimator.SetFloat(VTOLANGLE_STRING, EngineControl.VTOLAngle);

        vapor = EngineControl.Speed > 20;// only make vapor when going above "20m/s", prevents vapour appearing when taxiing into a wall or whatever

        AirbrakeLerper = Mathf.Lerp(AirbrakeLerper, EngineControl.BrakeInput, 1.3f * DeltaTime);

        PlaneAnimator.SetFloat(HEALTH_STRING, EngineControl.Health * FullHealthDivider);
        PlaneAnimator.SetFloat(AOA_STRING, vapor ? Mathf.Abs(EngineControl.AngleOfAttack * 0.00555555556f /* Divide by 180 */ ) : 0);
        PlaneAnimator.SetFloat(BRAKE_STRING, AirbrakeLerper);
    }

    private void LargeEffects()//large effects visible from a long distance
    {
        float DeltaTime = Time.deltaTime;

        //this is to finetune when wingtrails appear and disappear
        if (EngineControl.Gs >= Gs_trail) //Gs are increasing
        {
            Gs_trail = Mathf.Lerp(Gs_trail, EngineControl.Gs, 30f * DeltaTime);//apear fast when pulling Gs
        }
        else //Gs are decreasing
        {
            Gs_trail = Mathf.Lerp(Gs_trail, EngineControl.Gs, 2.7f * DeltaTime);//linger for a bit before cutting off
        }
        //("mach10", EngineControl.Speed / 343 / 10)
        PlaneAnimator.SetFloat(MACH10_STRING, EngineControl.Speed * 0.000291545189504373f);//should be airspeed but nonlocal players don't have it
        //("Gs", vapor ? EngineControl.Gs / 50 : 0)
        PlaneAnimator.SetFloat(GS_STRING, vapor ? EngineControl.Gs * 0.02f : 0);
        //("Gs_trail", vapor ? Gs_trail / 50 : 0);
        PlaneAnimator.SetFloat(GS_TRAIL_STRING, vapor ? Gs_trail * 0.02f : 0);
    }
    public void EffectsResetStatus()//called from enginecontroller.Explode();
    {
        DoEffects = 6;
        PlaneAnimator.SetInteger(WEAPON_STRING, 4);
        PlaneAnimator.SetFloat(BOMB_STRING, 1);
        PlaneAnimator.SetFloat(AAMS_STRING, 1);
        PlaneAnimator.SetFloat(AGMS_STRING, 1);
        PlaneAnimator.SetTrigger(RESPAWN_STRING);//this animation disables EngineControl.dead after 2.5s
        PlaneAnimator.SetTrigger(INSTANTGEARDOWN_STRING);
        if (!FrontWheelNull) FrontWheel.localRotation = Quaternion.identity;
    }
    public void EffectsExplode()//called from enginecontroller.explode();
    {
        PlaneAnimator.SetTrigger(EXPLODE_STRING);
        PlaneAnimator.SetFloat(BOMB_STRING, 1);
        PlaneAnimator.SetFloat(AAMS_STRING, 1);
        PlaneAnimator.SetFloat(AGMS_STRING, 1);
        PlaneAnimator.SetInteger(MISSILESINCOMING_STRING, 0);
        PlaneAnimator.SetInteger(WEAPON_STRING, 0);
        PlaneAnimator.SetFloat(PITCHINPUT_STRING, .5f);
        PlaneAnimator.SetFloat(YAWINPUT_STRING, .5f);
        PlaneAnimator.SetFloat(ROLLINPUT_STRING, .5f);
        PlaneAnimator.SetFloat(THROTTLE_STRING, 0);//non-owners use value that is similar, but smoothed and would feel bad if the pilot used it himself
        PlaneAnimator.SetFloat(ENGINEOUTPUT_STRING, 0);
        if (!InEditor) { PlaneAnimator.SetBool(OCCUPIED_STRING, false); }
        DoEffects = 0f;//keep awake
        if (!FrontWheelNull) FrontWheel.localRotation = Quaternion.identity;
    }
    public void EffectsLeavePlane()
    {
        PlaneAnimator.SetBool(OCCUPIED_STRING, false);
        PlaneAnimator.SetInteger(MISSILESINCOMING_STRING, 0);
        PlaneAnimator.SetBool(LOCALPILOT_STRING, false);
    }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogWarning("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}