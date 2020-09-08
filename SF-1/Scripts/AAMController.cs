
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AAMController : UdonSharpBehaviour
{
    public EngineController EngineControl;
    public float ColliderActiveDistance = 30;
    public float RotSpeed = 15;
    private EngineController TargetEngineControl;
    private bool LockHack = true;
    private float Lifetime = 0;
    private float LockAngle = 0;
    private float StartLockAngle = 0;
    private Transform Target;
    private bool ColliderActive = false;
    private bool Exploding = false;
    private CapsuleCollider AAMCollider;
    private bool Owner = false;
    private bool LockedOn = false;
    void Start()
    {
        Target = EngineControl.AAMTargets[EngineControl.AAMTarget].transform;
        LockAngle = EngineControl.AAMLockAngle;
        StartLockAngle = LockAngle;
        AAMCollider = gameObject.GetComponent<CapsuleCollider>();
        if (EngineControl.AAMTargets[EngineControl.AAMTarget].transform.parent != null)
        {
            TargetEngineControl = EngineControl.AAMTargets[EngineControl.AAMTarget].transform.parent.GetComponent<EngineController>();
            if (TargetEngineControl != null)
            {
                if (TargetEngineControl.Piloting || TargetEngineControl.Passenger)
                    TargetEngineControl.MissilesIncoming++;
                LockedOn = true;
            }
        }

        if (EngineControl.InEditor || EngineControl.IsOwner)
        {
            Owner = true;
            LockHack = false;//don't do netcode help hack if owner
        }
        else
        {
            LockAngle = 180;//help missiles fired during a lagged turnfight actually fly towards their targets for the people who didn't fire them (for the first 2 seconds)
        }
    }
    void LateUpdate()
    {
        if (!ColliderActive)
        {
            if (Vector3.Distance(gameObject.transform.position, EngineControl.CenterOfMass.position) > ColliderActiveDistance)
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
                LockAngle = StartLockAngle;
            }
        }
        if (Vector3.Angle(gameObject.transform.forward, (Target.position - gameObject.transform.position)) < LockAngle)
        {
            // homing to target, thx Guribo
            var missileToTargetVector = Target.position - gameObject.transform.position;
            var missileForward = gameObject.transform.forward;
            var targetDirection = missileToTargetVector.normalized;
            var rotationAxis = Vector3.Cross(missileForward, targetDirection);
            var deltaAngle = Vector3.Angle(missileForward, targetDirection);
            gameObject.transform.Rotate(rotationAxis, Mathf.Min(RotSpeed * Time.deltaTime, deltaAngle), Space.World);
        }
        else if (LockedOn)
        {
            if (TargetEngineControl.Piloting || TargetEngineControl.Passenger)
                TargetEngineControl.MissilesIncoming -= 1;
            LockedOn = false;
        }
        Lifetime += Time.deltaTime;
        if (Lifetime > 40)
        {
            if (Exploding)//missile exploded 10 seconds ago
            {
                Destroy(gameObject);
            }
            else Explode();//explode and give Lifetime another 10 seconds
        }
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
        if (LockedOn)
        {
            if (TargetEngineControl.Piloting || TargetEngineControl.Passenger)
                TargetEngineControl.MissilesIncoming -= 1;
            LockedOn = false;
        }
        if (TargetEngineControl != null)
        {
            //damage particles inherit the velocity of the missile, so this should help them hit the target plane
            //this is why kinematic is set 2 frameslater in the explode animation.
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
        Lifetime = 30;//10 seconds to finish exploding
    }
}
