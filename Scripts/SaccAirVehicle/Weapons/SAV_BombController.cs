
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SAV_BombController : UdonSharpBehaviour
{
    [SerializeField] private UdonSharpBehaviour BombLauncherControl;
    [Tooltip("Bomb will explode after this time")]
    [SerializeField] private float MaxLifetime = 40;
    [Tooltip("How long to wait to destroy the gameobject after it has exploded, (explosion sound/animation must finish playing)")]
    [SerializeField] private float ExplosionLifeTime = 10;
    [Tooltip("Play a random one of these explosion sounds")]
    [SerializeField] private AudioSource[] ExplosionSounds;
    [SerializeField] private float LaunchSpeed = 0;
    [Tooltip("Spawn bomb at a random angle up to this number")]
    [SerializeField] private float AngleRandomization = 1;
    [Tooltip("Distance from plane to enable the missile's collider, to prevent bomb from colliding with own plane")]
    [SerializeField] private float ColliderActiveDistance = 30;
    [Tooltip("How much the bomb's nose is pushed towards direction of movement")]
    [SerializeField] private float StraightenFactor = .1f;
    [Tooltip("Amount of drag bomb has when moving horizontally/vertically")]
    [SerializeField] private float AirPhysicsStrength = .1f;
    [Tooltip("Used for making rockets, should probably disable air physics and angle randomization when making rockets.")]
    private SaccEntity EntityControl;
    private ConstantForce BombConstant;
    private Rigidbody BombRigid;
    private bool Exploding = false;
    private bool ColliderActive = false;
    private CapsuleCollider BombCollider;
    private Transform VehicleCenterOfMass;
    private bool IsOwner;

    private void Start()
    {
        BombCollider = GetComponent<CapsuleCollider>();
        BombRigid = GetComponent<Rigidbody>();
        BombConstant = GetComponent<ConstantForce>();
        EntityControl = (SaccEntity)((UdonSharpBehaviour)BombLauncherControl.GetProgramVariable("SAVControl")).GetProgramVariable("EntityControl");
        VehicleCenterOfMass = EntityControl.CenterOfMass;
        transform.rotation = Quaternion.Euler(new Vector3(transform.rotation.eulerAngles.x + (Random.Range(0, AngleRandomization)), transform.rotation.eulerAngles.y + (Random.Range(-(AngleRandomization / 2), (AngleRandomization / 2))), transform.rotation.eulerAngles.z));
        if (EntityControl.InEditor) { IsOwner = true; }
        else
        { IsOwner = (bool)BombLauncherControl.GetProgramVariable("IsOwner"); }
        SendCustomEventDelayedSeconds(nameof(LifeTimeExplode), MaxLifetime);
        BombRigid.velocity += transform.forward * LaunchSpeed;
    }

    void LateUpdate()
    {
        if (!ColliderActive)
        {
            if (Vector3.Distance(transform.position, VehicleCenterOfMass.position) > ColliderActiveDistance)
            {
                BombCollider.enabled = true;
                ColliderActive = true;
            }
        }
        float sidespeed = Vector3.Dot(BombRigid.velocity, transform.right);
        float downspeed = Vector3.Dot(BombRigid.velocity, transform.up);
        BombConstant.relativeTorque = new Vector3(-downspeed, sidespeed, 0) * StraightenFactor;
        BombConstant.relativeForce = new Vector3(-sidespeed * AirPhysicsStrength, -downspeed * AirPhysicsStrength, 0);
    }
    public void LifeTimeExplode()
    { if (!Exploding) { Explode(); } }
    public void DestroySelf()
    { Destroy(gameObject); }
    private void OnCollisionEnter(Collision other)
    {
        if (!Exploding)
        {
            Explode();
        }
    }
    private void Explode()
    {
        if (BombRigid)
        {
            BombRigid.constraints = RigidbodyConstraints.FreezePosition;
            BombRigid.velocity = Vector3.zero;
        }
        Exploding = true;
        if (ExplosionSounds.Length > 0)
        {
            int rand = Random.Range(0, ExplosionSounds.Length);
            ExplosionSounds[rand].pitch = Random.Range(.94f, 1.2f);
            ExplosionSounds[rand].Play();
        }
        BombCollider.enabled = false;
        Animator Bombani = GetComponent<Animator>();
        if (IsOwner)
        { Bombani.SetTrigger("explodeowner"); }
        else { Bombani.SetTrigger("explode"); }
        SendCustomEventDelayedSeconds(nameof(DestroySelf), ExplosionLifeTime);
    }
}