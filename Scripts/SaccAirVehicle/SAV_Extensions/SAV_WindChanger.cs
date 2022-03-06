//comented stuff can be used to make changes in wind global
using UdonSharp;
using UnityEngine.UI;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class SAV_WindChanger : UdonSharpBehaviour
{
    [Tooltip("List of SaccAirVehicles to be effected by this WindChnager")]
    public bool DefaultSynced = false;
    public UdonSharpBehaviour[] SaccAirVehicles;
    public GameObject WindMenu;
    public Slider WindStrengthSlider;
    public Text WindStr_text;
    public Slider WindGustStrengthSlider;
    public Text WindGustStrength_text;
    public Slider WindGustinessSlider;
    public Text WindGustiness_text;
    public Slider WindTurbulanceScaleSlider;
    public Text WindTurbulanceScale_text;
    public Toggle WindSyncedToggle;
    public AudioSource WindApplySound;
    [FieldChangeCallback(nameof(WindStrength))] private float _windStrength;

    public float WindStrength
    {
        set
        {
            if (SyncedWind)
            {
                WindStrenth_3 = (gameObject.transform.rotation * Vector3.forward) * value;
                WindStrengthSlider.value = value;
                WindStr_text.text = WindStrengthSlider.value.ToString("F1");
                WindStrengthLocal = value;
                WindSound();
            }
            _windStrength = value;
        }
        get => _windStrength;
    }
    [UdonSynced, FieldChangeCallback(nameof(WindStrenth_3))] private Vector3 _windStrenth_3;
    public Vector3 WindStrenth_3
    {
        set
        {
            if (SyncedWind)
            {
                foreach (UdonSharpBehaviour vehicle in SaccAirVehicles)
                {
                    if (vehicle)
                    {
                        vehicle.SetProgramVariable("Wind", value);
                    }
                }
                WindStrengthSlider.value = WindStrenth_3.magnitude;
                WindStr_text.text = WindStrengthSlider.value.ToString("F1");
                WindStrengthLocal = WindStrengthSlider.value;
                WindSound();
            }
            _windStrenth_3 = value;
        }
        get => _windStrenth_3;
    }

    [UdonSynced, FieldChangeCallback(nameof(WindGustStrength))] private float _windGustStrength;

    public float WindGustStrength
    {
        set
        {
            if (SyncedWind)
            {
                foreach (UdonSharpBehaviour vehicle in SaccAirVehicles)
                {
                    if (vehicle)
                    {
                        vehicle.SetProgramVariable("WindGustStrength", value);
                    }
                }

                WindGustStrengthSlider.value = value;
                WindGustStrength_text.text = WindGustStrengthSlider.value.ToString("F1");
                WindGustStrengthLocal = value;
                WindSound();
            }
            _windGustStrength = value;
        }
        get => _windGustStrength;
    }
    [UdonSynced, FieldChangeCallback(nameof(WindGustiness))] private float _windGustiness = 0.03f;

    public float WindGustiness
    {
        set
        {
            if (SyncedWind)
            {
                foreach (UdonSharpBehaviour vehicle in SaccAirVehicles)
                {
                    if (vehicle)
                    {
                        vehicle.SetProgramVariable("WindGustiness", value);
                    }
                }

                WindGustinessSlider.value = value;
                WindGustiness_text.text = WindGustinessSlider.value.ToString("F3");
                WindGustinessLocal = value;
                WindSound();
            }
            _windGustiness = value;
        }
        get => _windGustiness;
    }
    [UdonSynced, FieldChangeCallback(nameof(WindTurbulanceScale))] private float _windTurbulanceScale = 0.0001f;

    public float WindTurbulanceScale
    {
        set
        {
            if (SyncedWind)
            {
                foreach (UdonSharpBehaviour vehicle in SaccAirVehicles)
                {
                    if (vehicle)
                    {
                        vehicle.SetProgramVariable("WindTurbulanceScale", value);
                    }
                }
                WindTurbulanceScaleSlider.value = value;
                WindTurbulanceScale_text.text = WindTurbulanceScaleSlider.value.ToString("F5");
                WindTurbulanceScaleLocal = value;
                WindSound();
            }
            _windTurbulanceScale = value;
        }
        get => _windTurbulanceScale;
    }
    private float WindStrengthLocal;
    private float WindGustStrengthLocal;
    private float WindGustinessLocal = 0.03f;
    private float WindTurbulanceScaleLocal = 0.0001f;
    private VRCPlayerApi localPlayer;
    private bool menuactive;
    [FieldChangeCallback(nameof(SyncedWind))] private bool _syncedWind = false;
    public bool SyncedWind
    {
        set
        {
            if (value)
            {
                WindSound();
                foreach (UdonSharpBehaviour vehicle in SaccAirVehicles)
                {
                    if (vehicle)
                    {
                        vehicle.SetProgramVariable("Wind", _windStrenth_3);
                        vehicle.SetProgramVariable("WindGustStrength", _windGustStrength);
                        vehicle.SetProgramVariable("WindGustiness", _windGustiness);
                        vehicle.SetProgramVariable("WindTurbulanceScale", _windTurbulanceScale);
                    }
                }
            }
            _syncedWind = value;
        }
        get => _syncedWind;
    }
    private void Start()
    {
        localPlayer = Networking.LocalPlayer;
        WindMenu.SetActive(false);
        if (DefaultSynced)
        { SendCustomEventDelayedSeconds(nameof(SyncDefault), 10); }
    }
    public void SyncDefault()
    { WindSyncedToggle.isOn = true; }
    public void ToggleSyncedWind()
    {
        SyncedWind = !SyncedWind;
        if (SyncedWind)
        {
            WindStrengthSlider.value = WindStrengthLocal;
            WindStr_text.text = WindStrengthSlider.value.ToString("F1");
            WindGustStrengthSlider.value = WindGustStrengthLocal;
            WindGustStrength_text.text = WindGustStrengthSlider.value.ToString("F1");
            WindGustinessSlider.value = WindGustinessLocal;
            WindGustiness_text.text = WindGustinessSlider.value.ToString("F3");
            WindTurbulanceScaleSlider.value = WindTurbulanceScaleLocal;
            WindTurbulanceScale_text.text = WindTurbulanceScaleSlider.value.ToString("F5");
        }
    }
    public void UpdateValues()
    {
        WindStrengthLocal = WindStrengthSlider.value;
        WindStr_text.text = WindStrengthSlider.value.ToString("F1");

        WindGustStrengthLocal = WindGustStrengthSlider.value;
        WindGustStrength_text.text = WindGustStrengthSlider.value.ToString("F1");

        WindGustinessLocal = WindGustinessSlider.value;
        WindGustiness_text.text = WindGustinessSlider.value.ToString("F3");

        WindTurbulanceScaleLocal = WindTurbulanceScaleSlider.value;
        WindTurbulanceScale_text.text = WindTurbulanceScaleSlider.value.ToString("F5");
    }
    public void DisableLoop()
    {
        if (menuactive)
        {
            if (Vector3.Distance(transform.position, localPlayer.GetPosition()) > 5)
            {
                WindMenu.SetActive(false);
                menuactive = false;
            }
            SendCustomEventDelayedSeconds(nameof(DisableLoop), 1);
        }
    }
    public override void OnPickup()
    {
        WindMenu.SetActive(true);
        menuactive = true;
        DisableLoop();
    }
    public override void OnPickupUseDown()
    {
        if (SyncedWind)
        {
            if (!Networking.LocalPlayer.IsOwner(gameObject))
            { Networking.SetOwner(Networking.LocalPlayer, gameObject); }
            WindApplySound.Play();
            WindStrength = WindStrengthLocal;
            WindGustStrength = WindGustStrengthLocal;
            WindGustiness = WindGustinessLocal;
            WindTurbulanceScale = WindTurbulanceScaleLocal;
            RequestSerialization();
        }
        else
        {
            UpdateValues();
            ApplyWindDir();
        }
    }
    public void WindSound()
    {
        if (!WindApplySound.isPlaying)
        { WindApplySound.Play(); }
    }
    public void ApplyWindDir()
    {
        WindApplySound.Play();
        Vector3 NewWindDir = (gameObject.transform.rotation * Vector3.forward) * WindStrengthLocal;
        foreach (UdonSharpBehaviour vehicle in SaccAirVehicles)
        {
            if (vehicle)
            {
                vehicle.SetProgramVariable("Wind", NewWindDir);
                vehicle.SetProgramVariable("WindGustStrength", WindGustStrengthLocal);
                vehicle.SetProgramVariable("WindGustiness", WindGustinessLocal);
                vehicle.SetProgramVariable("WindTurbulanceScale", WindTurbulanceScaleLocal);
            }
        }
    }
}
