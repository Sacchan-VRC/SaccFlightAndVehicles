
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SaccResupplyTrigger : UdonSharpBehaviour
    {
        [Tooltip("Object to send event to")]
        public SaccEntity SendEventTo;
        [Tooltip("Delay after entering a respply zone before the event is first sent")]
        public float ResupplyInitialDelay = 1;
        [Tooltip("Delay between resupplies")]
        public float ResupplyDelay = 1;
        [Tooltip("Layer to check for resupply triggers on")]
        public int ResupplyLayer = 27;
        [Tooltip("Name of event sent by this trigger")]
        public string EventName = "SFEXT_O_ReSupply";
        private float LastResupplyTime = 0;
        private int NumTriggers = 0;
        private bool InResupplyZone;
        private Collider ThisCollider;
        private bool Initialized = false;
        private void Initialize()
        {
            ThisCollider = gameObject.GetComponent<Collider>();
        }
        private void Update()
        {
            if (InResupplyZone)
            {
                if (Time.time - LastResupplyTime > ResupplyDelay)
                {
                    LastResupplyTime = Time.time;
                    SendEventTo.SendEventToExtensions(EventName);
                }
            }
        }
        private void OnTriggerEnter(Collider other)
        {
            if (other && other.gameObject.layer == ResupplyLayer)
            {
                float tim = Time.time;
                if (NumTriggers == 0) { LastResupplyTime = Mathf.Min((tim + ResupplyDelay) - ResupplyInitialDelay, tim); }
                NumTriggers += 1;
                InResupplyZone = true;
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (other && other.gameObject.layer == ResupplyLayer)
            {
                NumTriggers -= 1;
                if (NumTriggers == 0) { InResupplyZone = false; }
            }
        }
        //collider enabled and disabled so that it does ontriggerenter on enable
        private void OnEnable()
        {
            if (!Initialized) { Initialize(); }
            ThisCollider.enabled = true;
        }
        private void OnDisable()
        {
            ThisCollider.enabled = false;
            InResupplyZone = false;
            NumTriggers = 0;
        }
    }
}