
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SAV_AAMController : UdonSharpBehaviour
{
    [SerializeField] private UdonSharpBehaviour AAMLauncherControl;
    public SaccEntity EntityControl;
    [SerializeField] private float MaxLifetime = 12;
    [Tooltip("Strength of the effect of countermeasures on the missile")]
    [SerializeField] private float FlareEffect = 1;
    [SerializeField] private AudioSource[] ExplosionSounds;
    [SerializeField] private float ColliderActiveDistance = 45;
    [SerializeField] private float RotSpeed = 400;
    [Tooltip("Missile tracks weaker if target's throttle is low, TargetLowThrottleTrack is throttle at which lowering throttle more doesn't do anything")]
    [SerializeField] private float TargetLowThrottleTrack = .3f;
    [SerializeField] private float ProximityExplodeDistance = 20;
    private SaccAirVehicle TargetEngineControl;
    private bool LockHack = true;
    private float Lifetime = 0;
    private Transform Target;
    private bool ColliderActive = false;
    private bool Exploding = false;
    private CapsuleCollider AAMCollider;
    private bool Owner = false;
    private bool TargetIsVehicle = false;
    private bool MissileIncoming = false;
    private Rigidbody MissileRigid;
    private float TargDistlastframe = 999999999;
    private bool TargetLost = false;
    private float UnlockTime;
    private float TargetABPoint;
    private float TargetThrottleNormalizer;
    private bool TargetEngineNULL = true;
    private bool EngineControlNull;
    Vector3 TargetPosLastFrame;

    private Transform VehicleCenterOfMass;
    private bool IsOwner;
    private bool InEditor;
    private bool Initialized = false;

    //public Transform testobj;
    void Start()
    {
        //whatever script is launching the missiles must contain all of these variables
        IsOwner = (bool)AAMLauncherControl.GetProgramVariable("IsOwner");
        InEditor = (bool)AAMLauncherControl.GetProgramVariable("InEditor");
        GameObject[] AAMTargets = (GameObject[])AAMLauncherControl.GetProgramVariable("AAMTargets");
        int aamtarg = (int)AAMLauncherControl.GetProgramVariable("AAMTarget");
        VehicleCenterOfMass = (Transform)AAMLauncherControl.GetProgramVariable("CenterOfMass");


        MissileRigid = GetComponent<Rigidbody>();
        AAMCollider = GetComponent<CapsuleCollider>();
        Target = AAMTargets[aamtarg].transform;
        if (Target == null)
        {
            TargetLost = true;
            Debug.LogWarning("AAM spawned without target");
        }
        else
        {
            TargDistlastframe = Vector3.Distance(transform.position, Target.position) + 1;//1 meter further so the number is different and missile knows we're already moving toward target
            TargetPosLastFrame = Target.position - Target.forward;//assume enemy plane was 1 meter behind where it is now last frame because we don't know the truth
            if (Target.parent != null)
            {
                TargetEngineControl = Target.parent.GetComponent<SaccAirVehicle>();
                if (TargetEngineControl != null)
                {
                    if (TargetEngineControl.Piloting || TargetEngineControl.Passenger)
                    { TargetEngineControl.MissilesIncomingHeat++; }

                    TargetEngineNULL = false;
                    MissileIncoming = true;
                    TargetIsVehicle = true;
                    TargetABPoint = TargetEngineControl.ThrottleAfterburnerPoint;
                    TargetThrottleNormalizer = 1 / TargetABPoint;
                }
            }

            if (InEditor || IsOwner)
            {
                Owner = true;
                LockHack = false;
            }
        }
        Initialized = true;
    }
    void FixedUpdate()
    {
        float DeltaTime = Time.fixedDeltaTime;
        //Debug.Log(GetComponent<Rigidbody>().velocity.magnitude);
        if (!ColliderActive && Initialized)
        {
            if (Vector3.Distance(transform.position, VehicleCenterOfMass.position) > ColliderActiveDistance)
            {
                AAMCollider.enabled = true;
                ColliderActive = true;
            }
        }
        if (LockHack)
        {
            if (Lifetime > .6f)
            {
                LockHack = false;
            }
        }


        if (Lifetime > MaxLifetime)
        {
            if (Exploding)//missile exploded 10 seconds ago
            {
                Destroy(gameObject);
            }
            else Explode();//explode and give Lifetime another 10 seconds
        }

        if (!TargetLost)
        {
            Vector3 Position = transform.position;
            Vector3 TargetPos = Target.position;
            float TargetDistance = Vector3.Distance(Position, TargetPos);
            float EngineTrack;
            bool Dumb;
            if (!TargetEngineNULL)
            {
                Dumb = Random.Range(0, 100) < TargetEngineControl.NumActiveFlares * FlareEffect;//if there are flares active, there's a chance it will not track per frame.
                EngineTrack = Mathf.Max(TargetEngineControl.EngineOutput * TargetThrottleNormalizer, TargetLowThrottleTrack);//Track target more weakly the lower their throttle
            }
            else
            {
                EngineTrack = 1;
                Dumb = false;
            }
            if (EngineTrack > 1) { EngineTrack = 2; }//if AB on track 2x as well
            if (Target.gameObject.activeInHierarchy && UnlockTime < .1f)
            {
                if ((!Dumb && TargetDistance < TargDistlastframe) || LockHack)
                {
                    UnlockTime = 0;
                    //turn towards the target
                    Vector3 missileToTargetVector = TargetPos - Position;
                    var missileForward = transform.forward;
                    var targetDirection = missileToTargetVector.normalized;
                    var rotationAxis = Vector3.Cross(missileForward, targetDirection);
                    var deltaAngle = Vector3.Angle(missileForward, targetDirection);
                    transform.Rotate(rotationAxis, Mathf.Min(RotSpeed * EngineTrack * DeltaTime, deltaAngle), Space.World);
                }
                else
                {
                    if (TargetDistance < ProximityExplodeDistance)//missile flew past the target, but is within proximity explode range?
                    {
                        Explode();
                    }
                    UnlockTime += Time.deltaTime;
                }
            }
            else
            {
                TargetLost = true;
                if (MissileIncoming)
                {
                    //just flew past the target, stop missile warning sound
                    if (TargetEngineControl.Piloting || TargetEngineControl.Passenger)
                    { TargetEngineControl.MissilesIncomingHeat -= 1; }
                    MissileIncoming = false;
                }
            }
            TargDistlastframe = TargetDistance;
        }
        Lifetime += DeltaTime;
    }
    private void OnCollisionEnter(Collision other)
    {
        if (!Exploding)
        {
            Explode();
        }
    }
    private void Explode()
    {
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
            if (TargetEngineControl.Piloting || TargetEngineControl.Passenger)
            { TargetEngineControl.MissilesIncomingHeat -= 1; }
            MissileIncoming = false;
        }
        if (TargetEngineControl != null)
        {
            //damage particles inherit the velocity of the missile, so this should help them hit the target plane
            //this is why kinematic is set 2 frames later in the explode animation.
            MissileRigid.velocity = TargetEngineControl.CurrentVel;
        }
        else
        {
            MissileRigid.velocity = Vector3.zero;
        }

        //would rather do it like this but udon wont let me
        /*             if (TargetEngineControl != null)
                    {
                        //damage particles take the velocity of the target plane so they can hit it any speed
                        var vel = DamageParticles.velocityOverLifetime;
                        vel.enabled = true;
                        vel.space = ParticleSystemSimulationSpace.World;

                        AnimationCurve velcurvex = new AnimationCurve();
                        AnimationCurve velcurvey = new AnimationCurve();
                        AnimationCurve velcurvez = new AnimationCurve();
                        velcurvex.AddKey(0.0f, TargetEngineControl.CurrentVel.x);
                        velcurvey.AddKey(0.0f, TargetEngineControl.CurrentVel.y);
                        velcurvez.AddKey(0.0f, TargetEngineControl.CurrentVel.z);
                        vel.x = new ParticleSystem.MinMaxCurve(1.0f, velcurvex);
                        vel.x = new ParticleSystem.MinMaxCurve(1.0f, velcurvey);
                        vel.x = new ParticleSystem.MinMaxCurve(1.0f, velcurvez);
                    } */
        AAMCollider.enabled = false;
        Animator AGMani = GetComponent<Animator>();
        if (InEditor)
        {
            AGMani.SetTrigger("explodeowner");
        }
        else
        {
            if (Owner)
            {
                AGMani.SetTrigger("explodeowner");
            }
            else AGMani.SetTrigger("explode");
        }
        Lifetime = MaxLifetime - 10;//10 seconds to finish exploding
    }
}
