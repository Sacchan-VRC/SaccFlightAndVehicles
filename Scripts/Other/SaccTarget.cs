
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class SaccTarget : UdonSharpBehaviour
{
    public float HitPoints = 30f;
    [Tooltip("Other UdonBehaviours that will recieve the event 'Explode'")]
    public UdonSharpBehaviour[] ExplodeOther;
    private Animator TargetAnimator;
    private float FullHealth;
    private VRCPlayerApi localPlayer;
    void Start()
    {
        TargetAnimator = gameObject.GetComponent<Animator>();
        FullHealth = HitPoints;
        localPlayer = Networking.LocalPlayer;
    }
    void OnParticleCollision(GameObject other)//hit by bullet
    {
        if (other == null) return;

        if (HitPoints <= 10f)//hit does 10 damage, so we're dead
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Explode");
        }
        else
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "TargetTakeDamage");
        }
    }
    private void OnCollisionEnter(Collision other)
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Explode");
    }
    public void TargetTakeDamage()
    {
        HitPoints -= 10;
    }
    public void Explode()
    {
        TargetAnimator.SetTrigger("explode");
        HitPoints = FullHealth;
        foreach (UdonSharpBehaviour Exploder in ExplodeOther)
        {
            if (Exploder != null)
            {
                Exploder.SendCustomEvent("Explode");
            }
        }
    }
}
