
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class SaccEntitySendEvent : UdonSharpBehaviour
    {
        [Header("OtherScripts is for sending regular udon events to specific scripts\nRespawning is a regular event for SaccEntity. (it will tell extensions automatically)")]
        public UdonSharpBehaviour[] OtherScripts;
        [Tooltip("Name of event to send to OtherScripts (normal udon events)")]
        public string OtherScripts_Event_Name = string.Empty;
        public bool OtherScript_EventGlobal = false;
        [Space(20)]
        [Header("EntityControl reference is for sending entity events to extensions (but not to SaccEntity itself)")]
        public SaccEntity EntityControl;
        [Tooltip("Name of entity event to send to the SaccEntity (send to all extensions)")]
        public string EntityEventName = string.Empty;
        public bool EntityEventGlobal = false;
        [Space(20)]
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
            if (EntityEventName == string.Empty) { EntityEventGlobal = false; }
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
            if (EntityEventName != string.Empty)
            {
                EntityControl.SendEventToExtensions(EntityEventName);
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
            if (EntityEventName != string.Empty)
            {
                EntityControl.SendEventToExtensions(EntityEventName);
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
        [Tooltip("If on a pickup: Use VRChat's OnPickupUseDown functionality")]
        [SerializeField] bool use_OnPickupUseDown = false;
        [System.NonSerializedAttribute] public bool LeftDial = false;
        [System.NonSerializedAttribute] public int DialPosition = -999;
        private bool TriggerLastFrame;
        bool controlsActive = false;
        public void SFEXT_L_EntityStart() { Initialize(); }
        public void ControlInputs()
        {
            if (!controlsActive) { return; }
            float Trigger;
            if (use_OnPickupUseDown)
                Trigger = PickupTrigger;
            else
            {
                if (LeftDial)
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                else
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
            }

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
        public void SFEXT_O_OnDrop()
        {
            SFEXT_O_PilotExit();
        }
        private int PickupTrigger = 0;
        public void SFEXT_O_OnPickupUseDown()
        {
            PickupTrigger = 1;
        }
        public void SFEXT_O_OnPickupUseUp()
        {
            PickupTrigger = 0;
        }
    }
}