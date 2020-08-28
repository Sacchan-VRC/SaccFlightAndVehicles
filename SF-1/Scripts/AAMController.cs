
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AAMController : UdonSharpBehaviour
{
    public float LockAngle;
    public float RotSpeed = 15;
    public EngineController EngineControl;
    private EngineController TargetEngineControl;
    private bool NonOwnerLockHack = true;
    private float Lifetime = 0;
    private float StartLockAngle = 0;
    private Transform Target;
    private bool ColliderActive = false;
    private bool Exploding = false;
    private CapsuleCollider AAMCollider;
    private bool NoTarget = false;
    private bool Owner = false;
    private bool TargetIsPlane = false;
    void Start()
    {
        Assert(EngineControl != null, "Start: EngineControl != null");


        if (EngineControl.AAMTargets[EngineControl.AAMTarget].transform.parent != null)
        {
            TargetEngineControl = EngineControl.AAMTargets[EngineControl.AAMTarget].transform.parent.GetComponent<EngineController>();
            if (TargetEngineControl != null) TargetIsPlane = true;
        }

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
            if (TargetEngineControl != null)
            {
                gameObject.GetComponent<Rigidbody>().velocity = TargetEngineControl.CurrentVel; // damage particles inherit the velocity of the missile, so this should help them hit the target plane
            }
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
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogError("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}
