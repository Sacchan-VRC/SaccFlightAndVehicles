
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
    public float MaxGs = 40f;
    public float GDamage = 30f;
    private PilotSeat PilotSeat1;
    private PassengerSeat PassengerSeat1;
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
    private Vector3 enginefirelerper = new Vector3(1, 0, 1);
    [System.NonSerializedAttribute] [HideInInspector] public float DoEffects = 6f; //4 seconds before sleep so late joiners see effects if someone is already piloting
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool Smoking = false;
    //private float LTrigger;
    [System.NonSerializedAttribute] [HideInInspector] public bool LTriggerlastframe;
    [System.NonSerializedAttribute] [HideInInspector] public bool RTriggerLastFrame;
    [System.NonSerializedAttribute] [HideInInspector] public Vector3 Spawnposition;
    [System.NonSerializedAttribute] [HideInInspector] public Vector3 Spawnrotation;
    [System.NonSerializedAttribute] [HideInInspector] [UdonSynced(UdonSyncMode.None)] public bool IsFiringGun = false;
    private float brake;
    private float RTrigger;
    private void Start()
    {
        if (VehicleMainObj != null) { PlaneAnimator = VehicleMainObj.GetComponent<Animator>(); }
        if (PilotSeatStation != null) { PilotSeat1 = PilotSeatStation.gameObject.GetComponent<PilotSeat>(); }
        if (PassengerSeatStation != null) { PassengerSeat1 = PassengerSeatStation.gameObject.GetComponent<PassengerSeat>(); }
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

        if (EngineControl.localPlayer == null || (EngineControl.localPlayer.IsOwner(gameObject)))//works in editor or ingame
        {
            if (EngineControl.localPlayer == null || EngineControl.Piloting)
            {
                PlaneAnimator.SetFloat("throttle", EngineControl.ThrottleInput);
                RTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
                //Firing selected weapon
                if (RTrigger > 0.75 || Input.GetKey(KeyCode.Space))
                {
                    switch (EngineControl.RStickSelection)
                    {
                        case 0://nothing
                            break;
                        case 1://machinegun
                            IsFiringGun = true;
                            break;
                        case 2://smoke for now, will be missiles
                            IsFiringGun = false;
                            if (!RTriggerLastFrame) { Smoking = !Smoking; }
                            break;
                        case 3://bombs soon
                            IsFiringGun = false;
                            break;
                        case 4://flares
                            IsFiringGun = false;
                            if (!RTriggerLastFrame)
                            {
                                if (EngineControl.localPlayer == null) { PlaneAnimator.SetTrigger("flares"); }//editor
                                else { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "DropFlares"); }//ingame
                            }
                            break;
                    }
                    RTriggerLastFrame = true;
                }
                else
                {
                    IsFiringGun = false;
                    RTriggerLastFrame = false;
                }

                /*      //Display Smoke
                     LTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
                     if (LTrigger >= 0.75 && !LTriggerlastframe || (Input.GetKeyDown(KeyCode.C)))
                     {
                         Smoking = !Smoking;
                         if (!Input.GetKeyDown(KeyCode.C))
                         {
                             LTriggerlastframe = true;
                         }
                     }
                     else if (LTrigger < 0.75 && LTriggerlastframe)
                     {
                         LTriggerlastframe = false;
                     } */



            }
            rotationinputs.x = EngineControl.pitchinput * 25;
            rotationinputs.y = EngineControl.yawinput * 20;
            rotationinputs.z = EngineControl.rollinput * 35;

            //joystick movement
            Vector3 tempjoy = new Vector3(rotationinputs.x * 1.8f, -rotationinputs.z * 1.285714f, rotationinputs.y);//x and y to 45 degrees
            JoyStick.localRotation = Quaternion.Euler(tempjoy);

            //G Damage
            if (!EngineControl.dead)
            {
                EngineControl.Health += -Mathf.Clamp((EngineControl.Gs - MaxGs) * Time.deltaTime * GDamage, 0f, 99999f); //take damage of GDamage per second per G above MaxGs

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
        vapor = (EngineControl.CurrentVel.magnitude > 20) ? true : false;// only make vapor when going above "80m/s", prevents vapour appearing when taxiing into a wall or whatever

        if (IsFiringGun && EngineControl.Occupied) //send firing to animator
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

        //rotationinputs.x = pitch
        //rotationinputs.y = yaw
        //rotationinputs.z = roll
        if (AileronL != null) { aileronLlerper.y = Mathf.Lerp(aileronLlerper.y, rotationinputs.z + (-rotationinputs.x * .5f), 4.5f * Time.deltaTime); ; AileronL.localRotation = Quaternion.Euler(aileronLlerper); }
        if (AileronR != null) { aileronRlerper.y = Mathf.Lerp(aileronRlerper.y, -rotationinputs.z + (-rotationinputs.x * .5f), 4.5f * Time.deltaTime); ; AileronR.localRotation = Quaternion.Euler(aileronRlerper); }

        pitchlerper.x = Mathf.Lerp(pitchlerper.x, rotationinputs.x, 4.5f * Time.deltaTime);
        if (Elevator != null) { Elevator.localRotation = Quaternion.Euler(-pitchlerper); }
        if (Canards != null) { Canards.localRotation = Quaternion.Euler(pitchlerper * .5f); }

        yawlerper.y = Mathf.Lerp(yawlerper.y, rotationinputs.y, 4.5f * Time.deltaTime);
        if (RudderL != null) { RudderL.localRotation = Quaternion.Euler(-yawlerper); }
        if (RudderR != null) { RudderR.localRotation = Quaternion.Euler(yawlerper); }

        SlatsLerper.y = Mathf.Lerp(SlatsLerper.y, Mathf.Max((-EngineControl.CurrentVel.magnitude * 0.005f) * 35f + 35f, 0f), 4.5f * Time.deltaTime); //higher the speed, closer to 0 rot the slats get.
        if (SlatsL != null) { SlatsL.localRotation = Quaternion.Euler(SlatsLerper); }
        if (SlatsR != null) { SlatsR.localRotation = Quaternion.Euler(SlatsLerper); }


        if (EngineL != null) { enginelerperL.x = Mathf.Lerp(enginelerperL.x, (rotationinputs.z * .3f) + (-rotationinputs.x * .65f), 4.5f * Time.deltaTime); EngineL.localRotation = Quaternion.Euler(enginelerperL); }
        if (EngineR != null) { enginelerperR.x = Mathf.Lerp(enginelerperR.x, (-rotationinputs.z * .3f) + (-rotationinputs.x * .65f), 4.5f * Time.deltaTime); EngineR.localRotation = Quaternion.Euler(enginelerperR); }


        enginefirelerper.y = Mathf.Lerp(enginefirelerper.y, EngineControl.Throttle * 2, .9f * Time.deltaTime);
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

        PlaneAnimator.SetBool("displaysmoke", (Smoking && EngineControl.Occupied) ? true : false);
        PlaneAnimator.SetFloat("health", EngineControl.Health / EngineControl.FullHealth);
        PlaneAnimator.SetFloat("AoA", vapor ? Mathf.Abs(EngineControl.AngleOfAttack / 180) : 0);
        PlaneAnimator.SetFloat("brake", EngineControl.AirBrake);
        PlaneAnimator.SetBool("occupied", EngineControl.Occupied);
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
        PlaneAnimator.SetFloat("mach10", EngineControl.CurrentVel.magnitude / 343 / 10);
        PlaneAnimator.SetFloat("Gs", vapor ? EngineControl.Gs / 50 : 0);
        PlaneAnimator.SetFloat("Gs_trail", vapor ? Gs_trail / 50 : 0);
    }
    public void Explode()//all the things players see happen when the vehicle explodes
    {
        DoEffects = 0f; //keep awake

        EngineControl.dead = true;

        if (EngineControl.localPlayer == null || EngineControl.localPlayer.IsOwner(gameObject))
        {
            EngineControl.GearUp = true;//prevent touchdown sound
            EngineControl.CurrentVel = Vector3.zero;
            EngineControl.Health = EngineControl.FullHealth;//turns off low health smoke
        }

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

    }
    public void DropFlares()
    {
        PlaneAnimator.SetTrigger("flares");
    }
}