
using UdonSharp;
using UnityEngine.UI;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class WindChanger : UdonSharpBehaviour
{
    public GameObject WindMenu;
    public Slider WindSlider;
    public Text WindStr_text;
    public Slider WindGustStrengthSlider;
    public Text WindGustStrength_text;
    public Slider WindGustinessSlider;
    public Text WindGustiness_text;
    public Slider WindTurbulanceScaleSlider;
    public Text WindTurbulanceScale_text;
    public AudioSource WindApplySound;
    public EngineController[] VehicleEngines;
    [UdonSynced(UdonSyncMode.None)] private float WindStrength;
    [UdonSynced(UdonSyncMode.None)] private float WindGustStrength;
    [UdonSynced(UdonSyncMode.None)] private float WindGustiness;
    [UdonSynced(UdonSyncMode.None)] private float WindTurbulanceScale;
    private VRCPlayerApi localPlayer;
    private void Start()
    {
        Assert(WindMenu != null, "Start: WindMenu != null");
        Assert(WindSlider != null, "Start: WindSlider != null");
        Assert(WindStr_text != null, "Start: WindStr_text != null");
        Assert(WindGustStrengthSlider != null, "Start: WindGustStrengthSlider != null");
        Assert(WindGustStrength_text != null, "Start: WindGustStrength_text != null");
        Assert(WindGustinessSlider != null, "Start: WindGustinessSlider != null");
        Assert(WindGustiness_text != null, "Start: WindGustiness_text != null");
        Assert(WindTurbulanceScaleSlider != null, "Start: WindTurbulanceScaleSlider != null");
        Assert(WindTurbulanceScale_text != null, "Start: WindTurbulanceScale_text != null");
        Assert(WindApplySound != null, "Start: WindApplySound != null");
        Assert(VehicleEngines != null, "Start: VehicleEngines != null");


        localPlayer = Networking.LocalPlayer;
    }
    private void Update()
    {
        if (localPlayer.IsOwner(gameObject))
        {
            WindStrength = WindSlider.value;
            WindStr_text.text = WindSlider.value.ToString("F1");

            WindGustStrength = WindGustStrengthSlider.value;
            WindGustStrength_text.text = WindGustStrengthSlider.value.ToString("F1");

            WindGustiness = WindGustinessSlider.value;
            WindGustiness_text.text = WindGustinessSlider.value.ToString("F3");

            WindTurbulanceScale = WindTurbulanceScaleSlider.value;
            WindTurbulanceScale_text.text = WindTurbulanceScaleSlider.value.ToString("F5");
        }
        else
        {
            WindSlider.value = WindStrength;
            WindGustStrengthSlider.value = WindGustStrength;
            WindGustinessSlider.value = WindGustiness;
            WindTurbulanceScaleSlider.value = WindTurbulanceScale;
        }
    }
    private void OnPickupUseDown()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ApplyWindDir");
    }
    public void ApplyWindDir()
    {
        WindApplySound.Play();
        Vector3 NewWindDir = (gameObject.transform.rotation * Vector3.forward) * WindStrength;
        foreach (EngineController vehicle in VehicleEngines)
        {
            if (localPlayer.IsOwner(vehicle.gameObject))
            {
                vehicle.Wind = NewWindDir;
                vehicle.WindGustStrength = WindGustStrength;
                vehicle.WindGustiness = WindGustiness;
                vehicle.WindTurbulanceScale = WindTurbulanceScale;
            }
        }
    }
    private void OnPickup()
    {
        WindMenu.SetActive(true);
    }

    private void OnOwnershipTransferred()
    {
        WindMenu.SetActive(false);
    }
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogError("Assertion failed : '" + GetType() + " : " + message + "'", this);
        }
    }
}
