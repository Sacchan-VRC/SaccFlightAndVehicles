
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AAMController : UdonSharpBehaviour
{
    public EngineController EngineControl;
    public float MaxLifetime = 12;
    public AudioSource[] ExplosionSounds;
    public float ColliderActiveDistance = 30;
    public float RotSpeed = 130;
    public float MissileDriftCompensation = 65f;
    private EngineController TargetEngineControl;
    private bool LockHack = true;
    private float Lifetime = 0;
    private Transform Target;
    private bool ColliderActive = false;
    private bool Exploding = false;
    private CapsuleCollider AAMCollider;
    private bool Owner = false;
    private bool TargetIsPlane = false;
    private bool LockedOn = false;
    private Rigidbody MissileRigid;
    private float TargDistlastframe = 999999999;
    private bool TargetLost = false;
    Vector3 TargetPosLastFrame;
    //public Transform testobj;
    void Start()
    {
        MissileRigid = gameObject.GetComponent<Rigidbody>();
        AAMCollider = gameObject.GetComponent<CapsuleCollider>();
        Target = EngineControl.AAMTargets[EngineControl.AAMTarget].transform;
        TargDistlastframe = Vector3.Distance(transform.position, Target.position) + 1;//1 meter further so the number is different and missile knows we're already moving toward target
        TargetPosLastFrame = Target.position - Target.forward;//assume enemy plane was 1 meter behind where it is now last frame because we don't know the truth
        if (EngineControl.AAMTargets[EngineControl.AAMTarget].transform.parent != null)
        {
            TargetEngineControl = EngineControl.AAMTargets[EngineControl.AAMTarget].transform.parent.GetComponent<EngineController>();
            if (TargetEngineControl != null)
            {
                if (TargetEngineControl.Piloting || TargetEngineControl.Passenger)
                { TargetEngineControl.MissilesIncoming++; }

                LockedOn = true;
                TargetIsPlane = true;
            }
        }

        if (EngineControl.InEditor || EngineControl.IsOwner)
        {
            Owner = true;
            LockHack = false;
        }
    }
    void LateUpdate()
    {
        //Debug.Log(gameObject.GetComponent<Rigidbody>().velocity.magnitude);
        if (!ColliderActive)
        {
            if (Vector3.Distance(transform.position, EngineControl.CenterOfMass.position) > ColliderActiveDistance)
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
    }

    private void FixedUpdate()
    {
        float DeltaTime = Time.deltaTime;
        if (!TargetLost)
        {
            if (!Target.gameObject.activeInHierarchy) { TargetLost = true; }

            float TargetDistance;
            Vector3 Position = transform.position;
            Vector3 TargetPos = Target.position;
            TargetDistance = Vector3.Distance(Position, TargetPos);
            if (TargetIsPlane)
            {
                if (TargetDistance < TargDistlastframe || LockHack)
                {
                    //turn towards the target
                    Vector3 missileToTargetVector;
                    if (TargetDistance < 700)
                    {
                        Vector3 Targetmovedir = TargetPos - TargetPosLastFrame;
                        float timetotarget = TargetDistance / Mathf.Max(((TargDistlastframe - TargetDistance) / DeltaTime), 0.001f);//ensure no division by 0
                        Vector3 TargetPredictedPos = TargetPos + ((Targetmovedir * timetotarget) + (Targetmovedir.normalized * (TargetEngineControl.Speed / MissileDriftCompensation) * timetotarget) / DeltaTime);
                        missileToTargetVector = TargetPredictedPos - Position;
                        TargetPosLastFrame = TargetPos;
                    }
                    else
                    {
                        missileToTargetVector = TargetPos - Position;
                    }
                    var missileForward = transform.forward;
                    var targetDirection = missileToTargetVector.normalized;
                    var rotationAxis = Vector3.Cross(missileForward, targetDirection);
                    var deltaAngle = Vector3.Angle(missileForward, targetDirection);
                    transform.Rotate(rotationAxis, Mathf.Min(RotSpeed * DeltaTime, deltaAngle), Space.World);
                }
                else if (LockedOn)
                {
                    //just flew past the target, unlock
                    if (TargetEngineControl.Piloting || TargetEngineControl.Passenger)
                        TargetEngineControl.MissilesIncoming -= 1;
                    LockedOn = false;
                    if (Lifetime > 1) { TargetLost = true; }
                }
            }
            else //target is not a plane
            {
                if (TargetDistance < TargDistlastframe || LockHack)
                {
                    //turn towards the target
                    Vector3 missileToTargetVector = TargetPos - Position;
                    var missileForward = transform.forward;
                    var targetDirection = missileToTargetVector.normalized;
                    var rotationAxis = Vector3.Cross(missileForward, targetDirection);
                    var deltaAngle = Vector3.Angle(missileForward, targetDirection);
                    transform.Rotate(rotationAxis, Mathf.Min(RotSpeed * DeltaTime, deltaAngle), Space.World);
                }
                //just flew past the target, unlock
                else if (Lifetime > 1) { TargetLost = true; }
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
        if (ExplosionSounds.Length > 0)
        {
            int rand = Random.Range(0, ExplosionSounds.Length);
            ExplosionSounds[rand].pitch = Random.Range(.94f, 1.2f);
            ExplosionSounds[rand].Play();
        }
        if (LockedOn)
        {
            if (TargetEngineControl.Piloting || TargetEngineControl.Passenger)
                TargetEngineControl.MissilesIncoming -= 1;
            LockedOn = false;
        }
        if (TargetEngineControl != null)
        {
            //damage particles inherit the velocity of the missile, so this should help them hit the target plane
            //this is why kinematic is set 2 frames later in the explode animation.
            gameObject.GetComponent<Rigidbody>().velocity = TargetEngineControl.CurrentVel;
        }
        else
        {
            gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
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
        Animator AGMani = gameObject.GetComponent<Animator>();
        if (EngineControl.InEditor)
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
