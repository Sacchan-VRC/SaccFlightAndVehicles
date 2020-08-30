
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class HitDetectorAAGun : UdonSharpBehaviour
{
    public AAGunController AAGunControl;
    public AudioSource[] BulletHit;
    private void Start()
    {
        Assert(AAGunControl != null, "Start: AAGunControl != null");
        Assert(BulletHit[0] != null, "Start: BulletHit[0] != null");
    }
    void OnParticleCollision(GameObject other)
    {
        if (other == null || AAGunControl.dead) return;//avatars can't shoot you, and you can't take hits when you're dead
        if (AAGunControl.localPlayer == null)
        {
            AAGunHit();
        }
        else
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "AAGunHit");
        }
    }
    public void AAGunHit()
    {
        if (AAGunControl.dead) return;
        if (AAGunControl.localPlayer == null || AAGunControl.localPlayer.IsOwner(AAGunControl.gameObject))
        {
            AAGunControl.Health += -10;
        }
        if (BulletHit != null)
        {
            int rand = Random.Range(0, BulletHit.Length);
            BulletHit[rand].pitch = Random.Range(.8f, 1.2f);
            BulletHit[rand].Play();
        }
    }
    public void respawn()
    {
        AAGunControl.dead = false;
        if (AAGunControl.localPlayer == null || AAGunControl.localPlayer.IsOwner(AAGunControl.gameObject))
        {
            AAGunControl.Health = AAGunControl.FullHealth;
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