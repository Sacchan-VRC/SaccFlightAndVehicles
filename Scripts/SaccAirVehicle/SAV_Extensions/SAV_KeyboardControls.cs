/* Please make your own version of this script if you're making something greatly modified */
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SAV_KeyboardControls : UdonSharpBehaviour
    {
        [Header("Some functions may have their own keyboard controls options.")]
        public UdonSharpBehaviour Lfunc1;
        public KeyCode Lfunc1key;
        public UdonSharpBehaviour Lfunc2;
        public KeyCode Lfunc2key;
        public UdonSharpBehaviour Lfunc3;
        public KeyCode Lfunc3key;
        public UdonSharpBehaviour Lfunc4;
        public KeyCode Lfunc4key;
        public UdonSharpBehaviour Lfunc5;
        public KeyCode Lfunc5key;
        public UdonSharpBehaviour Lfunc6;
        public KeyCode Lfunc6key;
        public UdonSharpBehaviour Lfunc7;
        public KeyCode Lfunc7key;
        public UdonSharpBehaviour Lfunc8;
        public KeyCode Lfunc8key;
        public UdonSharpBehaviour Rfunc1;
        public KeyCode Rfunc1key;
        public UdonSharpBehaviour Rfunc2;
        public KeyCode Rfunc2key;
        public UdonSharpBehaviour Rfunc3;
        public KeyCode Rfunc3key;
        public UdonSharpBehaviour Rfunc4;
        public KeyCode Rfunc4key;
        public UdonSharpBehaviour Rfunc5;
        public KeyCode Rfunc5key;
        public UdonSharpBehaviour Rfunc6;
        public KeyCode Rfunc6key;
        public UdonSharpBehaviour Rfunc7;
        public KeyCode Rfunc7key;
        public UdonSharpBehaviour Rfunc8;
        public KeyCode Rfunc8key;
        private string KeyboardInput = "KeyboardInput";
        private float VTOLAngleDivider;
        void Update()
        {
            if (Input.GetKeyDown(Lfunc1key))
            {
                if (Lfunc1) Lfunc1.SendCustomEvent(KeyboardInput);
            }
            if (Input.GetKeyDown(Lfunc2key))
            {
                if (Lfunc2) Lfunc2.SendCustomEvent(KeyboardInput);
            }
            if (Input.GetKeyDown(Lfunc3key))
            {
                if (Lfunc3) Lfunc3.SendCustomEvent(KeyboardInput);
            }
            if (Input.GetKeyDown(Lfunc4key))
            {
                if (Lfunc4) Lfunc4.SendCustomEvent(KeyboardInput);
            }
            if (Input.GetKeyDown(Lfunc5key))
            {
                if (Lfunc5) Lfunc5.SendCustomEvent(KeyboardInput);
            }
            if (Input.GetKeyDown(Lfunc6key))
            {
                if (Lfunc6) Lfunc6.SendCustomEvent(KeyboardInput);
            }
            if (Input.GetKeyDown(Lfunc7key))
            {
                if (Lfunc7) Lfunc7.SendCustomEvent(KeyboardInput);
            }
            if (Input.GetKeyDown(Lfunc8key))
            {
                if (Lfunc8) Lfunc8.SendCustomEvent(KeyboardInput);
            }


            if (Input.GetKeyDown(Rfunc1key))
            {
                if (Rfunc1) Rfunc1.SendCustomEvent(KeyboardInput);
            }
            if (Input.GetKeyDown(Rfunc2key))
            {
                if (Rfunc2) Rfunc2.SendCustomEvent(KeyboardInput);
            }
            if (Input.GetKeyDown(Rfunc3key))
            {
                if (Rfunc3) Rfunc3.SendCustomEvent(KeyboardInput);
            }
            if (Input.GetKeyDown(Rfunc4key))
            {
                if (Rfunc4) Rfunc4.SendCustomEvent(KeyboardInput);
            }
            if (Input.GetKeyDown(Rfunc5key))
            {
                if (Rfunc5) Rfunc5.SendCustomEvent(KeyboardInput);
            }
            if (Input.GetKeyDown(Rfunc6key))
            {
                if (Rfunc6) Rfunc6.SendCustomEvent(KeyboardInput);
            }
            if (Input.GetKeyDown(Rfunc7key))
            {
                if (Rfunc7) Rfunc7.SendCustomEvent(KeyboardInput);
            }
            if (Input.GetKeyDown(Rfunc8key))
            {
                if (Rfunc8) Rfunc8.SendCustomEvent(KeyboardInput);
            }
        }
    }
}