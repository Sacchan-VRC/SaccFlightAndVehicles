
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class SAV_BombController : UdonSharpBehaviour
{
    [SerializeField] private UdonSharpBehaviour BombLauncherControl;
    public SaccEntity EntityControl;
    [SerializeField] private float MaxLifetime = 40;
    [SerializeField] private AudioSource[] ExplosionSounds;
    [SerializeField] private bool isRocket;
    [SerializeField] private float AngleRandomization = 1;
    [SerializeField] private float ColliderActiveDistance = 30;
    [SerializeField] private float StraightenFactor = .1f;
    [SerializeField] private float AirPhysicsStrength = .1f;
    [SerializeField] private float ForwardThrust = 0f;
    private ConstantForce BombConstant;
    private Rigidbody BombRigid;
    private bool Exploding = false;
    private bool ColliderActive = false;
    private float Lifetime = 0;
    private CapsuleCollider BombCollider;
    private Transform CenterOfMass;
    private bool IsOwner;

    private void Start()
    {
        CenterOfMass = EntityControl.CenterOfMass;
        BombCollider = GetComponent<CapsuleCollider>();
        BombRigid = GetComponent<Rigidbody>();
        BombConstant = GetComponent<ConstantForce>();
        transform.rotation = Quaternion.Euler(new Vector3(transform.rotation.eulerAngles.x + (Random.Range(0, AngleRandomization)), transform.rotation.eulerAngles.y + (Random.Range(-(AngleRandomization / 2), (AngleRandomization / 2))), transform.rotation.eulerAngles.z));
        if (EntityControl.InEditor) { IsOwner = true; }
        else
        { IsOwner = (bool)BombLauncherControl.GetProgramVariable("IsOwner"); }
    }

    void LateUpdate()
    {
        if (!ColliderActive)
        {
            if (Vector3.Distance(transform.position, CenterOfMass.position) > ColliderActiveDistance)
            {
                BombCollider.enabled = true;
                ColliderActive = true;
            }
        }
        float sidespeed = Vector3.Dot(BombRigid.velocity, transform.right);
        float downspeed = Vector3.Dot(BombRigid.velocity, transform.up);
        BombConstant.relativeTorque = new Vector3(-downspeed, sidespeed, 0) * StraightenFactor;
        BombConstant.relativeForce = new Vector3(-sidespeed, -downspeed, ForwardThrust);
        Lifetime += Time.deltaTime;
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
        Lifetime = MaxLifetime - 10;//10 seconds to finish exploding
    }
}