
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class VehicleRespawnButton : UdonSharpBehaviour
{
    public GameObject VehicleMainObj;
    public EngineController EngineControl;
    private void Interact()
    {
        if (!EngineControl.Occupied && !EngineControl.dead)
        {
            Networking.SetOwner(EngineControl.localPlayer, VehicleMainObj);
            Networking.SetOwner(EngineControl.localPlayer, EngineControl.gameObject);
            VehicleMainObj.transform.position = new Vector3(VehicleMainObj.transform.position.x, -10000, VehicleMainObj.transform.position.z);
            EngineControl.GearUp = false;
            EngineControl.Flaps = true;
            EngineControl.Health = EngineControl.FullHealth;
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
}