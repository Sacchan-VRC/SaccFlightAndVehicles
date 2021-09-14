
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class SAV_AGMController : UdonSharpBehaviour
{
    [SerializeField] private UdonSharpBehaviour AGMLauncherControl;
    public SaccEntity EntityControl;
    [SerializeField] private float MaxLifetime = 20;
    [SerializeField] private AudioSource[] ExplosionSounds;
    [SerializeField] private float ColliderActiveDistance = 30;
    [SerializeField] private float LockAngle;
    [SerializeField] private float RotSpeed = 15;
    private Transform CenterOfMass;
    private Vector3 Target;
    private float Lifetime = 0;
    private bool ColliderActive = false;
    private bool Exploding = false;
    private bool IsOwner = false;
    private CapsuleCollider AGMCollider;
    private Rigidbody AGMRigid;
    private void Start()
    {
        CenterOfMass = EntityControl.CenterOfMass;
        Target = (Vector3)AGMLauncherControl.GetProgramVariable("AGMTarget");
        AGMCollider = gameObject.GetComponent<CapsuleCollider>();
        AGMRigid = gameObject.GetComponent<Rigidbody>();

        if (EntityControl.InEditor) { IsOwner = true; }
        else
        { IsOwner = (bool)AGMLauncherControl.GetProgramVariable("IsOwner"); }
    }
    void LateUpdate()
    {
        float DeltaTime = Time.deltaTime;
        if (!ColliderActive)
        {
            if (Vector3.Distance(transform.position, CenterOfMass.position) > ColliderActiveDistance)
            {
                AGMCollider.enabled = true;
                ColliderActive = true;
            }
        }
        if (Vector3.Angle(transform.forward, (Target - transform.position)) < (LockAngle))
        {
            var missileToTargetVector = Target - transform.position;
            var missileForward = transform.forward;
            var targetDirection = missileToTargetVector.normalized;
            var rotationAxis = Vector3.Cross(missileForward, targetDirection);
            var deltaAngle = Vector3.Angle(missileForward, targetDirection);
            transform.Rotate(rotationAxis, Mathf.Min(RotSpeed * DeltaTime, deltaAngle), Space.World);
        }
        Lifetime += DeltaTime;
        if (Lifetime > MaxLifetime)
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
        if (AGMRigid != null)
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
        Lifetime = MaxLifetime - 10;//10 seconds to finish exploding
    }
}
