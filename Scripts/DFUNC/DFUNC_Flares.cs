
using BestHTTP.SecureProtocol.Org.BouncyCastle.Ocsp;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_Flares : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        [Tooltip("Only needs to be set if you want to be able to hold down the launch key to spam flares")]
        public KeyCode LaunchKey = KeyCode.None;
        public int NumFlares = 60;
        [Range(0, 2)]
        [Tooltip("1 = Flare(Heat), 1 = Chaff(Radar), 2 = Other. Controls what variable is added to in SaccAirVehicle to count active countermeasures, (NumActiveFlares MissilesIncomingHeat, NumActiveChaff MissilesIncomingRadar, NumActiveOtherCM MissilesIncomingOther)")]
        public int FlareType = 1;
        public ParticleSystem[] FlareParticles;
        [Tooltip("How long a flare has an effect for")]
        public float FlareActiveTime = 4f;
        [Tooltip("How long it takes to fully reload from empty in seconds. Can be inaccurate because it can only reload by integers per resupply")]
        public float FullReloadTimeSec = 15;
        public AudioSource FlareLaunch;
        public Text HUDText_flare_ammo;
        [Tooltip("Launch one particle system per click, cycling through, instead of all at once")]
        public bool SequentialLaunch = false;
        public bool Hol = false;
        public int NumFlare_PerShot = 1;
        [Tooltip("Delay between flares drops when holding the trigger")]
        public float FlareHoldDelay = 0.3f;
        private string[] CMTypes = { "NumActiveChaff", "NumActiveFlares", "NumActiveOtherCM" };//names of variables in SaccAirVehicle
        private bool UseLeftTrigger = false;
        [System.NonSerialized] public int FullFlares;
        private float reloadspeed;
        private bool Piloting, InVR, Selected;
        private bool TriggerLastFrame;
        private SaccEntity EntityControl;
        private float FlareLaunchTime;
        [UdonSynced] private bool FlareFireNow;
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        public void DFUNC_Selected()
        {
            Selected = true;
            TriggerLastFrame = true;
        }
        public void DFUNC_Deselected()
        {
            Selected = false;
                FlareFireNow = false;
                RequestSerialization();
        }
        public void SFEXT_L_EntityStart()
        {
            FullFlares = NumFlares;
            reloadspeed = FullFlares / FullReloadTimeSec;
            if (HUDText_flare_ammo) { HUDText_flare_ammo.text = NumFlares.ToString("F0"); }
            EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
            InVR = EntityControl.InVR;
        }
        public void ReInitNumFlares()//set FullFlares then run this to change vehicles max flares
        {
            NumFlares = FullFlares;
            reloadspeed = FullFlares / FullReloadTimeSec;
            if (HUDText_flare_ammo) { HUDText_flare_ammo.text = NumFlares.ToString("F0"); }
        }
        public void SFEXT_G_PilotEnter()
        { gameObject.SetActive(true); }
        public void SFEXT_G_PilotExit()
        { gameObject.SetActive(false); }
        public void SFEXT_O_PilotEnter()
        {
            Piloting = true;
            FlareFireNow = false;
            RequestSerialization();
        }
        public void SFEXT_O_PilotExit()
        {
            Piloting = false;
            Selected = false;
        }
        public void SFEXT_G_RespawnButton()
        {
            NumFlares = FullFlares;
            if (HUDText_flare_ammo) { HUDText_flare_ammo.text = NumFlares.ToString("F0"); }
        }
        public void SFEXT_G_Explode()
        {
            NumFlares = FullFlares;
            if (HUDText_flare_ammo) { HUDText_flare_ammo.text = NumFlares.ToString("F0"); }
        }
        public void SFEXT_G_ReSupply()
        {
            if (NumFlares != FullFlares)
            { SAVControl.SetProgramVariable("ReSupplied", (int)SAVControl.GetProgramVariable("ReSupplied") + 1); }
            NumFlares = (int)Mathf.Min(NumFlares + Mathf.Max(Mathf.Floor(reloadspeed), 1), FullFlares);
            if (HUDText_flare_ammo) { HUDText_flare_ammo.text = NumFlares.ToString("F0"); }
        }
        private void Update()
        {
            if ((Piloting && !InVR) || Selected)
            {
                float Trigger;
                if (UseLeftTrigger)
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                else
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

                if (Trigger > 0.75 || Input.GetKey(LaunchKey))
                {
                    if (!TriggerLastFrame)
                    {
                        if (NumFlares > 0)
                        {
                            LaunchFlare_Owner();
                        }
                    }
                    else if (NumFlares > 0 && ((Time.time - FlareLaunchTime) > FlareHoldDelay))
                    {///launch every FlareHoldDelay
                        LaunchFlare_Owner();
                    }
                    TriggerLastFrame = true;
                }
                else { TriggerLastFrame = false; }
            }
        }
        private int NextFlare;
        public void LaunchFlare()
        {
            FlareLaunchTime = Time.time;
            NumFlares--;
            FlareLaunch.Play();
            if (HUDText_flare_ammo) { HUDText_flare_ammo.text = NumFlares.ToString("F0"); }
            int d = FlareParticles.Length;
            if (SequentialLaunch)
            {
                if (Time.time - FlareLaunchTime > 5) { NextFlare = 0; }
                if (NextFlare < FlareParticles.Length)
                {
                    if (FlareParticles[NextFlare])
                    {
                        if (FlareParticles[NextFlare].emission.burstCount > 0)
                        {
                            FlareParticles[NextFlare].Emit((int)FlareParticles[NextFlare].emission.GetBurst(0).count.constant);
                        }
                        else
                        { FlareParticles[NextFlare].Emit(1); }
                    }
                }
                NextFlare++;
                if (NextFlare == FlareParticles.Length)
                {
                    NextFlare = 0;
                }
            }
            else
            {
                for (int x = 0; x < d; x++)
                {
                    /*      //this is to make flare particles inherit the velocity of the aircraft they were launched from (inherit doesn't work because non-owners don't have access to rigidbody velocity.)
                         var emitParams = new ParticleSystem.EmitParams();
                         Vector3 curspd = (Vector3)SAVControl.GetProgramVariable("CurrentVel");
                         emitParams.velocity = curspd + (FlareParticles[x].transform.forward * FlareLaunchSpeed);
                         FlareParticles[x].Emit(emitParams, 1); */

                    if (FlareParticles[x].emission.burstCount > 0)
                    {
                        FlareParticles[x].Emit((int)FlareParticles[x].emission.GetBurst(0).count.constant);
                    }
                    else
                    { FlareParticles[x].Emit(1); }
                }
            }
            SAVControl.SetProgramVariable(CMTypes[FlareType], (int)SAVControl.GetProgramVariable(CMTypes[FlareType]) + 1);
            SendCustomEventDelayedSeconds("RemoveFlare", FlareActiveTime);
            EntityControl.SendEventToExtensions("SFEXT_G_LaunchFlare");
        }
        public void RemoveFlare()
        {
            { SAVControl.SetProgramVariable(CMTypes[FlareType], (int)SAVControl.GetProgramVariable(CMTypes[FlareType]) - 1); }
        }
        private void LaunchFlare_Owner()
        {
            FireNextSerialization = true;
            RequestSerialization();
            LaunchFlare();
        }
        private bool FireNextSerialization = false;
        public override void OnPreSerialization()
        {
            if (FireNextSerialization)
            {
                FireNextSerialization = false;
                FlareFireNow = true;
            }
        }
        public override void OnPostSerialization(VRC.Udon.Common.SerializationResult result)
        {
            FlareFireNow = false;
        }
        public override void OnDeserialization()
        {
            if (FlareFireNow)
            {
                for (int i = 0; i < NumFlare_PerShot; i++)
                {
                    LaunchFlare();
                }
            }
        }
    }
}