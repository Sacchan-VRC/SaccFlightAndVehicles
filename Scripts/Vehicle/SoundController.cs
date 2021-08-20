//TO TEST OUTSIDE-OF-PLANE SOUNDS SET -100000 to 100000 on line 202 AND COMMENT OUT ' && !Piloting ' ON LINE 164
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
    public AudioSource[] BulletHit;
    public AudioSource Rolling;
    [SerializeField] private float RollingMaxVol = 1;
    [SerializeField] private float RollingVolCurve = .03f;
    [Tooltip("If ticked, will lerp Rolling volume on touchdown. useful for seaplanes")]
    [SerializeField] private bool Rolling_Seaplane;
    public AudioSource ReSupply;
    public AudioSource RadarLocked;
    public AudioSource MissileIncoming;
    [SerializeField] private AudioSource EnterWater;
    [SerializeField] private AudioSource UnderWater;
    [SerializeField] private AudioSource[] DopplerSounds;
    public float TouchDownSoundSpeed = 35;
    [SerializeField] private bool DefaultCanopyClosed;
    [System.NonSerializedAttribute] public bool PlaneIdleNull = true;
    [System.NonSerializedAttribute] public bool PlaneInsideNull = true;
    [System.NonSerializedAttribute] public bool PlaneDistantNull = true;
    [System.NonSerializedAttribute] public bool PlaneThrustNull = true;
    [System.NonSerializedAttribute] public bool ABOnInsideNull = true;
    [System.NonSerializedAttribute] public bool ABOnOutsideNull = true;
    [System.NonSerializedAttribute] public bool TouchDownNull = true;
    [System.NonSerializedAttribute] public bool PlaneWindNull = true;
    [System.NonSerializedAttribute] public bool SonicBoomNull = true;
    [System.NonSerializedAttribute] public bool ExplosionNull = true;
    [System.NonSerializedAttribute] public bool BulletHitNull = true;
    [System.NonSerializedAttribute] public bool MissileIncomingNull = true;
    [System.NonSerializedAttribute] public bool RollingNull = true;
    [System.NonSerializedAttribute] public bool ReSupplyNull = true;
    [System.NonSerializedAttribute] public bool RadarLockedNull = true;
    [System.NonSerializedAttribute] public bool AirbrakeNull = true;
    public Transform testcamera;
    private bool SuperSonic = false;
    private float IdleDoppleTemp;
    [System.NonSerializedAttribute] public float Doppler = 1;
    float LastFrameDist;
    [System.NonSerializedAttribute] public float ThisFrameDist = 0;
    private bool InPlane = false;
    [System.NonSerializedAttribute] public float PlaneIdlePitch;
    [System.NonSerializedAttribute] public float PlaneIdleVolume;
    [System.NonSerializedAttribute] public float PlaneDistantVolume;
    private float PlaneThrustPitch;
    [System.NonSerializedAttribute] public float PlaneThrustVolume;
    private float LastFramePlaneIdlePitch;
    private float LastFramePlaneThrustPitch;
    private float PlaneInsideInitialVolume;
    private float PlaneIdleInitialVolume;
    private float PlaneThrustInitialVolume;
    private float PlaneDistantInitialVolume;
    private float PlaneWindInitialVolume;
    private float[] DopplerSounds_InitialVolumes;
    private float PlaneInsideTargetVolume;
    private float PlaneIdleTargetVolume;
    private float PlaneThrustTargetVolume;
    private float PlaneDistantTargetVolume;
    private float PlaneWindTargetVolume;
    private float[] DopplerSounds_TargetVolumes;
    private const float InVehicleThrustVolumeFactor = .09f;
    [System.NonSerializedAttribute] public float SonicBoomWave = 0f;
    [System.NonSerializedAttribute] public float SonicBoomDistance = -1f;
    private int dopplecounter;
    [System.NonSerializedAttribute] public float DoSound = 20;//15 seconds before idle so late joiners have time to sync before going idle
    [System.NonSerializedAttribute] public bool silent;
    private int silentint = 0;
    [System.NonSerializedAttribute] public bool soundsoff;
    float relativespeed;
    private float SonicBoomPreventer = 5f;//used to prevent sonic booms from occuring too often in case of laggers etc
    [System.NonSerializedAttribute] public bool playsonicboom;
    private float MaxAudibleDistance;
    private bool TooFarToHear = false;
    private bool InEditor = true;
    [System.NonSerializedAttribute] public bool AllDoorsClosed = false;
    private Transform CenterOfMass;
    private VRCPlayerApi localPlayer;
    private bool Piloting;
    private bool Passenger;
    private bool Initiatlized;
    private int DoorsOpen = 0;
    private void SFEXT_L_ECStart()
    {
        Initiatlized = true;

        PlaneInsideNull = PlaneInside == null;
        PlaneDistantNull = PlaneDistant == null;
        ABOnInsideNull = ABOnOutside == null;
        ABOnOutsideNull = ABOnOutside == null;
        PlaneWindNull = PlaneWind == null;
        RollingNull = Rolling == null;
        ReSupplyNull = ReSupply == null;
        RadarLockedNull = RadarLocked == null;
        MissileIncomingNull = MissileIncoming == null;
        PlaneIdleNull = PlaneIdle.Length < 1;
        PlaneThrustNull = Thrust.Length < 1;
        TouchDownNull = TouchDown.Length < 1;
        SonicBoomNull = SonicBoom.Length < 1;
        ExplosionNull = Explosion.Length < 1;
        BulletHitNull = BulletHit.Length < 1;

        if (DefaultCanopyClosed)
        { DoorClose(); }

        localPlayer = Networking.LocalPlayer;
        if (localPlayer != null)
        { InEditor = false; }
        CenterOfMass = EngineControl.CenterOfMass;

        if (!PlaneInsideNull)
        {
            PlaneInsideInitialVolume = PlaneInsideTargetVolume = PlaneInside.volume;
        }

        //used to make it so that changing the volume in unity will do something //set 0 to avoid ear destruction
        if (!PlaneIdleNull)
        {
            PlaneIdleInitialVolume = PlaneIdleTargetVolume = PlaneIdle[0].volume;
            foreach (AudioSource idle in PlaneIdle)
            {
                idle.volume = 0;
            }
        }

        if (!PlaneThrustNull)
        {
            PlaneThrustInitialVolume = PlaneThrustTargetVolume = Thrust[0].volume;
            LastFramePlaneThrustPitch = Thrust[0].pitch;
            foreach (AudioSource thrust in Thrust)
            {
                thrust.volume = 0;
            }
        }

        if (!PlaneDistantNull)
        {
            PlaneDistantInitialVolume = PlaneDistantTargetVolume = PlaneDistant.volume;
            PlaneDistant.volume = 0f;
        }

        //get a Maximum audible distance of plane based on its assumed furthest reaching audio sources (for optimization)
        if (!SonicBoomNull) MaxAudibleDistance = SonicBoom[0].maxDistance;
        if (!ExplosionNull)
        {
            if (MaxAudibleDistance < Explosion[0].maxDistance)
            { MaxAudibleDistance = Explosion[0].maxDistance; }
        }
        if (!PlaneDistantNull)
        {
            if (MaxAudibleDistance < PlaneDistant.maxDistance)
            { MaxAudibleDistance = PlaneDistant.maxDistance + 50; }
        }

        if (!PlaneWindNull) { PlaneWindInitialVolume = PlaneWindTargetVolume = PlaneWind.volume; PlaneWind.volume = 0f; }

        dopplecounter = Random.Range(0, 5);

        DopplerSounds_InitialVolumes = new float[DopplerSounds.Length];
        DopplerSounds_TargetVolumes = new float[DopplerSounds.Length];
        for (int x = 0; x != DopplerSounds.Length; x++)
        {
            DopplerSounds_InitialVolumes[x] = DopplerSounds_TargetVolumes[x] = DopplerSounds[x].volume;
        }
    }
    private void Start()
    {
        if (!Initiatlized)
        { SFEXT_L_ECStart(); }
    }
    private void Update()
    {
        float DeltaTime = Time.smoothDeltaTime;
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
            if (!InEditor)
            {
                ThisFrameDist = Vector3.Distance(localPlayer.GetPosition(), CenterOfMass.position);
                if (ThisFrameDist > MaxAudibleDistance)
                {
                    LastFrameDist = ThisFrameDist; TooFarToHear = true;
                }
                else
                {
                    TooFarToHear = false;
                }
            }
            else if ((testcamera != null))//editor and testcamera is set
            {
                ThisFrameDist = Vector3.Distance(testcamera.transform.position, CenterOfMass.position);
            }

            relativespeed = (ThisFrameDist - LastFrameDist);
            float doppletemp = (343 * (SmoothDeltaTime * 5)) + relativespeed;

            //supersonic a bit lower than the speed of sound because dopple is speed towards you, if they're coming in at an angle it won't be as high. stupid hack
            if (doppletemp < .1f)
            {
                doppletemp = .0001f; // prevent divide by 0

                //Only Supersonic if the vehicle is actually moving faster than sound, and you're not inside it (prevents sonic booms from occuring if you move past a stationary vehicle)
                if (EngineControl.CurrentVel.magnitude > 343 && !Passenger && !Piloting)
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
            //step sound wave movement
            SonicBoomWave += Mathf.Max(343 * DeltaTime, -relativespeed * .2f);//*.2 because relativespeed is only calculated every 5th frame
            silent = true;
            silentint = 0;//for multiplying sound volumes
        }
        else
        {
            silent = false;
            silentint = 1;
        }

        //Piloting = true in editor play mode
        if ((Piloting || Passenger) && AllDoorsClosed)
        {
            if (!RollingNull)
            {
                if (EngineControl.Taxiing)
                {
                    if (!Rolling.isPlaying) { Rolling.Play(); }
                    Rolling.volume = Mathf.Lerp(Rolling.volume, Mathf.Min(EngineControl.Speed * RollingVolCurve, RollingMaxVol), 3f * DeltaTime);
                }
                else
                {
                    Rolling.volume = Mathf.Lerp(Rolling.volume, Mathf.Min(0), 5f * DeltaTime);
                }
            }
            if ((Piloting || (Passenger && EngineControl.Occupied)) && EngineControl.Fuel > 1) //you're piloting or someone is piloting and you're a passenger
            {
                if (!PlaneInsideNull)
                {
                    PlaneInside.pitch = Mathf.Lerp(PlaneInside.pitch, (EngineControl.EngineOutput * .4f) + .8f, 2.25f * DeltaTime);
                    PlaneInside.volume = Mathf.Lerp(PlaneInside.volume, PlaneInsideTargetVolume, .72f * DeltaTime);
                }
                PlaneThrustVolume = Mathf.Lerp(PlaneThrustVolume, EngineControl.EngineOutput * PlaneThrustTargetVolume * InVehicleThrustVolumeFactor, 1.08f * DeltaTime);
                if (!PlaneWindNull)
                {
                    PlaneWind.pitch = Mathf.Clamp(Doppler, -10, 10);
                    PlaneWind.volume = (Mathf.Min(((EngineControl.Speed / 20) * PlaneWindTargetVolume), 1) / 10f + (Mathf.Clamp(((EngineControl.VertGs - 1) * PlaneWindTargetVolume) * .125f, 0, 1) * .2f)) * silentint;
                }
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
            if (InPlane == true)
            {
                Exitplane();//passenger left or canopy opened
            }
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
            PlaneIdleVolume = Mathf.Lerp(PlaneIdleVolume, PlaneIdleTargetVolume, .72f * DeltaTime);
            if (Doppler > 50)
            {
                PlaneDistantVolume = Mathf.Lerp(PlaneDistantVolume, 0, 3 * DeltaTime);
                PlaneThrustVolume = Mathf.Lerp(PlaneThrustVolume, 0, 3 * DeltaTime);
            }
            else
            {
                PlaneDistantVolume = Mathf.Lerp(PlaneDistantVolume, EngineControl.EngineOutput * PlaneDistantTargetVolume, .72f * DeltaTime);
                PlaneThrustVolume = Mathf.Lerp(PlaneThrustVolume, EngineControl.EngineOutput * PlaneThrustTargetVolume, 1.08f * DeltaTime);
            }
            PlaneThrustPitch = 1;
            PlaneIdlePitch = Mathf.Lerp(PlaneIdlePitch, (EngineControl.EngineOutput - 0.3f) + 1.3f, .54f * DeltaTime);
        }
        else //no one is in the plane or its out of fuel
        {
            if (InPlane == true) { Exitplane(); }//pilot or passenger left or canopy opened
            PlaneThrustVolume = Mathf.Lerp(PlaneThrustVolume, 0, 1.08f * DeltaTime);
            PlaneIdlePitch = Mathf.Lerp(PlaneIdlePitch, 0, .09f * DeltaTime);
            PlaneIdleVolume = Mathf.Lerp(PlaneIdleVolume, 0, .09f * DeltaTime);
            PlaneDistantVolume = Mathf.Lerp(PlaneDistantVolume, 0, .72f * DeltaTime);
        }

        LastFramePlaneIdlePitch = PlaneIdlePitch;
        LastFramePlaneThrustPitch = PlaneThrustPitch;

        if (!Piloting && !Passenger) //apply dopper if you're not in the vehicle
        {
            float dopplemin = Mathf.Min(Doppler, 2.25f);
            PlaneIdlePitch *= dopplemin;
            PlaneThrustPitch *= dopplemin;
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
            PlaneDistant.pitch = Mathf.Lerp(PlaneDistant.pitch, Mathf.Min(Doppler, 2.25f), 30f * DeltaTime);
        }
        foreach (AudioSource thrust in Thrust)
        {
            thrust.volume = PlaneThrustVolume * silentint;
            thrust.pitch = Mathf.Lerp(thrust.pitch, PlaneThrustPitch, 30f * DeltaTime);
        }
        int d = DopplerSounds.Length;
        for (int x = 0; x < d; x++)
        {
            DopplerSounds[x].pitch = Doppler;
            DopplerSounds[x].volume = DopplerSounds_TargetVolumes[x] * silentint;
        }
    }
    private void Exitplane()//sets sound values to give continuity of engine sound when exiting the plane or opening canopy
    {
        if (!MissileIncomingNull) MissileIncoming.gameObject.SetActive(false);
        if (!RadarLockedNull) { RadarLocked.Stop(); }
        if (!RollingNull) { Rolling.Stop(); }
        if (!PlaneInsideNull) { PlaneInside.Stop(); }
        if (!PlaneWindNull) { PlaneWind.Stop(); }
        foreach (AudioSource idle in PlaneIdle) { idle.Play(); }
        foreach (AudioSource thrust in Thrust) { thrust.Play(); }
        if (!PlaneDistantNull) PlaneDistant.Play();
        InPlane = false;
        if (!EngineControl.dead)
        {
            //these are set differently EngineController.Explode(), so we don't do them if we're dead
            PlaneIdleVolume = PlaneIdleTargetVolume * .4f;
            PlaneThrustVolume *= 6.666666f;
            PlaneDistantVolume = PlaneThrustVolume;
            if (!PlaneInsideNull) { PlaneIdlePitch = PlaneInside.pitch; }
        }
    }
    public void SFEXT_G_EnterWater()
    {
        if (EngineControl.Piloting || EngineControl.Passenger)
        {
            if (EnterWater != null) { EnterWater.Play(); }
            if (UnderWater != null) { UnderWater.Play(); }
        }

        PlaneIdlePitch = 0;
        PlaneIdleVolume = 0;
        PlaneThrustVolume = 0;
        PlaneDistantVolume = 0;
        PlaneDistantVolume = 0;
        LastFramePlaneIdlePitch = 0;
        LastFramePlaneThrustPitch = 0;

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


        PlaneInsideTargetVolume = 0;
        PlaneIdleTargetVolume = 0;
        PlaneThrustTargetVolume = 0;
        PlaneDistantTargetVolume = 0;
        PlaneWindTargetVolume = 0;
        int d = DopplerSounds_TargetVolumes.Length;
        for (int x = 0; x < d; x++)
        {
            DopplerSounds_TargetVolumes[x] = 0;
        }
    }
    public void SFEXT_G_ExitWater()
    {
        if (UnderWater != null) { if (UnderWater.isPlaying) UnderWater.Stop(); }
        PlaneInsideTargetVolume = PlaneInsideInitialVolume;
        PlaneIdleTargetVolume = PlaneIdleInitialVolume;
        PlaneThrustTargetVolume = PlaneThrustInitialVolume;
        PlaneDistantTargetVolume = PlaneDistantInitialVolume;
        PlaneWindTargetVolume = PlaneWindInitialVolume;
        int d = DopplerSounds_TargetVolumes.Length;
        for (int x = 0; x < d; x++)
        {
            DopplerSounds_TargetVolumes[x] = DopplerSounds_InitialVolumes[x];
        }
    }
    public void SFEXT_G_Explode()
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
    public void SFEXT_O_PilotEnter()
    {
        PlaneWind.Play();
        if (AllDoorsClosed) { EnterPlane(); }
        Piloting = true;
    }
    public void SFEXT_O_PilotExit()
    {
        PlaneWind.Stop();
        Piloting = false;
        if (UnderWater != null) { if (UnderWater.isPlaying) { UnderWater.Stop(); } }
    }
    public void SFEXT_P_PassengerEnter()
    {
        PlaneWind.Play();
        if (AllDoorsClosed) { EnterPlane(); }
        Passenger = true;
    }
    public void SFEXT_P_PassengerExit()
    {
        PlaneWind.Stop();
        Passenger = false;
        if (UnderWater != null) { if (UnderWater.isPlaying) { UnderWater.Stop(); } }
    }
    public void SFEXT_G_PilotEnter()//old WakeUp
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
        if (!PlaneDistantNull) { PlaneDistant.gameObject.SetActive(true); }
        if (!PlaneWindNull) { PlaneWind.gameObject.SetActive(true); }
        if (!PlaneInsideNull) { PlaneInside.gameObject.SetActive(true); }
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
    //called form DFUNC_Canopy Delayed by canopy close time when playing the canopy animation
    public void DoorClose()
    {
        DoorsOpen -= 1;
        if (DoorsOpen == 0)
        {
            if (Piloting || Passenger)
            { EnterPlane(); }
            AllDoorsClosed = true;
            if (EngineControl.IsOwner) { EngineControl.SendEventToExtensions("SFEXT_O_DoorsClosed"); }
        }
        if (DoorsOpen < 0) Debug.LogWarning("DoorsOpen is negative");
        //Debug.Log("DoorClose");
    }
    public void DoorOpen()
    {
        DoorsOpen += 1;
        if (DoorsOpen != 0)
        {
            if (Piloting || Passenger)
            { Exitplane(); }
            if (EngineControl.IsOwner && AllDoorsClosed)//if AllDoorsClosed == true then a door is open and none were last frame, so send event
            { EngineControl.SendEventToExtensions("SFEXT_O_DoorsOpened"); }
            AllDoorsClosed = false;
        }
        //Debug.Log("DoorOpen");
    }
    private void EnterPlane()
    {
        //change stuff when you get in/canopy closes
        if (!ABOnOutsideNull) { ABOnOutside.Stop(); }
        PlaneThrustPitch = 0.8f;
        if (!PlaneInsideNull && !PlaneIdleNull)
        {
            PlaneInside.pitch = PlaneIdle[0].pitch * .8f;
            PlaneInside.volume = PlaneIdle[0].volume * .4f;//it'll lerp up from here
        }
        PlaneThrustVolume *= InVehicleThrustVolumeFactor;
        InPlane = true;//set when we leave to see if we just left later

        foreach (AudioSource thrust in Thrust)
        {
            if (!thrust.isPlaying)
            { thrust.Play(); }
        }
        if (PlaneDistant.isPlaying && !PlaneDistantNull)
        { PlaneDistant.Stop(); }
        if (!PlaneWind.isPlaying && !PlaneWindNull)
        { PlaneWind.Play(); }
        if (!PlaneInside.isPlaying && !PlaneInsideNull)
        { PlaneInside.Play(); }
        if (PlaneIdle[0].isPlaying && !PlaneIdleNull)
        {
            foreach (AudioSource idle in PlaneIdle)
            { idle.Stop(); }
        }
    }
    public void SFEXT_G_TouchDown()
    {
        if (EngineControl.Speed > TouchDownSoundSpeed)
        {
            PlayTouchDownSound();
        }
        if (!Rolling_Seaplane && !RollingNull) { Rolling.volume = EngineControl.Speed * RollingVolCurve; }
    }
    public void SFEXT_G_TouchDownWater()
    {
        if (EngineControl.Speed > TouchDownSoundSpeed)
        {
            PlayTouchDownSound();
        }
    }
    public void SFEXT_G_ReSupply()
    {
        SendCustomEventDelayedFrames("ResupplySound", 1);
    }
    public void ResupplySound()
    {
        if (EngineControl.ReSupplied > 0)
        {
            if (!ReSupplyNull)
            {
                ReSupply.Play();
            }
        }
    }
    public void SFEXT_O_AfterburnerOn()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PlayAfturburnersound");
    }
    public void PlayAfturburnersound()
    {
        if ((Piloting || Passenger) && (AllDoorsClosed))
        {
            if (!ABOnInsideNull)
                ABOnInside.Play();
        }
        else
        {
            if (!ABOnOutsideNull)
                ABOnOutside.Play();
        }
    }
    public void SFEXT_O_PlaneHit()
    {

        if (!BulletHitNull)
        {
            int rand = Random.Range(0, BulletHit.Length);
            BulletHit[rand].pitch = Random.Range(.8f, 1.2f);
            BulletHit[rand].Play();
        }
    }
}

