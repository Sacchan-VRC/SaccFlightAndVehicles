
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
    private void Initialize()
    {
        Initilized = true;
        WaterLayer = LayerMask.NameToLayer("Water");
        VehicleRigidbody = EngineControl.VehicleMainObj.GetComponent<Rigidbody>();
        ThisCollider = gameObject.GetComponent<Collider>();
        if (Networking.LocalPlayer != null)
        { gameObject.SetActive(false); }
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
        if (!Initilized) { Initialize(); return; }//OnEnable runs before Start()
        ThisCollider.enabled = true;
    }
    private void OnDisable()
    {
        if (!Initilized) { Initialize(); }
        ThisCollider.enabled = false;
        if (InWater) EngineControl.Health = -1;
        InWater = false;
        NumTriggers = 0;
    }
    public void SFEXT_TakeOwnership()
    {
        { gameObject.SetActive(true); }
    }
    public void SFEXT_LoseOwnership()
    {
        { gameObject.SetActive(false); }
    }
}
