﻿using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.UdonNetworkCalling;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DFUNC_Flares : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        [Tooltip("Only needs to be set if you want to be able to hold down the launch key to spam flares")]
        public KeyCode LaunchKey = KeyCode.X;
        public int NumFlares = 60;
        [Range(0, 2)]
        [Tooltip("0 = Chaff(Radar), 1 = Flare(Heat), 2 = Other. Controls what variable is added to in SaccAirVehicle to count active countermeasures, (NumActiveFlares MissilesIncomingHeat, NumActiveChaff MissilesIncomingRadar, NumActiveOtherCM MissilesIncomingOther)")]
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
        public int NumFlare_PerShot = 1;
        [Tooltip("Delay between flares drops when holding the trigger")]
        public float FlareHoldDelay = 0.3f;
        private string[] CMTypes = { "NumActiveChaff", "NumActiveFlares", "NumActiveOtherCM" };//names of variables in SaccAirVehicle
        [System.NonSerialized] public int FullFlares;
        private float reloadspeed;
        private bool Piloting, InVR, Selected;
        private bool TriggerLastFrame;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        private float FlareLaunchTime;
        public void DFUNC_Selected()
        {
            Selected = true;
            TriggerLastFrame = true;
        }
        public void DFUNC_Deselected()
        {
            Selected = false;
            RequestSerialization();
        }
        public void SFEXT_L_EntityStart()
        {
            FullFlares = NumFlares;
            reloadspeed = FullFlares / FullReloadTimeSec;
            if (HUDText_flare_ammo) { HUDText_flare_ammo.text = NumFlares.ToString("F0"); }
            InVR = EntityControl.InVR;
        }
        public void ReInitNumFlares()//set FullFlares then run this to change vehicles max flares
        {
            NumFlares = FullFlares;
            reloadspeed = FullFlares / FullReloadTimeSec;
            if (HUDText_flare_ammo) { HUDText_flare_ammo.text = NumFlares.ToString("F0"); }
        }
        byte numUsers;
        public void SFEXT_G_PilotEnter()
        {
            numUsers++;
            if (numUsers > 1) return;

            gameObject.SetActive(true);
        }
        public void SFEXT_G_PilotExit()
        {
            numUsers--;
            if (numUsers != 0) return;

            gameObject.SetActive(false);
        }
        public void SFEXT_O_PilotEnter()
        {
            Piloting = true;
            InVR = EntityControl.InVR;
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
            { EntityControl.SetProgramVariable("ReSupplied", (int)EntityControl.GetProgramVariable("ReSupplied") + 1); }
            NumFlares = (int)Mathf.Min(NumFlares + Mathf.Max(Mathf.Floor(reloadspeed), 1), FullFlares);
            if (HUDText_flare_ammo) { HUDText_flare_ammo.text = NumFlares.ToString("F0"); }
        }
        public void SFEXT_G_ReArm() { SFEXT_G_ReSupply(); }
        private void Update()
        {
            if (!Piloting) return;
            if (Input.GetKey(LaunchKey) || Selected)
            {
                float Trigger;
                if (LeftDial)
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                else
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

                if (Trigger > 0.75 || Input.GetKey(LaunchKey))
                {
                    if (!TriggerLastFrame)
                    {
                        if (NumFlares > 0)
                        {
                            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(LaunchFlare_Owner));
                        }
                    }
                    else if (NumFlares > 0 && ((Time.time - FlareLaunchTime) > FlareHoldDelay))
                    {///launch every FlareHoldDelay
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(LaunchFlare_Owner));
                    }
                    TriggerLastFrame = true;
                }
                else { TriggerLastFrame = false; }
            }
            else { TriggerLastFrame = false; }
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
        [NetworkCallable]
        public void LaunchFlare_Owner()
        {
            for (int i = 0; i < NumFlare_PerShot; i++)
            {
                LaunchFlare();
            }
        }
    }
}