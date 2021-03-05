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
    public AudioSource[] Thrust;
    public AudioSource ABOnInside;
    public AudioSource ABOnOutside;
    public AudioSource[] TouchDown;
    public AudioSource PlaneWind;
    public AudioSource[] SonicBoom;
    public AudioSource[] Explosion;
    public AudioSource GunSound;
    public AudioSource[] BulletHit;
    public AudioSource Rolling;
    public AudioSource Reloading;
    public AudioSource RadarLocked;
    public AudioSource MissileIncoming;
    public AudioSource AAMTargeting;
    public AudioSource AAMTargetLock;
    public AudioSource AGMLock;
    public AudioSource AGMUnlock;
    public AudioSource Airbrake;
    public AudioSource CatapultLock;
    public AudioSource CatapultLaunch;
    public AudioSource CableSnap;
    public AudioSource MenuSelect;
    [System.NonSerializedAttribute] public bool PlaneIdleNull;
    [System.NonSerializedAttribute] public bool PlaneInsideNull;
    [System.NonSerializedAttribute] public bool PlaneDistantNull;
    [System.NonSerializedAttribute] public bool PlaneThrustNull;
    [System.NonSerializedAttribute] public bool ABOnInsideNull;
    [System.NonSerializedAttribute] public bool ABOnOutsideNull;
    [System.NonSerializedAttribute] public bool TouchDownNull;
    [System.NonSerializedAttribute] public bool PlaneWindNull;
    [System.NonSerializedAttribute] public bool SonicBoomNull;
    [System.NonSerializedAttribute] public bool ExplosionNull;
    [System.NonSerializedAttribute] public bool GunSoundNull;
    [System.NonSerializedAttribute] public bool BulletHitNull;
    [System.NonSerializedAttribute] public bool MissileIncomingNull;
    [System.NonSerializedAttribute] public bool RollingNull;
    [System.NonSerializedAttribute] public bool ReloadingNull;
    [System.NonSerializedAttribute] public bool RadarLockedNull;
    [System.NonSerializedAttribute] public bool AAMTargetingNull;
    [System.NonSerializedAttribute] public bool AAMTargetLockNull;
    [System.NonSerializedAttribute] public bool AGMTargetLockNull;
    [System.NonSerializedAttribute] public bool AGMUnlockNull;
    [System.NonSerializedAttribute] public bool AirbrakeNull;
    [System.NonSerializedAttribute] public bool CatapultLockNull;
    [System.NonSerializedAttribute] public bool CatapultLaunchNull;
    [System.NonSerializedAttribute] public bool CableSnapNull;
    [System.NonSerializedAttribute] public bool MenuSelectNull;
    public Transform testcamera;
    private bool SuperSonic = false;
    private float IdleDoppleTemp;
    [System.NonSerializedAttribute] public float Doppler = 1;
    float LastFrameDist;
    [System.NonSerializedAttribute] public float ThisFrameDist = 0;
    private bool Leftplane = false;
    [System.NonSerializedAttribute] public float PlaneIdlePitch;
    [System.NonSerializedAttribute] public float PlaneIdleVolume;
    private float PlaneDistantPitch;
    [System.NonSerializedAttribute] public float PlaneDistantVolume;
    private float PlaneThrustPitch;
    [System.NonSerializedAttribute] public float PlaneThrustVolume;
    private float PlaneInsideInitialVolume;
    private float LastFramePlaneIdlePitch;
    private float LastFramePlaneThrustPitch;
    private float LastFrameGunPitch;
    private float PlaneIdleInitialVolume;
    private float PlaneDistantInitialVolume;
    private float PlaneThrustInitialVolume;
    private float PlaneWindInitialVolume;
    private const float InVehicleThrustVolumeFactor = .09f;
    [System.NonSerializedAttribute] public float SonicBoomWave = 0f;
    [System.NonSerializedAttribute] public float SonicBoomDistance = -1f;
    private int dopplecounter;
    [System.NonSerializedAttribute] public float DoSound = 20; //15 seconds before idle so late joiners have time to sync before going idle
    [System.NonSerializedAttribute] public bool silent;
    private int silentint = 0;
    private float GunSoundInitialVolume;
    [System.NonSerializedAttribute] public bool soundsoff;
    float relativespeed;
    private float SonicBoomPreventer = 5f;//used to prevent sonic booms from occuring too often in case of laggers etc
    [System.NonSerializedAttribute] public bool playsonicboom;
    private float MaxAudibleDistance;
    private bool TooFarToHear = false;
    private void Start()
    {
        Assert(EngineControl != null, "Start: EngineControl != null");
        Assert(PlaneInside != null, "Start: PlaneInside != null");
        Assert(PlaneDistant != null, "Start: PlaneDistant != null");
        Assert(ABOnInside != null, "Start: ABOnInside != null");
        Assert(ABOnOutside != null, "Start: ABOnOutside != null");
        Assert(PlaneWind != null, "Start: PlaneWind != null");
        Assert(GunSound != null, "Start: GunSound != null");
        Assert(MenuSelect != null, "Start: MenuSelect != null");
        Assert(AAMTargeting != null, "Start: AAMTargeting != null");
        Assert(AAMTargetLock != null, "Start: AAMTargetLock != null");
        Assert(AGMLock != null, "Start: AGMLock != null");
        Assert(AGMUnlock != null, "Start: AGMUnlock != null");
        Assert(Airbrake != null, "Start: Airbrake != null");
        Assert(CatapultLock != null, "Start: CatapultLock != null");
        Assert(CatapultLaunch != null, "Start: CatapultLaunch != null");
        Assert(CableSnap != null, "Start: CableSnap != null");
        Assert(Rolling != null, "Start: Rolling != null");
        Assert(Reloading != null, "Start: Reloading != null");
        Assert(RadarLocked != null, "Start: RadarLocked != null");
        Assert(MissileIncoming != null, "Start: MissileIncoming != null");
        Assert(PlaneIdle.Length > 0, "Start: PlaneIdle.Length > 0");
        Assert(Thrust.Length > 0, "Start: Thrust.Length > 0");
        Assert(TouchDown.Length > 0, "Start: TouchDown.Length > 0");
        Assert(SonicBoom.Length > 0, "Start: SonicBoom.Length > 0");
        Assert(Explosion.Length > 0, "Start: Explosion.Length > 0");
        Assert(BulletHit.Length > 0, "Start: BulletHit.Length > 0");

        PlaneInsideNull = (PlaneInside == null) ? true : false;
        PlaneDistantNull = (PlaneDistant == null) ? true : false;
        ABOnInsideNull = (ABOnOutside == null) ? true : false;
        ABOnOutsideNull = (ABOnOutside == null) ? true : false;
        PlaneWindNull = (PlaneWind == null) ? true : false;
        GunSoundNull = (GunSound == null) ? true : false;
        MenuSelectNull = (MenuSelect == null) ? true : false;
        AAMTargetingNull = (AAMTargeting == null) ? true : false;
        AAMTargetLockNull = (AAMTargetLock == null) ? true : false;
        AGMTargetLockNull = (AGMLock == null) ? true : false;
        AGMUnlockNull = (AGMUnlock == null) ? true : false;
        AirbrakeNull = (Airbrake == null) ? true : false;
        CatapultLockNull = (CatapultLock == null) ? true : false;
        CatapultLaunchNull = (CatapultLaunch == null) ? true : false; ;
        CableSnapNull = (CableSnap == null) ? true : false;
        RollingNull = (Rolling == null) ? true : false;
        ReloadingNull = (Reloading == null) ? true : false;
        RadarLockedNull = (RadarLocked == null) ? true : false;
        MissileIncomingNull = (MissileIncoming == null) ? true : false;
        PlaneIdleNull = (PlaneIdle.Length < 1) ? true : false;
        PlaneThrustNull = (Thrust.Length < 1) ? true : false;
        TouchDownNull = (TouchDown.Length < 1) ? true : false;
        SonicBoomNull = (SonicBoom.Length < 1) ? true : false;
        ExplosionNull = (Explosion.Length < 1) ? true : false;
        BulletHitNull = (BulletHit.Length < 1) ? true : false;


        if (!PlaneInsideNull)
        {
            PlaneInsideInitialVolume = PlaneInside.volume;
        }

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
            PlaneThrustInitialVolume = Thrust[0].volume;
            foreach (AudioSource thrust in Thrust)
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
        dopplecounter = Random.Range(0, 5);
    }

    private void Update()
    {
        float DeltaTime = Time.deltaTime;
        if (DoSound > 35f)
        {
            if (!soundsoff)//disable all the sounds that always play, re-enabled in pilotseat
            {
                foreach (AudioSource thrust in Thrust)
                {
                    thrust.gameObject.SetActive(false);
                }
                foreach (AudioSource idle in PlaneIdle)
                {
                    idle.gameObject.SetActive(false);
                }
                if (!PlaneDistantNull) PlaneDistant.gameObject.SetActive(false);
                if (!PlaneWindNull) PlaneWind.gameObject.SetActive(false);
                if (!PlaneInsideNull) PlaneInside.gameObject.SetActive(false);
                if (!AirbrakeNull) Airbrake.gameObject.SetActive(false);
                soundsoff = true;
            }
            else { return; }
            return;
        }
        if (EngineControl.Occupied) { DoSound = 0f; }
        else { DoSound += DeltaTime; }

        //undo doppler
        PlaneIdlePitch = LastFramePlaneIdlePitch;
        PlaneThrustPitch = LastFramePlaneThrustPitch;

        //the doppler code is done in a really hacky way to avoid having to do it in fixedupdate and have worse performance.
        //and because even if you do it in fixedupate, it only works properly in VRChat if you have max framerate. (objects owned by other players positions are only updated in Update())
        //only calculate doppler every 5 frames to smooth out laggers and frame drops
        if (dopplecounter > 4)
        {
            float SmoothDeltaTime = Time.smoothDeltaTime;
            //find distance to player or testcamera
            if (!EngineControl.InEditor) //ingame
            {
                ThisFrameDist = Vector3.Distance(EngineControl.localPlayer.GetPosition(), EngineControl.CenterOfMass.position);
                if (ThisFrameDist > MaxAudibleDistance)
                {
                    LastFrameDist = ThisFrameDist; TooFarToHear = true;
                }
                else
                {
                    TooFarToHear = false;
                } // too far away to hear, so just stop
            }
            else if ((testcamera != null))//editor and testcamera is set
            {
                ThisFrameDist = Vector3.Distance(testcamera.transform.position, EngineControl.CenterOfMass.position);
            }

            relativespeed = (ThisFrameDist - LastFrameDist);
            float doppletemp = (343 * (SmoothDeltaTime * 5)) + relativespeed;

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

            Doppler = (343 * (SmoothDeltaTime * 5)) / doppletemp;
            LastFrameDist = ThisFrameDist;
            dopplecounter = 0;
        }
        dopplecounter++;
        if (TooFarToHear) { return; }

        if (SonicBoomWave < SonicBoomDistance)
        {
            SonicBoomWave += Mathf.Max(343 * DeltaTime, -relativespeed * .2f); //simulate sound wave movement
            silent = true;
            silentint = 0;//for multiplying sound volumes
        }
        else
        {
            silent = false;
            silentint = 1;
        }

        //EngineControl.Piloting = true in editor play mode
        if ((EngineControl.Piloting || EngineControl.Passenger) && (EngineControl.CanopyCloseTimer < 0 && EngineControl.CanopyCloseTimer > -100000))
        {
            EngineControl.EffectsControl.PlaneAnimator.SetInteger("missilesincoming", EngineControl.MissilesIncoming);
            if (Leftplane == false)
            {
                if (!ABOnOutsideNull) { ABOnOutside.Stop(); }
                //change stuff when you get in
                if (!CatapultLaunchNull) { CatapultLaunch.volume *= InVehicleThrustVolumeFactor; }
                PlaneThrustPitch = 0.8f;
                if (!PlaneInsideNull && !PlaneIdleNull)
                {
                    PlaneInside.pitch = PlaneIdle[0].pitch * .8f;
                    PlaneInside.volume = PlaneIdle[0].volume * .4f;//it'll lerp up from here
                }
                PlaneThrustVolume *= InVehicleThrustVolumeFactor;
                Leftplane = true;//used when we leave to see if we just left
            }
            if (!RollingNull)
            {
                if (EngineControl.Taxiing)
                {
                    if (!RollingNull && !Rolling.isPlaying) { Rolling.Play(); }
                    Rolling.volume = Mathf.Clamp(EngineControl.Speed * 0.03f, 0, 1);
                }
                else if (!RollingNull) Rolling.volume = 0;
            }
            foreach (AudioSource thrust in Thrust)
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
            if (EngineControl.AAMLockTimer > 0 && !EngineControl.AAMLocked && EngineControl.RStickSelection == 2)
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
            if (!AirbrakeNull)
            {
                if (!Airbrake.isPlaying)
                { Airbrake.Play(); }
                Airbrake.pitch = EngineControl.BrakeInput * .2f + .9f;
                Airbrake.volume = EngineControl.EffectsControl.AirbrakeLerper * EngineControl.rotlift;
            }
            if ((EngineControl.Piloting || (EngineControl.Passenger && EngineControl.Occupied)) && EngineControl.Fuel > 1) //you're piloting or someone is piloting and you're a passenger
            {
                if (!PlaneInsideNull)
                {
                    PlaneInside.pitch = Mathf.Lerp(PlaneInside.pitch, (EngineControl.EngineOutput * .4f) + .8f, 2.25f * DeltaTime);
                    PlaneInside.volume = Mathf.Lerp(PlaneInside.volume, PlaneInsideInitialVolume, .72f * DeltaTime);
                }
                PlaneThrustVolume = Mathf.Lerp(PlaneThrustVolume, (EngineControl.EngineOutput * PlaneThrustInitialVolume) * InVehicleThrustVolumeFactor, 1.08f * DeltaTime);
            }
            else/*  if (InEditor) */ //enable here and disable 'Piloting' above for testing //you're a passenger and no one is flying
            {

                if (!PlaneInsideNull)
                {
                    PlaneInside.pitch = Mathf.Lerp(PlaneInside.pitch, 0, .108f * DeltaTime);
                    PlaneInside.volume = Mathf.Lerp(PlaneInside.volume, 0, .72f * DeltaTime);
                }
                PlaneThrustVolume = Mathf.Lerp(PlaneThrustVolume, 0, 1.08f * DeltaTime);
            }
        }
        else if (EngineControl.Occupied && EngineControl.Fuel > 1)//someone else is piloting
        {
            if (Leftplane == true)
            {
                Exitplane();
            }//passenger left or canopy opened
            foreach (AudioSource thrust in Thrust)
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
            PlaneIdleVolume = Mathf.Lerp(PlaneIdleVolume, 1, .72f * DeltaTime);
            if (Doppler > 50)
            {
                PlaneDistantVolume = Mathf.Lerp(PlaneDistantVolume, 0, 3 * DeltaTime);
                PlaneThrustVolume = Mathf.Lerp(PlaneThrustVolume, 0, 3 * DeltaTime);
            }
            else
            {
                PlaneDistantVolume = Mathf.Lerp(PlaneDistantVolume, EngineControl.EngineOutput, .72f * DeltaTime);
                PlaneThrustVolume = Mathf.Lerp(PlaneThrustVolume, EngineControl.EngineOutput, 1.08f * DeltaTime);
            }
            PlaneThrustPitch = 1;
            PlaneIdlePitch = Mathf.Lerp(PlaneIdlePitch, (EngineControl.EngineOutput - 0.3f) + 1.3f, .54f * DeltaTime);
        }
        else //no one is in the plane or its out of fuel
        {
            if (Leftplane == true) { Exitplane(); }//pilot or passenger left or canopy opened
            PlaneThrustVolume = Mathf.Lerp(PlaneThrustVolume, 0, 1.08f * DeltaTime);
            PlaneIdlePitch = Mathf.Lerp(PlaneIdlePitch, 0, .09f * DeltaTime);
            PlaneIdleVolume = Mathf.Lerp(PlaneIdleVolume, 0, .09f * DeltaTime);
            PlaneDistantVolume = Mathf.Lerp(PlaneDistantVolume, 0, .72f * DeltaTime);
        }

        LastFramePlaneIdlePitch = PlaneIdlePitch;

        if (!EngineControl.Piloting && !EngineControl.Passenger) //apply dopper if you're not in the vehicle
        {
            PlaneIdlePitch *= Doppler;
            PlaneDistantPitch = Mathf.Clamp(((Doppler - 1) * .4f) + 1, 0, 1.25f);//40% effect + clamp to prevent sounding too stupid while plane is flying towards you
            PlaneThrustPitch = Mathf.Clamp(Doppler, 0, 2.5f);
        }


        SonicBoomPreventer += DeltaTime;
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
            idle.volume = Mathf.Lerp(idle.volume, PlaneIdleVolume, 30f * DeltaTime) * silentint;
            idle.pitch = Mathf.Lerp(idle.pitch, PlaneIdlePitch, 30f * DeltaTime);
        }
        if (!PlaneDistantNull)
        {
            PlaneDistantVolume *= silentint;
            PlaneDistant.volume = Mathf.Lerp(PlaneDistant.volume, PlaneDistantVolume, 30f * DeltaTime);
            PlaneDistant.pitch = Mathf.Lerp(PlaneDistant.pitch, PlaneDistantPitch, 30f * DeltaTime);
        }
        foreach (AudioSource thrust in Thrust)
        {
            thrust.volume = PlaneThrustVolume * silentint;
            thrust.pitch = Mathf.Lerp(thrust.pitch, PlaneThrustPitch, 30f * DeltaTime);
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
                    GunSound.volume = Mathf.Lerp(GunSound.volume, 0, 3f * DeltaTime);
                }
                else
                {
                    GunSound.volume = Mathf.Lerp(GunSound.volume, GunSoundInitialVolume, 9f * DeltaTime);
                }
            }
            else if (!EngineControl.IsFiringGun || EngineControl.SoundControl.silent && GunSound.isPlaying)
            {
                GunSound.Stop();
            }
        }
    }
    private void Exitplane()//sets sound values to give continuity of engine sound when exiting the plane
    {
        if (!AAMTargetingNull) AAMTargeting.gameObject.SetActive(false);
        if (!AAMTargetLockNull) AAMTargetLock.gameObject.SetActive(false);
        if (!MissileIncomingNull) MissileIncoming.gameObject.SetActive(false);
        if (!RadarLockedNull) { RadarLocked.Stop(); }
        if (!CatapultLaunchNull) CatapultLaunch.volume /= InVehicleThrustVolumeFactor;
        if (!RollingNull) { Rolling.Stop(); }
        if (!PlaneInsideNull) { PlaneInside.Stop(); }
        foreach (AudioSource idle in PlaneIdle) { idle.Play(); }
        foreach (AudioSource thrust in Thrust) { thrust.Play(); }
        if (!PlaneDistantNull) PlaneDistant.Play();
        Leftplane = false;
        if (!EngineControl.dead)
        {
            //these are set differently EngineController.Explode(), so we don't do them if we're dead
            PlaneIdleVolume = PlaneIdleInitialVolume * .4f;
            PlaneThrustVolume *= 6.666666f;
            PlaneDistantVolume = PlaneThrustVolume;
            if (!PlaneInsideNull) { PlaneIdlePitch = PlaneInside.pitch; }
        }
    }
    public void Explode_Sound()
    {
        //play sonic boom if it was going to play before it exploded
        if (playsonicboom && silent)
        {
            if (!SonicBoomNull)
            {
                int rand = Random.Range(0, SonicBoom.Length);
                if (SonicBoom[rand] != null)
                {
                    SonicBoom[rand].pitch = Random.Range(.94f, 1.2f);
                    float delay = (SonicBoomDistance - SonicBoomWave) / 343;
                    if (delay > 7)
                    {
                    }
                    else
                    {
                        SonicBoom[rand].PlayDelayed(delay);
                    }
                }
            }
        }
        playsonicboom = false;
        silent = false;
        PlaneIdlePitch = 0;
        PlaneIdleVolume = 0;
        PlaneThrustVolume = 0;
        PlaneDistantVolume = 0;
        LastFramePlaneIdlePitch = 0;
        LastFramePlaneThrustPitch = 0;

        if (!ExplosionNull)
        {
            int rand = Random.Range(0, Explosion.Length);
            if (Explosion[rand] != null)
            {
                Explosion[rand].Play();
            }
        }

        if (!PlaneDistantNull) { PlaneDistant.volume = 0; }

        foreach (AudioSource thrust in Thrust)
        {
            thrust.pitch = 0;
            thrust.volume = 0;
        }
        foreach (AudioSource idle in PlaneIdle)
        {
            idle.pitch = 0;
            idle.volume = 0;
        }
    }
    public void PlayTouchDownSound()
    {
        if (!TouchDownNull)
        {
            TouchDown[Random.Range(0, TouchDown.Length)].Play();
        }
    }
    public void Wakeup()
    {
        DoSound = 0f;
        foreach (AudioSource thrust in Thrust)
        {
            thrust.gameObject.SetActive(true);
        }
        foreach (AudioSource idle in PlaneIdle)
        {
            idle.gameObject.SetActive(true);
        }
        if (!PlaneDistantNull) PlaneDistant.gameObject.SetActive(true);
        if (!PlaneWindNull) PlaneWind.gameObject.SetActive(true);
        if (!PlaneInsideNull) PlaneInside.gameObject.SetActive(true);
        if (!AirbrakeNull) Airbrake.gameObject.SetActive(true);
        if (soundsoff)
        {
            PlaneIdleVolume = 0;
            PlaneDistantVolume = 0;
            PlaneThrustVolume = 0;
            LastFramePlaneIdlePitch = 0;
            LastFramePlaneThrustPitch = 0;
        }
        soundsoff = false;
    }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogWarning("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}

