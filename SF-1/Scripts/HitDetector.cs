
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class HitDetector : UdonSharpBehaviour
{
    public EngineController EngineControl;
    [System.NonSerializedAttribute] public float LastHitTime = -100;
    [System.NonSerializedAttribute] public EngineController LastAttacker;
    private void Start()
    {
        Assert(EngineControl != null, "Start: EngineControl != null");
    }
    void OnParticleCollision(GameObject other)
    {
        if (other == null || EngineControl.dead) return;//avatars can't shoot you, and you can't get hurt when you're dead
        if (EngineControl.InEditor)
        {
            PlaneHit();
        }
        else
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PlaneHit");
        }
        GameObject EnemyObjs = other;
        HitDetector EnemyHitDetector = null;
        AAMController EnemyAAMController = null;
        while (EnemyAAMController == null && EnemyHitDetector == null && EnemyObjs.transform.parent != null)
        {
            EnemyObjs = EnemyObjs.transform.parent.gameObject;
            EnemyHitDetector = EnemyObjs.GetComponent<HitDetector>();
            EnemyAAMController = EnemyObjs.GetComponent<AAMController>();
        }
        if (EnemyHitDetector != null)
        {
            LastAttacker = EnemyHitDetector.EngineControl;
        }
        if (EnemyAAMController != null)
        {
            LastAttacker = EnemyAAMController.EngineControl;
        }
        LastHitTime = Time.time;
    }
    public void PlaneHit()
    {
        EngineControl.PlaneHit();
    }
    public void Respawn()//called by the explode animation on last frame
    {
        if (EngineControl.InEditor)//editor
        {
            EngineControl.Respawn_event();
        }
        else if (EngineControl.IsOwner)
        {
            EngineControl.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Respawn_event");//owner broadcasts because it's more reliable than everyone doing it individually
        }
    }
    public void MoveToSpawn()//called 3 seconds before respawn, to prevent a glitch where the plane will appear where it died for a second for non-owners
    {
        if (EngineControl.IsOwner)
        {
            if (EngineControl.InEditor)
            {
                EngineControl.VehicleTransform.SetPositionAndRotation(EngineControl.Spawnposition, Quaternion.Euler(EngineControl.Spawnrotation));
            }
            else
            { //this should respawn it in VRC, doesn't work in editor
                EngineControl.VehicleMainObj.transform.position = new Vector3(EngineControl.VehicleMainObj.transform.position.x, -10000, EngineControl.VehicleMainObj.transform.position.z);
            }
        }
    }
    public void NotDead()//called by 'respawn' animation twice because calling on the last frame of animation is unreliable for some reason
    {
        if (EngineControl.InEditor)
        {
            EngineControl.Health = EngineControl.FullHealth;
        }
        else if (EngineControl.IsOwner)
        {
            EngineControl.Health = EngineControl.FullHealth;
        }
        EngineControl.dead = false;
    }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogWarning("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}
