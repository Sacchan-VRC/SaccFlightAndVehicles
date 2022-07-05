//comented stuff can be used to make changes in wind global
using UdonSharp;
using UnityEngine.UI;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SAV_WindChanger : UdonSharpBehaviour
    {
        [Tooltip("List of SaccAirVehicles to be effected by this WindChnager")]
        public bool DefaultSynced = false;
        [HideInInspector] public UdonSharpBehaviour[] SaccAirVehicles;
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
        private bool UpdatingValuesFromOther;
        [FieldChangeCallback(nameof(WindStrength))] private float _windStrength;

        public float WindStrength
        {
            set
            {
                if (SyncedWind)
                {
                    WindStrenth_3 = (gameObject.transform.rotation * Vector3.forward) * value;
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
                    if (!UpdatingValuesFromOther)
                    {
                        UpdatingValuesFromOther = true;
                        SendCustomEventDelayedSeconds(nameof(UpdateValuesFromOther), 1);
                    }
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
                    if (!UpdatingValuesFromOther)
                    {
                        UpdatingValuesFromOther = true;
                        SendCustomEventDelayedSeconds(nameof(UpdateValuesFromOther), 1);
                    }
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
                    if (!UpdatingValuesFromOther)
                    {
                        UpdatingValuesFromOther = true;
                        SendCustomEventDelayedSeconds(nameof(UpdateValuesFromOther), 1);
                    }
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
                    if (!UpdatingValuesFromOther)
                    {
                        UpdatingValuesFromOther = true;
                        SendCustomEventDelayedSeconds(nameof(UpdateValuesFromOther), 1);
                    }
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
                    UpdateValuesFromOther();
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
        }
        public void UpdateValuesFromOther()
        {
            UpdatingValuesFromOther = false;
            WindStrengthLocal = _windStrenth_3.magnitude;
            WindStrengthSlider.value = WindStrengthLocal;
            WindStr_text.text = WindStrengthLocal.ToString("F1");
            WindGustStrengthLocal = _windGustStrength;
            WindGustStrengthSlider.value = _windGustStrength;
            WindGustStrength_text.text = _windGustStrength.ToString("F1");
            WindGustinessLocal = _windGustiness;
            WindGustinessSlider.value = _windGustiness;
            WindGustiness_text.text = _windGustiness.ToString("F3");
            WindTurbulanceScaleLocal = _windTurbulanceScale;
            WindTurbulanceScaleSlider.value = _windTurbulanceScale;
            WindTurbulanceScale_text.text = _windTurbulanceScale.ToString("F5");

            transform.rotation = Quaternion.FromToRotation(Vector3.forward, _windStrenth_3.normalized);
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
        public void ProximityDisableLoop()
        {
            if (menuactive)
            {
                if (Vector3.Distance(transform.position, localPlayer.GetPosition()) > 5)
                {
                    WindMenu.SetActive(false);
                    menuactive = false;
                }
                SendCustomEventDelayedSeconds(nameof(ProximityDisableLoop), 1);
            }
        }
        public override void OnPickup()
        {
            if (!menuactive)
            {
                menuactive = true;
                ProximityDisableLoop();
                if (_syncedWind)
                { UpdateValuesFromOther(); }
            }
            WindMenu.SetActive(true);
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
}