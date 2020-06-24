
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SaccTarget : UdonSharpBehaviour
{
    public float HitPoints = 100f;
    private float FullHealth;
    private Animator TargetAnimator;
    private VRCPlayerApi localPlayer;
    void OnParticleCollision(GameObject other)//hit by bullet
    {
        HitPoints += -10;
        if (HitPoints < 0f)
        {
            if (localPlayer == null)//editor
            {
                TargetExplode();
            }
            else//ingame
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "TargetExplode");
            }
            HitPoints = FullHealth;
        }
    }
    void Start()
    {
        TargetAnimator = gameObject.GetComponent<Animator>();
        FullHealth = HitPoints;
        localPlayer = Networking.LocalPlayer;
    }
    public void TargetExplode()
    {
        TargetAnimator.SetTrigger("explode");
    }
}
