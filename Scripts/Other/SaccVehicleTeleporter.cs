
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
        [Tooltip("Instead of moving vehicle to exactly the teleport point, move to relative offset of the point of where you entered")]
        public bool OffsetTelePoint = true;
        [Tooltip("If OffsetTelePoint: Only use relative for Z position (so vehicles don't get put into the ground), vehicles will be moved to the center of the TeleportPoint, at the height they entered.")]
        public bool OffSetZOnly = true;
        [Tooltip("Exit the teleporter at the offset of the angle you entered it?")]
        public bool OffsetRotation = false;
        public bool DontSetRotation = false;
        [Tooltip("Stop vehicle from rotating after it exits the teleporter?")]
        public bool SetAngVelZero = false;
        private float LastTeleTime;
        private void OnTriggerEnter(Collider other)
        {
            if (Time.time - LastTeleTime < 1 || !other.attachedRigidbody || !Networking.LocalPlayer.IsOwner(other.attachedRigidbody.gameObject)) { return; }
            Rigidbody otherRB = other.attachedRigidbody;
            SaccEntity otherSE = otherRB.GetComponent<SaccEntity>();
            if (!otherSE || otherSE.Holding) { return; }
            otherSE.ShouldTeleport = true;
            otherSE.SetNoGsFor(.5f);
            LastTeleTime = Time.time;
            if (OffsetTelePoint)
            {
                var relpos = transform.InverseTransformDirection(otherSE.transform.position - transform.position);
                if (OffSetZOnly)
                {
                    relpos.x = 0;
                    relpos.z = 0;
                }
                otherSE.transform.position = TeleportPoint.TransformDirection(relpos) + TeleportPoint.position;
            }
            else
            {
                otherSE.transform.position = TeleportPoint.position;
            }
            if (!DontSetRotation)
            {
                if (OffsetRotation)
                {
                    Quaternion RotDif = TeleportPoint.rotation * Quaternion.Inverse(transform.rotation);
                    otherSE.transform.rotation = RotDif * otherSE.transform.rotation;
                    otherRB.velocity = RotDif * otherRB.velocity;
                }
                else
                {
                    otherSE.transform.rotation = TeleportPoint.rotation;
                    var speed = otherRB.velocity.magnitude;
                    otherRB.velocity = TeleportPoint.forward * speed;
                }
            }
            if (SetAngVelZero)
            {
                otherRB.angularVelocity = Vector3.zero;
            }
            otherRB.position = otherSE.transform.position;
            otherRB.rotation = otherSE.transform.rotation;
            otherSE.SendEventToExtensions("SFEXT_O_VehicleTeleported");
        }
    }
}