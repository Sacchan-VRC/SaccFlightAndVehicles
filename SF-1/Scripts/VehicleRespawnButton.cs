
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class VehicleRespawnButton : UdonSharpBehaviour
{
    public EngineController EngineControl;
    private void Start()
    {
        Assert(EngineControl != null, "Start: EngineControl != null");
    }
    private void Interact()
    {
        if (!EngineControl.Occupied && !EngineControl.dead)
        {
            Networking.SetOwner(EngineControl.localPlayer, EngineControl.VehicleMainObj);
            Networking.SetOwner(EngineControl.localPlayer, EngineControl.gameObject);
            Networking.SetOwner(EngineControl.localPlayer, EngineControl.EffectsControl.gameObject);
            EngineControl.VehicleMainObj.transform.position = new Vector3(EngineControl.VehicleMainObj.transform.position.x, -10000, EngineControl.VehicleMainObj.transform.position.z);
            if (EngineControl.HasCanopy) { EngineControl.EffectsControl.CanopyOpen = true; }
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
            EngineControl.AAMLaunchOpositeSide = false;
            EngineControl.AGMLaunchOpositeSide = false;
            EngineControl.BombPoint = 0;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ButtonRespawn");
        }
    }
    public void ButtonRespawn()
    {
        EngineControl.EffectsControl.DoEffects = 6;
        EngineControl.dead = true;//this makes it invincible and unable to be respawned again for 5s
        EngineControl.EffectsControl.PlaneAnimator.SetTrigger("respawn");//this animation disables EngineControl.dead after 5s
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