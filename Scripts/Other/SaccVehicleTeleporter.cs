
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SaccVehicleTeleporter : UdonSharpBehaviour
    {
        public Transform TeleportPoint;
        public bool OffsetTelePoint = true;
        [Tooltip("OffsetTelePoint Must be ticked for this to work")]
        public bool OffSetZOnly = true;
        public bool OffsetRotation = false;
        public bool DontSetRotation = false;
        private float LastTeleTime;
        private void OnTriggerEnter(Collider other)
        {
            if (Time.time - LastTeleTime < 1 || !other.attachedRigidbody || !Networking.LocalPlayer.IsOwner(other.attachedRigidbody.gameObject)) { return; }
            var otherSE = other.attachedRigidbody.GetComponent<SaccEntity>();
            if (!otherSE || otherSE.Holding) { return; }
            otherSE.SetDeadFor(Time.fixedDeltaTime * 2f);
            LastTeleTime = Time.time;
            if (OffsetTelePoint)
            {
                if (OffSetZOnly)
                {
                    var relpos = transform.InverseTransformDirection(otherSE.transform.position - transform.position);
                    relpos.x = 0;
                    relpos.z = 0;
                    otherSE.transform.position = TeleportPoint.TransformDirection(relpos) + TeleportPoint.position;
                }
                else
                {
                    var relpos = transform.InverseTransformDirection(otherSE.transform.position - transform.position);
                    otherSE.transform.position = TeleportPoint.TransformDirection(relpos) + TeleportPoint.position;
                }
            }
            else
            {
                otherSE.transform.position = TeleportPoint.position;
            }
            if (!DontSetRotation)
            {
                if (OffsetRotation)
                {
                    //doesnt work perfectly stuff with more than just yaw
                    Quaternion RotDif = transform.rotation * TeleportPoint.rotation;
                    otherSE.transform.rotation *= RotDif;
                    other.attachedRigidbody.velocity = RotDif * other.attachedRigidbody.velocity;
                }
                else
                {
                    otherSE.transform.rotation = TeleportPoint.rotation;
                    var speed = other.attachedRigidbody.velocity.magnitude;
                    other.attachedRigidbody.velocity = TeleportPoint.forward * speed;
                }
            }
        }
    }
}