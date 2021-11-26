
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SAV_RocketController : UdonSharpBehaviour
{
    public UdonSharpBehaviour LauncherControl;
    [Tooltip("Bomb will explode after this time")]
    public float MaxLifetime = 15;
    [Tooltip("How long to wait to destroy the gameobject after it has exploded, (explosion sound/animation must finish playing)")]
    public float ExplosionLifeTime = 10;
    [Tooltip("Enable collider this long after missile has launched (collider is disabled to prevent hitting your own vehicle")]
    public float ColliderEnableDelay = .08f;
    [Tooltip("Play a random one of these explosion sounds")]
    public AudioSource[] ExplosionSounds;
    [Tooltip("Spawn bomb at a random angle up to this number of degrees")]
    public float AngleRandomization = 0;
    private Rigidbody BombRigid;
    private SaccEntity EntityControl;
    private bool Exploding = false;
    private CapsuleCollider RocketCollider;
    private Transform VehicleCenterOfMass;
    private bool IsOwner;

    private void Start()
    {
        EntityControl = (SaccEntity)((UdonSharpBehaviour)LauncherControl.GetProgramVariable("SAVControl")).GetProgramVariable("EntityControl");
        VehicleCenterOfMass = EntityControl.CenterOfMass;
        RocketCollider = GetComponent<CapsuleCollider>();
        BombRigid = GetComponent<Rigidbody>();
        transform.rotation = Quaternion.Euler(new Vector3(transform.rotation.eulerAngles.x + (Random.Range(0, AngleRandomization)), transform.rotation.eulerAngles.y + (Random.Range(-(AngleRandomization / 2), (AngleRandomization / 2))), transform.rotation.eulerAngles.z));
        if (EntityControl.InEditor) { IsOwner = true; }
        else
        { IsOwner = (bool)LauncherControl.GetProgramVariable("IsOwner"); }
        SendCustomEventDelayedSeconds(nameof(EnableCollider), ColliderEnableDelay);
        SendCustomEventDelayedSeconds(nameof(LifeTimeExplode), MaxLifetime);
    }
    public void EnableCollider()
    { RocketCollider.enabled = true; }
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
        RocketCollider.enabled = false;
        Animator Bombani = GetComponent<Animator>();
        if (IsOwner)
        { Bombani.SetTrigger("explodeowner"); }
        else { Bombani.SetTrigger("explode"); }
        SendCustomEventDelayedSeconds(nameof(DestroySelf), ExplosionLifeTime);
    }
}