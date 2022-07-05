
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DFUNC_ToggleHandling : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SGVControl;
        [Tooltip("Object enabled when function is active (used on MFD)")]
        public GameObject Dial_Funcon;
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
        private SaccEntity EntityControl;
        private Transform CoM;
        private Vector3 CoMOriginalPos;
        private bool UseLeftTrigger;
        private bool Toggled;
        private bool Selected;
        private bool TriggerLastFrame;
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        public void SFEXT_L_EntityStart()
        {
            if (Dial_Funcon) { Dial_Funcon.SetActive(false); }

            DriveWheels = (UdonSharpBehaviour[])SGVControl.GetProgramVariable("DriveWheels");
            SteerWheels = (UdonSharpBehaviour[])SGVControl.GetProgramVariable("SteerWheels");
            OtherWheels = (UdonSharpBehaviour[])SGVControl.GetProgramVariable("OtherWheels");
            EntityControl = (SaccEntity)SGVControl.GetProgramVariable("EntityControl");
            CoM = EntityControl.CenterOfMass;
            CoMOriginalPos = CoM.localPosition;

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
        void Update()
        {
            if (Selected)
            {
                float Trigger;
                if (UseLeftTrigger)
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                else
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

                if (Trigger > 0.75f)
                {
                    if (!TriggerLastFrame)
                    {
                        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ToggleValues));
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
            gameObject.SetActive(true);
        }
        public void DFUNC_Deselected()
        {
            Selected = false;
            gameObject.SetActive(false);
        }
        public void SFEXT_O_PilotExit()
        {
            Selected = false;
            gameObject.SetActive(false);
        }
        public void ToggleValues()
        {
            if (Toggled)
            {
                if (ToggledCoMPos)
                {
                    CoM.localPosition = CoMOriginalPos;
                    EntityControl.SetCoM();
                }

                if (Dial_Funcon) { Dial_Funcon.SetActive(false); }
                for (int i = 0; i < floatSGVNames.Length; i++)
                {
                    SGVControl.SetProgramVariable(floatSGVNames[i], floatSGVToggleOriginalValues[i]);
                }
                for (int i = 0; i < boolSGVNames.Length; i++)
                {
                    SGVControl.SetProgramVariable(boolSGVNames[i], boolSGVToggleOriginalValues[i]);
                }


                for (int i = 0; i < floatDriveWheelsNames.Length; i++)
                {
                    for (int x = 0; x < DriveWheels.Length; x++)
                    {
                        DriveWheels[x].SetProgramVariable(floatSteerWheelsNames[i], floatDriveWheelsToggleOriginalValues[i]);
                    }
                }
                for (int i = 0; i < floatSteerWheelsNames.Length; i++)
                {
                    for (int x = 0; x < SteerWheels.Length; x++)
                    {
                        SteerWheels[x].SetProgramVariable(floatSteerWheelsNames[i], floatSteerWheelsToggleOriginalValues[i]);
                    }
                }
                for (int i = 0; i < floatOtherWheelsNames.Length; i++)
                {
                    for (int x = 0; x < OtherWheels.Length; x++)
                    {
                        OtherWheels[x].SetProgramVariable(floatSteerWheelsNames[i], floatOtherWheelsToggleOriginalValues[i]);
                    }
                }
                Toggled = false;
            }
            else
            {
                if (ToggledCoMPos)
                {
                    CoM.position = ToggledCoMPos.position;
                    EntityControl.SetCoM();
                }

                if (Dial_Funcon) { Dial_Funcon.SetActive(true); }
                for (int i = 0; i < floatSGVNames.Length; i++)
                {
                    SGVControl.SetProgramVariable(floatSGVNames[i], floatSGVToggledValues[i]);
                }
                for (int i = 0; i < boolSGVNames.Length; i++)
                {
                    SGVControl.SetProgramVariable(boolSGVNames[i], !boolSGVToggleOriginalValues[i]);
                }


                for (int i = 0; i < floatDriveWheelsNames.Length; i++)
                {
                    for (int x = 0; x < DriveWheels.Length; x++)
                    {
                        DriveWheels[x].SetProgramVariable(floatSteerWheelsNames[i], floatDriveWheelsToggledValues[i]);
                    }
                }
                for (int i = 0; i < floatSteerWheelsNames.Length; i++)
                {
                    for (int x = 0; x < SteerWheels.Length; x++)
                    {
                        SteerWheels[x].SetProgramVariable(floatSteerWheelsNames[i], floatSteerWheelsToggledValues[i]);
                    }
                }
                for (int i = 0; i < floatOtherWheelsNames.Length; i++)
                {
                    for (int x = 0; x < OtherWheels.Length; x++)
                    {
                        OtherWheels[x].SetProgramVariable(floatSteerWheelsNames[i], floatOtherWheelsToggledValues[i]);
                    }
                }
                Toggled = true;
            }
        }
        public void SFEXT_G_RespawnButton()
        {
            if (Toggled)
            {
                ToggleValues();
            }
        }
        public void SFEXT_G_Explode()
        {
            if (Toggled)
            {
                ToggleValues();
            }
        }
        public void KeyboardInput()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ToggleValues));
        }
    }
}