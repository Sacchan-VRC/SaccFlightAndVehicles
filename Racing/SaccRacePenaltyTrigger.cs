
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SaccRacePenaltyTrigger : UdonSharpBehaviour
    {
        [Header("Put this script on a trigger on the 'Racing' layer\n and put the object inside the RaceObjs of a race")]
        [SerializeField] float Penalty = 10;
        [SerializeField] float ReTriggerDelay = 10;
        float TriggerTime;
        void OnTriggerEnter(Collider other)
        {
            if (other)
            {
                SaccRacingTrigger ort = other.gameObject.GetComponent<SaccRacingTrigger>();
                if ((Time.time - TriggerTime > ReTriggerDelay) && ort && ort.RaceOn)
                {
                    TriggerTime = Time.time;
                    ort.RecievedPenalty = Penalty;
                    ort.AddPenalty();
                }
            }
        }
    }
}