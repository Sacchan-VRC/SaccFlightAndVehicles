
using UdonSharp;
using UnityEngine.UI;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class WindChanger : UdonSharpBehaviour
{
    public GameObject Menu;
    public Slider WindSlider;
    public Text WindStr_text;
    public AudioSource WindApplySound;
    public EngineController[] VehicleEngines;
    [UdonSynced(UdonSyncMode.None)] private float WindStrength;
    private VRCPlayerApi localPlayer;
    private bool IsOwner = false;
    private void Start()
    {
        localPlayer = Networking.LocalPlayer;
        Menu.SetActive(false);
    }
    private void Update()
    {
        if (IsOwner)
        {
            WindStrength = WindSlider.value;
            WindStr_text.text = WindSlider.value.ToString("F1");
        }
        else WindSlider.value = WindStrength;
    }
    private void OnPickupUseDown()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ApplyWindDir");
    }
    public void ApplyWindDir()
    {
        WindApplySound.Play();
        Vector3 newwinddir = (gameObject.transform.rotation * Vector3.forward) * WindStrength;
        foreach (EngineController vehicle in VehicleEngines)
        {
            if (localPlayer.IsOwner(vehicle.gameObject))
                vehicle.Wind = newwinddir;
        }
    }
    private void OnPickup()
    {
        Menu.SetActive(true);
    }

    private void OnOwnershipTransferred()
    {
        if (localPlayer.IsOwner(gameObject)) { IsOwner = true; }
        else { IsOwner = false; }
        Menu.SetActive(false);
    }
}
