
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class EffectsController : UdonSharpBehaviour
{
    public GameObject VehicleMainObj;
    public EngineController EngineControl;
    public Transform JoyStick;
    public Transform[] Ailerons;
    public Transform[] Elevators;
    public Transform[] Rudders;
    public Transform Canards;
    public Transform Engines;
    public Transform[] Enginefire;
    public Transform FrontWheel;
    public ParticleSystem[] DisplaySmoke;
    public ParticleSystem CatapultSteam;



    private bool VehicleMainObjNull = true;
    private bool EngineControlNull = true;
    private bool JoyStickNull = true;
    private bool AileronsNull = true;
    private bool CanardsNull = true;
    private bool ElevatorNull = true;
    private bool EnginesNull = true;
    private bool EnginefireNull = true;
    private bool RuddersNull = true;
    public bool FrontWheelNull = true;
    private bool CatapultSteamNull = true;
    private bool DisplaySmokeNull = true;



    //best to remove synced variables if you aren't using them
    //moved some here from enginecontroller because there's a limit per-udonbehaviour
    [UdonSynced(UdonSyncMode.None)] private Vector3 rotationinputs;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool AfterburnerOn;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool CanopyOpen = true;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool GearUp = false;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool Flaps = true;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool HookDown = false;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool Smoking = false;


    private bool vapor;
    private float Gs_trail = 1000; //ensures it wont cause effects at first frame
    [System.NonSerializedAttribute] [HideInInspector] public Animator PlaneAnimator;
    private Vector3 PitchLerper = new Vector3(0, 0, 0);
    private Vector3 AileronLerper = new Vector3(0, 0, 0);
    private Vector3 YawLerper = new Vector3(0, 0, 0);
    private Vector3 EngineLerper = new Vector3(0, 0, 0);
    private Vector3 Enginefireerper = new Vector3(1, 0.6f, 1);
    [System.NonSerializedAttribute] [HideInInspector] public float AirbrakeLerper;
    [System.NonSerializedAttribute] [HideInInspector] public float DoEffects = 6f; //4 seconds before sleep so late joiners see effects if someone is already piloting
    [System.NonSerializedAttribute] [HideInInspector] public Vector3 Spawnposition;
    [System.NonSerializedAttribute] [HideInInspector] public Vector3 Spawnrotation;
    private float brake;
    private Color SmokeColorLerper = Color.white;
    private void Start()
    {
        Assert(VehicleMainObj != null, "Start: VehicleMainObj != null");
        Assert(EngineControl != null, "Start: EngineControl != null");
        //should work withouth these
        Assert(JoyStick != null, "Start: JoyStick != null");
        Assert(Enginefire != null, "Start: Enginefire != null");
        Assert(FrontWheel != null, "Start: FrontWheel != null");
        Assert(Ailerons.Length > 0, "Start: Ailerons.Length > 0");
        Assert(Elevators.Length > 0, "Start: Elevator.Length > 0");
        Assert(Rudders.Length > 0, "Start: Rudders.Length > 0");



        if (VehicleMainObj != null) VehicleMainObjNull = false;
        if (EngineControl != null) EngineControlNull = false;
        if (JoyStick != null) JoyStickNull = false;
        if (Canards != null) CanardsNull = false;
        if (Engines != null) EnginesNull = false;
        if (Enginefire != null) EnginefireNull = false;
        if (FrontWheel != null) FrontWheelNull = false;
        if (CatapultSteam != null) CatapultSteamNull = false;
        if (DisplaySmoke.Length > 0) DisplaySmokeNull = false;


        foreach (Transform fire in Enginefire)
            fire.localScale = new Vector3(fire.localScale.x, 0, fire.localScale.z);


        PlaneAnimator = VehicleMainObj.GetComponent<Animator>();
        Spawnposition = VehicleMainObj.transform.position;
        Spawnrotation = VehicleMainObj.transform.rotation.eulerAngles;
    }
    private void Update()
    {
        if (DoEffects > 10) { return; }

        //if a long way away just skip effects except large vapor effects
        if (EngineControl.SoundControl.ThisFrameDist > 2000f && !EngineControl.IsOwner) { DoVapor(); return; }//udonsharp doesn't support goto yet, so i'm usnig a function instead

        if (EngineControl.InEditor || EngineControl.IsOwner)
        {
            rotationinputs.x = Mathf.Clamp(EngineControl.PitchInput/*  + EngineControl.Trim.x */, -1, 1) * 25;
            rotationinputs.y = Mathf.Clamp(EngineControl.YawInput/*  + EngineControl.Trim.y */, -1, 1) * 20;
            rotationinputs.z = EngineControl.RollInput * 35;

            //joystick movement
            if (!JoyStickNull)
            {
                Vector3 tempjoy = new Vector3(EngineControl.PitchInput * 35f, EngineControl.YawInput * 35, EngineControl.RollInput * 35f);
                JoyStick.localRotation = Quaternion.Euler(tempjoy);
            }
        }
        vapor = (EngineControl.AirSpeed > 20) ? true : false;// only make vapor when going above "80m/s", prevents vapour appearing when taxiing into a wall or whatever

        PitchLerper.x = Mathf.Lerp(PitchLerper.x, rotationinputs.x, 4.5f * Time.deltaTime);
        AileronLerper.y = Mathf.Lerp(AileronLerper.y, rotationinputs.z, 4.5f * Time.deltaTime);
        YawLerper.y = Mathf.Lerp(YawLerper.y, rotationinputs.y, 4.5f * Time.deltaTime);
        Enginefireerper.y = Mathf.Lerp(Enginefireerper.y, EngineControl.Throttle, .9f * Time.deltaTime);

        if (EngineControl.Occupied == true)
        {
            DoEffects = 0f;
            if (EngineControl.IsFiringGun) //send firing to animator
            {
                PlaneAnimator.SetBool("gunfiring", true);
            }
            else
            {
                PlaneAnimator.SetBool("gunfiring", false);
            }

            if (!DisplaySmokeNull && Smoking)
            {
                SmokeColorLerper = Color.Lerp(SmokeColorLerper, EngineControl.SmokeColor_Color, 5 * Time.deltaTime);
                foreach (ParticleSystem smoke in DisplaySmoke)
                {
                    var main = smoke.main;
                    main.startColor = new ParticleSystem.MinMaxGradient(SmokeColorLerper, SmokeColorLerper * .8f);
                }
            }

            if (!FrontWheelNull)
            {
                if (EngineControl.Taxiing)
                {
                    FrontWheel.localRotation = Quaternion.Euler(new Vector3(0, -YawLerper.y * 3, 0));
                }
                else FrontWheel.localRotation = Quaternion.identity;
            }

        }
        else { DoEffects += Time.deltaTime; PlaneAnimator.SetBool("gunfiring", false); }

        foreach (Transform elevator in Elevators)
            elevator.localRotation = Quaternion.Euler(-PitchLerper);

        foreach (Transform aileron in Ailerons)
            aileron.localRotation = Quaternion.Euler(AileronLerper);

        foreach (Transform rudder in Rudders)
            rudder.localRotation = Quaternion.Euler(YawLerper);

        if (Flaps) { PlaneAnimator.SetBool("flaps", true); }
        else { PlaneAnimator.SetBool("flaps", false); }

        if (GearUp) { PlaneAnimator.SetBool("gearup", true); }
        else { PlaneAnimator.SetBool("gearup", false); }

        if (HookDown) { PlaneAnimator.SetBool("hookdown", true); }
        else { PlaneAnimator.SetBool("hookdown", false); }

        if (!EnginesNull)
        {
            Engines.localRotation = Quaternion.Euler(PitchLerper * -.6f);
        }
        if (!CanardsNull)
        {
            Canards.localRotation = Quaternion.Euler(PitchLerper * .6f);
        }

        //engine thrust animation

        if (!EnginefireNull)
        {
            if (AfterburnerOn)
            {
                foreach (Transform fire in Enginefire)
                {
                    fire.gameObject.SetActive(true);
                    fire.localScale = Enginefireerper;
                }
            }
            else
            {
                foreach (Transform fire in Enginefire)
                {
                    fire.gameObject.SetActive(true);
                    fire.localScale = new Vector3(1, 0, 1);
                }
            }
            if (EngineControl.Throttle <= .03f)
            {
                foreach (Transform fire in Enginefire)
                {
                    fire.gameObject.SetActive(false);
                }
            }
        }

        AirbrakeLerper = Mathf.Lerp(AirbrakeLerper, EngineControl.BrakeInput, 1.3f * Time.deltaTime);

        PlaneAnimator.SetBool("displaysmoke", (Smoking && EngineControl.Occupied) ? true : false);
        PlaneAnimator.SetBool("canopyopen", CanopyOpen);
        PlaneAnimator.SetFloat("health", EngineControl.Health / EngineControl.FullHealth);
        PlaneAnimator.SetFloat("AoA", vapor ? Mathf.Abs(EngineControl.AngleOfAttack / 180) : 0);
        PlaneAnimator.SetFloat("brake", AirbrakeLerper);
        //PlaneAnimator.SetBool("occupied", EngineControl.Occupied);
        //PlaneAnimator.SetInteger("rstickselection", EngineControl.RStickSelection);
        PlaneAnimator.SetFloat("AAMs", (float)EngineControl.NumAAM / EngineControl.FullAAMs);
        PlaneAnimator.SetFloat("AGMs", (float)EngineControl.NumAGM / EngineControl.FullAGMs);
        PlaneAnimator.SetFloat("bombs", (float)EngineControl.NumBomb / EngineControl.FullBombs);
        DoVapor();
    }


    private void DoVapor()//large vapor effects visible from a long distance
    {
        //this is to finetune when wingtrails appear and disappear
        if (EngineControl.Gs >= Gs_trail) //Gs are increasing
        {
            Gs_trail = Mathf.Lerp(Gs_trail, EngineControl.Gs, 30f * Time.deltaTime);//apear fast when pulling Gs
        }
        else //Gs are decreasing
        {
            Gs_trail = Mathf.Lerp(Gs_trail, EngineControl.Gs, 2.7f * Time.deltaTime);//linger for a bit before cutting off
        }
        PlaneAnimator.SetFloat("mach10", EngineControl.AirSpeed / 343 / 10);
        PlaneAnimator.SetFloat("Gs", vapor ? EngineControl.Gs / 50 : 0);
        PlaneAnimator.SetFloat("Gs_trail", vapor ? Gs_trail / 50 : 0);
    }

    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogError("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}