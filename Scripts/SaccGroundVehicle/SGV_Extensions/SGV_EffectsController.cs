//Sound and Effects for SaccGroundVehicle
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [DefaultExecutionOrder(5000)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SGV_EffectsController : UdonSharpBehaviour
    {
        public SaccGroundVehicle SGVControl;
        [Tooltip("Engine sounds to set pitch and doppler, DO NOT ANIMATE PITCH IN THE REVS ANIMATION")]
        public AudioSource[] EngineSounds;
        bool EngineSounds_playing;
        public AudioSource[] EngineSounds_Interior;
        bool EngineSounds_Interior_playing;
        bool noInteriorEngineSounds;
        private Transform[] EngineSoundsT;
        bool EngineSoundsActive;
        public bool DespawnIfUnused = false;
        [SerializeField] private float DespawnDist = 50f;
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
        [Tooltip("When you do damage to an enemy, play this sound")]
        public AudioSource DamageFeedBack;
        [Tooltip("Sounds that can be played when vehicle explodes")]
        public AudioSource[] Explosion;
        [Tooltip("Sounds that can be played when vehicle explodes underwater")]
        public AudioSource[] Explosion_Water;
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
        [Tooltip("Oneshot sound sound played each time vehicle recieves a refuel event")]
        public AudioSource ReFuel;
        [Tooltip("Oneshot sound sound played each time vehicle recieves a rearm event")]
        public AudioSource ReArm;
        [Tooltip("Oneshot sound sound played each time vehicle recieves a repair event")]
        public AudioSource Repair;
        public AudioSource GearChange;
        public AudioSource GearUp;
        public AudioSource GearDown;
        // public Transform testcamera;
        [Tooltip("Physics scripts that only need to be enabled if you're owner, and the vehicle is awake")]
        public GameObject[] Wings;
        [Tooltip("Driver seat reference to move the player around with G-forces")]
        [SerializeField] Transform DriverSeatMovement;
        [Tooltip("Maximum distance away from default the player will be moved by Gs")]
        public float SeatMovement_MaxDist = .65f;
        [Tooltip("Multiplier for how strongly Gs effect the player movement")]
        public float SeatMovement_ForceSTR = .2f;
        [Tooltip("How quickly the seat returns to default")]
        public float SeatMovement_SpringSTR = 15f;
        [Tooltip("Multiplier for strength of vertical G-forces")]
        public float SeatMovement_LRMulti = 1f;
        [Tooltip("Multiplier for strength of vertical G-forces")]
        public float SeatMovement_VertMulti = .33f;
        [System.NonSerialized] float SeatMovement_Slider = 1f;
        [Header("Tank Stuff:")]
        public bool DoCaterpillarTracks;
        [Tooltip("Object with the track materials on it")]
        public Renderer TracksRenderer;
        [Tooltip("material index of the tracks")]
        public int[] TrackMaterialSlots;
        public UdonSharpBehaviour[] TrackSourceWheels;
        [System.NonSerialized] public float[] TrackRotations;
        public Vector2[] TrackSpeedMulti = new Vector2[2];
        [Header("Corresponding arrays must be of equal length")]
        [Tooltip("Extra objects that rotate with the tracks, each with their own speed multiplier\nTrack0/1/2/3 use TrackSourceWheels0/1/2/3")]
        [SerializeField] Transform[] CogWheelsTrack0;
        [SerializeField] float[] CogWheelsTrack0_rotSpeeds;
        [SerializeField] Transform[] CogWheelsTrack1;
        [SerializeField] float[] CogWheelsTrack1_rotSpeeds;
        [SerializeField] Transform[] CogWheelsTrack2;
        [SerializeField] float[] CogWheelsTrack2_rotSpeeds;
        [SerializeField] Transform[] CogWheelsTrack3;
        [SerializeField] float[] CogWheelsTrack3_rotSpeeds;
        private Material[] Tracks = new Material[0];
        [Tooltip("Object that shows where the turret is pointing")]
        [SerializeField] Transform[] TurretAngleIndicator;
        [Tooltip("Object that shows where the commander is pointing")]
        [SerializeField] Transform[] CommanderAngleIndicator;
        [SerializeField] Transform Turret;
        [SerializeField] Transform Commander;
        [SerializeField] AudioSource TrackSound;
        [SerializeField] AudioSource TrackSound_Interior;
        bool noInteriorTrackSound;
        bool TrackSound_playing;
        bool TrackSound_Interior_playing;
        [SerializeField] float TrackSoundVolMulti = 1;
        [SerializeField] float TrackSoundPitchMulti = 1;
        [SerializeField] float TrackSoundMaxVol = 1;
        [SerializeField] float TrackSoundMaxPitch = 1;
        [SerializeField] float TrackSoundVolMulti_Interior = 1;
        [SerializeField] float TrackSoundPitchMulti_Interior = 1;
        [SerializeField] float TrackSoundMaxVol_Interior = 1;
        [SerializeField] float TrackSoundMaxPitch_Interior = 1;
        private SaccEntity EntityControl;
        private Animator VehicleAnimator;
        private Transform VehicleTransform;
        [System.NonSerializedAttribute] public bool AllDoorsClosed;//Tracks whether the local user is hearing the insde or outside sounds.
        private bool InWater;
        private bool Occupied;
        private bool Sleeping = true;
        private float RevLimiter = 1;
        private float DoEffects = 999f;
        private UdonSharpBehaviour[] DriveWheels;
        private UdonSharpBehaviour[] SteerWheels;
        private UdonSharpBehaviour[] OtherWheels;
        private bool InEditor;
        private bool Piloting;
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
        [System.NonSerializedAttribute] public bool Explosion_WaterNull = true;
        [System.NonSerializedAttribute] public bool BulletHitNull = true;
        VRCPlayerApi localPlayer;
        public void SFEXT_L_EntityStart()
        {
            if (DriverSeatMovement) GFeedback_SeatStartPos = DriverSeatMovement.localPosition;
            VehicleAnimator = EntityControl.GetComponent<Animator>();
            CenterOfMass = EntityControl.CenterOfMass;
            VehicleTransform = EntityControl.transform;
            DriveWheels = (UdonSharpBehaviour[])SGVControl.GetProgramVariable("DriveWheels");
            SteerWheels = (UdonSharpBehaviour[])SGVControl.GetProgramVariable("SteerWheels");
            OtherWheels = (UdonSharpBehaviour[])SGVControl.GetProgramVariable("OtherWheels");
            localPlayer = Networking.LocalPlayer;
            InEditor = localPlayer == null;
            IsOwner = EntityControl.IsOwner;
            VehicleAnimator.SetBool("owner", IsOwner);
            EngineSoundsT = new Transform[EngineSounds.Length];
            for (int i = 0; i < EngineSounds.Length; i++)
            {
                EngineSoundsT[i] = EngineSounds[i].transform;
            }

            EnableWings(IsOwner);

            FullHealthDivider = 1f / (float)SGVControl.GetProgramVariable("Health");
            FullFuelDivider = 1f / (float)SGVControl.GetProgramVariable("Fuel");
            RevLimiter = (float)SGVControl.GetProgramVariable("RevLimiter");
            EntityControl = (SaccEntity)SGVControl.GetProgramVariable("EntityControl");
            ExplosionNull = Explosion.Length == 0;
            Explosion_WaterNull = Explosion_Water.Length == 0;
            BulletHitNull = BulletHit.Length == 0;
            SmallCrashNULL = SmallCrash.Length == 0;
            MediumCrashNULL = MediumCrash.Length == 0;
            BigCrashNULL = BigCrash.Length == 0;
            SmallCrashInsideNULL = SmallCrashInside.Length == 0;
            MediumCrashInsideNULL = MediumCrashInside.Length == 0;
            BigCrashInsideNULL = BigCrashInside.Length == 0;
            TrackRotations = new float[TrackSourceWheels.Length];

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
            if (DoCaterpillarTracks)
            {
                int numtracks = TrackMaterialSlots.Length;
                Tracks = new Material[numtracks];
                for (int i = 0; i < TrackMaterialSlots.Length; i++)
                {
                    Tracks[i] = TracksRenderer.materials[TrackMaterialSlots[i]];
                }
            }
            for (int i = 0; i < EngineSounds.Length; i++)
            { EngineSounds[i].gameObject.SetActive(false); }
            for (int i = 0; i < EngineSounds_Interior.Length; i++)
            { EngineSounds_Interior[i].gameObject.SetActive(false); }
            if (TrackSound) { TrackSound.gameObject.SetActive(false); }
            if (TrackSound_Interior) { TrackSound_Interior.gameObject.SetActive(false); }
            noInteriorTrackSound = !TrackSound_Interior;
            noInteriorEngineSounds = EngineSounds_Interior.Length == 0;
            EngineSounds_Interior_playing = EngineSounds_playing = TrackSound_playing = TrackSound_Interior_playing = false;

            FallAsleep();// sleep until sync script wakes up
            SendCustomEventDelayedSeconds(nameof(WakeUp_Initial), Random.Range(5f, 7f));// same activation delay as sync script
        }
        private bool KeepAwake = false;
        public void SFEXT_L_KeepAwake() { KeepAwake = true; WakeUp(); }
        public void SFEXT_L_KeepAwakeFalse() { KeepAwake = false; }
        Vector3 GFeedback_SeatStartPos;
        Vector3 Gs_Last;
        private void LateUpdate()
        {
            if (Sleeping)
            { return; }
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
            if (DoCaterpillarTracks)
            {
                float wheelSpeed = 0;
                for (int i = 0; i < TrackSourceWheels.Length; i++)
                {
                    Vector2 uvs;
                    TrackRotations[i] = (float)TrackSourceWheels[i].GetProgramVariable("WheelRotation");
                    Vector2 wheelRotUV = TrackRotations[i] * TrackSpeedMulti[i];
                    wheelRotUV.x = wheelRotUV.x - Mathf.Floor(wheelRotUV.x);
                    wheelRotUV.y = wheelRotUV.y - Mathf.Floor(wheelRotUV.x);
                    uvs = wheelRotUV;
                    Tracks[i].mainTextureOffset = uvs;
                    wheelSpeed += Mathf.Abs((float)TrackSourceWheels[i].GetProgramVariable("WheelRotationSpeedRPS"));

                    switch (i)
                    {
                        case 0:
                            for (int o = 0; o < CogWheelsTrack0.Length; o++)
                            {
                                Quaternion newrot = Quaternion.AngleAxis(TrackRotations[i] * CogWheelsTrack0_rotSpeeds[o], Vector3.right);
                                CogWheelsTrack0[o].localRotation = newrot;
                            }
                            break;
                        case 1:
                            for (int o = 0; o < CogWheelsTrack1.Length; o++)
                            {
                                Quaternion newrot = Quaternion.AngleAxis(TrackRotations[i] * CogWheelsTrack1_rotSpeeds[o], Vector3.right);
                                CogWheelsTrack1[o].localRotation = newrot;
                            }
                            break;
                        case 2:
                            for (int o = 0; o < CogWheelsTrack2.Length; o++)
                            {
                                Quaternion newrot = Quaternion.AngleAxis(TrackRotations[i] * CogWheelsTrack2_rotSpeeds[o], Vector3.right);
                                CogWheelsTrack2[o].localRotation = newrot;
                            }
                            break;
                        case 3:
                            for (int o = 0; o < CogWheelsTrack3.Length; o++)
                            {
                                Quaternion newrot = Quaternion.AngleAxis(TrackRotations[i] * CogWheelsTrack3_rotSpeeds[o], Vector3.right);
                                CogWheelsTrack3[o].localRotation = newrot;
                            }
                            break;
                    }
                }
                if (TrackSound_Interior_playing)
                {
                    if (TrackSound_Interior)
                    {
                        TrackSound_Interior.volume = Mathf.Min(wheelSpeed * TrackSoundVolMulti_Interior, TrackSoundMaxVol_Interior);
                        TrackSound_Interior.pitch = Mathf.Min(wheelSpeed * TrackSoundPitchMulti_Interior, TrackSoundMaxPitch_Interior);
                    }
                }
                else if (TrackSound_playing)
                {
                    if (TrackSound)
                    {
                        TrackSound.volume = Mathf.Min(wheelSpeed * TrackSoundVolMulti, TrackSoundMaxVol);
                        TrackSound.pitch = Mathf.Min(wheelSpeed * TrackSoundPitchMulti, TrackSoundMaxPitch);
                    }
                }
            }
            if (InVehicle)
            {
                if (Piloting && DriverSeatMovement)
                {
                    Vector3 gs = SGVControl.Gs_all / SGVControl.NumFUinAvgTime;
                    gs.y -= 1; // remove gravity
                    gs.x *= SeatMovement_LRMulti;
                    gs.y *= SeatMovement_VertMulti;
                    gs *= SeatMovement_Slider;
                    DriverSeatMovement.position -= VehicleTransform.rotation * gs * Time.deltaTime * SeatMovement_ForceSTR;
                    if ((DriverSeatMovement.localPosition - GFeedback_SeatStartPos).magnitude > SeatMovement_MaxDist)
                    {
                        Vector3 dif = DriverSeatMovement.localPosition - GFeedback_SeatStartPos;
                        dif = Vector3.ClampMagnitude(dif, SeatMovement_MaxDist);
                        DriverSeatMovement.localPosition = GFeedback_SeatStartPos + dif;
                    }
                    DriverSeatMovement.localPosition = Vector3.Lerp(DriverSeatMovement.localPosition, GFeedback_SeatStartPos, 1 - Mathf.Pow(0.5f, Time.deltaTime * SeatMovement_SpringSTR));
                }
                if (DriverHeading)
                {
                    float heading = VehicleTransform.eulerAngles.y + HeadingOffset;
                    if (heading > 360) heading -= 360;
                    if (heading < 0) heading += 360;
                    heading -= 180;
                    Vector3 pos = DriverHeading.localPosition;
                    pos.x = heading / 10;
                    DriverHeading.localPosition = pos;
                }
                if (Turret)
                {
                    float angleTurret = Vector3.SignedAngle(VehicleTransform.forward, Vector3.ProjectOnPlane(Turret.forward, VehicleTransform.up), VehicleTransform.up);
                    for (int i = 0; i < TurretAngleIndicator.Length; i++)
                    {
                        TurretAngleIndicator[i].localRotation = Quaternion.Euler(0, 0, angleTurret);
                    }
                    if (GunnerHeading)
                    {
                        float heading = Turret.eulerAngles.y + HeadingOffset;
                        if (heading > 360) heading -= 360;
                        if (heading < 0) heading += 360;
                        heading -= 180;
                        Vector3 pos = GunnerHeading.localPosition;
                        pos.x = heading / 10;
                        GunnerHeading.localPosition = pos;
                    }
                    if (Commander)
                    {
                        float angleCommander = Vector3.SignedAngle(VehicleTransform.forward, Vector3.ProjectOnPlane(Commander.forward, VehicleTransform.up), VehicleTransform.up);
                        for (int i = 0; i < CommanderAngleIndicator.Length; i++)
                        {
                            CommanderAngleIndicator[i].localRotation = Quaternion.Euler(0, 0, angleCommander);
                        }
                        if (CommanderHeading)
                        {
                            float heading = Commander.eulerAngles.y + HeadingOffset;
                            if (heading > 360) heading -= 360;
                            if (heading < 0) heading += 360;
                            heading -= 180;
                            Vector3 pos = CommanderHeading.localPosition;
                            pos.x = heading / 10;
                            CommanderHeading.localPosition = pos;
                        }
                    }
                }
            }
            if (!Occupied)
            {
                if (DoEffects > 10)
                {
                    if (!KeepAwake && !InVehicle && (float)SGVControl.GetProgramVariable("Revs") / RevLimiter < 0.015f && (float)SGVControl.GetProgramVariable("VehicleSpeed") < 0.1f)
                    { FallAsleep(); }
                    else
                        DoEffects = 8f;
                }
                DoEffects += Time.deltaTime;
            }
        }
        [Range(-180, 180)][SerializeField] float HeadingOffset = 0;
        public Transform DriverHeading;
        public Transform GunnerHeading;
        public Transform CommanderHeading;
        public void FallAsleep()
        {
            if (DespawnIfUnused)
            {
                CheckingToDisable = true;
                CheckDisableLoop();
            }
            Sleeping = true;
            EngineSounds_playing = EngineSounds_Interior_playing = false;
            for (int i = 0; i < EngineSounds.Length; i++)
            { EngineSounds[i].gameObject.SetActive(false); }
            for (int i = 0; i < EngineSounds_Interior.Length; i++)
            { EngineSounds_Interior[i].gameObject.SetActive(false); }
            if (TrackSound) { TrackSound.gameObject.SetActive(false); }
            if (TrackSound_Interior) { TrackSound_Interior.gameObject.SetActive(false); }
            if (SGVControl.TankMode)
            {
                VehicleAnimator.SetFloat(THROTTLE_STRING, .5f);
                VehicleAnimator.SetFloat(STEERING_STRING, .5f);
            }
            else
                VehicleAnimator.SetFloat(THROTTLE_STRING, 0);
            VehicleAnimator.SetFloat(REVS_STRING, 0);

            for (int i = 0; i < DriveWheels.Length; i++)
            { DriveWheels[i].SendCustomEvent("FallAsleep"); }
            for (int i = 0; i < SteerWheels.Length; i++)
            { SteerWheels[i].SendCustomEvent("FallAsleep"); }
            for (int i = 0; i < OtherWheels.Length; i++)
            { OtherWheels[i].SendCustomEvent("FallAsleep"); }

            if (IsOwner) { EnableWings(false); }

            EntityControl.SendEventToExtensions("SFEXT_L_FallAsleep");
        }
        public void ResetGrips()
        {
            for (int i = 0; i < DriveWheels.Length; i++)
            { DriveWheels[i].SendCustomEvent("ResetGrip"); }
            for (int i = 0; i < SteerWheels.Length; i++)
            { SteerWheels[i].SendCustomEvent("ResetGrip"); }
            for (int i = 0; i < OtherWheels.Length; i++)
            { OtherWheels[i].SendCustomEvent("ResetGrip"); }
        }
        public void SFEXT_G_RespawnButton()
        {
            WakeUp();
            ResetGrips();

            if (DoCaterpillarTracks)
            {
                for (int i = 0; i < Tracks.Length; i++)
                    TrackRotations[i] = 0f;

                for (int o = 0; o < CogWheelsTrack0.Length; o++)
                    CogWheelsTrack0[o].localRotation = Quaternion.identity;
                for (int o = 0; o < CogWheelsTrack1.Length; o++)
                    CogWheelsTrack1[o].localRotation = Quaternion.identity;
                for (int o = 0; o < CogWheelsTrack2.Length; o++)
                    CogWheelsTrack2[o].localRotation = Quaternion.identity;
                for (int o = 0; o < CogWheelsTrack3.Length; o++)
                    CogWheelsTrack3[o].localRotation = Quaternion.identity;
            }
        }
        public void SFEXT_L_GrappleAttach()
        {
            WakeUp();
        }
        public void SFEXT_L_WakeUp()
        {
            if (!Sleeping) return;
            if (DespawnIfUnused && !EntityControl.gameObject.activeSelf)
            {
                EntityControl.gameObject.SetActive(true);
            }
            CheckingToDisable = false;
            Sleeping = false;
            DoEffects = 8f;

            if (IsOwner) { EnableWings(true); }

            for (int i = 0; i < DriveWheels.Length; i++)
            { DriveWheels[i].SendCustomEvent("WakeUp"); }
            for (int i = 0; i < SteerWheels.Length; i++)
            { SteerWheels[i].SendCustomEvent("WakeUp"); }
            for (int i = 0; i < OtherWheels.Length; i++)
            { OtherWheels[i].SendCustomEvent("WakeUp"); }
        }
        public void WakeUp_Initial()
        {
            if (Time.deltaTime > 0.06666f)
                SendCustomEventDelayedSeconds(nameof(WakeUp_Initial), Random.Range(.25f, 1f));
            else
                WakeUp();
        }
        public void WakeUp()
        {
            EntityControl.SendEventToExtensions("SFEXT_L_WakeUp");
        }
        private void EnableWings(bool enable)
        {
            for (int i = 0; i < Wings.Length; i++)
            {
                Wings[i].SetActive(enable);
            }
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

            if (!IsOwner)
            {
                if (InVehicle)
                {
                    if (InVehicle_Sounds)
                    { SetSoundsInside(); }
                    else
                    { SetSoundsOutside(); }
                }
                else
                {
                    SetSoundsOutside();
                }
            }
        }
        public void SFEXT_G_PilotExit()
        {
            Occupied = false;
            VehicleAnimator.SetBool("occupied", false);
        }
        public void SFEXT_O_PilotEnter()
        {
            Piloting = true;
            InVehicle = true;
            if (EntityControl.MySeat != -1)
            {
                InVehicle_Sounds = EntityControl.VehicleSeats[EntityControl.MySeat].numOpenDoors == 0;
            }
            VehicleAnimator.SetBool("insidevehicle", true);
            VehicleAnimator.SetBool("piloting", true);

            UpdateDoorsOpen();
            SendWheelEnter();
        }
        public void SFEXT_O_PilotExit()
        {
            Piloting = false;
            InVehicle = InVehicle_Sounds = false;
            if (UnderWater) { if (UnderWater.isPlaying) { UnderWater.Stop(); } }
            VehicleAnimator.SetBool("insidevehicle", false);
            VehicleAnimator.SetBool("piloting", false);
            if (UnderWater) { if (UnderWater.isPlaying) UnderWater.Stop(); }
            if (DriverSeatMovement) DriverSeatMovement.localPosition = GFeedback_SeatStartPos;

            SetSoundsOutside();
            SendWheelExit();
        }
        public void SFEXT_P_PassengerEnter()
        {
            InVehicle = true;
            WakeUp();
            if (EntityControl.MySeat != -1)
            {
                InVehicle_Sounds = EntityControl.VehicleSeats[EntityControl.MySeat].numOpenDoors == 0;
            }
            VehicleAnimator.SetBool("insidevehicle", true);
            VehicleAnimator.SetBool("passenger", true);

            if (EngineSounds_playing || EngineSounds_Interior_playing || TrackSound_playing || TrackSound_Interior_playing)
            { UpdateDoorsOpen(); }
            SendWheelEnter();
        }
        public void SFEXT_P_PassengerExit()
        {
            InVehicle = InVehicle_Sounds = false;
            VehicleAnimator.SetBool("insidevehicle", false);
            VehicleAnimator.SetBool("passenger", false);
            if (UnderWater) { if (UnderWater.isPlaying) UnderWater.Stop(); }

            if (EngineSounds_playing || EngineSounds_Interior_playing || TrackSound_playing || TrackSound_Interior_playing)
            { SetSoundsOutside(); }
            SendWheelExit();
        }
        public void SetSoundsInside()
        {
            if (noInteriorEngineSounds)
                SetEngineSounds_Outside();
            else
                SetEngineSounds_Inside();

            if (noInteriorTrackSound)
                SetTrackSounds_Outside();
            else
                SetTrackSounds_Inside();

            AllDoorsClosed = true;

            VehicleAnimator.SetBool("insidesounds", true);
        }
        public void SetSoundsOutside()
        {
            SetEngineSounds_Outside();
            SetTrackSounds_Outside();
            AllDoorsClosed = false;

            VehicleAnimator.SetBool("insidesounds", false);
        }
        public void SetEngineSounds_Inside()
        {
            if (!EngineSounds_Interior_playing)
            {
                EngineSounds_Interior_playing = HasFuel && EngineSounds_Interior.Length > 0;
                for (int i = 0; i < EngineSounds_Interior.Length; i++)
                { EngineSounds_Interior[i].gameObject.SetActive(EngineSounds_Interior_playing); }
            }

            if (EngineSounds_playing)
            {
                EngineSounds_playing = false;
                for (int i = 0; i < EngineSounds.Length; i++)
                { EngineSounds[i].gameObject.SetActive(EngineSounds_playing); }
            }
        }
        public void SetEngineSounds_Outside()
        {
            if (EngineSounds_Interior_playing)
            {
                EngineSounds_Interior_playing = false;
                for (int i = 0; i < EngineSounds_Interior.Length; i++)
                { EngineSounds_Interior[i].gameObject.SetActive(false); }
            }

            if (!EngineSounds_playing)
            {
                EngineSounds_playing = HasFuel;
                for (int i = 0; i < EngineSounds.Length; i++)
                { EngineSounds[i].gameObject.SetActive(EngineSounds_playing); }
            }
        }
        public void SetTrackSounds_Outside()
        {
            TrackSound_Interior_playing = false;
            if (TrackSound_Interior) { TrackSound_Interior.gameObject.SetActive(false); }
            if (TrackSound)
            {
                TrackSound_playing = true;
                TrackSound.gameObject.SetActive(true);
            }
        }
        public void SetTrackSounds_Inside()
        {
            TrackSound_playing = false;
            if (TrackSound) { TrackSound.gameObject.SetActive(false); }
            if (TrackSound_Interior)
            {
                TrackSound_Interior_playing = true;
                TrackSound_Interior.gameObject.SetActive(true);
            }
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
            SendCustomEventDelayedFrames(nameof(ResupplySound), 1);
        }
        public void ResupplySound()
        {
            if ((int)EntityControl.GetProgramVariable("ReSupplied") > 0)
            {
                if (ReSupply)
                {
                    ReSupply.Play();
                }
            }
        }
        public void SFEXT_G_ReFuel()
        {
            SendCustomEventDelayedFrames(nameof(ReFuelSound), 1);
        }
        public void ReFuelSound()
        {
            if ((int)EntityControl.GetProgramVariable("ReSupplied") > 0)
            {
                if (ReFuel)
                {
                    ReFuel.Play();
                }
            }
        }
        public void SFEXT_G_ReArm()
        {
            SendCustomEventDelayedFrames(nameof(ReArmSound), 1);
        }
        public void ReArmSound()
        {
            if ((int)EntityControl.GetProgramVariable("ReSupplied") > 0)
            {
                if (ReArm)
                {
                    ReArm.Play();
                }
            }
        }
        public void SFEXT_G_RePair()
        {
            SendCustomEventDelayedFrames(nameof(RePairSound), 1);
        }
        public void RePairSound()
        {
            if ((int)EntityControl.GetProgramVariable("ReSupplied") > 0)
            {
                if (Repair)
                {
                    Repair.Play();
                }
            }
        }
        public void SFEXT_G_CarChangeGear()
        {
            if (GearChange) { GearChange.PlayOneShot(GearChange.clip); }
            VehicleAnimator.SetInteger("currentgear", (int)SGVControl.GetProgramVariable("CurrentGear"));
        }
        public void SFEXT_G_CarGearUp()
        {
            if (GearUp) { GearUp.PlayOneShot(GearUp.clip); }
            VehicleAnimator.SetTrigger("gearup");
        }
        public void SFEXT_G_CarGearDown()
        {
            if (GearDown) { GearDown.PlayOneShot(GearDown.clip); }
            VehicleAnimator.SetTrigger("geardown");
        }
        public void SFEXT_L_OnCollisionEnter() { WakeUp(); }
        public void SFEXT_G_SmallCrash()
        {
            if (InVehicle_Sounds)
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
            if (InVehicle_Sounds)
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
            if (InVehicle_Sounds)
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
            VehicleAnimator.SetBool("owner", true);
        }
        public void SFEXT_O_LoseOwnership()
        {
            IsOwner = false;
            VehicleAnimator.SetBool("owner", false);
            EnableWings(false);
        }
        public void SFEXT_G_BulletHit()
        {
            VehicleAnimator.SetFloat(HEALTH_STRING, (float)SGVControl.GetProgramVariable("Health") * FullHealthDivider);
            if (!BulletHitNull)
            {
                int rand = Random.Range(0, BulletHit.Length);
                BulletHit[rand].pitch = Random.Range(.8f, 1.2f);
                BulletHit[rand].PlayOneShot(BulletHit[rand].clip);
            }
            VehicleAnimator.SetTrigger("bullethit");
        }
        public void SFEXT_G_ReAppear()
        {
            WakeUp();
            ResetGrips();
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
            VehicleAnimator.SetInteger("missilesincoming", 0);
            if (!InEditor) { VehicleAnimator.SetBool("occupied", false); }
            if (InWater && !Explosion_WaterNull)
            {
                int rand = Random.Range(0, Explosion_Water.Length);
                if (Explosion_Water[rand])
                {
                    Explosion_Water[rand].Play();
                }
            }
            else if (!ExplosionNull)
            {
                int rand = Random.Range(0, Explosion.Length);
                if (Explosion[rand])
                {
                    Explosion[rand].Play();
                }
            }
        }
        public void SFEXT_G_Dead()
        {
            VehicleAnimator.SetBool("dead", true);
        }
        public void SFEXT_G_NotDead()
        {
            VehicleAnimator.SetBool("dead", false);
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
        public void SFEXT_L_DamageFeedback()
        {
            if (DamageFeedBack) { DamageFeedBack.PlayOneShot(DamageFeedBack.clip); }
        }
        private bool CheckingToDisable;
        public void CheckDisableLoop()
        {
            if (!CheckingToDisable) { return; }
            //don't disable unless owner is far away
            if (EntityControl.OwnerAPI != null && Vector3.Distance(EntityControl.OwnerAPI.GetPosition(), EntityControl.transform.position) > DespawnDist)
            {
                EntityControl.gameObject.SetActive(false);
                CheckingToDisable = false;
            }
            else
            {
                SendCustomEventDelayedSeconds(nameof(CheckDisableLoop), 1);
            }
        }
        private bool InVehicle_Sounds;
        public void UpdateDoorsOpen()
        {
            if (EntityControl.MySeat != -1)
            {
                InVehicle_Sounds = EntityControl.VehicleSeats[EntityControl.MySeat].numOpenDoors == 0;
            }
            if (!InVehicle_Sounds)
            {
                SetSoundsOutside();
            }
            else
            {
                if (!AllDoorsClosed) SetSoundsInside();
            }
        }
    }
}