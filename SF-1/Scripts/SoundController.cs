//TO TEST OUTSIDE-OF-PLANE SOUNDS SET -100000 to 100000 on line 202 AND COMMENT OUT ' && !EngineControl.Piloting ' ON LINE 164
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SoundController : UdonSharpBehaviour
{

    public EngineController EngineControl;
    public AudioSource[] PlaneIdle;
    public AudioSource PlaneInside;
    public AudioSource PlaneDistant;
    public AudioSource[] PlaneThrust;
    public AudioSource PlaneABOn;
    public AudioSource[] TouchDown;
    public AudioSource PlaneWind;
    public AudioSource[] SonicBoom;
    public AudioSource[] Explosion;
    public AudioSource GunSound;
    public AudioSource[] BulletHit;
    public AudioSource RadarLocked;
    public AudioSource MissileIncoming;
    public AudioSource AAMTargeting;
    public AudioSource AAMTargetLock;
    public AudioSource AGMLock;
    public AudioSource AGMUnlock;
    public AudioSource Airbrake;
    public AudioSource CatapultLock;
    public AudioSource CatapultLaunch;
    public AudioSource HookLanding;
    public AudioSource MenuSelect;
    [System.NonSerializedAttribute] [HideInInspector] public bool PlaneIdleNull;
    [System.NonSerializedAttribute] [HideInInspector] public bool PlaneInsideNull;
    [System.NonSerializedAttribute] [HideInInspector] public bool PlaneDistantNull;
    [System.NonSerializedAttribute] [HideInInspector] public bool PlaneThrustNull;
    [System.NonSerializedAttribute] [HideInInspector] public bool PlaneABOnNull;
    [System.NonSerializedAttribute] [HideInInspector] public bool TouchDownNull;
    [System.NonSerializedAttribute] [HideInInspector] public bool PlaneWindNull;
    [System.NonSerializedAttribute] [HideInInspector] public bool SonicBoomNull;
    [System.NonSerializedAttribute] [HideInInspector] public bool ExplosionNull;
    [System.NonSerializedAttribute] [HideInInspector] public bool GunSoundNull;
    [System.NonSerializedAttribute] [HideInInspector] public bool BulletHitNull;
    [System.NonSerializedAttribute] [HideInInspector] public bool MissileIncomingNull;
    [System.NonSerializedAttribute] [HideInInspector] public bool RadarLockedNull;
    [System.NonSerializedAttribute] [HideInInspector] public bool AAMTargetingNull;
    [System.NonSerializedAttribute] [HideInInspector] public bool AAMTargetLockNull;
    [System.NonSerializedAttribute] [HideInInspector] public bool AGMTargetLockNull;
    [System.NonSerializedAttribute] [HideInInspector] public bool AGMUnlockNull;
    [System.NonSerializedAttribute] [HideInInspector] public bool AirbrakeNull;
    [System.NonSerializedAttribute] [HideInInspector] public bool CatapultLockNull;
    [System.NonSerializedAttribute] [HideInInspector] public bool CatapultLaunchNull;
    [System.NonSerializedAttribute] [HideInInspector] public bool HookLandingNull;
    [System.NonSerializedAttribute] [HideInInspector] public bool MenuSelectNull;
    public Transform testcamera;
    private bool SuperSonic = false;
    private float IdleDoppleTemp;
    [System.NonSerializedAttribute] [HideInInspector] public float Doppler = 1;
    float LastFrameDist;
    [System.NonSerializedAttribute] [HideInInspector] public float ThisFrameDist = 0;
    private bool Leftplane = false;
    private float PlaneIdlePitch;
    private float PlaneIdleVolume;
    private float PlaneDistantPitch;
    private float PlaneDistantVolume;
    private float PlaneThrustPitch;
    private float PlaneThrustVolume;
    private float LastFramePlaneIdlePitch;
    private float LastFramePlaneThrustPitch;
    private float LastFrameGunPitch;
    private float PlaneIdleInitialVolume;
    private float PlaneDistantInitialVolume;
    private float PlaneThrustInitialVolume;
    private float PlaneWindInitialVolume;
    float InVehicleThrustVolumeFactor = .09f;
    [System.NonSerializedAttribute] [HideInInspector] public float SonicBoomWave = 0f;
    [System.NonSerializedAttribute] [HideInInspector] public float SonicBoomDistance = -1f;
    bool Landed = false;
    private int dopplecounter;
    [System.NonSerializedAttribute] [HideInInspector] public float DoSound = 32; //3 seconds before idle so late joiners hear sound
    [System.NonSerializedAttribute] [HideInInspector] public bool silent;
    private int silentint = 0;
    private float GunSoundInitialVolume;
    [System.NonSerializedAttribute] [HideInInspector] public bool soundsoff;
    float relativespeed;
    private float SonicBoomPreventer = 5f;//used to prevent sonic booms from occuring too often in case of laggers etc
    [System.NonSerializedAttribute] [HideInInspector] public bool playsonicboom;
    private float MaxAudibleDistance;
    private void Start()
    {
        Assert(EngineControl != null, "Start: EngineControl != null");
        Assert(PlaneInside != null, "Start: PlaneInside != null");
        Assert(PlaneDistant != null, "Start: PlaneDistant != null");
        Assert(PlaneABOn != null, "Start: PlaneABOn != null");
        Assert(PlaneWind != null, "Start: PlaneWind != null");
        Assert(GunSound != null, "Start: GunSound != null");
        Assert(MenuSelect != null, "Start: MenuSelect != null");
        Assert(AAMTargeting != null, "Start: AAMTargeting != null");
        Assert(AAMTargetLock != null, "Start: AAMTargetLock != null");
        Assert(AGMLock != null, "Start: AGMTargetLock != null");
        Assert(AGMUnlock != null, "Start: AGMUnlock != null");
        Assert(Airbrake != null, "Start: Airbrake != null");
        Assert(CatapultLock != null, "Start: CatapultLock != null");
        Assert(CatapultLaunch != null, "Start: CatapultLaunch != null");
        Assert(HookLanding != null, "Start: HookLanding != null");
        Assert(RadarLocked != null, "Start: RadarLocked != null");
        Assert(MissileIncoming != null, "Start: MissileIncoming != null");
        Assert(PlaneIdle.Length > 0, "Start: PlaneIdle.Length > 0");
        Assert(PlaneThrust.Length > 0, "Start: PlaneThrust.Length > 0");
        Assert(TouchDown.Length > 0, "Start: TouchDown.Length > 0");
        Assert(SonicBoom.Length > 0, "Start: SonicBoom.Length > 0");
        Assert(Explosion.Length > 0, "Start: Explosion.Length > 0");
        Assert(BulletHit.Length > 0, "Start: BulletHit.Length > 0");

        PlaneInsideNull = (PlaneInside == null) ? true : false;
        PlaneDistantNull = (PlaneDistant == null) ? true : false;
        PlaneABOnNull = (PlaneABOn == null) ? true : false;
        PlaneWindNull = (PlaneWind == null) ? true : false;
        GunSoundNull = (GunSound == null) ? true : false;
        MenuSelectNull = (MenuSelect == null) ? true : false;
        AAMTargetingNull = (AAMTargeting == null) ? true : false;
        AAMTargetLockNull = (AAMTargetLock == null) ? true : false;
        AGMTargetLockNull = (AGMLock == null) ? true : false;
        AGMUnlockNull = (AGMUnlock == null) ? true : false;
        AirbrakeNull = (Airbrake == null) ? true : false;
        CatapultLockNull = (CatapultLock == null) ? true : false;
        CatapultLaunchNull = (CatapultLaunch == null) ? true : false;
        HookLandingNull = (HookLanding == null) ? true : false;
        RadarLockedNull = (RadarLocked == null) ? true : false;
        MissileIncomingNull = (MissileIncoming == null) ? true : false;
        PlaneIdleNull = (PlaneIdle.Length < 1) ? true : false;
        PlaneThrustNull = (PlaneThrust.Length < 1) ? true : false;
        TouchDownNull = (TouchDown.Length < 1) ? true : false;
        SonicBoomNull = (SonicBoom.Length < 1) ? true : false;
        ExplosionNull = (Explosion.Length < 1) ? true : false;
        BulletHitNull = (BulletHit.Length < 1) ? true : false;


        //used to make it so that changing the volume in unity will do something //set 0 to avoid ear destruction
        if (!PlaneIdleNull)
        {
            PlaneIdleInitialVolume = PlaneIdle[0].volume;
            foreach (AudioSource idle in PlaneIdle)
            {
                idle.volume = 0;
            }
        }

        if (!PlaneThrustNull)
        {
            PlaneThrustInitialVolume = PlaneThrust[0].volume;
            foreach (AudioSource thrust in PlaneThrust)
            {
                thrust.volume = 0;
            }
        }

        if (!PlaneDistantNull)
        {
            PlaneDistantInitialVolume = PlaneDistant.volume;
            PlaneDistant.volume = 0f;
        }

        if (!SonicBoomNull) MaxAudibleDistance = SonicBoom[0].maxDistance;
        else if (!ExplosionNull) MaxAudibleDistance = Explosion[0].maxDistance;
        else if (!PlaneDistantNull) MaxAudibleDistance = PlaneDistant.maxDistance + 50;
        else MaxAudibleDistance = 4000;

        if (!PlaneWindNull) { PlaneWindInitialVolume = PlaneWind.volume; PlaneWind.volume = 0f; }
        if (!GunSoundNull)
        {
            GunSoundInitialVolume = GunSound.volume;
        }
        dopplecounter = Random.Range(0, 4);
    }

    private void Update()
    {
        if (DoSound > 35f)
        {
            return;
        }


        //undo doppler
        PlaneIdlePitch = LastFramePlaneIdlePitch;
        PlaneThrustPitch = LastFramePlaneThrustPitch;

        //the doppler code is done in a really scuffed hacky way to avoid having to do it in fixedupdate and have worse performance.
        //only calculate doppler every 5 frames to smooth out laggers and frame drops
        if (dopplecounter > 4)
        {
            //find distance to player or testcamera
            if (!EngineControl.InEditor) //ingame
            {
                ThisFrameDist = Vector3.Distance(EngineControl.localPlayer.GetPosition(), EngineControl.CenterOfMass.position);
                if (ThisFrameDist > MaxAudibleDistance) { LastFrameDist = ThisFrameDist; return; } // too far away to hear, so just stop
            }
            else if ((testcamera != null))//editor
            {
                ThisFrameDist = Vector3.Distance(testcamera.transform.position, EngineControl.CenterOfMass.position);
            }
            if (EngineControl.Occupied == true) { DoSound = 0f; }
            else { DoSound += Time.deltaTime; }

            relativespeed = (ThisFrameDist - LastFrameDist);
            float doppletemp = (343 * (Time.deltaTime * 5)) + relativespeed;

            //supersonic a bit lower than the speed of sound because dopple is speed towards you, if they're coming in at an angle it won't be as high. stupid hack
            if (doppletemp < .1f)
            {
                doppletemp = .0001f; // prevent divide by 0

                //Only Supersonic if the vehicle is actually moving faster than sound, and you're not inside it (prevents sonic booms from occuring if you move past a stationary vehicle)
                if (EngineControl.CurrentVel.magnitude > 343 && !EngineControl.Passenger && !EngineControl.Piloting)
                {
                    if (!silent)
                    {
                        SonicBoomWave = 0f;
                        playsonicboom = true;
                        SonicBoomDistance = ThisFrameDist;
                    }
                }
            }

            Doppler = (343 * (Time.deltaTime * 5)) / doppletemp;
            LastFrameDist = ThisFrameDist;
            dopplecounter = 0;
        }
        dopplecounter++;
        if (SonicBoomWave < SonicBoomDistance)
        {
            SonicBoomWave += Mathf.Max(343 * Time.deltaTime, -relativespeed * .2f); //simulate sound wave movement
        }

        if (SonicBoomWave <= SonicBoomDistance)
        {
            silent = true;
            silentint = 0;//for multiplying sound volumes
        }
        else
        {
            silent = false;
            silentint = 1;
        }

        if (!TouchDownNull)
        {
            if (Landed == false && EngineControl.Taxiing == true) { TouchDown[Random.Range(0, TouchDown.Length)].Play(); }
            if (EngineControl.Taxiing == true) { Landed = true; } else { Landed = false; }
        }
        //EngineControl.Piloting = true in editor play
        if ((EngineControl.Piloting || EngineControl.Passenger) && (EngineControl.CanopyCloseTimer < 0 && EngineControl.CanopyCloseTimer > -100000))
        {
            if (Leftplane == false)//change stuff when you get in
            {
                PlaneThrustPitch = 0.8f;
                if (!PlaneInsideNull && !PlaneIdleNull)
                {
                    PlaneInside.pitch = PlaneIdle[0].pitch * .8f;
                    PlaneInside.volume = PlaneIdle[0].volume * .4f;//it'll lerp up from here
                }
                if (!PlaneABOnNull)
                {
                    PlaneABOn.volume *= .25f;//afterburner quieter inside
                }
                PlaneThrustVolume *= InVehicleThrustVolumeFactor;
                Leftplane = true;//used when we leave to see if we just left
            }
            foreach (AudioSource thrust in PlaneThrust)
            {
                if (!thrust.isPlaying)
                {
                    thrust.Play();
                }
            }
            if ((!PlaneDistantNull) && PlaneDistant.isPlaying)
            {
                PlaneDistant.Stop();
            }
            if ((!PlaneInsideNull) && !PlaneInside.isPlaying)
            {
                PlaneInside.Play();
            }
            if ((!PlaneIdleNull) && PlaneIdle[0].isPlaying)
            {
                foreach (AudioSource idle in PlaneIdle)
                    idle.Stop();
            }
            if (EngineControl.AAMHasTarget && !EngineControl.AAMLocked)
            {
                AAMTargeting.gameObject.SetActive(true);
                AAMTargetLock.gameObject.SetActive(false);
            }
            else if (EngineControl.AAMLocked)
            {
                AAMTargeting.gameObject.SetActive(false);
                AAMTargetLock.gameObject.SetActive(true);
            }
            else
            {
                AAMTargeting.gameObject.SetActive(false);
                AAMTargetLock.gameObject.SetActive(false);
            }
            if (EngineControl.MissilesIncoming > 0)
            {
                if (!MissileIncomingNull && !MissileIncoming.isPlaying) MissileIncoming.gameObject.SetActive(true);
                if (!RadarLockedNull && RadarLocked.isPlaying) { RadarLocked.Stop(); }
            }
            else
            {
                if (!MissileIncomingNull) MissileIncoming.gameObject.SetActive(false);
            }
            if (!AirbrakeNull)
            {
                if (!Airbrake.isPlaying)
                    Airbrake.Play();
                Airbrake.pitch = EngineControl.AirBrakeInput * .2f + .9f;
                Airbrake.volume = EngineControl.AirBrakeInput * EngineControl.rotlift;
            }
            else
            {
                if (!AirbrakeNull)
                    Airbrake.Stop();
            }
            if ((EngineControl.Piloting || (EngineControl.Passenger && EngineControl.Occupied)) && EngineControl.Fuel > 1) //you're piloting or someone is piloting and you're a passenger
            {
                if (!PlaneInsideNull)
                {
                    PlaneInside.pitch = Mathf.Lerp(PlaneInside.pitch, (EngineControl.Throttle * .4f) + .8f, 2.25f * Time.deltaTime);
                    PlaneInside.volume = Mathf.Lerp(PlaneInside.volume, .4f, .72f * Time.deltaTime);
                }
                PlaneThrustVolume = Mathf.Lerp(PlaneThrustVolume, (EngineControl.Throttle * PlaneThrustInitialVolume) * InVehicleThrustVolumeFactor, 1.08f * Time.deltaTime);
            }
            else/*  if (InEditor) */ //enable here and disable 'Piloting' above for testing //you're a passenger and no one is flying
            {

                if (!PlaneInsideNull)
                {
                    PlaneInside.pitch = Mathf.Lerp(PlaneInside.pitch, 0, .108f * Time.deltaTime);
                    PlaneInside.volume = Mathf.Lerp(PlaneInside.volume, 0, .72f * Time.deltaTime);
                }
                PlaneThrustVolume = Mathf.Lerp(PlaneThrustVolume, 0, 1.08f * Time.deltaTime);
            }
        }
        else if (EngineControl.Occupied && EngineControl.Fuel > 1)//someone else is piloting
        {
            if (Leftplane == true)
            {
                Exitplane();
            }//passenger left or canopy opened
            foreach (AudioSource thrust in PlaneThrust)
            {
                if (!thrust.isPlaying)
                {
                    thrust.Play();
                }
            }
            if (!PlaneIdleNull && !PlaneIdle[0].isPlaying)
            {
                foreach (AudioSource idle in PlaneIdle)
                    idle.Play();
            }
            if (!PlaneDistantNull && !PlaneDistant.isPlaying)
            {
                PlaneDistant.Play();
            }
            PlaneIdleVolume = Mathf.Lerp(PlaneIdleVolume, 1, .72f * Time.deltaTime);
            if (Doppler > 50)
            {
                PlaneDistantVolume = Mathf.Lerp(PlaneDistantVolume, 0, 3 * Time.deltaTime);
                PlaneThrustVolume = Mathf.Lerp(PlaneThrustVolume, 0, 3 * Time.deltaTime);
            }
            else
            {
                PlaneDistantVolume = Mathf.Lerp(PlaneDistantVolume, EngineControl.Throttle, .72f * Time.deltaTime);
                PlaneThrustVolume = Mathf.Lerp(PlaneThrustVolume, EngineControl.Throttle, 1.08f * Time.deltaTime);
            }
            PlaneThrustPitch = 1;
            PlaneIdlePitch = Mathf.Lerp(PlaneIdlePitch, (EngineControl.Throttle - 0.3f) + 1.3f, .54f * Time.deltaTime);
        }
        else //no one is in the plane or its out of fuel
        {
            if (Leftplane == true) { Exitplane(); }//pilot or passenger left or canopy opened
            PlaneThrustVolume = Mathf.Lerp(PlaneThrustVolume, 0, 1.08f * Time.deltaTime);
            PlaneIdlePitch = Mathf.Lerp(PlaneIdlePitch, 0, .09f * Time.deltaTime);
            PlaneIdleVolume = Mathf.Lerp(PlaneIdleVolume, 0, .09f * Time.deltaTime);
            PlaneDistantVolume = Mathf.Lerp(PlaneDistantVolume, 0, .72f * Time.deltaTime);
        }

        LastFramePlaneIdlePitch = PlaneIdlePitch;

        if (!EngineControl.Piloting && !EngineControl.Passenger) //apply dopper if you're not in the vehicle
        {
            PlaneIdlePitch *= Doppler;
            PlaneDistantPitch = Mathf.Clamp(((Doppler - 1) * .4f) + 1, 0, 1.25f);//40% effect + clamp to prevent sounding too stupid while plane is flying towards you
            PlaneThrustPitch = Mathf.Clamp(Doppler, 0, 2.5f);
        }


        if (SonicBoomPreventer < 5.1f)//count up, limited to 5.1(plane can only cause a sonic boom every 5 seconds max)
        {
            SonicBoomPreventer += Time.deltaTime;
        }
        //set final volumes and pitches
        //lerp should help smooth out laggers and the dopple only being calculated every 5 frames
        if (!SonicBoomNull && !silent && playsonicboom)
        {
            if (SonicBoomPreventer > 5 && !EngineControl.dead)
            {
                int rand = Random.Range(0, SonicBoom.Length);
                SonicBoom[rand].pitch = Random.Range(.94f, 1.2f);
                SonicBoom[rand].Play();
                SonicBoomPreventer = 0;
            }
            playsonicboom = false;
        }
        foreach (AudioSource idle in PlaneIdle)
        {
            idle.volume = Mathf.Lerp(idle.volume, PlaneIdleVolume, 30f * Time.deltaTime) * silentint;
            idle.pitch = Mathf.Lerp(idle.pitch, PlaneIdlePitch, 30f * Time.deltaTime);
        }
        if (!PlaneDistantNull)
        {
            PlaneDistantVolume *= silentint;
            PlaneDistant.volume = Mathf.Lerp(PlaneDistant.volume, PlaneDistantVolume, 30f * Time.deltaTime);
            PlaneDistant.pitch = Mathf.Lerp(PlaneDistant.pitch, PlaneDistantPitch, 30f * Time.deltaTime);
        }
        foreach (AudioSource thrust in PlaneThrust)
        {
            thrust.volume = PlaneThrustVolume * silentint;
            thrust.pitch = Mathf.Lerp(thrust.pitch, PlaneThrustPitch, 30f * Time.deltaTime);
        }
        if (!PlaneWindNull)
        {
            PlaneWind.pitch = Mathf.Clamp(Doppler, -10, 10);
            PlaneWind.volume = (Mathf.Clamp(((EngineControl.CurrentVel.magnitude / 20) * PlaneWindInitialVolume), 0, 1) / 10f + (Mathf.Clamp(((EngineControl.Gs - 1) * PlaneWindInitialVolume) / 8, 0, 1) * .2f)) * silentint;
        }

        if (!GunSoundNull)
        {
            if (EngineControl.IsFiringGun && !EngineControl.SoundControl.silent)
            {
                GunSound.pitch = Mathf.Clamp(((EngineControl.SoundControl.Doppler - 1) * .3f) + 1, 0, 1.2f);
                if (!GunSound.isPlaying)
                {
                    GunSound.Play();
                }
                if (EngineControl.SoundControl.Doppler > 50f)
                {
                    GunSound.volume = Mathf.Lerp(GunSound.volume, 0, 3f * Time.deltaTime);
                }
                else
                {
                    GunSound.volume = Mathf.Lerp(GunSound.volume, GunSoundInitialVolume, 9f * Time.deltaTime);
                }
            }
            else if (!EngineControl.IsFiringGun || EngineControl.SoundControl.silent && GunSound.isPlaying)
            {
                GunSound.Stop();
            }
        }
    }
    private void Exitplane()//sets sound values to give illusion of continuity of engine sound when exiting the plane
    {
        if (!AAMTargetingNull) AAMTargeting.gameObject.SetActive(false);
        if (!AAMTargetLockNull) AAMTargetLock.gameObject.SetActive(false);
        if (!RadarLockedNull) RadarLocked.gameObject.SetActive(false);
        if (!MissileIncomingNull) MissileIncoming.gameObject.SetActive(false);
        if (!PlaneInsideNull)
        {
            PlaneInside.Stop();
            PlaneIdlePitch = PlaneInside.pitch;
        }
        foreach (AudioSource idle in PlaneIdle) { idle.Play(); }
        foreach (AudioSource thrust in PlaneThrust) { thrust.Play(); }
        if (!PlaneDistantNull) PlaneDistant.Play();
        Leftplane = false;
        PlaneThrustVolume *= 6.666666f;
        PlaneIdleVolume = PlaneIdleInitialVolume * .4f;
        PlaneDistantVolume = PlaneThrustVolume;
        if (!PlaneABOnNull)
        {
            PlaneABOn.volume *= 4f;
        }
    }
    public void TargetedAlarm()
    {
        if (RadarLocked != null)
            RadarLocked.Play();
    }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogError("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}

