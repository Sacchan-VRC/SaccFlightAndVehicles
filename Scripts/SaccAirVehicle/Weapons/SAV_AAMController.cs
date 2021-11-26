
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SAV_AAMController : UdonSharpBehaviour
{
    public UdonSharpBehaviour AAMLauncherControl;
    public SaccEntity EntityControl;
    [Tooltip("Missile will explode after this time")]
    public float MaxLifetime = 12;
    [Tooltip("How long to wait to destroy the gameobject after it has exploded, (explosion sound/animation must finish playing)")]
    public float ExplosionLifeTime = 10;
    [Tooltip("Strength of the effect of countermeasures on the missile")]
    public float FlareEffect = 10;
    [Range(0, 90f)]
    [Tooltip("If the missile and target vehicle are facing towards each other, multiply rotation speed by HighAspectRotSpeedMulti with this nose angle (facing perfectly towards each other = 0 degrees, which is the same as disabled) Set 0 for any non-heatseeker missiles")]
    public float HighAspectTrackAngle = 60;
    [Tooltip("See above")]
    public float HighAspectRotSpeedMulti = .5f;
    [Tooltip("Name of integer to +1 on the target plane while chasing it")]
    public string AnimINTName = "missilesincoming";
    [Tooltip("Play a random one of these explosion sounds")]
    public AudioSource[] ExplosionSounds;
    [Tooltip("Distance from plane to enable the missile's collider, to prevent missile from collider with own plane")]
    public float ColliderActiveDistance = 45;
    [Tooltip("Speed missile can rotate in degrees per second")]
    public float RotSpeed = 180;
    [Range(1.01f, 2f)]
    [Tooltip("Amount the target direction vector is extended when calculating missile rotation. Lower number = more aggressive drifting missile, but more likely to oscilate")]
    public float TargetVectorExtension = 1.2f;
    [Tooltip("Maxmimum extrapolation distance in seconds for target interception, to prevent distant missiles getting confused too easily")]
    public float MaximumExtrapTime = 3f;
    [Tooltip("If target vehicle has afterburner on, multiply rotation speed by this value")]
    public float AfterBurnerTrackMulti = 2f;
    [Tooltip("Missile rotates weaker if target's throttle is low, this value is the throttle at which lowering throttle more doesn't do anything")]
    public float TargetMinThrottleTrack = .3f;
    [Tooltip("When passing target, if within this range, explode")]
    public float ProximityExplodeDistance = 20;
    [Tooltip("Lockhack stops the missile from being able to lose lock before a certain amount of time has passed after it starts tracking for people who didn't fire it. It ensures the missile tracks its target in cases where the firer's position is desynced badly. Very necessary when using VRC_ObjectSync.")]
    public float LockHackTime = .1f;
    [Tooltip("How long after launch the missile should start tracking")]
    public float FlyStraightTime = .3f;
    [Tooltip("Strength of the forces applied to the sides of the missiles as it drifts through the air when it turns")]
    public float AirPhysicsStrength = .8f;
    [Tooltip("Missile predicts movement of target and tries to intercept rather than flying towards targets current position")]
    public bool PredictiveChase = true;
    [Range(0, 90f)]
    [Tooltip("Closeness in degrees to perpendicular the missile must be to be notched 0 = no notching")]
    public float NotchAngle = 0;

    [Range(-90f, 90f)]
    [Tooltip("Degrees above the missile's horizon at which notching the missile becomes impossible")]
    public float NotchHorizon = 5;
    private UdonSharpBehaviour TargetSAVControl;
    private Animator TargetAnimator;
    SaccEntity TargetEntityControl;
    private bool LockHack = true;
    private Transform Target;
    private bool ColliderActive = false;
    private bool Exploding = false;
    private CapsuleCollider AAMCollider;
    private bool MissileIncoming = false;
    private Rigidbody MissileRigid;
    private float TargDistlastframe = 999999999;
    private bool TargetLost = false;
    private float UnlockTime;
    private float TargetABPoint;
    private float TargetThrottleNormalizer;
    Vector3 TargetPosLastFrame;

    private Transform VehicleCenterOfMass;
    private bool IsOwner;
    private bool InEditor;
    private bool Initialized = false;
    private bool DirectHit = false;
    private bool SplashHit = false;
    private bool StartTrack = false;
    private float HighAspectTrack;
    private float NotchHorizonDot;
    private float NotchLimitDot;
    private ConstantForce MissileConstant;
    void Start()
    {
        //whatever script is launching the missiles must contain all of these variables
        if (EntityControl.InEditor) { IsOwner = true; }
        else
        { IsOwner = (bool)AAMLauncherControl.GetProgramVariable("IsOwner"); }
        InEditor = (bool)AAMLauncherControl.GetProgramVariable("InEditor");
        GameObject[] AAMTargets = (GameObject[])AAMLauncherControl.GetProgramVariable("AAMTargets");
        int aamtarg = (int)AAMLauncherControl.GetProgramVariable("AAMTarget");
        VehicleCenterOfMass = (Transform)AAMLauncherControl.GetProgramVariable("CenterOfMass");

        MissileConstant = GetComponent<ConstantForce>();
        MissileRigid = GetComponent<Rigidbody>();
        AAMCollider = GetComponent<CapsuleCollider>();
        Target = AAMTargets[aamtarg].transform;
        NotchHorizonDot = 1 - Mathf.Cos(NotchHorizon * Mathf.Deg2Rad);//angle as dot product
        NotchLimitDot = 1 - Mathf.Cos(NotchAngle * Mathf.Deg2Rad);
        HighAspectTrack = Mathf.Cos(HighAspectTrackAngle * Mathf.Deg2Rad);
        if (!Target)
        {
            TargetLost = true;
            Debug.LogWarning("AAM spawned without target");
        }
        else
        {
            TargDistlastframe = Vector3.Distance(transform.position, Target.position) + 1;//1 meter further so the number is different and missile knows we're already moving toward target
            TargetPosLastFrame = Target.position - Target.forward;//assume enemy plane was 1 meter behind where it is now last frame because we don't know the truth
            if (Target.parent)
            {
                TargetSAVControl = Target.parent.GetComponent<SaccAirVehicle>();
                if (TargetSAVControl)
                {
                    if ((bool)TargetSAVControl.GetProgramVariable("Piloting") || (bool)TargetSAVControl.GetProgramVariable("Passenger"))
                    {
                        TargetSAVControl.SetProgramVariable("MissilesIncomingHeat", (int)TargetSAVControl.GetProgramVariable("MissilesIncomingHeat") + 1);
                    }
                    TargetEntityControl = (SaccEntity)TargetSAVControl.GetProgramVariable("EntityControl");
                    TargetAnimator = (Animator)TargetSAVControl.GetProgramVariable("VehicleAnimator");
                    TargetAnimator.SetInteger(AnimINTName, (int)TargetSAVControl.GetProgramVariable("MissilesIncomingHeat"));
                    MissileIncoming = true;
                    TargetABPoint = (float)TargetSAVControl.GetProgramVariable("ThrottleAfterburnerPoint");
                    TargetThrottleNormalizer = 1 / TargetABPoint;
                }
            }

            if (InEditor || IsOwner || LockHackTime == 0)
            { LockHack = false; }
            else
            { SendCustomEventDelayedSeconds(nameof(DisbaleLockHack), FlyStraightTime + LockHackTime); }
        }
        Initialized = true;
        SendCustomEventDelayedSeconds(nameof(LifeTimeExplode), MaxLifetime);
        SendCustomEventDelayedSeconds(nameof(StartTracking), FlyStraightTime);
    }
    public void StartTracking()
    {
        StartTrack = true;
    }
    void FixedUpdate()
    {
        float sidespeed = Vector3.Dot(MissileRigid.velocity, transform.right);
        float downspeed = Vector3.Dot(MissileRigid.velocity, transform.up);
        float ConstantRelativeForce = MissileConstant.relativeForce.z;
        Vector3 NewConstantRelativeForce = new Vector3(-sidespeed * AirPhysicsStrength, -downspeed * AirPhysicsStrength, ConstantRelativeForce);
        MissileConstant.relativeForce = NewConstantRelativeForce;
        float DeltaTime = Time.fixedDeltaTime;
        if (!ColliderActive && Initialized)
        {
            if (Vector3.Distance(transform.position, VehicleCenterOfMass.position) > ColliderActiveDistance)
            {
                AAMCollider.enabled = true;
                ColliderActive = true;
            }
        }
        if (!TargetLost && StartTrack)
        {
            Vector3 Position = transform.position;
            Vector3 TargetPos = Target.position;
            float TargetDistance = Vector3.Distance(Position, TargetPos);
            float EngineTrack;
            float AspectTrack;
            bool Dumb;
            Vector3 Targetmovedir = (TargetPos - TargetPosLastFrame) / DeltaTime;
            TargetPosLastFrame = TargetPos;
            Vector3 MissileToTargetVector = (TargetPos - Position).normalized;
            if (TargetSAVControl)
            {
                MissileToTargetVector = (TargetPos - transform.position).normalized;
                Dumb = //Missile just flies straight if it's confused by flares or notched
                       //flare effect
                    Random.Range(0, 100) < (int)TargetSAVControl.GetProgramVariable("NumActiveFlares") * FlareEffect//if there are flares active, there's a chance it will not track per frame.
                    ||
                    //notching
                    Vector3.Dot(Vector3.up, MissileToTargetVector) < NotchHorizonDot
                    && Mathf.Abs(Vector3.Dot(Targetmovedir.normalized, MissileToTargetVector)) < NotchLimitDot;//if the target is traveling perpendicular to the direction the missile is looking at it from, it is 'notching' the missile

                AspectTrack = Vector3.Dot(MissileToTargetVector, -TargetEntityControl.transform.forward) > HighAspectTrack ? HighAspectRotSpeedMulti : 1;
                EngineTrack = Mathf.Max((float)TargetSAVControl.GetProgramVariable("EngineOutput") * TargetThrottleNormalizer, TargetMinThrottleTrack);//Track target more weakly the lower their throttle
            }
            else
            {
                EngineTrack = 1;
                AspectTrack = 1;
                Dumb = false;
            }
            if (EngineTrack > 1) { EngineTrack = AfterBurnerTrackMulti; }//if AB on, faster rotation
            if (Target.gameObject.activeInHierarchy && UnlockTime < .1f)
            {
                if (!Dumb && Vector3.Dot(MissileToTargetVector, MissileRigid.velocity) > 0 || LockHack)
                {
                    if (PredictiveChase)
                    {
                        float timetotarget = Mathf.Min(TargetDistance / Mathf.Max(((TargDistlastframe - TargetDistance) / DeltaTime), 0.001f), MaximumExtrapTime);//ensure no division by 0
                        Vector3 TargetPredictedPos = TargetPos + ((Targetmovedir * timetotarget));
                        MissileToTargetVector = TargetPredictedPos - Position;
                    }
                    //else using the already set targdirection
                    UnlockTime = 0;
                    //turn towards the target
                    Vector3 TargetDirNormalized = MissileToTargetVector.normalized * TargetVectorExtension;
                    Vector3 MissileVelNormalized = MissileRigid.velocity.normalized;
                    Vector3 MissileForward = transform.forward;
                    Vector3 targetDirection = TargetDirNormalized - MissileVelNormalized;
                    Vector3 RotationAxis = Vector3.Cross(MissileForward, targetDirection);
                    float deltaAngle = Vector3.Angle(MissileForward, targetDirection);
                    transform.Rotate(RotationAxis, Mathf.Min(RotSpeed * EngineTrack * AspectTrack * DeltaTime, deltaAngle), Space.World);
                }
                else
                {
                    if (TargetDistance < ProximityExplodeDistance)//missile flew past the target, but is within proximity explode range?
                    {
                        SplashHit = true;
                        Explode();
                    }
                    UnlockTime += DeltaTime;
                }
            }
            else
            {
                TargetLost = true;
                if (MissileIncoming)
                {
                    //just flew past the target, stop missile warning sound
                    if ((bool)TargetSAVControl.GetProgramVariable("Piloting") || (bool)TargetSAVControl.GetProgramVariable("Passenger"))
                    {
                        TargetSAVControl.SetProgramVariable("MissilesIncomingHeat", (int)TargetSAVControl.GetProgramVariable("MissilesIncomingHeat") - 1);
                        TargetAnimator.SetInteger(AnimINTName, (int)TargetSAVControl.GetProgramVariable("MissilesIncomingHeat"));
                    }
                    MissileIncoming = false;
                }
            }
            TargDistlastframe = TargetDistance;
        }
    }
    public void DisbaleLockHack()
    { LockHack = false; }
    public void LifeTimeExplode()
    { if (!Exploding) { Explode(); } }
    public void DestroySelf()
    { Destroy(gameObject); }
    private void OnCollisionEnter(Collision other)
    {
        if (!Exploding)
        {
            if (IsOwner)
            {
                DirectHit = true;
                SaccEntity TargetEntity = other.gameObject.GetComponent<SaccEntity>();
                if (TargetEntity)
                {
                    TargetEntity.SendEventToExtensions("SFEXT_L_MissileHit100");
                    //Debug.Log("DIRECTHIT");
                }
            }
            Explode();
        }
    }
    private void Explode()
    {
        if (MissileRigid)
        {
            MissileRigid.constraints = RigidbodyConstraints.FreezePosition;
            MissileRigid.velocity = Vector3.zero;
        }
        Exploding = true;
        TargetLost = true;
        if (ExplosionSounds.Length > 0)
        {
            int rand = Random.Range(0, ExplosionSounds.Length);
            ExplosionSounds[rand].pitch = Random.Range(.94f, 1.2f);
            ExplosionSounds[rand].Play();
        }
        if (MissileIncoming)
        {
            if ((bool)TargetSAVControl.GetProgramVariable("Piloting") || (bool)TargetSAVControl.GetProgramVariable("Passenger"))
            {
                TargetSAVControl.SetProgramVariable("MissilesIncomingHeat", (int)TargetSAVControl.GetProgramVariable("MissilesIncomingHeat") - 1);
            }
            TargetAnimator.SetInteger(AnimINTName, (int)TargetSAVControl.GetProgramVariable("MissilesIncomingHeat"));
            MissileIncoming = false;
        }

        AAMCollider.enabled = false;
        Animator AAMani = GetComponent<Animator>();
        float DamageDist = 999f;
        if (TargetEntityControl && (DirectHit || SplashHit))
        {
            TargetEntityControl.LastAttacker = EntityControl;
            TargetSAVControl.SetProgramVariable("LastHitTime", Time.time);
            DamageDist = Vector3.Distance(transform.position, ((Transform)TargetSAVControl.GetProgramVariable("CenterOfMass")).position) / ProximityExplodeDistance;
            if (IsOwner)
            {
                if (DamageDist < 1 && !DirectHit)
                {
                    //Debug.Log(string.Concat("TARGETDIST: ", Vector3.Distance(transform.position, ((Transform)TargetSAVControl.GetProgramVariable("CenterOfMass")).position).ToString()));
                    if (DamageDist > .66666f)
                    {
                        TargetEntityControl.SendEventToExtensions("SFEXT_L_MissileHit25");
                    }
                    else if (DamageDist > .33333f)
                    {
                        TargetEntityControl.SendEventToExtensions("SFEXT_L_MissileHit50");
                    }
                    else
                    {
                        TargetEntityControl.SendEventToExtensions("SFEXT_L_MissileHit75");
                    }
                }
            }
        }
        AAMani.SetTrigger("explode");
        SendCustomEventDelayedSeconds(nameof(DestroySelf), ExplosionLifeTime);
    }
}
