
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SaccEntitySendEvent : UdonSharpBehaviour
    {
        public SaccEntity EntityControl;
        [Tooltip("Name of entity event to send to the SaccEntity (send to all extensions)")]
        public string EntityEvent_Name = "SFEXT_O_RespawnButton";
        [Tooltip("Name of event to send to the SaccEntity (just sent to entity)")]
        public bool EntityEventGlobal = false;
        public UdonSharpBehaviour[] OtherScripts;
        public string OtherScripts_Event_Name;
        public bool OtherScript_EventGlobal = false;
        [Tooltip("(Optional) Animator to send Trigger to")]
        [SerializeField] Animator AnimTriggerAnimator;
        [SerializeField] string AnimTriggerName;
        [SerializeField] bool AnimTrigger_Global;
        public AudioSource Sound;
        [SerializeField] bool Sound_Global;
        private bool BothGlobal;
        bool initialized;
        void Start() { Initialize(); }
        void Initialize()
        {
            if (initialized) return;
            initialized = true;
            if (EntityEvent_Name == string.Empty) { EntityEventGlobal = false; }
            if (OtherScripts_Event_Name == string.Empty) { OtherScript_EventGlobal = false; }
            if (EntityEventGlobal && OtherScript_EventGlobal) { BothGlobal = true; }
        }
        public override void Interact()
        {
            if (BothGlobal)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(Event_both));
                return;
            }
            else
            {
                if (EntityEventGlobal)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(EntityEvent));
                }
                else
                { EntityEvent(); }
                if (OtherScript_EventGlobal)
                {
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(NormalEvent));
                }
                else
                { NormalEvent(); }
            }
            if (AnimTriggerAnimator && (AnimTriggerName != string.Empty))
            {
                if (AnimTrigger_Global)
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(AnimTrigger));
                else
                    AnimTrigger();
            }
            if (Sound)
            {
                if (Sound_Global)
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(PlaySound));
                else
                    PlaySound();
            }
        }
        public void Event_both()
        {
            if (EntityEvent_Name != string.Empty)
            {
                EntityControl.SendEventToExtensions(EntityEvent_Name);
            }
            if (OtherScripts_Event_Name != string.Empty)
            {
                for (int i = 0; i < OtherScripts.Length; i++)
                {
                    OtherScripts[i].SendCustomEvent(OtherScripts_Event_Name);
                }
            }
        }
        public void EntityEvent()
        {
            if (EntityEvent_Name != string.Empty)
            {
                EntityControl.SendEventToExtensions(EntityEvent_Name);
            }
        }
        public void NormalEvent()
        {
            if (OtherScripts_Event_Name != string.Empty)
            {
                for (int i = 0; i < OtherScripts.Length; i++)
                {
                    OtherScripts[i].SendCustomEvent(OtherScripts_Event_Name);
                }
            }
        }
        public void AnimTrigger()
        {
            AnimTriggerAnimator.SetTrigger(AnimTriggerName);
        }
        public void PlaySound()
        {
            Sound.PlayOneShot(Sound.clip);
        }

        // can now be used as a DFUNC
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        [System.NonSerializedAttribute] public SAV_PassengerFunctionsController PassengerFunctionsControl;
        private bool TriggerLastFrame;
        bool controlsActive = false;
        public void SFEXT_L_EntityStart() { Initialize(); }
        public void ControlInputs()
        {
            if (!controlsActive) { return; }
            float Trigger;
            if (LeftDial)
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
            else
            { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

            if (Trigger > 0.75)
            {
                if (!TriggerLastFrame)
                {
                    Interact();
                }
                TriggerLastFrame = true;
            }
            else { TriggerLastFrame = false; }
            SendCustomEventDelayedFrames(nameof(ControlInputs), 1);
        }
        public void KeyboardInput()
        {
            Interact();
        }
        public void DFUNC_Deselected()
        {
            controlsActive = false;
        }
        public void DFUNC_Selected()
        {
            TriggerLastFrame = true;
            if (!controlsActive)
            {
                controlsActive = true;
                ControlInputs();
            }
        }
        public void SFEXT_O_PilotExit()
        {
            gameObject.SetActive(false);
            controlsActive = false;
        }
    }
}