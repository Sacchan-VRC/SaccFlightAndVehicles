
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class BombController : UdonSharpBehaviour
{
    public EngineController EngineControl;
    public float MaxLifetime = 40;
    public AudioSource[] ExplosionSounds;
    public float AngleRandomization = 1;
    public float ColliderActiveDistance = 30;
    public float StraightenFactor = .1f;
    public float AirPhysicsStrength = .1f;
    private ConstantForce BombConstant;
    private Rigidbody BombRigid;
    private bool Exploding = false;
    private bool ColliderActive = false;
    private float Lifetime = 0;
    private CapsuleCollider BombCollider;
    private void Start()
    {
        BombCollider = GetComponent<CapsuleCollider>();
        BombRigid = GetComponent<Rigidbody>();
        BombConstant = GetComponent<ConstantForce>();
        transform.rotation = Quaternion.Euler(new Vector3(transform.rotation.x + (Random.Range(0, AngleRandomization)), transform.rotation.eulerAngles.y + (Random.Range(-(AngleRandomization / 2), (AngleRandomization / 2))), transform.rotation.eulerAngles.z));
    }

    void LateUpdate()
    {
        float sidespeed = Vector3.Dot(BombRigid.velocity, transform.right);
        float downspeed = Vector3.Dot(BombRigid.velocity, transform.up);
        BombConstant.relativeTorque = new Vector3(-downspeed, sidespeed, 0) * StraightenFactor;
        BombConstant.relativeForce = new Vector3(-sidespeed, -downspeed, 0);
        if (!ColliderActive)
        {
            if (Vector3.Distance(transform.position, EngineControl.CenterOfMass.position) > ColliderActiveDistance)
            {
                BombCollider.enabled = true;
                ColliderActive = true;
            }
        }
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
        if (EngineControl.InEditor)
        {
            Bombani.SetTrigger("explodeowner");
        }
        else
        {
            if (EngineControl.localPlayer.IsOwner(EngineControl.gameObject))
            {
                Bombani.SetTrigger("explodeowner");
            }
            else Bombani.SetTrigger("explode");
        }
        Lifetime = MaxLifetime - 10;
    }
}