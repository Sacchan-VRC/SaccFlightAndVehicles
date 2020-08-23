
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AAM : UdonSharpBehaviour
{
    public float LockAngle;
    public float RotSpeed = 15;
    public EngineController EngineControl;
    private float Lifetime = 0;
    private Transform Target;
    private bool ColliderActive = false;
    private bool Exploding = false;
    private CapsuleCollider AAMCollider;
    private bool NoTarget = false;
    private bool Owner = false;
    void Start()
    {
        if (EngineControl.localPlayer.IsOwner(EngineControl.gameObject))
        {
            Owner = true;
        }
        else if (!EngineControl.Taxiing)
        {
            LockAngle = 70;//help missiles fired during a lagged turnfight actually fly towards their targets for the people who didn't fire them
        }
        if (EngineControl.NumTargets != 0)
        {
            Target = EngineControl.Targets[EngineControl.AAMTarget].transform;
        }
        else NoTarget = true;

        AAMCollider = gameObject.GetComponent<CapsuleCollider>();
    }
    void LateUpdate()
    {
        if (!ColliderActive)
        {
            if (Lifetime > 0.5f)
            {
                AAMCollider.enabled = true;
                ColliderActive = true;
            }
        }
        if (!NoTarget)
        {
            if (Vector3.Angle(gameObject.transform.forward, (Target.position - gameObject.transform.position)) < (LockAngle))
            {
                // homing to target, thx Guribo
                var missileToTargetVector = Target.position - gameObject.transform.position;
                var missileForward = gameObject.transform.forward;
                var targetDirection = missileToTargetVector.normalized;
                var rotationAxis = Vector3.Cross(missileForward, targetDirection);
                var deltaAngle = Vector3.Angle(missileForward, targetDirection);
                gameObject.transform.Rotate(rotationAxis, Mathf.Min(RotSpeed * Time.deltaTime, deltaAngle), Space.World);
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
            AAMCollider.enabled = false;
            Animator AGMani = gameObject.GetComponent<Animator>();
            if (EngineControl.InEditor)
            {
                AGMani.SetTrigger("explodeowner");
            }
            else
            {
                if (Owner)
                {
                    AGMani.SetTrigger("explodeowner");
                }
                else AGMani.SetTrigger("explode");
            }
            Lifetime = 30;//10 seconds to finish exploding
        }
    }
}
