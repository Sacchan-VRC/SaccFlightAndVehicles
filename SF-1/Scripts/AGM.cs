
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class AAM : UdonSharpBehaviour
{
    public float LockAngle;
    public float RotSpeed = 15;
    public EngineController EngineControl;
    private Vector3 Target;
    private float Lifetime = 0;
    private bool ColliderActive = false;
    private bool Exploding = false;
    private CapsuleCollider AGMCollider;
    void Start()
    {
        Target = EngineControl.AGMTarget;
        AGMCollider = gameObject.GetComponent<CapsuleCollider>();
    }
    void LateUpdate()
    {
        if (!ColliderActive)
        {
            if (Lifetime > 1f)
            {
                AGMCollider.enabled = true;
                ColliderActive = true;
            }
        }
        if (Lifetime > 40)
        {
            DestroyImmediate(gameObject);
        }
        Lifetime += Time.deltaTime;
        if (Vector3.Angle(gameObject.transform.forward, (Target - gameObject.transform.position).normalized) < (LockAngle))
        {
            Vector3 a = gameObject.transform.position;
            Vector3 b = gameObject.transform.position + gameObject.transform.forward;
            Vector3 c = Target;
            a = b - a;
            b = c - a;
            a = Vector3.Cross(a, b);
            Quaternion currentrot = gameObject.transform.rotation;
            gameObject.transform.LookAt(Target, a);
            currentrot = Quaternion.RotateTowards(currentrot, gameObject.transform.rotation, RotSpeed * Time.deltaTime);
            gameObject.transform.rotation = currentrot;
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
