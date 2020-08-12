
using UdonSharp;
using UnityEngine.UI;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class WindMenu : UdonSharpBehaviour
{
    public Slider windX;
    public Slider windZ;
    [UdonSynced(UdonSyncMode.None)] private Vector2 wind;
    public EngineController[] VehicleEngine;
    private ConstantForce[] VehicleConstantForce = new ConstantForce[2];
    void Start()
    {
        int n = 0;
        foreach (EngineController vehicle in VehicleEngine)
        {
            VehicleConstantForce[n] = vehicle.VehicleMainObj.GetComponent<ConstantForce>();
            n++;
        }
    }
    private void Update()
    {
        wind = new Vector2(windX.value, windZ.value);
        int n = 0;
        foreach (EngineController vehicle in VehicleEngine)
        {
            Vector3 NewWind = new Vector3(wind.x, 0, wind.y) * Mathf.Clamp(vehicle.Speed / 10, 0, 1);
            VehicleConstantForce[n].force = NewWind;
            n++;
        }
    }
}
