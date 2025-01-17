﻿
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
                    SendEventTo.SendCustomEvent("ReSupply");
                }
            }
        }
        private void OnTriggerEnter(Collider other)
        {
            if (other && other.gameObject.layer == ResupplyLayer)
            {
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