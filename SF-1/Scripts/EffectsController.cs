
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class EffectsController : UdonSharpBehaviour
{
    public GameObject VehicleMainObj;
    public EngineController EngineControl;
    public VRCStation PilotSeatStation; //so you can leave when exploded
    public VRCStation PassengerSeatStation; // so you can leave when exploded
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
    public ParticleSystem AoAVapor;
    public ParticleSystem GVaporL;
    public ParticleSystem GVaporR;
    public ParticleSystem MachVapor;
    public TrailRenderer WingTrailL;
    public TrailRenderer WingTrailR;
    public float TrailGs = 4f;
    public float MaxGs = 20f;
    public float GDamage = 15f;
    private PilotSeat PilotSeat1;
    private PassengerSeat PassengerSeat1;
    private bool vapor;
    [UdonSynced(UdonSyncMode.None)] private float roll;
    [UdonSynced(UdonSyncMode.None)] private float pitch;
    [UdonSynced(UdonSyncMode.None)] private float yaw;
    private float machspeed;
    private float Gs_trail = 1000; //ensures it wont cause effects at first frame
    [System.NonSerializedAttribute] [HideInInspector] public Animator PlaneAnimator;
    private ParticleSystem.EmissionModule emmachvapor;
    private ParticleSystem.EmissionModule emaoavapor;
    private ParticleSystem.EmissionModule emgvaporl;
    private ParticleSystem.EmissionModule emgvaporr;
    private Vector3 pitchlerper = new Vector3(0, 0, 0);
    private Vector3 aileronLlerper = new Vector3(0, 0, 0);
    private Vector3 aileronRlerper = new Vector3(0, 0, 0);
    private Vector3 yawlerper = new Vector3(0, 0, 0);
    private Vector3 FlapLerper = new Vector3(0, 0, 0);
    private Vector3 SlatsLerper = new Vector3(0, 35, 0);
    private Vector3 enginelerperL = new Vector3(0, 0, 0);
    private Vector3 enginelerperR = new Vector3(0, 0, 0);
    private Vector3 enginefirelerper = new Vector3(1, 1, 1);
    [System.NonSerializedAttribute] [HideInInspector] public float DoEffects = 6f; //4 seconds before sleep so late joiners see effects if someone is already piloting, and so that engine fire has time to disappear at start
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool Smoking;
    private float LGrip;
    private float RGrip;
    [System.NonSerializedAttribute] [HideInInspector] public bool RGriplastframetrigger;
    private bool LGriplastframetrigger;
    private ParticleSystem.EmissionModule flareemission;
    [System.NonSerializedAttribute] [HideInInspector] public Vector3 Spawnposition;
    [System.NonSerializedAttribute] [HideInInspector] public Vector3 Spawnrotation;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool isfiring = false;
    private float LTrigger = 0;
    private void Start()
    {
        if (VehicleMainObj != null) { PlaneAnimator = VehicleMainObj.GetComponent<Animator>(); }
        if (PilotSeatStation != null) { PilotSeat1 = PilotSeatStation.gameObject.GetComponent<PilotSeat>(); }
        if (PassengerSeatStation != null) { PassengerSeat1 = PassengerSeatStation.gameObject.GetComponent<PassengerSeat>(); }
        if (MachVapor != null) { emmachvapor = MachVapor.emission; }
        if (AoAVapor != null) { emaoavapor = AoAVapor.emission; }
        if (GVaporL != null) { emgvaporl = GVaporL.emission; }
        if (GVaporR != null) { emgvaporr = GVaporR.emission; }
        if (EngineControl.localPlayer == null) { DoEffects = 0f; } //not asleep in editor
        Spawnposition = VehicleMainObj.transform.position;
        Spawnrotation = VehicleMainObj.transform.rotation.eulerAngles;
    }
    private void Update()
    {
        if (EngineControl.localPlayer == null || EngineControl.localPlayer != null && (EngineControl.localPlayer.IsOwner(gameObject)))//kill plane if in sea
        {
            if (EngineControl.CenterOfMass.position.y < EngineControl.SeaLevel && !EngineControl.dead)
            {
                if (EngineControl.localPlayer == null)//so it works in editor
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
        if (EngineControl.SoundControl != null && EngineControl.SoundControl.ThisFrameDist > 2000f && !EngineControl.dead && !EngineControl.localPlayer.IsOwner(gameObject)) { DoVapor(); return; }//udonsharp doesn't support goto yet, so i'm usnig a function instead //vapor is visible from a long way away so only do vapor if far away.

        if (EngineControl.Occupied == true) { DoEffects = 0f; }
        else { DoEffects += Time.deltaTime; }

        machspeed = EngineControl.CurrentVel.magnitude / 343f;

        if (EngineControl.localPlayer == null || (EngineControl.localPlayer.IsOwner(gameObject)))//works in editor or ingame
        {
            if (EngineControl.localPlayer == null || EngineControl.Piloting)
            {
                LTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
                //Firing the gun
                if (LTrigger >= 0.75 || Input.GetKey(KeyCode.Space))
                {
                    isfiring = true;
                }
                else
                {
                    isfiring = false;
                }
                //Deploy Flares
                RGrip = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryHandTrigger");
                if (RGrip >= 0.75 && !RGriplastframetrigger || (Input.GetKeyDown(KeyCode.X)))
                {
                    if (EngineControl.localPlayer == null) { PlaneAnimator.SetTrigger("flares"); }//editor
                    else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "DropFlares"); }//ingame
                    if (!Input.GetKeyDown(KeyCode.X))
                    {
                        RGriplastframetrigger = true;
                    }
                }
                else if (RGrip < 0.75 && RGriplastframetrigger)
                {
                    RGriplastframetrigger = false;
                }
                //Display Smoke
                LGrip = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryHandTrigger");
                if (LGrip >= 0.75 && !LGriplastframetrigger || (Input.GetKeyDown(KeyCode.C)))
                {
                    Smoking = !Smoking;
                    if (!Input.GetKeyDown(KeyCode.C))
                    {
                        LGriplastframetrigger = true;
                    }
                }
                else if (LGrip < 0.75 && LGriplastframetrigger)
                {
                    LGriplastframetrigger = false;
                }

                roll = EngineControl.rollinput * 35;
                yaw = EngineControl.yawinput * 20;
                pitch = EngineControl.pitchinput * 25;
            }
            else
            {
                roll = EngineControl.rollinput;
                yaw = EngineControl.yawinput;
                pitch = EngineControl.pitchinput;
                roll *= 35;
                yaw *= 20;
                pitch *= 25;
            }


            //G Damage
            if (!EngineControl.dead)
            {
                EngineControl.Health += -Mathf.Clamp((EngineControl.Gs - MaxGs) * Time.deltaTime * GDamage, 0f, 99999f); //take damage of 15 per second per G above MaxGs

                if (EngineControl.Health <= 0f) //plane is ded
                {
                    if (EngineControl.localPlayer == null)//so it works in editor
                    {
                        Explode();
                    }
                    else
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Explode");
                    }
                }
            }
        }
        if (EngineControl.CurrentVel.magnitude > 80) { vapor = true; } // only make vapor when going above "80m/s", prevents vapour appearing when taxiing into a wall or whatever       
        else { vapor = false; }

        if (isfiring && EngineControl.Occupied) //send firing to animator
        {
            PlaneAnimator.SetBool("gunfiring", true);
        }
        else
        {
            PlaneAnimator.SetBool("gunfiring", false);
        }
        if (EngineControl.Flaps)
        {
            PlaneAnimator.SetBool("flaps", true);
        }
        else
        {
            PlaneAnimator.SetBool("flaps", false);
        }

        if (EngineControl.GearUp)
        {
            PlaneAnimator.SetBool("gearup", true);
        }
        else
        {
            PlaneAnimator.SetBool("gearup", false);
        }

        if (AileronL != null) { aileronLlerper.y = Mathf.Lerp(aileronLlerper.y, roll + (-pitch * .5f), 4.5f * Time.deltaTime); ; AileronL.localRotation = Quaternion.Euler(aileronLlerper); }
        if (AileronR != null) { aileronRlerper.y = Mathf.Lerp(aileronRlerper.y, -roll + (-pitch * .5f), 4.5f * Time.deltaTime); ; AileronR.localRotation = Quaternion.Euler(aileronRlerper); }

        pitchlerper.x = Mathf.Lerp(pitchlerper.x, pitch, 4.5f * Time.deltaTime);
        if (Elevator != null) { Elevator.localRotation = Quaternion.Euler(-pitchlerper); }
        if (Canards != null) { Canards.localRotation = Quaternion.Euler(pitchlerper * .5f); }

        yawlerper.y = Mathf.Lerp(yawlerper.y, yaw, 4.5f * Time.deltaTime);
        if (RudderL != null) { RudderL.localRotation = Quaternion.Euler(-yawlerper); }
        if (RudderR != null) { RudderR.localRotation = Quaternion.Euler(yawlerper); }

        SlatsLerper.y = Mathf.Lerp(SlatsLerper.y, Mathf.Max((-EngineControl.CurrentVel.magnitude * 0.005f) * 35f + 35f, 0f), 4.5f * Time.deltaTime); //higher the speed, closer to 0 rot the slats get.
        if (SlatsL != null) { SlatsL.localRotation = Quaternion.Euler(SlatsLerper); }
        if (SlatsR != null) { SlatsR.localRotation = Quaternion.Euler(SlatsLerper); }


        if (EngineL != null) { enginelerperL.x = Mathf.Lerp(enginelerperL.x, (roll * .3f) + (-pitch * .65f), 4.5f * Time.deltaTime); EngineL.localRotation = Quaternion.Euler(enginelerperL); }
        if (EngineR != null) { enginelerperR.x = Mathf.Lerp(enginelerperR.x, (-roll * .3f) + (-pitch * .65f), 4.5f * Time.deltaTime); EngineR.localRotation = Quaternion.Euler(enginelerperR); }


        enginefirelerper.y = Mathf.Lerp(enginefirelerper.y, EngineControl.ThrottleValue * 2, .9f * Time.deltaTime);
        if (EnginefireL != null)
        {
            if (enginefirelerper.y > .06f)
            {
                EnginefireL.gameObject.SetActive(true);
                EnginefireL.localScale = enginefirelerper;
            }
            else
            {
                EnginefireL.gameObject.SetActive(false);
            }
        }
        if (EnginefireR != null)
        {
            if (enginefirelerper.y > .06f)
            {
                EnginefireR.gameObject.SetActive(true);
                EnginefireR.localScale = enginefirelerper;
            }
            else
            {
                EnginefireR.gameObject.SetActive(false);
            }
        }

        if (AoAVapor != null)
        {
            if (Mathf.Abs(EngineControl.AngleOfAttack) > 30 && vapor)
            {
                emaoavapor.enabled = true;
                emaoavapor.rateOverTime = (Mathf.Abs(EngineControl.AngleOfAttack) - 30) * 3 * EngineControl.Gs - 3;
            }
            else
            {
                emaoavapor.enabled = false;
            }
        }
        if (GVaporL != null)
        {
            if (EngineControl.Gs > 3 && vapor)
            {
                emgvaporl.enabled = true;
                emgvaporl.rateOverTime = (EngineControl.Gs - 3) * 40;
            }
            else
            {
                emgvaporl.enabled = false;
            }
        }
        if (GVaporR != null)
        {
            if (EngineControl.Gs > 3 && vapor)
            {
                emgvaporr.enabled = true;
                emgvaporr.rateOverTime = (EngineControl.Gs - 3) * 40;
            }
            else
            {
                emgvaporr.enabled = false;
            }
        }

        if (Smoking && EngineControl.Occupied) { PlaneAnimator.SetBool("displaysmoke", true); }
        else { PlaneAnimator.SetBool("displaysmoke", false); }
        if (!EngineControl.dead)
        {
            PlaneAnimator.SetFloat("health", EngineControl.Health / EngineControl.FullHealth);
        }
        else
        {
            PlaneAnimator.SetFloat("health", 1);//plane is dead, set animator health to full so that there's no phantom healthsmoke
        }

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

        if (WingTrailL != null)
        {
            if (Gs_trail > TrailGs && vapor)
            {

                WingTrailL.emitting = true;
            }
            else
            {

                WingTrailL.emitting = false;
            }
        }
        if (WingTrailR != null)
        {
            if (Gs_trail > TrailGs && vapor)
            {

                WingTrailR.emitting = true;
            }
            else
            {

                WingTrailR.emitting = false;
            }
        }
        if (MachVapor != null)
        {
            if (machspeed > .985 && machspeed < 1.015)
            {
                emmachvapor.enabled = true;
            }
            else
            {
                emmachvapor.enabled = false;
            }

        }
        Gs_trail = EngineControl.Gs;
    }
    public void Explode()//all the things players see happen when the vehicle explodes
    {
        DoEffects = 0f; //keep awake

        EngineControl.dead = true;

        EngineControl.GearUp = true;//prevent touchdown sound

        //pilot and passenger are dropped out of the plane
        if (EngineControl.SoundControl != null && EngineControl.SoundControl.Explosion != null) { EngineControl.SoundControl.Explosion.Play(); }
        if (EngineControl.Piloting)
        {
            if (PilotSeatStation != null) { PilotSeatStation.ExitStation(EngineControl.localPlayer); }
        }
        if (EngineControl.Passenger)
        {
            if (PassengerSeatStation != null) { PassengerSeatStation.ExitStation(EngineControl.localPlayer); }
        }
        PlaneAnimator.SetTrigger("explode");
        EngineControl.Health = EngineControl.FullHealth;//turns off low health smoke
    }
    public void DropFlares()
    {
        PlaneAnimator.SetTrigger("flares");
    }
}