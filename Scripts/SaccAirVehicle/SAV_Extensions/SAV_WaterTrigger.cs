
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class SAV_WaterTrigger : UdonSharpBehaviour
{
    [SerializeField] private UdonSharpBehaviour SAVControl;
    [Tooltip("Damage applied to vehicle per second while vehicle is underwater")]
    [SerializeField] private float WaterDamageSec = 10;
    [Tooltip("Strength of force slowing down the vehicle when it's underwater")]
    [SerializeField] private float WaterSlowDown = 3;
    [Tooltip("Strength of force slowing down the vehicle's rotation when it's underwater")]
    [SerializeField] private float WaterSlowDownRot = 3;
    private SaccEntity EntityControl;
    private Rigidbody VehicleRigidbody;
    private bool CFOverridden;
    private int WaterLayer = 0;
    private int NumTriggers = 0;
    private bool InWater;
    private Collider ThisCollider;
    private bool Initilized;
    private bool DisableTaxiRotation;
    public void SFEXT_L_EntityStart()
    {
        Initilized = true;
        WaterLayer = LayerMask.NameToLayer("Water");
        EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
        VehicleRigidbody = EntityControl.GetComponent<Rigidbody>();
        ThisCollider = gameObject.GetComponent<Collider>();
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (localPlayer != null)
        {
            if (!localPlayer.isInstanceOwner)
            { gameObject.SetActive(false); }
        }

    }
    private void Update()
    {
        if (InWater)
        {
            float DeltaTime = Time.deltaTime;
            SAVControl.SetProgramVariable("Health", (float)SAVControl.GetProgramVariable("Health") - (WaterDamageSec * DeltaTime));
            VehicleRigidbody.velocity = Vector3.Lerp(VehicleRigidbody.velocity, Vector3.zero, WaterSlowDown * DeltaTime);
            VehicleRigidbody.angularVelocity = Vector3.Lerp(VehicleRigidbody.angularVelocity, Vector3.zero, WaterSlowDownRot * DeltaTime);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.gameObject.layer == WaterLayer)
        {
            NumTriggers += 1;
            InWater = true;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendEnterWater));
            if (!CFOverridden)
            {
                CFOverridden = true;
                SAVControl.SetProgramVariable("OverrideConstantForce", (int)SAVControl.GetProgramVariable("OverrideConstantForce") + 1);
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other != null && other.gameObject.layer == WaterLayer)
        {
            NumTriggers -= 1;
            if (NumTriggers == 0)
            {
                InWater = false;
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendExitWater));
                if (CFOverridden)
                {
                    CFOverridden = false;
                    SAVControl.SetProgramVariable("OverrideConstantForce", (int)SAVControl.GetProgramVariable("OverrideConstantForce") - 1);
                }
            }
        }
    }
    public void SendEnterWater()
    {
        EntityControl.SendEventToExtensions("SFEXT_G_EnterWater");
        if (!DisableTaxiRotation)
        {
            SAVControl.SetProgramVariable("DisableTaxiRotation", (int)SAVControl.GetProgramVariable("DisableTaxiRotation") + 1);
            DisableTaxiRotation = true;
        }
    }
    public void SendExitWater()
    {
        EntityControl.SendEventToExtensions("SFEXT_G_ExitWater");
        if (DisableTaxiRotation)
        {
            SAVControl.SetProgramVariable("DisableTaxiRotation", (int)SAVControl.GetProgramVariable("DisableTaxiRotation") - 1);
            DisableTaxiRotation = false;
        }
    }
    //collider enabled and disabled so that it does ontriggerenter on enable
    private void OnEnable()
    {
        if (!Initilized) { SFEXT_L_EntityStart(); }//for test mode where onenable runs before ECStart
        ThisCollider.enabled = true;
    }
    private void OnDisable()
    {
        ThisCollider.enabled = false;
        if (InWater && WaterDamageSec > 0)
        { SAVControl.SetProgramVariable("Health", -1); }//just kill the vehicle if it's underwater and the player gets out
        InWater = false;
        NumTriggers = 0;
    }
    public void SFEXT_G_Explode()
    {
        if (CFOverridden)
        {
            CFOverridden = false;
            SAVControl.SetProgramVariable("OverrideConstantForce", (int)SAVControl.GetProgramVariable("OverrideConstantForce") - 1);
        }
    }
    public void SFEXT_G_RespawnButton()
    {
        if (CFOverridden)
        {
            CFOverridden = false;
            SAVControl.SetProgramVariable("OverrideConstantForce", (int)SAVControl.GetProgramVariable("OverrideConstantForce") - 1);
        }
        if (InWater)
        {
            SendExitWater();
        }
    }
    public void SFEXT_O_TakeOwnership()
    {
        gameObject.SetActive(true);
    }
    public void SFEXT_O_LoseOwnership()
    {
        gameObject.SetActive(false);
    }
    public void SFEXT_L_OwnershipTransfer()
    {
        SendExitWater();
    }
}
