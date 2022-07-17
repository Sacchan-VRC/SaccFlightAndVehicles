
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(15)]//after SaccAirVehicle
    public class SAV_EffectsController : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        [Tooltip("Wing trails, emit when pulling Gs")]
        public TrailRenderer[] Trails;
        [Tooltip("How many Gs do you have to pull before the trails appear?")]
        public float TrailGs = 4;
        [Tooltip("Particle system that plays when vehicle enters water")]
        public ParticleSystem SplashParticle;
        [Tooltip("Only play the splash particle if vehicle is faster than this. Meters/s")]
        public float PlaySplashSpeed = 7;
        [Header("Wheel Posing/Rolling")]
        [Header("Both Arrays must be the same length")]
        [Tooltip("List of all wheel colliders to use")]
        public WheelCollider[] WheelColliders;
        [Tooltip("List of mesh objects or bones to rotate and position to the wheel collider pose")]
        public Transform[] WheelVisuals;
        private Vector3[] WheelStartPos;
        private float[] WheelRotations;
        private float[] WheelRadii;
        [Tooltip("Wheel will only be animated after the gear has finished deploying. This number should match the animation length, and the value in DFUNC_Gear")]
        public float GearTransitionTime = 5;
        [Tooltip("Tick if this vehicle has no gear toggle functionality")]
        public bool NoGearFunction = false;
        private bool GearDown = false;
        private bool TrailsOn;
        private bool HasTrails;
        private bool vapor;
        private float Gs_trail = 1000;//ensures trails wont emit at first frame
        [System.NonSerializedAttribute] public Animator VehicleAnimator;
        [System.NonSerializedAttribute] public float DoEffects = 999f;//don't do effects before initialized
        private float brake;
        private float FullHealthDivider;
        private float FullFuelDivider;
        private Vector3 OwnerRotationInputs;
        private VRCPlayerApi localPlayer;
        private bool EngineOn;
        private bool DoWheelPose = false;
        private bool Occupied;
        private bool IsOwner;
        private bool InVR;
        private bool InEditor = true;
        //animator strings that are sent every frame are converted to int for optimization
        private int PITCHINPUT_STRING = Animator.StringToHash("pitchinput");
        private int YAWINPUT_STRING = Animator.StringToHash("yawinput");
        private int ROLLINPUT_STRING = Animator.StringToHash("rollinput");
        private int THROTTLE_STRING = Animator.StringToHash("throttle");
        private int ENGINEOUTPUT_STRING = Animator.StringToHash("engineoutput");
        private int VTOLANGLE_STRING = Animator.StringToHash("vtolangle");
        private int HEALTH_STRING = Animator.StringToHash("health");
        private int AOA_STRING = Animator.StringToHash("AoA");
        private int MACH10_STRING = Animator.StringToHash("mach10");
        private int GS_STRING = Animator.StringToHash("Gs");
        private int FUEL_STRING = Animator.StringToHash("fuel");
        public bool PrintAnimHashNamesOnStart;

        public void SFEXT_L_EntityStart()
        {
            FullHealthDivider = 1f / (float)SAVControl.GetProgramVariable("Health");
            HasTrails = Trails.Length > 0;

            SaccEntity EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
            VehicleAnimator = EntityControl.GetComponent<Animator>();
            localPlayer = Networking.LocalPlayer;
            float fuel = (float)SAVControl.GetProgramVariable("Fuel");
            FullFuelDivider = 1f / (fuel > 0 ? fuel : 10000000);
            if (localPlayer == null)
            {
                EngineOn = true;
                VehicleAnimator.SetBool("occupied", true);
            }
            else { InEditor = false; }
            IsOwner = EntityControl.IsOwner;

            if (PrintAnimHashNamesOnStart)
            { PrintStringHashes(); }
            DoEffects = 6f;
            if (WheelVisuals.Length > 0 && WheelVisuals.Length == WheelColliders.Length)
            {
                DoWheelPose = true;
                WheelRotations = new float[WheelColliders.Length];
                WheelRadii = new float[WheelColliders.Length];
                WheelStartPos = new Vector3[WheelColliders.Length];
                for (int i = 0; i < WheelColliders.Length; i++)
                {
                    WheelRadii[i] = WheelColliders[i].radius * WheelColliders[i].transform.lossyScale.magnitude;
                    WheelStartPos[i] = WheelVisuals[i].localPosition;
                }
            }
            if (NoGearFunction)
            { GearDown = true; }
        }
        private void Update()
        {
            if (DoEffects > 10) { return; }

            Effects();
            LargeEffects();
        }
        public void Effects()
        {
            Vector3 RotInputs = (Vector3)SAVControl.GetProgramVariable("RotationInputs");
            float DeltaTime = Time.deltaTime;
            if (IsOwner)
            {
                if (InVR)
                { OwnerRotationInputs = RotInputs; }//vr users use raw input
                else
                { OwnerRotationInputs = Vector3.MoveTowards(OwnerRotationInputs, RotInputs, 7 * DeltaTime); }//desktop users use value movetowards'd to prevent instant movement
                VehicleAnimator.SetFloat(PITCHINPUT_STRING, (OwnerRotationInputs.x * 0.5f) + 0.5f);
                VehicleAnimator.SetFloat(YAWINPUT_STRING, (OwnerRotationInputs.y * 0.5f) + 0.5f);
                VehicleAnimator.SetFloat(ROLLINPUT_STRING, (OwnerRotationInputs.z * 0.5f) + 0.5f);
                VehicleAnimator.SetFloat(THROTTLE_STRING, (float)SAVControl.GetProgramVariable("ThrottleInput"));
                VehicleAnimator.SetFloat(ENGINEOUTPUT_STRING, (float)SAVControl.GetProgramVariable("EngineOutput"));
            }
            else
            {
                float EngineOutput = (float)SAVControl.GetProgramVariable("EngineOutput");
                VehicleAnimator.SetFloat(PITCHINPUT_STRING, (RotInputs.x * 0.5f) + 0.5f);
                VehicleAnimator.SetFloat(YAWINPUT_STRING, (RotInputs.y * 0.5f) + 0.5f);
                VehicleAnimator.SetFloat(ROLLINPUT_STRING, (RotInputs.z * 0.5f) + 0.5f);
                VehicleAnimator.SetFloat(THROTTLE_STRING, EngineOutput);//non-owners use value that is similar, but smoothed and would feel bad if the pilot used it himself
                VehicleAnimator.SetFloat(ENGINEOUTPUT_STRING, EngineOutput);
            }
            if (Occupied || EngineOn)
            {
                DoEffects = 0f;
                VehicleAnimator.SetFloat(FUEL_STRING, (float)SAVControl.GetProgramVariable("Fuel") * FullFuelDivider);
            }
            else { DoEffects += DeltaTime; }

            VehicleAnimator.SetFloat(VTOLANGLE_STRING, (float)SAVControl.GetProgramVariable("VTOLAngle"));

            vapor = (float)SAVControl.GetProgramVariable("Speed") > 20;// only make vapor when going above "20m/s", prevents vapour appearing when taxiing into a wall or whatever

            VehicleAnimator.SetFloat(HEALTH_STRING, (float)SAVControl.GetProgramVariable("Health") * FullHealthDivider);
            VehicleAnimator.SetFloat(AOA_STRING, vapor ? Mathf.Abs((float)SAVControl.GetProgramVariable("AngleOfAttack") * 0.00555555556f /* Divide by 180 */ ) : 0);

            if (DoWheelPose)
            {
                if (GearDown)
                {
                    if (IsOwner)
                    {
                        for (int i = 0; i < WheelVisuals.Length; i++)
                        {
                            Vector3 pos;
                            Quaternion rot;
                            WheelColliders[i].GetWorldPose(out pos, out rot);
                            WheelVisuals[i].position = pos;
                            WheelVisuals[i].rotation = rot;
                        }
                    }
                    else
                    {
                        float VehSpeed = (float)SAVControl.GetProgramVariable("Speed");
                        for (int i = 0; i < WheelVisuals.Length; i++)
                        {
                            WheelRotations[i] += VehSpeed / WheelRadii[i];
                            Quaternion newrot = Quaternion.AngleAxis(WheelRotations[i], Vector3.right);
                            WheelVisuals[i].localRotation = newrot;
                        }
                    }
                }
            }
        }
        private void LargeEffects()//large effects visible from a long distance
        {
            float DeltaTime = Time.deltaTime;

            if (HasTrails)
            {
                //this is to finetune when wingtrails appear and disappear
                float vertgs = Mathf.Abs((float)SAVControl.GetProgramVariable("VertGs"));
                if (vertgs > Gs_trail) //Gs are increasing
                {
                    Gs_trail = Mathf.Lerp(Gs_trail, vertgs, 30f * DeltaTime);//apear fast when pulling Gs
                    if (!TrailsOn && Gs_trail > TrailGs)
                    {
                        TrailsOn = true;
                        for (int x = 0; x < Trails.Length; x++)
                        { Trails[x].emitting = true; }
                    }
                }
                else //Gs are decreasing
                {
                    Gs_trail = Mathf.Lerp(Gs_trail, vertgs, 2.7f * DeltaTime);//linger for a bit before cutting off
                    if (TrailsOn && Gs_trail < TrailGs)
                    {
                        TrailsOn = false;
                        for (int x = 0; x < Trails.Length; x++)
                        { Trails[x].emitting = false; }
                    }
                }
            }
            //("mach10", EngineControl.Speed / 343 / 10)
            VehicleAnimator.SetFloat(MACH10_STRING, (float)SAVControl.GetProgramVariable("Speed") * 0.000291545189504373f);//should be airspeed but nonlocal players don't have it
                                                                                                                           //("Gs", vapor ? EngineControl.Gs / 200 + .5f : 0) (.5 == 0 Gs, 1 == 100Gs, 0 == -100Gs)
            VehicleAnimator.SetFloat(GS_STRING, vapor ? ((float)SAVControl.GetProgramVariable("VertGs") * 0.005f) + 0.5f : 0.5f);
        }
        public void GearDownEvent()
        {
            GearDown = true;
        }
        public void SFEXT_G_GearDown()
        {
            SendCustomEventDelayedSeconds(nameof(GearDownEvent), GearTransitionTime);
        }
        public void SFEXT_G_GearUp()
        {
            GearDown = false;
        }
        public void SFEXT_G_PilotEnter()
        {
            Occupied = true;
            DoEffects = 0f;
            VehicleAnimator.SetBool("occupied", true);
        }
        public void SFEXT_G_EngineOn()
        {
            EngineOn = true;
        }
        public void SFEXT_G_EngineOff()
        {
            EngineOn = false;
        }
        public void SFEXT_G_PilotExit()
        {
            Occupied = false;
            VehicleAnimator.SetBool("occupied", false);
            VehicleAnimator.SetInteger("missilesincoming", 0);
        }
        public void SFEXT_O_PilotEnter()
        {
            if (!InEditor) { InVR = localPlayer.IsUserInVR(); }
            VehicleAnimator.SetBool("localpilot", true);
        }
        public void SFEXT_O_PilotExit()
        {
            VehicleAnimator.SetBool("localpilot", false);
        }
        public void SFEXT_P_PassengerEnter()
        {
            VehicleAnimator.SetBool("localpassenger", true);
        }
        public void SFEXT_P_PassengerExit()
        {
            VehicleAnimator.SetBool("localpassenger", false);
            VehicleAnimator.SetInteger("missilesincoming", 0);
        }
        public void SFEXT_G_ReAppear()
        {
            DoEffects = 6f; //wake up if was asleep
            VehicleAnimator.SetTrigger("reappear");
            VehicleAnimator.SetBool("dead", false);
        }
        public void SFEXT_G_AfterburnerOn()
        {
            VehicleAnimator.SetBool("afterburneron", true);
        }
        public void SFEXT_G_AfterburnerOff()
        {
            VehicleAnimator.SetBool("afterburneron", false);
        }
        public void SFEXT_G_ReSupply()
        {
            VehicleAnimator.SetTrigger("resupply");
        }
        public void SFEXT_G_BulletHit()
        {
            WakeUp();
            VehicleAnimator.SetTrigger("bullethit");
        }
        public void WakeUp()
        {
            DoEffects = 0f;
        }
        public void SFEXT_G_EnterWater()
        {
            if ((float)SAVControl.GetProgramVariable("Speed") > PlaySplashSpeed && SplashParticle) { SplashParticle.Play(); }
            VehicleAnimator.SetBool("underwater", true);
        }
        public void SFEXT_G_ExitWater()
        {
            VehicleAnimator.SetBool("underwater", false);
        }
        public void SFEXT_G_TakeOff()
        {
            VehicleAnimator.SetBool("onground", false);
            VehicleAnimator.SetBool("onwater", false);
        }
        public void SFEXT_G_TouchDown()
        {
            VehicleAnimator.SetBool("onground", true);
        }
        public void SFEXT_G_TouchDownWater()
        {
            VehicleAnimator.SetBool("onwater", true);
        }
        public void SFEXT_O_TakeOwnership()
        {
            IsOwner = true;
        }
        public void SFEXT_O_LoseOwnership()
        {
            IsOwner = false;
            if (DoWheelPose)
            {
                for (int i = 0; i < WheelColliders.Length; i++)
                {
                    WheelVisuals[i].localPosition = WheelStartPos[i];
                }
            }
        }
        public void SFEXT_G_RespawnButton()
        {
            DoEffects = 6;
        }
        public void SFEXT_G_Explode()//old EffectsExplode()
        {
            VehicleAnimator.SetTrigger("explode");
            VehicleAnimator.SetBool("dead", true);
            VehicleAnimator.SetInteger("missilesincoming", 0);
            VehicleAnimator.SetFloat(PITCHINPUT_STRING, .5f);
            VehicleAnimator.SetFloat(YAWINPUT_STRING, .5f);
            VehicleAnimator.SetFloat(ROLLINPUT_STRING, .5f);
            VehicleAnimator.SetFloat(THROTTLE_STRING, 0);
            VehicleAnimator.SetFloat(ENGINEOUTPUT_STRING, 0);
            if (!InEditor) { VehicleAnimator.SetBool("occupied", false); }
            DoEffects = 0f;//keep awake
        }
        public void SFEXT_L_AAMTargeted()//sent locally by the person who's locking onto this plane
        {
            PlayLockedAAM();
        }
        public void PlayLockedAAM()
        {
            if ((bool)SAVControl.GetProgramVariable("Piloting") || (bool)SAVControl.GetProgramVariable("Passenger"))
            { VehicleAnimator.SetTrigger("radarlocked"); }
        }
        private void PrintStringHashes()
        {
            Debug.Log(string.Concat("PITCHINPUT_STRING : ", PITCHINPUT_STRING));
            Debug.Log(string.Concat("YAWINPUT_STRING : ", YAWINPUT_STRING));
            Debug.Log(string.Concat("ROLLINPUT_STRING : ", ROLLINPUT_STRING));
            Debug.Log(string.Concat("THROTTLE_STRING : ", THROTTLE_STRING));
            Debug.Log(string.Concat("ENGINEOUTPUT_STRING : ", ENGINEOUTPUT_STRING));
            Debug.Log(string.Concat("VTOLANGLE_STRING : ", VTOLANGLE_STRING));
            Debug.Log(string.Concat("HEALTH_STRING : ", HEALTH_STRING));
            Debug.Log(string.Concat("AOA_STRING : ", AOA_STRING));
            Debug.Log(string.Concat("MACH10_STRING : ", MACH10_STRING));
            Debug.Log(string.Concat("GS_STRING : ", GS_STRING));
            Debug.Log(string.Concat("FUEL_STRING : ", FUEL_STRING));
        }
    }
}