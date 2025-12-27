using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SimulatedPlayer : MonoBehaviour
{
    [SerializeField] private Transform _startTransform;
    [SerializeField] [Range(60.0f, 90.0f)] private float _baselineHeartRate = 75.0f;
    [SerializeField] [Range(0.5f, 1.5f)] private float _sensitivity = 1.0f;
    [SerializeField] [Range(8.0f, 18.0f)] private float _recoverySpeed = 13.0f;
    [SerializeField] private float _noiseLevel = 1.0f;
    [SerializeField] private Transform _groundTransform;
    [SerializeField] private Bounds _movementBounds = new Bounds();
    private Vector3 _currentTargetPosition;

    private float _currentHeartRate = 0.0f;
    private float _averageHeartRate = 0.0f; // Averages are over past second
    private float _averageSpeed = 0.0f;
    private float _averageRotationSpeed = 0.0f;

    private Rigidbody _rigidbody;
    private CapsuleCollider _collider;
    private NavMeshAgent _navMeshAgent;

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
    public CapsuleCollider Collider { get { return _collider; } }
    #endregion

    #region Awake
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<CapsuleCollider>();
        _navMeshAgent = GetComponent<NavMeshAgent>();
        SetNewTargetPosition();
    }
    #endregion

    #region logic
    public void ApplyEvent(Intensity intensity)
    {
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
        if ((_currentTargetPosition - transform.position).sqrMagnitude < 2.0f * 2.0f) SetNewTargetPosition();
    }

    private void SetNewTargetPosition()
    {
        float randomX = _groundTransform.position.x + _movementBounds.center.x + Random.Range(-_movementBounds.extents.x, _movementBounds.extents.x);
        float randomY = _groundTransform.position.y + _movementBounds.center.y + Random.Range(-_movementBounds.extents.y, _movementBounds.extents.y);
        float randomZ = _groundTransform.position.z + _movementBounds.center.z + Random.Range(-_movementBounds.extents.z, _movementBounds.extents.z);
        _currentTargetPosition = new Vector3(randomX, randomY, randomZ);
        _navMeshAgent.SetDestination(_currentTargetPosition);

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
        _averageSpeed = 0.0f;
        _averageRotationSpeed = 0.0f;

        StartCoroutine(CalculateAverageLoop());
    }
    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(_currentTargetPosition, 2.0f);
    }
}
