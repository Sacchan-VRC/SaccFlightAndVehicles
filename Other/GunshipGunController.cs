
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GunShipGunController : UdonSharpBehaviour
{
    public EngineController EngineController;
    Quaternion HeadRot;
    public Transform Gun;
    public ParticleSystem GunParticle;
    private bool lastframetrigger = false;
    [System.NonSerializedAttribute] [HideInInspector] public bool Manning = false;
    private void Update()
    {
        if (Manning)
        {
            float RTrigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
            HeadRot = EngineController.localPlayer.GetBoneRotation(HumanBodyBones.Head);
            Gun.rotation = HeadRot;
            if (RTrigger < 0.75 && lastframetrigger)
            {
                lastframetrigger = false;
            }
            if (RTrigger >= 0.75 && !lastframetrigger || (Input.GetKeyDown(KeyCode.Mouse0)))
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "FireGun");
                if (EngineController.localPlayer == null) { GunParticle.Emit(1); } // so it works in editor
                lastframetrigger = true;
            }
        }
    }
    public void FireGun()
    {
        GunParticle.Emit(1);
    }
}

