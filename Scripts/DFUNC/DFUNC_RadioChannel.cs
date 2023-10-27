
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using UnityEngine.UI;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_RadioChannel : UdonSharpBehaviour
    {
        public SaccRadioBase RadioBase;
        [Tooltip("Tick to make channel go down when you use it, instead of up")]
        public bool ChannelDown;
        [SerializeField] private TextMeshProUGUI ChannelNumber_TMP;
        [SerializeField] private Text ChannelNumber_text;
        private bool Selected;
        bool UseLeftTrigger;
        public void DFUNC_LeftDial() { UseLeftTrigger = true; }
        public void DFUNC_RightDial() { UseLeftTrigger = false; }
        private bool TriggerLastFrame;
        private void Update()
        {
            if (Selected)
            {
                float Trigger;
                if (UseLeftTrigger)
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
                else
                { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }
                if (Trigger > 0.75)
                {
                    if (!TriggerLastFrame)
                    {
                        ChangeChannel();
                    }
                    TriggerLastFrame = true;
                }
                else { TriggerLastFrame = false; }
            }
        }
        private void ChangeChannel()
        {
            if (ChannelDown)
            {
                RadioBase.DecreaseChannel();
            }
            else
            {
                RadioBase.IncreaseChannel();
            }
            if (ChannelNumber_text)
            { ChannelNumber_text.text = RadioBase.MyChannel.ToString(); }
            if (ChannelNumber_TMP)
            { ChannelNumber_TMP.text = RadioBase.MyChannel.ToString(); }
        }
        public void KeyboardInput()
        {
            ChangeChannel();
        }
    }
}