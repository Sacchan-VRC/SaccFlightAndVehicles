
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class VehicleRespawnButton : UdonSharpBehaviour
{
    public GameObject VehicleMainObj;
    public EngineController EngineControl;
    private void Start()
    {
        Assert(VehicleMainObj != null, "Start: VehicleMainObj != null");
        Assert(EngineControl != null, "Start: EngineControl != null");
    }
    private void Interact()
    {
        if (!EngineControl.Occupied && !EngineControl.dead)
        {
            Networking.SetOwner(EngineControl.localPlayer, VehicleMainObj);
            Networking.SetOwner(EngineControl.localPlayer, EngineControl.gameObject);
            Networking.SetOwner(EngineControl.localPlayer, EngineControl.EffectsControl.gameObject);
            VehicleMainObj.transform.position = new Vector3(VehicleMainObj.transform.position.x, -10000, VehicleMainObj.transform.position.z);
            EngineControl.EffectsControl.GearUp = false;
            EngineControl.EffectsControl.Flaps = true;
            EngineControl.EffectsControl.HookDown = false;
            EngineControl.Health = EngineControl.FullHealth;
            EngineControl.Fuel = EngineControl.FullFuel;
            EngineControl.NumAAM = EngineControl.FullAAMs;
            EngineControl.NumAGM = EngineControl.FullAGMs;
            EngineControl.NumBomb = EngineControl.FullBombs;
            EngineControl.GunAmmoInSeconds = EngineControl.FullGunAmmo;
            EngineControl.Fuel = EngineControl.FullFuel;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ButtonRespawn");
        }
    }
    public void ButtonRespawn()
    {
        EngineControl.EffectsControl.DoEffects = 6;
        EngineControl.dead = true;//this makes it invincible and unable to be respawned again for 5s
        EngineControl.EffectsControl.PlaneAnimator.SetTrigger("respawn");//this animation disables EngineControl.dead
        EngineControl.EffectsControl.PlaneAnimator.SetTrigger("instantgeardown");
    }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogError("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}