
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class BombController : UdonSharpBehaviour
{
    public EngineController EngineControl;
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
        BombCollider = gameObject.GetComponent<CapsuleCollider>();
        BombRigid = gameObject.GetComponent<Rigidbody>();
        BombConstant = gameObject.GetComponent<ConstantForce>();
    }

    void LateUpdate()
    {
        float sidespeed = Vector3.Dot(BombRigid.velocity, gameObject.transform.right);
        float downspeed = Vector3.Dot(BombRigid.velocity, gameObject.transform.up);
        BombConstant.relativeTorque = new Vector3(-downspeed, sidespeed, 0) * StraightenFactor;
        BombConstant.relativeForce = new Vector3(-sidespeed, -downspeed, 0);
        if (!ColliderActive)
        {
            if (Vector3.Distance(gameObject.transform.position, EngineControl.CenterOfMass.position) > ColliderActiveDistance)
            {
                BombCollider.enabled = true;
                ColliderActive = true;
            }
        }
        Lifetime += Time.deltaTime;
        if (Lifetime > 40)
        {
            Destroy(gameObject);
        }
    }
    private void OnCollisionEnter(Collision other)
    {
        if (!Exploding)
        {
            BombCollider.enabled = false;
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
            Lifetime = 30;
        }
    }
}
