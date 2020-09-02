
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class EffectsController : UdonSharpBehaviour
{
    public GameObject VehicleMainObj;
    public EngineController EngineControl;
    public Transform JoyStick;
    public Transform AileronL;
    public Transform AileronR;
    public Transform Canards;
    public Transform Elevator;
    public Transform Engines;
    public Transform Enginefire;
    public Transform RudderL;
    public Transform RudderR;
    public Transform SlatL;
    public Transform SlatR;
    public Transform FrontWheel;
    public ParticleSystem DisplaySmoke;



    private bool VehicleMainObjNull = true;
    private bool EngineControlNull = true;
    private bool JoyStickNull = true;
    private bool AileronLNull = true;
    private bool AileronRNull = true;
    private bool CanardsNull = true;
    private bool ElevatorNull = true;
    private bool EnginesNull = true;
    private bool EnginefireNull = true;
    private bool RudderLNull = true;
    private bool RudderRNull = true;
    private bool SlatLNull = true;
    private bool SlatRNull = true;
    private bool FrontWheelNull = true;
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
    private Vector3 SlatsLerper = new Vector3(0, 35, 0);
    private Vector3 EngineLerper = new Vector3(0, 0, 0);
    private Vector3 Enginefireerper = new Vector3(1, 0.6f, 1);
    private float AirbrakeLerper;
    [System.NonSerializedAttribute] [HideInInspector] public float DoEffects = 6f; //4 seconds before sleep so late joiners see effects if someone is already piloting
    [System.NonSerializedAttribute] [HideInInspector] public ParticleSystem.ColorOverLifetimeModule SmokeModule;
    [System.NonSerializedAttribute] [HideInInspector] public Vector3 Spawnposition;
    [System.NonSerializedAttribute] [HideInInspector] public Vector3 Spawnrotation;
    private float brake;
    private Color SmokeColorLerper = Color.white;
    private void Start()
    {
        Assert(VehicleMainObj != null, "Start: VehicleMainObj != null");
        Assert(EngineControl != null, "Start: EngineControl != null");
        //should work withouth these
        /* Assert(JoyStick != null, "Start: JoyStick != null");
        Assert(AileronL != null, "Start: AileronL != null");
        Assert(AileronR != null, "Start: AileronR != null");
        Assert(Canards != null, "Start: Canards != null");
        Assert(Elevator != null, "Start: Elevator != null");
        Assert(Engines != null, "Start: Engines != null");
        Assert(Enginefire != null, "Start: Enginefire != null");
        Assert(RudderL != null, "Start: RudderL != null");
        Assert(RudderR != null, "Start: RudderR != null");
        Assert(SlatL != null, "Start: SlatsL != null");
        Assert(SlatR != null, "Start: SlatsR != null");
        Assert(FrontWheel != null, "Start: FrontWheel != null"); */



        if (VehicleMainObj != null) VehicleMainObjNull = false;
        if (EngineControl != null) EngineControlNull = false;
        if (JoyStick != null) JoyStickNull = false;
        if (AileronL != null) AileronLNull = false;
        if (AileronR != null) AileronRNull = false;
        if (Canards != null) CanardsNull = false;
        if (Elevator != null) ElevatorNull = false;
        if (Engines != null) EnginesNull = false;
        if (Enginefire != null) EnginefireNull = false;
        if (RudderL != null) RudderLNull = false;
        if (RudderR != null) RudderRNull = false;
        if (SlatL != null) SlatLNull = false;
        if (SlatR != null) SlatRNull = false;
        if (FrontWheel != null) FrontWheelNull = false;
        if (DisplaySmoke != null) DisplaySmokeNull = false;



        if (!EnginesNull) EngineLerper = new Vector3(0, Engines.localRotation.eulerAngles.y, Engines.localRotation.eulerAngles.z);
        if (!EnginefireNull)
        {
            Enginefireerper = new Vector3(Enginefire.localScale.x, 0, Enginefire.localScale.z);
        }

        PlaneAnimator = VehicleMainObj.GetComponent<Animator>();
        Spawnposition = VehicleMainObj.transform.position;
        Spawnrotation = VehicleMainObj.transform.rotation.eulerAngles;
        SmokeModule = DisplaySmoke.colorOverLifetime;
    }
    private void Update()
    {
        if ((EngineControl.InEditor || EngineControl.IsOwner) && !EngineControl.dead)
        {
            if (EngineControl.CenterOfMass.position.y < EngineControl.SeaLevel && !EngineControl.dead)//kill plane if in sea
            {
                if (EngineControl.InEditor)//editor
                    Explode();
                else//VRC
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Explode");
            }

            //G/crash Damage
            EngineControl.Health += -Mathf.Clamp((EngineControl.Gs - EngineControl.MaxGs) * Time.deltaTime * EngineControl.GDamage, 0f, 99999f); //take damage of GDamage per second per G above MaxGs
            if (EngineControl.Health <= 0f)//plane is ded
            {
                if (EngineControl.InEditor)//editor
                    Explode();
                else//VRC
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Explode");
            }
        }

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
        vapor = (EngineControl.Speed > 20) ? true : false;// only make vapor when going above "80m/s", prevents vapour appearing when taxiing into a wall or whatever

        PitchLerper.x = Mathf.Lerp(PitchLerper.x, rotationinputs.x, 4.5f * Time.deltaTime);
        AileronLerper.y = Mathf.Lerp(AileronLerper.y, rotationinputs.z, 4.5f * Time.deltaTime);
        SlatsLerper.y = Mathf.Lerp(SlatsLerper.y, Mathf.Max((-EngineControl.Speed * 0.005f) * 35f + 35f, 0f), 4.5f * Time.deltaTime); //higher the speed, closer to 0 rot the slats get.
        YawLerper.y = Mathf.Lerp(YawLerper.y, rotationinputs.y, 4.5f * Time.deltaTime);
        EngineLerper.x = Mathf.Lerp(EngineLerper.x, (-rotationinputs.x * .65f), 4.5f * Time.deltaTime);
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

            if (!DisplaySmokeNull)
            {
                SmokeColorLerper = Color.Lerp(SmokeColorLerper, EngineControl.SmokeColor_Color, 5 * Time.deltaTime);
                var main = DisplaySmoke.main;
                main.startColor = new ParticleSystem.MinMaxGradient(SmokeColorLerper, SmokeColorLerper * .8f);
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

        if (Flaps) { PlaneAnimator.SetBool("flaps", true); }
        else { PlaneAnimator.SetBool("flaps", false); }

        if (GearUp) { PlaneAnimator.SetBool("gearup", true); }
        else { PlaneAnimator.SetBool("gearup", false); }

        if (HookDown) { PlaneAnimator.SetBool("hookdown", true); }
        else { PlaneAnimator.SetBool("hookdown", false); }


        /* rotationinputs.x == pitch
        rotationinputs.y == yaw
        rotationinputs.z == roll
        rotating the control surfaces based on inputs */
        if (!AileronLNull) { AileronL.localRotation = Quaternion.Euler(AileronLerper); }
        if (!AileronRNull) { AileronR.localRotation = Quaternion.Euler(-AileronLerper); }

        if (!ElevatorNull) { Elevator.localRotation = Quaternion.Euler(-PitchLerper); }
        if (!CanardsNull) { Canards.localRotation = Quaternion.Euler(PitchLerper * .5f); }

        if (!RudderLNull) { RudderL.localRotation = Quaternion.Euler(-YawLerper); }
        if (!RudderRNull) { RudderR.localRotation = Quaternion.Euler(YawLerper); }

        if (!SlatLNull) { SlatL.localRotation = Quaternion.Euler(SlatsLerper); }
        if (!SlatRNull) { SlatR.localRotation = Quaternion.Euler(SlatsLerper); }

        if (!EnginesNull) { Engines.localRotation = Quaternion.Euler(EngineLerper); }

        //engine thrust animation

        if (!EnginefireNull)
        {
            if (AfterburnerOn)
            {
                Enginefire.gameObject.SetActive(true);
                Enginefire.localScale = Enginefireerper;
            }
            else
            {
                Enginefire.gameObject.SetActive(true);
                Enginefire.localScale = new Vector3(1, 0, 1);
            }
            if (EngineControl.Throttle <= .03f)
            {
                Enginefire.gameObject.SetActive(false);
            }
        }

        AirbrakeLerper = Mathf.Lerp(AirbrakeLerper, EngineControl.AirBrakeInput, 1.3f * Time.deltaTime);

        PlaneAnimator.SetBool("displaysmoke", (Smoking && EngineControl.Occupied) ? true : false);
        PlaneAnimator.SetFloat("health", EngineControl.Health / EngineControl.FullHealth);
        PlaneAnimator.SetFloat("AoA", vapor ? Mathf.Abs(EngineControl.AngleOfAttack / 180) : 0);
        PlaneAnimator.SetFloat("brake", AirbrakeLerper);
        PlaneAnimator.SetBool("canopyopen", CanopyOpen);
        //PlaneAnimator.SetBool("occupied", EngineControl.Occupied);
        //PlaneAnimator.SetInteger("rstickselection", EngineControl.RStickSelection);
        PlaneAnimator.SetFloat("bombs", (float)EngineControl.NumBomb / (float)EngineControl.FullBombs);
        PlaneAnimator.SetFloat("AAMs", (float)EngineControl.NumAAM / (float)EngineControl.FullAAMs);
        PlaneAnimator.SetFloat("AGMs", (float)EngineControl.NumAGM / (float)EngineControl.FullAGMs);
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
        PlaneAnimator.SetFloat("mach10", EngineControl.Speed / 343 / 10);
        PlaneAnimator.SetFloat("Gs", vapor ? EngineControl.Gs / 50 : 0);
        PlaneAnimator.SetFloat("Gs_trail", vapor ? Gs_trail / 50 : 0);
    }
    public void Explode()//all the things players see happen when the vehicle explodes
    {
        DoEffects = 0f; //keep awake

        EngineControl.dead = true;
        GearUp = false;
        HookDown = false;
        EngineControl.AirBrakeInput = 0;
        EngineControl.FlightLimitsEnabled = true;
        CanopyOpen = false;
        EngineControl.Cruise = false;
        //EngineControl.Trim = Vector2.zero;
        CanopyOpen = true;
        EngineControl.CanopyCloseTimer = -100001;
        EngineControl.Hooked = -1;
        EngineControl.AAMLaunchOpositeSide = false;
        EngineControl.AGMLaunchOpositeSide = false;
        EngineControl.NumAAM = EngineControl.FullAAMs;
        EngineControl.NumAGM = EngineControl.FullAGMs;
        EngineControl.NumBomb = EngineControl.FullBombs;
        EngineControl.GunAmmoInSeconds = EngineControl.FullGunAmmo;
        EngineControl.Fuel = EngineControl.FullFuel;

        //play sonic boom if it was going to play before it exploded
        if (EngineControl.SoundControl.playsonicboom && EngineControl.SoundControl.silent)
        {
            int rand = Random.Range(0, EngineControl.SoundControl.SonicBoom.Length);
            EngineControl.SoundControl.SonicBoom[rand].pitch = Random.Range(.94f, 1.2f);
            EngineControl.SoundControl.SonicBoom[rand].PlayDelayed((EngineControl.SoundControl.SonicBoomDistance - EngineControl.SoundControl.SonicBoomWave) / 343);
        }
        EngineControl.SoundControl.playsonicboom = false;
        EngineControl.SoundControl.silent = false;

        if (EngineControl.InEditor || EngineControl.IsOwner)
        {
            EngineControl.VehicleRigidbody.velocity = Vector3.zero;
            EngineControl.Health = EngineControl.FullHealth;//turns off low health smoke
            EngineControl.Fuel = EngineControl.FullFuel;
        }

        //pilot and passenger are dropped out of the plane
        if (EngineControl.SoundControl != null && !EngineControl.SoundControl.ExplosionNull)
        {
            int rand = Random.Range(0, EngineControl.SoundControl.Explosion.Length);
            EngineControl.SoundControl.Explosion[rand].Play();//explosion sound has travel time
        }

        if ((EngineControl.Piloting || EngineControl.Passenger) && !EngineControl.InEditor)
        {
            foreach (LeaveVehicleButton seat in EngineControl.LeaveButtons)
            {
                seat.ExitStation();
            }
        }
        PlaneAnimator.SetTrigger("explode");
    }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogError("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}