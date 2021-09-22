//TO TEST OUTSIDE-OF-PLANE SOUNDS SET -100000 to 100000 on line 202 AND COMMENT OUT ' && !Piloting ' ON LINE 164
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class SAV_SoundController : UdonSharpBehaviour
{
    public UdonSharpBehaviour SAVControl;
    [Tooltip("Vehicle engine's 'Idle' sound, plays all the time vehicle is occupied, and pitches up with engine's output")]
    public AudioSource[] PlaneIdle;
    [Tooltip("Same as PlaneIdle but only plays when inside the vehicle with all doors closed")]
    public AudioSource PlaneInside;
    [Tooltip("Vehicle engine's sound for when it's closeby, increases volume with engine's output")]
    public AudioSource[] Thrust;
    [Tooltip("Vehicle engine's sound for when it's distant, increases volume with engine's output")]
    public AudioSource PlaneDistant;
    [Tooltip("One shot sound that plays when afterburner is toggled on, and you're inside the vehicle with all doors closed")]
    public AudioSource ABOnInside;
    [Tooltip("One shot sound that plays when afterburner is toggled on, and you're outside the vehicle")]
    public AudioSource ABOnOutside;
    [Tooltip("One shot sounds, a random one is played when vehicle touches the ground")]
    public AudioSource[] TouchDown;
    [Tooltip("Vehicle has to be moving faster than this to play the touchdown sound. Meters/s")]
    public float TouchDownSoundSpeed = 35;
    [Tooltip("'Wind' sound that gets louder with AoA and various other factors")]
    public AudioSource PlaneWind;
    [Tooltip("How quickly when pulling AoA the wind will get louder")]
    [SerializeField] private float PlaneWindMultiplier = .25f;
    [SerializeField] private float PlaneWindMaxVolume = 1;
    [Tooltip("How fast before the planewind stops getting louder when pulling AoA")]
    [SerializeField] private float PlaneWindMaxVolSpeed = 1000f;
    [Tooltip("Sounds that can be played when vehicle causes a sonic boom")]
    public AudioSource[] SonicBoom;
    [Tooltip("Sounds that can be played when vehicle explodes")]
    public AudioSource[] Explosion;
    [Tooltip("Sounds that can be played when vehicle gets hit by something")]
    public AudioSource[] BulletHit;
    [Tooltip("Sound that plays when vehicle is hit by a missile")]
    public AudioSource[] MissileHit;
    [Tooltip("Sounds played when vehicle is rolling along the ground. Also works for seaplanes")]
    public AudioSource Rolling;
    [Tooltip("Maximum volume rolling sound reaches when moving forward quickly")]
    [SerializeField] private float RollingMaxVol = 1;
    [Tooltip("How quickly the rolling sound reaches max volume as speed increases")]
    [SerializeField] private float RollingVolCurve = .03f;
    [Tooltip("If ticked, will lerp Rolling volume on touchdown. For seaplanes water touchdown")]
    [SerializeField] private bool Rolling_Seaplane;
    [Tooltip("Oneshot sound sound played each time vehicle recieves a resupply event")]
    public AudioSource ReSupply;
    [Tooltip("Looping Sound that plays when vehicle is being targeted by a missile")]
    public AudioSource RadarLocked;
    [Tooltip("Sound that plays when vehicle enters water and player is inside the vehicle")]
    public AudioSource EnterWater;
    [Tooltip("Oneshot sound that plays when vehicle enters water and player is outside of the vehicle")]
    public AudioSource EnterWaterOutside;
    [Tooltip("Looping Sound that plays while vehicle is underwater and player is inside")]
    public AudioSource UnderWater;
    [Tooltip("Add any extra sounds that you want to recieve the doppler effect to this list")]
    public AudioSource[] DopplerSounds;
    [Tooltip("Only untick this if you have no door/canopy functionality on the vehicle, and you wish to create an open-cockpit vehicle")]
    public bool AllDoorsClosed = true;
    [System.NonSerializedAttribute] public bool PlaneIdleNull = true;
    [System.NonSerializedAttribute] public bool PlaneInsideNull = true;
    [System.NonSerializedAttribute] public bool PlaneDistantNull = true;
    [System.NonSerializedAttribute] public bool ThrustNull = true;
    [System.NonSerializedAttribute] public bool ABOnInsideNull = true;
    [System.NonSerializedAttribute] public bool ABOnOutsideNull = true;
    [System.NonSerializedAttribute] public bool TouchDownNull = true;
    [System.NonSerializedAttribute] public bool PlaneWindNull = true;
    [System.NonSerializedAttribute] public bool SonicBoomNull = true;
    [System.NonSerializedAttribute] public bool ExplosionNull = true;
    [System.NonSerializedAttribute] public bool BulletHitNull = true;
    [System.NonSerializedAttribute] public bool RollingNull = true;
    [System.NonSerializedAttribute] public bool EnterWaterNull = true;
    [System.NonSerializedAttribute] public bool EnterWaterOutsideNull = true;
    [System.NonSerializedAttribute] public bool UnderwaterNull = true;
    [System.NonSerializedAttribute] public bool ReSupplyNull = true;
    [System.NonSerializedAttribute] public bool RadarLockedNull = true;
    [System.NonSerializedAttribute] public bool MissileHitNULL = true;
    private SaccEntity EntityControl;
    //public Transform testcamera;
    private float IdleDoppleTemp;
    [System.NonSerializedAttribute] public float Doppler = 1;
    float LastFrameDist;
    [System.NonSerializedAttribute] public float ThisFrameDist = 0;
    [System.NonSerializedAttribute] public float PlaneIdlePitch;
    [System.NonSerializedAttribute] public float PlaneIdleVolume;
    [System.NonSerializedAttribute] public float PlaneDistantVolume;
    private float PlaneThrustPitch;
    [System.NonSerializedAttribute] public float PlaneThrustVolume;
    private float StartPlaneThrustPitch;
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
    private float InVehicleThrustVolumeFactorReverse;
    private float PlaneWindMaxVolSpeedDivider;
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
    private Transform CenterOfMass;
    private VRCPlayerApi localPlayer;
    private bool Piloting;
    private bool Passenger;
    private bool InVehicle;
    private int DoorsOpen = 0;
    private bool InWater;
    private bool Initiatlized;
    private void SFEXT_L_EntityStart()
    {
        if (Initiatlized) { return; }
        Initiatlized = true;

        PlaneInsideNull = PlaneInside == null;
        PlaneDistantNull = PlaneDistant == null;
        ABOnInsideNull = ABOnOutside == null;
        ABOnOutsideNull = ABOnOutside == null;
        PlaneWindNull = PlaneWind == null;
        RollingNull = Rolling == null;
        UnderwaterNull = UnderWater == null;
        EnterWaterNull = EnterWater == null;
        EnterWaterOutsideNull = EnterWaterOutside == null;
        ReSupplyNull = ReSupply == null;
        RadarLockedNull = RadarLocked == null;
        MissileHitNULL = MissileHit.Length < 1;
        PlaneIdleNull = PlaneIdle.Length < 1;
        ThrustNull = Thrust.Length < 1;
        TouchDownNull = TouchDown.Length < 1;
        SonicBoomNull = SonicBoom.Length < 1;
        ExplosionNull = Explosion.Length < 1;
        BulletHitNull = BulletHit.Length < 1;

        InVehicleThrustVolumeFactorReverse = 1 / InVehicleThrustVolumeFactor;
        PlaneWindMaxVolSpeedDivider = 1 / PlaneWindMaxVolSpeed;

        localPlayer = Networking.LocalPlayer;
        if (localPlayer != null)
        { InEditor = false; }
        EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
        CenterOfMass = EntityControl.CenterOfMass;
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

        if (!ThrustNull)
        {
            PlaneThrustInitialVolume = PlaneThrustTargetVolume = Thrust[0].volume;
            StartPlaneThrustPitch = Thrust[0].pitch;
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
        { SFEXT_L_EntityStart(); }
    }
    private void Update()
    {
        float DeltaTime = Time.smoothDeltaTime;
        if (DoSound > 35f)
        {
            if (!soundsoff)//disable all the sounds that always play, re-enabled in pilotseat
            {
                if (!PlaneDistantNull) { PlaneDistant.Stop(); }
                if (!PlaneWindNull) { PlaneWind.Stop(); }
                if (!PlaneInsideNull) { PlaneInside.Stop(); }
                if (!ThrustNull)
                {
                    foreach (AudioSource thrust in Thrust)
                    { thrust.Stop(); }
                }
                if (!PlaneIdleNull)
                {
                    foreach (AudioSource idle in PlaneIdle)
                    { idle.Stop(); }
                }
                soundsoff = true;
            }
            return;
        }
        if ((bool)SAVControl.GetProgramVariable("Occupied")) { DoSound = 0f; }
        else { DoSound += DeltaTime; }


        //the doppler code is done in a really hacky way to avoid having to do it in fixedupdate and have worse performance.
        //and because even if you do it in fixedupate, it only works properly in VRChat if you have max framerate. (objects owned by other players positions are only updated in Update())
        //only calculate doppler every 5 frames to smooth out laggers and frame drops (it's also smoothed further using lerp later)
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
            /* else if ((testcamera != null))//editor and testcamera is set
            {
                ThisFrameDist = Vector3.Distance(testcamera.transform.position, CenterOfMass.position);
            } */

            relativespeed = (ThisFrameDist - LastFrameDist);
            float doppletemp = (343 * (SmoothDeltaTime * 5)) + relativespeed;

            //supersonic a bit lower than the speed of sound because dopple is speed towards you, if they're coming in at an angle it won't be as high. stupid hack (0 would be the speed of sound)
            if (doppletemp < .1f)
            {
                doppletemp = .0001f; // prevent divide by 0

                //Only Supersonic if the vehicle is actually moving faster than sound, and you're not inside it (prevents sonic booms from occuring if you move past a stationary vehicle)
                if (((Vector3)SAVControl.GetProgramVariable("CurrentVel")).magnitude > 343 && !InVehicle)
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
        if ((InVehicle) && AllDoorsClosed)
        {
            if (!RollingNull)
            {
                if ((bool)SAVControl.GetProgramVariable("Taxiing"))
                {
                    Rolling.volume = Mathf.Lerp(Rolling.volume, Mathf.Min((float)SAVControl.GetProgramVariable("Speed") * RollingVolCurve, RollingMaxVol), 3f * DeltaTime);
                }
                else
                {
                    Rolling.volume = Mathf.Lerp(Rolling.volume, Mathf.Min(0), 5f * DeltaTime);
                }
            }
            if ((Piloting || (Passenger && (bool)SAVControl.GetProgramVariable("Occupied"))) && (float)SAVControl.GetProgramVariable("Fuel") > 0.1f) //you're piloting or someone is piloting and you're a passenger
            {
                float engineout = (float)SAVControl.GetProgramVariable("EngineOutput");
                if (!PlaneInsideNull)
                {
                    PlaneInside.pitch = Mathf.Lerp(PlaneInside.pitch, (engineout * .4f) + .8f, 2.25f * DeltaTime);
                    PlaneInside.volume = Mathf.Lerp(PlaneInside.volume, PlaneInsideTargetVolume, .72f * DeltaTime);
                }
                PlaneThrustVolume = Mathf.Lerp(PlaneThrustVolume, engineout * PlaneThrustTargetVolume * InVehicleThrustVolumeFactor, 1.08f * DeltaTime);
                if (!PlaneWindNull)
                {
                    PlaneWind.volume = Mathf.Min((float)SAVControl.GetProgramVariable("AngleOfAttack") * ((float)SAVControl.GetProgramVariable("Speed") * PlaneWindMaxVolSpeedDivider) * PlaneWindMultiplier, PlaneWindMaxVolume);
                }
            }
            else//you're a passenger and no one is flying
            {
                if (!PlaneInsideNull)
                {
                    PlaneInside.pitch = Mathf.Lerp(PlaneInside.pitch, 0, .108f * DeltaTime);
                    PlaneInside.volume = Mathf.Lerp(PlaneInside.volume, 0, .72f * DeltaTime);
                }
                PlaneThrustVolume = Mathf.Lerp(PlaneThrustVolume, 0, 1.08f * DeltaTime);
            }
        }
        else if ((bool)SAVControl.GetProgramVariable("Occupied") && (float)SAVControl.GetProgramVariable("Fuel") > 1)//someone else is piloting
        {
            PlaneIdleVolume = Mathf.Lerp(PlaneIdleVolume, PlaneIdleTargetVolume, .72f * DeltaTime);
            float engineout = (float)SAVControl.GetProgramVariable("EngineOutput");
            if (Doppler > 50)
            {
                PlaneDistantVolume = Mathf.Lerp(PlaneDistantVolume, 0, 3 * DeltaTime);
                PlaneThrustVolume = Mathf.Lerp(PlaneThrustVolume, 0, 3 * DeltaTime);
            }
            else
            {
                PlaneDistantVolume = Mathf.Lerp(PlaneDistantVolume, engineout * PlaneDistantTargetVolume, .72f * DeltaTime);
                PlaneThrustVolume = Mathf.Lerp(PlaneThrustVolume, engineout * PlaneThrustTargetVolume, 1.08f * DeltaTime);
            }
            PlaneIdlePitch = Mathf.Lerp(PlaneIdlePitch, (engineout - 0.3f) + 1.3f, .54f * DeltaTime);
        }
        else //no one is in the plane or its out of fuel
        {
            PlaneThrustVolume = Mathf.Lerp(PlaneThrustVolume, 0, 1.08f * DeltaTime);
            PlaneIdlePitch = Mathf.Lerp(PlaneIdlePitch, 0, .09f * DeltaTime);
            PlaneIdleVolume = Mathf.Lerp(PlaneIdleVolume, 0, .09f * DeltaTime);
            PlaneDistantVolume = Mathf.Lerp(PlaneDistantVolume, 0, .72f * DeltaTime);
        }

        float PlaneIdlePitchDopple = PlaneIdlePitch;
        float dopplemin = Mathf.Min(Doppler, 2.25f);
        if (!InVehicle) //apply doppler if you're not in the vehicle
        {
            PlaneIdlePitchDopple *= dopplemin;
            PlaneThrustPitch = StartPlaneThrustPitch * dopplemin;
        }


        SonicBoomPreventer += DeltaTime;
        //set final volumes and pitches
        //lerp should help smooth out laggers and the dopple only being calculated every 5 frames
        if (!SonicBoomNull && !silent && playsonicboom)
        {
            if (SonicBoomPreventer > 5 && !EntityControl.dead)
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
            idle.pitch = Mathf.Lerp(idle.pitch, PlaneIdlePitchDopple, 30f * DeltaTime);
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
            DopplerSounds[x].pitch = dopplemin;
            DopplerSounds[x].volume = DopplerSounds_TargetVolumes[x] * silentint;
        }
    }
    public void SFEXT_G_EnterWater()
    {
        InWater = true;
        if (InVehicle)
        {
            if (!EnterWaterNull) { EnterWater.Play(); }
            if (!UnderwaterNull) { UnderWater.Play(); }
        }
        else
        {
            if (!EnterWaterOutsideNull) { EnterWaterOutside.Play(); }
        }

        if (!ABOnInsideNull && ABOnInside.isPlaying)
        { ABOnInside.Stop(); }

        if (!ABOnOutsideNull && ABOnOutside.isPlaying)
        { ABOnOutside.Stop(); }


        PlaneIdlePitch = 0;
        PlaneIdleVolume = 0;
        PlaneThrustVolume = 0;
        PlaneDistantVolume = 0;
        PlaneDistantVolume = 0;

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
        InWater = false;
        if (!UnderwaterNull) { if (UnderWater.isPlaying) UnderWater.Stop(); }
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
    public void SFEXT_G_RespawnButton()
    {
        ResetSounds();
    }
    public void SFEXT_G_Explode()
    {
        ResetSounds();
        //play the sonic boom that is coming towards you, after the vehicle explodes with the correct delay
        if (playsonicboom && silent)
        {
            if (!SonicBoomNull)
            {
                int rand = Random.Range(0, SonicBoom.Length);
                if (SonicBoom[rand] != null)
                {
                    SonicBoom[rand].pitch = Random.Range(.94f, 1.2f);
                    float delay = (SonicBoomDistance - SonicBoomWave) / 343;
                    if (delay < 4)
                    {
                        SonicBoom[rand].PlayDelayed(delay);
                    }
                }
            }
        }
        if (!ExplosionNull)
        {
            int rand = Random.Range(0, Explosion.Length);
            if (Explosion[rand] != null)
            {
                Explosion[rand].Play();
            }
        }
    }
    public void SFEXT_O_PilotEnter()
    {
        Piloting = true;
        InVehicle = true;
        if (AllDoorsClosed) { SetSoundsInside(); }
        if (InWater) { if (!UnderwaterNull) { UnderWater.Play(); } }
    }
    public void SFEXT_O_PilotExit()
    {
        Piloting = false;
        InVehicle = false;
        if (!PlaneWindNull) { PlaneWind.Stop(); }
        if (!UnderwaterNull) { if (UnderWater.isPlaying) { UnderWater.Stop(); } }
        if (AllDoorsClosed)
        { SetSoundsOutside(); }
    }
    public void SFEXT_P_PassengerEnter()
    {
        Passenger = true;
        InVehicle = true;
        if (AllDoorsClosed) { SetSoundsInside(); }
        if (InWater) { if (!UnderwaterNull) { UnderWater.Play(); } }
    }
    public void SFEXT_P_PassengerExit()
    {
        Passenger = false;
        InVehicle = false;
        if (!PlaneWindNull) PlaneWind.Stop();
        if (!UnderwaterNull) { if (UnderWater.isPlaying) { UnderWater.Stop(); } }
        if (AllDoorsClosed)
        { SetSoundsOutside(); }
    }
    public void SFEXT_G_PilotEnter()//old WakeUp
    {
        DoSound = 0f;
        if (soundsoff)
        {
            ResetSounds();
        }
        foreach (AudioSource thrust in Thrust)
        {
            if (!thrust.isPlaying)
            { thrust.Play(); }
        }
        if (!InVehicle || !AllDoorsClosed)
        {
            if (!PlaneIdleNull && !PlaneIdle[0].isPlaying)
            {
                foreach (AudioSource idle in PlaneIdle)
                { idle.Play(); }
            }
            if (!PlaneDistantNull && !PlaneDistant.isPlaying)
            {
                { PlaneDistant.Play(); }
            }
        }
        soundsoff = false;
    }
    public void SFEXT_G_BulletHit()
    {
        if (!BulletHitNull)
        {
            int rand = Random.Range(0, BulletHit.Length);
            BulletHit[rand].pitch = Random.Range(.8f, 1.2f);
            BulletHit[rand].Play();
        }
    }
    public void SFEXT_G_TouchDown()
    {
        if ((float)SAVControl.GetProgramVariable("Speed") > TouchDownSoundSpeed)
        {
            PlayTouchDownSound();
        }
        if (!Rolling_Seaplane && !RollingNull) { Rolling.volume = (float)SAVControl.GetProgramVariable("Speed") * RollingVolCurve; }
    }
    public void SFEXT_G_TouchDownWater()
    {
        if ((float)SAVControl.GetProgramVariable("Speed") > TouchDownSoundSpeed)
        {
            PlayTouchDownSound();
        }
    }
    public void SFEXT_G_ReSupply()
    {
        SendCustomEventDelayedFrames("ResupplySound", 1);
    }
    public void SFEXT_O_AfterburnerOn()
    {
        if (!InWater)
        { SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PlayAfturburnersound"); }
    }
    public void SFEXT_G_MissileHit25()
    { if (InVehicle && !MissileHitNULL) { PlayMissileHit(); } }
    public void SFEXT_G_MissileHit50()
    { if (InVehicle && !MissileHitNULL) { PlayMissileHit(); } }
    public void SFEXT_G_MissileHit75()
    { if (InVehicle && !MissileHitNULL) { PlayMissileHit(); } }
    public void SFEXT_G_MissileHit100()
    { if (InVehicle && !MissileHitNULL) { PlayMissileHit(); } }
    public void PlayMissileHit()
    {
        int rand = Random.Range(0, MissileHit.Length);
        MissileHit[rand].pitch = Random.Range(.8f, 1.2f);
        MissileHit[rand].Play();
    }
    public void ResetSounds()
    {
        InWater = false;
        playsonicboom = false;
        silent = false;
        PlaneIdlePitch = 0;
        PlaneIdleVolume = 0;
        PlaneThrustVolume = 0;
        PlaneDistantVolume = 0;

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
    //called form DFUNC_Canopy Delayed by canopy close time when playing the canopy animation, can be used to close any door
    public void DoorClose()
    {
        DoorsOpen -= 1;
        if (DoorsOpen == 0)
        {
            AllDoorsClosed = true;
            if (InVehicle)
            { SetSoundsInside(); }
            if ((bool)SAVControl.GetProgramVariable("IsOwner")) { EntityControl.SendEventToExtensions("SFEXT_O_DoorsClosed"); }
        }
        if (DoorsOpen < 0) Debug.LogWarning("DoorsOpen is negative");
        //Debug.Log("DoorClose");
    }
    public void DoorOpen()
    {
        DoorsOpen += 1;
        if (DoorsOpen != 0)
        {
            if (AllDoorsClosed && InVehicle)//only run exitplane if doors were closed before
            { SetSoundsOutside(); }
            if ((bool)SAVControl.GetProgramVariable("IsOwner") && AllDoorsClosed)//if AllDoorsClosed == true then all doors were closed last frame, so send 'opened' event
            { EntityControl.SendEventToExtensions("SFEXT_O_DoorsOpened"); }
            AllDoorsClosed = false;
        }
        //Debug.Log("DoorOpen");
    }
    private void SetSoundsInside()
    {
        //change stuff when you get in/canopy closes
        if (!ABOnOutsideNull) { ABOnOutside.Stop(); }
        if (!PlaneInsideNull && !PlaneIdleNull)
        {
            PlaneInside.pitch = PlaneIdle[0].pitch * .8f;
            PlaneInside.volume = PlaneIdle[0].volume * .4f;//it'll lerp up from here
        }
        PlaneThrustVolume *= InVehicleThrustVolumeFactor;

        if (!RollingNull)
        {
            Rolling.Play();
            Rolling.volume = 0;
        }

        foreach (AudioSource thrust in Thrust)
        {
            if (!thrust.isPlaying)
            { thrust.Play(); }
        }
        if (!PlaneDistantNull && PlaneDistant.isPlaying)
        { PlaneDistant.Stop(); }
        if (!PlaneWindNull && !PlaneWind.isPlaying)
        { PlaneWind.volume = 0; PlaneWind.Play(); }
        if (!PlaneInsideNull && !PlaneInside.isPlaying)
        { PlaneInside.Play(); }
        if (!PlaneIdleNull && PlaneIdle[0].isPlaying)
        {
            foreach (AudioSource idle in PlaneIdle)
            { idle.Stop(); }
        }
    }
    private void SetSoundsOutside()//sets sound values to give continuity of engine sound when exiting the plane or opening canopy
    {
        if (!RadarLockedNull) { RadarLocked.Stop(); }
        if (!RollingNull) { Rolling.Stop(); }
        if (!PlaneInsideNull) { PlaneInside.Stop(); }
        if (!PlaneWindNull) { PlaneWind.Stop(); }

        if (!EntityControl.dead)
        {
            foreach (AudioSource idle in PlaneIdle) { idle.Play(); }
            foreach (AudioSource thrust in Thrust) { thrust.Play(); }
            if (!PlaneDistantNull) { PlaneDistant.Play(); }
            PlaneIdleVolume = PlaneIdleTargetVolume * .4f;
            PlaneThrustVolume *= PlaneThrustTargetVolume * InVehicleThrustVolumeFactorReverse;
            PlaneDistantVolume = PlaneThrustVolume;
            if (!PlaneInsideNull) { PlaneIdlePitch = PlaneInside.pitch; }
        }
    }
    public void ResupplySound()
    {
        if ((int)SAVControl.GetProgramVariable("ReSupplied") > 0)
        {
            if (!ReSupplyNull)
            {
                ReSupply.Play();
            }
        }
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
}

