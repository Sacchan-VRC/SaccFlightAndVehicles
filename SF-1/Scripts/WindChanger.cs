
using UdonSharp;
using UnityEngine.UI;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class WindChanger : UdonSharpBehaviour
{
    public Slider WindSlider;
    public GameObject Menu;
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
        if (IsOwner) WindSlider.value = WindStrength;
        else WindStrength = WindSlider.value;
    }
    public void ApplyWindDir()
    {
        WindApplySound.Play();
        Vector3 newwinddir = (gameObject.transform.rotation * Vector3.forward) * WindStrength;
        foreach (EngineController vehicle in VehicleEngines)
        {
            vehicle.Wind = newwinddir;
        }
    }
    private void OnPickupUseDown()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ApplyWindDir");
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
