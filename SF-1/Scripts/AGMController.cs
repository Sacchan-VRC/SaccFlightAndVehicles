
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AGMController : UdonSharpBehaviour
{
    public float LockAngle;
    public float RotSpeed = 15;
    public EngineController EngineControl;
    private Vector3 Target;
    private float Lifetime = 0;
    private bool ColliderActive = false;
    private bool Exploding = false;
    private CapsuleCollider AGMCollider;
    private void Start()
    {

        AGMCollider = gameObject.GetComponent<CapsuleCollider>();
    }
    void LateUpdate()
    {
        if (!ColliderActive)
        {
            if (Lifetime > 0.5f)
            {
                AGMCollider.enabled = true;
                ColliderActive = true;
            }
        }
        if (Vector3.Angle(gameObject.transform.forward, (Target - gameObject.transform.position)) < (LockAngle))
        {
            // homing to target, thx Guribo
            var missileToTargetVector = Target - gameObject.transform.position;
            var missileForward = gameObject.transform.forward;
            var targetDirection = missileToTargetVector.normalized;
            var rotationAxis = Vector3.Cross(missileForward, targetDirection);
            var deltaAngle = Vector3.Angle(missileForward, targetDirection);
            gameObject.transform.Rotate(rotationAxis, Mathf.Min(RotSpeed * Time.deltaTime, deltaAngle), Space.World);
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
            Lifetime = 30;
        }
    }
}
