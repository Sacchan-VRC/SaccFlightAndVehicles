
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
        private string[] CMTypes = { "NumActiveFlares", "NumActiveChaff", "NumActiveOtherCM" };//names of variables in SaccAirVehicle
        private bool UseLeftTrigger = false;
        [System.NonSerialized] public int FullFlares;
        private float reloadspeed;
        private bool func_active;
        private bool TriggerLastFrame;
        private SaccEntity EntityControl;
        private float FlareLaunchTime;
        [UdonSynced, FieldChangeCallback(nameof(sendlaunchflare))] private short _SendLaunchFlare = -1;
        public short sendlaunchflare
        {
            set
            {
                _SendLaunchFlare = value;
                if (value > -1)
                { LaunchFlare(); }
            }
            get => _SendLaunchFlare;
        }
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        public void DFUNC_Selected()
        {
            TriggerLastFrame = true;
            func_active = true;
        }
        public void DFUNC_Deselected()
        {
            func_active = false;
            if (SequentialLaunch)
            {
                _SendLaunchFlare = (short)-1;
                RequestSerialization();
            }
        }
        public void SFEXT_L_EntityStart()
        {
            FullFlares = NumFlares;
            reloadspeed = FullFlares / FullReloadTimeSec;
            if (HUDText_flare_ammo) { HUDText_flare_ammo.text = NumFlares.ToString("F0"); }
            EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
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
            if (SequentialLaunch)
            {
                _SendLaunchFlare = (short)-1;
                RequestSerialization();
            }
        }
        public void SFEXT_O_PilotExit()
        {
            func_active = false;
            if (SequentialLaunch)
            {
                _SendLaunchFlare = (short)-1;
                RequestSerialization();
            }
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
            if (func_active)
            {
                float Trigger;
                if (UseLeftTrigger)
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                else
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

                if (Trigger > 0.75)
                {
                    if (!TriggerLastFrame)
                    {
                        if (NumFlares > 0)
                        {
                            Send_LaunchFlare();
                        }
                    }
                    TriggerLastFrame = true;
                }
                else { TriggerLastFrame = false; }
            }
        }
        public void LaunchFlare()
        {
            FlareLaunchTime = Time.time;
            NumFlares--;
            FlareLaunch.Play();
            if (HUDText_flare_ammo) { HUDText_flare_ammo.text = NumFlares.ToString("F0"); }
            int d = FlareParticles.Length;
            if (SequentialLaunch)
            {
                if (_SendLaunchFlare > -1 && _SendLaunchFlare < FlareParticles.Length)
                {
                    if (FlareParticles[_SendLaunchFlare])
                    {
                        if (FlareParticles[_SendLaunchFlare].emission.burstCount > 0)
                        {
                            FlareParticles[_SendLaunchFlare].Emit((int)FlareParticles[_SendLaunchFlare].emission.GetBurst(0).count.constant);
                        }
                        else
                        { FlareParticles[_SendLaunchFlare].Emit(1); }
                    }
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
        public void Send_LaunchFlare()
        {
            if (SequentialLaunch)
            {
                if (_SendLaunchFlare + 1 == FlareParticles.Length || Time.time - FlareLaunchTime > 5)
                { sendlaunchflare = 0; }
                else
                {
                    sendlaunchflare++;
                }
                if (sendlaunchflare == 0) { SendCustomEventDelayedSeconds(nameof(CheckForReset), 5); }
            }
            else
            {
                sendlaunchflare++;
            }
            RequestSerialization();
        }
        public void CheckForReset()
        {
            if (sendlaunchflare == 0)
            {
                sendlaunchflare = (short)-1;
                RequestSerialization();
            }
        }
        public void KeyboardInput()
        {
            if (NumFlares > 0)
            {
                Send_LaunchFlare();
            }
        }
        public void RemoveFlare()
        {
            { SAVControl.SetProgramVariable(CMTypes[FlareType], (int)SAVControl.GetProgramVariable(CMTypes[FlareType]) - 1); }
        }
    }
}