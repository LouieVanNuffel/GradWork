using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class SimulatedPlayer : MonoBehaviour
{
    [SerializeField] private Transform _startTransform;
    [SerializeField] private SplineContainer _walkPathSpline;
    [SerializeField] [Range(60.0f, 90.0f)] private float _baselineHeartRate = 75.0f;
    [SerializeField] [Range(0.5f, 1.5f)] private float _sensitivity = 1.0f;
    [SerializeField] [Range(8.0f, 18.0f)] private float _recoverySpeed = 13.0f;
    [SerializeField] private float _noiseLevel = 1.0f;
    [SerializeField] private float _movementSpeed = 5.0f;
    [SerializeField] private float _rotationSpeed = 90.0f;
    [SerializeField] private float _eventListenRange = 5.0f;

    private float _currentHeartRate = 0.0f;
    private float _averageHeartRate = 0.0f; // Averages are over past second
    private float _averageSpeed = 0.0f;
    private float _averageRotationSpeed = 0.0f;

    Rigidbody _rigidbody;

    // Simulated movement
    private float _currentT = -1.0f;

    // Events
    [SerializeField] private Event[] _events;

    private void Update()
    {
        UpdateHeartRate();
    }

    private void FixedUpdate()
    {
        UpdateMovement();
    }

    #region Getters
    public float CurrentHeartRate { get { return _currentHeartRate; } }
    public float AverageHeartRate { get{ return _averageHeartRate; } }
    public float AverageSpeed { get { return _averageSpeed; } }
    public float AverageRotationSpeed {  get { return _averageRotationSpeed; } }
    #endregion

    #region Subscriptions And GetComponent
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        foreach (Event evt in _events)
        {
            evt.OnEventHappened += ApplyEvent;
        }
    }

    private void OnDisable()
    {
        foreach (Event evt in _events)
        {
            evt.OnEventHappened -= ApplyEvent;
        }
    }
    #endregion

    #region logic
    public void ApplyEvent(Intensity intensity, Vector3 eventPosition)
    {
        // Check if event within range
        if ((eventPosition - transform.position).sqrMagnitude > _eventListenRange * _eventListenRange)
        {
            return;
        }

        // Apply Event
        float increase = 0.0f;
        switch (intensity)
        {
            case Intensity.Low:
                increase = 10.0f;
                break;
            case Intensity.Medium:
                increase = 15.0f;
                break;
            case Intensity.High:
                increase = 20.0f;
                break;
            default:
                increase = 10.0f;
                break;
        }

        _currentHeartRate += increase * _sensitivity;
    }

    private void UpdateHeartRate()
    {
        // Recovery to baseline
        float difference = _baselineHeartRate - _currentHeartRate;
        float recoveryRate = Time.deltaTime / _recoverySpeed;
        _currentHeartRate += difference * recoveryRate;

        // Noise
        float noise = UnityEngine.Random.Range(-_noiseLevel, _noiseLevel);
        _currentHeartRate += noise * Time.deltaTime;

        // Hard clamp within realistic range
        _currentHeartRate = Mathf.Clamp(_currentHeartRate, 40.0f, 220.0f);
    }

    private IEnumerator CalculateAverageLoop()
    {
        // Samples 10 times per second for results that don't depend on fps
        while (true)
        {
            float averageHeartRate = 0.0f;
            float averageSpeed = 0.0f;
            float averageRotationSpeed = 0.0f;

            float timer = 0.0f;
            int steps = 10;
            while (timer < 1.0f)
            {
                averageHeartRate += _currentHeartRate;
                averageSpeed += _rigidbody.linearVelocity.magnitude;
                averageRotationSpeed += _rigidbody.angularVelocity.magnitude;
                yield return new WaitForSeconds(1.0f / (float)steps);
                timer += 1.0f / (float)steps;
            }

            averageHeartRate /= (float)steps;
            averageSpeed /= (float)steps;
            averageRotationSpeed /= (float)steps;

            _averageHeartRate = averageHeartRate;
            _averageSpeed = averageSpeed;
            _averageRotationSpeed = averageRotationSpeed;

            Debug.Log($"HR: {_averageHeartRate}, MovSpeed: {_averageSpeed}, RotSpeed: {_averageRotationSpeed}");
        }
    }

    private void UpdateMovement()
    {
        if (_walkPathSpline == null)
        {
            Debug.LogWarning("No spline was assigned in the editor.");
            return;
        }

        // Convert current world position to spline space
        float3 localPos = _walkPathSpline.transform.InverseTransformPoint(transform.position);

        // Initialize t only once, based on nearest point
        if (_currentT < 0.0f) SplineUtility.GetNearestPoint(_walkPathSpline.Spline, localPos, out _, out _currentT);

        // Advance t by linear distance
        float advanceDistance = _movementSpeed * Time.fixedDeltaTime;
        SplineUtility.GetPointAtLinearDistance(_walkPathSpline.Spline, _currentT, advanceDistance, out float resultT);

        _currentT = resultT;
        _currentT %= 1f;
        if (_currentT < 0f) _currentT += 1f;

        // Evaluate new spline point
        SplineUtility.Evaluate(_walkPathSpline.Spline, _currentT,
            out float3 targetLocalPos,
            out float3 targetTangent,
            out float3 targetUpVector);

        // Convert to world space
        Vector3 targetWorldPos = _walkPathSpline.transform.TransformPoint((Vector3)targetLocalPos);

        Quaternion targetRotation = Quaternion.LookRotation(targetTangent, targetUpVector);
        MoveAndRotate(targetWorldPos, targetRotation);

        Debug.DrawRay(transform.position, transform.forward);
    }

    private void MoveAndRotate(Vector3 targetPosition, Quaternion targetRotation)
    {
        //Move
        Vector3 directionToTarget = targetPosition - transform.position;

        // Spline attraction force
        float stiffness = 500f;
        float damping = 5f;
        Vector3 springForce = directionToTarget * stiffness - _rigidbody.linearVelocity * damping;

        // Desired forward velocity
        Vector3 forwardVel = transform.forward * _movementSpeed;

        // Total physics force
        Vector3 desiredVelChange = (forwardVel - _rigidbody.linearVelocity);

        _rigidbody.AddForce(desiredVelChange + springForce * Time.fixedDeltaTime, ForceMode.VelocityChange);

        // Rotate
        Quaternion deltaRot = targetRotation * Quaternion.Inverse(transform.rotation);

        // Convert to axis-angle
        deltaRot.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;

        // Compute desired angular velocity to rotate toward spline
        Vector3 desiredAngularVelocity = axis.normalized * angle * Mathf.Deg2Rad * _rotationSpeed;

        // Apply torque to match angular velocity
        _rigidbody.AddTorque(desiredAngularVelocity - _rigidbody.angularVelocity, ForceMode.VelocityChange);
    }

    public void ResetPlayerState()
    {
        StopAllCoroutines();

        // transform
        transform.position = _startTransform.position;
        transform.rotation = _startTransform.rotation;

        // observation values
        _currentHeartRate = _baselineHeartRate;
        _averageHeartRate = _currentHeartRate;
        _averageSpeed = _movementSpeed;
        _averageRotationSpeed = _rotationSpeed;

        StartCoroutine(CalculateAverageLoop());
    }
    #endregion
}
