
using UnityEngine;
using UdonSharp;

namespace SaccFlightAndVehicles.KitKat
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TrajectoryPredictor : UdonSharpBehaviour
    {
        #region SERIALIZED FIELDS

        [Header("Dependencies")]
        [Tooltip("This should be 'InVehicleOnly/AtGCam'")]
        [SerializeField] private Camera atgCamera;
        [Tooltip("This should be 'InVehicleOnly/HUDController'")]
        [SerializeField] private Transform hudControlTransform;

        [Space]
        [Tooltip("This should be 'bigstuff/hud_velocity'")]
        [SerializeField] private Transform linkedHudVelocityVector;
        [Tooltip("This should be the root prefab called hud_CCIP")]
        [SerializeField] private Transform hudCcip;
        [Tooltip("This should be Bone.002")]
        [SerializeField] private Transform topOfCcipLine;

        [Space]
        [SerializeField] private Rigidbody vehicleRigidbody;
        [SerializeField] private Rigidbody bombRigidbody;

        [Header("Settings")]
        [Tooltip("This should be the same value as whatever you have set in 'SAV_HUD Controller'. I refrain from directly referencing it by type and automating this process so people can use custom hud controller scripts.")]
        [SerializeField] private float distanceFromHead = 1.333f;
        [Tooltip("This offsets the top of the CCIP line so you can make it look like it attaches to the hud_velocity icon. It's adjustable in case you want to use a different velocity icon.")]
        [SerializeField] private float lineOffset = -9;
        [Tooltip("The camera's fov is distance * atgCamZoom. It is clamped between 1 and 60 degrees.")]
        [SerializeField] private float atgCamZoom = 100f;
        [Tooltip("How rough the trajectory resolution will be, measured in seconds of flight time along the trajectory. If this number is bigger you'll get a 'lower poly' trajectory calculation, and you may save some performance. If this is lower, you'll get a smoother trajectory prediction at a high performance cost. 1 seems to be the sweet spot.")]
        [SerializeField] private float secondsBetweenRaycast = 1;
        [Tooltip("The max time the simulation will predict into the future. It's wise to stop it at some point so you won't get 0 fps if the trajectory misses the whole world and enters the void. If you have bombs that will self detonate after some time, it's probably wise to stop the prediction from calculating much further.")]
        [SerializeField] private float bombLifeTime = 45;

        #endregion // SERIALIZED FIELDS

        #region PRIVATE FIELDS

        private float _stepsPerSecond;
        private bool _hitdetect;
        private Vector3 _groundZero;
        private float _fixedDeltaTime;
        private int _stepsToPredict;

        private Vector3 _gravity;

        private float _drag;
        private float _dragConstant;

        private int _trajectoryResolution;

        private Transform _atgCameraTransform;

        private bool _gameObjectEnabled;

        #endregion // PRIVATE FIELDS

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        private void OnEnable()
        {
            if (!_initialized) Init();
            _gameObjectEnabled = true;
        }

        private void OnDisable()
        {
            _gameObjectEnabled = false;
            if (hudCcip) hudCcip.gameObject.SetActive(false);
            if (_atgCameraTransform) _atgCameraTransform.rotation = Quaternion.identity;
        }


        private bool _initialized;
        private void Init()
        {
            if (_initialized) return;
            _initialized = true;

            _gravity = Physics.gravity;
            _fixedDeltaTime = Time.fixedDeltaTime;

            _stepsPerSecond = 1 / _fixedDeltaTime;

            _drag = bombRigidbody.drag;
            if (_drag <= 0)
            {
                LogError("Sorry! This trajectory predictor only works with drag. I was lazy so please ensure you are using a nonzero drag value.");
                _drag = bombRigidbody.drag = 0.00000001f;
                LogError($"Bomb drag was automatically set to {_drag}. A really small number!");
            }

            _dragConstant = 1 - _drag * _fixedDeltaTime;

            _stepsToPredict = (int)(_stepsPerSecond * secondsBetweenRaycast);
            _trajectoryResolution = (int)(bombLifeTime / secondsBetweenRaycast);

            if (atgCamera) _atgCameraTransform = atgCamera.transform;
        }


        private void LateUpdate()
        {
            if (!_gameObjectEnabled) return; // https://feedback.vrchat.com/udon/p/update-is-executed-for-one-frame-after-the-script-is-disabled
            ApproximateRigidbodyTrajectory(transform.position, vehicleRigidbody.velocity);
            UpdateHud();
        }

        private void ApproximateRigidbodyTrajectory(Vector3 startPos, Vector3 startVelocity)
        {
            Vector3 constants = ((startVelocity * _drag - _gravity) * _dragConstant + _gravity * _dragConstant) / _drag;
            Vector3 nextVelocity = (Mathf.Pow(_dragConstant, _stepsToPredict - 1) * (constants * _drag - _gravity * _dragConstant) + _gravity) / _drag;
            Vector3 nextPos = _fixedDeltaTime * (Mathf.Pow(_dragConstant, _stepsToPredict) * (constants * _drag - _dragConstant * _gravity) + _gravity * ((_dragConstant - 1) * _stepsToPredict + _dragConstant) - constants * _drag) / ((_dragConstant - 1) * _drag) + startPos;

            _hitdetect = false;
            for (int i = 1; i < _trajectoryResolution; i++)
            {
                constants = ((nextVelocity * _drag - _gravity) * _dragConstant + _gravity * _dragConstant) / _drag;
                nextVelocity = (Mathf.Pow(_dragConstant, _stepsToPredict - 1) * (constants * _drag - _gravity * _dragConstant) + _gravity) / _drag;
                Vector3 lastPredictedPos = nextPos;
                nextPos = _fixedDeltaTime * (Mathf.Pow(_dragConstant, _stepsToPredict) * (constants * _drag - _dragConstant * _gravity) + _gravity * ((_dragConstant - 1) * _stepsToPredict + _dragConstant) - constants * _drag) / ((_dragConstant - 1) * _drag) + lastPredictedPos;

                if (!Physics.Raycast(lastPredictedPos, nextPos - lastPredictedPos, out RaycastHit hit, (nextPos - lastPredictedPos).magnitude + 2, 2065 /* Default, Water and Environment */, QueryTriggerInteraction.Ignore)) continue;

                _hitdetect = true;
                _groundZero = hit.point;
                return;
            }
        }

        private void UpdateHud()
        {
            Vector3 ccipLookDir = _groundZero - hudControlTransform.position;

            float dirAngleCorr = Vector3.SignedAngle(
                Vector3.ProjectOnPlane(ccipLookDir, Vector3.up),
                Vector3.ProjectOnPlane(linkedHudVelocityVector.forward, Vector3.up), Vector3.up);

            ccipLookDir = Quaternion.AngleAxis(dirAngleCorr, Vector3.up) * ccipLookDir;

            hudCcip.gameObject.SetActive(_hitdetect);

            if (_hitdetect)
            {
                Quaternion lookAtPlaneUp = Quaternion.LookRotation(-ccipLookDir, Vector3.up);

                hudCcip.position = hudControlTransform.position + ccipLookDir.normalized;
                hudCcip.localPosition = hudCcip.localPosition.normalized * distanceFromHead;
                hudCcip.rotation = lookAtPlaneUp;

                topOfCcipLine.SetPositionAndRotation(
                    linkedHudVelocityVector.position + Vector3.ProjectOnPlane(Vector3.up, linkedHudVelocityVector.forward).normalized * lineOffset,
                    lookAtPlaneUp);
            }

            if (atgCamera)
            {
                float newzoom = Mathf.Clamp(2.0f * Mathf.Atan(atgCamZoom * 0.5f / ccipLookDir.magnitude) * Mathf.Rad2Deg, 1.5f, 90);
                _atgCameraTransform.rotation = Quaternion.LookRotation(ccipLookDir, vehicleRigidbody.transform.up);
                atgCamera.fieldOfView = newzoom;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Enjoy reading this I guess :)

        /*
        public float DistanceBeforeRaycast = 30;
        public void SimulateTrajectory(Vector3 startPosition, Vector3 startVelocity)
        {
            bool hitdetect = false;
            float accumulatedDistance = 0;

            for (int i = 0; i < iterations; i++)
            {
                startPosition += startVelocity * iterationTime + _gravity * iterationTime * iterationTime * 0.5f; // Accounts for gravity and predicts the next position at that time step.

                startVelocity += _gravity * iterationTime;

                startVelocity = startVelocity * (1 - iterationTime * _drag); // This works pretty well but is slightly off...

                Vector3 posOffset = startVelocity * iterationTime; // This is the actual position the bomb will be at the current timestep.
                accumulatedDistance += startVelocity.magnitude;

                if (i > IterationsBeforeCollisionCheck && !hitdetect && accumulatedDistance > DistanceBeforeRaycast) // Iterations before collision check is to prevent the plane from marking itself as a hit. It then does a raycast that extends for 2m further than the next position.
                {
                    accumulatedDistance = 0;
                    if (Physics.Raycast(startPosition, posOffset, out var hit, DistanceBeforeRaycast + 2))
                    {
                        hitdetect = true;
                        _groundZero = hit.point;
                    }
                }
            }
        }
        */

        private void PredictRigidbodyPhysics(
            Vector3 startPos, Vector3 startVel, Vector3 gravity,
            float drag, float fixedDeltaTime, int stepsToPredict,
            out Vector3 endPos, out Vector3 endVel)
        {
            if (drag == 0)
            {
                endPos = startPos + 0.5f * stepsToPredict * fixedDeltaTime * (2 * startVel + (stepsToPredict + 1) * fixedDeltaTime * gravity);
                endVel = startVel + fixedDeltaTime * stepsToPredict * gravity;
            }
            else
            {
                //float a = 1 - drag * fixedDeltaTime;

                //Vector3 constants = ((startVel * drag - gravity) * a + gravity * a) / drag;
                //endVel = (Mathf.Pow(a, stepsToPredict - 1) * (constants * drag - gravity * a) + gravity) / drag;
                //endPos = fixedDeltaTime * (Mathf.Pow(a, stepsToPredict) * (constants * drag - a * gravity) + gravity * ((a - 1) * stepsToPredict + a) - constants * drag) / ((a - 1) * drag) + startPos;

                Vector3 vel = Vector3.zero;
                Vector3 pos = Vector3.zero;

                for (int n = 1; n <= stepsToPredict; n++)
                {
                    // vel = (vel + gravity * fixedDeltaTime) * drag * dt;
                    vel += gravity * fixedDeltaTime;
                    vel *= drag * fixedDeltaTime;
                    pos += vel * fixedDeltaTime;
                }

                endPos = pos;
                endVel = vel;
            }
        }

        #endregion

        private void LogError(string text)
        {
            Debug.LogError($"<size=16>[<color=cyan>KitKat</color>] <color=white>{name}</color> : <color=red>{text}</color></size>", this);
        }
    }
}