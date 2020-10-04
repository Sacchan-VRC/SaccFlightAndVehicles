
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AGMController : UdonSharpBehaviour
{
    public EngineController EngineControl;
    public float MaxLifetime = 20;
    public AudioSource[] ExplosionSounds;
    public float ColliderActiveDistance = 30;
    public float LockAngle;
    public float RotSpeed = 15;
    private Vector3 Target;
    private float Lifetime = 0;
    private bool ColliderActive = false;
    private bool Exploding = false;
    private CapsuleCollider AGMCollider;
    private void Start()
    {
        Target = EngineControl.AGMTarget;
        AGMCollider = gameObject.GetComponent<CapsuleCollider>();
    }
    void LateUpdate()
    {
        if (!ColliderActive)
        {
            if (Vector3.Distance(gameObject.transform.position, EngineControl.CenterOfMass.position) > ColliderActiveDistance)
            {
                AGMCollider.enabled = true;
                ColliderActive = true;
            }
        }
        if (Vector3.Angle(gameObject.transform.forward, (Target - gameObject.transform.position)) < (LockAngle))
        {
            var missileToTargetVector = Target - gameObject.transform.position;
            var missileForward = gameObject.transform.forward;
            var targetDirection = missileToTargetVector.normalized;
            var rotationAxis = Vector3.Cross(missileForward, targetDirection);
            var deltaAngle = Vector3.Angle(missileForward, targetDirection);
            gameObject.transform.Rotate(rotationAxis, Mathf.Min(RotSpeed * Time.deltaTime, deltaAngle), Space.World);
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
