
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.UdonNetworkCalling;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DFUNC_ToggleHandling : UdonSharpBehaviour
    {
        [Tooltip("Scripts other than SaccGroundVehicle work, but the wheel related stuff will not work if you choose something else")]
        public UdonSharpBehaviour SGVControl;
        [Tooltip("Keep this gameobject always enabled. So it can be used as an interactable by people outside of the vehicle.")]
        [SerializeField] private bool ObjectAlwaysEnabled;
        [Tooltip("Toggle if if fuel runs out?")]
        [SerializeField] private bool NoFuelTurnOff;
        [SerializeField] private bool WreckedTurnOff;
        [SerializeField] private bool KeepAwakeWhileOn;
        [SerializeField] Animator ToggleAnimator;
        [SerializeField] string ToggleAnimator_boolname = "HandlingToggle";
        [Tooltip("Object enabled when function is active (used on MFD)")]
        public GameObject Dial_Funcon;
        [SerializeField] private bool Funcon_Invert;
        public Transform ToggledCoMPos;
        public string[] floatSGVNames;
        public float[] floatSGVToggledValues;
        private float[] floatSGVToggleOriginalValues;
        public string[] boolSGVNames;
        private bool[] boolSGVToggleOriginalValues;
        public string[] floatSteerWheelsNames;
        public float[] floatSteerWheelsToggledValues;
        private float[] floatSteerWheelsToggleOriginalValues;
        public string[] floatDriveWheelsNames;
        public float[] floatDriveWheelsToggledValues;
        private float[] floatDriveWheelsToggleOriginalValues;
        public string[] floatOtherWheelsNames;
        public float[] floatOtherWheelsToggledValues;
        private float[] floatOtherWheelsToggleOriginalValues;
        private UdonSharpBehaviour[] DriveWheels;
        private UdonSharpBehaviour[] SteerWheels;
        private UdonSharpBehaviour[] OtherWheels;
        private bool isSGV;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SaccEntity EntityControl;
        [System.NonSerializedAttribute] public SAV_PassengerFunctionsController PassengerFunctionsControl;
        private Transform CoM;
        private Vector3 CoMOriginalPos;
        private bool Toggled;
        private bool Selected;
        private bool TriggerLastFrame;
        bool KeepingAwake;
        public void SFEXT_L_EntityStart()
        {
            if (Dial_Funcon) { Dial_Funcon.SetActive(Funcon_Invert); }
            CoM = EntityControl.CenterOfMass;
            CoMOriginalPos = CoM.localPosition;

            if (SGVControl.GetUdonTypeName() == GetUdonTypeName<SaccGroundVehicle>())
            {
                Debug.Log(SGVControl.GetUdonTypeName() + " : " + GetUdonTypeName<SaccGroundVehicle>());
                isSGV = true;
                EntityControl = (SaccEntity)SGVControl.GetProgramVariable("EntityControl");
                DriveWheels = (UdonSharpBehaviour[])SGVControl.GetProgramVariable("DriveWheels");
                SteerWheels = (UdonSharpBehaviour[])SGVControl.GetProgramVariable("SteerWheels");
                OtherWheels = (UdonSharpBehaviour[])SGVControl.GetProgramVariable("OtherWheels");

                floatDriveWheelsToggleOriginalValues = new float[floatDriveWheelsNames.Length];
                for (int i = 0; i < floatDriveWheelsToggleOriginalValues.Length; i++)
                {
                    floatDriveWheelsToggleOriginalValues[i] = (float)DriveWheels[i].GetProgramVariable(floatDriveWheelsNames[i]);
                }
                floatSteerWheelsToggleOriginalValues = new float[floatSteerWheelsNames.Length];
                for (int i = 0; i < floatSteerWheelsToggleOriginalValues.Length; i++)
                {
                    floatSteerWheelsToggleOriginalValues[i] = (float)SteerWheels[i].GetProgramVariable(floatSteerWheelsNames[i]);
                }
                floatOtherWheelsToggleOriginalValues = new float[floatOtherWheelsNames.Length];
                for (int i = 0; i < floatOtherWheelsToggleOriginalValues.Length; i++)
                {
                    floatOtherWheelsToggleOriginalValues[i] = (float)OtherWheels[i].GetProgramVariable(floatOtherWheelsNames[i]);
                }
            }

            floatSGVToggleOriginalValues = new float[floatSGVNames.Length];
            for (int i = 0; i < floatSGVToggleOriginalValues.Length; i++)
            {
                floatSGVToggleOriginalValues[i] = (float)SGVControl.GetProgramVariable(floatSGVNames[i]);
            }
            boolSGVToggleOriginalValues = new bool[boolSGVNames.Length];
            for (int i = 0; i < boolSGVToggleOriginalValues.Length; i++)
            {
                boolSGVToggleOriginalValues[i] = (bool)SGVControl.GetProgramVariable(boolSGVNames[i]);
            }

            if (ObjectAlwaysEnabled)
            { gameObject.SetActive(true); }
        }
        void Update()
        {
            if (Selected)
            {
                float Trigger;
                if (LeftDial)
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                else
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

                if (Trigger > 0.75f)
                {
                    if (!TriggerLastFrame)
                    {
                        Toggle();
                    }
                    TriggerLastFrame = true;
                }
                else
                {
                    TriggerLastFrame = false;
                }
            }
        }
        public void DFUNC_Selected()
        {
            Selected = true;
            TriggerLastFrame = true;
            gameObject.SetActive(true);
        }
        public void DFUNC_Deselected()
        {
            Selected = false;
            if (!ObjectAlwaysEnabled)
                gameObject.SetActive(false);
        }
        public void SFEXT_O_PilotExit()
        {
            Selected = false;
            if (!ObjectAlwaysEnabled)
                gameObject.SetActive(false);
        }
        [NetworkCallable]
        public void ToggleOn()
        {
            if (Toggled) return;
            if (KeepAwakeWhileOn && !KeepingAwake)
            {
                KeepingAwake = true;
                SGVControl.SetProgramVariable("KeepAwake", (int)SGVControl.GetProgramVariable("KeepAwake") + 1);
            }
            if (ToggleAnimator)
            {
                ToggleAnimator.SetBool(ToggleAnimator_boolname, true);
            }

            if (ToggledCoMPos)
            {
                CoM.position = ToggledCoMPos.position;
                EntityControl.SetCoM();
            }

            if (Dial_Funcon) { Dial_Funcon.SetActive(!Funcon_Invert); }
            for (int i = 0; i < floatSGVNames.Length; i++)
            {
                SGVControl.SetProgramVariable(floatSGVNames[i], floatSGVToggledValues[i]);
            }
            for (int i = 0; i < boolSGVNames.Length; i++)
            {
                SGVControl.SetProgramVariable(boolSGVNames[i], !boolSGVToggleOriginalValues[i]);
            }

            if (isSGV)
            {
                for (int i = 0; i < floatDriveWheelsNames.Length; i++)
                {
                    for (int x = 0; x < DriveWheels.Length; x++)
                    {
                        DriveWheels[x].SetProgramVariable(floatSteerWheelsNames[i], floatDriveWheelsToggledValues[i]);
                        DriveWheels[x].SendCustomEvent("ChangeSurface");
                    }
                }
                for (int i = 0; i < floatSteerWheelsNames.Length; i++)
                {
                    for (int x = 0; x < SteerWheels.Length; x++)
                    {
                        SteerWheels[x].SetProgramVariable(floatSteerWheelsNames[i], floatSteerWheelsToggledValues[i]);
                        SteerWheels[x].SendCustomEvent("ChangeSurface");
                    }
                }
                for (int i = 0; i < floatOtherWheelsNames.Length; i++)
                {
                    for (int x = 0; x < OtherWheels.Length; x++)
                    {
                        OtherWheels[x].SetProgramVariable(floatSteerWheelsNames[i], floatOtherWheelsToggledValues[i]);
                        OtherWheels[x].SendCustomEvent("ChangeSurface");
                    }
                }
            }
            Toggled = true;
        }
        [NetworkCallable]
        public void ToggleOff()
        {
            if (!Toggled) return;
            if (KeepAwakeWhileOn && KeepingAwake)
            {
                KeepingAwake = false;
                SGVControl.SetProgramVariable("KeepAwake", (int)SGVControl.GetProgramVariable("KeepAwake") - 1);
            }
            if (ToggleAnimator)
            {
                ToggleAnimator.SetBool(ToggleAnimator_boolname, false);
            }

            if (ToggledCoMPos)
            {
                CoM.localPosition = CoMOriginalPos;
                EntityControl.SetCoM();
            }

            if (Dial_Funcon) { Dial_Funcon.SetActive(Funcon_Invert); }
            for (int i = 0; i < floatSGVNames.Length; i++)
            {
                SGVControl.SetProgramVariable(floatSGVNames[i], floatSGVToggleOriginalValues[i]);
            }
            for (int i = 0; i < boolSGVNames.Length; i++)
            {
                SGVControl.SetProgramVariable(boolSGVNames[i], boolSGVToggleOriginalValues[i]);
            }

            if (isSGV)
            {
                for (int i = 0; i < floatDriveWheelsNames.Length; i++)
                {
                    for (int x = 0; x < DriveWheels.Length; x++)
                    {
                        DriveWheels[x].SetProgramVariable(floatSteerWheelsNames[i], floatDriveWheelsToggleOriginalValues[i]);
                        DriveWheels[x].SendCustomEvent("ChangeSurface");
                    }
                }
                for (int i = 0; i < floatSteerWheelsNames.Length; i++)
                {
                    for (int x = 0; x < SteerWheels.Length; x++)
                    {
                        SteerWheels[x].SetProgramVariable(floatSteerWheelsNames[i], floatSteerWheelsToggleOriginalValues[i]);
                        SteerWheels[x].SendCustomEvent("ChangeSurface");
                    }
                }
                for (int i = 0; i < floatOtherWheelsNames.Length; i++)
                {
                    for (int x = 0; x < OtherWheels.Length; x++)
                    {
                        OtherWheels[x].SetProgramVariable(floatSteerWheelsNames[i], floatOtherWheelsToggleOriginalValues[i]);
                        OtherWheels[x].SendCustomEvent("ChangeSurface");
                    }
                }
            }
            Toggled = false;
        }
        public void SFEXT_G_RespawnButton()
        {
            ToggleOff();
        }
        public void SFEXT_G_ReAppear()
        {
            ToggleOff();
        }
        public void SFEXT_G_Explode()
        {
            ToggleOff();
        }
        public void Toggle()
        {
            if (Toggled)
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ToggleOff));
            else if ((!NoFuelTurnOff || !NoFuel) && (!WreckedTurnOff || !wrecked))
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ToggleOn));
        }
        public void KeyboardInput()
        {
            Toggle();
        }
        public override void Interact()
        {
            Toggle();
        }
        private bool NoFuel;
        public void SFEXT_G_NoFuel()
        {
            NoFuel = true;
            if (NoFuelTurnOff && Toggled) ToggleOff();
        }
        public void SFEXT_G_NotNoFuel()
        {
            NoFuel = false;
        }
        private bool wrecked;
        public void SFEXT_G_Wrecked()
        {
            wrecked = true;
            if (WreckedTurnOff && Toggled) ToggleOff();
        }
        public void SFEXT_G_NotWrecked()
        {
            wrecked = false;
        }
    }
}