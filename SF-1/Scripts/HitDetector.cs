
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class HitDetector : UdonSharpBehaviour
{
    public EngineController EngineControl;
    [System.NonSerializedAttribute] public float LastHitTime = -100;
    [System.NonSerializedAttribute] public EngineController LastAttacker;
    private bool InEditor = true;
    VRC.SDK3.Components.VRCObjectSync VehicleObjectSync;
    private UdonSharpBehaviour[] ExtensionUdonBehaviours;
    private Rigidbody VehicleRigid;

    private void Start()
    {
        Assert(EngineControl != null, "Start: EngineControl != null");
        if (Networking.LocalPlayer != null)
        { InEditor = false; }

        VehicleObjectSync = (VRC.SDK3.Components.VRCObjectSync)GetComponent(typeof(VRC.SDK3.Components.VRCObjectSync));
        ExtensionUdonBehaviours = EngineControl.ExtensionUdonBehaviours;
        VehicleRigid = GetComponent<Rigidbody>();
    }
    void OnParticleCollision(GameObject other)
    {
        if (other == null || EngineControl.dead) return;//avatars can't shoot you, and you can't get hurt when you're dead
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "PlaneHit");

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
        if (EngineControl.IsOwner)
        {
            EngineControl.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "Respawn_event");//owner broadcasts because it's more reliable than everyone doing it individually
        }
    }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogWarning("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}
