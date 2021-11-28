
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SAV_AGMController : UdonSharpBehaviour
{
    public UdonSharpBehaviour AGMLauncherControl;
    public SaccEntity EntityControl;
    [Tooltip("Missile will explode after this time")]
    public float MaxLifetime = 35;
    [Tooltip("How long to wait to destroy the gameobject after it has exploded, (explosion sound/animation must finish playing)")]
    public float ExplosionLifeTime = 10;
    [Tooltip("AGM will fly straight for this many seconds before it starts homing in on target")]
    public float FlyStraightTime = .3f;
    [Tooltip("Play a random one of these explosion sounds")]
    public AudioSource[] ExplosionSounds;
    [Tooltip("Distance from plane to enable the missile's collider, to prevent missile from colliding with own plane")]
    public float ColliderActiveDistance = 30;
    [Tooltip("Max angle able to track target at")]
    public float LockAngle = 90;
    [Tooltip("Maximum speed missile can rotate")]
    public float RotSpeed = 15;
    [Range(1.01f, 2f)]
    [Tooltip("Amount the target direction vector is extended when calculating missile rotation. Lower number = more aggressive drifting missile, but more likely to oscilate")]
    public float TargetVectorExtension = 1.2f;
    [Tooltip("Strength of the forces applied to the sides of the missiles as it drifts through the air when it turns")]
    public float AirPhysicsStrength = 3f;
    private bool StartTrack = false;
    private Transform VehicleCenterOfMass;
    private Transform TargetTransform;
    private Vector3 TargetOffset;
    private bool ColliderActive = false;
    private bool Exploding = false;
    private bool IsOwner = false;
    private CapsuleCollider AGMCollider;
    private Rigidbody AGMRigid;
    private ConstantForce MissileConstant;
    private void Start()
    {
        VehicleCenterOfMass = EntityControl.CenterOfMass;
        TargetTransform = (Transform)AGMLauncherControl.GetProgramVariable("TrackedTransform");
        TargetOffset = (Vector3)AGMLauncherControl.GetProgramVariable("TrackedObjectOffset");
        AGMCollider = gameObject.GetComponent<CapsuleCollider>();
        AGMRigid = gameObject.GetComponent<Rigidbody>();
        MissileConstant = GetComponent<ConstantForce>();

        if (EntityControl.InEditor) { IsOwner = true; }
        else
        { IsOwner = (bool)AGMLauncherControl.GetProgramVariable("IsOwner"); }
        SendCustomEventDelayedSeconds(nameof(LifeTimeExplode), MaxLifetime);
        SendCustomEventDelayedSeconds(nameof(StartTracking), FlyStraightTime);
    }
    void LateUpdate()
    {
        float sidespeed = Vector3.Dot(AGMRigid.velocity, transform.right);
        float downspeed = Vector3.Dot(AGMRigid.velocity, transform.up);
        float ConstantRelativeForce = MissileConstant.relativeForce.z;
        Vector3 NewConstantRelativeForce = new Vector3(-sidespeed * AirPhysicsStrength, -downspeed * AirPhysicsStrength, ConstantRelativeForce);
        Vector3 missileToTargetVector = TargetTransform.TransformPoint(TargetOffset) - transform.position;
        MissileConstant.relativeForce = NewConstantRelativeForce;
        float DeltaTime = Time.deltaTime;
        if (!ColliderActive)
        {
            if (Vector3.Distance(transform.position, VehicleCenterOfMass.position) > ColliderActiveDistance)
            {
                AGMCollider.enabled = true;
                ColliderActive = true;
            }
        }
        if (StartTrack && Vector3.Angle(transform.forward, missileToTargetVector) < LockAngle)
        {
            Vector3 TargetDirNormalized = missileToTargetVector.normalized * TargetVectorExtension;
            Vector3 MissileVelNormalized = AGMRigid.velocity.normalized;
            Vector3 MissileForward = transform.forward;
            Vector3 targetDirection = TargetDirNormalized - MissileVelNormalized;
            Vector3 RotationAxis = Vector3.Cross(MissileForward, targetDirection);
            float deltaAngle = Vector3.Angle(MissileForward, targetDirection);
            transform.Rotate(RotationAxis, Mathf.Min(RotSpeed * DeltaTime, deltaAngle), Space.World);
        }
    }
    public void StartTracking()
    { StartTrack = true; }
    public void LifeTimeExplode()
    { if (!Exploding) { Explode(); } }
    public void DestroySelf()
    { Destroy(gameObject); }
    private void OnCollisionEnter(Collision other)
    { if (!Exploding) { Explode(); } }
    private void OnTriggerEnter(Collider other)
    {
        if (other && other.gameObject.layer == 4 /* water */)
        { if (!Exploding) { Explode(); } }
    }
    private void Explode()
    {
        if (AGMRigid)
        { AGMRigid.constraints = RigidbodyConstraints.FreezePosition; }
        Exploding = true;
        if (ExplosionSounds.Length > 0)
        {
            int rand = Random.Range(0, ExplosionSounds.Length);
            ExplosionSounds[rand].pitch = Random.Range(.94f, 1.2f);
            ExplosionSounds[rand].Play();
        }
        AGMCollider.enabled = false;
        Animator AGMani = gameObject.GetComponent<Animator>();
        if (IsOwner)
        { AGMani.SetTrigger("explodeowner"); }
        else { AGMani.SetTrigger("explode"); }
        SendCustomEventDelayedSeconds(nameof(DestroySelf), ExplosionLifeTime);
    }
}
