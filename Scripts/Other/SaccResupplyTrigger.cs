
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
        public UdonSharpBehaviour SendEventTo;
        [Tooltip("Delay after entering a respply zone before the event is first sent")]
        public float ResupplyInitialDelay = 1;
        [Tooltip("Delay between resupplies")]
        public float ResupplyDelay = 1;
        [Tooltip("Layer to check for resupply triggers on")]
        public int ResupplyLayer = 27;
        [Tooltip("Name of event sent by this trigger")]
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
                    switch (supplyType)
                    {
                        case -1: // ALL
                            SendEventTo.SendCustomEvent("ReSupply");
                            break;
                        case 0: // FUEL
                            SendEventTo.SendCustomEvent("ReFuel");
                            break;
                        case 1: // AMMO
                            SendEventTo.SendCustomEvent("ReArm");
                            break;
                        case 2: // REPAIRS
                            SendEventTo.SendCustomEvent("RePair");
                            break;
                    }
                }
            }
        }
        int supplyType = -1; // Default to -1 if no matching child is found
        string[] supplyNames = { "xSUPPLY_FUEL", "xSUPPLY_AMMO", "xSUPPLY_REPAIR" };
        private void OnTriggerEnter(Collider other)
        {
            if (other && other.gameObject.layer == ResupplyLayer)
            {
                supplyType = -1;
                if (other.transform.childCount > 0)
                {
                    for (int i = 0; i < supplyNames.Length; i++)
                    {
                        Transform child = other.transform.Find(supplyNames[i]);
                        if (child != null)
                        {
                            supplyType = i; // Set supplyType to the index of the matching child
                            break;          // Exit the loop once a match is found
                        }
                    }
                }
                if (NumTriggers == 0) { LastResupplyTime = Time.time - ResupplyDelay + ResupplyInitialDelay; }
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