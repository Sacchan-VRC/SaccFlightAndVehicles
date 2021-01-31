
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SaccTarget : UdonSharpBehaviour
{
    public float HitPoints = 100f;
    private float FullHealth;
    public Animator TargetAnimator;
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

        if (HitPoints <= 10f)//hit does 10 damage
        {
            if (localPlayer == null)//editor
            {
                TargetExplode();
            }
            else//ingame
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "TargetExplode");
            }
        }
        else
        {
            if (localPlayer == null)//editor
            {
                TargetTakeDamage();
            }
            else//ingame
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "TargetTakeDamage");
            }
        }
    }
    public void TargetTakeDamage()
    {
        HitPoints -= 10;
    }
    public void TargetExplode()
    {
        TargetAnimator.SetTrigger("explode");
        HitPoints = FullHealth;
    }
}
