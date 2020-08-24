
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AAMController : UdonSharpBehaviour
{
    public float LockAngle;
    public float RotSpeed = 15;
    public EngineController EngineControl;
    private bool NonOwnerLockHack = true;
    private float Lifetime = 0;
    private float StartLockAngle = 0;
    private Transform Target;
    private bool ColliderActive = false;
    private bool Exploding = false;
    private CapsuleCollider AAMCollider;
    private bool NoTarget = false;
    private bool Owner = false;
    void Start()
    {
        StartLockAngle = LockAngle;
        if (EngineControl.InEditor || EngineControl.localPlayer.IsOwner(EngineControl.gameObject))
        {
            Owner = true;
            NonOwnerLockHack = false;//don't do netcode help hack if owner
        }
        else
        {
            LockAngle = 180;//help missiles fired during a lagged turnfight actually fly towards their targets for the people who didn't fire them (for the first 2 seconds)
        }
        if (EngineControl.NumAAMTargets != 0)
        {
            Target = EngineControl.AAMTargets[EngineControl.AAMTarget].transform;
        }
        else NoTarget = true;

        AAMCollider = gameObject.GetComponent<CapsuleCollider>();
    }
    void LateUpdate()
    {
        Debug.Log(gameObject.GetComponent<Rigidbody>().velocity.magnitude);
        if (!ColliderActive)
        {
            if (Lifetime > 0.5f)
            {
                AAMCollider.enabled = true;
                ColliderActive = true;
            }
        }
        if (NonOwnerLockHack)
        {
            if (Lifetime > 2)
            {
                LockAngle = StartLockAngle;
                NonOwnerLockHack = false;
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
