
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_VTOLAngle : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public float SyncUpdateRate = .25f;
        private bool UseLeftTrigger = false;
        [System.NonSerializedAttribute, UdonSynced] public float VTOLAngle;
        private float VTOLAngleLast;
        private float VTOLDefault;
        private Transform ControlsRoot;
        private VRCPlayerApi localPlayer;
        private bool TriggerLastFrame;
        private bool IsOwner;
        private bool InVR;
        private bool Selected;
        private bool UpdatingVar;
        private bool UpdatingVR;
        private bool UpdatingKeyb;
        private float NewVTOLAngle;
        private float VTOLMover;
        private float UpdateTime;
        private bool VTOL360;
        private float VTOLTemp;
        private float VTOLZeroPoint;
        private float VTOLAngleDivider;
        private float ThrottleSensitivity;
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        public void SFEXT_L_EntityStart()
        {
            VTOLDefault = (float)SAVControl.GetProgramVariable("VTOLDefaultValue");
            ControlsRoot = (Transform)SAVControl.GetProgramVariable("ControlsRoot");
            localPlayer = Networking.LocalPlayer;
            ThrottleSensitivity = (float)SAVControl.GetProgramVariable("ThrottleSensitivity");
            InVR = (bool)SAVControl.GetProgramVariable("InVR");
            SAVControl.SetProgramVariable("VTOLenabled", true);
            VTOL360 = (bool)SAVControl.GetProgramVariable("VTOL360");
            IsOwner = (bool)SAVControl.GetProgramVariable("IsOwner");
            float vtolangledif = (float)SAVControl.GetProgramVariable("VTOLMaxAngle") - (float)SAVControl.GetProgramVariable("VTOLMinAngle");
            VTOLAngleDivider = (float)SAVControl.GetProgramVariable("VTOLAngleTurnRate") / vtolangledif;
        }
        public void SFEXT_O_PilotEnter()
        {
            TriggerLastFrame = false;
            gameObject.SetActive(true);
            VTOLMover = VTOLAngleLast = NewVTOLAngle = VTOLAngle;
            RequestSerialization();
        }
        public void SFEXT_G_PilotEnter()
        {
            gameObject.SetActive(true);
        }
        public void SFEXT_G_PilotExit()
        {
            gameObject.SetActive(false);
        }
        public void DFUNC_Selected()
        {
            Selected = true;
        }
        public void DFUNC_Deselected()
        {
            Selected = false;
            RequestSerialization();
        }
        private void LateUpdate()
        {
            if (IsOwner)
            {
                if (!InVR || Selected)
                {
                    VTOLAngle = (float)SAVControl.GetProgramVariable("VTOLAngle");
                    float pgup = Input.GetKey(KeyCode.PageUp) ? 1 : 0;
                    float pgdn = Input.GetKey(KeyCode.PageDown) ? 1 : 0;
                    if (pgup + pgdn != 0)
                    {
                        UpdatingKeyb = true;
                        float NewVTOL = (float)SAVControl.GetProgramVariable("VTOLAngleInput") + ((pgdn - pgup) * (VTOLAngleDivider * Time.smoothDeltaTime));
                        SAVControl.SetProgramVariable("VTOLAngleInput", NewVTOL);
                    }
                    float Trigger;
                    if (UseLeftTrigger)
                    { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                    else
                    { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
                    if (Trigger > 0.75)
                    {
                        UpdatingVR = true;
                        Vector3 handpos = ControlsRoot.position - localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
                        handpos = ControlsRoot.InverseTransformDirection(handpos);

                        if (!TriggerLastFrame)
                        {
                            VTOLZeroPoint = handpos.z;
                            VTOLTemp = VTOLAngle;
                        }
                        float newvtol = VTOLTemp + ((VTOLZeroPoint - handpos.z) * -ThrottleSensitivity);
                        SAVControl.SetProgramVariable("VTOLAngleInput", newvtol);

                        TriggerLastFrame = true;
                    }
                    else { TriggerLastFrame = false; }

                    if (UpdatingVR || UpdatingKeyb)
                    {
                        if (!UpdatingVar) { UpdateTime = Time.time; }//prevent a tiny adjustment being sent that causes the movement to stutter 
                        if (Time.time - UpdateTime > SyncUpdateRate)
                        {
                            RequestSerialization();
                            UpdateTime = Time.time;
                        }
                        UpdatingVar = true;
                    }
                    else
                    {
                        if (UpdatingVar)
                        {
                            RequestSerialization();//make sure others recieve final position after finished adjusting
                            UpdateTime = Time.time;
                            UpdatingVar = false;
                        }
                    }
                }
            }
            else
            {
                if (VTOL360)
                {
                    if (NewVTOLAngle >= 0)
                    { NewVTOLAngle = NewVTOLAngle - Mathf.Floor(NewVTOLAngle); }
                    else
                    {
                        float AbsIn = Mathf.Abs(NewVTOLAngle);
                        NewVTOLAngle = 1 - (AbsIn - Mathf.Floor(AbsIn));
                    }
                    //set value above or below current VTOLAngle to make it interpolate in the shortest direction
                    if (VTOLMover > NewVTOLAngle)
                    {
                        if (Mathf.Abs(VTOLMover - NewVTOLAngle) > .5f)
                        { NewVTOLAngle += 1; }
                    }
                    else
                    {
                        if (Mathf.Abs(VTOLMover - NewVTOLAngle) > .5f)
                        { NewVTOLAngle -= 1; }
                    }
                    VTOLMover = Mathf.MoveTowards(VTOLMover, NewVTOLAngle, VTOLAngleDivider * Time.smoothDeltaTime);
                    if (VTOLMover < 0) { VTOLMover++; }
                    else if (VTOLMover > 1) { VTOLMover--; }
                }
                else
                {
                    VTOLMover = Mathf.MoveTowards(VTOLMover, NewVTOLAngle, VTOLAngleDivider * Time.smoothDeltaTime);
                }
                SAVControl.SetProgramVariable("VTOLAngle", VTOLMover);
            }
        }
        public override void OnDeserialization()
        {
            if (!IsOwner)
            {
                //extrapolate VTOLAngle and lerp towards it to smooth out movement
                if (VTOLAngle != VTOLAngleLast)
                {
                    float tim = Time.time;
                    NewVTOLAngle = VTOLAngle;
                    if (VTOL360)
                    {
                        if (Mathf.Abs(VTOLAngle - VTOLAngleLast) > .5f)
                        {
                            if (VTOLAngle > VTOLAngleLast)
                            {
                                NewVTOLAngle++;
                            }
                            else
                            {
                                NewVTOLAngle--;
                            }
                        }
                    }
                    VTOLAngleLast = VTOLAngle;
                }
            }
        }
        public void SFEXT_G_Explode()
        {
            VTOLMover = VTOLAngleLast = VTOLAngle = NewVTOLAngle = VTOLDefault;
            SAVControl.SetProgramVariable("VTOLAngle", VTOLDefault);
        }
        public void SFEXT_G_RespawnButton()
        {
            VTOLMover = VTOLAngleLast = VTOLAngle = NewVTOLAngle = VTOLDefault;
            SAVControl.SetProgramVariable("VTOLAngle", VTOLDefault);
        }
        public void SFEXT_O_TakeOwnership()
        {
            IsOwner = true;
            VTOLMover = VTOLAngleLast = VTOLAngle = NewVTOLAngle;
        }
        public void SFEXT_O_LoseOwnership()
        {
            IsOwner = false;
            NewVTOLAngle = VTOLMover = VTOLAngleLast = VTOLAngle;
        }
    }
}