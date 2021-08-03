
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AGMController : UdonSharpBehaviour
{
    [SerializeField] private DFUNC_AGM DFUNC_AGMControl;
    private EngineController EngineControl;
    [SerializeField] private float MaxLifetime = 20;
    [SerializeField] private AudioSource[] ExplosionSounds;
    [SerializeField] private float ColliderActiveDistance = 30;
    [SerializeField] private float LockAngle;
    [SerializeField] private float RotSpeed = 15;
    private Vector3 Target;
    private float Lifetime = 0;
    private bool ColliderActive = false;
    private bool Exploding = false;
    private CapsuleCollider AGMCollider;
    private void Start()
    {
        EngineControl = DFUNC_AGMControl.EngineControl;
        Target = DFUNC_AGMControl.AGMTarget;
        AGMCollider = gameObject.GetComponent<CapsuleCollider>();
    }
    void LateUpdate()
    {
        float DeltaTime = Time.deltaTime;
        if (!ColliderActive)
        {
            if (Vector3.Distance(transform.position, EngineControl.CenterOfMass.position) > ColliderActiveDistance)
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
        Exploding = true;
        if (ExplosionSounds.Length > 0)
        {
            int rand = Random.Range(0, ExplosionSounds.Length);
            ExplosionSounds[rand].pitch = Random.Range(.94f, 1.2f);
            ExplosionSounds[rand].Play();
        }
        AGMCollider.enabled = false;
        Animator AGMani = gameObject.GetComponent<Animator>();
        if (EngineControl.InEditor)
        {
            AGMani.SetTrigger("explodeowner");
        }
        else
        {
            if (EngineControl.localPlayer.IsOwner(EngineControl.gameObject))
            {
                AGMani.SetTrigger("explodeowner");
            }
            else AGMani.SetTrigger("explode");
        }
        Lifetime = MaxLifetime - 10;
    }
}
