
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SSV_EffectsController : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SSVControl;
        [Tooltip("Particle system that plays when vehicle enters water")]
        public ParticleSystem SplashParticle;
        [Tooltip("Only play the splash particle if vehicle is faster than this. Meters/s")]
        public float PlaySplashSpeed = 7;
        public Transform[] FlatWaterEffects;
        public UdonSharpBehaviour FloatScript;
        [System.NonSerializedAttribute] public Animator VehicleAnimator;
        [System.NonSerializedAttribute] public float DoEffects = 999f;//don't do effects before initialized
        private float brake;
        private float FullHealthDivider;
        private Vector3 OwnerRotationInputs;
        private Vector3[] FlatWaterFXLocalSpawnPos;
        private VRCPlayerApi localPlayer;
        private int FlatWaterEffectsLength;
        private float FullFuelDivider;
        private bool Occupied;
        private bool InVR;
        private bool InEditor = true;
        private int YAWINPUT_STRING = Animator.StringToHash("yawinput");
        private int THROTTLE_STRING = Animator.StringToHash("throttle");
        private int ENGINEOUTPUT_STRING = Animator.StringToHash("engineoutput");
        private int HEALTH_STRING = Animator.StringToHash("health");
        private int MACH10_STRING = Animator.StringToHash("mach10");
        private int FUEL_STRING = Animator.StringToHash("fuel");
        public bool PrintAnimHashNamesOnStart;

        public void SFEXT_L_EntityStart()
        {
            FullHealthDivider = 1f / (float)SSVControl.GetProgramVariable("Health");
            float fuel = (float)SSVControl.GetProgramVariable("Fuel");
            FullFuelDivider = 1f / (fuel > 0 ? fuel : 10000000);

            VehicleAnimator = ((SaccEntity)SSVControl.GetProgramVariable("EntityControl")).GetComponent<Animator>();
            localPlayer = Networking.LocalPlayer;
            if (localPlayer == null)
            {
                VehicleAnimator.SetBool("occupied", true);
            }
            else { InEditor = false; }

            if (PrintAnimHashNamesOnStart)
            { PrintStringHashes(); }
            DoEffects = 6;
            FlatWaterEffectsLength = FlatWaterEffects.Length;
            FlatWaterFXLocalSpawnPos = new Vector3[FlatWaterEffects.Length];
            for (int x = 0; x < FlatWaterEffectsLength; x++)
            {
                FlatWaterFXLocalSpawnPos[x] = FlatWaterEffects[x].localPosition;
            }
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
            if (Occupied)
            {
                DoEffects = 0f;
                VehicleAnimator.SetFloat(FUEL_STRING, (float)SSVControl.GetProgramVariable("Fuel") * FullFuelDivider);
            }
            else { DoEffects += DeltaTime; }
            VehicleAnimator.SetFloat(HEALTH_STRING, (float)SSVControl.GetProgramVariable("Health") * FullHealthDivider);
            VehicleAnimator.SetFloat(MACH10_STRING, (float)SSVControl.GetProgramVariable("Speed") * 0.000291545189504373f);//should be airspeed but nonlocal players don't have it

            float watersurface = (float)FloatScript.GetProgramVariable("SurfaceHeight") + .02f;

            for (int x = 0; x < FlatWaterEffectsLength; x++)
            {
                FlatWaterEffects[x].localPosition = FlatWaterFXLocalSpawnPos[x];
                Vector3 pos = FlatWaterEffects[x].position;
                pos.y = watersurface;
                FlatWaterEffects[x].position = pos;
                Vector3 rot = FlatWaterEffects[x].eulerAngles;
                rot.z = 0; rot.x = 0;
                Quaternion newrot = Quaternion.Euler(rot);
                FlatWaterEffects[x].rotation = newrot;
            }
        }
        public void SFEXT_G_PilotEnter()
        {
            DoEffects = 0f;
            VehicleAnimator.SetBool("occupied", true);
            Occupied = true;
        }
        public void SFEXT_G_PilotExit()
        {
            VehicleAnimator.SetBool("occupied", false);
            Occupied = false;
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
            if ((float)SSVControl.GetProgramVariable("Speed") > PlaySplashSpeed && SplashParticle) { SplashParticle.Play(); }
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
        public void SFEXT_G_RespawnButton()
        {
            DoEffects = 6;
        }
        public void SFEXT_G_Explode()//old EffectsExplode()
        {
            VehicleAnimator.SetTrigger("explode");
            VehicleAnimator.SetBool("dead", true);
            VehicleAnimator.SetFloat(YAWINPUT_STRING, .5f);
            VehicleAnimator.SetFloat(THROTTLE_STRING, 0);
            VehicleAnimator.SetFloat(ENGINEOUTPUT_STRING, 0);
            if (!InEditor) { VehicleAnimator.SetBool("occupied", false); }
            DoEffects = 0f;//keep awake
        }
        private void PrintStringHashes()
        {
            Debug.Log(string.Concat("YAWINPUT_STRING : ", YAWINPUT_STRING));
            Debug.Log(string.Concat("THROTTLE_STRING : ", THROTTLE_STRING));
            Debug.Log(string.Concat("ENGINEOUTPUT_STRING : ", ENGINEOUTPUT_STRING));
            Debug.Log(string.Concat("HEALTH_STRING : ", HEALTH_STRING));
            Debug.Log(string.Concat("MACH10_STRING : ", MACH10_STRING));
            Debug.Log(string.Concat("FUEL_STRING : ", FUEL_STRING));
        }
    }
}