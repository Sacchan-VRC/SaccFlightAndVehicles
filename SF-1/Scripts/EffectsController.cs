
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class EffectsController : UdonSharpBehaviour
{
    public GameObject VehicleMainObj;
    public EngineController EngineControl;
    public LeaveVehicleButton[] LeaveButtons; //so you can leave when exploded
    public Transform JoyStick;
    public Transform AileronL;
    public Transform AileronR;
    public Transform Canards;
    public Transform Elevator;
    public Transform EngineL;
    public Transform EngineR;
    public Transform EnginefireL;
    public Transform EnginefireR;
    public Transform RudderL;
    public Transform RudderR;
    public Transform SlatsL;
    public Transform SlatsR;
    public ParticleSystem DisplaySmoke;
    private bool vapor;
    [UdonSynced(UdonSyncMode.None)] private Vector3 rotationinputs;
    private float Gs_trail = 1000; //ensures it wont cause effects at first frame
    [System.NonSerializedAttribute] [HideInInspector] public Animator PlaneAnimator;
    private Vector3 pitchlerper = new Vector3(0, 0, 0);
    private Vector3 aileronLlerper = new Vector3(0, 0, 0);
    private Vector3 aileronRlerper = new Vector3(0, 0, 0);
    private Vector3 yawlerper = new Vector3(0, 0, 0);
    private Vector3 FlapLerper = new Vector3(0, 0, 0);
    private Vector3 SlatsLerper = new Vector3(0, 35, 0);
    private Vector3 enginelerperL = new Vector3(0, 0, 0);
    private Vector3 enginelerperR = new Vector3(0, 0, 0);
    private Vector3 enginefirelerper = new Vector3(1, 0.6f, 1);
    private float airbrakelerper;
    [System.NonSerializedAttribute] [HideInInspector] public float DoEffects = 6f; //4 seconds before sleep so late joiners see effects if someone is already piloting
    [System.NonSerializedAttribute] [HideInInspector] public ParticleSystem.ColorOverLifetimeModule SmokeModule;
    [System.NonSerializedAttribute] [HideInInspector] public Vector3 Spawnposition;
    [System.NonSerializedAttribute] [HideInInspector] public Vector3 Spawnrotation;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool IsFiringGun = false;
    private float brake;
    private void Start()
    {
        if (VehicleMainObj != null) { PlaneAnimator = VehicleMainObj.GetComponent<Animator>(); }
        Spawnposition = VehicleMainObj.transform.position;
        Spawnrotation = VehicleMainObj.transform.rotation.eulerAngles;
        SmokeModule = DisplaySmoke.colorOverLifetime;
    }
    private void Update()
    {
        if ((EngineControl.InEditor || EngineControl.IsOwner) && !EngineControl.dead)//kill plane if in sea
        {
            if (EngineControl.CenterOfMass.position.y < EngineControl.SeaLevel && !EngineControl.dead)
            {
                if (EngineControl.InEditor)//so it works in editor
                {
                    Explode();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Explode");
                }
            }

            //G/crash Damage
            EngineControl.Health += -Mathf.Clamp((EngineControl.Gs - EngineControl.MaxGs) * Time.deltaTime * EngineControl.GDamage, 0f, 99999f); //take damage of GDamage per second per G above MaxGs
            if (EngineControl.Health <= 0f) //plane is ded
            {
                if (EngineControl.InEditor)//so it works in editor
                {
                    Explode();
                }
                else
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Explode");
                }
            }
        }

        if (DoEffects > 10) { return; }

        //if a long way away just skip effects except large vapor effects
        if (EngineControl.SoundControl != null && EngineControl.SoundControl.ThisFrameDist > 2000f && !EngineControl.IsOwner) { DoVapor(); return; }//udonsharp doesn't support goto yet, so i'm usnig a function instead

        if (EngineControl.InEditor || EngineControl.IsOwner)
        {
            PlaneAnimator.SetFloat("throttle", EngineControl.ThrottleInput);
            rotationinputs.x = Mathf.Clamp(EngineControl.PitchInput + EngineControl.Trim.x, -1, 1) * 25;
            rotationinputs.y = Mathf.Clamp(EngineControl.YawInput + EngineControl.Trim.y, -1, 1) * 20;
            rotationinputs.z = EngineControl.RollInput * 35;

            //joystick movement
            Vector3 tempjoy = new Vector3(EngineControl.PitchInput * 45f, -EngineControl.RollInput * 45f, EngineControl.YawInput * 45);
            JoyStick.localRotation = Quaternion.Euler(tempjoy);
        }
        vapor = (EngineControl.Speed > 20) ? true : false;// only make vapor when going above "80m/s", prevents vapour appearing when taxiing into a wall or whatever


        if (EngineControl.Occupied == true)
        {
            DoEffects = 0f;
            if (IsFiringGun) //send firing to animator
            {
                PlaneAnimator.SetBool("gunfiring", true);
            }
            else
            {
                PlaneAnimator.SetBool("gunfiring", false);
            }
            if (EngineControl.Smoking)
            {
                var main = DisplaySmoke.main;
                Color newsmoke = new Color(EngineControl.SmokeColor.x, EngineControl.SmokeColor.y, EngineControl.SmokeColor.z);
                main.startColor = new ParticleSystem.MinMaxGradient(newsmoke, newsmoke * .85f);
            }
        }
        else { DoEffects += Time.deltaTime; PlaneAnimator.SetBool("gunfiring", false); }

        if (EngineControl.Flaps) { PlaneAnimator.SetBool("flaps", true); }
        else { PlaneAnimator.SetBool("flaps", false); }

        if (EngineControl.GearUp) { PlaneAnimator.SetBool("gearup", true); }
        else { PlaneAnimator.SetBool("gearup", false); }

        if (EngineControl.HookDown) { PlaneAnimator.SetBool("hookdown", true); }
        else { PlaneAnimator.SetBool("hookdown", false); }


        //rotationinputs.x == pitch
        //rotationinputs.y == yaw
        //rotationinputs.z == roll
        //rotating the control surfaces based on inputs
        if (AileronL != null) { aileronLlerper.y = Mathf.Lerp(aileronLlerper.y, rotationinputs.z + (-rotationinputs.x * .5f), 4.5f * Time.deltaTime); ; AileronL.localRotation = Quaternion.Euler(aileronLlerper); }
        if (AileronR != null) { aileronRlerper.y = Mathf.Lerp(aileronRlerper.y, -rotationinputs.z + (-rotationinputs.x * .5f), 4.5f * Time.deltaTime); ; AileronR.localRotation = Quaternion.Euler(aileronRlerper); }

        pitchlerper.x = Mathf.Lerp(pitchlerper.x, rotationinputs.x, 4.5f * Time.deltaTime);
        if (Elevator != null) { Elevator.localRotation = Quaternion.Euler(-pitchlerper); }
        if (Canards != null) { Canards.localRotation = Quaternion.Euler(pitchlerper * .5f); }

        yawlerper.y = Mathf.Lerp(yawlerper.y, rotationinputs.y, 4.5f * Time.deltaTime);
        if (RudderL != null) { RudderL.localRotation = Quaternion.Euler(-yawlerper); }
        if (RudderR != null) { RudderR.localRotation = Quaternion.Euler(yawlerper); }

        SlatsLerper.y = Mathf.Lerp(SlatsLerper.y, Mathf.Max((-EngineControl.Speed * 0.005f) * 35f + 35f, 0f), 4.5f * Time.deltaTime); //higher the speed, closer to 0 rot the slats get.
        if (SlatsL != null) { SlatsL.localRotation = Quaternion.Euler(SlatsLerper); }
        if (SlatsR != null) { SlatsR.localRotation = Quaternion.Euler(SlatsLerper); }


        if (EngineL != null) { enginelerperL.x = Mathf.Lerp(enginelerperL.x, (rotationinputs.z * .3f) + (-rotationinputs.x * .65f), 4.5f * Time.deltaTime); EngineL.localRotation = Quaternion.Euler(enginelerperL); }
        if (EngineR != null) { enginelerperR.x = Mathf.Lerp(enginelerperR.x, (-rotationinputs.z * .3f) + (-rotationinputs.x * .65f), 4.5f * Time.deltaTime); EngineR.localRotation = Quaternion.Euler(enginelerperR); }

        //engine thrust animation
        enginefirelerper.y = Mathf.Lerp(enginefirelerper.y, EngineControl.Throttle, .9f * Time.deltaTime);

        if (EnginefireL != null)
        {
            if (EngineControl.AfterburnerOn)
            {
                EnginefireL.gameObject.SetActive(true);
                EnginefireL.localScale = enginefirelerper;
            }
            else
            {
                EnginefireL.gameObject.SetActive(true);
                EnginefireL.localScale = new Vector3(1, 0, 1);
            }
            if (EngineControl.Throttle <= .03f)
            {
                EnginefireL.gameObject.SetActive(false);
            }
        }
        if (EnginefireR != null)
        {
            if (EngineControl.AfterburnerOn)
            {
                EnginefireR.gameObject.SetActive(true);
                EnginefireR.localScale = enginefirelerper;
            }
            else
            {
                EnginefireR.gameObject.SetActive(true);
                EnginefireR.localScale = new Vector3(1, 0, 1);
            }
            if (EngineControl.Throttle <= .03f)
            {
                EnginefireR.gameObject.SetActive(false);
            }
        }

        airbrakelerper = Mathf.Lerp(airbrakelerper, EngineControl.AirBrakeInput, 5f * Time.deltaTime);

        PlaneAnimator.SetBool("displaysmoke", (EngineControl.Smoking && EngineControl.Occupied) ? true : false);
        PlaneAnimator.SetFloat("health", EngineControl.Health / EngineControl.FullHealth);
        PlaneAnimator.SetFloat("AoA", vapor ? Mathf.Abs(EngineControl.AngleOfAttack / 180) : 0);
        PlaneAnimator.SetFloat("brake", airbrakelerper);
        PlaneAnimator.SetBool("canopyopen", EngineControl.CanopyOpen);
        PlaneAnimator.SetBool("occupied", EngineControl.Occupied);
        PlaneAnimator.SetFloat("fuel", EngineControl.Fuel / EngineControl.FullFuel);
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
        EngineControl.GearUp = false;
        EngineControl.HookDown = false;
        EngineControl.AirBrakeInput = 0;
        EngineControl.FlightLimitsEnabled = true;
        EngineControl.CanopyOpen = false;
        EngineControl.Cruise = false;
        EngineControl.Trim = Vector2.zero;
        EngineControl.CanopyOpen = true;
        EngineControl.CanopyCloseTimer = -100001;


        if (EngineControl.InEditor || EngineControl.IsOwner)
        {
            EngineControl.VehicleRigidbody.velocity = Vector3.zero;
            EngineControl.Health = EngineControl.FullHealth;//turns off low health smoke
            EngineControl.Fuel = EngineControl.FullFuel;
        }

        //pilot and passenger are dropped out of the plane
        if (EngineControl.SoundControl != null && EngineControl.SoundControl.Explosion != null) { EngineControl.SoundControl.Explosion.Play(); }
        if ((EngineControl.Piloting || EngineControl.Passenger) && !EngineControl.InEditor)
        {
            foreach (LeaveVehicleButton seat in LeaveButtons)
            {
                seat.ExitStation();
            }
        }
        PlaneAnimator.SetTrigger("explode");
    }
    public void DropFlares()
    {
        PlaneAnimator.SetTrigger("flares");
    }
}