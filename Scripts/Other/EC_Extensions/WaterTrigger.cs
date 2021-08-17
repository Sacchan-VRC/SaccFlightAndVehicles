
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class WaterTrigger : UdonSharpBehaviour
{
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private float WaterDamageSec = 10;
    [SerializeField] private float WaterSlowDown = 3;
    private Rigidbody VehicleRigidbody;
    private int WaterLayer = 0;
    private int NumTriggers = 0;
    private bool InWater;
    private Collider ThisCollider;
    private bool Initilized;
    public void SFEXT_L_ECStart()
    {
        Initilized = true;
        WaterLayer = LayerMask.NameToLayer("Water");
        VehicleRigidbody = EngineControl.VehicleMainObj.GetComponent<Rigidbody>();
        ThisCollider = gameObject.GetComponent<Collider>();
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (localPlayer != null)
        {
            if (!localPlayer.isMaster)
            { gameObject.SetActive(false); }
        }
    }
    private void Update()
    {
        if (InWater)
        {
            float DeltaTime = Time.deltaTime;
            EngineControl.Health -= WaterDamageSec * DeltaTime;
            VehicleRigidbody.velocity = Vector3.Lerp(VehicleRigidbody.velocity, Vector3.zero, WaterSlowDown * DeltaTime);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.gameObject.layer == WaterLayer)
        {
            NumTriggers += 1;
            InWater = true;
            //playsplash
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other != null && other.gameObject.layer == WaterLayer)
        {
            NumTriggers -= 1;
            if (NumTriggers == 0) { InWater = false; }
        }
    }
    //collider enabled and disabled so that it does ontriggerenter on enable
    private void OnEnable()
    {
        if (!Initilized) { SFEXT_L_ECStart(); }//for test mode where onenable runs before ECStart
        ThisCollider.enabled = true;
    }
    private void OnDisable()
    {
        ThisCollider.enabled = false;
        if (InWater) { EngineControl.Health = -1; }//just kill the vehicle if it's underwater and the player gets out
        InWater = false;
        NumTriggers = 0;
    }
    public void SFEXT_O_TakeOwnership()
    {
        gameObject.SetActive(true);
    }
    public void SFEXT_O_LoseOwnership()
    {
        gameObject.SetActive(false);
    }
}
