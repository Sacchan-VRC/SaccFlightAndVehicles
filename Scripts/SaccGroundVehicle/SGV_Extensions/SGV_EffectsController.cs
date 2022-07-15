//Sound and Effects for SaccGroundVehicle
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SGV_EffectsController : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SGVControl;
        [Tooltip("Engine sounds to set pitch and doppler, DO NOT ANIMATE PITCH IN THE REVS ANIMATION")]
        public AudioSource[] EngineSounds;
        private Transform[] EngineSoundsT;
        [Tooltip("Add any extra sounds that you want to recieve the doppler effect to this list")]
        public AudioSource[] DopplerSounds;
        [Tooltip("Particle system that plays when vehicle enters water")]
        public ParticleSystem SplashParticle;
        [Tooltip("Only play the splash particle if vehicle is faster than this. Meters/s")]
        public float PlaySplashSpeed = 7;
        public AudioSource EnterWater;
        [Tooltip("Oneshot sound that plays when vehicle enters water and player is outside of the vehicle")]
        public AudioSource EnterWaterOutside;
        [Tooltip("Looping Sound that plays while vehicle is underwater and player is inside")]
        public AudioSource UnderWater;
        [Tooltip("Sounds that can be played when vehicle explodes")]
        public AudioSource[] Explosion;
        [Tooltip("Sounds that can be played when vehicle gets hit by something")]
        public AudioSource[] BulletHit;
        [Tooltip("Sounds that can play when vehicle has a small crash")]
        public AudioSource[] SmallCrash;
        [Tooltip("Sounds that can play when vehicle has a medium crash")]
        public AudioSource[] MediumCrash;
        [Tooltip("Sounds that can play when vehicle has a big crash")]
        public AudioSource[] BigCrash;
        [Tooltip("Sounds that can play when vehicle has a small crash")]
        public AudioSource[] SmallCrashInside;
        [Tooltip("Sounds that can play when vehicle has a medium crash")]
        public AudioSource[] MediumCrashInside;
        [Tooltip("Sounds that can play when vehicle has a big crash")]
        public AudioSource[] BigCrashInside;
        [Tooltip("Oneshot sound sound played each time vehicle recieves a resupply event")]
        public AudioSource ReSupply;
        public AudioSource GearChange;
        [Tooltip("Add any extra sounds that you want to recieve the doppler effect to this list")]
        public Transform testcamera;
        private SaccEntity EntityControl;
        private Animator VehicleAnimator;
        private bool InWater;
        private bool Occupied;
        private bool Sleeping = true;
        private float RevLimiter = 1;
        private float DoEffects = 999f;
        private UdonSharpBehaviour[] DriveWheels;
        private UdonSharpBehaviour[] SteerWheels;
        private UdonSharpBehaviour[] OtherWheels;
        private bool InEditor;
        private bool InVehicle;
        private int dopplecounter;
        private float Doppler;
        private float LastFrameDist;
        private float ThisFrameDist;
        private float relativespeed;
        private bool HasFuel = true;
        private bool IsOwner = true;
        [System.NonSerializedAttribute] public bool SmallCrashNULL = true;
        [System.NonSerializedAttribute] public bool MediumCrashNULL = true;
        [System.NonSerializedAttribute] public bool BigCrashNULL = true;
        [System.NonSerializedAttribute] public bool SmallCrashInsideNULL = true;
        [System.NonSerializedAttribute] public bool MediumCrashInsideNULL = true;
        [System.NonSerializedAttribute] public bool BigCrashInsideNULL = true;
        [System.NonSerializedAttribute] public Vector3[] SmallCrashPos;
        [System.NonSerializedAttribute] public Vector3[] SmallCrashInsidePos;
        [System.NonSerializedAttribute] public Vector3[] MediumCrashPos;
        [System.NonSerializedAttribute] public Vector3[] MediumCrashInsidePos;
        [System.NonSerializedAttribute] public Vector3[] BigCrashPos;
        [System.NonSerializedAttribute] public Vector3[] BigCrashInsidePos;
        private Transform CenterOfMass;
        private int REVS_STRING = Animator.StringToHash("revs");
        private int GROUNDED_STRING = Animator.StringToHash("grounded");
        private int SPEED_STRING = Animator.StringToHash("speed");
        private int THROTTLE_STRING = Animator.StringToHash("throttle");
        private int STEERING_STRING = Animator.StringToHash("steering");
        private int FUEL_STRING = Animator.StringToHash("fuel");
        private int HEALTH_STRING = Animator.StringToHash("health");
        private float FullFuelDivider;
        private float FullHealthDivider;
        [System.NonSerializedAttribute] public bool ExplosionNull = true;
        [System.NonSerializedAttribute] public bool BulletHitNull = true;
        VRCPlayerApi localPlayer;
        public void SFEXT_L_EntityStart()
        {
            VehicleAnimator = ((SaccEntity)SGVControl.GetProgramVariable("EntityControl")).GetComponent<Animator>();
            CenterOfMass = ((SaccEntity)SGVControl.GetProgramVariable("EntityControl")).CenterOfMass;
            DriveWheels = (UdonSharpBehaviour[])SGVControl.GetProgramVariable("DriveWheels");
            SteerWheels = (UdonSharpBehaviour[])SGVControl.GetProgramVariable("SteerWheels");
            OtherWheels = (UdonSharpBehaviour[])SGVControl.GetProgramVariable("OtherWheels");
            localPlayer = Networking.LocalPlayer;
            InEditor = localPlayer == null;
            IsOwner = EntityControl.IsOwner;
            EngineSoundsT = new Transform[EngineSounds.Length];
            for (int i = 0; i < EngineSounds.Length; i++)
            {
                EngineSoundsT[i] = EngineSounds[i].transform;
            }


            FullHealthDivider = 1f / (float)SGVControl.GetProgramVariable("Health");
            FullFuelDivider = 1f / (float)SGVControl.GetProgramVariable("Fuel");
            RevLimiter = (float)SGVControl.GetProgramVariable("RevLimiter");
            EntityControl = (SaccEntity)SGVControl.GetProgramVariable("EntityControl");
            ExplosionNull = Explosion.Length < 1;
            BulletHitNull = BulletHit.Length < 1;
            SmallCrashNULL = SmallCrash.Length < 1;
            MediumCrashNULL = MediumCrash.Length < 1;
            BigCrashNULL = BigCrash.Length < 1;
            SmallCrashInsideNULL = SmallCrashInside.Length < 1;
            MediumCrashInsideNULL = MediumCrashInside.Length < 1;
            BigCrashInsideNULL = BigCrashInside.Length < 1;

            //save original positions of all the crash sounds because non-owners can't set them to the collision contact point
            SmallCrashPos = new Vector3[SmallCrash.Length];
            for (int i = 0; i < SmallCrashPos.Length; i++)
            {
                SmallCrashPos[i] = SmallCrash[i].transform.localPosition;
            }
            SmallCrashInsidePos = new Vector3[SmallCrashInside.Length];
            for (int i = 0; i < SmallCrashInsidePos.Length; i++)
            {
                SmallCrashInsidePos[i] = SmallCrashInside[i].transform.localPosition;
            }
            MediumCrashPos = new Vector3[MediumCrash.Length];
            for (int i = 0; i < MediumCrashPos.Length; i++)
            {
                MediumCrashPos[i] = MediumCrash[i].transform.localPosition;
            }
            MediumCrashInsidePos = new Vector3[MediumCrashInside.Length];
            for (int i = 0; i < MediumCrashInsidePos.Length; i++)
            {
                MediumCrashInsidePos[i] = MediumCrashInside[i].transform.localPosition;
            }
            BigCrashPos = new Vector3[BigCrash.Length];
            for (int i = 0; i < BigCrashPos.Length; i++)
            {
                BigCrashPos[i] = BigCrash[i].transform.localPosition;
            }
            BigCrashInsidePos = new Vector3[BigCrashInside.Length];
            for (int i = 0; i < BigCrashInsidePos.Length; i++)
            {
                BigCrashInsidePos[i] = BigCrashInside[i].transform.localPosition;
            }


            DoEffects = 0f;
            Sleeping = false;
        }
        private void LateUpdate()
        {
            if (DoEffects > 10)
            {
                if (Sleeping)
                { return; }
                else
                {
                    if ((float)SGVControl.GetProgramVariable("VehicleSpeed") < 1)
                    { FallAsleep(); }
                    else
                    { DoEffects--; }
                }
            }
            if (dopplecounter > 4)
            {
                float SmoothDeltaTime = Time.smoothDeltaTime;
                //find distance to player or testcamera
                /*       if ((testcamera != null))//editor and testcamera is set
                      {
                          ThisFrameDist = Vector3.Distance(testcamera.transform.position, CenterOfMass.position);
                      }
                      else
                      { */
                if (!InEditor)
                {
                    ThisFrameDist = Vector3.Distance(localPlayer.GetPosition(), CenterOfMass.position);
                }
                /*   } */


                relativespeed = (ThisFrameDist - LastFrameDist);
                float doppletemp = (343 * (SmoothDeltaTime * 5)) + relativespeed;

                Doppler = (343 * (SmoothDeltaTime * 5)) / doppletemp;
                LastFrameDist = ThisFrameDist;
                dopplecounter = 0;
            }
            dopplecounter++;

            for (int x = 0; x < DopplerSounds.Length; x++)
            {
                DopplerSounds[x].pitch = Doppler;
            }

            for (int i = 0; i < EngineSounds.Length; i++)
            {
                EngineSounds[i].pitch = EngineSoundsT[i].localScale.x * Doppler;
            }
            VehicleAnimator.SetFloat(HEALTH_STRING, (float)SGVControl.GetProgramVariable("Health") * FullHealthDivider);
            VehicleAnimator.SetFloat(FUEL_STRING, (float)SGVControl.GetProgramVariable("Fuel") * FullFuelDivider);
            VehicleAnimator.SetFloat(SPEED_STRING, (float)SGVControl.GetProgramVariable("VehicleSpeed") / 500f);
            VehicleAnimator.SetFloat(THROTTLE_STRING, (float)SGVControl.GetProgramVariable("ThrottleInput"));
            VehicleAnimator.SetFloat(STEERING_STRING, (float)SGVControl.GetProgramVariable("YawInput") * .5f + .5f);
            if (!InWater && HasFuel)
            {
                VehicleAnimator.SetFloat(REVS_STRING, (float)SGVControl.GetProgramVariable("Revs") / RevLimiter);
            }
            if (!Occupied)
            {
                DoEffects += Time.deltaTime;
            }
        }
        public void FallAsleep()
        {
            Sleeping = true;
            VehicleAnimator.SetFloat("throttle", 0);
            VehicleAnimator.SetFloat(REVS_STRING, 0);
            EntityControl.SendEventToExtensions("SFEXT_L_FallAsleep");

            for (int i = 0; i < DriveWheels.Length; i++)
            { DriveWheels[i].SendCustomEvent("FallAsleep"); }
            for (int i = 0; i < SteerWheels.Length; i++)
            { SteerWheels[i].SendCustomEvent("FallAsleep"); }
            for (int i = 0; i < OtherWheels.Length; i++)
            { OtherWheels[i].SendCustomEvent("FallAsleep"); }
        }
        public void SFEXT_G_RespawnButton()
        {
            WakeUp();
        }
        public void WakeUp()
        {
            Sleeping = false;
            DoEffects = 0f;
            EntityControl.SendEventToExtensions("SFEXT_L_WakeUp");

            for (int i = 0; i < DriveWheels.Length; i++)
            { DriveWheels[i].SendCustomEvent("WakeUp"); }
            for (int i = 0; i < SteerWheels.Length; i++)
            { SteerWheels[i].SendCustomEvent("WakeUp"); }
            for (int i = 0; i < OtherWheels.Length; i++)
            { OtherWheels[i].SendCustomEvent("WakeUp"); }
        }
        public void SFEXT_G_EnterWater()
        {
            InWater = true;
            VehicleAnimator.SetFloat(REVS_STRING, 0);
            if ((float)SGVControl.GetProgramVariable("VehicleSpeed") > PlaySplashSpeed && SplashParticle) { SplashParticle.Play(); }
            VehicleAnimator.SetBool("underwater", true);
            if (InVehicle)
            {
                if (EnterWater) { EnterWater.Play(); }
                if (UnderWater) { UnderWater.Play(); }
            }
            else
            {
                if (EnterWaterOutside) { EnterWaterOutside.Play(); }
            }
        }
        public void SFEXT_G_ExitWater()
        {
            InWater = false;
            VehicleAnimator.SetBool("underwater", false);
            if (UnderWater) { if (UnderWater.isPlaying) UnderWater.Stop(); }
        }
        public void SFEXT_G_PilotEnter()
        {
            Occupied = true;
            WakeUp();
            VehicleAnimator.SetBool("occupied", true);
        }
        public void SFEXT_G_PilotExit()
        {
            Occupied = false;
            VehicleAnimator.SetBool("occupied", false);
        }
        public void SFEXT_O_PilotEnter()
        {
            InVehicle = true;
            VehicleAnimator.SetBool("insidevehicle", true);
            VehicleAnimator.SetBool("piloting", true);

            SendWheelEnter();
        }
        public void SFEXT_O_PilotExit()
        {
            InVehicle = false;
            VehicleAnimator.SetBool("insidevehicle", false);
            VehicleAnimator.SetBool("piloting", false);
            if (UnderWater) { if (UnderWater.isPlaying) UnderWater.Stop(); }

            SendWheelExit();
        }
        public void SFEXT_P_PassengerEnter()
        {
            InVehicle = true;
            VehicleAnimator.SetBool("insidevehicle", true);
            VehicleAnimator.SetBool("passenger", true);

            SendWheelEnter();
        }
        public void SFEXT_P_PassengerExit()
        {
            InVehicle = false;
            VehicleAnimator.SetBool("insidevehicle", false);
            VehicleAnimator.SetBool("passenger", false);
            if (UnderWater) { if (UnderWater.isPlaying) UnderWater.Stop(); }

            SendWheelExit();
        }
        public void SendWheelExit()
        {
            for (int i = 0; i < DriveWheels.Length; i++)
            { DriveWheels[i].SendCustomEvent("PlayerExitVehicle"); }
            for (int i = 0; i < SteerWheels.Length; i++)
            { SteerWheels[i].SendCustomEvent("PlayerExitVehicle"); }
            for (int i = 0; i < OtherWheels.Length; i++)
            { OtherWheels[i].SendCustomEvent("PlayerExitVehicle"); }
        }
        public void SendWheelEnter()
        {
            for (int i = 0; i < DriveWheels.Length; i++)
            { DriveWheels[i].SendCustomEvent("PlayerEnterVehicle"); }
            for (int i = 0; i < SteerWheels.Length; i++)
            { SteerWheels[i].SendCustomEvent("PlayerEnterVehicle"); }
            for (int i = 0; i < OtherWheels.Length; i++)
            { OtherWheels[i].SendCustomEvent("PlayerEnterVehicle"); }
        }
        public void SFEXT_G_ReSupply()
        {
            SendCustomEventDelayedFrames("ResupplySound", 1);
        }
        public void ResupplySound()
        {
            if ((int)SGVControl.GetProgramVariable("ReSupplied") > 0)
            {
                if (ReSupply)
                {
                    ReSupply.Play();
                }
            }
        }
        public void SFEXT_G_ChangeGear()
        {
            if (GearChange) { GearChange.PlayOneShot(GearChange.clip); }
            VehicleAnimator.SetInteger("currentgear", (int)SGVControl.GetProgramVariable("CurrentGear"));
        }
        public void SFEXT_L_OnCollisionEnter() { WakeUp(); }
        public void SFEXT_G_SmallCrash()
        {
            if (InVehicle)
            {
                if (SmallCrashInsideNULL) { return; }
                int rand = Random.Range(0, SmallCrashInside.Length);
                SmallCrashInside[rand].pitch = Random.Range(.8f, 1.2f);
                if (IsOwner)
                { SmallCrashInside[rand].transform.position = EntityControl.LastCollisionEnter.GetContact(0).point; }
                else
                { if (EntityControl.LastCollisionEnter != null) { SmallCrashInside[rand].transform.localPosition = SmallCrashInsidePos[rand]; } }
                SmallCrashInside[rand].PlayOneShot(SmallCrashInside[rand].clip);
            }
            else
            {
                if (SmallCrashNULL) { return; }
                int rand = Random.Range(0, SmallCrash.Length);
                SmallCrash[rand].pitch = Random.Range(.8f, 1.2f);
                if (IsOwner)
                { SmallCrash[rand].transform.position = EntityControl.LastCollisionEnter.GetContact(0).point; }
                else
                { if (EntityControl.LastCollisionEnter != null) { SmallCrash[rand].transform.localPosition = SmallCrashPos[rand]; } }
                SmallCrash[rand].PlayOneShot(SmallCrash[rand].clip);
            }
        }
        public void SFEXT_G_MediumCrash()
        {
            if (InVehicle)
            {
                if (MediumCrashInsideNULL) { return; }
                int rand = Random.Range(0, MediumCrashInside.Length);
                MediumCrashInside[rand].pitch = Random.Range(.8f, 1.2f);
                if (IsOwner)
                { MediumCrashInside[rand].transform.position = EntityControl.LastCollisionEnter.GetContact(0).point; }
                else
                { if (EntityControl.LastCollisionEnter != null) { MediumCrashInside[rand].transform.localPosition = MediumCrashInsidePos[rand]; } }
                MediumCrashInside[rand].PlayOneShot(MediumCrashInside[rand].clip);
            }
            else
            {
                if (MediumCrashNULL) { return; }
                int rand = Random.Range(0, MediumCrash.Length);
                MediumCrash[rand].pitch = Random.Range(.8f, 1.2f);
                if (IsOwner)
                { MediumCrash[rand].transform.position = EntityControl.LastCollisionEnter.GetContact(0).point; }
                else
                { if (EntityControl.LastCollisionEnter != null) { MediumCrash[rand].transform.localPosition = MediumCrashPos[rand]; } }
                MediumCrash[rand].PlayOneShot(MediumCrash[rand].clip);
            }
        }
        public void SFEXT_G_BigCrash()
        {
            if (InVehicle)
            {
                if (BigCrashInsideNULL) { return; }
                int rand = Random.Range(0, BigCrashInside.Length);
                BigCrashInside[rand].pitch = Random.Range(.8f, 1.2f);
                if (IsOwner)
                { BigCrashInside[rand].transform.position = EntityControl.LastCollisionEnter.GetContact(0).point; }
                else
                { if (EntityControl.LastCollisionEnter != null) { BigCrashInside[rand].transform.localPosition = BigCrashInsidePos[rand]; } }
                BigCrashInside[rand].PlayOneShot(BigCrashInside[rand].clip);
            }
            else
            {
                if (BigCrashNULL) { return; }
                int rand = Random.Range(0, BigCrash.Length);
                BigCrash[rand].pitch = Random.Range(.8f, 1.2f);
                if (IsOwner)
                { BigCrash[rand].transform.position = EntityControl.LastCollisionEnter.GetContact(0).point; }
                else
                { if (EntityControl.LastCollisionEnter != null) { BigCrash[rand].transform.localPosition = BigCrashPos[rand]; } }
                BigCrash[rand].PlayOneShot(BigCrash[rand].clip);
            }
        }
        public void SFEXT_O_TakeOwnership()
        {
            IsOwner = true;
        }
        public void SFEXT_O_LoseOwnership()
        {
            IsOwner = false;
        }
        public void SFEXT_G_BulletHit()
        {
            if (!BulletHitNull)
            {
                int rand = Random.Range(0, BulletHit.Length);
                BulletHit[rand].pitch = Random.Range(.8f, 1.2f);
                BulletHit[rand].PlayOneShot(BulletHit[rand].clip);
            }
        }
        public void SFEXT_G_ReAppear()
        {
            WakeUp();
        }
        public void SFEXT_G_Explode()
        {
            VehicleAnimator.SetFloat(HEALTH_STRING, (float)SGVControl.GetProgramVariable("Health") * FullHealthDivider);
            VehicleAnimator.SetFloat(FUEL_STRING, (float)SGVControl.GetProgramVariable("Fuel") * FullFuelDivider);
            VehicleAnimator.SetFloat(SPEED_STRING, (float)SGVControl.GetProgramVariable("VehicleSpeed") / 500f);
            VehicleAnimator.SetFloat(THROTTLE_STRING, (float)SGVControl.GetProgramVariable("ThrottleInput"));
            VehicleAnimator.SetFloat(STEERING_STRING, (float)SGVControl.GetProgramVariable("YawInput") * .5f + .5f);
            VehicleAnimator.SetFloat(REVS_STRING, 0f);

            VehicleAnimator.SetTrigger("explode");
            VehicleAnimator.SetBool("dead", true);
            VehicleAnimator.SetInteger("missilesincoming", 0);
            if (!InEditor) { VehicleAnimator.SetBool("occupied", false); }
            if (!ExplosionNull)
            {
                int rand = Random.Range(0, Explosion.Length);
                if (Explosion[rand])
                {
                    Explosion[rand].Play();
                }
            }
            FallAsleep();
        }
        public void SFEXT_O_Grounded()
        {
            VehicleAnimator.SetBool(GROUNDED_STRING, true);
        }
        public void SFEXT_O_Airborne()
        {
            VehicleAnimator.SetBool(GROUNDED_STRING, false);
        }
        public void SFEXT_G_HasFuel()
        {
            HasFuel = true;
        }
        public void SFEXT_G_NoFuel()
        {
            VehicleAnimator.SetFloat(REVS_STRING, 0);
            HasFuel = false;
        }
        public void SFEXT_L_NotDistant()
        {
            for (int i = 0; i < DriveWheels.Length; i++)
            { DriveWheels[i].SetProgramVariable("CurrentlyDistant", false); }
            for (int i = 0; i < SteerWheels.Length; i++)
            { SteerWheels[i].SetProgramVariable("CurrentlyDistant", false); }
            for (int i = 0; i < OtherWheels.Length; i++)
            { OtherWheels[i].SetProgramVariable("CurrentlyDistant", false); }
        }
        public void SFEXT_L_BecomeDistant()
        {
            for (int i = 0; i < DriveWheels.Length; i++)
            { DriveWheels[i].SetProgramVariable("CurrentlyDistant", true); }
            for (int i = 0; i < SteerWheels.Length; i++)
            { SteerWheels[i].SetProgramVariable("CurrentlyDistant", true); }
            for (int i = 0; i < OtherWheels.Length; i++)
            { OtherWheels[i].SetProgramVariable("CurrentlyDistant", true); }
        }
    }
}